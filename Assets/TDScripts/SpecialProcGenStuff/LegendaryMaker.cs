using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Text;
using System.Linq;
using System.Xml;
using System.IO;

public enum ELegendaryNameTypes { PERSONAL_POSSESSIVE, ITEM_PROPERNAME, ITEM_GENERIC, COUNT };
public enum EArtifactNameElements { HISTORIC, GENERICOBJECT, GENERICWEAPON, HAMMER, SWORD, CLAW, DAGGER, STAFF, SPEAR, AXE,
    RANGEDWEAPON, MAGIC, DEFENSE, ACCESSORY, PERSONALDESCRIPTOR, GENERALDESCRIPTOR, SWORDPROPER, SPEARPROPER, DAGGERPROPER,
    AXEPROPER, HAMMERPROPER, RANGEDPROPER, STAFFPROPER, ARMORPROPER, SHIELDPROPER, BOOKPROPER, ACCESSORYPROPER, CLAWPROPER,
    LIGHTARMOR, MEDIUMARMOR, HEAVYARMOR, QUIVER, MAGICBOOK, RING, NECKLACE, GLOVE, HELMET, INSTRUMENT, RINGPROPER, HELMETPROPER, WHIP, WHIPPROPER, COUNT }

public enum ELegendarySpriteTypes { SWORD, MACE, BOW, SPEAR, CLAW, STAFF, DAGGER, AXE, LIGHTARMOR, MEDIUMARMOR, HEAVYARMOR, SHIELD, BOOK, QUIVER,
    ACCESSORY, HELMET, INSTRUMENT, RING, NECKLACE, GLOVE, WHIP, HANDWRAP, COUNT }

public enum EWeaponFilterProperties { ANY, ONLY_RANGED, ONLY_MELEE, NO_AXES }

public enum EFlavorTextElements { OPENING_1, OPENING_2, TAIL_1, TAIL_2, DREAM_NOUN, DREAM_ACTION, DREAM_EVENT, DREAM_PLACE, DREAM_ACTIVITY, OWNER_ACTION, SONG_TYPE, SONG_NOUN, OWNER_ALTERNATE, COUNT }

public enum ERelicModTypes { LEGONLY, SPECIALMOD, REGULARMOD, COUNT }

// Data pack for legendary or special mods that don't normally spawn at random.
public class LegModData
{
    public Vector2 challengeRange;
    public List<EquipmentSlots> possibleSlots;
    public string refName;
    public MagicMod modObjectRef;
    public CharacterJobs preferredJob;
    public EWeaponFilterProperties weaponFilter;

    public LegModData(Vector2 cRange, List<EquipmentSlots> pSlots = null, CharacterJobs prefJob = CharacterJobs.COUNT, EWeaponFilterProperties wProp = EWeaponFilterProperties.ANY)
    {
        challengeRange = cRange;
        if (pSlots != null)
        {
            possibleSlots = pSlots;
        }
        else
        {
            possibleSlots = new List<EquipmentSlots>() { EquipmentSlots.ANY };
        }

        preferredJob = prefJob;
        weaponFilter = wProp;
    }
}

public class LegendaryNameData
{
    public string displayName;
    public string properName;
    public string historicName;

    public LegendaryNameData()
    {
        Clear();
    }

    public void Clear()
    {
        displayName = "";
        properName = "";
        historicName = "";
    }
}

public partial class LegendaryMaker
{
    

    static Dictionary<EArtifactNameElements, List<string>> nameElements = new Dictionary<EArtifactNameElements, List<string>>();
    

    const float CHANCE_2H_WEAPON = 0.2f;

    //static Dictionary<EArtifactNameElements, List<string>> nameElementsUsedInSaveFile = new Dictionary<EArtifactNameElements, List<string>>();
    static List<string> completeItemNamesUsedInSaveFile;
    static List<string> historicNamesUsedInSaveFile;
    static List<string> properNamesUsedInSaveFile;

    public static ActorTable accessorySubtypes;

    public static List<LegModData> specialModsSortedByCV;
    public static List<LegModData> legendaryOnlyModsSortedByCV;

    public static bool initialized;

    /// <summary>
    /// A text file with the information needed to roll up random flavor text.
    /// </summary>
    private static TextAsset flavorTextDataTables;

    public static ActorTable equipmentTypeTable;

    /// <summary>
    /// With shared stash, how is this going to work
    /// </summary>
    public static void FlushSaveFileData()
    {
        if (!initialized) // || !DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return;
        }

        completeItemNamesUsedInSaveFile.Clear();
        properNamesUsedInSaveFile.Clear();
        historicNamesUsedInSaveFile.Clear();        
    }

    /// <summary>
    /// Sets up dictionary with all naming elements, to be used for procedural name generation.
    /// The magic numbers here are pulled from en_us, we have to determine how many names there are per name element
    /// For example, are there 5 Book proper names? 10 sword? We just have to look in en_us and find out
    /// This will be localized too, so the count is the same amount languages
    /// </summary>
    public static void Initialize()
    {
        if (initialized) return;

        initialized = true;

        completeItemNamesUsedInSaveFile = new List<string>();
        historicNamesUsedInSaveFile = new List<string>();
        properNamesUsedInSaveFile = new List<string>();

        accessorySubtypes = new ActorTable();
        accessorySubtypes.refName = "acc_subtypes";
        accessorySubtypes.AddToTable("instrument", 25);
        accessorySubtypes.AddToTable("glove", 50);
        accessorySubtypes.AddToTable("helmet", 75);
        accessorySubtypes.AddToTable("ring", 100);
        accessorySubtypes.AddToTable("necklace", 100);
        accessorySubtypes.AddToTable("misc", 150);

        equipmentTypeTable = new ActorTable();
        equipmentTypeTable.AddToTable("WEAPON", 220);
        equipmentTypeTable.AddToTable("OFFHAND", 80);
        equipmentTypeTable.AddToTable("ARMOR", 80);
        equipmentTypeTable.AddToTable("ACCESSORY", 100);

        GetNameAndFlavorElementsFromData();

        specialModsSortedByCV = specialNonLegMods.Values.OrderBy(m => m.challengeRange.x).ToList();
        specialModsSortedByCV = specialNonLegMods.Values.OrderBy(n => n.challengeRange.x).ToList();
        legendaryOnlyModsSortedByCV = legendaryOnlyMods.Values.OrderBy(m => m.challengeRange.x).ToList();
        legendaryOnlyModsSortedByCV = legendaryOnlyMods.Values.OrderBy(n => n.challengeRange.x).ToList();

        // Make sure our LegModData packs contain all vital references.
        foreach(string modRef in specialNonLegMods.Keys)
        {
            specialNonLegMods[modRef].refName = modRef;
            specialNonLegMods[modRef].modObjectRef = GameMasterScript.masterMagicModList[modRef];
        }
        foreach (string modRef in legendaryOnlyMods.Keys)
        {
            legendaryOnlyMods[modRef].refName = modRef;
            legendaryOnlyMods[modRef].modObjectRef = GameMasterScript.masterMagicModList[modRef];
        }

        string resourceTablePath = "DLCResources/DLC1/Localization/perchance_mystery_item_flavor_table_";
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.en_us:
                resourceTablePath += "en_us";
                break;
            case EGameLanguage.de_germany:
                resourceTablePath += "de_germany";
                break;
            case EGameLanguage.jp_japan:
                resourceTablePath += "jp_japan";
                break;
            case EGameLanguage.zh_cn:
                resourceTablePath += "en_us";
                break;
            default:
                resourceTablePath += "en_us";
                break;                
        }

        flavorTextDataTables = Resources.Load<TextAsset>(resourceTablePath);
        PerchanceParser.ParsePerchanceFile(flavorTextDataTables);
        DebugConsole.RegisterCommand("legendaryflavor", PerchanceParser.Debug_GenerateLegendaryFlavor);

        //if (Debug.isDebugBuild) Debug.Log("<color=green>Legendary maker initialized!</color>");
    }

    public static int GetMinimumTotalModsByCV(float challengeValue)
    {
        int minTotalMods = 4;
        if (challengeValue >= 1.7f) minTotalMods++;
        return minTotalMods;
    }

    public static int GetMinimumLegOnlyModsByCV(float challengeValue)
    {
        int minLegendaryMods = 1;
        if (challengeValue >= 1.9f) minLegendaryMods++;
        return minLegendaryMods;
    }

    public static int GetMaximumTotalModsByCV(float challengeValue)
    {
        int maxTotalMods = 3;
        if (challengeValue >= 1.4f) maxTotalMods++;
        if (challengeValue >= 1.8f) maxTotalMods++;
        if (challengeValue >= 2.05f) maxTotalMods++;

        return maxTotalMods;
    }

    public static int GetMaximumLegOnlyModsByCV(float challengeValue)
    {
        int maxLegendaryMods = 1;
        if (challengeValue >= 1.5f) maxLegendaryMods++;
        return maxLegendaryMods;
    }

    /// <summary>
    /// Constructs a brand new Legendary item from scratch, fills it out, adds to game dictionaries.
    /// </summary>
    /// <param name="challengeValue">Specify CV as float from 1.0 to 2.5 - auto clamps.</param>
    public static Item CreateNewLegendaryItem(float challengeValue, ItemTypes targetItemType = ItemTypes.COUNT)
    {
        if (!initialized)
        {
            Initialize();
        }

        // For now, this is just equipment.
        Item itemCreated = CreateBaseItemWithRefName(targetItemType);
        Equipment eqCreated = itemCreated as Equipment;

        challengeValue = Mathf.Clamp(challengeValue, 1.0f, BalanceData.GetMaxChallengeValueForItems());
        itemCreated.challengeValue = challengeValue;

        // Sets weapon type, armor type, or offhand type
        SetSubtypeOfItem(itemCreated);

        // Sets most basic properties: power, defense, base mod
        // Since legendaries are powerful, we'll offset this by -1
        eqCreated.SetBaseItemStatsByCV(challengeValue, -1);

#if UNITY_EDITOR
        //Debug.Log("Created: " + itemCreated.actorRefName);
#endif

        /* Equipment copyEQ = null; // this "master copy" is the thing we're saving, not the version that the player gets!!!
        switch(eqCreated.itemType)
        {
            case ItemTypes.WEAPON:
                copyEQ = new Weapon();
                break;
            case ItemTypes.OFFHAND:
                copyEQ = new Offhand();
                break;
            case ItemTypes.ARMOR:
                copyEQ = new Armor();
                break;
            case ItemTypes.ACCESSORY:
                copyEQ = new Accessory();
                break;
            case ItemTypes.EMBLEM:
                copyEQ = new Emblem();
                break;
        }
        copyEQ.CopyFromItem(itemCreated); */

        SharedBank.AddGeneratedRelic(itemCreated);
        
        GameMasterScript.masterItemList.Add(itemCreated.actorRefName, itemCreated);

        int minLegendaryMods = GetMinimumLegOnlyModsByCV(challengeValue);
        int maxLegendaryMods = GetMaximumLegOnlyModsByCV(challengeValue);
        int minTotalMods = GetMinimumTotalModsByCV(challengeValue);
        int maxTotalMods = GetMaximumTotalModsByCV(challengeValue);

        int numMods = UnityEngine.Random.Range(minTotalMods, maxTotalMods + 1);
        int numLegMods = UnityEngine.Random.Range(minLegendaryMods, maxLegendaryMods + 1);

        eqCreated.AddModsToLegendary(numMods, numLegMods);

        eqCreated.SelectSpriteAtRandom();

        itemCreated.customItemFromGenerator = true;

        itemCreated.displayName = GenerateDisplayName(itemCreated);

#if UNITY_EDITOR
        Debug.Log("Created: " + itemCreated.displayName);
#endif

        itemCreated.rarity = Rarity.LEGENDARY;
        itemCreated.legendary = true;

        GenerateFlavorTextForItem(itemCreated);

        if (itemCreated.itemType == ItemTypes.OFFHAND)
        {
            CleanUpOffhandWeirdness(itemCreated);
        }

        //Debug.Log(itemCreated.actorRefName + " " + itemCreated.displayName);
        
        SharedBank.generatedLegendaryCounter++;

        itemCreated.CalculateShopPrice(0.8f, true);
        itemCreated.SetUniqueIDAndAddToDict();

        // We've created the item and added it to the master record, but now we need to create a local copy to give to the player.
        Item copyForPlayer = LootGeneratorScript.CreateItemFromTemplateRef(itemCreated.actorRefName, 1f, 0f, false, true);

        return copyForPlayer;
    }

    /// <summary>
    /// Rolls on a fun table to come up with a fun piece of flavor that may have some information in it
    /// related to the item's name.
    /// </summary>
    /// <param name="itm"></param>
    static void GenerateFlavorTextForItem(Item itm)
    {
        if (itm.itemType == ItemTypes.WEAPON)
        {
            Weapon w = itm as Weapon;
            if (w.weaponType == WeaponTypes.NATURAL)
            {
                int maxMods = w.ReadActorData("maxmods");
                w.numberTags.Add(maxMods.ToString());
                w.description = StringManager.GetString("budoka_glove1_desc");
                w.description = w.description.Replace("^number1^", "<color=yellow>" + maxMods.ToString() + "</color>");
                return;
            }
            
        }        

        //this section is language independent  
        var flavorString = PerchanceParser.GetResultFromTable("output");
        
        //add in variables based on item info, like owner, name, etc.
        var ownerName = itm.ReadActorDataString("flavor_historical_name");
        var itemType = itm.ReadActorDataString("flavor_item_type");

        //go over some language specific rules we might need
        switch (StringManager.gameLanguage)
        {
            case EGameLanguage.en_us:
                flavorString = ProcessFlavorStringVariables_en_us(flavorString, ownerName, itemType);
                break;
            case EGameLanguage.de_germany:
                flavorString = ProcessFlavorStringVariables_de(flavorString, ownerName, itemType);
                break;
            case EGameLanguage.jp_japan:
                flavorString = ProcessFlavorStringVariables_jp(flavorString, ownerName, itemType);
                break;
            case EGameLanguage.es_spain:
            case EGameLanguage.zh_cn:
                flavorString = "";
                break;
            default:
                flavorString = ProcessFlavorStringVariables_en_us(flavorString, ownerName, itemType);
                break;                
        }
            
        //hooray
        itm.description = flavorString;
    }

    /// <summary>
    /// Adjusts #item_name and #item_type variables in a flavor string based on american english rules.
    /// </summary>
    /// <param name="flavor"></param>
    /// <param name="ownerName"></param>
    /// <param name="itemType"></param>
    /// <returns></returns>
    static string ProcessFlavorStringVariables_en_us(string flavorString, string ownerName, string itemType)
    {
        if (string.IsNullOrEmpty(ownerName))
        {
            ownerName = StringManager.GetString("relic_flavor_no_owner");
        }
        
        if (string.IsNullOrEmpty(itemType))
        {
            itemType = StringManager.GetString("exp_rarity_relic").ToLowerInvariant();
        }
        
        //do a/an checks
        string itemTypeWithArticle;
        if ( "aeiou".Contains(itemType[0]))
        {
            itemTypeWithArticle = "an " + itemType;
        }
        else
        {
            itemTypeWithArticle = "a " + itemType;
        }
        
        //replace values
        flavorString = flavorString.Replace("#item_owner", ownerName);
        flavorString = flavorString.Replace("#item_type_with_article", itemTypeWithArticle);
        flavorString = flavorString.Replace("#item_type", itemType);
        
        //capitalize first letter
        flavorString = char.ToUpper(flavorString[0]) + flavorString.Substring(1);

        return flavorString;
    }

    static string ProcessFlavorStringVariables_jp(string flavorString, string ownerName, string itemType)
    {
        return flavorString;
    }
    static string ProcessFlavorStringVariables_de(string flavorString, string ownerName, string itemType)
    {
        return flavorString;
    }
    static string ProcessFlavorStringVariables_zh_cn(string flavorString, string ownerName, string itemType)
    {
        return flavorString;
    }

    /// <summary>
    /// Constructs an empty piece of equipment at random, with refname assigned.
    /// </summary>
    /// <returns></returns>
    static Item CreateBaseItemWithRefName(ItemTypes targetItemType = ItemTypes.COUNT)
    {
        string iType = equipmentTypeTable.GetRandomActorRef();

        /* iType = "WEAPON";
        Debug.LogError("REMOVE"); */

        ItemTypes typeForItem = (ItemTypes)Enum.Parse(typeof(ItemTypes), iType);

        if (targetItemType != ItemTypes.COUNT) typeForItem = targetItemType;

        string refNameForItem = "genleg_" + typeForItem.ToString().ToLowerInvariant() + "_" + SharedBank.generatedLegendaryCounter + "_" + GameStartData.saveGameSlot;

        // super sanity check to make sure we don't end up with the same item name twice
        while (GameMasterScript.masterItemList.ContainsKey(refNameForItem) || SharedBank.allRelicTemplates.ContainsKey(refNameForItem))
        {
            SharedBank.generatedLegendaryCounter++;
            refNameForItem = "genleg_" + typeForItem.ToString().ToLowerInvariant() + "_" + SharedBank.generatedLegendaryCounter;
        }


#if UNITY_EDITOR
        //typeForItem = ItemTypes.OFFHAND;
        //Debug.LogError("DEBUG - GENERATING OFFHANDS ONLY");
#endif

        // Construct the item object.

        Item itemCreated = null;

        switch (typeForItem)
        {
            case ItemTypes.WEAPON:
                itemCreated = new Weapon();
                break;
            case ItemTypes.ARMOR:
                itemCreated = new Armor();
                break;
            case ItemTypes.OFFHAND:
                itemCreated = new Offhand();
                break;
            case ItemTypes.ACCESSORY:
                itemCreated = new Accessory();
                break;
        }

        itemCreated.actorRefName = refNameForItem;

        return itemCreated;
    }

    /// <summary>
    /// Generates an interesting and properly localized display name for the item.
    /// </summary>
    /// <param name="template">The item we're naming.</param>
    /// <returns></returns>
    static string GenerateDisplayName(Item template)
    {
        bool nameValid = false; // later on, validate against duplicate names?

        LegendaryNameData nameData = new LegendaryNameData();

        while (!nameValid)
        {
            nameData.Clear();
            // These functions all set the correct info in nameData
            TryGenerateDisplayNameByLanguage(template, nameData);

            if (!completeItemNamesUsedInSaveFile.Contains(nameData.displayName))
            {
                nameValid = true;
                break;
            }

        } // end while check

        historicNamesUsedInSaveFile.Add(nameData.historicName);
        properNamesUsedInSaveFile.Add(nameData.properName);
        completeItemNamesUsedInSaveFile.Add(nameData.displayName);

        return nameData.displayName;
    }

    static void TryGenerateDisplayNameByLanguage(Item template, LegendaryNameData nameData)
    {
        ELegendaryNameTypes eType = (ELegendaryNameTypes)UnityEngine.Random.Range(0, (int)ELegendaryNameTypes.COUNT);

        // make ITEM_GENERIC a bit less common
        if (eType == ELegendaryNameTypes.ITEM_GENERIC)
        {
            eType = (ELegendaryNameTypes)UnityEngine.Random.Range(0, (int)ELegendaryNameTypes.COUNT);
        }
        if (eType == ELegendaryNameTypes.ITEM_PROPERNAME && template.itemType == ItemTypes.ARMOR)
        {
            // we don't have as many proper Armor names
            eType = (ELegendaryNameTypes)UnityEngine.Random.Range(0, (int)ELegendaryNameTypes.COUNT);
        }


        bool foundUnusedName = false;
        int attempts = 0; // some crazy players might use every name, so don't get locked up in while loop!

        switch (eType)
        {
            case ELegendaryNameTypes.PERSONAL_POSSESSIVE:
                while (!foundUnusedName)
                {
                    attempts++;
                    nameData.displayName = GetPersonalPossessiveName(template, out nameData.historicName);
                    if (!historicNamesUsedInSaveFile.Contains(nameData.historicName) || attempts > 100)
                    {
                        foundUnusedName = true;
                    }
                }
                break;
            case ELegendaryNameTypes.ITEM_PROPERNAME:
                while (!foundUnusedName)
                {
                    attempts++;
                    nameData.displayName = GetItemProperName(template, out nameData.properName);
                    if (!properNamesUsedInSaveFile.Contains(nameData.properName) || attempts > 100)
                    {
                        foundUnusedName = true;
                    }
                }
                break;
            case ELegendaryNameTypes.ITEM_GENERIC:
                switch(StringManager.gameLanguage)
                {
                    case EGameLanguage.de_germany:
                        SetGermanGenericNameData(template, nameData);
                        break;
                    case EGameLanguage.jp_japan:
                        SetJapaneseGenericNameData(template, nameData);
                        break;
                    case EGameLanguage.zh_cn:
                        SetChineseGenericNameData(template, nameData);
                        break;
                    case EGameLanguage.en_us:
                        SetEnglishGenericNameData(template, nameData);
                        break;
                    case EGameLanguage.es_spain:
                        SetSpanishGenericNameData(template, nameData);
                        break;
                }
                break;
        }

        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            nameData.displayName = CustomAlgorithms.RemoveTrailingCharacter(nameData.displayName, '-');
        }
    }

    static void SetGermanGenericNameData(Item template, LegendaryNameData nameData)
    {
        int roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            nameData.displayName = GetItemNoun(template);
        }
        else if (roll == 1)
        {
            nameData.displayName = GetItemNoun(template) + " " + nameElements[EArtifactNameElements.PERSONALDESCRIPTOR].GetRandomElement();
        }
        else if (roll == 2)
        {
            nameData.displayName = GetItemNoun(template) + " " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement();
        }
    }

    static void SetSpanishGenericNameData(Item template, LegendaryNameData nameData)
    {
        int roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            nameData.displayName = GetItemNoun(template);
        }
        else if (roll == 1)
        {
            nameData.displayName = nameElements[EArtifactNameElements.PERSONALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
        else if (roll == 2)
        {
            nameData.displayName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
    }

    static void SetChineseGenericNameData(Item template, LegendaryNameData nameData)
    {
        int roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            nameData.displayName = GetItemNoun(template);
        }
        else if (roll == 1)
        {
            nameData.displayName = nameElements[EArtifactNameElements.PERSONALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
        else if (roll == 2)
        {
            nameData.displayName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
    }

    static void SetJapaneseGenericNameData(Item template, LegendaryNameData nameData)
    {
        int roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            nameData.displayName = GetItemNoun(template);
        }
        else if (roll == 1)
        {
            nameData.displayName = nameElements[EArtifactNameElements.PERSONALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
        else if (roll == 2)
        {
            nameData.displayName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);
        }
    }

    static void SetEnglishGenericNameData(Item template, LegendaryNameData nameData)
    {
        int roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            nameData.displayName = "The " + GetItemNoun(template);
        }
        else if (roll == 1)
        {
            nameData.displayName = "The " + nameElements[EArtifactNameElements.PERSONALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template);
        }
        else if (roll == 2)
        {
            nameData.displayName = "The " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template);
        }
    }

    /// <summary>
    /// Returns a NON-proper noun for a generated legendary item. This could be something like 'Sword', 'Blade', 'Relic' etc.
    /// </summary>
    /// <param name="template">The item being named</param>
    /// <returns></returns>
    static string GetItemNoun(Item template)
    {
        float chanceOfGenericName = 0.15f;
        if (UnityEngine.Random.Range(0, 1f) <= chanceOfGenericName) return GetGenericNoun();

        Equipment eq = template as Equipment;

        switch (template.itemType)
        {
            case ItemTypes.WEAPON:
                if (eq.legSpriteType == ELegendarySpriteTypes.INSTRUMENT && UnityEngine.Random.Range(0,1f) <= 0.5f)
                {
                    return nameElements[EArtifactNameElements.INSTRUMENT].GetRandomElement();
                }

                if (UnityEngine.Random.Range(0, 1f) <= chanceOfGenericName) return GetGenericNoun();

                Weapon w = template as Weapon;
                // 33% chance to use generic Weapon name.
                if (UnityEngine.Random.Range(0,2) == 0)
                {
                    return nameElements[EArtifactNameElements.GENERICWEAPON].GetRandomElement();
                }
                // Otherwise, use specific type-based name.
                switch (w.weaponType)
                {
                    case WeaponTypes.STAFF:
                        return nameElements[EArtifactNameElements.STAFF].GetRandomElement();
                    case WeaponTypes.AXE:
                        return nameElements[EArtifactNameElements.AXE].GetRandomElement();
                    case WeaponTypes.DAGGER:
                        return nameElements[EArtifactNameElements.DAGGER].GetRandomElement();
                    case WeaponTypes.BOW:
                        return nameElements[EArtifactNameElements.RANGEDWEAPON].GetRandomElement();
                    case WeaponTypes.MACE:
                        return nameElements[EArtifactNameElements.HAMMER].GetRandomElement();
                    case WeaponTypes.CLAW:
                        return nameElements[EArtifactNameElements.CLAW].GetRandomElement();
                    case WeaponTypes.SPEAR:
                        return nameElements[EArtifactNameElements.SPEAR].GetRandomElement();
                    case WeaponTypes.WHIP:
                        if (StringManager.gameLanguage == EGameLanguage.en_us)
                        {
                            return nameElements[EArtifactNameElements.WHIP].GetRandomElement();
                        }
                        else
                        {
                            return nameElements[EArtifactNameElements.HAMMER].GetRandomElement();                       
                        }
                    case WeaponTypes.NATURAL:
                        return nameElements[EArtifactNameElements.GLOVE].GetRandomElement();
                    case WeaponTypes.SWORD:
                    default:
                        return nameElements[EArtifactNameElements.SWORD].GetRandomElement();
                }
            case ItemTypes.ARMOR:                
                Armor arm = template as Armor;
                // 50% chance to use generic Defense name
                if (UnityEngine.Random.Range(0,2) == 0)
                {
                    return nameElements[EArtifactNameElements.DEFENSE].GetRandomElement();
                }
                // Otherwise, use name specific to armor type
                switch (arm.armorType)
                {
                    case ArmorTypes.LIGHT:
                        return nameElements[EArtifactNameElements.LIGHTARMOR].GetRandomElement();
                    case ArmorTypes.MEDIUM:
                        return nameElements[EArtifactNameElements.MEDIUMARMOR].GetRandomElement();
                    case ArmorTypes.HEAVY:
                        return nameElements[EArtifactNameElements.HEAVYARMOR].GetRandomElement();
                }
                break;
            case ItemTypes.OFFHAND:
                Offhand oh = template as Offhand;
                if (oh.IsQuiver())
                {
                    return nameElements[EArtifactNameElements.RANGEDWEAPON].GetRandomElement();
                }
                else if (oh.IsShield())
                {
                    return nameElements[EArtifactNameElements.DEFENSE].GetRandomElement();
                }
                else // if (oh.IsMagicBook())
                {
                    if (UnityEngine.Random.Range(0,2) == 0)
                    {
                        return nameElements[EArtifactNameElements.MAGIC].GetRandomElement();
                    }
                    else
                    {
                        return nameElements[EArtifactNameElements.MAGICBOOK].GetRandomElement();
                    }
                }
            case ItemTypes.ACCESSORY:
                // Check again for generic name (relic etc)
                if (UnityEngine.Random.Range(0, 1f) <= 0.15f) return nameElements[EArtifactNameElements.GENERICOBJECT].GetRandomElement();

                switch(eq.legSpriteType)
                {
                    case ELegendarySpriteTypes.GLOVE:
                        return nameElements[EArtifactNameElements.GLOVE].GetRandomElement();
                    case ELegendarySpriteTypes.INSTRUMENT:
                        return nameElements[EArtifactNameElements.INSTRUMENT].GetRandomElement();
                    case ELegendarySpriteTypes.HELMET:
                        return nameElements[EArtifactNameElements.HELMET].GetRandomElement();
                    case ELegendarySpriteTypes.RING:
                        return nameElements[EArtifactNameElements.RING].GetRandomElement();
                    case ELegendarySpriteTypes.NECKLACE:
                        return nameElements[EArtifactNameElements.NECKLACE].GetRandomElement();
                }

                return nameElements[EArtifactNameElements.ACCESSORY].GetRandomElement();                
        }

        return "notfound";
    }

    static string GetItemProperName(Item template, out string properName)
    {
        string name = "";

        Equipment eq = template as Equipment;

        // This is a specially-named item based on mythical proper nouns.
        switch(template.itemType)
        {
            case ItemTypes.WEAPON:
                Weapon w = template as Weapon;
                switch (w.weaponType)
                {
                    case WeaponTypes.SWORD:
                        name = nameElements[EArtifactNameElements.SWORDPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.AXE:
                        name = nameElements[EArtifactNameElements.AXEPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.DAGGER:
                        name = nameElements[EArtifactNameElements.DAGGERPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.STAFF:
                        name = nameElements[EArtifactNameElements.STAFFPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.MACE:
                        name = nameElements[EArtifactNameElements.HAMMERPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.BOW:
                        name = nameElements[EArtifactNameElements.RANGEDPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.CLAW:
                    case WeaponTypes.NATURAL:
                        name = nameElements[EArtifactNameElements.CLAWPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.SPEAR:
                        name = nameElements[EArtifactNameElements.SPEARPROPER].GetRandomElement();
                        break;
                    case WeaponTypes.WHIP:
                        if (StringManager.gameLanguage == EGameLanguage.en_us)
                        {
                            name = nameElements[EArtifactNameElements.WHIPPROPER].GetRandomElement();
                        }
                        else
                        {
                            name = nameElements[EArtifactNameElements.HAMMERPROPER].GetRandomElement();                            
                        }
                        
                        break;
                }
                break;
            case ItemTypes.ARMOR:
                name = nameElements[EArtifactNameElements.ARMORPROPER].GetRandomElement();
                break;
            case ItemTypes.ACCESSORY:
                if (eq.legSpriteType == ELegendarySpriteTypes.HELMET)
                {
                    name = nameElements[EArtifactNameElements.HELMETPROPER].GetRandomElement();
                }
                else if (eq.legSpriteType == ELegendarySpriteTypes.RING)
                {
                    name = nameElements[EArtifactNameElements.RINGPROPER].GetRandomElement();
                }
                else
                {
                    name = nameElements[EArtifactNameElements.ACCESSORYPROPER].GetRandomElement();
                }
                
                break;
            case ItemTypes.OFFHAND:
                Offhand oh = template as Offhand;
                if (oh.IsShield())
                {
                    name = nameElements[EArtifactNameElements.SHIELDPROPER].GetRandomElement();
                }
                else if (oh.IsMagicBook())
                {
                    name = nameElements[EArtifactNameElements.BOOKPROPER].GetRandomElement();
                }
                else // (oh.IsQuiver())
                {
                    name = nameElements[EArtifactNameElements.RANGEDPROPER].GetRandomElement();
                }
                break;
        }

        properName = name;

        //Debug.Log(template.itemType + " " + name);
        
        switch(StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
                name = AssembleJapaneseProperNameWithPossibleDescriptors(template, name);
                break;
            case EGameLanguage.de_germany:
                name = AssembleGermanProperNameWithPossibleDescriptors(template, name);
                break;
            case EGameLanguage.es_spain:
                name = AssembleSpanishProperNameWithPossibleDescriptors(template, name);
                break;
            case EGameLanguage.zh_cn:
                // #todo chinese version
                name = AssembleChineseProperNameWithPossibleDescriptors(template, name);
                break;
            case EGameLanguage.en_us:
            default:
                name = AssembleEnglishProperNameWithPossibleDescriptors(template, name);
                break;
        }

        return name;
    }

    static string AssembleChineseProperNameWithPossibleDescriptors(Item template, string name)
    {
        int randomRoll = UnityEngine.Random.Range(0, 6);
        switch (randomRoll)
        {
            case 0:
                // The Celestial Blade, Excalibur
                name = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template) + name;
                break;
            case 1:
                name += "的" + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);

                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Just "Excalibur"
                break;
        }

        return name;
    }

    static string AssembleJapaneseProperNameWithPossibleDescriptors(Item template, string name)
    {
        int randomRoll = UnityEngine.Random.Range(0, 6);
        switch (randomRoll)
        {
            case 0:
                // The Celestial Blade, Excalibur
                name = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template) + name;
                break;
            case 1:
                name += "の" + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + GetItemNoun(template);

                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Just "Excalibur"
                break;
        }

        return name;
    }

    static string AssembleSpanishProperNameWithPossibleDescriptors(Item template, string name)
    {
        int randomRoll = UnityEngine.Random.Range(0, 6);
        switch (randomRoll)
        {
            case 0:
            case 1:
                name = name + " (" + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template) + ")";
                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Just "Excalibur"
                break;
        }

        return name;
    }

    static string AssembleGermanProperNameWithPossibleDescriptors(Item template, string name)
    {
        int randomRoll = UnityEngine.Random.Range(0, 6);
        switch (randomRoll)
        {
            case 0:
            case 1:
                name = name + " " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + ", " + GetItemNoun(template);
                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Just "Excalibur"
                break;
        }

        return name;
    }

    static string AssembleEnglishProperNameWithPossibleDescriptors(Item template, string name)
    {
        int randomRoll = UnityEngine.Random.Range(0, 6);
        switch (randomRoll)
        {
            case 0:
                // The Celestial Blade, Excalibur
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // add the
                    name = "The " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template) + ", " + name;
                }
                else
                {
                    name = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template) + ", " + name;
                }

                break;
            case 1:
                // Excalibur, the Celestial Blade
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // add 'the'
                    name += ", the " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template);
                }
                else
                {
                    name += ", " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + GetItemNoun(template);
                }

                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Just "Excalibur"
                break;
        }

        return name;
    }

    static string GetPersonalPossessiveName(Item template, out string historicName)
    {
        string dName = "";        
        List<string> historicNames = nameElements[EArtifactNameElements.HISTORIC];

        historicName = "";

        string randomHistoricalName = historicNames.GetRandomElement();
        template.SetActorDataString("flavor_historical_name", randomHistoricalName);

        //store this before we add fancy to the name, in case we want to use it
        //in the flavor text.
        string itemName = GetItemNoun(template);
        template.SetActorDataString("flavor_item_type", itemName.ToLower());

        if (UnityEngine.Random.Range(0,3) == 0)
        {
            // Celestial Blade instead of just Blade
            switch(StringManager.gameLanguage)
            {
                case EGameLanguage.de_germany:
                    itemName = itemName + " " + nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement();
                    break;
                case EGameLanguage.es_spain:
                    itemName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + itemName;
                    break;
                case EGameLanguage.en_us:
                    itemName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + " " + itemName;
                    break;
                case EGameLanguage.jp_japan:
                case EGameLanguage.zh_cn:
                    itemName = nameElements[EArtifactNameElements.GENERALDESCRIPTOR].GetRandomElement() + itemName;
                    break;

            }            
        }

        if (UnityEngine.Random.Range(0,4) == 0) // the blank of blank is less common
        {
            switch(StringManager.gameLanguage)
            {
                case EGameLanguage.en_us:
                default:
                    // The Blade of Arendor
                    dName = "The " + itemName + " of " + randomHistoricalName;
                    break;
                case EGameLanguage.jp_japan:
                    // '\u2009' is half space. we might need this
                    dName = randomHistoricalName + "の" + itemName;
                    break;
                case EGameLanguage.zh_cn:
                    dName = randomHistoricalName + "的" + itemName;
                    break;
                case EGameLanguage.de_germany:
                    dName = randomHistoricalName + " " + itemName;
                    break;
                case EGameLanguage.es_spain:
                    dName = itemName + " de " + randomHistoricalName;
                    break;
            }

        }
        else
        {
            historicName = randomHistoricalName;

            switch (StringManager.gameLanguage)
            {
                case EGameLanguage.en_us:
                default:
                    if (historicName[historicName.Length - 1] != 's')
                    {
                        dName = historicName + "'s " + itemName;
                    }
                    else
                    {
                        dName = historicName + "' " + itemName;
                    }
                    break;
                case EGameLanguage.de_germany:
                    dName = historicName + " " + itemName; // the apostrophe should be built in?
                    break;
                case EGameLanguage.es_spain:
                    dName = itemName + " de " + historicName;
                    break;
                case EGameLanguage.jp_japan:
                    // '\u2009' is half space. we might need this
                    dName = historicName + "の" + itemName;
                    break;
                case EGameLanguage.zh_cn:
                    dName = historicName + "的" + itemName;
                    break;
            }         
        }

        return dName;
    }

    /// <summary>
    /// Returns a generic 'artifact' noun that could apply to any item type.
    /// </summary>
    /// <returns></returns>
    static string GetGenericNoun()
    {
        return nameElements[EArtifactNameElements.GENERICOBJECT].GetRandomElement();
    }

    /// <summary>
    /// For weapons, sets a weapon type, ranged/2h. For armor, sets armor type. For offhand, selects shield, book, or quiver.
    /// </summary>
    /// <param name="genItem"></param>
    public static void SetSubtypeOfItem(Item genItem)
    {
        switch(genItem.itemType)
        {
            case ItemTypes.WEAPON:
                Weapon w = genItem as Weapon;
                w.weaponType = possibleLegWeaponTypes.GetRandomElement();

                // Bows, claws, daggers cannot be 2h.
                if (UnityEngine.Random.Range(0,1f) <= CHANCE_2H_WEAPON && w.weaponType != WeaponTypes.BOW 
                    && w.weaponType != WeaponTypes.CLAW && w.weaponType != WeaponTypes.DAGGER && w.weaponType != WeaponTypes.NATURAL)
                {
                    w.twoHanded = true;
                }

                w.SetSwingAndImpactAnimations();

                // Bows are always 2H. Bows and staves are always ranged. Both have a penalty at melee range.
                if (w.weaponType == WeaponTypes.BOW || w.weaponType == WeaponTypes.STAFF)
                {
                    w.isRanged = true;
                    w.twoHanded = w.weaponType == WeaponTypes.BOW;
                    w.eqFlags[(int)EquipmentFlags.MELEEPENALTY] = true;
                }

                break;
            case ItemTypes.ARMOR:
                Armor arm = genItem as Armor;
                arm.armorType = (ArmorTypes)UnityEngine.Random.Range((int)ArmorTypes.LIGHT, (int)ArmorTypes.COUNT);
                break;
            case ItemTypes.OFFHAND:
                Offhand oh = genItem as Offhand;
                int roll = UnityEngine.Random.Range(0, 4);

                oh.blockChance = 0f;
                oh.blockDamageReduction = 1f;

#if UNITY_EDITOR
                /* GameMasterScript.gmsSingleton.SetTempGameData("forcequiver", 1);
                Debug.LogError("DEBUG - GENERATING QUIVERS ONLY");
                oh.allowBow = true;                
                break; */
#endif

                if (roll >= 0 && roll <= 1)
                {
                    oh.blockChance = 0.01f; // its a shield
                }
                else if (roll == 2)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("forcequiver", 1);
                    oh.allowBow = true;
                }
                else
                {
                    // must be magic book
                    GameMasterScript.gmsSingleton.SetTempGameData("forcebook", 1);
                }
                break;
        }
    }

    // scaleitem, scalerelic, scaleleg
    public static Equipment CreateLevelScaledVersionOfRelic(Equipment relicToScale)
    {
        Equipment scaledItem = null;

        switch (relicToScale.itemType)
        {
            case ItemTypes.WEAPON:
                scaledItem = new Weapon();
                break;
            case ItemTypes.ARMOR:
                scaledItem = new Armor();
                break;
            case ItemTypes.OFFHAND:
                scaledItem = new Offhand();
                break;
            case ItemTypes.ACCESSORY:
                scaledItem = new Accessory();
                break;
        }

        int targetLevel = GameMasterScript.heroPCActor.myStats.GetLevel();
        if (!MysteryDungeonManager.GetActiveDungeon().resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            targetLevel = GameMasterScript.heroPCActor.myMysteryDungeonData.statsPriorToEntry.GetLevel();
        }

        //modify level so it's not always OP 100% of the time.
        float minLevelMult = MysteryDungeonManager.GetActiveDungeon().minScaledRelicValueMultiplier;
        float maxLevelMult = MysteryDungeonManager.GetActiveDungeon().maxScaledRelicValueMultiplier;

        int modifiedLevel = (int)(targetLevel * UnityEngine.Random.Range(minLevelMult , maxLevelMult));

        if (modifiedLevel < 1) modifiedLevel = 1;
        if (modifiedLevel > targetLevel) modifiedLevel = targetLevel;

        float targetCV = BalanceData.LEVEL_TO_CV[targetLevel];

        if (Debug.isDebugBuild) Debug.Log("Player true level is: " + targetLevel + ", target level is " + modifiedLevel + " CV is: " + targetCV + " Mods prior to removal: " + scaledItem.mods.Count);

        // Copy core data.
        scaledItem.CopyFromItem(relicToScale);

        if (scaledItem.itemType == ItemTypes.OFFHAND)
        {
            Offhand oh = scaledItem as Offhand;
            if (oh.IsMagicBook())
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forcebook", 1);
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forcebook", 0);
            }
            if (oh.IsShield())
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forceshield", 1);
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forceshield", 0);
            }
            if (oh.IsQuiver())
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forcequiver", 1);
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("forcequiver", 0);
            }
        }
        
        // remove "automods" that are upgrades based on rarity
        scaledItem.mods.RemoveAll(m => Equipment.modsThatCountAsAutoMods.Contains(m.refName));

        if (scaledItem.itemType == ItemTypes.OFFHAND)
        {
            scaledItem.mods.RemoveAll(m => Equipment.inherentOffhandMods.Contains(m.refName));
        }

        // Now adjust base properties based on intended cv (rank)
        scaledItem.SetBaseItemStatsByCV(targetCV, -1);
        scaledItem.upgradesByRarity = 0;
        scaledItem.challengeValue = targetCV;
       
        int maxPossibleMods = GetMaximumTotalModsByCV(targetCV);
        
        while (scaledItem.GetNonAutomodOrUpgradeCount() > maxPossibleMods)
        {
            MagicMod mmToRemove = scaledItem.mods.GetRandomElement();
            scaledItem.mods.Remove(mmToRemove);
            //Debug.Log("Removed " + mmToRemove.refName);
        }

        CheckForAndReplaceInvalidMods(scaledItem, targetCV);

        // Go through and make power, dodge, resist adjustments based on mods we have
        Weapon w = scaledItem as Weapon;
        bool isWeapon = w != null;
        foreach (MagicMod mm in scaledItem.mods)
        {
            foreach(ResistanceData rd in mm.resists)
            {
                scaledItem.AddResistanceFromData(rd);
            }
            if (isWeapon)
            {
                float changePowerAmount = 0;
                if (mm.changePowerAsPercent)
                {
                    changePowerAmount = mm.changePower * w.power;
                }
                else
                {
                    changePowerAmount = mm.changePower;
                }
                w.power += changePowerAmount;
            }
        }

        // And re-apply power enhancements by rarity.
        int totalRarityUpgrades = scaledItem.mods.Count;
        for (int i = 0; i < totalRarityUpgrades; i++)
        {
            scaledItem.IncreasePowerFromRarityBoost(scaledItem.mods[i]);
        }

        if (scaledItem.itemType == ItemTypes.OFFHAND)
        {
            CleanUpOffhandWeirdness(scaledItem);
        }

        if (Debug.isDebugBuild) Debug.Log("Scaled item cv: " + scaledItem.challengeValue + " with total mods " + scaledItem.mods.Count);

        scaledItem.SetUniqueIDAndAddToDict();

        return scaledItem;
    }

    /// <summary>
    /// Makes sure that ONLY shields have block chance/block dmg reduction, and makes sure values don't exceed 100% (??)
    /// </summary>
    /// <param name="item"></param>
    static void CleanUpOffhandWeirdness(Item item)
    {
        Offhand oh = item as Offhand;
        if (oh == null)
        {
            return;
        }

        if (oh.allowBow)
        {
            oh.blockChance = 0f;
            oh.blockDamageReduction = 1f;
        }
        else if (oh.blockChance == 0f)
        {
            oh.blockDamageReduction = 0.65f;
        }
        if (oh.blockDamageReduction <= 0.33f)
        {
            oh.blockDamageReduction = 0.33f; // this probably shouldn't happen anyway?
        }
    }
    
    static void CheckForAndReplaceInvalidMods(Equipment scaledItem, float targetCV)
    {
        Dictionary<MagicMod, ERelicModTypes> modsToReroll = new Dictionary<MagicMod, ERelicModTypes>(); // bool=true means it's a legendary-only mod
        

        foreach (MagicMod mm in scaledItem.mods)
        {
            if (Equipment.modsThatCountAsAutoMods.Contains(mm.refName) || Equipment.inherentOffhandMods.Contains(mm.refName))
            {
                continue;
            }

            ERelicModTypes rModType = ERelicModTypes.LEGONLY;
            LegModData lmd;
            if (!LegendaryMaker.legendaryOnlyMods.TryGetValue(mm.refName, out lmd))
            {
                // Maybe it's a "special" type?
                rModType = ERelicModTypes.SPECIALMOD;
                LegendaryMaker.specialNonLegMods.TryGetValue(mm.refName, out lmd);
                if (lmd == null)
                {
                    // Must be a regular ol' mod.
                    rModType = ERelicModTypes.REGULARMOD;
                    // make temporary mod data representing this resgular mod
                    lmd = new LegModData(new Vector2(mm.challengeValue, mm.maxChallengeValue));
                }
            }
            if (lmd == null)
            {
                Debug.LogError("Couldn't find a reference to mod: " + mm.refName);
            }

            // If our new target CV is lower than the minimum CV, or higher than maximum CV, for this mod...
            // Even with a bit of "lax factor"...
            // Set it aside to reroll later.
            //Debug.Log("For " + mm.refName + " compare item value " + targetCV + " to range " + lmd.challengeRange);
            if (targetCV < lmd.challengeRange.x-0.1f || targetCV > lmd.challengeRange.y+0.1f)
            {
                if (lmd.challengeRange.x < 10f || lmd.challengeRange.y < 10f)
                {
                    modsToReroll.Add(mm, rModType);
                }                
            }
        }

        int numRegularModsToReplace = 0;
        int numSpecialModsToReplace = 0;
        int numLegOnlyModsToReplace = 0;
        foreach(MagicMod mm in modsToReroll.Keys)
        {
            scaledItem.mods.Remove(mm);
            if (modsToReroll[mm] == ERelicModTypes.LEGONLY) numLegOnlyModsToReplace++;
            else if (modsToReroll[mm] == ERelicModTypes.SPECIALMOD) numSpecialModsToReplace++;
            else numRegularModsToReplace++;
        }

        int numTotalModsToAdd = numSpecialModsToReplace + numLegOnlyModsToReplace + numRegularModsToReplace;
        if (numTotalModsToAdd > 0)
        {
            scaledItem.AddModsToLegendary(numTotalModsToAdd, numLegOnlyModsToReplace, false);
        }

        List<Item> itemsToAdjust = new List<Item>();
        itemsToAdjust.Add(scaledItem);
        Item iToEdit = null;
        if (SharedBank.allRelicTemplates.TryGetValue(scaledItem.actorRefName, out iToEdit))
        {
            itemsToAdjust.Add(iToEdit);
        }
        if (GameMasterScript.masterItemList.TryGetValue(scaledItem.actorRefName, out iToEdit))
        {
            itemsToAdjust.Add(iToEdit);
        }

        List<string> autoModRefsToRemove = new List<string>();
        autoModRefsToRemove.Clear();
        foreach (string aModRef in scaledItem.autoModRef)
        {
            bool valid = false;
            foreach (MagicMod mm in scaledItem.mods)
            {
                if (mm.refName == aModRef)
                {
                    valid = true;
                    break;
                }
            }

            if (!valid)
            {
                autoModRefsToRemove.Add(aModRef);
            }
        }

        foreach (string modRef in autoModRefsToRemove)
        {
            scaledItem.autoModRef.Remove(modRef);
        }

        // go through every version of this item and remove autoMods that we no longer have

        foreach (Item i in itemsToAdjust)
        {
            foreach(MagicMod mm in i.mods)
            {
                if (!scaledItem.autoModRef.Contains(mm.refName))
                {
                    Debug.Log("Removing unwanted mod " + mm.refName + " from level scaled item " + i.actorRefName + " " + i.displayName);
                }
            }
        }


    }
}

public partial class Equipment : Item
{
    public ELegendarySpriteTypes legSpriteType; // No need to serialize, this is only used for legendary item creation.

    public virtual void SetBaseItemStatsByCV(float cv, int rankMod)
    {

    }

    /// <summary>
    /// Picks a sprite based on item characteristics like slot, weapon type, or purely aesthetic stuff!
    /// </summary>
    public virtual void SelectSpriteAtRandom()
    {

    }

    public virtual void AddModsToLegendary(int numTotalMods, int numLegendaryMods, bool guaranteeSpecialMods = true)
    {
        //if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;
        int legModsLeft = numLegendaryMods;

        float chanceToAllowNonPreferredJob = 0.5f;
        float chanceOfSpecialMod = 0.2f;
        chanceOfSpecialMod += ((challengeValue - 1f) / 4f);

        int minSpecialMods = 1;
        int specialModsLeft = 1;

        if (!guaranteeSpecialMods)
        {
            minSpecialMods = 0;
            specialModsLeft = 0;
        }

        // Construct lists of possible mods

        List<LegModData> possibleLegendaryMods = new List<LegModData>();
        List<LegModData> possibleSpecialMods = new List<LegModData>();

        foreach(LegModData mmdata in LegendaryMaker.specialModsSortedByCV)
        {
            if (mmdata.challengeRange.x > challengeValue) break;
            if (!mmdata.possibleSlots.Contains(EquipmentSlots.ANY) && !mmdata.possibleSlots.Contains(slot))
            {
                continue;
            }
            if (mmdata.preferredJob != CharacterJobs.COUNT && mmdata.preferredJob != GameMasterScript.heroPCActor.myJob.jobEnum && UnityEngine.Random.Range(0,1f) <= chanceToAllowNonPreferredJob)
            {
                continue;
            }
            if (itemType == ItemTypes.WEAPON && mmdata.weaponFilter != EWeaponFilterProperties.ANY)
            {
                Weapon w = this as Weapon;
                if (w.isRanged && mmdata.weaponFilter == EWeaponFilterProperties.ONLY_MELEE) continue;
                if (!w.isRanged && mmdata.weaponFilter == EWeaponFilterProperties.ONLY_RANGED) continue;
                if (w.weaponType == WeaponTypes.AXE && mmdata.weaponFilter == EWeaponFilterProperties.NO_AXES) continue;
            }

            if (HasModByRef(mmdata.refName)) continue;

            possibleSpecialMods.Add(mmdata);
        }
        foreach (LegModData mmdata in LegendaryMaker.legendaryOnlyModsSortedByCV)
        {
            if (mmdata.challengeRange.x > challengeValue) break;
            if (!mmdata.possibleSlots.Contains(EquipmentSlots.ANY) && !mmdata.possibleSlots.Contains(slot))
            {
                continue;
            }

            if (itemType == ItemTypes.WEAPON && mmdata.weaponFilter != EWeaponFilterProperties.ANY)
            {
                Weapon w = this as Weapon;
                if (w.isRanged && mmdata.weaponFilter == EWeaponFilterProperties.ONLY_MELEE) continue;
                if (!w.isRanged && mmdata.weaponFilter == EWeaponFilterProperties.ONLY_RANGED) continue;
                if (w.weaponType == WeaponTypes.AXE && mmdata.weaponFilter == EWeaponFilterProperties.NO_AXES) continue;
            }

            if (HasModByRef(mmdata.refName)) continue;

            possibleLegendaryMods.Add(mmdata);
        }

        //Debug.Log("For item " + itemType + " " + challengeValue + ", there are " + possibleLegendaryMods.Count + " leg mods and " + possibleSpecialMods.Count + " possible special mods.");
        //Debug.Log("Adding " + minSpecialMods + " special mods, " + numLegendaryMods + " leg mods out of " + numTotalMods + " total!!!");

        List<int> exclusionGroups = new List<int>();
        foreach(MagicMod mm in mods)
        {
            if (mm.exclusionGroup != 0 && !exclusionGroups.Contains(mm.exclusionGroup))
            {
                exclusionGroups.Add(mm.exclusionGroup);
            }
        }

        for (int i = 0; i < numTotalMods; i++)
        {
            MagicMod modSelected = null;

            if (legModsLeft > 0 && possibleLegendaryMods.Count > 0)
            {
                LegModData lmd = null;
                while (true)
                {
                    lmd = possibleLegendaryMods.GetRandomElement();

                    // Make sure this legendary mod can be added to this item in terms of CV, exclusions.
                    if (lmd.challengeRange.y < challengeValue || 
                        (lmd.modObjectRef.exclusionGroup != 0 && exclusionGroups.Contains(lmd.modObjectRef.exclusionGroup)))
                    {
                        possibleLegendaryMods.Remove(lmd);
                        continue;
                    }
                    // If we're here, we found a good mod!
                    modSelected = lmd.modObjectRef;
                    break;
                }               
                possibleLegendaryMods.Remove(lmd);
                legModsLeft--;
            }
            else
            {
                // Add a special mod, or a regular ol' mod?
                if ((UnityEngine.Random.Range(0,1f) <= chanceOfSpecialMod || specialModsLeft > 0) && possibleSpecialMods.Count > 0)
                {
                    LegModData lmd = null;
                    while (true)
                    {
                        lmd = possibleSpecialMods.GetRandomElement();

                        // Make sure this legendary mod can be added to this item in terms of CV, exclusions.
                        if (lmd.challengeRange.y < challengeValue ||
                            (lmd.modObjectRef.exclusionGroup != 0 && exclusionGroups.Contains(lmd.modObjectRef.exclusionGroup)))
                        {
                            possibleSpecialMods.Remove(lmd);
                            continue;
                        }
                        // If we're here, we found a good mod!
                        modSelected = lmd.modObjectRef;
                        break;
                    }
                    possibleSpecialMods.Remove(lmd);
                    specialModsLeft--;
                }
                else
                {
                    MagicMod modAdded = null;
                    // Must be adding a regular ol' mod.

                    if (UnityEngine.Random.Range(0,1) <= 0.04f) // Consider a nightmare or casino mod tho!!!
                    {
                        if (UnityEngine.Random.Range(0,5) != 0) // // Nightmare - higher chance
                        {
                            modAdded = EquipmentBlock.MakeMagicalFromModFlag(this, MagicModFlags.NIGHTMARE, true);
                        }
                        else // Casino: 33% chance
                        {
                            modAdded = EquipmentBlock.MakeMagicalFromModFlag(this, MagicModFlags.CASINO, true, "mm_fate");
                        }
                    }
                    else
                    {
                        // regular ol' mod
                        modAdded = EquipmentBlock.MakeMagical(this, challengeValue, true, rebuildDisplayName: false);
                    }
                    autoModRef.Add(modAdded.refName);
                    exclusionGroups.Add(modAdded.exclusionGroup);
                    if (upgradesByRarity < numTotalMods - 1)
                    {
                        IncreasePowerFromRarityBoost(modAdded);
                    }
                    //Debug.Log("Mod " + i + "added: " + modAdded.refName);
                    continue;
                }
            }

            // If we're here, we did a legendary or special mod.
            EquipmentBlock.MakeMagicalFromMod(this, modSelected, false, true, true);
            if (modSelected.exclusionGroup != 0)
            {
                exclusionGroups.Add(modSelected.exclusionGroup);
            }
            if (autoModRef == null)
            {
                autoModRef = new List<string>();
            }
            autoModRef.Add(modSelected.refName);
            if (upgradesByRarity < numTotalMods-1)
            {
                IncreasePowerFromRarityBoost(modSelected);
            }
            //Debug.Log("Mod " + i + "added: " + modSelected.refName);
        }

        // Sort mods by text length.

        List<MagicMod> sortedList = new List<MagicMod>();

        mods = mods.OrderByDescending(m => CustomAlgorithms.StripColors(m.GetDescription()).Length).ToList();
        
        /* for (int i = 0; i < mods.Count; i++)
        {
            Debug.LogError(mods[i].GetDescription());
            string desc = CustomAlgorithms.StripColors(mods[i].GetDescription());
            Debug.Log(desc.Length + " " + desc);
        }  */
    }
}

public partial class Weapon : Equipment
{
    public void SetSwingAndImpactAnimations()
    {
        Weapon w = this;

        w.swingEffect = "GenericSwingEffect";
        w.impactEffect = "";

        switch (w.weaponType)
        {
            case WeaponTypes.SWORD:
                w.impactEffect = "FervirSwordEffect";
                break;
            case WeaponTypes.STAFF:
                w.impactEffect = "ProjectileImpactEffect";
                w.SetProjectileEffect();                
                break;
            case WeaponTypes.SPEAR:
                w.impactEffect = "FervirPierceEffect";
                w.range = 2;
                w.eqFlags[(int)EquipmentFlags.RANGEPENALTY] = true;
                break;
            case WeaponTypes.DAGGER:
                w.impactEffect = "FervirPierceEffect";
                break;
            case WeaponTypes.AXE:
                w.impactEffect = "FervirAxeEffect";
                break;
            case WeaponTypes.CLAW:
                w.impactEffect = "FervirClawEffect";
                break;
            case WeaponTypes.BOW:
                SetProjectileEffect();
                break;
            case WeaponTypes.MACE:
                w.impactEffect = "FervirBluntEffect";
                break;
            case WeaponTypes.WHIP:
                w.impactEffect = "FervirWhipEffect";
                break;
            case WeaponTypes.NATURAL:
                w.impactEffect = "FervirPunchEffectSystem";
                break;
        }
    }

    void SetProjectileEffect()
    {        
        swingEffect = LegendaryMaker.projectilePrefabs[weaponType].GetRandomElement();

        // If we're elemental, use an elemental prefab instead.
        //if (UnityEngine.Random.Range(0, 1f) && damType != DamageTypes.PHYSICAL)
        {
            switch (damType)
            {
                case DamageTypes.FIRE:
                    swingEffect = "FireBall";
                    break;
                case DamageTypes.LIGHTNING:
                    swingEffect = "LightningBolt";
                    break;
                case DamageTypes.POISON:
                    swingEffect = "PoisonBolt";
                    break;
                case DamageTypes.WATER:
                    swingEffect = "WaterProjectile2";
                    break;
                case DamageTypes.SHADOW:
                    swingEffect = "ShadowBoltFast";
                    break;
            }
        }
    }

    public override void SelectSpriteAtRandom()
    {
        if (UnityEngine.Random.Range(0,1f) <= 0.05f) // chance of using an instrument sprite instead = lol
        {
            legSpriteType = ELegendarySpriteTypes.INSTRUMENT;            
        }
        else
        {
            switch(weaponType)
            {
                case WeaponTypes.SWORD:
                    legSpriteType = ELegendarySpriteTypes.SWORD;                    
                    break;
                case WeaponTypes.AXE:
                    legSpriteType = ELegendarySpriteTypes.AXE;
                    break;
                case WeaponTypes.STAFF:
                    legSpriteType = ELegendarySpriteTypes.STAFF;
                    break;
                case WeaponTypes.DAGGER:
                    legSpriteType = ELegendarySpriteTypes.DAGGER;
                    break;
                case WeaponTypes.CLAW:
                    legSpriteType = ELegendarySpriteTypes.CLAW;
                    break;
                case WeaponTypes.SPEAR:
                    legSpriteType = ELegendarySpriteTypes.SPEAR;
                    break;
                case WeaponTypes.BOW:
                    legSpriteType = ELegendarySpriteTypes.BOW;
                    break;
                case WeaponTypes.MACE:
                    legSpriteType = ELegendarySpriteTypes.MACE;
                    break;
                case WeaponTypes.WHIP:
                    legSpriteType = ELegendarySpriteTypes.WHIP;
                    break;
                case WeaponTypes.NATURAL:
                    legSpriteType = ELegendarySpriteTypes.HANDWRAP;
                    break;
            }
        }

        spriteRef = "assorteditems_" + LegendaryMaker.legPossibleSpritesByType[legSpriteType].GetRandomElement().ToString();
    }

    public override void SetBaseItemStatsByCV(float cv, int rankMod)
    {
        // Figure out the weapon's power based on existing weapons.
        int convertedRank = BalanceData.ConvertChallengeValueToRank(cv);
        convertedRank -= rankMod;
        if (convertedRank < 1) convertedRank = 1;
        if (convertedRank > 13) convertedRank = 13;

        Dictionary<int, float> powerByRankForType = BalanceData.weaponPowersByRank[weaponType];        
        power = powerByRankForType[convertedRank];
        
        power *= 0.85f; // Adjustment because Relic weapons are already pretty strong.

        if (twoHanded && weaponType != WeaponTypes.BOW)
        {
            power *= 1.17f; // Multiplier for 2H weapons.
        }        

        if (weaponType == WeaponTypes.BOW)
        {
            range = 4;
        }
        else if (weaponType == WeaponTypes.STAFF)
        {
            range = 3;
        }
        else if (weaponType == WeaponTypes.SPEAR)
        {
            range = 2;
        }
        else if (weaponType == WeaponTypes.NATURAL)
        {
            power = 0f;
            flavorDamType = FlavorDamageTypes.BLUNT;
            SetActorData("monkweapon", 1);
            switch (convertedRank)
            {
                case 1:
                case 2:
                case 3:
                    SetActorData("maxmods", 2);
                    break;
                case 4:
                case 5:
                case 6:
                    SetActorData("maxmods", 3);
                    break;
                case 7:
                case 8:
                case 9:
                    SetActorData("maxmods", 4);
                    break;
                case 10:
                case 11:
                case 12:
                case 13:
                    SetActorData("maxmods", 5);
                    break;

            }
        }

    }

    public override void AddModsToLegendary(int numTotalMods, int numLegendaryMods, bool guaranteeSpecialMods = true)
    {
        base.AddModsToLegendary(numTotalMods, numLegendaryMods, guaranteeSpecialMods);
    }
}

public partial class Armor : Equipment
{
    public override void SelectSpriteAtRandom()
    {
        switch(armorType)
        {
            case ArmorTypes.LIGHT:
                legSpriteType = ELegendarySpriteTypes.LIGHTARMOR;
                break;
            case ArmorTypes.MEDIUM:
                legSpriteType = ELegendarySpriteTypes.MEDIUMARMOR;
                break;
            case ArmorTypes.HEAVY:
                legSpriteType = ELegendarySpriteTypes.HEAVYARMOR;
                break;
        }

        spriteRef = "assorteditems_" + LegendaryMaker.legPossibleSpritesByType[legSpriteType].GetRandomElement().ToString();
    }

    public override void SetBaseItemStatsByCV(float cv, int rankMod)
    {
        // Figure out the armor's power based on existing weapons.
        int convertedRank = BalanceData.ConvertChallengeValueToRank(cv);
        convertedRank -= rankMod;
        if (convertedRank < 1) convertedRank = 1;
        if (convertedRank > 13) convertedRank = 13;

        int dodgeAmount = 0;
        int physicalOffset = 0;
        float physicalResist = 1f;
        resists.Clear(); // clearing our resists is ok because we are starting from scratch right?

        if (armorType == ArmorTypes.LIGHT || armorType == ArmorTypes.MEDIUM)
        {
            dodgeAmount = BalanceData.relicArmorDodgeAmountsByRank[armorType][convertedRank];
        }
        if (armorType == ArmorTypes.MEDIUM || armorType == ArmorTypes.HEAVY)
        {
            physicalResist = BalanceData.relicArmorPhysicalResistByRank[armorType][convertedRank];
            physicalOffset = BalanceData.relicArmorResistPhysicalOffsetsByRank[armorType][convertedRank];
            ResistanceData prd = new ResistanceData();
            prd.damType = DamageTypes.PHYSICAL;
            prd.flatOffset = physicalOffset;
            prd.multiplier = physicalResist;
            resists.Add(prd);
        }

        extraDodge = dodgeAmount;
    }

    public override void AddModsToLegendary(int numTotalMods, int numLegendaryMods, bool guaranteeSpecialMods = true)
    {
        base.AddModsToLegendary(numTotalMods, numLegendaryMods, guaranteeSpecialMods);
    }
}

public partial class Accessory : Equipment
{
    public override void SelectSpriteAtRandom()
    {
        string subType = LegendaryMaker.accessorySubtypes.GetRandomActorRef();
        switch(subType)
        {
            case "instrument":
                legSpriteType = ELegendarySpriteTypes.INSTRUMENT;
                break;
            case "glove":
                legSpriteType = ELegendarySpriteTypes.GLOVE;
                break;
            case "helmet":
                legSpriteType = ELegendarySpriteTypes.HELMET;
                break;
            case "ring":
                legSpriteType = ELegendarySpriteTypes.RING;
                break;
            case "necklace":
                legSpriteType = ELegendarySpriteTypes.NECKLACE;
                break;
            case "misc":
                legSpriteType = ELegendarySpriteTypes.ACCESSORY;
                break;
        }

        spriteRef = "assorteditems_" + LegendaryMaker.legPossibleSpritesByType[legSpriteType].GetRandomElement().ToString();
    }

    public override void SetBaseItemStatsByCV(float cv, int rankMod)
    {
        int convertedRank = BalanceData.ConvertChallengeValueToRank(cv);
        convertedRank -= rankMod;
        if (convertedRank < 1) convertedRank = 1;
        if (convertedRank > 13) convertedRank = 13;
    }

    public override void AddModsToLegendary(int numTotalMods, int numLegendaryMods, bool guaranteeSpecialMods = true)
    {
        base.AddModsToLegendary(numTotalMods, numLegendaryMods, guaranteeSpecialMods);
    }
}

public partial class Offhand : Equipment
{
    public override void SelectSpriteAtRandom()
    {
        if (IsShield())
        {
            legSpriteType = ELegendarySpriteTypes.SHIELD;
        }
        else if (IsQuiver())
        {
            legSpriteType = ELegendarySpriteTypes.QUIVER;
        }
        else
        {
            legSpriteType = ELegendarySpriteTypes.BOOK;
        }

        spriteRef = "assorteditems_" + LegendaryMaker.legPossibleSpritesByType[legSpriteType].GetRandomElement().ToString();
    }

    public override void SetBaseItemStatsByCV(float cv, int rankMod)
    {
        int convertedRank = BalanceData.ConvertChallengeValueToRank(cv);
        convertedRank -= rankMod;
        if (convertedRank < 1) convertedRank = 1;
        if (convertedRank > 13) convertedRank = 13;

        blockChance = 0f;
        blockDamageReduction = 0f;

        if ((UnityEngine.Random.Range(0,3) != 0 && GameMasterScript.gmsSingleton.ReadTempGameData("forcequiver") != 1 && GameMasterScript.gmsSingleton.ReadTempGameData("forcebook") != 1) 
            || GameMasterScript.gmsSingleton.ReadTempGameData("forceshield") == 1) // it is now a shield
        {
            blockChance = 0.01f;
            allowBow = false;
        }

        if (IsQuiver())
        {
            AddModByRef(BalanceData.relicQuiverBaseModByRank[convertedRank], false);
        }
        else if (IsShield()) 
        {
            resists.Clear();

            ShieldBlockData sbd = BalanceData.relicShieldBlockDataByRank[convertedRank];

            blockChance = sbd.blockChance;
            blockDamageReduction = sbd.blockDamageReduction;
            ResistanceData prd = new ResistanceData();
            prd.damType = DamageTypes.PHYSICAL;
            prd.flatOffset = sbd.physicalFlatOffset;
            prd.multiplier = sbd.physicalResist;

            resists.Add(prd);
        }
        else // must be book
        {
            AddModByRef(BalanceData.relicMagicBookBaseModByrank[convertedRank], false);
        }

        GameMasterScript.gmsSingleton.SetTempGameData("forcequiver", 0);
        GameMasterScript.gmsSingleton.SetTempGameData("forceshield", 0);
        GameMasterScript.gmsSingleton.SetTempGameData("forcebook", 0);
    }

    public override void AddModsToLegendary(int numTotalMods, int numLegendaryMods, bool guaranteeSpecialMods = true)
    {
        base.AddModsToLegendary(numTotalMods, numLegendaryMods, guaranteeSpecialMods);
    }
}