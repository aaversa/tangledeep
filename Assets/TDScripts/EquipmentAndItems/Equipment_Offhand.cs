using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public partial class Offhand : Equipment
{
    public float blockChance;
    public float blockDamageReduction;
    public bool allowBow;

    public Offhand() : base()
    {
        addAbilities = new List<AbilityScript>();
        slot = EquipmentSlots.OFFHAND;
        itemType = ItemTypes.OFFHAND;
        allowBow = false;
        blockChance = 0;
        blockDamageReduction = 0.65f;
    }

    //returns true if this equipment can be equipped in the offhand slot
    //at all. Does NOT check the current condition of the player equipment. So this will always
    //return true for things like Quivers, even if the player has a 2H sword equipped at the time.
    public override bool IsOffhandable()
    {
        return true;
    }

    public bool IsQuiver()
    {
        return allowBow;
    }

    public bool IsShield()
    {
        return blockChance > 0f;
    }

    public bool IsMagicBook()
    {
        if (!IsQuiver() && !IsShield()) return true;
        return false;
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        bool writeBlockDamage = false;
        bool writeBlockChance = false;

        if (Mathf.Abs(blockDamageReduction - 0.65f) >= 0.001f) // this was <= but that would mean *only* 65% BDR gets saved.....?
        {
            writeBlockDamage = true;            
        }
        if (blockChance != 0)
        {
            writeBlockChance = true;
        }

        if (writeBlockChance && writeBlockDamage)
        {
            writer.WriteElementString("blck", blockDamageReduction + "|" + blockChance);
        }
        else if (writeBlockDamage)
        {
            writer.WriteElementString("bdmg", blockDamageReduction.ToString());
        }
        else if (writeBlockChance)
        {
            writer.WriteElementString("bchance", blockChance.ToString());
        }

        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item offtemplate)
    {
        base.CopyFromItem(offtemplate);
        Offhand template = (Offhand)offtemplate;
        blockChance = template.blockChance;
        blockDamageReduction = template.blockDamageReduction;
        allowBow = template.allowBow;
        CopyFromEquipment(template);
    }

    public override string GetItemWorldUpgrade()
    {
        if (blockChance == 0 && !allowBow) // Book
        {
            switch (timesUpgraded)
            {
                case 0:
                    return StringManager.GetString("item_dream_upgrade_spirit_text_01");
                case 1:
                    return StringManager.GetString("item_dream_upgrade_spirit_text_02");
                case 2:
                    return StringManager.GetString("item_dream_upgrade_spirit_text_03");
                case 3:
                    if (GameStartData.NewGamePlus >= 2)
                    {
                        return StringManager.GetString("item_dream_upgrade_spirit_text_04");
                    }
                    else
                    {
                        return StringManager.GetString("ui_itemworld_noupgrade");
                    }
                    
                default:
                    return StringManager.GetString("ui_itemworld_noupgrade");
            }
        }

        if (allowBow) // quiver
        {
            switch (timesUpgraded)
            {
                case 0:
                    return StringManager.GetString("item_dream_upgrade_critchance_text_01");
                case 1:
                    return StringManager.GetString("item_dream_upgrade_critchance_text_02");
                case 2:
                    return StringManager.GetString("item_dream_upgrade_critchance_text_03");
                case 3:
                    if (GameStartData.NewGamePlus >= 2)
                    {
                        return StringManager.GetString("item_dream_upgrade_critchance_text_04");
                    }
                    else
                    {
                        return StringManager.GetString("ui_itemworld_noupgrade");
                    }
                    
                default:
                    return StringManager.GetString("ui_itemworld_noupgrade");
            }
        }

        if (timesUpgraded > Equipment.GetMaxUpgrades()-1)
        {
            return StringManager.GetString("ui_itemworld_noupgrade");
        }

        StringManager.SetTag(0, (blockChance + 0.03f) * 100f + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
        return StringManager.GetString("item_dream_upgrade_block_chance");
    }

    public override void UpgradeItem(bool debug = false)
    {
        base.UpgradeItem(debug);

        if (allowBow)
        {
            MagicMod template;
            MagicMod newMod;
            MagicMod remover = null;
            // This is probably a magic book, although we're making an assumption here.
            switch (timesUpgraded)
            {
                case 1:
                    template = MagicMod.FindModFromName("mm_crit3");
                    newMod = new MagicMod();
                    newMod.CopyFromMod(template);
                    AddMod(newMod, true);
                    break;
                case 2:
                    RemoveAndAddMod("mm_crit3", "crit3", "mm_crit5");
                    break;
                case 3:
                    RemoveAndAddMod("mm_crit5", "crit5", "mm_crit7");
                    break;
                case 4:
                    RemoveAndAddMod("mm_crit7", "crit7", "mm_crit9");
                    break;
            }
        }
        else if (blockChance == 0)
        {
            MagicMod template;
            MagicMod newMod;
            MagicMod remover = null;
            // This is probably a magic book, although we're making an assumption here.
            switch (timesUpgraded)
            {
                case 1:
                    template = MagicMod.FindModFromName("mm_spiritpowerflat5");
                    newMod = new MagicMod();
                    newMod.CopyFromMod(template);
                    AddMod(newMod, true);
                    break;
                case 2:
                    RemoveAndAddMod("mm_spiritpowerflat5", "spiritpowerflat5", "mm_spiritpowerflat10");
                    break;
                case 3:
                    RemoveAndAddMod("mm_spiritpowerflat10", "spiritpowerflat10", "mm_spiritpowerflat15");
                    break;
                case 4:
                    RemoveAndAddMod("mm_spiritpowerflat15", "spiritpowerflat15", "mm_spiritpowerflat25");
                    break;
            }
        }
        else
        {
            blockChance += 0.03f;
        }
        RebuildDisplayName();
    }

    public override bool TryReadFromXml(XmlReader reader)
    {
        if (base.TryReadFromXml(reader))
        {
            return true;
        }
        string txt = "";
        switch(reader.Name)
        {
            case "BlockChance":
                txt = reader.ReadElementContentAsString();
                blockChance = CustomAlgorithms.TryParseFloat(txt);
                return true;
            case "BlockDamageReduction":
                txt = reader.ReadElementContentAsString();
                blockDamageReduction = CustomAlgorithms.TryParseFloat(txt);
                return true;
            case "Quiver":
                allowBow = true;
                reader.Read();
                return true;
            case "AllowBow":
                allowBow = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
        }

        return false;
    }

    public override int GenerateSubtypeAsInt()
    {
        int baseValue = Item.OFFHAND_BASE_VALUE;
        if (blockChance > 0f)
        {
            baseValue += 10;
        }
        else if (allowBow)
        {
            baseValue += 20;
        }
        else
        {
            baseValue += 30;
        }

        return baseValue;
    }
}