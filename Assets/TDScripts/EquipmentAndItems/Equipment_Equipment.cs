using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

[System.Serializable]
[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public partial class Equipment : Item
{
    public EquipmentSlots slot;
    public List<ResistanceData> resists;
    public bool hasBeenUnequipped;
    public List<AbilityScript> addAbilities;
    public bool bDontAnnounceAddedAbilities;
    public GearSet gearSet;
    public bool[] eqFlags;
    public string gearSetRef; // Used during loading only.
    public List<EQPair> pairedItems;
    public float[] adventureStats;
    public const int MAX_UPGRADES_BY_RARITY = 5;

    public static Dictionary<AbilityScript, bool> abilitiesFromItem = new Dictionary<AbilityScript, bool>();

    public static StringBuilder eqSB;

    public static HashSet<string> modsThatCountAsAutoMods = new HashSet<string>()
    {
        "mm_statboost1","mm_statboost2","mm_statboost3","mm_statboost4","mm_statboost5",
        "mm_rangeddamage4","mm_rangeddamage8","mm_rangeddamage12","mm_rangeddamage16","mm_rangeddamage20","mm_rangeddamage24","mm_rangeddamage28",
        "mm_upgradeaccessory1","mm_upgradeaccessory2","mm_upgradeaccessory3",
        "mm_elemdamageboost3","mm_elemdamageboost6","mm_elemdamageboost9","mm_elemdamageboost12","mm_elemdamageboost15","mm_elemdamageboost18"
    };

    public static HashSet<string> inherentOffhandMods = new HashSet<string>()
    {
        "mm_quiver1", "mm_quiver2", "mm_quiver3", "mm_quiver4",
        "mm_magicbook1", "mm_magicbook2","mm_magicbook3","mm_magicbook4","mm_magicbook5"
    };

    public Equipment()
    {
        resists = new List<ResistanceData>();
        eqFlags = new bool[(int)EquipmentFlags.COUNT];
        pairedItems = new List<EQPair>();
        adventureStats = new float[(int)AdventureStats.COUNT];
    }

    public bool IsAutoMod(MagicMod mm)
    {
        if (autoModRef == null) return false;
        if (autoModRef.Contains(mm.refName)) return true;
        return false;
    }

    public void AddResistanceFromData(ResistanceData rd)
    {
        foreach(ResistanceData myRD in resists)
        {
            if (myRD.damType == rd.damType)
            {
                ModifyResistMult(rd.damType, rd.multiplier);
                ModifyResistOffset(rd.damType, rd.flatOffset);
                if (rd.absorb) SetResistAbsorb(rd.damType, rd.absorb);
            }
        }
    }

    public void WriteEQMods(XmlWriter writer)
    {
        if (mods == null || mods.Count == 0) return;
        string refNameBuild = "";
        string idBuild = "";

        bool firstRefName = true;
        bool firstID = true;

        for (int i = 0; i < mods.Count; i++)
        {
            MagicMod mm = mods[i];
            if (!GameMasterScript.masterMagicModList.ContainsKey(mm.refName))
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " trying to save a mod called " + mm.refName + " which doesn't seem to exist, skipping.");
                continue;
            }

            if (mm.magicModID == 0) // Save mod as the string ref (not ideal)
            {
                if (Debug.isDebugBuild) Debug.Log(mm.refName + " has a blank ID!");
                if (!firstRefName) refNameBuild += "|";
                refNameBuild += mm.refName;
                firstRefName = false;
            }
            else // Save mod as an int (ideal!)
            {
                if (!firstID) idBuild += "|";
                idBuild += mm.magicModID;
                firstID = false;
            }

            /* if (i == mods.Count - 1)
            {
                refNameBuild += mm.refName;
            }
            else
            {
                refNameBuild += mm.refName + "|";
            } */
        }

        if (!string.IsNullOrEmpty(refNameBuild)) writer.WriteElementString("mds", refNameBuild);
        if (!string.IsNullOrEmpty(idBuild)) writer.WriteElementString("mids", idBuild);
    }

    static List<string> modsToAdd;

    public bool ReadEQModsFromIDs(XmlReader reader)
    {
        if (modsToAdd == null) modsToAdd = new List<string>();
        modsToAdd.Clear();

        string modList = reader.ReadElementContentAsString();
        string[] parsed = modList.Split('|');

        for (int i = 0; i < parsed.Length; i++)
        {
            int modID = 0;
            if (!int.TryParse(parsed[i], out modID))
            {
                Debug.Log("Could not parse magic mod id " + modID + " from " + parsed[i]);
                continue;
            }

            MagicMod findMod = null;
            if (!GameMasterScript.dictMagicModIDs.TryGetValue(modID, out findMod))
            {
                Debug.Log("Could not find mod by ID: " + modID);
                continue;
            }

            if (autoModRef != null)
            {
                if (autoModRef.Contains(findMod.refName) && !customItemFromGenerator)
                {
                    continue;
                }
            }
            modsToAdd.Add(findMod.refName);
        }

        AddModsFromStringList(modsToAdd);

        return true;
    }

    public bool ReadEQMods(XmlReader reader)
    {
        if (modsToAdd == null) modsToAdd = new List<string>();
        modsToAdd.Clear();

        string modList = reader.ReadElementContentAsString();
        string[] parsed = modList.Split('|');
        
        for (int i = 0; i < parsed.Length; i++)
        {
            string modRef = parsed[i];
            if (autoModRef != null)
            {
                if (autoModRef.Contains(modRef) && !customItemFromGenerator)
                {
                    continue;
                }
            }
            modsToAdd.Add(modRef);
        }

        AddModsFromStringList(modsToAdd);

        return true;
    }

    void AddModsFromStringList(List<String> addList)
    {
        foreach (string modRef in addList)
        {
            MagicMod mmTemplate = MagicMod.FindModFromName(modRef);
            if (mmTemplate == null)
            {
                Debug.Log("Reader couldn't find mod template " + modRef);
            }
            else
            {

                MagicMod mm = new MagicMod();
                mm.CopyFromMod(mmTemplate);
                AddMod(mm, false);
            }

        }
    }

    public int GetMaxMagicMods()
    {
        int localMax = GameMasterScript.MAX_ITEM_MODS;
        if (itemType == ItemTypes.WEAPON)
        {
            Weapon w = this as Weapon;
            if (w.twoHanded)
            {
                localMax = GameMasterScript.MAX_2H_ITEM_MODS;
            }
            else
            {
                localMax = GameMasterScript.MAX_1H_ITEM_MODS;
            }

            int mmods = w.ReadActorData("maxmods");
            if (mmods > 0)
            {
                localMax = mmods;
            }
        }

        return localMax;
    }

    public Equipment GetPairedItem()
    {
        if (pairedItems.Count == 0) return null;
        return pairedItems[0].eq;
    }

    public void LinkAllPairedItems()
    {
        List<EQPair> remover = new List<EQPair>();
        foreach (EQPair pair in pairedItems)
        {
            if (pair == null) // 232019 - could this ever be null? hopefully not
            {
                remover.Add(pair);
                continue;
            }
            if (pair.itemID > 0)
            {
                Actor findAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(pair.itemID);
                if (findAct != null)
                {
                    //Debug.Log("We have paired me " + actorRefName + " " + actorUniqueID + " with " + findAct.actorRefName + " " + findAct.actorUniqueID);
                    pair.eq = findAct as Equipment;

                    if (pair.eq == null)
                    {
#if UNITY_EDITOR
                        Debug.LogError(pair.eq + " is not equipment for some reason?");
#endif
                        remover.Add(pair);
                        continue;
                    }

                    if (!pair.eq.CheckIfPairedWithSpecificItem(this))
                    {
                        pair.eq.PairWithItem(this, !pair.isMainhandItem, false);
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogError("Could not find the actor " + pair.itemID + " to pair with me, " + actorRefName + " " + actorUniqueID);
#endif
                    remover.Add(pair);
                }
            }
            else
            {
                remover.Add(pair);
            }
        }

        foreach (EQPair pair in remover)
        {
            pairedItems.Remove(pair);
        }
    }

    public string CreateAdventureStatsString()
    {
        string advStatString = "";

        bool allStatsZero = true;

        for (int i = 0; i < (int)AdventureStats.COUNT; i++)
        {
            if (adventureStats[i] != 0)
            {
                allStatsZero = false;
                break;
            }
        }

        if (allStatsZero)
        {
            return "";
        }

        for (int i = 0; i < (int)AdventureStats.COUNT; i++)
        {
            if (i < (int)AdventureStats.COUNT - 1)
            {
                advStatString += adventureStats[i] + "|";
            }
            else
            {
                advStatString += adventureStats[i].ToString();
            }
        }
        return advStatString;
    }

    public bool CanHandleFreeSkillOrb()
    {
        int max = GetMaxMagicMods();
        if (GetNonAutomodCount() >= max)
        {
            foreach(MagicMod mm in mods)
            {
                if (mm.lucidOrbsOnly && mm.jobAbilityMod) // Already have at least one skill orb? Can't handle a freebie.
                {
                    return false;
                }
            }
            return true; // Must not have a skill orb. Can handle a freebie, even at max mods.
        }
        return true;
    }

    // HandleAdditional Handlemagic
    public bool CanHandleMoreMagicMods()
    {
        int max = GetMaxMagicMods();
        if (GetNonAutomodCount() >= max)
        {
            return false;
        }
        return true;
    }
    public MagicModCompatibility IsModValidForMe(MagicMod mm)
    {
        bool plainOrb = mm == null;

        bool canHandleFreeMod = false;        
        if (!plainOrb && CanHandleFreeSkillOrb() && mm.lucidOrbsOnly && mm.jobAbilityMod)
        {
            canHandleFreeMod = true;
        }

        if (!CanHandleMoreMagicMods() && !canHandleFreeMod)
        {
            return MagicModCompatibility.NO_MORE_MODS_POSSIBLE;
        }

        if (plainOrb)
        {
            return MagicModCompatibility.POSSIBLE;
        }

        if (mm.slot != slot && mm.slot != EquipmentSlots.ANY)
        {
            return MagicModCompatibility.WRONG_ITEM_TYPE;
        }

        if (itemType == ItemTypes.WEAPON)
        {
            Weapon wp = this as Weapon;

            if (mm.modFlags[(int)MagicModFlags.MELEE] && wp.isRanged)
            {
                return MagicModCompatibility.WRONG_ITEM_TYPE;
            }
            if (mm.modFlags[(int)MagicModFlags.BOW] && (!wp.isRanged || wp.range < 3))
            {
                return MagicModCompatibility.WRONG_ITEM_TYPE;
            }

            if (mm.modFlags[(int)MagicModFlags.ONLY2HMELEE] && (wp.isRanged || !wp.twoHanded))
            {
                return MagicModCompatibility.WRONG_ITEM_TYPE;
            }

            if (mm.refName == "mm_lightweight" && (wp.twoHanded || wp.weaponType == WeaponTypes.CLAW))
            {
                return MagicModCompatibility.WRONG_ITEM_TYPE;
            }

        }


        if (itemType == ItemTypes.OFFHAND)
        {
            Offhand oh = this as Offhand;
            if (oh.allowBow)
            {
                // Quiver
                if (((mm.modFlags[(int)MagicModFlags.BOOK]) || (mm.modFlags[(int)MagicModFlags.SHIELD])) && (!mm.modFlags[(int)MagicModFlags.QUIVER]))
                {
                    return MagicModCompatibility.WRONG_ITEM_TYPE;
                }
            }
            else if (oh.blockChance == 0)
            {
                // Book
                if (((mm.modFlags[(int)MagicModFlags.QUIVER]) || (mm.modFlags[(int)MagicModFlags.SHIELD])) && (!mm.modFlags[(int)MagicModFlags.BOOK]))
                {
                    return MagicModCompatibility.WRONG_ITEM_TYPE;
                }
            }
            else
            {
                // Shield
                if (((mm.modFlags[(int)MagicModFlags.QUIVER]) || (mm.modFlags[(int)MagicModFlags.BOOK])) && (!mm.modFlags[(int)MagicModFlags.SHIELD]))
                {
                    return MagicModCompatibility.WRONG_ITEM_TYPE;
                }
            }
        }

        if ((mods == null) || (mods.Count == 0)) return MagicModCompatibility.POSSIBLE;

        List<string> abilitiesAlreadyModified = new List<string>();

        StatusEffect checkSE;

        if (mm.modEffects != null)
        {
            for (int i = 0; i < mm.modEffects.Count; i++)
            {
                checkSE = mm.modEffects[i];
                foreach (EffectScript eff in checkSE.listEffectScripts)
                {
                    if (eff.effectType != EffectType.ABILITYCOSTMODIFIER) continue;
                    AbilityModifierEffect ame = eff as AbilityModifierEffect;
                    if (string.IsNullOrEmpty(ame.strRemapAbilitiesToThisRef)) continue;
                    foreach (string modStr in ame.abilityRefsToModify)
                    {
                        abilitiesAlreadyModified.Add(modStr);
                    }
                }
            }
        }

        for (int i = 0; i < mods.Count; i++)
        {
            if (mm.refName == mods[i].refName)
            {
                // Cannot have two mods of same ref, right?
                return MagicModCompatibility.ALREADY_HAS_MOD;
            }
            MagicMod existing = mods[i];
            if ((existing.exclusionGroup == mm.exclusionGroup) && (existing.exclusionGroup > 0))
            {
                return MagicModCompatibility.CONFLICTING_MOD;
            }

            // Make sure the new magic mod does not alter the same ability as any existing mods

            if (existing.modEffects != null)
            {
                foreach (StatusEffect se in existing.modEffects)
                {
                    foreach (EffectScript eff in se.listEffectScripts)
                    {
                        if (eff.effectType != EffectType.ABILITYCOSTMODIFIER) continue;
                        AbilityModifierEffect ame = eff as AbilityModifierEffect;
                        if (string.IsNullOrEmpty(ame.strRemapAbilitiesToThisRef)) continue;

                        foreach (string aRef in ame.abilityRefsToModify)
                        {
                            if (abilitiesAlreadyModified.Contains(aRef))
                            {
                                return MagicModCompatibility.CONFLICTING_MOD;
                            }
                        }
                    }
                }
            }
        }

        return MagicModCompatibility.POSSIBLE;
    }

    public string GetMagicModDescriptionText()
    {
        string construct = "";
        float totalDodge = 0f;
        if (this.itemType == ItemTypes.ARMOR)
        {
            Armor ar = this as Armor;
            totalDodge = ar.extraDodge;
        }

        foreach (MagicMod mm in mods)
        {
            if (!mm.noDescChange && !string.IsNullOrEmpty(mm.description))
            {
                bool skip = false;
                if (mm.modEffects != null)
                {
                    foreach (StatusEffect se in mm.modEffects)
                    {
                        if (se.refName.Contains("status_mmdodge"))
                        {
                            int addDodge;
                            if (Int32.TryParse(se.refName.Substring(14), out addDodge))
                            {
                                totalDodge += addDodge;
                                skip = true;
                            }
                        }
                    }
                }

                if (mm.resists.Count > 0 && !mm.forceWriteDesc)
                {
                    continue; // this should be handled by the gear resist list, no need to list description again
                }

                if (skip) continue;

                string colorToUse = UIManagerScript.cyanHexColor;

                if (IsAutoMod(mm))
                {
                    colorToUse = UIManagerScript.goldHexColor;
                }

                string nonStackableText = "";

                if (mm.HasNonStackableEffect())
                {
                    nonStackableText = " " + StringManager.GetString("misc_nonstackable");
                }

                if (itemType == ItemTypes.EMBLEM)
                {
                    Emblem emb = this as Emblem;
                    StringManager.SetTag(0, CharacterJobData.GetJobDataByEnum((int)emb.jobForEmblem).DisplayName);
                    string ttext = String.Copy(mm.description);
                    string parsedText = CustomAlgorithms.ParseLiveMergeTags(ttext);
                    construct += colorToUse + parsedText + "</color>" + nonStackableText + "\n";
                }
                else
                {
                    construct += colorToUse + mm.GetDescription(null, showExtraModInfo:false) + "</color>" + nonStackableText + "\n";
                }
                
            }
        }

        if (totalDodge > 0)
        {
            StringManager.SetTag(0, totalDodge.ToString());
            construct += UIManagerScript.greenHexColor + "+" + StringManager.GetString("ui_dodge_chance_percent") +"</color>\n";
        }

        return construct;
    }

    public bool CheckIfPairedWithSpecificItem(Equipment eq)
    {
        foreach (EQPair pairedItem in pairedItems)
        {
            if (pairedItem.eq == eq)
            {
                return true;
            }
        }
        return false;
    }
    public void PairWithItem(Equipment eq, bool isMainHand, bool reciprocate)
    {
        if (eq == null)
        {
			return;
        }

        //Debug.Log("Try pair " + actorUniqueID + " with " + eq.actorUniqueID);
        foreach (EQPair pairedItem in pairedItems)
        {
            if (pairedItem.itemID == eq.actorUniqueID)
            {
                pairedItem.eq = eq;
                //Debug.Log(eq.actorUniqueID + " " + eq.actorRefName + " is already paired with this item, " + actorRefName + " " + actorUniqueID);
                return;
            }
        }

        if (isMainHand)
        {
            // Main hands can only have one OH pair.
            RemoveAllPairedItem();
        }

        EQPair pair = new EQPair(isMainHand);
        pair.eq = eq;
        pair.itemID = eq.actorUniqueID;
        pairedItems.Add(pair);

        if (reciprocate)
        {
            eq.PairWithItem(this, !isMainHand, false);
        }

        favorite = true;
        eq.favorite = true;
        vendorTrash = false;
        eq.vendorTrash = false;
    }

    public void RemoveAllPairedItem()
    {
        List<EQPair> pairsToRemove = new List<EQPair>();
        foreach (EQPair pair in pairedItems)
        {
            pairsToRemove.Add(pair);
        }

        foreach (EQPair pair in pairsToRemove)
        {
            RemovePairedItem(pair.itemID);
            if (pair.eq != null)
            {
                pair.eq.RemovePairedItem(actorUniqueID);
            }
        }

    }

    public void RemovePairedItemByRef(Equipment eq)
    {
        int idToRemove = 0;
        foreach (EQPair pair in pairedItems)
        {
            if (pair.eq == eq)
            {
                idToRemove = pair.itemID;
                break;
            }
        }
        if (idToRemove > 0)
        {
            RemovePairedItem(idToRemove);
        }
    }

    public void RemovePairedItem(int id)
    {
        EQPair remover = null;
        foreach (EQPair pair in pairedItems)
        {
            if (pair.itemID == id)
            {
                remover = pair;
                break;
            }
        }
        if (remover != null)
        {
            pairedItems.Remove(remover);
        }
    }

    public bool ValidForModRemoval()
    {
        int removableMods = 0;
        for (int i = 0; i < mods.Count; i++)
        {
            if (mods[i].noNameChange) continue;
            if (autoModRef != null)
            {
                if (autoModRef.Contains(mods[i].refName)) continue;
            }
            removableMods++;
        }

        if (removableMods == 0)
        {
            return false;
        }
        return true;
    }

    public ItemWorldMetaData GetItemWorldProperties()
    {
        ItemWorldMetaData returnData = new ItemWorldMetaData();

        bool[] returnProperties = new bool[(int)ItemWorldProperties.COUNT];

        returnData.properties = returnProperties;

        bool[] elementalAffinity = new bool[(int)DamageTypes.COUNT];

        foreach (ResistanceData rd in resists)
        {
            if (rd.multiplier != 1.0f || rd.absorb || Mathf.Abs(rd.flatOffset) >= 10f)
            {
                elementalAffinity[(int)rd.damType] = true;
            }
        }

        string uid = actorUniqueID.ToString();
        char lastChar;
        char secondToLastChar;
        char thirdChar;
        
        if (actorUniqueID >= 10)
        {
            if (Char.TryParse(uid.Substring(uid.Length - 2, 1), out secondToLastChar))
            {
                switch (secondToLastChar)
                {
                    case '0':
                    case '1':
                    case '2':
                        // Some kind of monster family affinity
                        // How to determine affinity...? Uhh let's do some random math!
                        int numToUse = 0;
                        if (Char.TryParse(uid.Substring(2, 1), out thirdChar))
                        {
                            numToUse = thirdChar;
                        }
                        switch (itemType)
                        {
                            case ItemTypes.ARMOR:
                            case ItemTypes.WEAPON:
                                numToUse += 1;
                                break;
                            case ItemTypes.ACCESSORY:
                                numToUse += 2;
                                break;
                            case ItemTypes.OFFHAND:
                                numToUse += 3;
                                break;
                            case ItemTypes.EMBLEM:
                                numToUse += 4;
                                break;
                        }
                        numToUse *= (int)(challengeValue * 111);
                        if (mods != null && mods.Count >= 1) numToUse *= mods.Count;

                        numToUse = numToUse % 10;

                        switch (numToUse)
                        {
                            case 0:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_JELLIES] = true;
                                break;
                            case 1:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_INSECTS] = true;
                                break;
                            case 2:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_FROGS] = true;
                                break;
                            case 3:
                            case 4:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_BANDITS] = true;
                                break;
                            case 5:
                            case 6:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_BEASTS] = true;
                                break;
                            case 7:
                            case 8:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_HYBRIDS] = true;
                                break;
                            case 9:
                                returnData.properties[(int)ItemWorldProperties.TYPE_FAMILY_ROBOTS] = true;
                                break;
                        }
                        
                        break;
                }
            }
        }


        if (Char.TryParse(uid.Substring(uid.Length - 1), out lastChar))
        {
            switch (lastChar)
            {
                case '0':
                    returnData.properties[(int)ItemWorldProperties.TYPE_DENSE] = true;
                    break;
                case '8':
                    returnData.properties[(int)ItemWorldProperties.TYPE_MELEEBOOST] = true;
                    break;
                case '9':
                    if (!legendary)
                    {
                        returnData.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS] = true;
                    }
                    break;
            }
        }

        foreach (MagicMod mm in mods)
        {
            if (mm.refName == "mm_challenges")
            {
                returnData.properties[(int)ItemWorldProperties.TYPE_MORECHAMPIONS] = true;
                returnData.properties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS] = false;
            }
            if (mm.changeDamageType != DamageTypes.PHYSICAL)
            {
                elementalAffinity[(int)mm.changeDamageType] = true;
            }
            if (mm.modFlags[(int)MagicModFlags.HEALTH])
            {
                returnProperties[(int)ItemWorldProperties.TYPE_HEALTH] = true;
            }
            if (mm.modFlags[(int)MagicModFlags.LUCKY])
            {
                returnProperties[(int)ItemWorldProperties.TYPE_GILDED] = true;
            }
            if (mm.modFlags[(int)MagicModFlags.CRITICAL])
            {
                returnProperties[(int)ItemWorldProperties.TYPE_CRITICAL] = true;
            }
            foreach (StatusEffect eff in mm.modEffects)
            {
                foreach (EffectScript eff2 in eff.listEffectScripts)
                {
                    if (eff2.effectType == EffectType.ALTERBATTLEDATA)
                    {
                        AlterBattleDataEffect abde = eff2 as AlterBattleDataEffect;
                        if ((abde.changeLightningDamage > 0.0f) || (abde.changeLightningResist > 0.0f))
                        {
                            elementalAffinity[(int)DamageTypes.LIGHTNING] = true;
                        }
                        if ((abde.changeFireDamage > 0.0f) || (abde.changeFireResist > 0.0f))
                        {
                            elementalAffinity[(int)DamageTypes.FIRE] = true;
                        }
                        if ((abde.changeWaterDamage > 0.0f) || (abde.changeWaterResist > 0.0f))
                        {
                            elementalAffinity[(int)DamageTypes.WATER] = true;
                        }
                        if ((abde.changeShadowDamage > 0.0f) || (abde.changeShadowResist > 0.0f))
                        {
                            elementalAffinity[(int)DamageTypes.SHADOW] = true;
                        }
                        if ((abde.changePoisonDamage > 0.0f) || (abde.changePoisonResist > 0.0f))
                        {
                            elementalAffinity[(int)DamageTypes.POISON] = true;
                        }
                    }
                }
            }
        }

        if (itemType == ItemTypes.WEAPON)
        {
            Weapon w = this as Weapon;
            if (w.damType != DamageTypes.PHYSICAL)
            {
                elementalAffinity[(int)w.damType] = true;
            }
            if (w.isRanged)
            {
                returnProperties[(int)ItemWorldProperties.TYPE_RANGED] = true;
            }
        }

        if (legendary)
        {
            returnProperties[(int)ItemWorldProperties.TYPE_LEGENDARY] = true;
        }

        if (gearSet != null)
        {
            returnProperties[(int)ItemWorldProperties.TYPE_GEARSET] = true;
        }

        if (elementalAffinity[(int)DamageTypes.FIRE])
        {
            returnProperties[(int)ItemWorldProperties.ELEM_FIRE] = true;
        }
        if (elementalAffinity[(int)DamageTypes.WATER])
        {
            returnProperties[(int)ItemWorldProperties.ELEM_WATER] = true;
        }
        if (elementalAffinity[(int)DamageTypes.SHADOW])
        {
            returnProperties[(int)ItemWorldProperties.ELEM_SHADOW] = true;
        }
        if (elementalAffinity[(int)DamageTypes.LIGHTNING])
        {
            returnProperties[(int)ItemWorldProperties.ELEM_LIGHTNING] = true;
        }
        if (elementalAffinity[(int)DamageTypes.POISON])
        {
            returnProperties[(int)ItemWorldProperties.ELEM_POISON] = true;
        }

        float rewardBonus = 0f;

        for (int i = 0; i < (int)ItemWorldProperties.COUNT; i++)
        {
            if (returnProperties[i])
            {
                rewardBonus += EquipmentBlock.itemWorldPropertyRewardMultiplier[i];
            }
        }

        returnData.rewards = rewardBonus;

        return returnData;
    }

    //returns true if this equipment can be equipped in the offhand slot
    //at all. Does NOT check the current condition of the player equipment. So this will always
    //return true for things like Quivers, even if the player has a 2H sword equipped at the time.
    public virtual bool IsOffhandable()
    {
        return false;
    }

    public string GetItemWorldDescription()
    {
        float cv = challengeValue;

        float maxDreamCV = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION : GameMasterScript.MAX_CHALLENGE_RATING;

        if (challengeValue < 1.0f || challengeValue > 500f)
        {
            cv = 1.0f;
        }
        else if (challengeValue > maxDreamCV)
        {
            cv = maxDreamCV - 0.1f;
        }

        int numFloors = 0;
        float cvIncrement = 0f;
        float startCV = 1.0f;
        string returnString = "";
        if (cv >= 1.0f && cv <= 1.2f)
        {
            numFloors = 2;
            startCV = cv - 0.1f;
            if (startCV < 1.0f) startCV = 1.0f;
            cvIncrement = 0.1f;
        }
        else if (cv > 1.2f && cv <= 1.4f)
        {
            numFloors = 3;
            startCV = cv - 0.1f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.4f && cv <= 1.6f)
        {
            numFloors = 4;
            startCV = cv - 0.15f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.6f && cv <= 1.9f)
        {
            numFloors = 5;
            startCV = cv - 0.15f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.9f && cv < 2.1f)
        {
            numFloors = 6;
            startCV = cv - 0.2f;
            cvIncrement = 0.05f;
        }
        else if (cv >= 2.1f)
        {
            numFloors = 7;
            startCV = cv - 0.2f;
            cvIncrement = 0.05f;
        }

        if ((rarity == Rarity.GEARSET || rarity == Rarity.LEGENDARY) && challengeValue <= 1.2f)
        {
            numFloors = 3;
            startCV = cv - 0.1f;
            cvIncrement = 0.05f;
        }

        if (startCV < 1.0f) startCV = 1.0f;

        float endCV = startCV + (cvIncrement * (numFloors - 1));

        Map[] itemWorld = new Map[numFloors];

        int startLevel = 0;
        int endLevel = 0;

        //float startCVRounded = (float)Math.Round(startCV, 1);
        //float endCVRounded = (float)Math.Round(endCV, 1);

        if (endCV < 1.0f)
        {
            endCV = 1.0f;
        }
        if (endCV > maxDreamCV)
        {
            endCV = maxDreamCV;
        }

        //Debug.Log(startCV + " " + endCV);

        List<DungeonLevel> possibleLevels = null;
        DungeonLevel selectedLevel = null;
        DungeonLevel endWorldMapDL = null;
        
        // Do some bullshit to ensure that we use rounded CVs like 1.00, 1.05, 1.10, etc.

        startCV = CustomAlgorithms.RoundToNearestFiveHundredth(startCV);
        endCV = CustomAlgorithms.RoundToNearestFiveHundredth(endCV);

        float actualFinalLevelCV = endCV;

        if (startCV > 1.9f || endCV > 1.9f)
        {
            startLevel = BalanceData.GetExpectedPlayerLevelByCV(startCV);
            endLevel = BalanceData.GetExpectedPlayerLevelByCV(endCV);            
        }
        else
        {
            foreach (float key in GameMasterScript.itemWorldMapDict.Keys)
            {
                if (Mathf.Abs(key - startCV) <= 0.01f)
                {
                    possibleLevels = GameMasterScript.itemWorldMapDict[key];
                    selectedLevel = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];
                    startLevel = selectedLevel.expectedPlayerLevel;
                }
                if (Mathf.Abs(key - endCV) <= 0.01f)
                {
                    possibleLevels = GameMasterScript.itemWorldMapDict[key];
                    selectedLevel = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];
                    endLevel = selectedLevel.expectedPlayerLevel;
                    endWorldMapDL = selectedLevel;
                    actualFinalLevelCV = endWorldMapDL.challengeValue;
                }
            }
        }

        if (endLevel == 0)
        {
            //Debug.Log("uh oh! " + selectedLevel.floor + " plvl is 0?");
            endLevel = StatBlock.GetMaxLevel(GameMasterScript.heroPCActor);
        }

        float rewardBonus = 0f;

        string floorText = numFloors.ToString();
        if (ItemWorldUIScript.orbSelected != null && ItemWorldUIScript.orbSelected.ReadActorData("nightmare_orb") == 1)
        {
            floorText = "????";
            startLevel++;
            endLevel++;
        }

        returnString = "<color=yellow>" + StringManager.GetString("ui_dreamcaster_floors") + ":</color> " + floorText + "\n";
        string diff = StringManager.GetString("ui_dreamcaster_difficulty");


        ItemWorldMetaData properties = GetItemWorldProperties();
        rewardBonus += properties.rewards;

        // Let's say we have +18% challenge/rewards = 0.18
        // This is about 1.44 levels of difficulty (actually more like 1.8??)
        int effectiveStartLevel = startLevel + (int)(10 * rewardBonus);
        int effectiveEndLevel = endLevel + (int)(10 * rewardBonus);

        if (endLevel > StatBlock.GetMaxLevel(GameMasterScript.heroPCActor)) endLevel = StatBlock.GetMaxLevel(GameMasterScript.heroPCActor);

        returnString += "<color=yellow>" + StringManager.GetString("ui_dreamcaster_start") + ":</color> " + startLevel + " (" + diff + ": " + Monster.EvaluateThreat(effectiveStartLevel) + ")\n";
        returnString += "<color=yellow>" + StringManager.GetString("ui_dreamcaster_final") + ":</color> " + endLevel + " (" + diff + ": " + Monster.EvaluateThreat(effectiveEndLevel) + ")\n";

#if UNITY_EDITOR
        //Debug.Log("CV of end level is " + endWorldMapDL.challengeValue);
#endif
        float timePassChance = GameMasterScript.GetChallengeModToPlayer(actualFinalLevelCV);

        float calculateRewardBonusForTimePass = rewardBonus + 1f;
        timePassChance *= calculateRewardBonusForTimePass;

        //Debug.Log("Base chance " + timePassChance);
        if (timePassChance > 1f) timePassChance = 1f;
        timePassChance *= 100f;
        string tPassString = ((int)timePassChance).ToString();
        returnString += UIManagerScript.cyanHexColor + StringManager.GetString("ui_dreamcaster_timepass") + ":</color> " + tPassString + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "\n";

        // Consolidate elemental resists x5 to a single string.

        int numElemResists = 0;
        for (int i = 0; i < (int)ItemWorldProperties.COUNT; i++)
        {
            if ((ItemWorldProperties)i == ItemWorldProperties.ELEM_FIRE || (ItemWorldProperties)i == ItemWorldProperties.ELEM_LIGHTNING || (ItemWorldProperties)i == ItemWorldProperties.ELEM_SHADOW ||
                (ItemWorldProperties)i == ItemWorldProperties.ELEM_WATER || (ItemWorldProperties)i == ItemWorldProperties.ELEM_POISON)
            {
                numElemResists++;
            }
        }
        if (numElemResists == 5)
        {
            returnString += "<color=yellow>" + StringManager.GetString("ui_dreamcaster_affinity") + "</color>: " + StringManager.GetString("itemdream_property_allelem") + "\n";
        }

        for (int i = 0; i < (int)ItemWorldProperties.COUNT; i++)
        {
            if (((ItemWorldProperties)i == ItemWorldProperties.ELEM_FIRE || (ItemWorldProperties)i == ItemWorldProperties.ELEM_LIGHTNING || (ItemWorldProperties)i == ItemWorldProperties.ELEM_SHADOW ||
                (ItemWorldProperties)i == ItemWorldProperties.ELEM_WATER || (ItemWorldProperties)i == ItemWorldProperties.ELEM_POISON) &&
                    numElemResists == 5)
            {
                continue; // Don't show elem resists one-by-one since we have the consolidated version already.
            }

            if (properties.properties[i])
            {
                returnString += "<color=yellow>" + StringManager.GetString("ui_dreamcaster_affinity") + "</color>: " + EquipmentBlock.itemWorldPropertiesVerbose[i] + "\n";
            }
        }


        if (rewardBonus > 0)
        {
            rewardBonus *= 100f;
            rewardBonus = Mathf.Round(rewardBonus);
            if (rewardBonus > 0f)
            {
                returnString += StringManager.GetString("ui_dreamcaster_challengerewards") + " " + UIManagerScript.greenHexColor + " +" + rewardBonus + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
            }
            else
            {
                returnString += StringManager.GetString("ui_dreamcaster_challengerewards") + " " + UIManagerScript.redHexColor + " +" + rewardBonus + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
            }

        }

        // Evaluate for special FX

        string upgrade = GetItemWorldUpgrade();

        returnString += "\n\n" + StringManager.GetString("ui_itemworld_upgrade") + "\n" + upgrade;

        return returnString;
    }

    public void CopyFromEquipment(Equipment template)
    {
        itemType = template.itemType;
        slot = template.slot;
        resists.Clear();
        gearSet = template.gearSet;
        if (gearSet != null)
        {
            RebuildDisplayName();
        }
        for (int i = 0; i < (int)EquipmentFlags.COUNT; i++)
        {
            eqFlags[i] = template.eqFlags[i];
        }
        foreach (ResistanceData res in template.resists)
        {
            ResistanceData rd = new ResistanceData();
            rd.CopyFromTemplate(res);
            resists.Add(rd);
        }
        foreach (AbilityScript abil in template.addAbilities)
        {
            addAbilities.Add(abil);
        }
        bDontAnnounceAddedAbilities = template.bDontAnnounceAddedAbilities;
        upgradesByRarity = template.upgradesByRarity;
    }

    public void ModifyResistMult(DamageTypes damType, float multiplier)
    {
        ResistanceData rd = null;
        foreach (ResistanceData res in resists)
        {
            if (res.damType == damType)
            {
                rd = res;
            }
        }
        if (rd == null)
        {
            rd = new ResistanceData();
            rd.damType = damType;
            resists.Add(rd);
        }
        rd.multiplier += multiplier;
    }
    public void ModifyResistOffset(DamageTypes damType, float offset)
    {
        ResistanceData rd = null;
        foreach (ResistanceData res in resists)
        {
            if (res.damType == damType)
            {
                rd = res;
            }
        }
        if (rd == null)
        {
            rd = new ResistanceData();
            rd.damType = damType;
            resists.Add(rd);
        }
        rd.flatOffset += offset;
    }
    public void SetResistMult(DamageTypes damType, float multiplier)
    {
        ResistanceData rd = null;
        foreach (ResistanceData res in resists)
        {
            if (res.damType == damType)
            {
                rd = res;
            }
        }
        if (rd == null)
        {
            rd = new ResistanceData();
            rd.damType = damType;
            resists.Add(rd);
        }
        rd.multiplier = multiplier;
    }
    public void SetResistOffset(DamageTypes damType, float offset)
    {
        ResistanceData rd = null;
        foreach (ResistanceData res in resists)
        {
            if (res.damType == damType)
            {
                rd = res;
            }
        }
        if (rd == null)
        {
            rd = new ResistanceData();
            rd.damType = damType;
            resists.Add(rd);
        }
        rd.flatOffset = offset;
    }
    public void SetResistAbsorb(DamageTypes damType, bool absorb)
    {
        ResistanceData rd = null;
        foreach (ResistanceData res in resists)
        {
            if (res.damType == damType)
            {
                rd = res;
            }
        }
        if (rd == null)
        {
            rd = new ResistanceData();
            rd.damType = damType;
            resists.Add(rd);
        }
        rd.absorb = absorb;
    }

    public void ReadAdventureStats(XmlReader reader)
    {
        string advStatsRead = reader.ReadElementContentAsString();
        string[] advParsed = advStatsRead.Split('|');
        for (int i = 0; i < advParsed.Length; i++)
        {
            float fParsed = CustomAlgorithms.TryParseFloat(advParsed[i]);
            adventureStats[i] = fParsed;
        }
    }

    public void ReadResistsFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        string txt;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            DamageTypes dt = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.Name.ToUpperInvariant());

            if (reader.IsEmptyElement)
            {
                reader.Read();
                continue;
            }

            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.Name)
                {
                    case "pc":
                    case "percent":
                        txt = reader.ReadElementContentAsString();
                        SetResistMult(dt, CustomAlgorithms.TryParseFloat(txt));
                        break;
                    case "flat":
                        txt = reader.ReadElementContentAsString();
                        SetResistOffset(dt, CustomAlgorithms.TryParseFloat(txt));
                        break;
                    case "absorb":
                        SetResistAbsorb(dt, reader.ReadElementContentAsBoolean());
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
            reader.ReadEndElement();
        }

        reader.ReadEndElement();
    }

    public void WriteEQResists(XmlWriter writer)
    {
        if (resists.Count > 0)
        {
            /* eqSB.Length = 0;
            eqSB.Append(resists.Count);            
            
            foreach(ResistanceData rd in resists)
            {
                eqSB.Append("|");
                eqSB.Append((int)rd.damType);                
                if (rd.multiplier != 1f)
                {
                    eqSB.Append(":");
                    eqSB.Append("p");
                    eqSB.Append(";");
                    eqSB.Append(rd.multiplier);
                }
                if (rd.flatOffset != 0)
                {
                    eqSB.Append(":");
                    eqSB.Append("f");
                    eqSB.Append(";");
                    eqSB.Append(rd.flatOffset);
                }
                if (rd.absorb != false)
                {
                    eqSB.Append(":");
                    eqSB.Append("a");
                }
            }

            writer.WriteElementString("res", eqSB.ToString()); */

             writer.WriteStartElement("res");

            foreach (ResistanceData rd in resists)
            {
                //Debug.Log(actorRefName + " " + rd.damType);
                writer.WriteStartElement(rd.damType.ToString().ToLowerInvariant());
                if (rd.multiplier != 1f)
                {
                    writer.WriteElementString("pc", rd.multiplier.ToString());
                }
                if (rd.flatOffset != 0)
                {
                    writer.WriteElementString("flat", rd.flatOffset.ToString());
                }
                if (rd.absorb != false)
                {
                    writer.WriteElementString("absorb", rd.absorb.ToString().ToLowerInvariant());
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); 
        }
    }

    public override void WriteToSave(XmlWriter writer)
    {
        base.WriteToSave(writer);

        string advStatsString = CreateAdventureStatsString();
        if (advStatsString != "")
        {
            writer.WriteElementString("advstats", advStatsString);
        }

        if (timesUpgraded != 0)
        {
            writer.WriteElementString("tu", timesUpgraded.ToString());
        }
        if (upgradesByRarity != 0)
        {
            writer.WriteElementString("upr", upgradesByRarity.ToString());
        }
        if (modsRemoved != 0)
        {
            writer.WriteElementString("modsremoved", modsRemoved.ToString());
        }
        if (cooldownTurnsRemaining != 0)
        {
            writer.WriteElementString("cdturns", cooldownTurnsRemaining.ToString());
        }

        if (hasBeenUnequipped)
        {
            writer.WriteStartElement("uneq");
            writer.WriteFullEndElement();
            //writer.WriteElementString("hasbeenuneq", hasBeenUnequipped.ToString().ToLowerInvariant());
        }

        WriteEQMods(writer);

        WriteEQResists(writer);
    }

    public virtual string GetItemWorldUpgrade()
    {
        return "";
    }

    public virtual void UpgradeItem(bool debug = false)
    {
        challengeValue += 0.15f;
        CalculateShopPrice(1.0f);
        CalculateSalePrice();
        timesUpgraded++;
        if (debug) Debug.Log("Upgraded " + actorRefName + " " + actorUniqueID + ", times upgraded: " + timesUpgraded + " and name is currently " + displayName);
        SetNameDirty(true);
    }

    public override bool TryReadFromXml(XmlReader reader)
    {
        if (base.TryReadFromXml(reader))
        {
            return true;
        }

        switch (reader.Name)
        {
            case "AddAbility":
                AbilityScript toCopy = AbilityScript.GetAbilityByName(reader.ReadElementContentAsString());
                if (toCopy != null)
                {
                    addAbilities.Add(toCopy);
                }
                return true;
            case "AddAbilitySilent":
                AbilityScript toCopySilent = AbilityScript.GetAbilityByName(reader.ReadElementContentAsString());
                if (toCopySilent != null)
                {
                    addAbilities.Add(toCopySilent);
                }
                bDontAnnounceAddedAbilities = true;
                return true;
            case "Resist":
                ResistanceData rd = new ResistanceData();
                resists.Add(rd);
                rd.ReadResist(reader);
                return true;
            case "EquipmentFlag":
                EquipmentFlags flag = (EquipmentFlags)Enum.Parse(typeof(EquipmentFlags), reader.ReadElementContentAsString());
                eqFlags[(int)flag] = true;
                return true;
            case "GearSet":
                gearSetRef = reader.ReadElementContentAsString();
                GameMasterScript.listDuringLoadOfEqInGearSets.Add(this);
                return true;
            case "AutoMods":
                string unparsedNames = reader.ReadElementContentAsString();
                if (autoModRef == null)
                {
                    autoModRef = new List<string>();
                }
                string[] mods = unparsedNames.Split(',');
                for (int i = 0; i < mods.Length; i++)
                {
                    autoModRef.Add(mods[i]);
                    MagicMod mmAdd = MagicMod.FindModFromName(mods[i]);
                    if (mmAdd != null)
                    {
                        EquipmentBlock.MakeMagicalFromMod(this, mmAdd, false, false, true);
                    }
                    else
                    {
                        Debug.Log("Automod " + mods[i] + " not found for " + actorRefName);
                    }

                }
                break;
            case "AutoMod":                
                string mmName = reader.ReadElementContentAsString();
                if (autoModRef == null)
                {
                    autoModRef = new List<string>();
                }
                autoModRef.Add(mmName);
                MagicMod mm = MagicMod.FindModFromName(mmName);
                if (mm != null)
                {
                    EquipmentBlock.MakeMagicalFromMod(this, mm, false, false, true);
                }
                else
                {
                    Debug.Log("Automod " + mmName + " not found for " + actorRefName);
                }
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns abilities built in to the item AND from magic mods. The bool is "true" if we should NOT announce the ability.
    /// </summary>
    /// <returns></returns>
    public Dictionary<AbilityScript, bool> GetAddedAbilities()
    {
        abilitiesFromItem.Clear();
        foreach(AbilityScript abil in addAbilities)
        {
            abilitiesFromItem.Add(abil, bDontAnnounceAddedAbilities);
        }
        foreach(MagicMod mm in mods)
        {
            foreach(AbilityScript abil in mm.addAbilities)
            {
                abilitiesFromItem.Add(abil, mm.bDontAnnounceAddedAbilities);
            }
        }
        return abilitiesFromItem;
    }

    public bool AnnounceAnyAddedAbilities()
    {
        foreach(var kvp in abilitiesFromItem)
        {
            if (kvp.Value) return true;
        }
        return false;
    }

    public static int GetMaxUpgrades()
    {
        if (GameStartData.NewGamePlus < 2)
        {
            return 3;
        }
        return 4;
    }

    public MagicMod RemoveAndAddMod(string modToRemove, string statusToRemove, string modToAdd)
    {
        if (!string.IsNullOrEmpty(modToRemove))
        {
            MagicMod remover = null;
            foreach (MagicMod mm in mods)
            {
                if (mm.refName == modToRemove)
                {
                    remover = mm;
                    break;
                }
            }

            mods.Remove(remover);
        }

        if (!string.IsNullOrEmpty(statusToRemove) && collection != null)
        {
            if (collection.Owner != null && collection.Owner.GetActorType() == ActorTypes.HERO && GameMasterScript.heroPCActor.myEquipment.IsEquipped(this))
            {
                Fighter ft = collection.Owner as Fighter;
                ft.myStats.RemoveStatusByRef(statusToRemove);
            }
        }

        MagicMod template = MagicMod.FindModFromName(modToAdd);
        MagicMod newMod = new MagicMod();
        newMod.CopyFromMod(template);
        AddMod(newMod, true);
        return newMod;
    }

    /// <summary>
    /// Returns TRUE if we have a magic mod that confers the given status
    /// </summary>
    /// <param name="statusRef"></param>
    /// <returns></returns>
    public bool CheckIfGrantsStatusViaMod(string statusRef)
    {
        foreach(MagicMod mm in mods)
        {
            foreach(StatusEffect se in mm.modEffects)
            {
                if (se.refName == statusRef) return true;
            }
        }

        return false;
    }
}