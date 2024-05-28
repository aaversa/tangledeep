using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public class Emblem : Equipment
{
    public CharacterJobs jobForEmblem;
    public int emblemLevel;
    public Dictionary<int, string> grantedStatusEffects;

    public Emblem()
    {
        slot = EquipmentSlots.EMBLEM;
        jobForEmblem = CharacterJobs.COUNT;
        emblemLevel = 0;
        grantedStatusEffects = new Dictionary<int, string>();
        addAbilities = new List<AbilityScript>();
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        writer.WriteElementString("jobforemblem", ((int)jobForEmblem).ToString());
        writer.WriteElementString("emblemlevel", emblemLevel.ToString());

        if (grantedStatusEffects.Keys.Count > 0)
        {
            writer.WriteStartElement("emblemstatuseffects");
            foreach (int key in grantedStatusEffects.Keys) // each key is a tier for the emblem
            {
                writer.WriteElementString("tier_" + key, grantedStatusEffects[key]); // i.e. tier_0, brigand_dagger_damageup
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item iTemplate)
    {
        base.CopyFromItem(iTemplate);
        Emblem template = iTemplate as Emblem;
        CopyFromEquipment(template);
        jobForEmblem = template.jobForEmblem;
        emblemLevel = template.emblemLevel;
        foreach(int key in template.grantedStatusEffects.Keys)
        {
            grantedStatusEffects.Add(key, template.grantedStatusEffects[key]);
        }
    }

    public void VerifyEmblemHasStatMod()
    {
        switch (emblemLevel)
        {
            case 0:
                RebuildDisplayName();
                if (!HasModByRef("mm_emblemwellrounded1"))
                {
                    EquipmentBlock.MakeMagicalFromMod(this, GameMasterScript.masterMagicModList["mm_emblemwellrounded1"], true, false, false);
                }
                
                break;
            case 1:
                if (HasModByRef("mm_emblemwellrounded1"))
                {
                    RemoveMod("mm_emblemwellrounded1");
                }
                
                RebuildDisplayName();
                if (!HasModByRef("mm_emblemwellrounded2"))
                {
                    EquipmentBlock.MakeMagicalFromMod(this, GameMasterScript.masterMagicModList["mm_emblemwellrounded2"], true, false, false);
                }
                
                break;
            case 2:
                if (HasModByRef("mm_emblemwellrounded2"))
                {
                    RemoveMod("mm_emblemwellrounded2");
                }
                if (HasModByRef("mm_emblemwellrounded1"))
                {
                    RemoveMod("mm_emblemwellrounded1");
                }
                RebuildDisplayName();
                if (!HasModByRef("mm_emblemwellrounded3"))
                {
                    EquipmentBlock.MakeMagicalFromMod(this, GameMasterScript.masterMagicModList["mm_emblemwellrounded3"], true, false, false);
                }
                
                break;
        }
    }

    public void IncreaseEmblemLevel()
    {
        if (emblemLevel >= 2) return; // Can't increase past tier 3, 0-based.

        emblemLevel++;

        switch(emblemLevel)
        {
            case 1:
                actorRefName = "emblem_jobtrial2";
                spriteRef = "assorteditems_341";
                rarity = Rarity.ARTIFACT;
                challengeValue = 1.4f;
                RemoveMod("mm_emblemwellrounded1");
                description = GameMasterScript.masterItemList[actorRefName].description;
                RebuildDisplayName();                
                EquipmentBlock.MakeMagicalFromMod(this, GameMasterScript.masterMagicModList["mm_emblemwellrounded2"], true, false, false);
                break;
            case 2:
                actorRefName = "emblem_jobtrial3";
                spriteRef = "assorteditems_342";
                rarity = Rarity.LEGENDARY;
                legendary = true;
                challengeValue = 1.8f;
                RemoveMod("mm_emblemwellrounded2");
                RebuildDisplayName();
                description = GameMasterScript.masterItemList[actorRefName].description;
                EquipmentBlock.MakeMagicalFromMod(this, GameMasterScript.masterMagicModList["mm_emblemwellrounded3"], true, false, false);
                break;
        }

        extraDescription = GameMasterScript.masterItemList[actorRefName].extraDescription;

        RebuildDisplayName();
    }

    public int GetNextTrialCost()
    {
        if (emblemLevel >= JobTrialScript.TRIAL_COSTS.Length-1)
        {
            return JobTrialScript.TRIAL_COSTS[JobTrialScript.TRIAL_COSTS.Length-1];
        }
        return JobTrialScript.TRIAL_COSTS[emblemLevel+1];
    }

    /* public override string GetItemWorldUpgrade()
    {
        switch (timesUpgraded)
        {
            case 0:
                return "New mod: +5 STAMINA / +5 ENERGY";
            case 1:
                return "Total bonus: +10 STAMINA / +10 ENERGY";
            case 2:
                return "Total bonus: +15 STAMINA / +15 ENERGY";
            default:
                return StringManager.GetString("ui_itemworld_noupgrade");
        }
    } */

    /* public override void UpgradeItem()
    {
        base.UpgradeItem();

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

                foreach (MagicMod mm in mods)
                {
                    if (mm.refName == "mm_upgradeaccessory1")
                    {
                        remover = mm;
                    }
                }

                mods.Remove(remover);
                if (collection != null)
                {
                    if ((collection.owner != null) && (collection.owner.GetActorType() == ActorTypes.HERO))
                    {
                        Fighter ft = collection.owner as Fighter;
                        ft.myStats.RemoveStatusByRef("upgradeaccessory1");
                    }
                }

                template = MagicMod.FindModFromName("mm_upgradeaccessory2");
                newMod = new MagicMod();
                newMod.CopyFromMod(template);
                AddMod(newMod, true);
                break;
            case 3:
                foreach (MagicMod mm in mods)
                {
                    if (mm.refName == "mm_upgradeaccessory2")
                    {
                        remover = mm;
                    }
                }

                mods.Remove(remover);
                if (collection != null)
                {
                    if ((collection.owner != null) && (collection.owner.GetActorType() == ActorTypes.HERO))
                    {
                        Fighter ft = collection.owner as Fighter;
                        ft.myStats.RemoveStatusByRef("upgradeaccessory2");
                    }
                }

                template = MagicMod.FindModFromName("mm_upgradeaccessory3");
                newMod = new MagicMod();
                newMod.CopyFromMod(template);
                AddMod(newMod, true);
                break;
        }

        RebuildDisplayName();
    } */

    public override int GenerateSubtypeAsInt()
    {
        return Item.EMBLEM_BASE_VALUE;
    }
}