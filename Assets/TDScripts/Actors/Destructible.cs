using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public enum DestructibleStepTrigger { ANY, HERO, HEROFACTION, NOTMYFACTION, ENEMYMONSTER, ANYMONSTER, COUNT }

public class DTPooling
{
    public const int START_DESTRUCTIBLE_POOL_SIZE = 40000;

    static Stack<Destructible> poolDestructibles;

    static bool initialized;

    static int masterUniqueID = 0;

    public static void Initialize()
    {
        if (initialized) return;

        poolDestructibles = new Stack<Destructible>(START_DESTRUCTIBLE_POOL_SIZE);

        for (int i = 0; i < START_DESTRUCTIBLE_POOL_SIZE; i++)
        {
            Destructible dt = new Destructible();
            dt.pooled = true;
            poolDestructibles.Push(dt);
            dt.tempDebugID = masterUniqueID;
            dt.tempDebugID++;
        }

        initialized = true;
    }

    public static void ReturnToPool(Destructible dt)
    {
        if (dt.pooled)
        {
            return;
        }
        poolDestructibles.Push(dt);
        dt.pooled = true;
        //Debug.Log("Returning " + dt.actorUniqueID + " to pool " + poolDestructibles.Count);
    }

    public static Destructible GetDestructible()
    {
        if (poolDestructibles.Count == 0)
        {
#if UNITY_EDITOR
            //Debug.Log("Had to expand destructible pool by 10.");
#endif
            for (int i = 0; i < 10; i++)
            {
                Destructible ndt = new Destructible();
                ndt.tempDebugID = masterUniqueID;
                masterUniqueID++;
                poolDestructibles.Push(ndt);
            }
        }

        Destructible dt = poolDestructibles.Pop();
        dt.ResetToInitializedState();
        dt.pooled = false;

        return dt;
    }
}

public class Destructible : Actor
{
    public int tempDebugID;

    public string deathPrefab;
    public string destroyedPrefab;
    public SpecialMapObject mapObjType;
    public float moneyChance;
    public int moneyHeld;
    public bool hasDestroyedState;
    public bool isDestroyed;
    public bool autoSpawn;
    public bool monsterDestroyable;
    public StatusEffect dtStatusEffect;
    public string statusRef;
    public Spread spreadType;
    public Spread changeSpreadAfterInitial;
    public Spread movementType;
    public bool spreadThisTurn;
    public bool movedThisTurn;
    public bool hoverDisplay;
    public bool startCheckThisTurn;
    public int stopSpreadThreshold;
    public int turnsAlive;
    public bool transparent;
    public bool destroyOnStep;
    public bool destroyOnWallHit;
    public bool runEffectOnLastTurn;
    public bool runEffectOnlyOnce;
    public bool runEffectNoMatterWhatIsOnMe;

    public bool dieAfterRunEffect;
    public bool dieAfterSpread;
    public bool showDirection;
    public bool rotateToMoveDirection;
    public GameObject directionIndicator;
    public bool passThroughAnything;
    public bool[] spawnInVisualSet;
    public string extraActorReference;
    public string reqDestroyItem;
    public string dialogRef;
    public bool hasDialog;
    public bool[] objectFlags;

    public int maxItems;
    public float lootChance;
    public float bonusLootValue;
    public float bonusMagicChance;
    public float bonusLegendaryChance;
    public int minItems;
    public int minMagicItems;

    public List<Vector2> dtSpreadPositions;

    public string monsterAttached;

    public List<string> prefabOptions;

    public bool replaceRef; // does not need to be serialized
    public bool isTerrainTile;
    public DestructibleStepTrigger stepTrigger;

    public bool pooled;

    public string playerPrefab; // if summoned by Player or friendly creature, use this prefab instead of our normal one.

    public Destructible()
    {
        Init();
    }

    // Checks if this Destructible executes a damaging effect when destroyed
    // Range is the distance between the destructible and attacker. Some objects only deal damage at melee    
    public bool HasHarmfulDeathStatusEffectAtRange(int range)
    {
        if (dtStatusEffect == null) return false;
        if (!dtStatusEffect.CheckRunTriggerOn(StatusTrigger.DESTROYED)) return false;
        foreach(EffectScript eff in dtStatusEffect.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                if (eff.tActorType == TargetActorType.ATTACKER) return true;
                if (eff.tActorType == TargetActorType.ADJACENT && eff.adjacentRange >= range) return true;
            }
        }

        return false;
    }

    public string GetRandomPrefab()
    {
        if (prefabOptions.Count == 0) return "";
        return prefabOptions[UnityEngine.Random.Range(0, prefabOptions.Count)];
    }

    public void AddMoney(int amount)
    {
        moneyHeld += amount;
    }

    //Removes the destructible from gameplay right away, it does not get another turn
    public override void RemoveImmediately()
    {
        base.RemoveImmediately();
        isDestroyed = true;
    }

    public void DoDestructibleTurnMovement(TurnData tData)
    {
        if (!spreadThisTurn && turnsToDisappear > stopSpreadThreshold && !tData.extraTurn)
        {
            switch (spreadType)
            {
                case Spread.NOSPREAD:
                    spreadThisTurn = false;
                    break;
                case Spread.FORWARD:
                    spreadThisTurn = true;
                    // This actor replicates itself by moving forward.
                    dtSpreadPositions.Clear();
                    switch (lastMovedDirection)
                    {
                        case Directions.NORTH:
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y + 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y + 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y + 1f));
                            break;
                        case Directions.NORTHEAST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y + 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y));
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y + 1f));
                            break;
                        case Directions.EAST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y + 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y));
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y - 1f));
                            break;
                        case Directions.SOUTHEAST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y));
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y - 1f));
                            break;
                        case Directions.SOUTH:
                            dtSpreadPositions.Add(new Vector2(GetPos().x + 1f, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y - 1f));
                            break;
                        case Directions.SOUTHWEST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y));
                            break;
                        case Directions.WEST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y + 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y - 1f));
                            break;
                        case Directions.NORTHWEST:
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y - 1f));
                            dtSpreadPositions.Add(new Vector2(GetPos().x - 1f, GetPos().y));
                            dtSpreadPositions.Add(new Vector2(GetPos().x, GetPos().y + 1f));
                            break;
                    }
                    break;
                case Spread.ADJACENT:
                    spreadThisTurn = true;
                    // This actor replicates itself by moving forward.
                    dtSpreadPositions.Clear();
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[0]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[1]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[2]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[3]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[4]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[5]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[6]));
                    dtSpreadPositions.Add((GetPos() + MapMasterScript.xDirections[7]));
                    break;
            }

            if (spreadThisTurn)
            {
                for (int p = 0; p < dtSpreadPositions.Count; p++)
                {
                    if (!MapMasterScript.InBounds(dtSpreadPositions[p]))
                    {
                        continue;
                    }
                    MapTileData mtd = MapMasterScript.GetTile(dtSpreadPositions[p]);

                    if (mtd.tileType != TileTypes.GROUND || mtd.CheckActorRef(actorRefName)) // Only one actor per tile, but collidable is OK.
                    {
                        continue;
                    }

                    // If the destructible is collidable, don't replicate to a collidable tile
                    if (mtd.playerCollidable && playerCollidable) continue;

                    if (mtd.CheckActorRef(actorRefName))
                    {
                        continue;
                    }

                    Actor origActor = this;
                    if (summoner != null)
                    {
                        origActor = summoner;
                    }

                    Destructible replicated = GameMasterScript.SummonDestructible(origActor, this, dtSpreadPositions[p], 99, 0.075f);

                    replicated.CopyFromLiving(this);
                    replicated.actorRefName = actorRefName;
                    replicated.displayName = displayName;
                    replicated.turnsToDisappear -= 1;
                    if (replicated.turnsToDisappear < 0)
                    {
                        replicated.turnsToDisappear = 0;
                    }

                    replicated.SetSpawnPos(dtSpreadPositions[p]);
                    replicated.SetPos(dtSpreadPositions[p]);
                    replicated.areaID = MapMasterScript.activeMap.CheckMTDArea(mtd);
                    replicated.acted = false;
                    replicated.lastMovedDirection = CombatManagerScript.GetDirection(this, replicated);

                    if (changeSpreadAfterInitial != spreadType)
                    {
                        replicated.spreadType = changeSpreadAfterInitial;
                    }
                    //Debug.Log(GameMasterScript.turnNumber + ": " + replicated.actorRefName + " " + " " + replicated.turnsToDisappear + " " + replicated.spreadType + " " + replicated.actorUniqueID + " spawned at " + replicated.GetPos() + " from " + GetPos() + " " + actorUniqueID + " turnsleft? " + turnsToDisappear + " " + stopSpreadThreshold);
                }

                if (dieAfterSpread)
                {
                    turnsToDisappear = 0;
                    stopSpreadThreshold = -1;
                }
            }
        }
    }

    public override void UpdateLastMovedDirection(Directions dir)
    {
        if (rotateToMoveDirection && spreadType == Spread.FORWARD)
        {
            // Some kind of moving projectile like Ice Dagger. If it's anchored, we DON'T want to change its actual trajectory.
            return;
        }
        lastMovedDirection = dir;
        if (myAnimatable == null) return;
        if (dir == Directions.WEST || dir == Directions.NORTHWEST || dir == Directions.SOUTHWEST)
        {
            myAnimatable.OrientSprite(Directions.WEST);
        }
        else if (dir == Directions.EAST || dir == Directions.NORTHEAST || dir == Directions.SOUTHEAST)
        {
            myAnimatable.OrientSprite(Directions.EAST);
        }
    }

    public static Destructible FindTemplate(string refName)
    {
        try
        {
            return GameMasterScript.masterMapObjectDict[refName];
        }
        catch
        {
            Debug.LogError("Could not find template for " + refName);
        }

        return null;
    }

    public void ShowDirection(bool status)
    {
        if (status)
        {
            if (directionIndicator != null)
            {
                //GameObject.Destroy(directionIndicator);
                GameMasterScript.ReturnToStack(directionIndicator, "DirectionIndicator");
            }
            GameObject go = CombatManagerScript.SpawnChildSprite("DirectionIndicator", this, lastMovedDirection, true);
            directionIndicator = go;
            showDirection = true;
        }
        else
        {
            if (directionIndicator != null)
            {
                //GameObject.Destroy(directionIndicator);
                GameMasterScript.ReturnToStack(directionIndicator, "DirectionIndicator");
                directionIndicator = null;
                showDirection = false;
            }
        }
    }

    public void CopyFromLiving(Destructible template)
    {
        CopyFromTemplate(template);
        actorfaction = template.actorfaction;
        spreadThisTurn = template.spreadThisTurn;
        movedThisTurn = template.movedThisTurn;
        moneyHeld = template.moneyHeld;
        // DON'T add last moved direction.
        if (template.summoner != null)
        {
            summoner = template.summoner;
            summonerID = template.summonerID;
            summoner.AddSummon(this);
        }
        turnsToDisappear = template.turnsToDisappear;
        turnsAlive = template.turnsAlive;
        maxTurnsToDisappear = template.maxTurnsToDisappear;
    }

    public void CopyFromTemplate(Destructible template)
    {
        // dtStatusEffect = template.dtStatusEffect;  Copy the status instead.
        if (template == null)
        {
            Debug.Log("WARNING: Trying to copy status effect from null template.");
            return;
        }

        actorfaction = template.actorfaction;

        if (template.dtStatusEffect != null)
        {
            StatusEffect nStatus = new StatusEffect();
            nStatus.CopyStatusFromTemplate(template.dtStatusEffect);
            dtStatusEffect = nStatus;

            if (actorfaction == Faction.DUNGEON)
            {
                foreach (EffectScript eff in dtStatusEffect.listEffectScripts)
                {
                    eff.originatingActor = GameMasterScript.theDungeonActor;
                    //Debug.Log(template.actorRefName + " has status effect " + dtStatusEffect.refName + " actor set to " + eff.originatingActor.actorRefName + " " + eff.originatingActor.actorUniqueID);
                }
            }
            statusRef = template.statusRef;
        }

        foreach (string p in template.prefabOptions)
        {
            prefabOptions.Add(p);
        }

        playerPrefab = template.playerPrefab;
        stepTrigger = template.stepTrigger;
        excludeFromHotbarCheck = template.excludeFromHotbarCheck;
        passThroughAnything = template.passThroughAnything;
        rotateToMoveDirection = template.rotateToMoveDirection;
        showDirection = template.showDirection;
        runEffectOnLastTurn = template.runEffectOnLastTurn;
        runEffectOnlyOnce = template.runEffectOnlyOnce;
        runEffectNoMatterWhatIsOnMe = template.runEffectNoMatterWhatIsOnMe;
        dieAfterSpread = template.dieAfterSpread;
        blocksVision = template.blocksVision;
        destroyOnStep = template.destroyOnStep;
        destroyOnWallHit = template.destroyOnWallHit;
        stopSpreadThreshold = template.stopSpreadThreshold;
        hoverDisplay = template.hoverDisplay;
        spreadType = template.spreadType;
        changeSpreadAfterInitial = template.changeSpreadAfterInitial;
        movementType = template.movementType;
        targetable = template.targetable;
        actorRefName = template.actorRefName;
        dieAfterRunEffect = template.dieAfterRunEffect;
        deathPrefab = template.deathPrefab;
        destroyedPrefab = template.destroyedPrefab;
        prefab = template.prefab;
        displayName = template.displayName;
        maxItems = template.maxItems;
        lootChance = template.lootChance;
        hasDestroyedState = template.hasDestroyedState;
        isDestroyed = template.isDestroyed;
        autoSpawn = template.autoSpawn;
        mapObjType = template.mapObjType;
        bonusLootValue = template.bonusLootValue;
        playerCollidable = template.playerCollidable;
        monsterCollidable = template.monsterCollidable;
        monsterDestroyable = template.monsterDestroyable;
        extraActorReference = template.extraActorReference;
        reqDestroyItem = template.reqDestroyItem;
        dialogRef = template.dialogRef;
        moneyChance = template.moneyChance;
        moneyHeld = template.moneyHeld;
        monsterAttached = template.monsterAttached;
        bonusMagicChance = template.bonusMagicChance;
        bonusLegendaryChance = template.bonusLegendaryChance;
        minItems = template.minItems;
        minMagicItems = template.minMagicItems;
        spriteRefOnSummon = template.spriteRefOnSummon;

        if (template.dictActorData != null)
        {
            foreach (string key in template.dictActorData.Keys)
            {
                SetActorData(key, template.dictActorData[key]);
            }
        }
        if (template.dictActorDataString != null)
        {
            foreach (string key in template.dictActorDataString.Keys)
            {
                SetActorDataString(key, template.dictActorDataString[key]);
            }
        }

        if (!String.IsNullOrEmpty(dialogRef))
        {
            hasDialog = true;
        }
        else
        {
            hasDialog = false;
        }
        for (int i = 0; i < spawnInVisualSet.Length; i++)
        {
            spawnInVisualSet[i] = template.spawnInVisualSet[i];
        }
        for (int i = 0; i < objectFlags.Length; i++)
        {
            objectFlags[i] = template.objectFlags[i];
        }

        if (prefab == "TerrainTile" || prefab == "MudTile" || prefab == "ElectricTile" || prefab == "LaserTile")
        {
            isTerrainTile = true;
        }        
        ignoreMeInTurnProcessing = template.ignoreMeInTurnProcessing;
    }

    public void ResetToInitializedState()
    {
        initialized = false;
        dtSpreadPositions.Clear();
        prefabOptions.Clear();
        Init();

    }

    protected override void Init()
    {

        base.Init();

        if (objectInitializedAtLeastOnce)
        {
            dtSpreadPositions.Clear();
            prefabOptions.Clear();
        }
        else
        {
            dtSpreadPositions = new List<Vector2>();
            prefabOptions = new List<string>();
        }

        destroyed = false;
        isInDeadQueue = false;
        deathPrefab = "";
        destroyedPrefab = "";
        mapObjType = SpecialMapObject.NOTHING;
        moneyChance = 0f;
        monsterDestroyable = false;
        dtStatusEffect = null;
        statusRef = "";
        skipTurn = false;
        actOnlyWithSummoner = false;
        movedThisTurn = false;
        spreadType = Spread.NOSPREAD;
        changeSpreadAfterInitial = Spread.NOSPREAD;
        movementType = Spread.NOSPREAD;
        spreadThisTurn = false;
        movedThisTurn = false;
        startCheckThisTurn = false;
        stopSpreadThreshold = 0;
        turnsAlive = 0;
        transparent = false;
        destroyOnStep = false;
        destroyOnWallHit = false;
        runEffectOnLastTurn = false;
        runEffectOnlyOnce = false;
        runEffectNoMatterWhatIsOnMe = false;
        dieAfterSpread = false;
        showDirection = false;
        rotateToMoveDirection = false;
        directionIndicator = null;
        extraActorReference = "";
        reqDestroyItem = "";
        hasDialog = false;
        bonusLootValue = 0f;
        bonusMagicChance = 0f;
        bonusLegendaryChance = 0f;
        minItems = 0;
        minMagicItems = 0;
        replaceRef = false;
        isTerrainTile = false;
        stepTrigger = DestructibleStepTrigger.ANY;



        blocksVision = false;
        SetActorType(ActorTypes.DESTRUCTIBLE);
        actorfaction = Faction.DUNGEON;
        anchorID = -1;
        anchorRange = 0;
        isDestroyed = false;
        hoverDisplay = false;
        hasDestroyedState = false;
        passThroughAnything = false;
        bRemovedAndTakeNoActions = false;
        monsterAttached = "";
        lootChance = 0f;
        autoSpawn = false;
        maxItems = 0;
        if (!objectInitializedAtLeastOnce)
        {
            spawnInVisualSet = new bool[(int)TileSet.COUNT];
        }
        else
        {
            for (int i = 0; i < spawnInVisualSet.Length; i++)
            {
                spawnInVisualSet[i] = false;
            }
        }
        lastMovedDirection = Directions.NEUTRAL;
        if (!objectInitializedAtLeastOnce)
        {
            objectFlags = new bool[(int)ObjectFlags.COUNT];
        }
        else
        {
            for (int i = 0; i < objectFlags.Length; i++)
            {
                objectFlags[i] = false;
            }
        }
        
        reqDestroyItem = "";

        objectInitializedAtLeastOnce = true;
    }

    // Make sure destructible is re-created
    public void ReadFromSave(XmlReader reader)
    {
        reader.Read();

        bool mapWasAssigned = false;

        int reads = 0;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            reads++;
            if (reads > 15000)
            {
                Debug.Log("Breaking");
                break;
            }

            string txt;
            switch (strValue)
            {
                case "ref":
                case "refname":
                case "actorrefname":
                    actorRefName = reader.ReadElementContentAsString();
                    Destructible template = FindTemplate(actorRefName);
                    CopyFromTemplate(template);
                    if (mapObjType == SpecialMapObject.BLOCKER)
                    {
                        actorfaction = Faction.MYFACTION;
                    }
                    break;
                case "dad":
                case "dictactordata":
                    ReadActorDict(reader);
                    break;
                case "dads":
                case "dictactordatastring":
                case "dictactordatastrings":
                    ReadActorDictString(reader);
                    break;
                case "extraactorreference":
                    extraActorReference = reader.ReadElementContentAsString();
                    break;
                case "money":
                    moneyHeld = reader.ReadElementContentAsInt();
                    break;
                case "fc":
                    actorfaction = (Faction)reader.ReadElementContentAsInt();
                    break;
                case "who":
                case "actorfaction":
                case "faction":
                    actorfaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "cr":
                    mapWasAssigned = ReadCoreActorInfo(reader);
                    break;
                case "fl":
                case "floor":
                case "dungeonfloor":
                    dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "mapid":
                case "actormap":
                    actorMapID = reader.ReadElementContentAsInt();
                    MapMasterScript.TryAssignMap(this, actorMapID);
                    mapWasAssigned = true;
                    break;
                case "id":
                case "uniqueid":
                    actorUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "anchorid":
                    anchorID = reader.ReadElementContentAsInt();
                    break;
                case "monsterattached":
                    monsterAttached = reader.ReadElementContentAsString();
                    break;
                case "anchorrange":
                    anchorRange = reader.ReadElementContentAsInt();
                    break;
                case "pos":
                    ReadCurrentPosition(reader);
                    spawnPosition.x = GetPos().x;
                    spawnPosition.y = GetPos().y;
                    break;
                case "posx":
                    txt = reader.ReadElementContentAsString();
                    float xPos = CustomAlgorithms.TryParseFloat(txt);
                    SetCurPosX(xPos);
                    spawnPosition.x = xPos;
                    break;
                case "posy":
                    txt = reader.ReadElementContentAsString();
                    float yPos = CustomAlgorithms.TryParseFloat(txt);
                    SetCurPosX(yPos);
                    spawnPosition.y = yPos; break;
                case "spawnposx":
                    txt = reader.ReadElementContentAsString();
                    spawnPosition.x = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "spawnposy":
                    txt = reader.ReadElementContentAsString();
                    spawnPosition.y = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "aid":
                case "areaid":
                    areaID = reader.ReadElementContentAsInt();
                    break;
                case "isdestroyed":
                    isDestroyed = reader.ReadElementContentAsBoolean();
                    break;
                case "diewithsummoner":
                    dieWithSummoner = reader.ReadElementContentAsBoolean();
                    break;
                case "acted":
                    acted = reader.ReadElementContentAsBoolean();
                    break;
                case "skipturn":
                    skipTurn = reader.ReadElementContentAsBoolean();
                    break;
                case "actonlywithsummoner":
                    actOnlyWithSummoner = reader.ReadElementContentAsBoolean();
                    break;
                case "spreadthisturn":
                    spreadThisTurn = reader.ReadElementContentAsBoolean();
                    break;
                case "movedthisturn":
                    movedThisTurn = reader.ReadElementContentAsBoolean();
                    break;
                case "ttd":
                case "turnstodisappear":
                    turnsToDisappear = reader.ReadElementContentAsInt();
                    break;
                case "ta":
                case "turnsalive":
                    turnsAlive = reader.ReadElementContentAsInt();
                    break;
                case "mttd":
                case "maxturnstodisappear":
                    maxTurnsToDisappear = reader.ReadElementContentAsInt();
                    break;
                case "summonerid":
                    summonerID = reader.ReadElementContentAsInt();
                    break;
                case "inv":
                case "inventory":
                    reader.ReadStartElement();
                    if (reader.Name.ToLowerInvariant() != "item")
                    {
                        continue;
                    }
                    myInventory.ReadFromSave(reader);
                    break;
                case "sref":
                case "statusref":
                    statusRef = reader.ReadElementContentAsString();
                    break;
                case "spread":
                    spreadType = (Spread)reader.ReadElementContentAsInt();
                    break;
                case "movedir":
                    lastMovedDirection = (Directions)reader.ReadElementContentAsInt();
                    break;
                case "lmd":
                case "lastmoveddirection":
                    lastMovedDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "se":
                case "statuseffect":
                    StatusEffect se = new StatusEffect();
                    reader.ReadStartElement();
                    se.ReadFromSave(reader, this);
                    dtStatusEffect = se;
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        if (isDestroyed)
        {
            playerCollidable = false; // Should this always be true?
            monsterCollidable = false; // Yeah ok
        }

        /* if (dungeonFloor == 358 || actorMapID == 358)
        {
            Debug.LogError(actorRefName + " " + actorMapID + " " + dungeonFloor + " " + (GetActorMap() == null) + " " + mapWasAssigned);
        } */

        if (!mapWasAssigned && GetActorMap() == null)
        {
            //Debug.LogError(actorRefName + " had no map " + actorMapID + " " + dungeonFloor);
            TryAssignMapOnLoad();
        }
        
        GameMasterScript.AddActorToDict(this);

        reader.ReadEndElement();
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("dt");
        writer.WriteElementString("ref", actorRefName);
        if (actorfaction != Faction.DUNGEON && !(actorfaction == Faction.MYFACTION && mapObjType == SpecialMapObject.BLOCKER))
        {
            //writer.WriteElementString("who", actorfaction.ToString());
            writer.WriteElementString("fc", ((int)actorfaction).ToString());
        }
        WriteCoreActorInfo(writer);
        if (!String.IsNullOrEmpty(monsterAttached))
        {
            writer.WriteElementString("monsterattached", monsterAttached);
        }

        if (moneyHeld > 0)
        {
            writer.WriteElementString("money", moneyHeld.ToString());
        }

        if (skipTurn)
        {
            writer.WriteElementString("skipturn", skipTurn.ToString().ToLowerInvariant());
        }
        WriteCurrentPosition(writer);
        /* writer.WriteElementString("posX", GetPos().x.ToString());
		writer.WriteElementString("posY", GetPos().y.ToString());
        if (areaID != MapMasterScript.FILL_AREA_ID)
        {
            writer.WriteElementString("aid", areaID.ToString());
        } */
        if (GetSpawnPos().x != GetPos().x)
        {
            writer.WriteElementString("spawnposx", GetSpawnPos().x.ToString());
        }
        if (GetSpawnPos().y != GetPos().y)
        {
            writer.WriteElementString("spawnposy", GetSpawnPos().y.ToString());
        }
        if (isDestroyed)
        {
            writer.WriteElementString("isdestroyed", isDestroyed.ToString().ToLowerInvariant());
        }
        if (summoner != null)
        {
            if (anchorID != -1)
            {
                writer.WriteElementString("anchorid", anchorID.ToString());
            }
            if (anchorRange != 0)
            {
                writer.WriteElementString("anchorrange", anchorRange.ToString());
            }
            if (dieWithSummoner)
            {
                writer.WriteElementString("diewithsummoner", dieWithSummoner.ToString().ToLowerInvariant());
            }
            if (actOnlyWithSummoner)
            {
                writer.WriteElementString("actonlywithsummoner", actOnlyWithSummoner.ToString().ToLowerInvariant());
            }
            writer.WriteElementString("summonerid", summonerID.ToString());
        }

        if (turnsToDisappear > 0)
        {
            writer.WriteElementString("ttd", turnsToDisappear.ToString());
        }
        if (turnsAlive > 0)
        {
            writer.WriteElementString("ta", turnsAlive.ToString());
        }
        if (maxTurnsToDisappear > 0)
        {
            writer.WriteElementString("mttd", maxTurnsToDisappear.ToString());
        }

        if (!string.IsNullOrEmpty(extraActorReference))
        {
            writer.WriteElementString("extraactorreference", extraActorReference);
        }
        if (spreadType != Spread.NOSPREAD)
        {
            writer.WriteElementString("spreadthisturn", spreadThisTurn.ToString().ToLowerInvariant());
            writer.WriteElementString("movedthisturn", movedThisTurn.ToString().ToLowerInvariant());
        }

        if (lastMovedDirection != Directions.NEUTRAL)
        {
            writer.WriteElementString("movedir", ((int)lastMovedDirection).ToString());
        }

        if (spreadType != GameMasterScript.masterMapObjectDict[actorRefName].spreadType)
        {
            writer.WriteElementString("spread", ((int)spreadType).ToString());
        }

        if (actorRefName != "obj_rivertile" && actorRefName != "obj_phasmashieldtile" && actorRefName != "obj_lavatile" && actorRefName != "obj_electile" && actorRefName != "obj_mudtile")
        {
            if (!string.IsNullOrEmpty(statusRef))
            {
                writer.WriteElementString("sref", statusRef);
                dtStatusEffect.WriteToSave(writer, actorUniqueID, true);
            }
        }

        if (myInventory.GetInventory().Count > 0)
        {
            myInventory.WriteToSave(writer);
        }

        WriteActorDict(writer);

        writer.WriteEndElement();
    }

    public bool ValidateBaseData()
    {
        if (dtStatusEffect == null)
        {
            if (runEffectOnLastTurn)
            {
                Debug.LogError(actorRefName + " set to run status effect on last turn, but no valid SE exists.");
                return false;
            }
        }
        if (!string.IsNullOrEmpty(reqDestroyItem))
        {
            if (GameMasterScript.masterItemList.ContainsKey(reqDestroyItem))
            {
                Debug.LogError(actorRefName + " has a ReqDestroyItem " + reqDestroyItem + " which doesn't exist in item dict.");
                return false;
            }
        }
        if (bonusLootValue < 0)
        {
            bonusLootValue = 0;
        }
        if (bonusLegendaryChance < 0)
        {
            bonusLegendaryChance = 0;
        }
        if (minItems >= maxItems)
        {
            minItems = maxItems;
        }
        if (minItems < 0)
        {
            minItems = 0;
        }        
        if (minMagicItems < 0)
        {
            minMagicItems = 0;        
        }


        return true;

    }
    
    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string txt;
            switch (reader.Name)
            {
                case "DName":
                case "DisplayName":
                    displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "RefName":
                    actorRefName = reader.ReadElementContentAsString();                    
                    break;
                case "ExcludeFromHotbarCheck":
                    excludeFromHotbarCheck = true;
                    reader.Read();
                    break;
                case "ReplaceRef":
                    replaceRef = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "AutoSpawn":
                    autoSpawn = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "StepTrigger":
                    stepTrigger = (DestructibleStepTrigger)Enum.Parse(typeof(DestructibleStepTrigger), reader.ReadElementContentAsString());
                    break;
                case "PlayerPrefab":
                    playerPrefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    if (!GameMasterScript.TryPreloadResourceNoBundles(prefab, "MapObjects/" + playerPrefab))
                    {
                        GameMasterScript.TryPreloadResourceNoBundles(prefab, "Art/DecorPrefabs/" + playerPrefab);
                    }
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                    {prefab, "MapObjects/" + prefab});
}
                    break;
                case "Tileset":
                case "SpawnInVisualSet":
                    TileSet ts = (TileSet)Enum.Parse(typeof(TileSet), reader.ReadElementContentAsString());
                    spawnInVisualSet[(int)ts] = true;
                    break;
                case "BlocksVision":
                    blocksVision = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "RunEffectOnLastTurn":
                    runEffectOnLastTurn = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "RunEffectOnlyOnce":
                    runEffectOnlyOnce = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "AlwaysRun":
                    runEffectNoMatterWhatIsOnMe = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;     
                case "DieAfterUse":
                    dieAfterRunEffect = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;               
                case "DieAfterSpread":
                    dieAfterSpread = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "RotateToMoveDirection":
                    rotateToMoveDirection = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "PassThroughAnything":
                    passThroughAnything = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ShowDirection":
                    showDirection = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ReqDestroyItem":
                    reqDestroyItem = reader.ReadElementContentAsString();
                    break;
                case "DialogRef":
                    dialogRef = reader.ReadElementContentAsString();
                    break;
                case "SpecObj":
                case "SpecialMapObjectType":
                    mapObjType = (SpecialMapObject)Enum.Parse(typeof(SpecialMapObject), reader.ReadElementContentAsString());
                    if (mapObjType == SpecialMapObject.BLOCKER)
                    {
                        ignoreMeInTurnProcessing = true;
                    }
                    break;
                case "Spread":
                    spreadType = (Spread)Enum.Parse(typeof(Spread), reader.ReadElementContentAsString());
                    changeSpreadAfterInitial = spreadType;
                    break;
                case "ChangeSpreadAfterInitial":
                    changeSpreadAfterInitial = (Spread)Enum.Parse(typeof(Spread), reader.ReadElementContentAsString());
                    break;
                case "Move":
                case "Movement":
                    movementType = (Spread)Enum.Parse(typeof(Spread), reader.ReadElementContentAsString());
                    break;
                case "LootChance":
                    txt = reader.ReadElementContentAsString();
                    lootChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "MoneyChance":
                    txt = reader.ReadElementContentAsString();
                    moneyChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "StopSpreadThreshold":
                    stopSpreadThreshold = reader.ReadElementContentAsInt();
                    break;
                case "BonusLootValue":
                    txt = reader.ReadElementContentAsString();
                    bonusLootValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "BonusMagicChance":
                    txt = reader.ReadElementContentAsString();
                    bonusMagicChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "BonusLegendaryChance":
                    txt = reader.ReadElementContentAsString();
                    bonusLegendaryChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "VisibleOnMinimap":
                    visibleOnMinimap = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "MaxItems":
                    maxItems = reader.ReadElementContentAsInt();
                    break;
                case "MinItems":
                    minItems = reader.ReadElementContentAsInt();
                    break;
                case "MinMagicItems":
                    minMagicItems = reader.ReadElementContentAsInt();
                    break;
                case "DefaultActorData":
                    // format: key|value
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split('|');
                    SetActorData(parsed[0], Int32.Parse(parsed[1]));
                    break;
                case "DefaultActorDataString":
                    // format: key|value
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    SetActorDataString(parsed[0], parsed[1]);
                    break;
                case "Prefab":
                    prefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    if (!GameMasterScript.TryPreloadResourceNoBundles(prefab, "MapObjects/" + prefab))
                    {
                        GameMasterScript.TryPreloadResourceNoBundles(prefab, "Art/DecorPrefabs/" + prefab);
                    }
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                    {prefab, "MapObjects/" + prefab});
}
                    if (string.IsNullOrEmpty(playerPrefab))
                    {
                        playerPrefab = prefab;
                    }
                    break;
                case "PrefabOption":
                    string newPrefabOption = reader.ReadElementContentAsString();
                    prefabOptions.Add(newPrefabOption);
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    GameMasterScript.TryPreloadResourceNoBundles(newPrefabOption, newPrefabOption);
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {newPrefabOption,newPrefabOption});
}
                    break;
                case "DeathPrefab":
                    deathPrefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    GameMasterScript.TryPreloadResourceNoBundles(deathPrefab, "SpriteEffects/" + deathPrefab);
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {deathPrefab, "SpriteEffects/" + deathPrefab});
}
                    break;
                case "DestroyedState":
                    hasDestroyedState = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "Hvr":
                case "HoverDisplay":
                    hoverDisplay = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "MonsterDestroyable":
                    monsterDestroyable = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "DStep":
                case "DestroyOnStep":
                    destroyOnStep = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "DestroyOnWallHit":
                    destroyOnWallHit = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "DestroyedPrefab":
                    destroyedPrefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    GameMasterScript.TryPreloadResourceNoBundles(destroyedPrefab, "MapObjects/" + destroyedPrefab);
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {destroyedPrefab, "MapObjects/" + destroyedPrefab});
}
                    break;
                case "Faction":
                    actorfaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString());
                    break;
                case "ObjectFlag":
                    ObjectFlags of = (ObjectFlags)Enum.Parse(typeof(ObjectFlags), reader.ReadElementContentAsString());
                    objectFlags[(int)of] = true;
                    break;
                case "StatusRef":
                    statusRef = reader.ReadElementContentAsString();
                    StatusEffect se = GameMasterScript.FindStatusTemplateByName(statusRef);
                    if (se == null)
                    {
                        Debug.LogWarning(actorRefName + " has null SE from ref " + statusRef);
                    }
                    dtStatusEffect = se;
                    break;
                case "Collidable":
                    playerCollidable = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    monsterCollidable = playerCollidable;
                    break;
                case "PCol":
                case "PlayerCollidable":
                    playerCollidable = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "MCol":
                case "MonsterCollidable":
                    monsterCollidable = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "Trg":
                case "Targetable":
                    targetable = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "Transparent":
                    transparent = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "VFX":
                case "SpriteEffectOnSummon":
                    spriteRefOnSummon = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    GameMasterScript.TryPreloadResourceNoBundles(spriteRefOnSummon, "SpriteEffects/" + spriteRefOnSummon);
}
else
{
                    GameMasterScript.resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {spriteRefOnSummon, "SpriteEffects/" + spriteRefOnSummon});
}
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
        return true;
    } 

    public bool CheckIfCanUseStatus()
    {
        if (dtStatusEffect == null) return false;
        if (runEffectOnlyOnce && ReadActorData("runeffect") == 1) return false;
        if (runEffectNoMatterWhatIsOnMe && turnSummoned == GameMasterScript.turnNumber) 
        {
            //Debug.Log(runEffectNoMatterWhatIsOnMe + " " + turnSummoned + " " + GameMasterScript.turnNumber);
            return false;
        }
        return true;
    }

    // Checks if we should run our status effect, which we might not want to if we have a flying/lava-loving (etc) monster..
    // And our status effect is lava, mud or some other terrain effect.
    public bool CheckForValidTargetsOnMe()
    {
        bool valid = false;
        foreach (Actor act in MapMasterScript.GetTile(GetPos()).GetAllTargetable())
        {
            valid = true; // we have at least one, probably valid?
                Monster monInTile = act as Monster;
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            bool bFlyingMonster = monInTile.CheckAttribute(MonsterAttributes.FLYING) > 0;
            switch (mapObjType)
                {
                case SpecialMapObject.LAVA:
                    return !bFlyingMonster && monInTile.CheckAttribute(MonsterAttributes.LOVESLAVA) != 0;
                case SpecialMapObject.ELECTRIC:
                    return !bFlyingMonster && monInTile.CheckAttribute(MonsterAttributes.LOVESELEC) != 0;
                case SpecialMapObject.MUD:
                    return !bFlyingMonster && monInTile.CheckAttribute(MonsterAttributes.LOVESMUD) != 0;
            }
        }
        
        return valid;
    }

    /// <summary>
    /// Returns FALSE if destructible has 100% enforced rules for triggering its effect and/or destroying on step
    /// </summary>
    /// <returns></returns>
    public bool CheckStepTriggerCondition()
    {
        if (stepTrigger == DestructibleStepTrigger.ANY) return true;
        switch(stepTrigger)
        {
            case DestructibleStepTrigger.HERO:
                if (MapMasterScript.GetTile(GetPos()).HasActor(GameMasterScript.heroPCActor))
                {
                    return true;
                }
                break;
            case DestructibleStepTrigger.HEROFACTION:
                foreach(Actor act in MapMasterScript.GetTile(GetPos()).GetAllActors())
                {
                    if (act.actorfaction == Faction.PLAYER && act.IsFighter())
                    {
                        return true;
                    }
                }
                break;
            case DestructibleStepTrigger.ENEMYMONSTER:
                foreach (Actor act in MapMasterScript.GetTile(GetPos()).GetAllActors())
                {
                    if (act.actorfaction == Faction.ENEMY && act.IsFighter())
                    {
                        return true;
                    }
                }
                break;
            case DestructibleStepTrigger.NOTMYFACTION:
                foreach (Actor act in MapMasterScript.GetTile(GetPos()).GetAllActors())
                {
                    if (act.actorfaction != actorfaction && act.IsFighter())
                    {
                        return true;
                    }
                }
                break;
            case DestructibleStepTrigger.ANYMONSTER:
                foreach (Actor act in MapMasterScript.GetTile(GetPos()).GetAllActors())
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        return true;
                    }
                }
                break;
        }

        return false;
    }
}