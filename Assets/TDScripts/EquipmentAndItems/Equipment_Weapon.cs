using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;


[System.Serializable]
[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName})")]
public partial class Weapon : Equipment
{
    public WeaponTypes weaponType;
    //public DamageTypes damType;
    public DamageTypes damType;
    public FlavorDamageTypes flavorDamType;
    public float power;
    public int curDurability;
    public int maxDurability;
    public int range;
    public bool isRanged;
    public string impactEffect;
    public string swingEffect;
    public bool twoHanded;
    public static string[] weaponProperties;

    public static float MAX_WEAPON_POWER = 150f;

    public static readonly float[] expectedMonsterWeaponPower =
    {
        1f, // 0
        3.8f, // 1
        4.5f,
        6.8f,
        8.2f,
        10f,
        15f, // 6
        30.1f,
        36f,
        54f,
        62.5f, // 10
        82f,
        88f,
        94f,
        100f,
        106f, // 15
        112f,
        118f,
        124f,
        130f,
        140f, // 20
        150f,
        160f,
        170f,
        180f,
        190f
    };

    public static readonly float[] expectedPetOrSummonWeaponPower =
    {
        5f, // 0
        9f, // 1
        9.5f, // 2
        10f, // 3
        10.5f,
        11f, // 5
        13f,
        16f, // 7
        19f,
        23f, // 9
        26f,
        30f, // 11
        33f,
        37f, // 13
        41f,
        44f, // 15
        47f,
        50f, // 17
        53f,
        56f,
        60f,
        65f, // 21
        71f,
        77f,
        85f,
        93f
    };
    public static string[] weaponTypesVerbose;

    public Weapon() : base()
    {
        addAbilities = new List<AbilityScript>();
        damType = DamageTypes.PHYSICAL;
        slot = EquipmentSlots.WEAPON;
        itemType = ItemTypes.WEAPON;
        range = 1;
        swingEffect = "GenericSwingEffect";
    }

    //returns true if this equipment can be equipped in the offhand slot
    //at all. Does NOT check the current condition of the player equipment. So this will always
    //return true for things like Quivers, even if the player has a 2H sword equipped at the time.
    public override bool IsOffhandable()
    {
        
        return !twoHanded;
    }

    public Weapon GetTemplate()
    {
        return GameMasterScript.masterItemList[actorRefName] as Weapon;
    }

    public void ValidatePairedItem()
    {
        if (pairedItems.Count == 0) return;

        List<EQPair> toRemove = new List<EQPair>();

        foreach (EQPair eqp in pairedItems)
        {
            if (eqp.eq == null)
            {
                toRemove.Add(eqp);
                continue;
            }

            if (IsWeaponBow())
            {
                if (eqp.eq.itemType == ItemTypes.OFFHAND)
                {
                    Offhand oh = eqp.eq as Offhand;
                    if (!oh.allowBow)
                    {
                        // Bows cannot have non-quiver offhands paired.
                        toRemove.Add(eqp);
                    }
                }
                else
                {
                    // Bows cannot have weapons paired.
                    toRemove.Add(eqp);
                }
            }
            else if (twoHanded)
            {
                // Two handers that aren't bows should have nothing paired at all.
                toRemove.Add(eqp);
            }
        }

        foreach (EQPair eqp in toRemove)
        {
            RemovePairedItemByRef(eqp.eq);
            //Debug.Log(actorRefName + " " + actorUniqueID + " removed paired item " + eqp.eq.actorRefName + " as it was incompatible.");
        }

    }

    // Used for monster checks.

    public override bool CheckIfSameAs(Item itm)
    {
        if (itm.itemType != ItemTypes.WEAPON) return false;

        Weapon w = itm as Weapon;

        if (w.actorRefName != actorRefName) return false;
        if (w.power != power) return false;
        if (w.damType != damType) return false;

        if (mods != null && w.mods == null) return false;
        if (mods == null && w.mods != null) return false;

        if (mods != null)
        {
            if (mods.Count != w.mods.Count) return false;
        }

        return true;
    }

    public override string GetItemWorldUpgrade()
    {
        if (timesUpgraded > Equipment.GetMaxUpgrades()-1)
        {
            return StringManager.GetString("ui_itemworld_noupgrade");
        }

        float nPower = power + 1;
        nPower *= 1.08f;
        float powerDifference = nPower - power;
        int display = (int)((float)(powerDifference * 10));
        return "+" + display + " " + StringManager.GetString("misc_weaponpower_short");
    }

    public bool IsWeaponBow()
    {
        if (isRanged && weaponType == WeaponTypes.BOW)
        {
            return true;
        }
        return false;
    }

    public override void UpgradeItem(bool debug = false)
    {
        base.UpgradeItem(debug);

        if (weaponType == WeaponTypes.NATURAL)
        {
            string modToRemove = "";
            string modToAdd = "";

            switch (timesUpgraded)
            {
                case 1:
                    RemoveAndAddMod("", "", "mm_budokatouch1");
                    break;
                case 2:
                    RemoveAndAddMod("mm_budokatouch1", "physpen10", "mm_budokatouch2");
                    break;
                case 3:
                    RemoveAndAddMod("mm_budokatouch2", "physpen15", "mm_budokatouch3");
                    break;
                case 4:
                    RemoveAndAddMod("mm_budokatouch3", "physpen20", "mm_budokatouch4");
                    break;
            }
        }
        else
        {
            power += 1;
            power *= 1.08f;
        }

        RebuildDisplayName();
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        if (pairedItems.Count > 0)
        {
            foreach (EQPair pair in pairedItems)
            {
                pair.WriteToSave(writer);
            }
        }

        string weapBuilder = power.ToString() + "|" + range.ToString();
        if (damType != DamageTypes.PHYSICAL)
        {
            weapBuilder += "|" + (int)damType;
        }
        writer.WriteElementString("weap",weapBuilder);

        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item template)
    {
        base.CopyFromItem(template);
        Weapon weaponTemplate = template as Weapon;
        CopyFromEquipment(weaponTemplate);
        twoHanded = weaponTemplate.twoHanded;
        weaponType = weaponTemplate.weaponType;
        damType = weaponTemplate.damType;
        flavorDamType = weaponTemplate.flavorDamType;
        power = weaponTemplate.power;
        maxDurability = weaponTemplate.maxDurability;
        curDurability = weaponTemplate.maxDurability;
        range = weaponTemplate.range;
        isRanged = weaponTemplate.isRanged;
        impactEffect = weaponTemplate.impactEffect;
        swingEffect = weaponTemplate.swingEffect;
    }

    public void SetDurability(int amount)
    {
        curDurability = amount;
    }

    public int ChangeDurability(int amount)
    {
        curDurability = curDurability + amount;
        if (curDurability <= 0)
        {
            curDurability = 0;
        }
        if (curDurability > maxDurability)
        {
            curDurability = maxDurability;
        }
        return curDurability;
    }

    public override bool ValidateEssentialProperties()
    {
        if (!base.ValidateEssentialProperties())
        {
            return false;
        }

        if (string.IsNullOrEmpty(impactEffect) && string.IsNullOrEmpty(swingEffect))
        {
            Debug.LogError("Weapon ref " + actorRefName + " has no swing OR impact effect.");
            return false;
        }

        if (string.IsNullOrEmpty(impactEffect) && !isRanged)
        {
            Debug.LogError("Weapon ref " + actorRefName + " is not a ranged weapon, and has no impact effect.");
            return false;
        }

        if (string.IsNullOrEmpty(swingEffect) && isRanged)
        {
            Debug.LogError("Weapon ref " + actorRefName + " is a ranged weapon, and has no project (swing effect).");
            return false;
        }

        if (power == 0)
        {
            Debug.LogError("Weapon ref " + actorRefName + " has power of 0. Must be higher!");
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
        switch(reader.Name)
        {
            case "Range":
                range = reader.ReadElementContentAsInt();
                return true;
            case "IsRanged":
                isRanged = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "Ranged":
                isRanged = true;
                reader.Read();
                return true;
            case "TwoHanded":
                twoHanded = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "TwoHand":
                twoHanded = true;
                reader.Read();
                return true;
            case "DamageType":
                damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                return true;
            case "FlavorDamageType":
                flavorDamType = (FlavorDamageTypes)Enum.Parse(typeof(FlavorDamageTypes), reader.ReadElementContentAsString());
                return true;
            case "WeaponType":
                weaponType = (WeaponTypes)Enum.Parse(typeof(WeaponTypes), reader.ReadElementContentAsString());
                return true;
            case "Power":
                txt = reader.ReadElementContentAsString();
                power = CustomAlgorithms.TryParseFloat(txt);
                if (power <= 1f)
                {
                    power = 10f;
                }
                return true;
            case "SwingEffect":
                swingEffect = reader.ReadElementContentAsString();
                if (!String.IsNullOrEmpty(swingEffect))
                {

                    if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                    {
                        GameMasterScript.gmsSingleton.TryPreloadResourceInstant(swingEffect, "SpriteEffects/" + swingEffect);
                    }
                    else
                    {
                        GameMasterScript.TryPreloadResourceNoBundles(swingEffect, "SpriteEffects/" + swingEffect);
                    }
                }
                return true;
            case "ImpactEffect":
                impactEffect = reader.ReadElementContentAsString();
                if (!String.IsNullOrEmpty(impactEffect))
                {
                    if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                    {
                        GameMasterScript.gmsSingleton.TryPreloadResourceInstant(impactEffect, "SpriteEffects/" + impactEffect);
                    }
                    else
                    {
                        GameMasterScript.TryPreloadResourceNoBundles(impactEffect, "SpriteEffects/" + impactEffect);
                    }
                }
                return true;
        }

        return false;
    }

    public override int GenerateSubtypeAsInt()
    {
        int baseValue = Item.WEAPON_BASE_VALUE;
        baseValue += (int)weaponType;
        if (twoHanded)
        {
            baseValue += 20;
        }

        return baseValue;
    }
}