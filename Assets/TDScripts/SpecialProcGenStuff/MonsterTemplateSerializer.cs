using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System;

// Handles writing and reading custom monster TEMPLATES to a save file
// This is different than saving a single instance of a monster, as we have to save the entire template here.
public class MonsterTemplateSerializer
{
    /// <summary>
    /// Must start at a "power" node in custom monster save info
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static MonsterPowerData ReadPowerFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        MonsterPowerData mpd = new MonsterPowerData();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name)
            {
                case "ref":
                    string powerRef = reader.ReadElementContentAsString();
                    GameMasterScript.masterAbilityList.TryGetValue(powerRef, out mpd.abilityRef);
                    break;
                case "minrange":
                    mpd.minRange = reader.ReadElementContentAsInt();
                    break;
                case "maxrange":
                    mpd.minRange = reader.ReadElementContentAsInt();
                    break;
                case "chance":
                    mpd.chanceToUse = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "health":
                    mpd.healthThreshold = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "reqdata":
                    mpd.reqActorData = reader.ReadElementContentAsString();
                    break;
                case "reqdatavalue":
                    mpd.reqActorDataValue = reader.ReadElementContentAsInt();
                    break;
                case "usestate":
                    mpd.useState = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString());
                    break;
                case "ignorecosts":
                    mpd.ignoreCosts = reader.ReadElementContentAsBoolean();
                    break;
                case "notarget":
                    mpd.useWithNoTarget = reader.ReadElementContentAsBoolean();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();

        return mpd;
    }

    /// <summary>
    /// If we are here, we must be on a "custommonster" XML node. Creates the monster template, any weapon/powers, and then adds to master monster dict.
    /// </summary>
    /// <param name="reader"></param>
    public static MonsterTemplateData ReadCustomMonsterFromSave(XmlReader reader)
    {
        reader.ReadStartElement(); // "custommonster";

        MonsterTemplateData monsterFromSave = new MonsterTemplateData();

        string unparsed;
        string[] parsed;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name)
            {
                case "coreinfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    monsterFromSave.refName = parsed[0];
                    monsterFromSave.monsterName = parsed[1];
                    monsterFromSave.challengeValue = CustomAlgorithms.TryParseFloat(parsed[2]);
                    monsterFromSave.monFamily = parsed[3];
                    monsterFromSave.faction = (Faction)Enum.Parse(typeof(Faction), parsed[4]);
                    monsterFromSave.prefab = parsed[5];
                    monsterFromSave.baseLevel = Int32.Parse(parsed[6]);
                    break;
                case "extendedinfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    monsterFromSave.moveRange = Int32.Parse(parsed[0]);
                    monsterFromSave.aggroRange = Int32.Parse(parsed[1]);
                    monsterFromSave.turnsToLoseInterest = Int32.Parse(parsed[2]);
                    monsterFromSave.xpMod = CustomAlgorithms.TryParseFloat(parsed[3]);
                    monsterFromSave.lootChance = CustomAlgorithms.TryParseFloat(parsed[4]);
                    monsterFromSave.isBoss = Boolean.Parse(parsed[5]);
                    monsterFromSave.showInPedia = Boolean.Parse(parsed[6]);
                    monsterFromSave.showBossHealthBar = Boolean.Parse(parsed[7]);
                    break;
                case "otherinfo":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    monsterFromSave.drunkWalkChance = CustomAlgorithms.TryParseFloat(parsed[0]);
                    monsterFromSave.stalkerRange = Int32.Parse(parsed[1]);
                    monsterFromSave.autoSpawn = Boolean.Parse(parsed[2]);
                    break;
                case "attributes":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        unparsed = reader.ReadElementContentAsString();
                        parsed = unparsed.Split('|');
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            string[] subParsed = parsed[i].Split(',');
                            MonsterAttributes attr = (MonsterAttributes)Enum.Parse(typeof(MonsterAttributes), subParsed[0]);
                            int amount = Int32.Parse(subParsed[1]);
                            monsterFromSave.monAttributes[(int)attr] = amount;
                        }
                    }

                    break;
                case "stats":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    monsterFromSave.hp = CustomAlgorithms.TryParseFloat(parsed[0]);
                    monsterFromSave.chargetime = CustomAlgorithms.TryParseFloat(parsed[1]);
                    monsterFromSave.accuracy = CustomAlgorithms.TryParseFloat(parsed[2]);
                    monsterFromSave.visionRange = CustomAlgorithms.TryParseFloat(parsed[3]);
                    monsterFromSave.strength = CustomAlgorithms.TryParseFloat(parsed[4]);
                    monsterFromSave.swiftness = CustomAlgorithms.TryParseFloat(parsed[5]);
                    monsterFromSave.guile = CustomAlgorithms.TryParseFloat(parsed[6]);
                    monsterFromSave.discipline = CustomAlgorithms.TryParseFloat(parsed[7]);
                    monsterFromSave.spirit = CustomAlgorithms.TryParseFloat(parsed[8]);
                    break;
                case "power":
                    MonsterPowerData mpd = ReadPowerFromSave(reader);
                    monsterFromSave.monsterPowers.Add(mpd);
                    break;
                case "weaponid":
                    Item customWeapon = CustomItemSerializer.ReadCustomItemFromSave(reader);
                    if (!MonsterMaker.uniqueWeaponsSpawnedInSaveFile.ContainsKey(customWeapon.actorRefName))
                    {
                        MonsterMaker.uniqueWeaponsSpawnedInSaveFile.Add(customWeapon.actorRefName, customWeapon as Weapon);
                    }                    
                    monsterFromSave.weaponID = customWeapon.actorRefName;
                    break;
                case "offhandweaponid":
                    customWeapon = CustomItemSerializer.ReadCustomItemFromSave(reader);

                    if (!MonsterMaker.uniqueWeaponsSpawnedInSaveFile.ContainsKey(customWeapon.actorRefName))
                    {
                        MonsterMaker.uniqueWeaponsSpawnedInSaveFile.Add(customWeapon.actorRefName, customWeapon as Weapon);
                    }
                    monsterFromSave.weaponID = customWeapon.actorRefName;
                    break;
                case "armorid":
                    monsterFromSave.armorID = reader.ReadElementContentAsString();
                    break;
                case "scriptondefeat":
                    monsterFromSave.scriptOnDefeat = reader.ReadElementContentAsString();
                    break;
                case "scripttakeaction":
                    monsterFromSave.scriptTakeAction = reader.ReadElementContentAsString();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement(); // fin!


        //Debug.Log("Success! Read monster " + monsterFromSave.refName);


        if (!GameMasterScript.masterMonsterList.ContainsKey(monsterFromSave.refName))
        {
            GameMasterScript.masterMonsterList.Add(monsterFromSave.refName, monsterFromSave);
        }
        return monsterFromSave;
    }

    public static void WriteCustomMonsterToSave(MonsterTemplateData mtd, XmlWriter writer)
    {
        writer.WriteStartElement("custommonster");

        // Construct core info: 
        // refname, displayname, cv, family, faction (enum), prefab, base level
        string coreInfo = mtd.refName + "|" + mtd.monsterName + "|" + mtd.challengeValue + "|" + mtd.monFamily + "|" + mtd.faction + "|" + mtd.prefab + "|" + mtd.baseLevel;
        writer.WriteElementString("coreinfo", coreInfo);

        // More info
        // move range, aggro range, turns to bore, xp mod, loot chance, is boss (bool), showinpedia (bool), showhealthbar (bool)
        string extendedInfo = mtd.moveRange + "|" + mtd.aggroRange + "|" + mtd.turnsToLoseInterest + "|" + mtd.xpMod + "|" + mtd.lootChance + "|" + mtd.isBoss + "|" + mtd.showInPedia + "|" + mtd.showBossHealthBar;
        writer.WriteElementString("extendedinfo", extendedInfo);

        // Other random stats: drunkwalk, stalkerrange, autospawn (bool)
        string otherInfo = mtd.drunkWalkChance + "|" + mtd.stalkerRange + "|" + mtd.autoSpawn;
        writer.WriteElementString("otherinfo", otherInfo);

        // All our attributes of course, like BERSERKER,10|RONIN,50
        string attributes = "";
        bool firstAttributeWritten = false;
        for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
        {
            if (mtd.monAttributes[i] > 0)
            {
                if (firstAttributeWritten)
                {
                    attributes += "|";
                }
                attributes += ((MonsterAttributes)i).ToString() + "," + mtd.monAttributes[i];
                firstAttributeWritten = true;
            }
        }
        if (!string.IsNullOrEmpty(attributes))
        {
            writer.WriteElementString("attributes", attributes);
        }        

        // And then our core STATS: Health, Str etc.
        string stats = "";
        // Health, CT, accuracy, vision, Strength, Swiftness, Guile, Discipline, Spirit
        stats = mtd.hp + "|" + mtd.chargetime + "|" + mtd.accuracy + "|" + mtd.visionRange + "|" + mtd.strength + "|" + mtd.swiftness + "|" + mtd.guile + "|" + mtd.discipline + "|" + mtd.spirit;
        writer.WriteElementString("stats", stats);

        // And power references!
        string powers = "";
        bool firstPowerWritten = false;
        
        foreach(MonsterPowerData mpd in mtd.monsterPowers)
        {
            writer.WriteStartElement("power");
            writer.WriteElementString("ref", mpd.abilityRef.refName);
            if (mpd.minRange != 1)
            {
                writer.WriteElementString("minrange", mpd.minRange.ToString());
            }
            if (mpd.maxRange != 99)
            {
                writer.WriteElementString("maxrange", mpd.maxRange.ToString());
            }
            if (mpd.chanceToUse != 1.0f)
            {
                writer.WriteElementString("chance", mpd.chanceToUse.ToString());
            }
            if (mpd.healthThreshold != 1.0f)
            {
                writer.WriteElementString("health", mpd.healthThreshold.ToString());
            }
            if (mpd.useState != BehaviorState.ANY)
            {
                writer.WriteElementString("usestate", mpd.useState.ToString());
            }
            if (mpd.ignoreCosts)
            {
                writer.WriteElementString("ignorecosts", mpd.ignoreCosts.ToString().ToLowerInvariant());
            }
            if (mpd.useWithNoTarget)
            {
                writer.WriteElementString("notarget", mpd.useWithNoTarget.ToString().ToLowerInvariant());
            }
            writer.WriteEndElement();
        }

        if (!string.IsNullOrEmpty(mtd.scriptTakeAction))
        {
            writer.WriteElementString("scripttakeaction", mtd.scriptTakeAction);
        }
        if (!string.IsNullOrEmpty(mtd.scriptOnDefeat))
        {
            writer.WriteElementString("scriptondefeat", mtd.scriptOnDefeat);
        }

        // Our weapon(s) must be serialized as well.
        writer.WriteElementString("weaponid", mtd.weaponID);
        GameMasterScript.masterItemList[mtd.weaponID].WriteEntireTemplateToSaveAsCustomItem(writer);

        if (!string.IsNullOrEmpty(mtd.offhandWeaponID))
        {
            writer.WriteElementString("offhandweaponid", mtd.offhandWeaponID);
            GameMasterScript.masterItemList[mtd.offhandWeaponID].WriteEntireTemplateToSaveAsCustomItem(writer);
        }

        // Armors are not custom generated; we don't need to serialize the armor stats.
        if (!string.IsNullOrEmpty(mtd.armorID))
        {
            writer.WriteElementString("armorid", mtd.armorID);
        }

        writer.WriteEndElement();
    }

}
