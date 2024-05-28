using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System;



public class MonsterTemplateData
{
    // This is monster TEMPLATE data
    public string monsterName;
    public string refName;
    public bool cannotBeChampion;
    public bool cannotBeRumorTarget;
    public bool showInPedia;
    public float hp;
    public float strength;
    public float swiftness;
    public float discipline;
    public float spirit;
    public float guile;
    public float stamina;
    public float energy;

    public string monFamily;
    public string eggRef;

    public int weight;

    public int healthRegenRate;
    public float healthRegenAmount;
    public int staminaRegenRate;
    public float staminaRegenAmount;
    public int energyRegenRate;
    public float energyRegenAmount;

    public float chargetime;
    public float accuracy;
    public float visionRange;
    public float challengeValue;
    public float lootChance;
    public float xpMod;
    public float aggroRange;
    public float stalkerRange;
    public float helpRange;
    public int moveRange;
    // For drunk walk, new wandering;
    public Directions wanderDirection;
    public float drunkWalkChance;
    // End wandering data
    public int turnsToLoseInterest;
    public bool autoSpawn;
    public bool isBoss;
    public bool showBossHealthBar;
    public string weaponID;
    public string offhandWeaponID;
    public string offhandArmorID;
    public string armorID;
    public string prefab;
    public int baseLevel;
    public MoveBehavior monMoveType;
    public BehaviorState monBehavior;
    public MoveBoundary monMoveBoundary;
    public int[] monAttributes;
    public List<MonsterPowerData> monsterPowers;

    public string spriteRefOnSummon;

    public Faction faction;

    public int uniqueID;

    public List<SeasonalPrefabData> seasonalPrefabReplacements;

    public bool replaceRef; // only used for initial read from XML

    public static string[] monsterAttributesAsString;

    public string scriptTakeAction;
    public string scriptOnDefeat;

    // When monster is spawned, add the contents of the dictionaries below into the Actor's dictActorData/String
    public Dictionary<string, int> defaultActorData;
    public Dictionary<string, string> defaultActorDataString;

    public bool excludeFromHotbarCheck; // if true, this actor ref is allowed to exist if summoned and you don't have skill on hotbar

    public List<string> guaranteeLoot;

    public static bool WriteSpecialFieldsForSerialization(object source, string strFieldName, StringBuilder sbData)
    {
        MonsterTemplateData mon = source as MonsterTemplateData;

        switch (strFieldName.ToLowerInvariant())
        {
            case "monsterpowers":
                if (mon.monsterPowers != null &&
                    mon.monsterPowers.Count > 0)
                {
                    sbData.Append("monsterpowers|" + mon.monsterPowers.Count);
                    foreach (MonsterPowerData ab in mon.monsterPowers)
                    {
                        sbData.Append("|" + ab.healthThreshold.ToString());
                        sbData.Append("|" + ab.useState.ToString());
                        sbData.Append("|" + ab.minRange.ToString());
                        sbData.Append("|" + ab.maxRange.ToString());
                        sbData.Append("|" + ab.chanceToUse.ToString());
                        sbData.Append("|" + ab.useWithNoTarget.ToString());
                        sbData.Append("|" + ab.abilityRef.refName);
                    }

                    sbData.Append("|");
                }
                return true;
            case "seasonalprefabreplacements":
                if (mon.seasonalPrefabReplacements != null &&
                    mon.seasonalPrefabReplacements.Count > 0)
                {
                    sbData.Append("seasonalprefabreplacements|" + mon.seasonalPrefabReplacements.Count);
                    foreach (SeasonalPrefabData spd in mon.seasonalPrefabReplacements)
                    {
                        sbData.Append("|" + spd.whichSeason.ToString());
                        sbData.Append("|" + spd.prefab.ToString());
                    }

                    sbData.Append("|");
                }
                return true;
        }

        return false;
    }

    public bool ValidateMonsterTemplate()
    {
        if (hp < 1f)
        {
            Debug.Log("Monster " + refName + " must have at least 1 Health.");
            return false;
        }
        if (stamina < 0 || energy < 0)
        {
            Debug.Log("Monster " + refName + " cannot have negative Stamina or Energy.");
            return false;
        }
        if (strength < 1f || swiftness < 1f || guile < 1f || discipline < 1f || spirit < 1f)
        {
            Debug.Log("Monster " + refName + " must have core stats of at least 1 (Str, Spirit, Swiftness etc)");
            return false;
        }
        if (accuracy < 1f)
        {
            //Debug.Log("Monster " + refName + " has chargetime or accuracy of less than 1, invalid.");
            accuracy = 1f;
        }
        if (chargetime < 1f)
        {
            chargetime = 1f;
        }
        if (accuracy > 100f)
        {
            accuracy = 100f;
            Debug.Log("Monster " + refName + " has chargetime of above 100. Cap is 100.");
        }
        /* if (visionRange < 1 || visionRange > 25)
        {
            Debug.Log("Monster " + refName + " has vision range outside of max range 1-14. Clamping.");
            visionRange = Mathf.Clamp(visionRange, 1f, 25f);            
        } */
        /*if (challengeValue < 1f || challengeValue > Item.MAX_STARTING_CHALLENGE_VALUE)
        {
            Debug.Log("Monster " + refName + " challenge value must be betwee 1.0f and " + Item.MAX_STARTING_CHALLENGE_VALUE + ", clamping");
            challengeValue = Mathf.Clamp(challengeValue, 1f, Item.MAX_STARTING_CHALLENGE_VALUE);
        }*/
        if (lootChance < 0)
        {
            Debug.Log("Monster " + refName + " cannot have <0 loot chance. setting to 0.");
            lootChance = 0;
        }
        if (drunkWalkChance < 0 || drunkWalkChance > 1f)
        {
            Debug.Log("Monster " + refName + " drunkwalk chance is less than 0 or higher than 1, clamping");
            drunkWalkChance = Mathf.Clamp(drunkWalkChance, 0f, 1f);
        }
        if (baseLevel < 1 || baseLevel > GameMasterScript.MAX_MONSTER_LEVEL_CAP)
        {
            //Debug.Log("Monster " + refName + " base level is less than 1 or greater than cap of " + GameMasterScript.MAX_MONSTER_LEVEL_CAP);
            //return false;
        }
        if (turnsToLoseInterest < 0)
        {
            turnsToLoseInterest = 0;
            Debug.Log("Monster " + refName + " has turns to lose interest of <0, must be at least 0. Value set to 0 now.");
        }
        if (aggroRange < 0)
        {
            Debug.Log("Monster " + refName + " cannot have negative aggro range, value has been set to 0.");
            aggroRange = 0;
        }
        if (stalkerRange < 0)
        {
            Debug.Log("Monster " + refName + " cannot have negative stalkerRange range, value has been set to 0.");
            stalkerRange = 0;
        }
        if (helpRange < 0)
        {
            Debug.Log("Monster " + refName + " cannot have negative callforhelp range, value has been set to 0.");
            helpRange = 0;
        }
        if (moveRange < 0)
        {
            Debug.Log("Monster " + refName + " cannot have negative move range, value has been set to 0.");
            moveRange = 0;
        }

        return true;
    }

    public void ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();
        monsterPowers = new List<MonsterPowerData>();
        string txt;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "Name":
                    monsterName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                    break;
                case "ReplaceRef":
                    replaceRef = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ExcludeFromHotbarCheck":
                    excludeFromHotbarCheck = true;
                    reader.Read();
                    break;
                case "Weight":
                    weight = reader.ReadElementContentAsInt();
                    break;
                case "Family":
                    monFamily = reader.ReadElementContentAsString();
                    if (!GameMasterScript.monsterFamilyList.Contains(monFamily))
                    {
                        GameMasterScript.monsterFamilyList.Add(monFamily);
                    }
                    break;
                case "Egg":
                    eggRef = reader.ReadElementContentAsString();
                    break;
                case "RefName":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "NoChampion":
                    cannotBeChampion = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "AutoSpawn":
                    autoSpawn = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "Spawn":
                    autoSpawn = true;
                    reader.ReadElementContentAsString();
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
                    GameMasterScript.gmsSingleton.TryPreloadResourceInstant(spriteRefOnSummon, "SpriteEffects/" + spriteRefOnSummon);
}
                    break;
                case "Boss":
                    isBoss = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ShowBossHealthBar":
                    showBossHealthBar = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "HP":
                    txt = reader.ReadElementContentAsString();
                    hp = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Stamina":
                    txt = reader.ReadElementContentAsString();
                    stamina = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Energy":
                    txt = reader.ReadElementContentAsString();
                    energy = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "HPRegenRate":
                    healthRegenRate = reader.ReadElementContentAsInt();
                    break;
                case "HPRegenAmount":
                    txt = reader.ReadElementContentAsString();
                    healthRegenAmount = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "StaminaRegenRate":
                    staminaRegenRate = reader.ReadElementContentAsInt();
                    break;
                case "StaminaRegenAmount":
                    txt = reader.ReadElementContentAsString();
                    staminaRegenAmount = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "EnergyRegenRate":
                    energyRegenRate = reader.ReadElementContentAsInt();
                    break;
                case "EnergyRegenAmount":
                    txt = reader.ReadElementContentAsString();
                    energyRegenAmount = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "GuaranteeLoot":
                    guaranteeLoot.Add(reader.ReadElementContentAsString());
                    break;
                case "Stats":
                    txt = reader.ReadElementContentAsString();
                    /*  8 stats written in order:
                     Strength
                     Swift
                     Disc
                     Spirit
                     Guile
                     Acc
                     Vision
                     Chargetime */
                    char splitChar = ',';
                    //Debug.Log("Loading monster template " + refName + " game ver is " + GameStartData.loadGameVer + " text is " + txt);
                    /* if (GameStartData.loadGameVer <= 109)
                    {
                        splitChar = ',';
                    } */
                    string[] parsed = txt.Split(splitChar);
                    strength = CustomAlgorithms.TryParseFloat(parsed[0]);
                    swiftness = CustomAlgorithms.TryParseFloat(parsed[1]);
                    discipline = CustomAlgorithms.TryParseFloat(parsed[2]);
                    spirit = CustomAlgorithms.TryParseFloat(parsed[3]);
                    guile = CustomAlgorithms.TryParseFloat(parsed[4]);
                    accuracy = CustomAlgorithms.TryParseFloat(parsed[5]);
                    visionRange = CustomAlgorithms.TryParseFloat(parsed[6]);
                    chargetime = CustomAlgorithms.TryParseFloat(parsed[7]);
                    break;
                case "Strength":
                    txt = reader.ReadElementContentAsString();
                    strength = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Discipline":
                    txt = reader.ReadElementContentAsString();
                    discipline = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Swiftness":
                    txt = reader.ReadElementContentAsString();
                    swiftness = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Spirit":
                    txt = reader.ReadElementContentAsString();
                    spirit = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Guile":
                    txt = reader.ReadElementContentAsString();
                    guile = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Accuracy":
                    txt = reader.ReadElementContentAsString();
                    accuracy = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "VisionRange":
                    txt = reader.ReadElementContentAsString();
                    visionRange = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Chargetime":
                case "ChargeTime":
                    txt = reader.ReadElementContentAsString();
                    chargetime = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Rank":
                    int r = reader.ReadElementContentAsInt();
                    if (r < 1) r = 1;
                    if (r > 10) r = 10;
                    challengeValue = Item.ConvertRankToChallengeValue(r);
                    break;
                case "CV":
                case "ChallengeValue":
                    txt = reader.ReadElementContentAsString();
                    challengeValue = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "LootChance":
                    txt = reader.ReadElementContentAsString();
                    lootChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "DrunkWalkChance":
                    txt = reader.ReadElementContentAsString();
                    drunkWalkChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "NoRumor":
                    cannotBeRumorTarget = true;
                    reader.ReadElementContentAsString();
                    break;
                case "WeaponID":
                    weaponID = reader.ReadElementContentAsString();
                    break;
                case "OffhandWeaponID":
                    offhandWeaponID = reader.ReadElementContentAsString();
                    break;
                case "ArmorID":
                    armorID = reader.ReadElementContentAsString();
                    break;
                case "OffhandArmorID":
                    offhandArmorID = reader.ReadElementContentAsString();
                    break;
                case "Prefab":
                    prefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                    GameMasterScript.TryPreloadResourceNoBundles(prefab, "Monsters/" + prefab);
}
else
{
                    GameMasterScript.gmsSingleton.TryPreloadResourceInstant(prefab, "Monsters/" + prefab);
}
                    break;
                case "XPMod":
                    txt = reader.ReadElementContentAsString();
                    xpMod = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "Lv":
                case "Level":
                    baseLevel = reader.ReadElementContentAsInt();
                    break;
                case "Bore":
                case "TurnsToLoseInterest":
                    turnsToLoseInterest = reader.ReadElementContentAsInt();
                    break;
                case "Pedia":
                    //showInPedia = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    showInPedia = true;
                    reader.ReadElementContentAsString();
                    break;
                case "ShowInPedia":
                    showInPedia = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "ScriptTakeAction":
                case "Script_TakeAction":
                    scriptTakeAction = reader.ReadElementContentAsString();
                    MonsterBehaviorScript.CacheScript(scriptTakeAction);
                    break;
                case "ScriptOnDefeat":
                    scriptOnDefeat = reader.ReadElementContentAsString();
                    break;
                case "Agg":
                case "AggroRange":
                    txt = reader.ReadElementContentAsString();
                    aggroRange = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "StalkerRange":
                    txt = reader.ReadElementContentAsString();
                    stalkerRange = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "CallForHelpRange":
                    txt = reader.ReadElementContentAsString();
                    helpRange = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "SeasonalPrefab":
                    SeasonalPrefabData spd = new SeasonalPrefabData();
                    seasonalPrefabReplacements.Add(spd);
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "Season":
                                spd.whichSeason = (Seasons)Enum.Parse(typeof(Seasons), reader.ReadElementContentAsString());
                                break;
                            case "Prefab":
                                spd.prefab = reader.ReadElementContentAsString();
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
                                GameMasterScript.TryPreloadResourceNoBundles(spd.prefab, "Monsters/" + spd.prefab);
}
else
{
                                GameMasterScript.gmsSingleton.TryPreloadResourceInstant(spd.prefab, "Monsters/" + spd.prefab);
}
                                
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "MoveType":
                    MoveBehavior mv = (MoveBehavior)Enum.Parse(typeof(MoveBehavior), reader.ReadElementContentAsString());
                    monMoveType = mv;
                    break;
                case "MoveBoundary":
                    MoveBoundary mr = (MoveBoundary)Enum.Parse(typeof(MoveBoundary), reader.ReadElementContentAsString());
                    monMoveBoundary = mr;
                    break;
                case "Behavior":
                    BehaviorState bs = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString());
                    monBehavior = bs;
                    break;
                case "MoveRange":
                    moveRange = reader.ReadElementContentAsInt();
                    break;
                case "DefaultActorData":
                    // format: key|value
                    string unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    defaultActorData.Add(parsed[0], Int32.Parse(parsed[1]));
                    break;
                case "DefaultActorDataString":
                    // format: key|value
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    defaultActorDataString.Add(parsed[0], parsed[1]);
                    break;
                case "Skl":
                case "MonsterPower":
                    reader.ReadStartElement();
                    MonsterPowerData mpd = new MonsterPowerData();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "Ref":
                            case "SkillRef":
                                string abilRef = reader.ReadElementContentAsString();
                                mpd.abilityRef = AbilityScript.GetAbilityByName(abilRef);
                                if (mpd.abilityRef == null)
                                {
                                    if (Debug.isDebugBuild) Debug.Log("WARNING: " + abilRef + " for monster " + refName + " does not exist, NOT adding this power to monster.");
                                    // read through to end of node.
                                    while (reader.NodeType != XmlNodeType.EndElement && reader.Name != "MonsterPower")
                                    {
                                        reader.Read();
                                    }
                                }
                                else
                                {
                                    monsterPowers.Add(mpd);
                                }
                                break;
                            case "IgnoreCosts":
                                /* mpd.abilityRef.energyCost = 0;
                                mpd.abilityRef.healthCost = 0;
                                mpd.abilityRef.staminaCost = 0; */
                                mpd.ignoreCosts = true;
                                reader.ReadElementContentAsInt();
                                break;
                            case "EnforceRangesForHeroTargeting":
                                mpd.enforceRangesForHeroTargeting = true;
                                reader.ReadElementContentAsInt();
                                break;
                            case "AlwaysUseIfInRange":
                                mpd.alwaysUseIfInRange = true;
                                reader.ReadElementContentAsInt();
                                break;
                            case "MinRange":
                                mpd.minRange = reader.ReadElementContentAsInt();
                                break;
                            case "MaxRange":
                                mpd.maxRange = reader.ReadElementContentAsInt();
                                break;
                            case "HealthThreshold":
                                txt = reader.ReadElementContentAsString();
                                mpd.healthThreshold = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "UseWithNoTarget":
                                mpd.useWithNoTarget = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "ReqData":
                                mpd.reqActorData = reader.ReadElementContentAsString();
                                break;
                            case "ReqDataValue":
                                mpd.reqActorDataValue = reader.ReadElementContentAsInt();
                                break;
                            case "ChanceToUse":
                                txt = reader.ReadElementContentAsString();
                                mpd.chanceToUse = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "BehaviorStateReq":
                            case "UseState":
                                mpd.useState = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString());
                                break;
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    reader.ReadEndElement();
                    break;
                case "Faction":
                    faction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString());
                    break;
                case "Attr":
                    unparsed = reader.ReadElementContentAsString();
                    // Format is ATTR:10,ATTR:10, etc.
                    parsed = unparsed.Split(',');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        string[] sub = parsed[i].Split(':');
                        MonsterAttributes readAttribute = (MonsterAttributes)Enum.Parse(typeof(MonsterAttributes), sub[0]);
                        Int32.TryParse(sub[1], out monAttributes[(int)readAttribute]);
                    }
                    break;
                default:
                    bool anyAttribute = false;
                    // Old deprecated method
                    /* string rName = reader.Name;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        for (int c = 0; c < (int)MonsterAttributes.COUNT; c++)
                        {
                            if (rName == monsterAttributesAsString[c])
                            {
                                Debug.Log("Uh oh! " + refName + " read an attribute.");
                                monAttributes[c] = reader.ReadElementContentAsInt();
                                anyAttribute = true;
                            }
                        }
                    } */

                    if (!anyAttribute)
                    {
                        reader.Read();
                    }
                    break;
            }

        } // End of monster while loop 

        // Clean up the monster.

        if (String.IsNullOrEmpty(weaponID))
        {
            Debug.Log(refName + " has no weapon ID, assigning a default.");
            weaponID = "weapon_fists";
        }

        reader.ReadEndElement();
    }

    //return value is how much to advance the index by. If it is < 0, that means we didn't catch anything.
    public static int ReadSpecialFieldsForSerialization(object source, string strFieldName, string[] splitValues, int idx)
    {
        MonsterTemplateData mon = source as MonsterTemplateData;

        switch (strFieldName.ToLowerInvariant())
        {
            case "monsterpowers":
                {
                    mon.monsterPowers = new List<MonsterPowerData>();
                    //
                    // |monsterpowers|[count]|[6 pieces of regular data followed by 1 ability ref]

                    int iNumPowers = Int32.Parse(splitValues[idx + 1]);
                    idx += 2;
                    int iNumAdvanced = 0;
                    for (int t = 0; t < iNumPowers; t++)
                    {
                        MonsterPowerData mpd = new MonsterPowerData();
                        mpd.healthThreshold = CustomAlgorithms.TryParseFloat(splitValues[idx]);
                        mpd.useState = (BehaviorState)Enum.Parse(typeof(BehaviorState), splitValues[idx + 1]);
                        mpd.minRange = Int32.Parse(splitValues[idx + 2]);
                        mpd.maxRange = Int32.Parse(splitValues[idx + 3]);
                        mpd.chanceToUse = CustomAlgorithms.TryParseFloat(splitValues[idx + 4]);
                        mpd.useWithNoTarget = splitValues[idx + 5].ToLowerInvariant() == "true";
                        mpd.abilityRef = AbilityScript.GetAbilityByName(splitValues[idx + 6]);
                        iNumAdvanced += 7;

                        mon.monsterPowers.Add(mpd);
                    }

                    return iNumAdvanced;
                }

            case "seasonalprefabreplacements":
                {
                    mon.seasonalPrefabReplacements = new List<SeasonalPrefabData>();

                    int iNumPowers = Int32.Parse(splitValues[idx + 1]);
                    idx += 2;
                    int iNumAdvanced = 0;
                    for (int t = 0; t < iNumPowers; t++)
                    {
                        SeasonalPrefabData spd = new SeasonalPrefabData();
                        spd.whichSeason = (Seasons)Enum.Parse(typeof(Seasons), splitValues[idx]);
                        spd.prefab = splitValues[idx + 1];
                        iNumAdvanced += 2;
                        mon.seasonalPrefabReplacements.Add(spd);
                    }

                    return iNumAdvanced;
                }
        }

        return -1;
    }


    public static void PostLoadFromSerialization(object source)
    {
        MonsterTemplateData mon = source as MonsterTemplateData;
        if (mon.prefab != null)
        {
if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
{
            GameMasterScript.TryPreloadResourceNoBundles(mon.prefab, "Monsters/" + mon.prefab);
}
else
{
            GameMasterScript.gmsSingleton.TryPreloadResourceInstant(mon.prefab, "Monsters/" + mon.prefab);
}
        }
    }


    public MonsterTemplateData()
    {
        faction = Faction.ENEMY;
        defaultActorData = new Dictionary<string, int>();
        defaultActorDataString = new Dictionary<string, string>();
        monAttributes = new int[(int)MonsterAttributes.COUNT];
        monsterPowers = new List<MonsterPowerData>();
        seasonalPrefabReplacements = new List<SeasonalPrefabData>();
        monMoveType = MoveBehavior.WANDER;
        monMoveBoundary = MoveBoundary.WANDER;
        lootChance = 1.0f;
        xpMod = 1.0f;
        guaranteeLoot = new List<string>();
    }

    public int CompareTo(MonsterTemplateData mtd)
    {
        if (baseLevel > mtd.baseLevel)
        {
            return 1;
        }
        if ((int)baseLevel < mtd.baseLevel)
        {
            return -1;
        }
        return 0;
    }

    public bool IsRanged()
    {
        Weapon w = Item.GetItemTemplateFromRef(weaponID) as Weapon;
        if (w.isRanged)
        {
            return true;
        }

        foreach (MonsterPowerData mpd in monsterPowers)
        {
            if (mpd.maxRange > 1)
            {
                return true;
                //break;
            }
        }
        return false;
    }
}