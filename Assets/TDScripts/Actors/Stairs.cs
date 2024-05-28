using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Stairs : Actor
{
    public bool stairsUp = false;
    public bool isPortal = false;

    public Map NewLocation
    {
        get
        {
            Map outMap;
            MapMasterScript.dictAllMaps.TryGetValue(newLocationID, out outMap);
            return outMap;
        }

        set
        {
            if (value == null)
            {
                newLocationID = -1;
            }
            else
            {
                newLocationID = value.mapAreaID;
            }
        }
    }

    public int pointsToFloor;
    public int newLocationID;
    public bool autoMove = false;
    public bool pointsToSpecificTile;
    public int pointsToTileX;
    public int pointsToTileY;
    public bool usedByPlayer = false;

    public Stairs()
    {
        SetActorType(ActorTypes.STAIRS);
        NewLocation = null;
        actorRefName = "stairs";
        pointsToTileX = 0;
        pointsToTileY = 0;
        pointsToSpecificTile = false;
        pointsToFloor = -1;
    }

    protected override void Init()
    {
        if (initialized)
        {
            return;
        }
        base.Init();
        SetActorType(ActorTypes.STAIRS);
        NewLocation = null;
        playerCollidable = false;
        monsterCollidable = false;
    }

    public void SetDestination(int floor)
    {
        NewLocation = MapMasterScript.theDungeon.FindFloor(floor);
        if (NewLocation == null)
        {
            Debug.Log("WARNING: Tried to set destination for stairs on floor " + dungeonFloor + " to " + floor + " but that map doesn't exist!");
            return;
        }
        newLocationID = NewLocation.mapAreaID;
    }

    public void SetDestination(Map dMap)
    {
        /* if (dMap != null)
        {
            Debug.Log("For stairs on " + dungeonFloor + " try pointing to " + dMap.floor);
        } */
        NewLocation = dMap;
        if (dMap == null)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log("Null map destination for stairs at " + dungeonFloor + " " + GetPos());
            }
        }
        else
        {
            newLocationID = dMap.mapAreaID;
            pointsToFloor = dMap.floor;
        }
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("stairs");

        WriteCoreActorInfo(writer);

        if (isPortal)
        {
            writer.WriteElementString("portal", isPortal.ToString().ToLowerInvariant());
        }
        if (pointsToSpecificTile)
        {
            writer.WriteElementString("pointstox", pointsToTileX.ToString());
            writer.WriteElementString("pointstoy", pointsToTileY.ToString());
        }
        if (prefab == "MightyVine" || (!isPortal && prefab == "RedPortal")) // #todo - generalize this
        {
            writer.WriteElementString("prefab", prefab);
        }
        if (stairsUp)
        {
            writer.WriteElementString("up", "1");
        }
        if (usedByPlayer)
        {
            writer.WriteElementString("use", "1");
        }
        if (autoMove)
        {
            writer.WriteElementString("automove", "1");
        }

        if (!actorEnabled)
        {
            writer.WriteElementString("actorenabled", actorEnabled.ToString().ToLowerInvariant());
        }
        if (NewLocation != null)
        {
            writer.WriteElementString("tofloor", NewLocation.floor.ToString());
            writer.WriteElementString("locid", NewLocation.mapAreaID.ToString());
        }
        else
        {
            //Debug.Log("Trying to save stairs " + actorUniqueID + " at " + GetPos() + " on floor " + dungeonFloor + " but there is no location? StairsUp " + stairsUp);
        }

        WriteActorDict(writer);

        WriteCurrentPosition(writer);
        /* writer.WriteElementString("posx", GetPos().x.ToString());
        writer.WriteElementString("posy", GetPos().y.ToString()); 
        if (areaID != MapMasterScript.FILL_AREA_ID)
        {
            writer.WriteElementString("aid", areaID.ToString());
        } */

        writer.WriteEndElement();
        //Debug.Log("Wrote stairs on floor " + dungeonFloor + " map id " + actorMap.mapAreaID + " pointing  to " + newLocation.mapAreaID + " fl " + newLocation.floor);
    }



    public void ReadFromSave(XmlReader reader)
    {
        bool mapAssigned = false;

        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();

            switch (strValue)
            {
                case "cr":
                    mapAssigned = ReadCoreActorInfo(reader);
                    break;
                case "fl":
                case "floor":
                case "dungeonfloor":
                    dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "actorenabled":
                    _actorEnabled = reader.ReadElementContentAsBoolean();
                    break;
                case "mapid":
                case "actormap":
                    actorMapID = reader.ReadElementContentAsInt();
                    MapMasterScript.TryAssignMap(this, actorMapID);
                    mapAssigned = true;
                    break;
                case "dad":
                case "dictactordata":
                    ReadActorDict(reader);
                    break;
                case "id":
                case "uniqueid":
                    actorUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "portal":
                    isPortal = reader.ReadElementContentAsBoolean();
                    break;
                case "prefab":
                    prefab = reader.ReadElementContentAsString();
                    break;
                case "pointstox":
                    pointsToTileX = reader.ReadElementContentAsInt();
                    pointsToSpecificTile = true;
                    break;
                case "pointstoy":
                    pointsToTileY = reader.ReadElementContentAsInt();
                    pointsToSpecificTile = true;
                    break;
                case "locid":
                case "newlocationid":
                    newLocationID = reader.ReadElementContentAsInt();
                    break;
                case "aid":
                case "areaid":
                    areaID = reader.ReadElementContentAsInt();
                    break;
                case "stairsup":
                    stairsUp = reader.ReadElementContentAsBoolean();
                    break;
                case "up":
                    stairsUp = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "used":
                    usedByPlayer = reader.ReadElementContentAsBoolean();
                    break;
                case "use":
                    usedByPlayer = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "automove":
                    autoMove = reader.ReadElementContentAsBoolean();
                    break;
                case "auto":
                    autoMove = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "tofloor":
                case "pointstofloor":
                    pointsToFloor = reader.ReadElementContentAsInt();
                    break;
                case "pos":
                    ReadCurrentPosition(reader);
                    spawnPosition.x = GetPos().x;
                    spawnPosition.y = GetPos().y;
                    break;
                case "posx":
                    SetCurPosX(reader.ReadElementContentAsInt());
                    spawnPosition.x = GetPos().x;
                    break;
                case "posy":
                    SetCurPosY(reader.ReadElementContentAsInt());
                    spawnPosition.y = GetPos().y;
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        if (!mapAssigned && GetActorMap() == null)
        {
            TryAssignMapOnLoad();
        }

        GameMasterScript.AddActorToDict(this);
        reader.ReadEndElement();
    }
}