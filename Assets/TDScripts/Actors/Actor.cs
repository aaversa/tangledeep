using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

[System.Diagnostics.DebuggerDisplay("{actorRefName}")]
public class Actor
{
    public string actorRefName;
    private GameObject actorGO;
    private ActorTypes actorType;
    public InventoryScript myInventory;
    public int dungeonFloor; // Deprecate this
    public int actorMapID = -1;
    public Vector2 spawnPosition;
    Vector2 currentPosition;
    public Vector2 previousPosition;
    public int actorUniqueID;
    public string prefab;
    public string displayName;
    
    public bool acted;
    public bool skipTurn;
    public bool startedTurn;
    public bool blocksVision;
    public List<OverlayData> overlays;
    public int areaID;
    public bool movedByAnchorThisTurn;
    public int lastAreaVisited;
    public int turnsToDisappear;
    public int maxTurnsToDisappear;

    public int turnSummoned;

    public bool destroyed;
    /* bool det;
    public bool destroyed
    {
        get
        {
            return det;
        }
        set
        {
            det = value;
            if (GameMasterScript.gameLoadSequenceCompleted && actorType == ActorTypes.MONSTER)
            {
                Debug.Log(displayName + " " + actorRefName + " " + actorUniqueID + " " + det);
            }            
        }
    } */
    public bool initialized;

    /// <summary>
    /// If TRUE, when re-initializing, we do not need to recreate lists and such.
    /// </summary>
    public bool objectInitializedAtLeastOnce;

    public bool playerCollidable;
    public bool monsterCollidable;
    public bool isVisible;
    public Faction actorfaction;
    public bool targetable;
    public Fighter summoner;
    public bool actOnlyWithSummoner;
    public bool dieWithSummoner;
    public int summonerID;
    public int lastTurnActed;
    public Directions lastMovedDirection;
    public Directions lastCardinalDirection;

    public int timesActedThisTurn;

    public Actor anchor;
    public int anchorID;
    public int anchorRange;
    public bool isInDeadQueue;

    public bool visibleOnMinimap;

    public bool loadOnlyIfLocalized;
    public bool blockActorFromAddingToTables; // if TRUE during game load, this will not appear in spawn/loot tables etc.

    public bool excludeFromHotbarCheck; // if true, this actor ref is allowed to exist if summoned and you don't have skill on hotbar


    static List<GameObject> pool_toReturn = new List<GameObject>();
    public bool ignoreMeInTurnProcessing;

    public bool objectSet
    {
        get
        {
            return IsObjectAlive();
        }
        set
        {

        }
    }

    public Map GetActorMap()
    {
        Map returnMap = null;
        if (MapMasterScript.dictAllMaps.TryGetValue(actorMapID, out returnMap))
        {
            return returnMap;
        }

        //Debug.Log(actorRefName + " " + actorUniqueID + " has no map for ID " + actorMapID + " " + dungeonFloor);

        return null;
    }
    public Area GetActorArea()
    {
        Area returnArea = null;
        if (GetActorMap().areaDictionary.TryGetValue(areaID, out returnArea))
        {
            return returnArea;
        }
        return null;
    }
    public void SetActorArea(Area ar)
    {
        if (ar != null)
        {
            areaID = ar.areaID;
        }
    }
    public void SetActorMap(Map m)
    {
        if (m != null)
        {
            actorMapID = m.mapAreaID;
#if UNITY_EDITOR
            if (actorMapID == -1)
            {
                Debug.Log("Wait! Why is " + m.mapAreaID + " equal to -1?!");
            }
#endif
            dungeonFloor = m.floor;
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("Why are we setting NULL MAP for " + actorUniqueID + " " + actorRefName);
#endif
        }

    }

    public bool actorEnabled
    {
        get
        {
            return _actorEnabled;
        }
    }

    protected bool _actorEnabled = true; //moved this away from Init()
    public bool pushedThisTurn;

    public string spriteRefOnSummon;

    public static List<Actor> emptyActorList = new List<Actor>(0);
    public List<OverlayData> pool_overlayData;

    public Movable myMovable;
    public Animatable myAnimatable;
    public SpriteRenderer mySpriteRenderer;

    public bool flipSpriteY;
    public bool noAnimation;

    protected Dictionary<string, int> dictActorData;
    protected Dictionary<string, string> dictActorDataString;

    public float opacityMod;

    public AbilityUsageInstance ignoreEffectsOfAbility;

    public GameObject auraObject;

    //Removed from gameplay just not cleared out yet
    public bool bRemovedAndTakeNoActions;

    public void ResetTurnData()
    {
        acted = false;
        skipTurn = false;
        timesActedThisTurn = 0;
        movedByAnchorThisTurn = false;
        if (GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = this as Destructible;
            dt.spreadThisTurn = false;
            dt.movedThisTurn = false;
            dt.startCheckThisTurn = false;
        }

    }

    /// <summary>
    /// Causes the sprite to not display for some amount of time
    /// </summary>
    /// <param name="fTime">Seconds to wait until displaying sprite</param>
    public void SetDelayBeforeRendering(float fTime)
    {
        myMovable.StartCoroutine(myMovable.HideThenShowAfterSeconds(fTime, spriteRefOnSummon));
    }

    public bool IsHero()
    {
        return GetActorType() == ActorTypes.HERO;
    }

    public void ClearActorDictString()
    {
        dictActorDataString.Clear();
    }

    public virtual void MarkAsDestroyed(bool ignoreHealth = false)
    {
        destroyed = true;
    }
    public Dictionary<string, int> GetActorDataDict()
    {
        return dictActorData;
    }

    public Dictionary<string, string> GetActorDataStringDict()
    {
        return dictActorDataString;
    }

    public void WriteCoreActorInfo(XmlWriter writer)
    {
        if (actorUniqueID == 0 && dungeonFloor == 0)
        {
            // Nothing to write? Don't write it.
            return;
        }
        string build = actorUniqueID + "|" + dungeonFloor;
        if (GetActorMap() == null)
        {
            //Debug.Log(actorRefName + " " + actorUniqueID + " " + dungeonFloor + " does not have a map.");
        }
        else
        {
            build += "|" + GetActorMap().mapAreaID;
        }

        writer.WriteElementString("cr", build);
    }

    /// <summary>
    /// Returns TRUE if we assigned the actor's map from the master dict
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="assignToMap"></param>
    /// <returns></returns>
    public bool ReadCoreActorInfo(XmlReader reader, bool assignToMap = true)
    {
        //Debug.Log("Read core actor data for " + actorUniqueID + " " + actorRefName + " " + GetActorType());
        string readData = reader.ReadElementContentAsString();
        string[] parsed = readData.Split('|');
        Int32.TryParse(parsed[0], out actorUniqueID);
        Int32.TryParse(parsed[1], out dungeonFloor);

        bool debug = false;

        if (parsed.Length == 3 && assignToMap)
        {
            Int32.TryParse(parsed[2], out actorMapID);
            MapMasterScript.TryAssignMap(this, actorMapID, debug);
            return true;
        }
        return false;
        //Debug.Log("Data read: " + readData + ", node name is " + reader.Name + " node type is " + reader.NodeType);

    }

    public void WriteCurrentPosition(XmlWriter writer)
    {
        if (currentPosition.x == 0 && currentPosition.y == 0) return;

        string writeStr = currentPosition.x + "," + currentPosition.y;

        if (areaID != MapMasterScript.FILL_AREA_ID)
        {
            writeStr += "," + areaID.ToString();
        }

        writer.WriteElementString("pos", writeStr);
    }

    public void ReadCurrentPosition(XmlReader reader)
    {
        string readStr = reader.ReadElementContentAsString();
        float xPos = -1f;
        float yPos = -1f;

        string[] splitValues = readStr.Split(',');
        float.TryParse(splitValues[0], out currentPosition.x);
        float.TryParse(splitValues[1], out currentPosition.y);
        if (splitValues.Length == 3)
        {
            Int32.TryParse(splitValues[2], out areaID);
        }
        //Debug.Log("Position " + readStr + " leaves our reader at " + reader.Name + " " + reader.NodeType + " for actor " + actorRefName + " " + GetActorType() + " " + actorUniqueID);
    }

    public void ClearActorDict()
    {
        if (dictActorData == null)
        {
            dictActorData = new Dictionary<string, int>();
        }
        List<string> allKeys = dictActorData.Keys.ToList();

        // #todo - data drive which keys should not be reset
        if (dictActorData != null && GetActorType() == ActorTypes.HERO)
        {
            foreach (string dictKey in allKeys)
            {
                // If we "continue", that means we are NOT clearing this key.
                if (dictKey.Contains("champskilled_ready"))
                {
                    continue;
                }
                if (dictKey.Contains("legfound_"))
                {
                    // legendaries found persist into NG+
                    continue;
                }
                if (dictKey == "herbalist_quest" && dictActorData["herbalist_quest"] >= 4)
                {
                    if (allKeys.Contains("herbalist_resetonce"))
                    {
                        continue; // Do not reset this key!
                    }
                    else
                    {
                        dictActorData.Add("herbalist_resetonce", 1);
                    }                    
                }
                if (dictKey == "herbalist_resetonce")
                {
                    continue;
                }

                // We've already learned our ultimate weapon. Should we maintain this data?
                if (dictKey == "learned_ultimate_weapontech")
                {                    
                    if (allKeys.Contains("ultimate_weapontechlearned_resetonce"))
                    {
                        // We've reset at least once, so... yes.
                        continue;
                    }
                    else
                    {
                        // We'll allow it to be reset, but don't reset it again.
                        dictActorData.Add("ultimate_weapontechlearned_resetonce", 1);
                    }
                }
                if (dictKey == "ultimate_weapontechlearned_resetonce")
                {
                    continue;
                }
                if (dictKey == "armormaster_quest" && dictActorData["armormaster_quest"] == 3)
                {
                    if (allKeys.Contains("armormasteryquest_resetonce"))
                    {
                        continue;
                    }
                    else
                    {
                        dictActorData.Add("armormasteryquest_resetonce", 1);
                    }
                }
                if (dictKey == "armormasteryquest_resetonce")
                {
                    continue;
                }
                if (dictKey.Contains("infuse")) // Infusions are not deleted
                {
                    continue;
                }
                if (dictKey == "petinsurance")
                {
                    continue;
                }
                if (dictKey == "monsterpetid")
                {
                    continue;
                }
                if (dictKey == "bg")
                {
                    continue;
                }
                if (dictKey.Contains("entertext"))
                {
                    continue;
                }
                if (dictKey.Contains("tutorial"))
                {
                    continue;
                }
                if (dictKey.Contains("weaponmaster_callout"))
                {
                    continue;
                }
                if (dictKey == "kjpspent")
                {
                    continue;
                }
                if (dictKey.Contains("mastery"))
                {
                    continue;
                }
                if (dictKey.Contains("_leg_"))
                {
                    continue;
                }
                dictActorData.Remove(dictKey);
            }
            //dictActorData.Clear();
        }
        else if (dictActorData != null)
        {
            dictActorData.Clear();
        }
    }

    public bool HasActorDataString(string data1)
    {
        if (dictActorDataString == null)
        {
            return false;
        }
        if (dictActorDataString.ContainsKey(data1))
        {
            return true;
        }
        return false;
    }

    public bool HasActorData(string data1)
    {
        if (dictActorData == null)
        {
            return false;
        }
        if (dictActorData.ContainsKey(data1))
        {
            return true;
        }
        return false;
    }

    public string ReadActorDataString(string data1)
    {
        if (dictActorDataString == null)
        {
            return "";
        }
        if (dictActorDataString.ContainsKey(data1))
        {
            return dictActorDataString[data1];
        }
        return "";
    }

    public int ReadActorData(string data1)
    {
        if (dictActorData == null)
        {
            return -1;
        }
        if (dictActorData.ContainsKey(data1))
        {
            return dictActorData[data1];
        }
        //Debug.Log(actorRefName + " does not contain key " + data1);
        return -1;
    }

    public void SetActorDataString(string data1, string data2)
    {
        if (dictActorDataString == null)
        {
            dictActorDataString = new Dictionary<string, string>();
        }

        if (string.IsNullOrEmpty(data2))
        {
            Debug.Log("Warning! Trying to set " + actorRefName + " datastring " + data1 + " with null/empty data.");
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                GameLogScript.GameLogWrite("Warning! Trying to set " + actorRefName + " datastring " + data1 + " with null/empty data.", GameMasterScript.heroPCActor);
            }
        }

        if (dictActorDataString.ContainsKey(data1))
        {
            dictActorDataString[data1] = data2;
        }
        else
        {
            dictActorDataString.Add(data1, data2);
        }
    }

    public void SetActorData(string data1, int data2)
    {
        if (dictActorData == null)
        {
            dictActorData = new Dictionary<string, int>();
        }
        if (dictActorData.ContainsKey(data1))
        {
            dictActorData[data1] = data2;
        }
        else
        {
            dictActorData.Add(data1, data2);
        }


    }

    /// <summary>
    /// Literally ADDS the data2 value for key data1. Does not SET. It ADDS!
    /// </summary>
    /// <param name="data1"></param>
    /// <param name="data2"></param>
    public void AddActorData(string data1, int data2)
    {
        if (dictActorData == null)
        {
            dictActorData = new Dictionary<string, int>();
        }
        if (dictActorData.ContainsKey(data1))
        {
            dictActorData[data1] += data2;
        }
        else
        {
            dictActorData.Add(data1, data2);
        }
    }

    public void ReadActorDict(XmlReader reader)
    {
        // Start of "dictactordata"
        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.None)
            {
                reader.Read();
            }
            else if (reader.Name == "dummy")
            {
                reader.Read();
            }
            else
            {
                string name = reader.Name;
                int value = reader.ReadElementContentAsInt();
                SetActorData(name, value); // this WAS AddActorData, but that seems incorrect.
            }
        }
        reader.ReadEndElement();
    }

    public void ReadActorDictString(XmlReader reader)
    {
        // Start of "dictactordatastring"
        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.None)
            {
                reader.Read();
            }
            else if (reader.Name == "dummy")
            {
                reader.Read();
            }
            else
            {
                string name = reader.Name;
                string value = reader.ReadElementContentAsString();
                SetActorDataString(name, value);
            }
        }
        reader.ReadEndElement();
    }

    public void WriteActorDict(XmlWriter writer)
    {
        bool permanent = false;
        if (GetActorType() != ActorTypes.MONSTER && turnsToDisappear <= 0 && maxTurnsToDisappear <= 0)
        {
            permanent = true;
        }

        bool hasStringData = dictActorDataString != null && dictActorDataString.Keys.Count > 0;

        // Check for orb conversions
        if (actorType == ActorTypes.ITEM)
        {
            Item itm = this as Item;
            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable c = itm as Consumable;
                if (c.actorRefName == "orb_itemworld" && hasStringData)
                {
                    string modRef = c.GetOrbMagicModRef();
                    MagicMod mm = GameMasterScript.masterMagicModList[modRef];
                    SetActorData("orbid", mm.magicModID);
                    dictActorDataString.Remove("orbmagicmodref");
                }
            }
        }              

        if (dictActorData != null && dictActorData.Keys.Count > 0)
        {
            if (permanent)
            {
                dictActorData.Remove("excludefromhotbarcheck");
                dictActorData.Remove("markedforremoval");
            }
            if (dictActorData.Keys.Count > 0)
            {
                writer.WriteStartElement("dad");
                bool anyKeys = false;
                foreach (string key in dictActorData.Keys)
                {
                    if (permanent && (key == "excludefromhotbarcheck" || key == "markedforremoval"))
                    {
                        continue;
                    }

                    string writeKey = key;

                    if (actorType == ActorTypes.ITEM)
                    {
                        if (key == "guaranteerelic")
                        {
                            writeKey = "grc";
                        }
                        else if (key == "playerowned")
                        {
                            writeKey = "pwn";
                        }
                    }

                    anyKeys = true;
                    writer.WriteElementString(writeKey, dictActorData[key].ToString());
                    // End foreach loop
                }
                if (!anyKeys)
                {
                    writer.WriteElementString("nothing", 0.ToString());
                }
                writer.WriteEndElement();
            }
        }

        if (hasStringData)
        {
            if (permanent)
            {
                dictActorDataString.Remove("player_abil_summonref");
                if (dictActorDataString.Keys.Count > 0)
                {
                    writer.WriteStartElement("dads"); // dict actor data string
                    bool anyKeys = false;
                    foreach (string key in dictActorDataString.Keys)
                    {
                        if (string.IsNullOrEmpty(dictActorDataString[key]))
                        {
                            Debug.Log("Actor " + actorRefName + " " + actorUniqueID + " had a null entry for data string " + key);
                            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                            {
                                GameLogScript.GameLogWrite("Actor " + actorRefName + " " + actorUniqueID + " had a null entry for data string " + key, GameMasterScript.heroPCActor);
                            }
                            continue;
                        }
                        anyKeys = true;
                        writer.WriteElementString(key, dictActorDataString[key].ToString());
                    }
                    if (!anyKeys)
                    {
                        writer.WriteElementString("nothing", 0.ToString());
                    }
                    writer.WriteEndElement();
                }
            }
        }
    }

    public void RemoveActorData(string data1)
    {
        if (dictActorData == null) return;
        if (dictActorData.ContainsKey(data1))
        {
            dictActorData[data1] = 0;
            dictActorData.Remove(data1);
        }
    }

    public void RemoveActorDataString(string data1)
    {
        if (dictActorDataString == null) return;
        if (dictActorDataString.ContainsKey(data1))
        {
            dictActorDataString.Remove(data1);
        }
    }

    bool IsObjectAlive()
    {
        if (actorGO == null || !actorGO.activeSelf) return false;
        return true;
    }

    public Actor()
    {
        Init();
    }

    public bool CheckIgnoreAbility(AbilityScript abil)
    {
        if (ignoreEffectsOfAbility == null) return false;
        return (CombatManagerScript.CheckIfAbilityInstanceMatchesLastUsedAbility(ignoreEffectsOfAbility));
    }

    public void VerifySpritePositionIsAccurate()
    {
        if (actorGO == null || !GetObject().activeSelf)
        {
            Debug.Log(actorRefName + " (" + actorUniqueID + ") at " + GetPos() + " has no object, or its object is inactive. Why would that be?");
            return;
        }

        if (actorGO.transform.parent == GameMasterScript.goPooledObjectHolder)
        {
#if UNITY_EDITOR
            Debug.LogError(actorRefName + " " + actorUniqueID + " is using a NON-PUSHED POOLED OBJECT for some reason!!");
#else 
            Debug.Log(actorRefName + " " + actorUniqueID + " is using a NON-PUSHED POOLED OBJECT for some reason!!");
#endif
            if (GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = this as Monster;
                //Debug.Log("Alive state? " + mn.myStats.IsAlive());
            }
        }

        if (myMovable.IsMoving())
        {
            return;
        }

        if (Mathf.Abs(actorGO.transform.position.x - currentPosition.x) > 0.05f || Mathf.Abs(actorGO.transform.position.y - currentPosition.y) > 0.05f)
        {
            currentPosition.x = Mathf.Round(currentPosition.x);
            currentPosition.y = Mathf.Round(currentPosition.y);
            myMovable.AnimateSetPosition(currentPosition, 0.01f, false, 0f, 0f, MovementTypes.LERP);
        }

    }

    public virtual void WriteToSave(XmlWriter writer)
    {
    }

    //Removes the actor from gameplay right away, it does not get another turn
    public virtual void RemoveImmediately()
    {
        //depends on whatever the child needs to do to die correctly
        bRemovedAndTakeNoActions = true;
    }

    public bool IsFighter()
    {
        if (actorType == ActorTypes.MONSTER || actorType == ActorTypes.HERO)
        {
            return true;
        }
        return false;
    }

    public virtual void UpdateSpriteOrder(bool turnEnd = false)
    {
        try
        {
            Internal_UpdateSpriteOrder(turnEnd);
        }
        catch (Exception e)
        {
            Debug.Log("ERROR updating sprite order " + actorRefName + " " + actorUniqueID + ": " + e);
            return;
        }
    }

    private void Internal_UpdateSpriteOrder(bool turnEnd = false)
    {
        bool mapLoaded = MapMasterScript.mapLoaded;

        if (mapLoaded && !MapMasterScript.InBounds(currentPosition)) return;

        if (myMovable != null && objectSet)
        {
            if (!myMovable.srFound)
            {
                myMovable.sr = GetObject().GetComponent<SpriteRenderer>();
                if (myMovable.sr != null)
                {
                    myMovable.srFound = true;
                }
            }
            if (myMovable.srFound)
            {
                int newOrder = (120 - (int)currentPosition.y) * 10; // Multiplying x10 gives us more options for sort order on a single tile.

                myMovable.sr.sortingOrder = newOrder;

                if (GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = this as Destructible;
                    if (dt.mapObjType == SpecialMapObject.POWERUP)
                    {
                        myMovable.sr.sortingOrder += 4; // make powerups pop!
                    }
                    else if (actorRefName != "obj_plank")
                    {
                        myMovable.sr.sortingOrder--;
                        if (actorRefName == "obj_voidtile")
                        {
                            // Instead of -1, why not make the void tiles -10 so characters never get stuck behind them?
                            // This seems to work OK.
                            myMovable.sr.sortingOrder -= 10;
                        }
                    }
                    if (dt.turnsToDisappear > 0)
                    {
                        myMovable.sr.sortingOrder += 2;
                    }
                }
                else if (mapLoaded && GetActorType() == ActorTypes.STAIRS && !MapMasterScript.activeMap.IsTownMap())
                {
                    // Stairs can also be brought down an entire layer (10 instead of 1)
                    // Because otherwise, creatures and actors can end up behind the stairs
                    // That looks jank
                    myMovable.sr.sortingOrder -= 10;
                }
                else if (GetActorType() == ActorTypes.ITEM)
                {
                    myMovable.sr.sortingOrder--;
                    if (mapLoaded && MapMasterScript.activeMap.GetTile(GetPos()).CheckTag(LocationTags.ISLANDSWATER))
                    {
                        myMovable.sr.sortingOrder++; // new 12/25 to prevent weirdness in item dreams w/ planks...
                    }
                }
                else if (GetActorType() == ActorTypes.MONSTER || GetActorType() == ActorTypes.HERO)
                {
                    // Deadly Voids cause weird sprite rendering issues because of how they stack on things
                    if (mapLoaded && MapMasterScript.activeMap.GetTile(GetPos()).CheckTag(LocationTags.ISLANDSWATER))                        
                    {
                        // lets make absolutely sure we are drawing ON TOP of the deadly void on our tile
                        bool anyActor = false;
                        Actor voidActor = MapMasterScript.activeMap.GetTile(GetPos()).FindActorByRef("obj_voidtile", out anyActor);
                        if (anyActor)
                        {
                            myMovable.sr.sortingOrder += 10;
                            if (myMovable.sr.sortingOrder < voidActor.mySpriteRenderer.sortingOrder)
                            {
                                myMovable.sr.sortingOrder = voidActor.mySpriteRenderer.sortingOrder + 10;
                            }
                        }
                    }
                }
                if (GetActorType() == ActorTypes.HERO)
                {
                    if (previousPosition.y < GetPos().y)
                    {
                        // We are moving UP
                        bool adjustHeight = false;
                        if (GameMasterScript.gameLoadSequenceCompleted)
                        {
                            MapTileData below = MapMasterScript.GetTile(new Vector2(GetPos().x, GetPos().y - 1f));
                            if (below.CheckHasExtraHeight(1))
                            {
                                adjustHeight = true;
                            }
                        }
                        if (!adjustHeight)
                        {
                            myMovable.sr.sortingOrder++;
                        }

                        if (turnEnd)
                        {
                            myMovable.sr.sortingOrder += 10; // We want to make sure it LOOKS like our sorting order changes at end of move
                            myMovable.WaitThenChangeSortingOrder(GameMasterScript.gmsSingleton.playerMoveSpeed * 0.9f, -10);
                        }
                        
                    }
                    if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.GetTile(GetPos()).GetStairsInTile() != null)
                    {
                        myMovable.sr.sortingOrder++;
                    }
                }

                myMovable.sr.sortingOrder += myMovable.sortOrderOffset;
            }
        }
    }

    public void CreateNewInventory(bool initializedEver = false)
    {
        if (!initializedEver)
        {
            myInventory = new InventoryScript();
        }
        else
        {
            if (actorRefName == "npc_banker")
            {
                if (Debug.isDebugBuild) Debug.Log("Don't clear the banker's inventory");
            }
            else
            {
                myInventory.ClearInventory();
            }
            
        }
        
        myInventory.Owner = this;
    }

    public virtual void UpdateLastMovedDirection(Directions dir)
    {
        if (dir == Directions.EAST || dir == Directions.WEST || dir == Directions.SOUTH || dir == Directions.NORTH)
        {
            lastCardinalDirection = dir;
        }
        lastMovedDirection = dir;
        if (myAnimatable == null) return;
        if ((dir == Directions.WEST) || (dir == Directions.NORTHWEST) || (dir == Directions.SOUTHWEST))
        {
            myAnimatable.OrientSprite(Directions.WEST);
        }
        else if ((dir == Directions.EAST) || (dir == Directions.NORTHEAST) || (dir == Directions.SOUTHEAST))
        {
            myAnimatable.OrientSprite(Directions.EAST);
        }
    }

    protected virtual void Init()
    {
        
        destroyed = false;
        isInDeadQueue = false;

        actorGO = null;
        actorRefName = "";
        actorUniqueID = 0;
        dungeonFloor = 0;
        actorMapID = -1;
        spawnPosition = Vector2.zero;
        currentPosition = Vector2.zero;
        previousPosition = Vector2.zero;
        startedTurn = false;
        blocksVision = false;
        areaID = 0;
        lastAreaVisited = 0;
        destroyed = false;
        isVisible = false;
        actorfaction = Faction.NONE;
        summoner = null;
        actOnlyWithSummoner = false;
        dieWithSummoner = false;
        summonerID = 0;
        lastTurnActed = 0;
        timesActedThisTurn = 0;
        anchor = null;
        anchorID = 0;
        anchorRange = 0;
        isInDeadQueue = false;
        blockActorFromAddingToTables = false;
        excludeFromHotbarCheck = false;
               
        visibleOnMinimap = true;
        displayName = "";
        actorGO = null;
        prefab = null;
        acted = false;
        skipTurn = false;
        playerCollidable = true;
        monsterCollidable = true;
        targetable = true;
        CreateNewInventory(objectInitializedAtLeastOnce);
        turnsToDisappear = -1;
        maxTurnsToDisappear = -1;
        movedByAnchorThisTurn = false;

        if (!objectInitializedAtLeastOnce)
        {
            pool_overlayData = new List<OverlayData>();
        }
        else
        {
            pool_overlayData.Clear();
        }
        
        areaID = MapMasterScript.FILL_AREA_ID;
        opacityMod = 1.0f;

        //This was never getting set! 
        initialized = true;
    }

    public void SetOpacity(float opacityAmt)
    {
        opacityMod = opacityAmt;
        if (myAnimatable != null)
        {
            myAnimatable.opacityMod = opacityMod;
        }
        myMovable.SetColor(new Color(myMovable.forceColor.r, myMovable.forceColor.g, myMovable.forceColor.g, opacityAmt));
    }

    public void EnableActor()
    {
        //Debug.Log("Enabling " + actorRefName);
        _actorEnabled = true;
        if (!objectSet)
        {
            // Debug.Log(actorRefName + " " + GetPos() + " has no object to enable!");
        }
        else
        {
            GetObject().GetComponent<Movable>().FadeIn();
        }
    }

    public void DisableActor()
    {
        _actorEnabled = false;
        if (!objectSet)
        {
            //Debug.Log(actorRefName + " " + GetPos() + " has no object to disable!");
        }
        else
        {
            GetObject().GetComponent<Movable>().SetInSightAndSnapEnable(false);
        }
    }

    public void SetUniqueIDAndAddToDict()
    {
        GameMasterScript.AssignActorID(this);
        GameMasterScript.AddActorToDict(this);

    }

    public void HideOverlays()
    {
        if (overlays == null)
        {
            return;
        }
        for (int i = 0; i < overlays.Count; i++)
        {
            overlays[i].overlayGO.GetComponent<SpriteEffect>().SetBaseVisible(false);
        }
    }

    public void ShowOverlays()
    {
        if (overlays == null)
        {
            return;
        }
        for (int i = 0; i < overlays.Count; i++)
        {
            overlays[i].overlayGO.GetComponent<SpriteEffect>().SetBaseVisible(true);
        }
    }

    public List<OverlayData> GetIntermittentOverlays()
    {
        pool_overlayData.Clear();

        for (int i = 0; i < overlays.Count; i++)
        {
            if (!overlays[i].alwaysDisplay)
            {
                pool_overlayData.Add(overlays[i]);
            }
        }

        return pool_overlayData;
    }

    public void SetOverlaysCurVisibility(bool state)
    {
        if (overlays == null)
        {
            return;
        }
        for (int i = 0; i < overlays.Count; i++)
        {
            if ((overlays[i].overlayGO == null) || (!overlays[i].overlayGO.activeSelf))
            {
                continue;
            }
            if (overlays[i].alwaysDisplay)
            {
                overlays[i].overlayGO.GetComponent<SpriteEffect>().SetCurVisible(true);
            }
            else
            {
                overlays[i].overlayGO.GetComponent<SpriteEffect>().SetCurVisible(state);
            }
        }
    }

    public void AddOverlay(GameObject go, bool alwaysDisplay)
    {
        if (overlays == null)
        {
            overlays = new List<OverlayData>(3);
        }
        if (go == null)
        {
            Debug.Log("Trying to add null overlay?");
            return;
        }
        if (go.GetComponent<SpriteEffect>() == null)
        {
            Debug.Log("No sprite effect for " + go.name);
            return;
        }
        OverlayData od = new OverlayData();
        od.overlayGO = go;
        od.alwaysDisplay = alwaysDisplay;
        overlays.Add(od);
        od.overlayGO.GetComponent<SpriteEffect>().SetCurVisible(true);
        od.overlayGO.GetComponent<SpriteEffect>().SetAlwaysVisible(alwaysDisplay);
    }

    public void RemoveOverlay(GameObject go)
    {
        if (overlays == null)
        {
            return;
        }
        for (int i = 0; i < overlays.Count; i++)
        {
            if (overlays[i].overlayGO == go)
            {
                overlays.Remove(overlays[i]);
                break;
            }
        }
        /* if (overlays.Count == 0)
        {
            if (GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = this as Monster;
                if (mn.healthBar != null)
                {
                    mn.healthBar.hasOverlays = false;
                }
                
            }
        } */
    }

    public void RemoveOverlays()
    {
        if (overlays == null)
        {
            return;
        }
        SpriteEffect se;
        foreach (OverlayData od in overlays)
        {
            if ((od.overlayGO == null) || (!od.overlayGO.activeSelf))
            {
                continue;
            }
            se = od.overlayGO.GetComponent<SpriteEffect>();
            if (se != null)
            {
                GameMasterScript.ReturnToStack(od.overlayGO, se.refName);

                SpriteEffectSystem ses = od.overlayGO.GetComponent<SpriteEffectSystem>();
                if (ses != null)
                {
                    foreach (Transform child in ses.gameObject.transform)
                    {
                        GameMasterScript.ReturnToStack(child.gameObject, child.GetComponent<SpriteEffect>().refName);
                    }
                }

            }
            else
            {
                //GameObject.Destroy(od.overlayGO);
                GameMasterScript.ReturnToStack(od.overlayGO, od.overlayGO.name.Replace("(Clone)", String.Empty));
            }
        }
        overlays.Clear();
    }

    public void SetOverlayObject(GameObject go)
    {
        go.transform.SetParent(actorGO.transform);
        go.transform.position = actorGO.transform.position;
    }

    public GameObject GetObject()
    {
        return actorGO;
    }

    public Vector2 GetPos()
    {
        return currentPosition;
    }

    public Vector2 GetSpawnPos()
    {
        return spawnPosition;
    }

    public void SetSpawnPosXY(int x, int y)
    {
        spawnPosition = new Vector2(x, y);
        currentPosition = spawnPosition;
    }

    public void SetSpawnPos(Vector2 v2)
    {
        spawnPosition = v2;
        currentPosition = spawnPosition;
    }

    public void SetPos(Vector2 newPos)
    {
        spawnPosition = newPos;
    }

    public void SetCurPosX(float x)
    {
        currentPosition.x = x;
    }

    public void SetCurPosY(float y)
    {
        currentPosition.y = y;
    }

    public void SetCurPos(Vector2 v2)
    {
        if (v2 != currentPosition)
        {
            previousPosition = currentPosition;
        }
        currentPosition = v2;
    }

    public void SetObject(GameObject go, bool doTransLayer = true)
    {
        if (go != null)
        {
            //objectSet = true; Deprecating this...
            actorGO = go;
            myMovable = go.GetComponent<Movable>();
            myMovable.SetOwner(this);
            myMovable.Initialize();
            myAnimatable = go.GetComponent<Animatable>();
            if (myAnimatable != null)
            {
                myAnimatable.SetOwner(this);
            }            
            mySpriteRenderer = go.GetComponent<SpriteRenderer>();
            mySpriteRenderer.color = Color.white; 
if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
            mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
}
            if (!myMovable.transparentStairs)
            {
            if (doTransLayer)
            {
                CreateTransparencyLayerIfNeeded();
            }
            else // Don't create a new TransLayer, but if we already have one, make sure it's initialized to default
            {
                FindExistingTransparencyLayerAndInitialize();
                }
            }

            // Check our child transforms for any lingering Buff effects, because this is happening for unknown reasons.
            pool_toReturn.Clear();
            foreach (Transform t in go.transform)
            {
                if (t.name.Contains("FervirBuff"))
                {
                    pool_toReturn.Add(t.gameObject);
                }
            }
            foreach(GameObject checkGO in pool_toReturn)
            {
                GameMasterScript.ReturnToStack(checkGO, checkGO.name.Replace("(Clone)", String.Empty));
            }
        }
        else
        {
            Debug.Log("Null object");
        }
    }

    public void SetActorType(ActorTypes type)
    {
        actorType = type;
    }

    public ActorTypes GetActorType()
    {
        return actorType;
    }

    //Default nice best range is 2.
    public void SetAnchor(Actor actIShouldFollow, int range = 2)
    {
        anchorRange = range;
        anchor = actIShouldFollow;
        anchorID = actIShouldFollow.actorUniqueID;
    }

    protected virtual bool HasAnchor(Actor act)
    {
        return anchor == act;
    }

    public void ClearAnchor()
    {
        anchor = null;
        anchorID = 0;
        anchorRange = 0;
    }

    public void RemoveSelfFromMap()
    {
        MapMasterScript.activeMap.RemoveActorFromLocation(GetPos(), this);
        MapMasterScript.activeMap.RemoveActorFromMap(this);
        if (objectSet && myMovable != null)
        {
            myMovable.FadeOutThenDie();
        }
    }

    public void CreateTransparencyLayerIfNeeded()
    {
        if (FindExistingTransparencyLayerAndInitialize())
        {
            // We already have one and we've initialized it, nothing more to do here.
            return;
        }
        // Create a new layer and make sure it has default/initialized settings.
        GameObject transLayer = GameMasterScript.TDInstantiate("TransLayer");
        transLayer.transform.SetParent(GetObject().transform);
        transLayer.transform.localScale = Vector3.one;
        transLayer.transform.localEulerAngles = Vector3.zero; 
    }

    public bool FindExistingTransparencyLayerAndInitialize()
    {
        foreach (Transform t in GetObject().transform)
        {
            if (t.name.Contains("TransLayer"))
            {
                t.localScale = Vector3.one;
                t.localEulerAngles = Vector3.zero;
                return true;
            }
        }
        return false;
    }

    GameObject FindExistingOrCreateAuraMesh()
    {
        GameObject mesh = null;
        bool meshFound = false;
        foreach (Transform t in GetObject().transform)
        {
            if (t.name.Contains("TargetingMesh"))
            {
                mesh = t.gameObject;
                meshFound = true;
                break;                
            }
        }
        if (!meshFound)
        {
            mesh = GameMasterScript.TDInstantiate("TargetingMesh");
        }

        mesh.transform.SetParent(GetObject().transform);
        
        Vector3 actorPos = GetPos();
        mesh.transform.localPosition = new Vector3(actorPos.x * -1f, actorPos.y * -1f, 0f);

        TargetingMeshScript tms = mesh.GetComponent<TargetingMeshScript>();
        tms.goodColor = Color.green;
        tms.goodTiles.Clear();
        tms.badTiles.Clear();
        tms.usedTiles.Clear();
        tms.reqOpenTiles.Clear();

        return mesh;
    }

    public void BuildDangerMeshIfNeeded()
    {
        GameObject auraMesh = FindExistingOrCreateAuraMesh();
        TargetingMeshScript tms = auraMesh.GetComponent<TargetingMeshScript>();
        CustomAlgorithms.GetTilesAroundPoint(GetPos(), 1, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            tms.goodTiles.Add(CustomAlgorithms.tileBuffer[i].pos);
        }
        if (actorfaction == Faction.PLAYER || actorRefName == "obj_shrapnelbomb")
        {
            tms.goodColor = Color.green;
        }
        else
        {
            tms.goodColor = Color.red;
        }
        tms.maxTransparency = 0.29f;
        tms.minTransparency = 0.06f;
        tms.transparencyInterval = 0.05f;
        tms.BuildMesh();
    }

    public void BuildItemWorldAuraIfNeeded()
    {
        int getAura = ReadActorData("itemworldaura");
        if (getAura == -1)
        {
            getAura = 0;
        }
        else if (getAura == (int)ItemWorldAuras.BLESSEDPOOL)
        {
            SetActorData("itemworldaura", getAura - 1);
        }

        //Debug.Log("Building aura for " + actorUniqueID + " " + getAura);

        BuildAura(3, getAura);    
    }

    public void BuildNightmareCrystalAuraIfNeeded()
    {
        GameObject auraMesh = FindExistingOrCreateAuraMesh();

        TargetingMeshScript tms = auraMesh.GetComponent<TargetingMeshScript>();
        tms.goodTiles.Clear();
        CustomAlgorithms.GetTilesAroundPoint(GetPos(), 2, MapMasterScript.activeMap);
        for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
        {
            tms.goodTiles.Add(CustomAlgorithms.tileBuffer[x].pos);
        }
        tms.goodColor = new Color(UnityEngine.Random.Range(0.15f, 0.8f), UnityEngine.Random.Range(0.15f, 0.8f), UnityEngine.Random.Range(0.1f, 0.8f));
        tms.maxTransparency = 0.36f;
        tms.minTransparency = 0.1f;
        tms.transparencyInterval = 0.05f;
        tms.BuildMesh();
    }

    public virtual bool IsLocalized()
    {
        return true;
    }

    public void UpdateMyAnchor()
    {
        if (anchor != null)
        {
            //drop our anchor if dead, or if we are somehow anchored to a non-fighter (which should never happen)
            if (anchor.destroyed || (!anchor.IsFighter() && anchor.actorUniqueID != actorUniqueID))
            {
                anchor = null;
                //if we are a pet/summon of the player, re-anchor to her
                if (actorfaction == Faction.PLAYER && GameMasterScript.heroPCActor.CheckSummon(this))
                {
                    SetAnchor(GameMasterScript.heroPCActor);
                }
            }
        }
    }

    /// <summary>
    ///  Checks that the loaded actor is not in a wall or out-of-bounds position. If it is, relocates the actor. Returns FALSE if actor could not be added to map due to critical failure.
    /// </summary>
    public bool VerifyLoadPositionIsValidThenAddToMap()
    {
        if (!GetActorMap().AddActorToMap(this)) // If this fails, the thing was already in map. Exit now, no need to do anything else.
        {
            return true;
        }

        bool positionValid = true;
        bool inMapAtAll = true;

        MapTileData addPosition = null;

        // Are we out of bounds?
        if (currentPosition.x >= GetActorMap().columns || currentPosition.y >= GetActorMap().rows || currentPosition.x < 0 || currentPosition.y < 0)
        {
#if UNITY_EDITOR
            Debug.Log(actorRefName + " " + actorUniqueID + " " + dungeonFloor + " Outside of map bounds at " + GetPos());
#endif
            positionValid = false;
            inMapAtAll = false;
        }
        else // Maybe not, but maybe our spawn tile is a wall.
        {
            MapTileData check = GetActorMap().GetTile(GetPos());
            if (check != null && check.tileType == TileTypes.WALL)
            {
                // UH OH, WHY IS ACTOR IN A WALL?
                //Debug.Log("Uh oh, " + actorUniqueID + " " + actorRefName + " is loading into a wall on floor " + GetActorMap().floor + " pos: " + GetPos());
                positionValid = false;
            }
        }

        if (!positionValid)
        {
            addPosition = GetActorMap().GetRandomEmptyTileForMapGen();
            if (addPosition == null)
            {
#if UNITY_EDITOR
                Debug.Log("Failure to add " + actorUniqueID + " " + actorRefName + " to floor " + GetActorMap().floor);
#endif
                return false;
            }
            SetCurPos(addPosition.pos);
            SetSpawnPos(addPosition.pos); // necessary for destructibles, as they visually spawn based on this
        }
        else
        {
            addPosition = GetActorMap().GetTile(GetPos());
        }

        if (inMapAtAll && !positionValid) // If our previous position was invalid but not out of bounds...
        {
            GetActorMap().RemoveActorFromLocation(GetPos(), this);
        }
        
        GetActorMap().AddActorToLocation(addPosition.pos, this);

        return true;
    }

    public void ChangeMyFaction(Faction f)
    {
        actorfaction = f;
    }

    /// <summary>
    /// Called if we have a NULL map on load. Tries to assign our actormap.
    /// </summary>
    public void TryAssignMapOnLoad()
    {
        int idToTry = actorMapID == -1 ? dungeonFloor : actorMapID;
        MapMasterScript.TryAssignMap(this, idToTry);
        if (GetActorMap() == null)
        {
            Debug.LogError("STILL no map for " + actorRefName + " " + actorUniqueID + " on " + dungeonFloor + " mapid " + actorMapID);
        }
    }

    public void BuildAura(int radius, int auraIndex)
    {
        GameObject auraMesh = FindExistingOrCreateAuraMesh();

        TargetingMeshScript tms = auraMesh.GetComponent<TargetingMeshScript>();
        CustomAlgorithms.GetTilesAroundPoint(GetPos(), radius, MapMasterScript.activeMap);
        tms.goodTiles.Clear();
        for (int x = 0; x < CustomAlgorithms.numTilesInBuffer; x++)
        {
            tms.goodTiles.Add(CustomAlgorithms.tileBuffer[x].pos);
        }
        
        tms.goodColor = EffectScript.itemWorldAuraColors[auraIndex];
        tms.maxTransparency = 0.32f;
        tms.minTransparency = 0.06f;
        tms.transparencyInterval = 0.04f;
        tms.BuildMesh();
    }

    public bool QuickCompareTo(Actor a)
    {
        if (actorRefName != a.actorRefName)
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong ref name");
            return false;
        }
        if (GetActorType() != a.GetActorType())
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong actor type");
            return false;
        }

        if (GetActorType() == ActorTypes.MONSTER)
        {
            Monster m2 = this as Monster;
            Monster m1 = a as Monster;
            if (m1.tamedMonsterStuff != null && m2.tamedMonsterStuff != null)
            {
                if (!m1.tamedMonsterStuff.CompareTo(m2.tamedMonsterStuff))
                {
                    //if (Debug.isDebugBuild) Debug.Log("TCM doesn't match");
                    return false;
                }
                if (m1.tamedMonsterStuff.sharedBankID == m2.tamedMonsterStuff.sharedBankID)
                {
                    // No matter what, if we have the same shared bank ID, we must be the same creature.
                    return true;
                }
            }

            if (m1.myStats.GetLevel() != m2.myStats.GetLevel())
            {
                //if (Debug.isDebugBuild) Debug.Log("Wrong level");
                return false;
            }
        }

        if (prefab != a.prefab)
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong prefab");
            return false;
        }
        if (actorfaction != a.actorfaction)
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong faction");
            return false;
        }
        if (displayName != a.displayName)
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong displayname");
            return false;
        }


        if (myInventory.GetInventory().Count != a.myInventory.GetInventory().Count)
        {
            //if (Debug.isDebugBuild) Debug.Log("Wrong inv count");
            return false;
        }

        return true;
    }

    public bool IsCorralPetAndInCorral()
    {
        if (GetActorType() != ActorTypes.MONSTER) return false;
        Monster mn = this as Monster;
        if (mn.tamedMonsterStuff == null) return false;
        if (!mn.isInCorral) return false;

        /* if (GameMasterScript.gmsSingleton.gameMode == GameModes.HARDCORE ||
            GameStartData.challengeType != ChallengeTypes.NONE ||
            RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return false;
        } */

        //if (Debug.isDebugBuild) Debug.Log("Do not save local copy of " + mn.actorRefName + "," + mn.displayName + "," + mn.tamedMonsterStuff.sharedBankID);

        return true;
    }
}