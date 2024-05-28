using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;

// Notes on creating magic mods in XML.
// ModName = Internal mod reference name and what is displayed to the player
// Prefix = Is the mod name added before the item name?
// EquipmentSlot = What slot does this mod affect?
// ChallengeValue = Required challenge value to spawn this mod
// ChangeDurability / ChangeDurabilityAsPercent = Changes the core weapon durability
// ChangePower / ChangePowerAsPercent = Changes the core weapon power
// ModEffect = Begins a mod effect
//      StatusRef = Name of the reference status effect for the mod effect
//      DamageFormula = Used for weapon procs
//      ProcChance = Chance to fire status from 0.0 to 1.0. This should be 1.0 for all but procs
//      Stat = i.e. STAMINA or STRENGTH to boost a stat. Can do RANDOM_RESOURCE, RANDOM_ANY, RANDOM_NONRESOURCE also.
//      StatAmount = How much to boost / reduce stat?
//      StatFlat = If false, boosts/reduces by a percentage.

public class MagicMod
{
    public string modName;
    public string refName;
    public int magicModID;
    public string description;
    public string unbakedDescription;
    public string backupDescription;    
    public bool prefix;
    public bool lucidOrbsOnly;
    public EquipmentSlots slot;
    public float challengeValue;
    public float maxChallengeValue;
    public List<StatusEffect> modEffects;
    public float changeDurability;
    public bool changeDurabilityAsPercent;
    public float changeBlock;
    public float changePower;
    public bool changePowerAsPercent;
    public DamageTypes changeDamageType;
    public bool monsterAllowed;
    public bool noNameChange;
    public bool noDescChange;
    public int exclusionGroup;
    public bool jobAbilityMod;
    public bool[] modFlags;
    public bool bDontAnnounceAddedAbilities;

    /// <summary>
    /// If TRUE, we will always list the mod's description even if it includes a resist (that would normally cause it to be skipped)
    /// </summary>
    public bool forceWriteDesc;

    public float[] adventureStats;

    public List<ResistanceData> resists;
    public List<AbilityScript> addAbilities;

    private List<string> list_StatusEffectRefNamesForLoad;
    private List<string> list_AbilityScriptRefNamesForLoad;

    public List<string> numberTags;

    public static bool WriteSpecialFieldsForSerialization(object source, string strFieldName, StringBuilder sbData)
    {
        MagicMod sourceMod = source as MagicMod;

        switch (strFieldName.ToLowerInvariant())
        {
            case "addabilities":
                foreach (AbilityScript ab in sourceMod.addAbilities)
                {
                    sbData.Append("addabilities|" + ab.refName + "|");
                }
                return true;

            case "modeffects":
                foreach (StatusEffect se in sourceMod.modEffects)
                {
                    sbData.Append("modeffect|" + se.refName + "|");
                }
                return true;

            case "resists":
                if (sourceMod.resists == null ||
                    sourceMod.resists.Count == 0)
                {
                    return true;
                }

                sbData.Append("resists|" + sourceMod.resists.Count + "|");
                foreach (ResistanceData rd in sourceMod.resists)
                {
                    sbData.Append(rd.damType.ToString() + "|");
                    sbData.Append(rd.absorb.ToString() + "|");
                    sbData.Append(rd.flatOffset.ToString() + "|");
                    sbData.Append(rd.multiplier.ToString() + "|");
                }

                return true;
        }


        return false;
    }

    //return value is how much to advance the index by. If it is < 0, that means we didn't catch anything.
    public static int ReadSpecialFieldsForSerialization(object source, string strFieldName, string[] splitValues, int idx)
    {
        MagicMod sourceMod = source as MagicMod;

        switch (strFieldName.ToLowerInvariant())
        {
            case "addabilities":
                if (sourceMod.list_AbilityScriptRefNamesForLoad == null)
                {
                    sourceMod.list_AbilityScriptRefNamesForLoad = new List<string>();
                }
                sourceMod.list_AbilityScriptRefNamesForLoad.Add(splitValues[idx + 1]);
                return 0;

            case "modeffects":
                if (sourceMod.list_StatusEffectRefNamesForLoad == null)
                {
                    sourceMod.list_StatusEffectRefNamesForLoad = new List<string>();
                }
                sourceMod.list_StatusEffectRefNamesForLoad.Add(splitValues[idx + 1]);
                return 0;

            case "resists":
                int iNumAdvanced = 0;
                int iNumModsToRead = Int32.Parse(splitValues[idx + 1]);
                idx += 2;
                sourceMod.resists = new List<ResistanceData>();
                for (int t = 0; t < iNumModsToRead; t++)
                {
                    ResistanceData rd = new ResistanceData();
                    rd.damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), splitValues[idx]);
                    rd.absorb = splitValues[idx + 1].ToLowerInvariant() == "true";
                    rd.flatOffset = CustomAlgorithms.TryParseFloat(splitValues[idx + 2]);
                    rd.multiplier = CustomAlgorithms.TryParseFloat(splitValues[idx + 3]);
                    iNumAdvanced += 4;
                    idx += 4;

                    sourceMod.resists.Add(rd);
                }

                return iNumAdvanced;
        }


        return -1;
    }

    public static void PostLoadFromSerialization(object source)
    {
        MagicMod sourceMod = source as MagicMod;
        if (sourceMod.list_AbilityScriptRefNamesForLoad != null)
        {
            foreach (string s in sourceMod.list_AbilityScriptRefNamesForLoad)
            {
                sourceMod.addAbilities.Add(GameMasterScript.masterAbilityList[s]);
            }
        }

        if (sourceMod.list_StatusEffectRefNamesForLoad != null)
        {
            foreach (string s in sourceMod.list_StatusEffectRefNamesForLoad)
            {
                sourceMod.modEffects.Add(GameMasterScript.masterStatusList[s]);
            }
        }
    }


    public List<string> GetRefsOfSkillsModified()
    {
        List<string> listOfSkillsModified = new List<string>();

        foreach (StatusEffect se in modEffects)
        {
            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.ABILITYCOSTMODIFIER)
                {
                    AbilityModifierEffect ame = eff as AbilityModifierEffect;
                    foreach (string aRef in ame.abilityRefsToModify)
                    {
                        listOfSkillsModified.Add(aRef);
                    }
                }
            }
        }

        return listOfSkillsModified;
    }

    public bool HasNonStackableEffect()
    {
        foreach(StatusEffect se in modEffects)
        {
            if (!se.stackMultipleEffects)
            {
                return true;
            }
        }
        return false;
    }

    public static MagicMod FindModFromName(string name)
    {
        MagicMod outMod;

        if (GameMasterScript.masterMagicModList.TryGetValue(name, out outMod))
        {
            return outMod;
        }
        else
        {
            Debug.Log("Can't find mod: [" + name + "] " + name.Length);
            return null;
        }

    }

    public bool IsSpecialMod()
    {
        if (modFlags[(int)MagicModFlags.CASINO] || modFlags[(int)MagicModFlags.NIGHTMARE])
        {
            return true;
        }
        return false;
    }

    public string GetDescription(Item itm = null, bool showExtraModInfo = true)
    {
        string build = "";
        bool first = false;
        if (!string.IsNullOrEmpty(description))
        {            
            if (itm != null && itm.itemType == ItemTypes.EMBLEM)
            {
                Emblem emb = itm as Emblem;
                StringManager.SetTag(0, CharacterJobData.GetJobDataByEnum((int)emb.jobForEmblem).DisplayName);
                build = String.Copy(description) + " ";
                build = CustomAlgorithms.ParseLiveMergeTags(build);                
            }        
            else
            {
                build += description + " ";
            } 

            first = true;
        }
        else
        {
            foreach (ResistanceData rd in resists)
            {
                if (rd.multiplier != 0f)
                {
                    if (first)
                    {
                        build += "\n";
                        first = false;
                    }
                    float readMult = rd.multiplier * 100f;
                    Debug.Log(readMult);
                    int dMult = (int)readMult;
                    StringManager.SetTag(0, dMult.ToString());
                    StringManager.SetTag(1, CombatManagerScript.verboseDamageTypes[(int)rd.damType]);
                    build += StringManager.GetString("ui_percent_resist") + " ";
                }
                if (rd.flatOffset != 0)
                {
                    if (first)
                    {
                        build += "\n";
                        first = false;
                    }
                    int dOffset = (int)(rd.flatOffset * -1);
                    StringManager.SetTag(0, dOffset.ToString());
                    StringManager.SetTag(1, CombatManagerScript.verboseDamageTypes[(int)rd.damType]);
                    build += StringManager.GetString("ui_flat_resist") + " ";

                }
            }
        }
        
        if (showExtraModInfo)
        {
            if (modFlags[(int)MagicModFlags.MELEE])
            {
                build += "(" + StringManager.GetString("mod_desc_meleeonly") + ") ";
            }
            if (modFlags[(int)MagicModFlags.BOW])
            {
                build += "(" + StringManager.GetString("mod_desc_bowonly") + ") ";
            }
            if (modFlags[(int)MagicModFlags.ONLY2HMELEE])
            {
                build += "(" + StringManager.GetString("mod_desc_2hmeleeonly") + ") ";
            }
            if (modFlags[(int)MagicModFlags.QUIVER])
            {
                build += "(" + StringManager.GetString("mod_desc_quiveronly") + ") ";
            }
            if (modFlags[(int)MagicModFlags.SHIELD])
            {
                build += "(" + StringManager.GetString("mod_desc_shieldonly") + ") ";
            }
            if (modFlags[(int)MagicModFlags.BOOK])
            {
                build += "(" + StringManager.GetString("mod_desc_bookonly") + ") ";
            }
        }     

        return build;

    }

    public void CopyFromMod(MagicMod template)
    {
        magicModID = template.magicModID;
        backupDescription = template.backupDescription;
        lucidOrbsOnly = template.lucidOrbsOnly;
        changeBlock = template.changeBlock;
        noNameChange = template.noNameChange;
        noDescChange = template.noDescChange;
        forceWriteDesc = template.forceWriteDesc;
        refName = template.refName;
        monsterAllowed = template.monsterAllowed;
        description = template.description;
        modName = template.modName;
        prefix = template.prefix;
        slot = template.slot;
        challengeValue = template.challengeValue;
        maxChallengeValue = template.maxChallengeValue;
        changeDurability = template.changeDurability;
        changePower = template.changePower;
        changeDurabilityAsPercent = template.changeDurabilityAsPercent;
        changePowerAsPercent = template.changePowerAsPercent;
        changeDamageType = template.changeDamageType;
        exclusionGroup = template.exclusionGroup;
        jobAbilityMod = template.jobAbilityMod;
        bDontAnnounceAddedAbilities = template.bDontAnnounceAddedAbilities;

        for (int i = 0; i < (int)AdventureStats.COUNT; i++)
        {
            adventureStats[i] = template.adventureStats[i];
        }

        for (int i = 0; i < (int)MagicModFlags.COUNT; i++)
        {
            modFlags[i] = template.modFlags[i];
        }

        foreach (ResistanceData rd in template.resists)
        {
            ResistanceData rd2 = new ResistanceData();
            rd2.damType = rd.damType;
            rd2.flatOffset = rd.flatOffset;
            rd2.absorb = rd.absorb;
            rd2.multiplier = rd.multiplier;
            resists.Add(rd2);
        }

        foreach (StatusEffect se in template.modEffects)
        {
            StatusEffect nEffect = new StatusEffect();
            // At the time of instantiation of this new mod, we need to check any random elements and decide them.
            nEffect.CopyStatusFromTemplate(se);
            modEffects.Add(nEffect);
        }

        foreach (AbilityScript abil in template.addAbilities)
        {
            addAbilities.Add(abil);
        }
    }

    public MagicMod()
    {
        modName = "";
        description = "";
        unbakedDescription = "";
        backupDescription = "";
        maxChallengeValue = 999f;
        modEffects = new List<StatusEffect>();
        changeDamageType = DamageTypes.PHYSICAL;
        resists = new List<ResistanceData>();
        modFlags = new bool[(int)MagicModFlags.COUNT];
        addAbilities = new List<AbilityScript>();
        adventureStats = new float[(int)AdventureStats.COUNT];
        numberTags = new List<string>();
    }

    public void ParseNumberTags()
    {
        unbakedDescription = description;
        if (!numberTags.Any()) return;
        for (int i = 0; i < numberTags.Count; i++)
        {
            string parsedVer = numberTags[i];
            if (StringManager.gameLanguage == EGameLanguage.de_germany)
            {
                parsedVer = parsedVer.Replace("%", " %");
            }
            description = description.Replace("^number" + (i + 1) + "^", "<color=yellow>" + parsedVer + "</color>");
            backupDescription = backupDescription.Replace("^number" + (i + 1) + "^", "<color=yellow>" + parsedVer +"</color>");
        }
    }
}
