using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;


public enum ActorTypes { HERO, ITEM, DOOR, MONSTER, POWERUP, THING, STAIRS, DESTRUCTIBLE, NPC, COUNT };
public enum CharacterJobs { BRIGAND, FLORAMANCER, SWORDDANCER, SPELLSHAPER, PALADIN, BUDOKA, HUNTER, GAMBLER, HUSYN,
    SOULKEEPER, EDGETHANE, WILDCHILD, BERSERKER, SHARA, DUALWIELDER, MIRAISHARA, MONSTER, GENERIC, COUNT };
public enum SpecialMapObject { NOTHING, ALTAR, MONSTERSPAWNER, SWINGINGVINE, TREASURESPARKLE, FOUNTAIN, BREATHEPILLAR,
    STORYOBJECT, MONEY, WATER, MUD, LAVA, ELECTRIC, ISLANDSWATER, LASER, POWERUP, LAVA_LIKE_HAZARD, FLOORSWITCH, OILSLICK, BLOCKER, BLESSEDPOOL,
    BOMB_ATTACK, SLIMETOWER, METALGATE, FORCEFIELD, COUNT };
public enum Faction { NONE, ANY, ENEMY, PLAYER, DUNGEON, MYFACTION, NOTMYFACTION, CREATOR, NOTMYFACTION_NOHERO, HERO_ONLY, COUNT };
// CREATOR is deprecated
public enum Spread { NOSPREAD, FORWARD, ADJACENT, RANDOM, COUNT }
public enum ActorData { INITIALIZED, COMPLETE, COUNT }
public enum ObjectFlags { FIRE, WATER, SHADOW, LIGHTNING, POISON, TARGETING, COUNT }

static class MyExtensions
{

    //private static System.Random rng = new System.Random();
    private static XXHashRNG rng = new XXHashRNG();

    public static void InitRNG(int value)
    {
        rng = new XXHashRNG(value);
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        int reps = 0;
        while ((n > 1) && (reps < 5000))
        {
            reps++;
            n--;
            //int k = rng.Next(n + 1);            
            int k = rng.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        if (reps >= 5000)
        {
            Debug.Log("Broke list shuffle while loop");
        }
    }

}

public static class UsefulExtensions
{
    //static System.Random randomizer = new System.Random();

    public static T GetRandomElement<T>(this List<T> list)
    {
        if (list.Count == 0) return default(T);
        //return list[randomizer.Next(0, list.Count)];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T GetRandomElement<T>(this T[] list)
    {
        if (list.Length == 0) return default(T);
        return list[UnityEngine.Random.Range(0, list.Length)];
    }
}

static class IListExtensions
{
    public static void Swap<T>(
        this IList<T> list,
        int firstIndex,
        int secondIndex
    )
    {
        //Contract.Requires(list != null);
        //Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
        //Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);

        if (firstIndex == secondIndex)
        {
            return;
        }
        T temp = list[firstIndex];
        list[firstIndex] = list[secondIndex];
        list[secondIndex] = temp;
    }
}


public class DamageCarrier
{
    public float amount;
    public DamageTypes damType;
    public float floor;
    public float ceiling;

    public DamageCarrier(float amt, DamageTypes dam)
    {
        amount = amt;
        damType = dam;
        floor = -9999f;
        ceiling = 9999f;
    }
}

public class MonsterFamilyCombatBonus
{
    public float damage;
    public float defense;
    public string fName;

    public MonsterFamilyCombatBonus(string family)
    {
        fName = family;
        damage = 1f;
        defense = 1f;
    }
}

public class AnchoredActorData
{
    public Actor actorRef;
    public int actorID;
    public string refName;
    public ActorTypes actorType;

    public AnchoredActorData()
    {
        refName = "";
        actorType = ActorTypes.COUNT;
    }

    public bool WriteToSave(XmlWriter writer)
    {
        if (actorRef == null)
        {
            Debug.Log("Cannot write null anchored actor.");
            return false;
        }
        string builder = actorID + "|" + refName + "|" + actorType.ToString();
        writer.WriteElementString("anchdata", builder);
        return true;
    }

    public void ReadFromSave(XmlReader reader)
    {
        string baseData = reader.ReadElementContentAsString();
        string[] parsed = baseData.Split('|');
        Int32.TryParse(parsed[0], out actorID);
        refName = parsed[1];
        actorType = (ActorTypes)Enum.Parse(typeof(ActorTypes), parsed[2]);
    }
}

public class FighterBattleData
{
    public float physicalWeaponDamage;  // Damage with weapon
    public float physicalWeaponOffhandDamage;  // Damage with weapon
    public float offhandDamageMod; // For display only
    public float mainhandDamageMod; // For display only
    public float offhandAccuracyMod;
    public float mainhandAccuracyMod;
    public float physicalMeleeBaseDamage; // Raw damage output
    public float physicalRangedBaseDamage; // Raw damage output
    public float spiritPower;

    public float spiritPowerMod;
    public float spiritPowerModMult;
    public float critDamageMod;
    public float critChanceMod;

    public float energyCostMod;
    public float staminaCostMod;
    public float healthCostMod;

    public int energyReservedByAbilities; // Reduces an actor's *effective* max energy
    public int staminaReservedByAbilities; // Reduces an actor's *effective* max stamina

    public float forcedStaminaCosts;
    public float forcedEnergyCosts;

    public float maxAttackRange;
    public float weaponAttackRange;
    public int maxMoveRange;

    public int attackRangeModifier;

    public float parryMeleeChance;
    public float parryRangedChance;
    public float blockMeleeChance;
    public float blockRangedChance;
    public float dodgeMeleeChange;
    public float dodgeRangedChance;
    public float critMeleeChance;
    public float critRangedChance;
    public float critMeleeDamageMult;
    public float critRangedDamageMult;

    public float chargeGain;

    public float extraHeathRegen;
    public float extraStaminaRegen;
    public float extraEnergyRegen;

    public ResistanceData[] resistances;
    public ResistanceData[] pierceResistances;
    public float[] resistanceExternalMods;
    public float[] damageExternalMods;
    public float[] temporaryDamageMods;

    // Effects can convert a damage type like FIRE into PHYSICAL damage dealt OR received, track that here.
    public DamageTypes[] damageTypeDealtConversions;
    public DamageTypes[] damageTypeReceivedConversions;

    public float stealthValue; // used only for hero, for now
    public float healModifierValue;

    public Dictionary<string, MonsterFamilyCombatBonus> familyBonuses;

    public Dictionary<string, float> effectValueModifiers; // This applies changes to any effects with an effectEquation. Damage, changestat.

    //Shep: Remapped abilities -- when one ability overwrites another
    private Dictionary<string, string> remappedAbilityRefs;

    private bool bIsDirty;

    public void SetDirty() { bIsDirty = true; }
    public bool IsDirty() { return bIsDirty; }
    public void SetClean() { bIsDirty = false; }

    public AbilityScript GetOriginalVersionOfRemappedAbility(string abilRef, Fighter owner)
    {
        if (remappedAbilityRefs == null)
        {
            return owner.myAbilities.GetAbilityByRef(abilRef);
        }

        // abilRef being passed in is something like "skill_summonlivingvine2", which is a dictionary VALUE
        // We want to find the corresponding KEY
        // Which is not the right use of dictionaries but that's where we are

        foreach(string keyCheck in remappedAbilityRefs.Keys)
        {
            if (remappedAbilityRefs[keyCheck] == abilRef)
            {
                // Ok, abilRef is the MODIFIED VERSION of keyCheck
                // So find the original keyCheck in our abilities
                return owner.myAbilities.GetAbilityByRef(keyCheck);
            }
        }

        return owner.myAbilities.GetAbilityByRef(abilRef);
    }

    public AbilityScript GetRemappedAbilityIfExists(AbilityScript checkAbility, Fighter owner, bool changeCooldownsIfNecessary = true)
    {
        //Debug.Log("Running remapper on " + checkAbility.refName);
        if (remappedAbilityRefs == null)
        {
            //Debug.Log("No remaps.");
            return checkAbility;
        }
        string strRemappedName;
        remappedAbilityRefs.TryGetValue(checkAbility.refName, out strRemappedName);

        //Debug.Log("Str remapped name is: " + strRemappedName);

        if (!string.IsNullOrEmpty(strRemappedName))
        {
            AbilityScript mappedAbility = owner.myAbilities.GetAbilityByRef(strRemappedName);

            if (mappedAbility == null)
            {
                Debug.Log("WARNING: " + strRemappedName + " is not known by player.");
                return checkAbility;
            }

            if (changeCooldownsIfNecessary)
            {
                //Debug.Log(checkAbility.refName + " " + changeCooldownsIfNecessary + " " + checkAbility.GetCurCooldownTurns() + " " + mappedAbility.GetCurCooldownTurns() + " " + mappedAbility.refName);
            }


            // Previously we used the greater of the two. Instead, let's keep them in sync.
            /* mappedAbility.curCooldownTurns = Math.Max(mappedAbility.curCooldownTurns, checkAbility.curCooldownTurns);
            if (mappedAbility.curCooldownTurns > mappedAbility.maxCooldownTurns)
            {
                mappedAbility.ResetCooldown();
            } */

            if (changeCooldownsIfNecessary)
            {
                mappedAbility.SetCurCooldownTurns(checkAbility.GetCurCooldownTurns());
                //Debug.Log("Setting " + mappedAbility.refName + " cooldown turns to " + checkAbility.refName + " CD turns. The values are now equal at " + mappedAbility.GetCurCooldownTurns());
            }
            return mappedAbility;
        }

        return checkAbility;
    }

    public void SetRemappedAbility(string strSourceAbility, string strRemap)
    {
        if (remappedAbilityRefs == null)
        {
            remappedAbilityRefs = new Dictionary<string, string>();
        }

        remappedAbilityRefs[strSourceAbility] = strRemap;
    }

    /// <summary>
    /// Returns TRUE if any of our damage dealt/received is being remapped to another type.
    /// </summary>
    /// <returns></returns>
    public bool AnyDamageTypeConversions()
    {
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            if (damageTypeDealtConversions[i] != (DamageTypes)i)
            {
                return true;
            }
            if (damageTypeReceivedConversions[i] != (DamageTypes)i)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearRemappedAbilities()
    {
        AbilityScript mod;
        AbilityScript unmod;
        foreach (string key in remappedAbilityRefs.Keys)
        {
            // Let's look at the modified abilities and set the UNMODIFIED cooldowns to the same value
            if (string.IsNullOrEmpty(key)) continue;


            mod = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(remappedAbilityRefs[key]);
            if (mod != null)
            {
                unmod = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(key);
                if (unmod == null) continue;
                if (mod.refName.Contains("bones"))
                {
                    //Debug.Log("Modified: " + mod.GetCurCooldownTurns() + " Unmodified: " + unmod.GetCurCooldownTurns());
                }
                unmod.SetCurCooldownTurns(mod.GetCurCooldownTurns());
            }
        }
        remappedAbilityRefs = new Dictionary<string, string>();
    }

    public void ReadFromSave(XmlReader reader, Fighter owner)
    {
        reader.ReadStartElement();
        string txt;
        string unparsed = "";
        string[] parsed;

        while (reader.NodeType != XmlNodeType.EndElement)
        {            switch (reader.Name)
            {
                case "damagedealtconversion":
                    // format: <damagedealtconversion>fire|shadow</damagedealtconversion>
                    // TO|FROM
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    DamageTypes damageConvertedFrom = (DamageTypes)Enum.Parse(typeof(DamageTypes), parsed[0]);
                    DamageTypes damageConvertedTo = (DamageTypes)Enum.Parse(typeof(DamageTypes), parsed[1]);
                    damageTypeDealtConversions[(int)damageConvertedFrom] = damageConvertedTo;
                    break;
                case "damagereceivedconversion":
                    // format: <damagedealtconversion>fire|shadow</damagedealtconversion>
                    // TO|FROM
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    damageConvertedFrom = (DamageTypes)Enum.Parse(typeof(DamageTypes), parsed[0]);
                    damageConvertedTo = (DamageTypes)Enum.Parse(typeof(DamageTypes), parsed[1]);
                    damageTypeReceivedConversions[(int)damageConvertedFrom] = damageConvertedTo;
                    break;
                case "attackrangemod":
                    attackRangeModifier = reader.ReadElementContentAsInt();
                    break;
                case "crt":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    // crit damage followed by chance mod
                    critDamageMod = CustomAlgorithms.TryParseFloat(parsed[0]);
                    critChanceMod = CustomAlgorithms.TryParseFloat(parsed[1]);
                    break;
                case "cdm":
                case "critdamagemod":
                    txt = reader.ReadElementContentAsString();
                    critDamageMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "stealth":
                    txt = reader.ReadElementContentAsString();
                    stealthValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "healmod":
                    txt = reader.ReadElementContentAsString();
                    healModifierValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ccm":
                case "critchancemod":
                    txt = reader.ReadElementContentAsString();
                    critChanceMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistfiremod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.FIRE] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistwatermod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.WATER] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistshadowmod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.SHADOW] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistlightningmod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.LIGHTNING] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistpoisonmod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.POISON] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "resistphysicalmod":
                    txt = reader.ReadElementContentAsString();
                    resistanceExternalMods[(int)DamageTypes.PHYSICAL] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "firedmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.FIRE] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "waterdmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.WATER] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "lightningdmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.LIGHTNING] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "poisondmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.POISON] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "shadowdmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.SHADOW] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "physicaldmgmod":
                    txt = reader.ReadElementContentAsString();
                    damageExternalMods[(int)DamageTypes.PHYSICAL] = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "energycosts":
                    txt = reader.ReadElementContentAsString();
                    energyCostMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "staminacosts":
                    txt = reader.ReadElementContentAsString();
                    staminaCostMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "healthcosts":
                    txt = reader.ReadElementContentAsString();
                    healthCostMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "forcedenergy":
                    txt = reader.ReadElementContentAsString();
                    forcedEnergyCosts = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "forcedstamina":
                    txt = reader.ReadElementContentAsString();
                    forcedStaminaCosts = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "spiritpowermod":
                    txt = reader.ReadElementContentAsString();
                    spiritPowerMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "spiritpowermodmult":
                    txt = reader.ReadElementContentAsString();
                    spiritPowerModMult = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "dummy":
                    reader.ReadElementContentAsString();
                    break;
                case "effectvalmodifiers":
                    reader.ReadStartElement();

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            string name = reader.Name;
                            string unparsedValue = reader.ReadElementContentAsString();
                            float parsedF = CustomAlgorithms.TryParseFloat(unparsedValue);
                            effectValueModifiers.Add(name, parsedF);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }

                    reader.ReadEndElement();
                    break;
                case "monfamilybonus":
                    reader.ReadStartElement();
                    string fName = reader.ReadElementContentAsString();
                    MonsterFamilyCombatBonus mfcb = new MonsterFamilyCombatBonus(fName);

                    txt = reader.ReadElementContentAsString();

                    float damage = CustomAlgorithms.TryParseFloat(txt);

                    txt = reader.ReadElementContentAsString();

                    float defense = CustomAlgorithms.TryParseFloat(txt);
                    mfcb.damage = damage;
                    mfcb.defense = defense;
                    reader.ReadEndElement();
                    if (familyBonuses == null)
                    {
                        familyBonuses = new Dictionary<string, MonsterFamilyCombatBonus>();
                    }
                    //Debug.Log("Adding " + fName + " " + damage + " " + defense + " to " + owner.actorUniqueID);
                    familyBonuses.Add(fName, mfcb);
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }

    bool AnyDataToWrite()
    {
        if (critDamageMod != 0) return true;
        if (critChanceMod != 0) return true;
        if (attackRangeModifier != 0) return true;
        if (resistanceExternalMods[(int)DamageTypes.FIRE] != 1f) return true;
        if (resistanceExternalMods[(int)DamageTypes.WATER] != 1f) return true;
        if (resistanceExternalMods[(int)DamageTypes.LIGHTNING] != 1f) return true;
        if (resistanceExternalMods[(int)DamageTypes.SHADOW] != 1f) return true;
        if (resistanceExternalMods[(int)DamageTypes.POISON] != 1f) return true;
        if (resistanceExternalMods[(int)DamageTypes.PHYSICAL] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.FIRE] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.WATER] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.SHADOW] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.POISON] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.PHYSICAL] != 1f) return true;
        if (damageExternalMods[(int)DamageTypes.LIGHTNING] != 1f) return true;

        if (AnyDamageTypeConversions()) return true;

        if (energyCostMod != 1f) return true;
        if (staminaCostMod != 1f) return true;
        if (healthCostMod != 0f) return true;
        if (forcedEnergyCosts != 0f) return true;
        if (forcedStaminaCosts != 0f) return true;
        if (spiritPowerMod != 0) return true;
        if (spiritPowerModMult != 1f) return true;
        if (familyBonuses != null) return true;

        if (stealthValue != 1f) return true;
        if (healModifierValue != 1f) return true;

        return false;
    }

    public void WriteToSave(XmlWriter writer)
    {
        // Check if there is anything to write.
        if (!AnyDataToWrite()) return;

        writer.WriteStartElement("btld");
        //writer.WriteElementString("dummy", "nothing");

        if (!CustomAlgorithms.CompareFloats(stealthValue, 1f))
        {
            writer.WriteElementString("stealth", stealthValue.ToString());
        }

        if (!CustomAlgorithms.CompareFloats(healModifierValue, 1f))
        {
            writer.WriteElementString("healmod", healModifierValue.ToString());
        }

        // Compact CDM / CCM writing
        if (!CustomAlgorithms.CompareFloats(critDamageMod, 0f) && !CustomAlgorithms.CompareFloats(critChanceMod, 0f))
        {
            writer.WriteElementString("crt", critDamageMod + "|" + critChanceMod);
        }
        else
        {
            if (!CustomAlgorithms.CompareFloats(critDamageMod, 0f))
            {
                writer.WriteElementString("cdm", critDamageMod.ToString());
            }
            if (!CustomAlgorithms.CompareFloats(critChanceMod, 0f))
            {
                writer.WriteElementString("ccm", critChanceMod.ToString());
            }
        }

        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            if (damageTypeDealtConversions[i] != (DamageTypes)i)
            {
                writer.WriteElementString("damagedealtconversion", ((DamageTypes)i).ToString() + "|" + damageTypeDealtConversions[i].ToString());
            }
            if (damageTypeReceivedConversions[i] != (DamageTypes)i)
            {
                writer.WriteElementString("damagereceivedconversion", ((DamageTypes)i).ToString() + "|" + damageTypeDealtConversions[i].ToString());
            }
        }

        if (attackRangeModifier != 0)
        {
            writer.WriteElementString("attackrangemod", attackRangeModifier.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.FIRE], 1f))
        {
            writer.WriteElementString("resistfiremod", resistanceExternalMods[(int)DamageTypes.FIRE].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.WATER], 1f))
        {
            writer.WriteElementString("resistwatermod", resistanceExternalMods[(int)DamageTypes.WATER].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.SHADOW], 1f))
        {
            writer.WriteElementString("resistshadowmod", resistanceExternalMods[(int)DamageTypes.SHADOW].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.LIGHTNING], 1f))
        {
            writer.WriteElementString("resistlightningmod", resistanceExternalMods[(int)DamageTypes.LIGHTNING].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.POISON], 1f))
        {
            writer.WriteElementString("resistpoisonmod", resistanceExternalMods[(int)DamageTypes.POISON].ToString());
        }

        if (!CustomAlgorithms.CompareFloats(resistanceExternalMods[(int)DamageTypes.PHYSICAL], 1f))
        {
            writer.WriteElementString("resistphysicalmod", resistanceExternalMods[(int)DamageTypes.PHYSICAL].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.FIRE], 1f))
        {
            writer.WriteElementString("firedmgmod", damageExternalMods[(int)DamageTypes.FIRE].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.WATER], 1f))
        {
            writer.WriteElementString("waterdmgmod", damageExternalMods[(int)DamageTypes.WATER].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.LIGHTNING], 1f))
        {
            writer.WriteElementString("lightningdmgmod", damageExternalMods[(int)DamageTypes.LIGHTNING].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.POISON], 1f))
        {
            writer.WriteElementString("poisondmgmod", damageExternalMods[(int)DamageTypes.POISON].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.SHADOW], 1f))
        {
            writer.WriteElementString("shadowdmgmod", damageExternalMods[(int)DamageTypes.SHADOW].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(damageExternalMods[(int)DamageTypes.PHYSICAL], 1f))
        {
            writer.WriteElementString("physicaldmgmod", damageExternalMods[(int)DamageTypes.PHYSICAL].ToString());
        }
        if (!CustomAlgorithms.CompareFloats(energyCostMod, 1f))
        {
            writer.WriteElementString("energycosts", energyCostMod.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(staminaCostMod, 1f))
        {
            writer.WriteElementString("staminacosts", staminaCostMod.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(healthCostMod, 0f))
        {
            writer.WriteElementString("healthcosts", healthCostMod.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(forcedEnergyCosts, 0f))
        {
            writer.WriteElementString("forcedenergy", forcedEnergyCosts.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(forcedStaminaCosts, 0f))
        {
            writer.WriteElementString("forcedstamina", forcedStaminaCosts.ToString());
        }

        if (!CustomAlgorithms.CompareFloats(spiritPowerMod, 0f))
        {
            writer.WriteElementString("spiritpowermod", spiritPowerMod.ToString());
        }
        if (!CustomAlgorithms.CompareFloats(spiritPowerModMult, 1f))
        {
            writer.WriteElementString("spiritpowermodmult", spiritPowerModMult.ToString());
        }
        
        if (familyBonuses != null)
        {
            foreach (string str in familyBonuses.Keys)
            {
                writer.WriteStartElement("monfamilybonus");
                writer.WriteElementString("family", str);
                writer.WriteElementString("damage", familyBonuses[str].damage.ToString());
                writer.WriteElementString("defense", familyBonuses[str].defense.ToString());
                writer.WriteEndElement();
            }
        } 
        if (effectValueModifiers.Keys.Count > 0)
        {          
            writer.WriteStartElement("effectvalmodifiers");
            foreach (string str in effectValueModifiers.Keys)
            {
                writer.WriteElementString(str, effectValueModifiers[str].ToString());
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    /// <summary>
    /// Returns converted dmg type (like Fire --> Shadow) if one exists, for damage DEALT.
    /// </summary>
    /// <param name="inputDamage"></param>
    /// <returns></returns>
    public DamageTypes GetConvertedDamageDealType(DamageTypes inputDamage)
    {
        return damageTypeDealtConversions[(int)inputDamage];
    }

    /// <summary>
    /// Returns converted dmg type (like Fire --> Shadow) if one exists, for damage RECEIVED.
    /// </summary>
    /// <param name="inputDamage"></param>
    /// <returns></returns>
    public DamageTypes GetConvertedDamageReceiveType(DamageTypes inputDamage)
    {
        return damageTypeReceivedConversions[(int)inputDamage];
    }

    public void ChangeFamilyBonus(string family, float damage, float defense)
    {
        if (familyBonuses == null)
        {
            familyBonuses = new Dictionary<string, MonsterFamilyCombatBonus>();
        }
        if (!familyBonuses.ContainsKey(family))
        {
            familyBonuses.Add(family, new MonsterFamilyCombatBonus(family));
        }

        familyBonuses[family].damage += damage;
        familyBonuses[family].defense += defense;

        //Debug.Log("Changed " + family + " dmg to " + familyBonuses[family].damage + " and defense to " + familyBonuses[family].defense);
    }

    public float GetDamageBonusByFamily(string family)
    {
        if (familyBonuses == null) return 1f;
        if (!familyBonuses.ContainsKey(family)) return 1f;
        return familyBonuses[family].damage;
    }

    public float GetDefenseBonusByFamily(string family)
    {
        if (familyBonuses == null) return 1f;
        if (!familyBonuses.ContainsKey(family)) return 1f;
        return familyBonuses[family].defense;
    }

    public FighterBattleData()
    {
        resistanceExternalMods = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            resistanceExternalMods[i] = 1.0f;
        }

        damageExternalMods = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            damageExternalMods[i] = 1.0f;
        }

        temporaryDamageMods = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            temporaryDamageMods[i] = 1.0f;
        }

        resistances = new ResistanceData[(int)DamageTypes.COUNT];
        pierceResistances = new ResistanceData[(int)DamageTypes.COUNT];
        damageTypeDealtConversions = new DamageTypes[(int)DamageTypes.COUNT];
        damageTypeReceivedConversions = new DamageTypes[(int)DamageTypes.COUNT];

        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            resistances[i] = new ResistanceData();
            resistances[i].damType = (DamageTypes)i;

            pierceResistances[i] = new ResistanceData();
            pierceResistances[i].damType = (DamageTypes)i;

            damageTypeDealtConversions[i] = (DamageTypes)i;
            damageTypeReceivedConversions[i] = (DamageTypes)i;
        }
        
        mainhandDamageMod = 1.0f;
        offhandDamageMod = 1.0f;
        offhandAccuracyMod = 1.0f;
        mainhandAccuracyMod = 1.0f;
        spiritPowerModMult = 1.0f;
        energyCostMod = 1.0f;
        staminaCostMod = 1.0f;
        stealthValue = 1f;
        healModifierValue = 1f;

        remappedAbilityRefs = new Dictionary<string, string>();
        effectValueModifiers = new Dictionary<string, float>();
    }
}

public class OverlayData
{
    public GameObject overlayGO;
    public bool alwaysDisplay;

    public void TrySetCurVisible(bool value)
    {
        if (overlayGO == null)
        {
            return;
        }
        SpriteEffect se = overlayGO.GetComponent<SpriteEffect>();
        if (se == null)
        {
            return;
        }
        se.SetCurVisible(true);
    }
}



