using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System;

// Handles writing and reading CUSTOM item references to and from save file.
// This is different than saving a single instance of an item, as we have to save the entire template here.
public class CustomItemSerializer
{
    public const string DELETE_RELIC_ON_EXIT = "del_on_exit_md";

    public static readonly string[] delimiter = new string[] { "</color>" };

    /// <summary>
    /// Reads an item template from XML data and adds it to the master dictionary. If we are calling this, we must be at a node called "customitem".
    /// </summary>
    /// <param name="reader"></param>
    public static Item ReadCustomItemFromSave(XmlReader reader, bool appendSaveSlot = false, int saveSlot = 0)
    {
        //Debug.Log("Request read a custom item. Append save slot? Save slot? " + appendSaveSlot + " | " + saveSlot);
        reader.ReadStartElement(); // Reads "customitem" header

        while (reader.Name != "coreinfo" && reader.Name != "ci") // read until we get to the core item info
        {
            reader.Read();
        }

        string unparsed = reader.ReadElementContentAsString();

        // itemType, refname, displayname, desc, cv, rarity, spriteref
        string[] parsed = unparsed.Split('|');

        ItemTypes iType = (ItemTypes)Enum.Parse(typeof(ItemTypes), parsed[0]);

        Item itemReadFromFile = null;

        switch (iType)
        {
            case ItemTypes.WEAPON:
                itemReadFromFile = new Weapon();
                break;
            case ItemTypes.ARMOR:
                itemReadFromFile = new Armor();
                break;
            case ItemTypes.OFFHAND:
                itemReadFromFile = new Offhand();
                break;
            case ItemTypes.ACCESSORY:
                itemReadFromFile = new Accessory();
                break;
        }

        itemReadFromFile.actorRefName = parsed[1];

        if (appendSaveSlot)
        {
            itemReadFromFile.actorRefName = itemReadFromFile.actorRefName + "_" + saveSlot;
        }

        itemReadFromFile.displayName = parsed[2];
        itemReadFromFile.description = parsed[3];
        itemReadFromFile.challengeValue = CustomAlgorithms.TryParseFloat(parsed[4]);

        if (MetaProgressScript.loadedGameVersion < 150)
        {
            try
            {
                itemReadFromFile.rarity = (Rarity)Enum.Parse(typeof(Rarity), parsed[5]);
                itemReadFromFile.spriteRef = parsed[6];
            }
            catch(Exception e)
            {
                itemReadFromFile.spriteRef = parsed[5];
            }

        }
        else
        {
            itemReadFromFile.spriteRef = parsed[5];
        }

        /* string[] up = itemReadFromFile.actorRefName.Split('_');
        int num;
        if (Int32.TryParse(up[2], out num))
        {
            if (num > 2500)
            {
                Debug.Log("Read: " + itemReadFromFile.actorRefName);
            }            
        } */            

        // Now let the item itself handle the rest.
        itemReadFromFile.ReadEntireTemplateFromSaveAsCustomItem(reader);

        itemReadFromFile.customItemFromGenerator = true;        
        
        if (!GameMasterScript.masterItemList.ContainsKey(itemReadFromFile.actorRefName))
        {
            GameMasterScript.masterItemList.Add(itemReadFromFile.actorRefName, itemReadFromFile);
#if UNITY_EDITOR
            //Debug.Log("Success! Read " + itemReadFromFile.actorRefName + " " + itemReadFromFile.displayName + " from file and added to master item dict.");
#endif
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Failed to add " + itemReadFromFile.actorRefName + " to dict, because it was already there? " + itemReadFromFile.actorRefName + " vs " + GameMasterScript.masterItemList[itemReadFromFile.actorRefName].actorRefName);
        }
        

        reader.ReadEndElement(); // Read the "customitem" closing tag.

        return itemReadFromFile;
    }
	
}

public partial class Item : Actor, ISelectableUIObject
{
    /// <summary>
    /// If a relic is in use on a specific save slot, this int tracks the slot index
    /// </summary>
    public int saveSlotIndexForCustomItemTemplate;


    public virtual void WriteEntireTemplateToSaveAsCustomItem(XmlWriter writer)
    {

    }

    /// <summary>
    /// Writes barebones item info to XML. You MUST CLOSE the "customitem" opening tag manually!
    /// </summary>
    /// <param name="writer"></param>
    public void WriteCustomItemTemplateHeader(XmlWriter writer)
    {
        writer.WriteStartElement("citm"); // switched from "customitem" to save a few bytes!

        /* if (displayName == "") displayName = "e"; // Monster items might not have a display name
        if (description == "") description = "e"; // Nor a description. We don't want to write nothing for either.
        */
        // itemType, refname, displayname, desc, cv, rarity, spriteref    

        string nameToWrite = CustomAlgorithms.StripColors(displayName);

        if (nameToWrite.Contains("+"))
        {
            string[] parsed = nameToWrite.Split(CustomItemSerializer.delimiter, StringSplitOptions.RemoveEmptyEntries);
            nameToWrite = parsed[0] + "</color>";
        }
        
        string basicInfo = itemType.ToString() + "|" + actorRefName + "|" + nameToWrite + "|" + description + "|" + challengeValue + "|" + spriteRef;
        writer.WriteElementString("ci", basicInfo);

        bool anyTags = false;
        string tagStr = "";
        foreach(string tag in numberTags)
        {
            if (anyTags)
            {
                tagStr += "|" + tag;
            }
            else
            {
                tagStr = tag;
            }
            anyTags = true;            
        }
        if (anyTags)
        {
            writer.WriteElementString("tags", tagStr);
        }

        if (dictActorData != null)
        {
            if (dictActorData.Keys.Count > 0)
            {
                string dad = "";
                bool first = true;
                foreach (var kvp in dictActorData)
                {
                    if (kvp.Key == "grc" || kvp.Key == "guaranteerelic") continue;
                    if (!first)
                    {
                        dad += "|" + kvp.Key + ":" + kvp.Value;
                    }
                    else
                    {
                        dad += kvp.Key + ":" + kvp.Value;
                        first = false;
                    }
                }
                if (!first)
                {
                    writer.WriteElementString("dictactordata", dad);
                }                
            }
        }

        if (dictActorDataString != null)
        {
            if (dictActorDataString.Keys.Count > 0)
            {
                string dad = "";
                bool first = true;
                foreach (var kvp in dictActorDataString)
                {
                    if (!first)
                    {
                        dad += "|" + kvp.Key + ":" + kvp.Value;
                    }
                    else
                    {
                        dad += kvp.Key + ":" + kvp.Value;
                        first = false;
                    }
                }
                writer.WriteElementString("dictactordatastring", dad);
            }
        }

        if (saveSlotIndexForCustomItemTemplate != 99)
        {
            writer.WriteElementString("ciss", saveSlotIndexForCustomItemTemplate.ToString());
        }
    }

    public virtual bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        bool debug = false;
#if UNITY_EDITOR
        /* if (actorRefName == "genleg_weapon_2737")
        {
            debug = true;
            Debug.Log("Consider reading base item data from this item. " + reader.Name + " " + reader.NodeType);
        } */
#endif 

        bool success = true;

        while (success)
        {
            if (reader.NodeType != XmlNodeType.EndElement)
            {
                if (debug) Debug.Log(reader.NodeType + " " + reader.Name);
                switch (reader.Name)
                {
                    case "tags":
                        string unparsed = reader.ReadElementContentAsString();
                        string[] parsed = unparsed.Split('|');
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            numberTags.Add(parsed[i]);
                        }
                        if (debug) Debug.Log("We just read tags. Now at: " + reader.NodeType + " " + reader.Name);
                        success = true;
                        break;
                    case "dictactordata":
                        if (dictActorData == null)
                        {
                            dictActorData = new Dictionary<string, int>();
                        }
                        unparsed = reader.ReadElementContentAsString();
                        parsed = unparsed.Split('|');
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            string[] subparsed = parsed[i].Split(':');
                            // 0 should be key
                            // 1 is value as int
                            int tryValue;
                            if (Int32.TryParse(subparsed[1], out tryValue))
                            {
                                dictActorData.Add(subparsed[0], tryValue);
                            }
                        }
                        success = true;
                        break;
                    case "dictactordatastring":
                        if (dictActorDataString == null)
                        {
                            dictActorDataString = new Dictionary<string, string>();
                        }
                        unparsed = reader.ReadElementContentAsString();
                        parsed = unparsed.Split('|');
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            string[] subparsed = parsed[i].Split(':');
                            dictActorDataString.Add(subparsed[0], subparsed[1]);
                        }
                        success = true;
                        break;
                    case "ciss":
                        saveSlotIndexForCustomItemTemplate = reader.ReadElementContentAsInt();
                        //if (Debug.isDebugBuild) Debug.Log("Relic template " + actorRefName + " is being used by slot " + saveSlotIndexForCustomItemTemplate);
                        break;
                    default:
                        success = false;
                        break;
                }
            }
        }

        return false;
    }
}

public partial class Equipment : Item
{
    public override bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        base.ReadEntireTemplateFromSaveAsCustomItem(reader);

        bool debug = false;
#if UNITY_EDITOR
        /* if (actorRefName == "genleg_weapon_2737")
        {
            debug = true;
        } */
#endif

        if (reader.NodeType != XmlNodeType.EndElement)
        {
            if (debug) Debug.Log(reader.NodeType + " " + reader.Name);
            switch (reader.Name)
            {
                case "mid":
                    ReadEQAutoModIDs(reader);
                    return true;
                case "amods":
                case "automods":
                    ReadEQAutoModsFromCustomItem(reader);
                    if (debug) Debug.Log("We just read mods. Now at: " + reader.NodeType + " " + reader.Name);
                    return true;
                case "advstats":
                    ReadAdventureStats(reader);
                    if (debug) Debug.Log("We just read adv. Now at: " + reader.NodeType + " " + reader.Name);
                    return true;
                case "res":
                case "resists":
                    ReadResistsFromSave(reader);
                    if (debug) Debug.Log("We just read res. Now at: " + reader.NodeType + " " + reader.Name);
                    return true;
            }
        }

        if (debug) Debug.Log("didn't find anything to read");
        return false;
    }

    public void WriteEQAutoModsToCustomItem(XmlWriter writer)
    {
        if (autoModRef == null || autoModRef.Count == 0) return;

        string modBuilder = "";
        string autoModBuilder = "";

        bool first = true;
        bool stringFirst = true;
        foreach (string modRef in autoModRef)
        {
            if (!first) modBuilder += "|";
            first = false;
            if (string.IsNullOrEmpty(modRef)) continue;
            //Debug.Log("Write " + modRef);

            if (!GameMasterScript.masterMagicModList.TryGetValue(modRef, out MagicMod magicMod))
            {
                Debug.Log("Couldn't find magic mod " + modRef + " when trying to save an item's custom magic mods.");
                continue;
            }

            int ID = magicMod.magicModID;
            if (ID == 0)
            {
                Debug.Log("WARNING: Magic mod " + modRef + " DOES NOT HAVE an id!");
                if (!stringFirst) autoModBuilder += "|";
                autoModBuilder += modRef;
                stringFirst = false;
            }
            else
            {
                modBuilder += ID;
            }            
        }

        if (!string.IsNullOrEmpty(modBuilder))
        {
            writer.WriteElementString("mid", modBuilder); // was automods
        }
        if (!string.IsNullOrEmpty(autoModBuilder))
        {
            writer.WriteElementString("amods", modBuilder); // was automods
        }                
    }

    public void ReadEQAutoModIDs(XmlReader reader)
    {
        if (autoModRef == null) autoModRef = new List<string>();

        string unparsed = reader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');

        for (int i = 0; i < parsed.Length; i++)
        {
            int modID = 0;
            if (!int.TryParse(parsed[i], out modID))
            {
                Debug.Log("Failed to parse relic automod ID " + parsed[i]);
            }
            else
            {
                MagicMod mm;
                if (!GameMasterScript.dictMagicModIDs.TryGetValue(modID, out mm))
                {
                    Debug.Log("Magic mod with ID " + modID + " does not exist");
                }
                else
                {
                    autoModRef.Add(mm.refName);
                }
            }
            
        }
    }

    public void ReadEQAutoModsFromCustomItem(XmlReader reader)
    {
        if (autoModRef == null) autoModRef = new List<string>();

        string unparsed = reader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');

        for (int i = 0; i < parsed.Length; i++)
        {
            autoModRef.Add(parsed[i]);
        }
    }

    public void WriteEquipmentInfoToSaveAsCustomItem(XmlWriter writer)
    {
        WriteEQAutoModsToCustomItem(writer);

        string advStatsString = CreateAdventureStatsString();
        if (advStatsString != "")
        {
            writer.WriteElementString("advstats", advStatsString);
        }

        WriteEQResists(writer);
    }
}

public partial class Weapon : Equipment
{
    public override void WriteEntireTemplateToSaveAsCustomItem(XmlWriter writer)
    {
        WriteCustomItemTemplateHeader(writer);

        WriteEquipmentInfoToSaveAsCustomItem(writer);

        string weaponInfoString = "";

        // weap type, damtype, power, range(#), isRanged(bool), 2h (bool), flavordamage
        weaponInfoString = weaponType.ToString() + "|" + damType.ToString() + "|" + power + "|" + range;
        weaponInfoString += "|" + isRanged + "|" + twoHanded + "|" + flavorDamType.ToString();

        writer.WriteElementString("wnf", weaponInfoString);
        if (!string.IsNullOrEmpty(impactEffect))
        {
            writer.WriteElementString("ie", impactEffect);
        }
        if (!string.IsNullOrEmpty(swingEffect))
        {
            writer.WriteElementString("se", swingEffect);
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Returns TRUE if we read weapon-specific data, FALSE if we didn't.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public override bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        bool debug = false;
#if UNITY_EDITOR
        /* if (actorRefName == "genleg_weapon_2737")
        {
            debug = true;
        } */
#endif

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            // Base item, equipment read nothing, so let's try our custom stuff.
            if (!base.ReadEntireTemplateFromSaveAsCustomItem(reader))
            {
                if (debug) Debug.Log(reader.NodeType + " " + reader.Name + " wreader");
                switch (reader.Name)
                {
                    case "wnf":
                    case "weaponinfo":
                        string unparsed = reader.ReadElementContentAsString();
                        string[] parsed = unparsed.Split('|');
                        // weap type, damtype, power, range(#), isRanged(bool), 2h (bool), flavordamage

                        weaponType = (WeaponTypes)Enum.Parse(typeof(WeaponTypes), parsed[0]);
                        damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), parsed[1]);
                        power = CustomAlgorithms.TryParseFloat(parsed[2]);
                        range = Int32.Parse(parsed[3]);
                        isRanged = Boolean.Parse(parsed[4]);
                        twoHanded = Boolean.Parse(parsed[5]);
                        flavorDamType = (FlavorDamageTypes)Enum.Parse(typeof(FlavorDamageTypes), parsed[6]);
                        break;
                    case "ie":
                    case "impacteffect":
                        impactEffect = reader.ReadElementContentAsString();
                        break;
                    case "se":
                    case "swingeffect":
                        swingEffect = reader.ReadElementContentAsString();
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
            else
            {
                if (debug) Debug.Log("Good read! " + reader.NodeType + " " + reader.Name);
            }
        }

        return false; 
    }
}

public partial class Offhand : Equipment
{
    public override void WriteEntireTemplateToSaveAsCustomItem(XmlWriter writer)
    {
        WriteCustomItemTemplateHeader(writer);

        WriteEquipmentInfoToSaveAsCustomItem(writer);

        if (blockChance > 0)
        {
            string blockinfo = blockChance + "|" + blockDamageReduction;
            writer.WriteElementString("bi", blockinfo);
        }
        if (allowBow)
        {
            writer.WriteElementString("allowbow", "true");
        }

        writer.WriteEndElement();
    }

    public override bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (!base.ReadEntireTemplateFromSaveAsCustomItem(reader))
            {
                switch (reader.Name)
                {
                    case "bi":
                    case "blockinfo":
                        string unparsed = reader.ReadElementContentAsString();
                        string[] parsed = unparsed.Split('|');
                        blockChance = CustomAlgorithms.TryParseFloat(parsed[0]);
                        blockDamageReduction = CustomAlgorithms.TryParseFloat(parsed[1]);
                        break;
                    case "allowbow":
                        allowBow = reader.ReadElementContentAsBoolean();
                        break;
                    default:
                        reader.Read();
                        break;
                }                
            }
        }

        // Finish on "customitem", in theory        

        return false;
    }
}

public partial class Accessory : Equipment
{
    public override void WriteEntireTemplateToSaveAsCustomItem(XmlWriter writer)
    {
        WriteCustomItemTemplateHeader(writer);

        WriteEquipmentInfoToSaveAsCustomItem(writer);

        if (uniqueEquip)
        {
            writer.WriteElementString("uniqueequip", "true");
        }

        writer.WriteEndElement();
    }

    public override bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (!base.ReadEntireTemplateFromSaveAsCustomItem(reader))
            {
                switch(reader.Name)
                {
                    case "uniqueequip":
                        uniqueEquip = reader.ReadElementContentAsBoolean();
                        break;
                    default:
                        reader.Read();
                        break;
                }
                
            }
        }

        // Finish on "customitem", in theory        

        return false;
    }
}

public partial class Armor : Equipment
{
    public override void WriteEntireTemplateToSaveAsCustomItem(XmlWriter writer)
    {
        WriteCustomItemTemplateHeader(writer);

        WriteEquipmentInfoToSaveAsCustomItem(writer);

        writer.WriteElementString("at", armorType.ToString());

        if (extraDodge > 0)
        {
            writer.WriteElementString("dge", extraDodge.ToString());
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Returns TRUE if we read armor-specific data, FALSE if we didn't.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public override bool ReadEntireTemplateFromSaveAsCustomItem(XmlReader reader)
    {
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (!base.ReadEntireTemplateFromSaveAsCustomItem(reader))
            {
                switch (reader.Name)
                {
                    case "at":
                    case "armortype":
                        armorType = (ArmorTypes)Enum.Parse(typeof(ArmorTypes), reader.ReadElementContentAsString());
                        break;
                    case "dge":
                    case "extradodge":
                        extraDodge = reader.ReadElementContentAsInt();
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
        }

        // Finish on "customitem", in theory        

        return false;
    }
}
