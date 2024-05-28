using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public enum MoveBehavior { WANDER };
public enum BehaviorState { NEUTRAL, FIGHT, SEEKINGITEM, CURIOUS, STALKING, RUN, FORCEMOVE, PETFORCEDRETURN, ANY }
public enum MoveBoundary { WANDER, LIMIT };
public enum MonsterAttributes
{
    GREEDY, TIMID, BERSERKER, SNIPER, LOVESBATTLES, STALKER, GANGSUP, PREDATOR, SUPPORTER,
    HEALER, COMBINABLE, RONIN, CALLFORHELP, CANTATTACK, CANTACT, LOVESLAVA, LOVESMUD, FLYING, ALWAYSUSEMOVEABILITIES, PACIFIST,
    STARTASLEEP, LOVESELEC, SOULKEEPER_SUMMON, LAZY, NO_KNOCKOUT, PLAYERCANTSWAP, LIVEINWATER, COUNT
};
// ATTRIBUTE DEFINITIONS
// GREEDY: If it sees an item, it will go to it and pick it up. Higher % Greedy will consider attacking an entity on top of an item, in order to get it, or stop chasing momentarily to pick something up.
// TIMID: At Timid% of max health, it will run away during battle.
// BERSERKER: At Berserker% of max health, it will start attacking anything that is within its range.
// SNIPER: In battle, it will try to stay at range from its target, and run away from the target otherwise.
// LOVESBATTLES: Will "listen" for entities in battle and move toward them to investigate.
// STALKER: After it sees a target, it will follow them at a given distance (stalkerRange)
// GANGSUP: Waits until target has a certain number of enemies before aggroing (GangsUp / 20)
// PREDATOR: Will not aggro of its own accord unless the target is below 100-Predator% HP
// HEALER: When self (or ideally friends) get below Healer%, try to heal them.
// COMBINABLE: At health% hp, will combine with other monsters of the same type to make a bigger monster.
// RONIN: Pick a random area. Walk to it. Why not?
// CALLFORHELP: Call to monsters in same family to join the fray

public class MonsterMoveData
{
    public MapTileData destinationTile;
    public AbilityScript abilityUsed;

    public MonsterMoveData(MapTileData mtd)
    {
        destinationTile = mtd;
    }
}

public class InfluenceTurnData
{
    public float sleepChance;
    public float paralyzeChance;
    public float stunChance;
    public float rootChance;
    public float confuseChance;
    public float silenceChance;
    public float charmChance;
    public float fearChance;
    public bool anyChange = false;

    public bool rootedThisTurn;
    public bool stunnedThisTurn;
    public bool movedByExternalActorThisTurn;

    public InfluenceTurnData()
    {
        Reset();
    }

    public void Reset()
    {
        sleepChance = 0.0f;
        paralyzeChance = 0.0f;
        stunChance = 0.0f;
        confuseChance = 0.0f;
        silenceChance = 0.0f;
        rootChance = 0.0f;
        charmChance = 0.0f;
        fearChance = 0.0f;
        anyChange = false;
        rootedThisTurn = false;
        stunnedThisTurn = false;
        movedByExternalActorThisTurn = false;
    }
}

public class SeasonalPrefabData
{
    public Seasons whichSeason;
    public string prefab;
}


public class MonsterTurnData
{
    public float waitTime;
    public List<Actor> affectedActors;
    public List<CombatResult> results;
    public TurnTypes turnType;
    public CombatResult result;

    public MonsterTurnData(float time, TurnTypes tip)
    {
        waitTime = time;
        affectedActors = new List<Actor>();
        results = new List<CombatResult>();
        turnType = tip;
    }

    public void Clear()
    {
        waitTime = 0.0f;
        affectedActors.Clear();
        results.Clear();
    }

    public MonsterTurnData Pass()
    {
        Clear();
        turnType = TurnTypes.PASS;
        return this;
    }

    public MonsterTurnData ChargeTurn()
    {
        Clear();
        waitTime = 0.01f;
        turnType = TurnTypes.PASS;
        return this;
    }
    public MonsterTurnData Continue()
    {
        turnType = TurnTypes.CONTINUE;
        return this;
    }

    public MonsterTurnData Move()
    {
        Clear();
        turnType = TurnTypes.MOVE;
        return this;
    }
}

public class AggroData
{
    public Fighter combatant;
    public int combatantUniqueID;
    public float aggroAmount;
    public int turnsSinceCombatAction;
}

public partial class Monster : Fighter
{
    // This is the flattened version of a monster.
    public bool autoSpawn;
    public bool dataHasBeenLoaded;
    public MoveBehavior myMoveBehavior;
    public MoveBoundary myMoveBoundary;
    public BehaviorState myBehaviorState;
    public MonsterTemplateData myTemplate;
    public Directions wanderDirection;
    public string monFamily;
    private int[] myAttributes;
    public float aggroRange;
    public float stalkerRange;
    public float helpRange;
    public int moveRange; // Make this variable later.
    public int startAreaID;
    public float challengeValue;
    public float lootChance;
    public float xpMod;
    public Actor myTarget;
    public int moneyHeld;
    public int myTargetUniqueID;
    public Actor myActorOfInterest;
    public int myActorOfInterestUniqueID;
    public Vector2 myTargetTile;
    private bool berserking = false;
    private bool runningAway = false;
    public bool isChampion;
    public bool isBoss;
    public bool isItemBoss;
    public bool isEnraged;
    public bool surpressTraits;
    public bool isInCorral;
    public bool sleepUntilSeehero;
    public bool weaponScaled;
    public Faction bufferedFaction;

    public bool recentlyNamedMonster = false;

    public bool foodLovingMonster;
    public int turnsToLoseInterest;

    public List<MapTileData> tilePath;
    public List<MagicMod> magicMods;
    public List<ChampionMod> championMods;
    public List<MonsterPowerData> monsterPowers;
    public List<MonsterPowerData> considerAbilities;

    HeroVisibilityToMonster heroVisibilityThisTurn;

    // Factions / allegiances
    public bool friendlyToHero;

    // Memory management / pooling
    public MonsterTurnData myMonsterTurnData;
    public List<Vector2> tilesWithinRange;
    public List<Vector2> affectedTiles;
    public List<MapTileData> clearTiles;
    public static Vector2 emptyVector = new Vector2(0, 0);

    public HashSet<MapTileData> openList;
    public List<PFNode> openNodeList;

    public List<MapTileData> adjacent;
    public MapTileData[] adjacentArray;
    public bool[] validTileAdjacent;
    public List<MapTileData> allMTD;
    public List<Actor> pool_targets;
    public static List<Actor> pool_actorGeneric;
    public static List<AggroData> pool_aggroData;
    public bool pathIsLine;

    public List<AggroData> aggroToRemove;
    public List<MonsterPowerData> usables;
    public List<MapTileData> nearbyTiles;
    public List<MapTileData> possibleTiles;

    public TurnData storeTurnData;
    public bool storingTurnData;

    public Item wantsItem;

    // For targeting

    public AbilityScript createWarningSquares;
    public SummonActorEffect createWarningSquaresSubEffect;
    public static Dictionary<string, string> familyNamesVerbose;
    public static string[] attributeNamesLocalized;
    public static Actor lockedTargetForDangerTiles;

    public const float CHANCE_PREDATOR_AGGRO = 0.15f;
    public const float CHANCE_GANGUP_AGGRO = 0.15f;

    public TamedCorralMonster tamedMonsterStuff;

    public int localMovementXMin;
    public int localMovementXMax;
    public int localMovementYMin;
    public int localMovementYMax;

    public bool levelScaled;
    public int targetScalingLevel;

    public TileDangerStates[,] dangerousTilesToMe;

    // Allow for custom turn behavior on a monster-by-monster basis via custom scripting.
    public string scriptTakeAction;

    /// <summary>
    /// used for hover text building
    /// </summary>
    static StringBuilder reusableStringBuilder;
    static StringBuilder reusableStringBuilder2;
    static StringBuilder resistStringBuilder;
    static StringBuilder resistStringBuilder2;
    /// <summary>
    /// used for hover text building
    /// </summary>
    static Dictionary<string, List<DamageTypes>> dictElemStrings;
    static bool poolingVarsInitialized;
    public string scriptOnDefeat;

    /// <summary>
    /// List of all ability refs that player has caused monster to permanently forget. These won't be auto-learned on load game.
    /// </summary>
    public List<string> abilitiesForgotten;

    static List<string> invalidAbilitiesForMonsterLetters = new List<string>()
    {
        "skill_mon_taunt",
        "skill_mon_fasthealing",
        "skill_mon_counterattack",
        "skill_mon_desperatestrike"
    };

    Dictionary<string, MonsterPowerData> dictMonsterPowersStrToMPD;

    public Monster()
    {
        Init();
        if (!poolingVarsInitialized)
        {
            poolingVarsInitialized = true;
            reusableStringBuilder = new StringBuilder();
            reusableStringBuilder2 = new StringBuilder();
            resistStringBuilder = new StringBuilder();
            resistStringBuilder2 = new StringBuilder();
            dictElemStrings = new Dictionary<string, List<DamageTypes>>();
        }
        abilResults = new List<CombatResult>();
        abilAffectedActors = new List<Actor>();
    }

    public static string GetAttributeName(int i)
    {
        return attributeNamesLocalized[i];
    }

    public override void RepositionWrathBarIfNeeded()
    {
        if (wrathBarScript == null) return;
        if (GameMasterScript.heroPCActor.GetPos().x == GetPos().x && GameMasterScript.heroPCActor.GetPos().y == GetPos().y - 1)
        {
            wrathBarScript.gameObject.transform.localPosition = new Vector3(0, 0.6f, wrathBarScript.gameObject.transform.localPosition.z);
        }
        else
        {
            wrathBarScript.gameObject.transform.localPosition = new Vector3(0, -0.8f, wrathBarScript.gameObject.transform.localPosition.z);
        }
    }

    public override void EnableWrathBarIfNeeded()
    {
        VerifyWrathBarIsActive();
        if (wrathBarScript == null) return;
        if (myStats.CheckHasStatusName("runic_crystal2_buff"))
        {
            wrathBarScript.ToggleWrathBar(true);
        }
        else
        {
            wrathBarScript.ToggleWrathBar(false);
        }
    }

    public bool ReadFromSave(XmlReader reader, bool addToDict = true, bool addToMap = true, bool assignAndIncrementSharedCorralID = true)
    {
        reader.Read();

        if (dataHasBeenLoaded)
        {
            if (Debug.isDebugBuild) Debug.Log(actorUniqueID + " data was already loaded.");
            while (true)
            {
                if ((reader.Name == "mn" || reader.Name == "monster") && reader.NodeType == XmlNodeType.EndElement)
                {
                    reader.ReadEndElement();
                    return false;
                }
                reader.Read();
            }
        }

        Vector2 previousPos = Vector2.zero;
        int reads = 0;
        string txt;
        Faction readFaction = Faction.NONE;

        bool mapWasAssigned = false;

        bool debugCreature = false;

        //Debug.Log("<color=green>BEGIN READ for monster.</color>");
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            reads++;
            if (reads > 15000)
            {
                Debug.Log("Breaking");
                break;
            }

            //Debug.Log("Prior to ReadFighterStuff, " + actorRefName + " " + actorUniqueID + " Node Name: " + reader.Name + " Node Type: " + reader.NodeType);

            if (GameStartData.loadGameVer < 104)
            {
                bool successRead = true;
                while (successRead)
                {
                    successRead = ReadFighterStuffFromSave(reader, false);
                }
            }

            //Debug.Log("MON Postfighter: " + actorRefName + " " + actorUniqueID + " Node Name: " + reader.Name + " Node Type: " + reader.NodeType);

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            

            strValue = reader.Name.ToLowerInvariant();            

            if (debugCreature)
            {
                Debug.Log(reader.Name + " " + reader.NodeType);
            }

            switch (strValue)
            {
                case "name":
                case "displayname":
                    string localDisplay = reader.ReadElementContentAsString();
                    displayName = Regex.Replace(localDisplay, "&lt;", "<");
                    displayName = Regex.Replace(displayName, "&gt;", ">");

                    //debugCreature = displayName.ToLowerInvariant().Contains("chella"); 
                    break;
                case "prefab":
                    prefab = reader.ReadElementContentAsString();
                    break;
                case "family":
                    monFamily = reader.ReadElementContentAsString();
                    break;
                case "cr":
                    mapWasAssigned = ReadCoreActorInfo(reader, addToMap);
                    break;
                case "lastarea":
                    reader.ReadElementContentAsInt();
                    break;
                case "fight":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        ReadFighterStuffFromSave(reader);
                    }

                    break;
                case "fl":
                case "floor":
                case "dungeonfloor":
                    dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "defeatscript":
                    scriptOnDefeat = reader.ReadElementContentAsString();
                    break;
                case "actionscript":
                    scriptTakeAction = reader.ReadElementContentAsString();
                    break;
                case "foodlovingmonster":
                    foodLovingMonster = reader.ReadElementContentAsBoolean();
                    AddAttribute(MonsterAttributes.GREEDY, 100);
                    RemoveAttribute(MonsterAttributes.STALKER);
                    if (myBehaviorState == BehaviorState.STALKING)
                    {
                        myBehaviorState = BehaviorState.NEUTRAL;
                    }
                    break;
                case "attr":
                    string lump = reader.ReadElementContentAsString();
                    string[] attParsed = lump.Split('|');
                    for (int i = 0; i < attParsed.Length; i++) // WAS monsterattributes.count, but we can expand the # of enums so...
                    {
                        Int32.TryParse(attParsed[i], out myAttributes[i]);
                    }
                    break;
                case "destroyed":
                    destroyed = reader.ReadElementContentAsBoolean();
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
                case "weaponscaled":
                    weaponScaled = reader.ReadElementContentAsBoolean();
                    if (weaponScaled)
                    {
                        int weaponLookupLevel = myStats.GetLevel();

                        if (actorfaction == Faction.ENEMY)
                        {
                            weaponLookupLevel--;
                            if (weaponLookupLevel < 1)
                            {
                                weaponLookupLevel = 1;
                            }
                        }

                        ScaleWeaponToLevelAsPetOrSummon(weaponLookupLevel);
                        SetBattleDataDirty();
                    }
                    break;
                case "lscl":
                case "levelscaled":
                    levelScaled = reader.ReadElementContentAsBoolean();
                    break;
                case "tlvl":
                case "targetscalinglevel":
                    targetScalingLevel = reader.ReadElementContentAsInt();
                    break;
                case "ref":
                case "templateref":
                    actorRefName = reader.ReadElementContentAsString();

                    if (actorfaction == Faction.PLAYER)
                    {
                        MonsterManagerScript.CopyMonsterFromTemplateRef(this, actorRefName, false, true, true, 0f, true);
                    }
                    else
                    {
                        MonsterManagerScript.CopyMonsterFromTemplateRef(this, actorRefName, false, true, true, 0f, false);
                    }

                    if (readFaction != Faction.NONE)
                    {
                        actorfaction = readFaction;
                        bufferedFaction = readFaction;
                    }
                    break;

                case "moneyheld":
                    moneyHeld = reader.ReadElementContentAsInt();
                    break;
                case "aggr":
                case "aggromult":
                    aggroMultiplier = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    break;
                case "actorenabled":
                    _actorEnabled = reader.ReadElementContentAsBoolean();
                    break;
                case "pos":
                    ReadCurrentPosition(reader);
                    spawnPosition.x = GetPos().x;
                    spawnPosition.y = GetPos().y;
                    break;
                case "posx":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosX(CustomAlgorithms.TryParseFloat(txt));
                    spawnPosition.x = GetPos().x;
                    break;
                case "posy":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosY(CustomAlgorithms.TryParseFloat(txt));
                    spawnPosition.y = GetPos().y;
                    break;
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
                    lastAreaVisited = areaID;
                    break;
                case "cv":
                case "challengevalue":
                    txt = reader.ReadElementContentAsString();
                    // Don't actually do anything with this value as it stacks with TimesUpgraded...
                    //challengeValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "lootchance":
                    txt = reader.ReadElementContentAsString();
                    lootChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "xpmod":
                    txt = reader.ReadElementContentAsString();
                    xpMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "mytargetuniqueid":
                    myTargetUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "curiousid":
                case "myactorofinterestuniqueid":
                    myActorOfInterestUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "ttx":
                case "targettilex":
                    txt = reader.ReadElementContentAsString();
                    myTargetTile.x = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "tty":
                case "targettiley":
                    txt = reader.ReadElementContentAsString();
                    myTargetTile.y = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "berserking":
                    berserking = reader.ReadElementContentAsBoolean();
                    break;
                case "running":
                    runningAway = reader.ReadElementContentAsBoolean();
                    break;
                case "skipturn":
                    skipTurn = reader.ReadElementContentAsBoolean();
                    break;
                case "isincorral":
                    isInCorral = reader.ReadElementContentAsBoolean();

                    if (isInCorral && !assignAndIncrementSharedCorralID)
                    {
                        // We must be loading this monster from savedGame, which is wrong and bad
                        // So short out the loop here
                        int attempts = 0;
                        while (true)
                        {
                            if ((reader.Name == "mn" || reader.Name == "monster") && reader.NodeType == XmlNodeType.EndElement)
                            {
                                break;
                            }
                            attempts++;
                            if (attempts >= 20000)
                            {
                                if (Debug.isDebugBuild) Debug.Log("Serious error with trying to read through a normal monster from savegame in corral.");
                                break;
                            }
                            reader.Read();
                        }
                        if (Debug.isDebugBuild) Debug.Log("This local version should not be in the corral, or in the game at all.");
                        reader.ReadEndElement();
                        return false;
                    }
                    break;
                case "sleepuntilseehero":
                    sleepUntilSeehero = reader.ReadElementContentAsBoolean();
                    break;
                case "chm":
                    isChampion = true;
                    reader.ReadElementContentAsString();
                    if (championMods == null)
                    {
                        championMods = new List<ChampionMod>();
                    }
                    break;
                case "champ": // deprecated
                    isChampion = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    if (championMods == null)
                    {
                        championMods = new List<ChampionMod>();
                    }
                    break;
                case "ischampion": // deprecated
                    isChampion = reader.ReadElementContentAsBoolean();
                    if (championMods == null)
                    {
                        championMods = new List<ChampionMod>();
                    }
                    break;
                case "bss":
                    isBoss = true;
                    reader.ReadElementContentAsString();
                    break;
                case "isboss": // deprecated
                    isBoss = reader.ReadElementContentAsBoolean();
                    break;
                case "isitemboss":
                    isItemBoss = reader.ReadElementContentAsBoolean();
                    break;
                case "surpresstraits":
                    surpressTraits = reader.ReadElementContentAsBoolean();
                    break;
                case "enraged":
                case "isenraged":
                    isEnraged = reader.ReadElementContentAsBoolean();
                    break;
                case "rage":
                    isEnraged = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "wanderdirection":
                    wanderDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "wandir":
                    wanderDirection = (Directions)reader.ReadElementContentAsInt();
                    break;
                case "state":
                    myBehaviorState = (BehaviorState)reader.ReadElementContentAsInt();
                    if (foodLovingMonster && myBehaviorState == BehaviorState.STALKING)
                    {
                        myBehaviorState = BehaviorState.NEUTRAL;
                    }
                    break;
                case "behaviorstate":
                    myBehaviorState = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString().ToUpperInvariant());
                    if (foodLovingMonster && myBehaviorState == BehaviorState.STALKING)
                    {
                        myBehaviorState = BehaviorState.NEUTRAL;
                    }
                    break;
                case "fc":
                    actorfaction = (Faction)reader.ReadElementContentAsInt();
                    bufferedFaction = actorfaction;
                    readFaction = actorfaction;
                    break;
                case "who":
                case "faction":
                    actorfaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString().ToUpperInvariant());
                    bufferedFaction = actorfaction;
                    readFaction = actorfaction;
                    break;
                case "bufferedfaction":
                    bufferedFaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "sts":
                case "statblock":
                case "stats":
                    myStats.ReadFromSave(reader, false);
                    break;
                case "inv":
                case "inventory":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }
                    reader.ReadStartElement();
                    if (reader.Name.ToLowerInvariant() != "item")
                    {
                        continue;
                    }

                    // If we aren't adding actor to map, chances are we should just read through their inv and skip it.
                    if (!addToMap)
                    {
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            reader.Read();
                        }
                        reader.ReadEndElement();
                    }
                    else
                    {
                        myInventory.ReadFromSave(reader, addToDict); // new 9/28 - we're reusing "addToDict", hopefully no conflicts?
                    }

                    break;
                case "wref":
                case "weaponref":
                    reader.ReadStartElement();
                    reader.ReadStartElement();
                    Weapon w = new Weapon();
                    w = w.ReadFromSave(reader) as Weapon;
                    if (w != null)
                    {
                        myEquipment.Equip(w, SND.SILENT, 0, false);
                    }
                    reader.ReadEndElement();
                    break;
                case "offhandarmorref":
                    reader.ReadStartElement();
                    reader.ReadStartElement();
                    //Debug.Log("Reading offhand for " + actorRefName + " " + actorUniqueID + " on floor " + dungeonFloor + " " + myTemplate.offhandWeaponID + " " + myTemplate.offhandArmorID);
                    Offhand oh = new Offhand();
                    oh = oh.ReadFromSave(reader) as Offhand;
                    if (oh != null)
                    {
                        myEquipment.Equip(oh, SND.SILENT, EquipmentSlots.OFFHAND, false, false, false);
                    }
                    reader.ReadEndElement();
                    break;
                case "offhandweaponref":
                    reader.ReadStartElement();
                    reader.ReadStartElement();
                    Weapon offhandW = new Weapon();
                    offhandW = offhandW.ReadFromSave(reader) as Weapon;
                    if (offhandW != null)
                    {
                        myEquipment.Equip(offhandW, SND.SILENT, 1, false);
                    }
                    reader.ReadEndElement();
                    break;
                case "armorref":
                    reader.ReadStartElement();

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        reader.ReadStartElement();
                        Armor arm = new Armor();
                        arm = arm.ReadFromSave(reader) as Armor;
                        if (arm != null)
                        {
                            myEquipment.Equip(arm, SND.SILENT, 0, false);
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "abilitiesforgotten":
                    // format is abil1|abil2|abil3, as string refs
                    string unparsedAbils = reader.ReadElementContentAsString();
                    string[] parsedAbils = unparsedAbils.Split('|');
                    for (int i = 0; i < parsedAbils.Length; i++)
                    {
                        if (!abilitiesForgotten.Contains(parsedAbils[i]))
                        {
                            abilitiesForgotten.Add(parsedAbils[i]);
                            RemoveMonsterPowerByAbilityRef(parsedAbils[i]);
                        }
                    }
                    break;
                case "specialability":

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }

                    reader.ReadStartElement();
                    AbilityScript abilToLearn = new AbilityScript();
                    MonsterPowerData newMPD = new MonsterPowerData();
                    string readTemp = "";
                    monsterPowers.Add(newMPD);

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "abilityref":
                                string abilityRef = reader.ReadElementContentAsString();



                                AbilityScript learnTemplate = AbilityScript.GetAbilityByName(abilityRef);
                                if (learnTemplate == null)
                                {
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        reader.Read();
                                    }
                                    monsterPowers.Remove(newMPD);
                                }
                                else
                                {
                                    AbilityScript.CopyFromTemplate(abilToLearn, learnTemplate);
                                    myAbilities.AddNewAbility(abilToLearn, false);
                                    newMPD.abilityRef = abilToLearn;
                                    OnMonsterPowerAdded(newMPD, abilToLearn);
                                }
                                break;
                            case "chancetouse":
                                readTemp = reader.ReadElementContentAsString();
                                float useChance = CustomAlgorithms.TryParseFloat(readTemp);
                                newMPD.chanceToUse = useChance;
                                break;
                            case "healththreshold":
                                readTemp = reader.ReadElementContentAsString();
                                float thresh = CustomAlgorithms.TryParseFloat(readTemp);
                                newMPD.healthThreshold = thresh;
                                break;
                            case "minrange":
                                newMPD.minRange = reader.ReadElementContentAsInt();
                                break;
                            case "maxrange":
                                newMPD.maxRange = reader.ReadElementContentAsInt();
                                break;
                            case "usewithnotarget":
                                newMPD.useWithNoTarget = reader.ReadElementContentAsBoolean();
                                break;
                            case "usestate":
                                newMPD.useState = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString().ToUpperInvariant());
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }

                    reader.ReadEndElement();


                    break;
                case "ability":
                    reader.ReadStartElement();
                    AbilityScript newAbil = new AbilityScript();
                    newAbil.ReadFromSave(reader, this);
                    break;
                case "abilitycd":
                    reader.ReadStartElement();
                    string aRef = reader.Name;
                    int turns = reader.ReadElementContentAsInt();
                    foreach (AbilityScript abil in myAbilities.GetAbilityList())
                    {
                        if (abil.refName == aRef)
                        {
                            abil.SetCurCooldownTurns(turns);
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "eq":
                case "equipmentblock":
                    reader.ReadStartElement();
                    myEquipment.ReadFromSave(reader, true);
                    //reader.Read();
                    break;

                case "pposx":
                case "previousposx":
                    string iStr = reader.ReadElementContentAsString();
                    int checkInt = 0;
                    Int32.TryParse(iStr, out checkInt);
                    previousPos.x = (float)checkInt;
                    break;
                case "pposy":
                case "previousposy":
                    iStr = reader.ReadElementContentAsString();
                    checkInt = 0;
                    Int32.TryParse(iStr, out checkInt);
                    previousPos.y = (float)checkInt;
                    break;
                case "anchorid":
                    anchorID = reader.ReadElementContentAsInt();
                    break;
                case "anchorrange":
                    anchorRange = reader.ReadElementContentAsInt();
                    break;
                case "ttd":
                case "turnstodisappear":
                    turnsToDisappear = reader.ReadElementContentAsInt();
                    break;

                case "wait":
                case "waitturnsremaining":
                    waitTurnsRemaining = reader.ReadElementContentAsInt();
                    break;
                case "mttd":
                case "maxturnstodisappear":
                    maxTurnsToDisappear = reader.ReadElementContentAsInt();
                    break;

                case "diewithsummoner":
                    dieWithSummoner = reader.ReadElementContentAsBoolean();
                    break;
                case "actonlywithsummoner":
                    actOnlyWithSummoner = reader.ReadElementContentAsBoolean();
                    break;
                case "flipspritey":
                    flipSpriteY = reader.ReadElementContentAsBoolean();
                    break;
                case "noanimation":
                    noAnimation = reader.ReadElementContentAsBoolean();
                    break;
                case "ttli":
                    turnsToLoseInterest = reader.ReadElementContentAsInt();
                    break;
                case "summonerid":
                    summonerID = reader.ReadElementContentAsInt();
                    break;
                // Deprecated
                case "anchoredactorid":
                    if (anchoredActorsIDs == null)
                    {
                        anchoredActorsIDs = new List<int>();
                    }
                    anchoredActorsIDs.Add(reader.ReadElementContentAsInt());
                    break;
                // End deprecated
                case "cmods":
                case "championmods":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        string refMod = reader.ReadElementContentAsString();
                        if (championMods == null)
                        {
                            championMods = new List<ChampionMod>();
                        }
                        ChampionMod cm = FindMod(refMod);
                        championMods.Add(cm);
                        AddChampPowers(cm);
                    }
                    reader.ReadEndElement();
                    break;
                case "magicmods":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        MagicMod mm = new MagicMod();
                        string refMod = reader.ReadElementContentAsString();
                        MagicMod template = MagicMod.FindModFromName(refMod);
                        mm.CopyFromMod(template);
                        if (magicMods == null)
                        {
                            magicMods = new List<MagicMod>();
                        }
                        magicMods.Add(mm);
                    }
                    reader.ReadEndElement();
                    break;
                case "storingturn":
                    storingTurnData = reader.ReadElementContentAsBoolean();
                    if (storingTurnData)
                    {
                        reader.ReadStartElement();
                        storeTurnData = new TurnData();
                        storeTurnData.SetTurnType((TurnTypes)Enum.Parse(typeof(TurnTypes), reader.ReadElementContentAsString().ToUpperInvariant()));
                        storeTurnData.centerPosition = new Vector2(0, 0);
                        txt = reader.ReadElementContentAsString();
                        storeTurnData.centerPosition.x = CustomAlgorithms.TryParseFloat(txt);
                        txt = reader.ReadElementContentAsString();
                        storeTurnData.centerPosition.y = CustomAlgorithms.TryParseFloat(txt);
                        storeTurnData.canHeroSeeThisTurn = reader.ReadElementContentAsBoolean();
                        storeTurnData.direction = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                        if (reader.Name == "abiltotry")
                        {
                            string abilRef = reader.ReadElementContentAsString();
                            storeTurnData.tAbilityToTry = myAbilities.GetAbilityByRef(abilRef);
                        }
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            switch (reader.Name)
                            {
                                case "targetactor":
                                    storeTurnData.targetIDs.Add(reader.ReadElementContentAsInt());
                                    break;
                                case "targetposition":
                                    reader.ReadStartElement();
                                    Vector2 v2 = new Vector2(0, 0);
                                    txt = reader.ReadElementContentAsString();
                                    v2.x = CustomAlgorithms.TryParseFloat(txt);
                                    txt = reader.ReadElementContentAsString();
                                    v2.y = CustomAlgorithms.TryParseFloat(txt);
                                    storeTurnData.targetPosition.Add(v2);
                                    reader.ReadEndElement();
                                    break;
                            }
                        }
                        reader.ReadEndElement();
                    }
                    break;

                case "wantsitem":
                    string sRef = reader.ReadElementString();
                    wantsItem = GameMasterScript.GetItemFromRef(sRef);
                    break;
                case "tamedcorralmonster":
                    tamedMonsterStuff = new TamedCorralMonster();
                    tamedMonsterStuff.ReadFromSave(reader);
                    tamedMonsterStuff.monsterID = actorUniqueID;
                    if (tamedMonsterStuff.sharedBankID < 0 && assignAndIncrementSharedCorralID)
                    {
                        tamedMonsterStuff.AssignNewSharedBankID();
                    }
                    tamedMonsterStuff.monsterObject = this;
                    actorfaction = Faction.PLAYER;
                    bufferedFaction = Faction.PLAYER;

                    if (myStats.GetXP() == 0)
                    {
                        myStats.SetXPFlat(myStats.GetXPToCurrentLevel());
                    }
                    
                    //Debug.Log("Read TCM data for " + actorUniqueID + " " + actorRefName + " " + displayName);
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
                default:
                    //Debug.Log("Default node. Node name: " + reader.Name + " Type " + reader.NodeType);
                    reader.Read();
                    break;
            }
            //Debug.Log("End mon while loop: " + reader.Name + " Type " + reader.NodeType);
        }

        bool success = true;

        if (levelScaled)
        {
            ScaleToSpecificLevel(targetScalingLevel, true);
        }

        if (spawnPosition != GetPos()) // new 3/30 to sync position
        {
            spawnPosition.x = GetPos().x;
            spawnPosition.y = GetPos().y;
            Debug.Log(actorUniqueID + " spawn pos didn't match grid pos, adjusting to match.");
        }

        if (addToDict)
        {
            //if (displayName.ToLowerInvariant().Contains("chella")) Debug.Log("Add to dict");
            if (!GameMasterScript.AddActorToDict(this))
            {
                if (Debug.isDebugBuild) 
                {
                    Debug.Log("During monster read, " + PrintCorralDebug() + " was already in dictionary, not adding again.");
                    Debug.Log("The existing actor was " + GameMasterScript.dictAllActors[actorUniqueID].actorRefName + " " + GameMasterScript.dictAllActors[actorUniqueID].displayName);
                }
                
                if (tamedMonsterStuff != null)
                {
                    TryReassignIDForCorralPet();
                }
            }
            else
            {
                if (tamedMonsterStuff != null)
                {
                    if (!isInCorral && anchorID >= 0)
                    {
                        // This is a captured corral pet that is not currently in the corral
                        // But it thinks it is buddied up with the hero
                        GameMasterScript.heroPCActor.TryRelinkMonsterPetToSpecificObject(this);
                    }
                    destroyed = false;
                    //Debug.Log("WARNING! Caught a player pet marked as destroyed, but it really wasn't. " + actorUniqueID + " " + actorRefName + " " + displayName);
                }

                if (!mapWasAssigned && GetActorMap() == null)
                {
                    //Debug.LogError(actorRefName + " had no map " + actorMapID + " " + dungeonFloor);
                    TryAssignMapOnLoad();
                }
            }

            if (destroyed && myStats.GetCurStat(StatTypes.HEALTH) >= 1f)
            {
                //destroyed = false;
                //Debug.Log(actorRefName + " " + actorUniqueID + " was marked as destroyed, but is not dead.");
                myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
            }
        }

        reader.ReadEndElement();

        dataHasBeenLoaded = true;

        // Don't validate item dream monster stats. These can get really weird. 
        // This problem will fix itself in about a week.
        // #todo remove the conditional by 12/9/2017
        if (dungeonFloor < 400 || dungeonFloor >= 500)
        {
            ValidateAndFixStats(true); // New as of 12/2, monster stats should be validated too. Not just player.
        }

        // double check that stalkers dont have melee weapons
        if (CheckAttribute(MonsterAttributes.STALKER) > 0)
        {
            Weapon w = myEquipment.GetWeapon();
            if (w.range == 1)
            {
                myAttributes[(int)MonsterAttributes.STALKER] = 0;
                if (myBehaviorState == BehaviorState.STALKING)
                {
                    myBehaviorState = BehaviorState.NEUTRAL;
                }
            }
        }

        if (myBehaviorState != BehaviorState.FIGHT && myBehaviorState != BehaviorState.RUN)
        {
            // No need to save combat data if we aren't in combat.
            PruneCombatTargets(removeAll:true);            
        }

        // Check if knocked out.
        if (surpressTraits && myStats.CheckHasStatusName("sleepvisual"))
        {
            HitWithMallet();
        }

        TryOverwritingWeaponFXFromDict();

        return success;
    }

    public override void WriteToSave(XmlWriter writer)
    {
        if (myTemplate == null) return;

        if (GetActorMap() == null)
        {
            SetActorMap(MapMasterScript.theDungeon.FindFloor(dungeonFloor));
            if (GetActorMap() == null)
            {
#if UNITY_EDITOR
                Debug.Log("ERROR: Could not save map for monster " + actorRefName + " " + actorUniqueID + " floor " + dungeonFloor + "; map doesn't exist in dict.");
#endif
                return;
            }
        }

        writer.WriteStartElement("mn");

        WriteCoreActorInfo(writer);


        if (destroyed)
        {
            writer.WriteElementString("destroyed", "true");
        }

        if (actorfaction != myTemplate.faction)
        {
            //writer.WriteElementString("who", actorfaction.ToString().ToLowerInvariant());
            writer.WriteElementString("fc", ((int)actorfaction).ToString());
        }
        writer.WriteElementString("ref", actorRefName);

        if (displayName != myTemplate.monsterName)
        {
            writer.WriteElementString("name", displayName);
        }


        WriteCurrentPosition(writer);

        if (GetSpawnPos().x != GetPos().x)
        {
            writer.WriteElementString("spawnposx", GetSpawnPos().x.ToString());
        }

        if (GetSpawnPos().y != GetPos().y)
        {
            writer.WriteElementString("spawnposy", GetSpawnPos().y.ToString());
        }

        if (!CustomAlgorithms.CompareFloats(aggroMultiplier, 1f))
        {
            writer.WriteElementString("aggr", aggroMultiplier.ToString());
        }

        if (moneyHeld > 0f)
        {
            writer.WriteElementString("moneyheld", ((int)(moneyHeld)).ToString());
        }

        if (weaponScaled)
        {
            writer.WriteElementString("weaponscaled", weaponScaled.ToString().ToLowerInvariant());
        }
        if (skipTurn)
        {
            writer.WriteElementString("skipturn", skipTurn.ToString().ToLowerInvariant());
        }
        if (flipSpriteY)
        {
            writer.WriteElementString("flipspritey", flipSpriteY.ToString().ToLowerInvariant());
        }
        if (foodLovingMonster)
        {
            writer.WriteElementString("foodlovingmonster", foodLovingMonster.ToString().ToLowerInvariant());
        }
        if (noAnimation)
        {
            writer.WriteElementString("noanimation", noAnimation.ToString().ToLowerInvariant());
        }

        if (waitTurnsRemaining > 0)
        {
            writer.WriteElementString("wait", waitTurnsRemaining.ToString());
        }

        if (turnsToLoseInterest != myTemplate.turnsToLoseInterest)
        {
            writer.WriteElementString("ttli", turnsToLoseInterest.ToString());
        }

        if (wanderDirection != Directions.NORTH)
        {
            //writer.WriteElementString("wanderdirection", wanderDirection.ToString().ToLowerInvariant());
            writer.WriteElementString("wandir", ((int)wanderDirection).ToString());
        }

        if (challengeValue != myTemplate.challengeValue)
        {
            writer.WriteElementString("cv", challengeValue.ToString());
        }

        if (lootChance != myTemplate.lootChance)
        {
            writer.WriteElementString("lootchance", lootChance.ToString());
        }

        // Relevant for player pets.
        if (prefab != myTemplate.prefab)
        {
            writer.WriteElementString("prefab", prefab);
        }
        if (monFamily != myTemplate.monFamily)
        {
            writer.WriteElementString("family", monFamily);
        }

        if (xpMod != myTemplate.xpMod)
        {
            writer.WriteElementString("xpmod", xpMod.ToString());
        }

        if (!string.IsNullOrEmpty(scriptOnDefeat) && scriptOnDefeat != myTemplate.scriptOnDefeat)
        {
            writer.WriteElementString("defeatscript", scriptOnDefeat);
        }
        if (!string.IsNullOrEmpty(scriptTakeAction) && scriptTakeAction != myTemplate.scriptTakeAction)
        {
            writer.WriteElementString("actionscript", scriptTakeAction);
        }

        if (!actorEnabled)
        {
            writer.WriteElementString("actorenabled", actorEnabled.ToString().ToLowerInvariant());
        }

        WriteFighterStuffToSave(writer); // Includes cached battle data.

        if (GetTargetUniqueID() != 0)
        {
            writer.WriteElementString("mytargetuniqueid", GetTargetUniqueID().ToString());
        }
        if (GetActorOfInterestUniqueID() != 0)
        {
            writer.WriteElementString("curiousid", GetActorOfInterestUniqueID().ToString());
        }
        if ((myTargetTile.x != 0) && (myTargetTile.y != 0))
        {
            writer.WriteElementString("ttx", myTargetTile.x.ToString());
            writer.WriteElementString("tty", myTargetTile.y.ToString());
        }
        if (berserking)
        {
            writer.WriteElementString("berserking", berserking.ToString().ToLowerInvariant());
        }

        if (runningAway)
        {
            writer.WriteElementString("running", runningAway.ToString().ToLowerInvariant());
        }

        if (sleepUntilSeehero)
        {
            writer.WriteElementString("sleepuntilseehero", sleepUntilSeehero.ToString().ToLowerInvariant());
        }

        if (isInCorral)
        {
            writer.WriteElementString("isincorral", isInCorral.ToString().ToLowerInvariant());
        }
        if (isChampion)
        {
            //writer.WriteElementString("champ", "1");
            writer.WriteStartElement("chm");
            writer.WriteFullEndElement();
        }
        if (isBoss)
        {
            writer.WriteStartElement("bss");
            writer.WriteFullEndElement();
            //writer.WriteElementString("isboss", isBoss.ToString().ToLowerInvariant());
        }
        if (isItemBoss)
        {
            writer.WriteElementString("isitemboss", isItemBoss.ToString().ToLowerInvariant());
        }
        if (surpressTraits)
        {
            writer.WriteElementString("surpresstraits", surpressTraits.ToString().ToLowerInvariant());
        }
        if (isEnraged)
        {
            writer.WriteElementString("rage", "1");
        }
        if (myBehaviorState != BehaviorState.NEUTRAL)
        {
            writer.WriteElementString("state", ((int)myBehaviorState).ToString());
        }

        if (bufferedFaction != actorfaction)
        {
            writer.WriteElementString("bufferedfaction", bufferedFaction.ToString().ToLowerInvariant());
        }


        if (summoner != null || anchor != null)
        {
            writer.WriteElementString("anchorid", anchorID.ToString());
            writer.WriteElementString("anchorrange", anchorRange.ToString());
            if (summoner != null)
            {
                writer.WriteElementString("diewithsummoner", dieWithSummoner.ToString().ToLowerInvariant());
                writer.WriteElementString("actonlywithsummoner", actOnlyWithSummoner.ToString().ToLowerInvariant());
                writer.WriteElementString("ttd", turnsToDisappear.ToString());
                writer.WriteElementString("mttd", maxTurnsToDisappear.ToString());
                writer.WriteElementString("summonerid", summonerID.ToString());
            }
        }

        myStats.WriteToSave(writer, false);

        if (myInventory.GetSize() > 0)
        {
            myInventory.WriteToSave(writer);
        }

        if (levelScaled)
        {
            writer.WriteElementString("lscl", levelScaled.ToString().ToLowerInvariant());
            writer.WriteElementString("tlvl", targetScalingLevel.ToString());
        }

        if (abilitiesForgotten.Count > 0)
        {
            string lister = "";
            for (int i = 0; i < abilitiesForgotten.Count; i++)
            {
                if (i > 0)
                {
                    lister += "|";
                }
                lister += abilitiesForgotten[i];                
            }
            writer.WriteElementString("abilitiesforgotten", lister);
        }

        // Monster abilities and equipment don't really need to be serialized.

        if (myEquipment.equipment[(int)EquipmentSlots.WEAPON] != null && !levelScaled)
        {
            if (GameMasterScript.masterItemList.ContainsKey(myTemplate.weaponID))
            {
                Weapon temp = GameMasterScript.masterItemList[myTemplate.weaponID] as Weapon;
                if (!temp.CheckIfSameAs(myEquipment.GetWeapon()))
                {
                    writer.WriteStartElement("wref");
                    myEquipment.GetWeapon().WriteToSave(writer);
                    writer.WriteEndElement();
                }
            }
            else
            {
                Debug.Log(myTemplate.weaponID + " does not exist? Monster is " + actorRefName + " on " + dungeonFloor + " " + displayName);
            }
        }

        Item oh = myEquipment.GetOffhand();
        if (oh != null && !levelScaled)
        {
            //Debug.Log(actorRefName + " has offhand item " + oh.actorRefName + " my offhand weapon id is " + myTemplate.offhandWeaponID + " and my offhand armor id is " + myTemplate.offhandArmorID);
            if (oh.itemType == ItemTypes.WEAPON && oh.actorRefName != myTemplate.offhandWeaponID)
            {
                writer.WriteStartElement("offhandweaponref");
                myEquipment.GetOffhand().WriteToSave(writer);
                writer.WriteEndElement();
            }
            else if (oh.itemType != ItemTypes.WEAPON && oh.actorRefName != myTemplate.offhandArmorID)
            {
                writer.WriteStartElement("offhandarmorref");
                myEquipment.GetOffhand().WriteToSave(writer);
                writer.WriteEndElement();
            }
        }

        Armor myArmor = myEquipment.GetArmor();
        if (myArmor != null)
        {
            if (myArmor.actorRefName != myTemplate.armorID && myEquipment.GetArmor() != null)
            {
                writer.WriteStartElement("armorref");
                myEquipment.GetArmor().WriteToSave(writer);
                writer.WriteEndElement();
            }
        }

        // Serialize accessories only - these are used as champion modifiers.
        if ((myEquipment.equipment[(int)EquipmentSlots.ACCESSORY] != null) || (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2] != null))
        {
            writer.WriteStartElement("eq");

            for (int i = (int)EquipmentSlots.ACCESSORY; i <= (int)EquipmentSlots.ACCESSORY2; i++)
            {
                writer.WriteStartElement(((EquipmentSlots)i).ToString().ToLowerInvariant());
                if (myEquipment.equipment[i] != null)
                {
                    myEquipment.equipment[i].WriteToSave(writer);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        List<string> abilitiesFromChampion = new List<string>();
        if (championMods != null)
        {
            if (championMods.Count > 0)
            {
                writer.WriteStartElement("cmods");

                foreach (ChampionMod cm in championMods)
                {
                    writer.WriteElementString("mod", cm.refName);
                    foreach (MonsterPowerData mpd in cm.modPowers)
                    {
                        abilitiesFromChampion.Add(mpd.abilityRef.refName);
                    }
                }

                writer.WriteEndElement();
            }
        }

        foreach (AbilityScript abil in myAbilities.GetAbilityList())
        {
            bool defaultAbility = false;
            foreach (MonsterPowerData mpd in myTemplate.monsterPowers)
            {
                if (mpd.abilityRef.refName == abil.refName)
                {
                    defaultAbility = true;
                    break;
                }
            }
            if (defaultAbility) continue;

            if (abilitiesFromChampion.Contains(abil.refName)) continue;

            // Got the ability some other way.
            //Debug.Log(abil.refName + " is special ability, non-default of " + actorRefName);
            writer.WriteStartElement("specialability");

            foreach (MonsterPowerData mpd in monsterPowers)
            {
                if (mpd.abilityRef.refName == abil.refName)
                {
                    writer.WriteElementString("abilityref", abil.refName);
                    writer.WriteElementString("chancetouse", mpd.chanceToUse.ToString());
                    writer.WriteElementString("healththreshold", mpd.healthThreshold.ToString());
                    writer.WriteElementString("minrange", mpd.minRange.ToString());
                    writer.WriteElementString("maxrange", mpd.maxRange.ToString());
                    writer.WriteElementString("usestate", mpd.useState.ToString());
                    writer.WriteElementString("usewithnotarget", mpd.useWithNoTarget.ToString().ToLowerInvariant());
                }
            }

            writer.WriteEndElement();

        }

        // Do we need to write the magic mods...?
        if (magicMods != null)
        {
            if (magicMods.Count > 0)
            {
                writer.WriteStartElement("magicmods");

                foreach (MagicMod mm in magicMods)
                {
                    writer.WriteElementString("mod", mm.refName);
                }

                writer.WriteEndElement();
            }
        }

        foreach (AbilityScript abil in myAbilities.abilities)
        {
            if ((!abil.passiveAbility) && (abil.GetCurCooldownTurns() > 0))
            {
                writer.WriteStartElement("abilitycd");
                writer.WriteElementString(abil.refName, abil.GetCurCooldownTurns().ToString());
                writer.WriteEndElement();
            }
        }


        if (previousPosition.x != 0)
        {
            writer.WriteElementString("pposx", ((int)(previousPosition.x)).ToString());
        }
        if (previousPosition.y != 0)
        {
            writer.WriteElementString("pposy", ((int)(previousPosition.y)).ToString());
        }

        if (wantsItem != null)
        {
            writer.WriteElementString("wantsitem", wantsItem.actorRefName);
        }

        if (tamedMonsterStuff != null)
        {
            tamedMonsterStuff.WriteToSave(writer);
        }

        if (storingTurnData)
        {
            writer.WriteElementString("storingturn", storingTurnData.ToString().ToLowerInvariant());
        }

        if (storingTurnData)
        {
            writer.WriteStartElement("storedturndata");
            writer.WriteElementString("ttype", storeTurnData.GetTurnType().ToString().ToLowerInvariant());
            writer.WriteElementString("centerx", storeTurnData.centerPosition.x.ToString());
            writer.WriteElementString("centery", storeTurnData.centerPosition.y.ToString());
            writer.WriteElementString("cansee", storeTurnData.canHeroSeeThisTurn.ToString().ToLowerInvariant());
            writer.WriteElementString("direction", storeTurnData.direction.ToString());
            if (storeTurnData.tAbilityToTry != null)
            {
                writer.WriteElementString("abiltotry", storeTurnData.tAbilityToTry.refName);
            }
            foreach (Actor target in storeTurnData.target)
            {
                writer.WriteElementString("targetactor", target.actorUniqueID.ToString());
            }
            foreach (Vector2 v2 in storeTurnData.targetPosition)
            {
                writer.WriteStartElement("targetposition");
                writer.WriteElementString("posx", v2.x.ToString());
                writer.WriteElementString("posy", v2.y.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        string attributeBuilder = "";

        bool writeAttributes = false;
        for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
        {
            if (myAttributes[i] != myTemplate.monAttributes[i])
            {
                writeAttributes = true;
                break;
            }
        }
        if (writeAttributes)
        {
            for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
            {
                if (i == 0)
                {
                    attributeBuilder = myAttributes[i].ToString();
                }
                else
                {
                    attributeBuilder += "|" + myAttributes[i];
                }

            }
            writer.WriteElementString("attr", attributeBuilder);
        }

        WriteActorDict(writer);

        // Do we need to write Influence Turn Data? Run statuses for that.

        writer.WriteEndElement(); // End hero writing

    }

    public void DisplayAllPowerAndAbilities()
    {
        Debug.Log(actorRefName + " " + actorUniqueID);
        foreach (AbilityScript abil in myAbilities.abilities)
        {
            Debug.Log("Ability: " + abil.refName);
        }
        foreach (MonsterPowerData mpd in monsterPowers)
        {
            Debug.Log("MPD: " + mpd.abilityRef.refName);
        }
        foreach (MonsterPowerData mpd in myTemplate.monsterPowers)
        {
            Debug.Log("Template MPD: " + mpd.abilityRef.refName);
        }
    }

    public void LearnNewPower(string abilRef, float healthThreshold, float chanceToUse, int minRange, int maxRange)
    {
        AbilityScript newAbil = new AbilityScript();
        AbilityScript nTemplate = AbilityScript.GetAbilityByName(abilRef);
        AbilityScript.CopyFromTemplate(newAbil, nTemplate);
        myAbilities.AddNewAbility(newAbil, true);
        MonsterPowerData newMPD = new MonsterPowerData();
        newMPD.abilityRef = newAbil;
        newMPD.healthThreshold = healthThreshold;
        newMPD.minRange = minRange;
        newMPD.maxRange = maxRange;
        newMPD.chanceToUse = chanceToUse;
        monsterPowers.Add(newMPD);
        OnMonsterPowerAdded(newMPD, newAbil);
    }

    protected override void Init()
    {
        if (initialized)
        {
            return;
        }
        if (pool_actorGeneric == null)
        {
            pool_actorGeneric = new List<Actor>();
        }
        if (pool_aggroData == null)
        {
            pool_aggroData = new List<AggroData>();
        }
        base.Init();

        tryDir = new List<Directions>();
        abilitiesForgotten = new List<string>();
        scriptTakeAction = "";
        allMTD = new List<MapTileData>();
        pool_targets = new List<Actor>();
        affectedTiles = new List<Vector2>();
        actorFlags = new bool[(int)ActorFlags.COUNT];
        for (int i = 0; i < actorFlags.Length; i++)
        {
            actorFlags[i] = false;
        }
        aggroToRemove = new List<AggroData>();
        nearbyTiles = new List<MapTileData>();
        possibleTiles = new List<MapTileData>();
        usables = new List<MonsterPowerData>();
        SetActorType(ActorTypes.MONSTER);
        myMonsterTurnData = new MonsterTurnData(0.0f, TurnTypes.PASS);
        tilesWithinRange = new List<Vector2>(18);
        clearTiles = new List<MapTileData>();
        adjacent = new List<MapTileData>(9);
        adjacentArray = new MapTileData[8];
        validTileAdjacent = new bool[8];
        monsterPowers = new List<MonsterPowerData>();
        targetable = true;
        moneyHeld = 0;

        lastAreaVisited = MapMasterScript.FILL_AREA_ID;

        //openList = new FastPriorityQueue<MapTileData>(256);
        openList = new HashSet<MapTileData>();
        openNodeList = new List<PFNode>();
        //closedList = new HashSet<MapTileData>();

        monsterCollidable = true;
        playerCollidable = true;

        myAttributes = new int[(int)MonsterAttributes.COUNT];
        SetState(BehaviorState.NEUTRAL);
        SetActorType(ActorTypes.MONSTER);
        tilePath = new List<MapTileData>();

        // What / when to do this
        myStats = new StatBlock();
        myStats.SetOwner(this);
        myAbilities = new AbilityComponent();
        myAbilities.owner = this;
        myEquipment = new EquipmentBlock();
        myEquipment.owner = this;
        CreateNewInventory();
        acted = false;
        skipTurn = false;
        isEnraged = false;
        isItemBoss = false;
        isChampion = false;
        berserking = false;
        runningAway = false;
        surpressTraits = false;

        anchorID = -1;
        anchorRange = 0;

        physicalWeaponDamageAddPercent = 1.0f;
        physicalWeaponDamageAddFlat = 0.0f;
        allDamageMultiplier = 1.0f;
        allMitigationAddPercent = 1.0f;
        TurnsSinceLastCombatAction = 999;
        turnsSinceLastDamaged = 999;

        levelScaled = false;
        targetScalingLevel = 0;

        myTargetTile = Vector2.zero;
        storeTurnData = new TurnData();

        // Dummy ability.
        createWarningSquares = new AbilityScript();
        createWarningSquares.refName = "dummy_createwarningsquares";
        createWarningSquaresSubEffect = new SummonActorEffect();
        createWarningSquares.listEffectScripts.Add(createWarningSquaresSubEffect);
        createWarningSquaresSubEffect.parentAbility = createWarningSquares;
        createWarningSquaresSubEffect.effectRefName = "summonwarningsquares";
        createWarningSquaresSubEffect.summonActorPerTile = true;
        createWarningSquaresSubEffect.summonActorType = ActorTypes.DESTRUCTIBLE;
        createWarningSquaresSubEffect.summonActorRef = "obj_dangersquare";
        createWarningSquaresSubEffect.summonOnCollidable = true;
        createWarningSquaresSubEffect.actOnlyWithSummoner = true;
        createWarningSquaresSubEffect.dieWithSummoner = true;
        createWarningSquaresSubEffect.anchorRange = 99;
        createWarningSquaresSubEffect.anchorType = TargetActorType.SELF;
        createWarningSquaresSubEffect.silent = true;
        considerAbilities = new List<MonsterPowerData>();
        myJob = GameMasterScript.monsterJob;

        championMods = new List<ChampionMod>();

        dictMonsterPowersStrToMPD = new Dictionary<string, MonsterPowerData>();
    }

    public void TurnIntoCoolfrog(Map m)
    {
        SetActorData("coolfrog", 1);
        Item newItem = LootGeneratorScript.GenerateLoot(m.challengeRating + 0.1f, 3.0f);
        myInventory.AddItemRemoveFromPrevCollection(newItem, true);
        newItem = LootGeneratorScript.GenerateLoot(m.challengeRating + 0.1f, 3.0f);
        myInventory.AddItemRemoveFromPrevCollection(newItem, true);
        myStats.AddStatusByRef("status_goldaura", this, 99);
        displayName = StringManager.GetString("mon_coolfrog");
    }

    public void ConvertToWildUntamedForQuest()
    {
        // Let's convert the monster to a wild one!
        SetActorData("tcmrarityup", 1);
        myStats.BoostStatByPercent(StatTypes.HEALTH, 0.5f);
        myStats.BoostCoreStatsByPercent(0.2f);
        aggroRange += 4;
        RemoveAttribute(MonsterAttributes.TIMID);
        RemoveAttribute(MonsterAttributes.BERSERKER);
        RemoveAttribute(MonsterAttributes.GANGSUP);
        RemoveAttribute(MonsterAttributes.PREDATOR);
        RemoveAttribute(MonsterAttributes.STALKER);

        displayName = StringManager.GetString("misc_monster_wilduntamed") + " " + displayName;

        SetActorData("monstertotame", 1);

        // Also, give the monster some pals!

        int numPals = 3;
        for (int i = 0; i < numPals; i++)
        {
            Monster monsterPal = MonsterManagerScript.CreateMonster(actorRefName, true, true, false, 0f, false);
            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, monsterPal, true);
            monsterPal.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.5f);
            monsterPal.myStats.BoostCoreStatsByPercent(0.2f);
            monsterPal.aggroRange += 4;
            monsterPal.RemoveAttribute(MonsterAttributes.TIMID);
            monsterPal.RemoveAttribute(MonsterAttributes.BERSERKER);
            monsterPal.RemoveAttribute(MonsterAttributes.GANGSUP);
            monsterPal.RemoveAttribute(MonsterAttributes.PREDATOR);
            monsterPal.RemoveAttribute(MonsterAttributes.STALKER);
            MapTileData placeTile = GetActorMap().GetRandomEmptyTile(GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
            GetActorMap().PlaceActor(monsterPal, placeTile);
        }
    }

    public void ConvertToPacifiedGreedyForQuest(Item iRef)
    {
        foodLovingMonster = true;
        AddAttribute(MonsterAttributes.GREEDY, 100);
        RemoveAttribute(MonsterAttributes.STALKER);
        aggroRange = 0;
        turnsToLoseInterest = 4;
        wantsItem = iRef;
        actorFlags[(int)ActorFlags.GREEDYFORQUEST] = true;
        StringManager.SetTag(0, iRef.displayName);
        StringManager.SetTag(1, displayName);
        displayName = StringManager.GetString("add_item_loving_to_name");
    }

    public void RemoveItemLovingAttributeFromSelf()
    {
        foodLovingMonster = false;
        AddAttribute(MonsterAttributes.GREEDY,myTemplate.monAttributes[(int)MonsterAttributes.GREEDY]);
        AddAttribute(MonsterAttributes.STALKER,myTemplate.monAttributes[(int)MonsterAttributes.STALKER]);
        aggroRange = myTemplate.aggroRange;
        turnsToLoseInterest = myTemplate.turnsToLoseInterest;
        wantsItem = null;
        actorFlags[(int)ActorFlags.GREEDYFORQUEST] = false;
        displayName = myTemplate.monsterName;

        myStats.RemoveAllStatusByRef("enemy_quest_target");

        Debug.Log("Removed food loving attribute from " + actorRefName + " " + actorUniqueID);
    }

    public string GetWantedItem()
    {
        if (wantsItem == null) return "";
        return wantsItem.actorRefName;
    }

    public void ScaleWeaponToLevelAsPetOrSummon(int weaponLookupLevel)
    {
        myEquipment.GetWeapon().power = Weapon.expectedPetOrSummonWeaponPower[weaponLookupLevel];
    }

    // Scales to the desired MONSTER LEVEL. This is NOT the desired PLAYER LEVEL
    // Player level 8 for example does not necessarily fight MONSTER level 8. That could be too hard.
    // In the regular, non-expanded game, monster level 12 is the hardest while player can reach level 15!
    public void ScaleToSpecificLevel(int targetLevel, bool scalingOnLoad, bool scaleToPlayerLevel = false)
    {
        //Debug.Log("Request scale monster to " + targetLevel + " " + scaleToPlayerLevel + " " + actorRefName);
        if (targetLevel == 0 || actorfaction == Faction.PLAYER)
        {
            //Debug.Log("Can't scale " + actorRefName + " to " + targetLevel);
            return;
        }

        // Compare our health to target health, and adjust as needed

        float expectHealth = BalanceData.expectedMonsterHealth[myStats.GetLevel()];        

        float percentHealthVsExpected = myStats.GetMaxStat(StatTypes.HEALTH) / expectHealth;
        
        if (xpMod > 0)
        {
            percentHealthVsExpected = Mathf.Clamp(percentHealthVsExpected, 0.8f, 99f);
        }

        float newHealth = percentHealthVsExpected * BalanceData.expectedMonsterHealth[targetLevel];

        //Debug.Log(actorRefName + " in " + dungeonFloor + " " + isBoss + " " + isChampion + " we are level " + myStats.GetLevel() + " and % health is " + expectHealth + " v " + percentHealthVsExpected);        

        if (GameStartData.NewGamePlus >= 1)
        {
            float boost = (1f + MonsterManagerScript.GetHealthBoostForNewGamePlus(this));            
            newHealth *= boost;
        }

        float healthCap = BalanceData.expectedMonsterHealth[targetLevel] * 2f;

        if (isBoss || isChampion)
        {
            healthCap = BalanceData.expectedMonsterHealth[targetLevel] * 3f;
        }

        //Debug.Log("New health is " + newHealth + " and cap is " + healthCap + " scale to plvl? " + scaleToPlayerLevel + " Target level? " + targetLevel);

        if (newHealth > healthCap && !scaleToPlayerLevel)
        {
            newHealth = healthCap; // relevant for NG+ scaling. Don't overdo it!
        }

        //Debug.Log(myStats.GetMaxStat(StatTypes.HEALTH) + " " + actorRefName + " " + isBoss + " " + isChampion + " " + expectHealth + " " + percentHealthVsExpected);
        //Debug.Log(healthCap + " " + scaleToPlayerLevel + " " + myStats.GetLevel() + " new health: " + newHealth);

        myStats.SetStat(StatTypes.HEALTH, newHealth, StatDataTypes.ALL, true);

        float weapExpect = BalanceData.expectedMonsterWeaponPower[myStats.GetLevel()];

        /* if (!isBoss)
        {
            if (GameStartData.newGamePlus >= 1) weapExpect *= 0.9f;
            if (GameStartData.newGamePlus >= 2) weapExpect *= 0.9f;
        } */

        // this was using Weapon.expectedMonsterWeaponPower before.
        float percentWeaponPowerVsExpected = myEquipment.GetWeaponPower(myEquipment.GetWeapon()) / weapExpect;

        if (xpMod > 0)
        {
            percentWeaponPowerVsExpected = Mathf.Clamp(percentWeaponPowerVsExpected, 0.8f, 99f);
        }

        if (GameStartData.NewGamePlus > 0)
        {
            percentWeaponPowerVsExpected = 1f + (GameStartData.NewGamePlus * 0.1f); // Don't overdo it
        }

        // this was using Weapon.expectedMonsterWeaponPower before.
        float newWP = percentWeaponPowerVsExpected * weapExpect;
        myEquipment.GetWeapon().power = newWP;

        if (myEquipment.GetOffhand() as Weapon != null)
        {
            Weapon ohWeap = myEquipment.GetOffhand() as Weapon;
            ohWeap.power = newWP;
        }

        levelScaled = true;
        targetScalingLevel = targetLevel;

        // Core stat changes are not to be changed on load, as the stats themselves are properly serialized.
        if (!scalingOnLoad)
        {
            int numStatsPerRow = 3; // Core Stats, Charge Time, Accuracy                
            for (int i = 0; i < (int)StatBlock.nonResourceStats.Length; i++)
            {
                float percentVsExpected = myStats.GetMaxStat(StatBlock.nonResourceStats[i]) / BalanceData.expectedStatValues[myStats.GetLevel() * numStatsPerRow];

                if (xpMod > 0)
                {
                    percentVsExpected = Mathf.Clamp(percentVsExpected, 0.8f, 99f);
                }

                float newValue = percentVsExpected * BalanceData.expectedStatValues[targetLevel * numStatsPerRow];

                if (newValue >= (BalanceData.expectedStatValues[targetLevel * numStatsPerRow] * 1.45f))
                {
                    newValue = BalanceData.expectedStatValues[targetLevel * numStatsPerRow] * 1.45f; // again relevant for NG+
                }

                myStats.SetStat(StatBlock.nonResourceStats[i], newValue, StatDataTypes.ALL, true);
            }

            if (ReadActorData("dontscalect") != 1)
            {
                myStats.SetStat(StatTypes.CHARGETIME, BalanceData.expectedStatValues[(targetLevel * numStatsPerRow) + 1], StatDataTypes.ALL, true);
            }
            
            myStats.SetStat(StatTypes.ACCURACY, BalanceData.expectedStatValues[(targetLevel * numStatsPerRow) + 2], StatDataTypes.ALL, true);
        }

        if (aggroRange < 2)
        {
            aggroRange = 2;
        }
        if (aggroRange > 4)
        {
            aggroRange = 4;
        }

        RemoveAttribute(MonsterAttributes.STALKER);
        RemoveAttribute(MonsterAttributes.TIMID);

        if (targetLevel >= 5)
        {
            if (aggroRange < 3) aggroRange = 3;
            if (aggroRange > 5) aggroRange = 5;
            RemoveAttribute(MonsterAttributes.PREDATOR);
            if (CheckAttribute(MonsterAttributes.LOVESBATTLES) < 33)
            {
                AddAttribute(MonsterAttributes.LOVESBATTLES, 33);
            }

        }
        if (targetLevel >= 9)
        {
            if (aggroRange < 4) aggroRange = 4;
            if (aggroRange > 6) aggroRange = 6;
            RemoveAttribute(MonsterAttributes.GANGSUP);
            RemoveAttribute(MonsterAttributes.BERSERKER);
            if (CheckAttribute(MonsterAttributes.LOVESBATTLES) < 33)
            {
                AddAttribute(MonsterAttributes.LOVESBATTLES, 75);
            }
        }
        if (targetLevel >= 12)
        {
            if (aggroRange < 5) aggroRange = 5;
            if (CheckAttribute(MonsterAttributes.LOVESBATTLES) < 33)
            {
                AddAttribute(MonsterAttributes.LOVESBATTLES, 100);
            }
        }

        myStats.SetLevel(targetLevel);
        
        // dont change the xp mod, damn it!!!!

        if (targetLevel >= BalanceData.LEVEL_TO_CV.Length)
        {
            targetLevel = BalanceData.LEVEL_TO_CV.Length - 1;
        }
        challengeValue = BalanceData.LEVEL_TO_CV[targetLevel];

        SetBattleDataDirty();
    }

    public void ScaleWithDifficulty(float cv)
    {
        float diff = cv - challengeValue;
        if (diff > 0f)
        {
            allDamageMultiplier += diff * 1.5f;
            allMitigationAddPercent -= diff / 10f; // Was additive. Wrong direction?
            myStats.BoostStatByPercent(StatTypes.HEALTH, (diff * 3.1f));
        }
    }



    public int GetActorOfInterestUniqueID()
    {
        if (myActorOfInterest == null)
        {
            return 0;
        }
        if (myActorOfInterest.destroyed)
        {
            return 0;
        }
        return myActorOfInterest.actorUniqueID;
    }

    public int GetTargetUniqueID()
    {
        if (myTarget == null)
        {
            return 0;
        }
        if (myTarget.destroyed)
        {
            return 0;
        }
        return myTarget.actorUniqueID;
    }



    public int CheckAttribute(MonsterAttributes ma)
    {
        return myAttributes[(int)ma];
    }

    public void AddAttribute(MonsterAttributes ma, int value)
    {
        myAttributes[(int)ma] = value;
    }
    public void RemoveAttribute(MonsterAttributes ma)
    {
        myAttributes[(int)ma] = 0;
    }

    public void ApplyHeroPetAttributes()
    {
        SetAnchor(GameMasterScript.heroPCActor);
        RemoveAttribute(MonsterAttributes.CANTACT);
        pushedThisTurn = false;
        cachedBattleData.maxMoveRange = 1;
        surpressTraits = false;
        actorfaction = Faction.PLAYER;
        bufferedFaction = Faction.PLAYER;
        AddAttribute(MonsterAttributes.LOVESLAVA, 100);
        if (tamedMonsterStuff != null)
        {
            MonsterBeautyStuff.ProcessPetToAddAppropriateBeautyEffects(tamedMonsterStuff);
        }        
    }

    public void HitWithMallet()
    {
        RemoveActorData("costumeparty");
        RemoveActorData("bigmode");
#if UNITY_EDITOR
        Debug_AddMessage("->NEUTRAL: *bonked*");
#endif
        SetState(BehaviorState.NEUTRAL);
        surpressTraits = true;
        AddAttribute(MonsterAttributes.CANTACT, 100);
        AddAttribute(MonsterAttributes.CANTATTACK, 100);
        AddAttribute(MonsterAttributes.LOVESMUD, 100); // Reduce annoyances with KO'd monsters
        AddAttribute(MonsterAttributes.LOVESLAVA, 100); // Reduce annoyances with KO'd monsters
        moveRange = 0;
        cachedBattleData.maxMoveRange = 0;
        if (myAnimatable != null)
        {
            myAnimatable.StopAnimation();
            if (!flipSpriteY)
            {
                myAnimatable.FlipSpriteY();
            }
        }
        anchor = GameMasterScript.heroPCActor;
        anchorID = GameMasterScript.heroPCActor.actorUniqueID;
        anchorRange = 0;
        ClearCombatTargets();
        summoner = GameMasterScript.heroPCActor;
        actorfaction = Faction.PLAYER;
        bufferedFaction = Faction.PLAYER;
        if (!myStats.CheckHasStatusName("sleepvisual"))
        {
            myStats.AddStatusByRef("sleepvisual", GameMasterScript.heroPCActor, 0);
        }
        myStats.RemoveTemporaryNegativeStatusEffects();        
        myStats.ForciblyRemoveStatus("status_preexplode"); // Also, remove self-destruct visual effect.

        // Also, remove storing turn data.
        storeTurnData = null;
        storingTurnData = false;

        if (myMovable != null)
        {
            myMovable.RemoveParticleSystem("charging_skill");
        }
        
        myStats.RemoveStatusesByFlag(StatusFlags.HALTMOVEMENT);
        flipSpriteY = true;
        noAnimation = true;
    }



    private int SimpleRand(int max)
    {
        int value = UnityEngine.Random.Range(0, max + 1);
        int neg = UnityEngine.Random.Range(0, 2);
        if (neg == 0)
        {
            value = value * -1;
        }
        return value;
    }

    public void TickAllCombatants()
    {
        for (int i = 0; i < GetNumCombatTargets(); i++)
        {
            combatTargets[i].turnsSinceCombatAction++;
        }
    }

    public float InternalGetXPMod(int inputLevel)
    {
        int diff = inputLevel - myStats.GetLevel();

        float localxpMod = 0;

        if (diff >= 0)
        {
            if (diff < BalanceData.playerMonsterRewardTable.GetLength(1) && inputLevel < BalanceData.playerMonsterRewardTable.GetLength(0))
            {
                localxpMod = BalanceData.playerMonsterRewardTable[inputLevel, diff];
            }
            else
            {
                localxpMod = 0.0f;
                if (Debug.isDebugBuild) Debug.Log("XP mod issue. Player level " + inputLevel + " vs my level " + actorRefName + " " + myStats.GetLevel() + " " + diff);
            }

        }
        else
        {
            localxpMod = 1 + (-0.05f * diff);
        }
         
        localxpMod *= xpMod; // Don't use TEMPLATE here?
        return localxpMod;
    }

    public float GetXPModToPlayer()
    {
        float value = 0f;
        try { value = InternalGetXPMod(GameMasterScript.heroPCActor.myStats.GetLevel()); }
        catch (Exception e)
        {
            string extraInfo = "";
            if (myStats != null)
            {
                extraInfo = "Mon level: " + myStats.GetLevel();
            }
            else
            {
                extraInfo = "NULL stats?";
            }
            Debug.Log(actorRefName + " " + extraInfo + " PLevel " + GameMasterScript.heroPCActor.myStats.GetLevel() + " " + dungeonFloor + " " + displayName + " error when con to player: " + e);
        }
        return value;
    }

    public static string EvaluateThreat(int monsterLevel)
    {
        int diff = GameMasterScript.heroPCActor.myStats.GetLevel() - monsterLevel;
        // 3 - 1 = 2 levels higher
        // 3 - 5 = -2 levels higher

        if (diff >= 0)
        {
            float localxpMod = 0f;

            if (diff >= BalanceData.playerMonsterRewardTable.GetLength(1))
            {
                diff = BalanceData.playerMonsterRewardTable.GetLength(1) - 1;
            }

            try { localxpMod = BalanceData.playerMonsterRewardTable[GameMasterScript.heroPCActor.myStats.GetLevel(), diff]; }
            catch (Exception e)
            {
                Debug.Log("Error trying to evaluate threat... Hero is " + GameMasterScript.heroPCActor.myStats.GetLevel() + " and level is " + monsterLevel);
                Debug.Log(e);
                return "Error Evaluating Threat";
            }

            if (localxpMod >= 0.9f)
            {
                return StringManager.GetString("difficulty_5");
            }
            if (localxpMod >= 0.75f)
            {
                return StringManager.GetString("difficulty_4");
            }
            if (localxpMod >= 0.5f)
            {
                return UIManagerScript.greenHexColor + StringManager.GetString("difficulty_3") + " </color>";
            }
            if (localxpMod >= 0.25f)
            {
                return UIManagerScript.greenHexColor + StringManager.GetString("difficulty_2") + "</color>";
            }
            if (localxpMod > 0.0f)
            {
                return UIManagerScript.greenHexColor + StringManager.GetString("difficulty_1") + "</color>";
            }
            else return UIManagerScript.silverHexColor + StringManager.GetString("difficulty_0") + "</color>";
        }
        if (diff == -1)
        {
            return StringManager.GetString("difficulty_6");
        }
        else if (diff == -2)
        {
            return "<color=yellow>" + StringManager.GetString("difficulty_7") + "</color>";
        }
        else if (diff == -3 || diff == -4)
        {
            return UIManagerScript.orangeHexColor + StringManager.GetString("difficulty_8") + "</color>";
        }
        else if (diff < -4 && diff > -7)
        {
            return "<color=red>" + StringManager.GetString("difficulty_9") + "</color>";
        }
        else if (diff <= -7)
        {
            return "<color=red>" + StringManager.GetString("difficulty_10") + "</color>";
        }

        return "???";
    }

    public string EvaluateThreatToPlayer()
    {
        return EvaluateThreat(myStats.GetLevel());
    }

    private bool DisplayMonAttributeMessage()
    {
        if (UnityEngine.Random.Range(0, 1f) <= 0.25f && GameMasterScript.heroPCActor.visibleTilesArray[(int)GetPos().x, (int)GetPos().y])
        {
            return true;
        }
        return false;
    }



    private AbilityScript FetchLocalAbility(AbilityScript template)
    {
        for (int i = 0; i < myAbilities.abilities.Count; i++)
        {
            if (myAbilities.abilities[i].refName == template.refName)
            {
                return myAbilities.abilities[i];
            }
        }
        return null;
    }

    void RemoveAllAttributes()
    {
        for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
        {
            myAttributes[i] = 0;
        }
    }





    IEnumerator WaitPassResultsToGMS(List<CombatResult> results, List<Actor> affectedActors, float waitTime)
    {
        GameMasterScript.SetAnimationPlaying(true);
        yield return new WaitForSeconds(waitTime);
        GameMasterScript.SetAnimationPlaying(false);
    }

    public void SetMoveBehavior(MoveBehavior behave)
    {
        myMoveBehavior = behave;
    }

    public void SetMoveBoundary(MoveBoundary range)
    {
        myMoveBoundary = range;
    }



    public void AlterFromItemWorldProperties(ItemWorldMetaData itemWorldProperties, List<string> priorityItemRefs)
    {
        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_HEALTH])
        {
            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.25f);
        }
        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_CRITICAL])
        {
            cachedBattleData.critChanceMod += 0.05f;
            cachedBattleData.critDamageMod += 0.2f;
        }
        if (itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_MELEEBOOST])
        {
            myStats.AddStatusByRef("itemdream_meleeboost", this, 0);
        }
        if ((itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_GEARSET]) && (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_IW_GEARSET_BONUS))
        {
            if (priorityItemRefs.Count > 0)
            {
                string getRef = priorityItemRefs[UnityEngine.Random.Range(0, priorityItemRefs.Count)];
                Item addPriority = LootGeneratorScript.CreateItemFromTemplateRef(getRef, challengeValue, 0f, false);
                myInventory.AddItemRemoveFromPrevCollection(addPriority, false);
            }
        }
        if ((itemWorldProperties.properties[(int)ItemWorldProperties.TYPE_GILDED]) && (UnityEngine.Random.Range(0, 1f) <= 0.2f))
        {
            moneyHeld = UnityEngine.Random.Range(6, 13) * myStats.GetLevel();
        }
    }

    public void AddStatusAtRandomAngle(string statusRef)
    {
        StatusEffect template = GameMasterScript.FindStatusTemplateByName(statusRef);
        StatusEffect se = new StatusEffect();
        se.CopyStatusFromTemplate(template);
        se.curDuration = 9999;
        se.maxDuration = 9999;

        int randomDir = UnityEngine.Random.Range(0, 8);
        Vector2 check = GetPos() + MapMasterScript.xDirections[randomDir];
        MapTileData mtd = MapMasterScript.GetTile(check);
        bool[] tried = new bool[8];
        while (mtd.tileType == TileTypes.WALL)
        {
            randomDir = UnityEngine.Random.Range(0, 8);
            bool allTried = true;
            for (int x = 0; x < tried.Length; x++)
            {
                if (!tried[x])
                {
                    allTried = false;
                    break;
                }
            }
            if (allTried) break;
            check = GetPos() + MapMasterScript.xDirections[randomDir];
            mtd = MapMasterScript.GetTile(check);
            tried[randomDir] = true;
        }
        se.direction = MapMasterScript.allDirections[randomDir];

        se.listEffectScripts[0].originatingActor = GameMasterScript.heroPCActor;
        se.listEffectScripts[0].selfActor = this;
        se.listEffectScripts[0].parentAbility = se;
        myStats.AddStatus(se, GameMasterScript.heroPCActor);
    }



    public static string GetFamilyName(string refName)
    {
        if (familyNamesVerbose.ContainsKey(refName))
        {
            return familyNamesVerbose[refName];
        }
        else
        {
            Debug.Log("Couldn't find real name for " + refName);
            return refName;
        }
    }

    public void ReverseMalletEffect()
    {
        RemoveAttribute(MonsterAttributes.CANTACT);
        RemoveAttribute(MonsterAttributes.CANTATTACK);
        AddAttribute(MonsterAttributes.LOVESLAVA, myTemplate.monAttributes[(int)MonsterAttributes.LOVESLAVA]);
        AddAttribute(MonsterAttributes.LOVESMUD, myTemplate.monAttributes[(int)MonsterAttributes.LOVESLAVA]);

        surpressTraits = false;
        flipSpriteY = false;
        noAnimation = false;
        if (myAnimatable != null)
        {
            myAnimatable.SetAnim(myAnimatable.defaultIdleAnimationName);
            myAnimatable.FlipSpriteY();
        }
        moveRange = myTemplate.moveRange;
        actorfaction = Faction.ENEMY;
        bufferedFaction = Faction.ENEMY;
        anchor = null;
        summoner = null;
        SetBattleDataDirty();
        myBehaviorState = BehaviorState.NEUTRAL;
        myStats.RemoveStatusByRef("sleepvisual");

        if (actorUniqueID == GameMasterScript.heroPCActor.ReadActorData("knockedoutmonster"))
        {
            GameMasterScript.heroPCActor.RemoveActorData("knockedoutmonster");
        }
    }

    public string GetMonsterResistanceString(bool keenEyes)
    {
        return GetMonsterResistanceOrPierceStrings(keenEyes, true);
    }

    public string GetMonsterPierceResistanceString(bool keenEyes)
    {
        return GetMonsterResistanceOrPierceStrings(keenEyes, false);
    }

    // Checks elemental resistances from various sources.
    public string GetMonsterResistanceOrPierceStrings(bool keenEyes, bool resistance)
    {
        if (!resistStringDirty)
        {
            return resistString;
        }
        resistStringBuilder.Length = 0;
        resistStringBuilder2.Length = 0;
        bool firstStringEvaluated = true;

        // use a dict so we can clean up the display and combine 'like' res strings
        dictElemStrings.Clear();
        string localText = "";

        ResistanceData[] rdToCheck = cachedBattleData.resistances;
        if (!resistance)
        {
            rdToCheck = cachedBattleData.pierceResistances;
        }

        for (int i = 0; i < rdToCheck.Length; i++)
        {
            resistStringBuilder.Append(CustomAlgorithms.GetElementalSpriteStringFromSpriteFont(rdToCheck[i].damType));
            resistStringBuilder.Append("     ");
            //string elementalNameString = CustomAlgorithms.GetElementalSpriteStringFromSpriteFont(rdToCheck[i].damType) + "     ";
            StringManager.SetTag(0, resistStringBuilder.ToString());
            localText = "";
            float mult = rdToCheck[i].multiplier;
            if (rdToCheck[i].absorb)
            {
                if (resistance)
                {
                    localText = "misc_damage_immune";
                }
                else
                {
                    localText = "misc_damagepierce_nullify";
                }
            }
            else if (rdToCheck[i].multiplier <= 0.6f)
            {
                if (resistance)
                {
                    localText = "misc_damage_resistantstrong";
                }
                else
                {
                    localText = "misc_damagepierce_pierce";
                }

            }
            else if (rdToCheck[i].multiplier <= 0.85f)
            {
                if (resistance)
                {
                    localText = "misc_damage_resistant";
                }
                else
                {
                    localText = "misc_damagepierce_pierce";
                }
            }
            else if (rdToCheck[i].multiplier >= 1.1f)
            {
                localText = "misc_damage_vulnerable";
            }
            else if (rdToCheck[i].multiplier >= 1.25f)
            {
                localText = "misc_damage_vulnerablestrong";
            }

            if (localText != "")
            {
                if (keenEyes && !rdToCheck[i].absorb)
                {
                    resistStringBuilder.Length = 0;
                    if (mult > 1f)
                    {
                        resistStringBuilder.Append("+");
                    }
                    resistStringBuilder.Append(((int)((mult - 1f) * 100f)));
                    resistStringBuilder.Append(StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                    localText += " (" + resistStringBuilder.ToString() + ")";
                }

                if (!dictElemStrings.ContainsKey(localText))
                {
                    dictElemStrings.Add(localText, new List<DamageTypes>());
                }
                dictElemStrings[localText].Add(rdToCheck[i].damType);

                //builder += StringManager.GetString(localText);
            }

        }

        resistStringBuilder.Length = 0;
        firstStringEvaluated = true;
        // Now combine stuff from the dict.        
        foreach (string key in dictElemStrings.Keys)
        {
            //localText = "";
            resistStringBuilder2.Length = 0;
            foreach (DamageTypes dt in dictElemStrings[key])
            {
                string elementalNameString = CustomAlgorithms.GetElementalSpriteStringFromSpriteFont(dt) + "     ";
                //localText += elementalNameString;
                resistStringBuilder2.Append(elementalNameString);
            }
            StringManager.SetTag(0, resistStringBuilder2.ToString());

            string refNameOfString = key; // but this could look like misc_blah_whatever (100%)
            refNameOfString = refNameOfString.Replace(" ", "");
            refNameOfString = Regex.Replace(refNameOfString, @"\([^()]*\)", "");

            string extra = "";
            extra = key.Replace(refNameOfString, ""); // this adds (100%) if any

            string addToBuilder = StringManager.GetString(refNameOfString) + extra;
            if (firstStringEvaluated)
            {
                //builder += addToBuilder;
                resistStringBuilder.Append(addToBuilder);
                firstStringEvaluated = false;
            }
            else
            {
                //builder += ", " + addToBuilder;
                resistStringBuilder.Append(", ");
                resistStringBuilder.Append(addToBuilder);
            }
        }

        if (resistStringBuilder.Length > 0)
        {
            resistStringBuilder2.Length = 0;
            //builder = "\n" + builder + "";
            resistStringBuilder2.Append("\n");
            resistStringBuilder2.Append(resistStringBuilder.ToString());
        }

        resistString = resistStringBuilder2.ToString();
        resistStringDirty = false;
        return resistString;
    }

    public void CopyStatsAndWeaponFromTemplate(string templateRef)
    {
        MonsterTemplateData mtd = GameMasterScript.masterMonsterList[templateRef];

        myStats.SetStat(StatTypes.HEALTH, mtd.hp, StatDataTypes.ALL, true);
        myStats.SetStat(StatTypes.STRENGTH, mtd.strength, StatDataTypes.ALL, true);
        myStats.SetStat(StatTypes.DISCIPLINE, mtd.discipline, StatDataTypes.ALL, true);
        myStats.SetStat(StatTypes.SPIRIT, mtd.spirit, StatDataTypes.ALL, true);
        myStats.SetStat(StatTypes.GUILE, mtd.guile, StatDataTypes.ALL, true);
        myStats.SetStat(StatTypes.SWIFTNESS, mtd.swiftness, StatDataTypes.ALL, true);
        challengeValue = mtd.challengeValue;
        myEquipment.equipment[(int)EquipmentSlots.WEAPON] = LootGeneratorScript.CreateItemFromTemplateRef(mtd.weaponID, 0f, 0f, false) as Weapon;
    }

    public bool MakeMemoryKing(int champMods, Equipment eq)
    {
        for (int i = 0; i < champMods; i++)
        {
            MakeChampion();
        }
        if (!isChampion) return false;

        if (challengeValue >= 1.5f)
        {
            ChampionMod memoryMod = null;
            bool valid = false;
            int tries = 0;
            while (!valid)
            {
                tries++;
                valid = true;
                memoryMod = GameMasterScript.masterMemoryKingChampModList[UnityEngine.Random.Range(0, GameMasterScript.masterMemoryKingChampModList.Count)];
                foreach (ChampionMod cm in championMods)
                {
                    if (cm.exclusionGroup == memoryMod.exclusionGroup)
                    {
                        valid = false;
                        break;
                    }
                }
                if (tries > 200) break;
            }
            MakeChampionFromMod(memoryMod);
            aggroRange += 2;
            myStats.SetStat(StatTypes.CHARGETIME, 99f, StatDataTypes.ALL, true);
            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.25f);
        }

        displayName = "<color=yellow>" + StringManager.GetString("mon_memory_king_disp") + " " + myTemplate.monsterName + "</color>";
        if (eq != null)
        {
            myInventory.AddItemRemoveFromPrevCollection(eq, false);
        }
        isItemBoss = true;
        isBoss = true;
        return true;
    }

    public bool InMyLocalBounds(Vector2 pos)
    {
        if (!MapMasterScript.InBounds(pos)) return false;
            // #todo
        if (pos.x < localMovementXMin || pos.y < localMovementYMin || pos.x > localMovementXMax || pos.y > localMovementYMax)
        {
            return false;
        }
        return true;
    }

    // Add the Tuckered Out status to our PETS instead of the owner
    public void SetTuckeredOut()
    {
        int iCooldownDuration = 12;
        HeroPC hero = GameMasterScript.heroPCActor;
        myStats.AddStatusByRef("status_pet_call", hero, iCooldownDuration);        
    }

    public void UpdateMyMoveBoundaries()
    {
        if (actorRefName != "mon_finalboss2")
        {
            localMovementXMin = 1;
            localMovementXMax = MapMasterScript.activeMap.columns - 1;
            localMovementYMin = 1;
            localMovementYMax = MapMasterScript.activeMap.rows - 1;
        }
        else
        {
            localMovementXMin = 6;
            localMovementXMax = MapMasterScript.activeMap.columns - 6;
            localMovementYMin = 6;
            localMovementYMax = MapMasterScript.activeMap.rows - 6;
        }
    }

    public float GetHealPercentageAsPlayerPet()
    {
        float percentageToUse = GameMasterScript.PLAYER_PET_HEAL_PERCENTAGE;
        if (actorRefName == "mon_runiccrystal")
        {
            percentageToUse = GameMasterScript.RUNIC_HEAL_PERCENTAGE;
        }
        if (myStats.CheckHasStatusName("status_fasthealing"))
        {
            percentageToUse += 0.15f;
        }
        return percentageToUse;
    }

#if UNITY_EDITOR
    public void ResetDebugAIInfo()
    {
        var debugText = GetDebugInfoText();
        debugText.ClearAdditionalMessages();
    }

    //Make sure there's a display overhead that shows the current state of
    //this monster's AI
    public void UpdateDebugAIInfo()
    {
        if (!DebugConsole.IsOpen)
        {
            return;
        }

        //Sloppy, but only in debug/editor builds <3
        var debugText = GetDebugInfoText();
        debugText.SetMonster(this);
    }

    DebugMonsterInfo CreateDebugInfoText()
    {
        var newGO = GameObject.Instantiate(Resources.Load<GameObject>("ShepPrefabs/Debug_MonsterInfoText"), GetObject().transform);
        var debugText = newGO.GetComponent<DebugMonsterInfo>();
        debugText.SetMonster(this);
        return debugText;
    }

    DebugMonsterInfo GetDebugInfoText()
    {
        var debugText = GetObject().GetComponentInChildren<DebugMonsterInfo>();
        if (debugText == null)
        {
            debugText = CreateDebugInfoText();
        }
        return debugText;
    }

    //Adds a debug message over the monster's head that clears at the end of the turn
    public void Debug_AddMessage(string strMessage)
    {
        if (!DebugConsole.IsOpen)
        {
            return;
        }

        var debugText = GetObject().GetComponentInChildren<DebugMonsterInfo>();
        if (debugText == null)
        {
            CreateDebugInfoText();
        }

        debugText.AddAdditionalMessage("<size=3>" + strMessage + "</size>");
    }

#endif

    public void RemoveMonsterPowerByAbilityRef(string abilRef)
    {
        MonsterPowerData powerToForget = null;
        foreach(MonsterPowerData mpd in monsterPowers)
        {
            if (mpd.abilityRef.refName == abilRef)
            {
                powerToForget = mpd;
            }
        }
        monsterPowers.Remove(powerToForget);

        if (myAbilities != null)
        {
            myAbilities.RemoveAbility(abilRef, false);
        }        
    }

    /// <summary>
    /// Runs when a monster dies. IF we are haunting something/someone, we should remove our haunt status from that target.
    /// </summary>
    public void CheckForAndRemoveHauntTargetOnDeath()
    {
        int myHauntTarget = ReadActorData("haunttarget");
        if (myHauntTarget < 0) return;

        Actor findActor;
        if (GameMasterScript.dictAllActors.TryGetValue(myHauntTarget, out findActor))
        {
            Fighter ft = findActor as Fighter;
            if (ft == null) return;
            ft.myStats.RemoveAllStatusByRef("status_haunted");
        }
    }

    public void SaveWeaponSwingDataToDict()
    {
        if (myEquipment.GetWeapon() == null) return;
        if (!string.IsNullOrEmpty(myEquipment.GetWeapon().impactEffect))
        {
            SetActorDataString("weaponimpactfx", myEquipment.GetWeapon().impactEffect);
        }
        if (!string.IsNullOrEmpty(myEquipment.GetWeapon().swingEffect))
        {
            SetActorDataString("weaponswingfx", myEquipment.GetWeapon().swingEffect);
        }
    }

    public void TryOverwritingWeaponFXFromDict()
    {
        if (dictActorDataString == null) return;
        string fx;
        if (dictActorDataString.TryGetValue("weaponimpactfx", out fx))
        {
            myEquipment.GetWeapon().impactEffect = fx;
        }
        if (dictActorDataString.TryGetValue("weaponswingfx", out fx))
        {
            myEquipment.GetWeapon().swingEffect = fx;
        }
    }

    /// <summary>
    /// Called when examining an untamed monster and we want to reveal its rarity value for corral
    /// </summary>
    /// <returns></returns>
    public string GetUntamedMonsterRarityString()
    {
        int rarity = -1;
        if ((MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR || actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID()) && actorfaction == Faction.PLAYER)
        {
            // this might be a corral beast, or our pet, so it might have a TCM?
            if (tamedMonsterStuff != null)
            {
                rarity = tamedMonsterStuff.unique;
            }
        }        
        if (rarity == -1) // this is a wild, definitely not tamed monster
        {
            rarity = ReadActorData("runiq");
        }
        if (rarity < 0)
        {
            // not set yet!
            rarity = UnityEngine.Random.Range(0, 101);
            if (ReadActorData("tcmrarityup") > 0) // "wild untamed" creatures have better rarity
            {
                rarity = UnityEngine.Random.Range(70, 101);
            }
            SetActorData("runiq", rarity);
        }

        return TamedCorralMonster.GetRarityStringByValue(rarity);
    }

    public List<MonsterPowerData> GetMonsterLetterWritableAbilities()
    {
        List<MonsterPowerData> mpd = new List<MonsterPowerData>();
        foreach(MonsterPowerData power in monsterPowers)
        {
            if (invalidAbilitiesForMonsterLetters.Contains(power.abilityRef.refName))
            {
                continue;
            }
            mpd.Add(power);
        }
        return mpd;
    }

    public void VerifyMaxHealthAsCorralPet()
    {
        float maxHealth = 4000f;
        if (GameStartData.NewGamePlus == 1) maxHealth = 6000f;
        if (GameStartData.NewGamePlus == 2) maxHealth = 8000f;
        if (myStats.GetMaxStat(StatTypes.HEALTH) > maxHealth)
        {
            myStats.SetStat(StatTypes.HEALTH, maxHealth, StatDataTypes.MAX, true);
        }
        tamedMonsterStuff.baseMonsterHealth = maxHealth;
    }

    public void OnMonsterPowerAdded(MonsterPowerData mpd, AbilityScript abil)
    {
        if (!dictMonsterPowersStrToMPD.ContainsKey(abil.refName))
        {
            dictMonsterPowersStrToMPD.Add(abil.refName, mpd);
        }
    }

    public void RemoveAllChampionModsAndAbilities()
    {
        foreach(ChampionMod cm in championMods)
        {
            foreach(MonsterPowerData mpd in cm.modPowers)
            {
                monsterPowers.Remove(mpd);
                if (mpd.abilityRef == null) continue;
                dictMonsterPowersStrToMPD.Remove(mpd.abilityRef.refName);
            }
        }

        championMods.Clear();
    }

    public string PrintCorralDebug()
    {
        if (!Debug.isDebugBuild) return "";
        if (tamedMonsterStuff == null)
        {
            return actorRefName + " " + displayName + " aid: " + actorUniqueID + " nothing to debug...?";
        }
        else 
        {
            return actorRefName + " (" + displayName + ") actorID: " + actorUniqueID + " prefab: " + prefab + " shared corral ID: " + tamedMonsterStuff.sharedBankID;
        }
        
    }

    private void TryReassignIDForCorralPet()
    {
        int oldID = actorUniqueID;
        GameMasterScript.AssignActorID(this, 900000);
        GameMasterScript.AddActorToDict(this);

        if (Debug.isDebugBuild) Debug.Log("Attempting to reassign ID... Is now: " + actorUniqueID);
        if (!isInCorral)
        {
            int heroPetSharedID = GameMasterScript.heroPCActor.GetMonsterPetSharedID();

            if (Debug.isDebugBuild) Debug.Log("Compare " + heroPetSharedID + " to " + tamedMonsterStuff.sharedBankID);

            bool putBackInCorral = true;

            if (heroPetSharedID == tamedMonsterStuff.sharedBankID)
            {
                if (Debug.isDebugBuild) Debug.Log("Wait it's the same monster. Is it in the same map as hero?");

                if (GameMasterScript.heroPCActor.dungeonFloor == dungeonFloor)
                {
                    if (Debug.isDebugBuild) Debug.Log("Yes, it's the same floor. So keep it here.");
                    
                    GameMasterScript.heroPCActor.summonedActorIDs.Remove(oldID);
                    GameMasterScript.heroPCActor.summonedActorIDs.Add(actorUniqueID);

                    GameMasterScript.heroPCActor.SetMonsterPetID(actorUniqueID);
                    putBackInCorral = false;
                }                
                else 
                {
                    if (Debug.isDebugBuild) Debug.Log("No, it's somewhere else, so put it back in corral.");
                }
            }
            else 
            {
                if (Debug.isDebugBuild) Debug.Log("This is OUT of the corral. For safety, let's put this back in the corral.");
            }
            
            if (tamedMonsterStuff.sharedBankID == -1)
            {
                tamedMonsterStuff.sharedBankID = SharedCorral.GetUniqueSharedPetID();
            }

            if (putBackInCorral) 
            {
                MetaProgressScript.AddExistingTamedMonsterActorToCorral(this);
                
                //if (Debug.isDebugBuild) Debug.Log("This is player's current pet, reassigning it for hero.");
                GameMasterScript.heroPCActor.summonedActorIDs.Remove(oldID);
                GameMasterScript.heroPCActor.ResetPetData();

                if (dungeonFloor != 150)
                {
                    if (Debug.isDebugBuild) Debug.Log("Removing from current floor, too.");
                    Map currentMap = MapMasterScript.theDungeon.FindFloor(dungeonFloor);
                    currentMap.actorsInMap.Remove(this);
                }

                SetActorData("loadtime_repair", 1);
            }
            

            //GameMasterScript.heroPCActor.summonedActorIDs.Add(actorUniqueID);
            //GameMasterScript.heroPCActor.SetMonsterPetID(actorUniqueID);
        }
    }
}

public class MonsterFamily
{
    public string refName;
    public string displayName;
}