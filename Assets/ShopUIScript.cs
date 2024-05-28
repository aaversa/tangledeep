using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

public class ShopUIScript : MonoBehaviour
{

    //public static bool shopInterfaceOpen;

    public static TextMeshProUGUI shopHeader;
    public static TextMeshProUGUI shopMoney;
    public static GameObject shopInterface;
    public static UIManagerScript.UIObject[] shopItemButtonList;
    public static UIManagerScript.UIObject shopExit;
    public static UIManagerScript.UIObject shopItemButton1;
    public static UIManagerScript.UIObject shopItemButton2;
    public static UIManagerScript.UIObject shopItemButton3;
    public static UIManagerScript.UIObject shopItemButton4;
    public static UIManagerScript.UIObject shopItemButton5;
    public static UIManagerScript.UIObject shopItemButton6;
    public static UIManagerScript.UIObject shopItemButton7;
    public static UIManagerScript.UIObject shopItemButton8;
    public static UIManagerScript.UIObject shopItemButton9;
    public static UIManagerScript.UIObject shopItemButton10;
    public static UIManagerScript.UIObject shopItemButton11;
    public static UIManagerScript.UIObject shopItemButton12;
    public static UIManagerScript.UIObject shopItemButton13;
    public static GameObject shopItemComparisonHeader;

    public static TextMeshProUGUI shopComparisonAreaText;

    public static UIManagerScript.UIObject shopItemSortType;
    public static UIManagerScript.UIObject shopItemSortValue;

    public static ShopState shopState;

    public static List<Item> playerItemList;
    public static ShopUIScript singleton;

    public static TextMeshProUGUI shopItemInfoText;
    public static TextMeshProUGUI shopItemInfoName;
    public static Image shopItemInfoImage;
    public static TextMeshProUGUI shopCompItemInfoText;
    public static TextMeshProUGUI shopCompItemInfoName;
    public static Image shopCompItemInfoImage;

    public bool initialized;

    public void Initialize()
    {
        if (initialized) return;
        playerItemList = new List<Item>();
        singleton = this;

        shopInterface = GameObject.Find("Shop Interface");
        UIManagerScript.menuScreenObjects[(int)UITabs.SHOP] = shopInterface;
        shopHeader = GameObject.Find("Shop Header").GetComponent<TextMeshProUGUI>();
        shopMoney = GameObject.Find("Shop Money").GetComponent<TextMeshProUGUI>();
        shopItemButtonList = new UIManagerScript.UIObject[13]; // Number of buttons
        //shopItemInfo = GameObject.Find("Shop Item Info");
        //shopItemComparison = GameObject.Find("Shop Comparison");
        shopItemComparisonHeader = GameObject.Find("Shop Comparison Header");

        FontManager.LocalizeMe(shopHeader, TDFonts.WHITE);
        FontManager.LocalizeMe(shopMoney, TDFonts.WHITE);
        FontManager.LocalizeMe(shopItemComparisonHeader.GetComponent<TextMeshProUGUI>(), TDFonts.WHITE);

        shopExit = new UIManagerScript.UIObject();
        shopExit.gameObj = GameObject.Find("Shop Exit");
        shopExit.button = new ButtonCombo();
        shopExit.button.dbr = DialogButtonResponse.EXIT;
        shopExit.mySubmitFunction = shopExit.CloseShopFromButton;

        shopItemSortType = new UIManagerScript.UIObject();
        shopItemSortType.gameObj = GameObject.Find("Shop Sort Type");
        shopItemSortType.onSubmitValue = (int)InventorySortTypes.ITEMTYPE;
        shopItemSortType.mySubmitFunction = UIManagerScript.singletonUIMS.SortPlayerInventory_UICallback;
        shopItemSortType.myOnSelectAction = ClearItemInfo;


        shopItemSortValue = new UIManagerScript.UIObject();
        shopItemSortValue.gameObj = GameObject.Find("Shop Sort Value");
        shopItemSortValue.onSubmitValue = (int)InventorySortTypes.VALUE;
        shopItemSortValue.mySubmitFunction = UIManagerScript.singletonUIMS.SortPlayerInventory_UICallback;
        shopItemSortValue.myOnSelectAction = ClearItemInfo;


        shopItemButton1 = new UIManagerScript.UIObject();
        shopItemButton1.gameObj = GameObject.Find("Shop Item Button 1");
        shopItemButton1.subObjectImage = GameObject.Find("Shop Item Button 1 Sprite").GetComponent<Image>();
        shopItemButton1.myOnSelectAction = ShowItemInfo;
        shopItemButton1.myOnExitAction = ClearItemInfo;
        shopItemButton1.gameObj.GetComponent<EQItemButtonScript>().myID = 0;
        shopItemButton1.onSelectValue = 0;
        shopItemButton1.onSubmitValue = 0;
        shopItemButton1.mySubmitFunction = InteractShopItem;
        shopItemButton1.myOnSelectAction = ShowItemInfo;

        shopItemSortType.directionalActions[(int)Directions.NORTH] = ShopUIScript.singleton.GoToBottomOfShopList;
        shopItemSortType.neighbors[(int)Directions.SOUTH] = shopItemButton1;
        shopItemSortType.neighbors[(int)Directions.EAST] = shopItemSortValue;
        shopItemSortType.neighbors[(int)Directions.WEST] = shopItemSortValue;

        shopItemSortValue.directionalActions[(int)Directions.NORTH] = ShopUIScript.singleton.GoToBottomOfShopList;
        shopItemSortValue.neighbors[(int)Directions.SOUTH] = shopItemButton1;
        shopItemSortValue.neighbors[(int)Directions.EAST] = shopItemSortType;
        shopItemSortValue.neighbors[(int)Directions.WEST] = shopItemSortType;

        shopItemButton2 = new UIManagerScript.UIObject();
        shopItemButton2.gameObj = GameObject.Find("Shop Item Button 2");
        shopItemButton2.subObjectImage = GameObject.Find("Shop Item Button 2 Sprite").GetComponent<Image>();
        shopItemButton2.myOnSelectAction = ShowItemInfo;
        shopItemButton2.myOnExitAction = ClearItemInfo;
        shopItemButton2.gameObj.GetComponent<EQItemButtonScript>().myID = 1;
        shopItemButton2.onSelectValue = 1;
        shopItemButton2.onSubmitValue = 1;
        shopItemButton2.mySubmitFunction = InteractShopItem;
        shopItemButton2.myOnSelectAction = ShowItemInfo;

        shopItemButton3 = new UIManagerScript.UIObject();
        shopItemButton3.gameObj = GameObject.Find("Shop Item Button 3");
        shopItemButton3.subObjectImage = GameObject.Find("Shop Item Button 3 Sprite").GetComponent<Image>();
        shopItemButton3.myOnSelectAction = ShowItemInfo;
        shopItemButton3.myOnExitAction = ClearItemInfo;
        shopItemButton3.gameObj.GetComponent<EQItemButtonScript>().myID = 2;
        shopItemButton3.onSelectValue = 2;
        shopItemButton3.onSubmitValue = 2;
        shopItemButton3.mySubmitFunction = InteractShopItem;
        shopItemButton3.myOnSelectAction = ShowItemInfo;

        shopItemButton4 = new UIManagerScript.UIObject();
        shopItemButton4.gameObj = GameObject.Find("Shop Item Button 4");
        shopItemButton4.subObjectImage = GameObject.Find("Shop Item Button 4 Sprite").GetComponent<Image>();
        shopItemButton4.myOnSelectAction = ShowItemInfo;
        shopItemButton4.myOnExitAction = ClearItemInfo;
        shopItemButton4.gameObj.GetComponent<EQItemButtonScript>().myID = 3;
        shopItemButton4.onSelectValue = 3;
        shopItemButton4.onSubmitValue = 3;
        shopItemButton4.mySubmitFunction = InteractShopItem;
        shopItemButton4.myOnSelectAction = ShowItemInfo;

        shopItemButton5 = new UIManagerScript.UIObject();
        shopItemButton5.gameObj = GameObject.Find("Shop Item Button 5");
        shopItemButton5.subObjectImage = GameObject.Find("Shop Item Button 5 Sprite").GetComponent<Image>();
        shopItemButton5.myOnSelectAction = ShowItemInfo;
        shopItemButton5.myOnExitAction = ClearItemInfo;
        shopItemButton5.gameObj.GetComponent<EQItemButtonScript>().myID = 4;
        shopItemButton5.onSelectValue = 4;
        shopItemButton5.onSubmitValue = 4;
        shopItemButton5.mySubmitFunction = InteractShopItem;
        shopItemButton5.myOnSelectAction = ShowItemInfo;

        shopItemButton6 = new UIManagerScript.UIObject();
        shopItemButton6.gameObj = GameObject.Find("Shop Item Button 6");
        shopItemButton6.subObjectImage = GameObject.Find("Shop Item Button 6 Sprite").GetComponent<Image>();
        shopItemButton6.myOnSelectAction = ShowItemInfo;
        shopItemButton6.myOnExitAction = ClearItemInfo;
        shopItemButton6.gameObj.GetComponent<EQItemButtonScript>().myID = 5;
        shopItemButton6.onSelectValue = 5;
        shopItemButton6.onSubmitValue = 5;
        shopItemButton6.mySubmitFunction = InteractShopItem;
        shopItemButton6.myOnSelectAction = ShowItemInfo;

        shopItemButton7 = new UIManagerScript.UIObject();
        shopItemButton7.gameObj = GameObject.Find("Shop Item Button 7");
        shopItemButton7.subObjectImage = GameObject.Find("Shop Item Button 7 Sprite").GetComponent<Image>();
        shopItemButton7.myOnSelectAction = ShowItemInfo;
        shopItemButton7.myOnExitAction = ClearItemInfo;
        shopItemButton7.gameObj.GetComponent<EQItemButtonScript>().myID = 6;
        shopItemButton7.onSelectValue = 6;
        shopItemButton7.onSubmitValue = 6;
        shopItemButton7.mySubmitFunction = InteractShopItem;
        shopItemButton7.myOnSelectAction = ShowItemInfo;

        shopItemButton8 = new UIManagerScript.UIObject();
        shopItemButton8.gameObj = GameObject.Find("Shop Item Button 8");
        shopItemButton8.subObjectImage = GameObject.Find("Shop Item Button 8 Sprite").GetComponent<Image>();
        shopItemButton8.myOnSelectAction = ShowItemInfo;
        shopItemButton8.myOnExitAction = ClearItemInfo;
        shopItemButton8.gameObj.GetComponent<EQItemButtonScript>().myID = 7;
        shopItemButton8.onSelectValue = 7;
        shopItemButton8.onSubmitValue = 7;
        shopItemButton8.mySubmitFunction = InteractShopItem;
        shopItemButton8.myOnSelectAction = ShowItemInfo;

        shopItemButton9 = new UIManagerScript.UIObject();
        shopItemButton9.gameObj = GameObject.Find("Shop Item Button 9");
        shopItemButton9.subObjectImage = GameObject.Find("Shop Item Button 9 Sprite").GetComponent<Image>();
        shopItemButton9.myOnSelectAction = ShowItemInfo;
        shopItemButton9.myOnExitAction = ClearItemInfo;
        shopItemButton9.gameObj.GetComponent<EQItemButtonScript>().myID = 8;
        shopItemButton9.onSelectValue = 8;
        shopItemButton9.onSubmitValue = 8;
        shopItemButton9.mySubmitFunction = InteractShopItem;
        shopItemButton9.myOnSelectAction = ShowItemInfo;

        shopItemButton10 = new UIManagerScript.UIObject();
        shopItemButton10.gameObj = GameObject.Find("Shop Item Button 10");
        shopItemButton10.subObjectImage = GameObject.Find("Shop Item Button 10 Sprite").GetComponent<Image>();
        shopItemButton10.myOnSelectAction = ShowItemInfo;
        shopItemButton10.myOnExitAction = ClearItemInfo;
        shopItemButton10.gameObj.GetComponent<EQItemButtonScript>().myID = 9;
        shopItemButton10.onSelectValue = 9;
        shopItemButton10.onSubmitValue = 9;
        shopItemButton10.mySubmitFunction = InteractShopItem;
        shopItemButton10.myOnSelectAction = ShowItemInfo;

        shopItemButton11 = new UIManagerScript.UIObject();
        shopItemButton11.gameObj = GameObject.Find("Shop Item Button 11");
        shopItemButton11.subObjectImage = GameObject.Find("Shop Item Button 11 Sprite").GetComponent<Image>();
        shopItemButton11.myOnSelectAction = ShowItemInfo;
        shopItemButton11.myOnExitAction = ClearItemInfo;
        shopItemButton11.gameObj.GetComponent<EQItemButtonScript>().myID = 10;
        shopItemButton11.onSelectValue = 10;
        shopItemButton11.onSubmitValue = 10;
        shopItemButton11.mySubmitFunction = InteractShopItem;
        shopItemButton11.myOnSelectAction = ShowItemInfo;

        shopItemButton12 = new UIManagerScript.UIObject();
        shopItemButton12.gameObj = GameObject.Find("Shop Item Button 12");
        shopItemButton12.subObjectImage = GameObject.Find("Shop Item Button 12 Sprite").GetComponent<Image>();
        shopItemButton12.myOnSelectAction = ShowItemInfo;
        shopItemButton12.myOnExitAction = ClearItemInfo;
        shopItemButton12.gameObj.GetComponent<EQItemButtonScript>().myID = 11;
        shopItemButton12.onSelectValue = 11;
        shopItemButton12.onSubmitValue = 11;
        shopItemButton12.mySubmitFunction = InteractShopItem;
        shopItemButton12.myOnSelectAction = ShowItemInfo;

        shopItemButton13 = new UIManagerScript.UIObject();
        shopItemButton13.gameObj = GameObject.Find("Shop Item Button 13");
        shopItemButton13.subObjectImage = GameObject.Find("Shop Item Button 13 Sprite").GetComponent<Image>();
        shopItemButton13.myOnSelectAction = ShowItemInfo;
        shopItemButton13.myOnExitAction = ClearItemInfo;
        shopItemButton13.gameObj.GetComponent<EQItemButtonScript>().myID = 12;
        shopItemButton13.onSelectValue = 12;
        shopItemButton13.onSubmitValue = 12;
        shopItemButton13.mySubmitFunction = InteractShopItem;
        shopItemButton13.myOnSelectAction = ShowItemInfo;

        shopItemButton1.directionalValues[(int)Directions.SOUTH] = 1;
        shopItemButton1.directionalValues[(int)Directions.NORTH] = -1;
        shopItemButton1.directionalActions[(int)Directions.SOUTH] = shopItemButton1.TryScrollPool;
        shopItemButton1.directionalActions[(int)Directions.NORTH] = shopItemButton1.TryScrollPool;

        //shopItemButton12.directionalValues[(int)Directions.SOUTH] = 1;
        //shopItemButton12.directionalValues[(int)Directions.NORTH] = -1;
        //shopItemButton12.directionalActions[(int)Directions.SOUTH] = shopItemButton12.TryScrollPool;
        //shopItemButton12.directionalActions[(int)Directions.NORTH] = shopItemButton12.TryScrollPool;

        shopItemButton13.directionalValues[(int)Directions.SOUTH] = 1;
        shopItemButton13.directionalValues[(int)Directions.NORTH] = -1;
        shopItemButton13.directionalActions[(int)Directions.SOUTH] = shopItemButton13.TryScrollPool;
        shopItemButton13.directionalActions[(int)Directions.NORTH] = shopItemButton13.TryScrollPool;

        shopComparisonAreaText = GameObject.Find("ShopComparisonText").GetComponent<TextMeshProUGUI>();
        FontManager.LocalizeMe(shopComparisonAreaText, TDFonts.WHITE);

        shopItemButtonList[0] = shopItemButton1;
        shopItemButtonList[1] = shopItemButton2;
        shopItemButtonList[2] = shopItemButton3;
        shopItemButtonList[3] = shopItemButton4;
        shopItemButtonList[4] = shopItemButton5;
        shopItemButtonList[5] = shopItemButton6;
        shopItemButtonList[6] = shopItemButton7;
        shopItemButtonList[7] = shopItemButton8;
        shopItemButtonList[8] = shopItemButton9;
        shopItemButtonList[9] = shopItemButton10;
        shopItemButtonList[10] = shopItemButton11;
        shopItemButtonList[11] = shopItemButton12;
        shopItemButtonList[12] = shopItemButton13;

        /* shopExit.neighbors[(int)Directions.NORTH] = shopItemButton12;
        shopExit.neighbors[(int)Directions.SOUTH] = shopItemButton1; */

        shopItemButton1.neighbors[(int)Directions.NORTH] = shopItemSortValue; // WAS btn 12
        shopItemButton1.neighbors[(int)Directions.SOUTH] = shopItemButton2;

        shopItemButton2.neighbors[(int)Directions.NORTH] = shopItemButton1;
        shopItemButton2.neighbors[(int)Directions.SOUTH] = shopItemButton3;

        shopItemButton3.neighbors[(int)Directions.NORTH] = shopItemButton2;
        shopItemButton3.neighbors[(int)Directions.SOUTH] = shopItemButton4;

        shopItemButton4.neighbors[(int)Directions.NORTH] = shopItemButton3;
        shopItemButton4.neighbors[(int)Directions.SOUTH] = shopItemButton5;

        shopItemButton5.neighbors[(int)Directions.NORTH] = shopItemButton4;
        shopItemButton5.neighbors[(int)Directions.SOUTH] = shopItemButton6;

        shopItemButton6.neighbors[(int)Directions.NORTH] = shopItemButton5;
        shopItemButton6.neighbors[(int)Directions.SOUTH] = shopItemButton7;

        shopItemButton7.neighbors[(int)Directions.NORTH] = shopItemButton6;
        shopItemButton7.neighbors[(int)Directions.SOUTH] = shopItemButton8;

        shopItemButton8.neighbors[(int)Directions.NORTH] = shopItemButton7;
        shopItemButton8.neighbors[(int)Directions.SOUTH] = shopItemButton9;

        shopItemButton9.neighbors[(int)Directions.NORTH] = shopItemButton8;
        shopItemButton9.neighbors[(int)Directions.SOUTH] = shopItemButton10;

        shopItemButton10.neighbors[(int)Directions.NORTH] = shopItemButton9;
        shopItemButton10.neighbors[(int)Directions.SOUTH] = shopItemButton11;

        shopItemButton11.neighbors[(int)Directions.NORTH] = shopItemButton10;
        shopItemButton11.neighbors[(int)Directions.SOUTH] = shopItemButton12;

        shopItemButton12.neighbors[(int)Directions.NORTH] = shopItemButton11;
        shopItemButton12.neighbors[(int)Directions.SOUTH] = shopItemButton13;

        shopItemButton13.neighbors[(int)Directions.NORTH] = shopItemButton12;
        shopItemButton13.neighbors[(int)Directions.SOUTH] = shopItemButton1;

        shopItemInfoText = GameObject.Find("ShopItemText").GetComponent<TextMeshProUGUI>();
        shopItemInfoName = GameObject.Find("ShopItemName").GetComponent<TextMeshProUGUI>();
        shopItemInfoImage = GameObject.Find("ShopItemImage").GetComponent<Image>();

        shopCompItemInfoText = GameObject.Find("ShopCompItemText").GetComponent<TextMeshProUGUI>();
        shopCompItemInfoName = GameObject.Find("ShopCompItemName").GetComponent<TextMeshProUGUI>();
        shopCompItemInfoImage = GameObject.Find("ShopCompItemImage").GetComponent<Image>();

        FontManager.LocalizeMe(shopItemInfoText, TDFonts.WHITE);
        FontManager.LocalizeMe(shopItemInfoName, TDFonts.WHITE);
        FontManager.LocalizeMe(shopCompItemInfoText, TDFonts.WHITE);
        FontManager.LocalizeMe(shopCompItemInfoName, TDFonts.WHITE);

        initialized = true;
    }

    // Use this for initialization
    void Start()
    {
    }

    public void ShowItemInfo(int index)
    {
        if (UIManagerScript.dialogBoxOpen) return;
        UIManagerScript.singletonUIMS.ShowItemInfoShop(index, playerItemList);
    }

    public void ClearItemInfo(int index)
    {
        if (UIManagerScript.dialogBoxOpen) return;
        UIManagerScript.singletonUIMS.ClearItemInfo(index);
    }

    public static void UpdateShop()
    {
        if (!UIManagerScript.GetWindowState(UITabs.SHOP))
        {
            return;
        }        

        HeroPC hero = GameMasterScript.heroPCActor;
        NPC merchant = UIManagerScript.currentConversation.whichNPC;

        if (merchant == null)
        {
            if (!DesperatelyTryToFindWhoWeAreTalkingTo())
            {
                // welp
                CloseShopInterface();
                return;
            }
        }

        bool banker = false;
        if (merchant.actorRefName == "npc_banker")
        {
            banker = true;
        }

        if (!merchant.actorRefName.Contains("npc_casinoshop"))
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
            shopMoney.text = StringManager.GetString("ui_shop_money_normal");
        }
        else
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.myInventory.GetItemQuantity("item_casinochip").ToString());
            shopMoney.text = StringManager.GetString("ui_shop_money_casino");
        }


        // Player item list is being used here for either buying OR selling. Could be the NPC's inventory.

        List<Item> inventoryToUse = null;
        //Debug.Log("Shopstate? " + shopState + " merchant id? " + merchant.actorUniqueID);
        if (shopState == ShopState.BUY)
        {
            inventoryToUse = merchant.myInventory.GetInventory();
            if (UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_banker" || UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_foodcart")
            {
                shopHeader.text = StringManager.GetString("ui_shop_withdraw");
                int usedSlots = UIManagerScript.currentConversation.whichNPC.myInventory.GetInventory().Count;
                int maxSlots = SharedBank.CalculateMaxBankableItems();
                shopHeader.text += " " + usedSlots + "/" + maxSlots;
            }
            else
            {
                shopHeader.text = UIManagerScript.currentConversation.whichNPC.displayName;
                merchant.myInventory.RemoveInvalidItems();             
            }

        }
        else
        {
            inventoryToUse = hero.myInventory.GetInventory();            
            if (UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_banker")
            {
                shopHeader.text = StringManager.GetString("ui_shop_deposit");
                int usedSlots = UIManagerScript.currentConversation.whichNPC.myInventory.GetInventory().Count;
                int maxSlots = SharedBank.CalculateMaxBankableItems();
                shopHeader.text += " " + usedSlots + "/" + maxSlots;
            }
            else
            {
                shopHeader.text = StringManager.GetString("ui_shop_sell");
            }

        }

        bool foodCart = UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_foodcart";



        playerItemList.Clear();
        foreach (Item itm in inventoryToUse)
        {
            bool skip = false;

            if (itm.itemType == ItemTypes.EMBLEM || itm.ReadActorData("permabound") == 1) continue;

            if (itm.itemType == ItemTypes.WEAPON && shopState == ShopState.SELL)
            {
                for (int w = 0; w < UIManagerScript.hotbarWeapons.Length; w++)
                {
                    if (UIManagerScript.hotbarWeapons[w] == itm)
                    {
                        skip = true;
                        break;
                    }
                }                
            }

            if (foodCart && !itm.IsItemFood()) skip = true;

            if (itm.dreamItem) skip = true;

            if (!TDSearchbar.CheckIfItemMatchesTerms(itm)) continue;
            
            //Debug.Log("Adding " + itm.actorRefName + " " + itm.actorUniqueID + " " + itm.GetQuantity() + " to list? " + skip + " " + foodCart + " " + UIManagerScript.currentConversation.whichNPC.actorRefName);
            if (!skip) playerItemList.Add(itm);
        }

        string invSpriteRef;

        bool casino = false;
        int casinoTokens = 0;
        if (UIManagerScript.currentConversation.whichNPC.actorRefName.Contains("npc_casinoshop"))
        {
            casino = true;
            casinoTokens = hero.myInventory.GetItemQuantity("item_casinochip");
        }

        for (int i = 0; i < shopItemButtonList.Length; i++)
        {
            if (i >= playerItemList.Count)
            {
                TextMeshProUGUI txt = shopItemButtonList[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = "";
                shopItemButtonList[i].gameObj.SetActive(false);
                shopItemButtonList[i].enabled = false;
            }
            else
            {
                TextMeshProUGUI txt = shopItemButtonList[i].gameObj.GetComponentInChildren<TextMeshProUGUI>();
                int offset = i;

                offset += Math.Abs(UIManagerScript.listArrayIndexOffset);

                if (offset >= playerItemList.Count)
                {
                    shopItemButtonList[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    shopItemButtonList[i].gameObj.SetActive(false);
                    shopItemButtonList[i].enabled = false;
                    break;
                }

                bool difftext = false;

                if (playerItemList[offset].itemType == ItemTypes.WEAPON && shopState == ShopState.SELL)
                {
                    for (int w = 0; w < UIManagerScript.hotbarWeapons.Length; w++)
                    {
                        if (UIManagerScript.hotbarWeapons[w] == playerItemList[offset])
                        {
                            txt.text = "* " + playerItemList[offset].displayName;
                            difftext = true;
                        }
                    }
                }

                int displayPrice = 0;
                if (casino)
                {
                    displayPrice = playerItemList[offset].GetIndividualCasinoPrice();
                }
                else
                {
                    displayPrice = playerItemList[offset].GetIndividualShopPrice();
                }
                int playerMoney = hero.GetMoney();
                if (casino)
                {
                    playerMoney = casinoTokens;
                }

                if (shopState == ShopState.BUY && displayPrice > playerMoney && !banker)
                {
                    txt.text = "<color=red>" + Regex.Replace(playerItemList[offset].displayName, "<.*?>", string.Empty) + "</color>";
                    difftext = true;
                }
                else if (banker && shopState == ShopState.SELL && playerItemList[offset].GetBankPrice() > hero.GetMoney())
                {
                    txt.text = "<color=red>" + Regex.Replace(playerItemList[offset].displayName, "<.*?>", string.Empty) + "</color>";
                    difftext = true;
                }

                if (!difftext)
                {
                    txt.text = playerItemList[offset].displayName;
                }

                txt.text = CustomAlgorithms.CheckForFavoriteOrTrashAndInsertMark(txt.text, playerItemList[offset]);

                if (playerItemList[offset].itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable c = playerItemList[offset] as Consumable;
                    if (c.Quantity > 1)
                    {
                        txt.text += " (" + c.Quantity + ")";
                    }
                }

                if (shopState == ShopState.SELL && foodCart)
                {
                    if (FoodCartScript.HasDemand(playerItemList[offset].actorRefName))
                    {
                        txt.text = UIManagerScript.greenHexColor + txt.text + "</color>";
                    }
                }

                shopItemButtonList[i].gameObj.SetActive(true);
                invSpriteRef = "";
                if ((playerItemList[offset].spriteRef == null) || (playerItemList[offset].spriteRef == ""))
                {
                    invSpriteRef = "assorteditems_140"; // TODO: Better placeholders.
                    shopItemButtonList[i].subObjectImage.sprite = null;
                    shopItemButtonList[i].subObjectImage.color = UIManagerScript.transparentColor;
                }
                else
                {
                    //invSpriteRef = playerItemList[offset].GetSpriteForUI();
                    shopItemButtonList[i].subObjectImage.sprite = playerItemList[offset].GetSpriteForUI();
                    shopItemButtonList[i].subObjectImage.color = Color.white;
                }

                shopItemButtonList[i].enabled = true;
            }
        }

        shopItemComparisonHeader.GetComponent<TextMeshProUGUI>().text = "";
        UIManagerScript.UpdateScrollbarPosition();
    }

    public static void CloseShopInterface()
    {
        if (shopInterface.activeSelf)
        {
            GuideMode.OnFullScreenUIClosed();
        }
        UIManagerScript.singletonUIMS.DisableCursor();
        UIManagerScript.CleanupAfterUIClose(UITabs.SHOP);
        TutorialManagerScript.OnUIClosed();        
    }

    public void CloseShop()
    {
        if (UIManagerScript.dialogBoxOpen)
        {
            return;
        }

        UIManagerScript.singletonUIMS.DisableCursor();
        UIManagerScript.CleanupAfterUIClose(UITabs.SHOP);
        TutorialManagerScript.OnUIClosed();
        GuideMode.OnFullScreenUIClosed();

        Debug.Log("Close shop ui 2");
    }

    public static void OpenShopInterface(NPC merchant)
    {
        TDInputHandler.OnDialogOrFullScreenUIOpened();

        GuideMode.OnFullScreenUIOpened();

        //Debug.Log("Opening shop with " + merchant.actorRefName + " " + merchant.actorUniqueID + " " + merchant.GetPos());
        GameMasterScript.gmsSingleton.SetTempGameData("last_shop_id", merchant.actorUniqueID);

        TDSearchbar.ClearSearchTerms();

        merchant.SetNewStuff(false);
        if (!GameMasterScript.heroPCActor.shopkeepersThatRefresh.Contains(merchant.actorUniqueID))
        {
            GameMasterScript.heroPCActor.shopkeepersThatRefresh.Add(merchant.actorUniqueID);
        }

        UIManagerScript.singletonUIMS.CloseAllDialogsExcept(UITabs.SHOP);
        UIManagerScript.SetListOffset(0);
        //listArrayIndexPosition = 0;
        //shopInterfaceOpen = true;

        UIManagerScript.SetWindowState(UITabs.SHOP, true);

        shopInterface.SetActive(true);
        UIManagerScript.singletonUIMS.ClearItemInfo(0, true);
        UIManagerScript.singletonUIMS.EnableCursor();
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(shopInterface.transform);
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.ChangeUIFocus(shopExit);

        bool merchantIsCraftingBox = merchant.actorRefName == "npc_crafter";
        CraftingScreen.SetCraftingUIState(merchantIsCraftingBox);

        UpdateShop();

        if (UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_banker" && shopState == ShopState.SELL)
        {
            // Actually don't check this here, because what if we are depositing a stackable item...?
            /* if (UIManagerScript.currentConversation.whichNPC.myInventory.GetInventory().Count == MetaProgressScript.CalculateMaxBankableItems())
            {
                singleton.CloseShop();
                StringManager.SetTag(0, MetaProgressScript.CalculateMaxBankableItems().ToString());
                GameLogScript.LogWriteStringRef("log_error_bank_maxitems");
                return;
            } */
        }


        if (playerItemList.Count > 0)
        {
            try
            {
                UIManagerScript.singletonUIMS.ShowItemInfo(0);
            }
            catch (Exception e)
            {
                Debug.Log("Shop open error " + e);
            }
        }


        if (playerItemList.Count > 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(shopItemButton1);
            UIManagerScript.singletonUIMS.ShowItemInfo(0);
            singleton.ShowItemInfo(0);
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(shopExit);
        }

        SetShopObjectsForUI();

        //AlignCursorPos(singletonUIMS.uiDialogMenuCursor, uiObjectFocus.gameObj, -5f, -4f, false); // Should this be done in change focus?

        UIManagerScript.CloseDialogBox();
        UIManagerScript.singletonUIMS.EnableCursor();
    }

    static void SetShopObjectsForUI()
    {
        UIManagerScript.allUIObjects.Clear();
        for (int i = 0; i < shopItemButtonList.Length; i++)
        {
            UIManagerScript.allUIObjects.Add(shopItemButtonList[i]);
        }
        UIManagerScript.allUIObjects.Add(shopExit);
    }

    public void InteractShopItem(int index)
    {
        int indexOfButton = UIManagerScript.singletonUIMS.GetIndexOfSelectedButton();

        int checkIndex = indexOfButton + UIManagerScript.listArrayIndexOffset;

        if (checkIndex < 0 || checkIndex >= playerItemList.Count)
        {
            Debug.Log("Tried to access a shop button out of its normal array position.");
            return;
        }

        Item selectedItem = playerItemList[checkIndex]; // was index pos
        HeroPC hero = GameMasterScript.heroPCActor;

        if (shopState == ShopState.BUY)
        {
            int localPrice = 0;
            bool singleItem = true;

            bool casino = false;

            bool isFoodCart = UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_foodcart";
            bool isBanker = UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_banker";

            if (isBanker || isFoodCart)
            {
                localPrice = 0;
                singleItem = false;
            }
            else if (UIManagerScript.currentConversation.whichNPC.actorRefName.Contains("npc_casinoshop"))
            {
                localPrice = selectedItem.GetIndividualCasinoPrice();
                casino = true;
            }
            else
            {
                // 10/28: Only buy single items, not stacks, from merchants.
                localPrice = selectedItem.GetIndividualShopPrice();
            }

            if (isBanker)
            {
                if (selectedItem.GetQuantity() > 1)
                {
                    //UIManagerScript.singletonUIMS.CloseAllDialogs();
                    GameMasterScript.gmsSingleton.SetTempStringData("adjustquantity", "withdraw");
                    GameMasterScript.gmsSingleton.SetTempGameData("dropitem", selectedItem.actorUniqueID);
                    GameMasterScript.gmsSingleton.SetTempGameData("scrollpost", UIManagerScript.listArrayIndexOffset);
                    GameMasterScript.gmsSingleton.SetTempGameData("idxselected", UIManagerScript.singletonUIMS.GetIndexOfSelectedButton());
                    StringManager.SetTag(0, selectedItem.displayName);
                    StringManager.SetTag(1, StringManager.GetString("misc_withdraw"));
                    UIManagerScript.StartConversationByRef("adjust_quantity", DialogType.STANDARD, null);
                    GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "notgold");
                    UIManagerScript.EnableDialogSlider("", 1, selectedItem.GetQuantity(), false);



                    return;
                }
            }

            bool canPurchase = false;
            if (!casino)
            {
                if (hero.GetMoney() >= localPrice)
                {
                    canPurchase = true;
                }
            }
            else
            {
                int numTokens = hero.myInventory.GetItemQuantity("item_casinochip");
                if (numTokens >= localPrice)
                {
                    canPurchase = true;
                }
            }

            if (RandomJobMode.IsCurrentGameInRandomJobMode() && canPurchase && selectedItem.actorRefName == "scroll_jobchange") canPurchase = false;

            if (canPurchase)
            {
                if (!casino)
                {
                    hero.ChangeMoney(-1 * localPrice);
                }
                else
                {
                    hero.myInventory.ChangeItemQuantityByRef("item_casinochip", -1 * localPrice);
                }

                if (localPrice != 0)
                {
                    string qtyText = " ";
                    if (selectedItem.itemType == ItemTypes.CONSUMABLE && !singleItem)
                    {
                        Consumable c = selectedItem as Consumable;
                        qtyText = " (" + c.Quantity + ") ";
                    }
                    StringManager.SetTag(0, selectedItem.displayName);
                    StringManager.SetTag(1, qtyText);
                    StringManager.SetTag(2, localPrice.ToString());
                    if (casino)
                    {
                        GameLogScript.LogWriteStringRef("log_shop_purchase_casino");
                    }
                    else
                    {
                        GameLogScript.LogWriteStringRef("log_shop_purchase_normal");
                    }
                    GameMasterScript.gmsSingleton.statsAndAchievements.AddMerchantGoldSpent(localPrice);
                }
                else
                {
                    StringManager.SetTag(0, selectedItem.displayName + selectedItem.GetQuantityText());
                    if (isBanker)
                    {
                        GameLogScript.LogWriteStringRef("log_player_withdrawitem");
                    }
                    else
                    {
                        StringManager.SetTag(0, selectedItem.displayName);
                        StringManager.SetTag(1, selectedItem.GetQuantityText());
                        StringManager.SetTag(2, "0");
                        GameLogScript.LogWriteStringRef("log_shop_purchase_normal");                        
                    }
                        
                }

                bool moveItemToHero = true;

                if (selectedItem.itemType == ItemTypes.CONSUMABLE && singleItem)
                {
                    Consumable c = selectedItem as Consumable;
                    if (c.Quantity > 1)
                    {
                        moveItemToHero = false;
                        c.ChangeQuantity(-1);

                        Consumable copyOfConsumable = new Consumable();                        
                        copyOfConsumable.CopyFromItem(c);
                        copyOfConsumable.Quantity = 1;
                        copyOfConsumable.SetUniqueIDAndAddToDict(); // EXTREEEEMLY IMPORTANT so we dont have two items with identical ID!
                        GameMasterScript.heroPCActor.myInventory.AddItem(copyOfConsumable, true);
                    }
                }

                if (moveItemToHero)
                {
                    hero.myInventory.AddItemRemoveFromPrevCollection(selectedItem, true, 9999, isBanker);
                    
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log("Banker withdrawal transaction occurred. Banker count: " + SharedBank.allItemsInBank.Count);
                    }

                    hero.OnItemPickedUpOrPurchased(selectedItem, purchased: true);

                    if (selectedItem.IsEquipment())
                    {
                        Equipment e = selectedItem as Equipment;
                        if (e.customItemFromGenerator)
                        {
                            SharedBank.MarkRelicTemplateAsInUseOnCurrentSlot(e.actorRefName);
                        }
                    }

                    if (selectedItem.customItemFromGenerator && MapMasterScript.activeMap.IsMysteryDungeonMap())
                    {                        
                        CloseShopInterface();
                        LootGeneratorScript.OnLegendaryItemFound(selectedItem);
                    }

                }

                UIManagerScript.PlayCursorSound("Buy Item");

                if (UIManagerScript.listArrayIndexOffset > playerItemList.Count - shopItemButtonList.Length)
                {
                    UIManagerScript.SetListOffset(playerItemList.Count - shopItemButtonList.Length);
                }
                if (UIManagerScript.listArrayIndexOffset < 0)
                {
                    UIManagerScript.SetListOffset(0);
                }

                UpdateShop();

                if (playerItemList.Count > 0)
                {
                    int cursorPos = UIManagerScript.singletonUIMS.GetIndexOfSelectedButton();

                    //Debug.Log("Current cursor pos is: " + cursorPos + " or with offset is " + (cursorPos + UIManagerScript.listArrayIndexOffset) + " vs max of " + playerItemList.Count);

                    if (cursorPos + UIManagerScript.listArrayIndexOffset >= playerItemList.Count)
                    {
                        UIManagerScript.MoveCursor(Directions.NORTH);
                    }


                    UIManagerScript.singletonUIMS.HoverItemInfoConditional(UIManagerScript.singletonUIMS.GetIndexOfSelectedButton(), true, false);

                }
                else
                {
                    UpdateShop();
                }

                if (localPrice != 0)
                {
                    if (playerItemList.Count == 0)
                    {
                        if (Debug.isDebugBuild) Debug.Log("Bought out the merchant!");
                        CloseShopInterface();
                        //GameMasterScript.gmsSingleton.statsAndAchievements.SetLowestMerchantItems(playerItemList.Count);
                    }
                }

            }
            else
            {
                UIManagerScript.PlayCursorSound("Error");
            }
        }

        bool itemManaged = false;

        if (shopState == ShopState.SELL)
        {
            float salePrice = selectedItem.GetSalePrice(UIManagerScript.currentConversation.whichNPC.GetShop().GetShop().saleMult);

            if (UIManagerScript.currentConversation.whichNPC.actorRefName == "npc_banker")
            {
                if (selectedItem.GetBankPrice() > GameMasterScript.heroPCActor.GetMoney())
                {
                    UIManagerScript.PlayCursorSound("Error");
                    return;
                }

                // Uh oh, the bank is full! But maybe it's ok if the banker already has at least one stack of 
                // what we're trying to deposit.
                
                int invCount = UIManagerScript.currentConversation.whichNPC.myInventory.GetInventory().Count;
 
                int maxItems = SharedBank.CalculateMaxBankableItems();

                bool okToDeposit = true;
                if (invCount == maxItems)
                {
                    okToDeposit = false;
                    if (selectedItem.itemType == ItemTypes.CONSUMABLE)
                    {
                        Consumable c = selectedItem as Consumable;
                        if (UIManagerScript.currentConversation.whichNPC.myInventory.HasItemByRef(c.actorRefName))
                        {
                            if (UIManagerScript.currentConversation.whichNPC.myInventory.CanStackItem(c))
                            {
                                // Even though we are at max, we can still deposit the item, because it goes into a stack
                                okToDeposit = true;
                            }                            
                        }
                    }
                }
                else if (invCount >= maxItems)
                {
                    okToDeposit = false;
                }

                if (!okToDeposit)
                {
                    singleton.CloseShop(); // Is this annoying? maybe
                    UIManagerScript.PlayCursorSound("Error");
                    StringManager.SetTag(0, "<color=yellow>" + SharedBank.CalculateMaxBankableItems().ToString() + "</color>");
                    GameLogScript.GameLogWrite(StringManager.GetString("bank_max_items"), GameMasterScript.heroPCActor);
                    return;
                }
                
                if (selectedItem.GetQuantity() > 1)
                {
                    //UIManagerScript.singletonUIMS.CloseAllDialogs();
                    GameMasterScript.gmsSingleton.SetTempStringData("adjustquantity", "deposit");
                    GameMasterScript.gmsSingleton.SetTempGameData("dropitem", selectedItem.actorUniqueID);
                    GameMasterScript.gmsSingleton.SetTempGameData("scrollpost", UIManagerScript.listArrayIndexOffset);
                    GameMasterScript.gmsSingleton.SetTempGameData("idxselected", UIManagerScript.singletonUIMS.GetIndexOfSelectedButton());
                    StringManager.SetTag(0, selectedItem.displayName);
                    StringManager.SetTag(1, StringManager.GetString("misc_deposit"));
                    UIManagerScript.StartConversationByRef("adjust_quantity", DialogType.STANDARD, null);                    
                    GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "notgold");

                    int maxItemsToDeposit = GameMasterScript.heroPCActor.GetMoney() / selectedItem.GetBankPrice();
                    if (maxItemsToDeposit > selectedItem.GetQuantity())
                    {
                        maxItemsToDeposit = selectedItem.GetQuantity();
                    }
                    if (maxItemsToDeposit * selectedItem.GetBankPrice() > GameMasterScript.heroPCActor.GetMoney())
                    {
                        maxItemsToDeposit--;
                    }

                    UIManagerScript.EnableDialogSlider("", 1, maxItemsToDeposit, false);
                    return;
                }

                DepositItem(selectedItem, 1);
                itemManaged = true;
            }
            else
            {
                /* if (selectedItem.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable con = selectedItem as Consumable;
                    salePrice *= con.quantity;
                } */

                if (selectedItem.GetQuantity() > 1)
                {
                    //UIManagerScript.singletonUIMS.CloseAllDialogs();
                    GameMasterScript.gmsSingleton.SetTempStringData("adjustquantity", "sellitem");
                    GameMasterScript.gmsSingleton.SetTempGameData("merchantid", UIManagerScript.currentConversation.whichNPC.actorUniqueID);
                    GameMasterScript.gmsSingleton.SetTempGameData("dropitem", selectedItem.actorUniqueID);
                    GameMasterScript.gmsSingleton.SetTempGameData("scrollpost", UIManagerScript.listArrayIndexOffset);
                    GameMasterScript.gmsSingleton.SetTempGameData("idxselected", UIManagerScript.singletonUIMS.GetIndexOfSelectedButton());
                    StringManager.SetTag(0, selectedItem.displayName);
                    StringManager.SetTag(1, StringManager.GetString("misc_sellitem"));
                    UIManagerScript.StartConversationByRef("adjust_quantity", DialogType.STANDARD, null);
                    UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.dialogUIObjects[0]);
                    GameMasterScript.gmsSingleton.SetTempStringData("dialogslider", "notgold");

                    UIManagerScript.EnableDialogSlider("", 1, selectedItem.GetQuantity(), false);
                    return;
                }

                if (selectedItem.favorite)
                {
                    UIManagerScript.CloseDialogBox();
                    GameMasterScript.gmsSingleton.SetTempGameData("merchantid", UIManagerScript.currentConversation.whichNPC.actorUniqueID);
                    GameMasterScript.gmsSingleton.SetTempGameData("sellitem", selectedItem.actorUniqueID);
                    StringManager.SetTag(0, selectedItem.displayName);
                    UIManagerScript.StartConversationByRef("confirm_sell_favoriteitem", DialogType.STANDARD, null);                    
                    UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.dialogUIObjects[0]);
                    return;
                }

                hero.ChangeMoney((int)salePrice, doNotAlterFromGameMods:true);
                string qtyText = "";
                if (selectedItem.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable c = selectedItem as Consumable;
                    qtyText = " (" + c.Quantity + ")";
                }
                StringManager.SetTag(0,selectedItem.displayName + qtyText);
                StringManager.SetTag(1, ((int) salePrice).ToString());
                GameLogScript.GameLogWrite(StringManager.GetString("sold_item"), GameMasterScript.heroPCActor);
            }

            if (!itemManaged)
            {
                UIManagerScript.currentConversation.whichNPC.myInventory.AddItemRemoveFromPrevCollection(selectedItem, false);
            }

            if (selectedItem.itemType == ItemTypes.WEAPON)
            {
                UIManagerScript.RemoveWeaponFromActives(selectedItem as Weapon);
            }
            else if (selectedItem.itemType == ItemTypes.CONSUMABLE)
            {
                UIManagerScript.RemoveItemFromHotbar(selectedItem as Consumable);
            }
            //listArrayIndexPosition--;            
            UIManagerScript.PlayCursorSound("Buy Item");
            /* if (playerItemList.Count == 0)
            {
                ChangeUIFocus(shopExit);
            }
            else
            {
                ChangeUIFocusAndAlignCursor(shopItemButtonList[listArrayIndexPosition-listArrayIndexOffset-1]);
            } */
            //Debug.Log("Prior to sell, position was: " + listArrayIndexOffset);

            /* if ((uiObjectFocus != shopItemButton1) && (listArrayIndexOffset == 0))
            {
                MoveCursor(Directions.NORTH);
            }            
            else
            {
                Debug.Log("Some offset.");
                UpdateListIndex(Directions.NEUTRAL);
                HoverItemInfo(0);
            } */

            // New index position update logic.
            /*if (listArrayIndexPosition >= playerItemList.Count)
            {
                listArrayIndexPosition = playerItemList.Count - 1;
                if (listArrayIndexPosition < 0)
                {
                    listArrayIndexPosition = 0;
                }
            }*/

            // Shop Item Button list length is 12
            /*if (listArrayIndexPosition >= shopItemButtonList.Length) // is 12 greater than 11?
            {
                listArrayIndexOffset = listArrayIndexPosition - shopItemButtonList.Length + 1;
            }
            else
            {
                listArrayIndexOffset = 0;
            } */

            if (UIManagerScript.listArrayIndexOffset > playerItemList.Count - shopItemButtonList.Length)
            {
                UIManagerScript.SetListOffset(playerItemList.Count - shopItemButtonList.Length);
            }
            if (UIManagerScript.listArrayIndexOffset < 0)
            {
                UIManagerScript.SetListOffset(0);
            }

            //Debug.Log("After sell, before shop update position is: " + listArrayIndexPosition + " offset " + listArrayIndexOffset);

            int btnIndexForItemDisplay = 0;

            if (playerItemList.Count == 0)
            {
                UIManagerScript.ChangeUIFocusAndAlignCursor(shopExit);
            }
            else
            {
                // Do nothing?
                if (UIManagerScript.singletonUIMS.GetIndexOfSelectedButton() + UIManagerScript.listArrayIndexOffset >= playerItemList.Count)
                {
                    UIManagerScript.MoveCursor(Directions.NORTH);
                }

            }

            UpdateShop();

            //Debug.Log(playerItemList.Count + " total items, offset is " + listArrayIndexOffset);


            UIManagerScript.singletonUIMS.HoverItemInfoConditional(UIManagerScript.singletonUIMS.GetIndexOfSelectedButton(), true, false);

        }
    }

    public static bool CheckShopInterfaceState()
    {
        return UIManagerScript.GetWindowState(UITabs.SHOP);
    }

    public void GoToBottomOfShopList(int amount)
    {
        UIManagerScript.SetListOffset(playerItemList.Count - ShopUIScript.shopItemButtonList.Length); // Loop to the bottom of the list
        if (UIManagerScript.listArrayIndexOffset < 0)
        {
            UIManagerScript.SetListOffset(0);
        }
        ShopUIScript.UpdateShop();
        int index = UIManagerScript.singletonUIMS.GetIndexOfSelectedButton();
        UIManagerScript.singletonUIMS.ShowItemInfo(index); // New code to try to make keyboard display items deep in the shop.
        if (UIManagerScript.listArrayIndexOffset == 0)
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(ShopUIScript.shopItemButtonList[playerItemList.Count - 1]);
        }
        else
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(ShopUIScript.shopItemButtonList[shopItemButtonList.Length - 1]);
        }
        UIManagerScript.uiObjectFocus.myOnSelectAction.Invoke(UIManagerScript.uiObjectFocus.onSelectValue);
    }

    public static void ClearComparison()
    {
        shopItemInfoText.text = "";
        shopItemInfoName.text = "";
        shopItemInfoImage.sprite = null;
        shopItemInfoImage.color = UIManagerScript.transparentColor;

        shopCompItemInfoText.text = "";
        shopCompItemInfoName.text = "";
        shopCompItemInfoImage.sprite = null;
        shopCompItemInfoImage.color = UIManagerScript.transparentColor;

        shopComparisonAreaText.text = "";
    }

    public static void SellItem(Item selectedItem, int quantity)
    {
        int price = (int)selectedItem.GetIndividualSalePrice(); // do we need to check sale mult here?

        price *= quantity;

        NPC n = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("merchantid")) as NPC;

        if (n.GetShop() != null && n.GetShop().GetShop() != null)
        {
            price = (int)(price * n.GetShop().GetShop().saleMult);
        }

        bool foodCart = false;
        if (n != null && n.actorRefName == "npc_foodcart")
        {
            foodCart = true;
            price = 0;
        }

        GameMasterScript.heroPCActor.ChangeMoney(price, doNotAlterFromGameMods:true);

        string qtyText = "";
        if (quantity > 1)
        {
            qtyText = " (" + quantity + ")";
        }
        StringManager.SetTag(0, selectedItem.displayName + qtyText);
        StringManager.SetTag(1, "<color=yellow>" + price + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>");

        if (foodCart)
        {
            GameLogScript.LogWriteStringRef("log_player_stockfoodcart");
        }
        else
        {
            GameLogScript.LogWriteStringRef("log_player_sellitem");
        }

        GameMasterScript.heroPCActor.OnItemSoldOrDropped(selectedItem, true);

        n.myInventory.AddItemRemoveFromPrevCollection(selectedItem, true);
        if (!foodCart && n.GetShop() != null && n.GetShop().GetShop() != null)
        {
            selectedItem.CalculateShopPrice(n.GetShop().GetShop().valueMult, true);
        }

    }

    public static void DepositItem(Item selectedItem, int quantity)
    {
        int price = (int)selectedItem.GetBankPrice(quantity);
        GameMasterScript.heroPCActor.ChangeMoney(-1 * price);
        string qtyText = "";
        if (quantity > 1)
        {
            qtyText = " (" + quantity + ") ";
        }
        //Debug.Log(price + " " + quantity);
        StringManager.SetTag(0, selectedItem.displayName + qtyText);
        StringManager.SetTag(1, "<color=yellow>" + price + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + "</color>");
        GameLogScript.LogWriteStringRef("log_player_deposititem");

        NPC n = MapMasterScript.activeMap.FindActor("npc_banker") as NPC;
        n.myInventory.AddItemRemoveFromPrevCollection(selectedItem, true);

        if (selectedItem.IsEquipment())
        {
            Equipment e = selectedItem as Equipment;
            if (e.customItemFromGenerator)
            {
                SharedBank.MarkRelicTemplateAsReturnedToSharedBank(e.actorRefName);
            }
        }

    }

    public static void ReopenShop()
    {
        GameMasterScript.gmsSingleton.SetTempGameData("dropitem", -1);
        SetShopObjectsForUI();
        Actor actorOfLastShop = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("last_shop_id"));
        //Debug.Log(actorOfLastShop.actorUniqueID + " last shop?");
        if (actorOfLastShop != null)
        {
            NPC n = actorOfLastShop as NPC;
            UIManagerScript.currentConversation = new Conversation();
            UIManagerScript.currentConversation.whichNPC = n;
            if (ShopUIScript.CheckShopInterfaceState())
            {
                UIManagerScript.listArrayIndexOffset = GameMasterScript.gmsSingleton.ReadTempGameData("scrollpost");
                int selectedIndex = GameMasterScript.gmsSingleton.ReadTempGameData("idxselected");
                if (selectedIndex >= ShopUIScript.playerItemList.Count)
                {
                    selectedIndex--;
                }

                ShopUIScript.UpdateShop();

                if (selectedIndex >= 0)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(ShopUIScript.shopItemButtonList[selectedIndex]);
                }
                else
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(ShopUIScript.shopExit);

                }

                if (selectedIndex >= 0)
                {
                    ShopUIScript.singleton.ShowItemInfo(selectedIndex);
                }

            }
            else
            {
                ShopUIScript.OpenShopInterface(n);
            }

            //ShopUIScript.UpdateShop();
        }
    }

    /// <summary>
    /// whichNPC is null in our convo? Search around the player's immediate vicinity for an NPC. Returns TRUE if we found something.
    /// </summary>
    static bool DesperatelyTryToFindWhoWeAreTalkingTo()
    {
        CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 1, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            MapTileData mtd = CustomAlgorithms.tileBuffer[i];
            if (mtd.tileType != TileTypes.GROUND) continue;
            NPC tryn = mtd.GetInteractableNPC();
            if (!string.IsNullOrEmpty(tryn.dialogRef))
            {
                UIManagerScript.currentConversation.whichNPC = tryn;
                return true;
            }
        }

        return false;
    }
}
