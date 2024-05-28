using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

public enum HealingItemState { NOT_CHECKED, YES, NO, COUNT }

public partial class Item : Actor, ISelectableUIObject
{
    public string cultureSensitiveName; // Used for special non-English sorts
    public string strippedName; // Not serialized. Generated on RebuildItemName

    public HealingItemState cachedHealItemState;
    

    //InventoryScript actualCollection;
    public InventoryScript collection;
    /* {
        get
        {
            return actualCollection;
        }
        set
        {
            if (GameMasterScript.gameLoadSequenceCompleted && value != null && value.owner != null)
            {
                Debug.Log("Setting " + actorUniqueID + " collection to " + value.owner.actorRefName);                
            } 
            actualCollection = value;
        }
    } */

    public Sprite itemSprite;
    public int shopPrice;
    public int salePrice;
    public string spriteRef;
    public string extraDescription; // For seasoning effects, mostly

    public string unbakedExtraDescription;
    public string unbakedDescription;

    public List<string> autoModRef;
    public List<string> modsAddedByDreamcaster;
    public string spriteEffect;
    public float challengeValue;
    public ItemTypes itemType;
    public List<MagicMod> mods;
    public Rarity rarity;
    public Rarity defaultTemplateRarity;
    public string description;
    public int numModPrefixes;
    public int numModSuffixes;
    public int cooldownTurnsRemaining; // Used for consumables, accessories if dropped after usage.
    public int timesUpgraded;
    public int upgradesByRarity;
    public int modsRemoved;
    public static List<Item> emptyItemList;
    public bool legendary;
    public bool favorite;

    /// <summary>
    /// Items marked as vendor trash will be sold at vendors when doing "SELL ALL", regardless of rarity etc.
    /// </summary>
    public bool vendorTrash;

    public bool dreamItem;
    public bool newlyPickedUp;
    public int reqNewGamePlusLevel;
    public List<ItemFilters> tags;
    public List<string> numberTags;

    public int forceAddToLootTablesAtRate; // Used for player-generated stuff.
    public bool autoAddToShopTables; // Same here

    public const float MAX_STARTING_CHALLENGE_VALUE = 1.9f;
    public const int CORE_ITEM_SPRITESHEET_MAXSIZE = 678;
    public const int MAX_RANK = 13;

    public Dictionary<string, int> addToShopRefs; // used only during load / mod process, doesn't need to be serialized
    bool replaceExistingRef; // used only during load / mod process, doesn't need to be serialized

    public string scriptOnAddToInventory; // Script that runs AFTER an item has been added to ANY inventory
    public string scriptBuildDisplayName; // replace normal naming scheme with custom function

    static List<string> germanSpecialMagicMods;

    static List<string> suffixes;
    static List<string> prefixes;
    static List<string> specialStrings;
    static bool firstRebuild;

    public bool customItemFromGenerator; // proc gen

    bool nameDirty;

    /// <summary>
    /// Any item that restores a stat
    /// </summary>
    public const int HEALING_ITEM_SORT_BASE_VALUE = 100;
    public const int DAMAGE_ITEM_BASE_VALUE = 200;
    public const int SUMMON_ITEM_BASE_VALUE = 300;
    public const int BUFF_ITEM_SORT_BASE_VALUE = 400;
    public const int GENERIC_CONSUMABLE_BASE_VALUE = 500;
    public const int WEAPON_BASE_VALUE = 600;
    public const int ARMOR_BASE_VALUE = 700;
    public const int OFFHAND_BASE_VALUE = 800;
    public const int EMBLEM_BASE_VALUE = 900;
    public const int ACCESSORY_BASE_VALUE = 1000;
    public const int GENERIC_ITEM_BASE_VALUE = 1100;
    public const int VALUABLES_BASE_VALUE = 1200;



    #region Interface Jibba

    public bool CheckNameDirty()
    {
        return nameDirty;
    }

    public void SetNameDirty(bool value)
    {
        nameDirty = value;
    }

    public Sprite GetSpriteForUI()
    {
        Sprite spr = UIManagerScript.GetItemSprite(spriteRef);
        if (spr == null)
        {
            // Maybe it's in the player mod files?
            spr = PlayerModManager.TryGetSpriteFromMods(spriteRef.ToLowerInvariant());
        }
        if (spr == null)
        {
            Debug.Log("No sprite found anywhere for item " + spriteRef + " " + actorRefName);
        }

        return spr;
    }

    public string GetNameForUI()
    {
        return displayName;
    }

    public string GetInformationForTooltip()
    {
        return GetItemInformationNoName(true);
    }

    public string GetBaseDisplayName()
    {
        return GameMasterScript.masterItemList[actorRefName].displayName;
    }

    #endregion

    protected override void Init()
    {
        if (initialized)
        {
            return;
        }
        base.Init();

        saveSlotIndexForCustomItemTemplate = 99;

        addToShopRefs = new Dictionary<string, int>();
        numberTags = new List<string>();
        extraDescription = "";
        scriptOnAddToInventory = "";
        autoAddToShopTables = false;
        forceAddToLootTablesAtRate = 0;
        unbakedDescription = "";
        unbakedExtraDescription = "";
        description = "";
        playerCollidable = false;
        monsterCollidable = false;
        collection = null;
        targetable = false;
        SetActorType(ActorTypes.ITEM);
        mods = new List<MagicMod>();
        modsAddedByDreamcaster = new List<string>();
        emptyItemList = new List<Item>(0);
        cooldownTurnsRemaining = 0;
        legendary = false;
        dreamItem = false;
        tags = new List<ItemFilters>();
        newlyPickedUp = false;
        spriteRef = "";
        scriptBuildDisplayName = "";
    }

    public virtual void ParseNumberTags()
    {
        unbakedDescription = description;
        unbakedExtraDescription = extraDescription;
        if (numberTags == null) return;
        //if (!numberTags.Any()) return;
        for (int i = 0; i < numberTags.Count; i++)
        {
            description = description.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
            extraDescription = extraDescription.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
        }
    }

    public virtual bool CanBeUsed()
    {
        return false;
    }

    public virtual bool CheckIfSameAs(Item itm)
    {
        if (itemType != itm.itemType) return false;
        return true;
    }

    public bool ValidForPlayer()
    {
        // No dream drums for Shara 
        if (SharaModeStuff.IsSharaModeActive() && itemType == ItemTypes.CONSUMABLE && actorRefName == "item_dreamdrum") return false;

        if (!legendary || SharaModeStuff.IsSharaModeActive()) return true;
        // If a legendary item ("Item X") has spawned, and the player has looted it, another COPY of "Item X" should never be gained by the player again.
        if (!GameMasterScript.createdHeroPCActor)
        {
            //Debug.Log(actorRefName + " must be valid; haven't created hero yet.");
            return true;
        }
        if (GameMasterScript.heroPCActor.FoundLegItem(actorRefName) 
            && (ReadActorData("pwn") != 1 && ReadActorData("playerowned") != 1)
            && !customItemFromGenerator)
        {
            return false;
        }
        return true;
    }

    public string GetDisplayItemType()
    {
        switch (itemType)
        {
            case ItemTypes.WEAPON:
                Weapon w = this as Weapon;
                return Weapon.weaponTypesVerbose[(int)w.weaponType];
            case ItemTypes.ARMOR:
                Armor a = this as Armor;
                return Armor.armorTypesVerbose[(int)a.armorType];
            case ItemTypes.ACCESSORY:
                return StringManager.GetString("eq_slot_accessory");
            case ItemTypes.EMBLEM:
                return StringManager.GetString("eq_slot_emblem");
            case ItemTypes.OFFHAND:
                return StringManager.GetString("eq_slot_offhand");
            case ItemTypes.CONSUMABLE:
                return StringManager.GetString("itemtype_consumable");
        }

        return "";
    }

    public bool ValidForInventory(Actor act)
    {
        if (act.GetActorType() == ActorTypes.MONSTER && challengeValue > 500f)
        {
            return false;
        }
    /* 
        if (act.GetActorType() == ActorTypes.NPC && legendary)
        {
            if (act.myInventory.HasItemByRef(actorRefName))
            {
                return false;
            }
        } */
        return true;
    }

    public bool IsCookingIngredient()
    {
        if (itemType != ItemTypes.CONSUMABLE) return false;

        Consumable con = this as Consumable;
        if (con.cookingIngredient)
        {
            return true;
        }
        return false;
    }

    public void CheckForAndInitiatePickupDialog()
    {
        if (ReadActorDataString("monsterletter_author") == "") return;

        string abilRef = ReadActorDataString("monsterletter_skill");
        AbilityScript getAbil = AbilityScript.GetAbilityByName(abilRef);

        string skillTemplateName = getAbil.abilityName;

        string recip = ReadActorDataString("monsterletter_recipient");

        if (string.IsNullOrEmpty(recip))
        {
            recip = GameMasterScript.heroPCActor.displayName;
        }

        // Deprecate tags here in favor of $functions in the dialog
        StringManager.SetTag(0, recip);
        StringManager.SetTag(1, ReadActorDataString("monsterletter_author"));
        StringManager.SetTag(2, skillTemplateName);

        GameMasterScript.gmsSingleton.SetTempStringData("letter_heroname", recip);
        GameMasterScript.gmsSingleton.SetTempStringData("letter_author", ReadActorDataString("monsterletter_author"));
        GameMasterScript.gmsSingleton.SetTempStringData("letter_skill", skillTemplateName);

        UIManagerScript.StartConversationByRef("happy_released_monster", DialogType.KEYSTORY, null);
    }

    public virtual int GetQuantity()
    {
        return 1;
    }

    public string GetQuantityText()
    {
        int iQuantity = GetQuantity();
        if (iQuantity > 1)
        {
            return " (" + iQuantity + ") ";
        }

        return "";
    }

    //Default Items don't have quantities yet
    public virtual bool ChangeQuantity(int amt)
    {
#if UNITY_EDITOR
        Debug.LogError("Calling ChangeQuantity() on an default class Item, did you mean to this? Because they don't have quantities.");
#endif
        //return true to indicate the item is still in the inventory.
        return true;
    }


    public void WritePrice(XmlWriter writer)
    {
        if ((shopPrice == 0) && (salePrice == 0))
        {
            return;
        }

        string builder = shopPrice.ToString();
        if (salePrice != 0)
        {
            builder += "|" + salePrice.ToString();
        }

        writer.WriteElementString("pr", builder);
    }


    public void IncreasePowerFromRarityBoost(MagicMod mod)
    {
        if (upgradesByRarity >= Equipment.MAX_UPGRADES_BY_RARITY)
        {
            return;
        }
        switch (itemType)
        {
            case ItemTypes.WEAPON:
                Weapon w = this as Weapon;

                // Don't modify power based on CURRENT power. Do it on BASE weapon power.
                float powerGain = 0f;
                if (mod.changePower != 0f)
                {
                    powerGain = w.GetTemplate().power * .025f;
                }
                else
                {
                    powerGain = w.GetTemplate().power * .05f;
                }
                powerGain = Mathf.Clamp(powerGain, 0.1f, 2.5f);
                w.power += powerGain;
                upgradesByRarity++;
                break;
            case ItemTypes.ACCESSORY:
                Accessory acc = this as Accessory;
                if (acc.HasModByRef("mm_statboost1"))
                {
                    acc.RemoveMod("mm_statboost1");
                    acc.AddModByRef("mm_statboost2", false);
                    upgradesByRarity++;
                }
                else if (acc.HasModByRef("mm_statboost2"))
                {
                    acc.RemoveMod("mm_statboost2");
                    acc.AddModByRef("mm_statboost3", false);
                    upgradesByRarity++;
                }
                else if (acc.HasModByRef("mm_statboost3"))
                {
                    acc.RemoveMod("mm_statboost3");
                    acc.AddModByRef("mm_statboost4", false);
                    upgradesByRarity++;
                }
                else if (acc.HasModByRef("mm_statboost4"))
                {
                    acc.RemoveMod("mm_statboost4");
                    acc.AddModByRef("mm_statboost5", false);
                    upgradesByRarity++;
                }
                else if (!acc.HasModByRef("mm_statboost5"))
                {
                    acc.AddModByRef("mm_statboost1", false);
                    upgradesByRarity++;
                }

                break;
            case ItemTypes.OFFHAND:
                Offhand oh = this as Offhand;
                if (oh.blockChance > 0) // Shield
                {
                    oh.blockDamageReduction -= 0.035f;
                    upgradesByRarity++;
                }
                else if (oh.allowBow) // Quiver
                {
                    if (oh.HasModByRef("mm_rangeddamage4"))
                    {
                        oh.RemoveMod("mm_rangeddamage4");
                        oh.AddModByRef("mm_rangeddamage8", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_rangeddamage8"))
                    {
                        oh.RemoveMod("mm_rangeddamage8");
                        oh.AddModByRef("mm_rangeddamage12", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_rangeddamage12"))
                    {
                        oh.RemoveMod("mm_rangeddamage12");
                        oh.AddModByRef("mm_rangeddamage16", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_rangeddamage16"))
                    {
                        oh.RemoveMod("mm_rangeddamage16");
                        oh.AddModByRef("mm_rangeddamage20", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_rangeddamage20"))
                    {
                        oh.RemoveMod("mm_rangeddamage20");
                        oh.AddModByRef("mm_rangeddamage24", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_rangeddamage24"))
                    {
                        oh.RemoveMod("mm_rangeddamage24");
                        oh.AddModByRef("mm_rangeddamage28", false);
                        upgradesByRarity++;
                    }
                    else if (!oh.HasModByRef("mm_rangeddamage28"))
                    {
                        oh.AddModByRef("mm_rangeddamage4", false);
                        upgradesByRarity++;
                    }
                }
                else // Book, probably
                {
                    if (oh.HasModByRef("mm_elemdamageboost3"))
                    {
                        oh.RemoveMod("mm_elemdamageboost3");
                        oh.AddModByRef("mm_elemdamageboost6", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_elemdamageboost6"))
                    {
                        oh.RemoveMod("mm_elemdamageboost6");
                        oh.AddModByRef("mm_elemdamageboost9", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_elemdamageboost9"))
                    {
                        oh.RemoveMod("mm_elemdamageboost9");
                        oh.AddModByRef("mm_elemdamageboost12", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_elemdamageboost12"))
                    {
                        oh.RemoveMod("mm_elemdamageboost12");
                        oh.AddModByRef("mm_elemdamageboost15", false);
                        upgradesByRarity++;
                    }
                    else if (oh.HasModByRef("mm_elemdamageboost15"))
                    {
                        oh.RemoveMod("mm_elemdamageboost15");
                        oh.AddModByRef("mm_elemdamageboost18", false);
                        upgradesByRarity++;
                    }
                    else if (!oh.HasModByRef("mm_elemdamageboost18"))
                    {
                        oh.AddModByRef("mm_elemdamageboost3", false);
                        upgradesByRarity++;
                    }
                }
                break;
            case ItemTypes.ARMOR:
                Armor a = this as Armor;
                upgradesByRarity++;

                switch (a.armorType)
                {                    
                    case ArmorTypes.HEAVY:
                        float resistMult = a.resists[(int)DamageTypes.PHYSICAL].multiplier;
                        resistMult -= 0.0125f;
                        a.resists[(int)DamageTypes.PHYSICAL].multiplier = resistMult;
                        break;
                    case ArmorTypes.MEDIUM:
                        resistMult = a.resists[(int)DamageTypes.PHYSICAL].multiplier;
                        resistMult -= 0.0075f;
                        a.resists[(int)DamageTypes.PHYSICAL].multiplier = resistMult;
                        break;
                }

                if ((a.armorType == ArmorTypes.MEDIUM && UnityEngine.Random.Range(0, 2) == 0) || a.armorType == ArmorTypes.LIGHT)
                {
                    switch (a.armorType)
                    {
                        case ArmorTypes.LIGHT:
                        case ArmorTypes.MEDIUM:
                            MagicMod mmRemove = null;
                            int dodgeAmount = 0;
                            foreach (MagicMod mm in a.mods)
                            {
                                if (mm.refName.Contains("mm_dodge"))
                                {
                                    string sub = mm.refName.Substring(mm.refName.Length - 2);
                                    if (sub[0] == 'e')
                                    {
                                        sub = sub.Substring(1);
                                    }
                                    dodgeAmount = Int32.Parse(sub);
                                    mmRemove = mm;
                                    break;
                                }
                            }
                            if (mmRemove != null)
                            {
                                //a.mods.Remove(mmRemove);
                                dodgeAmount += 1;
                                a.extraDodge += 1;
                                //mNewMod = MagicMod.FindModFromName("mm_dodge" + dodgeAmount);
                            }
                            else
                            {
                                // Add dodge
                                //mNewMod = MagicMod.FindModFromName("mm_dodge2");
                                a.extraDodge += 2;
                            }
                            //EquipmentBlock.MakeMagicalFromMod(a, mNewMod, false, false, true);
                            break;
                    }
                }
                break;
        }
    }

    int CalculateBaseShopPrice()
    {
        int returnShopPrice = shopPrice;
        if (GameMasterScript.initialGameAwakeComplete)
        {
            // Player has shop bonus of 0.23, so decrease buy price by 0.23%, i.e. multiply by 0.87
            float multiplier = 1f - GameMasterScript.heroPCActor.advStats[(int)AdventureStats.SHOPBONUS];
            returnShopPrice = (int)(returnShopPrice * multiplier);
        }
        return returnShopPrice;
    }

    public int GetShopPrice()
    {
        int returnShopPrice = CalculateBaseShopPrice();

        if (itemType != ItemTypes.CONSUMABLE)
        {
            return returnShopPrice;
        }
        else
        {
            Consumable cn = this as Consumable;
            return (returnShopPrice * cn.Quantity);
        }
    }


    public int GetIndividualCasinoPrice()
    {
        // returns price in tokens
        int goldPrice = CalculateBaseShopPrice();
        int tokens = (int)(goldPrice / 16f);
        if (tokens < 10) tokens = 10;
        if (tokens > 400) tokens = 400;

        if (legendary || itemType != ItemTypes.CONSUMABLE)
        {
            if (challengeValue >= 1.0f && challengeValue < 1.1f)
            {
                tokens = 30;
            }
            else if (challengeValue >= 1.1f && challengeValue < 1.2f)
            {
                tokens = 40;
            }
            else if (challengeValue >= 1.2f && challengeValue < 1.3f)
            {
                tokens = 50;
            }
            else if (challengeValue >= 1.3f && challengeValue < 1.4f)
            {
                tokens = 60;
            }
            else if (challengeValue >= 1.4f && challengeValue < 1.5f)
            {
                tokens = 80;
            }
            else if (challengeValue >= 1.5f && challengeValue < 1.6f)
            {
                tokens = 100;
            }
            else if (challengeValue >= 1.6f && challengeValue < 1.7f)
            {
                tokens = 120;
            }
            else if (challengeValue >= 1.7f && challengeValue < 1.8f)
            {
                tokens = 150;
            }
            else if (challengeValue >= 1.8f && challengeValue < 1.9f)
            {
                tokens = 180;
            }
            else if (challengeValue >= 1.9f && challengeValue < 2f)
            {
                tokens = 200;
            }
            else if (challengeValue >= 2.0f && challengeValue < 2.1f)
            {
                tokens = 240;
            }
            else if (challengeValue >= 2.1f && challengeValue < 2.2f)
            {
                tokens = 280;
            }
            else if (challengeValue >= 2.2f)
            {
                tokens = 350;
            }
        }

        if (legendary)
        {
            tokens = (int)(tokens * 2f);
        }

        tokens = (int)(tokens * 0.5f);

        if (tokens < 10) tokens = 10;
        if (tokens > 400) tokens = 400;

        return tokens;
    }

    public int GetIndividualShopPrice()
    {
        return CalculateBaseShopPrice();
    }

    public string GetIndividualShopPriceWithUnit()
    {
        int cost = 0;
        string unit = StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD);
        if (UIManagerScript.currentConversation != null && ShopUIScript.CheckShopInterfaceState() && UIManagerScript.currentConversation.whichNPC.actorRefName.Contains("npc_casinoshop"))
        {
            cost = GetIndividualCasinoPrice();
            unit = " " + StringManager.GetString("misc_tokens");
        }
        else
        {
            cost = GetIndividualShopPrice();
        }
        return (cost + unit);
    }

    public int GetSalePrice(float saleMult = 1f, bool multiplyByQuantity = true)
    {
        if (salePrice == 0)
        {
            CalculateSalePrice();
        }

        //Debug.Log(salePrice);

        int returnPrice = salePrice;

        if (GameMasterScript.initialGameAwakeComplete)
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_entrepreneur"))
            {
                returnPrice = (int)(returnPrice * 1.2f);
                //Debug.Log("Entrep");
            }

            // Player has shop bonus of 0.23, so increase return value by 23%
            returnPrice += (int)(returnPrice * GameMasterScript.heroPCActor.advStats[(int)AdventureStats.SHOPBONUS]);
            //Debug.Log("Shop bonus? " + GameMasterScript.heroPCActor.advStats[(int)AdventureStats.SHOPBONUS]);
        }

        returnPrice = (int)(returnPrice * saleMult);

        //Debug.Log("Sale mult is " + saleMult);

        if (returnPrice >= shopPrice)
        {
            returnPrice = shopPrice - 1;
            if (returnPrice < 0)
            {
                returnPrice = 0;
            }
        }

        if (multiplyByQuantity)
        {
            return returnPrice * GetQuantity();
        }
        else
        {
            return returnPrice;
        }
        

        /*
        if (itemType != ItemTypes.CONSUMABLE)
        {
            return returnPrice;
        }
        else
        {
            Consumable cn = this as Consumable;
            //Debug.Log(returnPrice + " " + cn.quantity);
            return (returnPrice * cn.quantity);
        }
        */
    }

    public int GetIndividualSalePrice(float saleMult = 1f)
    {
        return GetSalePrice(saleMult, false);
    }

    public void AddTag(ItemFilters tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }

    public bool CheckTag(int tag)
    {
        if (tags.Contains((ItemFilters)tag))
        {
            return true;
        }
        return false;
    }

    public bool CheckTag(ItemFilters tag)
    {
        if (tags.Contains(tag))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// This returns the number of mods that aren't automods, and also aren't rarity-based upgrades like corestats etc.
    /// </summary>
    /// <returns></returns>
    public int GetNonAutomodOrUpgradeCount()
    {
        int modCount = 0;
        bool freeSkillOrbCounted = false; // The FIRST skill orb on any item is free to add

        foreach (MagicMod mm in mods)
        {
            if (mm.noNameChange) continue;
            if (autoModRef != null)
            {
                if (autoModRef.Contains(mm.refName))
                {
                    continue;
                }
                modCount++;
            }
            else
            {
                if (Equipment.modsThatCountAsAutoMods.Contains(mm.refName)) continue;
                if (Equipment.inherentOffhandMods.Contains(mm.refName)) continue;
                modCount++;
            }

            if (mm.lucidOrbsOnly && mm.jobAbilityMod && !freeSkillOrbCounted)
            {
                modCount--;
                freeSkillOrbCounted = true;
            }
        }

        return modCount;
    }

    public int GetNonAutomodCount()
    {
        if (mods.Count == 0)
        {
            return 0;
        }

        int modCount = 0;

        bool freeSkillOrbCounted = false; // The FIRST skill orb on any item is free to add

        foreach (MagicMod mm in mods)
        {
            //Debug.Log(actorUniqueID + " " + actorRefName + " mod: " + mm.refName);
            if (mm.noNameChange) continue;
            if (autoModRef != null)
            {
                if (autoModRef.Contains(mm.refName))
                {
                    continue;
                }

                modCount++;
                //Debug.Log(mm.refName + " " + mm.modName + " counts");
            }
            else
            {
                //Debug.Log(mm.refName + " " + mm.modName + " also counts");
                modCount++;
            }

            if (mm.lucidOrbsOnly && mm.jobAbilityMod && !freeSkillOrbCounted)
            {
                modCount--;
                freeSkillOrbCounted = true;
            }
        }

        //Debug.Log("Final Counter level : " + modCount);
        return modCount;
    }

    public bool ValidForItemWorld()
    {
        Equipment eq = this as Equipment;

        if (!eq.CanHandleMoreMagicMods() && eq.timesUpgraded >= Equipment.GetMaxUpgrades())
        {
            return false;
        }
        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(this, onlyActualFists: true))
        {
            return false;
        }
        return true;
    }

    public bool IsItemFood()
    {
        if (itemType != ItemTypes.CONSUMABLE) return false;
        Consumable con = this as Consumable;
        return con.isFood;
    }

    public string GetSortableName()
    {
        // Even better than the stripped name, this is stripped *and* prepped for culture as needed
        return cultureSensitiveName; 
    }

    //#todo japaaannnn
    public string GetPluralName()
    {
        switch(StringManager.gameLanguage)
        {
            case EGameLanguage.en_us:
                string localStr = displayName;
                localStr = localStr.Replace("</color>", "");
                char[] nameArray = localStr.ToCharArray();
                char lastChar = nameArray[nameArray.Length - 1];
                if (lastChar == 's')
                {
                    return displayName;
                }

                return displayName + "s";
            case EGameLanguage.jp_japan:
            case EGameLanguage.zh_cn:
            default:
                return displayName;
        }
    }

    public int GetBankPrice(int qty = 1)
    {
        float price = shopPrice * 0.2f;

        if (challengeValue <= 1.4f)
        {
            price *= 0.6f;
        }

        if (qty > 1)
        {
            price = (GetBankPrice(1) * qty); // * 0.5f; // this was getindividual SHOP price, but should be BANK price? Also why was this 50%
        }

        int returnPrice = (int)price;

        if (returnPrice == 0)
        {
            returnPrice = 1;
        }

        return (int)returnPrice;
    }
    
    public bool IsCurative(StatTypes stat = StatTypes.COUNT)
    {
        if (itemType != ItemTypes.CONSUMABLE)
        {
            return false;
        }

        if (cachedHealItemState != HealingItemState.NOT_CHECKED)
        {
            return cachedHealItemState == HealingItemState.YES ? true : false;
        }

        // Uhh just hardcode this???

        Consumable con = this as Consumable;

        if (stat != StatTypes.COUNT)
        {
            // Must heal a specific stat.
            if (con.parentForEffectChildren == null) return false;
            foreach (EffectScript eff in con.parentForEffectChildren.listEffectScripts)
            {
                if (eff.effectType == EffectType.CHANGESTAT)
                {
                    ChangeStatEffect cse = eff as ChangeStatEffect;
                    if (cse.stat == stat)
                    {
                        cachedHealItemState = HealingItemState.YES;
                        return true;
                    }
                }
                else if (eff.effectType == EffectType.ADDSTATUS)
                {
                    AddStatusEffect ase = eff as AddStatusEffect;
                    StatusEffect addedStatus = GameMasterScript.FindStatusTemplateByName(ase.statusRef);
                    if (!addedStatus.isPositive) continue;
                    foreach (EffectScript statusSubEffect in addedStatus.listEffectScripts)
                    {
                        if (statusSubEffect.effectType == EffectType.CHANGESTAT)
                        {
                            ChangeStatEffect cse = statusSubEffect as ChangeStatEffect;
                            if (cse.stat == stat) 
                            {
                                cachedHealItemState = HealingItemState.YES;
                                return true;
                            }
                        }
                    }
                }
            }
            cachedHealItemState = HealingItemState.NO;
            return false;
        }
        else
        {
            cachedHealItemState = HealingItemState.NO;
            return CheckTag((int)ItemFilters.RECOVERY);
        }


    }

    public bool IsEquipment()
    {
        if (itemType == ItemTypes.WEAPON || itemType == ItemTypes.EMBLEM || itemType == ItemTypes.OFFHAND || itemType == ItemTypes.ARMOR || itemType == ItemTypes.ACCESSORY)
        {
            return true;
        }
        return false;
    }

    public bool IsLucidOrb()
    {
        if (itemType != ItemTypes.CONSUMABLE) return false;
        if (actorRefName == "orb_itemworld" && !string.IsNullOrEmpty(GetOrbMagicModRef()))
        {
            return true;
        }
        return false;
    }

    public bool IsJobSkillOrb()
    {
        if (actorRefName == "orb_itemworld" && !string.IsNullOrEmpty(GetOrbMagicModRef()))
        {
            MagicMod template = MagicMod.FindModFromName(GetOrbMagicModRef());

            // This is a class orb.
            if (template.bDontAnnounceAddedAbilities)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsNightmareOrb()
    {
        if (actorRefName == "orb_itemworld" && ReadActorData("nightmare_orb") == 1)
        {
            return true;
        }
        return false;
    }

    void RebuildStrippedName()
    {
        string returner = "";
        if (!displayName.Contains("color")) // Speeds things up to avoid regex.
        {
            strippedName = displayName;
            BuildCultureSensitiveName();
            return;
        }
        returner = displayName.Replace("</color>", "");
        string regex = "(\\<.*\\>)";
        returner = Regex.Replace(returner, regex, "");
        strippedName = returner;
        BuildCultureSensitiveName();
    }

    void BuildCultureSensitiveName()
    {
        if (strippedName == "")
        {
            cultureSensitiveName = "";
            return;
        }
        cultureSensitiveName = StringManager.BuildCultureSensitiveName(strippedName);
    }

    // #todo - Make this all localization compatible. Easy right?
    // Right?
    //
    //
    //
    // Right?
    public void RebuildDisplayName()
    {
        if (!firstRebuild)
        {
            suffixes = new List<string>();
            prefixes = new List<string>();
            specialStrings = new List<string>();
            firstRebuild = true;
        }

        bool debug = false;

        #if UNITY_EDITOR
            //if (displayName.Contains("Headhunter")) debug = false;
        #endif

        if (!string.IsNullOrEmpty(scriptBuildDisplayName))
        {
            // override everything below if we have a custom function!
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(ItemNameGenerationFunctions), scriptBuildDisplayName);
            object[] paramList = new object[1];
            paramList[0] = this;
            displayName = runscript.Invoke(null, paramList) as string;
            return;
        }

        displayName = GetItemTemplateFromRef(actorRefName).displayName;

        if (debug) Debug.Log("Step 1: Display name is " + displayName);

        string baseDisplayName = GetBaseDisplayName();

        if (debug) Debug.Log("Step 2: BASE display name is " + baseDisplayName);

        if (itemType == ItemTypes.EMBLEM)
        {
            Emblem jobEmblem = this as Emblem;
            if (jobEmblem.jobForEmblem == CharacterJobs.GENERIC || jobEmblem.jobForEmblem == CharacterJobs.COUNT)
            {
                jobEmblem.jobForEmblem = GameMasterScript.heroPCActor.myJob.jobEnum;
            }
            CharacterJobData emblemCJD = CharacterJobData.GetJobDataByEnum((int)jobEmblem.jobForEmblem);

            // Localization friendly now!
            if (SharaModeStuff.IsSharaModeActive())
            {
                StringManager.SetTag(0, GameMasterScript.heroPCActor.displayName);
            }
            else
            {
                StringManager.SetTag(0, emblemCJD.DisplayName);
            }
            
            StringManager.SetTag(1, displayName);
            displayName = StringManager.GetString("item_jobemblem");
            baseDisplayName = displayName;
        }

        // New 11/9/17, start with no color at all.
        if (displayName.Contains('<')) // Fast check to avoid regex
        {
            displayName = Regex.Replace(displayName, "<.*?>", string.Empty);
            baseDisplayName = displayName; // why though
            if (debug) Debug.Log("Step 3: Stripped colors, is now " + displayName);
        }        

        if (legendary)
        {
            if (IsEquipment())
            {
                Equipment eq = this as Equipment;
                if (eq.gearSet != null)
                {
                    rarity = Rarity.GEARSET;
                    for (int i = 0; i < timesUpgraded; i++)
                    {
                        if (i == Equipment.GetMaxUpgrades()) break;
                        displayName += '+';
                    }

                    displayName = UIManagerScript.greenHexColor + displayName + "</color>";
                    RebuildStrippedName();
                    return;
                }
            }
            if (rarity != Rarity.GEARSET)
            {
                rarity = Rarity.LEGENDARY;
                displayName = GetLegendaryColor() + baseDisplayName + "</color>"; // GetBase WAS regular DisplayName... this ok?

                if (debug) Debug.Log("Step 4: Added color string for legendary, is now " + displayName);
            }
            for (int i = 0; i < timesUpgraded; i++)
            {
                if (i == Equipment.GetMaxUpgrades()) break;
                displayName += '+';
            }
            RebuildStrippedName();
            return;
        } // End legendary conditional

        for (int i = 0; i < timesUpgraded; i++)
        {
            if (i == Equipment.GetMaxUpgrades()) break;
            displayName += '+';
        }

        if (dreamItem)
        {
            displayName = StringManager.GetString("misc_dream") + " " + displayName;
        }

        if (actorRefName == "item_monsterletter")
        {
            string skillRef = ReadActorDataString("monsterletter_skill");
            AbilityScript template = AbilityScript.GetAbilityByName(skillRef);
            string dispName = template.abilityName;
            displayName += ": " + dispName;
        }
        else if (actorRefName == "item_lucidorb_shard" && GetOrbMagicModRef() != "")
        {
            MagicMod template = MagicMod.FindModFromName(GetOrbMagicModRef());
            displayName = StringManager.GetString("item_special_orb_shard");
            rarity = Rarity.UNCOMMON;
            displayName = displayName + ": " + template.modName;
        }
        else if (actorRefName == "orb_itemworld")
        {
            if (ReadActorData("nightmare_orb") == 1)
            {
                spriteRef = "assorteditems_547"; // Nightmare orb prefab
                displayName = StringManager.GetString("item_nightmare_orb");
                rarity = Rarity.ARTIFACT;

            }
            else if (!string.IsNullOrEmpty(GetOrbMagicModRef())) // Lucid or skill orb
            {
                spriteRef = "assorteditems_404"; // Lucid orb prefab
                MagicMod template = MagicMod.FindModFromName(GetOrbMagicModRef());

                displayName = StringManager.GetString("item_special_orb");

                // This is a class orb.
                if (template.bDontAnnounceAddedAbilities)
                {
                    spriteRef = "assorteditems_542"; // SKILL orb prefab
                    displayName = StringManager.GetString("misc_skillorb") + ": " + template.modName;
                    rarity = Rarity.ANCIENT;
                }
                else
                {
                    rarity = Rarity.UNCOMMON;
                    displayName = displayName + ": " + template.modName;
                }
            }
        }

        bool languageJP = StringManager.gameLanguage == EGameLanguage.jp_japan;
        bool languageCN = StringManager.gameLanguage == EGameLanguage.zh_cn;
        bool languageDE = StringManager.gameLanguage == EGameLanguage.de_germany;
        bool languageES = StringManager.gameLanguage == EGameLanguage.es_spain;

        if ((mods == null || mods.Count == 0) && !legendary)
        {
            // Don't do anything
        }
        else if (mods != null)
        {
            numModPrefixes = 0;
            numModSuffixes = 0;

            Equipment eq = this as Equipment;
            bool anyAutoMod = false;
            if (IsEquipment() && eq.autoModRef != null && eq.autoModRef.Count > 0)
            {
                anyAutoMod = true;
            }

            suffixes.Clear();
            prefixes.Clear();
            specialStrings.Clear();

            if (languageDE && germanSpecialMagicMods == null)
            {
                germanSpecialMagicMods = new List<string>()
                    {
                        "mm_soldiers",
                        "mm_warriors",
                        "mm_gladiators",
                        "mm_warlords",
                        "mm_fencers",
                        "mm_duelists",
                        "mm_gluttony2",
                        "mm_xp2_grandmaster",
                        "mm_xp2_scholar"
                    };
            }

            foreach (MagicMod mod in mods)
            {
                if (anyAutoMod && eq.autoModRef.Contains(mod.refName))
                {
                    continue;
                }

                if (!mod.noNameChange && !legendary)
                {
                    // Possessive mod names are treated differently in German
                    if (languageDE)
                    {
                        if (germanSpecialMagicMods.Contains(mod.refName))
                        {
                            specialStrings.Add(mod.modName);
                        }
                        else
                        {
                            suffixes.Add(mod.modName);
                        }
                        continue;
                    }

                    if (languageES)
                    {
                        suffixes.Add(mod.modName);
                        continue;
                    }

                    // Japanese ONLY uses prefixes, also there are no spaces.
                    if (mod.prefix || languageJP) 
                    {
                        // Change the name if it's a prefix.
                        if (languageJP)
                        {                            
                            prefixes.Add(mod.modName);
                        }
                        else
                        {
                            displayName = mod.modName + " " + displayName;
                        }
                        
                        numModPrefixes++;
                    }
                    else
                    {
                        // Suffix, add to the end
                        suffixes.Add(mod.modName);
                        numModSuffixes++;
                    }

                    displayName = Regex.Replace(displayName, "<.*?>", string.Empty);

                }
            }

            if (languageJP)
            {
                string prefixBuilder = "";
                for (int i = 0; i < prefixes.Count; i++)
                {
                    prefixBuilder += prefixes[i]; // Add prefixes one by one
                    if (i == prefixes.Count - 1)
                    {
                        prefixBuilder += "の" + '\u2009'; // The final one gets a finishing character and then a half space
                    }
                    else
                    {
                        prefixBuilder += '\u2009'; // All prefixes BEFORE the final get a half space too
                    }
                }
                displayName = prefixBuilder + displayName;
            }
            else if (languageCN)
            {
                string prefixBuilder = "";
                for (int i = 0; i < prefixes.Count; i++)
                {
                    prefixBuilder += prefixes[i]; // Add prefixes one by one
                    prefixBuilder += '\u2009'; // All prefixes BEFORE the final get a half space too
                }
                displayName = prefixBuilder + displayName;
            }
            else if (languageES)
            {
                if (suffixes.Count > 0)
                {
                    string modAdder = " (";
                    for (int i = 0; i < suffixes.Count; i++)
                    {
                        modAdder += suffixes[i];
                        if (suffixes.Count > 1 && i < suffixes.Count - 1)
                        {
                            modAdder += ", ";
                        }
                    }
                    displayName += modAdder + ")";
                }
            }
            else if (languageDE)
            {
                // First, append our special possessive strings carefully.
                if (specialStrings.Count == 1)
                {
                    displayName = displayName + " " + specialStrings[0]; // Shortsword des Chemikers
                }
                else if (germanSpecialMagicMods.Count == 2)
                {
                    // Shortsword des Chemikers und des Warrior
                    displayName = displayName + " " + specialStrings[0] + " und " + specialStrings[1];
                }
                else if (specialStrings.Count > 2)
                {
                    // Shortsword des Chemikers, des Soldaten, und des Kriegers
                    for (int i = 0; i < specialStrings.Count; i++)
                    {
                        displayName += specialStrings[i];
                        if (i > 0 && i < specialStrings.Count - 1)
                        {
                            displayName += ", "; 
                        }
                        else if (i == specialStrings.Count-1)
                        {
                            displayName += " und ";
                        }
                    }
                }

                for (int i = 0; i < suffixes.Count; i++)
                {
                    if (i == 0) // Open parens on the first suffix
                    {
                        displayName += " (" + suffixes[i];
                    }
                    else if (i < suffixes.Count - 1) // If we're 2nd, 3rd, 4th (etc) suffix, add a space + comma
                    {
                        displayName += ", " + suffixes[i];
                    }

                    if (i == suffixes.Count - 1)
                    {
                        displayName += ")";
                    }                    
                }
            }

            // Only use commas / of / and (etc) in English.
            switch (StringManager.gameLanguage)
            {
                case EGameLanguage.en_us:
                    if (suffixes.Count > 0)
                    {
                        for (int i = 0; i < suffixes.Count; i++)
                        {
                            if (i == 0)
                            {
                                displayName += " of " + suffixes[i];
                            }
                            else
                            {
                                string oxfordComma = "";

                                if (i != suffixes.Count - 1)
                                {

                                    displayName += ", " + suffixes[i];
                                }
                                else
                                {
                                    if (suffixes.Count > 2)
                                    {
                                        oxfordComma = ",";
                                    }
                                    displayName += oxfordComma + " and " + suffixes[i];
                                }
                            }
                        }
                    }
                    break;
            }
            

            int numMods = 0;
            if (mods != null)
            {
                numMods = mods.Count;
            }

            for (int i = 0; i < mods.Count; i++)  // was numMods, which gave us an inaccurate count
            {
                if (anyAutoMod && eq.autoModRef.Contains(mods[i].refName))
                {
                    numMods--;
                    continue;
                }

                if (Equipment.modsThatCountAsAutoMods.Contains(mods[i].refName))
                {
                    // We don't want rarity-related mods to count toward our rarity. That would be circular.
                    numMods--;
                    continue;
                }

                if (mods[i].noNameChange || legendary)
                {
                    numMods--;
                }
            }

            if (itemType != ItemTypes.EMBLEM && itemType != ItemTypes.CONSUMABLE)
            {
                switch (numMods)
                {
                    case 0:
                        rarity = Rarity.COMMON;
                        break;
                    case 1:
                        rarity = Rarity.UNCOMMON;
                        break;
                    case 2:
                    case 3:
                        rarity = Rarity.MAGICAL;
                        break;
                    case 4:
                    case 5:
                    case 6:
                        rarity = Rarity.ANCIENT;
                        break;
                }
            }

        }

        //Debug.Log(actorRefName + " " + displayName + " " + rarity);

        if (debug) Debug.Log("Step 5: Add a rarity string to " + displayName);

        switch (rarity)
        {
            case Rarity.UNCOMMON:
                displayName = "<color=#25a3dd>" + displayName + "</color>";
                break;
            case Rarity.MAGICAL:
                displayName = "<color=#edd50d>" + displayName + "</color>";
                break;
            case Rarity.ANCIENT:
                displayName = "<color=orange>" + displayName + "</color>";
                break;
            case Rarity.ARTIFACT:
                displayName = "<color=#ffc80a>" + displayName + "</color>";
                break;
            case Rarity.LEGENDARY:
                displayName = GetLegendaryColor() + displayName + "</color>";
                if (debug) Debug.Log("Step 6: Is now " + displayName);
                break;
            case Rarity.GEARSET:
                displayName = UIManagerScript.greenHexColor + displayName + "</color>";
                break;
        }
        RebuildStrippedName();
    }

    public Item ReadFromSave(XmlReader reader, bool addItemToMasterDict = true, bool readingFromSharedBank = false)
    {
        Item itemToRead = null;
        Armor armorToRead = null;
        Weapon weaponToRead = null;
        Accessory accessoryToRead = null;
        Consumable consumableToRead = null;
        Offhand offhandToRead = null;
        Emblem emblemToRead = null;
        Equipment eqToRead = null;
        bool rebuildName = false;
        string txt;

        bool failedToFindTemplate = false;

        bool readItemType = false;

        string templateName = "";

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (failedToFindTemplate) 
            {
                // This could happen if item data was changed, i.e. a mod was removed and we're trying to load the file.
                // In this case, read to the end of the ITEM node.
                int attempts = 0;
                while (reader.NodeType != XmlNodeType.EndElement || reader.Name != "item")
                {
                    attempts++;
                    if (attempts > 500)
                    {
                        break;
                    }
                    reader.Read();
                }
                reader.ReadEndElement();

                if (Debug.isDebugBuild) Debug.Log("Failed to find template for an item " + templateName);

                return null;
            }

            string strValue = reader.Name.ToLowerInvariant();
            switch (strValue)
            {
                case "tp":
                case "itype":
                case "itemtype":                
                    if (strValue == "itemtype")
                    {
                        itemType = (ItemTypes)Enum.Parse(typeof(ItemTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    }
                    else
                    {
                        itemType = (ItemTypes)reader.ReadElementContentAsInt();
                    }

                    readItemType = true;

                    switch (itemType)
                    {
                        case ItemTypes.ARMOR:
                            itemToRead = new Armor();
                            armorToRead = itemToRead as Armor;
                            eqToRead = itemToRead as Equipment;
                            break;
                        case ItemTypes.ACCESSORY:
                            itemToRead = new Accessory();
                            accessoryToRead = itemToRead as Accessory;
                            eqToRead = itemToRead as Equipment;
                            break;
                        case ItemTypes.WEAPON:
                            itemToRead = new Weapon();
                            weaponToRead = itemToRead as Weapon;
                            eqToRead = itemToRead as Equipment;
                            //Debug.Log("Created a new weapon during read");
                            break;
                        case ItemTypes.CONSUMABLE:
                            itemToRead = new Consumable();
                            consumableToRead = itemToRead as Consumable;
                            break;
                        case ItemTypes.OFFHAND:
                            itemToRead = new Offhand();
                            eqToRead = itemToRead as Equipment;
                            offhandToRead = itemToRead as Offhand;
                            break;
                        case ItemTypes.EMBLEM:
                            itemToRead = new Emblem();
                            eqToRead = itemToRead as Equipment;
                            emblemToRead = itemToRead as Emblem;
                            break;
                    }
                    itemToRead.itemType = itemType;
                    //Debug.Log("Reading an item of type " + itemType.ToString());
                    break;
                case "cv":
                    string st = reader.ReadElementContentAsString();
                    //itemToRead.challengeValue = CustomAlgorithms.TryParseFloat(st);
                    break;
                case "dreamitem":
                    itemToRead.dreamItem = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    rebuildName = true;
                    break;
                case "iref":
                case "itemref":
                    if (!readItemType)
                    {
                        itemType = ItemTypes.WEAPON; // Default.
                        itemToRead = new Weapon();
                        weaponToRead = itemToRead as Weapon;
                        eqToRead = itemToRead as Equipment;
                        itemToRead.itemType = itemType;
                    }

                    itemToRead.actorRefName = reader.ReadElementContentAsString();
                    //Debug.Log("Read: " + itemToRead.actorRefName);
                    Item template = Item.GetItemTemplateFromRef(itemToRead.actorRefName);
                    if (template == null)
                    {
                        // Let's double check that it won't work just by adjusting the ref name with our save slot.
                        string oldName = itemToRead.actorRefName;
                        itemToRead.actorRefName = itemToRead.actorRefName + "_" + GameStartData.saveGameSlot;
                        //if (Debug.isDebugBuild) Debug.Log("Reader could not find template " + oldName + ", so now searching " + itemToRead.actorRefName);
                        template = Item.GetItemTemplateFromRef(itemToRead.actorRefName);
                    }

                    if (template == null)
                    {
                        //Debug.Log("STILL could not find item template: " + itemToRead.actorRefName);
                        failedToFindTemplate = true;
                    }                    
                    else
                    {
                        //Debug.Log(template.itemType.ToString() + " about to be copied as temp " + itemToRead.itemType + " " + itemToRead.actorRefName);
                        itemToRead.CopyFromItem(template);

                        /* if (itemToRead.IsEquipment())
                        {
                            Equipment eq = template as Equipment;
                            Equipment eq2 = itemToRead as Equipment;
                            Debug.Log(eq.slot + " " + eq2.slot + " " + eq.actorRefName + " " + itemToRead.itemType);
                        } */
                        //Debug.Log(itemToRead.itemType.ToString() + " read: " + itemToRead.displayName);
                    }

                    templateName = itemToRead.actorRefName;

                    break;
                case "curuses":
                    consumableToRead.curUsesRemaining = reader.ReadElementContentAsInt();
                    break;
                case "advstats":
                    eqToRead.ReadAdventureStats(reader);
                    break;
                case "seas":
                case "seasoningattached":
                    consumableToRead.seasoningAttached = reader.ReadElementContentAsString();
                    consumableToRead.AddSeasoningToName();
                    break;
                case "ciss":
                    eqToRead.saveSlotIndexForCustomItemTemplate = reader.ReadElementContentAsInt();
                    break;
                case "upr":
                    if (itemToRead == null)
                    {
                        Debug.Log("Item being read belongs to Relic that was deleted. Not loading.");
                        while (reader.Name != "item")
                        {
                            reader.Read();
                        }
                        reader.ReadEndElement();
                        return null;
                    }
                    eqToRead.upgradesByRarity = reader.ReadElementContentAsInt();
                    break;
                case "tu":
                case "timesupgraded":
                    if (itemToRead == null)
                    {
                        Debug.Log("Item being read belongs to Relic that was deleted. Not loading.");
                        while (reader.Name != "item")
                        {
                            reader.Read();
                        }
                        reader.ReadEndElement();
                        return null;
                    }
                    eqToRead.timesUpgraded = reader.ReadElementContentAsInt();

                    if (eqToRead.timesUpgraded > 0)
                    {
                        int localTimesUpgraded = eqToRead.timesUpgraded;
                        if (eqToRead.timesUpgraded > Equipment.GetMaxUpgrades())
                        {
                            localTimesUpgraded = Equipment.GetMaxUpgrades();
                        }
                        eqToRead.challengeValue += (localTimesUpgraded * 0.15f);
                    }
                    break;
                case "modsremoved":
                    eqToRead.modsRemoved = reader.ReadElementContentAsInt();
                    break;
                /* case "modsaddedthroughdreamcaster":
                    eqToRead.modsAddedThroughDreamcaster = reader.ReadElementContentAsInt();
                    break; */
                case "maxuses":
                    consumableToRead.maxUses = reader.ReadElementContentAsInt();
                    break;
                case "qty":
                case "quantity":
                    consumableToRead.Quantity = reader.ReadElementContentAsInt();
                    break;
                case "istreeseed":
                    consumableToRead.isTreeSeed = reader.ReadElementContentAsBoolean();
                    break;
                case "itemtag":
                    ItemFilters tag = (ItemFilters)Enum.Parse(typeof(ItemFilters), reader.ReadElementContentAsString().ToUpperInvariant());
                    //Debug.Log(itemToRead.actorRefName + " read tag " + tag);
                    itemToRead.AddTag(tag);
                    break;
                case "cdturns":
                    itemToRead.cooldownTurnsRemaining = reader.ReadElementContentAsInt();
                    break;
                case "fav":
                    itemToRead.favorite = true;
                    reader.ReadElementContentAsString();
                    break;
                case "trash":
                    itemToRead.vendorTrash = true;
                    reader.ReadElementContentAsString();
                    break;
                case "favorite": // deprecated
                    txt = reader.ReadElementContentAsString();
                    txt.ToUpperInvariant();
                    itemToRead.favorite = Boolean.Parse(txt);
                    break;
                case "leg":
                case "legendary":
                    itemToRead.legendary = reader.ReadElementContentAsBoolean();
                    rebuildName = true;
                    break;
                case "id":
                case "uniqueid":
                    itemToRead.actorUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "cr":
                    itemToRead.ReadCoreActorInfo(reader);
                    break;
                case "fl":
                case "floor":
                case "dungeonfloor":
                    itemToRead.dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "mapid":
                case "actormap":
                    itemToRead.actorMapID = reader.ReadElementContentAsInt();
                    MapMasterScript.TryAssignMap(itemToRead, itemToRead.actorMapID);
                    break;
                case "pos":
                    itemToRead.ReadCurrentPosition(reader);
                    spawnPosition.x = GetPos().x;
                    spawnPosition.y = GetPos().y;
                    break;
                case "posx":
                    txt = reader.ReadElementContentAsString();
                    itemToRead.SetCurPosX(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "posy":
                    txt = reader.ReadElementContentAsString();
                    itemToRead.SetCurPosY(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "pr":
                    string price = reader.ReadElementContentAsString();
                    string[] parsed = price.Split('|');
                    if (parsed.Length == 1)
                    {
                        // no sale price
                        Int32.TryParse(parsed[0], out itemToRead.shopPrice);
                    }
                    else if (parsed.Length == 2)
                    {
                        Int32.TryParse(parsed[0], out itemToRead.shopPrice);
                        Int32.TryParse(parsed[1], out itemToRead.salePrice);
                    }
                    break;
                case "sell":
                case "saleprice":
                    itemToRead.salePrice = reader.ReadElementContentAsInt();
                    break;
                case "buy":
                case "shopprice":
                    itemToRead.shopPrice = reader.ReadElementContentAsInt();
                    break;
                case "dge":
                case "extradodge":
                    armorToRead.extraDodge = reader.ReadElementContentAsInt();
                    break;
                case "areaid":
                    itemToRead.areaID = reader.ReadElementContentAsInt();
                    break;
                case "rarity":
                    itemToRead.rarity = (Rarity)Enum.Parse(typeof(Rarity), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "rare":
                    itemToRead.rarity = (Rarity)reader.ReadElementContentAsInt();
                    rebuildName = true; // New to make sure consumables like seeds have correct visual rarity?
                    // Safety precaution.
                    if (itemToRead.IsEquipment())
                    {
                        int preExistingUpgrades = eqToRead.upgradesByRarity;
                        switch (itemToRead.rarity)
                        {
                            case Rarity.UNCOMMON:
                                if (preExistingUpgrades < 1)
                                {
                                    eqToRead.upgradesByRarity = 1;
                                }
                                break;
                            case Rarity.MAGICAL:
                                if (preExistingUpgrades < 2)
                                {
                                    eqToRead.upgradesByRarity = 2;
                                }                                
                                break;
                            case Rarity.ANCIENT:
                                if (preExistingUpgrades < 3)
                                {
                                    eqToRead.upgradesByRarity = 3;
                                }                                
                                break;
                            case Rarity.ARTIFACT:
                                if (!itemToRead.legendary)
                                {
                                    if (preExistingUpgrades < 4)
                                    {
                                        eqToRead.upgradesByRarity = 4;
                                    }                                    
                                }
                                break;
                        }
                    }
                    break;
                case "paireditem":
                    reader.ReadStartElement();
                    EQPair newPair = null;

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "mainhand":
                                newPair = new EQPair(GameMasterScript.simpleBool[reader.ReadElementContentAsInt()]);
                                break;
                            case "id":
                                newPair.itemID = reader.ReadElementContentAsInt();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    eqToRead.pairedItems.Add(newPair);

                    reader.ReadEndElement();
                    break;
                case "paireditemid":
                    // Legacy cruft.
                    int dummyVar = reader.ReadElementContentAsInt();
                    break;
                case "mds":
                    bool doRebuild = eqToRead.ReadEQMods(reader);
                    if (doRebuild)
                    {
                        rebuildName = true;
                    }
                    break;
                case "mids":
                    //Debug.Log("Reading mod IDs from an item. " + reader.NodeType + " " + reader.Name);
                    doRebuild = eqToRead.ReadEQModsFromIDs(reader);
                    if (doRebuild)
                    {
                        rebuildName = true;
                    }
                    //Debug.Log(reader.Name + " " + reader.NodeType);
                    break;
                // Deprecated
                case "mmods":
                case "magicmods":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        MagicMod mm = new MagicMod();
                        string mRefName = reader.ReadElementContentAsString();

                        bool readThisMod = true;
                        if (itemToRead.autoModRef != null)
                        {
                            foreach (string s in itemToRead.autoModRef)
                            {
                                if (s == mRefName)
                                {
                                    readThisMod = false;
                                    break;
                                }
                            }
                        }

                        if (!readThisMod)
                        {
                            continue;
                        }

                        MagicMod mmTemplate = MagicMod.FindModFromName(mRefName);
                        if (mmTemplate == null)
                        {
                            Debug.Log("Reader couldn't find mod template " + mRefName);
                        }
                        else
                        {
                            //Debug.Log("Adding mod to " + itemToRead.actorUniqueID + " " + mRefName + " cur count " + itemToRead.mods.Count);
                            mm.CopyFromMod(mmTemplate);
                        }
                        itemToRead.AddMod(mm, false);
                    }
                    reader.ReadEndElement();
                    rebuildName = true;
                    break;
                case "blck": // format is blockdamage|blockchance
                    parsed = reader.ReadElementContentAsString().Split('|');
                    offhandToRead.blockDamageReduction = CustomAlgorithms.TryParseFloat(parsed[0]);
                    offhandToRead.blockChance = CustomAlgorithms.TryParseFloat(parsed[1]);
                    break;
                case "bchance":
                case "blockchance":
                    txt = reader.ReadElementContentAsString();
                    offhandToRead.blockChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "bdmg":
                case "blockdamagereduction":
                    txt = reader.ReadElementContentAsString();
                    offhandToRead.blockDamageReduction = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "damtype": // deprecated
                    weaponToRead.damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "dtype": // deprecated
                    weaponToRead.damType = (DamageTypes)reader.ReadElementContentAsInt();
                    break;
                case "uneq":
                    eqToRead.hasBeenUnequipped = true;
                    reader.ReadElementContentAsString();
                    break;
                case "hasbeenuneq": // deprecated
                    eqToRead.hasBeenUnequipped = reader.ReadElementContentAsBoolean();
                    break;
                case "weap":
                    string unparsed = reader.ReadElementContentAsString();
                    char splitChar = '|';
                    //Debug.Log(GameStartData.loadGameVer + " check");
                    if (GameStartData.loadGameVer <= 109 && !readingFromSharedBank)
                    {
                        splitChar = ',';
                    }
                    parsed = unparsed.Split(splitChar);
                    weaponToRead.power = CustomAlgorithms.TryParseFloat(parsed[0]);
                    weaponToRead.range = Int32.Parse(parsed[1]);
                    if (parsed.Length == 3)
                    {
                        weaponToRead.damType = (DamageTypes)Int32.Parse(parsed[2]);
                    }
                    if (GameStartData.loadGameVer <= 109)
                    {
                        // This data could have been written badly due to using a comma instead of a |
                        weaponToRead.range = (GameMasterScript.masterItemList[weaponToRead.actorRefName] as Weapon).range;
                    }
                    break;
                case "power": // deprecated
                    txt = reader.ReadElementContentAsString();
                    weaponToRead.power = CustomAlgorithms.TryParseFloat(txt);
                    if (weaponToRead.power > Weapon.MAX_WEAPON_POWER)
                    {
                        weaponToRead.power = Weapon.MAX_WEAPON_POWER;
                    }

                    break;
                case "range": // deprecated
                    weaponToRead.range = reader.ReadElementContentAsInt();
                    break;
                case "dad":
                case "dictactordata":
                    itemToRead.ReadActorDict(reader);
                    break;
                case "dads":
                case "dictactordatastring":
                case "dictactordatastrings":
                    itemToRead.ReadActorDictString(reader);
                    break;
                case "res":
                case "resists":
                    eqToRead.ReadResistsFromSave(reader);                    
                    break;
                case "emblemstatuseffects":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.Name.Contains("tier_"))
                        {
                            int tierValue;
                            if (Int32.TryParse(reader.Name.Substring(5), out tierValue))
                            {
                                emblemToRead.grantedStatusEffects.Add(tierValue, reader.ReadElementContentAsString());
                            }
                            else
                            {
                                Debug.Log("Error reading emblem " + emblemToRead.actorRefName + " " + emblemToRead.actorUniqueID);
                                reader.Read();
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "jobforemblem":
                    emblemToRead.jobForEmblem = (CharacterJobs)reader.ReadElementContentAsInt();
                    //Debug.Log("Emblem: " + emblemToRead.actorUniqueID + " " + emblemToRead.jobForEmblem);
                    break;
                case "emblemlevel":
                    emblemToRead.emblemLevel = reader.ReadElementContentAsInt();
                    break;
                case "nw": // Self-closing node.
                    itemToRead.newlyPickedUp = true;
                    reader.ReadElementContentAsString();
                    break;
                case "new":
                    itemToRead.newlyPickedUp = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        //Debug.Log("end item " + reader.Name + " " + reader.NodeType.ToString());
        reader.ReadEndElement();
        //Debug.Log("end item2 " + reader.Name + " " + reader.NodeType.ToString());
        //Debug.Log("Finished reading item " + itemToRead.displayName + " " + itemToRead.refName + " " + itemToRead.itemType);
        if (itemToRead.actorRefName != "weapon_fists" && addItemToMasterDict)
        {
            GameMasterScript.AddActorToDict(itemToRead);
        }
        else
        {
            //Debug.Log("NOT adding " + itemToRead.actorUniqueID + " " + itemToRead.actorRefName + " to master dict for now.");
        }

        if (itemToRead.timesUpgraded > 0 || itemToRead.legendary || 
            (itemToRead.actorRefName == "orb_itemworld" && !String.IsNullOrEmpty(itemToRead.GetOrbMagicModRef())))
        {
            rebuildName = true;
        }

        if (itemToRead.actorRefName == "item_lucidorb_shard" || itemToRead.actorRefName == "item_monsterletter")
        {
            rebuildName = true;
        }

        if (itemToRead.customItemFromGenerator)
        {
            rebuildName = true;
        }

        if (itemToRead.IsNightmareOrb())
        {
            rebuildName = true;
        }

        if (rebuildName)
        {
            itemToRead.RebuildDisplayName();
        }

        //Debug.Log("Finished reading " + itemToRead.actorUniqueID + " " + itemToRead.actorRefName + " " + itemToRead.currentPosition);
        return itemToRead;
    }

    public override void WriteToSave(XmlWriter writer)
    {
        if (!GameMasterScript.masterItemList.ContainsKey(actorRefName))
        {
            //Debug.Log("WARNING! Cannot write " + actorRefName + " " + actorUniqueID + " as no key exists?");
            return;
        }
        if ((int)itemType != 0)
        {
            writer.WriteElementString("tp", ((int)itemType).ToString()); // was "itype"
        }        
        writer.WriteElementString("iref", actorRefName);

        if (customItemFromGenerator && saveSlotIndexForCustomItemTemplate != 99)
        {
            writer.WriteElementString("ciss", saveSlotIndexForCustomItemTemplate.ToString());
        }

        if (newlyPickedUp && collection != null && collection.Owner != null && collection.Owner.GetActorType() == ActorTypes.HERO)
        {
            writer.WriteStartElement("nw");
            writer.WriteFullEndElement();
        }

        if (challengeValue != GameMasterScript.masterItemList[actorRefName].challengeValue)
        {
            writer.WriteElementString("cv", challengeValue.ToString());
        }
        bool isInCollection = true;

        if (collection == null || dungeonFloor == MapMasterScript.TOWN2_MAP_FLOOR)
        {
            isInCollection = false;
            WriteCurrentPosition(writer);
        }
        if (favorite)
        {
            writer.WriteStartElement("fav");
            writer.WriteFullEndElement();
        }
        if (vendorTrash)
        {
            writer.WriteStartElement("trash");
            writer.WriteFullEndElement();
        }
        if (dreamItem)
        {
            //Item template = GameMasterScript.masterItemList[actorRefName];
            //if (!template.dreamItem)
            {
                writer.WriteElementString("dreamitem", "1");
            }
        }
        WritePrice(writer);
        if (isInCollection)
        {
            writer.WriteElementString("id", actorUniqueID.ToString());
        }
        else
        {
            WriteCoreActorInfo(writer);
        }
        if (rarity != Rarity.COMMON)
        {
            writer.WriteElementString("rare", ((int)rarity).ToString());
        }
        if (legendary)
        {
            writer.WriteElementString("leg", legendary.ToString().ToLowerInvariant());
        }
        WriteActorDict(writer);
    }

    public Item()
    {
        Init();
    }

    public int CalculateSalePrice(bool forceRecalculateShopPrice = true)
    {
        if (salePrice != 0)
        {
            return salePrice;
        }
        int sell = CalculateShopPrice(1.00f, forceRecalculateShopPrice);
        float sellV = sell * 0.12f;
        if (sellV <= 1f)
        {
            sellV = 1f;
        }
        salePrice = (int)sellV;
        return salePrice;
    }

    public int CalculateShopPrice(float vMult, bool forceRecalculate = true, bool useBaseCost = false)
    {
        int preExistingShopPrice = shopPrice;
        if (shopPrice != 0)
        {
            if (!forceRecalculate)
            {
                return shopPrice;
            }
        }

        if (itemType == ItemTypes.CONSUMABLE)
        {
            if (actorRefName.Contains("gem"))
            {
                //Debug.Log("Price is " + shopPrice + " so just return that.");
                return shopPrice;
            }
        }

        //Debug.Log(actorRefName + " shop price is 0. ");
        //float basePrice = 50f + ((challengeValue*100f)-100f)*2f;
        float basePrice = 50f;

        if (challengeValue == 1.0f)
        {
            basePrice = 60f;
        }
        else if (challengeValue > 1.1f && challengeValue < 1.2f)
        {
            basePrice = 100f;
        }
        else if (challengeValue >= 1.2f && challengeValue < 1.3f)
        {
            basePrice = 170f;
        }
        else if (challengeValue >= 1.3f && challengeValue < 1.4f)
        {
            basePrice = 250f;
        }
        else if (challengeValue >= 1.4f && challengeValue < 1.5f)
        {
            basePrice = 350f;
        }
        else if (challengeValue >= 1.5f && challengeValue < 1.6f)
        {
            basePrice = 500f;
        }
        else if (challengeValue >= 1.6f && challengeValue < 1.7f)
        {
            basePrice = 750f;
        }
        else if (challengeValue >= 1.7f)
        {
            basePrice = 1200f;
        }

        switch (itemType)
        {
            case ItemTypes.CONSUMABLE:
                basePrice += 50f;
                break;
            case ItemTypes.WEAPON:
                basePrice *= 2f;
                break;
            case ItemTypes.OFFHAND:
                basePrice *= 2.5f;
                break;
            case ItemTypes.ARMOR:
                basePrice *= 3f;
                break;
            case ItemTypes.ACCESSORY:
                basePrice *= 3.5f;
                break;
            case ItemTypes.EMBLEM:
                basePrice *= 5f;
                break;
        }

        switch (rarity)
        {
            case Rarity.COMMON:
                break;
            case Rarity.UNCOMMON:
                basePrice *= 1.75f;
                break;
            case Rarity.MAGICAL:
                basePrice *= 3f;
                break;
            case Rarity.ANCIENT:
                basePrice *= 3.5f;
                break;
            case Rarity.ARTIFACT:
                basePrice *= 4f;
                break;
            case Rarity.LEGENDARY:
            case Rarity.GEARSET:
                basePrice *= 5f;
                break;
        }

        if (legendary && !customItemFromGenerator)
        {
            basePrice *= 2f;
        }

        if (useBaseCost && preExistingShopPrice > 0)
        {
            basePrice = preExistingShopPrice;
        }

        basePrice *= vMult;

        shopPrice = (int)basePrice;

        return shopPrice;
    }

    public static Item GetItemTemplateFromRef(string refName)
    {

        return GameMasterScript.GetItemFromRef(refName);
    }

    public bool HasModByRef(string modRef)
    {
        foreach (MagicMod mod in mods)
        {
            if (modRef == mod.refName)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasMod(MagicMod mm)
    {
        foreach (MagicMod mod in mods)
        {
            if (mm.modName == mod.refName)
            {
                return true;
            }
        }
        return false;
    }


    // Newlines for magic mods?
    public float GetBaseItemRank()
    {
        return GetItemTemplateFromRef(actorRefName).challengeValue;
    }

    public float GetConvertedItemRankFloat()
    {
        float returnValue = challengeValue;
        if (returnValue >= 3.0f)
        {
            returnValue = 1.0f;
        }
        return returnValue;
    }

    public static float ConvertRankToChallengeValue(int rank)
    {
        return (1f + ((rank - 1f) * 0.1f));
    }

    public string GetConvertedItemRank()
    {
        float baseCV = GetItemTemplateFromRef(actorRefName).challengeValue;

        // Use local CV instead?

        baseCV = challengeValue;

        return BalanceData.ConvertChallengeValueToRank(baseCV).ToString();
    }

    public string GetRarityString()
    {
        switch (rarity)
        {
            case Rarity.COMMON:
            default:
                return UIManagerScript.silverHexColor + StringManager.GetString("misc_rarity_0") + "</color>";
            case Rarity.UNCOMMON:
                return UIManagerScript.blueHexColor + StringManager.GetString("misc_rarity_1") + "</color>";
            case Rarity.MAGICAL:
                return "<color=yellow>" + StringManager.GetString("misc_rarity_2") + "</color>";
            case Rarity.ANCIENT:
                return UIManagerScript.orangeHexColor + StringManager.GetString("misc_rarity_3") + "</color>";
            case Rarity.ARTIFACT:
                return UIManagerScript.goldHexColor + StringManager.GetString("misc_rarity_4a") + "</color>";
            case Rarity.LEGENDARY:
                string legStr = customItemFromGenerator ? "exp_rarity_relic" : "misc_rarity_4b";
                return GetLegendaryColor() + StringManager.GetString(legStr) + "</color>";
            case Rarity.GEARSET:
                return UIManagerScript.greenHexColor + StringManager.GetString("misc_rarity_5") + "</color>";
        }
    }

    public string GetLegendaryColor()
    {
        return (customItemFromGenerator || ReadActorData("exprelic") == 1) ? UIManagerScript.customLegendaryColor : UIManagerScript.lightPurpleHexColor;
    }

    public string GetItemInformationNoName(bool includeFlavorText)
    {
        if (SharaModeStuff.IsSharaModeActive())
        {
            includeFlavorText = false; // since we don't have flavor text for shara.
        }

        string construct = UIManagerScript.brightOrangeHexColor + StringManager.GetString("ui_item_rank") + ": " + GetConvertedItemRank() + "</color> " + StringManager.GetString("ui_corral_header_rarity") + ": " + GetRarityString() + "\n";

        newlyPickedUp = false;

        switch (itemType)
        {
            case ItemTypes.WEAPON:
                Weapon wpn = this as Weapon;

                float usePower = wpn.power;

                bool unarmed1 = false;
                bool unarmed2 = false;
                bool offhandEmpty = false;

                if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(this))
                {
                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_unarmedfighting1"))
                    {
                        unarmed1 = true;
                        usePower = CombatManagerScript.CalculateBudokaWeaponPower(GameMasterScript.heroPCActor, 1);
                        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_unarmedfighting2"))
                        {
                            unarmed2 = true;
                        }
                    }
                }
                if (GameMasterScript.heroPCActor.myEquipment.GetOffhand() == null 
                    || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(GameMasterScript.heroPCActor.myEquipment.GetOffhandWeapon()))
                {
                    offhandEmpty = true;
                }

                bool asceticWrap = false;

                if (unarmed1 && GameMasterScript.heroPCActor.myEquipment.GetOffhand() != null && 
                    (GameMasterScript.heroPCActor.myEquipment.GetOffhand().actorRefName == "offhand_leg_ascetic_wrap" 
                    || GameMasterScript.heroPCActor.myEquipment.GetOffhand().HasModByRef("mm_budokavalid")
                    || GameMasterScript.heroPCActor.myEquipment.GetOffhand().HasModByRef("mm_asceticgrab")))
                {
                    asceticWrap = true;
                }

                int displayPower = ((int)(usePower * 10f));

                string wType = "(" + Weapon.weaponTypesVerbose[(int)wpn.weaponType] + ") ";

                if (wpn.damType != DamageTypes.PHYSICAL)
                {
                    string elemStr = StringManager.GetString("misc_dmg_" + wpn.damType.ToString().ToLowerInvariant());
                    construct += StringManager.GetString("ui_equipment_weaponpower") + ": <color=yellow>" + displayPower + " " + elemStr + "</color> " + wType;
                }
                else
                {
                    construct += StringManager.GetString("ui_equipment_weaponpower") + ": <color=yellow>" + displayPower + "</color> " + wType;
                }

                // Dual wield stuff
                bool isDualWielding = false;
                if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() != null 
                    && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(GameMasterScript.heroPCActor.myEquipment.GetWeapon()))
                {
                    if (GameMasterScript.heroPCActor.myEquipment.GetOffhandWeapon() != null 
                        && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(GameMasterScript.heroPCActor.myEquipment.GetOffhandWeapon()))
                    {
                        isDualWielding = true;
                    }
                }

                // Fist stuff.

                if (unarmed1 && (offhandEmpty || asceticWrap))
                {
                    float offhandPower = CombatManagerScript.CalculateBudokaWeaponPower(GameMasterScript.heroPCActor, 1) * 0.5f;
                    int accuracy = -50;
                    string attackName = StringManager.GetString("desc_unarmed_attack1");
                    if (unarmed2)
                    {
                        offhandPower = CombatManagerScript.CalculateBudokaWeaponPower(GameMasterScript.heroPCActor, 2) * 0.65f;
                        accuracy = -35;
                        attackName = StringManager.GetString("desc_unarmed_attack2");
                    }
                    displayPower = (int)(offhandPower * 10f);

                    construct += "\n" + attackName + ": <color=yellow>" + displayPower + "</color> " + UIManagerScript.redHexColor + "(" + accuracy + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_accuracy") + ")</color>";
                }

                if (isDualWielding)
                {
                    if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() == wpn)
                    {
                        float damagePenalty = (1f - GameMasterScript.heroPCActor.cachedBattleData.mainhandDamageMod) * 100f;
                        int damagePenaltyDisplay = (int)damagePenalty;
                        float accuracyPenalty = (1f - GameMasterScript.heroPCActor.cachedBattleData.mainhandAccuracyMod) * 100f;
                        int accPenaltyDisplay = (int)accuracyPenalty;

                        if (damagePenaltyDisplay != 0 || accPenaltyDisplay != 0)
                        {
                            // Do nothing
                            construct += "\n<color=yellow>" + StringManager.GetString("desc_dualwield_penalty") + ":</color> " + UIManagerScript.redHexColor +
                                "-" + damagePenaltyDisplay + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("ui_damage_shorthand") + ", -" +
                                accPenaltyDisplay + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_accuracy") + "</color>";
                        }
                    }
                    else if (GameMasterScript.heroPCActor.myEquipment.GetOffhandWeapon() == wpn)
                    {
                        float damagePenalty = (1f - GameMasterScript.heroPCActor.cachedBattleData.offhandDamageMod) * 100f;
                        int damagePenaltyDisplay = (int)damagePenalty;
                        float accuracyPenalty = (1f - GameMasterScript.heroPCActor.cachedBattleData.offhandAccuracyMod) * 100f;
                        int accPenaltyDisplay = (int)accuracyPenalty;
                        construct += "\n<color=yellow>" + StringManager.GetString("desc_dualwield_penalty") + ":</color> " + UIManagerScript.redHexColor +
                            "-" + damagePenaltyDisplay + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("ui_damage_shorthand") + ", -" +
                            accPenaltyDisplay + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_accuracy") + "</color>";
                    }
                }

                if (!String.IsNullOrEmpty(Weapon.weaponProperties[(int)wpn.weaponType]))
                {
                    construct += "\n" + UIManagerScript.orangeHexColor + Weapon.weaponProperties[(int)wpn.weaponType] + "</color>";
                }

                if (wpn.range > 1)
                {
                    construct += "\n" + StringManager.GetString("desc_weaponproperty_range") + ": <color=yellow>" + wpn.range + "</color>";
                    if (wpn.isRanged)
                    {
                        construct += " (" + StringManager.GetString("desc_weaponproperty_projectile") + ")";
                    }
                    construct += "\n";
                }
                if (wpn.twoHanded)
                {
                    construct += "\n<color=yellow>" + StringManager.GetString("desc_weaponproperty_2handed") + " </color>";
                }
                break;
        }
        if (itemType == ItemTypes.ARMOR || itemType == ItemTypes.EMBLEM || itemType == ItemTypes.WEAPON || itemType == ItemTypes.ACCESSORY || itemType == ItemTypes.OFFHAND)
        {
            Equipment eq = (Equipment)this as Equipment;
            if (this.itemType == ItemTypes.ARMOR)
            {
                Armor ar = eq as Armor;
                construct += "<color=yellow>" + Armor.armorTypesVerbose[(int)ar.armorType] + "</color>\n";
                if (Armor.armorProperties[(int)ar.armorType] != "")
                {
                    construct += UIManagerScript.orangeHexColor + Armor.armorProperties[(int)ar.armorType] + "</color>\n";
                }
            }
            else if (this.itemType != ItemTypes.WEAPON)
            {
                //construct += "";
            }

            if (itemType == ItemTypes.OFFHAND)
            {
                Offhand oh = this as Offhand;
                if (oh.blockChance > 0f)
                {
                    int disp = (int)(oh.blockChance * 100);
                    float baseReduction = oh.blockDamageReduction;
                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_paladinblockbuff"))
                    {
                        baseReduction -= 0.15f;
                    }
                    int damageBlocked = (int)((1f - baseReduction) * 100);
                    StringManager.SetTag(0, disp.ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                    StringManager.SetTag(1, damageBlocked.ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                    construct += StringManager.GetString("ui_shieldinfo") + "\n";
                }
            }

            float resPercent = 1;
            float resFlat = 0;
            bool allElementsSame = true;
            bool firstResist = true;
            int numElements = 0;

            foreach (ResistanceData rd in eq.resists)
            {
                if (rd.damType == DamageTypes.PHYSICAL)
                {
                    if (rd.multiplier != 1.0f)
                    {
                        float pAmt = (1 - rd.multiplier) * 100f;
                        construct += UIManagerScript.greenHexColor + (int)pAmt + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color> ";
                        construct += GameMasterScript.elementNames[(int)rd.damType] + " " + StringManager.GetString("misc_abbreviation_resistance");
                        if (rd.flatOffset != 0)
                        {
                            construct += ", ";
                        }
                    }
                    if (rd.flatOffset != 0)
                    {
                        StringManager.SetTag(0, UIManagerScript.greenHexColor + "-" + (int)rd.flatOffset + "</color>");
                        StringManager.SetTag(1, GameMasterScript.elementNames[(int)rd.damType]);
                        construct += StringManager.GetString("misc_reduce_damage_elem") + " ";
                    }
                }
            }

            string constructBackup = construct;
            bool flatElementsSame = true;
            string percentConstruct = "";
            string flatConstruct = "";

            foreach (ResistanceData rd in eq.resists)
            {
                //Debug.Log(rd.damType + " " + rd.multiplier + " " + " " + rd.flatOffset + " " + allElementsSame + " " + firstResist + " " + resPercent + " " + resFlat);
                //numElements++;
                if (rd.multiplier != 1.0f)
                {
                    float pAmt = (1 - rd.multiplier) * 100f;

                    if (rd.damType != DamageTypes.PHYSICAL)
                    {
                        percentConstruct += UIManagerScript.greenHexColor + (int)pAmt + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color> ";
                        percentConstruct += GameMasterScript.elementNames[(int)rd.damType] + " " + StringManager.GetString("misc_abbreviation_resistance") + " ";
                        if (rd.flatOffset != 0)
                        {
                            //construct += ", ";
                        }
                    }


                    if (rd.damType != DamageTypes.PHYSICAL)
                    {
                        numElements++;
                        if (firstResist)
                        {
                            resPercent = rd.multiplier;
                        }
                        else
                        {
                            if (rd.multiplier != resPercent)
                            {
                                //Debug.Log(rd.multiplier + " is NOT equal to " + resPercent + " so all elems are not the same.");
                                allElementsSame = false;
                            }
                        }
                    }

                }
                else if ((resPercent != 1.0f) && (rd.damType != DamageTypes.PHYSICAL))
                {
                    //Debug.Log("Res multiplier mismatch. 1.0f for this, but " + resPercent);
                    allElementsSame = false;
                }

                if (rd.flatOffset != 0)
                {
                    if (rd.damType != DamageTypes.PHYSICAL)
                    {
                        StringManager.SetTag(0, UIManagerScript.greenHexColor + "-" + (int)rd.flatOffset + "</color>");
                        StringManager.SetTag(1, GameMasterScript.elementNames[(int)rd.damType]);
                        flatConstruct += StringManager.GetString("misc_reduce_damage_elem") + " ";

                        if (firstResist)
                        {
                            resFlat = rd.flatOffset;
                            //Debug.Log("Set flat res to " + resFlat + " " + rd.damType);
                        }
                        else if (resFlat != rd.flatOffset)
                        {
                            //Debug.Log(rd.flatOffset + " is NOT equal to " + resFlat + " so all elems are not the same.");
                            flatElementsSame = false;
                        }
                    }

                }
                else if (resFlat != 0 && rd.damType != DamageTypes.PHYSICAL)
                {
                    //allElementsSame = false;
                }
                if (rd.damType != DamageTypes.PHYSICAL)
                {
                    firstResist = false;
                }

            }

            if (numElements < 5)
            {
                //Debug.Log("Num elements is " + numElements + " so not same");
                allElementsSame = false;
                flatElementsSame = false;
            }

            //Debug.Log(resFlat + " " + flatElementsSame + " " + allElementsSame);

            if (allElementsSame)
            {
                construct = constructBackup;

                /* if (hasPhys)
                {
                    construct += ",";
                } */
                if (resPercent != 1.0f)
                {
                    float pAmt = (1 - resPercent) * 100f;
                    StringManager.SetTag(0, UIManagerScript.greenHexColor + pAmt + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>");
                    construct += StringManager.GetString("misc_resist_elemental") + " ";
                }
            }
            else
            {
                //Debug.Log("All elements are not the same");
                construct += percentConstruct;
            }

            if (resFlat != 0 && percentConstruct != "")
            {
                construct += ", ";
            }

            if (resFlat != 0 && flatElementsSame)
            {
                StringManager.SetTag(0, UIManagerScript.greenHexColor + "-" + resFlat + "</color>");

                construct += StringManager.GetString("misc_modify_elemental");
                //construct += UIManagerScript.greenHexColor + "-" + resFlat + "</color> elemental dmg";
            }
            else if (resFlat != 0 && !flatElementsSame)
            {
                construct += flatConstruct;
            }

            if (eq.resists.Count > 0)
            {
                construct += "\n";
            }

            // GetAddedAbilities assigns a static dictionary to Item, so we can reference that.
            if (eq.GetAddedAbilities().Count > 0 && !eq.AnnounceAnyAddedAbilities())
            {
                construct += "\n";
                foreach (AbilityScript abil in Equipment.abilitiesFromItem.Keys)
                {
                    if (Equipment.abilitiesFromItem[abil]) continue; // Don't announce this.
                    StringManager.SetTag(0, UIManagerScript.cyanHexColor + abil.abilityName + "</color>");
                    construct += StringManager.GetString("desc_grants_skill") + "\n";
                    //construct += "Grants Ability: <color=yellow>" + abil.abilityName + "</color>\n";
                }
            }
        }

        if (itemType == ItemTypes.CONSUMABLE)
        {
            Consumable con = this as Consumable;
            bool possiblyOrbShard = true;
            if (con.isHealingFood)
            {
                if (con.seasoningAttached == "")
                {
                    construct += "\n<color=yellow>" + con.EstimateFoodHealing() + "</color>\n\n";
                }
                else if (con.seasoningAttached != "")
                {
                    construct += "\n<color=yellow>" + con.EstimateFoodHealing() + "</color>\n";
                }
                possiblyOrbShard = false;
            }
            if (con.isDamageItem)
            {
                construct += "\n<color=yellow>" + con.EstimateItemDamage() + "</color>\n\n";
                possiblyOrbShard = false;
            }
            if (con.effectDescription != "")
            {
                string parsedEffectDescription = CustomAlgorithms.ParseItemDescStuff(con.effectDescription);
                construct += UIManagerScript.cyanHexColor + parsedEffectDescription + "</color>\n\n"; // Extra item effect desc
            }

            string fullAmount = "";
            fullAmount = con.GetFoodFullTurns();
            if (!String.IsNullOrEmpty(fullAmount))
            {
                construct += fullAmount + "\n";
            }
            if (con.seasoningAttached != "" && !con.seasoning)
            {
                Item template = Item.GetItemTemplateFromRef(con.seasoningAttached);
                if (template != null)
                {
                    construct += "\n" + UIManagerScript.cyanHexColor + template.extraDescription + "</color>\n\n";
                }
            }

            if (possiblyOrbShard && con.actorRefName == "item_lucidorb_shard")
            {
                string mRef = GetOrbMagicModRef();
                if (!string.IsNullOrEmpty(mRef))
                {
                    MagicMod template = MagicMod.FindModFromName(GetOrbMagicModRef());
                    construct += "\n" + StringManager.GetString("lucidorb_shard_mmdesc") + " " + template.GetDescription() + "\n\n";
                }
                else
                {
                    construct += "\n" + StringManager.GetString("lucidorb_shard_mmdesc") + "\n\n";
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(extraDescription))
            {
                construct += "\n" + UIManagerScript.cyanHexColor + extraDescription + "</color>\n";
            }
        }

        if (mods.Count > 0 && itemType == ItemTypes.WEAPON)
        {
            construct += "\n";
        }

        string gear = null;

        if (IsEquipment())
        {
            Equipment eq = this as Equipment;
            construct += eq.GetMagicModDescriptionText();
            if (eq.gearSet != null)
            {
                gear = UIManagerScript.greenHexColor + eq.gearSet.description + "</color>\n";
            }
        }
        else
        {
            if (actorRefName == "orb_itemworld")
            {
                string mmRef = GetOrbMagicModRef();
                if (!string.IsNullOrEmpty(mmRef))
                {
                    MagicMod mmTemplate = MagicMod.FindModFromName(mmRef);
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
                        case EquipmentSlots.EMBLEM:
                            StringManager.SetTag(0, StringManager.GetString("eq_slot_emblem"));
                            break;
                        case EquipmentSlots.OFFHAND:
                            StringManager.SetTag(0, StringManager.GetString("eq_slot_offhand"));
                            break;
                    }
                    construct += "<color=yellow>" + StringManager.GetString("ui_hover_orb_readout") + "</color>\n\n";
                    construct += UIManagerScript.greenHexColor + mmTemplate.GetDescription(this) + "</color>\n\n";

                }
            }
        }

        string desc = "";

        int constructLength = construct.Length;
        
        // Don't include flavor text if the item display is too huge due to mods, stats, etc.
        int numLineBreaks = constructLength - construct.Replace("\n", "").Length;
        if (includeFlavorText && numLineBreaks < 10)
        {
            desc = CustomAlgorithms.ParseItemDescStuff(description);
        }
        if (constructLength > 600)
        {
            construct = construct.Replace(StringManager.GetString("misc_nonstackable"), StringManager.GetString("misc_nonstackable_abbr"));
        }
        //Debug.Log(constructLength + " " + numLineBreaks);

        if (dreamItem)
        {
            construct += "\n" + UIManagerScript.redHexColor + StringManager.GetString("desc_dreamitem") + "</color>\n";
        }
        if (itemType == ItemTypes.EMBLEM)
        {
            construct += "\n" + StringManager.GetString("misc_item_bound") + "\n";
        }

        if (mods.Count > 0)
        {
            if (gear != null)
            {
                construct += "\n" + gear;
            }
            construct += "\n" + desc;
        }
        else if (itemType != ItemTypes.CONSUMABLE && itemType != ItemTypes.ACCESSORY) // No mods.
        {
            if (gear != null)
            {
                construct += "\n\n" + gear;
            }
            construct += "\n\n" + desc;
        }
        else
        {
            if (gear != null)
            {
                construct += gear;// + "\n\n";
            }
            construct += desc;
        }

        if (IsEquipment())
        {
            Equipment eq = this as Equipment;
            if (eq.gearSet != null)
            {
                construct += "\n\n<color=yellow>" + StringManager.GetString("ui_item_partofset") + ": </color>" + UIManagerScript.greenHexColor + eq.gearSet.displayName + "</color>";
            }

            foreach (EQPair pair in eq.pairedItems)
            {
                if (pair.eq != null)
                {
                    construct += "\n" + UIManagerScript.silverHexColor + StringManager.GetString("ui_eq_paired") + pair.eq.displayName + "</color>";
                }                
            }
        }

        if (DebugConsole.IsOpen)
        {
            construct += "\n\nID: " + actorUniqueID;
        }

        construct = construct.Replace("\n\n\n\n", "\n\n");
        construct = construct.Replace("\n\n\n", "\n\n");
        construct = construct.TrimEnd('\n');

        return construct;
    }

    public void TryAddMod(MagicMod mm)
    {
        if (mods.Contains(mm))
        {
            Debug.Log("Can't double add mod " + mm.refName + " to " + actorRefName + " " + actorUniqueID);
            return;
        }
        foreach (MagicMod checkMM in mods)
        {
            if (checkMM.refName == mm.refName)
            {
                Debug.Log("Can't double add mod " + mm.refName + " to " + actorRefName + " " + actorUniqueID);
                return;
            }
        }
        mods.Add(mm);
    }

    public void RemoveMod(string modRef)
    {
        MagicMod modToRemove = null;
        foreach (MagicMod mm in mods)
        {
            if (mm.refName == modRef)
            {
                modToRemove = mm;
            }
        }
        if (modToRemove == null)
        {
            Debug.Log("Couldn't find mod " + modRef + " in " + actorRefName + " " + displayName + " " + actorUniqueID);
            return;
        }

        mods.Remove(modToRemove);
        Weapon thisWeap = null;
        if (itemType == ItemTypes.WEAPON)
        {
            thisWeap = (Weapon)this as Weapon;
        }

        if ((modToRemove.changeBlock != 0) && (itemType == ItemTypes.OFFHAND))
        {
            Offhand oh = this as Offhand;
            oh.blockChance -= modToRemove.changeBlock;
        }

        if (modToRemove.resists.Count > 0)
        {
            Equipment eq = this as Equipment;
            foreach (ResistanceData rd in modToRemove.resists)
            {
                eq.ModifyResistMult(rd.damType, -1f * rd.multiplier); // this was 1- before, which seemed wrong.
                eq.ModifyResistOffset(rd.damType, -1f * rd.flatOffset);
                if (rd.absorb)
                {
                    eq.SetResistAbsorb(rd.damType, false);
                }

            }
        }

        if (((modToRemove.changePower != 0) || (modToRemove.changeDurability != 0)) && (thisWeap != null))
        {
            //Debug.Log("Trying to add mod " + mm.modName + " to " + displayName);
            float changePowerAmount = 0;
            if (modToRemove.changePowerAsPercent)
            {
                changePowerAmount = modToRemove.changePower * thisWeap.power;
            }
            else
            {
                changePowerAmount = modToRemove.changePower;
            }
            thisWeap.power -= changePowerAmount;
        }
        if (thisWeap != null)
        {
            if ((modToRemove.changeDamageType == DamageTypes.FIRE) || (modToRemove.changeDamageType == DamageTypes.WATER) || (modToRemove.changeDamageType == DamageTypes.POISON) || (modToRemove.changeDamageType == DamageTypes.LIGHTNING) || (modToRemove.changeDamageType == DamageTypes.SHADOW))
            {
                thisWeap.damType = DamageTypes.PHYSICAL;
            }
        }


    }

    public void AddModByRef(string modRef, bool changeStats)
    {
        MagicMod addMod = MagicMod.FindModFromName(modRef);
        AddMod(addMod, changeStats);
    }

    public void AddMod(MagicMod mm, bool changeStats)
    {
        if (mods.Contains(mm))
        {
            Debug.Log("Can't double add mod " + mm.refName + " to " + actorRefName + " " + actorUniqueID);
            return;
        }
        foreach (MagicMod checkMM in mods)
        {
            if (checkMM.refName == mm.refName)
            {
                Debug.Log("Can't double add mod " + mm.refName + " to " + actorRefName + " " + actorUniqueID);
                return;
            }
        }

        mods.Add(mm);
        Weapon thisWeap = null;
        if (itemType == ItemTypes.WEAPON)
        {
            thisWeap = (Weapon)this as Weapon;
        }

        if (mm.changeBlock != 0 && itemType == ItemTypes.OFFHAND && changeStats)
        {
            Offhand oh = this as Offhand;
            oh.blockChance += mm.changeBlock;
        }

        if (mm.resists.Count > 0)
        {
            Equipment eq = this as Equipment;
            foreach (ResistanceData rd in mm.resists)
            {
                eq.ModifyResistMult(rd.damType, rd.multiplier);
                eq.ModifyResistOffset(rd.damType, rd.flatOffset);
                eq.SetResistAbsorb(rd.damType, rd.absorb);
            }
        }

        if ((mm.changePower != 0 || mm.changeDurability != 0) && thisWeap != null && changeStats)
        {
            //Debug.Log("Trying to add mod " + mm.modName + " to " + displayName);
            float changePowerAmount = 0;
            if (mm.changePowerAsPercent)
            {
                changePowerAmount = mm.changePower * thisWeap.power;
            }
            else
            {
                changePowerAmount = mm.changePower;
            }
            thisWeap.power += changePowerAmount;
            float changeDurabilityAmount = 0;
            if (mm.changeDurabilityAsPercent)
            {
                changeDurabilityAmount = mm.changeDurability * thisWeap.maxDurability;
            }
            else
            {
                changeDurabilityAmount = mm.changeDurability;
            }
            thisWeap.maxDurability += (int)changeDurabilityAmount;
            thisWeap.curDurability += (int)changeDurabilityAmount;
        }
        if (thisWeap != null)
        {
            if ((mm.changeDamageType == DamageTypes.FIRE) || (mm.changeDamageType == DamageTypes.WATER) || (mm.changeDamageType == DamageTypes.POISON) || (mm.changeDamageType == DamageTypes.LIGHTNING) || (mm.changeDamageType == DamageTypes.SHADOW))
            {
                thisWeap.damType = mm.changeDamageType;
            }
        }

    }

    // I should have named this CopyFromTemplate
    public virtual void CopyFromItem(Item i1)
    {
        displayName = i1.displayName;        
        actorRefName = i1.actorRefName;
        prefab = i1.prefab;
        actorUniqueID = i1.actorUniqueID;
        shopPrice = i1.shopPrice;
        salePrice = i1.salePrice;
        autoModRef = i1.autoModRef;
        challengeValue = i1.challengeValue;
        itemType = i1.itemType;
        collection = i1.collection;
        spriteEffect = i1.spriteEffect;
        spriteRef = i1.spriteRef;
        rarity = i1.rarity;
        defaultTemplateRarity = i1.defaultTemplateRarity;
        description = i1.description;
        legendary = i1.legendary;
        extraDescription = i1.extraDescription;
        dreamItem = i1.dreamItem;
        autoAddToShopTables = i1.autoAddToShopTables;
        forceAddToLootTablesAtRate = i1.forceAddToLootTablesAtRate;
        scriptOnAddToInventory = i1.scriptOnAddToInventory;
        scriptBuildDisplayName = i1.scriptBuildDisplayName;
        customItemFromGenerator = i1.customItemFromGenerator;
        if (legendary)
        {
            rarity = Rarity.LEGENDARY;
            if (IsEquipment())
            {
                Equipment eq = this as Equipment;
                if (eq != null)
                {
                    if (eq.gearSet != null)
                    {
                        //Debug.Log("Read " + eq.gearSet.refName);
                        rarity = Rarity.GEARSET;
                    }
                }
            }
        }
        mods.Clear();
        foreach (MagicMod mm in i1.mods)
        {
            MagicMod newMod = new MagicMod();
            newMod.CopyFromMod(mm);
            TryAddMod(newMod);
        }
        foreach (ItemFilters tag in i1.tags)
        {
            AddTag(tag);
        }
        if (i1.dictActorData != null)
        {
            foreach (var kvp in i1.dictActorData)
            {
                SetActorData(kvp.Key, kvp.Value);
            }
        }

        RebuildStrippedName();
    }

    public virtual bool ValidateEssentialProperties()
    {       
        if (string.IsNullOrEmpty(actorRefName))
        {
            Debug.LogError("Item has no ref name.");
            return false;
        }
        if (string.IsNullOrEmpty(displayName) && challengeValue >= 1.0f && challengeValue <= MAX_STARTING_CHALLENGE_VALUE)
        {
            Debug.LogError("Item ref " + actorRefName + " has no display name and is potentially findable by player? CV " + challengeValue);
            return false;
        }
        if (string.IsNullOrEmpty(description) && challengeValue >= 1.0f && challengeValue <= MAX_STARTING_CHALLENGE_VALUE)
        {
            Debug.LogError("Item ref " + actorRefName + " has no description.");
            return false;
        }
        if (string.IsNullOrEmpty(spriteRef) && challengeValue >= 1.0f && challengeValue <= MAX_STARTING_CHALLENGE_VALUE)
        {
            Debug.LogError("Item ref " + actorRefName + " has no sprite reference.");
            return false;
        }
        if (!string.IsNullOrEmpty(spriteRef) && spriteRef.Contains("assorteditems"))
        {
            string onlyNumber = spriteRef.Replace("assorteditems_", "");
            int spriteNum;
            if (Int32.TryParse(onlyNumber, out spriteNum))
            {
                if (spriteNum < 0 || spriteNum > CORE_ITEM_SPRITESHEET_MAXSIZE)
                {
                    Debug.LogError("Item ref " + actorRefName + " sprite ref number " + spriteNum + " is below 0 or exceeds max in sheet of " + CORE_ITEM_SPRITESHEET_MAXSIZE);
                    return false;
                }
            }
        }
        if ((challengeValue < 1.0 || challengeValue > Item.MAX_STARTING_CHALLENGE_VALUE) && forceAddToLootTablesAtRate > 0)
        {
            Debug.Log("Item ref " + actorRefName + " has a challenge value (rank) that may be too low or high. Player-findable items should be between 1.0 and 1.9.");
        }        
        if (forceAddToLootTablesAtRate < 0)
        {
            Debug.LogError("Item ref " + actorRefName + " drop rate cannot be below 0. Try a value like 100 (average)");
            return false;
        }

        return true;
    }

    public virtual bool TryReadFromXml(XmlReader reader)
    {
        string txt;

        //Debug.Log(reader.Name + " " + reader.NodeType);

        switch (reader.Name)
        {
            case "DisplayName":
                displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                return true;
            case "LoadOnlyIfLocalized":
                loadOnlyIfLocalized = true;
                reader.Read();
                return true;
            case "DefaultActorData":
                // format: key|value
                string unparsed = reader.ReadElementContentAsString();
                string[] parsed = unparsed.Split('|');
                SetActorData(parsed[0], Int32.Parse(parsed[1]));
                return true;
            case "DefaultActorDataString":
                // format: key|value
                unparsed = reader.ReadElementContentAsString();
                parsed = unparsed.Split('|');
                SetActorDataString(parsed[0], parsed[1]);
                return true;
            case "Script_BuildDisplayName":
                scriptBuildDisplayName = reader.ReadElementContentAsString();
                return true;
            case "ReplaceRef":
                if (GameMasterScript.simpleBool[reader.ReadElementContentAsInt()])
                {
                    replaceExistingRef = true;
                }
                return true;
            case "AddToShopRef":
                string unparsedShop = reader.ReadElementContentAsString();
                parsed = unparsedShop.Split(',');
                if (parsed.Length == 2)
                {
                    int qty;
                    if (Int32.TryParse(parsed[1], out qty))
                    {
                        addToShopRefs.Add(parsed[0], qty);
                    }
                }

                return true;
            case "RefName":
                actorRefName = reader.ReadElementContentAsString();

                if (String.IsNullOrEmpty(actorRefName))
                {
                    Debug.Log("Trying to add item with no refname? " + displayName);
                }
                if (this == null)
                {
                    Debug.Log("Trying to add null item to dict");
                }

                bool addToDict = true;
                if (GameMasterScript.masterItemList.ContainsKey(actorRefName))
                {
                    if (replaceExistingRef)
                    {
                        if (itemType != GameMasterScript.masterItemList[actorRefName].itemType)
                        {
                            Debug.LogError("WARNING: Cannot ReplaceRef " + actorRefName + ". Player content type is " + itemType + " but original type is " + GameMasterScript.masterItemList[actorRefName].itemType);
                            addToDict = false;
                        }
                        else
                        {
                            GameMasterScript.masterItemList.Remove(actorRefName);
                        }
                    }
                    else
                    {
                        Debug.LogError("WARNING: Item ref " + actorRefName + " already exists in the dict. Not adding it.");
                        addToDict = false;
                    }                                        
                }
                
                if (addToDict)
                {
                    //Debug.Log("Reading item from XML! " + actorRefName);
                    GameMasterScript.masterItemList.Add(actorRefName, this);
                    GameMasterScript.gmsSingleton.globalUniqueItemID++;
                    GameMasterScript.temp_itemsAddedToDictDuringLoad.Add(this);
                }
                    return true;
            case "DefaultRarity":
                defaultTemplateRarity = (Rarity)Enum.Parse(typeof(Rarity), reader.ReadElementContentAsString());
                rarity = defaultTemplateRarity;
                return true;
            case "Legendary":
                legendary = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                rarity = Rarity.LEGENDARY;
                return true;
            case "Leg":
                rarity = Rarity.LEGENDARY;
                legendary = true;
                reader.Read();
                return true;
            case "Description":
                string stringRefName = reader.ReadElementContentAsString();
                txt = StringManager.GetLocalizedStringOrFallbackToEnglish(stringRefName);
                description = CustomAlgorithms.ParseRichText(txt, false);
                return true;
            case "EquipmentSlot":
                // Deprecate this eventually in the file.
                reader.ReadElementContentAsString();
                break;
            case "Prefab":
                prefab = reader.ReadElementContentAsString();
                return true;
            case "ShopPrice":
                shopPrice = reader.ReadElementContentAsInt();
                return true;
            case "SalePrice":
                salePrice = reader.ReadElementContentAsInt();
                return true;
            case "CV":
            case "ChallengeValue":
                txt = reader.ReadElementContentAsString();
                challengeValue = CustomAlgorithms.TryParseFloat(txt);
                return true;
            case "Rank":
                int rank = reader.ReadElementContentAsInt();
                if (rank < 1)
                {
                    Debug.LogError("Item ref " + actorRefName + " rank must be at least 1! You entered " + rank);
                    rank = 1;
                }
                else if (rank > Item.MAX_RANK)
                {
                    Debug.LogError("Item ref " + actorRefName + " rank exceeds max of " + Item.MAX_RANK + "! You entered " + rank);
                    rank = Item.MAX_RANK;
                }
                challengeValue = Item.ConvertRankToChallengeValue(rank);
                break;
            /* case "EquipmentSlot": // TODO - DELET THIS
                EquipmentSlots es = (EquipmentSlots)Enum.Parse(typeof(EquipmentSlots), reader.ReadElementContentAsString());
                //eq.slot = es;
                return true; */
            case "ReqNewGamePlus":
                reqNewGamePlusLevel = reader.ReadElementContentAsInt();
                return true;
            case "Rarity":
                rarity = (Rarity)Enum.Parse(typeof(Rarity), reader.ReadElementContentAsString());
                return true;
            case "SpriteEffect":
                spriteEffect = reader.ReadElementContentAsString();
                return true;
            case "NumberTag":
            case "NTag":
                if (numberTags == null)
                {
                    numberTags = new List<string>();
                }
                numberTags.Add(reader.ReadElementContentAsString());
                return true;
            case "SpriteRef":
                spriteRef = reader.ReadElementContentAsString();
                return true;
            case "ExtraDescription":
                extraDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                return true;
            case "DropRate":
                forceAddToLootTablesAtRate = reader.ReadElementContentAsInt();
                break;
            case "AutoAddToShopTables":
                autoAddToShopTables = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                break;
            case "DreamItem":
                //dreamItem = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                dreamItem = true;
                reader.Read();
                return true;
            // Deprecated
            /* case "ItemEffect":
            effect = new EffectScript();
            readItemEffect = true;
            return true; */
            case "ScriptOnAddToInventory":
                scriptOnAddToInventory = reader.ReadElementContentAsString();
                break;
            case "ItemTag":
                ItemFilters tag = (ItemFilters)Enum.Parse(typeof(ItemFilters), reader.ReadElementContentAsString().ToUpperInvariant());
                AddTag(tag);
                return true;
        }

        return false;
    }

    public static IEnumerator ReadEntireFileFromXml(XmlReader reader, int index)
    {
        using (reader)
        {
            reader.Read();

            int reads = 0;
            float timeAtLastYield = Time.realtimeSinceStartup;
            while (reader.Read())
            {
                if (Time.realtimeSinceStartup - timeAtLastYield >= GameMasterScript.MIN_FPS_DURING_LOAD)
                {
                    yield return null;
                    timeAtLastYield = Time.realtimeSinceStartup;
                    GameMasterScript.IncrementLoadingBar(GameMasterScript.ELoadingBarIncrementValues.tiny);
                }
                reads++;
                
                if ((reader.NodeType == XmlNodeType.EndElement && reader.Name.ToLowerInvariant() == "document") ||
                    reads > 20000)
                {
                    if (reads >= 20000)
                    {
                        Debug.LogError("Item read alart: Stopped at num reads: " + reads);
                    }
                    break;
                }

                if (reader.Name.ToLowerInvariant() == "gearset")
                {
                    GearSet gs = new GearSet();
                    bool success = gs.ReadFromXml(reader);

                    if (success)
                    {
                        GameMasterScript.masterGearSetList.Add(gs);
                    }
                    else
                    {
                        Debug.Log("Failed to read gear set from XML.");
                    }
                }
                if (reader.Name.ToLowerInvariant() == "recipe")
                {
                    Recipe rp = new Recipe();
                    bool success = rp.ReadFromXml(reader); 

                    if (success)
                    {
                        CookingScript.masterRecipeList.Add(rp);
                    }
                    else
                    {
                        Debug.LogError("Failed to read recipe from XML.");
                    }

                }

                bool isItemStart = false;
                for (int b = 0; b < (int)ItemTypes.COUNT; b++)
                {
                    if (reader.Name == ((ItemTypes)b).ToString())
                    {
                        isItemStart = true;
                        break;
                    }
                }

                // We have detected an item! This block will read it completely, start to finish.
                if (reader.NodeType == XmlNodeType.Element && isItemStart)
                {
                    ItemTypes iType = (ItemTypes)Enum.Parse(typeof(ItemTypes), reader.Name); // Safe to do because we already verified this in the loop above.

                    // Now actually construct the object.
                    Item item = null;
                    Equipment eq = null;
                    Weapon weap = null;
                    Armor arm = null;
                    Accessory acc = null;
                    Offhand off = null;
                    Consumable consume = null;
                    Emblem emblem = null;

                    switch (iType)
                    {
                        case ItemTypes.CONSUMABLE:
                            item = new Consumable();
                            consume = item as Consumable;
                            consume.itemType = ItemTypes.CONSUMABLE;
                            break;
                        case ItemTypes.ARMOR:
                            item = new Armor();
                            eq = item as Equipment;
                            arm = item as Armor;
                            arm.itemType = ItemTypes.ARMOR;
                            arm.slot = EquipmentSlots.ARMOR;
                            break;
                        case ItemTypes.EMBLEM:
                            item = new Emblem();
                            eq = item as Equipment;
                            emblem = item as Emblem;
                            emblem.itemType = ItemTypes.EMBLEM;
                            emblem.slot = EquipmentSlots.EMBLEM;
                            break;
                        case ItemTypes.OFFHAND:
                            item = new Offhand();
                            eq = item as Equipment;
                            off = item as Offhand;
                            off.itemType = ItemTypes.OFFHAND;
                            off.slot = EquipmentSlots.OFFHAND;
                            break;
                        case ItemTypes.ACCESSORY:
                            item = new Accessory();
                            eq = item as Equipment;
                            acc = item as Accessory;
                            acc.itemType = ItemTypes.ACCESSORY;
                            acc.slot = EquipmentSlots.ACCESSORY;
                            break;
                        case ItemTypes.WEAPON:
                            item = new Weapon();
                            eq = item as Equipment;
                            weap = item as Weapon;
                            weap.itemType = ItemTypes.WEAPON;
                            weap.slot = EquipmentSlots.WEAPON;
                            break;
                    }

                    item.itemType = iType;

                    // Read data into the newly-created object until it's full o' data.
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        // This function tries reading ANY data into the item based on the reader.
                        // But if it's whitespace or unknown, the function returns false.
                        // So we just move ahead one node.
                        if (!item.TryReadFromXml(reader))
                        {
                            reader.Read();
                        }
                    }

                    //Debug.Log("Read item! " + item.actorRefName);

                    if (!item.IsLocalized())
                    {
                        Debug.Log(item.actorRefName + " is not localized.");
                        GameMasterScript.masterItemList.Remove(item.actorRefName);
                        foreach(string shop in item.addToShopRefs.Keys)
                        {
                            GameMasterScript.masterShopTableList[shop].RemoveFromTable(item.actorRefName);
                        }
                        item.blockActorFromAddingToTables = true;
                        continue;
                    }

                    bool validateItemProperties = item.ValidateEssentialProperties();

                    if (validateItemProperties)
                    {                        
                        if (item.itemType == ItemTypes.CONSUMABLE && item.actorRefName != null)
                        {
                            Consumable con = item as Consumable;
                            if (con.isFood)
                            {
                                GameMasterScript.masterFoodList.Add(item);
                                if (con.spawnFromTree)
                                {
                                    GameMasterScript.masterTreeFoodList.Add(con);
                                }
                            }
                        }
                        if (item.autoAddToShopTables)
                        {
                            GameMasterScript.itemsAutoAddToShops.Add(item);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.actorRefName) && GameMasterScript.masterItemList.ContainsKey(item.actorRefName))
                        {
                            Debug.LogError("Validation failed for item ref " + item.actorRefName + ", removing from dict.");
                            GameMasterScript.masterItemList.Remove(item.actorRefName);
                        }
                    }

                } // End of read item  

            } // End of master item file loop.
        }
    }

    public static Color GetRarityColor(Item itm)
    {
        Rarity r = itm.rarity;
        switch (r)
        {
            case Rarity.COMMON:
            default:
                return Color.white;
            case Rarity.UNCOMMON:
                return new Color(37.0f / 255.0f, 163f / 255.0f, 221f / 255.0f, 1.0f);
            case Rarity.MAGICAL:
                return new Color(237.0f / 255.0f, 213f / 255.0f, 13f / 255.0f, 1.0f);
            case Rarity.ANCIENT:
                return new Color(255.0f / 255.0f, 165f / 255.0f, 0f / 255.0f, 1.0f);
            case Rarity.ARTIFACT:
                return new Color(255.0f / 255.0f, 200f / 255.0f, 10f / 255.0f, 1.0f);
            case Rarity.LEGENDARY:
                if (itm.customItemFromGenerator || itm.ReadActorData("exprelic") == 1)
                {
                    return new Color(252f / 255.0f, 75f / 255.0f, 244f / 113f, 1.0f);
                }
                else 
                { 
                    return new Color(191.0f / 255.0f, 66f / 255.0f, 244f / 255.0f, 1.0f); // regular ol' legendary
                }
                
            case Rarity.GEARSET:
                return new Color(64.0f / 255.0f, 184f / 255.0f, 67f / 255.0f, 1.0f);
        }
    }

    public override bool IsLocalized()
    {
        if (!loadOnlyIfLocalized) return true;

        if (!StringManager.dictStringsByLanguage[StringManager.gameLanguage].ContainsKey(description) ||
            !StringManager.dictStringsByLanguage[StringManager.gameLanguage].ContainsKey(displayName))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Based on our characteristics like item effect, weapon type, armor type (etc) return a unique int that we can use for sorting
    /// </summary>
    /// <returns></returns>
    public virtual int GenerateSubtypeAsInt()
    {
        int baseValue = GENERIC_CONSUMABLE_BASE_VALUE;

        if (CheckTag(ItemFilters.VALUABLES) || CheckTag(ItemFilters.GEM))
        {
            baseValue = VALUABLES_BASE_VALUE;
        }

        return baseValue;
    }

    public virtual void SetTagsFromEffectIfAny()
    {

    }

    public string GetOrbMagicModRef()
    {
        int mmID = ReadActorData("orbid");
        if (mmID > 0)
        {
            MagicMod mm;
            if (GameMasterScript.dictMagicModIDs.TryGetValue(mmID, out mm))
            {
                return mm.refName;
            }
        }
        return ReadActorDataString("orbmagicmodref");
    }

    public void SetOrbMagicModRef(string modName)
    {
        MagicMod mm;
        if (GameMasterScript.masterMagicModList.TryGetValue(modName, out mm))
        {
            if (mm.magicModID > 0)
            {
                SetActorData("orbid", mm.magicModID);
                return;
            }
        }

        SetActorDataString("orbmagicmodref", modName);
    }
}
