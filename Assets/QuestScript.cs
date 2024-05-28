using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Text;

public enum QuestType { KILLCHAMPION, FINDAREA, FINDITEM, APPEASEMONSTER, KILLMONSTERELEMENTAL, BOSSGANG,
    DREAMWEAPON_BOSS, TAMEMONSTER, COUNT }
public enum QuestRequirementTypes { STEPSINDUNGEON, DAMAGETAKEN, NOFLASK, SAMEGEAR, COUNT }

public enum QuestRewardTypes { ITEM, CONSUMABLE_PACK, GOLD, XP, JP, COUNT }

public class QuestRequirement
{
    public QuestRequirementTypes qrType;
    public int maxStepsInDungeon;
    public int stepsTaken;
    public int maxDamageTaken;
    public int damageTaken;
}

public partial class QuestScript
{

    public QuestType qType;
    public Monster targetMonster;
    public int targetMonsterID;
    public List<QuestRequirement> qRequirements;
    public Actor targetActor;
    public int targetActorID;
    public Map targetMap;
    public int targetMapID;
    public Item targetItem;
    public bool itemIsGeneric;
    public int targetItemID;
    public float challengeValue;
    public string refName;
    public string displayName;
    public string questText;
    public string rewardsText;
    public string targetRef;
    public int numTargetsRemaining;
    public DamageTypes damType;
    public Item itemReward;
    public bool consumableRewardPack;
    public int itemRewardID;
    public int goldReward;
    public int xpReward;
    public int jpReward;
    public int lowestPossibleFloor;
    public int highestPossibleFloor;
    public bool[] rewardTypesUsed;
    public bool complete;

    public int dayReceived;

    public static float actualMonsterCV;

    public const float CHANCE_LUCID_ORB_REPLACE_ITEM = 0.4f;
    public const float CHANCE_LEGENDARY_ITEM = 0.08f;
    public const float QUEST_REQUIREMENTS_CHANCE = 0.33f;
    public const int MONSTER_RUMOR_SOFTCAP = 22;
    public const int MONSTER_RUMOR_HARDCAP = 35;

    public static Item desiredItemByMonster;

    public static QuestType lastGeneratedQuestType;
    public static int lastTargetItemID;
    public static int lastTargetMonsterID;


    static bool initialized;
    
    static Stack<List<Map>> stackedMapLists;
    static Stack<List<Item>> stackedItemLists;
    static Stack<List<Monster>> stackedMonsterLists;

    static void InitializeStaticPools()
    {
        if (initialized) return;

        possibleQuestTypes = new List<QuestType>();

        possibleMonsters = new List<string>();
        monsterRefsToMaps = new Dictionary<string, Map>();

        possibleDamageTypes = new List<DamageTypes>();

        stackedMapLists = new Stack<List<Map>>();
        for (int i = 0; i < 10; i++)
        {
            List<Map> m = new List<Map>();
            stackedMapLists.Push(m);
        }

        stackedItemLists = new Stack<List<Item>>();
        for (int i = 0; i < 10; i++)
        {
            List<Item> m = new List<Item>();
            stackedItemLists.Push(m);
        }

        stackedMonsterLists = new Stack<List<Monster>>();
        for (int i = 0; i < 10; i++)
        {
            List<Monster> m = new List<Monster>();
            stackedMonsterLists.Push(m);
        }

        initialized = true;
    }

    static List<Map> GetEmptyMapList()
    {
        if (!initialized) InitializeStaticPools();
        if (stackedMapLists.Count == 0)
        {
            stackedMapLists.Push(new List<Map>());
        }

        List<Map> m = stackedMapLists.Pop();
        m.Clear();

        return m;
    }

    static List<Item> GetEmptyItemList()
    {
        if (!initialized) InitializeStaticPools();
        if (stackedItemLists.Count == 0)
        {
            stackedItemLists.Push(new List<Item>());
        }

        List<Item> m = stackedItemLists.Pop();
        m.Clear();

        return m;
    }

    static List<Monster> GetEmptyMonsterList()
    {
        if (!initialized) InitializeStaticPools();
        if (stackedMonsterLists.Count == 0)
        {
            stackedMonsterLists.Push(new List<Monster>());
        }

        List<Monster> m = stackedMonsterLists.Pop();
        m.Clear();

        return m;
    }

    static void ReturnMapListToStack(List<Map> m)
    {
        stackedMapLists.Push(m);
    }
    static void ReturnItemListToStack(List<Item> m)
    {
        stackedItemLists.Push(m);
    }
    static void ReturnMonsterListToStack(List<Monster> m)
    {
        stackedMonsterLists.Push(m);
    }

    public QuestScript()
    {
        qRequirements = new List<QuestRequirement>();
        rewardTypesUsed = new bool[(int)QuestRewardTypes.COUNT];

        if (!initialized)
        {
            InitializeStaticPools();
            lastGeneratedQuestType = QuestType.COUNT;
            initialized = true;
        }
    }

    public void VerifyLinkedMapsAreEnabled()
    {
        if (targetMap == null) return; // Should never happen... can't hurt to check though
        if (targetMap.IsMainPath()) return;
        if (targetMap.mapIsHidden) return;
        targetMap.SetMapVisibility(true);
        if (targetMap.dungeonLevelData.deepSideAreaFloor)
        {
            Map processMap = targetMap;
            bool finished = false;
            int attempts = 0;
            while (!finished)
            {
                attempts++;
                if (attempts > 500)
                {
                    Debug.Log("Caught in map verification loop for rumor " + targetMap.floor + " " + GetAllQuestText(8));
                    return;
                }
                foreach (Stairs st in processMap.mapStairs)
                {
                    if (st.stairsUp)
                    {
                        if (st.NewLocation.IsMainPath())
                        {
                            return;
                        }
                        else
                        {
                            processMap = st.NewLocation;
                            processMap.SetMapVisibility(true);
                            break;
                        }
                    }
                }
            }

        }
    }

    public bool ValidateData()
    {
        switch(qType)
        {
            case QuestType.APPEASEMONSTER:
                if (targetMonster == null && targetMonsterID == 0)
                {
                    return false;
                }
                break;
            case QuestType.DREAMWEAPON_BOSS:
                if (targetItem == null && targetItemID == 0)
                {
                    return false;
                }
                break;
            case QuestType.BOSSGANG:
                if (targetMonster == null && targetMonsterID == 0)
                {
                    return false;
                }
                break;
            case QuestType.KILLCHAMPION:
                if (targetMonster == null && targetMonsterID == 0)
                {
                    return false;
                }
                break;
            case QuestType.KILLMONSTERELEMENTAL:
                if (string.IsNullOrEmpty(targetRef))
                {
                    return false;
                }
                break;
            case QuestType.FINDITEM:
                if (targetItem == null && targetItemID == 0)
                {
                    return false;
                }
                break;
        }

        return true;
    }

    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();
        string txt;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            //Debug.Log(reader.Name + " " + reader.NodeType);
            switch (reader.Name.ToLowerInvariant())
            {
                case "questtype":
                    qType = (QuestType)reader.ReadElementContentAsInt();
                    break;
                case "targetmonsterid":
                    targetMonsterID = reader.ReadElementContentAsInt();
                    break;
                case "targetactorid":
                    targetActorID = reader.ReadElementContentAsInt();
                    break;
                case "targetitemid":
                    targetItemID = reader.ReadElementContentAsInt();
                    break;
                case "targetmapid":
                    targetMapID = reader.ReadElementContentAsInt();
                    break;
                case "dayreceived":
                    dayReceived = reader.ReadElementContentAsInt();
                    break;
                case "itemrewardid":
                    itemRewardID = reader.ReadElementContentAsInt();
                    break;
                case "challengevalue":
                    txt = reader.ReadElementContentAsString();
                    challengeValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "gold":
                    goldReward = reader.ReadElementContentAsInt();
                    break;
                case "xp":
                    xpReward = reader.ReadElementContentAsInt();
                    break;
                case "jp":
                    jpReward = reader.ReadElementContentAsInt();
                    break;
                case "highestfloor":
                    highestPossibleFloor = reader.ReadElementContentAsInt();
                    break;
                case "lowestfloor":
                    lowestPossibleFloor = reader.ReadElementContentAsInt();
                    break;
                case "targetref":
                    targetRef = reader.ReadElementContentAsString();
                    break;
                case "damtype":
                    damType = (DamageTypes)reader.ReadElementContentAsInt();
                    break;
                case "cpack":
                    consumableRewardPack = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "numtargets":
                    numTargetsRemaining = reader.ReadElementContentAsInt();
                    break;
                case "iref":
                case "itemref":
                    string rName = reader.ReadElementContentAsString();
                    targetItem = GameMasterScript.GetItemFromRef(rName);
                    break;
                case "itemisgeneric":
                    itemIsGeneric = reader.ReadElementContentAsBoolean();
                    break;
                case "item":
                    Item itm = new Item();
                    itm = itm.ReadFromSave(reader);
                    if (itm != null)
                    {
                        itemReward = itm;
                        itm.collection = GameMasterScript.heroPCActor.myInventory;
                    }
                    break;
                case "rtypes":
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        Boolean.TryParse(parsed[i], out rewardTypesUsed[i]);
                    }
                    break;
                case "requirement":
                    reader.ReadStartElement();
                    QuestRequirement qr = new QuestRequirement();
                    qRequirements.Add(qr);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "requirementtype":
                                qr.qrType = (QuestRequirementTypes)reader.ReadElementContentAsInt();
                                break;
                            case "maxsteps":
                                qr.maxStepsInDungeon = reader.ReadElementContentAsInt();
                                break;
                            case "stepstaken":
                                qr.stepsTaken = reader.ReadElementContentAsInt();
                                break;
                            case "maxdamage":
                                qr.maxDamageTaken = reader.ReadElementContentAsInt();
                                break;
                            case "damagetaken":
                                qr.damageTaken = reader.ReadElementContentAsInt();
                                break;
                            default:
                                reader.Read();
                                break;
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

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("quest");
        writer.WriteElementString("questtype", ((int)qType).ToString());
        if (targetMonster != null)
        {
            writer.WriteElementString("targetmonsterid", targetMonsterID.ToString());
        }
        if (targetActor != null)
        {
            writer.WriteElementString("targetactorid", targetActorID.ToString());
        }
        if (targetMap != null)
        {
            writer.WriteElementString("targetmapid", targetMapID.ToString());
        }
        if (itemReward != null)
        {
            //writer.WriteElementString("itemrewardid", itemRewardID.ToString());
            itemReward.WriteToSave(writer);
        }

        writer.WriteElementString("dayreceived", dayReceived.ToString());

        if (targetItem != null)
        {
            if (itemIsGeneric)
            {
                writer.WriteElementString("itemisgeneric", itemIsGeneric.ToString().ToLowerInvariant());
                writer.WriteElementString("itemref", targetItem.actorRefName.ToString().ToLowerInvariant());
            }
            else
            {
                writer.WriteElementString("targetitemid", targetItem.actorUniqueID.ToString());
            }

        }
        if (qRequirements.Count > 0)
        {
            foreach (QuestRequirement qr in qRequirements)
            {
                writer.WriteStartElement("requirement");
                writer.WriteElementString("requirementtype", ((int)qr.qrType).ToString());
                writer.WriteElementString("maxsteps", qr.maxStepsInDungeon.ToString());
                writer.WriteElementString("stepstaken", qr.stepsTaken.ToString());
                writer.WriteElementString("maxdamage", qr.maxDamageTaken.ToString());
                writer.WriteElementString("damagetaken", qr.damageTaken.ToString());
                writer.WriteEndElement();
            }
        }
        if ((targetRef != null) && (targetRef != ""))
        {
            writer.WriteElementString("targetref", targetRef.ToString().ToLowerInvariant());
        }
        if (numTargetsRemaining > 0)
        {
            writer.WriteElementString("numtargets", numTargetsRemaining.ToString());
        }
        writer.WriteElementString("damtype", ((int)damType).ToString());
        writer.WriteElementString("challengevalue", challengeValue.ToString());
        writer.WriteElementString("gold", goldReward.ToString());
        writer.WriteElementString("xp", xpReward.ToString());
        writer.WriteElementString("jp", jpReward.ToString());
        if (consumableRewardPack || rewardTypesUsed[(int)QuestRewardTypes.CONSUMABLE_PACK])
        {
            writer.WriteElementString("cpack", "1");
        }

        string rewardTypesString = "";
        for (int i = 0; i < rewardTypesUsed.Length; i++)
        {
            rewardTypesString += rewardTypesUsed[i].ToString().ToLowerInvariant();
            if (i < rewardTypesUsed.Length-1)
            {
                rewardTypesString += "|";
            }
        }
        writer.WriteElementString("rtypes", rewardTypesString);
        
        writer.WriteElementString("lowestfloor", lowestPossibleFloor.ToString());
        writer.WriteElementString("highestfloor", highestPossibleFloor.ToString());
        writer.WriteEndElement();
   } 

    // Are any items involved with the quest LEGENDARY, and marked as dropped in Hero actor data?
    public bool ItemsInvolvedStillValid()
    {
        if (qType != QuestType.DREAMWEAPON_BOSS && targetItem != null && targetItem.legendary && GameMasterScript.heroPCActor.FoundLegItem(targetItem.actorRefName))
        {
            return false;
        }
        // If a quest had a leg item reward, then swap the item somewhere else. The quest is still valid.
        /* if (itemReward != null && itemReward.legendary && GameMasterScript.heroPCActor.FoundLegItem(itemReward.actorRefName))
        {
            return false;
        } */
        return true;
    }

    // Because this sets tags, you MUST call this before calling other tag0/1 stuff
    public string GetTargetFloorNameAndNearbyFloor(int floor)
    {
        string builder = "";
        Map mToProcess = MapMasterScript.theDungeon.FindFloor(floor);
        if (mToProcess.IsMainPath())
        {
            return "<color=yellow>" + mToProcess.GetName() + "</color>";
        }
        StringManager.SetTag(0, mToProcess.GetName());
        //Debug.Log("Target floor is " + floor + " " + mToProcess.GetName());
        string nearby = mToProcess.GetNearbyPathFloor(allowHidden: true);
        if (nearby != "")
        {
            StringManager.SetTag(1, nearby);
            builder = StringManager.GetString("quest_near_location");
        }
        else
        {
            builder = "<color=yellow>" + mToProcess.GetName() + "</color>";
        }
        return builder;
    }

    static StringBuilder reusableSB;

    public string GenerateAbbreviatedQuestText()
    {
        if (reusableSB == null) reusableSB = new StringBuilder();
        reusableSB.Length = 0;

        switch (qType)
        {
            case QuestType.APPEASEMONSTER:

                if (targetItem == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Target item for appease monster quest is null.");
                    return "";
                }
                StringManager.SetTag(0, GetTargetMonDisplayName());
                StringManager.SetTag(1, targetItem.displayName);
                return StringManager.GetString("quest_desc_appeasemon_abbr");
            case QuestType.BOSSGANG:
                if (targetMonster == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Target monster for boss gang quest is null.");
                    return "";
                }

                StringManager.SetTag(0, targetMonster.displayName);
                return StringManager.GetString("quest_desc_bossgang_abbr");
            case QuestType.DREAMWEAPON_BOSS:
                if (targetItem == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Target item for elemental king dream quest is null.");
                    return "";
                }
                StringManager.SetTag(0, targetItem.GetBaseDisplayName());
                StringManager.SetTag(1, StringManager.GetString("misc_dmg_" + damType.ToString().ToLowerInvariant()));
                return StringManager.GetString("quest_desc_dreamweapon_abbr");
            case QuestType.FINDAREA:
                if (targetMap == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Target map for find area quest is null.");
                    return "";
                }

                StringManager.SetTag(0, targetMap.GetName());

                Map nearestConnection = targetMap.GetNearestConnectionThatIsNotSideArea();

                if (nearestConnection == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Could not find connection for find area rumor, map is " + targetMap.GetName());
                    return "";
                }

                StringManager.SetTag(1, nearestConnection.GetName());
                return StringManager.GetString("quest_desc_findarea_abbr");
            case QuestType.FINDITEM:
                if (targetItem == null || targetActor == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Item or actor for FindItem quest was null.");
                    return "";
                }


                StringManager.SetTag(0, targetItem.displayName);
                StringManager.SetTag(1, targetActor.displayName);
                return StringManager.GetString("quest_desc_finditem_abbr");
            case QuestType.KILLCHAMPION:
                if (targetMonster == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Champion was null for killchampion quest");
                    return "";
                }

                StringManager.SetTag(0, targetMonster.displayName);
                return StringManager.GetString("quest_desc_killchampion_abbr");
            case QuestType.KILLMONSTERELEMENTAL:
                StringManager.SetTag(0, MonsterManagerScript.GetMonsterDisplayNameByRef(targetRef));
                StringManager.SetTag(1, StringManager.GetString("misc_dmg_" + damType.ToString().ToLowerInvariant()));
                return StringManager.GetString("quest_desc_elemental_slay_abbr");
            case QuestType.TAMEMONSTER:          
                if (targetMonster == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Null target monster for tame monster rumor.");
                    return "";
                }

                StringManager.SetTag(0, targetMonster.displayName);
                return StringManager.GetString("quest_desc_tamemonster_abbr");
        }

        return "";
    }

    public void GenerateQuestText()
    {
        switch (qType)
        {
            // #todo - localize title / elem descriptors
            case QuestType.KILLMONSTERELEMENTAL:

                displayName = "";
                
                switch (damType)
                {
                    case DamageTypes.FIRE:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.fireDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.fireDescriptors.Length)] + "</color>");
                        break;
                    case DamageTypes.WATER:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.waterDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.waterDescriptors.Length)] + "</color>");
                        break;
                    case DamageTypes.LIGHTNING:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.lightningDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.lightningDescriptors.Length)] + "</color>");
                        break;
                    case DamageTypes.POISON:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.poisonDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.poisonDescriptors.Length)] + "</color>");
                        break;
                    case DamageTypes.PHYSICAL:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.physicalDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.poisonDescriptors.Length)] + "</color>");
                        break;
                    case DamageTypes.SHADOW:
                        StringManager.SetTag(0, "<color=yellow>" + CombatManagerScript.shadowDescriptors[UnityEngine.Random.Range(0, CombatManagerScript.shadowDescriptors.Length)] + "</color>");
                        break;
                }

                StringManager.SetTag(1, MonsterManagerScript.GetMonsterDisplayNameByRef(targetRef));
                displayName = StringManager.GetString("quest_name_elemental_slay");

                // But Wait, in German it's different!
                if (StringManager.gameLanguage == EGameLanguage.de_germany)
                {
                    displayName = "Versuche doch mal " + StringManager.GetTag(1) + " (" + StringManager.GetTag(0) + ")!";
                }
                
                // Tag 2: Name of monsters
                StringManager.SetTag(1, MonsterManagerScript.GetMonsterDisplayNameByRef(targetRef));

                // Tag 3: The damage type
                string tag2contents = StringManager.GetString("misc_dmg_" + damType.ToString().ToLowerInvariant()); // this is the damage type                
                StringManager.SetTag(2, tag2contents);

                // Tag 7: Name of monster again (safe)
                StringManager.SetTag(6, MonsterManagerScript.GetMonsterDisplayNameByRef(targetRef));
                // Tag  6: Where to find it
                StringManager.SetTag(5, GetTargetFloorNameAndNearbyFloor(targetMap.floor));

                // Tag 1: Monsters to defeat
                StringManager.SetTag(0, numTargetsRemaining.ToString());

                questText = StringManager.GetString("quest_desc_elemental_slay");
                break;
            case QuestType.BOSSGANG:
                StringManager.SetTag(0, targetMonster.displayName);
                displayName = StringManager.GetString("quest_title_boss_gang");

                StringManager.SetTag(0, targetMonster.displayName);

                string floorTag = GetTargetFloorNameAndNearbyFloor(targetMonster.dungeonFloor);
                StringManager.SetTag(0, targetMonster.displayName);
                StringManager.SetTag(1, floorTag);

                questText = StringManager.GetString("quest_desc_boss_gang");                
                break;
            case QuestType.TAMEMONSTER:
                string wildMonString = StringManager.GetString("misc_monster_wilduntamed");
                string displayStringToUse = targetMonster.displayName;
                if (!displayStringToUse.Contains(wildMonString))
                {
                    displayStringToUse = wildMonString + " " + displayStringToUse;
                }

                if (Debug.isDebugBuild)
                {
                    Debug.Log("We have " + displayStringToUse + " as display string. DName of mon is " + targetMonster.displayName + " so...");
                }

                StringManager.SetTag(0, displayStringToUse);
                displayName = StringManager.GetString("quest_title_tamemonster");

                floorTag = GetTargetFloorNameAndNearbyFloor(targetMonster.dungeonFloor);
                StringManager.SetTag(1, floorTag);
                StringManager.SetTag(0, displayStringToUse);
                questText = StringManager.GetString("quest_desc_tamemonster");
                break;
            case QuestType.DREAMWEAPON_BOSS:
                StringManager.SetTag(0, targetItem.GetBaseDisplayName());
                StringManager.SetTag(1, StringManager.GetString("misc_dmg_" + damType.ToString().ToLowerInvariant()));
                questText = StringManager.GetString("quest_desc_dreamweapon");
                displayName = StringManager.GetString("quest_title_dreamweapon");
                break;
            case QuestType.KILLCHAMPION:
                StringManager.SetTag(0, targetMonster.displayName);
                displayName = StringManager.GetString("quest_title_slaymon");
                
                StringManager.SetTag(1, GetTargetFloorNameAndNearbyFloor(targetMonster.dungeonFloor));
                StringManager.SetTag(0, targetMonster.displayName);
                questText = StringManager.GetString("quest_desc_slaymon");
                break;
            case QuestType.FINDITEM:
                StringManager.SetTag(2, GetTargetFloorNameAndNearbyFloor(targetMap.floor));
                StringManager.SetTag(0, targetItem.displayName);
                StringManager.SetTag(1, targetActor.displayName);
                displayName = StringManager.GetString("quest_title_finditem");
                questText = StringManager.GetString("quest_desc_finditem");
                break;
            case QuestType.FINDAREA:
                StringManager.SetTag(0, targetMap.GetName());
                displayName = StringManager.GetString("quest_title_findarea");

                Map mapConnection = targetMap.GetNearestConnectionThatIsNotSideArea();

                StringManager.SetTag(1, mapConnection.GetName());
                questText = StringManager.GetString("quest_desc_findarea");
                break;
            case QuestType.APPEASEMONSTER:

                string monsterName = GetTargetMonDisplayName();
                
                //StringManager.SetTag(1, targetMonster.GetActorMap().GetName());
                if (targetMonster == null)
                {
                    StringManager.SetTag(1, "???");
                }
                else
                {
                    StringManager.SetTag(1, GetTargetFloorNameAndNearbyFloor(targetMonster.dungeonFloor));
                }
                
                StringManager.SetTag(2, targetItem.GetPluralName());
                StringManager.SetTag(0, monsterName);

                displayName = StringManager.GetString("quest_title_appeasemon");
                questText = StringManager.GetString("quest_desc_appeasemon");
                break;
        }
        if (qRequirements.Count > 0)
        {
            questText += "\n\n" + StringManager.GetString("quest_conditions") + "\n";
            foreach (QuestRequirement qr in qRequirements)
            {
                switch (qr.qrType)
                {
                    case QuestRequirementTypes.DAMAGETAKEN:
                        StringManager.SetTag(0,qr.maxDamageTaken.ToString());
                        StringManager.SetTag(1, qr.damageTaken.ToString());
                        questText += StringManager.GetString("quest_rec_damagetaken");
                        if (qr.damageTaken > 0)
                        {
                            questText += " " + StringManager.GetString("quest_taken");
                        }

                        break;
                    case QuestRequirementTypes.STEPSINDUNGEON:
                        StringManager.SetTag(0, qr.maxStepsInDungeon.ToString());
                        StringManager.SetTag(1, qr.stepsTaken.ToString());

                        questText += StringManager.GetString("quest_rec_steps");
                        if (qr.stepsTaken > 0)
                        {
                            questText += " " + StringManager.GetString("quest_taken");
                        }
                        break;
                    case QuestRequirementTypes.NOFLASK:
                        questText += StringManager.GetString("quest_rec_noflask"); ;
                        break;
                    case QuestRequirementTypes.SAMEGEAR:
                        questText += StringManager.GetString("quest_rec_samegear");
                        break;
                }
            }
        }
        rewardsText = "";
        if (itemReward != null)
        {
            rewardsText = itemReward.displayName + "\n";
        }
        if (xpReward > 0)
        {
            rewardsText += xpReward + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.XP) + "\n";
        }
        if (jpReward > 0)
        {
            rewardsText += jpReward + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP) + "\n";
        }
        if (goldReward > 0)
        {
            rewardsText += "<color=yellow>" + goldReward + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD) + " </color>\n";
        }
        if (consumableRewardPack)
        {
            rewardsText += StringManager.GetString("quest_rewards_consumablepack") + "\n";
        }
        if (qType == QuestType.DREAMWEAPON_BOSS)
        {
            // hardcoded reward for elemental kings
            rewardsText += StringManager.GetString("quest_rewards_elemkingessence") + "\n";
        }
    }

    public static int GetQuestCost()
    {
        return (50 + (15 * GameMasterScript.heroPCActor.myStats.GetLevel()));
    }

    public string GetAllQuestText(int maxSize)
    {
        //GenerateQuestText();
        string buildText = GetAllQuestTextExceptRewards(maxSize);
        buildText += GetRewardText(maxSize);
        //buildText += GetRewardText(maxSize);
        //buildText += "<size=" + maxSize + "><color=yellow>" + StringManager.GetString("misc_rewards") + ":</color></size>\n";
        //buildText += rewardsText;   
        return buildText;
    }

    public string GetAllQuestTextExceptRewards(int maxSize)
    {
        GenerateQuestText();
        string buildText = "";
        buildText = "<size=" + maxSize + ">" + displayName + "</size>\n\n";
        buildText += questText + "\n\n";
        return buildText;
    }

    public string GetRewardText(int maxSize)
    {
        // We assume "rewards" header is added elsewhere;
        string rewardsString = StringManager.GetString("misc_rewards") + ":";
        string buildText = "<size=" + maxSize + "><color=yellow>" + rewardsString + "</color></size>\n";
        buildText += rewardsText;
        return buildText;
    }

    public void GenerateQuestRewards(float cv, bool allowItems, int maxRewards)
    {
        //Debug.Log("Generate quest rewards for ... " + qType);
        if (rewardsText == null)
        {
            rewardsText = "";
        }
        if (rewardTypesUsed == null)
        {
            rewardTypesUsed = new bool[(int)QuestRewardTypes.COUNT]; // 4 types
        }

        //bool[] rewardTypesUsedLocal = new bool[(int)QuestRewardTypes.COUNT];

        float cubeCV = Mathf.Pow((cv + 0.1f), 4f);
        float triCV = Mathf.Pow((cv + 0.1f), 3f);

        List<QuestRewardTypes> types = new List<QuestRewardTypes>();
        for (int i = 0; i < maxRewards; i++)
        {
            bool shouldHaveItem = false;
            types.Clear();

            if (itemReward == null) // This was !=... Shouldn't it be the other way around?
            {
                types.Add(QuestRewardTypes.ITEM);
                types.Add(QuestRewardTypes.ITEM);
            }

            types.Add(QuestRewardTypes.CONSUMABLE_PACK);

            if (GameMasterScript.heroPCActor.myStats.GetLevel() < GameMasterScript.MAX_PLAYER_LEVEL_CAP)
            {
                types.Add(QuestRewardTypes.XP);
            }

            types.Add(QuestRewardTypes.JP);
            types.Add(QuestRewardTypes.GOLD);

            if (qType == QuestType.BOSSGANG)
            {
                types.Remove(QuestRewardTypes.XP);
                types.Remove(QuestRewardTypes.JP);
                if (!rewardTypesUsed[(int)QuestRewardTypes.ITEM])
                {
                    types.Remove(QuestRewardTypes.GOLD);
                    types.Remove(QuestRewardTypes.CONSUMABLE_PACK);
                }
            }

            if (types.Count == 0) continue;

            QuestRewardTypes typeToUse = types[UnityEngine.Random.Range(0, types.Count)];

            //Debug.Log("Try using " + typeToUse);

            switch (typeToUse)
            {
                case QuestRewardTypes.ITEM:
                    shouldHaveItem = true;
                    if (UnityEngine.Random.Range(0,1f) <= CHANCE_LEGENDARY_ITEM && cv >= 1.3f)
                    {
                        itemReward = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "legendary");
                        if (itemReward == null) // 312019 - If we can't find a legendary for any reason, use the master table instead
                        {
                            itemReward = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "allitems");
                        }

                        int tries = 0;
                        if (itemReward.legendary)
                        {
                            bool legMatchesOtherReward = true;
                            while (legMatchesOtherReward || GameMasterScript.heroPCActor.FoundLegItem(itemReward.actorRefName))
                            {
                                if (tries >= 5)
                                {
                                    Debug.Log("No other legendaries available.");
                                    GenerateItemReward(true, cv);
                                    break;
                                }
                                legMatchesOtherReward = false;
                                foreach(QuestScript existingQS in GameMasterScript.heroPCActor.myQuests)
                                {
                                    if (existingQS.itemReward != null && existingQS.itemReward.actorRefName == itemReward.actorRefName)
                                    {
                                        legMatchesOtherReward = true;
                                        break;
                                    }
                                }
                                if (legMatchesOtherReward)
                                {
                                    itemReward = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "legendary");
                                    tries++;
                                }
                            }
                            
                        }

                        itemRewardID = itemReward.actorUniqueID;
                    }
                    else if (qType == QuestType.BOSSGANG)
                    {
                        // Boss gangs drop skill orbs
                        itemReward = ItemWorldUIScript.CreateItemWorldOrb(cv, true, true);
                        if (itemReward != null) // 312019 - Maybe the above could fail?
                        {
                        itemRewardID = itemReward.actorUniqueID;
                        }
                    }
                    else
                    {
                        if (cv >= 1.4f && UnityEngine.Random.Range(0, 1f) <= CHANCE_LUCID_ORB_REPLACE_ITEM
                            && ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1)
                        {
                            itemReward = ItemWorldUIScript.CreateItemWorldOrb(cv, true);
                            if (itemReward != null) // 312019 - Maybe the above could fail? 
                            {
                            itemRewardID = itemReward.actorUniqueID;
                            }
                        }
                        else
                        {
                            GenerateItemReward(true, cv);
                            /* if (qType == QuestType.APPEASEMONSTER || qType == QuestType.FINDAREA || qType == QuestType.FINDITEM)
                            {
                                GenerateItemReward(false, cv);
                            }
                            else
                            {
                                GenerateItemReward(true, cv);
                            } */
                            
                        }
                    }

                    break;
                case QuestRewardTypes.GOLD:
                    goldReward += (int)(cubeCV * 90f) + ((GameMasterScript.heroPCActor.myStats.GetLevel() - 1) * 40);
                    goldReward += UnityEngine.Random.Range(60, 70);
                    goldReward = (int)(goldReward * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.GOLD_GAIN));
                    if (RandomJobMode.IsCurrentGameInRandomJobMode()) goldReward = (int)(goldReward * RandomJobMode.GetRJGoldMultiplier());
                    break;
                case QuestRewardTypes.XP:
                    xpReward += (int)(cubeCV * 50f) - 30;

                    break;
                case QuestRewardTypes.JP:
                    jpReward += (int)(triCV * 30f);

                    break;
                case QuestRewardTypes.CONSUMABLE_PACK:
                    // This is handled upon quest completion, not pre-generated.
                    consumableRewardPack = true;
                    break;
            }
            if (shouldHaveItem && itemReward == null)
            {
            }
            else
            {
            rewardTypesUsed[(int)typeToUse] = true;

            //Debug.Log("Used reward type " + typeToUse); // Delete this once we solved the quest reward generation problem.
            }            
        }
    }

    public void GenerateItemReward(bool guaranteeEquipment, float cv)
    {
        //Debug.Log("Generate reward");
        if (guaranteeEquipment)
        {
            itemReward = LootGeneratorScript.GenerateLootFromTable(cv, 2f, "equipment");
            if (itemReward.rarity == Rarity.COMMON && itemReward.IsEquipment())
            {
                Equipment eq = itemReward as Equipment;
                EquipmentBlock.MakeMagical(eq, cv, false);
            }
        }
        else
        {
            itemReward = LootGeneratorScript.GenerateLoot(cv, 2f);
        }

        

        itemRewardID = itemReward.actorUniqueID;
    }

    public class QuestMonsterMapPair
    {
        public string monsterRef;
        public MonsterTemplateData mtd;
        public Map map;
        public int mapID;

        public QuestMonsterMapPair(MonsterTemplateData _mtd, Map _map)
        {
            map = _map;
            mapID = _map.mapAreaID;
            mtd = _mtd;
            monsterRef = mtd.refName;
        }
    }

    static Dictionary<string, Map> monsterRefsToMaps;
    static List<string> possibleMonsters;

    public static void HeroFailedQuest(QuestScript qs)
    {
        StringManager.SetTag(0, qs.displayName);
        if (qs.targetActor != null && qs.targetActor.GetActorType() == ActorTypes.MONSTER)
        {
            Monster m = qs.targetActor as Monster;
            m.myStats.RemoveStatusByRef("enemy_quest_target");
        }
        if (qs.targetMonster != null)
        {
            Monster m = qs.targetMonster as Monster;
            m.myStats.RemoveStatusByRef("enemy_quest_target");
        }
        GameLogScript.LogWriteStringRef("log_quest_failed");
        qs.complete = true; // will be removed later
        UIManagerScript.PlayCursorSound("Failure");
        RumorTextOverlay.OnRumorCompletedOrFailed();

        QuestScript.OnQuestFailedOrAbandoned(qs);

    }

    public static void CompleteQuest(QuestScript qs)
    {
        StringManager.SetTag(0, qs.displayName);
        StringManager.SetTag(1, qs.rewardsText);

        GameMasterScript.gmsSingleton.SetTempStringData("rumor_name", qs.displayName);
        GameMasterScript.gmsSingleton.SetTempStringData("rumor_rewards", qs.rewardsText);

        UIManagerScript.PlayCursorSound("CookingSuccess");

        if (!GameMasterScript.playerDied)
        {
            UIManagerScript.StartConversationByRef("rumor_complete", DialogType.STANDARD, null);
        }        

        /* UIManagerScript.ToggleDialogBox(DialogType.TUTORIAL, true, false);
        UIManagerScript.SetDialogPos(0, 0f);
        UIManagerScript.CreateDialogOption("<color=yellow>" + StringManager.GetString("misc_celebrate") + "</color>", DialogButtonResponse.EXIT);
        string text = "<size=50>" + StringManager.GetString("misc_completed_quest").ToUpperInvariant() + ": " + qs.displayName + " </size>\n\n";
        text += "<size=50>" + StringManager.GetString("misc_rewards") + ":</size>\n\n";
        text += qs.rewardsText;
        UIManagerScript.DialogBoxWrite(text);
        UIManagerScript.UpdateDialogCursorPos(); */

        qs.complete = true;

        GameLogScript.GameLogWrite(StringManager.GetString("misc_completed_quest_excite") + " (" + qs.displayName + ")", GameMasterScript.heroPCActor);

        string debugLogText = "player finish qtype " + qs.qType;

        if (qs.goldReward > 0)
        {
            debugLogText += " gold: " + qs.goldReward;
            GameMasterScript.heroPCActor.ChangeMoney(qs.goldReward, doNotAlterFromGameMods:true);
        }
        if (qs.jpReward > 0)
        {
            debugLogText += " jp: " + qs.jpReward;
            GameMasterScript.gmsSingleton.AwardJP(qs.jpReward);
        }
        if (qs.xpReward > 0)
        {
            debugLogText += " xp: " + qs.xpReward;
            GameMasterScript.gmsSingleton.AwardXPFlat(qs.xpReward, false);
        }

        if (qs.itemReward != null)
        {
            debugLogText += " item: " + qs.itemReward.displayName;
            GameMasterScript.heroPCActor.myInventory.AddItem(qs.itemReward, true);
        }

        if (qs.consumableRewardPack)
        {
            debugLogText += " con pack";
            int numItems = UnityEngine.Random.Range(3, 6);
            List<Item> conItems = GetEmptyItemList();
            for (int i = 0; i < numItems; i++)
            {
                string table = "restoratives";
                if (UnityEngine.Random.Range(0,2) == 0)
                {
                    table = "food_and_meals";
                }
                Item newConsumable = LootGeneratorScript.GenerateLootFromTable(qs.challengeValue, 0f, table);
                conItems.Add(newConsumable);
            }

            string itemNameList = "";

            foreach(Item itm in conItems)
            {
                GameMasterScript.heroPCActor.myInventory.AddItem(itm, true);
                itemNameList += " " + itm.displayName;
                if (itm == conItems[conItems.Count-1]) // last in list
                {
                    // nothing
                }
                else
                {
                    itemNameList += ",";
                }
            }

            StringManager.SetTag(0, itemNameList);
            GameLogScript.LogWriteStringRef("log_reward_itempack");
            ReturnItemListToStack(conItems);
        }

        RumorTextOverlay.OnRumorCompletedOrFailed();

        //Debug.Log(debugLogText);
        UIManagerScript.RefreshPlayerStats();
    }

    /* public Map FindConnection(int floor)
    {
        Map questMap = MapMasterScript.theDungeon.FindFloor(floor);

        if (!questMap.dungeonLevelData.sideArea)
        {
            return null;
        }
    } */

    public void DoQuestSetup()
    {
        switch (qType)
        {
            case QuestType.TAMEMONSTER:
                if (targetMonster != null)
                {
                    targetMonster.ConvertToWildUntamedForQuest();
                }
                
                break;
        }
    }

    public static IEnumerator WaitThenCompleteQuest(QuestScript qs, float time)
    {
        yield return new WaitForSeconds(time);

        CompleteQuest(qs);
        GameMasterScript.heroPCActor.myQuests.Remove(qs);
    }

    public string GetTargetMonDisplayName()
    {
        string targetMonDisplayName = "";
        if (targetMonster == null)
        {
            targetMonDisplayName = MonsterManagerScript.GetMonsterDisplayNameByRef(targetRef);
        }
        else
        {
            targetMonDisplayName = targetMonster.displayName;
        }

        return targetMonDisplayName;
    }


    public static void OnQuestFailedOrAbandoned(QuestScript qs)
    {
        if (qs.qType != QuestType.APPEASEMONSTER) return;
        if (qs.targetMonster == null) 
        {
            if (qs.targetMonsterID > 0)
            {
                Actor mob;
                if (!GameMasterScript.dictAllActors.TryGetValue(qs.targetMonsterID, out mob))
                {
                    return;
                }

                qs.targetMonster = mob as Monster;
                if (qs.targetMonster == null)
                {
                    return;
                }
            }
        }

        if (qs.targetMonster != null)
        {
            qs.targetMonster.RemoveItemLovingAttributeFromSelf();
        }

    }
}
