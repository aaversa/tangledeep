using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;

public class TamedCorralMonster
{
    //public string displayName; Store this in the monster object. Redundant here.
    public string refName;
    public string family;
    public string catcherName;
    public Monster monsterObject;
    public List<string> loveFoods;
    public List<string> hateFoods;
    public List<string> knownLoveFoods;
    public List<string> knownHateFoods;
    public int beauty;
    public int unique;
    public int weight;
    public int happiness;

    int actualID;
    public int monsterID
    {
        get
        {
            return actualID;
        }
        set
        {
            /* Debug.Log(actualID + " is setting its id to " + value);
            if (monsterObject != null)
            {
                Debug.Log("It has an obj attached with id " + monsterObject.actorUniqueID);
            } */
            actualID = value;
        }
    }

    public int daysPassedAtLastEgg;

    public float baseMonsterHealth;
    public float highestHeroHealth; // deprecated

    public readonly int[] MAX_FOOD_METER_BY_LEVEL = new int[]{
        3, // level 1
        3,
        4,
        4,
        5, // level 5
        5,
        6,
        6,
        7,
        7, // level 10
        8,
        8,
        9,
        9,
        10, // level 15
        10,
        10,
        10,
        10,
        10
    };
    public const int MAX_HAPPINESS = 10;
    public const float BASE_EGG_CHANCE = 0.025f;
    public const int MIN_ATTRACTION = -5;
    public const int MAX_ATTRACTION = 10;

    public int foodMeter;

    public int inheritedMaxWeight;

    public int timesGroomed;

    public string parent1Name;
    public string parent2Name;

    /// <summary>
    /// If this is -1, it is *unassigned* and it MUST receive a new value.
    /// </summary>
    public int sharedBankID = -1;

    public Dictionary<int, int> attractionToMonsters;

    public bool CompareTo(TamedCorralMonster otherStuff)
    {
        if (sharedBankID == otherStuff.sharedBankID) return true;

        if (foodMeter != otherStuff.foodMeter) return false;
        if (timesGroomed != otherStuff.timesGroomed) return false;
        if (parent1Name != otherStuff.parent1Name) return false;
        if (parent2Name != otherStuff.parent2Name) return false;
        if (refName != otherStuff.refName) return false;
        if (family != otherStuff.family) return false;
        if (catcherName != otherStuff.catcherName) return false;
        if (unique != otherStuff.unique) return false;
        if (beauty != otherStuff.beauty) return false;
        if (weight != otherStuff.weight) return false;

        return true;
    }


    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("tamedcorralmonster");

        //writer.WriteElementString("displayname", displayName);
        writer.WriteElementString("refname", refName);
        writer.WriteElementString("catchername", catcherName);
        writer.WriteElementString("id", monsterID.ToString());
        writer.WriteElementString("sharedbankid", sharedBankID.ToString());
        writer.WriteElementString("family", family);
        writer.WriteElementString("beauty", beauty.ToString());
        writer.WriteElementString("unique", unique.ToString());
        writer.WriteElementString("weight", weight.ToString());
        writer.WriteElementString("happiness", happiness.ToString());
        writer.WriteElementString("foodmeter", foodMeter.ToString());
        writer.WriteElementString("inheritedmaxweight", inheritedMaxWeight.ToString());
        writer.WriteElementString("dayspassedatlastegg", daysPassedAtLastEgg.ToString());
        writer.WriteElementString("parent1", parent1Name);
        writer.WriteElementString("parent2", parent2Name);
        writer.WriteElementString("timesgroomed", timesGroomed.ToString());
        writer.WriteElementString("basemonsterhealth", baseMonsterHealth.ToString());
        writer.WriteElementString("highestherohealth", highestHeroHealth.ToString());
        foreach (string food in loveFoods)
        {
            writer.WriteElementString("love", food);
        }
        foreach (string food in hateFoods)
        {
            writer.WriteElementString("hate", food);
        }
        foreach (string food in knownLoveFoods)
        {
            writer.WriteElementString("klove", food);
        }
        foreach (string food in knownHateFoods)
        {
            writer.WriteElementString("khate", food);
        }
        foreach (int dKey in attractionToMonsters.Keys)
        {
            bool foundMonster = false;
            foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
            {
                if (tcm.sharedBankID == dKey)
                {
                    foundMonster = true;
                }
            }
            if (!foundMonster)
            {
                continue;
            }
            writer.WriteStartElement("attractiontomonster");
            writer.WriteElementString("id", dKey.ToString());
            writer.WriteElementString("amt", attractionToMonsters[dKey].ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void AdjustWeightFromTemplate()
    {
        if (monsterObject != null)
        {
            float minWeight = (float)monsterObject.myTemplate.weight * 0.4f;
            float maxWeight = (float)monsterObject.myTemplate.weight * 2.5f;
            float fWeight = UnityEngine.Random.Range(minWeight, maxWeight);
            weight = (int)fWeight;
        }
    }

    public int CalculateFoodThresholdForPet()
    {
        int lvlForThreshold = monsterObject.myStats.GetLevel();

        if (lvlForThreshold >= MAX_FOOD_METER_BY_LEVEL.Length)
        {
            lvlForThreshold = MAX_FOOD_METER_BY_LEVEL.Length - 1;
        }

        int amt = MAX_FOOD_METER_BY_LEVEL[lvlForThreshold];

        return amt;
    }

    public int CalculateHappinessThresholdForPet()
    {
        int baseNum = monsterObject.myStats.GetLevel();

        if (baseNum >= MonsterCorralScript.singleton.THRESHOLD_MONSTER_PET_HAPPINESS_BY_MONLEVEL.Length)
        {
            baseNum = MonsterCorralScript.singleton.THRESHOLD_MONSTER_PET_HAPPINESS_BY_MONLEVEL.Length - 1;
        }

        int happyThreshold = MonsterCorralScript.singleton.THRESHOLD_MONSTER_PET_HAPPINESS_BY_MONLEVEL[baseNum];
        if (happyThreshold >= MAX_HAPPINESS)
        {
            happyThreshold = MAX_HAPPINESS;
        }

        return happyThreshold;
    }

    public bool IsAngryAtPlayer()
    {
        int dayAtDeath = monsterObject.ReadActorData("day_at_uninsured_death");
        if (dayAtDeath > MetaProgressScript.totalDaysPassed)
        {
            dayAtDeath = MetaProgressScript.totalDaysPassed - 1;
            monsterObject.SetActorData("day_at_uninsured_death", dayAtDeath);
        }
        if (dayAtDeath <= 0) return false;
        int daysPassed = MetaProgressScript.totalDaysPassed - dayAtDeath;
        if (daysPassed < GameMasterScript.MONSTER_PET_ANGRY_THRESHOLD_DAYS)
        {
            return true;
        }
        return false;
    }

    public bool CanMonsterBePet()
    {
        int lvlForThreshold = CalculateHappinessThresholdForPet();
        if (happiness < lvlForThreshold)
        {
            return false;
        }
        return true;
    }
    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        string fUnparsed = "";

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name.ToLowerInvariant())
            {
                case "displayname":
                    reader.ReadElementContentAsString();
                    break;
                case "refname":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "catchername":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        catcherName = reader.ReadElementContentAsString();
                    }
                    break;
                case "sharedbankid":
                    sharedBankID = reader.ReadElementContentAsInt();
                    //if (Debug.isDebugBuild) Debug.Log("Shared bank ID read as " + sharedBankID + " for " + refName);
                    break;
                case "id":
                    monsterID = reader.ReadElementContentAsInt();
                    break;
                case "family":
                    family = reader.ReadElementContentAsString();
                    break;
                case "unique":
                    unique = reader.ReadElementContentAsInt();
                    break;
                case "happiness":
                    happiness = reader.ReadElementContentAsInt();
                    break;
                case "weight":
                    weight = reader.ReadElementContentAsInt();
                    break;
                case "beauty":
                    beauty = reader.ReadElementContentAsInt();
                    break;
                case "foodmeter":
                    foodMeter = reader.ReadElementContentAsInt();
                    break;
                case "inheritedmaxweight":
                    inheritedMaxWeight = reader.ReadElementContentAsInt();
                    break;
                case "parent1":
                    parent1Name = reader.ReadElementContentAsString();
                    break;
                case "parent2":
                    parent2Name = reader.ReadElementContentAsString();
                    break;
                case "dayspassedatlastegg":
                    daysPassedAtLastEgg = reader.ReadElementContentAsInt();
                    break;
                case "timesgroomed":
                    timesGroomed = reader.ReadElementContentAsInt();
                    break;
                case "love":
                case "lovefood":
                    loveFoods.Add(reader.ReadElementContentAsString());
                    break;
                case "hate":
                case "hatefood":
                    hateFoods.Add(reader.ReadElementContentAsString());
                    break;
                case "khate":
                    knownHateFoods.Add(reader.ReadElementContentAsString());
                    break;
                case "klove":
                    knownLoveFoods.Add(reader.ReadElementContentAsString());
                    break;
                case "basemonsterhealth":
                    fUnparsed = reader.ReadElementContentAsString();
                    baseMonsterHealth = CustomAlgorithms.TryParseFloat(fUnparsed);
                    break;
                case "highestherohealth":
                    fUnparsed = reader.ReadElementContentAsString();
                    highestHeroHealth = CustomAlgorithms.TryParseFloat(fUnparsed);
                    break;
                case "attractiontomonster":
                    reader.ReadStartElement();
                    int actorID = 0;
                    int attractionLevel = 0;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "id":
                                actorID = reader.ReadElementContentAsInt();
                                break;
                            case "amt":
                                attractionLevel = reader.ReadElementContentAsInt();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    if (!attractionToMonsters.ContainsKey(actorID))
                    {
                        AddMonsterToAttractionDict(actorID, attractionLevel);
                    }
                    else
                    {
                        Debug.Log(monsterID + " " + monsterObject.displayName + " already has attraction data for " + actorID + ", cannot add it again");
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

    public TamedCorralMonster()
    {
        loveFoods = new List<string>();
        hateFoods = new List<string>();
        knownLoveFoods = new List<string>();
        knownHateFoods = new List<string>();
        beauty = UnityEngine.Random.Range(0, 101);
        unique = UnityEngine.Random.Range(0, 101);
        weight = UnityEngine.Random.Range(1, 81);
        happiness = 0;
        foodMeter = 0;
        attractionToMonsters = new Dictionary<int, int>();
        sharedBankID = -1;
    }

    public void SetFoodPreferences()
    {
        int numLoves = UnityEngine.Random.Range(5, 8);
        int numHates = UnityEngine.Random.Range(2, 5);
        loveFoods.Clear();
        hateFoods.Clear();

        for (int i = 0; i < numLoves; i++)
        {
            ActorTable foodTable = LootGeneratorScript.GetLootTable("food_and_meals");
            string foodRef = foodTable.GetRandomActorRefNonWeighted();
            while (loveFoods.Contains(foodRef) || foodRef == "food_monsterchow" || foodRef == "item_summonfood")
            {
                foodRef = foodTable.GetRandomActorRefNonWeighted();
            }
            loveFoods.Add(foodRef);
        }

        for (int i = 0; i < numHates; i++)
        {
            ActorTable foodTable = LootGeneratorScript.GetLootTable("food_and_meals");
            string foodRef = foodTable.GetRandomActorRefNonWeighted();
            while (loveFoods.Contains(foodRef) || hateFoods.Contains(foodRef) || foodRef == "food_monsterchow" || foodRef == "item_summonfood")
            {
                foodRef = foodTable.GetRandomActorRefNonWeighted();
            }
            hateFoods.Add(foodRef);
        }
    }

    public Rarity GetMonsterRarity()
    {
        if (unique < 70)
        {
            return Rarity.COMMON;
        }
        else if (unique >= 70 && unique <= 90)
        {
            return Rarity.UNCOMMON;
        }
        else if (unique >= 90 && unique <= 95)
        {
            return Rarity.MAGICAL;
        }
        else if (unique >= 96 && unique <= 99)
        {
            return Rarity.ANCIENT;
        }
        else
        {
            return Rarity.ARTIFACT;
        }
    }

    public static string GetRarityStringByValue(int rarityValue)
    {
        if (rarityValue < 70)
        {
            return StringManager.GetString("misc_rarity_0");
        }
        else if (rarityValue >= 70 && rarityValue <= 90)
        {
            return UIManagerScript.blueHexColor + StringManager.GetString("misc_rarity_1") + "</color>";
        }
        else if (rarityValue >= 90 && rarityValue <= 95)
        {
            return "<color=yellow>" + StringManager.GetString("misc_rarity_2") + "</color>";
        }
        else if (rarityValue >= 96 && rarityValue <= 99)
        {
            return UIManagerScript.orangeHexColor + StringManager.GetString("corral_rarity_3") + "</color>";
        }
        else
        {
            return UIManagerScript.purpleHexColor + StringManager.GetString("corral_rarity_4") + "</color>";
        }
    }

    public string GetRarityString()
    {
        return GetRarityStringByValue(unique);
    }

    public string GetBeautyString()
    {
        string beautyString = "";
        if (beauty < 10)
        {
            beautyString = UIManagerScript.redHexColor + StringManager.GetString("corral_beauty_0") + "</color>";
        }
        else if (beauty >= 10 && beauty <= 20)
        {
            beautyString = UIManagerScript.redHexColor + StringManager.GetString("corral_beauty_1") + "</color>";
        }
        else if (beauty >= 21 && beauty <= 40)
        {
            beautyString = StringManager.GetString("corral_beauty_2");
        }
        else if (beauty > 40 && beauty <= 60)
        {
            beautyString = StringManager.GetString("corral_beauty_3");
        }
        else if (beauty > 61 && beauty <= 70)
        {
            beautyString = StringManager.GetString("corral_beauty_4");
        }
        else if (beauty > 71 && beauty <= 80)
        {
            beautyString = StringManager.GetString("corral_beauty_5");
        }
        else if (beauty > 81 && beauty <= 90)
        {
            beautyString = UIManagerScript.greenHexColor + StringManager.GetString("corral_beauty_6") + "</color>";
        }
        else
        {
            beautyString = UIManagerScript.orangeHexColor + StringManager.GetString("corral_beauty_7") + "</color>";
        }

        beautyString += " (" + beauty + ")";
        return beautyString;
    }

    public string GetWeightString()
    {
        return weight + StringManager.GetString("unit_weight");
    }

    public string GetHappinessString()
    {

        return GetHappinessString_Internal(happiness);
    }

    public string GetHappinessString_Internal(int checkAmount)
    {
        string retString = "";
        if (checkAmount == 0)
        {
            retString = UIManagerScript.redHexColor + StringManager.GetString("corral_happy_0") + "</color>";
        }
        else if (checkAmount >= 1 && checkAmount <= 2)
        {
            retString = StringManager.GetString("corral_happy_1");
        }
        else if (checkAmount >= 3 && checkAmount <= 4)
        {
            retString = StringManager.GetString("corral_happy_2");
        }
        else if (checkAmount == 5)
        {
            retString = StringManager.GetString("corral_relationship_neutral");
        }
        else if (checkAmount >= 6 && checkAmount <= 7)
        {
            retString = StringManager.GetString("corral_happy_4");
        }
        else if (checkAmount >= 8 && checkAmount <= 9)
        {
            retString = StringManager.GetString("corral_happy_5");
        }
        else
        {
            retString = UIManagerScript.greenHexColor + StringManager.GetString("corral_happy_6") + "</color>";
        }
        retString += " (" + checkAmount + ")";
        return retString;
    }

    public string GetMonsterInfo()
    {
        string builder = "";
        string uniqueString = GetRarityString();
        string beautyString = GetBeautyString();
        string happinessString = GetHappinessString();

        builder = UIManagerScript.cyanHexColor + "Weight:</color> " + weight + "kg " + UIManagerScript.cyanHexColor + "Uniqueness:</color> " + uniqueString + " " + UIManagerScript.cyanHexColor + "Beauty:</color> " + beautyString + "\n";
        builder += UIManagerScript.cyanHexColor + "Happiness:</color> " + happinessString;

        return builder;
    }

    public void FeedMonster(Item itm)
    {
        int baseFeedValue = 1;

        StringManager.ClearTags();
        StringManager.SetTag(0, monsterObject.displayName);
        StringManager.SetTag(1, itm.displayName);

        string logMessage = "";

        string soundToPlay = "";

        if (hateFoods.Contains(itm.actorRefName))
        {
            // Yuck!
            baseFeedValue = 0;
            if (!knownHateFoods.Contains(itm.actorRefName))
            {
                knownHateFoods.Add(itm.actorRefName);
            }
            logMessage = StringManager.GetString("log_corralmonster_hatefood");
            CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FireBreathSFX", null);
            monsterObject.myAnimatable.SetAnim("TakeDamage");
        }
        else if (loveFoods.Contains(itm.actorRefName))
        {
            baseFeedValue = 3;
            if (!knownLoveFoods.Contains(itm.actorRefName))
            {
                knownLoveFoods.Add(itm.actorRefName);
            }
            logMessage = StringManager.GetString("log_corralmonster_lovefood");
            //CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "FervirRecovery", null);
            CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "CharmEffectSystem", null);
            soundToPlay = "ShamanHeal";
        }
        else if (itm.actorRefName == "food_monsterchow")
        {
            if (happiness < MonsterCorralScript.MONSTERCHOW_MAX_THRESHOLD)
            {
                baseFeedValue = 10;
                ChangeBeauty(UnityEngine.Random.Range(1, 3));
                logMessage = StringManager.GetString("log_corralmonster_monsterchow");
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "CharmEffectSystem", null);
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "FervirRecovery", null);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "CharmEffectSystem", null, 0.7f);
                BattleTextManager.NewText(StringManager.GetString("misc_eatingsounds"), monsterObject.GetObject(), Color.green, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_eatingsounds"), monsterObject.GetObject(), Color.green, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_eatingsounds"), monsterObject.GetObject(), Color.green, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_eatingsounds"), monsterObject.GetObject(), Color.green, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_eatingsounds"), monsterObject.GetObject(), Color.green, 0f);
                soundToPlay = "ShamanHeal";
            }
            else
            {
                baseFeedValue = 0;
                logMessage = StringManager.GetString("log_corralmonster_monsterchow_sick");
                monsterObject.myAnimatable.SetAnim("TakeDamage");
                UIManagerScript.PlayCursorSound("CookingFailure");
            }
        }
        else
        {
            logMessage = StringManager.GetString("log_corralmonster_neutralfood");
            CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "FervirRecovery", null);
            soundToPlay = "Skill Learnt";
        }

        logMessage = CustomAlgorithms.ParseLiveMergeTags(logMessage);

        GameLogScript.GameLogWrite(logMessage, GameMasterScript.heroPCActor);

        foodMeter += baseFeedValue;

        if (foodMeter < 0)
        {
            foodMeter = 0;
        }

        int lvlForThreshold = (int)Mathf.Clamp(monsterObject.myStats.GetLevel(), 1f, (float)MAX_FOOD_METER_BY_LEVEL.Length);

        if (lvlForThreshold >= MAX_FOOD_METER_BY_LEVEL.Length)
        {
            lvlForThreshold = MAX_FOOD_METER_BY_LEVEL.Length - 1;
        }

        if (foodMeter >= MAX_FOOD_METER_BY_LEVEL[lvlForThreshold])
        {
            foodMeter = MAX_FOOD_METER_BY_LEVEL[lvlForThreshold];
            // Food event!
            MaxFoodEvent();
        }
        else
        {
            if (soundToPlay != "")
            {
                UIManagerScript.PlayCursorSound(soundToPlay);
            }
        }

        bool fullyRemove = false;

        if (itm.itemType == ItemTypes.CONSUMABLE)
        {
            Consumable con = itm as Consumable;
            if (!con.ChangeQuantity(-1))
            {
                fullyRemove = true;
            }
        }
        else
        {
            fullyRemove = true;
        }

        if (fullyRemove)
        {
            GameMasterScript.heroPCActor.myInventory.RemoveItem(itm);
            GameMasterScript.gmsSingleton.SetTempGameData("feedmonsteritem", -1);
        }
        else
        {
            GameMasterScript.gmsSingleton.SetTempGameData("feedmonsteritem", itm.actorUniqueID);
        }
    }

    public void ChangeHappiness(int amount)
    {
        int startHappiness = happiness;
        happiness += amount;

        if (happiness < 0)
        {
            happiness = 0;
        }

        if (happiness >= MAX_HAPPINESS)
        {
            happiness = MAX_HAPPINESS;
        }

        if (startHappiness < happiness)
        {
            ImproveHappinessEvent();
        }
    }

    public string GetStringListOfHatedFoods()
    {
        string build = "";
        for (int i = 0; i < knownHateFoods.Count; i++)
        {
            string refName = knownHateFoods[i];
            string dispName = GameMasterScript.masterItemList[refName].displayName;
            if (i == knownHateFoods.Count - 1)
            {
                build += dispName;
            }
            else
            {
                build += dispName + ", ";
            }
        }
        return build;
    }

    public string GetStringListOfLovedFoods()
    {
        string build = "";
        for (int i = 0; i < knownLoveFoods.Count; i++)
        {
            string refName = knownLoveFoods[i];
            string dispName = GameMasterScript.masterItemList[refName].displayName;
            if (i == knownLoveFoods.Count - 1)
            {
                build += dispName;
            }
            else
            {
                build += dispName + ", ";
            }
        }
        return build;
    }

    public void ChangeBeauty(int amount)
    {
        int oldBeauty = beauty;
        beauty += amount;
        if (beauty < 0)
        {
            beauty = 0;
        }
        if (beauty > 100)
        {
            beauty = 100;
        }
        //Debug.Log(monsterObject.actorRefName + " " + monsterObject.actorUniqueID + " " + beauty + " " + oldBeauty);
    }

    public void MaxFoodEvent()
    {
        ChangeHappiness(1);

        foodMeter = 0;

        if (weight > 5)
        {
            weight += (int)((weight * 0.03f));
        }

        int maxWeight = monsterObject.myTemplate.weight * 4;

        if (maxWeight == 0)
        {
            if (!String.IsNullOrEmpty(parent1Name))
            {
                maxWeight = inheritedMaxWeight;
            }
            else
            {
                maxWeight = 100;
            }
        }

        if (weight >= maxWeight)
        {
            weight = maxWeight;
        }

        StringManager.SetTag(0, monsterObject.displayName);
        StringManager.SetTag(1, GetHappinessString());

        GameLogScript.GameLogWrite(StringManager.GetString("log_corralmonster_happychangefromfood"), GameMasterScript.heroPCActor);
        UIManagerScript.PlayCursorSound("CasinoWin");
    }

    public string CheckTimePassed()
    {
        string baseString = "";
        int daysPassed = MetaProgressScript.totalDaysPassed - daysPassedAtLastEgg;
        if (daysPassed > 0)
        {
            float dayMult = (float)daysPassed;
            if (dayMult > 6f)
            {
                dayMult = 6f;
            }
            float chance = BASE_EGG_CHANCE * dayMult;
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                baseString = ImproveHappinessEvent();
                daysPassedAtLastEgg = MetaProgressScript.totalDaysPassed;
            }
        }

        daysPassedAtLastEgg = MetaProgressScript.totalDaysPassed;

        return baseString;
    }

    public string ImproveHappinessEvent()
    {
        // Do stuff here
        // Lay an egg or something?
        if (monsterObject == null)
        {
            Debug.Log("Monster object for " + refName + " " + refName + " is null?");
        }
        {
            switch (monsterObject.myTemplate.monFamily)
            {
                case "bandits":
                case "robots":
                case "beasts":
                case "snakes":
                case "jelly":
                    return FetchRandomItem("allitems");
                case "spirits":
                case "hybrids":
                    return FetchRandomItem("potions", true);
            }
        }

        return "";
    }

    public string FetchRandomItem(string category, bool isPotion = false)
    {
        float magicChance = 0.2f;

        float chanceMod = (float)happiness * 0.06f;
        magicChance += chanceMod;

        float baseItemCV = monsterObject.challengeValue;

        if (happiness > 4)
        {
            baseItemCV += 0.1f;
        }
        if (happiness > 8)
        {
            baseItemCV += 0.1f;
        }

        Item randomItem = LootGeneratorScript.GenerateLootFromTable(baseItemCV, magicChance, category);
        StringManager.SetTag(0, monsterObject.displayName);
        StringManager.SetTag(1, randomItem.displayName);
        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(randomItem, true);
        if (isPotion)
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_corral_madepotion") + " " + StringManager.GetString("log_corral_pickup"), GameMasterScript.heroPCActor);
            return StringManager.GetString("log_corral_madepotion");
        }
        else
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_corral_fetcheditem") + " " + StringManager.GetString("log_corral_pickup"), GameMasterScript.heroPCActor);
            return StringManager.GetString("log_corral_fetcheditem");
        }
        if (Debug.isDebugBuild) Debug.Log("Monster created " + randomItem.actorRefName + " " + randomItem.actorUniqueID + ", does player have it now? " + GameMasterScript.heroPCActor.myInventory.HasItem(randomItem));
    }

    public string LayEgg()
    {
        Item nEgg = LootGeneratorScript.CreateItemFromTemplateRef(monsterObject.myTemplate.eggRef, 1.0f, 0f, false);
        StringManager.SetTag(0, monsterObject.displayName);
        StringManager.SetTag(1, nEgg.displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("log_corral_laidegg") + " " + StringManager.GetString("log_corral_pickup"), GameMasterScript.heroPCActor);
        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(nEgg, true);

        if (happiness >= 3)
        {
            int bonusTurns = happiness * 20;
            nEgg.SetActorData("bonusturns", bonusTurns);
        }


        return StringManager.GetString("log_corral_laidegg");
    }

    public int GetGroomingCost(GroomingTypes gt)
    {
        int baseCost = 0;

        baseCost = 100 * monsterObject.myStats.GetLevel();

        baseCost += (int)(baseCost * timesGroomed * 0.1f);
        
        switch (gt)
        {
            case GroomingTypes.BRUSH_AND_TRIM: // This is the medium beauty up, happiness up                
                if (beauty > 50)
                {
                    baseCost += (int)(baseCost * (beauty - 50f) / 100f);
                }
                break;
            case GroomingTypes.BATHE_AND_STYLE: // High beauty up, no happiness change                
                if (beauty > 50)
                {
                    baseCost += (int)(baseCost * (beauty - 50f) / 100f);
                }
                break;
            case GroomingTypes.MUD_BATH: // beauty down, happiness up
                if (beauty < 50)
                {
                    baseCost += (int)(baseCost * (50f - beauty) / 100f);
                }
                break;
            case GroomingTypes.HUMILIATING_OUTFIT: // beauty down, happiness down
                if (beauty < 50)
                {
                    baseCost += (int)(baseCost * (50f - beauty) / 100f);
                }
                break;
        }

        return baseCost;
    }

    public void DoMonsterGrooming(GroomingTypes gt)
    {
        StringManager.SetTag(0, monsterObject.displayName);
        switch (gt)
        {
            case GroomingTypes.BRUSH_AND_TRIM:
                ChangeBeauty(UnityEngine.Random.Range(4, 7));
                ChangeHappiness(1);
                GameLogScript.GameLogWrite(StringManager.GetString("log_corral_groom1"), GameMasterScript.heroPCActor);
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "CharmEffectSystem", null);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup1"), monsterObject.GetObject(), Color.yellow, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup2"), monsterObject.GetObject(), Color.yellow, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup3"), monsterObject.GetObject(), Color.yellow, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup1"), monsterObject.GetObject(), Color.yellow, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup2"), monsterObject.GetObject(), Color.yellow, 0f);
                UIManagerScript.PlayCursorSound("ShamanHeal");
                break;
            case GroomingTypes.BATHE_AND_STYLE:
                ChangeBeauty(UnityEngine.Random.Range(8, 11));
                GameLogScript.GameLogWrite(StringManager.GetString("log_corral_groom2"), GameMasterScript.heroPCActor);
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "SplashEffectSystem", null);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup2"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom1_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup5"), monsterObject.GetObject(), Color.blue, 0f);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "OneshotSparkles", null, 0.75f, true);
                monsterObject.myAnimatable.SetAnim("TakeDamage");
                UIManagerScript.PlayCursorSound("Splash");
                break;
            case GroomingTypes.MUD_BATH:
                ChangeBeauty(-1 * UnityEngine.Random.Range(4, 7));
                ChangeHappiness(1);
                GameLogScript.GameLogWrite(StringManager.GetString("log_corral_mudbath"), GameMasterScript.heroPCActor);
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "MudExplosion", null);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "MudExplosion", null, 0.35f, true);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "MudExplosion", null, 0.75f, true);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup2"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup2"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom2_popup1"), monsterObject.GetObject(), Color.blue, 0f);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "FervirDebuff", null, 0.9f, true);
                //monsterObject.myAnimatable.SetAnim("TakeDamage");
                UIManagerScript.PlayCursorSound("Splash");
                break;
            case GroomingTypes.HUMILIATING_OUTFIT:
                ChangeBeauty(-1 * UnityEngine.Random.Range(11, 15));
                GameLogScript.GameLogWrite(StringManager.GetString("log_corral_outfit"), GameMasterScript.heroPCActor);
                CombatManagerScript.GenerateSpecificEffectAnimation(monsterObject.GetPos(), "ConstrictEffect", null);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "ConstrictEffect", null, 0.4f, true);
                BattleTextManager.NewText(StringManager.GetString("misc_groom4_popup"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom4_popup"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom4_popup"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom4_popup"), monsterObject.GetObject(), Color.blue, 0f);
                BattleTextManager.NewText(StringManager.GetString("misc_groom4_popup"), monsterObject.GetObject(), Color.blue, 0f);
                CombatManagerScript.WaitThenGenerateSpecificEffect(monsterObject.GetPos(), "FervirDebuff", null, 0.75f, true);
                monsterObject.myAnimatable.SetAnim("TakeDamage");
                UIManagerScript.PlayCursorSound("Failure");
                break;
        }

        timesGroomed++;
    }

    public void SetRelationshipAmount(TamedCorralMonster tcm, int amount)
    {
        if (!attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            StartRelationship(tcm, true);
        }
        InternalSetRelationshipAmount(tcm.sharedBankID, amount);
    }

    public void AdjustRelationship(TamedCorralMonster tcm, int amount, bool reciprocate)
    {
        if (!attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            StartRelationship(tcm, true);
        }

        InternalSetRelationshipAmount(tcm.sharedBankID, attractionToMonsters[tcm.sharedBankID] + 5);

        if (attractionToMonsters[tcm.sharedBankID] >= MAX_ATTRACTION)
        {
            InternalSetRelationshipAmount(tcm.sharedBankID, MAX_ATTRACTION);
        }

        if (reciprocate)
        {
            tcm.AdjustRelationship(this, amount, false);
        }
    }

    public void StartRelationship(TamedCorralMonster tcm, bool reciprocate)
    {
        if (!attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            AddMonsterToAttractionDict(tcm.sharedBankID, UnityEngine.Random.Range(-5, 2));
            if (reciprocate)
            {
                tcm.StartRelationship(this, false);
            }
        }
        else
        {
            //Debug.Log(monsterID + " " + monsterObject.displayName + " already has a relationship with " + tcm.monsterID);
        }
    }

    public void DevelopRelationship(TamedCorralMonster tcm)
    {
        if (tcm == this) return;
        int currentRelationshipLevel = attractionToMonsters[tcm.sharedBankID];

        float baseChanceOfAffection = 0f;

        switch (happiness)
        {
            case 0:
                baseChanceOfAffection = 0.25f;
                break;
            case 1:
                baseChanceOfAffection = 0.3f;
                break;
            case 2:
                baseChanceOfAffection = 0.35f;
                break;
            case 3:
                baseChanceOfAffection = 0.4f;
                break;
            case 4:
                baseChanceOfAffection = 0.45f;
                break;
            case 5:
                baseChanceOfAffection = 0.55f;
                break;
            case 6:
                baseChanceOfAffection = 0.6f;
                break;
        }

        int beautyDiff = tcm.beauty - beauty;

        // Examples: Our beauty 20, theirs is 80. Difference is 60. That is good for us.
        // Or the reverse, would be -60.

        if (beautyDiff < -30) // Less likely to be attracted to an ugly monster, comparatively speaking
        {
            baseChanceOfAffection *= 0.8f;
        }

        // Beautiful monsters are just always more attractive.
        if (tcm.beauty > 65)
        {
            baseChanceOfAffection += 0.08f;
        }
        if (tcm.beauty > 80)
        {
            baseChanceOfAffection += 0.1f;
        }
        if (tcm.beauty > 90)
        {
            baseChanceOfAffection += 0.15f;
        }

        // Happier monsters are more lovable, harder to be mad at.

        if (tcm.happiness == 4)
        {
            baseChanceOfAffection *= 1.15f;
        }
        else if (tcm.happiness == 5)
        {
            baseChanceOfAffection *= 1.3f;
        }
        else if (tcm.happiness == 6)
        {
            baseChanceOfAffection *= 1.45f;
        }

        if (currentRelationshipLevel > 2)
        {
            baseChanceOfAffection *= 1.2f;
        }
        if (currentRelationshipLevel > 5)
        {
            baseChanceOfAffection *= 1.2f;
        }

        // Does the recipient LIKE or DISLIKE me?
        int recipientAffectionLevel = tcm.TryGetRelationshipAmount(this);
        if (recipientAffectionLevel > 3)
        {
            baseChanceOfAffection *= 1.1f;
        }
        if (recipientAffectionLevel > 6)
        {
            baseChanceOfAffection *= 1.1f;
        }

        // Same family gets along
        if (family == tcm.family)
        {
            baseChanceOfAffection += 0.15f;
        }

        if (refName == tcm.refName)
        {
            baseChanceOfAffection += 0.05f;
        }

        // Cap off the affection and disaffection chances
        if (baseChanceOfAffection < 0.1f)
        {
            baseChanceOfAffection = 0.1f;
        }
        else if (baseChanceOfAffection >= 1.0f)
        {
            baseChanceOfAffection = 1.0f;
        }

        //Debug.Log(monsterObject.displayName + " at level " + currentRelationshipLevel + " with " + tcm.monsterObject.displayName + " Affection chance: " + baseChanceOfAffection + " Disaffection: " + baseChanceOfDisaffection);

        if (UnityEngine.Random.Range(0, 1f) <= baseChanceOfAffection)
        {
            ChangeAttractionToMonster(tcm, 1);
        }

        StringManager.SetTag(0, tcm.monsterObject.displayName);
        StringManager.SetTag(1, GetRelationshipString(tcm));

    }

    public void ChangeAttractionToMonster(TamedCorralMonster tcm, int amount)
    {
        if (attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            InternalSetRelationshipAmount(tcm.sharedBankID, attractionToMonsters[tcm.sharedBankID] + amount);
            if (attractionToMonsters[tcm.sharedBankID] < MIN_ATTRACTION)
            {
                InternalSetRelationshipAmount(tcm.sharedBankID, MIN_ATTRACTION);
            }
            else if (attractionToMonsters[tcm.sharedBankID] > MAX_ATTRACTION)
            {
                InternalSetRelationshipAmount(tcm.sharedBankID, MAX_ATTRACTION);
            }
        }
        else
        {
            Debug.Log("Monster " + monsterObject.displayName + " " + monsterID + " trying to change attraction for " + tcm.sharedBankID + " " + tcm.monsterObject.displayName + " but that doesn't exist in dict.");
        }
    }

    public string GetRelationshipString(TamedCorralMonster tcm)
    {
        if (attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            int relationshipValue = attractionToMonsters[tcm.sharedBankID];
            if (relationshipValue >= MIN_ATTRACTION && relationshipValue <= -4)
            {
                return StringManager.GetString("corral_relationship_negative3");
            }
            if (relationshipValue >= -3 && relationshipValue <= -2)
            {
                return StringManager.GetString("corral_relationship_negative2");
            }
            if (relationshipValue >= -1 && relationshipValue < 0)
            {
                return StringManager.GetString("corral_relationship_negative1");
            }
            if (relationshipValue >= 0 && relationshipValue <= 1)
            {
                return StringManager.GetString("corral_relationship_neutral");
            }
            if (relationshipValue >= 2 && relationshipValue <= 4)
            {
                return StringManager.GetString("corral_relationship_positive1");
            }
            if (relationshipValue >= 5 && relationshipValue <= 7)
            {
                return StringManager.GetString("corral_relationship_positive2");
            }
            if (relationshipValue >= 8 && relationshipValue <= 9)
            {
                return StringManager.GetString("corral_relationship_positive3");
            }
            if (relationshipValue >= MAX_ATTRACTION)
            {
                return StringManager.GetString("corral_relationship_positive4");
            }
        }
        else
        {
            return StringManager.GetString("corral_relationship_neutral");
        }

        return StringManager.GetString("corral_relationship_neutral");
    }

    /* public string GetAllRelationshipStrings()
    {
        string builder = "";
        foreach(TamedCorralMonster tcm in MetaProgressScript.tamedMonsters)
        {
            if (tcm == this) continue;

            StringManager.SetTag(0, tcm.displayName);
            StringManager.SetTag(1, GetRelationshipString(tcm));

            builder += UIManagerScript.cyanHexColor + displayName + "</color> " + StringManager.GetString("corral_relationship_descriptor") + " (" + TryGetRelationshipAmount(tcm) + ")\n";
        }
        return builder;
    } */

    public int TryGetRelationshipAmount(TamedCorralMonster tcm)
    {
        if (attractionToMonsters.ContainsKey(tcm.sharedBankID))
        {
            return attractionToMonsters[tcm.sharedBankID];
        }
        else
        {
            Debug.Log(sharedBankID + " " + monsterObject.actorRefName + " doesn't have attraction to " + tcm.sharedBankID);
            return 0;
        }
    }

    public string GetBattlePowerStats()
    {
        string masterBattleText = "";
        string parentText = "";

        Monster monToView = monsterObject;

        if (!String.IsNullOrEmpty(parent1Name))
        {
            parentText = UIManagerScript.cyanHexColor + StringManager.GetString("ui_corral_parents") + "</color> " + parent1Name + ", " + parent2Name + "\n";
            masterBattleText += parentText;
        }

        List<string> allPowers = new List<string>();

        foreach (MonsterPowerData mpd in monToView.monsterPowers)
        {
            if (!allPowers.Contains(mpd.abilityRef.abilityName))
            {
                allPowers.Add(mpd.abilityRef.abilityName);
            }
        }
        // Why were we adding template powers here? Seems bad? What if you tell your monster to forget something?
        /* foreach (MonsterPowerData mpd in monToView.myTemplate.monsterPowers)
        {
            if (!allPowers.Contains(mpd.abilityRef.abilityName))
            {
                allPowers.Add(mpd.abilityRef.abilityName);
            }
        } */

        if (allPowers.Count > 0)
        {
            string powersText = UIManagerScript.orangeHexColor + StringManager.GetString("ui_corral_monpowers") + "</color> ";

            for (int i = 0; i < allPowers.Count; i++)
            {
                if (i < allPowers.Count - 1)
                {
                    powersText += allPowers[i] + ", ";
                }
                else
                {
                    powersText += allPowers[i] + "\n";
                }
            }
            masterBattleText += powersText;
        }

        string beautyEffect = MonsterBeautyStuff.GetEffectToUseFromBeauty(this);
        if (!string.IsNullOrEmpty(beautyEffect))
        {
            masterBattleText += StringManager.GetString("monster_beauty_power") + " " + MonsterBeautyStuff.GetBeautyEffectDescription(beautyEffect, monsterObject.displayName) + "\n";
        }
        
        masterBattleText += "<color=yellow>" + StringManager.GetString("ui_equipment_weaponpower") + ":</color> " + (int)((monToView.myEquipment.GetWeapon().power * 10f)) + "\n";
        masterBattleText += UIManagerScript.greenHexColor + StringManager.GetString("charsheet_tab1") + ":</color> " + monToView.myStats.GetCoreStatDisplay() + "\n";

        if (knownLoveFoods.Count > 0)
        {
            masterBattleText += StringManager.GetString("ui_corral_foodlikes") + ": " + GetStringListOfLovedFoods() + "\n";
        }
        if (knownHateFoods.Count > 0)
        {
            masterBattleText += StringManager.GetString("ui_corral_fooddislikes") + ": " + GetStringListOfHatedFoods() + "\n";
        }

        return masterBattleText;
    }

    // Add the Tuckered Out status to our PETS instead of the owner
    public void SetTuckeredOut()
    {
        int iCooldownDuration = 12 - (happiness / 2);
        HeroPC hero = GameMasterScript.heroPCActor;
        if (monsterObject != null)
        {
            monsterObject.myStats.AddStatusByRef("status_pet_call", hero, iCooldownDuration);
        }
    }

    public void RemoveMonsterFromAttractionDict(int id)
    {
        /* Debug.Log(monsterID + " removes attraction to " + id);
        if (attractionToMonsters.ContainsKey(id))
        {
            Debug.Log("Attraction was at: " + attractionToMonsters[id]);
        } */
        attractionToMonsters.Remove(id);
    }

    public void AddMonsterToAttractionDict(int id, int amount)
    {
        //if (Debug.isDebugBuild) Debug.Log(monsterID + " adding mID " + id + " to dict with attraction " + amount);
        attractionToMonsters.Add(id, amount);
    }

    /// <summary>
    /// Assumes that id is already in our dictionary of attractionToMonsters
    /// </summary>
    /// <param name="id"></param>
    /// <param name="amount"></param>
    void InternalSetRelationshipAmount(int id, int amount)
    {
        attractionToMonsters[id] = amount;
        //if (Debug.isDebugBuild) Debug.Log(monsterID + " SETS relationship value for " + id + " to " + amount);
    }

    /// <summary>
    /// Go through all our relationships and make sure anyone who knows us as our current monsterID instead references us as newID
    /// </summary>
    /// <param name="newID"></param>
    public void UpdateRelationshipsWithNewID(int newID)
    {
        //Debug.Log(monsterID + " is becoming " + newID);

        // Search through all other monsters
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        //foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[i];
            // Ignoring THIS monster
            if (tcm == this) continue;

            int oldValue;
            // If another TCM has an attraction to us, we need to 'redirect' the ID that it's attracted to.
            if (tcm.attractionToMonsters.TryGetValue(sharedBankID, out oldValue))
            {
                tcm.RemoveMonsterFromAttractionDict(sharedBankID);
                tcm.AddMonsterToAttractionDict(newID, oldValue);
            }
        }
    }

    public void AssignNewSharedBankID()
    {
        sharedBankID = SharedCorral.GetUniqueSharedPetID();
        if (Debug.isDebugBuild) Debug.Log(refName + " corral pet has a new SHARED BANK id: " + sharedBankID);
    }
}
