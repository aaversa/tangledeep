using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml;
using System.Text;

// Save data related to mystery dungeons
public class MysteryDungeonSaveData
{
    public bool dungeonVictory;
    public string dungeonRefName;
    public InventoryScript inventoryPriorToEntry;
    public EquipmentBlock eqPriorToEntry;
    public StatBlock statsPriorToEntry;
    public FighterBattleData fighterDataPriorToEntry;
    public int pandoraBoxesPriorToEntry;
    public int flaskUsesPriorToEntry;
    public int moneyPriorToEntry;
    public int[] jpPerJobPriorToEntry;
    public AbilityComponent abilitiesPriorToEntry;
    public float[] jobJPPriortoEntry;
    public float[] jobJPSpentPriorToEntry;
    public int[] hotbarWeaponsPriorToEntry;
    public HotbarBindable[] hotbarBindingsPriorToEntry;
    public List<PlayingCard> playingCardsPriorToEntry;
    public float[] advStatsPriorToEntry;
    public bool[] actorFlagsPriorToEntry;
    public float allDamageMultiplierPriorToEntry;
    public float allMitigationPriorToEntry;
    public Dictionary<string, int> dictSavedActorData;
    public CharacterJobs jobPriorToEntry;
    public List<int> idsOfBankedRelicsForVictory;

    public MysteryDungeonSaveData()
    {
        inventoryPriorToEntry = new InventoryScript();
        eqPriorToEntry = new EquipmentBlock();
        statsPriorToEntry = new StatBlock();
        fighterDataPriorToEntry = new FighterBattleData();
        abilitiesPriorToEntry = new AbilityComponent();
        jobJPPriortoEntry = new float[(int)CharacterJobs.COUNT];
        jobJPSpentPriorToEntry = new float[(int)CharacterJobs.COUNT];
        hotbarWeaponsPriorToEntry = new int[4];
        hotbarBindingsPriorToEntry = new HotbarBindable[16];
        playingCardsPriorToEntry = new List<PlayingCard>();
        advStatsPriorToEntry = new float[(int) AdventureStats.COUNT];
        actorFlagsPriorToEntry = new bool[(int)ActorFlags.COUNT];
        allDamageMultiplierPriorToEntry = 1f;
        allMitigationPriorToEntry = 1f;
        dictSavedActorData = new Dictionary<string, int>();
        jobPriorToEntry = CharacterJobs.BRIGAND;
        idsOfBankedRelicsForVictory = new List<int>();
    }

    public void AddPlayingCard(PlayingCard pc)
    {
        playingCardsPriorToEntry.Add(pc);
    }

    public void AddBankedRelic(Item itm)
    {
        if (idsOfBankedRelicsForVictory.Contains(itm.actorUniqueID))
        {
            Debug.Log("What? " + itm.actorRefName + " " + itm.actorUniqueID + " is already in our ID list???");
        }
        else
        {
            idsOfBankedRelicsForVictory.Add(itm.actorUniqueID);
            // Make sure this is NOT in use for any slot.
            itm.saveSlotIndexForCustomItemTemplate = 99;
            if (Debug.isDebugBuild) Debug.Log(itm.actorRefName + " is no longer marked in use by any slot.");
        }
    }

    /// <summary>
    /// Must be on "mysterydungeondata" node
    /// </summary>
    /// <param name="reader"></param>
    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        string unparsed;
        string[] parsed;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "coreinfo":
                    // dungeon ref, defeated(bool), flask uses, money, pandora
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    dungeonRefName = parsed[0];
                    dungeonVictory = Boolean.Parse(parsed[1]);
                    flaskUsesPriorToEntry = Int32.Parse(parsed[2]);
                    moneyPriorToEntry = Int32.Parse(parsed[3]);
                    pandoraBoxesPriorToEntry = Int32.Parse(parsed[4]);
                    break;
                case "actorflags":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        actorFlagsPriorToEntry[i] = Boolean.Parse(parsed[i]);
                    }
                    break;
                case "job":
                    jobPriorToEntry = (CharacterJobs)reader.ReadElementContentAsInt();
                    break;
                case "sts":
                    statsPriorToEntry = new StatBlock();
                    statsPriorToEntry.ReadFromSave(reader, true, false, true);
                    //if (Debug.isDebugBuild) Debug.Log("Loaded stats! " + statsPriorToEntry.GetCurStat(StatTypes.HEALTH) + " " + statsPriorToEntry.GetLevel());
                    break;
                case "inv":
                    inventoryPriorToEntry = new InventoryScript();
                    inventoryPriorToEntry.ReadFromSave(reader, true, false);
                    break;
                case "ability":
                    if (abilitiesPriorToEntry == null)
                    {
                        abilitiesPriorToEntry = new AbilityComponent();
                    }
                    reader.ReadStartElement();
                    AbilityScript newAbil = new AbilityScript();
                    newAbil.ReadFromSave(reader, GameMasterScript.heroPCActor);
                    abilitiesPriorToEntry.abilities.Add(newAbil);
                    if (!abilitiesPriorToEntry.dictAbilities.ContainsKey(newAbil.refName))
                    {
                        abilitiesPriorToEntry.dictAbilities.Add(newAbil.refName, newAbil);
                    }
                    break;
                case "eq":
                    eqPriorToEntry = new EquipmentBlock();
                    reader.ReadStartElement();
                    eqPriorToEntry.ReadFromSave(reader, false, false);
                    break;
                case "jp":
                    //             writer.WriteElementString("jp", jobJP.ToString() + ":" + jobJPSpent.ToString());
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split(':'); // 0 is jp, 1 is jp spent
                    string[] jp = parsed[0].Split('|');
                    string[] jpSpent = parsed[1].Split('|');
                    for (int i = 0; i < jp.Length; i++)
                    {
                        jobJPPriortoEntry[i] = CustomAlgorithms.TryParseFloat(jp[i]);
                        jobJPSpentPriorToEntry[i] = CustomAlgorithms.TryParseFloat(jpSpent[i]);
                    }
                    break;
                case "weaponhotbar":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        int itemID = Int32.Parse(parsed[i]);
                        hotbarWeaponsPriorToEntry[i] = itemID;
                    }
                    break;
                case "hotbarbindings":
                    reader.ReadStartElement();
                    int index = 0;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "hba":
                                HotbarBindable hb = new HotbarBindable();
                                hb.ReadFromSave(reader);
                                hotbarBindingsPriorToEntry[index] = hb;
                                index++;
                                break;
                            default:
                                reader.Read();
                                break;
                        }

                    }
                    reader.ReadEndElement();
                    break;
                case "advstats":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        advStatsPriorToEntry[i] = CustomAlgorithms.TryParseFloat(parsed[i]);                        
                    }
                    break;
                case "playingcard":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    int face = Int32.Parse(parsed[0]);
                    int suit = Int32.Parse(parsed[1]);
                    PlayingCard pc = PlayingCard.DrawSpecificCard((CardSuit)suit, (CardFace)face);
                    playingCardsPriorToEntry.Add(pc);
                    break;
                case "combatmults":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    allDamageMultiplierPriorToEntry = CustomAlgorithms.TryParseFloat(parsed[0]);
                    allMitigationPriorToEntry = CustomAlgorithms.TryParseFloat(parsed[0]);
                    break;
                case "btld":
                case "battledata":
                    fighterDataPriorToEntry.ReadFromSave(reader, null);
                    break;
                case "savedactordata":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        string key = reader.Name;
                        int value = reader.ReadElementContentAsInt();
                        dictSavedActorData.Add(key, value);
                    }
                    reader.ReadEndElement();
                    break;
                case "bankedrelics":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        int id = Int32.Parse(parsed[i]);
                        idsOfBankedRelicsForVictory.Add(id);
                    }
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
        MysteryDungeon activeDungeon = MysteryDungeonManager.GetActiveDungeon();

        writer.WriteStartElement("mysterydungeondata");

        // dungeon ref, defeated(bool), flask uses, money
        string coreInfo = activeDungeon.refName + "|" + dungeonVictory + "|" + flaskUsesPriorToEntry + "|" + moneyPriorToEntry + "|" + pandoraBoxesPriorToEntry;
        writer.WriteElementString("coreinfo", coreInfo);

        /* string monstersCreatedInThisDungeon = "";
        bool firstWrite = true;
        foreach(MonsterTemplateData mtd in activeDungeon.monstersInDungeon)
        {
            if (!firstWrite)
            {
                monstersCreatedInThisDungeon += "|";
            }
            firstWrite = false;
            monstersCreatedInThisDungeon += mtd.refName;
        }
        writer.WriteElementString("monsters", monstersCreatedInThisDungeon); */

        if (idsOfBankedRelicsForVictory.Count > 0)
        {
            StringBuilder relicsBanked = new StringBuilder();
            for (int i = 0; i < idsOfBankedRelicsForVictory.Count; i++)
            {
                if (i != 0) relicsBanked.Append('|');
                relicsBanked.Append(idsOfBankedRelicsForVictory[i]);
            }
            writer.WriteElementString("bankedrelics", relicsBanked.ToString());
        }

        // Were we not allowed to bring in our items? Save that here.
        if (!activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.ITEMS])
        {
            if (inventoryPriorToEntry.GetInventory().Count > 0)
            {
                inventoryPriorToEntry.WriteToSave(writer);
            }
        }

        // Same with gear
        if (!activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            eqPriorToEntry.WriteToSave(writer);
            string weaponHotbar = "";
            for (int i = 0; i < hotbarWeaponsPriorToEntry.Length; i++)
            {
                if (i > 0) weaponHotbar += "|";
                weaponHotbar += hotbarWeaponsPriorToEntry[i];
            }
            writer.WriteElementString("weaponhotbar", weaponHotbar);

            string advStats = "";
            for (int i = 0; i < advStatsPriorToEntry.Length; i++)
            {
                if (i > 0)
                {
                    advStats += "|";
                }
                advStats += advStatsPriorToEntry[i];
            }
            writer.WriteElementString("advstats",advStats);
        }

        // Same with stats
        if (!activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            statsPriorToEntry.WriteToSave(writer, true, true);
            fighterDataPriorToEntry.WriteToSave(writer);

            if (allDamageMultiplierPriorToEntry != 1f || allMitigationPriorToEntry != 1f)
            {
                writer.WriteElementString("combatmults", allDamageMultiplierPriorToEntry + "|" + allMitigationPriorToEntry);
            }

            string flagBuilder = "";
            for (int i = 0; i < (int)ActorFlags.COUNT; i++)
            {
                if (i > 0) flagBuilder += "|";
                flagBuilder += actorFlagsPriorToEntry[i];
            }
            writer.WriteElementString("actorflags", flagBuilder);
        }

        // Same with skills
        if (!activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS])
        {
            abilitiesPriorToEntry.WriteToSave(writer, true);
            writer.WriteStartElement("hotbarbindings");
            for (int i = 0; i < hotbarBindingsPriorToEntry.Length; i++)
            {
                hotbarBindingsPriorToEntry[i].WriteToSave(writer);
            }
            writer.WriteEndElement();

            StringBuilder jobJP = new StringBuilder();
            StringBuilder jobJPSpent = new StringBuilder();
            int length = jobJPPriortoEntry.Length;
            for (int i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    jobJP.Append("|");
                    jobJPSpent.Append("|");
                }
                jobJP.Append(jobJPPriortoEntry[i]);
                jobJPSpent.Append(jobJPSpentPriorToEntry[i]);
            }
            writer.WriteElementString("jp", jobJP.ToString() + ":" + jobJPSpent.ToString());
        }

        writer.WriteElementString("job", ((int)jobPriorToEntry).ToString());

        foreach(PlayingCard pc in playingCardsPriorToEntry)
        {
            string cardBuilder = (int)pc.face + "|" + (int)pc.suit;
            writer.WriteElementString("playingcard", cardBuilder);
        }

        if (dictSavedActorData.Keys.Count > 0)
        {
            writer.WriteStartElement("savedactordata");
            foreach (string key in dictSavedActorData.Keys)
            {
                writer.WriteElementString(key, dictSavedActorData[key].ToString());
            }
            writer.WriteEndElement();
        }


        writer.WriteEndElement();
    }
}