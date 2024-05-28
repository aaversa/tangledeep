using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public enum RoomCharTypes { STAIRS, TERRAIN, PLAYERSTART, NPC, MONSTER, DESTRUCTIBLE, TREE, RANDOMMONSTER, ITEM, FOOD, TREASURESPARKLE, ITEM_TYPE, COUNT }
public enum StairDirections { BACK, FORWARDS }

public class CharDefinitionForRoom
{
    public char symbol;
    public RoomCharTypes eCharType;
    public LocationTags locTag;
    public List<string> actorRef;
    public StairDirections stairDir;
    public bool changeTileType;
    public TileTypes eTileType;
    public bool actorEnabled;
    public int pointToFloor;
    public int pointToModLevelID;
    //public List<int> possibleFloors;
    public List<string> possibleActorTables;
    public int champMods;
    public Faction actorFaction;
    public List<LootPackage> extraLoot;
    public Directions actorDirection;
    public bool transparentStairs;
    public bool sleepUntilSeePlayer;
    public bool bDontSpawnAtMapGeneration;
    public string prefab;
    public Dictionary<string, int> startActorData;

    public CharDefinitionForRoom()
    {
        actorEnabled = true;
        possibleActorTables = new List<string>();
        actorRef = new List<string>();
        extraLoot = new List<LootPackage>();
        actorDirection = Directions.NEUTRAL;
        pointToFloor = -1;
        pointToModLevelID = -1;
        transparentStairs = false;
        sleepUntilSeePlayer = false;
        startActorData = new Dictionary<string, int>();
    }

    public string GetRandomActorRef()
    {
        if (actorRef.Count == 0)
        {
            Debug.Log("ERROR: Symbol " + symbol + " no actor refs?");
            return "";
        }
        return actorRef[UnityEngine.Random.Range(0, actorRef.Count)];
    }

}

public class LootPackage
{
    public float challengeValue;
    public float bonusMagicChance;
}

public class RoomBuilder : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public static CharDefinitionForRoom ReadCharDefinition(XmlReader reader)
    {
        reader.ReadStartElement();

        CharDefinitionForRoom charDef = new CharDefinitionForRoom();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            //Debug.Log(reader.Name + " " + reader.NodeType);
            switch(reader.Name.ToLowerInvariant())
            {
                case "symbol":
                    string temp = reader.ReadElementContentAsString();
                    charDef.symbol = char.Parse(temp);
                    break;
                case "setactordata":
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split(',');
                    charDef.startActorData.Add(parsed[0], Int32.Parse(parsed[1]));
                    break;
                case "chartype":
                    charDef.eCharType = (RoomCharTypes)Enum.Parse(typeof(RoomCharTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "tag":
                    charDef.locTag = (LocationTags)Enum.Parse(typeof(LocationTags), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "stairdirection":
                    charDef.stairDir = (StairDirections)Enum.Parse(typeof(StairDirections), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "actortable":
                    charDef.possibleActorTables.Add(reader.ReadElementContentAsString());
                    break;
                case "tiletype":
                    charDef.changeTileType = true;                    
                    charDef.eTileType = (TileTypes)Enum.Parse(typeof(TileTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "actorref":
                    charDef.actorRef.Add(reader.ReadElementContentAsString());
                    break;
                case "disableactor":
                    reader.ReadElementContentAsString();
                    charDef.actorEnabled = false;
                    break;
                case "transparentstairs":
                    reader.ReadElementContentAsInt();
                    charDef.transparentStairs = true;
                    break;
                case "pointstofloor":
                    charDef.pointToFloor = reader.ReadElementContentAsInt();
                    break;
                case "pointstomodlevelid":
                    charDef.pointToModLevelID = reader.ReadElementContentAsInt();
                    break;
                case "sleep":
                    charDef.sleepUntilSeePlayer = reader.ReadElementContentAsBoolean();
                    break;
                /* case "possiblefloor":
                charDef.possibleFloors.Add(reader.ReadElementContentAsInt());
                break; */
                case "who":
                case "faction":
                    charDef.actorFaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "actordirection":
                    charDef.actorDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "champmods":
                    charDef.champMods = reader.ReadElementContentAsInt();
                    break;
                case "prefab":
                    charDef.prefab = reader.ReadElementContentAsString();
                    break;
                case "bonusloot":
                    LootPackage lp = new LootPackage();
                    charDef.extraLoot.Add(lp);
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch(reader.Name.ToLowerInvariant())
                        {
                            case "challengevalue":
                                string cv = reader.ReadElementContentAsString();
                                float fCV = CustomAlgorithms.TryParseFloat(cv);
                                lp.challengeValue = fCV;
                                break;
                            case "bonusmagicchance":
                                string chance = reader.ReadElementContentAsString();
                                float fChance = CustomAlgorithms.TryParseFloat(chance);
                                lp.bonusMagicChance = fChance;
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "chardef":
                    Debug.Log("WARNING: Reading " + charDef.symbol + " and there is a CharDef which should be CharType");
                    reader.ReadElementContentAsString();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();

        return charDef;
    }
}
