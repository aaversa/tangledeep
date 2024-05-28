using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[System.Serializable]
[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public partial class Armor : Equipment
{
    public float defense;
    public bool resistMessage;
    public ArmorTypes armorType;
    public static string[] armorProperties;
    public int extraDodge;
    public static string[] armorTypesVerbose;

    public Armor() : base()
    {
        addAbilities = new List<AbilityScript>();
        defense = 0.0f;
        extraDodge = 0;
        slot = EquipmentSlots.ARMOR;
        itemType = ItemTypes.ARMOR;
        if (armorProperties == null)
        {
            armorProperties = new string[(int)ArmorTypes.COUNT];
            armorProperties[(int)ArmorTypes.LIGHT] = StringManager.GetString("armor_light_desc");
            armorProperties[(int)ArmorTypes.MEDIUM] = StringManager.GetString("armor_medium_desc");
            armorProperties[(int)ArmorTypes.HEAVY] = StringManager.GetString("armor_heavy_desc");
        }
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        if (extraDodge != 0)
        {
            writer.WriteElementString("dge", extraDodge.ToString());
        }

        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item template)
    {
        base.CopyFromItem(template);
        Armor armorTemplate = template as Armor;
        CopyFromEquipment(armorTemplate);
        defense = armorTemplate.defense;
        armorType = armorTemplate.armorType;
        resistMessage = armorTemplate.resistMessage;
        extraDodge = armorTemplate.extraDodge;
    }

    public override string GetItemWorldUpgrade()
    {
        string builder = "";

        if (timesUpgraded > Equipment.GetMaxUpgrades()-1)
        {
            return StringManager.GetString("ui_itemworld_noupgrade");
        }

        float hypotheticalCV = challengeValue + 0.15f;

        switch (armorType)
        {
            case ArmorTypes.LIGHT:
            case ArmorTypes.MEDIUM:
                MagicMod mmRemove = null;
                int dodgeAmount = 0;
                foreach (MagicMod mm in mods)
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
                    //mods.Remove(mmRemove);
                    if (armorType == ArmorTypes.MEDIUM)
                    {
                        builder = "+1% " + StringManager.GetString("stat_dodge") + "\n";
                    }
                    else
                    {
                        builder = "+2% " + StringManager.GetString("stat_dodge") + "\n";
                    }
                }
                else
                {
                    // Add dodge
                    builder = "+2% " + StringManager.GetString("stat_dodge");
                }
                break;
        }

        // Phys resists
        switch (armorType)
        {
            case ArmorTypes.MEDIUM:
            case ArmorTypes.HEAVY:

                if (armorType == ArmorTypes.MEDIUM)
                {
                    float offset = (1 * hypotheticalCV);
                    StringManager.SetTag(0, "+1%");
                    StringManager.SetTag(1, "-" + offset);
                }
                else
                {
                    float offset = (2 * hypotheticalCV);
                    StringManager.SetTag(0, "+2.3%");
                    StringManager.SetTag(1, "-" + offset);
                }
                break;

        }

        if (armorType != ArmorTypes.LIGHT)
        {
            builder += StringManager.GetString("ui_armordefense_desc");
        }

        return builder;
    }

    public override void UpgradeItem(bool debug = false)
    {
        base.UpgradeItem(debug);

        // Dodge
        switch (armorType)
        {
            case ArmorTypes.LIGHT:
            case ArmorTypes.MEDIUM:
                MagicMod mmRemove = null;
                int dodgeAmount = 0;
                foreach (MagicMod mm in mods)
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
                    //mods.Remove(mmRemove);
                    if (armorType == ArmorTypes.MEDIUM)
                    {
                        dodgeAmount += 1;
                        extraDodge += 1;
                    }
                    else
                    {
                        dodgeAmount += 2;
                        extraDodge += 2;
                    }
                    //newMod = MagicMod.FindModFromName("mm_dodge" + dodgeAmount);                    
                }
                else
                {
                    // Add dodge
                    //newMod = MagicMod.FindModFromName("mm_dodge2");
                    dodgeAmount += 2;
                    extraDodge += 2;
                }
                //EquipmentBlock.MakeMagicalFromMod(this, newMod, false, false, true);

                break;
        }

        // Phys resists
        switch (armorType)
        {
            case ArmorTypes.MEDIUM:
            case ArmorTypes.HEAVY:
                float flatOffset = resists[(int)DamageTypes.PHYSICAL].flatOffset;
                float resistMult = resists[(int)DamageTypes.PHYSICAL].multiplier;

                if (armorType == ArmorTypes.MEDIUM)
                {
                    flatOffset += (1 * challengeValue);
                    resistMult -= 0.01f;
                }
                else
                {
                    flatOffset += (2 * challengeValue);
                    resistMult -= 0.023f;
                }

                resists[(int)DamageTypes.PHYSICAL].flatOffset = flatOffset;
                resists[(int)DamageTypes.PHYSICAL].multiplier = resistMult;
                break;
        }
        RebuildDisplayName();
    }

    public override bool ValidateEssentialProperties()
    {
        if (!base.ValidateEssentialProperties())
        {
            return false;
        }

        if (armorType == ArmorTypes.NATURAL && challengeValue >= 1.0f && challengeValue <= MAX_STARTING_CHALLENGE_VALUE)
        {
            Debug.LogError("Armor ref " + actorRefName + " cannot have NATURAL armor type. Must be Light, Medium, or Heavy.");
            return false;
        }

        return true;
    }

    public override bool TryReadFromXml(XmlReader reader)
    {
        if (base.TryReadFromXml(reader))
        {
            return true;
        }
        string txt = "";
        switch (reader.Name)
        {
            case "Defense":
                txt = reader.ReadElementContentAsString();
                defense = CustomAlgorithms.TryParseFloat(txt);
                return true;
            case "ArmorType":
                armorType = (ArmorTypes)Enum.Parse(typeof(ArmorTypes), reader.ReadElementContentAsString());
                return true;
            case "ResistMessage":
                resistMessage = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "ResMsg":
                resistMessage = true;
                reader.Read();
                return true;
        }

        return false;

    }

    public override int GenerateSubtypeAsInt()
    {
        int baseValue = Item.ARMOR_BASE_VALUE;

        baseValue += (int)armorType;

        return baseValue;
    }
}