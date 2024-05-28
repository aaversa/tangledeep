using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public class ShopScript
{

    public string refName;
    public List<ShopData> shopList;

    public ShopScript()
    {
        shopList = new List<ShopData>();
    }

    public bool ValidateAllShopDataOnLoad()
    {        
        for (int i = 0; i < shopList.Count; i++)
        {
            ShopData sd = shopList[i];
            if (sd.minLevel < 0 || sd.minLevel > 15)
            {
                Debug.LogError("Min level " + sd.minLevel + " of " + refName + " shop data index + " + i + " is out of range. Must be between 0-15");
                return false;
            }
            if (sd.maxLevel < 0)
            {
                Debug.LogError("MAX level " + sd.maxLevel + " of " + refName + " shop data index + " + i + " is out of range. Must be between 0-15");
                return false;
            }
            if (sd.minLevel > sd.maxLevel)
            {
                Debug.LogError("MIN level " + sd.minLevel + " of " + refName + " shop data index + " + i + " is greater than max level " + sd.maxLevel + ". Please fix.");
                return false;
            }
            if (sd.valueMult < 0 || sd.saleMult < 0)
            {
                Debug.LogError("Value mult " + sd.valueMult + " or sale mult " + sd.saleMult + " of " + refName + " shop data index " + i + " cannot be below 0, setting to 0.");
                return false;
            }
            if ((sd.minItems <= 0 || sd.maxItems <= 0 || sd.maxItems < sd.minItems) &&
                refName != "shop_banker" && refName != "shop_farmercart")
            {
                Debug.LogError("Min or max items of " + refName + " shop data index " + i + " has a bad value. Must be at least 1, max must be above min.");
                return false;
            }
            if (sd.challengeValue < 1.0f || sd.challengeValue > Item.MAX_STARTING_CHALLENGE_VALUE)
            {
                //Debug.Log("Challenge value " + sd.challengeValue + " of " + refName + " shop data index " + i + " is less than 1.0 or greater than " + Item.MAX_STARTING_CHALLENGE_VALUE + ", please fix.");
                //return false;
            }
            if (sd.magicChance < 0 || sd.maxMagicMods < 0 || sd.maxMagicMods > 5)
            {
                Debug.Log("Magic chance of " + sd.magicChance + " or magic mod count " + sd.maxMagicMods + " is out of range. Magic chance must be >= 0, magic mods must be 0-5. Shop is " + refName + " index " + i);
                return false;
            }
        }

        return true;

    }

    public class ShopData
    {
        public ActorTable items;
        public int minLevel;
        public int maxLevel;
        public int minItems;
        public int maxItems;
        public float magicChance;
        public int maxMagicMods;
        public float challengeValue;
        public float valueMult;
        public float saleMult;
        public bool[] limitModFlags;
        public List<MagicModFlags> addPossibleModFlags;
        public bool modLimited;
        public bool adaptChallengeValue;
        public float chanceToUseBaseTable;
        public ActorTable specialTables;

        public ShopData()
        {
            valueMult = 1.0f;
            saleMult = 1.0f;
            chanceToUseBaseTable = 1.0f;
            limitModFlags = new bool[(int)MagicModFlags.COUNT];
            addPossibleModFlags = new List<MagicModFlags>();
            specialTables = new ActorTable();
            items = new ActorTable();
            minItems = 5;
            maxItems = 10;
        }
    }

    public ShopData GetShop()
    {
        if (shopList.Count == 0)
        {
            Debug.Log("No shops in this list!");
            return null;
        }
        int playerLevel = GameMasterScript.heroPCActor.myStats.GetLevel();
        ShopData returnShop = null;
        foreach (ShopData sd in shopList)
        {
            if (playerLevel >= sd.minLevel && playerLevel <= sd.maxLevel)
            {
                returnShop = sd;
            }
        }
        if (returnShop == null)
        {
            returnShop = shopList[0];
        }
        return returnShop;
    }

    public void ReadFromXML(XmlReader reader)
    {
        reader.ReadStartElement();

        string txt;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "RefName":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "ReplaceRef":
                    if (GameMasterScript.masterShopList.ContainsKey(refName))
                    {
                        GameMasterScript.masterShopList.Remove(refName);
                        //Debug.Log("ShopScript " + refName + " overrides existing shop.");
                    }
                    reader.ReadElementContentAsInt(); 
                    break;
                case "ShopData":
                    ShopScript.ShopData sd = new ShopScript.ShopData();
                    reader.ReadStartElement();
                    shopList.Add(sd);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "Reference":
                                string localRefName = reader.ReadElementContentAsString();

                                if (!GameMasterScript.masterShopTableList.TryGetValue(localRefName, out sd.items))
                                {
                                    if (Debug.isDebugBuild) Debug.Log("Could not find reference shop list " + localRefName + " for master shop " + refName);
                                    sd.items = new ActorTable();
                                }
                                break;
                            case "ValueMult":
                                txt = reader.ReadElementContentAsString();
                                sd.valueMult = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "SellMult":
                                txt = reader.ReadElementContentAsString();
                                sd.saleMult = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "MinLevel":
                                sd.minLevel = reader.ReadElementContentAsInt();
                                break;
                            case "MaxLevel":
                                sd.maxLevel = reader.ReadElementContentAsInt();
                                break;
                            case "MinItems":
                                sd.minItems = reader.ReadElementContentAsInt();
                                break;
                            case "MaxItems":
                                sd.maxItems = reader.ReadElementContentAsInt();
                                break;
                            case "MagicChance":
                                txt = reader.ReadElementContentAsString();
                                sd.magicChance = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "ChallengeValue":
                                txt = reader.ReadElementContentAsString();
                                sd.challengeValue = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "ChanceToUseBaseTable":
                                txt = reader.ReadElementContentAsString();
                                sd.chanceToUseBaseTable = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "MaxMagicMods":
                                sd.maxMagicMods = reader.ReadElementContentAsInt();
                                break;
                            case "AdaptChallengeValue":
                                sd.adaptChallengeValue = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "SpecialTables":
                                reader.ReadStartElement();
                                string tableName = "";
                                int attempts = 0;
                                while (reader.NodeType != XmlNodeType.EndElement)
                                {
                                    attempts++;
                                    if (attempts > 250)
                                    {
                                        Debug.LogError("Bad error reading shop data. " + reader.Name + " " + reader.NodeType);
                                        return;
                                    }
                                    if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.None)
                                    {
                                        reader.Read();
                                        continue;
                                    }
                                    tableName = reader.Name;
                                    int qty = reader.ReadElementContentAsInt();
                                    sd.specialTables.AddToTable(tableName, qty);
                                    //Debug.Log("Read table " + tableName);
                                }
                                reader.ReadEndElement();
                                break;
                            case "LimitModFlag":
                                sd.modLimited = true;
                                MagicModFlags mmf = (MagicModFlags)Enum.Parse(typeof(MagicModFlags), reader.ReadElementContentAsString());
                                sd.limitModFlags[(int)mmf] = true;
                                break;
                            case "AddPossibleModFlag":
                                MagicModFlags possibleFlag = (MagicModFlags)Enum.Parse(typeof(MagicModFlags), reader.ReadElementContentAsString());
                                sd.addPossibleModFlags.Add(possibleFlag);
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }

                    // Finished reading this ShopData.
                    if (sd.items != null && sd.items.GetTotalCount() > 0)
                    {
                        foreach (Item itm in GameMasterScript.itemsAutoAddToShops)
                        {
                            if (itm.challengeValue >= sd.challengeValue)
                            {
                                int mult = itm.forceAddToLootTablesAtRate;
                                if (mult == 0)
                                {
                                    mult = 100;
                                }
                                sd.items.AddToTable(itm.actorRefName, (sd.items.GetAverageValue() * mult) / 100);
                            }
                        }
                    }

                    reader.ReadEndElement();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }

}
