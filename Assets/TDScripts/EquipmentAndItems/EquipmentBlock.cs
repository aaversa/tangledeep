using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Linq;
using System.Text;

public enum DamageTypes { PHYSICAL, FIRE, POISON, WATER, LIGHTNING, SHADOW, COUNT };
public enum FlavorDamageTypes { SLASH, BLUNT, PIERCE, BITE, COUNT };
public enum WeaponTypes { NATURAL, SWORD, AXE, SPEAR, DAGGER, MACE, BOW, STAFF, SLING, CLAW, WHIP, SPECIAL, ANY, COUNT }
public enum ArmorTypes { NATURAL, LIGHT, MEDIUM, HEAVY, COUNT }
public enum ItemTypes { WEAPON, OFFHAND, ARMOR, ACCESSORY, CONSUMABLE, EQUIPMENT, ANY, MAGICAL, EMBLEM, COUNT };
public enum EquipmentSlots { WEAPON, OFFHAND, ARMOR, ACCESSORY, ACCESSORY2, EMBLEM, ANY, COUNT };
public enum Rarity { COMMON, UNCOMMON, MAGICAL, ANCIENT, ARTIFACT, LEGENDARY, GEARSET, COUNT };
public enum MagicModFlags { SLAYING, HEALTH, LUCKY, CRITICAL, SHIELD, QUIVER, BOOK, NIGHTMARE, MELEE, CASINO, ONLY2HMELEE, BOW, COUNT };
public enum EquipmentFlags { MELEEPENALTY, RANGEPENALTY, COUNT }
public enum GearFilters { WEAPON, OFFHAND, ARMOR, ACCESSORY, COMMON, MAGICAL, LEGENDARY, GEARSET, VIEWALL, FAVORITES, COUNT }
public enum ItemFilters { INGREDIENT, MEAL, SUPPORT, OFFENSE, VALUABLES, HEALHP, HEALSTAMINA, HEALENERGY, DEALDAMAGE,
    SELFBUFF, SUMMON, VIEWALL, RECOVERY, FAVORITES, UTILITY, GEM, MULTI_USE, DICT_NOSTACK, COUNT }
public enum ItemWorldProperties { ELEM_FIRE, ELEM_WATER, ELEM_LIGHTNING, ELEM_SHADOW, ELEM_POISON, TYPE_RANGED, TYPE_LEGENDARY, TYPE_HEALTH,
    TYPE_GEARSET, TYPE_GILDED, TYPE_CRITICAL, TYPE_DENSE, TYPE_MORECHAMPIONS, TYPE_NOCHAMPIONS, TYPE_MELEEBOOST, NIGHTMARE,
    TYPE_FAMILY_FROGS, TYPE_FAMILY_JELLIES, TYPE_FAMILY_BEASTS, TYPE_FAMILY_ROBOTS, TYPE_FAMILY_BANDITS, TYPE_FAMILY_HYBRIDS, TYPE_FAMILY_INSECTS,
    COUNT }


public class GearSetBonus
{
    public int numPieces;
    public string abilityRef;
    public string statusRef;

    public GearSetBonus()
    {
        numPieces = 1;
        abilityRef = "";
        statusRef = "";
    }
}

public class GearSet
{
    public string refName;
    public string displayName;
    public string description;
    public string unbakedDescription;
    public List<Equipment> gearPieces;
    //public string[] statusBonusRefs;
    public List<GearSetBonus> setBonuses;
    public List<string> numberTags;

    public GearSet()
    {
        gearPieces = new List<Equipment>();
        //statusBonusRefs = new string[5]; // There are a max of 5 equipment slots.
        setBonuses = new List<GearSetBonus>();
        description = "";
        unbakedDescription = "";
        numberTags = new List<string>();
    }

    public void ParseNumberTags()
    {
        unbakedDescription = description;
        if (!numberTags.Any()) return;        
        for (int i = 0; i < numberTags.Count; i++)
        {
            description = description.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
        }
    }

    public void CheckAndExecuteBonus(int numPiecesForBonus, bool bonusGained) // If false, we are reversing
    {
        foreach (GearSetBonus gsb in setBonuses)
        {
            //Debug.Log("Check " + numPiecesForBonus + " " + bonusGained + " against " + gsb.numPieces + " which gives " + gsb.abilityRef + " " + gsb.statusRef);
            if (gsb.numPieces == numPiecesForBonus)
            {
                //Debug.Log("Yes we can process " + gsb.abilityRef + " | " + gsb.statusRef);
                if (!string.IsNullOrEmpty(gsb.abilityRef))
                {
                    if (bonusGained)
                    {
                        if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(gsb.abilityRef))
                        {
                            // Player does not know this ability. Make a copy and learn it.
                            AbilityScript learned = GameMasterScript.heroPCActor.LearnAbility(GameMasterScript.masterAbilityList[gsb.abilityRef], false, true, false);
                        }
                    }
                    else
                    {
                        GameMasterScript.heroPCActor.myAbilities.RemoveAbility(gsb.abilityRef);
                    }
                }
                if (!string.IsNullOrEmpty(gsb.statusRef))
                {
                    if (bonusGained)
                    {
                        if (!GameMasterScript.heroPCActor.myStats.CheckHasStatusName(gsb.statusRef))
                        {
                            //Debug.Log("Adding " + gsb.statusRef);
                            GameMasterScript.heroPCActor.myStats.AddStatusByRef(gsb.statusRef, GameMasterScript.heroPCActor, 0);
                        }
                        else
                        {
                            //Debug.Log("Wait, we already have " + gsb.statusRef);
                        }
                    }
                    else
                    {
                        //Debug.Log("Removing " + gsb.statusRef);
                        GameMasterScript.heroPCActor.myStats.RemoveStatusByRef(gsb.statusRef);
                    }
                }

                GameMasterScript.heroPCActor.cachedBattleData.SetDirty();
            }
        }
    }
    
    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();

        int statusBonusIndex = 0;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            //Debug.Log(reader.Name + " " + reader.NodeType);
            switch (reader.Name.ToLowerInvariant())
            {
                case "setbonus":
                    reader.ReadStartElement();
                    GearSetBonus gsb = new GearSetBonus();
                    setBonuses.Add(gsb);
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name.ToLowerInvariant())
                        {
                            case "reqpieces":
                                gsb.numPieces = reader.ReadElementContentAsInt();
                                break;
                            case "grantability":
                                gsb.abilityRef = reader.ReadElementContentAsString();
                                break;
                            case "statusbonus":
                                gsb.statusRef = reader.ReadElementContentAsString();
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "displayname":
                    displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "refname":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "description":
                    description = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "ntag":
                case "numbertag":
                    numberTags.Add(reader.ReadElementContentAsString());
                    break;
                case "gearpieceref":
                    string itemRef = reader.ReadElementContentAsString();
                    Equipment eq = Item.GetItemTemplateFromRef(itemRef) as Equipment;
                    if (eq == null)
                    {
                        Debug.Log("Couldn't find set component for " + refName + " item ref " + itemRef);
                    }
                    else
                    {
                        gearPieces.Add(eq);
                    }
                    break;
                case "statusbonusref":
                    reader.Read();
                    statusBonusIndex++;
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();

        return true;
    }
}

public class ItemWorldMetaData
{
    public bool[] properties;
    public float rewards;

    public ItemWorldMetaData()
    {
        properties = new bool[(int)ItemWorldProperties.COUNT];
        rewards = 0f;
    }
}

public class EQPair
{
    public int itemID;
    public Equipment eq;
    public bool isMainhandItem;

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("paireditem");

        if (isMainhandItem)
        {
            writer.WriteElementString("mainhand", "1");
        }
        else
        {
            writer.WriteElementString("mainhand", "0");
        }
        writer.WriteElementString("id", itemID.ToString());

        writer.WriteEndElement();

    }

    public EQPair(bool mainHand)
    {
        isMainhandItem = mainHand;
    }
}


public class ResistanceData
{
    public DamageTypes damType;
    public float multiplier;
    public float flatOffset;
    public bool absorb; // does it heal you?

    public ResistanceData()
    {
        multiplier = 1.0f;
        flatOffset = 0.0f;
        absorb = false;
    }

    public void ReadResist(XmlReader reader)
    {
        reader.ReadStartElement();
        string txt = "";
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "ResistDamageType":
                    damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                    break;
                case "ResistMultiplier":
                    txt = reader.ReadElementContentAsString();
                    multiplier = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ResistFlatOffset":
                    txt = reader.ReadElementContentAsString();
                    flatOffset = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "ResistAbsorb":
                    absorb = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }

    public void ResetResist(DamageTypes type)
    {
        damType = type;
        multiplier = 1.0f;
        flatOffset = 0.0f;
        absorb = false;
    }

    public void CopyFromTemplate(ResistanceData rd)
    {
        damType = rd.damType;
        multiplier = rd.multiplier;
        flatOffset = rd.flatOffset;
        absorb = rd.absorb;
    }
}




[System.Serializable]
public class EquipmentBlock
{
    public Fighter owner;
    public Equipment[] equipment;
    public Weapon defaultWeapon;


    public static string[] itemTypeNames;
    public static string[] equipmentSlotNames;
    public static string[] damageTypeNames;
    public static string[] itemWorldPropertiesVerbose;
    public static float[] itemWorldPropertyRewardMultiplier;

    static List<MagicMod> possibleMods;
    static HashSet<int> exclusions;
    static List<MagicMod> possibleModFlagMods;
    static bool poolingListsInitializedEver;

    static void CheckForPoolingListsInitialized()
    {
        if (poolingListsInitializedEver) return;
        poolingListsInitializedEver = true;
        possibleMods = new List<MagicMod>();
        exclusions = new HashSet<int>();
        possibleModFlagMods = new List<MagicMod>();
    }

    public void ReadFromSave(XmlReader reader, bool equipItems, bool blockHasOwner = true)
    {
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
            {
                strValue = reader.Name.ToLowerInvariant();
                if (strValue == ((EquipmentSlots)i).ToString().ToLowerInvariant())
                {
                    Item itm = new Item();
                    reader.ReadStartElement();
                    if (reader.Name.ToLowerInvariant() != "item")
                    {
                        continue;
                    }

                    itm = itm.ReadFromSave(reader);
                    if (itm != null)
                    {
                        if (blockHasOwner) itm.collection = owner.myInventory;

                        int subSlot = 0;
                        if ((EquipmentSlots)i == EquipmentSlots.OFFHAND && itm.itemType == ItemTypes.WEAPON)
                        {
                            subSlot = 1;
                        }
                        if ((EquipmentSlots)i == EquipmentSlots.ACCESSORY2)
                        {
                            subSlot = 1;
                        }
                        Equipment eq = itm as Equipment;

                        if (blockHasOwner && equipItems)
                        {
                            Equip(eq, SND.SILENT, subSlot, true);
                        }
                        SetEquipment(eq, (EquipmentSlots)i, false);
                    }

                    reader.ReadEndElement();
                    // Do things need to be run post-equip?
                }
            }
        }
        reader.ReadEndElement();
    }

    public bool IsDualWielding()
    {
        Weapon offhandWeapon = equipment[(int)EquipmentSlots.OFFHAND] as Weapon;
        if (offhandWeapon != null)
        {
            return true;
        }

        return false;
    }

    public float GetOffhandBlock()
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null) return 0f;

        if (equipment[(int)EquipmentSlots.OFFHAND].itemType == ItemTypes.WEAPON) return 0f;

        Offhand oh = equipment[(int)EquipmentSlots.OFFHAND] as Offhand;

        return oh.blockChance;
    }

    public bool HasEquipmentByRef(string refName)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == null) continue;
            if (equipment[i].actorRefName == refName) return true;
        }
        return false;
    }

    public float GetDodgeFromArmor()
    {
        if (GetArmor() == null) return 0;

        float baseAmount = (float)(GetArmor().extraDodge);

        if (owner.GetActorType() == ActorTypes.HERO && GetArmorType() == ArmorTypes.MEDIUM
            && owner.myStats.CheckHasStatusName("mediumarmormastery1"))
        {
            float extraDodgeChance = owner.myStats.GetCurStat(StatTypes.SWIFTNESS) * 0.125f;
            if (extraDodgeChance >= 10f) extraDodgeChance = 10f;
            baseAmount += extraDodgeChance;
        }

        return baseAmount;
    }

    public bool GetWeaponFlag(EquipmentFlags ef)
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null) return false;

        return equipment[(int)EquipmentSlots.WEAPON].eqFlags[(int)ef];
    }

    public float GetBlockDamageReduction()
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null) return 1f;
        if (equipment[(int)EquipmentSlots.OFFHAND].itemType != ItemTypes.OFFHAND) return 1f;
        Offhand currentOH = equipment[(int)EquipmentSlots.OFFHAND] as Offhand;
        if (currentOH.blockChance == 0f) return 1f;
        float baseBlockReduction = currentOH.blockDamageReduction;
        if ((owner == GameMasterScript.heroPCActor) && (owner.myStats.CheckHasStatusName("status_paladinblockbuff")))
        {
            baseBlockReduction -= 0.15f;
        }
        if (baseBlockReduction < 0.05f)
        {
            baseBlockReduction = 0.05f;
        }

        if (owner == GameMasterScript.heroPCActor && owner.myStats.CheckHasStatusName("emblem_paladinemblem_tier2_maxblock") && 
            owner.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.331f)
        {
            baseBlockReduction = 0f;
        }

        return baseBlockReduction;
    }

    public Item GetItemByID(int iItemID)
    {
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (equipment[i] == null)
            {
                continue;
            }

            if (equipment[i].actorUniqueID == iItemID)
            {
                return equipment[i];
            }
        }
        return null;
    }

    public Equipment GetItemByRefIfEquipped(string refName)
    {
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (equipment[i] == null) continue;
            if (equipment[i].actorRefName == refName) return equipment[i];
        }
        return null;
    }

    public Equipment GetItemByIDIfEquipped(int actorID)
    {
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (equipment[i] == null) continue;
            if (equipment[i].actorUniqueID == actorID) return equipment[i];
        }
        return null;
    }

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("eq");

        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            writer.WriteStartElement(((EquipmentSlots)i).ToString().ToLowerInvariant());
            if (equipment[i] != null)
            {
                equipment[i].WriteToSave(writer);
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    public bool GetOffhandWeaponMod(string modRef)
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null) return false;
        if (equipment[(int)EquipmentSlots.OFFHAND].itemType != ItemTypes.WEAPON) return false;
        foreach (MagicMod mod in equipment[(int)EquipmentSlots.OFFHAND].mods)
        {
            if (modRef == mod.refName)
            {
                return true;
            }
        }
        return false;
    }

    public bool GetWeaponMod(string modRef)
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null) return false;
        if (equipment[(int)EquipmentSlots.WEAPON].itemType != ItemTypes.WEAPON) return false;
        foreach (MagicMod mod in equipment[(int)EquipmentSlots.WEAPON].mods)
        {
            if (modRef == mod.refName)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsEquipped(Item itm)
    {
        if (!itm.IsEquipment())
        {
            return false;
        }
        Equipment eq = itm as Equipment;
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == eq)
            {
                return true;
            }
        }
        return false;
    }

    public void EvaluateSets(Equipment removedItem)
    {
        //Debug.Log("Evaluating player sets. Item null? " + (removedItem == null));

        List<GearSet> setsToEvaluate = new List<GearSet>();

        if (removedItem != null)
        {
            if (removedItem.gearSet != null)
            {
                // Make sure this is evaluated specifically
                setsToEvaluate.Add(removedItem.gearSet);
            }
        }

        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            //Debug.Log("Item in slot " + (EquipmentSlots)i + " is null? " + (equipment[i] == null));
            if (equipment[i] != null)
            {
                //Debug.Log("Checking " + equipment[i].actorRefName);
                if (equipment[i].gearSet != null && !setsToEvaluate.Contains(equipment[i].gearSet))
                {
                    setsToEvaluate.Add(equipment[i].gearSet);
                }
            }
        }

        // Now go through each set and evaluate bonuses or lack thereof.

        foreach (GearSet gs in setsToEvaluate)
        {
            int numPieces = 0;
            for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
            {
                if (equipment[i] != null && equipment[i].gearSet == gs)
                {
                    //Debug.Log("We have " + numPieces + " set pieces now.");
                    numPieces++;
                }
            }

            if (gs.gearPieces.Count == numPieces)
            {
                // Max set
                GameMasterScript.gmsSingleton.statsAndAchievements.CompletedGearSet();
            }

            //Debug.Log("Have " + numPieces + " of set " + gs.refName);
            numPieces -= 2;
            for (int i = 0; i <= 3; i++)
            {
                // If we have TWO pieces of gear... The FIRST slot [0] in statusBonusRefs is the 2 piece bonus.
                // Second slot [1] is 3 pieces
                // Third slot [2] is 4 pieces
                // Fourth slot [3] is max.
                if (numPieces >= i)
                {
                    gs.CheckAndExecuteBonus(i + 2, true); // 2 piece bonus, 3 piece, etc. 0 based index.                    
                    //Debug.Log("Executing bonus " + (i + 2));
                }
                else
                {
                    // REMOVE bonus
                    gs.CheckAndExecuteBonus(i + 2, false); // 2 piece bonus, 3 piece, etc. 0 based index.

                    //Debug.Log("Removing " + (i + 2) + " piece bonus ");
                }
            }

            owner.SetActorData(gs.refName, numPieces + 2);
        }

    }

    public EquipmentSlots GetSlotOfEquippedItem(Equipment eq)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == eq)
            {
                return (EquipmentSlots)i;
            }
        }
        Debug.Log(eq.actorRefName + " is not equipped at all!");
        return EquipmentSlots.ANY;
    }
    
    private const float avgCritDamageBonusMult = 0.30f; //average of melee and ranged bonus    

    //This will look through the player's gear and see if something matches
    public static Equipment FindEquipmentToCompareAgainst(Equipment targetItem, bool bCheckAltSlot = false)
    {
        if (targetItem == null) // Why would this be happening
        {
            return null;
        }
        if (targetItem.slot == EquipmentSlots.WEAPON && bCheckAltSlot)
        {
            return GameMasterScript.heroPCActor.myEquipment.GetOffhand() as Equipment;
        }

        switch (targetItem.slot)
        {
            case EquipmentSlots.WEAPON:
                return GameMasterScript.heroPCActor.myEquipment.GetWeapon();
            case EquipmentSlots.ARMOR:
                return GameMasterScript.heroPCActor.myEquipment.GetArmor();
            case EquipmentSlots.EMBLEM:
                return GameMasterScript.heroPCActor.myEquipment.GetEmblem();
            case EquipmentSlots.OFFHAND:
                return GameMasterScript.heroPCActor.myEquipment.GetOffhand() as Equipment;
            case EquipmentSlots.ACCESSORY:
                return GameMasterScript.heroPCActor.myEquipment.equipment[(int)(bCheckAltSlot ? EquipmentSlots.ACCESSORY2 : EquipmentSlots.ACCESSORY)];
        }
        return null;
    }

    public static string CompareItems(Equipment oldItem, Equipment newItem, EquipmentSlots slot)
    {
        if (oldItem == null || newItem == null || oldItem == newItem)
        {
            return "";
        }
        string text = "";

        float[] oldElementalDamage = new float[(int)DamageTypes.COUNT];
        float[] newElementalDamage = new float[(int)DamageTypes.COUNT];
        float[] effectiveResistsOld = new float[(int)DamageTypes.COUNT];
        float[] effectiveResistsNew = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < 6; i++)
        {
            newElementalDamage[i] = 1f;
            oldElementalDamage[i] = 1f;
            effectiveResistsOld[i] = 1f;
            effectiveResistsNew[i] = 1f;
        }


        float[] oldStatMods = new float[(int)StatTypes.RANDOM_RESOURCE];
        float[] oldResistsOffset = new float[(int)DamageTypes.COUNT];
        float oldChangeCritChance = 0f;
        float oldChangeCritDamage = 0f;
        float oldAlterParryFlat = 0f;
        float oldAlterAccuracyFlat = 0f;
        ProcessItemStats(oldItem, oldElementalDamage, ref oldChangeCritDamage, oldStatMods, effectiveResistsOld, oldResistsOffset, ref oldChangeCritChance, ref oldAlterParryFlat, ref oldAlterAccuracyFlat);

        float[] newStatMods = new float[(int)StatTypes.RANDOM_RESOURCE];
        float[] newResistOffset = new float[(int)DamageTypes.COUNT];
        float newChangeCritChance = 0f;
        float newChangeCritDamage = 0f;
        float newAlterParryFlat = 0f;
        float newAlterAccuracyFlat = 0f;
        ProcessItemStats(newItem, newElementalDamage, ref newChangeCritDamage, newStatMods, effectiveResistsNew, newResistOffset, ref newChangeCritChance, ref newAlterParryFlat, ref newAlterAccuracyFlat);

        float critChanceModOld = GameMasterScript.heroPCActor.cachedBattleData.critChanceMod;
        float critDamageModOld = GameMasterScript.heroPCActor.cachedBattleData.critDamageMod;

        float critChanceModNew = critChanceModOld - oldChangeCritChance + newChangeCritChance;
        float critDamageModNew = critDamageModOld - oldChangeCritDamage + newChangeCritDamage;


        foreach (var equipment in GameMasterScript.heroPCActor.myEquipment.equipment)
        {
            if (equipment == oldItem ||
                equipment == null)
            {
                continue;
            }
            foreach (ResistanceData resist in equipment.resists)
            {
                effectiveResistsOld[(int)resist.damType] *= resist.multiplier;
                effectiveResistsNew[(int)resist.damType] *= resist.multiplier;
                oldResistsOffset[(int)resist.damType] += resist.flatOffset;
                newResistOffset[(int)resist.damType] += resist.flatOffset;
            }
            foreach (var effect in equipment.mods.SelectMany(mod => mod.modEffects).SelectMany(statEff => statEff.listEffectScripts).OfType<AlterBattleDataEffect>())
            {
                AddBattleDataElementalDamage(effect, oldElementalDamage);
                AddBattleDataElementalDamage(effect, newElementalDamage);
            }
        }
        float[] effectiveStatsOld = new float[(int)StatTypes.RANDOM_RESOURCE], effectiveStatsNew = new float[(int)StatTypes.RANDOM_RESOURCE];
        for (int i = 0; i < (int)StatTypes.RANDOM_RESOURCE; i++)
        {
            effectiveStatsOld[i] = GameMasterScript.heroPCActor.myStats.GetCurStat((StatTypes)i);
            effectiveStatsNew[i] = effectiveStatsOld[i] - oldStatMods[i] + newStatMods[i];
        }

        //Offense
        float oldWeaponPower = CalculateWeaponPower(oldItem as Weapon ?? GameMasterScript.heroPCActor.myEquipment.GetWeapon(),
            effectiveStatsOld, critChanceModOld, critDamageModOld, oldElementalDamage);
        float newWeaponPower = CalculateWeaponPower(newItem as Weapon ?? GameMasterScript.heroPCActor.myEquipment.GetWeapon(),
            effectiveStatsNew, critChanceModNew, critDamageModNew, newElementalDamage);

        float damageChange = (float)Math.Round((1f - newWeaponPower / oldWeaponPower) * 100f, 1) * -1.0f;

        if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(newItem) && GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(oldItem))
        {
            damageChange = 0f;
        }

        string dmgStr = StringManager.GetString("misc_generic_damage");

        if (StringManager.gameLanguage != EGameLanguage.de_germany)
        {
            dmgStr = dmgStr.ToLowerInvariant();
        }

        if (Math.Abs(damageChange) >= 0.1f)
        {
            text = string.Concat(text, "\n", GetTextPrefix(damageChange), Math.Abs(damageChange), StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + dmgStr + " </color>");
        }

        //Defense
        float oldDefense = CalculateDefense(oldItem, effectiveResistsOld, effectiveStatsOld, oldResistsOffset, oldAlterParryFlat, oldAlterAccuracyFlat);
        float newDefense = CalculateDefense(newItem, effectiveResistsNew, effectiveStatsNew, newResistOffset, newAlterParryFlat, newAlterAccuracyFlat);
        float absoluteChange = (float)Math.Round(Mathf.Abs(newDefense - oldDefense), 1);
        if (absoluteChange >= 0.1f)
        {
            text = string.Concat(text, "\n", GetTextPrefix(newDefense - oldDefense), absoluteChange, StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_generic_defense") + " </color>");
        }
        return text;
    }

    private static string GetTextPrefix(float change)
    {
        return Math.Sign(change) == -1 ? "<color=red>-" : "<color=#61e029>+";
    }

    private static void ProcessItemStats(Equipment item, float[] elementalDamage, ref float changeCritDamage, float[] statMods,
        float[] effectiveResists, float[] resistOffset, ref float changeCritChance, ref float alterParryFlat, ref float alterAccuracyFlat)
    {
        foreach (EffectScript current7 in item.mods.SelectMany(mod => mod.modEffects).SelectMany(statEff => statEff.listEffectScripts))
        {
            ProcessItemMods(current7, elementalDamage, ref changeCritDamage, statMods, ref changeCritChance, ref alterParryFlat, ref alterAccuracyFlat);
        }

        Armor ar = item as Armor;
        if (ar != null)
        {
            alterAccuracyFlat += ar.extraDodge;
        }

        //alterAccuracyFlat += (item as Armor)?.extraDodge ?? 0;

        foreach (ResistanceData resist in item.resists)
        {
            effectiveResists[(int)resist.damType] = resist.multiplier;
            resistOffset[(int)resist.damType] = resist.flatOffset;
        }
    }


    private static float CalculateDefense(Equipment item, float[] resists, float[] effectiveStats,
            float[] resistOffset, float alterParryFlat, float alterAccuracyFlat)
    {
        float defense = 0f;

        for (int l = 1; l < (int)DamageTypes.COUNT; l++)
        {
            defense += resists[l] * (1f - effectiveStats[(int)StatTypes.DISCIPLINE] * (StatBlock.DISCIPLINE_PERCENT_ELEMRESIST_MOD * 0.01f));
        }

        //Assume 50% of incoming damage is elemental, spread evenly across all elements
        defense /= (((int)DamageTypes.COUNT - 1) * 2);

        //And the other 50% is physical
        defense += (resists[(int)DamageTypes.PHYSICAL] * (1f - effectiveStats[(int)StatTypes.STRENGTH] * (StatBlock.STRENGTH_PERCENT_PHYSICALRESIST_MOD * 0.01f)) / 2);

        defense = (1f - defense) * 100f;
        if (item.itemType == ItemTypes.OFFHAND)
        {
            Offhand offhand2 = item as Offhand;
            defense += offhand2.blockChance * (1 - offhand2.blockDamageReduction) * 100f;
        }

        defense += resistOffset[(int)DamageTypes.PHYSICAL] / 2;
        for (int n = 1; n < (int)DamageTypes.COUNT; n++)
        {
            defense += resistOffset[n] / (((int)DamageTypes.COUNT - 1) * 2);
        }
        Weapon weapon = item as Weapon;
        if (weapon != null && weapon.weaponType == WeaponTypes.SWORD)
        {
            alterParryFlat += 0.03f;
        }
        defense += alterParryFlat * 100f + alterAccuracyFlat + effectiveStats[(int)StatTypes.GUILE] * 0.2f;
        return defense;
    }

    private static float CalculateWeaponPower(Weapon weapon, float[] effectiveStats, float critChanceMod, float critDamageMod, float[] elementalDamage)
    {
        float critChance = effectiveStats[(int)StatTypes.GUILE] * (StatBlock.GUILE_PERCENT_CRITCHANCE_MOD * 0.01f) + critChanceMod;
        float critDamageBonusMult = EquipmentBlock.avgCritDamageBonusMult + (effectiveStats[(int)StatTypes.SWIFTNESS] * 0.01f) + critDamageMod;
        float baseWeaponPower = GetWeaponBasePower(weapon, GameMasterScript.heroPCActor, effectiveStats);
        float weaponPower = baseWeaponPower + baseWeaponPower * critChance * critDamageBonusMult;
        weaponPower *= (elementalDamage[(int)weapon.damType]);
        return weaponPower;
    }

    //This function would ideally be shared with Fighter.CalculateBattleData to guaranteee the formula stay in sync
    private static float GetWeaponBasePower(Weapon weapon, Fighter fighter, float[] effectiveStats)
    {
        float weaponPower = weapon.power;
        if (fighter.myStats.CheckHasStatusName("status_unarmedfighting1"))
        {
            if (fighter.myEquipment.IsDefaultWeapon(weapon))
            {
                weaponPower = CombatManagerScript.CalculateBudokaWeaponPower(fighter, 1);
            }
        }
        float stat;
        if (weapon.weaponType == WeaponTypes.STAFF)
        {
            stat = (effectiveStats[(int)StatTypes.SPIRIT] + effectiveStats[(int)StatTypes.DISCIPLINE]) / 2f;
        }
        else if (weapon.weaponType == WeaponTypes.DAGGER)
        {
            stat = (effectiveStats[(int)StatTypes.GUILE] + effectiveStats[(int)StatTypes.STRENGTH]) / 2f;
        }
        else
        {
            stat = effectiveStats[(int)(weapon.isRanged ? StatTypes.SWIFTNESS : StatTypes.STRENGTH)];
        }
        weaponPower *= (1f + stat / 100);
        weaponPower += fighter.myStats.GetLevel() * 2; //TODO: use constants here. I would but I have no idea what they represent.
        weaponPower = weaponPower + weaponPower * (fighter.myStats.GetLevel() * 5f) / 100f;
        return weaponPower;
    }


    private static void ProcessItemMods(EffectScript effect, float[] elementalDamage, ref float changeCritDamage, float[] statMods, ref float changeCritChance, ref float alterParryFlat, ref float alterAccuracyFlat)
    {
        switch (effect.effectType)
        {
            case EffectType.ALTERBATTLEDATA:
                AlterBattleDataEffect alterBattleDataEffect = effect as AlterBattleDataEffect;
                AddBattleDataElementalDamage(alterBattleDataEffect, elementalDamage);
                changeCritDamage += alterBattleDataEffect.changeCritDamage;
                changeCritChance += alterBattleDataEffect.changeCritChance;
                break;
            case EffectType.CHANGESTAT:
                ChangeStatEffect changeStatEffect = effect as ChangeStatEffect;
                statMods[(int)changeStatEffect.stat] += changeStatEffect.effectPower; // this was just an EQUALS before, which meant only one mod was being processed!!!!
                statMods[(int)changeStatEffect.stat] += changeStatEffect.baseAmount; // could be stored here now due to deprecation of old effectequation system
                break;
            case EffectType.ATTACKREACTION:
                AttackReactionEffect attackReactionEffect = effect as AttackReactionEffect;
                alterParryFlat += attackReactionEffect.alterParryFlat;
                alterAccuracyFlat += (-1f * attackReactionEffect.alterAccuracyFlat);
                break;
        }
    }


    private static void AddBattleDataElementalDamage(AlterBattleDataEffect effect, float[] elementalDamage)
    {
        elementalDamage[(int)DamageTypes.FIRE] += effect.changeFireDamage;
        elementalDamage[(int)DamageTypes.PHYSICAL] += effect.changePhysicalDamage;
        elementalDamage[(int)DamageTypes.WATER] += effect.changeWaterDamage;
        elementalDamage[(int)DamageTypes.LIGHTNING] += effect.changeLightningDamage;
        elementalDamage[(int)DamageTypes.SHADOW] += effect.changeShadowDamage;
        elementalDamage[(int)DamageTypes.POISON] += effect.changePoisonDamage;
    }

    public string GetWeaponDurabilityDisplay(Weapon wp)
    {
        //Weapon wp = GetWeapon();
        string duraText = wp.curDurability + "/" + wp.maxDurability;
        if (wp.maxDurability == 0)
        {
            //return "∞/∞";
            return duraText;
        }
        else
        {
            float percentOfMax = (float)wp.curDurability / (float)wp.maxDurability; // 5/10 = 0.5
            if ((percentOfMax >= 0.25f) && (percentOfMax <= 0.5f))
            {
                duraText = "<color=yellow>" + duraText + "</color>";
            }
            if (percentOfMax <= 0.25f)
            {
                duraText = "<color=red>" + duraText + "</color>";
            }
        }
        return duraText;
    }

    public static void MakeMagicalFromMod(Item item, MagicMod mod, bool recolorText, bool upgradeRawPower, bool allowLegendaries, bool rebuildDisplayName = true)
    {
        if (item.mods != null)
        {
            for (int i = 0; i < item.mods.Count; i++)
            {
                if (item.mods[i].refName == mod.refName)
                {
                    // Don't double up on mods.
                    Debug.Log("Item " + item.actorRefName + " already has mod " + mod.refName);
                    return;
                }
            }
        }

        if (item.itemType == ItemTypes.WEAPON)
        {
            Weapon w = item as Weapon;
            if (mod.refName == "mm_lightweight" && (w.twoHanded || w.weaponType == WeaponTypes.CLAW))
            {
                Debug.Log("Cannot make " + w.actorRefName + " lightweight!");
                return;
            }
        }

        MagicMod newMod = new MagicMod();
        newMod.CopyFromMod(mod);
        item.AddMod(newMod, true);
        int numMods = item.mods.Count;

        for (int i = 0; i < item.mods.Count; i++)
        {
            if (item.mods[i].noNameChange || item.legendary)
            {
                //Debug.Log(item.mods[i].refName + " no name change for " + item.displayName);
                numMods--;
            }
        }

        if (numMods == 0 || item.legendary)
        {
            recolorText = false;
        }

        if (rebuildDisplayName)
        {
            item.RebuildDisplayName();
        }

        if (recolorText)
        {
            if (!upgradeRawPower) return;

            if (item.timesUpgraded <= Equipment.GetMaxUpgrades())
            {
                item.IncreasePowerFromRarityBoost(mod);
            }
        }
    }

    public string GetItemSpriteRef(EquipmentSlots slot)
    {
        Equipment eq = equipment[(int)slot];
        if (eq == null)
        {
            return "";
        }
        else
        {
            return eq.spriteRef;
        }
    }

    public static MagicMod MakeMagicalFromModFlag(Equipment item, MagicModFlags modFlag, bool allowLegendaries, string exclude = "")
    {
        
        CheckForPoolingListsInitialized();
        possibleModFlagMods.Clear();
        foreach (MagicMod mm in GameMasterScript.dictMagicModsByFlag[modFlag])
        {
            if (item.IsModValidForMe(mm) == MagicModCompatibility.POSSIBLE && mm.refName != exclude)
            {
                possibleModFlagMods.Add(mm);
            }
        }

        MagicMod modAdded = null;

        if (possibleModFlagMods.Count == 0)
        {
#if UNITY_EDITOR
            //Debug.Log("WARNING: No possible " + modFlag + " mods for " + item.actorRefName + ", using any old mod instead.");
#endif
            modAdded = MakeMagical(item, item.challengeValue, allowLegendaries, true, exclude);
        }
        else
        {
            modAdded = possibleModFlagMods[UnityEngine.Random.Range(0, possibleModFlagMods.Count)];
            MakeMagicalFromMod(item, modAdded, true, true, allowLegendaries);
        }

        return modAdded;
    }

    // Returns the mod that was used.
    public static MagicMod MakeMagical(Item item, float challengeValue, bool allowLegendaries, bool rebuildDisplayName = true, string exclude = "")
    {
        //Shep: This function does nothing to items that aren't equipment.
        Equipment eq = item as Equipment;
        if (eq == null)
        {
            return null;
        }

        if (item.legendary && !allowLegendaries) return null;
        ItemTypes itemType = item.itemType;

        CheckForPoolingListsInitialized();

        possibleMods.Clear();
        exclusions.Clear();

        //wat
        if (challengeValue > 50f)
        {
            challengeValue = 1.0f;
        }

        if (item.mods != null)
        {
            for (int i = 0; i < item.mods.Count; i++)
            {
                if (item.mods[i].exclusionGroup > 0)
                {
                    exclusions.Add(item.mods[i].exclusionGroup);
                }
            }
        }
        foreach (MagicMod mm in GameMasterScript.listModsSortedByChallengeRating) // GameMasterScript.masterMagicModList.Values)
        {
            if (mm.lucidOrbsOnly) continue;
            if (challengeValue < mm.challengeValue) // mm.challengeValue is the MINIMUM value.
            {
                //we're good -- all the ones ahead of us are also out of range
                break;
            }
            if (mm.challengeValue >= 500f)
            {
                continue;
            }
            else if (challengeValue > mm.maxChallengeValue)
            {
                continue;
            }
            else
            {
                if (SharaModeStuff.IsSharaModeActive() && SharaModeStuff.disallowSharaModeMagicMods.Contains(mm.refName))
                {
                    continue;
                }


                if (itemType == ItemTypes.WEAPON)
                {
                    Weapon wp = item as Weapon;
                    if ((wp.twoHanded || wp.weaponType == WeaponTypes.CLAW) && mm.refName == "mm_lightweight")
                    {
                        continue;
                    }
                    if (mm.modFlags[(int)MagicModFlags.MELEE] && wp.isRanged)
                    {
                        continue;
                    }
                    if (mm.modFlags[(int)MagicModFlags.BOW] && (!wp.isRanged || wp.range < 3))
                    {
                        continue;
                    }
                    if (mm.modFlags[(int)MagicModFlags.ONLY2HMELEE] && (wp.isRanged || !wp.twoHanded))
                    {
                        continue;
                    }
                }

                if (eq.slot == mm.slot || mm.slot == EquipmentSlots.ANY)
                {
                    //Debug.Log("Adding mod " + mm.modName + " to " + item.displayName + " which is slot " + eq.slot.ToString() + " mm slot " + mm.slot.ToString());
                    // This is possible.
                    if (!item.HasModByRef(mm.refName))
                    {
                        if (mm.exclusionGroup == 0 || !exclusions.Contains(mm.exclusionGroup))
                        {
                            if (eq.slot == EquipmentSlots.OFFHAND)
                            {
                                Offhand oh = eq as Offhand;
                                if (oh.allowBow)
                                {
                                    // Quiver.
                                    if (((mm.modFlags[(int)MagicModFlags.SHIELD]) || (mm.modFlags[(int)MagicModFlags.BOOK])) && (!(mm.modFlags[(int)MagicModFlags.QUIVER])))
                                    {
                                        continue;
                                    }
                                }
                                else if (oh.blockChance > 0)
                                {
                                    // Shield
                                    if (((mm.modFlags[(int)MagicModFlags.QUIVER]) || (mm.modFlags[(int)MagicModFlags.BOOK])) && (!(mm.modFlags[(int)MagicModFlags.SHIELD])))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Magic book
                                    if (((mm.modFlags[(int)MagicModFlags.SHIELD]) || (mm.modFlags[(int)MagicModFlags.QUIVER])) && (!(mm.modFlags[(int)MagicModFlags.BOOK])))
                                    {
                                        continue;
                                    }
                                }
                            }

                            if (mm.refName == exclude)
                            {
                                continue;
                            }

                            possibleMods.Add(mm);

                        }


                    }
                }
            }
        }

        if (possibleMods.Count > 0)
        {
            MagicMod mod = possibleMods[UnityEngine.Random.Range(0, possibleMods.Count)];
            // Never recolor/change text if it's a legendary item, right??
            MakeMagicalFromMod(item, mod, true, true, allowLegendaries, rebuildDisplayName);
            return mod;
            // Use newMod going forward.
        }

        return null; // Nothing happened.
    }

    public EquipmentBlock()
    {
        equipment = new Equipment[(int)EquipmentSlots.COUNT];
        for (int i = 0; i < equipment.Length; i++)
        {
            equipment[i] = null;
        }
    }

    public WeaponTypes GetOffhandWeaponType()
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null)
        {
            return WeaponTypes.NATURAL;
        }
        else if (equipment[(int)EquipmentSlots.OFFHAND].itemType != ItemTypes.WEAPON)
        {
            return WeaponTypes.NATURAL;
        }
        else
        {
            Weapon w = equipment[(int)EquipmentSlots.OFFHAND] as Weapon;
            return w.weaponType;
        }
    }

    public DamageTypes GetWeaponElement()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            return DamageTypes.PHYSICAL;
        }
        else
        {
            Weapon wp = equipment[(int)EquipmentSlots.WEAPON] as Weapon;
            return wp.damType;
        }
    }

    public WeaponTypes GetWeaponType()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            return WeaponTypes.NATURAL;
        }
        else
        {
            Weapon wp = equipment[(int)EquipmentSlots.WEAPON] as Weapon;
            return wp.weaponType;
        }
    }

    public Weapon GetWeapon()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            if (defaultWeapon == null)
            {
                //Debug.Log(owner.actorRefName + " has no weapon and no default weapon");
            }
            return defaultWeapon;
        }
        return equipment[(int)EquipmentSlots.WEAPON] as Weapon;
    }

    public int GetWeaponRange()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            return 1;
        }
        return ((Weapon)equipment[(int)EquipmentSlots.WEAPON]).range;
    }

    public Equipment GetOffhandItem()
    {
        Equipment eq = equipment[(int)EquipmentSlots.OFFHAND];
        if (eq != null)
        {
            if ((eq.actorRefName == "") || (eq.actorRefName == null))
            {
                SetEquipment(null, EquipmentSlots.OFFHAND, true);
                return null;
            }
            return eq;
        }
        else
        {
            return null;
        }

    }

    public Weapon GetOffhandWeapon()
    {
        Equipment eq = equipment[(int)EquipmentSlots.OFFHAND];
        // This shouldn't be necessary. Don't know why offhand is getting bad data written to it.
        if (eq != null)
        {
            if (string.IsNullOrEmpty(eq.actorRefName))
            {
                SetEquipment(null, EquipmentSlots.OFFHAND, true);
                return null;
            }
            if (eq.itemType == ItemTypes.WEAPON)
            {
                return (Weapon)eq as Weapon;
            }
        }

        return null;
    }

    public Equipment GetOffhand()
    {
        return equipment[(int)EquipmentSlots.OFFHAND];
    }

    public string GetOffhandName()
    {
        return equipment[(int)EquipmentSlots.OFFHAND] == null ? StringManager.GetString("eq_slot_offhand") + ": " + StringManager.GetString("misc_nothing") : equipment[(int)EquipmentSlots.OFFHAND].displayName;
    }

    public string GetOffhandRefName()
    {
        return equipment[(int)EquipmentSlots.OFFHAND] == null ? "" : equipment[(int)EquipmentSlots.OFFHAND].actorRefName;
    }

    public string GetWeaponName()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            return StringManager.GetString("eq_slot_weapon") + ": " + StringManager.GetString("misc_nothing");
        }
        else
        {
            return equipment[(int)EquipmentSlots.WEAPON].displayName;
        }
    }

    public string GetWeaponRefName()
    {
        if (equipment[(int)EquipmentSlots.WEAPON] == null)
        {
            return "";
        }
        else
        {
            return equipment[(int)EquipmentSlots.WEAPON].actorRefName;
        }
    }

    public string GetGearNameForSlot(EquipmentSlots es)
    {
        switch (es)
        {
            case EquipmentSlots.ACCESSORY:
                return GetAccessoryName(0);
            case EquipmentSlots.ACCESSORY2:
                return GetAccessoryName(1);
            case EquipmentSlots.ARMOR:
                return GetArmorName();
            case EquipmentSlots.EMBLEM:
                return GetEmblemName();
            case EquipmentSlots.OFFHAND:
                return GetOffhandName();
        }

        return "";
    }

    public string GetEmblemName()
    {
        if (equipment[(int)EquipmentSlots.EMBLEM] == null)
        {
            return StringManager.GetString("eq_slot_emblem") + ": " + StringManager.GetString("misc_nothing");
        }
        else
        {
            return equipment[(int)EquipmentSlots.EMBLEM].displayName;
        }
    }

    public int GetEmblemLevel()
    {
        if (GetEmblem() == null) return 0;
        else
        {
            return (GetEmblem().emblemLevel + 1); 
        }
    }

    public Emblem GetEmblem()
    {
        return equipment[(int)EquipmentSlots.EMBLEM] as Emblem;
    }

    public string GetAccessoryName(int slot)
    {
        EquipmentSlots eSlot = EquipmentSlots.ACCESSORY;
        if (slot == 1)
        {
            eSlot = EquipmentSlots.ACCESSORY2;
        }
        if (equipment[(int)eSlot] == null)
        {
            return StringManager.GetString("eq_slot_accessory") + " " + (slot + 1) + ": " + StringManager.GetString("misc_nothing");
        }
        else
        {
            return equipment[(int)eSlot].displayName;
        }
    }

    public string GetArmorName()
    {
        if (equipment[(int)EquipmentSlots.ARMOR] == null)
        {
            return StringManager.GetString("eq_slot_armor_plural") + ": " + StringManager.GetString("misc_nothing");
        }
        else
        {
            return equipment[(int)EquipmentSlots.ARMOR].displayName;
        }
    }

    public Equipment GetEquipmentInSlot(EquipmentSlots slot)
    {
        //not always as simple as just returning from the array,
        //because there are preconditions for weapons -- such as 
        //null weapon actually meaning defaultweapon / fists in some cases.
        switch (slot)
        {
            case EquipmentSlots.WEAPON:
                return GetWeapon();
            case EquipmentSlots.OFFHAND:
                return GetOffhand();
            case EquipmentSlots.ARMOR:
                return GetArmor();
            case EquipmentSlots.EMBLEM:
                return GetEmblem();
            case EquipmentSlots.ACCESSORY:
            case EquipmentSlots.ACCESSORY2:
                return equipment[(int)slot];
            case EquipmentSlots.ANY:
                return GetEquipmentInSlot((EquipmentSlots)UnityEngine.Random.Range(0, (int)EquipmentSlots.ANY));
        }

        return null;
    }

    public void SetEquipment(Equipment eq, EquipmentSlots slot, bool forceNull)
    {
        if (!forceNull)
        {
            if (eq == null)
            {
                Debug.Log("WARNING: " + owner.actorRefName + " trying to set null equipment to " + slot);
                return;
            }
            if (eq.actorRefName == null)
            {
                //Debug.Log("WARNING: " + owner.actorRefName + " trying to set garbage equipment to " + slot);
                return;
            }
        }
        else
        {
            if (eq != null)
            {
                Debug.Log("Not properly nulling gear for " + owner.actorRefName);
            }
        }
        equipment[(int)slot] = eq;

        /* if (eq == null) Debug.Log("ACTUALLY SETTING null equipment to slot " + slot);
        else Debug.Log("ACTUALLY SETTING equipment " + eq.actorRefName + " to slot " + slot);
        */
    }

    public Armor GetArmor()
    {
        if (equipment[(int)EquipmentSlots.ARMOR] == null) return null;
        if (equipment[(int)EquipmentSlots.ARMOR].itemType != ItemTypes.ARMOR)
        {
            Debug.Log(owner.actorRefName + "'s armor " + equipment[(int)EquipmentSlots.ARMOR].actorRefName + " not actually armor? " + equipment[(int)EquipmentSlots.ARMOR].itemType);
            SetEquipment(null, EquipmentSlots.ARMOR, true);
        }
        return (Armor)equipment[(int)EquipmentSlots.ARMOR];
    }

    public ArmorTypes GetArmorType()
    {
        if (GetArmor() == null)
        {
            return ArmorTypes.NATURAL;
        }
        else
        {
            return GetArmor().armorType;
        }
    }

    public static void SetItemDreamPropertyStrings()
    {
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.ELEM_FIRE] = StringManager.GetString("itemdream_property_fire");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.ELEM_LIGHTNING] = StringManager.GetString("itemdream_property_lightning");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.ELEM_SHADOW] = StringManager.GetString("itemdream_property_shadow");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.ELEM_POISON] = StringManager.GetString("itemdream_property_poison");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.ELEM_WATER] = StringManager.GetString("itemdream_property_water");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_RANGED] = StringManager.GetString("itemdream_property_ranged");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_MELEEBOOST] = StringManager.GetString("itemdream_property_melee");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_LEGENDARY] = StringManager.GetString("itemdream_property_legendary");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_HEALTH] = StringManager.GetString("itemdream_property_health");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_GILDED] = StringManager.GetString("itemdream_property_gilded");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_GEARSET] = StringManager.GetString("itemdream_property_gearset");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_CRITICAL] = StringManager.GetString("itemdream_property_critical");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_DENSE] = StringManager.GetString("itemdream_property_dense");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_NOCHAMPIONS] = StringManager.GetString("itemdream_property_nochampions");
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_MORECHAMPIONS] = StringManager.GetString("itemdream_property_morechampions");


        StringManager.SetTag(0, Monster.familyNamesVerbose["jelly"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_JELLIES] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["bandits"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_BANDITS] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["beasts"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_BEASTS] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["hybrids"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_HYBRIDS] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["insects"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_INSECTS] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["robots"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_ROBOTS] = StringManager.GetString("itemdream_property_monstertype");
        StringManager.SetTag(0, Monster.familyNamesVerbose["frogs"]);
        itemWorldPropertiesVerbose[(int)ItemWorldProperties.TYPE_FAMILY_FROGS] = StringManager.GetString("itemdream_property_monstertype");

    }

    public static void InitStaticArrays()
    {
        itemTypeNames = new string[(int)ItemTypes.COUNT];
        equipmentSlotNames = new string[(int)EquipmentSlots.COUNT];
        damageTypeNames = new string[(int)DamageTypes.COUNT];
        itemWorldPropertiesVerbose = new string[(int)ItemWorldProperties.COUNT];
        itemWorldPropertyRewardMultiplier = new float[(int)ItemWorldProperties.COUNT];

        itemTypeNames[(int)ItemTypes.ARMOR] = StringManager.GetString("eq_slot_armor");
        itemTypeNames[(int)ItemTypes.CONSUMABLE] = StringManager.GetString("itemtype_consumable");
        itemTypeNames[(int)ItemTypes.WEAPON] = StringManager.GetString("eq_slot_weapon");
        itemTypeNames[(int)ItemTypes.ACCESSORY] = StringManager.GetString("eq_slot_accessory");
        itemTypeNames[(int)ItemTypes.OFFHAND] = StringManager.GetString("eq_slot_offhand");
        itemTypeNames[(int)ItemTypes.EMBLEM] = StringManager.GetString("eq_slot_emblem");

        equipmentSlotNames[(int)EquipmentSlots.WEAPON] = StringManager.GetString("eq_slot_weapon").ToUpperInvariant();
        equipmentSlotNames[(int)EquipmentSlots.OFFHAND] = StringManager.GetString("eq_slot_offhand").ToUpperInvariant();
        equipmentSlotNames[(int)EquipmentSlots.ARMOR] = StringManager.GetString("eq_slot_armor").ToUpperInvariant();
        equipmentSlotNames[(int)EquipmentSlots.ACCESSORY] = StringManager.GetString("eq_slot_accessory1").ToUpperInvariant();
        equipmentSlotNames[(int)EquipmentSlots.ACCESSORY2] = StringManager.GetString("eq_slot_accessory2").ToUpperInvariant();
        equipmentSlotNames[(int)EquipmentSlots.EMBLEM] = StringManager.GetString("eq_slot_emblem").ToUpperInvariant();

        damageTypeNames[(int)DamageTypes.PHYSICAL] = StringManager.GetString("misc_dmg_physical");
        damageTypeNames[(int)DamageTypes.FIRE] = StringManager.GetString("misc_dmg_fire");
        damageTypeNames[(int)DamageTypes.POISON] = StringManager.GetString("misc_dmg_poison");
        damageTypeNames[(int)DamageTypes.WATER] = StringManager.GetString("misc_dmg_water");
        damageTypeNames[(int)DamageTypes.LIGHTNING] = StringManager.GetString("misc_dmg_lightning");
        damageTypeNames[(int)DamageTypes.SHADOW] = StringManager.GetString("misc_dmg_shadow");

        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.ELEM_FIRE] = 0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.ELEM_LIGHTNING] = 0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.ELEM_SHADOW] = 0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.ELEM_POISON] = 0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.ELEM_WATER] = 0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_RANGED] = 0.075f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_MELEEBOOST] = 0.15f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_LEGENDARY] = 0.2f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_GEARSET] = -0.1f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_HEALTH] = 0.15f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_GILDED] = -0.2f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_CRITICAL] = 0.15f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_MORECHAMPIONS] = 0.25f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_NOCHAMPIONS] = -0.4f;
        itemWorldPropertyRewardMultiplier[(int)ItemWorldProperties.TYPE_DENSE] = 0.33f;
    }

    public bool CheckBroken(Weapon weap)
    {
        if ((weap.curDurability <= 0) && (weap.maxDurability > 0))
        {
            // Weapon is broken.
            if (equipment[(int)EquipmentSlots.WEAPON] == weap)
            {
                Unequip(EquipmentSlots.WEAPON, true, SND.PLAY, true);
                UIManagerScript.RemoveWeaponFromActives(weap);
            }
            else
            {
                Unequip(EquipmentSlots.OFFHAND, true, SND.PLAY, true);
            }

            weap.collection.RemoveItem(weap);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void CheckEquipItemAbilities(Item item, StatusTrigger trigger)
    {
        // Maybe other item types should be able to have abilities also?

        Equipment eq = item as Equipment;
        foreach (AbilityScript abil in eq.addAbilities)
        {
            if (trigger == StatusTrigger.ONADD)
            {
                if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abil.refName))
                {
                    // Player does not know this ability. Make a copy and learn it.
                    AbilityScript learned = GameMasterScript.heroPCActor.LearnAbility(abil, false, autoActivatePassive: true, learnFromJob: true, verboseMessage: true);
                    learned.SetCurCooldownTurns(item.cooldownTurnsRemaining);
                }
            }
            if (trigger == StatusTrigger.ONREMOVE)
            {
                if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abil.refName))
                {
                    AbilityScript removed = GameMasterScript.heroPCActor.myAbilities.RemoveAbility(abil.refName);
                    if (removed != null)
                    {
                        item.cooldownTurnsRemaining = removed.GetCurCooldownTurns();
                    }
                }
            }
        }

        // Magic mods can grant abilities too.
        foreach (MagicMod mm in eq.mods)
        {
            foreach (AbilityScript abil in mm.addAbilities)
            {
                if (trigger == StatusTrigger.ONADD)
                {
                    if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abil.refName))
                    {
                        // Player does not know this ability. Make a copy and learn it.

                        AbilityScript learned = GameMasterScript.heroPCActor.LearnAbility(abil, false, true);
                        learned.SetCurCooldownTurns(item.cooldownTurnsRemaining);

                    }
                }
                if (trigger == StatusTrigger.ONREMOVE)
                {
                    if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abil.refName))
                    {
                        AbilityScript removed = GameMasterScript.heroPCActor.myAbilities.RemoveAbility(abil.refName);
                        if (removed != null)
                        {
                            item.cooldownTurnsRemaining = removed.GetCurCooldownTurns();
                            UIManagerScript.TryRemoveAbilityFromHotbar(removed);
                        }
                    }
                }

            }
        }

        // If gear is changing, we DO want to actually alter cooldowns here.
        if (owner.GetActorType() == ActorTypes.HERO) UIManagerScript.singletonUIMS.RefreshAbilityCooldowns(true);


    }

    private void CheckEquipItemMods(Item item, StatusTrigger trigger)
    {
        if (Debug.isDebugBuild)
        {
            if (owner == null)
            {
                Debug.Log("ERROR! NO OWNER FOR ITEM " + item.actorRefName);
                return;
            }
        }
        foreach (MagicMod mod in item.mods)
        {
            if (owner.GetActorType() == ActorTypes.HERO)
        {
            for (int i = 0; i < mod.adventureStats.Length; i++)
            {
                if (mod.adventureStats[i] != 0)
                {
                    if (trigger == StatusTrigger.ONADD)
                    {
                        GameMasterScript.heroPCActor.advStats[i] += mod.adventureStats[i];
                    }
                    else
                    {
                        GameMasterScript.heroPCActor.advStats[i] -= mod.adventureStats[i];
                        }
                    }
                }
            }

            foreach (StatusEffect modEffect in mod.modEffects)
            {
                foreach (EffectScript eff in modEffect.listEffectScripts)
                {
                    eff.selfActor = owner;
                    eff.originatingActor = owner; // Should originating owner be the item itself? 
                }

                if (trigger == StatusTrigger.ONREMOVE)
                {
                    owner.myStats.RemoveStatusByRefAndSource(modEffect.refName, item.actorUniqueID, owner);
                    //owner.myStats.RemoveStatus(modEffect, false); // This should connect to the exact status effect granted by the item. Probably?
                }
                else
                {
                    // Adding it
                    if (item.itemType == ItemTypes.EMBLEM)
                    {
                        if (modEffect.refName.Contains("emblemwellrounded")) // Job exclusive mod!!
                        {
                            Emblem emb = item as Emblem;
                            if (emb.jobForEmblem != owner.myJob.jobEnum)
                            {
                                continue;
                            }
                        }
                    }
                    owner.myStats.AddStatus(modEffect, owner, item);
                }
            }
        }
    }

    private bool IsItemEquipped(Equipment item)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] != null)
            {
                //Debug.Log(item.uniqueID + " vs " + equipment[i].uniqueID);
                if ((item == equipment[i]) || (item.actorRefName == equipment[i].actorRefName))
                {
                    return true;
                }
            }

        }
        return false;
    }

    private int GetItemSlot(Equipment item)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] != null)
            {
                //Debug.Log(item.uniqueID + " vs " + equipment[i].uniqueID);
                if ((item == equipment[i]) || (item.actorRefName == equipment[i].actorRefName))
                {
                    return i;
                }
            }

        }
        return -1;
    }

    public void ClearGear()
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            SetEquipment(null, (EquipmentSlots)i, true);
        }
    }

    public bool IsOffhandQuiver()
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null) return false;
        if (equipment[(int)EquipmentSlots.OFFHAND].itemType != ItemTypes.OFFHAND) return false;
        Offhand oh = equipment[(int)EquipmentSlots.OFFHAND] as Offhand;
        if (oh.allowBow)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsOffhandShield()
    {
        if (equipment[(int)EquipmentSlots.OFFHAND] == null) return false;
        if (equipment[(int)EquipmentSlots.OFFHAND].itemType != ItemTypes.OFFHAND) return false;
        Offhand oh = equipment[(int)EquipmentSlots.OFFHAND] as Offhand;
        if (oh.allowBow)
        {
            return false;
        }
        else
        {
            if (oh.blockChance == 0) return false;
            return true;
        }
    }

    void EquipBestQuiver()
    {
        Offhand bestCandidate = null;
        Offhand secondBest = null;
        float bestChallengeValue = 0.0f;
        bool anyFavoriteFound = false;
        foreach (Item itm in owner.myInventory.GetInventory())
        {
            if (itm.itemType == ItemTypes.OFFHAND)
            {
                Offhand oh = itm as Offhand;
                if (oh.allowBow)
                {
                    if (oh.favorite)
                    {
                        anyFavoriteFound = true;
                        if (oh.challengeValue > bestChallengeValue)
                        {
                            bestCandidate = oh;
                            bestChallengeValue = oh.challengeValue;
                        }
                    }
                    else
                    {
                        if (anyFavoriteFound) continue;
                        if (bestCandidate == null)
                        {
                            if (oh.challengeValue > bestChallengeValue)
                            {
                                secondBest = oh;
                                bestChallengeValue = oh.challengeValue;
                            }
                        }
                    }
                }
            }
        }

        if (bestCandidate != null)
        {
            Equip(bestCandidate, SND.SILENT, 0, true);
        }
        else if (secondBest != null)
        {
            Equip(secondBest, SND.SILENT, 0, true);
        }
    }

    public bool EquipOnlyIfValid(Equipment itm, SND play, EquipmentSlots slotToEquipIn, bool showText, bool returnOldItemToInventory = true)
    {
        if (slotToEquipIn == EquipmentSlots.ACCESSORY2 || slotToEquipIn == EquipmentSlots.OFFHAND)
        {
            return EquipOnlyIfValid(itm, play, 1, showText, returnOldItemToInventory);
        }
        else
        {
            return EquipOnlyIfValid(itm, play, 0, showText, returnOldItemToInventory);
        }
    }

    public bool EquipOnlyIfValid(Equipment itm, SND play, int subSlot, bool showText, bool returnOldItemToInventory = true)
    {
        // Equipping armor, accessories: ok
        if (itm == null)
        {
            Debug.Log("Trying to equip a null item! Warning!");
            if (owner != null)
            {
                Debug.Log("Owner is " + owner.actorRefName + " " + owner.actorUniqueID);
            }
            return false;
        }
        if (itm.itemType != ItemTypes.OFFHAND && itm.itemType != ItemTypes.WEAPON)
        {
            return Equip(itm, play, subSlot, showText, returnOldItemToInventory);
        }
        // Equipping mainhands: ok
        if (itm.itemType == ItemTypes.WEAPON && subSlot == 0)
        {
            //Debug.Log("Equipping a weapon " + itm.actorRefName + " to mainhand.");
            return Equip(itm, play, subSlot, showText, returnOldItemToInventory);
        }

        // We are equipping something to offhand.. Hmm what are we doing

        if (GetWeapon().twoHanded)
        {
            if (itm.itemType != ItemTypes.OFFHAND) return false; // Can't equip weapons to OH while MH is 2h
            Offhand oh = itm as Offhand;
            if (GetWeaponType() == WeaponTypes.BOW && oh.allowBow)
            {
                // quivers are ok
                // proceed
            }
            else
            {
                // Unless it's quiver+bow, it's not ok
                return false;
            }
        }


        return Equip(itm, play, subSlot, showText, returnOldItemToInventory);

    }

    public bool Equip(Equipment itm, SND play, EquipmentSlots inThisSlot, bool showText, bool returnOldItemToInventory = true, bool equipPairedItemIfPossible = true)
    {
        switch (inThisSlot)
        {
            case EquipmentSlots.WEAPON:
            case EquipmentSlots.ARMOR:
            case EquipmentSlots.ACCESSORY:
            case EquipmentSlots.EMBLEM:
                return Equip(itm, play, 0, showText, returnOldItemToInventory);
            case EquipmentSlots.OFFHAND:
            case EquipmentSlots.ACCESSORY2:
                return Equip(itm, play, 1, showText, returnOldItemToInventory);
        }

        return false;
    }

    public bool Equip(Equipment item, SND play, int subSlot, bool showText, bool returnOldItemToInventory = true, bool equipPairedItemIfPossible = true)
    {
        //if (Debug.isDebugBuild) Debug.Log("Prepare to equip " + item.actorRefName + " slot " + subSlot + " return old to inv: " + returnOldItemToInventory + " equip pair? " + equipPairedItemIfPossible);
            if (item == null)
        {
            //Debug.Log("Can't equip null item");
            return false;
        }
        else
        {
            bool alreadyEquippedPairedOffhand = false;
            if (IsDefaultWeapon(item, onlyActualFists: false) && GetWeapon() != null)
            {
                Unequip(EquipmentSlots.WEAPON, false, SND.SILENT, false, returnOldItemToInventory);
                Unequip(EquipmentSlots.OFFHAND, false, SND.SILENT, false, returnOldItemToInventory);
                if (!alreadyEquippedPairedOffhand && equipPairedItemIfPossible)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Trying to equip the paired item");
                    GetWeapon().ValidatePairedItem();
                    TryEquipPairedItem(GetWeapon(), returnOldItemToInventory, true);
                }
            }
            if (item.itemType == ItemTypes.WEAPON && subSlot == 1)
            {
                Weapon w = item as Weapon;
                if (w.twoHanded)
                {
                    Debug.Log("Trying to equip 2h weapon to offhand somehow?");
                    return false;
                }
            }


            //if (Debug.isDebugBuild) Debug.Log("Attempting to equip " + item.displayName);
            if (item.itemType == ItemTypes.ACCESSORY)
            {
                if (((equipment[(int)EquipmentSlots.ACCESSORY] == item) && (subSlot == 0)) || ((equipment[(int)EquipmentSlots.ACCESSORY2] == item) && subSlot == 1))
                {
                    Debug.Log("Item " + item.displayName + " is identical to acc slot " + subSlot);
                    return false;
                }
                Accessory acc = (Accessory)item as Accessory;
                int isEquipped = GetItemSlot(item);
                int slotToEquip = (int)EquipmentSlots.ACCESSORY;
                if (subSlot == 1) slotToEquip = (int)EquipmentSlots.ACCESSORY2;
                if ((acc.uniqueEquip) && (isEquipped != -1) && (isEquipped != slotToEquip))
                {
                    GameLogScript.LogWriteStringRef("log_error_uniqueequip");
                    return false;
                }
            }
            //If we're equipping a weapon in the offhand slot
            else if (item.itemType == ItemTypes.WEAPON && subSlot == 1) // Dual wield check.
            {
                //If it is already IN the offhand slot, uh, cool? Do nothing.
                if (equipment[(int)EquipmentSlots.OFFHAND] == item)
                {
                    //ok ty
//                    if (Debug.isDebugBuild) Debug.Log("Item is already in offhand slot, do nothing.");
                    return false;
                }
            }
            else
            {
                if (equipment[(int)item.slot] == item)
                {
                    // Already equipped.
//                    if (Debug.isDebugBuild) Debug.Log("Already equipped in slot " + item.slot);
                    return false;
                }
            }
            EquipmentSlots eSlot = EquipmentSlots.ANY;

            //Main hand weapon, 1 or 2H
            if (item.itemType == ItemTypes.WEAPON && subSlot == 0)
            {
                eSlot = EquipmentSlots.WEAPON;

                //Find what we already have equipped, and if it is not our fists, unequip it.
                Item prevWeapon = equipment[(int)EquipmentSlots.WEAPON];
                if (prevWeapon != null && !IsDefaultWeapon(prevWeapon, onlyActualFists: true))
                {
                    Unequip(EquipmentSlots.WEAPON, false, play, true, returnOldItemToInventory);
                }

                Weapon wp = item as Weapon;

                //If we are not equipping a staff, remove spiritpower10mult
                if (wp.weaponType != WeaponTypes.STAFF)
                {
                    owner.myStats.RemoveAllStatusByRef("spiritpower10mult");
                }

                //Assume we'll check for the best offhand option.
                bool checkForBestOffhandOption = true;

                if (wp.twoHanded) // Can't equip a shield etc with a two handed weapon.
                {
                    Equipment currentOH = equipment[(int)EquipmentSlots.OFFHAND];

                    //If we're using a bow
                    if (wp.IsWeaponBow())
                    {
                        //and we have something in the offhand
                        if (currentOH != null)
                        {
                            //assume we won't remove it, 
                            bool removeOffhandForBow = false;
                            if (currentOH.itemType == ItemTypes.OFFHAND)
                            {
                                //but if it does not allow us to keep using it with a bow
                                Offhand cOH = currentOH as Offhand;
                                if (!cOH.allowBow)
                                {
                                    //we don't want it.
                                    removeOffhandForBow = true;
                                }
                            }
                            else
                            {
                                //if the thing in our offhand is not ItemTypes.OFFHAND, we should remove it.
                                removeOffhandForBow = true;
                            }

                            //remove it if we want to.
                            if (removeOffhandForBow)
                            {
                                Unequip(EquipmentSlots.OFFHAND, false, play, true, returnOldItemToInventory);
                            }
                        }
                    }
                    //if it isn't a bow, but is 2H, remove the offhand right away.
                    else
                    {
                        Unequip(EquipmentSlots.OFFHAND, false, play, true, returnOldItemToInventory);
                        checkForBestOffhandOption = false;
                    }
                }

                // Confirm bows and 2H don't have anything weird paired.


                if (!alreadyEquippedPairedOffhand && equipPairedItemIfPossible)
                {
                    wp.ValidatePairedItem();
                    TryEquipPairedItem(wp, returnOldItemToInventory, checkForBestOffhandOption);
                }


                if (owner == GameMasterScript.heroPCActor)
                {
                    GameMasterScript.uims.ExitTargeting();
                }
            }
            else if (item.itemType == ItemTypes.WEAPON && subSlot == 1)
            {
                // Equip weapon to offhand (subslot 1)
                Item prev = equipment[(int)EquipmentSlots.OFFHAND];
/*                 if (prev != null) Debug.Log("Unequipping previous offhand: " + prev.actorRefName);
                else Debug.Log("There isn't anything in the offhand slot at all."); */
                if (prev != null)
                {
                    Unequip(EquipmentSlots.OFFHAND, false, play, true, returnOldItemToInventory);
                }
                eSlot = EquipmentSlots.OFFHAND;
                UIManagerScript.RemoveWeaponFromActives((Weapon)item, updateHotbarImmediately:false);

                Weapon wp = GetWeapon();
                if (wp.twoHanded)
                {
                    if ((wp.IsWeaponBow()) && (IsOffhandQuiver()))
                    {
                        // Allow this;
                    }
                    else
                    {
                        Unequip(EquipmentSlots.WEAPON, true, play, showText, returnOldItemToInventory); // If equipping in offhand, and we have a 2h in mainhand, unequip it.
                    }
                }
            }
            else if (item.itemType == ItemTypes.OFFHAND)
            {
                Equipment prevOff = equipment[(int)EquipmentSlots.OFFHAND];
                if (prevOff != null)
                {
                    Unequip(EquipmentSlots.OFFHAND, false, play, showText, returnOldItemToInventory);
                }
                eSlot = EquipmentSlots.OFFHAND;
                Weapon wp = GetWeapon();

                if (wp.twoHanded)
                {
                    Offhand newOH = item as Offhand;
                    if (wp.IsWeaponBow() && newOH.allowBow)
                    {
                        // This is fine
                    }
                    else
                    {
                        GameMasterScript.heroPCActor.GetObject().GetComponent<AudioStuff>().PlayCue("Error");
                        return false;
                    }

                    //Unequip(EquipmentSlots.WEAPON, true, play, showText); // If equipping in offhand, and we have a 2h in mainhand, unequip it.
                }
            }
            else if (item.itemType == ItemTypes.ARMOR)
            {
                Equipment prevArmor = equipment[(int)EquipmentSlots.ARMOR];
                if (prevArmor != null)
                {
                    Unequip(EquipmentSlots.ARMOR, false, play, showText, returnOldItemToInventory);
                }
                eSlot = EquipmentSlots.ARMOR;
            }
            else if (item.itemType == ItemTypes.EMBLEM)
            {
                Equipment prevEmblem = equipment[(int)EquipmentSlots.EMBLEM];
                if (prevEmblem != null)
                {
                    Unequip(EquipmentSlots.EMBLEM, false, play, showText, returnOldItemToInventory);
                }
                eSlot = EquipmentSlots.EMBLEM;
            }
            else if (item.itemType == ItemTypes.ACCESSORY)
            {
                Equipment prevAcc = null;
                switch (subSlot)
                {
                    case 0:
                        prevAcc = equipment[(int)EquipmentSlots.ACCESSORY];
                        if (prevAcc != null)
                        {
                            Unequip(EquipmentSlots.ACCESSORY, false, play, showText, returnOldItemToInventory);
                        }
                        eSlot = EquipmentSlots.ACCESSORY;
                        break;
                    case 1:
                        prevAcc = equipment[(int)EquipmentSlots.ACCESSORY2];
                        if (prevAcc != null)
                        {
                            Unequip(EquipmentSlots.ACCESSORY2, false, play, showText, returnOldItemToInventory);
                        }
                        eSlot = EquipmentSlots.ACCESSORY2;
                        break;
                }

            }
            if (item.collection != null)
            {
                item.collection.RemoveItem(item, true);
            }
            SetEquipment(item, eSlot, false);

            if (owner.GetActorType() == ActorTypes.HERO)
            {
                CheckEquipItemMods(item, StatusTrigger.ONADD);
                CheckEquipItemAbilities(item, StatusTrigger.ONADD);
            }
            owner.SetBattleDataDirty();
            if (owner == GameMasterScript.heroPCActor)
            {
                HeroPC hero = owner as HeroPC;
                if (eSlot == EquipmentSlots.OFFHAND)
                {
                    if (item != null)
                    {
                        if (item.itemType == ItemTypes.OFFHAND)
                        {
                            Offhand oh = item as Offhand;
                            if (!oh.allowBow)
                            {
                                hero.lastOffhandEquipped = item;
                                hero.lastOffhandEquippedID = item.actorUniqueID;
                            }
                        }
                        else
                        {
                            hero.lastOffhandEquipped = item;
                            hero.lastOffhandEquippedID = item.actorUniqueID;
                        }
                    }
                }
            }

            HeroPC checkHero = owner as HeroPC;
            if (item.itemType == ItemTypes.WEAPON && subSlot == 0 && checkHero != null)
            {
                UIManagerScript.UpdateActiveWeaponInfo();
            }
            if (play == SND.PLAY && showText)
            {
                if (GameMasterScript.actualGameStarted)
                {
                    if (item.displayName != "Fists")
                    {
                        StringManager.SetTag(0, item.displayName);
                        GameLogScript.LogWriteStringRef("log_equipped_item");
                        GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem");
                    }
                    GameMasterScript.heroPCActor.lastEquippedItem = item;
                }
            }
            if (owner.GetActorType() == ActorTypes.HERO)
            {
                owner.SetActorData("equipment_dirty", 1);                
                UIManagerScript.RefreshPlayerStats();
                if (item.gearSet != null)
                {
                    EvaluateSets(null);
                }
            }            

            return true;
        }
    }

    void TryEquipPairedItem(Weapon wp, bool returnOldItemToInventory, bool checkForBestOffhandOption)
    {
        //Find what offhand is paired with the weapon we are currently equipping.
        Equipment pairedOH = wp.GetPairedItem();

        // If we have a paired offhand, and it isn't already equipped,
        if (pairedOH != null && equipment[(int)EquipmentSlots.OFFHAND] != pairedOH)
        {
            // First, do we still have the item?
            if (owner.myInventory.HasItem(pairedOH))
            {
                //Debug.Log(wp.actorRefName + " " + wp.actorUniqueID + " has paired item " + pairedOH.actorRefName + " " + pairedOH.actorUniqueID);
                Equip(pairedOH, SND.SILENT, 1, true, returnOldItemToInventory, false);
            }
            else
            {
                // We don't still have the paired item.
                //if (Debug.isDebugBuild) Debug.Log(pairedOH.actorRefName + " not found, unpairing");
                wp.RemovePairedItem(pairedOH.actorUniqueID);

                if (PlayerOptions.autoEquipBestOffhand && owner != null && owner.GetActorType() == ActorTypes.HERO && checkForBestOffhandOption)
                {
                    FindBestOffhandOption(wp, returnOldItemToInventory);
                }
            }
        }
        //otherwise, if we don't have one, but have sworn to look for one, do that now.
        else if ((pairedOH == null) && (checkForBestOffhandOption))
        {
            if (PlayerOptions.autoEquipBestOffhand && owner != null && owner.GetActorType() == ActorTypes.HERO)
            {
                FindBestOffhandOption(wp, returnOldItemToInventory);
            }
        }
    }

    public void FindBestOffhandOption(Weapon wp, bool returnOldItemToInventory)
    {
        // OK, swap to a quiver.
        if (wp.IsWeaponBow())
        {
            //if we don't have a quiver in our our offhand, find one.
            if (!IsOffhandQuiver())
            {
                EquipBestQuiver();
            }
        }
        else // We aren't a bow, so find the best offhand.
        {
            if (GameMasterScript.heroPCActor.lastOffhandEquipped != null)
            {
                if (GameMasterScript.heroPCActor.myInventory.HasItem(GameMasterScript.heroPCActor.lastOffhandEquipped) && GameMasterScript.heroPCActor.lastOffhandEquipped.itemType != ItemTypes.WEAPON)
                {
                    GameMasterScript.heroPCActor.myEquipment.Equip(GameMasterScript.heroPCActor.lastOffhandEquipped, SND.SILENT, 1, false, returnOldItemToInventory);
                }
            }
        }
    }

    public void UnequipByReference(Equipment eqToRemove)
    {
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (equipment[i] == eqToRemove)
            {
                Unequip((EquipmentSlots)i, false, SND.SILENT, true);
                return;
            }
        }
        //Debug.Log(eqToRemove.actorRefName + " not equipped by " + owner.actorRefName);
    }

    /// <summary>
    /// If "onlyActualFists" is FALSE, we will count handwraps as fists.
    /// </summary>
    /// <param name="wpn"></param>
    /// <param name="onlyActualFists"></param>
    /// <returns></returns>
    public bool IsDefaultWeapon(Item wpn, bool onlyActualFists = false)
    {
        if (wpn == null) return true;

        if (wpn.itemType != ItemTypes.WEAPON) return false;

        if (wpn.actorUniqueID == -1 || wpn.actorRefName == "weapon_fists") return true;

        if (!onlyActualFists && wpn.ReadActorData("monkweapon") == 1)
        {
            return true;
        }

        if (defaultWeapon.actorRefName == wpn.actorRefName) return true;

        return false;
    }

    public void Unequip(EquipmentSlots slot, bool replaceWithDefaultWeapon, SND silent, bool showText, bool returnOldItemToInventory = true)
    {
        //if (Debug.isDebugBuild) Debug.Log("Unequip item from " + slot + " return old: " + returnOldItemToInventory + " replace w/ default? " + replaceWithDefaultWeapon);
            Equipment itemToRemove = equipment[(int)slot];
        
        bool hero = owner.GetActorType() == ActorTypes.HERO;

        if (itemToRemove == null || IsDefaultWeapon(itemToRemove, onlyActualFists: true))
        {
            //if (Debug.isDebugBuild) Debug.Log("Item is null? " + (itemToRemove==null) + " or it's default. " + IsDefaultWeapon(itemToRemove, onlyActualFists: true));
            return;
            // Do nothing
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Try unequip " + itemToRemove.actorRefName + " " + itemToRemove.actorUniqueID);
            SetEquipment(null, slot, true);

            if (itemToRemove.collection != null && returnOldItemToInventory)
            {
                if (itemToRemove.actorRefName == "accessory_xpjpring")
                {
                    // Special case - destroy this ring on removal.
                    StringManager.SetTag(0, itemToRemove.displayName);
                    GameLogScript.LogWriteStringRef("log_xpring_disappear");
                }
                else
                {
                    //if (Debug.isDebugBuild) Debug.Log("Returning item to inventory, " + itemToRemove.collection.owner.actorRefName);
                    itemToRemove.collection.AddItem(itemToRemove, false);
                    itemToRemove.newlyPickedUp = false;
                }
            }
            else
            {
                //if (Debug.isDebugBuild) Debug.Log("Cannot remove " + owner.actorRefName + "'s " + itemToRemove.actorRefName + " from null collection.");
            }

            // SetEquipment to null WAS here.

            if (hero) GameMasterScript.gmsSingleton.SetTempGameData("unequipping", 1);

           // if (Debug.isDebugBuild) Debug.Log("Removing: " + itemToRemove.actorRefName);
            CheckEquipItemMods(itemToRemove, StatusTrigger.ONREMOVE);
            CheckEquipItemAbilities(itemToRemove, StatusTrigger.ONREMOVE);

            if (hero) GameMasterScript.gmsSingleton.SetTempGameData("unequipping", 0);

            if (itemToRemove.actorRefName == "weapon_leg_doublebiteaxe" || itemToRemove.HasModByRef("mm_doublebite"))
            {
                GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("doublebite_shadow");
                GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("doublebite_physical");
                UIManagerScript.RefreshStatuses();
            }

            if ((int)slot == (int)EquipmentSlots.WEAPON && replaceWithDefaultWeapon)
            {
                //if (Debug.isDebugBuild) Debug.Log("Let's re-equip default weapon");
                Equip(defaultWeapon, silent, 0, showText);
            }
            if (SND.PLAY == silent && showText)
            {
                if (itemToRemove.displayName != "Fists")
                {
                    StringManager.SetTag(0, itemToRemove.displayName);
                    GameLogScript.LogWriteStringRef("log_unequipped_item");
                }
            }

            itemToRemove.hasBeenUnequipped = true;
            owner.SetBattleDataDirty();
            UIManagerScript.RefreshPlayerStats();
            if (itemToRemove.gearSet != null)
            {
                EvaluateSets(itemToRemove);
            }

            if (hero) owner.SetActorData("equipment_dirty", 1);
        }
    }

    public void AddEquipment(EquipmentSlots slot, Equipment item)
    {
        owner.myInventory.AddItem(item, false);
        if (item.itemType == ItemTypes.WEAPON && slot == EquipmentSlots.OFFHAND)
        {
            // Equip as dual wield.
            Equip(item, SND.SILENT, 1, true);
        }
        else
        {
            Equip(item, SND.SILENT, 0, true);
        }

    }

    public float GetWeaponPower(Weapon wp)
    {
        //if (equipment[(int)EquipmentSlots.WEAPON] != null)
        if (wp != null)
        {
            //Weapon weapon = (Weapon)equipment[(int)EquipmentSlots.WEAPON];
            return wp.power;
        }
        else
        {
            if (defaultWeapon != null)
            {
                return defaultWeapon.power;
            }
            else
            {
                return 0.0f;
            }
        }
    }

    public bool IsCurrentWeaponRanged()
    {
        return IsWeaponRanged(equipment[(int)EquipmentSlots.WEAPON] as Weapon);
    }

    public bool IsWeaponRanged(Weapon wp)
    {
        //if (equipment[(int)EquipmentSlots.WEAPON] != null)
        if (wp != null)
        {
            //Weapon weapon = (Weapon)equipment[(int)EquipmentSlots.WEAPON];
            return wp.isRanged;
        }
        else
        {
            if (defaultWeapon != null)
            {
                return defaultWeapon.isRanged;
            }
            else
            {
                return false;
            }
        }

    }

    public FlavorDamageTypes GetFlavorDamageType(Weapon wp)
    {
        //if (equipment[(int)EquipmentSlots.WEAPON] != null)
        if (wp != null)
        {
            //Weapon weapon = (Weapon)equipment[(int)EquipmentSlots.WEAPON];
            return wp.flavorDamType;
        }
        else
        {
            return FlavorDamageTypes.BLUNT;
        }
    }

    public DamageTypes GetDamageType(Weapon wp)
    {
        //if (equipment[(int)EquipmentSlots.WEAPON] != null)
        if (wp != null)
        {
            //Weapon weapon = (Weapon)equipment[(int)EquipmentSlots.WEAPON];
            return wp.damType;
        }
        else
        {
            return DamageTypes.PHYSICAL;
        }
    }

    public void UnequipAllGear()
    {
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            Unequip((EquipmentSlots)i, true, SND.SILENT, true);
        }
        // All gear unequipped!
    }

    public void UnequipAndReequipAllGear()
    {
        Dictionary<EquipmentSlots, Equipment> gearUnequipped = new Dictionary<EquipmentSlots, Equipment>();
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            if (equipment[i] == null) continue;
            if (IsDefaultWeapon(equipment[i], onlyActualFists: true)) continue;
            Equipment eq = equipment[i];
            gearUnequipped.Add((EquipmentSlots)i, equipment[i]);
            Unequip((EquipmentSlots)i, true, SND.SILENT, false);
            Debug.Log("Unequipped " + eq.actorRefName);
        }

        foreach(EquipmentSlots slot in gearUnequipped.Keys)
        {
            Equip(gearUnequipped[slot], SND.SILENT, slot, false, true, false);
            Debug.Log("Re-equipped " + gearUnequipped[slot].actorRefName);
        }
    }

    public void SetHeroDefaults(bool newGame)
    {
        ClearGear();

        // Set starting equipment
        GameMasterScript.heroPCActor.SetDefaultWeapon(newGame);
    }

    public void RemoveGearTag(string tag)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == null) continue;
            if (IsDefaultWeapon(equipment[i], onlyActualFists: true)) continue;
            equipment[i].RemoveActorData(tag);
        }
    }

    /// <summary>
    /// Sets ActorData for all equipped gear to tag / value
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="value"></param>
    public void TagAllGear(string tag, int value)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == null) continue;
            if (IsDefaultWeapon(equipment[i], onlyActualFists: true)) continue;
            equipment[i].SetActorData(tag, value);
        }
    }

    /// <summary>
    /// Clears the requested tag (actor data) on all equipped gear
    /// </summary>
    /// <param name="tag"></param>
    public void UntagAllGear(string tag)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == null) continue;
            if (IsDefaultWeapon(equipment[i], onlyActualFists: true)) continue;
            equipment[i].RemoveActorData(tag);
        }
    }

    public Item GetItemWithMagicMod(string modRef)
    {
        for (int i = 0; i < equipment.Length; i++)
        {
            if (equipment[i] == null) continue;
            if (IsDefaultWeapon(equipment[i], onlyActualFists: true)) continue;
            if (equipment[i].HasModByRef(modRef))
            {
                return equipment[i];
            }
        }

        return null;
    }

    public void OnHeroEquipmentChanged()
    {
        if (!MapMasterScript.activeMap.IsJobTrialFloor())
        {
            foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
            {
                if (qs.complete) continue;
                foreach (QuestRequirement qr in qs.qRequirements)
                {
                    if (qr.qrType == QuestRequirementTypes.SAMEGEAR)
                    {
                        QuestScript.HeroFailedQuest(qs);
                    }
                }
            }
        }        
    }
}
