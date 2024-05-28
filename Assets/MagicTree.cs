using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class MagicTree
{
    public int dayPlanted;
    public TreeAges age;
    public TreeSpecies species;
    public Rarity treeRarity;
    public bool alive;
    public int slot;
    public string whoPlanted;
    public static int[] MAX_FRUIT = new int[5] { 0, 0, 1, 2, 3 }; // Nothing, seed, seedling, sapling, adult
    public NPC npcObject;

    public const float FRUIT_CHANCE = 0.25f;

    public void Initialize(NPC myNewObject)
    {
        dayPlanted = 0;
        age = TreeAges.NOTHING;
        species = TreeSpecies.CASHCROPS;
        treeRarity = Rarity.COMMON;
        alive = false;
        slot = 0;
        whoPlanted = "";
        npcObject = myNewObject;
    }

    public MagicTree(NPC myNewObject)
    {
        npcObject = myNewObject;
        treeRarity = Rarity.COMMON;
    }

    public string GetSpeciesName()
    {
        string baseName = "";
        switch (species)
        {
            case TreeSpecies.FOOD_A:
                baseName = StringManager.GetString("treenames_food_a");
                break;
            case TreeSpecies.FOOD_B:
                baseName = StringManager.GetString("treenames_food_b");
                break;
            case TreeSpecies.SPICES:
                baseName = StringManager.GetString("treenames_spices");
                break;
            case TreeSpecies.CASHCROPS:
                baseName = StringManager.GetString("treenames_cashcrop");
                break;
            default:
                baseName = species.ToString();
                break;
        }

        switch (treeRarity)
        {
            case Rarity.COMMON:
                return UIManagerScript.silverHexColor + baseName + "</color>";
            case Rarity.UNCOMMON:
                return UIManagerScript.blueHexColor + baseName + "</color>";
            case Rarity.MAGICAL:
                return "<color=yellow>" + baseName + "</color>";
        }

        return baseName;
    }

    public string GetRarity()
    {
        switch (treeRarity)
        {
            case Rarity.COMMON:
            default:
                return StringManager.GetString("corral_rarity_0");
            case Rarity.UNCOMMON:
                return StringManager.GetString("misc_rarity_1");
            case Rarity.MAGICAL:
                return StringManager.GetString("misc_rarity_2");
        }
    }

    public string GetTreeAgeString()
    {
        switch (age)
        {
            case TreeAges.SEED:
            default:
                return StringManager.GetString("treeage_seed");
            case TreeAges.SEEDLING:
                return StringManager.GetString("treeage_seedling");
            case TreeAges.SAPLING:
                return StringManager.GetString("treeage_sapling");
            case TreeAges.ADULT:
                return StringManager.GetString("treeage_adult");
        }
    }

    public float GetXPReward()
    {
        int calcAge = CalcAge();
        if (calcAge > 20) calcAge = 30;
        float valueMult = 2.5f;
        switch (treeRarity)
        {
            case Rarity.UNCOMMON:
                valueMult = 4f;
                break;
            case Rarity.MAGICAL:
                valueMult = 6f;
                break;
        }
        return (calcAge * valueMult);
    }
    public float GetJPReward()
    {
        int calcAge = CalcAge();
        if (calcAge > 20) calcAge = 30;
        float valueMult = 3.5f;
        switch (treeRarity)
        {
            case Rarity.UNCOMMON:
                valueMult = 4.5f;
                break;
            case Rarity.MAGICAL:
                valueMult = 7f;
                break;
        }
        return (calcAge * valueMult);
    }

    public int CalcAge()
    {
        int age = MetaProgressScript.totalDaysPassed - dayPlanted;
        if (age < 0) age = 0;
        return MetaProgressScript.totalDaysPassed - dayPlanted;
    }

    public List<Item> GetAllFoodFromTree()
    {
        if (!alive) return null;
        if (npcObject == null)
        {
            Debug.Log("Magic tree component has no NPC component!");
            return null;
        }
        List<Item> fruits = new List<Item>();

        foreach (Item itm in npcObject.myInventory.GetInventory())
        {
            fruits.Add(itm);
        }

        return fruits;
    }

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("magictree");
        writer.WriteElementString("slot", slot.ToString());
        writer.WriteElementString("alive", alive.ToString().ToLowerInvariant());
        writer.WriteElementString("rarity", treeRarity.ToString().ToLowerInvariant());
        writer.WriteElementString("age", age.ToString().ToLowerInvariant());
        writer.WriteElementString("dayplanted", dayPlanted.ToString());
        writer.WriteElementString("species", species.ToString().ToLowerInvariant());
        writer.WriteElementString("whoplanted", whoPlanted);
        writer.WriteEndElement();
    }

    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name.ToLowerInvariant())
            {
                case "slot":
                    slot = reader.ReadElementContentAsInt();
                    break;
                case "age":
                    age = (TreeAges)Enum.Parse(typeof(TreeAges), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "rarity":
                    treeRarity = (Rarity)Enum.Parse(typeof(Rarity), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "species":
                    species = (TreeSpecies)Enum.Parse(typeof(TreeSpecies), reader.ReadElementContentAsString().ToUpperInvariant());
                    if (species == TreeSpecies.ELM)
                    {
                        species = TreeSpecies.FOOD_A;
                    }
                    if (species == TreeSpecies.ORCHID)
                    {
                        species = TreeSpecies.FOOD_B;
                    }
                    if (species == TreeSpecies.OAK)
                    {
                        species = TreeSpecies.SPICES;
                    }
                    break;
                case "dayplanted":
                    dayPlanted = reader.ReadElementContentAsInt();
                    break;
                case "whoplanted":
                    whoPlanted = reader.ReadElementContentAsString();
                    break;
                case "alive":
                    alive = reader.ReadElementContentAsBoolean();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();
    }

    /// <summary>
    /// Creates a tree NPC of index 'slot' with a treeComponent. It will just be a pile of dirt with nothing in it.
    /// </summary>
    /// <param name="slot"></param>
    public static NPC CreateTree(int slot)
    {
        NPC treePlanted = new NPC();
        treePlanted.treeComponent = new MagicTree(treePlanted);
        treePlanted.treeComponent.alive = false;
        treePlanted.treeComponent.age = TreeAges.NOTHING;
        treePlanted.treeComponent.dayPlanted = 0;
        treePlanted.treeComponent.slot = slot;
        treePlanted.treeComponent.species = TreeSpecies.OAK;

        int dispSlot = slot + 1;

        treePlanted.actorRefName = "town_tree" + dispSlot;
        treePlanted.displayName = "Magic Tree";
        treePlanted.prefab = "GroveTree";
        treePlanted.dialogRef = "grovetree";
        treePlanted.playerCollidable = true;
        treePlanted.interactable = true;

        return treePlanted;
    }

    public void UpdateAgeOfTree()
    {
        int physicalAge = MetaProgressScript.totalDaysPassed - dayPlanted;
        if (physicalAge <= 0)
        {
            age = TreeAges.SEED;
        }
        if (physicalAge > 0 && physicalAge < 6)
        {
            age = TreeAges.SEEDLING;
        }
        if (physicalAge >= 6 && physicalAge < 15)
        {
            age = TreeAges.SAPLING;
        }
        if (physicalAge >= 15)
        {
            age = TreeAges.ADULT;
        }
    }
}