using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Audio;
using System.Xml;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;
using System.Text;
using TMPro;
using System.Reflection;
using System.Runtime;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
    using LapinerTools.Steam.Data;
    using LapinerTools.uMyGUI;
#endif

public partial class UIManagerScript
{
    public IEnumerator MainGameStart()
    {

        HoverInfoScript.InitializeSelectIcon();

        float timeAtStart = Time.realtimeSinceStartup;

        // make sure next/prev page buttons have text!
        if (nextPageResponseButton != null && string.IsNullOrEmpty(nextPageResponseButton.buttonText))
        {
            nextPageResponseButton.buttonText = StringManager.GetString("misc_nextpage");
            previousPageResponseButton.buttonText = StringManager.GetString("misc_previouspage");
        }
        dynamicCanvasRaycaster = GameObject.Find("DynamicCanvas").GetComponent<GraphicRaycaster>();

        eqWeaponSprites = new Image[4];
        for (int i = 0; i < eqWeaponSprites.Length; i++)
        {
            eqWeaponSprites[i] = GameObject.Find("Weapon" + (i + 1)).GetComponent<Image>();
        }

        mainUINavContainer = GameObject.Find("MainUINav");
        csStatBars = mainUINavContainer.GetComponent<CSStatBars>();


        dictFullScreenUI = new Dictionary<UITabs, ImpactUI_Base>();
        foreach (ImpactUI_Base ui in FullscreenUIObjectsInScene)
        {
            try { dictFullScreenUI.Add(ui.GetUIType(), ui); }
            catch (Exception e)
            {
                Debug.Log(ui.gameObject.name + " " + ui.GetUIType().ToString() + " is already in dict of ui elements " + e);
            }
            ui.TurnOff();
        }

        endingCanvas = GameObject.Find("EndingCanvas").GetComponent<RectTransform>();

        creditsRoll = GameObject.Find("Credits Container").GetComponent<CreditRollScript>();
        endingCutscene = GameObject.Find("EndingCutsceneCanvasContainer").GetComponent<EndingCutsceneManager>();

        endingCanvas.gameObject.SetActive(false);

        creditsRoll.gameObject.SetActive(false);
        endingCutscene.gameObject.SetActive(false);

        nePosHelper = GameObject.Find("NEPosHelper").transform;
        nwPosHelper = GameObject.Find("NWPosHelper").transform;
        sePosHelper = GameObject.Find("SEPosHelper").transform;
        swPosHelper = GameObject.Find("SWPosHelper").transform;

        if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            if (greyButton250x50 == null)
            {
                greyButton250x50 = Resources.Load<Sprite>("Art/UI/Generic Button 250");
            }
            if (pressedGreyButton250x50 == null)
            {
                pressedGreyButton250x50 = Resources.Load<Sprite>("Art/UI/Generic Button 250 Pressed");
            }
        }

        FadingToGame = false;
        singletonUIMS = this;

        statModifierIcons = new List<GameObject>();


        //regenFlask = GameObject.Find("RegenFlask");
        regenFlaskText = GameObject.Find("FlaskText").GetComponent<TextMeshProUGUI>();

        screenOverlayHeaderText = GameObject.Find("ScreenOverlayHeaderText").GetComponent<TextMeshProUGUI>();
        screenOverlayDescText = GameObject.Find("ScreenOverlayDescText").GetComponent<TextMeshProUGUI>();
        screenOverlayTextCGFader = GameObject.Find("ScreenOverlayText").GetComponent<CanvasGroupFader>();
        screenOverlayBG = GameObject.Find("ScreenTextBG");

        screenOverlayHeaderText.lineSpacing = 2f;
        screenOverlayDescText.lineSpacing = 2f;

        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                screenOverlayBG.GetComponent<RectTransform>().sizeDelta = new Vector2(2000f, 370f);
                break;
            default:
                screenOverlayDescText.transform.parent.GetComponent<VerticalLayoutGroup>().spacing = 0f;
                screenOverlayDescText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 170f);
                screenOverlayBG.GetComponent<RectTransform>().sizeDelta = new Vector2(2000f, 240f);
                break;
        }

        FontManager.LocalizeMe(screenOverlayHeaderText, TDFonts.WHITE);
        FontManager.LocalizeMe(screenOverlayDescText, TDFonts.WHITE);

        screenOverlayBGCGFader = screenOverlayBG.GetComponent<CanvasGroupFader>();
        screenOverlayStuff = GameObject.Find("ScreenOverlayStuff");

        introOverlayBG = GameObject.Find("IntroTextBG");
        // For the intro overlay BG, which plays upon game start, we need to adjust size for Japanese only. Just a quirk of the font.
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
                introOverlayBG.GetComponent<RectTransform>().sizeDelta = new Vector2(2000f, 400f);
                break;
        }
        introOverlayDescText = GameObject.Find("IntroOverlayDescText").GetComponent<TextMeshProUGUI>();
        introOverlayBGFader = introOverlayBG.GetComponent<CanvasGroupFader>();
        introOverlayTextFader = introOverlayDescText.GetComponent<CanvasGroupFader>();

        FontManager.LocalizeMe(introOverlayDescText, TDFonts.WHITE);

        screenOverlayStuff.SetActive(false);

        TryLoadDialogCursor();

        //todo #dialogrefactor Take all these inits/sets and make sure they're in the myDialogBoxComponent init
        dialogValueSliderParent = GameObject.Find("DialogValueSlider");
        dialogValueSlider = GameObject.Find("DialogSlider").GetComponent<Slider>();
        dialogValueSliderText = GameObject.Find("DialogSliderText").GetComponent<TextMeshProUGUI>();
        dialogValueSliderParent.SetActive(false);
        dialogValueSliderParent.transform.SetParent(myDialogBoxComponent.transform);

        dialogBoxImageLayoutParent = GameObject.Find("DialogBoxImageLayoutParent");
        dialogBoxImage = GameObject.Find("DialogBoxImage").GetComponent<Image>();
        dialogBoxImageLayoutParent.transform.SetParent(myDialogBoxComponent.transform);
        //dialogBoxImage.transform.SetParent(myDialogBoxComponent.transform);
        HideDialogBoxImage();

        genericTextInput = GameObject.Find("GenericTextInput");
        genericTextInputField = GameObject.Find("GenericTextInputField").GetComponent<TMP_InputField>();

        if (genericTextInputField != null)
        {
            genericTextInputField.onDeselect.AddListener(arg0 =>
            {
                //Debug.Log("Dialog box input text box has been deselected.");
                textInputFieldIsActivated = false;
            });
        }

        genericTextInputPlaceholder = GameObject.Find("GenericTextInputPlaceholder").GetComponent<TextMeshProUGUI>();
        genericTextInputText = GameObject.Find("GenericTextInputText").GetComponent<TextMeshProUGUI>();
        genericTextInput.transform.SetParent(myDialogBoxComponent.transform);

        FontManager.LocalizeMe(genericTextInputPlaceholder, TDFonts.WHITE);
        FontManager.LocalizeMe(genericTextInputText, TDFonts.WHITE);

        HideTextInputField();

        shopScrollbar = GameObject.Find("ShopScrollbar");
        eqScrollbar = GameObject.Find("EQScrollbar");


        invScrollbar = GameObject.Find("InvScrollbar");
        skillActiveScrollbar = GameObject.Find("SkillActiveScrollbar");
        skillSupportScrollbar = GameObject.Find("SkillPassiveScrollbar");
        //skillReserveScrollbar = GameObject.Find("SkillReserveScrollbar");

        IntToKeyCode = new KeyCode[10];
        IntToKeyCode[0] = KeyCode.Alpha1;
        IntToKeyCode[1] = KeyCode.Alpha2;
        IntToKeyCode[2] = KeyCode.Alpha3;
        IntToKeyCode[3] = KeyCode.Alpha4;
        IntToKeyCode[4] = KeyCode.Alpha5;
        IntToKeyCode[5] = KeyCode.Alpha6;
        IntToKeyCode[6] = KeyCode.Alpha7;
        IntToKeyCode[7] = KeyCode.Alpha8;
        IntToKeyCode[8] = KeyCode.Alpha9;
        IntToKeyCode[9] = KeyCode.Alpha0;

        eqBlinkingCursor = GameObject.Find("EQBlinkingCursor");



        mainUINav = new UIObject();
        mainUINav.gameObj = GameObject.Find("MainUINav");

        uiNavHeaderText = GameObject.Find("UINavHeader").GetComponent<TextMeshProUGUI>();

        uiNavCharacter = new UIObject();
        uiNavCharacter.gameObj = GameObject.Find("UINavCharacter");
        uiNavCharacter.subObjectImage = GameObject.Find("UINavCharacter Sprite").GetComponent<Image>();
        uiNavCharacter.subObjectImage.color = transparentColor;

        if (uiNavSelected == null)
        {
            uiNavSelected = Resources.LoadAll<Sprite>("Art/UI/nav-tab-icons-selected");
        }
        if (uiNavDeselected == null)
        {
            uiNavDeselected = Resources.LoadAll<Sprite>("Art/UI/nav-tab-icons");
        }


        uiNavEquipment = new UIObject();
        uiNavEquipment.gameObj = GameObject.Find("UINavEquipment");
        uiNavEquipment.subObjectImage = GameObject.Find("UINavEquipment Sprite").GetComponent<Image>();
        uiNavEquipment.subObjectImage.color = transparentColor;

        uiNavInventory = new UIObject();
        uiNavInventory.gameObj = GameObject.Find("UINavInventory");
        uiNavInventory.subObjectImage = GameObject.Find("UINavInventory Sprite").GetComponent<Image>();
        uiNavInventory.subObjectImage.color = transparentColor;

        uiNavSkills = new UIObject();
        uiNavSkills.gameObj = GameObject.Find("UINavSkills");
        uiNavSkills.subObjectImage = GameObject.Find("UINavSkills Sprite").GetComponent<Image>();
        uiNavSkills.subObjectImage.color = transparentColor;

        uiNavRumors = new UIObject();
        uiNavRumors.gameObj = GameObject.Find("UINavRumors");
        uiNavRumors.subObjectImage = GameObject.Find("UINavRumors Sprite").GetComponent<Image>();
        uiNavRumors.subObjectImage.color = transparentColor;

        uiNavOptions = new UIObject();
        uiNavOptions.gameObj = GameObject.Find("UINavOptions");
        uiNavOptions.subObjectImage = GameObject.Find("UINavOptions Sprite").GetComponent<Image>();
        uiNavOptions.subObjectImage.color = transparentColor;

        uiNavButtons = new UIObject[6];
        uiNavButtons[0] = uiNavCharacter;
        uiNavButtons[1] = uiNavEquipment;
        uiNavButtons[2] = uiNavInventory;
        uiNavButtons[3] = uiNavSkills;
        uiNavButtons[4] = uiNavRumors;
        uiNavButtons[5] = uiNavOptions;

        mainUINav.gameObj.SetActive(false);

        invSubmenu = GameObject.Find("Inv Submenu");

        invSubmenuUse = new UIObject();
        invSubmenuUse.gameObj = GameObject.Find("InvSubmenuUse");
        invSubmenuUse.mySubmitFunction = TryUseEQItem;

        invSubmenuDrop = new UIObject();
        invSubmenuDrop.gameObj = GameObject.Find("InvSubmenuDrop");

        invSubmenuFavorite = new UIObject();
        invSubmenuFavorite.gameObj = GameObject.Find("InvSubmenuFavorite");

        invSubmenuUse.neighbors[(int)Directions.NORTH] = invSubmenuFavorite;
        invSubmenuUse.neighbors[(int)Directions.SOUTH] = invSubmenuDrop;

        invSubmenuDrop.neighbors[(int)Directions.NORTH] = invSubmenuUse;
        invSubmenuDrop.neighbors[(int)Directions.SOUTH] = invSubmenuFavorite;

        invSubmenuFavorite.neighbors[(int)Directions.NORTH] = invSubmenuDrop;
        invSubmenuFavorite.neighbors[(int)Directions.SOUTH] = invSubmenuUse;

        invSubmenuObjects = new UIObject[3];
        invSubmenuObjects[0] = invSubmenuUse;
        invSubmenuObjects[1] = invSubmenuDrop;
        invSubmenuObjects[2] = invSubmenuFavorite;

        invItemInfoHolder = GameObject.Find("Inv Item Info");
        invTooltipContainer = GameObject.Find("InvTooltipContainer");

        invItemInfoText = GameObject.Find("InvItemText").GetComponent<TextMeshProUGUI>();
        invItemInfoTextRect = invItemInfoHolder.GetComponent<RectTransform>();
        invItemInfoName = GameObject.Find("InvItemName").GetComponent<TextMeshProUGUI>();
        invItemInfoImage = GameObject.Find("InvItemImage").GetComponent<Image>();

        HideInvTooltipContainer();

        if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.01f);
        }

        slotsGame = GameObject.Find("SlotsGame");
        blackjackGame = GameObject.Find("BlackjackGame");
        slotsHeader = GameObject.Find("SlotsText").GetComponent<TextMeshProUGUI>();
        blackjackHeader = GameObject.Find("BlackjackText").GetComponent<TextMeshProUGUI>();

        blackjackPlayGame = new UIObject();
        blackjackPlayGame.gameObj = GameObject.Find("BlackjackPlayGame");
        blackjackPlayGame.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
        blackjackPlayGame.onSubmitValue = 0;
        blackjackPlayGame.onSelectValue = 0;
        blackjackPlayGame.button = new ButtonCombo();
        blackjackPlayGame.button.dbr = DialogButtonResponse.CASINOBLACKJACK;

        blackjackExit = new UIObject();
        blackjackExit.gameObj = GameObject.Find("BlackjackExit");
        blackjackExit.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
        blackjackExit.onSubmitValue = 1;
        blackjackExit.onSelectValue = 1;
        blackjackExit.button = new ButtonCombo();
        blackjackExit.button.dbr = DialogButtonResponse.EXIT;

        blackjackExit.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("misc_button_exit_normalcase").ToUpperInvariant();

        blackjackPlayGame.neighbors[(int)Directions.NORTH] = blackjackExit;
        blackjackPlayGame.neighbors[(int)Directions.SOUTH] = blackjackExit;

        blackjackExit.neighbors[(int)Directions.NORTH] = blackjackPlayGame;
        blackjackExit.neighbors[(int)Directions.SOUTH] = blackjackPlayGame;

        blackjackHit = new UIObject();
        blackjackHit.gameObj = GameObject.Find("BlackjackHit");
        blackjackHit.mySubmitFunction = GameMasterScript.gmsSingleton.TakeCasinoAction;
        blackjackHit.onSubmitValue = 0;
        blackjackHit.onSelectValue = 0;

        blackjackStay = new UIObject();
        blackjackStay.gameObj = GameObject.Find("BlackjackStay");
        blackjackStay.mySubmitFunction = GameMasterScript.gmsSingleton.TakeCasinoAction;
        blackjackStay.onSubmitValue = 1;
        blackjackStay.onSelectValue = 1;

        blackjackHit.neighbors[(int)Directions.NORTH] = blackjackStay;
        blackjackHit.neighbors[(int)Directions.SOUTH] = blackjackStay;

        blackjackStay.neighbors[(int)Directions.NORTH] = blackjackHit;
        blackjackStay.neighbors[(int)Directions.SOUTH] = blackjackHit;

        slotsPlayGame = new UIObject();
        slotsPlayGame.gameObj = GameObject.Find("SlotsPlayGame");
        slotsPlayGame.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
        slotsPlayGame.onSubmitValue = 0;
        slotsPlayGame.onSelectValue = 0;
        slotsPlayGame.button = new ButtonCombo();
        slotsPlayGame.button.dbr = DialogButtonResponse.CASINOSLOTS;

        slotsExit = new UIObject();
        slotsExit.gameObj = GameObject.Find("SlotsExit");
        slotsExit.mySubmitFunction = singletonUIMS.DialogCursorConfirm;
        slotsExit.onSubmitValue = 1;
        slotsExit.onSelectValue = 1;
        slotsExit.button = new ButtonCombo();
        slotsExit.button.dbr = DialogButtonResponse.EXIT;

        slotsExit.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("misc_button_exit_normalcase").ToUpperInvariant();

        slotsPlayGame.neighbors[(int)Directions.NORTH] = slotsExit;
        slotsPlayGame.neighbors[(int)Directions.SOUTH] = slotsExit;

        slotsExit.neighbors[(int)Directions.NORTH] = slotsPlayGame;
        slotsExit.neighbors[(int)Directions.SOUTH] = slotsPlayGame;


        slotsImage1 = GameObject.Find("SlotsImage1").GetComponent<Image>();
        slotsImage2 = GameObject.Find("SlotsImage2").GetComponent<Image>();
        slotsImage3 = GameObject.Find("SlotsImage3").GetComponent<Image>();
        slotsBet = GameObject.Find("SlotsBet").GetComponent<TextMeshProUGUI>();

        blackjackPlayerHand = GameObject.Find("BlackjackPlayerHand").GetComponent<TextMeshProUGUI>();
        blackjackDealerHand = GameObject.Find("BlackjackDealerHand").GetComponent<TextMeshProUGUI>();
        blackjackResults = GameObject.Find("BlackjackResults").GetComponent<TextMeshProUGUI>();

        CloseSlotsGame();
        CloseBlackjackGame();

        FontManager.LocalizeMe(blackjackPlayGame.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(blackjackHit.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(blackjackStay.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(slotsPlayGame.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(slotsExit.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);
        FontManager.LocalizeMe(blackjackExit.gameObj.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.BLACK);

        FontManager.LocalizeMe(blackjackPlayerHand, TDFonts.WHITE);
        FontManager.LocalizeMe(blackjackDealerHand, TDFonts.WHITE);
        FontManager.LocalizeMe(blackjackResults, TDFonts.WHITE);
        FontManager.LocalizeMe(slotsBet, TDFonts.WHITE);
        FontManager.LocalizeMe(slotsHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(blackjackHeader, TDFonts.WHITE);


        characterSheet = GameObject.Find("Character Sheet");
        menuScreenObjects[(int)UITabs.CHARACTER] = characterSheet;
        gameplayJobSelectionScreen = GameObject.Find("CharacterSelect");
        gameplayJobSelectionScreen.GetComponent<CharCreation>().Initialize();
        gameplayJobSelectionScreen.SetActive(false);

        csPortrait = GameObject.Find("CSPortrait").GetComponent<Image>();
        csName = GameObject.Find("CSName").GetComponent<TextMeshProUGUI>();
        csLevel = GameObject.Find("CSLevel").GetComponent<TextMeshProUGUI>();
        csExperience = GameObject.Find("CSExperience").GetComponent<TextMeshProUGUI>();
        csExplore = GameObject.Find("CSExplore").GetComponent<TextMeshProUGUI>();
        csMonster = GameObject.Find("CSMonster").GetComponent<TextMeshProUGUI>();
        csFavorites = GameObject.Find("CSFavorites").GetComponent<TextMeshProUGUI>();
        csResources = GameObject.Find("CSResources").GetComponent<TextMeshProUGUI>();
        csStrength = GameObject.Find("CSStrength").GetComponent<TextMeshProUGUI>();
        csSwiftness = GameObject.Find("CSSwiftness").GetComponent<TextMeshProUGUI>();
        csSpirit = GameObject.Find("CSSpirit").GetComponent<TextMeshProUGUI>();
        csDiscipline = GameObject.Find("CSDiscipline").GetComponent<TextMeshProUGUI>();
        csGuile = GameObject.Find("CSGuile").GetComponent<TextMeshProUGUI>();
        csWeaponPower = GameObject.Find("CSWeaponPower").GetComponent<TextMeshProUGUI>();
        csSpiritPower = GameObject.Find("CSSpiritPower").GetComponent<TextMeshProUGUI>();
        csCritChance = GameObject.Find("CSCritChance").GetComponent<TextMeshProUGUI>();
        csCritDamage = GameObject.Find("CSCritDamage").GetComponent<TextMeshProUGUI>();
        csChargeTime = GameObject.Find("CSChargeTime").GetComponent<TextMeshProUGUI>();
        csPhysicalResist = GameObject.Find("CSPhysicalResist").GetComponent<TextMeshProUGUI>();
        csFireResist = GameObject.Find("CSFireResist").GetComponent<TextMeshProUGUI>();
        csWaterResist = GameObject.Find("CSWaterResist").GetComponent<TextMeshProUGUI>();
        csPoisonResist = GameObject.Find("CSPoisonResist").GetComponent<TextMeshProUGUI>();
        csLightningResist = GameObject.Find("CSLightningResist").GetComponent<TextMeshProUGUI>();
        csShadowResist = GameObject.Find("CSShadowResist").GetComponent<TextMeshProUGUI>();

        csPhysicalDamage = GameObject.Find("CSPhysicalDamage").GetComponent<TextMeshProUGUI>();
        csFireDamage = GameObject.Find("CSFireDamage").GetComponent<TextMeshProUGUI>();
        csWaterDamage = GameObject.Find("CSWaterDamage").GetComponent<TextMeshProUGUI>();
        csPoisonDamage = GameObject.Find("CSPoisonDamage").GetComponent<TextMeshProUGUI>();
        csLightningDamage = GameObject.Find("CSLightningDamage").GetComponent<TextMeshProUGUI>();
        csShadowDamage = GameObject.Find("CSShadowDamage").GetComponent<TextMeshProUGUI>();

        csParryChance = GameObject.Find("CSParryChance").GetComponent<TextMeshProUGUI>();
        csBlockChance = GameObject.Find("CSBlockChance").GetComponent<TextMeshProUGUI>();
        csDodgeChance = GameObject.Find("CSDodgeChance").GetComponent<TextMeshProUGUI>();
        csPowerupDrop = GameObject.Find("CSPowerupDrop").GetComponent<TextMeshProUGUI>();
        csDamageMod = GameObject.Find("CSDamageMod").GetComponent<TextMeshProUGUI>();
        csDefenseMod = GameObject.Find("CSDefenseMod").GetComponent<TextMeshProUGUI>();
        csMoney = GameObject.Find("CSMoney").GetComponent<TextMeshProUGUI>();
        csMoneyLabel = GameObject.Find("CSMoneyLabel").GetComponent<TextMeshProUGUI>();
        //csStatusEffects = GameObject.Find("CSStatusEffects").GetComponent<TextMeshProUGUI>();
        csFeatsText = GameObject.Find("CSFeatsText").GetComponent<TextMeshProUGUI>();

        csStrengthLabel = GameObject.Find("CSStrengthLabel");
        csSwiftnessLabel = GameObject.Find("CSSwiftnessLabel");
        csSpiritLabel = GameObject.Find("CSSpiritLabel");
        csDisciplineLabel = GameObject.Find("CSDisciplineLabel");
        csGuileLabel = GameObject.Find("CSGuileLabel");

        csWeaponPowerLabel = GameObject.Find("CSWeaponPowerLabel");
        csSpiritPowerLabel = GameObject.Find("CSSpiritPowerLabel");
        csCritChanceLabel = GameObject.Find("CSCritChanceLabel");
        csChargeTimeLabel = GameObject.Find("CSChargeTimeLabel");
        csPowerupDropLabel = GameObject.Find("CSPowerupDropLabel");
        csDamageModlabel = GameObject.Find("CSDamageModLabel");
        csDefenseModlabel = GameObject.Find("CSDefenseModLabel");
        csParryChanceLabel = GameObject.Find("CSParryChanceLabel");

        if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.01f);
        }

        csStrengthUIObject = new UIObject();
        csStrengthUIObject.gameObj = csStrengthLabel.gameObject;
        csStrengthUIObject.myOnSelectAction = HoverStatInfo;
        csStrengthUIObject.onSelectValue = 0;
        csStrengthUIObject.myOnExitAction = HoverStatInfo;
        csStrengthUIObject.onExitValue = -1;

        csSwiftnessUIObject = new UIObject();
        csSwiftnessUIObject.gameObj = csSwiftnessLabel.gameObject;
        csSwiftnessUIObject.myOnSelectAction = HoverStatInfo;
        csSwiftnessUIObject.onSelectValue = 1;
        csSwiftnessUIObject.myOnExitAction = HoverStatInfo;
        csSwiftnessUIObject.onExitValue = -1;

        csSpiritUIObject = new UIObject();
        csSpiritUIObject.gameObj = csSpiritLabel.gameObject;
        csSpiritUIObject.myOnSelectAction = HoverStatInfo;
        csSpiritUIObject.onSelectValue = 2;
        csSpiritUIObject.myOnExitAction = HoverStatInfo;
        csSpiritUIObject.onExitValue = -1;

        csDisciplineUIObject = new UIObject();
        csDisciplineUIObject.gameObj = csDisciplineLabel.gameObject;
        csDisciplineUIObject.myOnSelectAction = HoverStatInfo;
        csDisciplineUIObject.onSelectValue = 3;
        csDisciplineUIObject.myOnExitAction = HoverStatInfo;
        csDisciplineUIObject.onExitValue = -1;

        csGuileUIObject = new UIObject();
        csGuileUIObject.gameObj = csGuileLabel.gameObject;
        csGuileUIObject.myOnSelectAction = HoverStatInfo;
        csGuileUIObject.onSelectValue = 4;
        csGuileUIObject.myOnExitAction = HoverStatInfo;
        csGuileUIObject.onExitValue = -1;

        csWeaponPowerUIObject = new UIObject();
        csWeaponPowerUIObject.gameObj = csWeaponPowerLabel.gameObject;
        csWeaponPowerUIObject.myOnSelectAction = HoverStatInfo;
        csWeaponPowerUIObject.onSelectValue = 5;
        csWeaponPowerUIObject.myOnExitAction = HoverStatInfo;
        csWeaponPowerUIObject.onExitValue = -1;

        csSpiritPowerUIObject = new UIObject();
        csSpiritPowerUIObject.gameObj = csSpiritPowerLabel.gameObject;
        csSpiritPowerUIObject.myOnSelectAction = HoverStatInfo;
        csSpiritPowerUIObject.onSelectValue = 6;
        csSpiritPowerUIObject.myOnExitAction = HoverStatInfo;
        csSpiritPowerUIObject.onExitValue = -1;

        csCritChanceUIObject = new UIObject();
        csCritChanceUIObject.gameObj = csCritChanceLabel.gameObject;
        csCritChanceUIObject.myOnSelectAction = HoverStatInfo;
        csCritChanceUIObject.onSelectValue = 7;
        csCritChanceUIObject.myOnExitAction = HoverStatInfo;
        csCritChanceUIObject.onExitValue = -1;

        csChargeTimeUIObject = new UIObject();
        csChargeTimeUIObject.gameObj = csChargeTimeLabel.gameObject;
        csChargeTimeUIObject.myOnSelectAction = HoverStatInfo;
        csChargeTimeUIObject.onSelectValue = 8;
        csChargeTimeUIObject.myOnExitAction = HoverStatInfo;
        csChargeTimeUIObject.onExitValue = -1;

        csPowerupDropUIObject = new UIObject();
        csPowerupDropUIObject.gameObj = csPowerupDropLabel.gameObject;
        csPowerupDropUIObject.myOnSelectAction = HoverStatInfo;
        csPowerupDropUIObject.onSelectValue = 9;
        csPowerupDropUIObject.myOnExitAction = HoverStatInfo;
        csPowerupDropUIObject.onExitValue = -1;

        csDamageModUIObject = new UIObject();
        csDamageModUIObject.gameObj = csDamageModlabel.gameObject;
        csDamageModUIObject.myOnSelectAction = HoverStatInfo;
        csDamageModUIObject.onSelectValue = 10;
        csDamageModUIObject.myOnExitAction = HoverStatInfo;
        csDamageModUIObject.onExitValue = -1;

        csDefenseModUIObject = new UIObject();
        csDefenseModUIObject.gameObj = csDefenseModlabel.gameObject;
        csDefenseModUIObject.myOnSelectAction = HoverStatInfo;
        csDefenseModUIObject.onSelectValue = 11;
        csDefenseModUIObject.myOnExitAction = HoverStatInfo;
        csDefenseModUIObject.onExitValue = -1;

        csParryChanceUIObject = new UIObject();
        csParryChanceUIObject.gameObj = csParryChanceLabel.gameObject;
        csParryChanceUIObject.myOnSelectAction = HoverStatInfo;
        csParryChanceUIObject.onSelectValue = 12;
        csParryChanceUIObject.myOnExitAction = HoverStatInfo;
        csParryChanceUIObject.onExitValue = -1;

        csObjectButtons = new UIObject[13]; // Number of objects on char sheet
        csObjectButtons[0] = csStrengthUIObject;
        csObjectButtons[1] = csSwiftnessUIObject;
        csObjectButtons[2] = csSpiritUIObject;
        csObjectButtons[3] = csDisciplineUIObject;
        csObjectButtons[4] = csGuileUIObject;
        csObjectButtons[5] = csWeaponPowerUIObject;
        csObjectButtons[6] = csSpiritPowerUIObject;
        csObjectButtons[7] = csCritChanceUIObject;
        csObjectButtons[8] = csChargeTimeUIObject;
        csObjectButtons[9] = csPowerupDropUIObject;
        csObjectButtons[10] = csDamageModUIObject;
        csObjectButtons[11] = csDefenseModUIObject;
        csObjectButtons[12] = csParryChanceUIObject;

        csStrengthUIObject.neighbors[(int)Directions.SOUTH] = csSwiftnessUIObject;
        csStrengthUIObject.neighbors[(int)Directions.NORTH] = csChargeTimeUIObject;
        csStrengthUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csStrengthUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csSwiftnessUIObject.neighbors[(int)Directions.SOUTH] = csSpiritUIObject;
        csSwiftnessUIObject.neighbors[(int)Directions.NORTH] = csStrengthUIObject;
        csSwiftnessUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csSwiftnessUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csSpiritUIObject.neighbors[(int)Directions.NORTH] = csSwiftnessUIObject;
        csSpiritUIObject.neighbors[(int)Directions.SOUTH] = csDisciplineUIObject;
        csSpiritUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csSpiritUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csDisciplineUIObject.neighbors[(int)Directions.NORTH] = csSpiritUIObject;
        csDisciplineUIObject.neighbors[(int)Directions.SOUTH] = csGuileUIObject;
        csDisciplineUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csDisciplineUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csGuileUIObject.neighbors[(int)Directions.NORTH] = csDisciplineUIObject;
        csGuileUIObject.neighbors[(int)Directions.SOUTH] = csWeaponPowerUIObject;
        csGuileUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csGuileUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csWeaponPowerUIObject.neighbors[(int)Directions.NORTH] = csGuileUIObject;
        csWeaponPowerUIObject.neighbors[(int)Directions.SOUTH] = csSpiritPowerUIObject;
        csWeaponPowerUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csWeaponPowerUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csSpiritPowerUIObject.neighbors[(int)Directions.NORTH] = csWeaponPowerUIObject;
        csSpiritPowerUIObject.neighbors[(int)Directions.SOUTH] = csCritChanceUIObject;
        csSpiritPowerUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csSpiritPowerUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csCritChanceUIObject.neighbors[(int)Directions.NORTH] = csSpiritPowerUIObject;
        csCritChanceUIObject.neighbors[(int)Directions.SOUTH] = csChargeTimeUIObject;
        csCritChanceUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csCritChanceUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csChargeTimeUIObject.neighbors[(int)Directions.NORTH] = csCritChanceUIObject;
        csChargeTimeUIObject.neighbors[(int)Directions.SOUTH] = csStrengthUIObject;
        csChargeTimeUIObject.neighbors[(int)Directions.EAST] = csParryChanceUIObject;
        csChargeTimeUIObject.neighbors[(int)Directions.WEST] = csParryChanceUIObject;

        csParryChanceUIObject.neighbors[(int)Directions.NORTH] = csPowerupDropUIObject;
        csParryChanceUIObject.neighbors[(int)Directions.SOUTH] = csDamageModUIObject;
        csParryChanceUIObject.neighbors[(int)Directions.EAST] = csStrengthUIObject;
        csParryChanceUIObject.neighbors[(int)Directions.WEST] = csStrengthUIObject;

        csPowerupDropUIObject.neighbors[(int)Directions.NORTH] = csDefenseModUIObject;
        csPowerupDropUIObject.neighbors[(int)Directions.SOUTH] = csParryChanceUIObject;
        csPowerupDropUIObject.neighbors[(int)Directions.EAST] = csStrengthUIObject;
        csPowerupDropUIObject.neighbors[(int)Directions.WEST] = csStrengthUIObject;

        csDamageModUIObject.neighbors[(int)Directions.NORTH] = csParryChanceUIObject;
        csDamageModUIObject.neighbors[(int)Directions.SOUTH] = csDefenseModUIObject;
        csDamageModUIObject.neighbors[(int)Directions.EAST] = csStrengthUIObject;
        csDamageModUIObject.neighbors[(int)Directions.WEST] = csStrengthUIObject;

        csDefenseModUIObject.neighbors[(int)Directions.NORTH] = csDamageModUIObject;
        csDefenseModUIObject.neighbors[(int)Directions.SOUTH] = csPowerupDropUIObject;
        csDefenseModUIObject.neighbors[(int)Directions.EAST] = csStrengthUIObject;
        csDefenseModUIObject.neighbors[(int)Directions.WEST] = csStrengthUIObject;

        charSheetStatInfo = GameObject.Find("CharSheetStatInfo").GetComponent<TextMeshProUGUI>();
        charSheetStatImage = GameObject.Find("CharSheetStatImage");

        //GetWindowState(UITabs.CHARACTER) = true;
        //CloseCharacterSheet();

        //disable old character sheet
        characterSheet.SetActive(false);

        questSheet = GameObject.Find("Quest Sheet");
        menuScreenObjects[(int)UITabs.RUMORS] = questSheet;
        StartCoroutine(gameObject.GetComponent<JournalScript>().Initialize());
        CloseQuestSheet();

        gamblerHand = GameObject.Find("GamblerHand");
        gamblerHand.SetActive(false);
        gamblerHandObjects = new List<GameObject>();
        cardsInUIArea = new List<PlayingCard>();

        immutableDialogObjects = new List<GameObject>();
        immutableDialogObjects.Add(slotsExit.gameObj);
        immutableDialogObjects.Add(slotsPlayGame.gameObj);
        immutableDialogObjects.Add(blackjackExit.gameObj);
        immutableDialogObjects.Add(blackjackHit.gameObj);
        immutableDialogObjects.Add(blackjackStay.gameObj);
        immutableDialogObjects.Add(blackjackPlayGame.gameObj);


        playerJobAbilities = new List<JobAbility>();
        playerActiveAbilities = new List<AbilityScript>();
        playerSupportAbilities = new List<AbilityScript>();

        uiInteractButtons = new List<GameObject>();

        // Cooking

        transparentSprite = Resources.Load<Sprite>("Art/transparentpixel");

        cookingIngredientItems = new Item[3];
        lastCookedItems = new Item[4]; // fourth slot is seasoning.

        cookingUI = GameObject.Find("CookingUI");
        menuScreenObjects[(int)UITabs.COOKING] = cookingUI;

        cookingReset = new UIObject();
        cookingReset.gameObj = GameObject.Find("CookReset");
        cookingReset.mySubmitFunction = singletonUIMS.ResetCookingFromBtn;

        cookingRepeat = new UIObject();
        cookingRepeat.gameObj = GameObject.Find("CookRepeatButton");
        cookingRepeat.mySubmitFunction = singletonUIMS.RepeatLastRecipeFromBtn;

        cookingExit = new UIObject();
        cookingExit.gameObj = GameObject.Find("Cooking Exit");
        cookingExit.button = new ButtonCombo();
        cookingExit.button.dbr = DialogButtonResponse.EXIT;

        cookButton = new UIObject();
        cookButton.gameObj = GameObject.Find("CookButton");
        cookButton.button = new ButtonCombo();
        cookButton.button.dbr = DialogButtonResponse.CONTINUE;
        cookButton.mySubmitFunction = singletonUIMS.CookItemsFromButton;

        ingredientsHolders = new GameObject[NUM_INGREDIENTS];
        ingredients = new UIObject[NUM_INGREDIENTS];
        seasoning = new UIObject[CookingScript.NUM_ALL_SEASONINGS];
        seasoningHolders = new GameObject[CookingScript.NUM_ALL_SEASONINGS];

        ingredientsQuantityText = new TextMeshProUGUI[NUM_INGREDIENTS];
        seasoningQuantityText = new TextMeshProUGUI[CookingScript.NUM_ALL_SEASONINGS];

        cookButton.neighbors[(int)Directions.WEST] = ingredients[11];
        cookButton.neighbors[(int)Directions.EAST] = ingredients[6];

        for (int a = 0; a < ingredients.Length; a++)
        {
            ingredients[a] = new UIObject();
            ingredients[a].myOnSelectAction = CookingIngredientHover;
            ingredients[a].onSelectValue = a;
            ingredients[a].mySubmitFunction = SelectCookingIngredient;
            ingredients[a].onSubmitValue = a;
            int disp = a + 1;
            string findStr = "Ingredient" + disp;
            ingredientsHolders[a] = GameObject.Find(findStr + "Holder");
            try { ingredients[a].gameObj = GameObject.Find(findStr); }
            catch (Exception e)
            {
                Debug.Log("Couldn't find " + findStr + " " + e);
            }

            ingredientsQuantityText[a] = ingredientsHolders[a].GetComponentInChildren<TextMeshProUGUI>();
            if (ingredientsQuantityText[a] == null)
            {
                Debug.Log("Could not find component child of " + ingredients[a].gameObj.name);
            }
        }
        for (int a = 0; a < seasoning.Length; a++)
        {
            seasoning[a] = new UIObject();
            seasoning[a].myOnSelectAction = CookingIngredientHover;
            seasoning[a].onSelectValue = a + 100;
            seasoning[a].mySubmitFunction = SelectCookingIngredient;
            seasoning[a].onSubmitValue = a + 100;
            int disp = a + 1;
            string findStr = "Seasoning" + disp;
            seasoningHolders[a] = GameObject.Find(findStr + "Holder");
            seasoning[a].gameObj = GameObject.Find(findStr);
            if (seasoning[a] == null)
            {
                Debug.Log("Failed to find " + findStr);
            }
            if (seasoningHolders[a] == null)
            {
                Debug.Log("Failed to find " + findStr + "Holder");
            }

            seasoningQuantityText[a] = seasoningHolders[a].GetComponentInChildren<TextMeshProUGUI>();
        }

        cookingReset.neighbors[(int)Directions.NORTH] = seasoning[2];
        cookingReset.neighbors[(int)Directions.SOUTH] = ingredients[2];
        cookingReset.neighbors[(int)Directions.WEST] = cookingRepeat;
        cookingReset.neighbors[(int)Directions.EAST] = cookingRepeat;

        cookingRepeat.neighbors[(int)Directions.NORTH] = seasoning[5];
        cookingRepeat.neighbors[(int)Directions.SOUTH] = ingredients[4];
        cookingRepeat.neighbors[(int)Directions.WEST] = cookingReset;
        cookingRepeat.neighbors[(int)Directions.EAST] = cookingReset;

        // FIRST ROW:

        ingredients[0].neighbors[(int)Directions.EAST] = ingredients[1];
        ingredients[0].neighbors[(int)Directions.WEST] = cookButton;
        ingredients[0].neighbors[(int)Directions.NORTH] = cookingReset;
        ingredients[0].neighbors[(int)Directions.SOUTH] = ingredients[6];

        ingredients[1].neighbors[(int)Directions.EAST] = ingredients[2];
        ingredients[1].neighbors[(int)Directions.WEST] = ingredients[0];
        ingredients[1].neighbors[(int)Directions.NORTH] = cookingReset; //ingredients[11];
        ingredients[1].neighbors[(int)Directions.SOUTH] = ingredients[7];

        ingredients[2].neighbors[(int)Directions.EAST] = ingredients[3];
        ingredients[2].neighbors[(int)Directions.WEST] = ingredients[1];
        ingredients[2].neighbors[(int)Directions.NORTH] = cookingReset; // ingredients[12];
        ingredients[2].neighbors[(int)Directions.SOUTH] = ingredients[8];

        ingredients[3].neighbors[(int)Directions.EAST] = ingredients[4];
        ingredients[3].neighbors[(int)Directions.WEST] = ingredients[2];
        ingredients[3].neighbors[(int)Directions.NORTH] = cookingRepeat; // ingredients[13];
        ingredients[3].neighbors[(int)Directions.SOUTH] = ingredients[9];

        ingredients[4].neighbors[(int)Directions.EAST] = ingredients[5];
        ingredients[4].neighbors[(int)Directions.WEST] = ingredients[3];
        ingredients[4].neighbors[(int)Directions.NORTH] = cookingRepeat; //ingredients[14];
        ingredients[4].neighbors[(int)Directions.SOUTH] = ingredients[10];

        ingredients[5].neighbors[(int)Directions.EAST] = cookButton;
        ingredients[5].neighbors[(int)Directions.WEST] = ingredients[4];
        ingredients[5].neighbors[(int)Directions.NORTH] = cookingRepeat;
        ingredients[5].neighbors[(int)Directions.SOUTH] = ingredients[11];

        // SECOND ROW:

        ingredients[6].neighbors[(int)Directions.EAST] = ingredients[7];
        ingredients[6].neighbors[(int)Directions.WEST] = cookButton;
        ingredients[6].neighbors[(int)Directions.NORTH] = ingredients[0];
        ingredients[6].neighbors[(int)Directions.SOUTH] = ingredients[12];

        ingredients[7].neighbors[(int)Directions.EAST] = ingredients[8];
        ingredients[7].neighbors[(int)Directions.WEST] = ingredients[6];
        ingredients[7].neighbors[(int)Directions.NORTH] = ingredients[1];
        ingredients[7].neighbors[(int)Directions.SOUTH] = ingredients[13];

        ingredients[8].neighbors[(int)Directions.EAST] = ingredients[9];
        ingredients[8].neighbors[(int)Directions.WEST] = ingredients[7];
        ingredients[8].neighbors[(int)Directions.NORTH] = ingredients[2];
        ingredients[8].neighbors[(int)Directions.SOUTH] = ingredients[14];

        ingredients[9].neighbors[(int)Directions.EAST] = ingredients[10];
        ingredients[9].neighbors[(int)Directions.WEST] = ingredients[8];
        ingredients[9].neighbors[(int)Directions.NORTH] = ingredients[3];
        ingredients[9].neighbors[(int)Directions.SOUTH] = ingredients[15];

        ingredients[10].neighbors[(int)Directions.EAST] = ingredients[11];
        ingredients[10].neighbors[(int)Directions.WEST] = ingredients[9];
        ingredients[10].neighbors[(int)Directions.NORTH] = ingredients[4];
        ingredients[10].neighbors[(int)Directions.SOUTH] = ingredients[16];

        ingredients[11].neighbors[(int)Directions.EAST] = cookButton;
        ingredients[11].neighbors[(int)Directions.WEST] = ingredients[10];
        ingredients[11].neighbors[(int)Directions.NORTH] = ingredients[5];
        ingredients[11].neighbors[(int)Directions.SOUTH] = ingredients[17];

        // THIRD ROW:

        ingredients[12].neighbors[(int)Directions.EAST] = ingredients[13];
        ingredients[12].neighbors[(int)Directions.WEST] = cookButton;
        ingredients[12].neighbors[(int)Directions.NORTH] = ingredients[6];
        ingredients[12].neighbors[(int)Directions.SOUTH] = seasoning[0];

        ingredients[13].neighbors[(int)Directions.EAST] = ingredients[14];
        ingredients[13].neighbors[(int)Directions.WEST] = ingredients[12];
        ingredients[13].neighbors[(int)Directions.NORTH] = ingredients[7];
        ingredients[13].neighbors[(int)Directions.SOUTH] = seasoning[1];

        ingredients[14].neighbors[(int)Directions.EAST] = ingredients[15];
        ingredients[14].neighbors[(int)Directions.WEST] = ingredients[13];
        ingredients[14].neighbors[(int)Directions.NORTH] = ingredients[8];
        ingredients[14].neighbors[(int)Directions.SOUTH] = seasoning[2];

        ingredients[15].neighbors[(int)Directions.EAST] = ingredients[16];
        ingredients[15].neighbors[(int)Directions.WEST] = ingredients[14];
        ingredients[15].neighbors[(int)Directions.NORTH] = ingredients[9];
        ingredients[15].neighbors[(int)Directions.SOUTH] = seasoning[3];

        ingredients[16].neighbors[(int)Directions.EAST] = ingredients[17];
        ingredients[16].neighbors[(int)Directions.WEST] = ingredients[15];
        ingredients[16].neighbors[(int)Directions.NORTH] = ingredients[10];
        ingredients[16].neighbors[(int)Directions.SOUTH] = seasoning[4];

        ingredients[17].neighbors[(int)Directions.EAST] = cookButton;
        ingredients[17].neighbors[(int)Directions.WEST] = ingredients[16];
        ingredients[17].neighbors[(int)Directions.NORTH] = ingredients[11];
        ingredients[17].neighbors[(int)Directions.SOUTH] = seasoning[5];


        seasoning[0].neighbors[(int)Directions.EAST] = seasoning[1];
        seasoning[0].neighbors[(int)Directions.WEST] = seasoning[5];
        seasoning[0].neighbors[(int)Directions.NORTH] = ingredients[12];
        seasoning[0].neighbors[(int)Directions.SOUTH] = cookingReset; //ingredients[0];

        seasoning[1].neighbors[(int)Directions.EAST] = seasoning[2];
        seasoning[1].neighbors[(int)Directions.WEST] = seasoning[0];
        seasoning[1].neighbors[(int)Directions.NORTH] = ingredients[13];
        seasoning[1].neighbors[(int)Directions.SOUTH] = cookingReset;

        seasoning[2].neighbors[(int)Directions.EAST] = seasoning[3];
        seasoning[2].neighbors[(int)Directions.WEST] = seasoning[1];
        seasoning[2].neighbors[(int)Directions.NORTH] = ingredients[14];
        seasoning[2].neighbors[(int)Directions.SOUTH] = cookingReset;

        seasoning[3].neighbors[(int)Directions.EAST] = seasoning[4];
        seasoning[3].neighbors[(int)Directions.WEST] = seasoning[2];
        seasoning[3].neighbors[(int)Directions.NORTH] = ingredients[15];
        seasoning[3].neighbors[(int)Directions.SOUTH] = cookingRepeat;

        seasoning[4].neighbors[(int)Directions.EAST] = seasoning[5];
        seasoning[4].neighbors[(int)Directions.WEST] = seasoning[3];
        seasoning[4].neighbors[(int)Directions.NORTH] = ingredients[16];
        seasoning[4].neighbors[(int)Directions.SOUTH] = cookingRepeat; //ingredients[4];

        seasoning[5].neighbors[(int)Directions.EAST] = seasoning[0];
        seasoning[5].neighbors[(int)Directions.WEST] = seasoning[4];
        seasoning[5].neighbors[(int)Directions.NORTH] = ingredients[17];
        seasoning[5].neighbors[(int)Directions.SOUTH] = cookingRepeat;

        cookButton.neighbors[(int)Directions.WEST] = ingredients[11];

        panIngredient1Holder = GameObject.Find("PanIngredient1Holder");
        panIngredient2Holder = GameObject.Find("PanIngredient2Holder");
        panIngredient3Holder = GameObject.Find("PanIngredient3Holder");
        panIngredient1 = GameObject.Find("PanIngredient1");
        panIngredient2 = GameObject.Find("PanIngredient2");
        panIngredient3 = GameObject.Find("PanIngredient3");

        panSeasoning = GameObject.Find("PanSeasoning");
        panSeasoningHolder = GameObject.Find("PanSeasoningHolder");
        cookingResultImage = GameObject.Find("CookingResultImage");
        cookingResultImageHolder = GameObject.Find("CookingResultImageHolder");
        cookingResultText = GameObject.Find("CookingResultText").GetComponent<TextMeshProUGUI>();

        // End cooking

        LocalizeCookingStrings();
        if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.01f);
        }

        eqEquippedWeapon = GameObject.Find("EQ Equipped Weapon");

        eqWeaponHighlight = GameObject.Find("EQ Weapon Highlight");

        equipmentSheet = GameObject.Find("EquipmentSheet");
        menuScreenObjects[(int)UITabs.EQUIPMENT] = equipmentSheet;
        cursorPosition = new int[2]; // x,y of cursor.
        inventorySheet = GameObject.Find("Player Inventory");
        menuScreenObjects[(int)UITabs.INVENTORY] = inventorySheet;

        cookingDragger = GameObject.Find("CookingDragger");
        cookingDragger.SetActive(false);

        playerItemList = new List<Item>();



        ShopUIScript.CloseShopInterface();

        // Loading groups were here.

        dialogObjects = new List<GameObject>();
        dialogUIObjects = new List<UIObject>();
        ClearAllDialogOptions();

        nonDialogHUDElements = new List<GameObject>();
        nonDialogHUDElements.Add(GameObject.Find("Player Stats"));

        GameObject log = GameObject.Find("Game Log");

        if (log == null)
        {
            log = GameLogScript.singleton.uiGameLog;
        }

        nonDialogHUDElements.Add(log);
        nonDialogHUDElements.Add(GameObject.Find("Dungeon"));
        nonDialogHUDElements.Add(GameObject.Find("Player Weapons"));
        nonDialogHUDElements.Add(GameObject.Find("Abilities"));

        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
        {
            GameObject hotbarBG = GameObject.Find("HotbarBG");
            if (hotbarBG != null) nonDialogHUDElements.Add(hotbarBG);
            nonDialogHUDElements.Add(uiBGImageBar);
        }
        else
        {
            nonDialogHUDElements.Add(Switch_BGImageBar.gameObject);
        }

        nonDialogHUDElements.Add(GameObject.Find("UIMouseBlocker"));        

        nonDialogHUDElements.Add(GameObject.Find("GenericInfoBar"));
        nonDialogHUDElements.Add(gamblerHand);

        genericInfoBar = GameObject.Find("GenericInfoBar");
        genericInfoBarText = genericInfoBar.GetComponentInChildren<TextMeshProUGUI>();
        FontManager.LocalizeMe(genericInfoBarText, TDFonts.WHITE);

        hudPlayerPortrait = GameObject.Find("PlayerPortrait").GetComponent<Image>();
        hudPortraitHolder = GameObject.Find("PortraitHolder");

        genericInfoBarTransform = genericInfoBar.GetComponent<RectTransform>();
        SetInfoText("");
        genericInfoBar.SetActive(false);

        /* petInfo = GameObject.Find("PetInfo");
        petInfo.SetActive(false); */

        playerHUD = GameObject.Find("PlayerHUD");
        sRenderColumns = renderColumns;
        sRenderRows = renderRows;
        mainCamera = GameObject.Find("Main Camera");
        GameObject go = GameObject.Find("Player Health");
        uiPlayerHealth = go.GetComponent<TextMeshProUGUI>();
        go = GameObject.Find("Player Stamina");
        uiPlayerStamina = go.GetComponent<TextMeshProUGUI>();
        go = GameObject.Find("Player Energy");
        uiPlayerEnergy = go.GetComponent<TextMeshProUGUI>();
        go = GameObject.Find("Player Energy Bar Fill");
        uiPlayerEnergyBarFill = go.GetComponent<Image>();
        uiPlayerHealthBar = GameObject.Find("Player Health Bar").GetComponent<Image>();
        go = GameObject.Find("Player Stamina Bar Fill");
        uiPlayerStaminaBarFill = go.GetComponent<Image>();
        go = GameObject.Find("Player Health Bar Fill");
        uiPlayerHealthBarFill = go.GetComponent<Image>();
        go = GameObject.Find("Player Name");
        uiPlayerName = go.GetComponent<TextMeshProUGUI>();
        go = GameObject.Find("Player XP");
        uiPlayerXP = go.GetComponent<TextMeshProUGUI>();


        //shield for shara
        go = GameObject.Find("Player Shield Bar Fill");
        uiPlayerShieldBarFill = go.GetComponent<Image>();
        uiPlayerShieldBarText = go.GetComponentInChildren<TextMeshProUGUI>();

        lowHealthDangerFlash = GameObject.Find("LowHealthWarning");
        lowHealthDangerFlash.SetActive(false);
        nonDialogHUDElements.Add(lowHealthDangerFlash);

        go = GameObject.Find("Player XP Level");
        uiPlayerLevel = go.GetComponent<TextMeshProUGUI>();
        //uiPlayerLevel.font = FontManager.GetFontAsset(TDFonts.BASIC);

        go = GameObject.Find("Player CT");
        uiPlayerCT = go.GetComponent<TextMeshProUGUI>();
        FontManager.LocalizeMe(uiPlayerCT, TDFonts.WHITE);

        uiPlayerXPBarFill = GameObject.Find("Player XP Bar Fill").GetComponent<Image>();
        uiPlayerCTBarFill = GameObject.Find("Player CT Bar Fill").GetComponent<Image>();

        //uiPlayerLearnSkill = GameObject.Find("Player Learn Skill");
        uiPortraitExclamation = GameObject.Find("PortraitExclamation");
        uiPortraitExclamation.SetActive(false);
        //uiPlayerLearnSkill.SetActive(false);
        go = GameObject.Find("Player Skill Info");
        uiPlayerSkillInfo = go.GetComponent<TextMeshProUGUI>();
        uiPlayerSkillInfoContainer = GameObject.Find("Player Skill Info Holder");
        uiPlayerSkillInfoContainer.gameObject.SetActive(false);
        go = GameObject.Find("Player Status Info");

        FontManager.LocalizeMe(uiPlayerSkillInfo, TDFonts.WHITE);

        go = GameObject.Find("Dungeon Name");
        uiDungeonName = go.GetComponent<TextMeshProUGUI>();
        FontManager.LocalizeMe(uiDungeonName, TDFonts.WHITE);
        uiDungeonIcon = GameObject.Find("Dungeon Icon").GetComponent<Image>();
        uiDungeonIcon.gameObject.SetActive(false);

        if (dungeonIconChestClosed == null)
        {
            dungeonIconChestClosed = Resources.Load<Sprite>("Art/UI/24-chest-closed");
        }
        if (dungeonIconChestOpen == null)
        {
            dungeonIconChestOpen = Resources.Load<Sprite>("Art/UI/24-chest-open");
        }


        uiHotbarCursor = GameObject.Find("Hotbar Cursor");

        TryLoadDialogCursor();

        go = GameObject.Find("GameMaster");
        gms = go.GetComponent<GameMasterScript>();
        staticGMSReference = gms;
        if (allUIGraphics == null)
        {
            allUIGraphics = Resources.LoadAll<Sprite>("Spritesheets/SkillIcons");
        }
        if (allItemGraphics == null)
        {
            allItemGraphics = Resources.LoadAll<Sprite>("assorteditems"); // Was uf_items
            dictItemGraphics = new Dictionary<string, Sprite>();
            for (int i = 0; i < allItemGraphics.Length; i++)
            {
                dictItemGraphics.Add(allItemGraphics[i].name, allItemGraphics[i]);
            }
        }
        if (eqFilterButtons == null)
        {
            eqFilterButtons = Resources.LoadAll<Sprite>("Art/UI/equipmentscreen/buttons-spritesheet");
        }
        if (invFilterButtons == null)
        {
            invFilterButtons = Resources.LoadAll<Sprite>("Art/UI/inventoryscreen/inv ui button sheet");
        }
        if (playingCardSprites == null)
        {
            playingCardSprites = Resources.LoadAll<Sprite>("Spritesheets/Cards");
        }

        TryLoadingAllPortraits();

        abilityIcons = new GameObject[16];
        abilityIcons[0] = GameObject.Find("Ability1");
        abilityIcons[1] = GameObject.Find("Ability2");
        abilityIcons[2] = GameObject.Find("Ability3");
        abilityIcons[3] = GameObject.Find("Ability4");
        abilityIcons[4] = GameObject.Find("Ability5");
        abilityIcons[5] = GameObject.Find("Ability6");
        abilityIcons[6] = GameObject.Find("Ability7");
        abilityIcons[7] = GameObject.Find("Ability8");
        abilityIcons[8] = GameObject.Find("Ability9");
        abilityIcons[9] = GameObject.Find("Ability10");
        abilityIcons[10] = GameObject.Find("Ability11");
        abilityIcons[11] = GameObject.Find("Ability12");
        abilityIcons[12] = GameObject.Find("Ability13");
        abilityIcons[13] = GameObject.Find("Ability14");
        abilityIcons[14] = GameObject.Find("Ability15");
        abilityIcons[15] = GameObject.Find("Ability16");

        for (int i = 8; i <= 15; i++)
        {
            abilityIcons[i].SetActive(false);
        }

        hudHotbarAbilities = new UIObject[19]; // 8 abils plus flask, portal, snackbag

        hudHotbarFlask = new UIObject();
        hudHotbarFlask.gameObj = GameObject.Find("FlaskSprite");
        hudHotbarFlask.mySubmitFunction = UseRegenFlask;
        hudHotbarFlask.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarFlask.onSelectValue = 100;

        hudHotbarPortal = new UIObject();
        hudHotbarPortal.gameObj = GameObject.Find("EscapeTorch");
        hudHotbarPortal.mySubmitFunction = UseTownPortal;
        hudHotbarPortal.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarPortal.onSelectValue = 101;

        hudHotbarSnackBag = new UIObject();
        hudHotbarSnackBag.gameObj = GameObject.Find("SnackBag");
        hudHotbarSnackBag.mySubmitFunction = SnackBagUIScript.singleton.OpenSnackBagUIFromButton;
        hudHotbarSnackBag.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSnackBag.onSelectValue = 108;

        hudHotbarSkill1 = new UIObject();
        hudHotbarSkill1.gameObj = abilityIcons[0];
        hudHotbarSkill1.mySubmitFunction = TryUseAbility;
        hudHotbarSkill1.onSubmitValue = 0;
        hudHotbarSkill1.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill1.onSelectValue = 0;

        hudHotbarSkill2 = new UIObject();
        hudHotbarSkill2.gameObj = abilityIcons[1];
        hudHotbarSkill2.mySubmitFunction = TryUseAbility;
        hudHotbarSkill2.onSubmitValue = 1;
        hudHotbarSkill2.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill2.onSelectValue = 1;

        hudHotbarSkill3 = new UIObject();
        hudHotbarSkill3.gameObj = abilityIcons[2];
        hudHotbarSkill3.mySubmitFunction = TryUseAbility;
        hudHotbarSkill3.onSubmitValue = 2;
        hudHotbarSkill3.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill3.onSelectValue = 2;

        hudHotbarSkill4 = new UIObject();
        hudHotbarSkill4.gameObj = abilityIcons[3];
        hudHotbarSkill4.mySubmitFunction = TryUseAbility;
        hudHotbarSkill4.onSubmitValue = 3;
        hudHotbarSkill4.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill4.onSelectValue = 3;

        hudHotbarSkill5 = new UIObject();
        hudHotbarSkill5.gameObj = abilityIcons[4];
        hudHotbarSkill5.mySubmitFunction = TryUseAbility;
        hudHotbarSkill5.onSubmitValue = 4;
        hudHotbarSkill5.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill5.onSelectValue = 4;

        hudHotbarSkill6 = new UIObject();
        hudHotbarSkill6.gameObj = abilityIcons[5];
        hudHotbarSkill6.mySubmitFunction = TryUseAbility;
        hudHotbarSkill6.onSubmitValue = 5;
        hudHotbarSkill6.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill6.onSelectValue = 5;

        hudHotbarSkill7 = new UIObject();
        hudHotbarSkill7.gameObj = abilityIcons[6];
        hudHotbarSkill7.mySubmitFunction = TryUseAbility;
        hudHotbarSkill7.onSubmitValue = 6;
        hudHotbarSkill7.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill7.onSelectValue = 6;

        hudHotbarSkill8 = new UIObject();
        hudHotbarSkill8.gameObj = abilityIcons[7];
        hudHotbarSkill8.mySubmitFunction = TryUseAbility;
        hudHotbarSkill8.onSubmitValue = 7;
        hudHotbarSkill8.myOnSelectAction = MouseoverFetchAbilityInfo;
        hudHotbarSkill8.onSelectValue = 7;

        hudHotbarAbilities[0] = hudHotbarFlask;
        hudHotbarAbilities[1] = hudHotbarPortal;
        hudHotbarAbilities[2] = hudHotbarSnackBag;
        hudHotbarAbilities[3] = hudHotbarSkill1;
        hudHotbarAbilities[4] = hudHotbarSkill2;
        hudHotbarAbilities[5] = hudHotbarSkill3;
        hudHotbarAbilities[6] = hudHotbarSkill4;
        hudHotbarAbilities[7] = hudHotbarSkill5;
        hudHotbarAbilities[8] = hudHotbarSkill6;
        hudHotbarAbilities[9] = hudHotbarSkill7;
        hudHotbarAbilities[10] = hudHotbarSkill8;

        // 9th skill, abilityIcons = 8, submit = 8, select = 8
        // i=10

        // Add 1 for snack bag...

        for (int i = 11; i <= 18; i++)
        {
            hudHotbarAbilities[i] = new UIObject();
            hudHotbarAbilities[i].gameObj = abilityIcons[i - 3];
            hudHotbarAbilities[i].mySubmitFunction = TryUseAbility;
            hudHotbarAbilities[i].onSubmitValue = i - 3;
            hudHotbarAbilities[i].myOnSelectAction = MouseoverFetchAbilityInfo;
            hudHotbarAbilities[i].onSelectValue = i - 3;
            if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
            //if (Switch_BGImageBar == null)
            {
                hudHotbarAbilities[i].directionalActions[(int)Directions.NORTH] = CycleHotbars;
                hudHotbarAbilities[i].directionalValues[(int)Directions.NORTH] = -1;
            }
        }

        if (!PlatformVariables.USE_GAMEPAD_ONLY_HOTBAR_STYLE)
        //if (Switch_BGImageBar == null)
        {
            for (int i = 3; i < 11; i++)
            {
                hudHotbarAbilities[i].directionalActions[(int)Directions.SOUTH] = CycleHotbars;
                hudHotbarAbilities[i].directionalValues[(int)Directions.SOUTH] = 1;
            }

            hudHotbarFlask.neighbors[(int)Directions.WEST] = hudHotbarSnackBag;
            hudHotbarFlask.neighbors[(int)Directions.EAST] = hudHotbarPortal;
            hudHotbarFlask.neighbors[(int)Directions.SOUTH] = hudHotbarSkill1;
            hudHotbarFlask.neighbors[(int)Directions.NORTH] = hudHotbarSkill1;

            hudHotbarPortal.neighbors[(int)Directions.WEST] = hudHotbarFlask;
            hudHotbarPortal.neighbors[(int)Directions.EAST] = hudHotbarSnackBag;
            hudHotbarPortal.neighbors[(int)Directions.SOUTH] = hudHotbarSkill2;
            hudHotbarPortal.neighbors[(int)Directions.NORTH] = hudHotbarSkill2;

            hudHotbarSnackBag.neighbors[(int)Directions.WEST] = hudHotbarPortal;
            hudHotbarSnackBag.neighbors[(int)Directions.EAST] = hudHotbarFlask;
            hudHotbarSnackBag.neighbors[(int)Directions.SOUTH] = hudHotbarSkill3;
            hudHotbarSnackBag.neighbors[(int)Directions.NORTH] = hudHotbarSkill3;

            hudHotbarSkill1.neighbors[(int)Directions.WEST] = hudHotbarSkill8;
            hudHotbarSkill1.neighbors[(int)Directions.EAST] = hudHotbarSkill2;
            hudHotbarSkill1.neighbors[(int)Directions.NORTH] = hudHotbarFlask;
            hudHotbarSkill1.neighbors[(int)Directions.SOUTH] = hudHotbarFlask;

            hudHotbarSkill2.neighbors[(int)Directions.WEST] = hudHotbarSkill1;
            hudHotbarSkill2.neighbors[(int)Directions.EAST] = hudHotbarSkill3;
            hudHotbarSkill2.neighbors[(int)Directions.NORTH] = hudHotbarPortal;
            hudHotbarSkill2.neighbors[(int)Directions.SOUTH] = hudHotbarPortal;

            hudHotbarSkill3.neighbors[(int)Directions.WEST] = hudHotbarSkill2;
            hudHotbarSkill3.neighbors[(int)Directions.EAST] = hudHotbarSkill4;
            hudHotbarSkill3.neighbors[(int)Directions.NORTH] = hudHotbarSnackBag;
            hudHotbarSkill3.neighbors[(int)Directions.SOUTH] = hudHotbarSnackBag;

            hudHotbarSkill4.neighbors[(int)Directions.WEST] = hudHotbarSkill3;
            hudHotbarSkill4.neighbors[(int)Directions.EAST] = hudHotbarSkill5;

            hudHotbarSkill5.neighbors[(int)Directions.WEST] = hudHotbarSkill4;
            hudHotbarSkill5.neighbors[(int)Directions.EAST] = hudHotbarSkill6;

            hudHotbarSkill6.neighbors[(int)Directions.WEST] = hudHotbarSkill5;
            hudHotbarSkill6.neighbors[(int)Directions.EAST] = hudHotbarSkill7;

            hudHotbarSkill7.neighbors[(int)Directions.WEST] = hudHotbarSkill6;
            hudHotbarSkill7.neighbors[(int)Directions.EAST] = hudHotbarSkill8;

            hudHotbarSkill8.neighbors[(int)Directions.WEST] = hudHotbarSkill7;
            hudHotbarSkill8.neighbors[(int)Directions.EAST] = hudHotbarSkill1;


            // 0 flask, 1 portal, 

            hudHotbarAbilities[11].neighbors[(int)Directions.WEST] = hudHotbarAbilities[18];
            hudHotbarAbilities[11].neighbors[(int)Directions.EAST] = hudHotbarAbilities[12];

            hudHotbarAbilities[12].neighbors[(int)Directions.WEST] = hudHotbarAbilities[11];
            hudHotbarAbilities[12].neighbors[(int)Directions.EAST] = hudHotbarAbilities[13];

            hudHotbarAbilities[13].neighbors[(int)Directions.WEST] = hudHotbarAbilities[12];
            hudHotbarAbilities[13].neighbors[(int)Directions.EAST] = hudHotbarAbilities[14];

            hudHotbarAbilities[14].neighbors[(int)Directions.WEST] = hudHotbarAbilities[13];
            hudHotbarAbilities[14].neighbors[(int)Directions.EAST] = hudHotbarAbilities[15];

            hudHotbarAbilities[15].neighbors[(int)Directions.WEST] = hudHotbarAbilities[14];
            hudHotbarAbilities[15].neighbors[(int)Directions.EAST] = hudHotbarAbilities[16];

            hudHotbarAbilities[16].neighbors[(int)Directions.WEST] = hudHotbarAbilities[15];
            hudHotbarAbilities[16].neighbors[(int)Directions.EAST] = hudHotbarAbilities[17];

            hudHotbarAbilities[17].neighbors[(int)Directions.WEST] = hudHotbarAbilities[16];
            hudHotbarAbilities[17].neighbors[(int)Directions.EAST] = hudHotbarAbilities[18];

            hudHotbarAbilities[18].neighbors[(int)Directions.WEST] = hudHotbarAbilities[17];
            hudHotbarAbilities[18].neighbors[(int)Directions.EAST] = hudHotbarAbilities[11];
        }
        else
        {
            Switch_PlayerBottomBar.SetupNavigation();
        }

        weaponHolders = new GameObject[4];
        weaponHolders[0] = GameObject.Find("Weapon1");
        weaponHolders[1] = GameObject.Find("Weapon2");
        weaponHolders[2] = GameObject.Find("Weapon3");
        weaponHolders[3] = GameObject.Find("Weapon4");

        weaponBoxes = new GameObject[4];
        weaponBoxes[0] = GameObject.Find("ItemBox1");
        weaponBoxes[1] = GameObject.Find("ItemBox2");
        weaponBoxes[2] = GameObject.Find("ItemBox3");
        weaponBoxes[3] = GameObject.Find("ItemBox4");

        weaponBoxPaths = new string[5];
        weaponBoxPaths[0] = "Art/UI/Items/weapon-1";
        weaponBoxPaths[1] = "Art/UI/Items/weapon-2";
        weaponBoxPaths[2] = "Art/UI/Items/weapon-3";
        weaponBoxPaths[3] = "Art/UI/Items/weapon-4";
        weaponBoxPaths[4] = "Art/UI/Items/selected-weapon";

        weaponItemIcons = new Image[4];
        weaponItemIcons[0] = GameObject.Find("ItemSprite1").GetComponent<Image>();
        weaponItemIcons[1] = GameObject.Find("ItemSprite2").GetComponent<Image>();
        weaponItemIcons[2] = GameObject.Find("ItemSprite3").GetComponent<Image>();
        weaponItemIcons[3] = GameObject.Find("ItemSprite4").GetComponent<Image>();

        buffIcons = new Image[12];
        for (int i = 0; i < buffIcons.Length; i++)
        {
            buffIcons[i] = GameObject.Find("Buff" + (i + 1)).GetComponent<Image>();
        }

        buffCountIcons = new Image[12];
        for (int i = 0; i < buffCountIcons.Length; i++)
        {
            buffCountIcons[i] = GameObject.Find("Buff" + (i + 1) + "Counter").GetComponent<Image>();
        }

        debuffIcons = new Image[12];
        for (int i = 0; i < debuffIcons.Length; i++)
        {
            debuffIcons[i] = GameObject.Find("Debuff" + (i + 1)).GetComponent<Image>();
        }

        debuffCountIcons = new Image[12];
        for (int i = 0; i < buffCountIcons.Length; i++)
        {
            debuffCountIcons[i] = GameObject.Find("Debuff" + (i + 1) + "Counter").GetComponent<Image>();
        }

        if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.01f);
        }

        for (int i = 0; i < buffIcons.Length; i++)
        {
            buffIcons[i].enabled = true;
            debuffIcons[i].enabled = true;
            buffIcons[i].sprite = transparentSprite;
            debuffIcons[i].sprite = transparentSprite;
            buffIcons[i].raycastTarget = false;
            debuffIcons[i].raycastTarget = false;
        }

        hotbarWeapons = new Weapon[4];
        hotbarAbilities = new HotbarBindable[16];
        for (int i = 0; i < hotbarAbilities.Length; i++)
        {
            hotbarAbilities[i] = new HotbarBindable();
        }
        playerBuffs = new StatusEffect[12];
        playerDebuffs = new StatusEffect[12];

        // *** OPTIONS MENU ***

        uiOptionsMenu = GameObject.Find("OptionsMenu");
        menuScreenObjects[(int)UITabs.OPTIONS] = uiOptionsMenu;
        uiOptionsObjects = new List<UIObject>();
        allOptionsSliders = new List<UIObject>();

        worldSeedText = GameObject.Find("WorldSeedText").GetComponent<TextMeshProUGUI>();
        FontManager.LocalizeMe(worldSeedText, TDFonts.WHITE);

        DeclareOptionsUIObjects();

        SetOptionsUIContainers();

        OptionsLocalizationHelper.SetupFonts();

        if (Time.realtimeSinceStartup - timeAtStart > GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            timeAtStart = Time.realtimeSinceStartup;
            GameMasterScript.IncrementLoadingBar(0.01f);
        }

        SetupOptionsUISliders();

        SetupOptionsUIObjectsList();

        SetupOptionsUINavigation();

        CloseOptionsMenu();
        //CloseJobSheet();
        ShopUIScript.CloseShopInterface();
        CloseCookingInterface();

        SetResolutionFromOptions();
    }

    public void TryLoadingAllPortraits()
    {
        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES) return;

        if (allPortraits == null)
        {
            allPortraits = Resources.LoadAll<Sprite>("Portraits");
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                allPortraits = allPortraits.Concat(Resources.LoadAll<Sprite>("DLCResources/DLC1/Portraits")).ToArray();
            }
            portraitNames = new string[allPortraits.Length];
            for (int i = 0; i < UIManagerScript.allPortraits.Length; i++)
            {
                portraitNames[i] = allPortraits[i].name;
            }
        }
    }

    void DeclareOptionsUIObjects()
    {
        optionsResolution = new UIObject();
        optionsResolution.gameObj = GameObject.Find("Resolution Slider");
        optionsResolution.mySubmitFunction = SelectOptionsSlider;
        optionsResolution.onSubmitValue = (int)OptionsSlider.RESOLUTION;

        optionsZoomScale = new UIObject();
        optionsZoomScale.gameObj = GameObject.Find("FOV Slider");
        optionsZoomScale.mySubmitFunction = SelectOptionsSlider;
        optionsZoomScale.onSubmitValue = (int)OptionsSlider.ZOOMSCALE;

        optionsMusicVolume = new UIObject();
        optionsMusicVolume.gameObj = GameObject.Find("Music Volume");
        optionsMusicVolume.mySubmitFunction = SelectOptionsSlider;
        optionsMusicVolume.onSubmitValue = (int)OptionsSlider.MUSICVOLUME;

        optionsSFXVolume = new UIObject();
        optionsSFXVolume.gameObj = GameObject.Find("SFX Volume");
        optionsSFXVolume.mySubmitFunction = SelectOptionsSlider;
        optionsSFXVolume.onSubmitValue = (int)OptionsSlider.SFXVOLUME;

        optionsFootstepsVolume = new UIObject();
        optionsFootstepsVolume.gameObj = GameObject.Find("Footsteps Volume");
        optionsFootstepsVolume.mySubmitFunction = SelectOptionsSlider;
        optionsFootstepsVolume.onSubmitValue = (int)OptionsSlider.FOOTSTEPSVOLUME;

        optionsAudioOffWhenMinimized = new UIObject();
        optionsAudioOffWhenMinimized.gameObj = GameObject.Find("OptionsAudioOffWhenMinimized");
        optionsAudioOffWhenMinimized.mySubmitFunction = optionsAudioOffWhenMinimized.ToggleAudioOffWhenMinimized;

        optionsFramecap = new UIObject();
        optionsFramecap.gameObj = GameObject.Find("Frame Cap");
        optionsFramecap.mySubmitFunction = SelectOptionsSlider;
        optionsFramecap.onSubmitValue = (int)OptionsSlider.FRAMECAP;

        optionsTextSpeed = new UIObject();
        optionsTextSpeed.gameObj = GameObject.Find("Text Speed");
        optionsTextSpeed.mySubmitFunction = SelectOptionsSlider;
        optionsTextSpeed.onSubmitValue = (int)OptionsSlider.TEXTSPEED;

        optionsBattleTextSpeed = new UIObject();
        optionsBattleTextSpeed.gameObj = GameObject.Find("Battle Text Speed");
        optionsBattleTextSpeed.mySubmitFunction = SelectOptionsSlider;
        optionsBattleTextSpeed.onSubmitValue = (int)OptionsSlider.BATTLETEXTSPEED;

        optionsBattleTextScale = new UIObject();
        optionsBattleTextScale.gameObj = GameObject.Find("Battle Text Scale");
        optionsBattleTextScale.mySubmitFunction = SelectOptionsSlider;
        optionsBattleTextScale.onSubmitValue = (int)OptionsSlider.BATTLETEXTSCALE;

        optionsCursorRepeatDelay = new UIObject();
        optionsCursorRepeatDelay.gameObj = GameObject.Find("Cursor Repeat Delay");
        optionsCursorRepeatDelay.mySubmitFunction = SelectOptionsSlider;
        optionsCursorRepeatDelay.onSubmitValue = (int)OptionsSlider.CURSORREPEATDEALY;

        optionsButtonDeadZone = new UIObject();
        optionsButtonDeadZone.gameObj = GameObject.Find("Button Dead Zone");
        optionsButtonDeadZone.mySubmitFunction = SelectOptionsSlider;
        optionsButtonDeadZone.onSubmitValue = (int)OptionsSlider.BUTTONDEADZONE;

        optionsFrameCapText = GameObject.Find("Frame Cap Text").GetComponent<TextMeshProUGUI>();
        optionsTextSpeedText = GameObject.Find("Text Speed Text").GetComponent<TextMeshProUGUI>();
        optionsBattleTextSpeedText = GameObject.Find("Battle Text Speed Text").GetComponent<TextMeshProUGUI>();
        optionsBattleTextScaleText = GameObject.Find("Battle Text Scale Text").GetComponent<TextMeshProUGUI>();
        optionsButtonDeadZoneText = GameObject.Find("Button Dead Zone Text").GetComponent<TextMeshProUGUI>();
        optionsCursorRepeatDelayText = GameObject.Find("Cursor Repeat Delay Text").GetComponent<TextMeshProUGUI>();
        optionsResolutionText = GameObject.Find("Resolution Text").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(optionsFrameCapText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsTextSpeedText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsBattleTextSpeedText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsBattleTextScaleText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsButtonDeadZoneText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsCursorRepeatDelayText, TDFonts.WHITE);
        FontManager.LocalizeMe(optionsResolutionText, TDFonts.WHITE);

        optionsFullscreen = new UIObject();
        optionsFullscreen.gameObj = GameObject.Find("OptionsFullscreen");
        optionsFullscreen.mySubmitFunction = optionsFullscreen.ToggleFullscreen;

        optionsCameraScanlines = new UIObject();
        optionsCameraScanlines.gameObj = GameObject.Find("Camera Scanlines");
        optionsCameraScanlines.mySubmitFunction = optionsCameraScanlines.ToggleScanlines;

        SetAvailableDisplayResolutions();

        if (LogoSceneScript.AllowResolutionSelection())
        {
            SetResolutionFromOptions(); // This is new, so that we change res on game scene start
            optionsResolution.gameObj.GetComponent<Slider>().maxValue = availableDisplayResolutions.Count - 1;
        }


        optionsGridOverlay = new UIObject();
        optionsGridOverlay.gameObj = GameObject.Find("Grid Overlay");
        optionsGridOverlay.mySubmitFunction = optionsGridOverlay.ToggleGridOverlay;

        optionsShowTutorialPopups = new UIObject();
        optionsShowTutorialPopups.gameObj = GameObject.Find("Tutorial Popups");
        optionsShowTutorialPopups.mySubmitFunction = optionsShowTutorialPopups.ToggleTutorialPopups;

        optionsShowJPXPGain = new UIObject();
        optionsShowJPXPGain.gameObj = GameObject.Find("Show JPXP Gain");
        optionsShowJPXPGain.mySubmitFunction = optionsShowJPXPGain.ToggleShowJPXPGain;

        optionsPlayerHealthBar = new UIObject();
        optionsPlayerHealthBar.gameObj = GameObject.Find("Options Player Health Bar");
        optionsPlayerHealthBar.mySubmitFunction = optionsPlayerHealthBar.TogglePlayerHealthBar;

        optionsMonsterHealthBars = new UIObject();
        optionsMonsterHealthBars.gameObj = GameObject.Find("Monster Health Bars");
        optionsMonsterHealthBars.mySubmitFunction = optionsMonsterHealthBars.ToggleMonsterHealthBars;

        optionsScreenFlashes = new UIObject();
        optionsScreenFlashes.gameObj = GameObject.Find("Screen Flashes");
        optionsScreenFlashes.mySubmitFunction = optionsScreenFlashes.ToggleScreenFlashes;

        optionsSmallCombatLogText = new UIObject();
        optionsSmallCombatLogText.gameObj = GameObject.Find("Smaller Log Text");
        optionsSmallCombatLogText.mySubmitFunction = optionsSmallCombatLogText.ToggleSmallCombatLogText;

        optionsUIPulses = new UIObject();
        optionsUIPulses.gameObj = GameObject.Find("UI Pulses");
        optionsUIPulses.mySubmitFunction = optionsUIPulses.ToggleUIPulses;

        optionsAutoEatFood = new UIObject();
        optionsAutoEatFood.gameObj = GameObject.Find("Auto Eat Food");
        optionsAutoEatFood.mySubmitFunction = optionsAutoEatFood.ToggleAutoEatFood;

        optionsAutoAbandonRumors = new UIObject();
        optionsAutoAbandonRumors.gameObj = GameObject.Find("Auto Abandon Rumors");
        optionsAutoAbandonRumors.mySubmitFunction = optionsAutoEatFood.ToggleAutoAbandonRumors;

        optionsShowRumorOverlay = new UIObject();
        optionsShowRumorOverlay.gameObj = GameObject.Find("Show Rumor Overlay");
        optionsShowRumorOverlay.mySubmitFunction = optionsShowRumorOverlay.ToggleShowRumorOverlay;

        optionsAutoUsePlanks = new UIObject();
        optionsAutoUsePlanks.gameObj = GameObject.Find("Auto Use Planks");
        optionsAutoUsePlanks.mySubmitFunction = optionsAutoUsePlanks.ToggleAutoPlanksInItemWorld;

        optionsAutoEquipBestOffhand = new UIObject();
        optionsAutoEquipBestOffhand.gameObj = GameObject.Find("Auto Equip Best Offhand");
        optionsAutoEquipBestOffhand.mySubmitFunction = optionsAutoEquipBestOffhand.ToggleAutoEquipBestOffhand;

        optionsAutoEquipWeapons = new UIObject();
        optionsAutoEquipWeapons.gameObj = GameObject.Find("Auto Equip Weapons");
        optionsAutoEquipWeapons.mySubmitFunction = optionsAutoEquipWeapons.ToggleAutoEquipWeapons;

        optionsVerboseCombatLog = new UIObject();
        optionsVerboseCombatLog.gameObj = GameObject.Find("Verbose Combat Log");
        optionsVerboseCombatLog.mySubmitFunction = optionsVerboseCombatLog.ToggleGameLogVerbosity;

        optionsPickupDisplay = new UIObject();
        optionsPickupDisplay.gameObj = GameObject.Find("Pickup Text");
        optionsPickupDisplay.mySubmitFunction = optionsPickupDisplay.TogglePickupDisplay;

        optionsExtraTurnDisplay = new UIObject();
        optionsExtraTurnDisplay.gameObj = GameObject.Find("Extra Turn Popup");
        optionsExtraTurnDisplay.mySubmitFunction = optionsExtraTurnDisplay.ToggleExtraTurnPopup;

        optionsInputManager = new UIObject();
        optionsInputManager.gameObj = GameObject.Find("Key Bindings");
        optionsInputManager.mySubmitFunction = GameMasterScript.gmsSingleton.OpenInputManager;

        optionsKeybindWASD = new UIObject();
        optionsKeybindWASD.gameObj = GameObject.Find("Reset Bindings WASD");
        optionsKeybindWASD.mySubmitFunction = GameMasterScript.gmsSingleton.SwitchControlModeByInt;
        optionsKeybindWASD.onSubmitValue = (int)KeyboardControlMaps.WASD;

        optionsKeybind2Hands = new UIObject();
        optionsKeybind2Hands.gameObj = GameObject.Find("Reset Bindings Full Keyboard");
        optionsKeybind2Hands.mySubmitFunction = GameMasterScript.gmsSingleton.SwitchControlModeByInt;
        optionsKeybind2Hands.onSubmitValue = (int)KeyboardControlMaps.DEFAULT;

        optionsShowControllerPrompts = new UIObject();
        optionsShowControllerPrompts.gameObj = GameObject.Find("Controller Prompts");
        optionsShowControllerPrompts.mySubmitFunction = optionsShowControllerPrompts.ToggleControllerPrompts;

        optionsUseStepMoveJoystickStyle = new UIObject();
        optionsUseStepMoveJoystickStyle.gameObj = GameObject.Find("Controller Style");
        optionsUseStepMoveJoystickStyle.mySubmitFunction = optionsUseStepMoveJoystickStyle.ToggleJoystickStyle;

        optionsDisableMouseOnKeyJoystick = new UIObject();
        optionsDisableMouseOnKeyJoystick.gameObj = GameObject.Find("Mouse Disable");
        optionsDisableMouseOnKeyJoystick.mySubmitFunction = optionsDisableMouseOnKeyJoystick.ToggleDisableMouse;

        optionsDisableMouseMovement = new UIObject();
        optionsDisableMouseMovement.gameObj = GameObject.Find("Disable Mouse Movement");
        optionsDisableMouseMovement.mySubmitFunction = optionsDisableMouseMovement.ToggleDisableMouseMovement;

        optionsSaveAndQuit = new UIObject();
        optionsSaveAndQuit.gameObj = GameObject.Find("Save and Quit");
        optionsSaveAndQuit.mySubmitFunction = optionsSaveAndQuit.SaveAndQuit;

        optionsViewHelp = new UIObject();
        optionsViewHelp.gameObj = GameObject.Find("View Help");
        optionsViewHelp.mySubmitFunction = ViewHelp;

        optionsSaveAndBackToTitle = new UIObject();
        optionsSaveAndBackToTitle.gameObj = GameObject.Find("Save and Title");
        optionsSaveAndBackToTitle.mySubmitFunction = optionsSaveAndBackToTitle.SaveAndBackToTitle;
    }

    void SetOptionsUIContainers()
    {
        optionsMusicVolumeContainer = GameObject.Find("OptionsMusicVolumeContainer");
        optionsSFXVolumeContainer = GameObject.Find("OptionsSFXVolumeContainer");
        optionsFootstepsVolumeContainer = GameObject.Find("OptionsFootstepsVolumeContainer");
        optionsCursorRepeatDelayContainer = GameObject.Find("OptionsCursorRepeatDelayContainer");
        optionsButtonDeadZoneContainer = GameObject.Find("OptionsButtonDeadZoneContainer");
        optionsZoomScaleContainer = GameObject.Find("OptionsZoomScaleContainer");
        optionsFrameCapContainer = GameObject.Find("OptionsFrameCapContainer");
        optionsTextSpeedContainer = GameObject.Find("OptionsTextSpeedContainer");
        optionsBattleTextSpeedContainer = GameObject.Find("OptionsBattleTextSpeedContainer");
        optionsBattleTextScaleContainer = GameObject.Find("OptionsBattleTextScaleContainer");
        optionsResolutionContainer = GameObject.Find("OptionsResolutionContainer");

        optionsMusicVolumeContainer.GetComponent<Image>().color = transparentColor;
        optionsSFXVolumeContainer.GetComponent<Image>().color = transparentColor;
        optionsFootstepsVolumeContainer.GetComponent<Image>().color = transparentColor;
        optionsCursorRepeatDelayContainer.GetComponent<Image>().color = transparentColor;
        optionsButtonDeadZoneContainer.GetComponent<Image>().color = transparentColor;
        optionsZoomScaleContainer.GetComponent<Image>().color = transparentColor;
        optionsFrameCapContainer.GetComponent<Image>().color = transparentColor;
        optionsTextSpeedContainer.GetComponent<Image>().color = transparentColor;
        optionsBattleTextSpeedContainer.GetComponent<Image>().color = transparentColor;
        optionsBattleTextScaleContainer.GetComponent<Image>().color = transparentColor;
        optionsResolutionContainer.GetComponent<Image>().color = transparentColor;

        uiObjectsWithHighlights = new List<GameObject>();
        uiObjectsWithHighlights.Add(optionsMusicVolumeContainer);
        uiObjectsWithHighlights.Add(optionsSFXVolumeContainer);
        uiObjectsWithHighlights.Add(optionsFootstepsVolumeContainer);
        uiObjectsWithHighlights.Add(optionsCursorRepeatDelayContainer);
        uiObjectsWithHighlights.Add(optionsButtonDeadZoneContainer);
        uiObjectsWithHighlights.Add(optionsZoomScaleContainer);
        uiObjectsWithHighlights.Add(optionsTextSpeedContainer);
        uiObjectsWithHighlights.Add(optionsBattleTextSpeedContainer);
        uiObjectsWithHighlights.Add(optionsBattleTextScaleContainer);

        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            uiObjectsWithHighlights.Add(optionsFrameCapContainer);
        }


        if (LogoSceneScript.AllowResolutionSelection())
        {
            uiObjectsWithHighlights.Add(optionsResolutionContainer);
        }

    }

    void SetupOptionsUISliders()
    {
        allOptionsSliders.Add(optionsMusicVolume);
        allOptionsSliders.Add(optionsSFXVolume);
        allOptionsSliders.Add(optionsFootstepsVolume);


        if (!LogoSceneScript.globalIsSolsticeBuild && !LogoSceneScript.globalSolsticeDebug)
        {
            allOptionsSliders.Add(optionsFramecap);
        }

        allOptionsSliders.Add(optionsTextSpeed);
        allOptionsSliders.Add(optionsBattleTextSpeed);
        allOptionsSliders.Add(optionsBattleTextScale);
        allOptionsSliders.Add(optionsCursorRepeatDelay);
        allOptionsSliders.Add(optionsButtonDeadZone);
        allOptionsSliders.Add(optionsZoomScale);

        if (LogoSceneScript.AllowResolutionSelection())
        {
            allOptionsSliders.Add(optionsResolution);
        }
    }

    void SetupOptionsUIObjectsList()
    {
        uiOptionsObjects.Add(optionsZoomScale);
        uiOptionsObjects.Add(optionsMusicVolume);
        uiOptionsObjects.Add(optionsSFXVolume);
        uiOptionsObjects.Add(optionsFootstepsVolume);
        uiOptionsObjects.Add(optionsTextSpeed);
        uiOptionsObjects.Add(optionsBattleTextSpeed);
        uiOptionsObjects.Add(optionsBattleTextScale);
        uiOptionsObjects.Add(optionsAudioOffWhenMinimized);
        uiOptionsObjects.Add(optionsFramecap);
        uiOptionsObjects.Add(optionsCursorRepeatDelay);

        if (!LogoSceneScript.globalIsSolsticeBuild)
        {
            uiOptionsObjects.Add(optionsFullscreen);
        }

        uiOptionsObjects.Add(optionsCameraScanlines);
        uiOptionsObjects.Add(optionsShowTutorialPopups);
        uiOptionsObjects.Add(optionsShowJPXPGain);
        uiOptionsObjects.Add(optionsPlayerHealthBar);
        uiOptionsObjects.Add(optionsMonsterHealthBars);
        uiOptionsObjects.Add(optionsScreenFlashes);
        uiOptionsObjects.Add(optionsSmallCombatLogText);
        uiOptionsObjects.Add(optionsUIPulses);
        uiOptionsObjects.Add(optionsShowRumorOverlay);
        uiOptionsObjects.Add(optionsAutoEatFood);
        uiOptionsObjects.Add(optionsDisableMouseMovement);
        uiOptionsObjects.Add(optionsAutoAbandonRumors);
        uiOptionsObjects.Add(optionsAutoUsePlanks);
        uiOptionsObjects.Add(optionsAutoEquipBestOffhand);
        uiOptionsObjects.Add(optionsAutoEquipWeapons);
        uiOptionsObjects.Add(optionsVerboseCombatLog);
        uiOptionsObjects.Add(optionsPickupDisplay);
        uiOptionsObjects.Add(optionsExtraTurnDisplay);
        uiOptionsObjects.Add(optionsGridOverlay);
        uiOptionsObjects.Add(optionsInputManager);
        uiOptionsObjects.Add(optionsKeybind2Hands);
        uiOptionsObjects.Add(optionsKeybindWASD);
        uiOptionsObjects.Add(optionsShowControllerPrompts);
        uiOptionsObjects.Add(optionsViewHelp);
        uiOptionsObjects.Add(optionsSaveAndQuit);
        uiOptionsObjects.Add(optionsSaveAndBackToTitle);
        //uiOptionsObjects.Add(optionsBackToGame);
    }

    void SetupOptionsUINavigation()
    {
        // Audio block
        optionsMusicVolume.neighbors[(int)Directions.NORTH] = optionsAudioOffWhenMinimized;
        optionsMusicVolume.neighbors[(int)Directions.SOUTH] = optionsSFXVolume;
        optionsMusicVolume.neighbors[(int)Directions.EAST] = optionsCursorRepeatDelay;
        optionsMusicVolume.neighbors[(int)Directions.WEST] = optionsShowTutorialPopups;

        optionsSFXVolume.neighbors[(int)Directions.NORTH] = optionsMusicVolume;
        optionsSFXVolume.neighbors[(int)Directions.SOUTH] = optionsFootstepsVolume;
        optionsSFXVolume.neighbors[(int)Directions.EAST] = optionsCursorRepeatDelay;
        optionsSFXVolume.neighbors[(int)Directions.WEST] = optionsShowTutorialPopups;

        optionsFootstepsVolume.neighbors[(int)Directions.NORTH] = optionsSFXVolume;
        optionsFootstepsVolume.neighbors[(int)Directions.SOUTH] = optionsAudioOffWhenMinimized;
        optionsFootstepsVolume.neighbors[(int)Directions.EAST] = optionsCursorRepeatDelay;
        optionsFootstepsVolume.neighbors[(int)Directions.WEST] = optionsShowTutorialPopups;

        optionsAudioOffWhenMinimized.neighbors[(int)Directions.NORTH] = optionsFootstepsVolume;
        optionsAudioOffWhenMinimized.neighbors[(int)Directions.SOUTH] = optionsMusicVolume;
        optionsAudioOffWhenMinimized.neighbors[(int)Directions.EAST] = optionsCursorRepeatDelay;
        optionsAudioOffWhenMinimized.neighbors[(int)Directions.WEST] = optionsShowTutorialPopups;

        // Visual block

        optionsResolution.neighbors[(int)Directions.NORTH] = optionsShowRumorOverlay;
        optionsResolution.neighbors[(int)Directions.SOUTH] = !LogoSceneScript.AllowFullScreenSelection() ? optionsZoomScale : optionsFullscreen;
        optionsResolution.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsResolution.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsFullscreen.neighbors[(int)Directions.NORTH] = optionsResolution;
        optionsFullscreen.neighbors[(int)Directions.SOUTH] = optionsZoomScale;
        optionsFullscreen.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsFullscreen.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsZoomScale.neighbors[(int)Directions.NORTH] = !LogoSceneScript.AllowFullScreenSelection() ? optionsUIPulses : optionsFullscreen;
        optionsZoomScale.neighbors[(int)Directions.SOUTH] = (LogoSceneScript.globalIsSolsticeBuild || LogoSceneScript.globalSolsticeDebug) ? optionsTextSpeed : optionsFramecap;
        optionsZoomScale.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsZoomScale.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsFramecap.neighbors[(int)Directions.NORTH] = optionsZoomScale;
        optionsFramecap.neighbors[(int)Directions.SOUTH] = optionsTextSpeed;
        optionsFramecap.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsFramecap.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsTextSpeed.neighbors[(int)Directions.NORTH] = (LogoSceneScript.globalIsSolsticeBuild || LogoSceneScript.globalSolsticeDebug) ? optionsZoomScale : optionsFramecap;
        optionsTextSpeed.neighbors[(int)Directions.SOUTH] = optionsBattleTextSpeed;
        optionsTextSpeed.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsTextSpeed.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsBattleTextSpeed.neighbors[(int)Directions.NORTH] = optionsTextSpeed;
        optionsBattleTextSpeed.neighbors[(int)Directions.SOUTH] = optionsBattleTextScale;
        optionsBattleTextSpeed.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsBattleTextSpeed.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsBattleTextScale.neighbors[(int)Directions.NORTH] = optionsBattleTextSpeed;
        optionsBattleTextScale.neighbors[(int)Directions.SOUTH] = optionsCameraScanlines;
        optionsBattleTextScale.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsBattleTextScale.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsCameraScanlines.neighbors[(int)Directions.NORTH] = optionsBattleTextScale;
        optionsCameraScanlines.neighbors[(int)Directions.SOUTH] = optionsGridOverlay;
        optionsCameraScanlines.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsCameraScanlines.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsGridOverlay.neighbors[(int)Directions.NORTH] = optionsCameraScanlines;
        optionsGridOverlay.neighbors[(int)Directions.SOUTH] = optionsPlayerHealthBar;
        optionsGridOverlay.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsGridOverlay.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsPlayerHealthBar.neighbors[(int)Directions.NORTH] = optionsGridOverlay;
        optionsPlayerHealthBar.neighbors[(int)Directions.SOUTH] = optionsMonsterHealthBars;
        optionsPlayerHealthBar.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsPlayerHealthBar.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsMonsterHealthBars.neighbors[(int)Directions.NORTH] = optionsPlayerHealthBar;
        optionsMonsterHealthBars.neighbors[(int)Directions.SOUTH] = optionsScreenFlashes;
        optionsMonsterHealthBars.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsMonsterHealthBars.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsScreenFlashes.neighbors[(int)Directions.NORTH] = optionsMonsterHealthBars;
        optionsScreenFlashes.neighbors[(int)Directions.SOUTH] = optionsSmallCombatLogText;
        optionsScreenFlashes.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsScreenFlashes.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsSmallCombatLogText.neighbors[(int)Directions.NORTH] = optionsScreenFlashes;
        optionsSmallCombatLogText.neighbors[(int)Directions.SOUTH] = optionsUIPulses; 
        optionsSmallCombatLogText.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsSmallCombatLogText.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsUIPulses.neighbors[(int)Directions.NORTH] = optionsSmallCombatLogText;
        optionsUIPulses.neighbors[(int)Directions.SOUTH] = optionsShowRumorOverlay;
        optionsUIPulses.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsUIPulses.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        optionsShowRumorOverlay.neighbors[(int)Directions.NORTH] = optionsUIPulses;
        optionsShowRumorOverlay.neighbors[(int)Directions.SOUTH] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsShowRumorOverlay.neighbors[(int)Directions.WEST] = optionsCursorRepeatDelay;
        optionsShowRumorOverlay.neighbors[(int)Directions.EAST] = optionsShowTutorialPopups;

        // Control block

        optionsCursorRepeatDelay.neighbors[(int)Directions.NORTH] = optionsDisableMouseOnKeyJoystick;
        optionsCursorRepeatDelay.neighbors[(int)Directions.SOUTH] = optionsButtonDeadZone;
        optionsCursorRepeatDelay.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsCursorRepeatDelay.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsButtonDeadZone.neighbors[(int)Directions.NORTH] = optionsCursorRepeatDelay;
        optionsButtonDeadZone.neighbors[(int)Directions.SOUTH] = optionsInputManager;
        optionsButtonDeadZone.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsButtonDeadZone.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsInputManager.neighbors[(int)Directions.NORTH] = optionsButtonDeadZone;
        optionsInputManager.neighbors[(int)Directions.SOUTH] = optionsKeybindWASD;
        optionsInputManager.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsInputManager.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsKeybindWASD.neighbors[(int)Directions.NORTH] = optionsInputManager;
        optionsKeybindWASD.neighbors[(int)Directions.SOUTH] = optionsKeybind2Hands;
        optionsKeybindWASD.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsKeybindWASD.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsKeybind2Hands.neighbors[(int)Directions.NORTH] = optionsKeybindWASD;
        optionsKeybind2Hands.neighbors[(int)Directions.SOUTH] = optionsShowControllerPrompts;
        optionsKeybind2Hands.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsKeybind2Hands.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsShowControllerPrompts.neighbors[(int)Directions.NORTH] = optionsKeybind2Hands;
        optionsShowControllerPrompts.neighbors[(int)Directions.SOUTH] = optionsUseStepMoveJoystickStyle;
        optionsShowControllerPrompts.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsShowControllerPrompts.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsUseStepMoveJoystickStyle.neighbors[(int)Directions.NORTH] = optionsShowControllerPrompts;
        optionsUseStepMoveJoystickStyle.neighbors[(int)Directions.SOUTH] = optionsDisableMouseOnKeyJoystick;
        optionsUseStepMoveJoystickStyle.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsUseStepMoveJoystickStyle.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsDisableMouseOnKeyJoystick.neighbors[(int)Directions.NORTH] = optionsUseStepMoveJoystickStyle;
        optionsDisableMouseOnKeyJoystick.neighbors[(int)Directions.SOUTH] = optionsDisableMouseMovement;
        optionsDisableMouseOnKeyJoystick.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsDisableMouseOnKeyJoystick.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        optionsDisableMouseMovement.neighbors[(int)Directions.NORTH] = optionsDisableMouseOnKeyJoystick;
        optionsDisableMouseMovement.neighbors[(int)Directions.SOUTH] = optionsCursorRepeatDelay;
        optionsDisableMouseMovement.neighbors[(int)Directions.EAST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsDisableMouseMovement.neighbors[(int)Directions.WEST] = optionsMusicVolume;

        // Gameplay block

        optionsShowTutorialPopups.neighbors[(int)Directions.NORTH] = optionsSaveAndBackToTitle;
        optionsShowTutorialPopups.neighbors[(int)Directions.SOUTH] = optionsShowJPXPGain;
        optionsShowTutorialPopups.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsShowTutorialPopups.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsShowJPXPGain.neighbors[(int)Directions.NORTH] = optionsShowTutorialPopups;
        optionsShowJPXPGain.neighbors[(int)Directions.SOUTH] = optionsPickupDisplay;
        optionsShowJPXPGain.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsShowJPXPGain.neighbors[(int)Directions.EAST] = optionsMusicVolume;


        optionsPickupDisplay.neighbors[(int)Directions.NORTH] = optionsShowJPXPGain;
        optionsPickupDisplay.neighbors[(int)Directions.SOUTH] = optionsExtraTurnDisplay;
        optionsPickupDisplay.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsPickupDisplay.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsExtraTurnDisplay.neighbors[(int)Directions.NORTH] = optionsPickupDisplay;
        optionsExtraTurnDisplay.neighbors[(int)Directions.SOUTH] = optionsAutoUsePlanks;
        optionsExtraTurnDisplay.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsExtraTurnDisplay.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsAutoUsePlanks.neighbors[(int)Directions.NORTH] = optionsExtraTurnDisplay;
        optionsAutoUsePlanks.neighbors[(int)Directions.SOUTH] = optionsAutoEquipBestOffhand;
        optionsAutoUsePlanks.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsAutoUsePlanks.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsAutoEquipBestOffhand.neighbors[(int)Directions.NORTH] = optionsAutoUsePlanks;
        optionsAutoEquipBestOffhand.neighbors[(int)Directions.SOUTH] = optionsAutoEquipWeapons;
        optionsAutoEquipBestOffhand.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsAutoEquipBestOffhand.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsAutoEquipWeapons.neighbors[(int)Directions.NORTH] = optionsAutoEquipBestOffhand;
        optionsAutoEquipWeapons.neighbors[(int)Directions.SOUTH] = optionsVerboseCombatLog;
        optionsAutoEquipWeapons.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsAutoEquipWeapons.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsVerboseCombatLog.neighbors[(int)Directions.NORTH] = optionsAutoEquipWeapons;
        optionsVerboseCombatLog.neighbors[(int)Directions.SOUTH] = optionsAutoEatFood;
        optionsVerboseCombatLog.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsVerboseCombatLog.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsAutoEatFood.neighbors[(int)Directions.NORTH] = optionsVerboseCombatLog;
        optionsAutoEatFood.neighbors[(int)Directions.SOUTH] = optionsAutoAbandonRumors;
        optionsAutoEatFood.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsAutoEatFood.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsAutoAbandonRumors.neighbors[(int)Directions.NORTH] = optionsAutoEatFood;
        optionsAutoAbandonRumors.neighbors[(int)Directions.SOUTH] = optionsViewHelp;
        optionsAutoAbandonRumors.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsAutoAbandonRumors.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsViewHelp.neighbors[(int)Directions.NORTH] = optionsAutoAbandonRumors;
        optionsViewHelp.neighbors[(int)Directions.SOUTH] = optionsSaveAndQuit;
        optionsViewHelp.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsViewHelp.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsSaveAndQuit.neighbors[(int)Directions.NORTH] = optionsViewHelp;
        optionsSaveAndQuit.neighbors[(int)Directions.SOUTH] = optionsSaveAndBackToTitle;
        optionsSaveAndQuit.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsSaveAndQuit.neighbors[(int)Directions.EAST] = optionsMusicVolume;

        optionsSaveAndBackToTitle.neighbors[(int)Directions.NORTH] = optionsSaveAndQuit;
        optionsSaveAndBackToTitle.neighbors[(int)Directions.SOUTH] = optionsShowTutorialPopups;
        optionsSaveAndBackToTitle.neighbors[(int)Directions.WEST] = !LogoSceneScript.AllowResolutionSelection() ? optionsZoomScale : optionsResolution;
        optionsSaveAndBackToTitle.neighbors[(int)Directions.EAST] = optionsMusicVolume;
    }
}
