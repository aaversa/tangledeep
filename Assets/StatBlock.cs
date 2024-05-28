using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Globalization;
using System.Reflection;

public enum StatTypes { HEALTH, STAMINA, ENERGY, STRENGTH, SWIFTNESS, SPIRIT, DISCIPLINE, GUILE, CHARGETIME, ACCURACY, VISIONRANGE, RANDOM_RESOURCE, RANDOM_NONRESOURCE, RANDOM_CORE, ALLRESOURCE, COUNT };
public enum StatGrowths { STRENGTH, SWIFTNESS, SPIRIT, DISCIPLINE, GUILE, COUNT }

public class SingleStat
{
    public float cur;
    public float max;
    public float trueMax;
    public StatTypes statType;
    public int maxRegenRate;
    public int curRegenRate;
    public float maxRegenAmount;
    public float curRegenAmount;
    public int regenCounter = 0;

    public SingleStat()
    {
        cur = 0;
        max = 0;
        trueMax = 0;
        maxRegenAmount = 0;
        curRegenRate = 0;
        maxRegenAmount = 0;
        curRegenAmount = 0;
        statType = StatTypes.COUNT;
    }
}

public class StatBlock {

    Fighter owner;
    bool ownerSet;
    public static string[] statNames;
    //public static string[] resistNames;
    public static StatTypes[] coreStats;
    public static StatTypes[] expandedCoreStats;
    public static StatTypes[] nonResourceStats;
    public static StatTypes[] onlyResourceStats;
    private SingleStat[] statArray; // This SHOULD be used for everything.
    private int actorLevel;
    private int experiencePoints;
    private int xpToNextLevel;
    private List<StatusEffect> statuses;
    Dictionary<string, int> dictStatusQuantities;
    //Shep: For read access to all statuses in a stat block
    public IList<StatusEffect> GetStatuses() { return statuses.AsReadOnly(); }

    public const float MAX_POSSIBLE_PLAYER_STAT = 255f;
    public const float MAX_POSSIBLE_MONSTER_STAT = 150f;
    public const float MAX_POSSIBLE_MONSTER_STAT_SAVAGE = 200f;
    public const float STRENGTH_PERCENT_PHYSICALRESIST_MOD = 0.24f;
    public const float GUILE_PERCENT_PARRY_MOD = 0.25f;
    public const float GUILE_PERCENT_POWERUP_MOD = 0.25f;
    public const float GUILE_PERCENT_CRITCHANCE_MOD = 0.14f;
    public const float DISCIPLINE_PERCENT_ELEMRESIST_MOD = 0.33f;
    public const float DISCIPLINE_PERCENT_SPIRITPOWER_MOD = 0.65f;
    public const float SPIRIT_PERCENT_SPIRITPOWER_MOD = 1.3f;

    public const float HERO_BASE_STRENGTH = 25f;
    public const float HERO_BASE_SWIFTNESS = 20f;
    public const float HERO_BASE_DISCIPLINE = 20f;
    public const float HERO_BASE_SPIRIT = 20f;
    public const float HERO_BASE_GUILE = 20f;

    private List<StatusEffect> queuedStatuses; // These statuses are queued up to be added.
    public List<StatusEffect> statusesRemovedSinceLastTurn;
    public List<string> displayStatusNames;

    public const int MAX_MONSTER_PET_LEVEL = 12;
    public const int MAX_MONSTER_PET_LEVEL_NGPLUS = 15;
    public const int MAX_MONSTER_PET_LEVEL_EXPANSION = 15;
    public const int MAX_MONSTER_PET_LEVEL_NGPLUS_EXPANSION = 20;

    // Mem management /pooling
    List<StatusEffect> statusToRemove;
    static List<OverlayData> overlaysToRemove;

    // We use an ARRAY of hashsets here, with one for every status trigger, because while evaluating Run conditions for one trigger (like ATTACK)
    // we could end up evaluating another trigger, like ONADD, in which case we don't want to re-use the same hashset. That would be Bad.
    HashSet<StatusEffect>[] runStatusesEvaluated;

    public static List<StatusEffect> activeSongs;

    public static string[] statGrowthsAsString;

    //These are only used for save/load time, no need to localize
    public static readonly string[] statAbbreviationsNotLocalized = { "HL", "ST", "EN", "STR", "SWI", "SPI", "DIS", "GUI", "CT", "ACC", "VIS" };
    // HEALTH, STAMINA, ENERGY, STRENGTH, SWIFTNESS, SPIRIT, DISCIPLINE, GUILE, CHARGETIME, ACCURACY, VISIONRANGE

    public const float ABSOLUTE_MAX_HEALTH = 9999f;
    public const float ABSOLUTE_MAX_HEALTH_SAVAGE = 99999f;

    public Dictionary<string, int> statusesQueuedForRemoval;

    public bool anyStatusesConsume;

    public List<string> scriptsToTry = new List<string>();
    public bool statusDirty;    

    public static readonly List<StatTypes> CORE_NON_RESOURCE_STATS = new List<StatTypes>()
    {
        StatTypes.STRENGTH,
        StatTypes.SWIFTNESS,
        StatTypes.GUILE,
        StatTypes.DISCIPLINE,
        StatTypes.SPIRIT
    };
    Dictionary<string, int> inflictedStatusStrings = new Dictionary<string, int>();

    public void SetOwner(Fighter newOwner)
    {
        owner = newOwner;
        ownerSet = true;
    }

    public void ReadFromSave(XmlReader reader, bool readTrueMax, bool validateWithOwner = true, bool mysteryDungeonBlock = false) {

    	reader.ReadStartElement();

        bool debugRead = false;

        if (statuses != null)
        {
            statuses.Clear();
        }
        else
        {
            statuses = new List<StatusEffect>();
            dictStatusQuantities = new Dictionary<string, int>();
        }

		while(reader.NodeType != XmlNodeType.EndElement) {
			string strValue = reader.Name.ToLowerInvariant();

            if (debugRead) Debug.Log("Reading for " + owner.actorRefName + " " + owner.actorUniqueID + " " + reader.Name + " " + reader.NodeType);

            switch (strValue) {
                case "lv":
                case "level":
					SetLevel(reader.ReadElementContentAsInt(), validateWithOwner);
					break;
				case "currentxp":
					experiencePoints = reader.ReadElementContentAsInt();
					break;
				case "xptonextlevel":
					xpToNextLevel = reader.ReadElementContentAsInt();
					break;
                case "bl":
                case "block":
                    reader.ReadStartElement();

                    bool readCompactedStatBlock = false;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        bool foundStat = false;

                        if (reader.Name == "mst")
                        {
                            readCompactedStatBlock = true;
                            // compacted monster block
                            string unparsed = reader.ReadElementContentAsString();
                            char splitChar = ':';
                            if (GameStartData.loadGameVer <= 109)
                            {
                                splitChar = ',';
                            }
                            string[] parsed = unparsed.Split(splitChar);
                            // represents the 8 core stats
                            for (int i = 0; i < 6; i++)
                            {
                                string subUnParsed = parsed[i]; // the mini stat block with cur, and/or max, and/or truemax
                                string[] parsedStats = subUnParsed.Split('|');

                                int convertedStatIndex = i;
                                if (i > 0)
                                {
                                    convertedStatIndex += 2; // We're skipping stamina and energy, which means we're offset by 2
                                    // This is a bad use of enums I know
                                }
                                ReadParsedStats(parsedStats, convertedStatIndex, validateWithOwner);
                            }
                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                break;
                            }
                        }

                        for (int i = 0; i < statAbbreviationsNotLocalized.Length; i++)
                        {
                            if (reader.Name == statAbbreviationsNotLocalized[i])
                            {
                                foundStat = true;

                                string content = reader.ReadElementContentAsString();

                                string[] parsedStats = content.Split('|');
                                ReadParsedStats(parsedStats, i, validateWithOwner);

                                break;
                            }
                        }
                        if (!foundStat)
                        {
                            reader.Read();
                        }
                    }

                    reader.ReadEndElement();                    

                    break;
                // deprecated
                case "actorcorestats":
                case "core":
					reader.ReadStartElement();
                    // Do we want to actually read cur/max? Or will these be affected by DoEffect()?

                    // Read core block
                    string txt;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        //Debug.Log("Outer " + reader.Name + " " + reader.NodeType);                        
                        StatTypes stat = (StatTypes)Enum.Parse(typeof(StatTypes), reader.Name);
                        reader.ReadStartElement();
                        float cur = 0f;
                        float max = 0f;
                        float truemax = 0f;
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            //Debug.Log("Inner " + reader.Name + " " + reader.NodeType);
                            switch (reader.Name)
                            {
                                case "CUR":
                                    txt = reader.ReadElementContentAsString();
                                    cur = CustomAlgorithms.TryParseFloat(txt);
                                    break;
                                case "MAX":
                                    txt = reader.ReadElementContentAsString();
                                    max = CustomAlgorithms.TryParseFloat(txt);
                                    truemax = max;
                                    break;
                                case "TRUEMAX":
                                    txt = reader.ReadElementContentAsString();
                                    truemax = CustomAlgorithms.TryParseFloat(txt);
                                    break;
                                default:
                                    reader.Read();
                                    break;
                            }
                        }
                        // End stat
                        reader.ReadEndElement();
                        SetStat(stat, cur, StatDataTypes.CUR, false);
                        SetStat(stat, max, StatDataTypes.MAX, false);
                        SetStat(stat, truemax, StatDataTypes.TRUEMAX, false);
                    }

                    reader.ReadEndElement();
                    // End core block

					break;
                case "se":
                case "statuseffect":
					StatusEffect se = new StatusEffect();
                    reader.ReadStartElement();

                    if (!validateWithOwner)
                    {
                        // Must be a floating stat block that WAS attached to hero, so we'll assume hero is the owner here
                        se.ReadFromSave(reader, GameMasterScript.heroPCActor); // Ends by reading its own end element
                    }
                    else
                    {
                        se.ReadFromSave(reader, owner); // Ends by reading its own end element.
                    }					

                    bool ownerIsHero = owner != null && owner.GetActorType() == ActorTypes.HERO;
                    AddStatusObjectToList(se, ownerIsHero);

                    break;
				default:
					reader.Read();
					break;
			}
		}    

		reader.ReadEndElement();

    }

    public float CheckParalyzeChance()
    {
        float chance = 0f;
        StatusEffect se;
        for (int i = 0; i < statuses.Count; i++)
        {
            se = statuses[i];
            if (se.CheckRunTriggerOn(StatusTrigger.TURNSTART))
            {
                foreach(EffectScript eff in se.listEffectScripts)
                {
                    if (eff.effectType == EffectType.INFLUENCETURN)
                    {
                        InfluenceTurnEffect ite = eff as InfluenceTurnEffect;
                        chance += ite.paralyzeChance;
                    }
                }
            }
        }
        return chance;
    }

    public float CheckRootChance()
    {
        float chance = 0f;
        StatusEffect se;
        int count = statuses.Count;
        for (int i = 0; i < count; i++)
        {
            se = statuses[i];
            if (se.CheckRunTriggerOn(StatusTrigger.TURNSTART))
            {
                foreach (EffectScript eff in se.listEffectScripts)
                {
                    if (eff.effectType == EffectType.INFLUENCETURN)
                    {
                        InfluenceTurnEffect ite = eff as InfluenceTurnEffect;
                        chance += ite.rootChance;
                    }
                }
            }
        }
        return chance;
    }


    public void WriteToSave(XmlWriter writer, bool writeTrueMax, bool mysteryDungeonBlock = false)
    {
    	writer.WriteStartElement("sts"); // was "stats"

    	writer.WriteElementString("lv",actorLevel.ToString()); // was "level"

        if (owner == GameMasterScript.heroPCActor || mysteryDungeonBlock || 
            (owner.GetActorType() == ActorTypes.MONSTER && owner.actorfaction == Faction.PLAYER && experiencePoints > 0))
        {
            writer.WriteElementString("currentxp", experiencePoints.ToString());
            writer.WriteElementString("xptonextlevel", xpToNextLevel.ToString());
        }

        bool anyStatsWritten = false;
        Monster mn;

        // Alternate, even more compact method of writing monster stats.
        bool compactMonsterWrite = true;
        if (!mysteryDungeonBlock && owner.GetActorType() == ActorTypes.MONSTER)
        {
            mn = owner as Monster;
            if (GetStat(StatTypes.HEALTH, StatDataTypes.MAX) == mn.myTemplate.hp || GetStat(StatTypes.STRENGTH, StatDataTypes.MAX) == mn.myTemplate.strength ||
                GetStat(StatTypes.SPIRIT, StatDataTypes.MAX) == mn.myTemplate.spirit || GetStat(StatTypes.DISCIPLINE, StatDataTypes.MAX) == mn.myTemplate.discipline
                || GetStat(StatTypes.SWIFTNESS, StatDataTypes.MAX) == mn.myTemplate.swiftness || GetStat(StatTypes.GUILE, StatDataTypes.MAX) == mn.myTemplate.guile)
            {
                compactMonsterWrite = false;
            }
        }
        else
        {
            // Not a monster, so can't compact the block
            compactMonsterWrite = false;
        }

        string monsterBuilder = "";
        // If we ARE compacting the block, rather than writing 6 lines of stats like HL, STR, DIS, we write a single line separated by commas
        // This reduces the number of nodes the reader has to traverse, sometimes significantly

        for (int i = 0; i < (int)StatTypes.COUNT-4; i++) {

            bool writeStat = false;
            if (!mysteryDungeonBlock && owner.GetActorType() == ActorTypes.MONSTER)
            {
                mn = owner as Monster;
                switch((StatTypes)i)
                {
                    case StatTypes.HEALTH:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.hp)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.STRENGTH:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.strength)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.DISCIPLINE:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.discipline)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.SPIRIT:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.spirit)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.SWIFTNESS:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.swiftness)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.GUILE:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.guile)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.ACCURACY:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.accuracy)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.VISIONRANGE:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.visionRange)
                        {
                            writeStat = true;
                        }
                        break;
                    case StatTypes.CHARGETIME:
                        if (GetStat((StatTypes)i, StatDataTypes.MAX) != mn.myTemplate.chargetime)
                        {
                            writeStat = true;
                        }
                        break;
                }
            }

            if ((GetCurStatAsPercentOfMax((StatTypes)i) == 1.0f || GetStat((StatTypes)i,StatDataTypes.MAX) == 0) 
                && owner != GameMasterScript.heroPCActor && !writeStat && !mysteryDungeonBlock)
            {
                continue;
            }

            if (!anyStatsWritten)
            {
                writer.WriteStartElement("bl"); // was "block"
                anyStatsWritten = true;
            }

            if (!writeTrueMax)
            {
                if (GetStat((StatTypes)i, StatDataTypes.CUR) == GetStat((StatTypes)i, StatDataTypes.MAX))
                {
                    string valueToWrite = GetCurStat((StatTypes)i, mysteryDungeonBlock).ToString();
                    if (compactMonsterWrite && i < (int)StatTypes.CHARGETIME && i != (int)StatTypes.ENERGY && i != (int)StatTypes.STAMINA)
                    {
                        monsterBuilder += valueToWrite + ":";
                    }
                    else
                    {
                        writer.WriteElementString(statAbbreviationsNotLocalized[i], valueToWrite);
                    }
                    
                }
                else
                {
                    string valueToWrite = GetCurStat((StatTypes)i, mysteryDungeonBlock) + "|" + GetMaxStat((StatTypes)i);
                    if (compactMonsterWrite && i < (int)StatTypes.CHARGETIME && i != (int)StatTypes.ENERGY && i != (int)StatTypes.STAMINA)
                    {
                        monsterBuilder += valueToWrite + ":";
                    }
                    else
                    {
                        writer.WriteElementString(statAbbreviationsNotLocalized[i], valueToWrite);
                    }
                        
                }
            }        
            else
            {
                string valueToWrite = GetCurStat((StatTypes)i, mysteryDungeonBlock) + "|" + GetMaxStat((StatTypes)i) + "|" + GetStat((StatTypes)i, StatDataTypes.TRUEMAX);
                if (compactMonsterWrite && i < (int)StatTypes.CHARGETIME && i != (int)StatTypes.ENERGY && i != (int)StatTypes.STAMINA)
                {
                    monsterBuilder += valueToWrite + ":"; // used to be a comma
                }
                else
                {
                    writer.WriteElementString(statAbbreviationsNotLocalized[i], valueToWrite);
                }
            } 
    	}

        if (compactMonsterWrite)
        {
            monsterBuilder = monsterBuilder.Substring(0, monsterBuilder.Length - 1);
            writer.WriteElementString("mst", monsterBuilder);
        }

        if (anyStatsWritten)
        {
            writer.WriteEndElement();
        }    	


    	// Write all statuses.
    	for (int i = 0; i < statuses.Count; i++)
        {
            if (mysteryDungeonBlock)
            {
                statuses[i].WriteToSave(writer, GameMasterScript.heroPCActor.actorUniqueID);
            }
            else
            {
                statuses[i].WriteToSave(writer, owner.actorUniqueID);
            }
    		
    	}

    	// End stat block
    	writer.WriteEndElement();
    }

    public bool CheckIfSealed()
    {
        //foreach(StatusEffect se in statuses)
        for (int i = 0; i < statuses.Count; i++)  
        {
            if (statuses[i].isPositive) continue;
            if (statuses[i].refName.Contains("status_sealed"))
            {
                return true;
            }
        }
        return false;
    }
    public int CountStatusesRemovedSinceLastTurn()
    {
        if (owner != GameMasterScript.heroPCActor)
        {
            return statusesRemovedSinceLastTurn.Count;
        }
        int count = 0;
        for (int i = 0; i < statusesRemovedSinceLastTurn.Count; i++)
        {
            StatusEffect se = statusesRemovedSinceLastTurn[i];
            if (se.listEffectScripts.Count == 0)
            {
                count++;
            }
            else
            {
                bool countThisStatus = true;
                for (int x = 0; x < se.listEffectScripts.Count; x++)
                {
                    EffectScript eff = se.listEffectScripts[x];
                    if (eff.originatingActor != null && eff.originatingActor.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = eff.originatingActor as Monster;
                        if (mn.GetXPModToPlayer() == 0 && GameMasterScript.gmsSingleton.gameMode != GameModes.ADVENTURE)
                        {
                            countThisStatus = false;
                        }
                    }
                }
                if (countThisStatus)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public string GetDisplayStatuses(bool keenEyes)
    {
        //Dictionary<string, int> inflictedStatusStrings = new Dictionary<string, int>();
        inflictedStatusStrings.Clear();

          string construct = "";
        for(int x = 0; x < statuses.Count; x++)
        {
            if (statuses[x].CheckDurTriggerOn(StatusTrigger.PERMANENT))
            {
                continue;
            }
            if (!statuses[x].showIcon)
            {
                continue;
            }
            if (string.IsNullOrEmpty(statuses[x].abilityName)) continue; // #todo for localization Need a better way to do this.

            if (keenEyes)
            {
                string build = statuses[x].abilityName + " (" + statuses[x].curDuration + "t)";
                //construct = statuses[x].abilityName + " (" + statuses[x].curDuration + "t)";
                if (inflictedStatusStrings.ContainsKey(build))
                {
                    inflictedStatusStrings[build]++;
                }
                else
                {
                    inflictedStatusStrings.Add(build, 1);
                }
            }
            else
            {
                //construct = statuses[x].abilityName;
                if (inflictedStatusStrings.ContainsKey(statuses[x].abilityName))
                {
                    inflictedStatusStrings[statuses[x].abilityName]++;
                }
                else
                {
                    inflictedStatusStrings.Add(statuses[x].abilityName, 1);
                }
            }                            
        }

        bool first = true;
        foreach(string status in inflictedStatusStrings.Keys)
        {
            if (inflictedStatusStrings[status] == 1)
            {
                if (first)
                {
                    construct += status;
                    first = false;
                }
                else
                {
                    construct += ", " + status;
                }
            }
            else
            {
                if (first)
                {
                    construct += status + " x" + inflictedStatusStrings[status];
                    first = false;
                }
                else
                {
                    construct += ", " + status + " x" + inflictedStatusStrings[status];
                }
            }
        }

        return construct;
    }

    static StatBlock()
    {
        statNames = new string[(int)StatTypes.COUNT - 4];
        for (int t = 0; t < statNames.Length; t++)
        {
            statNames[t] = GetCoreStatString((StatTypes) t);
        }

        /*
        statNames[(int)StatTypes.HEALTH] =              "Health";
        statNames[(int)StatTypes.STAMINA] =             "Stamina";
        statNames[(int)StatTypes.ENERGY] =              "Energy";
        statNames[(int)StatTypes.STRENGTH] =            "Strength";
        statNames[(int)StatTypes.SWIFTNESS] =           "Swiftness";
        statNames[(int)StatTypes.SPIRIT] =              "Spirit";
        statNames[(int)StatTypes.DISCIPLINE] =          "Discipline";
        statNames[(int)StatTypes.GUILE] =               "Guile";
        statNames[(int)StatTypes.CHARGETIME] =          "CT";
        statNames[(int)StatTypes.ACCURACY] =            "Accuracy";
        statNames[(int)StatTypes.VISIONRANGE] =         "Vision Range";
        */

        // Hard coded

        // Original order...
        /* coreStats = new StatTypes[8];
        coreStats[0] = StatTypes.STRENGTH;
        coreStats[1] = StatTypes.SWIFTNESS;
        coreStats[2] = StatTypes.SPIRIT;
        coreStats[3] = StatTypes.DISCIPLINE;
        coreStats[4] = StatTypes.GUILE;
        coreStats[5] = StatTypes.HEALTH;
        coreStats[6] = StatTypes.STAMINA;
        coreStats[7] = StatTypes.ENERGY; */

        expandedCoreStats = new StatTypes[11];
        expandedCoreStats[0] = StatTypes.HEALTH;
        expandedCoreStats[1] = StatTypes.STAMINA;
        expandedCoreStats[2] = StatTypes.ENERGY;
        expandedCoreStats[3] = StatTypes.STRENGTH;
        expandedCoreStats[4] = StatTypes.SWIFTNESS;
        expandedCoreStats[5] = StatTypes.SPIRIT;
        expandedCoreStats[6] = StatTypes.DISCIPLINE;
        expandedCoreStats[7] = StatTypes.GUILE;
        expandedCoreStats[8] = StatTypes.CHARGETIME;
        expandedCoreStats[9] = StatTypes.ACCURACY;
        expandedCoreStats[10] = StatTypes.VISIONRANGE;

        coreStats = new StatTypes[8];
        coreStats[0] = StatTypes.HEALTH;
        coreStats[1] = StatTypes.STAMINA;
        coreStats[2] = StatTypes.ENERGY;
        coreStats[3] = StatTypes.STRENGTH;
        coreStats[4] = StatTypes.SWIFTNESS;
        coreStats[5] = StatTypes.SPIRIT;
        coreStats[6] = StatTypes.DISCIPLINE;
        coreStats[7] = StatTypes.GUILE;

        onlyResourceStats = new StatTypes[3];
        onlyResourceStats[0] = StatTypes.HEALTH;
        onlyResourceStats[1] = StatTypes.STAMINA;
        onlyResourceStats[2] = StatTypes.ENERGY;

        nonResourceStats = new StatTypes[5];
        nonResourceStats[0] = StatTypes.STRENGTH;
        nonResourceStats[1] = StatTypes.SWIFTNESS;
        nonResourceStats[2] = StatTypes.SPIRIT;
        nonResourceStats[3] = StatTypes.DISCIPLINE;
        nonResourceStats[4] = StatTypes.GUILE;
    }

    public StatBlock()
    {
        inflictedStatusStrings = new Dictionary<string, int>();
        if (activeSongs == null)
        {
            activeSongs = new List<StatusEffect>();
        }
        displayStatusNames = new List<string>();
    	statusToRemove = new List<StatusEffect>(5);
        if (queuedStatuses == null)
        {
            queuedStatuses = new List<StatusEffect>(5);
        }        
        if (overlaysToRemove == null)
        {
            overlaysToRemove = new List<OverlayData>();
        }

        if (scriptsToTry == null)
        {
            scriptsToTry = new List<string>();
        }
        //durStatusesEvaluated = new HashSet<StatusEffect>();
        runStatusesEvaluated = new HashSet<StatusEffect>[(int)StatusTrigger.COUNT];
        for (int i = 0; i < (int)StatusTrigger.COUNT; i++)
        {
            runStatusesEvaluated[i] = new HashSet<StatusEffect>();
        }
        //consumeStatusesEvaluated = new HashSet<StatusEffect>();

        statuses = new List<StatusEffect>();
        dictStatusQuantities = new Dictionary<string, int>();
        actorLevel = 0;
        experiencePoints = 0;
        xpToNextLevel = 123456789;

        statusDirty = true;

        statusesRemovedSinceLastTurn = new List<StatusEffect>();

        statArray = new SingleStat[(int)StatTypes.COUNT-4];
        for (int i = 0; i < (int)StatTypes.COUNT-4; i++)
        {
            statArray[i] = new SingleStat();
            statArray[i].statType = (StatTypes.HEALTH + i);
        }

        statusesQueuedForRemoval = new Dictionary<string, int>();
    }

    public void CheckRunAndTickAllStatuses(StatusTrigger trigger)
    {
        if (!IsAlive())
        {
            return;
        }
        if (!ownerSet)
        {
            return;
        }
        if (trigger == StatusTrigger.TURNSTART)
        {
            statusesRemovedSinceLastTurn.Clear();
        }
        GameLogScript.BeginTextBuffer();
        CheckRunAllStatuses(trigger);
        CheckTickAllStatus(trigger);
        CheckConsumeAllStatuses(trigger);
        GameLogScript.EndTextBufferAndWrite();
        if (!IsAlive() && GameMasterScript.actualGameStarted)
        {
            GameMasterScript.AddToDeadQueue(owner);
        }
    }

    public List<StatusEffect> GetAllStatuses()
    {
        return statuses;
    }

    public int CheckStatusQuantity(string seRef)
    {
        if (!dictStatusQuantities.ContainsKey(seRef)) return 0;
        int qty = 0;
        foreach (StatusEffect status in statuses)
        {
            if (status.refName == seRef)
            {
                if (status.quantity <= 1)
                {
                    qty++;
                }
                else
                {
                    qty += status.quantity;
                }
            }
        }
        return qty;
    }

    public bool CheckHasActiveStatusName(string seRef)
    {
        bool hasStatus = CheckHasStatusName(seRef);
        if (!hasStatus) return false;

        StatusEffect se = GetStatusByRef(seRef);
        if (owner.myStats.GetCurStat(StatTypes.STAMINA) < se.staminaReq) return false;
        if (owner.myStats.GetCurStat(StatTypes.ENERGY) < se.energyReq) return false;

        return true;
    }

    public bool CheckHasStatusName(string seRef)
    {
        int amt = 0;
        if (dictStatusQuantities.TryGetValue(seRef, out amt))
        {
            if (amt > 0) return true;
            else
            {
                return false;
            }
        }
        return false;                     
        
    }

    public void UpdateStatusDirections() {
        overlaysToRemove.Clear();

        for (int i = 0; i < statuses.Count; i++) {
    		if ((statuses[i].directionFollowActor) && (owner.lastMovedDirection != statuses[i].direction)) {
    			statuses[i].direction = owner.lastMovedDirection;
    			// Realign the sprite?
                if (owner.overlays == null)
                {
                    //Debug.Log(owner.displayName + " should have overlays, but does not.");
                    continue;
                }
    			for (int x = 0; x < owner.overlays.Count; x++) {
    				GameObject overlay = owner.overlays[x].overlayGO;
                    if (overlay == null)
                    {
                        overlaysToRemove.Add(owner.overlays[x]);
                        continue;     
                    }
                    if (!overlay.activeSelf) continue;
                    if (overlay.GetComponent<SpriteEffect>().parentAbility == statuses[i]) {
    					// Rotate this overlay.
    					CustomAlgorithms.RotateGameObject(overlay,owner.GetObject(),owner.lastMovedDirection);
    					continue;
    				}
    			}
    		}
    	}

        if (overlaysToRemove.Count > 0)
        {
            foreach(OverlayData od in overlaysToRemove)
            {
                owner.overlays.Remove(od);
                //Debug.Log("Clearing " + owner.actorRefName + " overlay.");
                if (od.overlayGO != null)
                {
                    GameMasterScript.ReturnToStack(od.overlayGO, od.overlayGO.name.Replace("(Clone)", String.Empty));
                }                
            }
        }
    }

    // Validate, cache stuff info about our statuses for efficiency
    void AddStatusObjectToList(StatusEffect se, bool ownerIsHero)
    {

        statuses.Add(se);
        if (ownerIsHero && se.statusFlags[(int)StatusFlags.THANESONG])
        {
            activeSongs.Add(se);
        }
        if (dictStatusQuantities.ContainsKey(se.refName))
        {
            dictStatusQuantities[se.refName]++;
        }
        else
        {
            dictStatusQuantities.Add(se.refName, 1);
        }
        
        if (!anyStatusesConsume)
        {
            anyStatusesConsume = se.AnyConsumeTriggers();
        }
    }

    // Same cache/validation check as above.
    void RemoveStatusObjectFromList(StatusEffect se)
    {

        statuses.Remove(se);
        dictStatusQuantities[se.refName]--;
        
        if (anyStatusesConsume && se.AnyConsumeTriggers())
        {
            anyStatusesConsume = false;
            foreach(StatusEffect checkSE in statuses)
            {
                if (checkSE.AnyConsumeTriggers())
                {
                    anyStatusesConsume = true;
                    return;
                }
            }
        }
        
    }

    /* public void QueueStatus(StatusEffect se) {
    	if (!queuedStatuses.Contains(se)) {
			queuedStatuses.Add(se);
    	}
    }

    public void ProcessQueuedStatuses() {
    	foreach(StatusEffect se in queuedStatuses) {
            //AddStatus(se);
            statuses.Add(se);
            if ((se.uniqueID == 3217) || (se.uniqueID == 3538))
            {
                Debug.Log(se.refName);
            }
            //foreach(EffectScript eff in se.effects)
            {
                eff.originatingActor = GameMasterScript.dictAllActors[eff.originatingActorUniqueID];
            //}
        }
    	queuedStatuses.Clear();
        foreach(StatusEffect se in statuses)
        {
            Debug.Log(se.refName);
        }
    } */

    public StatusEffect AddStatusByRefAndLog(string refName, Actor source, int duration)
    {
        bool alreadyHaveStatus = CheckHasStatusName(refName);

        // status immunity should apply even if statuses are added outside AddStatusEffect
        if (owner.CheckForStatusImmunity(GameMasterScript.masterStatusList[refName]))
        {
            return null;
        }

        StatusEffect se = AddStatusByRef(refName, source, duration);
        if (se != null && !alreadyHaveStatus)
        {
            StringManager.SetTag(0, owner.displayName);
            StringManager.SetTag(1, se.abilityName);
            GameLogScript.LogWriteStringRef("log_gainstatus_single_withtag");
        }
        return se;
    }

    public StatusEffect AddStatusByRef(string refName, Actor source, int duration, bool checkForImmunity = true)
    {
        StatusEffect template = GameMasterScript.FindStatusTemplateByName(refName);
        if (template == null)
        {
            Debug.Log("WARNING! Could not find status " + refName + " for " + owner.actorRefName + " source: " + source.actorRefName);
            return null;
        }

        // status immunity should apply even if statuses are added outside AddStatusEffect
        if (checkForImmunity && owner.CheckForStatusImmunity(GameMasterScript.masterStatusList[refName]))
        {
            if (Debug.isDebugBuild) Debug.Log("Immune to " + refName + ", not adding!");
            return null;
        }

        if (CheckStatusQuantity(refName) >= template.maxStacks)
        {
            if (Debug.isDebugBuild) Debug.Log(refName + " max stacks of " + template.maxStacks + ", not adding!");
            return null;
        }
        StatusEffect nStatus = new StatusEffect();
        nStatus.CopyStatusFromTemplate(template);
        nStatus.curDuration = duration;
        nStatus.maxDuration = duration;
        StatusEffect added = AddStatus(nStatus, source);
        return added;
    }

    // This is used by items that give specific effects.
    public StatusEffect AddStatus(StatusEffect se, Actor source, Actor granterOfStatus)
    {
        statusDirty = true;

        // Run some logic in case it's the same root status, but for now, just add it again.
        // TODO: When adding a new status, actually instantiate a new copy entirely?
        bool contains = false;

        bool validOwner = owner != null;

        //Shep #todo check status effect for stun condition instead of hardcoded refname
        if (validOwner)
        {
            if (se.refName == "status_basicstun" || se.refName == "status_tempstun")
            {
                if (owner.turnsSinceLastStun >= 0 && owner.turnsSinceLastStun <= 3)
                {
                    return null;
                }
                owner.turnsSinceLastStun = 0;
            }
            // Hero cannot be charmed
            if (owner.GetActorType() == ActorTypes.HERO && se.refName == "status_charmed")
            {
                return null;
            }
        }


        StatusEffect existing = null;

        //Shep #todo check status effect for spirit granting data rather than hardcoded refname
        if (se.refName == "spiritcollected")
        {
            int maxSpirits = 5;
            if (CheckHasStatusName("spiritmaster"))
            {
                maxSpirits = 8;
            }
            if (CheckStatusQuantity("spiritcollected") >= maxSpirits)
            {
                return null;
            }
        }

        if (se.destroyStatusOnAdd.Count > 0)
        {
            foreach (string seRef in se.destroyStatusOnAdd)
            {
                StatusEffect tryGetRef = owner.myStats.GetStatusByRef(seRef);
                if (tryGetRef != null)
                {
                    RemoveStatus(tryGetRef, true);
                }
            }
        }

        bool ranOnAddStatusScript = false;
        bool refreshedOnce = false;

        // in NG++, scale duration of status effects inflicted by player to enemy monsters by half
        if (GameStartData.NewGamePlus >= 2 && validOwner && owner.actorfaction == Faction.ENEMY && !se.isPositive && se.maxDuration > 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
        {
            bool reduceDuration = true;
            foreach(EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.DAMAGE)
                {
                    reduceDuration = false;
                    break;
                }
            }
            if (reduceDuration)
            {                
                int adjustedDur = (int)Mathf.Round(se.maxDuration / 2f);
                if (adjustedDur < 1) adjustedDur = 1;
                se.maxDuration = adjustedDur;
                se.curDuration = adjustedDur;
            }
        }

        //If we already have the status effect on us
        foreach (StatusEffect status in statuses)
        {
            if (status.refName == se.refName)
            {
                contains = true;
                existing = status;
                //refresh the stack if we're flagged to do so.
                if (se.refreshDurationOnCast)
                {
                    status.RefreshDuration();
                    TryRunOnAddStatusScript(se);
                    ranOnAddStatusScript = true;
                    refreshedOnce = true;
                    //Debug.Log("Refresh duration on cast.");
    
                }                
            }
        }



        if (!se.stackMultipleEffects && se.refreshDurationOnCast && refreshedOnce)
        {
            // We've already refreshed all instances of the status.
            return null;
        }

        StatusEffect effToprocess = se;
        if ( contains && 
            (se.stackMultipleDurations || !se.stackMultipleEffects))
        {
            existing.quantity++;
            effToprocess = existing;
        }
        else //if (se.stackMultipleEffects)
        {
            // This would be all other scenarios I guess.
            //Debug.Log("Adding a NEW status instance.");
            se.quantity = 1;
            bool ownerIsHero = false;
            if (validOwner && owner.GetActorType() == ActorTypes.HERO)
            {
                ownerIsHero = true;                
            }
            AddStatusObjectToList(se, ownerIsHero);
        }

        if (effToprocess.combatLogText != null && effToprocess.combatLogText != "")
        {
            GameLogScript.GameLogWrite("<color=yellow>" + effToprocess.combatLogText + "</color>", owner);
        }

        if (!ranOnAddStatusScript)
        {
            TryRunOnAddStatusScript(se);
        }

        // Don't make teleports take longer tho
        if (effToprocess.isPositive && !effToprocess.noDurationExtensionFromStats)
        {
            // Enhance duration via spirit.
            // Buff duration
            float modifier = owner.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * 0.33f;
            float durAdd = modifier * effToprocess.maxDuration;
            int finalDurAdd = (int)durAdd;
            effToprocess.maxDuration += finalDurAdd;
            effToprocess.curDuration += finalDurAdd;
        }
        if (!contains || effToprocess.spriteAlwaysVisible)
        {
            if (effToprocess.ingameSprite != null && validOwner)
            {
                // There is some sort of overlay sprite to apply.
                // Which direction should it point?
                if (owner.GetObject() != null)
                {
                    effToprocess.AddSpawnedOverlayRef(owner, effToprocess.direction);
                }
            }
        }

        foreach (EffectScript eff in effToprocess.listEffectScripts)
        {
            eff.selfActor = owner;
            eff.originatingActor = source;
            eff.parentAbility = effToprocess;
            if (effToprocess.stackMultipleEffects)
            {
                eff.ResetAccumulatedAmounts(); // WARNING: HACK!
            }
        }
		if (validOwner) 
		{
	        if (effToprocess.refName == "invisible")
	        {
	            owner.SetOpacity(0.15f);
	        }
	        else if (effToprocess.refName == "spiritwalk")
	        {
	            owner.SetOpacity(0.5f);
	        }

	        if (owner.GetActorType() == ActorTypes.HERO && se.refName == "wrathcharge")
	        {
	            int numWraths = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("wrathcharge");
	            GameMasterScript.heroPCActor.wrathBarScript.UpdateWrathCount(numWraths);
	        }		
		}


        // Persistent duration statuses: Things like passive abilities (sneak attack) which can be toggled on and off
        // When the player toggles OFF the status, we don't want it to reset duration when toggled back ON.
        if (effToprocess.persistentDuration && validOwner)
        {
            int previousDur = owner.ReadActorData("statusdur_" + effToprocess.refName);
            if (previousDur >= 1)
            {
                effToprocess.curDuration = previousDur;
            }
            else
            {
                owner.SetActorData("statusdur_" + effToprocess.refName, (int)effToprocess.curDuration);
            }
        }

        CheckRunStatus(effToprocess, StatusTrigger.ONADD);

        // think about this conditional more...
        /* if (!(!se.stackMultipleEffects && se.stackMultipleDurations && !contains))
        {
            CheckRunStatus(effToprocess, StatusTrigger.ONADD);
        }
        else
        {
            Debug.Log("We already have this status " + se.refName + ", don't add it again.");
        } */

        if (granterOfStatus != null && GameMasterScript.gameLoadSequenceCompleted)
        {
            //Debug.Log("Granter of status is: " + granterOfStatus.actorUniqueID);
            se.addedByActorID = granterOfStatus.actorUniqueID;
            if (granterOfStatus.GetActorType() == ActorTypes.ITEM)
            {
                Item grantItem = granterOfStatus as Item;
                if (grantItem.IsEquipment())
                {
                    se.sourceOfEffectIsEquippedGear = true;
                }
            }
        }

        se.active = true;

        se.RunCommandsOnAdd(owner);

        return se;
    }

    void TryRunOnAddStatusScript(StatusEffect se)
    {
        if (!string.IsNullOrEmpty(se.script_runOnAddStatus))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(OnRunAddStatusFunctions), se.script_runOnAddStatus);
            object[] paramList = new object[2];
            paramList[0] = owner;
            paramList[1] = se;
            object returnObj = runscript.Invoke(null, paramList);
        }
    }

    void TryRunOnRemoveStatusScript(StatusEffect se)
    {
        if (!string.IsNullOrEmpty(se.script_runOnRemoveStatus))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(OnRunAddStatusFunctions), se.script_runOnRemoveStatus);
            object[] paramList = new object[2];
            paramList[0] = owner;
            paramList[1] = se;
            object returnObj = runscript.Invoke(null, paramList);
        }
    }

    public StatusEffect AddStatus(StatusEffect se, Actor source)
    {
        return AddStatus(se, source, null);
    }

    public void CheckReverseStatus(StatusEffect se)
    {
        foreach(EffectScript eff in se.listEffectScripts)
        {
            if (eff.effectType == EffectType.CHANGESTAT)
            {
                ChangeStatEffect cs = (ChangeStatEffect)eff as ChangeStatEffect;
                if (cs.reverseOnEnd)
                {
                    cs.ReverseEffect();
                    owner.SetBattleDataDirty();
                }
            }
            if (eff.effectType == EffectType.ALTERBATTLEDATA)
            {
                eff.ReverseEffect();
            }
        }
    }

    public void CheckRunStatus(StatusEffect se, StatusTrigger trigger)
    {
        if (statuses.Contains(se))
        {
            if (se.CheckRunTriggerOn(trigger))
            {
                if (trigger == StatusTrigger.ONREMOVE && !se.temp_RemovalInProgress)
                {
                    se.temp_RemovalInProgress = true;
                }
                else if (se.temp_RemovalInProgress)
                {
                    return;
                }
                if (se.CheckAbilityTag(AbilityTags.REQUIREMELEE) && owner.IsHero() && owner.myEquipment.IsWeaponRanged(owner.myEquipment.GetWeapon())) return;
                if (se.reqWeaponType != WeaponTypes.ANY && owner.IsHero())
                {
                    if (se.reqWeaponType != owner.myEquipment.GetWeaponType())
                    {
                        //Debug.Log("Not running " + se.refName);
                        return;
                    }
                }
                if (se.spiritsRequired > owner.myStats.CheckStatusQuantity("spiritcollected"))
                {
                    return;
                }
                se.RunStatus(trigger, owner);
                owner.SetBattleDataDirty();
            }
        }
        if (!IsAlive() && GameMasterScript.actualGameStarted)
        {
            GameMasterScript.AddToDeadQueue(owner);
        }
    }

    public void CheckRunAllStatuses(StatusTrigger trigger)
    {
        if (!ownerSet)
        {
            //Debug.Log("Warning! Something is trying to run status trigger " + trigger + " with no owner.");
            return;
        }

        // We use this convoluted loop because we need to do the following:
        // 1. Evaluate all statuses we currently have
        // 2. Evaluate any NEW statuses added *during* the loop
        // 3. NOT evaluate any statuses REMOVED during the loop

        runStatusesEvaluated[(int)trigger].Clear();

        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && owner.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = owner as Monster;
            if (mn.isInCorral) return;
        }

        for (int i = 0; i < statuses.Count;)
        {
            StatusEffect se = statuses[i];

            if (!runStatusesEvaluated[(int)trigger].Contains(se))
            {
                bool canRunStatus = EvaluateRunStatus(se, trigger);
                if (canRunStatus)
                {
                    se.RunStatus(trigger, owner);
                    if (i >= statuses.Count || statuses[i] != se)
                    {
                        i = 0; // Collection changed, let's start over, skipping anything we already evaluated this time.
                    }
                    else
                    {
                        i++; // Collection didn't change, so continue on.
                    }
                }
                else
                {
                    i++; // We couldn't run this status, so continue on.
                }
            }
            else
            {
                i++; // We already evaluated this status, so continue on.
            }
            runStatusesEvaluated[(int)trigger].Add(se);
        }
    }

    bool EvaluateRunStatus(StatusEffect se, StatusTrigger trigger)
    {
        if (se == null)
        {
            Debug.Log(owner.actorRefName + " " + owner.dungeonFloor + " alive? " + owner.myStats.IsAlive() + " has a null status.");
            return false;
        }
        if (se.CheckRunTriggerOn(trigger))
        {

            if (owner != null)
            {
                if (owner.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = owner as Monster;
                    if (se.isPositive && mn.surpressTraits && mn.actorRefName != "mon_runiccrystal")
                    {
                        return false;
                    }
                    if (se.isPositive && !se.active)
                    {
                        return false;
                    }
                }


                if (se.CheckDurTriggerOn(StatusTrigger.DUNGEONONLY) && owner.GetActorMap().IsTownMap())
                {
                    return false;
                }

                if (se.spiritsRequired > GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("spiritcollected"))
                {
                    return false;
                }

                if (se.energyReq > 0 && owner.myStats.GetCurStat(StatTypes.ENERGY) < se.energyReq)
                {
                    return false;
                }
                if (se.staminaReq > 0 && owner.myStats.GetCurStat(StatTypes.STAMINA) < se.staminaReq)
                {
                    return false;
                }
                //Debug.Log(statuses[i].reqWeaponType + " " + statuses[i].refName);
                if (se.CheckAbilityTag(AbilityTags.REQUIREMELEE) && owner.IsHero() && owner.myEquipment.IsWeaponRanged(owner.myEquipment.GetWeapon())) return false;
                if (se.CheckAbilityTag(AbilityTags.REQUIRERANGED) && owner.IsHero() && !owner.myEquipment.IsWeaponRanged(owner.myEquipment.GetWeapon())) return false;
                if (se.reqWeaponType != WeaponTypes.ANY && owner.IsHero())
                {
                    if (se.reqWeaponType != owner.myEquipment.GetWeaponType())
                    {
                        //Debug.Log("Not running " + statuses[i].refName);
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    public void CheckTickAllStatus(StatusTrigger trigger)
    {
        statusToRemove.Clear();

        /* if (trigger == StatusTrigger.TURNEND && owner.GetActorType() == ActorTypes.HERO)
        {
            Debug.Log("Test this now.");
        } */

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffect status = statuses[i];
            bool expired = false;

            if (status.CheckDurTriggerOn(StatusTrigger.DUNGEONONLY) && owner.GetActorMap().IsTownMap())
            {
                continue;
            }

            if (trigger == StatusTrigger.TURNEND)
            {
                //if (owner != GameMasterScript.heroPCActor) Debug.Log("Check " + status.refName + " " + status.curCooldownTurns + " " + status.maxCooldownTurns + " " + owner.displayName);

                if (status.energyTick != 0)
                {
                    ChangeStat(StatTypes.ENERGY, status.energyTick, StatDataTypes.CUR, true);
                    if (GetCurStat(StatTypes.ENERGY) < status.energyTick)
                    {
                        expired = true;
                    }
                }
                if (status.staminaTick != 0)
                {
                    ChangeStat(StatTypes.STAMINA, status.staminaTick, StatDataTypes.CUR, true);
                    if (GetCurStat(StatTypes.STAMINA) < status.staminaTick)
                    {
                        expired = true;
                    }
                }
            }

            bool forceExpire = false;

            if (status.CheckDurTriggerOn(trigger))
            {
                expired = status.TickStatus();
            }

            if (status.curDuration <= 0) // Duration of 0 should always be removed.
            {
                expired = true;
            }
            else if (status.persistentDuration)
            {
                owner.SetActorData("statusdur_" + status.refName, (int)status.curDuration);
            }

            if (expired && (!status.durStatusTriggers[(int)StatusTrigger.PERMANENT] || forceExpire)) // Permanent statuses should not get removed via ticking under any circumstances.
            {
                statusToRemove.Add(status);
            }
        }

        if (statusToRemove.Count > 0)
        {
            List<StatusEffect> localRemove = new List<StatusEffect>();
            foreach (StatusEffect str in statusToRemove)
            {
                localRemove.Add(str);
            }
            foreach (StatusEffect str in localRemove)
            {
                RemoveStatus(str, false);
            }
        }
    }

    public void CheckConsumeAllStatuses(StatusTrigger trigger)
    {
        if (!anyStatusesConsume) return;
        statusToRemove.Clear();

        int count = statuses.Count;
        for (int i = 0; i < count; i++)
        {
            StatusEffect status = statuses[i];
            if (status.CheckConsumeTriggerOn(trigger))
            {
                if (trigger == StatusTrigger.ONCRIT && status.refName == "sneakattack" && !CombatManagerScript.IsCombatMelee())
                {
                    continue;
                }
                if (status.CheckAbilityTag(AbilityTags.REQUIREMELEE) && owner.IsHero() && owner.myEquipment.IsWeaponRanged(owner.myEquipment.GetWeapon())) continue;
                if (statuses[i].CheckAbilityTag(AbilityTags.REQUIRERANGED) && owner.IsHero() && !owner.myEquipment.IsWeaponRanged(owner.myEquipment.GetWeapon())) continue;
                //Debug.Log("Consume " + status.refName + " " + status.CheckAbilityTag(AbilityTags.REQUIREMELEE));
                statusToRemove.Add(status);
                //Debug.Log(status.curDuration + " turns left on " + status.refName + " trigger " + trigger);
            }
        }

        if (statusToRemove.Count > 0)
        {
            List<StatusEffect> localRemove = new List<StatusEffect>();
            foreach(StatusEffect str in statusToRemove)
            {
                localRemove.Add(str);
            }
            foreach (StatusEffect str in localRemove)
            {
                RemoveStatus(str, false);
            }
        }
    }

    public bool RemoveAllStatusByRef(string statusRef)
    {
        List<StatusEffect> remove = new List<StatusEffect>();
        foreach (StatusEffect se in statuses)
        {
            if (se.refName == statusRef)
            {
                remove.Add(se);
            }
        }
        if (remove.Count > 0)
        {
            foreach(StatusEffect se in remove)
            {
                RemoveStatus(se, true);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns TRUE if anything was removed
    /// </summary>
    public bool ReallyForciblyRemoveStatus(string statusRef)
    {
        StatusEffect seToRemove = null;
        foreach(StatusEffect se in statuses)
        {
            if (se.refName == statusRef)
            {
                seToRemove = se;
                break;
            }
        }
        if (seToRemove != null)
        {
            if (seToRemove.quantity > 1) seToRemove.quantity -= 1;
            else RemoveStatusObjectFromList(seToRemove);
            return true;
        } 

        return false;
    }

    public void ReallyForciblyRemoveAllStatus(string statusRef)
    {
        bool foundAnythingYet = true;
        while (foundAnythingYet)
        {
            foundAnythingYet = ReallyForciblyRemoveStatus(statusRef);
        }            
    }

    public void ForciblyRemoveStatus(string statusRef)
    {
        StatusEffect seToRemove = null;
        foreach(StatusEffect se in statuses)
        {
            if (se.refName == statusRef)
            {
                seToRemove = se;
                break;
            }
        }

        if (seToRemove != null)
        {
            seToRemove.durStatusTriggers[(int)StatusTrigger.PERMANENT] = false;
            seToRemove.noRemovalOrImmunity = false;
            RemoveStatus(seToRemove, true);
        }
    }

    public bool RemoveStatusByRefAndSource(string statusRef, int sourceID, Actor source = null)
    {
        StatusEffect remove = null;
        StatusEffect possibleRemove = null;
        foreach (StatusEffect se in statuses)
        {
            if (se.refName == statusRef)
            {
                possibleRemove = se;
                //Debug.Log(se.refName + " matches ref " + statusRef + " but does it match " + sourceID + "? " + se.addedByActorID);
                if (se.addedByActorID == sourceID)
                {
                    remove = se;
                    break;
                }
            }
        }

        if (remove == null && possibleRemove != null)
        {
            //if (Debug.isDebugBuild) Debug.Log("For " + statusRef + ", couldn't find actor source ID " + sourceID + " so removing " + possibleRemove.refName + " " + possibleRemove.addedByActorID + " instead");
            remove = possibleRemove;
        }

        if (remove != null)
        {
            RemoveStatus(remove, false, source);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool RemoveStatusByRef(string statusRef)
    {
        StatusEffect remove = null;
        foreach(StatusEffect se in statuses)
        {
            if (se.refName == statusRef)
            {
                remove = se;
                break;
            }
        }
        if (remove != null)
        {
            RemoveStatus(remove, false);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsBleeding()
    {
        foreach(StatusEffect se in statuses)
        {
            if (se.statusFlags[(int)StatusFlags.BLEED])
            {
                return true;
            }
        }
        return false;
    }

    public static string GetCoreStatString(StatTypes s)
    {
        switch(s)
        {
            case StatTypes.CHARGETIME:
                return StringManager.GetString("stat_chargetime");
            case StatTypes.VISIONRANGE:
                return StringManager.GetString("stat_visionrange");
            case StatTypes.ACCURACY:
                return StringManager.GetString("stat_accuracy");
            case StatTypes.STRENGTH:
                return StringManager.GetString("stat_strength");
            case StatTypes.SWIFTNESS:
                return StringManager.GetString("stat_swiftness");
            case StatTypes.SPIRIT:
                return StringManager.GetString("stat_spirit");
            case StatTypes.DISCIPLINE:
                return StringManager.GetString("stat_discipline");
            case StatTypes.GUILE:
                return StringManager.GetString("stat_guile");
            case StatTypes.HEALTH:
                return StringManager.GetString("misc_hp");
            case StatTypes.ENERGY:
                return StringManager.GetString("stat_energy");
            case StatTypes.STAMINA:
            default:
                return StringManager.GetString("stat_stamina");
        }
    }

    public string GetCoreStatDisplay()
    {
        string returnString = "";
        returnString = StringManager.GetString("stat_strength") + ": " + UIManagerScript.greenHexColor + (int)GetCurStat(StatTypes.STRENGTH) + "</color> ";
        returnString += StringManager.GetString("stat_swiftness") + ": " + UIManagerScript.greenHexColor + (int)GetCurStat(StatTypes.SWIFTNESS) + "</color> ";
        returnString += StringManager.GetString("stat_spirit") + ": " + UIManagerScript.greenHexColor + (int)GetCurStat(StatTypes.SPIRIT) + "</color> ";
        returnString += StringManager.GetString("stat_discipline") + ": " + UIManagerScript.greenHexColor + (int)GetCurStat(StatTypes.DISCIPLINE) + "</color> ";
        returnString += StringManager.GetString("stat_guile") + ": " + UIManagerScript.greenHexColor + (int)GetCurStat(StatTypes.GUILE) + "</color>";
        return returnString;
    }

    public bool RemoveTemporaryNegativeStatusEffects()
    {
        bool anyRemoved = false;
        List<StatusEffect> remover = new List<StatusEffect>();
        foreach(StatusEffect se in statuses)
        {
            if (!se.isPositive)
            {
                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                {
                    remover.Add(se);
                    anyRemoved = true;
                }
            }
        }
        foreach(StatusEffect se in remover)
        {
            RemoveStatus(se, true);
        }
        return anyRemoved;
    }

    public void RemoveStatus(StatusEffect se, bool removeAll, Actor source = null)
    {
        statusDirty = true;

        List<StatusEffect> additionalRemovedStatuses = new List<StatusEffect>();
        bool ownerIsHero = owner.GetActorType() == ActorTypes.HERO;

        bool debug = false;
        #if UNITY_EDITOR
            debug = false;
        #endif
        
        if (statuses.Contains(se))
        {
            bool doubleBitePhysical = false;
            bool doubleBiteShadow = false;
            
            if (ownerIsHero) 
            {
                bool unequipContext = GameMasterScript.gmsSingleton.ReadTempGameData("unequipping") == 1;                

                if (debug && (doubleBitePhysical || doubleBiteShadow)) Debug.Log("Req Remove status " + se.refName + " from hero. Qty: " + se.quantity + ". Double bite this turn? " + GameMasterScript.heroPCActor.HasDoubleBiteSwappedThisTurn());

                doubleBitePhysical = se.refName == "doublebite_physical";
                doubleBiteShadow = se.refName == "doublebite_shadow";

                if (!doubleBitePhysical && !doubleBiteShadow) debug = false;

                if (doubleBitePhysical || doubleBiteShadow)
                {
                    if (GameMasterScript.heroPCActor.HasDoubleBiteSwappedThisTurn() && !unequipContext) 
                    {
                        if (debug) Debug.Log("Double bite already swapped this turn, so NOT REMOVING " + se.refName);
                        return;
                    }
                } 
                
            }
             
            se.quantity--;

            TryRunOnRemoveStatusScript(se);

            if (se.quantity <= 0)
            {
                se.quantity = 0;
            }

            if (removeAll)
            {
                se.quantity = 0;
            }

            if (se.quantity > 0 && se.stackMultipleDurations)
            {
                se.curDuration = se.maxDuration;
                if (debug) Debug.Log("Expiring the status " + se.refName + " , should expire soon.");
                return;
            }
            else
            {
                //if (debug) Debug.Log("Removing " + owner.displayName + " " + se.refName);
            }

            if (ownerIsHero && se.refName == "status_eagleeye")
            {
                GameMasterScript.heroPCActor.tempRevealTiles.Clear();
            }
            else if (se.refName == "status_tracked")
            {
                owner.SetFlag(ActorFlags.TRACKED, false);
            }
            else if (se.refName == "invisible" || se.refName == "spiritwalk")
            {
                owner.SetOpacity(1.0f);
            }
            else if (!ownerIsHero && (se.refName == "status_charmed" || se.refName == "status_permacharmed"))
            {
                StringManager.SetTag(0, owner.displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("charm_expire"), GameMasterScript.heroPCActor);
                Monster mn = owner as Monster;
                mn.ChangeMyFaction(mn.bufferedFaction);
                owner.myStats.AddStatusByRef("status_confused50", owner, 1);
            }
            else if (ownerIsHero && doubleBitePhysical && !GameMasterScript.heroPCActor.HasDoubleBiteSwappedThisTurn())
            {
                bool swapStatus = GameMasterScript.heroPCActor.myEquipment.GetWeapon().actorRefName == "weapon_leg_doublebiteaxe";
                if (!swapStatus)
                {
                    for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
                    {
                        if (GameMasterScript.heroPCActor.myEquipment.equipment[i] == null) continue;
                        if (GameMasterScript.heroPCActor.myEquipment.equipment[i].HasModByRef("mm_doublebite"))
                        {
                            swapStatus = true;
                            break;
                        }
                    }
                }

                if (swapStatus)
                {
                    GameMasterScript.heroPCActor.myStats.AddStatusByRef("doublebite_shadow", GameMasterScript.heroPCActor, 99);
                    GameMasterScript.heroPCActor.SetDoubleBiteSwappedThisTurn();
                    if (debug) Debug.Log("Swapping dbite to SHADOW on turn " + GameMasterScript.turnNumber);
                }
            }
            else if (ownerIsHero && doubleBiteShadow && !GameMasterScript.heroPCActor.HasDoubleBiteSwappedThisTurn())
            {
                bool swapStatus = GameMasterScript.heroPCActor.myEquipment.GetWeapon().actorRefName == "weapon_leg_doublebiteaxe";
                if (!swapStatus)
                {
                    for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
                    {
                        if (GameMasterScript.heroPCActor.myEquipment.equipment[i] == null) continue;
                        if (GameMasterScript.heroPCActor.myEquipment.equipment[i].HasModByRef("mm_doublebite"))
                        {
                            swapStatus = true;
                            break;
                        }
                    }
                }
                if (swapStatus)
                {
                    GameMasterScript.heroPCActor.myStats.AddStatusByRef("doublebite_physical", GameMasterScript.heroPCActor, 99);
                    GameMasterScript.heroPCActor.SetDoubleBiteSwappedThisTurn();
                    if (debug) Debug.Log("Swapping dbite to PHYSICAL on turn " + GameMasterScript.turnNumber);
                }
            }

            if (se.destroyStatusOnRemove.Count > 0)
            {
                foreach(string seRef in se.destroyStatusOnRemove)
                {
                    StatusEffect tryGetRef = owner.myStats.GetStatusByRef(seRef);
                    if (tryGetRef != null)
                    {
                        additionalRemovedStatuses.Add(tryGetRef);
                    }
                }
            }

            if (se.refName == "crabbleed") // Bad hardcoded, generalize this //yes.
            {
                Actor orig = se.listEffectScripts[0].originatingActor;
                owner.RemoveAnchor(orig);

                //if the originator is a player pet, restore that pet's anchor
                //to the player. Otherwise, let it go free, a pinchy shelled ronin.
                if (orig.actorfaction == Faction.PLAYER && orig != GameMasterScript.heroPCActor)
                {
                    orig.SetAnchor(GameMasterScript.heroPCActor);
                }
                else
                {
                    orig.anchor = null;
                    orig.anchorRange = 0;
                    orig.anchorID = -1;
                }
            }

            if (ownerIsHero && se.statusFlags[(int)StatusFlags.THANESONG])
            {
                RemoveStatusesByFlag(StatusFlags.THANEVERSE);
                activeSongs.Remove(se);
            }

            if (!ownerIsHero && se.refName == "status_crabgrab")
            {
                Monster mn = owner as Monster;
                mn.moveRange = 1;
                mn.cachedBattleData.maxMoveRange = 1;
            }

            if (source != null)
            {
                foreach(EffectScript eff in se.listEffectScripts)
                {
                    if (eff.originatingActor == null)
                    {
                        eff.originatingActor = source;
                    }
                    if (eff.selfActor == null)
                    {
                        eff.selfActor = owner;
                    }
                }
            }

            se.hasExpired = true;
            statusesRemovedSinceLastTurn.Add(se);
            CheckRunStatus(se, StatusTrigger.ONREMOVE);
            CheckReverseStatus(se);

            RemoveStatusObjectFromList(se);            

            se.RunCommandsOnRemove(owner);

            if (debug) Debug.Log("Total removal of " + se.refName + " " + se.quantity);
            if (GameMasterScript.gameLoadSequenceCompleted)
            {
                GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            }
            
            se.CleanupAfterRemoval();
            owner.SetBattleDataDirty();
            if (ownerIsHero)
            {
                GameMasterScript.heroPCActor.TryRefreshStatuses();
            }            
            if (owner.GetActorType() == ActorTypes.HERO && se.refName == "wrathcharge")
            {
                int numWraths = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("wrathcharge");
                GameMasterScript.heroPCActor.wrathBarScript.UpdateWrathCount(numWraths);
            }
            //Debug.Log("Removing " + owner.displayName + " " + se.refName);
        }
        else
        {
            //Debug.Log(se.refName + " not in " + owner.displayName + " but trying to remove.");
        }

        foreach(StatusEffect removeSE in additionalRemovedStatuses)
        {
            RemoveStatus(removeSE, true);
        }
    }

    // Regenerates as a %
    public void TickRegenCounter()
    {
        if (owner.IsHero())
        {
            // SPECIAL CASE: Husyn recharge.
            if (owner.myJob.jobEnum == CharacterJobs.HUSYN && owner.CheckSummonRefs("mon_runiccrystal"))
            {
                if (GetCurStat(StatTypes.ENERGY) < 25f)
                {
                    ChangeStat(StatTypes.ENERGY, 1.5f, StatDataTypes.CUR, true);
                    return;
                }
            }            
        }

        // Hero PC does not regen for now.
        #region Old Regen Code

        if (owner.TurnsSinceLastCombatAction <= 5 && owner.actorfaction == Faction.PLAYER)
        {
            return;
        }

        if ((GameStartData.CheckGameModifier(GameModifiers.PLAYER_REGEN) && owner.actorfaction == Faction.PLAYER) ||
            GameStartData.CheckGameModifier(GameModifiers.MONSTER_REGEN) && owner.actorfaction != Faction.PLAYER)
        {
            ChangeStat(StatTypes.HEALTH, GetMaxStat(StatTypes.HEALTH) * 0.01f, StatDataTypes.CUR, true);
        }

        if ((GameStartData.CheckGameModifier(GameModifiers.PLAYER_RESOURCEREGEN) && owner.actorfaction == Faction.PLAYER) ||
            GameStartData.CheckGameModifier(GameModifiers.MONSTER_REGEN) && owner.actorfaction != Faction.PLAYER)
        {
            ChangeStat(StatTypes.STAMINA, GetMaxStat(StatTypes.STAMINA) * 0.015f, StatDataTypes.CUR, true);
            ChangeStat(StatTypes.ENERGY, GetMaxStat(StatTypes.ENERGY) * 0.015f, StatDataTypes.CUR, true);
        }
        

        /*

        for (int i = 0; i < 3; i++) // Only regen health, energy, stamina
        {
            if (statArray[i].curRegenRate > 0)
            {
                statArray[i].regenCounter++;
                if (statArray[i].regenCounter >= statArray[i].curRegenRate)
                {
                    if (((StatTypes)i == StatTypes.HEALTH) && ((owner.turnsSinceLastCombatAction <= 5)))
                    {
                        if ((owner.myJob == null) || ((owner.myJob != null) && (owner.myJob.jobEnum != CharacterJobs.FLORAMANCER)))
                        continue;
                    }
                    if ((StatTypes)i == StatTypes.STAMINA)
                    {
                        if (owner.turnsSinceLastCombatAction <= 3)
                        {
                            continue;

                        }
                    }
                    statArray[i].regenCounter = 0;
                    float percentHeal = statArray[i].curRegenAmount;
                    switch((StatTypes)i)
                    {
                        case StatTypes.HEALTH:
                            percentHeal += owner.cachedBattleData.extraHeathRegen;
                            break;
                        case StatTypes.STAMINA:
							percentHeal += owner.cachedBattleData.extraEnergyRegen;
                            break;
                        case StatTypes.ENERGY:
							percentHeal += owner.cachedBattleData.extraStaminaRegen;
                            break;
                    }
					float amountHeal = (statArray[i].max * percentHeal);

                    //if (((StatTypes)i) == StatTypes.HEALTH)
                    //{
                    //    amountHeal = Mathf.Clamp(amountHeal, 0f, GetLevel() * 8f);
                    //} 

                    ChangeStat((StatTypes)i, amountHeal, StatDataTypes.CUR, true);
                }
            }
        }
        */
        #endregion
    }

    public void SetMaxRegenRate(StatTypes stat, int amount)
    {
        statArray[(int)stat].maxRegenRate = amount;
        statArray[(int)stat].curRegenRate = amount;
    }
    public void SetCurRegenRate(StatTypes stat, int amount)
    {
        statArray[(int)stat].curRegenRate = amount;
    }
    public void SetMaxRegenAmount(StatTypes stat, float amount)
    {
        statArray[(int)stat].maxRegenAmount = amount;
        statArray[(int)stat].curRegenAmount = amount;
    }
    public void SetCurRegenAmount(StatTypes stat, float amount)
    {
        statArray[(int)stat].curRegenAmount = amount;
    }

    public int GetLevel() {
        //Debug.LogError(owner.displayName + " and lvl is " + actorLevel);
		return actorLevel;
	}

	public void SetLevel(int lvl, bool validateWithOwner = true) {
        
        //Debug.LogError("Set " + owner.actorRefName + " " + owner.displayName + " " + owner.actorUniqueID + " to " + lvl + " " + validateWithOwner);

        actorLevel = lvl;        
        if (!validateWithOwner)
        {
            return;
        }
        int cap = GetMaxLevel(owner, true, owner.GetActorType() == ActorTypes.MONSTER && owner.actorfaction == Faction.PLAYER);        
        if (actorLevel > cap)
        {
            actorLevel = cap;
        }

	}

    public void SetXPFlat(int amount)
    {
        experiencePoints = amount;
    }

    /// <summary>
    /// ONLY supports increasing or decreasing level by 1. #todo: Support greater values.
    /// </summary>
    /// <param name="amount">Must be 1 or -1</param>
    public void AdjustLevel(int amount)
    {
        if (actorLevel + amount < 1) return;
        if (owner.GetActorType() == ActorTypes.HERO)
        {
            if (actorLevel + amount > GetMaxLevel(owner, true)) return;
        }
        else
        {
            int localCap = GetMaxLevel(owner, true, owner.actorfaction == Faction.PLAYER);
            if (actorLevel + amount > localCap) return;
        }
        
        SetLevel(actorLevel + amount);


        float multiplier = (float)amount;

        CharacterJobData cjd = owner.myJob;

        if (owner.IsHero())
        {
            GameMasterScript.heroPCActor.timesHealedThisLevel = 0;
            GameMasterScript.gmsSingleton.statsAndAchievements.SetHighestCharacterLevel(actorLevel);
            GameMasterScript.heroPCActor.HealAllSummonsToFull();
            GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
            float hDiff = 0;
            if (CheckHasStatusName("status_toughness"))
            {
                hDiff = BalanceData.playerToughnessHealthCurve[GetLevel() - 1] - BalanceData.playerToughnessHealthCurve[GetLevel() - 2];
            }
            else
            {
                hDiff = BalanceData.playerHealthCurve[GetLevel() - 1] - BalanceData.playerHealthCurve[GetLevel() - 2];
            }

            ChangeStat(StatTypes.HEALTH, hDiff, StatDataTypes.ALL, true);

            ChangeStat(StatTypes.STRENGTH, multiplier * cjd.statGrowth[(int)StatGrowths.STRENGTH], StatDataTypes.ALL, true);
            ChangeStat(StatTypes.SWIFTNESS, multiplier * cjd.statGrowth[(int)StatGrowths.SWIFTNESS], StatDataTypes.ALL, true);
            ChangeStat(StatTypes.DISCIPLINE, multiplier * cjd.statGrowth[(int)StatGrowths.DISCIPLINE], StatDataTypes.ALL, true);
            ChangeStat(StatTypes.GUILE, multiplier * cjd.statGrowth[(int)StatGrowths.GUILE], StatDataTypes.ALL, true);
            ChangeStat(StatTypes.SPIRIT, multiplier * cjd.statGrowth[(int)StatGrowths.SPIRIT], StatDataTypes.ALL, true);
        }
        else
        {
            // Monster level up. How 2 Handle dis
            float hDiff = BalanceData.playerHealthCurve[GetLevel() - 1] - BalanceData.playerHealthCurve[GetLevel() - 2];
            ChangeStat(StatTypes.HEALTH, hDiff, StatDataTypes.ALL, true);

            int indexOfNewStatRow = (GetLevel() - 1) * 3;
            int indexOfOldStatRow = (GetLevel() - 2) * 3;

            float coreStatsDiff = BalanceData.expectedStatValues[indexOfNewStatRow] - BalanceData.expectedStatValues[indexOfOldStatRow];
            for (int i = 0; i < nonResourceStats.Length; i++)
            {
                ChangeStat(nonResourceStats[i], coreStatsDiff, StatDataTypes.ALL, true);
            }

            float chargeTime = BalanceData.expectedStatValues[indexOfNewStatRow + 1] - BalanceData.expectedStatValues[indexOfOldStatRow+ 1];

            ChangeStat(StatTypes.CHARGETIME, chargeTime, StatDataTypes.ALL, true);
            if (GetStat(StatTypes.CHARGETIME, StatDataTypes.TRUEMAX) >= 100f) // max for monsters
            {
                SetStat(StatTypes.CHARGETIME, 100f, StatDataTypes.TRUEMAX, true);
            }

            float accuracyDiff = BalanceData.expectedStatValues[indexOfNewStatRow + 2] - BalanceData.expectedStatValues[indexOfOldStatRow + 2];
            ChangeStat(StatTypes.ACCURACY, accuracyDiff, StatDataTypes.ALL, true);
            if (GetStat(StatTypes.ACCURACY, StatDataTypes.TRUEMAX) >= 100f) // max for monsters
            {
                SetStat(StatTypes.ACCURACY, 100f, StatDataTypes.TRUEMAX, true);
            }

            float weaponPowerDiff = BalanceData.expectedMonsterWeaponPower[GetLevel() - 1] - BalanceData.expectedMonsterWeaponPower[GetLevel() - 2];

            float expectedNewMax = BalanceData.expectedMonsterWeaponPower[GetLevel() - 1] * 1.15f;
            if (owner.myEquipment.GetWeapon().power < expectedNewMax)
            {
                owner.myEquipment.GetWeapon().power += weaponPowerDiff;
                // Don't go nutz and gain too much power
                if (owner.myEquipment.GetWeapon().power >= expectedNewMax)
                {
                    owner.myEquipment.GetWeapon().power = expectedNewMax;
                }
            }
            else
            {
                owner.myEquipment.GetWeapon().power += 2f; // nominal improvement for free
            }

        }


        HealToFull();

        owner.SetBattleDataDirty();

        if (owner.GetActorType() == ActorTypes.HERO)
        {
            UIManagerScript.RefreshPlayerStats();
            UIManagerScript.UpdateDungeonText();
        }
        else
        {
            UIManagerScript.UpdatePetInfo();
        }


        bool forceShowFX = false;

        if (amount > 0)
        {
            StringManager.SetTag(0, owner.displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("gained_level_log"), owner);

            if (owner.GetActorType() == ActorTypes.HERO)
            {
                if (!UIManagerScript.dialogBoxOpen && !GameMasterScript.IsNextTurnPausedByAnimations())
                {
                    GameMasterScript.SetAnimationPlaying(true);
                    GameMasterScript.DisplayLevelUpDialog();
                    GameMasterScript.heroPCActor.levelupBoostWaiting += amount;
                    forceShowFX = true;
                }
                else
                {
                    GameMasterScript.heroPCActor.levelupBoostWaiting += amount;
                }
            }
            else
            {
                if (PlayerOptions.tutorialTips)
                {
                    if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_dialog_monster_gainlevel"))
                    {                        
                        int amt = (int)(GameMasterScript.PET_INHERIT_XP * 100f);
                        StringManager.SetTag(4, amt + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
                        Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_dialog_monster_gainlevel");
                        UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
                    }
                }
            }

            if (!GameMasterScript.IsNextTurnPausedByAnimations() || forceShowFX)
            {
                GameObject go2 = CombatManagerScript.SpawnChildSprite("LevelUpEffect", owner, Directions.NORTH, false);
                GameObject go = CombatManagerScript.SpawnChildSprite("FervirBuffSilent", owner, Directions.TRUENEUTRAL, false);
                go.transform.position = owner.GetObject().transform.position;
                go.transform.SetParent(owner.GetObject().transform);
                go.transform.localPosition = Vector3.zero;
            }
        }
    }
    public void LevelUp()
    {
        AdjustLevel(1);
    }

    public void ReverseLevelUp()
    {
        AdjustLevel(-1);
        if (owner.GetActorType() == ActorTypes.HERO)
        {
            int baseXP = HeroPC.GetXPCurve(owner.myStats.GetLevel() - 1);
            owner.myStats.SetXPFlat(baseXP);
        }

    }

    public StatusEffect GetStatusByRef(string refName)
    {
        foreach(StatusEffect se in statuses)
        {
            if (se.refName == refName)
            {
                return se;
            }
        }

        //Debug.Log(owner.actorRefName + " does not have " + refName);
        return null;
    }

    public void RemoveAllTemporaryEffects()
    {
        List<StatusEffect> statusesToRemove = new List<StatusEffect>();
        foreach(StatusEffect se in statuses)
        {
            if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
            {
                statusesToRemove.Add(se);
            }
        }

        foreach(StatusEffect se in statusesToRemove)
        {
            RemoveStatus(se, true);
        }
    }

    public void HealToFull()
    {
        if (owner.healthBarScript != null && GameMasterScript.actualGameStarted)
        {
            try { owner.healthBarScript.UpdateBar(1f); }
            catch(Exception e)
            {
                Debug.Log("Health bar update failure: " + e);
            }
        }
        for (int i = 0; i <= (int)StatTypes.VISIONRANGE; i++)
        {
            HealStat((StatTypes)i);
        }

        if (GameMasterScript.actualGameStarted && owner.IsHero())
        {
            UIManagerScript.ToggleHealthBlink(false, 1.0f);
        }
    }

    private void HealStat(StatTypes stat)
    {
        // Set the given stat to MAX, in data type CUR.
        SetStat(stat, GetStat(stat, StatDataTypes.MAX), StatDataTypes.CUR, true);
    }

	public bool ChangeExperience(int amount, bool passToPets = true)
    {
        bool canGainXp = false;
        int monsterCap = GetMaxLevelForMonsterPets();

        //Debug.LogError("Change experience by " + amount + " for " + owner.actorRefName + ", monster cap is " + monsterCap + " and pass to pets is " + passToPets);

        if (owner.GetActorType() == ActorTypes.HERO)
        {
            amount = (int)(amount * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.XP_GAIN));
            canGainXp = GetLevel() < GetMaxLevel(owner);
        }
        else
        {
            canGainXp = GetLevel() < monsterCap;
        }
        

        if (canGainXp)
        {
            experiencePoints += amount;
            if (owner.GetActorType() == ActorTypes.HERO)
            {
                if (MapMasterScript.activeMap.IsItemWorld() && amount > 0)
                {
                    GameMasterScript.heroPCActor.AddActorData("dream_xp", amount);
                }
            }
        }
        
        if (owner.GetActorType() == ActorTypes.HERO && passToPets)
        {
            Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
            if (pet != null)
            {

                amount = (int)(amount * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.PET_XP));

                //Debug.LogError("The new XP amount is now " + amount);

                if (pet.myStats.ChangeExperience((int)(amount * GameMasterScript.PET_INHERIT_XP), false))
                {
                    //if (Debug.isDebugBuild) Debug.LogError("Pet levels up.");
                    pet.myStats.LevelUp();
                    if (GetLevel() == monsterCap) 
                    {
                        //Debug.LogError("Monster is capped in level.");
                        experiencePoints = HeroPC.GetXPCurve(GetLevel()); // Cap off our experience!
                    }
                }
            }
        }

        if (!canGainXp)
        {
            return false;
        }

        int curveXPRequired = HeroPC.GetXPCurve(GetLevel());

        //Debug.LogError("Compare " + owner.actorRefName + " " + experiencePoints + " to curve " + curveXPRequired);

        if (experiencePoints >= curveXPRequired)
        {
            return true;
        }
        else
        {
            return false;
        }
	}

    // Deprecated?
    public void SetXPToNextLevel(int amount) {
		xpToNextLevel = amount;
	}

	public int GetXP() {
		return experiencePoints;
	}

    // Deprecated?
	public int GetXPToNextLevel() {

        if (GetLevel() == GetMaxLevel(owner, true))
        {
            return experiencePoints;
        }

        return HeroPC.GetXPCurve(GetLevel());
        //return xpToNextLevel;
	}

    public int GetXPToCurrentLevel()
    {

        if (GetLevel() == 1)
        {
            return 0;
        }

        return HeroPC.GetXPCurve(GetLevel()-1);
        //return xpToNextLevel;
    }

    public float GetStat(StatTypes stat, StatDataTypes sType)
    {
        switch (sType) {
            case StatDataTypes.CUR:
                return statArray[(int)stat].cur;
            case StatDataTypes.MAX:
                return statArray[(int)stat].max;
            case StatDataTypes.TRUEMAX:
                return statArray[(int)stat].trueMax;
        }
        return 0.0f;
    }


    public float GetMaxStat(StatTypes stat)
    {
        return statArray[(int)stat].max;
    }
    public float GetCurStat(StatTypes stat, bool forceHero = false)
    {
        float curStat = statArray[(int)stat].cur;
        if (stat != StatTypes.HEALTH && stat != StatTypes.STAMINA && stat != StatTypes.ENERGY)
        {
            float maxPossible = StatBlock.MAX_POSSIBLE_PLAYER_STAT;
            if (!forceHero && owner.GetActorType() != ActorTypes.HERO)
            {
                maxPossible = GetMaxPossibleStatForMonster();
            }
            if (curStat >= maxPossible)
            {
                curStat = maxPossible;
            }
        }

        return curStat;
    }

    public float GetCurStatAsPercentOfMax(StatTypes stat)
    {
        return (statArray[(int)stat].cur / statArray[(int)stat].max);
    }

    public float GetCurStatAsPercent(StatTypes stat)
    {
        return (statArray[(int)stat].cur/100f);
    }

    public void BoostCoreStatsByPercent(float percent)
    {
        BoostStatByPercent(StatTypes.HEALTH, percent, false);
        BoostStatByPercent(StatTypes.ENERGY, percent, false);
        BoostStatByPercent(StatTypes.STAMINA, percent, false);
        BoostStatByPercent(StatTypes.STRENGTH, percent, false);
        BoostStatByPercent(StatTypes.SWIFTNESS, percent, false);
        BoostStatByPercent(StatTypes.GUILE, percent, false);
        BoostStatByPercent(StatTypes.SPIRIT, percent, false);
        BoostStatByPercent(StatTypes.DISCIPLINE, percent, false);

        owner.SetBattleDataDirty();
    }

    public void BoostStatByPercent(StatTypes stat, float percent, bool bShouldRecalcData = true)
    {
        float current = GetStat(stat, StatDataTypes.TRUEMAX);
        current *= percent;
        ChangeStat(stat, current, StatDataTypes.ALL, true, true, bShouldRecalcData);
    }

    public bool ChangeStatAndSubtypes(StatTypes stat, float amount, StatDataTypes dataType)
    {
        switch (dataType)
        {
            case StatDataTypes.CUR:
                ChangeStat(stat, amount, StatDataTypes.CUR, false);
                break;
            case StatDataTypes.MAX:
                ChangeStat(stat, amount, StatDataTypes.MAX, false);
                ChangeStat(stat, amount, StatDataTypes.CUR, false);
                break;
            case StatDataTypes.TRUEMAX:
                ChangeStat(stat, amount, StatDataTypes.TRUEMAX, false);
                break;
            case StatDataTypes.ALL:
                ChangeStat(stat, amount, StatDataTypes.ALL, false);
                break;
        }
       
        ValidateStat(stat);
        return IsAlive();
    }

    public bool ChangeStat(StatTypes stat, float amount, StatDataTypes dataType, bool validate, bool addToDeadQueue = true, bool bRecalcComabtData = true)
    {
        switch(dataType)
        {
            case StatDataTypes.CUR:
                statArray[(int)stat].cur += amount;
                break;
            case StatDataTypes.MAX:
                statArray[(int)stat].max += amount;
                break;
            case StatDataTypes.TRUEMAX:
                statArray[(int)stat].trueMax += amount;
                break;
            case StatDataTypes.ALL:
                statArray[(int)stat].cur += amount;
                statArray[(int)stat].max += amount;
                statArray[(int)stat].trueMax += amount;
                break;
        }
        if (validate)
        {
            ValidateStat(stat);
        }        
        if (bRecalcComabtData &&
            (stat != StatTypes.HEALTH) && (stat != StatTypes.STAMINA) && (stat != StatTypes.ENERGY))
        {
            owner.SetBattleDataDirty();
        }
        else
        {
            if (owner.healthBarScript != null)
            {
                if (stat == StatTypes.HEALTH && GameMasterScript.actualGameStarted && owner.dungeonFloor == MapMasterScript.activeMap.floor)
                {
                    if (owner.GetActorType() == ActorTypes.HERO)
                    {
                        HeroPC hpc = owner as HeroPC;
                        try { owner.healthBarScript.UpdateBar(owner.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH)); }
                        catch(Exception e)
                        {
                            Debug.Log("Player health bar update failure: " + e);
                        }
                    }
                    else
                    {
                        try { owner.healthBarScript.UpdateBar(owner.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH)); }
                        catch(Exception e)
                        {
                            Debug.Log("Non-player update failure of health bar: " + e);
                        }
                    }
                }
            }

            if (stat == StatTypes.HEALTH && amount < 0)
            {
                scriptsToTry.Clear();                
                foreach(StatusEffect se in GetAllStatuses().ToArray())
                {
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.33f)
                    {
                        if (!string.IsNullOrEmpty(se.script_FighterBelow33Health))
                        {
                            scriptsToTry.Add(se.script_FighterBelow33Health);
                        }
                    }
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f)
                    {
                        if (!string.IsNullOrEmpty(se.script_FighterBelowHalfHealth))
                        {
                            scriptsToTry.Add(se.script_FighterBelowHalfHealth);
                        }
                    }
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.6f)
                    {
                        if (!string.IsNullOrEmpty(se.script_FighterBelow60Health))
                        {
                            scriptsToTry.Add(se.script_FighterBelow60Health);
                        }
                    }
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f)
                    {
                        if (!string.IsNullOrEmpty(se.script_FighterBelowQuarterHealth))
                        {
                            scriptsToTry.Add(se.script_FighterBelowQuarterHealth);
                        }
                    }
                }

                foreach(string tryScript in scriptsToTry)
                {
                    MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(GameplayScripts), tryScript);
                    object[] paramList = new object[1];
                    paramList[0] = owner as Fighter;
                    runscript.Invoke(null, paramList);
                }
                
            }

            if (owner.IsHero())
            {
                GameMasterScript.playerStatsChangedThisTurn = true;

                if (stat == StatTypes.HEALTH)
                {
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.3f)
                    {
                        UIManagerScript.ToggleHealthBlink(true, 0.7f); // Health blind red cycle time
                    }
                    else
                    {
                        UIManagerScript.ToggleHealthBlink(false, 1.0f);
                    }

                    // #todo - Find a way to do the below two things better.
                    // if you're not an edge thane, you should lose the survive50 bonus
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) > 0.6f)
                    {
                        if (CheckHasStatusName("edgethane_survive50"))
                        {
                            RemoveStatusByRef("edgethane_survive50");
                        }
                    }
                    if (GetCurStatAsPercentOfMax(StatTypes.HEALTH) > 0.25f)
                    {
                        if (CheckHasStatusName("vanishing_lowhealthdodge"))
                        {
                            RemoveStatusByRef("vanishing_lowhealthdodge");
                        }
                    }

                    //SHEP: Undying check
                    if (GameMasterScript.debug_neverDie && statArray[(int)StatTypes.HEALTH].cur <= 0f)
                    {
                        statArray[(int) StatTypes.HEALTH].cur = 1.0f;
                        GameLogScript.LogWriteStringRef("log_youdied");
                        BattleTextManager.NewText("UNDYING LOL", owner.GetObject(), Color.yellow, 0.1f);
                    }
                }
            }
        }

        return IsAlive(addToDeadQueue);
    }

    public List<StatusEffect> RefreshAndReturnActiveSongStatus()
    {
        activeSongs.Clear();
        StatusEffect se;
        for (int i = 0; i < statuses.Count; i++)
        {
            se = statuses[i];
            if (se.statusFlags[(int)StatusFlags.THANESONG])
            {
                activeSongs.Add(se);
            }
        }

        return activeSongs;
    }

    public void RemoveStatusesByFlag(StatusFlags matchFlag)
    {
        List<StatusEffect> remove = new List<StatusEffect>();
        foreach(StatusEffect se in statuses)
        {
            if (se.statusFlags[(int)matchFlag])
            {
                remove.Add(se);
            }
        }

        foreach(StatusEffect se in remove)
        {
            RemoveStatus(se, true);
        }
    }

    public bool IsAlive(bool addToDeadQueue = true)
    {
        if (statArray[(int)StatTypes.HEALTH].cur < 1.0f)
        {
            statArray[(int)StatTypes.HEALTH].cur = 0.0f;
            if (owner != null && addToDeadQueue)
            {
                try { GameMasterScript.AddToDeadQueue(owner); }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            return false;
        }
        return true;
    }

    public void SetStat(StatTypes stat, float amount, StatDataTypes sType, bool validate, bool bAlsoCacheBattleData = true, bool blockHasOwner = true)
    {
        switch (sType)
        {
            case StatDataTypes.CUR:
                statArray[(int)stat].cur = amount;
                break;
            case StatDataTypes.MAX:
                statArray[(int)stat].max = amount;
                break;
            case StatDataTypes.TRUEMAX:
                statArray[(int)stat].trueMax = amount;
                break;
            case StatDataTypes.ALL:
                statArray[(int)stat].cur = amount;
                statArray[(int)stat].max = amount;
                statArray[(int)stat].trueMax = amount;
                break;
        }
        if (validate) {
			ValidateStat(stat);
        }

        if (!blockHasOwner) return;

        if (bAlsoCacheBattleData &&
            stat != StatTypes.HEALTH && stat != StatTypes.STAMINA && stat != StatTypes.ENERGY)
        {
            owner.SetBattleDataDirty();
        }
        else
        {
            if (owner == GameMasterScript.heroPCActor)
            {
                // We may not need to run this as often.
                //UIManagerScript.RefreshPlayerStats();
                GameMasterScript.playerStatsChangedThisTurn = true;
            }
        }                
    }

    // Ensures current does not exceed max.
    public void ValidateStat(StatTypes stat)
    {
        float current = statArray[(int)stat].cur;
        float max = statArray[(int)stat].max;
        float trueMax = statArray[(int)stat].trueMax;

        if (stat == StatTypes.HEALTH)
        {
            if (current > GetAbsoluteMaxHealth()) current = GetAbsoluteMaxHealth();
            if (max > GetAbsoluteMaxHealth()) max = GetAbsoluteMaxHealth();
            if (trueMax > GetAbsoluteMaxHealth()) trueMax = GetAbsoluteMaxHealth();
        }

        float currentCheckMax = max;
        // Limit our *effective* max Energy by certain abilities.
        if (owner.GetActorType() == ActorTypes.HERO && stat == StatTypes.ENERGY)
        {
            currentCheckMax -= owner.cachedBattleData.energyReservedByAbilities;
        }
        // Limit our *effective* max Stamina by certain abilities.
        else if (owner.GetActorType() == ActorTypes.HERO && stat == StatTypes.STAMINA)
        {
            currentCheckMax -= owner.cachedBattleData.staminaReservedByAbilities;
        }

        if (current > currentCheckMax)
        {
            current = currentCheckMax;

        }
        if (current < 0)
        {
            current = 0;
        }
        if (max < 0)
        {
            max = 0;
        }
        if (trueMax < 0)
        {
            trueMax = 0;
        }

        if (stat == StatTypes.HEALTH && current < 1f)
        {
            current = 0f;
        }



        statArray[(int)stat].cur = current;
        statArray[(int)stat].max = max;
        statArray[(int)stat].trueMax = trueMax;
        if ((stat == StatTypes.HEALTH || stat == StatTypes.ENERGY || stat == StatTypes.STAMINA) && owner == GameMasterScript.heroPCActor)
        {
            //UIManagerScript.RefreshPlayerStats();
            GameMasterScript.playerStatsChangedThisTurn = true;
        }

    }

    private void ValidateAllStats()
    {
       for (int i = 0; i < (int)StatTypes.COUNT-4; i++)
        {
            ValidateStat((StatTypes)i);
        }
    }

    public int CountStatusesByFlag(StatusFlags flag)
    {
        int count = 0;
        foreach(StatusEffect se in statuses)
        {
            if (se.statusFlags[(int)flag])
            {
                count++;
            }
        }

        return count;
    }

    public void ResetStatBonusesFromLevelUps()
    {
        SingleStat[] preChangeStats = new SingleStat[5];
        for (int i = 0; i < nonResourceStats.Length; i++)
        {
            preChangeStats[i] = new SingleStat();
            preChangeStats[i].trueMax = GetStat(nonResourceStats[i], StatDataTypes.TRUEMAX);
        }

        SetStat(StatTypes.STRENGTH, HERO_BASE_STRENGTH, StatDataTypes.TRUEMAX, false);
        SetStat(StatTypes.SWIFTNESS, HERO_BASE_SWIFTNESS, StatDataTypes.TRUEMAX, false);
        SetStat(StatTypes.SPIRIT, HERO_BASE_SPIRIT, StatDataTypes.TRUEMAX, false);
        SetStat(StatTypes.DISCIPLINE, HERO_BASE_DISCIPLINE, StatDataTypes.TRUEMAX, false);
        SetStat(StatTypes.GUILE, HERO_BASE_GUILE, StatDataTypes.TRUEMAX, false);

        for (int i = 0; i < nonResourceStats.Length; i++)
        {
            ChangeStat(nonResourceStats[i], (GetLevel() - 1) * 1.5f, StatDataTypes.TRUEMAX, true);
            ChangeStatAndSubtypes(nonResourceStats[i], (GetStat(nonResourceStats[i], StatDataTypes.TRUEMAX) - preChangeStats[i].trueMax), StatDataTypes.MAX);
        }

        owner.SetActorData("levelup_bonuses_left", GetLevel() - 1);
    }

    void ReadParsedStats(string[] parsedStats, int iStatType, bool validateWithOwner = true)
    {
        if (parsedStats.Length == 1)
        {
            // All stats same.
            float value = CustomAlgorithms.TryParseFloat(parsedStats[0]);
            SetStat((StatTypes)iStatType, value, StatDataTypes.CUR, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, value, StatDataTypes.MAX, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, value, StatDataTypes.TRUEMAX, false, true, validateWithOwner);
        }
        else if (parsedStats.Length == 2)
        {
            // Cur, max, and truemax is the same as max
            float cur = CustomAlgorithms.TryParseFloat(parsedStats[0]);
            float max = CustomAlgorithms.TryParseFloat(parsedStats[1]);
            SetStat((StatTypes)iStatType, cur, StatDataTypes.CUR, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, max, StatDataTypes.MAX, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, max, StatDataTypes.TRUEMAX, false, true, validateWithOwner);
        }
        else
        {
            // OK, reading all three then.
            float cur = CustomAlgorithms.TryParseFloat(parsedStats[0]);
            float max = CustomAlgorithms.TryParseFloat(parsedStats[1]);
            float truemax = CustomAlgorithms.TryParseFloat(parsedStats[2]);

            SetStat((StatTypes)iStatType, cur, StatDataTypes.CUR, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, max, StatDataTypes.MAX, false, true, validateWithOwner);
            SetStat((StatTypes)iStatType, truemax, StatDataTypes.TRUEMAX, false, true, validateWithOwner);
        }
    }

    public void MarkStatusForRemoval(StatusEffect se)
    {
        if (statusesQueuedForRemoval.ContainsKey(se.refName))
        {
            statusesQueuedForRemoval[se.refName]++;
        }
        else
        {
            statusesQueuedForRemoval.Add(se.refName, 1);
        }
    }

    public void MarkStatusForRemoval(string statusRef)
    {
        StatusEffect checkForSE = GetStatusByRef(statusRef);
        if (checkForSE != null)
        {
            MarkStatusForRemoval(checkForSE);
        }
    }

    // Statuses may be marked for removal by string refname and quantity, this function will *actually* remove them
    // And will then clear the 'queue' completely
    // This function is always run at the end of a Fighter's turn, but can be run before that too.
    public void RemoveQueuedStatuses()
    {
        foreach(string statusKey in statusesQueuedForRemoval.Keys)
        {
            int amountToRemove = statusesQueuedForRemoval[statusKey];
            for (int i = 0; i < amountToRemove; i++)
            {
                RemoveStatusByRef(statusKey);
            }            
        }

        statusesQueuedForRemoval.Clear();
    }

    public static int GetMaxLevel(Actor owner, bool validateWithOwner = true, bool isPlayerPet = false)
    {
        if (validateWithOwner && owner.GetActorType() == ActorTypes.HERO)
        {
            int maxHeroLevel = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_PLAYER_LEVEL_CAP_EXPANSION : GameMasterScript.MAX_PLAYER_LEVEL_CAP;
            //Debug.LogError("max hero level for " + owner.displayName + " is " + maxHeroLevel);
            return maxHeroLevel;
        }
        else
        {            
            if (isPlayerPet)
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                {
                    int value = GameStartData.NewGamePlus > 0 ? MAX_MONSTER_PET_LEVEL_NGPLUS_EXPANSION : MAX_MONSTER_PET_LEVEL_EXPANSION;
                    //Debug.LogError("max pet level for " + owner.displayName + " is " + value + " " + GameStartData.NewGamePlus);
                    return value;
                }
                else
                {
                    int value = GameStartData.NewGamePlus > 0 ? MAX_MONSTER_PET_LEVEL_NGPLUS : MAX_MONSTER_PET_LEVEL;
                    //Debug.LogError("or max pet level for " + owner.displayName + " is " + value + " " + GameStartData.NewGamePlus);
                    return value;
                }                
            }
            else
            {
                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                {
                    int value = GameMasterScript.MAX_MONSTER_LEVEL_CAP_EXPANSION;
                    //Debug.LogError("3 max pet level for " + owner.displayName + " is " + value);
                    return value;
                }
                else
                {
                    int value = GameMasterScript.MAX_MONSTER_LEVEL_CAP;
                    //Debug.LogError("4 max pet level for " + owner.displayName + " is " + value);
                    return value;
                }
            }

            //return DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_MONSTER_LEVEL_CAP_EXPANSION : GameMasterScript.MAX_MONSTER_LEVEL_CAP;
        }
    }

    public static int GetMaxLevelForMonsterPets()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return GameStartData.NewGamePlus > 0 ? MAX_MONSTER_PET_LEVEL_NGPLUS_EXPANSION : MAX_MONSTER_PET_LEVEL_EXPANSION;
        }
        else
        {
            return GameStartData.NewGamePlus > 0 ? MAX_MONSTER_PET_LEVEL_NGPLUS : MAX_MONSTER_PET_LEVEL;
        }
        
    }

    public void SetHeroBaseStats()
    {
        SetStat(StatTypes.HEALTH, 170.0f, StatDataTypes.ALL, true);
        SetStat(StatTypes.STAMINA, 100.0f, StatDataTypes.ALL, true);
        SetStat(StatTypes.ENERGY, 100.0f, StatDataTypes.ALL, true);
        SetStat(StatTypes.STRENGTH, HERO_BASE_STRENGTH, StatDataTypes.ALL, true);
        SetStat(StatTypes.SWIFTNESS, HERO_BASE_SWIFTNESS, StatDataTypes.ALL, true);
        SetStat(StatTypes.ACCURACY, 100f, StatDataTypes.ALL, true);
        SetStat(StatTypes.SPIRIT, HERO_BASE_SPIRIT, StatDataTypes.ALL, true);
        SetStat(StatTypes.DISCIPLINE, HERO_BASE_DISCIPLINE, StatDataTypes.ALL, true);
        SetStat(StatTypes.GUILE, HERO_BASE_GUILE, StatDataTypes.ALL, true);
        SetStat(StatTypes.CHARGETIME, 100.0f, StatDataTypes.ALL, true);
        SetStat(StatTypes.VISIONRANGE, 16f, StatDataTypes.ALL, true);
        SetLevel(1);
    }

    public void RemoveNullStatuses()
    {
        statuses.RemoveAll(s => s == null);
    }
    public void VerifySelfAndOriginatingActorForAllStatuses()
    {
        if (owner == null)
        {
            return;
        }
        foreach (StatusEffect se in GetAllStatuses())
        {
            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff != null)
                {
                    if (eff.selfActor == null)
                    {
                        eff.selfActor = owner;
                    }
                    if (eff.originatingActor == null)
                    {
                        eff.originatingActor = owner; // making an assumption here, but we have to.
                    }
                }
            }
        }
    }
    public float GetAbsoluteMaxHealth()
    {
        if (GameStartData.NewGamePlus >= 2 && owner.GetActorType() == ActorTypes.MONSTER && owner.actorfaction == Faction.ENEMY)
        {
            return ABSOLUTE_MAX_HEALTH_SAVAGE;
        }
        else
        {
            if (MysteryDungeonManager.InOrCreatingMysteryDungeon() && MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.NO_SCALING_LIMIT))
            {
                return ABSOLUTE_MAX_HEALTH_SAVAGE;
            }
            return ABSOLUTE_MAX_HEALTH;
        }
    }

    public float GetMaxPossibleStatForMonster()
    {
        if (GameStartData.NewGamePlus >= 2 && owner.GetActorType() == ActorTypes.MONSTER && owner.actorfaction == Faction.ENEMY)
        {
            return MAX_POSSIBLE_MONSTER_STAT_SAVAGE;
        }
        else
        {
            if (MysteryDungeonManager.InOrCreatingMysteryDungeon() && MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.NO_SCALING_LIMIT))
            {
                return MAX_POSSIBLE_MONSTER_STAT_SAVAGE;
            }
            return MAX_POSSIBLE_MONSTER_STAT;
        }
            
    }

    public StatusEffect GetFirstActiveStatusOfTag(AbilityTags tag)
    {
        foreach(StatusEffect se in statuses)
        {
            if (se.CheckAbilityTag(tag))
            {
                return se;
            }
        }

        return null;
    }
}
