using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public partial class Accessory : Equipment
{
    public bool uniqueEquip;

    public Accessory() : base()
    {
        addAbilities = new List<AbilityScript>();
        slot = EquipmentSlots.ACCESSORY;
        itemType = ItemTypes.ACCESSORY;
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item iTemplate)
    {
        base.CopyFromItem(iTemplate);
        Accessory template = (Accessory)iTemplate as Accessory;
        CopyFromEquipment(template);
        uniqueEquip = template.uniqueEquip;
        /* foreach (AbilityScript abil in template.addAbilities)
        {
            addAbilities.Add(abil);
        } */
    }

    public override string GetItemWorldUpgrade()
    {
        // ng++ todo
        switch (timesUpgraded)
        {
            case 0:
                return StringManager.GetString("item_dream_upgradetext_01");
            case 1:
                return StringManager.GetString("item_dream_upgradetext_02");
            case 2:
                return StringManager.GetString("item_dream_upgradetext_03");
            case 3:
                if (GameStartData.NewGamePlus >= 2)
                {
                    return StringManager.GetString("item_dream_upgradetext_04");
                }
                else
                {
                    return StringManager.GetString("ui_itemworld_noupgrade");
                }                
            default:
                return StringManager.GetString("ui_itemworld_noupgrade");
        }
    }

    public override void UpgradeItem(bool debug = false)
    {
        base.UpgradeItem(debug);

        MagicMod template;
        MagicMod newMod;
        MagicMod remover = null;


        switch (timesUpgraded)
        {
            case 1:
                template = MagicMod.FindModFromName("mm_upgradeaccessory1");
                newMod = new MagicMod();
                newMod.CopyFromMod(template);
                AddMod(newMod, true);
                break;
            case 2:
                newMod = RemoveAndAddMod("mm_upgradeaccessory1", "upgradeaccessory1", "mm_upgradeaccessory2");
                break;
            case 3:
                newMod = RemoveAndAddMod("mm_upgradeaccessory2", "upgradeaccessory2", "mm_upgradeaccessory3");
                break;
            case 4:
                newMod = RemoveAndAddMod("mm_upgradeaccessory3", "upgradeaccessory3", "mm_upgradeaccessory4");
                break;
        }

        RebuildDisplayName();
    }

    public override bool TryReadFromXml(XmlReader reader)
    {
        if (base.TryReadFromXml(reader))
        {
            return true;
        }

        switch (reader.Name)
        {
            case "UniqueEquip":
            case "UniqueItem":
                uniqueEquip = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "Uniq":
                uniqueEquip = true;
                reader.Read();
                return true;
        }

        return false;
    }

    public override int GenerateSubtypeAsInt()
    {
        return Item.ACCESSORY_BASE_VALUE;
    }
}