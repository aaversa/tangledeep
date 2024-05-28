using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public enum ItemWorldMenuState { SELECTITEM, SELECTORB, COUNT }
public enum MagicModCompatibility { POSSIBLE, WRONG_ITEM_TYPE, ALREADY_HAS_MOD, CONFLICTING_MOD, NO_MORE_MODS_POSSIBLE, COUNT }

public class ItemWorldUIScript : MonoBehaviour {

    public static GameObject itemWorldInterface;
    public static bool itemWorldInterfaceOpen;
    public static ItemWorldUIScript singleton;
    public static TextMeshProUGUI itemWorldMenuHeader;
    public static TextMeshProUGUI itemInfoText;
    public static TextMeshProUGUI itemInfoName;
    //public static Text orbCount;
    public static Image itemInfoImage;
    public static TextMeshProUGUI itemGoldHeader;
    public static TextMeshProUGUI itemModifyCost;
    public static Slider itemGoldSlider;
    public static TextMeshProUGUI playerGold;
    public static ItemWorldMenuState menuState;

    public static UIManagerScript.UIObject itemWorldEnter;
    public static UIManagerScript.UIObject itemWorldModify;
    public static UIManagerScript.UIObject itemWorldExit;
    public static UIManagerScript.UIObject[] itemListButtons;

    public static Item itemSelected;
    public static Item orbSelected;
    public static bool lucidSkillOrbSelected;
    public static bool isItemSelected;
    public static List<Item> playerItemList;

    static string textForItemSelected;
    static string textForOrbselected;

    static int lastIndexHovered;

    public const int BASE_LUCID_SKILL_ORB_COST = 200; // jp

    public static readonly int[] tieredGoldCosts = {
        400, // 1.0
        500, // 1.05
        600, // 1.1
        700, // 1.15
        800, // 1.2
        1100, //1.25
        1400, // 1.3
        1700, // 1.35
        2200, //1.4
        2700, //1.45
        3200, // 1.5
        3750, // 1.55
        4500, //1.6
        5500, // 1.65
        6500, // 1.7
        7500, // 1.75
        8500, // 1.8
        9500, // 1.85
        11000, // 1.9
        12500, // 1.95
        14500, // 2
        17000, // 2.05
        20000, // 2.1
        24000, // 2.15
        30000, // 2.2
    };

    public static int goldTribute;

    public static bool initialized = false;

    static Item bufferOrb = null;

	// Use this for initialization

    public static void ResetAllVariablesToGameLoad()
    {
        initialized = false;
    }

	void Start () {

        if (initialized) return;

        initialized = true;
        menuState = ItemWorldMenuState.SELECTITEM;
       

        itemWorldMenuHeader = GameObject.Find("Item World Header").GetComponent<TextMeshProUGUI>();

        itemWorldInterface = GameObject.Find("Item World Interface");
        itemWorldInterfaceOpen = false;
        singleton = this;
        isItemSelected = false;
        itemSelected = null;
        playerItemList = new List<Item>();

        itemInfoText = GameObject.Find("ItemWorldItemText").GetComponent<TextMeshProUGUI>();
        itemInfoName = GameObject.Find("ItemWorldItemName").GetComponent<TextMeshProUGUI>();
        itemInfoImage = GameObject.Find("ItemWorldItemImage").GetComponent<Image>();

        itemWorldEnter = new UIManagerScript.UIObject();
        itemWorldEnter.gameObj = GameObject.Find("Item World Enter");
        itemWorldEnter.mySubmitFunction = TryEnterItemWorld;

        itemWorldModify = new UIManagerScript.UIObject();
        itemWorldModify.gameObj = GameObject.Find("Item World Modify");
        itemWorldModify.mySubmitFunction = ModifyItem;

        itemWorldExit = new UIManagerScript.UIObject();
        itemWorldExit.gameObj = GameObject.Find("Item World Exit");
        itemWorldExit.mySubmitFunction = CloseItemWorldInterface;

        itemGoldHeader = GameObject.Find("Item World Gold Header").GetComponent<TextMeshProUGUI>();
        itemGoldSlider = GameObject.Find("Item World Gold").GetComponent<Slider>();
        itemModifyCost = GameObject.Find("Item World Modify Cost").GetComponent<TextMeshProUGUI>();

        playerGold = GameObject.Find("Item World Player Gold").GetComponent<TextMeshProUGUI>();

        itemListButtons = new UIManagerScript.UIObject[12];
        for (int i = 0; i < itemListButtons.Length; i++)
        {
            itemListButtons[i] = new UIManagerScript.UIObject();
            string finder = "Item World Button " + (i + 1);
            itemListButtons[i].gameObj = GameObject.Find(finder);
            itemListButtons[i].subObjectImage = GameObject.Find(finder + " Sprite").GetComponent<Image>();
            itemListButtons[i].myOnSelectAction = ShowItemInfo;
            itemListButtons[i].mySubmitFunction = SelectItem;
            itemListButtons[i].onSelectValue = i;
            itemListButtons[i].onSubmitValue = i;
        }

        itemListButtons[0].neighbors[(int)Directions.NORTH] = itemListButtons[11];
        itemListButtons[0].neighbors[(int)Directions.SOUTH] = itemListButtons[1];

        itemListButtons[1].neighbors[(int)Directions.NORTH] = itemListButtons[0];
        itemListButtons[1].neighbors[(int)Directions.SOUTH] = itemListButtons[2];

        itemListButtons[2].neighbors[(int)Directions.NORTH] = itemListButtons[1];
        itemListButtons[2].neighbors[(int)Directions.SOUTH] = itemListButtons[3];

        itemListButtons[3].neighbors[(int)Directions.NORTH] = itemListButtons[2];
        itemListButtons[3].neighbors[(int)Directions.SOUTH] = itemListButtons[4];

        itemListButtons[4].neighbors[(int)Directions.NORTH] = itemListButtons[3];
        itemListButtons[4].neighbors[(int)Directions.SOUTH] = itemListButtons[5];

        itemListButtons[5].neighbors[(int)Directions.NORTH] = itemListButtons[4];
        itemListButtons[5].neighbors[(int)Directions.SOUTH] = itemListButtons[6];

        itemListButtons[6].neighbors[(int)Directions.NORTH] = itemListButtons[5];
        itemListButtons[6].neighbors[(int)Directions.SOUTH] = itemListButtons[7];

        itemListButtons[7].neighbors[(int)Directions.NORTH] = itemListButtons[6];
        itemListButtons[7].neighbors[(int)Directions.SOUTH] = itemListButtons[8];

        itemListButtons[8].neighbors[(int)Directions.NORTH] = itemListButtons[7];
        itemListButtons[8].neighbors[(int)Directions.SOUTH] = itemListButtons[9];

        itemListButtons[9].neighbors[(int)Directions.NORTH] = itemListButtons[8];
        itemListButtons[9].neighbors[(int)Directions.SOUTH] = itemListButtons[10];

        itemListButtons[10].neighbors[(int)Directions.NORTH] = itemListButtons[9];
        itemListButtons[10].neighbors[(int)Directions.SOUTH] = itemListButtons[11];

        itemListButtons[11].neighbors[(int)Directions.NORTH] = itemListButtons[10];
        itemListButtons[11].neighbors[(int)Directions.SOUTH] = itemListButtons[0];

        itemListButtons[11].directionalActions[(int)Directions.SOUTH] = itemListButtons[11].TryScrollPool;
        itemListButtons[11].directionalValues[(int)Directions.SOUTH] = 1;

        itemListButtons[0].directionalActions[(int)Directions.NORTH] = itemListButtons[0].TryScrollPool;
        itemListButtons[0].directionalValues[(int)Directions.NORTH] = -1;

        itemWorldEnter.neighbors[(int)Directions.NORTH] = itemWorldModify;
        itemWorldEnter.neighbors[(int)Directions.SOUTH] = itemWorldModify;

        itemWorldModify.neighbors[(int)Directions.NORTH] = itemWorldEnter;
        itemWorldModify.neighbors[(int)Directions.SOUTH] = itemWorldEnter;

        UIManagerScript.itemWorldScrollbar = GameObject.Find("ItemWorldScrollbar");

        CloseItemWorldInterface(0);

        FontManager.LocalizeMe(itemWorldMenuHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(itemInfoText, TDFonts.WHITE);
        FontManager.LocalizeMe(itemInfoName, TDFonts.WHITE);
        FontManager.LocalizeMe(itemGoldHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(itemModifyCost, TDFonts.WHITE);
        FontManager.LocalizeMe(playerGold, TDFonts.WHITE);

        TextMeshProUGUI enterMesh = itemWorldEnter.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        FontManager.LocalizeMe(enterMesh, TDFonts.BLACK);
        if (StringManager.gameLanguage != EGameLanguage.de_germany)
        {
            enterMesh.text = StringManager.GetString("dialog_openitemworld_main2_btn_0").ToUpperInvariant();
        }
        else
        {
            enterMesh.text = StringManager.GetString("dialog_openitemworld_main2_btn_0");
            enterMesh.rectTransform.offsetMin = new Vector2(8f, 0f);
            enterMesh.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }
        

        TextMeshProUGUI modifyMesh = itemWorldModify.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        FontManager.LocalizeMe(modifyMesh, TDFonts.BLACK);
        modifyMesh.text = StringManager.GetString("ui_btn_modifyitem");
        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            modifyMesh.rectTransform.offsetMin = new Vector2(8f, 0f);
            modifyMesh.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }

        TextMeshProUGUI exitMesh = itemWorldExit.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        if (exitMesh != null)
        {
            FontManager.LocalizeMe(exitMesh, TDFonts.BLACK);
        }        
    }

    public void CloseItemWorldInterface(int dummy)
    {
        bool wasOpen = itemWorldInterfaceOpen;
        itemWorldInterfaceOpen = false;
        UIManagerScript.HideDialogMenuCursor();
        itemWorldInterface.SetActive(false);
        if (wasOpen)
        {
            TutorialManagerScript.OnUIClosed();
            GuideMode.OnFullScreenUIClosed();
        }
    }

    public void ModifyItem(int dummy)
    {
        if (itemSelected == null)
        {
            Debug.Log("No item selected.");
            return;
        }

        if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("orb_itemworld") < GetOrbCost(orbSelected, itemSelected))
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        StringManager.SetTag(0, itemSelected.displayName);

        if (itemSelected.mods == null || itemSelected.mods.Count == 0)
        {
            UIManagerScript.PlayCursorSound("Error");
            GameLogScript.GameLogWrite(StringManager.GetString("log_error_cannotremovemods"),GameMasterScript.heroPCActor);
            return;
        }

        Equipment eq = itemSelected as Equipment;

        if (!eq.ValidForModRemoval())
        {
            UIManagerScript.PlayCursorSound("Error");
            GameLogScript.GameLogWrite(StringManager.GetString("log_error_cannotremovemods"),GameMasterScript.heroPCActor);
            return;
        }

        int removeModCost = GetEnchantMaxCost(true) / 2;

        if (GameMasterScript.heroPCActor.GetMoney() < removeModCost)
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        itemWorldInterfaceOpen = false;
        itemWorldInterface.SetActive(false);

        int removeModOrbCost = GetOrbCost(null, itemSelected, true);
        
        GameMasterScript.gmsSingleton.SetTempGameData("removemodcost", removeModCost);
        GameMasterScript.gmsSingleton.SetTempGameData("removemodbaseorbcost", removeModOrbCost);
        GameMasterScript.gmsSingleton.SetTempStringData("removemoditem", itemSelected.displayName);
        GameMasterScript.gmsSingleton.SetTempGameData("removemoditemid", itemSelected.actorUniqueID);
        StringManager.SetTag(0, itemSelected.displayName);
        StringManager.SetTag(1, removeModCost.ToString());
        StringManager.SetTag(2, removeModOrbCost.ToString());
        UIManagerScript.StartConversationByRef("dreamcaster_modify", DialogType.STANDARD, MapMasterScript.activeMap.FindActor("npc_itemworld") as NPC);
    }

    public void TryEnterItemWorld(int dummy)
    {
        if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("orb_itemworld") == 0)
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }
        if (itemSelected == null)
        {
            Debug.Log("No item selected.");
            return;
        }

        Equipment eq = itemSelected as Equipment;
        bool freeValid = (orbSelected.IsJobSkillOrb() && eq.CanHandleFreeSkillOrb());

        if (!itemSelected.ValidForItemWorld() && !freeValid)
        {
            StringManager.SetTag(0, itemSelected.displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("log_error_itemupgrade"), GameMasterScript.heroPCActor);
            UIManagerScript.PlayCursorSound("Error");
            return;
        }
        if (IsItemCompatibleWithOrb(orbSelected) != MagicModCompatibility.POSSIBLE)
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }
        if (orbSelected == null)
        {
            Debug.Log("No orb selected.");
            return;
        }


        if (goldTribute == 0 
            && (eq.CanHandleMoreMagicMods() || (eq.CanHandleFreeSkillOrb() && lucidSkillOrbSelected)))
        {
            GameMasterScript.gmsSingleton.SetTempGameData("dreamcaster_item_selected", itemSelected.actorUniqueID);
            GameMasterScript.gmsSingleton.SetTempGameData("dreamcaster_orb_selected", orbSelected.actorUniqueID);
            if (lucidSkillOrbSelected)
            {
                GameMasterScript.gmsSingleton.SetTempGameData("dreamcaster_lucidorb", 1);
                StringManager.SetTag(0, StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP));
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("dreamcaster_lucidorb", 0);
                StringManager.SetTag(0, StringManager.GetString("misc_gold"));
            }
            UIManagerScript.StartConversationByRef("itemdream_enter_nogold", DialogType.KEYSTORY, null);
            CloseItemWorldInterface(0);
            return;
        }

        itemWorldInterfaceOpen = false;
        itemWorldInterface.SetActive(false);       
        float magicChance = (float)goldTribute / (float)(GetEnchantMaxCost(false));

        if (lucidSkillOrbSelected)
        {
            GameMasterScript.heroPCActor.AddJP(goldTribute * -1);
        }
        else
        {
            GameMasterScript.heroPCActor.ChangeMoney(goldTribute * -1);
        }

        

        GameMasterScript.TryBeginItemWorld(itemSelected, orbSelected, magicChance);
    }

    public static void ClearItemHighlights()
    {
        for (int i = 0; i < itemListButtons.Length; i++)
        {
            itemListButtons[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
        }
    }

    void UpdateGoldText()
    {
        if (itemSelected == null || !isItemSelected)
        {
            itemGoldHeader.text = StringManager.GetString("ui_itemworld_transmute_then_select_item_first");  
            itemGoldHeader.gameObject.SetActive(false);
            itemGoldSlider.gameObject.SetActive(false);
            return;
        }

        itemGoldHeader.gameObject.SetActive(true);
        itemGoldSlider.gameObject.SetActive(true);

        float maxEnchantCost = GetEnchantMaxCost(false);


        Equipment eq = itemSelected as Equipment;
        if (eq.ValidForModRemoval())
        {
            itemModifyCost.gameObject.SetActive(true);
        }
        else
        {
            itemModifyCost.gameObject.SetActive(false);
        }        

        int modCost = (int)(maxEnchantCost / 2);

        string textColor = "<color=yellow>";
        string orbColor = UIManagerScript.cyanHexColor;

        if (modCost > GameMasterScript.heroPCActor.GetMoney())
        {
            textColor = UIManagerScript.redHexColor;
        }
        if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("orb_itemworld") == 0)
        {
            orbColor = UIManagerScript.redHexColor;
        }

        int baseOrbCost = GetOrbCost(null, itemSelected, true);

        StringManager.SetTag(0, baseOrbCost.ToString());        

        itemModifyCost.text = "<color=yellow>" + StringManager.GetString("ui_itemworld_removemod") + "</color>\n" + textColor + modCost + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>, " + orbColor + StringManager.GetString("misc_itemworld_orbcost") + "</color>";
        
        float chance = 0;


        bool canHandleMoreMods = eq.CanHandleMoreMagicMods();

        if (orbSelected.IsJobSkillOrb() && eq.CanHandleFreeSkillOrb())
        {
            canHandleMoreMods = true;
        }

        if (!canHandleMoreMods && eq.timesUpgraded >= Equipment.GetMaxUpgrades() && !(orbSelected.IsJobSkillOrb() && eq.CanHandleFreeSkillOrb()))
        {
            itemGoldHeader.text = StringManager.GetString("ui_dreamcaster_nomoremods");
            return;
        }

        if (maxEnchantCost == 0)
        {
            chance = 0;
        }
        else
        {
            chance = ((float)goldTribute / maxEnchantCost)*100f;
        }

        //Debug.Log(canHandleMoreMods + " " + goldTribute + " " + maxEnchantCost);

        if (!canHandleMoreMods)
        {
            chance = 0f;
        }

        StringManager.SetTag(0, goldTribute.ToString());
        StringManager.SetTag(1, ((int)chance).ToString());

        if (lucidSkillOrbSelected)
        {
            itemGoldHeader.text = StringManager.GetString("ui_dreamcaster_addmod_jp_cost");
        }
        else
        {
            itemGoldHeader.text = StringManager.GetString("ui_dreamcaster_addmod_cost");
        }        
    }

    public static int GetOrbCostByCV(float challengeValue1, float challengeValue2, bool legendary, bool specialMod)
    {
        float calculateCV = (challengeValue1 + challengeValue2) / 2f;

        if (legendary)
        {
            calculateCV += 0.1f;
        }
        if (specialMod)
        {
            calculateCV += 0.1f;
        }

        if (calculateCV < 1.3f)
        {
            return 1;
        }
        else if (calculateCV >= 1.3f && calculateCV < 1.5f)
        {
            return 2;
        }
        else if (calculateCV >= 1.5 && calculateCV < 1.65f)
        {
            return 3;
        }
        else if (calculateCV >= 1.65 && calculateCV < 1.75f)
        {
            return 4;
        }
        else if (calculateCV >= 1.75f && calculateCV < 1.8f)
        {
            return 5;
        }
        else
        {
            return 6;
        }
    }

    public static int GetOrbCost(Item orb, Item itemToModify, bool getMinimum = false)
    {
        float cv2 = itemToModify.challengeValue;
        float cv1 = 0f;
        if (getMinimum)
        {
            cv1 = 1.0f; 
        }

        bool specialMod = false;

        if (orb != null && orb.IsLucidOrb())
        {
            MagicMod mm = singleton.GetMagicModFromOrb(orb);
            cv1 = mm.challengeValue;
            specialMod = mm.IsSpecialMod();
        }

        return GetOrbCostByCV(cv1, cv2, itemToModify.legendary, specialMod);        
    }
    public static int GetEnchantMaxCost(bool forceGold)
    {                
        if (itemSelected != null)
        {
            // Magic chance formula here
            float multiplier = 1f;
            switch((int)itemSelected.rarity)
            {
                case (int)Rarity.UNCOMMON:
                    multiplier = 1.2f;
                    break;
                case (int)Rarity.MAGICAL:
                    multiplier = 1.4f;
                    break;
                case (int)Rarity.ANCIENT:
                    multiplier = 1.65f;
                    break;
                case (int)Rarity.ARTIFACT:
                    multiplier = 1.9f;
                    break;
                case (int)Rarity.LEGENDARY:
                    multiplier = 2.3f;
                    break;
                case (int)Rarity.GEARSET:
                    multiplier = 2.3f;
                    break;          
            }

            bool orbIsNull = orbSelected == null;
            
            // JP cost for orbs!
            if (!orbIsNull && lucidSkillOrbSelected && !forceGold)
            {
                int cost = (int)(ItemWorldUIScript.BASE_LUCID_SKILL_ORB_COST * multiplier);
                if (itemSelected.rarity >= Rarity.LEGENDARY)
                {
                    cost *= 2;
                }
                return cost;
            }

                float baseCost = 0;

            float maxChallengeRating = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION : GameMasterScript.MAX_CHALLENGE_RATING;

            float useCV = itemSelected.challengeValue;
            if (useCV >= 50f || useCV < 1.0f)
            {
                useCV = 1.0f;
            }
            else if (useCV >= maxChallengeRating)
            {
                useCV = maxChallengeRating;
            }

            if (useCV >= 1.0f && useCV < 1.05f)
            {
                baseCost = tieredGoldCosts[0];
            }
            else if (useCV >= 1.05f && useCV < 1.1f)
            {
                baseCost = tieredGoldCosts[1];
            }
            else if (useCV >= 1.1f && useCV < 1.15f)
            {
                baseCost = tieredGoldCosts[2];
            }
            else if (useCV >= 1.15f && useCV < 1.2f)
            {
                baseCost = tieredGoldCosts[3];
            }
            else if (useCV >= 1.2f && useCV < 1.25f)
            {
                baseCost = tieredGoldCosts[4];
            }
            else if (useCV >= 1.25f && useCV < 1.3f)
            {
                baseCost = tieredGoldCosts[5];
            }
            else if (useCV >= 1.3f && useCV < 1.35f)
            {
                baseCost = tieredGoldCosts[6];
            }
            else if (useCV >= 1.35f && useCV < 1.4f)
            {
                baseCost = tieredGoldCosts[7];
            }
            else if (useCV >= 1.4f && useCV < 1.45f)
            {
                baseCost = tieredGoldCosts[8];
            }
            else if (useCV >= 1.45f && useCV < 1.5f)
            {
                baseCost = tieredGoldCosts[9];
            }
            else if (useCV >= 1.5f && useCV < 1.55f)
            {
                baseCost = tieredGoldCosts[10];
            }
            else if (useCV >= 1.55f && useCV < 1.6f)
            {
                baseCost = tieredGoldCosts[11];
            }
            else if (useCV >= 1.6f && useCV < 1.65f)
            {
                baseCost = tieredGoldCosts[12];
            }
            else if (useCV >= 1.65f && useCV < 1.7f)
            {
                baseCost = tieredGoldCosts[13];
            }
            else if (useCV >= 1.7f && useCV < 1.75f)
            {
                baseCost = tieredGoldCosts[14];
            }
            else if (useCV >= 1.75f && useCV < 1.8f)
            {
                baseCost = tieredGoldCosts[15];
            }
            else if (useCV >= 1.8f && useCV < 1.85f)
            {
                baseCost = tieredGoldCosts[16];
            }
            else if (useCV >= 1.85f && useCV < 1.9f)
            {
                baseCost = tieredGoldCosts[17];
            }
            else if (useCV >= 1.9f && useCV < 1.95f)
            {
                baseCost = tieredGoldCosts[18];
            }
            else if (useCV >= 1.95f && useCV < 2f)
            {
                baseCost = tieredGoldCosts[19];
            }
            else if (useCV >= 2f && useCV < 2.05f)
            {
                baseCost = tieredGoldCosts[20];
            }
            else if (useCV >= 2.05f && useCV < 2.1f)
            {
                baseCost = tieredGoldCosts[21];
            }
            else if (useCV >= 2.1f && useCV < 2.15f)
            {
                baseCost = tieredGoldCosts[22];
            }
            else if (useCV >= 2.15f && useCV < 2.2f)
            {
                baseCost = tieredGoldCosts[23];
            }
            else if (useCV >= 2.2f)
            {
                baseCost = tieredGoldCosts[24];
            }

            if (baseCost == 0)
            {
                baseCost = 1;
                //Debug.Log("WARNING: Item world tribute cost is 1 for item " + itemSelected.actorRefName + " " + itemSelected.actorUniqueID + " " + itemSelected.challengeValue);
            }

            if (!orbIsNull)
            {
                if (!orbSelected.IsLucidOrb())
                {
                    multiplier *= 0.66f;
                }
            }

            return (int)(baseCost * multiplier);
        }
        else
        {
            return 0;
        }
    }

    public static void RefreshInterfaceToSelectOrb()
    {
        // Don't let the player get here without any orbs.
        menuState = ItemWorldMenuState.SELECTORB;
        itemWorldMenuHeader.text = StringManager.GetString("iw_header_selectorb");

        ClearItemHighlights();

        playerItemList.Clear();
        UIManagerScript.listArrayIndexOffset = 0;

        bufferOrb = null;

        PopulateItemList(orbs: true);

        if (bufferOrb != null)
        {
            playerItemList.Insert(0, bufferOrb);
        }

        UIManagerScript.UpdateItemWorldList(true);

        UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
        UIManagerScript.uiObjectFocus.myOnSelectAction.Invoke(UIManagerScript.uiObjectFocus.onSelectValue);        
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(itemWorldInterface.transform);

        itemWorldEnter.gameObj.SetActive(false);
        itemWorldEnter.enabled = false;
        itemWorldModify.gameObj.SetActive(false);
        itemWorldModify.enabled = false;
        itemGoldSlider.gameObject.SetActive(false);
        itemGoldHeader.gameObject.SetActive(false);
        itemModifyCost.gameObject.SetActive(false);

        UIManagerScript.UpdateScrollbarPosition();


    }

    static void UpdateGoldOrJPDisplay()
    {
        if (lucidSkillOrbSelected)
        {
            StringManager.SetTag(0, ((int)GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum]).ToString());
            playerGold.text = UIManagerScript.cyanHexColor + StringManager.GetString("ui_dreamcaster_player_jp") + "</color>";
        }
        else
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
            playerGold.text = "<color=yellow>" + StringManager.GetString("ui_dreamcaster_player_gold") + "</color>";
        }
    }

    public static void PopulateItemList(bool orbs)
    {
        playerItemList.Clear();
        if (orbs)
        {
            foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
            {
                if (itm.actorRefName == "orb_itemworld")
                {
                    // Just mark incompatible orbs in red and display some text to make this clearer for players
                    //bool orbValid = IsItemCompatibleWithOrb(itm);
                    //if (orbValid)
                    {
                        if (string.IsNullOrEmpty(itm.GetOrbMagicModRef()) && itm.ReadActorData("nightmare_orb") != 1)
                        {
                            bufferOrb = itm;
                            continue;
                        }

                        if (!TDSearchbar.CheckIfItemMatchesTerms(itm)) continue;

                        playerItemList.Add(itm);
                    }
                }
            }
            return;
        }

        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(itm, onlyActualFists: true))
            if (itm.displayName == "Fists" || itm.actorRefName == "weapon_fists") continue;
            if (itm.itemType == ItemTypes.EMBLEM) continue; // Emblem cannot be upgraded via Dreamcaster.
            if (itm.IsEquipment())
            {
                Equipment eq = itm as Equipment;

                if (!TDSearchbar.CheckIfItemMatchesTerms(eq)) continue;

                playerItemList.Add(eq);
            }
        }

        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[i];
            if (eq == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(eq, onlyActualFists: true)) continue;
            if (eq.actorRefName == "weapon_fists") continue;
            if (eq.itemType == ItemTypes.EMBLEM) continue;

            if (!TDSearchbar.CheckIfItemMatchesTerms(eq)) continue;

            playerItemList.Add(eq);
        }
    }

    public static void OpenItemWorldInterface()
    {
        bool alreadyOpen = itemWorldInterfaceOpen == true;
        ClearItemHighlights();
        itemSelected = null;
        isItemSelected = false;
        orbSelected = null;

        itemWorldInterfaceOpen = true;
        itemWorldInterface.SetActive(true);

        GuideMode.OnFullScreenUIOpened();


if (PlatformVariables.SHOW_SEARCHBARS)
{
        TDSearchbar.ClearSearchTerms();
        // Make sure to clear the visual text in the search bar
        itemWorldInterface.GetComponentInChildren<TMP_InputField>().text = "";
}

        menuState = ItemWorldMenuState.SELECTITEM;

        itemWorldMenuHeader.text = StringManager.GetString("iw_header_selectitem");

        playerItemList.Clear();
        
        itemInfoName.text = "";
        itemInfoText.text = "";
        itemInfoImage.color = UIManagerScript.transparentColor;

        if (!alreadyOpen)
        {
            UIManagerScript.PlayCursorSound("OPSelect");
        }
        
        UpdateGoldOrJPDisplay();

        PopulateItemList(orbs:false);

        UIManagerScript.UpdateItemWorldList(false);
        if (playerItemList.Count > 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(itemWorldExit);
        }

        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.ShowDialogMenuCursor();
        if (playerItemList.Count > 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
            UIManagerScript.uiObjectFocus.myOnSelectAction.Invoke(UIManagerScript.uiObjectFocus.onSelectValue);
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(itemWorldExit);
        }

        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(itemWorldInterface.transform);

        ResetGoldAmount();
        singleton.UpdateGoldText();

        itemWorldEnter.gameObj.SetActive(false);
        itemWorldEnter.enabled = false;
        itemWorldModify.gameObj.SetActive(false);
        itemWorldModify.enabled = false;
        itemGoldSlider.gameObject.SetActive(false);
        itemGoldHeader.gameObject.SetActive(false);
        itemModifyCost.gameObject.SetActive(false);

        UIManagerScript.UpdateScrollbarPosition();
    }

    public static void CancelPressed()
    {
if (PlatformVariables.SHOW_SEARCHBARS)
{
        TDSearchbar.ClearSearchTerms();
        itemWorldInterface.GetComponentInChildren<TMP_InputField>().text = "";
}
        UIManagerScript.PlayCursorSound("UITock");
        if (menuState == ItemWorldMenuState.SELECTORB)
        {
            if (orbSelected != null)
            {
                isItemSelected = false;
                ResetGoldAmount();
                singleton.UpdateGoldText();

                itemWorldEnter.gameObj.SetActive(false);
                itemWorldEnter.enabled = false;
                itemWorldModify.gameObj.SetActive(false);
                itemWorldModify.enabled = false;
                itemGoldSlider.gameObject.SetActive(false);
                itemGoldHeader.gameObject.SetActive(false);
                itemModifyCost.gameObject.SetActive(false);

                orbSelected = null;
                lucidSkillOrbSelected = false;
                UpdateGoldOrJPDisplay();
                ClearItemHighlights();
                UIManagerScript.UpdateItemWorldList(true);
                UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
            }
            else
            {
                OpenItemWorldInterface();
            }
            orbSelected = null;
            return;
        }
        /* if (isItemSelected)
        {
            ClearItemHighlights();
            UIManagerScript.UpdateItemWorldList();
            UIManagerScript.ChangeUIFocusAndAlignCursor(itemListButtons[0]);
        }
        else */
        {
            singleton.CloseItemWorldInterface(0);
        }
    }

    public static void ResetGoldAmount()
    {
        goldTribute = 0;
        itemGoldSlider.value = 0;
        singleton.UpdateGoldText();
    }

    public void ShowItemInfo(int index)
    {
        if (index < 0) return;

        //if (isItemSelected) return;

        Item highlight = null;

        lastIndexHovered = index;

        int lookupIndex = index + UIManagerScript.listArrayIndexOffset;
        if (index < 999)
        {
            if (lookupIndex >= playerItemList.Count)
            {
                // This could happen if search bar was used and the cursor was scrolled down.
                // Just do nothing I guess
                return; 
            }
            highlight = playerItemList[lookupIndex];
        }
        else
        {
            highlight = itemSelected;
        }
        

        if (highlight == null) return;

        if (highlight.IsEquipment())
        {
            itemInfoName.text = highlight.displayName;
            itemInfoImage.color = Color.white;
            //itemInfoImage.sprite = UIManagerScript.LoadSpriteFromAtlas(UIManagerScript.allItemGraphics, highlight.spriteRef);
            itemInfoImage.sprite = highlight.GetSpriteForUI();
            Equipment eqHighlight = highlight as Equipment;
            textForItemSelected = eqHighlight.GetItemWorldDescription();
            itemInfoText.text = textForItemSelected;
        }

        if (index == 999)
        {
            highlight = orbSelected;
        }

        if (!highlight.IsEquipment())
        {
            // Must be an orb.            
            string mmRef = highlight.GetOrbMagicModRef();
            string textForOrb = highlight.displayName + "\n"; // This was size 44 before, was that necessary?

            if (!string.IsNullOrEmpty(mmRef))
            {
                MagicMod mmTemplate = MagicMod.FindModFromName(mmRef);
                //mmTemplate.modFlags[(int)MagicModFlags.QUIVER
                switch (mmTemplate.slot)
                {
                    case EquipmentSlots.ANY:
                        StringManager.SetTag(0, StringManager.GetString("eq_slot_any"));
                        break;
                    case EquipmentSlots.WEAPON:
                        StringManager.SetTag(0, StringManager.GetString("eq_slot_weapon"));
                        break;
                    case EquipmentSlots.ARMOR:
                        StringManager.SetTag(0, StringManager.GetString("eq_slot_armor"));
                        break;
                    case EquipmentSlots.ACCESSORY:
                        StringManager.SetTag(0, StringManager.GetString("eq_slot_accessory"));
                        break;
                    case EquipmentSlots.OFFHAND:
                        StringManager.SetTag(0, StringManager.GetString("eq_slot_offhand"));
                        break;
                }
                textForOrb += "<color=yellow>" + StringManager.GetString("ui_hover_orb_readout") + "</color>\n\n";
                textForOrb += UIManagerScript.greenHexColor + mmTemplate.GetDescription() + "</color>\n\n";                
            }
            else if (highlight.ReadActorData("nightmare_orb") == 1)
            {
                textForOrb += UIManagerScript.orangeHexColor + StringManager.GetString("ui_hover_orb_readout_nightmare") + "</color>";
            }
            else
            {
                textForOrb += "<color=yellow>" + StringManager.GetString("ui_hover_orb_readout_generic") + "</color>";
            }

            if (itemSelected != null && itemSelected.IsEquipment())
            {
                textForItemSelected = (itemSelected as Equipment).GetItemWorldDescription();
            }

            MagicModCompatibility orbCompatibility = IsItemCompatibleWithOrb(highlight);
            if (orbCompatibility != MagicModCompatibility.POSSIBLE)
            {
                switch(orbCompatibility)
                {
                    case MagicModCompatibility.ALREADY_HAS_MOD:
                        StringManager.SetTag(0, StringManager.GetString("mod_compatibility_existing"));
                        break;
                    case MagicModCompatibility.WRONG_ITEM_TYPE:
                        StringManager.SetTag(0, StringManager.GetString("mod_compatibility_slot"));
                        break;
                    case MagicModCompatibility.NO_MORE_MODS_POSSIBLE:
                        StringManager.SetTag(0, StringManager.GetString("mod_compatibility_full"));
                        break;
                    case MagicModCompatibility.CONFLICTING_MOD:
                        StringManager.SetTag(0, StringManager.GetString("mod_compatibility_conflict"));
                        break;
                }
                textForOrb += "\n" + StringManager.GetString("mod_compatibility_generic");
            }

            itemInfoText.text = textForItemSelected + "\n\n" + textForOrb;


        }

        // Visual stuff below.

        if (isItemSelected) return;

        ClearItemHighlights();

        itemListButtons[index].gameObj.GetComponent<Image>().color = Color.white;

        //UIManagerScript.uiObjectFocus = itemListButtons[index];
        UIManagerScript.ChangeUIFocus(itemListButtons[index], processEvent: false);
    }

    public MagicMod GetMagicModFromOrb(Item c)
    {
        string modRef = c.GetOrbMagicModRef();
        if (!string.IsNullOrEmpty(modRef))
        {
            MagicMod mm = MagicMod.FindModFromName(modRef);
            return mm;
        }
        return null;
    }

    public static MagicModCompatibility IsItemCompatibleWithOrb(Item orb)
    {
        MagicMod mm = singleton.GetMagicModFromOrb(orb);

        Equipment eq = itemSelected as Equipment;
        MagicModCompatibility eqCheck = eq.IsModValidForMe(mm);

        if (eqCheck != MagicModCompatibility.POSSIBLE)
        {

            // If we are at max MODS but not at max UPGRADES, we should allow continuing here.
            if (mm == null && itemSelected.timesUpgraded < Equipment.GetMaxUpgrades())
            {
                return MagicModCompatibility.POSSIBLE;
            }

            return eqCheck;
        }

        bool plainOrb = mm == null;

        if (plainOrb)
        {
            return MagicModCompatibility.POSSIBLE;
        }

        if (!plainOrb && eq.HasModByRef(mm.refName))
        {
            return MagicModCompatibility.ALREADY_HAS_MOD;
        }

        if (!plainOrb && (mm.slot == EquipmentSlots.ANY || mm.slot == eq.slot))
        {
            return MagicModCompatibility.POSSIBLE;
        }
        return MagicModCompatibility.WRONG_ITEM_TYPE;
    }

    public void SelectItem(int index)
    {
        isItemSelected = false;
        ShowItemInfo(index);
        isItemSelected = true;

        UIManagerScript.PlayCursorSound("Equip Item");

        int indexToUse = index + UIManagerScript.listArrayIndexOffset;

        if (indexToUse >= playerItemList.Count || indexToUse < 0)
        {
            // This is a clumsy fix. What's actually going on here?
            Debug.Log("Selected Item World item index of " + (index + UIManagerScript.listArrayIndexOffset) + " exceeds player inv count " + playerItemList.Count + " State is: " + menuState);
            indexToUse = playerItemList.Count - 1;
        }

        if (menuState == ItemWorldMenuState.SELECTORB)
        {
            orbSelected = playerItemList[indexToUse];
            if (orbSelected.IsJobSkillOrb())
            {
                lucidSkillOrbSelected = true;
            }
            else
            {
                lucidSkillOrbSelected = false;
            }

            UpdateGoldOrJPDisplay();

            ShowItemInfo(999);

            Equipment eq = itemSelected as Equipment;

            bool validForItemWorld = itemSelected.ValidForItemWorld() || (orbSelected.IsJobSkillOrb() && eq.CanHandleFreeSkillOrb());
            if (validForItemWorld)
            {
                itemWorldEnter.gameObj.SetActive(true);
                itemWorldEnter.enabled = true;
                itemGoldSlider.gameObject.SetActive(true);
                itemGoldHeader.gameObject.SetActive(true);
                ResetGoldAmount();
                UpdateGoldText();
            }
            else
            {
                itemWorldEnter.gameObj.SetActive(false);
                itemWorldEnter.enabled = false;
                itemGoldSlider.gameObject.SetActive(false);
                itemGoldHeader.gameObject.SetActive(false);
            }
            if (eq.ValidForModRemoval())
            {
                itemWorldModify.gameObj.SetActive(true);
                itemWorldModify.enabled = true;
                itemModifyCost.gameObject.SetActive(true);
                UpdateGoldText();
            }
            else
            {
                itemWorldModify.gameObj.SetActive(false);
                itemWorldModify.enabled = false;
                itemModifyCost.gameObject.SetActive(false);
            }

            if (itemWorldEnter.enabled)
            {
                UIManagerScript.ChangeUIFocusAndAlignCursor(itemWorldEnter);
            }
            else
            {
                UIManagerScript.ChangeUIFocusAndAlignCursor(itemWorldModify);
            }

            return;
        }

        // Selected an ORB instead.

        itemSelected = playerItemList[indexToUse];
        int qty = GameMasterScript.heroPCActor.myInventory.GetItemQuantity("orb_itemworld");
        if (qty == 0)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            return;
        }

        // Make sure lucid/skill orbs are valid before moving on.
        bool anyValid = false;
        bool anyOrbsAtAll = false;
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.actorRefName == "orb_itemworld")
            {
                anyOrbsAtAll = true;
                bool orbValid = IsItemCompatibleWithOrb(itm) == MagicModCompatibility.POSSIBLE;
                if (orbValid)
                {
                    anyValid = true;
                    break;
                }
            }
        }

        if (!anyOrbsAtAll) // was anyValid, but we DO want to proceed to next screen if we have *anything*
        {
            CloseItemWorldInterface(0);
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_iw_need_orb");
            return;
        }

if (PlatformVariables.SHOW_SEARCHBARS)
{
        TDSearchbar.ClearSearchTerms();
        itemWorldInterface.GetComponentInChildren<TMP_InputField>().text = "";
}

        orbSelected = null;
        lucidSkillOrbSelected = false;
        
        UpdateGoldOrJPDisplay();
        RefreshInterfaceToSelectOrb();
    }

    public static float TryAdjustGoldAmount(float amount)
    {
        if (!itemGoldSlider.gameObject.activeSelf) return 0f;

        int max = GetEnchantMaxCost(false);

        float transAmount = amount * max;

        if (lucidSkillOrbSelected)
        {

            if (transAmount > GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum])
            {
                transAmount = GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum];
            }

        }
        else
        {
            if (transAmount > GameMasterScript.heroPCActor.GetMoney())
            {
                transAmount = GameMasterScript.heroPCActor.GetMoney();
            }
        }

        if (transAmount > max)
        {
            transAmount = max;
        }

        goldTribute = (int)transAmount;
        singleton.UpdateGoldText();

        float returnVal = transAmount / GetEnchantMaxCost(false);
        return returnVal;
        
    }

    public static Consumable CreateItemWorldOrb(float challengeValue, bool guaranteeLucidOrb, bool guaranteeSkillOrb = false)
    {

        Item orb = Item.GetItemTemplateFromRef("orb_itemworld");
        Consumable newOrb = new Consumable();
        newOrb.CopyFromItem(orb);
        newOrb.SetUniqueIDAndAddToDict();

        bool skillModOrbsPossible = false;
        bool specificOrbsPossible = false;

        // Conditions under which special orbs drop.
        if (GameMasterScript.heroPCActor.lowestFloorExplored >= 7 || guaranteeLucidOrb)
        {
            specificOrbsPossible = true;
        }
        if (GameMasterScript.heroPCActor.lowestFloorExplored >= 10 || guaranteeSkillOrb)
        {
            skillModOrbsPossible = true;
        }

        // Special magic mod stuff!

        float localChance = GameMasterScript.gmsSingleton.specificModOrbDropChance;

        if (GameMasterScript.actualGameStarted && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("rubymoon"))
        {
            localChance += 0.05f;
        }

        if (!specificOrbsPossible)
        {
            localChance = 0f;
        }

        if ((UnityEngine.Random.Range(0, 1f) <= localChance && challengeValue >= 1.5f) || guaranteeLucidOrb || guaranteeSkillOrb)
        {
            List<MagicMod> possibleMods = new List<MagicMod>();

            List<MagicMod> playerTailoredSkillMods = new List<MagicMod>();

            bool isSkillOrb = false;

            if ((skillModOrbsPossible && UnityEngine.Random.Range(0,1f) <= GameMasterScript.gmsSingleton.classModOrbDropChance) || guaranteeSkillOrb)
            {
                isSkillOrb = true;
            }

            //isSkillOrb = false; // TEMPORARY: DISABLE SKILL ORBS

            List<string> modifiedRefs = new List<string>();

            foreach (MagicMod mm in GameMasterScript.masterMagicModList.Values)
            {
                // Class ability.
                if (mm.lucidOrbsOnly)
                {
                    if (!isSkillOrb) continue;
                }
                else
                {
                    if (!mm.lucidOrbsOnly && isSkillOrb) continue;
                }

                float minCV = mm.challengeValue;
                float maxCV = mm.maxChallengeValue;

                if ((challengeValue >= minCV && challengeValue <= maxCV && !mm.noNameChange) || guaranteeSkillOrb)
                {
                    possibleMods.Add(mm);
                    if (mm.lucidOrbsOnly)
                    {
                        foreach(string modifiedRef in mm.GetRefsOfSkillsModified())
                        {
                            //modifiedRefs.Add(modifiedRef);
                            if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(modifiedRef))
                            {
                                playerTailoredSkillMods.Add(mm);
                                break;
                            }
                        }
                        
                    }

                }
            }
            
            if (skillModOrbsPossible && playerTailoredSkillMods.Count > 0 && UnityEngine.Random.Range(0,1f) <= 0.4f)
            {
                // 40% chance to get a tailored mod.                
                possibleMods = playerTailoredSkillMods.Distinct().ToList();
            }

            if (possibleMods.Count > 0)
            {
                MagicMod template = possibleMods[UnityEngine.Random.Range(0, possibleMods.Count)];
                string mRef = template.refName;
                newOrb.SetOrbMagicModRef(mRef);

                if (template.bDontAnnounceAddedAbilities)
                {
                    // Class orb!
                    newOrb.rarity = Rarity.ANCIENT;
                }
                else
                {
                    newOrb.rarity = Rarity.UNCOMMON;
                }
                
                newOrb.salePrice = 80 * (int)(Mathf.Pow(challengeValue, 4f));
                newOrb.RebuildDisplayName();
            }
        }

        return newOrb;
    }	

    public static void ToggleAlternateInfo(bool state)
    {
        // if TRUE, and we are looking at an item in the dreamcaster, replace the tooltip with that item's description.

        Item check = itemSelected;
        if (check == null)
        {
            if (lastIndexHovered < 999)
            {
                if (lastIndexHovered + UIManagerScript.listArrayIndexOffset >= playerItemList.Count)
                {
                    return;
                }
                check = playerItemList[lastIndexHovered + UIManagerScript.listArrayIndexOffset];
            }
        }

        if (check == null)
        {
            return;
        }

        if (state) // Show alternate info
        {
            itemInfoText.text = check.GetItemInformationNoName(false);
        }
        else // Revert to regular item / orb info related to dreams
        {
            singleton.ShowItemInfo(lastIndexHovered);
        }
    }
}
