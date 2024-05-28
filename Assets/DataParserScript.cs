using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;  

public enum DefType { BASE, THING, TABLE }

public class ActorTable
{
    public Dictionary<string, int> table;
    private int total;
    public string refName;
    public List<Actor> actors;
    public List<string> allKeys;
    public bool replaceRef;
    
    static int globalID;

    public void Clear()
    {
        actors.Clear();
        allKeys.Clear();
        table.Clear();
        total = 0;
    }

    public int GetTotalCount()
    {
        int count = 0;
        foreach(string key in table.Keys)
        {
            count += table[key];
        }
        return count;
    }

    public int GetAverageValue()
    {
        int average = (GetTotalCount() / table.Keys.Count);
        return average;
    }

    public void AddToTable(string aRef, int amount)
    {       
        int curValue;
        if (table.TryGetValue(aRef, out curValue))
        {
            curValue += amount;
            table[aRef] = curValue;
        }
        else
        {
            table.Add(aRef, amount);
            allKeys.Add(aRef);
        }
        total += amount;
    }

    public void AddToTableIncludingItemActor(string aRef, int amount)
    {       
        AddToTable(aRef, amount);
        Item iTemplate = Item.GetItemTemplateFromRef(aRef);

        if (!actors.Contains(iTemplate)) actors.Add(iTemplate);        
    }    

    public void RemoveFromTable(string aRef)
    {
        if (table.ContainsKey(aRef))
        {
            int amount = table[aRef];
            table.Remove(aRef);
            total -= amount;
        }
        Actor rem = null;
        foreach(Actor act in actors)
        {
            if (act.actorRefName == aRef)
            {
                rem = act;
            }
        }
        actors.Remove(rem);
        allKeys.Remove(aRef);
    }

    public bool HasActor(string checkRef)
    {
        foreach (string entry in table.Keys)
        {
            if (entry == checkRef)
            {
                return true;
            }
        }
        return false;
    }
    public string GetSpecificActorRef(string checkRef)
    {
        foreach(string entry in table.Keys)
        {
            if (entry == checkRef)
            {
                return entry;
            }
        }

        return "";
    }

    public string GetRandomActorRefNonWeighted()
    {
        if (allKeys.Count == 0) return "";
        return allKeys[UnityEngine.Random.Range(0, allKeys.Count)];
    }

    public int GetNumActors()
    {
        return table.Count;
    }

    public string GetRandomActorRef()
    {
        if (total == 0)
        {
            Debug.Log("WARNING: table " + refName + " has no keys.");
            return "";
        }
        int roll = Random.Range(0, total);
        foreach (KeyValuePair<string, int> kvp in table)
        {
            if (roll <= kvp.Value)
            {
                return kvp.Key;
            }
            roll -= kvp.Value;
        }
        /*
                foreach (string entry in table.Keys)
                {
                    if (roll <= table[entry])
                    {
                        return entry;
                    }
                    roll -= table[entry];
                }
        */
#if UNITY_EDITOR
        Debug.LogError("Can't find random actor in table " + refName );
#else
        Debug.Log("Can't find random actor in table " + refName );
#endif
        //returing "error" causes bugs
        return "";
    }

    public void ReadFromXML(XmlReader reader, ActorTypes actorType, Dictionary<string, ActorTable> tableDict, out bool mergedWithExistingTable)
    {
        reader.ReadStartElement();

        bool isBreakableTable = false; // Special case for the master table of breakable objects.
        mergedWithExistingTable = false;
        ActorTable existingTableToAddTo = null;

        int attempts = 0;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            attempts++;
            if (attempts > 19997)
            {
                Debug.Log("WARNING: Probable infinite loop when reading a table...");
                if (attempts > 20000)
                {
                    mergedWithExistingTable = false;
                    return;
                }
            }
            switch (reader.Name)
            {
                case "RefName":
                    refName = reader.ReadElementContentAsString();
                    if (refName == "randombreakables")
                    {
                        isBreakableTable = true;
                    }
                    if (tableDict.TryGetValue(refName, out existingTableToAddTo))
                    {
                        mergedWithExistingTable = true;
                    }
                    break;
                case "ReplaceRef":
                    replaceRef = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "MergeWithExisting":
                    reader.Read();                 
                    if (tableDict.TryGetValue(refName, out existingTableToAddTo))
                    {
                        mergedWithExistingTable = true;
                    }                    
                    break;
                default:
                    if (reader.Name != "" && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        bool safeToAddEntry = false;
                        if (actorType == ActorTypes.COUNT || isBreakableTable)
                        {
                            safeToAddEntry = true;
                        }
                        else if (actorType != ActorTypes.COUNT)
                        {
                            bool addActorToTable = false;
                            Actor template = null;
                            switch(actorType)
                            {
                                case ActorTypes.ITEM:
                                    Item iTemplate = Item.GetItemTemplateFromRef(reader.Name);
                                    template = iTemplate;
                                    if (iTemplate == null || iTemplate.blockActorFromAddingToTables)
                                    {
                                        safeToAddEntry = false;
                                        //Debug.Log("Couldn't find item for loot table " + refName + ": " + reader.Name);
                                    }
                                    else
                                    {
                                        safeToAddEntry = true;
                                        addActorToTable = true;
                                    }
                                    break;
                                case ActorTypes.MONSTER:
                                    MonsterTemplateData mTemplate;
                                    if (GameMasterScript.masterMonsterList.TryGetValue(reader.Name, out mTemplate))
                                    {
                                        safeToAddEntry = true;
                                        addActorToTable = false;
                                    }
                                    else
                                    {
                                        safeToAddEntry = false;
                                        Debug.Log("Couldn't find monster for spawn table " + refName + ": " + reader.Name);
                                    }
                                    break;
                            }

                            if (addActorToTable)
                            {
                                if (mergedWithExistingTable)
                                {
                                    if (!existingTableToAddTo.actors.Contains(template))
                                    {
                                        existingTableToAddTo.actors.Add(template);
                                    }
                                }
                                else
                                {
                                    actors.Add(template);
                                }                                
                            }
                        }
                                                
                        string itemRef = reader.Name;
                        int amount = reader.ReadElementContentAsInt();

                        if (safeToAddEntry)
                        {
                            if (mergedWithExistingTable)
                            {
                                existingTableToAddTo.AddToTable(itemRef, amount);
                            }
                            else
                            {
                                AddToTable(itemRef, amount);
                            }
                        }

                    }
                    else
                    {
                        reader.Read();
                    }

                    break;
            }
        }

        if (isBreakableTable)
        {
            GameMasterScript.masterBreakableSpawnTable = this;
        }

        reader.ReadEndElement();
    }

    public int id = 0;

    public ActorTable() {
        table = new Dictionary<string, int>();
        actors = new List<Actor>();
        allKeys = new List<string>();

        id = globalID;

        globalID++;
    }

}

// Goal: X actors are in the table. 40 actor A, 30 actor B, 10 actor C
// Actors are: string ref, and how many.
// Scan through table to find total.

public class ActorTableEntry
{
    
}