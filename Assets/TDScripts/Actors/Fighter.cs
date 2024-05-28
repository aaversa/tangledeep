using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public class Fighter : Actor
{
    public EquipmentBlock myEquipment;
    public StatBlock myStats;
    public AbilityComponent myAbilities;
    public float actionTimer; // Current CT value.
    public CharacterJobData myJob;
    //public float lastAngleReceivedAttack;
    //public float lastAngleUsedAttack;
    public Directions lastDirectionAttackedFrom;
    public Directions lastDirectionUsedAttack;

    public float[] damageTakenLastThreeTurns;

    public List<Vector2> tempRevealTiles;
    public Vector2 positionAtStartOfTurn;

    public ElementalAuraManager elemAuraObject;
    public float aggroMultiplier;

    public string resistString;
    public bool resistStringDirty;
    public FighterBattleData cachedBattleData
    {
        get
        {
            if (_cachedBattleData == null)
            {
                _cachedBattleData = new FighterBattleData();
            }

            if (_cachedBattleData.IsDirty())
            {
                CalculateBattleData_Internal();
            }

            return _cachedBattleData;
        }
    }

    public float damageTakenThisTurn;

    private FighterBattleData _cachedBattleData;
    public void SetBattleDataDirty() { _cachedBattleData.SetDirty(); }
    public Fighter lastActorAttacked;
    public Fighter lastActorAttackedBy;    
    public int lastActorAttackedUniqueID;
    public int lastActorAttackedByUniqueID;
    public int consecutiveAttacksOnLastActor;
    public int turnsInSamePosition;
    public List<AggroData> combatTargets;
    public List<AggroData> combatAllies;
    public InfluenceTurnData influenceTurnData;

    public int lastTurnDamaged;

    public AttackType lastAttackTypeReceived;

    public HealthBarScript healthBarScript;

    public Actor whoKilledMe;
    public DamageTypes killedByDamage;

    public float physicalWeaponDamageAddFlat;
    public float physicalWeaponDamageAddPercent;
    public float allDamageMultiplier;
    public float allMitigationAddPercent;

    //public Vector2 positionPreviousTurn;
    public bool deathProcessed;

    public bool checkForCustomAnimations;

    int turnsSinceLastCombatAction;
    public int TurnsSinceLastCombatAction
    {
        get
        {            
            return turnsSinceLastCombatAction;
        }
        set
        {
            /* if (GetActorType() == ActorTypes.HERO)
            {
                Debug.Log("Set to " + value);
            } */
            turnsSinceLastCombatAction = value;
        }
    }

    public int turnsSinceLastStun;
    public int turnsSinceLastDamaged;

    public List<int> summonedActorIDs;
    public List<Actor> summonedActors;

    // These will be DEPRECATED.
    public List<int> anchoredActorsIDs;
    public List<Actor> anchoredActors;
    // End deprecated

    public List<AnchoredActorData> anchoredActorData;

    public bool movedLastTurn;

    public Dictionary<string, int> effectsInflictedOnTurn;
    public HashSet<string> effectsInflictedStringKeys;

    public int waitTurnsRemaining = 0;
    public bool[,] visibleTilesArray;
    public bool[] actorFlags;
    public int[] flagData;

    public EffectScript lastSlowedEffect;
    public int turnsSinceLastSlow;

    public DamageTypes lastDamageTypeReceived;

    static List<string> effectsToRemove = new List<string>();
    static List<string> effectsKeys = new List<string>();

    public WrathBarScript wrathBarScript;

    public bool clearTrackDamageFlagAtEndOfTurn;

    static List<string> spritesToIgnore = new List<string>()
        {
            "TransLayer",
            "PlayerWrathBar",
            "PlayerIngameHealthBar",
            "DiagonalGrip",
            "SharaTKWeapon",
            "ExamineModeIcon",
            "AnalogArrowOverlay",
            "Diagonal Only Arrows"
        };

    public Fighter()
    {
        Init();
    }

    public void ClearAllFighterBattleData()
    {
        _cachedBattleData = new FighterBattleData();
    }

    public void SetCachedBattleData(FighterBattleData fbd)
    {
        _cachedBattleData = fbd;
    }

    public override void MarkAsDestroyed(bool ignoreHealth = false)
    {
        if (myStats.GetCurStat(StatTypes.HEALTH) > 0 && myStats.GetMaxStat(StatTypes.HEALTH) > 0)
        {
            if (ignoreHealth)
            {
                myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
                destroyed = true;
            }
            else
            {
                return;
            }
        }

        destroyed = true;
    }

    public void ResetTurnsSinceLastCombatAction()
    {
        TurnsSinceLastCombatAction = 0;
    }

    public void TickCombatStats()
    {
        TurnsSinceLastCombatAction++;
        turnsSinceLastDamaged++;
        turnsSinceLastSlow++;
        if (turnsSinceLastStun >= 0)
        {
            turnsSinceLastStun++;
        }
    }

    public bool CheckForStatusImmunity(StatusEffect se)
    {
        if (se == null)
        {
            Debug.Log("WARNING: Can't check for null status...");
            return false;
        }
        if (se.noRemovalOrImmunity) return false;

        if (se.refName == "status_charmed" && actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            return true;
        }

        foreach (StatusEffect mySE in myStats.GetAllStatuses())
        {
            if (mySE.reqWeaponType != WeaponTypes.ANY && mySE.reqWeaponType != myEquipment.GetWeaponType())
            {
                continue;
            }
            foreach (EffectScript eff in mySE.listEffectScripts)
            {
                if (eff.effectType == EffectType.IMMUNESTATUS)
                {
                    if (!(eff is ImmuneStatusEffect))
                    {
                        Debug.Log(eff.effectRefName + " " + eff.effectName + " thinks it is Immune Status, but is actually " + eff.GetType().ToString());
                        continue;
                    }
                    ImmuneStatusEffect ise = eff as ImmuneStatusEffect;
                    ise.selfActor = this;
                    bool immunity = ise.CheckForImmunity(se);
                    if (immunity) return true;
                }
            }
        }
        return false;
    }

    public bool ReadFighterStuffFromSave(XmlReader reader, bool readAllData = true)
    {
        bool successRead = true;
        if (readAllData)
        {
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                ReadFighterDataInternal(reader, readAllData);
            }
            reader.ReadEndElement();
        }
        else
        {
            successRead = ReadFighterDataInternal(reader, readAllData);
        }
        return successRead;
    }

    bool ReadFighterDataInternal(XmlReader reader, bool readOnEmpty)
    {
        bool successRead = true;
        string txt = "";

        //Debug.Log("FBD Read: " + reader.Name + " " + reader.NodeType);

        switch (reader.Name)
        {
            case "ct":
            case "actiontimer":
                txt = reader.ReadElementContentAsString();
                actionTimer = CustomAlgorithms.TryParseFloat(txt);
                break;
            case "tsincecmbt":
            case "turnssincelastcombataction":
                TurnsSinceLastCombatAction = reader.ReadElementContentAsInt();
                break;
            case "tsincedmg":
            case "turnssincelastdamaged":
                turnsSinceLastDamaged = reader.ReadElementContentAsInt();
                break;
            case "turnssincelaststun":
                turnsSinceLastStun = reader.ReadElementContentAsInt();
                break;
            case "ldmt":
                lastTurnDamaged = reader.ReadElementContentAsInt();
                break;
            case "ldm":
            case "lastdamagetypereceived":
                lastDamageTypeReceived = (DamageTypes)reader.ReadElementContentAsInt();
                break;
            case "diratkme":
                lastDirectionAttackedFrom = (Directions)reader.ReadElementContentAsInt();
                break;
            case "ldaf":
            case "lastdirectionattackedfrom":
                lastDirectionAttackedFrom = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                break;
            case "diriatk":
                lastDirectionUsedAttack = (Directions)reader.ReadElementContentAsInt();
                break;
            case "ldua":
            case "lastdirectionusedattack":
                lastDirectionUsedAttack = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                break;
            case "lmd":
            case "lastmoveddirection":
                lastMovedDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                break;
            case "lcd":
            case "lastcardinaldirection":
                lastCardinalDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                break;
            case "movedir":
            case "lastmove":
                lastMovedDirection = (Directions)reader.ReadElementContentAsInt();
                break;
            case "consecatks":
            case "consecutiveattacksonlastactor":
                consecutiveAttacksOnLastActor = reader.ReadElementContentAsInt();
                break;
            case "lastactorattackedid":
                lastActorAttackedUniqueID = reader.ReadElementContentAsInt();
                break;
            case "lastactorattackedbyid":
                lastActorAttackedByUniqueID = reader.ReadElementContentAsInt();
                break;
            case "dmg":
                // consolidated and easier to read
                string unparsed = reader.ReadElementContentAsString();
                string[] parsed = unparsed.Split('|');
                allDamageMultiplier = CustomAlgorithms.TryParseFloat(parsed[0]);
                allMitigationAddPercent = CustomAlgorithms.TryParseFloat(parsed[1]);
                break;
            case "alldamageaddpercent":
            case "alldamagemultiplier":
            case "dmgadd":
                txt = reader.ReadElementContentAsString();
                allDamageMultiplier = CustomAlgorithms.TryParseFloat(txt);
                break;
            case "allmitigationaddpercent":
            case "defadd":
                txt = reader.ReadElementContentAsString();
                allMitigationAddPercent = CustomAlgorithms.TryParseFloat(txt);
                break;
            case "anchdata":
                AnchoredActorData aadNew = new AnchoredActorData();
                aadNew.ReadFromSave(reader);
                anchoredActorData.Add(aadNew);
                break;
            case "effecthistory":
                //Debug.Log("Read effect history for " + actorRefName + " " + actorUniqueID);
                reader.ReadStartElement();
                //Debug.Log("Start element read. " + reader.Name + " " + reader.NodeType);
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    //Debug.Log(reader.Name + " " + reader.NodeType);
                    string key = reader.Name;
                    int value = reader.ReadElementContentAsInt();
                    AddEffectInflicted(key, value);
                }
                reader.ReadEndElement();
                break;
            case "combattargets":
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                }
                else
                {
                    reader.ReadStartElement();
                    if (reader.Name != "actor")
                    {
                        reader.Read();
                    }
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        AggroData ad = new AggroData();
                        reader.ReadStartElement(); // actor
                        txt = reader.ReadElementContentAsString();
                        ad.aggroAmount = CustomAlgorithms.TryParseFloat(txt);
                        ad.combatantUniqueID = reader.ReadElementContentAsInt();
                        reader.ReadEndElement();
                        if (ad.combatantUniqueID != actorUniqueID)
                        {
                            combatTargets.Add(ad);
                        }                        
                    }
                    reader.ReadEndElement();
                }

                break;
            case "combatallies":
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                }
                else
                {
                    reader.ReadStartElement();
                    if (reader.Name != "actor")
                    {
                        reader.Read();
                    }
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        AggroData ad = new AggroData();
                        reader.ReadStartElement(); // actor
                        txt = reader.ReadElementContentAsString();
                        ad.aggroAmount = CustomAlgorithms.TryParseFloat(txt);
                        ad.combatantUniqueID = reader.ReadElementContentAsInt();
                        reader.ReadEndElement();
                        combatAllies.Add(ad);
                    }
                    reader.ReadEndElement();
                }
                break;
            case "flags":
                reader.ReadStartElement();

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    while ((reader.NodeType == XmlNodeType.Whitespace) || (reader.NodeType == XmlNodeType.None))
                    {
                        reader.Read();
                    }
                    string flagName = reader.Name.ToUpperInvariant();
                    ActorFlags flag = (ActorFlags)Enum.Parse(typeof(ActorFlags), flagName);
                    actorFlags[(int)flag] = reader.ReadElementContentAsBoolean();
                    if (reader.Name == "data")
                    {
                        flagData[(int)flag] = reader.ReadElementContentAsInt();
                    }
                    else
                    {
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        else
                        {
                            //reader.Read();
                        }
                    }
                }
                reader.ReadEndElement();
                if (actorFlags[(int)ActorFlags.GREEDYFORQUEST])
                {
                    Monster mn = this as Monster;
                    mn.aggroRange = 0;
                    mn.AddAttribute(MonsterAttributes.GREEDY, 100);
                }
                break;
            case "bd":
            case "btld":
            case "battledata": // deprecated
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                }
                else
                {
                    cachedBattleData.ReadFromSave(reader, this);
                }
                break;
            case "sid":
            case "summonedactorid":
                if (summonedActorIDs == null)
                {
                    summonedActorIDs = new List<int>();
                }
                summonedActorIDs.Add(reader.ReadElementContentAsInt());
                break;
            case "physicalweapondamageaddflat":
                txt = reader.ReadElementContentAsString();
                physicalWeaponDamageAddFlat = CustomAlgorithms.TryParseFloat(txt);
                break;
            case "physicalweapondamageaddpercent":
                txt = reader.ReadElementContentAsString();
                physicalWeaponDamageAddPercent = CustomAlgorithms.TryParseFloat(txt);
                break;
            case "movedlastturn":
                movedLastTurn = reader.ReadElementContentAsBoolean();
                break;
            case "prevmv":
                movedLastTurn = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                break;
            case "turnsinsamepos":
                turnsInSamePosition = reader.ReadElementContentAsInt();
                break;
            case "lastarea":
            case "lastareavisited":
                lastAreaVisited = reader.ReadElementContentAsInt();
                break;
            default:
                if (readOnEmpty)
                {
                    reader.Read();
                }
                else
                {
                    successRead = false;
                }
                break;
        }
        return successRead;
    }

    public void WriteFighterStuffToSave(XmlWriter writer)
    {
        writer.WriteStartElement("fight");
        bool wroteAnything = false;

        if (turnsSinceLastStun >= 0)
        {
            writer.WriteElementString("turnssincelaststun", turnsSinceLastStun.ToString());
        }
        if (turnsInSamePosition > 0)
        {
            writer.WriteElementString("turnsinsamepos", ((int)turnsInSamePosition).ToString());
        }
        if (TurnsSinceLastCombatAction > 0 && TurnsSinceLastCombatAction <= 25)
        {
            writer.WriteElementString("tsincecmbt", TurnsSinceLastCombatAction.ToString());
        }
        if (turnsSinceLastDamaged >= 0 && turnsSinceLastDamaged <= 25)
        {
            writer.WriteElementString("tsincedmg", turnsSinceLastDamaged.ToString());
        }
        if (movedLastTurn)
        {
            writer.WriteElementString("prevmv", "1");
        }
        if (lastDirectionAttackedFrom != Directions.NORTH)
        {
            writer.WriteElementString("ldaf", ((int)lastDirectionAttackedFrom).ToString());
        }
        if (lastDirectionUsedAttack != Directions.NORTH)
        {
            writer.WriteElementString("diriatk", ((int)lastDirectionUsedAttack).ToString());
        }

        if (consecutiveAttacksOnLastActor > 0)
        {
            writer.WriteElementString("consecatks", consecutiveAttacksOnLastActor.ToString());
        }
        if (GetLastActorAttackedUniqueID() != 0)
        {
            writer.WriteElementString("lastactorattackedid", GetLastActorAttackedUniqueID().ToString());
        }
        if (GetLastActorAttackedByUniqueID() != 0)
        {
            writer.WriteElementString("lastactorattackedbyid", GetLastActorAttackedByUniqueID().ToString());
        }
        if (physicalWeaponDamageAddFlat != 0)
        {
            writer.WriteElementString("physicalweapondamageaddflat", physicalWeaponDamageAddFlat.ToString());
        }
        if (physicalWeaponDamageAddPercent != 1)
        {
            writer.WriteElementString("physicalweapondamageaddpercent", physicalWeaponDamageAddPercent.ToString());
        }

        if (lastTurnDamaged != 0)
        {
            writer.WriteElementString("ldmt", lastTurnDamaged.ToString());
        }

        if (allDamageMultiplier != 1f && allMitigationAddPercent != 1f)
        {
            string builder = allDamageMultiplier + "|" + allMitigationAddPercent;
            writer.WriteElementString("dmg", builder);
        }
        else
        {
            if (allDamageMultiplier != 1f)
            {
                writer.WriteElementString("dmgadd", allDamageMultiplier.ToString());
            }
            if (allMitigationAddPercent != 1f)
            {
                writer.WriteElementString("defadd", allMitigationAddPercent.ToString());
            }
        }

        if (lastMovedDirection != Directions.NORTH)
        {
            writer.WriteElementString("movedir", ((int)lastMovedDirection).ToString());
        }
        if (GetActorType() == ActorTypes.HERO)
        {
            writer.WriteElementString("lcd", ((int)lastCardinalDirection).ToString());
        }

        if (effectsInflictedOnTurn.Count > 0)
        {
            writer.WriteStartElement("effecthistory");
            foreach (string entry in effectsInflictedOnTurn.Keys)
            {
                writer.WriteElementString(entry, effectsInflictedOnTurn[entry].ToString());
            }
            writer.WriteEndElement();
        }

        bool anyFlags = false;
        for (int i = 0; i < (int)ActorFlags.COUNT; i++)
        {
            if (actorFlags[i])
            {
                anyFlags = true;
                break;
            }
        }
        if (anyFlags)
        {
            writer.WriteStartElement("flags");
            for (int i = 0; i < (int)ActorFlags.COUNT; i++)
            {
                if (actorFlags[i])
                {
                    writer.WriteElementString(((ActorFlags)i).ToString().ToLowerInvariant(), actorFlags[i].ToString().ToLowerInvariant());
                    if (flagData != null && flagData[i] != 0)
                    {
                        writer.WriteElementString("data", flagData[i].ToString().ToLowerInvariant());
                    }
                }
            }
            writer.WriteEndElement();
        }

        if (GetNumCombatAllies() > 0)
        {
            initialized = false;


            foreach (AggroData ad in combatAllies)
            {
                if (ad == null) continue;
                if (ad.combatant == null) continue;

                if (!initialized)
                {
                    writer.WriteStartElement("combatallies");
                    initialized = true;
                }
                writer.WriteStartElement("actor");
                writer.WriteElementString("aggroamount", ad.aggroAmount.ToString());
                writer.WriteElementString("actoruniqueid", ad.combatant.actorUniqueID.ToString());
                writer.WriteEndElement();
            }

            if (initialized)
            {
                writer.WriteEndElement();
            }
        }

        if (GetNumCombatTargets() > 0)
        {
            bool initialized = false;


            foreach (AggroData ad in combatTargets)
            {
                if (ad == null) continue;
                if (ad.combatant == null) continue;

                if (!initialized)
                {
                    writer.WriteStartElement("combattargets");
                    initialized = true;
                }

                writer.WriteStartElement("actor");
                writer.WriteElementString("aggroamount", ad.aggroAmount.ToString());
                writer.WriteElementString("actoruniqueid", ad.combatant.actorUniqueID.ToString());
                writer.WriteEndElement();
            }
            if (initialized)
            {
                writer.WriteEndElement();
            }

        }

        if (summonedActorIDs != null && summonedActorIDs.Count > 0)
        {
            foreach (int h in summonedActorIDs)
            {
                if (GameMasterScript.DoesActorExistByID(h))
                {
                    writer.WriteElementString("sid", h.ToString());
                }                
            }
        }

        List<AnchoredActorData> aadToRemove = new List<AnchoredActorData>();
        foreach (AnchoredActorData aad in anchoredActorData)
        {
            if (!aad.WriteToSave(writer))
            {
                aadToRemove.Add(aad);
            }
        }
        foreach (AnchoredActorData aad in aadToRemove)
        {
            anchoredActorData.Remove(aad);
        }

        if (lastAreaVisited != MapMasterScript.FILL_AREA_ID && lastAreaVisited != areaID)
        {
            writer.WriteElementString("lastarea", lastAreaVisited.ToString());
        }

        if ((int)lastDamageTypeReceived != 0)
        {
            writer.WriteElementString("ldm", ((int)(lastDamageTypeReceived)).ToString());
        }

        if (actionTimer > 0)
        {
            writer.WriteElementString("ct", actionTimer.ToString());
        }

        bool wasDirty = false;
        if (cachedBattleData.IsDirty())
        {
            cachedBattleData.SetClean(); // No need to recalculate battle data on save.
            wasDirty = true;
        }
        
        cachedBattleData.WriteToSave(writer);
        if (wasDirty)
        {
            cachedBattleData.SetDirty();
        }

        writer.WriteEndElement();
    }

    public void SetHealthBarVisibility(bool state)
    {
        if (healthBarScript != null)
        {
            healthBarScript.enabled = state;
        }
    }

    // For some reason, we're having issues where actors are getting anchored that should NOT be anchored.
    // Don't know how or why, so we need to validate. anchorParent is the thing that *this* actor is following.
    public void LinkAndValidateAllAnchoredActors(bool linkFromMasterDict, Actor checkSpecific = null)
    {
        if (anchoredActorData.Count == 0) return;
        List<AnchoredActorData> aadToRemove = new List<AnchoredActorData>();
        foreach (AnchoredActorData aad in anchoredActorData)
        {
            Actor link = aad.actorRef;

            if (checkSpecific != null && checkSpecific != link)
            {
                continue;
            }

            if (linkFromMasterDict)
            {
                link = GameMasterScript.gmsSingleton.TryLinkActorFromDict(aad.actorID);
            }

            if (link == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Could not load originating anchored actor for " + displayName + " " + actorUniqueID + " " + actorRefName + ": " + aad.actorID);
                aadToRemove.Add(aad);
                continue;
            }
            if (link.actorRefName != aad.refName)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " linked actor " + link.actorRefName + " " + link.actorUniqueID + " doesn't match expected name " + aad.refName);
                aadToRemove.Add(aad);
                continue;
            }
            if (link.GetActorType() != aad.actorType)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " linked actor " + link.actorRefName + " " + link.actorUniqueID + " doesn't match expected type " + aad.actorType);
                aadToRemove.Add(aad);
                continue;
            }
            if (link.GetActorType() == ActorTypes.ITEM)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " linked actor " + link.actorRefName + " " + link.actorUniqueID + " is an item. Remove it.");
                aadToRemove.Add(aad);
                continue;
            }
            if (link.actorfaction == Faction.DUNGEON)
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " linked actor " + link.actorRefName + " " + link.actorUniqueID + " is dungeon faction.");
                aadToRemove.Add(aad);
                continue;
            }
            if (link.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = link as Monster;
                if (MetaProgressScript.IsMonsterInCorral(mn) || MetaProgressScript.IsMonsterInCorralByID(aad.actorID))
                {
                    Debug.Log(actorRefName + " is trying to add an anchored actor, " + mn.actorRefName + " " + mn.actorUniqueID + " which is a monster that SHOULD be in the corral.");
                    aadToRemove.Add(aad);
                    continue;
                }
            }
            aad.actorRef = link;
            if (aad.actorRef.anchor != this) 
            {                
                aad.actorRef.SetAnchor(this);
            }
            //Debug.Log("Linked up AAD " + aad.refName + " " + aad.actorID + " for " + actorRefName);
        }

        foreach(AnchoredActorData aad in aadToRemove) 
        {
            anchoredActorData.Remove(aad);
        }
    }

    public virtual void ValidateAndFixStats(bool writeFixedStats)
    {
        // Verify hero passives that are auto-equipped are in effect.
        // Below legacy block should no longer be needed
        /* if (GetActorType() == ActorTypes.HERO)
        {
            HeroPC theHero = this as HeroPC;
            foreach(AbilityScript abil in theHero.myAbilities.GetAbilityList())
            {
                if (!abil.passiveAbility) continue;
                if (!abil.usePassiveSlot && !abil.passiveEquipped)
                {
                    Debug.Log(abil.refName + " should be auto-equipped, but it is not. Equipping now.");
                    myAbilities.EquipPassiveAbility(abil, true);
                }
            }
        } */

        float[] baseStatArray = new float[(int)StatBlock.expandedCoreStats.Length];
        float[] expectedStatArray = new float[(int)StatBlock.expandedCoreStats.Length];
        for (int i = 0; i < baseStatArray.Length; i++)
        {
            baseStatArray[i] = myStats.GetStat((StatTypes)i, StatDataTypes.TRUEMAX);
        }

        float[] changesFromStatusEffects = new float[(int)StatBlock.expandedCoreStats.Length];

        float[] expectedResistances = new float[(int)DamageTypes.COUNT];
        float[] expectedElementalDamage = new float[(int)DamageTypes.COUNT];
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            expectedResistances[i] = 1f;
            expectedElementalDamage[i] = 1f;
        }

        float expectedDamageMultiplier = 1f;
        float expectedMitigationAddPercent = 1f;

        // If we are the hero, any damage mitigation or multipliers should be accounted for in our statuses. There is no other source.
        // Monsters on the other hand can have these values raised by hand for any number of reasons.
        // We'll make sure to adjust this as we go through our status effects.

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            //if (!se.CheckRunTriggerOn(StatusTrigger.ONADD)) continue; // This should only apply to non-permanent effects?
            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.ALTERBATTLEDATA)
                {
                    AlterBattleDataEffect abde = eff as AlterBattleDataEffect;
                    expectedResistances[(int)DamageTypes.FIRE] += abde.changeFireResist;
                    expectedResistances[(int)DamageTypes.WATER] += abde.changeWaterResist;
                    expectedResistances[(int)DamageTypes.LIGHTNING] += abde.changeLightningResist;
                    expectedResistances[(int)DamageTypes.POISON] += abde.changePoisonResist;
                    expectedResistances[(int)DamageTypes.SHADOW] += abde.changeShadowResist;
                    expectedResistances[(int)DamageTypes.PHYSICAL] += abde.changePhysicalResist;

                    expectedElementalDamage[(int)DamageTypes.FIRE] += abde.changeFireDamage;
                    expectedElementalDamage[(int)DamageTypes.WATER] += abde.changeWaterDamage;
                    expectedElementalDamage[(int)DamageTypes.LIGHTNING] += abde.changeLightningDamage;
                    expectedElementalDamage[(int)DamageTypes.POISON] += abde.changePoisonDamage;
                    expectedElementalDamage[(int)DamageTypes.SHADOW] += abde.changeShadowDamage;
                    expectedElementalDamage[(int)DamageTypes.PHYSICAL] += abde.changePhysicalDamage;

                    expectedDamageMultiplier += abde.changePercentAllDamage;
                    expectedMitigationAddPercent += abde.changePercentAllMitigation;
                    continue;
                }

                if (eff.effectType != EffectType.CHANGESTAT) continue;
                ChangeStatEffect cse = eff as ChangeStatEffect;
                if (!cse.reverseOnEnd)
                {
                    //Debug.Log(cse.effectName + " " + cse.effectRefName + " should not be considered.");
                    continue;
                }
                if (cse.stat == StatTypes.HEALTH || cse.stat == StatTypes.ENERGY || cse.stat == StatTypes.STAMINA)
                {
                    if (cse.statData == StatDataTypes.CUR)
                    {
                        continue;
                    }
                }
                //Debug.Log(cse.effectName + " " + cse.effectRefName + " " + cse.stat + " " + cse.statData + " " + cse.accumulatedAmount + " " + cse.effectPower + " " + cse.effectEquation);
                //Debug.Log(cse.reverseOnEnd);
                changesFromStatusEffects[(int)cse.stat] += cse.accumulatedAmount;
            }
        }

        if (GetActorType() == ActorTypes.HERO)
        {
            if (!CustomAlgorithms.CompareFloats(expectedDamageMultiplier, allDamageMultiplier))
            {
//                Debug.Log("We have " + allDamageMultiplier + " all DMG multiplier but expected " + expectedDamageMultiplier);
                allDamageMultiplier = expectedDamageMultiplier;
            }
            if (!CustomAlgorithms.CompareFloats(expectedMitigationAddPercent, allMitigationAddPercent))
            {
//                Debug.Log("We have " + allMitigationAddPercent + " all mitigation multiplier but expected " + expectedMitigationAddPercent);
                allMitigationAddPercent = expectedMitigationAddPercent;
            }
        }

        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            float convertedValue = expectedResistances[i];
            //Debug.Log((DamageTypes)i + " " + expectedResistances[i] + " " + cachedBattleData.resistanceExternalMods[i]);
            if (!CustomAlgorithms.CompareFloats(convertedValue, cachedBattleData.resistanceExternalMods[i]))
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " " + "<color=yellow>For resistance " + (DamageTypes)i + ", expected " + convertedValue + " but saved value is " + cachedBattleData.resistanceExternalMods[i] + "</color>");
                if (writeFixedStats)
                {
                    cachedBattleData.resistanceExternalMods[i] = convertedValue;
                }
            }

            convertedValue = expectedElementalDamage[i];
            if (!CustomAlgorithms.CompareFloats(convertedValue, cachedBattleData.damageExternalMods[i]))
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " " + "<color=yellow>For damage type " + (DamageTypes)i + ", expected " + convertedValue + " but saved value is " + cachedBattleData.damageExternalMods[i] + "</color>");
                if (writeFixedStats)
                {
                    cachedBattleData.damageExternalMods[i] = convertedValue;
                }
            }
        }

        for (int i = 0; i < 3; i++) // health, stamina, energy
        {
            expectedStatArray[i] = baseStatArray[i] + changesFromStatusEffects[i];
            if (!CustomAlgorithms.CompareFloats(expectedStatArray[i], myStats.GetMaxStat((StatTypes)i)))
            {
                Debug.Log(actorRefName + " " + actorUniqueID + " " + "<color=yellow>For max stat " + (StatTypes)i + ", expected " + expectedStatArray[i] + " (base " + baseStatArray[i] + ") but loaded value is " + myStats.GetMaxStat((StatTypes)i) + "</color>");
                if (writeFixedStats)
                {
                    myStats.SetStat((StatTypes)i, expectedStatArray[i], StatDataTypes.MAX, true, false);
                }
            }
        }

        for (int i = 3; i < baseStatArray.Length; i++)
        {
            expectedStatArray[i] = baseStatArray[i] + changesFromStatusEffects[i];
            if (!CustomAlgorithms.CompareFloats(expectedStatArray[i], myStats.GetMaxStat((StatTypes)i)))
            {
                //Debug.Log(actorRefName + " " + actorUniqueID + " " + "<color=yellow>For max stat " + (StatTypes)i + ", expected " + expectedStatArray[i] + " (base " + baseStatArray[i] + ") but loaded value is " + myStats.GetMaxStat((StatTypes)i) + "</color>");
                if (writeFixedStats)
                {
                    myStats.SetStat((StatTypes)i, expectedStatArray[i], StatDataTypes.MAX, true, false);
                }
            }
            if (!CustomAlgorithms.CompareFloats(expectedStatArray[i], myStats.GetCurStat((StatTypes)i)))
            {
                //Debug.Log(actorRefName + " " + actorUniqueID + " " + "<color=yellow>For cur stat " + (StatTypes)i + ", expected " + expectedStatArray[i] + " (base " + baseStatArray[i] + ") but loaded value is " + myStats.GetCurStat((StatTypes)i) + "</color>");
                if (writeFixedStats)
                {
                    myStats.SetStat((StatTypes)i, expectedStatArray[i], StatDataTypes.CUR, true, false);
                }
            }

        }

        if (writeFixedStats)
        {
            CalculateBattleData_Internal();
        }
    }

    public override void UpdateSpriteOrder(bool turnEnd = false)
    {
        base.UpdateSpriteOrder(turnEnd);
        if (elemAuraObject != null && objectSet)
        {
            elemAuraObject.UpdateSpriteOrder(mySpriteRenderer.sortingOrder);
        }
    }

    public virtual void RepositionWrathBarIfNeeded()
    {

    }

    public virtual void EnableWrathBarIfNeeded()
    {
        VerifyWrathBarIsActive();
        wrathBarScript.ToggleWrathBar(false);
    }

    public void VerifyWrathBarIsActive()
    {
        if (wrathBarScript == null) // || !wrathBarScript.gameObject.activeSelf)
        {
            GameObject wrathBar = GameMasterScript.TDInstantiate("PlayerWrathBar");
            wrathBarScript = wrathBar.GetComponent<WrathBarScript>();
            wrathBarScript.gameObject.transform.SetParent(GetObject().transform);
            wrathBarScript.gameObject.transform.localPosition = new Vector3(0f, -0.84f, 1f);
        }
    }

    public void CreateNewElementalAura(DamageTypes dType, bool forceSpawn = false)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted && !forceSpawn) return;

        if (dungeonFloor != MapMasterScript.activeMap.floor) return;

        if (dType == DamageTypes.PHYSICAL) return;

        if (elemAuraObject != null && elemAuraObject.gameObject.activeSelf)
        {
            elemAuraObject.StopAndDie();
        }

        elemAuraObject = GameMasterScript.TDInstantiate("ElementalAura").GetComponent<ElementalAuraManager>();
        elemAuraObject.Initialize(dType, mySpriteRenderer);
        elemAuraObject.gameObject.transform.SetParent(GetObject().transform);
        elemAuraObject.gameObject.transform.localPosition = Vector2.zero;
        elemAuraObject.gameObject.transform.localScale = Vector3.one;

    }

    public int GetMaxAttackRange(Weapon w = null)
    {
        Weapon wToEvaluate = myEquipment.GetWeapon();
        if (w != null)
        {
            wToEvaluate = w;
        }
        int baseRange = wToEvaluate.range;
        //if (GetActorType() == ActorTypes.HERO && baseRange > 1 && myStats.CheckHasStatusName("status_overdraw")) //wToEvaluate.HasModByRef("mm_overdraw"))
        if (GetActorType() == ActorTypes.HERO && baseRange > 1 && GameMasterScript.heroPCActor.IsOverdrawingActiveOnWeaponOrPairedQuiver(wToEvaluate))
        {
            baseRange++;
        }
        return baseRange;
    }

    public void DoAbilityAnimation(TurnData td, Directions dir1 = Directions.NEUTRAL, Directions dir2 = Directions.NEUTRAL)
    {
        AbilityScript abil = td.tAbilityToTry;
        Item itemUsed = td.usedItem;
        if (dir1 == Directions.NEUTRAL)
        {
            dir1 = lastMovedDirection;
        }
        if (dir2 == Directions.NEUTRAL)
        {
            dir2 = lastCardinalDirection;
        }
        //Debug.Log(abil.refName + " use anim is " + abil.useAnimation);
        switch (abil.useAnimation)
        {
            case "UseItem":
                myAnimatable.SetAnimWithDirectionalBackup("UseItem", "Attack", dir1, dir2);
                if (itemUsed != null)
                {
                    TDVisualEffects.PopupSprite(itemUsed.spriteRef, GetObject().transform, true, itemUsed.GetSpriteForUI());
                }
                else if (!string.IsNullOrEmpty(abil.spritePopInfo))
                {
                    TDVisualEffects.ParseAndExecuteSpritePop(abil.spritePopInfo, GetObject().transform);
                }
                break;
            case "Attack":
                myAnimatable.SetAnimDirectional("Attack", dir1, dir2);
                break;
            case "Walk":
                myAnimatable.SetAnimDirectional("Walk", dir1, dir2);
                myAnimatable.speedMultiplier = 2.0f;
                break;
        }
    }

    public void TickEffectCooldownCounters()
    {
        myStats.TickRegenCounter();
        effectsToRemove.Clear();
        //effectsKeys = effectsInflictedOnTurn.Keys.ToList<string>();

        foreach (string entry in effectsInflictedStringKeys)
        //foreach (string key in effectsInflictedOnTurn.Keys) 
        {
            effectsInflictedOnTurn[entry]++;
            if (effectsInflictedOnTurn[entry] >= 51)
            {
                effectsToRemove.Add(entry);
            }
        }
        if (effectsToRemove.Count > 0)
        {
            foreach (string str in effectsToRemove)
            {
                effectsInflictedOnTurn.Remove(str);
                effectsInflictedStringKeys.Remove(str);
            }
        }
    }

    public bool CanSeeActor(Actor check)
    {
        if (visibleTilesArray == null) return false;
        int x = (int)check.GetPos().x;
        int y = (int)check.GetPos().y;
        if (x >= visibleTilesArray.Length ||
            x < 0 ||
            y >= visibleTilesArray.LongLength ||
            y < 0)
        {
            return false;
        }
        return visibleTilesArray[x, y];
    }

    public void SetFlagData(ActorFlags f, int value)
    {
        if (flagData == null || actorFlags == null)
        {
            flagData = new int[(int)ActorFlags.COUNT];
            actorFlags = new bool[(int)ActorFlags.COUNT];
        }
        flagData[(int)f] = value;
    }

    public int GetFlagData(ActorFlags f)
    {
        if (flagData == null)
        {
            return 0;
        }
        return flagData[(int)f];
    }

    public bool CheckFlag(ActorFlags f)
    {
        if (actorFlags == null)
        {
            return false;
        }
        return actorFlags[(int)f];
    }

    public void SetFlag(ActorFlags f, bool value)
    {
        if ((actorFlags == null) || (flagData == null))
        {
            actorFlags = new bool[(int)ActorFlags.COUNT];
            flagData = new int[(int)ActorFlags.COUNT];
        }
        actorFlags[(int)f] = value;
        flagData[(int)f] = 0;
    }

    public void ClearCombatTargets()
    {
        if (combatTargets != null)
        {
            combatTargets.Clear();
        }
    }

    public void ClearCombatAllies()
    {
        if (combatAllies != null)
        {
            combatAllies.Clear();
        }
    }

    public int GetNumCombatTargets()
    {
        if (combatTargets == null) return 0;
        else
        {
            return combatTargets.Count;
        }
    }

    public int GetNumCombatAllies()
    {
        if (combatAllies == null) return 0;
        else
        {
            return combatAllies.Count;
        }
    }

    public void ClearVisibleTiles()
    {
        int xMax = visibleTilesArray.GetLength(0);
        int yMax = visibleTilesArray.GetLength(1);
        for (int x = 0; x < xMax; x++)
        {
            for (int y = 0; y < yMax; y++)
            {
                visibleTilesArray[x, y] = false;
            }
        }
    }

    public void ChangeCT(float amount)
    {
        if (this == GameMasterScript.heroPCActor)
        {
            // Don't bother with CT in safe zones??
        }

        actionTimer += amount;
        if (actionTimer >= 300f)
        {
            actionTimer = 300f;
        }
        if (actionTimer <= -50f)
        {
            actionTimer = -50f;
        }
    }

    //Removes the destructible from gameplay right away, it does not get another turn
    public override void RemoveImmediately()
    {
        base.RemoveImmediately();
        myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
        myStats.SetStat(StatTypes.CHARGETIME, 0f, StatDataTypes.CUR, false);
    }

    public void AddSummon(Actor act)
    {
        if (act == null)
        {
            Debug.Log("Warning: Trying to add null summon to " + actorRefName + " " + actorUniqueID);
            return;
        }
        if (act.GetActorType() == ActorTypes.ITEM)
        {
            Debug.Log("Do not add item to player as summon! " + act.actorRefName + " " + act.actorUniqueID);
            return;
        }
        if (summonedActors == null)
        {
            summonedActors = new List<Actor>();
            summonedActorIDs = new List<int>();
        }

        if (!summonedActors.Contains(act))
        {
            summonedActors.Add(act);
        }
        if (!summonedActorIDs.Contains(act.actorUniqueID))
        {
            summonedActorIDs.Add(act.actorUniqueID);
        }

        OnAddSummon(act);

    }


    protected override bool HasAnchor(Actor act)
    {
        foreach (AnchoredActorData aad in anchoredActorData)
        {
            if (aad.actorRef == act)
            {
                return true;
            }
            if (aad.actorID == act.actorUniqueID)
            {
                return true;
            }
        }

        return false;
    }

    public void AddAnchor(Actor act)
    {
        if (HasAnchor(act)) return;

        AnchoredActorData aad = new AnchoredActorData();
        aad.actorRef = act;
        aad.actorID = act.actorUniqueID;
        aad.actorType = act.GetActorType();
        aad.refName = act.actorRefName;
        anchoredActorData.Add(aad);
        LinkAndValidateAllAnchoredActors(false, act);

    }

    /// <summary>
    /// Disconnects 'act' from us, so 'act' will no longer follow our movements
    /// </summary>
    /// <param name="act"></param>
    public void RemoveAnchor(Actor act)
    {
        AnchoredActorData aadToRemove = null;
        foreach (AnchoredActorData aad in anchoredActorData)
        {
            if (aad.actorRef == act)
            {
                aadToRemove = aad;
                break;
            }
        }
        if (aadToRemove == null)
        {
            //Debug.Log("Warning: " + actorRefName + " " + actorUniqueID + " trying to remove " + act.actorRefName + " " + act.actorUniqueID + " but does not have this anchor.");
            return;
        }
        anchoredActorData.Remove(aadToRemove);

    }

    public void DestroyAllSummonsByRef(string summonRef)
    {
        List<Actor> toDestroy = new List<Actor>();
        foreach (Actor act in summonedActors)
        {
            if (act.actorRefName == summonRef)
            {
                toDestroy.Add(act);
            }
        }
        foreach (Actor act in toDestroy)
        {
            RemoveSummon(act);
            switch (act.GetActorType())
            {
                case ActorTypes.MONSTER:
                    Monster mn = act as Monster;
                    mn.myStats.SetStat(StatTypes.HEALTH, 0, StatDataTypes.CUR, true);
                    break;
                case ActorTypes.DESTRUCTIBLE:
                    Destructible dt = act as Destructible;
                    dt.RemoveImmediately();
                    GameMasterScript.AddToDeadQueue(dt);
                    break;
            }
        }
    }

    public void RemoveSummon(Actor act)
    {
        if (summonedActors == null)
        {
            Debug.Log(displayName + " at " + GetPos() + " is trying to remove summond actor " + act.actorRefName + " " + act.GetPos() + " but doesn't have any summoned actors.");
            return;
        }
        //Debug.Log(actorUniqueID + " removing summon " + act.actorUniqueID + " " + act.actorRefName);
        summonedActors.Remove(act);
        summonedActorIDs.Remove(act.actorUniqueID);

        OnRemoveSummon(act);
    }

    public Actor GetSummonByRef(string refName)
    {
        if (summonedActors == null)
        {
            return null;
        }
        foreach (Actor act in summonedActors)
        {
            if (act.actorRefName == refName)
            {
                return act;
            }
        }
        return null;
    }

    public int CountSummonRefs(string refName)
    {
        if (summonedActors == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Actor act in summonedActors)
        {
            if (act.actorRefName == refName)
            {
                count++;
            }
        }

        return count;
    }

    public bool CheckSummonRefs(string refName)
    {
        if (summonedActors == null)
        {
            return false;
        }
        foreach (Actor act in summonedActors)
        {
            if (act.actorRefName == refName)
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckSummon(Actor act)
    {
        if (summonedActors == null)
        {
            return false;
        }
        if (summonedActors.Contains(act)) return true;
        return false;
    }

    public int GetLastActorAttackedUniqueID()
    {
        if (lastActorAttacked == null)
        {
            return 0;
        }
        if (lastActorAttacked.destroyed || !lastActorAttacked.myStats.IsAlive())
        {
            return 0;
        }
        return lastActorAttacked.actorUniqueID;
    }

    public int GetLastActorAttackedByUniqueID()
    {
        if (lastActorAttackedBy == null)
        {
            return 0;
        }
        if (lastActorAttackedBy.destroyed || !lastActorAttackedBy.myStats.IsAlive())
        {
            return 0;
        }
        return lastActorAttackedBy.actorUniqueID;
    }

    protected override void Init()
    {
        if (initialized)
        {
            return;
        }
        base.Init();
        damageTakenLastThreeTurns = new float[3];
        turnsSinceLastStun = -1;
        lastDamageTypeReceived = DamageTypes.PHYSICAL;
        effectsInflictedOnTurn = new Dictionary<string, int>();
        effectsInflictedStringKeys = new HashSet<string>();
        _cachedBattleData = new FighterBattleData();
        TurnsSinceLastCombatAction = 999;
        turnsSinceLastDamaged = 999;
        combatTargets = new List<AggroData>();
        combatAllies = new List<AggroData>();
        influenceTurnData = new InfluenceTurnData();
        consecutiveAttacksOnLastActor = 0;
        movedLastTurn = false;
        previousPosition = Vector2.zero;
        anchoredActorData = new List<AnchoredActorData>();

        // NEW 11/23/2017 - Shouldn't all fighters be collidable?
        monsterCollidable = true;
        playerCollidable = true;
    }

    public void RemoveAlly(Actor act)
    {
        if (combatAllies.Count == 0)
        {
            return;
        }
        AggroData thingToRemove = null;
        foreach (AggroData ad in combatAllies)
        {
            if (ad.combatant == act)
            {
                thingToRemove = ad;
            }
        }

        if (thingToRemove != null) combatAllies.Remove(thingToRemove);

    }

    public void RemoveTarget(Actor act)
    {
        if (GetNumCombatTargets() == 0)
        {
            return;
        }
        AggroData thingToRemove = null;
        foreach (AggroData ad in combatTargets)
        {
            if (ad.combatant == act)
            {
                thingToRemove = ad;
            }
        }
        if (thingToRemove != null) combatTargets.Remove(thingToRemove);
    }

    public bool CheckTarget(Actor act)
    {
        foreach (AggroData ad in combatTargets)
        {
            if (ad.combatant == act)
            {
                return true;
            }
        }
        return false;
    }

    public void SetAggro(Actor act, float aggroAmount)
    {
        foreach (AggroData ad in combatTargets)
        {
            if (ad.combatant == act)
            {
                // Add aggro here
                ad.aggroAmount = aggroAmount;
                return;
            }
        }
        Debug.Log(act.actorRefName + " not found in " + actorRefName + "'s target list");
    }

    public float GetTargetAggro(Actor act)
    {
        foreach (AggroData ad in combatTargets)
        {
            if (ad.combatant == act)
            {
                return ad.aggroAmount;
            }
        }
        return -1f;
    }

    public float GetAllyAggro(Actor act)
    {
        foreach (AggroData ad in combatAllies)
        {
            if (ad.combatant == act)
            {
                return ad.aggroAmount;
            }
        }
        return -1f;
    }


    public void SymmetricalRemoveTargetsAndAllies()
    {
        if (combatTargets == null) return;
        if (combatAllies == null) return;
        foreach (AggroData ad in combatTargets)
        {
            if (ad == null || ad.combatant == null) continue;
            if (ad.combatant.actorUniqueID == actorUniqueID) continue; // Trying to remove self from own aggro list modifies the enumerable = error
            ad.combatant.RemoveTarget(this);
        }
        foreach (AggroData at in combatAllies)
        {
            if (at == null || at.combatant == null) continue;
            if (at.combatant.actorUniqueID == actorUniqueID) continue; // Trying to remove self from own aggro list modifies the enumerable = error
            at.combatant.RemoveAlly(this);
        }
    }

    public bool CheckAlly(Actor act)
    {
        foreach (AggroData ad in combatAllies)
        {
            if (ad.combatant == act)
            {
                return true;
            }
        }
        return false;
    }

    public void AddAlly(Actor target)
    {
        if (target == this) return;
        if (target == null) return;

        if (CheckTarget(target))
        {
            if (GetTargetAggro(target) > 50f) // This thing can't be an ally, we're fighting it and have a lot of aggro with it.
            {
                return;
            }
        }

        if (!CheckAlly(target))
        {
            //Debug.Log("Adding ally to " + this.actorRefName + " is " + target.actorRefName);
            AggroData ad = new AggroData();
            ad.combatant = target as Fighter;
            ad.aggroAmount = 50f; // Base aggro amount?
            ad.turnsSinceCombatAction = 0;
            ad.combatantUniqueID = target.actorUniqueID;
            combatAllies.Add(ad);
            ad.combatant.AddTarget(this); // Not recursive I hope?
        }
    }

    public void AddTarget(Actor target)
    {
        /* if (GetActorType() == ActorTypes.MONSTER) {
    		Monster mon = this as Monster;
    		if ((mon.friendlyToHero) && (target == GameMasterScript.heroPCActor)) {
    			// Refuse to engage in combat with hero. Is there a better way to do this?
    			return;
    		}
    	} */
        if (target == this) return;
        if (target == null) return;

        if (!target.IsFighter()) return;

        if (target.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Debug.Log("WARNING: Do NOT add destructible target to " + actorRefName + " " + actorUniqueID);
            return;
        }

        if (target.actorUniqueID == actorUniqueID)
        {
            return;
        }

        if (!CheckTarget(target))
        {
            AggroData ad = new AggroData();
            ad.combatant = target as Fighter;
            ad.combatantUniqueID = target.actorUniqueID;
            ad.aggroAmount = 50f; // Base aggro amount?
            ad.turnsSinceCombatAction = 0;
            combatTargets.Add(ad);
            ad.combatant.AddTarget(this); // Not recursive I hope?

            // Now let's build ally list. For each ENEMY of my enemy (target)
            /* foreach(AggroData enemy in ad.combatant.combatTargets)
            {
                // Add to MY ally list.
                if (enemy.combatant.actorfaction != actorfaction) continue;
                Debug.Log(this.actorRefName + " wants to add ally " + enemy.combatant.actorRefName);
                AddAlly(enemy.combatant);
            } */
        }
    }

    public bool TakeDamage(float dmgAmount, DamageTypes dType)
    {
        bool isHero = GetActorType() == ActorTypes.HERO;
        
        //Debug.Log("Input damage " + dmgAmount + " " + dType);
        float startHealth = myStats.GetCurStat(StatTypes.HEALTH);
        // Do we need dType here?

        GameMasterScript.gmsSingleton.SetTempFloatData("dmg", dmgAmount);

        myStats.CheckRunAndTickAllStatuses(StatusTrigger.DAMAGE);

        dmgAmount = GameMasterScript.gmsSingleton.ReadTempFloatData("dmg");

        dmgAmount = Mathf.Floor(dmgAmount);
        lastDamageTypeReceived = dType;

        if (dType == DamageTypes.FIRE && GetActorType() == ActorTypes.MONSTER && actorRefName == "mon_fungalcolumn")
        {
            myStats.ChangeStatAndSubtypes(StatTypes.ENERGY, 5f, StatDataTypes.ALL);
        }

        if (CheckFlag(ActorFlags.TRACKDAMAGE))
        {
            SetFlagData(ActorFlags.TRACKDAMAGE, (GetFlagData(ActorFlags.TRACKDAMAGE) + (int)dmgAmount));
        }

        bool monsterIsKnockedOut = false;

        if (GetActorType() == ActorTypes.MONSTER && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
        {
            Monster monMe = this as Monster;
            monsterIsKnockedOut = monMe.surpressTraits;
            if (monMe.isInCorral)
            {
                dmgAmount = 0;
                return true;
            }
        }

        if (GetActorType() == ActorTypes.MONSTER && actorfaction == Faction.PLAYER && dmgAmount >= myStats.GetCurStat(StatTypes.HEALTH) && actorUniqueID != GameMasterScript.heroPCActor.actorUniqueID)
        {
            bool rollSuccess = UnityEngine.Random.Range(0, 1f) <= 0.20f;
            bool hasStatus = GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_floramancer_tier1_resummon");
            if (rollSuccess && hasStatus)
            {
                myStats.SetStat(StatTypes.HEALTH, myStats.GetMaxStat(StatTypes.HEALTH), StatDataTypes.CUR, true);
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("log_pet_healtofull_recycle");
                BattleTextManager.NewText(StringManager.GetExcitedString("misc_fullheal"), GetObject(), Color.green, 1.5f);
                dmgAmount = 0;
            }
        }

        if (CombatManagerScript.bufferedCombatData != null)
        {
            CombatManagerScript.bufferedCombatData.lastDamageAmountReceived = dmgAmount;
        }

        bool isDummy = actorfaction != Faction.PLAYER && actorRefName == "mon_targetdummy";

        if (!isDummy)
        {

            // extra protection against dying during duel.......
            if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                if (GetActorType() == ActorTypes.HERO || actorfaction == Faction.ENEMY)// && myStats.CheckHasStatusName("pet_duel"))
                {
                    if (dmgAmount >= myStats.GetCurStat(StatTypes.HEALTH))
                    {
                        dmgAmount = myStats.GetCurStat(StatTypes.HEALTH) - 2f;
                    }
                }
            }

            switch (actorRefName)
            {
                case "mon_harmlessfungaltoad":
                    if (!monsterIsKnockedOut)
                    {
                        if (dmgAmount >= myStats.GetCurStat(StatTypes.HEALTH))
                        {
                            dmgAmount = myStats.GetCurStat(StatTypes.HEALTH) - 2f;
                        }
                        myStats.ChangeStat(StatTypes.HEALTH, -1 * dmgAmount, StatDataTypes.CUR, true, false);
                    }
                    break;
                default:
                    myStats.ChangeStat(StatTypes.HEALTH, -1 * dmgAmount, StatDataTypes.CUR, true, false);
                    break;
            }
        }

        myStats.CheckRunAndTickAllStatuses(StatusTrigger.TAKEDAMAGE);

        if (isHero)
        {
            ProcessHeroDamage(dmgAmount, startHealth, dType);
        }
        else if (GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = this as Monster;

            GameEventsAndTriggers.CheckForEventsOnMonsterDamage(mn);

            if (mn.actorfaction != Faction.PLAYER && !isDummy)
            {
                if (mn.GetXPModToPlayer() > 0 || MapMasterScript.activeMap.IsJobTrialFloor())
                {
                    float chance = GameMasterScript.gmsSingleton.globalPowerupDropChance * 0.22f * mn.GetXPModToPlayer();
                    if (MapMasterScript.activeMap.IsJobTrialFloor())
                    {
                        chance = 0.33f;
                    }
                    if (UnityEngine.Random.Range(0, 1f) <= chance && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("dropsoul"))
                    {
                        Vector2 position = MapMasterScript.GetRandomEmptyTile(mn.GetPos(), 1, true, true).pos;
                        Destructible pu = GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, Destructible.FindTemplate("monsterspirit"), position, GameMasterScript.GetPowerupLength());
                        pu.monsterAttached = mn.actorRefName;
                    }
                }
            }

            try { healthBarScript.UpdateBar(myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH)); }
            catch (Exception e)
            {
                Debug.Log("Monster health bar update failure " + e);
            }
            try { myAnimatable.SetAnimConditional(myAnimatable.defaultTakeDamageAnimationName); }
            catch (Exception e)
            {
                Debug.Log("Error when trying Takedamage anim for monster " + actorRefName + " " + e);
            }
        }


        lastTurnDamaged = GameMasterScript.turnNumber;

        //Debug.Log(myStats.IsAlive() + " " + myStats.GetCurStat(StatTypes.HEALTH) + " " + dType);

        if (!myStats.IsAlive() && GetActorType() == ActorTypes.MONSTER)
        {
            killedByDamage = dType;
        }

        if (myStats.IsAlive() && myStats.CheckHasStatusName("status_asleep"))
        {
            myStats.RemoveStatusByRef("status_asleep");
            StringManager.SetTag(0, displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("log_sleeping_wakeup"), this);
        }

        if (GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = this as Monster;
            if (mn.isBoss && mn.myTemplate.showBossHealthBar)
            {
                BossHealthBarScript.EnableBoss(mn);
            }
        }

        if (GetActorType() == ActorTypes.HERO && StatBlock.activeSongs.Count > 0)
        {
            GameMasterScript.heroPCActor.TryIncreaseSongDuration(2);
            GameMasterScript.heroPCActor.AddToDamageTakenThisTurn(dmgAmount);

        }

        turnsSinceLastDamaged = 0;

        damageTakenThisTurn += dmgAmount;

        return myStats.IsAlive();
    }

    void ProcessHeroDamage(float dmgAmount, float startHealth, DamageTypes dType)
    {
        HeroPC hpc = this as HeroPC;

        if (hpc.myJob.jobEnum == CharacterJobs.SHARA)
        {
            myAnimatable.SetAnimConditional("TakeDamage");
        }

        hpc.CheckForLimitBreakOnDamageTaken(dmgAmount);

        try { healthBarScript.UpdateBar(myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH)); }
        catch (Exception e)
        {
            Debug.Log("Player health bar update failure " + e);
        }

        hpc.CheckUpdateQuestHP((int)Mathf.Abs(dmgAmount));
        if (startHealth > 0 && myStats.GetCurStat(StatTypes.HEALTH) < 1f)
        {
            bool playerSurvived = false;
            if (myStats.CheckHasStatusName("status_cheatdeath"))
            {
                bool alreadyCheatedDeathThisTurn = ReadActorData("cheatdeath_on_turn") == 1;
                if (alreadyCheatedDeathThisTurn || UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHEAT_DEATH_CHANCE)
                {
                    myStats.SetStat(StatTypes.HEALTH, 1f, StatDataTypes.CUR, true);
                    myStats.RemoveTemporaryNegativeStatusEffects();
                    if (!alreadyCheatedDeathThisTurn)
                    {
                        GameLogScript.LogWriteStringRef("log_cheatdeath");
                        SetActorData("cheatdeath_on_turn", 1);
                    }
                    playerSurvived = true;
                }

            }
            else if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.SURVIVE_1HP_CHANCE)
            {
                myStats.SetStat(StatTypes.HEALTH, 1f, StatDataTypes.CUR, true);
                playerSurvived = true;
            }
            else if (myEquipment.GetItemByRefIfEquipped("accessory_leg_phoenix") != null)
            {
                Equipment eq = myEquipment.GetItemByRefIfEquipped("accessory_leg_phoenix");
                StringManager.SetTag(0, eq.displayName);
                GameLogScript.LogWriteStringRef("log_phoenixpendant");
                BattleTextManager.NewText(StringManager.GetString("log_reborn"), this.GetObject(), Color.red, 2f);
                myStats.HealToFull();
                HealAllSummonsToFull();
                myEquipment.Unequip(myEquipment.GetSlotOfEquippedItem(eq), false, SND.SILENT, false);
                myInventory.RemoveItem(eq);
                UIManagerScript.FlashWhite(0.7f);
                //myStats.SetStat(StatTypes.HEALTH, myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX), StatDataTypes.CUR, true);
                playerSurvived = true;
            }

            if (playerSurvived)
            {
                GameMasterScript.RemoveActorFromDeadQueue(GameMasterScript.heroPCActor);
            }
        }
        if (myStats.GetCurStat(StatTypes.HEALTH) >= 1f && myStats.GetCurStat(StatTypes.HEALTH) < 2f && myStats.IsAlive())
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.Survive1HP();
        }
        if (dType != DamageTypes.PHYSICAL)
        {
            int turnsSincePrism = 99;
            if (effectsInflictedOnTurn.ContainsKey("prismresist"))
            {
                turnsSincePrism = effectsInflictedOnTurn["prismresist"];
            }
            if (turnsSincePrism >= 8 && !myStats.CheckHasStatusName("status_prismresist") && myStats.CheckHasStatusName("status_prismatic"))
            {
                myStats.AddStatusByRef("status_prismresist", this, 4);
            }

        }
    }

    public void CalculateMaxRange()
    {
        Weapon weap = myEquipment.GetWeapon();
        float maxRange = 0f;
        if (weap == null)
        {
            weap = myEquipment.defaultWeapon;
        }
        else
        {
            maxRange = weap.range;
        }
        if (weap == null)
        {
            maxRange = 1f;
        }
        else
        {
            maxRange = weap.range;
        }
        // By default, it's the primary weapon.

        cachedBattleData.weaponAttackRange = maxRange;

        int moveRange = 1;

        if (GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = this as Monster;
            moveRange = mn.moveRange;
        }

        cachedBattleData.maxAttackRange = maxRange;
        cachedBattleData.maxMoveRange = moveRange;
    }

    public FighterBattleData CalculateHypotheticalFBD()
    {
        FighterBattleData returnData = new FighterBattleData();

        return returnData;
    }


    FighterBattleData CalculateBattleData_Internal()
    {
        if (_cachedBattleData == null)
        {
            _cachedBattleData = new FighterBattleData();
        }

        _cachedBattleData.SetClean();

        // Physical damage is based on level plus weapon damage, with strength modifying weapon damage.

        if (!CalculateChargeTime())
        {
            return _cachedBattleData;
        }

        float weaponDamageMod = 0.0f;

        float weaponPower = myEquipment.GetWeaponPower(myEquipment.GetWeapon());

        if (this == GameMasterScript.heroPCActor && myStats.CheckHasStatusName("status_unarmedfighting1"))
        {
            if (myEquipment.IsDefaultWeapon(myEquipment.GetWeapon()))
            {
                weaponPower = CombatManagerScript.CalculateBudokaWeaponPower(this, 1);
            }
        }

        if (myEquipment.IsWeaponRanged(myEquipment.GetWeapon()))
        {
            if (myEquipment.GetWeaponType() != WeaponTypes.STAFF)
            {
                weaponDamageMod = weaponPower + (weaponPower * myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS)); // Weapon damage + Weapon modified by swiftness%
            }
            else
            {
                float statMod = ((myStats.GetCurStatAsPercent(StatTypes.SPIRIT) + myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE))) / 2f;
                weaponDamageMod = weaponPower + (weaponPower * statMod); // Weapon damage + Weapon modified by spirit%
            }

        }
        else
        {
            switch (myEquipment.GetWeaponType())
            {
                case WeaponTypes.DAGGER:
                float statMod = ((myStats.GetCurStatAsPercent(StatTypes.STRENGTH) + myStats.GetCurStatAsPercent(StatTypes.GUILE))) / 2f;
                weaponDamageMod = weaponPower + (weaponPower * statMod);
                    break;
                case WeaponTypes.WHIP:
                    statMod = ((myStats.GetCurStatAsPercent(StatTypes.STRENGTH) + myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS))) / 2f;
                    weaponDamageMod = weaponPower + (weaponPower * statMod);
                    break;
                default:
                    weaponDamageMod = weaponPower + (weaponPower * myStats.GetCurStatAsPercent(StatTypes.STRENGTH)); // Weapon damage + Weapon modified by strength%
                    break;
            }
        }

        float effectiveLevel = myStats.GetLevel();
        if (effectiveLevel > 15) 
        {
            // For all stat calculation purposes, all levels over 15 count for half normal.
            effectiveLevel = ((effectiveLevel - 15f) * 0.5f) + 15f;
            // For example, level 17:
            // 17-15 = 2 * 0.5 = 1 + 15 = 16
        }

        float rawPlusWeapon = (effectiveLevel * 2) + weaponDamageMod;
        _cachedBattleData.physicalWeaponDamage = rawPlusWeapon + ((rawPlusWeapon * (effectiveLevel * 5f)) / 100f);
        _cachedBattleData.physicalWeaponDamage += physicalWeaponDamageAddFlat;
        _cachedBattleData.physicalWeaponDamage *= physicalWeaponDamageAddPercent;

        _cachedBattleData.energyReservedByAbilities = 0;
        if (GetActorType() == ActorTypes.HERO)
        {
            foreach(string reservingAbility in myAbilities.abilitiesThatReserveEnergy)
            {                
                AbilityScript rAbil = myAbilities.GetAbilityByRef(reservingAbility);
                _cachedBattleData.energyReservedByAbilities += rAbil.energyReserve;
            }
        }

        _cachedBattleData.staminaReservedByAbilities = 0;
        if (GetActorType() == ActorTypes.HERO)
        {
            foreach (string reservingAbility in myAbilities.abilitiesThatReserveStamina)
            {
                AbilityScript rAbil = myAbilities.GetAbilityByRef(reservingAbility);
                _cachedBattleData.staminaReservedByAbilities += rAbil.staminaReserve;
            }
        }

        // HARDCODED STATUS STUFF

        bool hasTwoHandSpecialist = false;

        if (this == GameMasterScript.heroPCActor)
        {
            if (myEquipment.GetWeapon() != null)
            {
                if (myEquipment.GetWeapon().twoHanded && !myEquipment.GetWeapon().isRanged && myStats.CheckHasStatusName("twohand_specialist"))
                {
                    hasTwoHandSpecialist = true;

                    float twoHandBonusMultiplier = 1.1f;

                    if (myStats.CheckHasStatusName("blackfurbonus2"))
                    {
                        twoHandBonusMultiplier = 1.2f;
                    }

                    _cachedBattleData.physicalWeaponDamage *= twoHandBonusMultiplier;

                }
            }
        }

        // END HARDCODED

        cachedBattleData.stealthValue = 1f;
        cachedBattleData.healModifierValue = 1f;


        float baseSpiritPower = (effectiveLevel * 3f) + 10;
        float finalPower = baseSpiritPower;

        if (myEquipment.GetWeaponType() == WeaponTypes.STAFF)
        {
            baseSpiritPower += myStats.GetLevel();
        }

        finalPower += (myStats.GetCurStatAsPercent(StatTypes.SPIRIT) * baseSpiritPower * StatBlock.SPIRIT_PERCENT_SPIRITPOWER_MOD);
        finalPower += (myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * baseSpiritPower * StatBlock.DISCIPLINE_PERCENT_SPIRITPOWER_MOD);
        finalPower += 11f;
        
        if (myStats.CheckHasStatusName("status_mmspiritpower10"))
        {
            finalPower *= 1.1f;
        }        

        // todo - roll this hardcoded stuff into AlterBattleData effects??
        if (GetActorType() == ActorTypes.HERO)
        {
            Offhand oh = myEquipment.GetOffhand() as Offhand;
            if (oh != null)
            {                
                if (oh.IsMagicBook())
                {
                    if (myStats.CheckHasStatusName("status_mmmagicbook1"))
                    {
                        finalPower *= 1.1f;
                    }
                    else if (myStats.CheckHasStatusName("status_mmmagicbook2"))
                    {
                        finalPower *= 1.15f;
                    }
                    else if (myStats.CheckHasStatusName("status_mmmagicbook3"))
                    {
                        finalPower *= 1.2f;
                    }
                    else if (myStats.CheckHasStatusName("status_mmmagicbook4"))
                    {
                        finalPower *= 1.25f;
                    }
                    else if (myStats.CheckHasStatusName("status_mmmagicbook5"))
                    {
                        finalPower *= 1.3f;
                    }
                }
            }

            if (myStats.CheckHasStatusName("xp2_battlemage"))
            {
                finalPower += (weaponPower * 1.15f);
            }
        }


        finalPower += _cachedBattleData.spiritPowerMod;
        finalPower *= _cachedBattleData.spiritPowerModMult;

        _cachedBattleData.spiritPower = finalPower;

        if (myEquipment.GetOffhandWeapon() != null && myEquipment.GetOffhand().ReadActorData("monkweapon") != 1)
        {
            if (myEquipment.IsWeaponRanged(myEquipment.GetOffhandWeapon()))
            {
                if (myEquipment.GetWeaponType() != WeaponTypes.STAFF)
                {
                    weaponDamageMod = myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) + ((myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) * myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS))); // Weapon damage + Weapon modified by swiftness%
                }
                else
                {
                    float statMod = (myStats.GetCurStatAsPercent(StatTypes.SPIRIT) + myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE)) / 2f;
                    weaponDamageMod = myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) + ((myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) * statMod)); // Weapon damage + Weapon modified by swiftness%
                }

            }
            else
            {
                weaponDamageMod = myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) + ((myEquipment.GetWeaponPower(myEquipment.GetOffhandWeapon()) * myStats.GetCurStatAsPercent(StatTypes.STRENGTH))); // Weapon damage + Weapon modified by strength%
            }
            rawPlusWeapon = (effectiveLevel * 2) + weaponDamageMod;
            _cachedBattleData.physicalWeaponOffhandDamage = rawPlusWeapon + ((rawPlusWeapon * (effectiveLevel * 5f)) / 100f);
            _cachedBattleData.physicalWeaponOffhandDamage += physicalWeaponDamageAddFlat;
            _cachedBattleData.physicalWeaponOffhandDamage *= physicalWeaponDamageAddPercent;

            CalculateDualwieldingStuff();

            //Debug.Log(_cachedBattleData.mainhandDamageMod + " " + _cachedBattleData.mainhandAccuracyMod);
        }
        else if (myEquipment.GetOffhand() == null || myEquipment.GetOffhand().actorRefName == "offhand_leg_ascetic_wrap" || myEquipment.GetOffhand().HasModByRef("mm_budokavalid")
            || myEquipment.GetOffhand().HasModByRef("mm_asceticgrab") || myEquipment.GetOffhand().ReadActorData("monkweapon") == 1)
        {
            // Nothing in offhand.        
            if (this == GameMasterScript.heroPCActor && myStats.CheckHasStatusName("status_unarmedfighting1") 
                && !myStats.CheckHasStatusName("status_unarmedfighting2"))
            {
                float punchPower = CombatManagerScript.CalculateBudokaWeaponPower(this, 1);

                weaponDamageMod = punchPower + (punchPower * myStats.GetCurStatAsPercent(StatTypes.STRENGTH)); // Weapon damage + Weapon modified by strength%
                rawPlusWeapon = (effectiveLevel * 2) + weaponDamageMod;
                _cachedBattleData.physicalWeaponOffhandDamage = rawPlusWeapon + ((rawPlusWeapon * (effectiveLevel * 5f)) / 100f);
                _cachedBattleData.physicalWeaponOffhandDamage += physicalWeaponDamageAddFlat;
                _cachedBattleData.physicalWeaponOffhandDamage *= physicalWeaponDamageAddPercent;
                _cachedBattleData.physicalWeaponOffhandDamage *= 0.5f;
                _cachedBattleData.offhandAccuracyMod = 0.5f;
            }
            else if (this == GameMasterScript.heroPCActor && myStats.CheckHasStatusName("status_unarmedfighting2"))
            {
                //float kickPower = 14f + (myStats.GetLevel() * 2.75f);
                float kickPower = CombatManagerScript.CalculateBudokaWeaponPower(this, 2);
                weaponDamageMod = kickPower + (kickPower * myStats.GetCurStatAsPercent(StatTypes.STRENGTH)); // Weapon damage + Weapon modified by strength%
                rawPlusWeapon = (effectiveLevel * 2) + weaponDamageMod;
                _cachedBattleData.physicalWeaponOffhandDamage = weaponDamageMod + ((weaponDamageMod * (effectiveLevel * 5f)) / 100f);
                _cachedBattleData.physicalWeaponOffhandDamage += physicalWeaponDamageAddFlat;
                _cachedBattleData.physicalWeaponOffhandDamage *= physicalWeaponDamageAddPercent;
                _cachedBattleData.physicalWeaponOffhandDamage *= 0.65f;
                _cachedBattleData.offhandAccuracyMod = 0.6f;
            }
            else
            {
                _cachedBattleData.physicalWeaponOffhandDamage = 0.0f;
            }
        }
        else
        {
            _cachedBattleData.physicalWeaponOffhandDamage = 0.0f;
        }

        float extraDefense = 0f;

        if (myStats.CheckHasStatusName("emeraldsetbonus1"))
        {
            extraDefense = _cachedBattleData.physicalWeaponDamage * 0.001f;
        }

        _cachedBattleData.blockMeleeChance = 0.0f;
        _cachedBattleData.blockRangedChance = 0f;
        Equipment off = myEquipment.equipment[(int)EquipmentSlots.OFFHAND];
        if (off != null)
        {
            if (off.slot == EquipmentSlots.OFFHAND)
            {
                Offhand oh = off as Offhand;
                float blockChance = oh.blockChance;
                _cachedBattleData.blockMeleeChance = blockChance;
                _cachedBattleData.blockRangedChance = blockChance;
            }
        }

        _cachedBattleData.physicalMeleeBaseDamage = (effectiveLevel * 2) * myStats.GetCurStatAsPercent(StatTypes.STRENGTH);
        _cachedBattleData.physicalRangedBaseDamage = (effectiveLevel * 2) * myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS); // Better way to calc this?



        CalculateMaxRange();

        _cachedBattleData.critMeleeChance = myStats.GetCurStatAsPercent(StatTypes.GUILE) * StatBlock.GUILE_PERCENT_CRITCHANCE_MOD;
        _cachedBattleData.critRangedChance = myStats.GetCurStatAsPercent(StatTypes.GUILE) * StatBlock.GUILE_PERCENT_CRITCHANCE_MOD;
        _cachedBattleData.parryMeleeChance = myStats.GetCurStatAsPercent(StatTypes.GUILE) * StatBlock.GUILE_PERCENT_PARRY_MOD;
        _cachedBattleData.parryMeleeChance += (effectiveLevel * 0.0025f);

        if (GetActorType() == ActorTypes.HERO && myEquipment.GetWeaponType() == WeaponTypes.NATURAL && myStats.CheckHasStatusName("emblem_budokaemblem_tier0_spiritfists"))
        {
            float spiritPowerMod = _cachedBattleData.spiritPower * 0.0003f;
            if (spiritPowerMod > 0.12f) spiritPowerMod = 0.12f;
            _cachedBattleData.parryMeleeChance += spiritPowerMod;
        }


        if (GetActorType() == ActorTypes.MONSTER)
        {
            _cachedBattleData.parryMeleeChance -= 0.03f;
            if (_cachedBattleData.parryMeleeChance < 0.01f)
            {
                _cachedBattleData.parryMeleeChance = 0.01f;
            }
        }


        if (myEquipment.GetWeaponType() == WeaponTypes.SWORD)
        {
            _cachedBattleData.parryMeleeChance += 0.03f;
        }
        if (myEquipment.GetOffhandWeapon() != null && myEquipment.GetOffhandWeapon().weaponType == WeaponTypes.SWORD)
        {
            _cachedBattleData.parryMeleeChance += 0.03f;

        }

        if (hasTwoHandSpecialist)
        {
            float twoHandParryBonus = 0.05f;
            if (myStats.CheckHasStatusName("blackfurbonus2"))
            {
                twoHandParryBonus = 0.1f;
            }

            _cachedBattleData.parryMeleeChance += twoHandParryBonus;
        }

        if (GetActorType() == ActorTypes.MONSTER)
        {
            _cachedBattleData.parryMeleeChance *= 0.5f;
        }

        _cachedBattleData.parryRangedChance = _cachedBattleData.parryMeleeChance * 0.5f;

        _cachedBattleData.critMeleeDamageMult = 1.35f + myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS);

        if (GetActorType() == ActorTypes.HERO && myEquipment.GetWeaponMod("mm_xp2_grandmaster"))
        {
            _cachedBattleData.critMeleeDamageMult += (myStats.GetCurStatAsPercent(StatTypes.STRENGTH) * 0.5f);
        }

        _cachedBattleData.critRangedDamageMult = 1.25f + myStats.GetCurStatAsPercent(StatTypes.SWIFTNESS);


        _cachedBattleData.critMeleeChance += _cachedBattleData.critChanceMod;
        _cachedBattleData.critRangedChance += _cachedBattleData.critChanceMod;

        _cachedBattleData.critMeleeDamageMult += _cachedBattleData.critDamageMod;
        _cachedBattleData.critRangedDamageMult += _cachedBattleData.critDamageMod;

        if (hasTwoHandSpecialist)
        {
            float twoHandCritBonus = 0.05f;
            if (myStats.CheckHasStatusName("blackfurbonus2"))
            {
                twoHandCritBonus = 0.1f;
            }
            _cachedBattleData.critMeleeChance += twoHandCritBonus;
        }

        if (GetActorType() == ActorTypes.HERO)
        {
            if ((myEquipment.GetWeaponType() == WeaponTypes.NATURAL) && (myStats.CheckHasStatusName("fistmastery2")))
            {
                _cachedBattleData.critMeleeDamageMult += 0.4f;
            }
        }

        if (GetActorType() == ActorTypes.MONSTER)
        {
            // Monsters crit less.
            //_cachedBattleData.critMeleeChance *= 0.75f;
            //_cachedBattleData.critRangedChance *= 0.75f;
            _cachedBattleData.critMeleeDamageMult = 1.3f;
            _cachedBattleData.critRangedDamageMult = 1.3f;
        }

        Equipment offhand = myEquipment.equipment[(int)EquipmentSlots.OFFHAND];
        if (offhand != null)
        {
            if (offhand.itemType == ItemTypes.OFFHAND)
            {

            }
        }

        if (myStats.statusDirty)
        {
            float baseDodge = 0;
            _cachedBattleData.dodgeMeleeChange = 0f;
            _cachedBattleData.dodgeRangedChance = 0f;

            foreach (StatusEffect se in myStats.GetAllStatuses())
            {
                baseDodge = 0;
                if (se.listEffectScripts.Count > 0)
                {
                    foreach (EffectScript eff in se.listEffectScripts)
                    {
                        if (eff.effectType == EffectType.ATTACKREACTION)
                        {
                            AttackReactionEffect are = eff as AttackReactionEffect;
                            if (are.procChance == 1.0f && are.reactCondition == AttackConditions.ANY)
                            {
                                baseDodge += are.alterAccuracyFlat;
                            }
                        }
                    }
                    baseDodge *= -0.01f;
                }

                _cachedBattleData.dodgeMeleeChange += baseDodge;
                _cachedBattleData.dodgeRangedChance += baseDodge;
            }
        }

        // Special case - medium armor mastery1
        if (GetActorType() == ActorTypes.HERO && myEquipment.GetArmorType() == ArmorTypes.MEDIUM
            && myStats.CheckHasStatusName("mediumarmormastery1"))
        {
            float extraDodgeChance = myStats.GetCurStat(StatTypes.SWIFTNESS) * 0.00125f;
            if (extraDodgeChance >= 0.1f) extraDodgeChance = 0.1f;
            _cachedBattleData.dodgeMeleeChange += extraDodgeChance;
            _cachedBattleData.dodgeRangedChance += extraDodgeChance;
        }

        // Clear resists
        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            _cachedBattleData.resistances[i].ResetResist((DamageTypes)i);
            _cachedBattleData.resistances[i].flatOffset = 0; // This WAS level. There should be no base flat resistance.

            _cachedBattleData.pierceResistances[i].ResetResist((DamageTypes)i);
            _cachedBattleData.pierceResistances[i].flatOffset = 0; // This WAS level. There should be no base flat resistance.

            // Heavy armor mastery 1 special case
            if (GetActorType() == ActorTypes.HERO && i != (int)DamageTypes.PHYSICAL)
            {
                if (myEquipment.GetArmorType() == ArmorTypes.HEAVY && myStats.CheckHasStatusName("heavyarmormastery1"))
                {
                    _cachedBattleData.resistances[i].flatOffset += myStats.GetLevel();
                }
            }
        }

        if (myStats.statusDirty)
        {
            resistStringDirty = true;
            foreach (StatusEffect se in myStats.GetAllStatuses())
            {
                int stacks = se.quantity;
                if (stacks < 1)
                {
                    stacks = 1;
                }
                foreach (EffectScript eff in se.listEffectScripts)
                {
                    if (eff.effectType == EffectType.ALTERBATTLEDATA)
                    {
                        AlterBattleDataEffect abde = eff as AlterBattleDataEffect;
                        cachedBattleData.stealthValue *= (abde.alterStealthDuringCache * stacks);
                        cachedBattleData.healModifierValue *= (abde.alterHealingDuringCache * stacks);
                        cachedBattleData.pierceResistances[(int)DamageTypes.FIRE].multiplier *= abde.pierceFire;
                        cachedBattleData.pierceResistances[(int)DamageTypes.WATER].multiplier *= abde.pierceWater;
                        cachedBattleData.pierceResistances[(int)DamageTypes.POISON].multiplier *= abde.piercePoison;
                        cachedBattleData.pierceResistances[(int)DamageTypes.SHADOW].multiplier *= abde.pierceShadow;
                        cachedBattleData.pierceResistances[(int)DamageTypes.LIGHTNING].multiplier *= abde.pierceLightning;
                        cachedBattleData.pierceResistances[(int)DamageTypes.PHYSICAL].multiplier *= abde.piercePhysical;
                    }
                }
            }
        }
       

        // Now calculate resists. Flat offsets
        for (int i = 0; i < (int)EquipmentSlots.COUNT; i++)
        {
            Equipment eq = myEquipment.equipment[i];
            if (eq != null)
            {
                if (eq.resists.Count > 0)
                {
                    foreach (ResistanceData rd in eq.resists)
                    {
                        if (rd.absorb) // If ANY piece of equipment absorbs, then everything does.
                        {
                            _cachedBattleData.resistances[(int)rd.damType].absorb = rd.absorb;
                        }

                        // Flat offsets are just added together.
                        _cachedBattleData.resistances[(int)rd.damType].flatOffset += rd.flatOffset;

                        // Multipliers are multiplied
                        /* if (this == GameMasterScript.heroPCActor)
                        {
                            Debug.Log(rd.damType.ToString() + " multiplier for " + eq.displayName + " is " + rd.multiplier + " cur mult is " + _cachedBattleData.resistances[(int)rd.damType].multiplier);
                        }   */
                        _cachedBattleData.resistances[(int)rd.damType].multiplier *= rd.multiplier;
                    }
                }
            }
        }

        // Cap min resist at 0.1 (10% of damage) to 3.0 (300%)

        float sumElementalResistance = 0f;

        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            float statMulti = 1.0f;
            switch ((DamageTypes)i)
            {
                case DamageTypes.FIRE:
                case DamageTypes.WATER:
                case DamageTypes.LIGHTNING:
                case DamageTypes.POISON:
                case DamageTypes.SHADOW:
                    statMulti = 1f - (myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * StatBlock.DISCIPLINE_PERCENT_ELEMRESIST_MOD); // 100 discipline = reduces energy by 25%
                    break;
                case DamageTypes.PHYSICAL:
                    statMulti = 1f - (myStats.GetCurStatAsPercent(StatTypes.STRENGTH) * StatBlock.STRENGTH_PERCENT_PHYSICALRESIST_MOD); // 100 strength = Reduces physical by 24%
                    break;
            }
            _cachedBattleData.resistances[i].multiplier -= extraDefense;
            _cachedBattleData.resistances[i].multiplier *= statMulti;

            /* if (i == (int)DamageTypes.PHYSICAL && myStats.CheckHasActiveStatusName("status_physdefdown_med"))
            {
                Debug.Log("Phys resist is " + _cachedBattleData.resistances[i].multiplier + " with ext. mod " + _cachedBattleData.resistanceExternalMods[i]);
            } */

            _cachedBattleData.resistances[i].multiplier *= _cachedBattleData.resistanceExternalMods[i];

            /* if (i == (int)DamageTypes.PHYSICAL && myStats.CheckHasActiveStatusName("status_physdefdown_med"))
            {
                Debug.Log("Phys resist is now " + _cachedBattleData.resistances[i].multiplier);
            } */

            if ((DamageTypes)i != DamageTypes.PHYSICAL)
            {
                sumElementalResistance += _cachedBattleData.resistances[i].multiplier;
            }

            _cachedBattleData.resistances[i].multiplier *= allMitigationAddPercent;
            /* if (this == GameMasterScript.heroPCActor)
            {
                Debug.Log((DamageTypes)i + " " + statMulti + " " + _cachedBattleData.resistanceExternalMods[i] + " " + allMitigationAddPercent);
            } */

            float minimumResistance = GameMasterScript.MAX_RESISTANCES;
            if (actorfaction == Faction.PLAYER && GetActorType() == ActorTypes.MONSTER)
            {
                minimumResistance = GameMasterScript.CORRALPET_MAX_RESISTANCES;
            }

            Mathf.Clamp(_cachedBattleData.resistances[i].multiplier, minimumResistance, 3.0f);

        }

        for (int i = 0; i < (int)DamageTypes.COUNT; i++)
        {
            _cachedBattleData.temporaryDamageMods[i] = 1.0f;
        }

        if (GetActorType() == ActorTypes.HERO && (myEquipment.GetWeaponType() == WeaponTypes.SWORD || myEquipment.GetWeaponType() == WeaponTypes.MACE) 
            && myEquipment.GetOffhandBlock() > 0f)
        {
            _cachedBattleData.temporaryDamageMods[(int)DamageTypes.PHYSICAL] += (0.18f * myStats.CheckStatusQuantity("status_mmknightgloves"));
        }     

        float avgElementalResistance = sumElementalResistance / 5f;

        if (myStats.CheckHasStatusName("status_panthoxskin"))
        {
            if (avgElementalResistance < 1f)
            {
                // Let's say we have 0.75f average elemental res.
                float converted = 1f - avgElementalResistance;
                // Now we have a value of 0.25f;
                // Take 20% of this for the skin ability
                converted *= 0.20f;
                // Now we have 0.05. So, we are increasing phys res by 5%.

                //Debug.Log("Pre-conversion phys res: " + _cachedBattleData.resistances[(int)DamageTypes.PHYSICAL].multiplier + ", subtract " + converted + " from avg elemental " + avgElementalResistance);
                _cachedBattleData.resistances[(int)DamageTypes.PHYSICAL].multiplier -= converted;
                Mathf.Clamp(_cachedBattleData.resistances[(int)DamageTypes.PHYSICAL].multiplier, GameMasterScript.MAX_RESISTANCES, 3.0f);
            }
        }

        if (GetActorType() == ActorTypes.HERO && myStats.CheckHasStatusName("relichunter"))
        {
            int numLegendaries = 0;
            for (int i = 0; i < myEquipment.equipment.Length; i++)
            {
                if (myEquipment.equipment[i] == null) continue;
                if (myEquipment.equipment[i].rarity == Rarity.LEGENDARY || myEquipment.equipment[i].rarity == Rarity.GEARSET)
                {
                    numLegendaries++;
                }
            }
            if (numLegendaries > 0)
            {
                float resMult = 1f - (numLegendaries * CombatManagerScript.RELIC_HUNTER_GEARBONUS);
                for (int i = 0; i < _cachedBattleData.temporaryDamageMods.Length; i++)
                {
                    _cachedBattleData.temporaryDamageMods[i] += (numLegendaries * CombatManagerScript.RELIC_HUNTER_GEARBONUS);
                    _cachedBattleData.resistances[i].multiplier *= resMult;
                    Mathf.Clamp(_cachedBattleData.resistances[i].multiplier, GameMasterScript.MAX_RESISTANCES, 3.0f);
                }
                
            }
        }


        // FOR NOW, only the hero has modifier effects
        // We save a lot of time by not running this routinely for monsters who never have altering effects
        if (GetActorType() == ActorTypes.HERO)
        {
            //Get all ability modifiers on all gear, remap abilities if necessary
            _cachedBattleData.ClearRemappedAbilities();
            List<StatusEffect> effects = myStats.GetAllStatuses();
            //  Build a List<AbilityEffects> from all effects in the statuseffects. Don't use listEffectScripts. 
            List<EffectScript> alllistEffectScriptsFromCaster = effects.SelectMany(ef => ef.listEffectScripts).ToList();
            List<AbilityModifierEffect> modifierList = alllistEffectScriptsFromCaster.OfType<AbilityModifierEffect>().ToList();

            // foreach of those, if there is a remapping, then remap
            foreach (AbilityModifierEffect ame in modifierList)
            {
                if (!string.IsNullOrEmpty(ame.strRemapAbilitiesToThisRef))
                {
                    foreach (string s in ame.abilityRefsToModify)
                    {
                        _cachedBattleData.SetRemappedAbility(s, ame.strRemapAbilitiesToThisRef);
                    }
                }
            }
        }

        myStats.statusDirty = false;

        return _cachedBattleData;
    }

    public void HealAllSummonsToFull()
    {
        if (summonedActors == null) return;
        foreach (Actor act in summonedActors)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster summonedMon = act as Monster;
                if (!summonedMon.myStats.IsAlive())
                {
                    continue;
                }

                summonedMon.myStats.HealToFull();
            }
        }
    }

    void CalculateDualwieldingStuff()
    {
        bool offhandIsClaw = (myEquipment.GetOffhandWeaponType() == WeaponTypes.CLAW);
        bool offhandHasLightweight = myEquipment.GetOffhandWeaponMod("mm_lightweight");
        bool knowsDualwieldingInnateSkill = myStats.CheckHasStatusName("dualwielderbonus1");
        bool knowsDaggerMastery2 = myStats.CheckHasStatusName("daggermastery2");

        float physicalWeaponDamageMod = 1f; // multiply physicalWeaponOffhandDamage by this
        float physicalAccuracyMod = 1f; // set offhandAccuracyMod to this
        float physicalOverallDamageMod = 1f; // set offhandDamageMod to this

        // Calculate OFFHAND WEAPON penalties here.

        if (offhandHasLightweight || offhandIsClaw) // Lightweight and Claw has the same effect.
        {
            physicalWeaponDamageMod = 0.7f;
            physicalAccuracyMod = 0.6f;
            physicalOverallDamageMod = 0.7f;

            if (offhandHasLightweight && offhandIsClaw) // But if we have both, it's a little better.
            {
                physicalWeaponDamageMod = 0.8f;
                physicalAccuracyMod = 0.7f;
                physicalOverallDamageMod = 0.8f;
            }
        }
        else
        {
            // Base penalties if you have no Lightweight or Claw.
            physicalWeaponDamageMod = 0.6f;
            physicalAccuracyMod = 0.5f;
            physicalOverallDamageMod = 0.6f;
        }
        if (knowsDualwieldingInnateSkill) // Regardless of weapon type or mods, this skill improves all dual wielding.
        {
            physicalWeaponDamageMod += 0.1f;
            physicalAccuracyMod += 0.1f;
            physicalOverallDamageMod += 0.1f;
        }
        if (knowsDaggerMastery2 && myEquipment.GetOffhandWeaponType() == WeaponTypes.DAGGER)
        {
            physicalWeaponDamageMod += 0.075f;
            physicalAccuracyMod += 0.075f;
            physicalOverallDamageMod += 0.075f;
        }

        physicalWeaponDamageMod = Mathf.Clamp(physicalWeaponDamageMod, 0.1f, 1f);
        physicalAccuracyMod = Mathf.Clamp(physicalAccuracyMod, 0.1f, 1f);
        physicalOverallDamageMod = Mathf.Clamp(physicalOverallDamageMod, 0.1f, 1f);

        _cachedBattleData.physicalWeaponOffhandDamage *= physicalWeaponDamageMod;
        _cachedBattleData.offhandAccuracyMod = physicalAccuracyMod;
        _cachedBattleData.offhandDamageMod = physicalOverallDamageMod;

        // Now for MAINHAND bonuses or penalties.
        physicalWeaponDamageMod = 1f; // multiply physicalWeaponDamage by this
        physicalAccuracyMod = 1f; // set mainhandAccuracyMod to this
        physicalOverallDamageMod = 1f; // set mainhandDamageMod to this

        bool mainhandIsClaw = (myEquipment.GetWeaponType() == WeaponTypes.CLAW);
        bool mainhandHasLightweight = myEquipment.GetWeaponMod("mm_lightweight");

        if (mainhandIsClaw || mainhandHasLightweight)
        {
            physicalWeaponDamageMod = 0.9f;
            physicalAccuracyMod = 0.9f;
            physicalOverallDamageMod = 0.9f;

            if (mainhandIsClaw && mainhandHasLightweight)
            {
                physicalWeaponDamageMod = 0.95f;
                physicalAccuracyMod = 0.95f;
                physicalOverallDamageMod = 0.95f;
            }
        }
        else
        {
            physicalWeaponDamageMod = 0.85f;
            physicalAccuracyMod = 0.85f;
            physicalOverallDamageMod = 0.85f;
        }

        if (knowsDualwieldingInnateSkill) // Regardless of weapon type or mods, this skill improves all dual wielding.
        {
            physicalWeaponDamageMod += 0.1f;
            physicalAccuracyMod += 0.1f;
            physicalOverallDamageMod += 0.1f;
        }
        if (knowsDaggerMastery2 && myEquipment.GetWeaponType() == WeaponTypes.DAGGER)
        {
            physicalWeaponDamageMod += 0.075f;
            physicalAccuracyMod += 0.075f;
            physicalOverallDamageMod += 0.075f;
        }

        physicalWeaponDamageMod = Mathf.Clamp(physicalWeaponDamageMod, 0.1f, 1f);
        physicalAccuracyMod = Mathf.Clamp(physicalAccuracyMod, 0.1f, 1f);
        physicalOverallDamageMod = Mathf.Clamp(physicalOverallDamageMod, 0.1f, 1f);

        _cachedBattleData.physicalWeaponDamage *= physicalWeaponDamageMod;
        _cachedBattleData.mainhandAccuracyMod = physicalAccuracyMod;
        _cachedBattleData.mainhandDamageMod = physicalOverallDamageMod;
    }

    //Ideally these functions will one day become the one-stop-shop for
    //calculating and grabbing the various values. They'll be used to populate
    //as much of CachedBattleData as possible. Until then, they exist to make sure
    //the data on the Character Sheet is clean.
    #region Values for Character Sheet

    public string GetCritChanceForCharacterSheet()
    {
        //Get the cached data
        float fCritValue = myEquipment.IsWeaponRanged(myEquipment.GetWeapon()) ? cachedBattleData.critRangedChance : cachedBattleData.critMeleeChance;

        // Empowerattack effects too
        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.EMPOWERATTACK)
                {
                    EmpowerAttackEffect eae = eff as EmpowerAttackEffect;

                    if (eae.parentAbility.reqWeaponType != WeaponTypes.ANY && eae.parentAbility.reqWeaponType != GameMasterScript.heroPCActor.myEquipment.GetWeaponType())
                    {
                        continue;
                    }
                    if (eae.extraChanceToCritFlat != 0f) // Add extra logic for conditionals here.
                    {
                        //critDisp += (eae.extraChanceToCritFlat * 100f);
                        fCritValue += eae.extraChanceToCritFlat;
                    }
                    if (eae.extraChanceToCrit != 1f)
                    {
                        //critDisp *= eae.extraChanceToCrit;
                        fCritValue *= eae.extraChanceToCrit;
                    }
                }
            }
        }

        //check for Heavyguard, because reasons
        /* if (myStats.CheckHasActiveStatusName("status_heavyguard"))
    {
        StatusEffect se = myStats.GetStatusByRef("status_heavyguard");
        if (myStats.GetCurStat(StatTypes.STAMINA) >= se.staminaReq)
        {
            fCritValue /= 2f;
        }
    } */

        //here it be, our base crit before considering any variables in the combat
        //between the current target and ourselves. Now let's decorate it for display

        fCritValue = (float)Math.Round(fCritValue, 2) * 100f;

        if (fCritValue < 0)
        {
            fCritValue = 0;
        }

        if (fCritValue > CombatManagerScript.CRIT_CHANCE_MAX * 100f)
        {
            fCritValue = CombatManagerScript.CRIT_CHANCE_MAX * 100f;
        }

        return ((int)fCritValue) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);


    }

    public string GetSpiritPowerForCharacterSheet()
    {
        //The value has been cached beforehand
        float spiritPowerDisplay = cachedBattleData.spiritPower;

        //Apply the effects of this status
        if (myStats.CheckHasStatusName("status_kineticmagic") && myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.51f)
        {
            spiritPowerDisplay *= 1.25f;
        }

        //display as a pretty int.
        return ((int)spiritPowerDisplay).ToString();
    }

    public string GetStatusEffectsTextForCharacterSheet()
    {
        string positiveStatuses = "";
        string negativeStatuses = "";
        string statusString = "";
        int countStatuses = 0;

        Dictionary<string, int> dictPositives = new Dictionary<string, int>();
        Dictionary<string, int> dictNegatives = new Dictionary<string, int>();

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (!se.showIcon) continue;
            if (se.passiveAbility) continue;
            countStatuses++;
            if (se.isPositive)
            {
                string durString = "";

                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                {
                    durString = " (" + se.curDuration + "t)";
                }

                string addString = "<color=yellow>" + se.abilityName + ": " + se.description + durString + "</color>";
                if (dictPositives.ContainsKey(addString))
                {
                    dictPositives[addString]++;
                }
                else
                {
                    dictPositives.Add(addString, 1);
                }

            }
            else
            {
                string durString = "";

                if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                {
                    durString = " (" + se.curDuration + "t)";
                }

                string addString = UIManagerScript.orangeHexColor + se.abilityName + ": " + se.description + durString + "</color>";

                if (dictNegatives.ContainsKey(addString))
                {
                    dictNegatives[addString]++;
                }
                else
                {
                    dictNegatives.Add(addString, 1);
                }
            }
        }

        if (countStatuses > 0)
        {
            statusString = StringManager.GetString("ui_current_statuses") + "\n\n"; // was adding size before, but don't do that

            foreach (string key in dictPositives.Keys)
            {
                if (dictPositives[key] == 1)
                {
                    positiveStatuses += key + "\n";
                }
                else
                {
                    positiveStatuses += key + " (" + dictPositives[key] + "x)\n";
                }
            }
            foreach (string key in dictNegatives.Keys)
            {
                if (dictNegatives[key] == 1)
                {
                    negativeStatuses += key + "\n";
                }
                else
                {
                    negativeStatuses += key + " (" + dictNegatives[key] + "x)\n";
                }
            }

            statusString += positiveStatuses + "\n" + negativeStatuses + "</size>";
        }
        else
        {
            statusString = "";
        }

        statusString = CustomAlgorithms.ParseLiveMergeTags(statusString);

        return statusString;

    }

    public string GetFeatsTextForCharacterSheet()
    {
        string strFeatsText = "";
        if (GameMasterScript.heroPCActor.heroFeats.Count > 0)
        {
            strFeatsText = "<color=yellow>" + StringManager.GetString("misc_feats_plural") + ":</color>";
        }

        bool hasKeenEyes = false;
        foreach (string feat in GameMasterScript.heroPCActor.heroFeats)
        {
            if (feat == "skill_keeneyes") hasKeenEyes = true;
            CreationFeat cf = CreationFeat.FindFeatBySkillRef(feat);
            if (cf != null)
            {
                strFeatsText += "\n\n" + UIManagerScript.greenHexColor + cf.featName + "</color>: " + cf.description;
            }
        }

        if (!hasKeenEyes && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("mmdungeondigest"))
        {
            AbilityScript theFeat = AbilityScript.GetAbilityByName("skill_keeneyes");
            if (theFeat != null)
            {
                strFeatsText += "\n\n" + UIManagerScript.greenHexColor + theFeat.abilityName + "</color>: " + theFeat.description;
            }
        }

        List<string> gameModifierStuff = new List<string>();
        for (int i = 0; i < GameStartData.gameModifiers.Length; i++)
        {
            if (GameStartData.gameModifiers[i])
            {
                gameModifierStuff.Add(GameStartData.gameModifierDataList[i].GetModifierDescription());
            }
        }            
        if (gameModifierStuff.Count > 0)
        {
            if (strFeatsText != "")
            {
                strFeatsText += "\n\n";
            }
            //strFeatsText += "\n\n<color=yellow>" + StringManager.GetString("ui_misc_gamemods") + ": ";
            for (int i = 0; i < gameModifierStuff.Count; i++)
            {
                strFeatsText += gameModifierStuff[i];
                if (i < gameModifierStuff.Count-1)
                {
                    strFeatsText += "\n";
                }
            }
        }

        return strFeatsText;
    }

    public string GetAllDamageModForCharacterSheet()
    {
        float damageMod = allDamageMultiplier;

        int wolfStatusQuantity = myStats.CheckStatusQuantity("xp2_thewolf");
        if (wolfStatusQuantity > 0)
        {
            if (ReadActorData("any_summoned_creatures") != 1)
            {
                damageMod += (CombatManagerScript.LONE_WOLF_DMG_MULT * wolfStatusQuantity);
            }
        }

        damageMod -= 1f;
        damageMod *= 100f;
        damageMod = (float)Math.Round(damageMod, 2);

        string strReturn;

        if (damageMod <= -1f)
        {
            strReturn = UIManagerScript.redHexColor + damageMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else if (damageMod >= 1f)
        {
            strReturn = UIManagerScript.greenHexColor + "+" + damageMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else
        {
            strReturn = "0" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }

        return strReturn;
    }

    public string GetAllDefenseModForCharacterSheet()
    {
        float defenseMod = allMitigationAddPercent;

        int wolfStatusQuantity = myStats.CheckStatusQuantity("xp2_thewolf");
        if (wolfStatusQuantity > 0)
        {
            if (ReadActorData("any_summoned_creatures") != 1)
            {
                defenseMod += (CombatManagerScript.LONE_WOLF_DEF_MULT * wolfStatusQuantity);
            }
        }

        defenseMod -= 1f;
        defenseMod *= -1f; // Since a higher number means we take MORE damage
        defenseMod *= 100f;
        defenseMod = (float)Math.Round(defenseMod, 2);

        string strReturn;

        if (GetActorType() == ActorTypes.HERO && GameStartData.NewGamePlus > 0 && (!MapMasterScript.IsActiveMapMysteryDungeon() || MysteryDungeonManager.CheckMysteryDungeonPlayerResources(EMysteryDungeonPlayerResources.STATS)))
        {
            // NG+ resists are nerfed
            defenseMod -= 25f;
        }

        if (defenseMod <= -1f)
        {
            strReturn = UIManagerScript.redHexColor + defenseMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else if (defenseMod >= 1f)
        {
            strReturn = UIManagerScript.greenHexColor + "+" + defenseMod + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>";
        }
        else
        {
            strReturn = "0" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
        }

        return strReturn;
    }

    public string GetPowerupDropChanceForCharacterSheet()
    {
        float localPowerup = GameMasterScript.gmsSingleton.globalPowerupDropChance;
        if (GameStartData.NewGamePlus > 0 && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            localPowerup *= (2f * GameStartData.NewGamePlus); // cancel out the NG+ nerf in mystery dungeons
        }

        float calcPowerupChance = (localPowerup * 100f) + (myStats.GetCurStat(StatTypes.GUILE) * StatBlock.GUILE_PERCENT_POWERUP_MOD);

        // Scavenging improves drop chance by 4%      
        calcPowerupChance += myStats.CheckStatusQuantity("status_mmscavenging") * 4f;   // This was 400% which was wrong

        if (myStats.CheckHasStatusName("sthergebonus1"))
        {
            calcPowerupChance += 20f;
        }

        return (int)(calcPowerupChance) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
    }

    public void GetElementResistAndDamageValuesForCharacterSheet(Switch_UICharacterSheet.ECharacterSheetValueType elementType, out string strResist,
        out string strDamage)
    {
        float percent = 0.0f;
        float offset = 0.0f;

        //damage types are 0 - 4
        int iElementIdx = (int)(elementType - (int)Switch_UICharacterSheet.ECharacterSheetValueType.element_physical);

        percent = cachedBattleData.resistances[iElementIdx].multiplier; // 0.1 = 90% resistance, 1.1 = -10%
        percent = (1 - percent) * 100f;
        percent = (int)percent;
        offset = (int)cachedBattleData.resistances[iElementIdx].flatOffset;
        strResist = percent + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        if (offset != 0)
        {
            strResist += " (" + offset + ")";
        }

        float damagePercent = cachedBattleData.damageExternalMods[iElementIdx];
        damagePercent += (cachedBattleData.temporaryDamageMods[iElementIdx] - 1f);
        damagePercent -= 1f;
        damagePercent *= 100f;

        damagePercent = (float)Math.Round(damagePercent, 1);

        strDamage = (int)damagePercent + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);

        if (damagePercent >= 1f)
        {
            strDamage = UIManagerScript.cyanHexColor + "+" + strDamage + "</color>";
        }
        if (damagePercent < -1f)
        {
            strDamage = UIManagerScript.redHexColor + strDamage + "</color>";
        }
    }

    public string GetBlockChanceForCharacterSheet()
    {
        float avg = cachedBattleData.blockMeleeChance + cachedBattleData.blockRangedChance;
        avg *= 0.5f;

        float blockDisp = (float)Math.Round(avg, 2) * 100f;

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.listEffectScripts == null || se.listEffectScripts.Count == 0)
            {
                continue;
            }

            foreach (EffectScript eff in se.listEffectScripts)
            {
                if (eff.effectType == EffectType.ATTACKREACTION)
                {
                    AttackReactionEffect react = eff as AttackReactionEffect;
                    if (react.alterBlockFlat != 0f) // Add extra logic for conditionals here.
                    {
                        blockDisp += (react.alterBlockFlat * 100f);
                    }
                    if (react.alterBlock != 1f)
                    {
                        blockDisp *= react.alterBlock;
                    }
                }
            }
        }

        if (myStats.CheckHasActiveStatusName("status_heavyguard") &&
            myEquipment.GetOffhandBlock() > 0f)
        {
            StatusEffect se = myStats.GetStatusByRef("status_heavyguard");
            if (myStats.GetCurStat(StatTypes.STAMINA) >= se.staminaReq)
            {
                //blockDisp *= 1.25f;
                blockDisp += 20f;
                blockDisp = (int)blockDisp;
            }
        }

        if (blockDisp < 0) blockDisp = 0;

        return ((int)blockDisp).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
    }

    public string GetFavoriteJob()
    {
        CharacterJobs highest = CharacterJobs.COUNT;
        int highestCount = 0;
        for (int i = 0; i < (int)CharacterJobs.COUNT; i++)
        {
            if (MetaProgressScript.jobsStarted[i] > highestCount)
            {
                highestCount = MetaProgressScript.jobsStarted[i];
                highest = (CharacterJobs)i;
            }
        }

        return CharacterJobData.GetJobDataByEnum((int)highest).GetBaseDisplayName();
    }
    #endregion

    public bool HasHealthBarObject()
    {
        if (GetObject() == null) return false;
        foreach(Transform t in GetObject().transform)
        {
            if (t.name.Contains("HealthBar"))
            {
                healthBarScript = t.gameObject.GetComponent<HealthBarScript>();
                return true;
            }
        }
        return false;
    }

    // Look for any Buff or Debuff effects which, for some reason, are sticking around now and again, and repool them.
    public void CleanStuckVisualFX()
    {
        if (GetObject() == null) return;

        try { healthBarScript.UpdateBar(myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH)); }
        catch (Exception e)
        {
            Debug.Log(actorRefName + " health bar update failure " + e);
        }

        EnableWrathBarIfNeeded();

        List<Transform> transformsToRemove = new List<Transform>();

        // Look through all statuses and see if anything is generating an overlay icon.
        foreach(StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.refName.Contains("steadfast") || se.refName.Contains("bling") || se.refName.Contains("flyingcloud")) spritesToIgnore.Add(se.ingameSprite);
            if (!se.showIcon) continue;
            foreach(GameObject go in se.spawnedOverlayRef)
            {
                if (go == null) continue;
                spritesToIgnore.Add(go.name);
            }
            if (!string.IsNullOrEmpty(se.ingameSprite))
            {
                spritesToIgnore.Add(se.ingameSprite);
            }
        }

        foreach(Transform child in GetObject().transform)
        {
            if (child == null || child.gameObject == null || !child.gameObject.activeSelf)
            {
                continue;
            }
            if (child.gameObject.name.Contains("FervirBuff") || child.gameObject.name.Contains("FervirDebuff"))
            {
                transformsToRemove.Add(child);
            }
            else
            {
                bool skipTransform = false;
                foreach(string str in spritesToIgnore)
                {
                    if (child.name.Contains(str))
                    {
                        skipTransform = true;
                        break;
                    }
                }
                if (!skipTransform)
                {
                    transformsToRemove.Add(child);
                }
            }
        }

        foreach(Transform t in transformsToRemove)
        {
            GameMasterScript.ReturnToStack(t.gameObject, t.gameObject.name.Replace("Clone", String.Empty));
        }
        
    }

    public void RemoveAllSummonedActorsOnDeath()
    {
        if (summonedActors != null)
        {
            List<Actor> actsToKill = new List<Actor>();
            foreach (Actor act in summonedActors)
            {
                bool killSummoned = false;
                if (act.dieWithSummoner)
                {
                    killSummoned = true;
                }
                else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = act as Destructible;
                    if (dt.objectFlags[(int)ObjectFlags.TARGETING])
                    {
                        killSummoned = true;
                    }
                }

                if (killSummoned)
                {
                    actsToKill.Add(act);
                }
            }
            foreach (Actor act in actsToKill)
            {
                if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    GameMasterScript.gmsSingleton.DestroyActor(act);
                }
                else
                {
                    Monster mn = act as Monster;
                    mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.ALL, true);
                    GameMasterScript.AddToDeadQueue(act);
                }
            }
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
        }
    }

    public bool CheckIfActorCanBeMoved(bool alsoLog)
    {
        bool unmoved = false;
        if (UnityEngine.Random.Range(0, 1f) <= 1f && myStats.CheckHasStatusName("status_mmsuperheavy"))
        {
            unmoved = true;
        }
        else if (myStats.CheckHasStatusName("status_mmultraheavy"))
        {
            unmoved = true;
        }
        else if (myStats.CheckHasStatusName("player_tempimmune_move"))
        {
            unmoved = true;
        }
        if (unmoved)
        {
            if (alsoLog)
            {
                StringManager.SetTag(0, displayName);
                GameLogScript.LogWriteStringRef("log_actor_immovable");
            }
            return false; // can't be
        }

        return true; // can be moved
    }

    public void AddToDamageTakenThisTurn(float dmg)
    {
        damageTakenLastThreeTurns[0] += dmg;
    }

    public void RefreshDamageTakenLastThreeTurns()
    {
        damageTakenLastThreeTurns[2] = damageTakenLastThreeTurns[1];
        damageTakenLastThreeTurns[1] = damageTakenLastThreeTurns[0];
        damageTakenLastThreeTurns[0] = 0f;
    }

    /// <summary>
    /// Removes any mimics (or other Adhesive creatures) that might be stuck to us, e.g. on zone change
    /// </summary>
    public void DisconnectMimicsIfNecessary()
    {
        StatusEffect mimicTongue = myStats.GetStatusByRef("adhesivetongue_target");
        if (mimicTongue != null)
        {
            myStats.ForciblyRemoveStatus("adhesivetongue_target");
            myStats.ForciblyRemoveStatus("tonguechecker");
            Actor mimicAttacker = GameMasterScript.gmsSingleton.TryLinkActorFromDict(ReadActorData("tongueattacker"));
            if (mimicAttacker != null && mimicAttacker.IsFighter())
            {
                RemoveAnchor(mimicAttacker);
                Fighter mimicFighter = mimicAttacker as Fighter;
                if (mimicFighter.summoner == this)
                {
                    mimicFighter.summoner = null;
                }
                if (mimicFighter.anchor == this)
                {
                    mimicFighter.anchor = null;
                }
                mimicFighter.myStats.ForciblyRemoveStatus("stickywarp");
                mimicFighter.RemoveActorData("stucktoactor");
            }
            RemoveActorData("tongueattacker");
        }
    }

    /// <summary>
    /// Returns FALSE if there is a catastrophic error
    /// </summary>
    /// <returns></returns>
    bool CalculateChargeTime()
    {
        float ctFromSwiftness = (myStats.GetCurStat(StatTypes.SWIFTNESS) * 0.10f);
        if (ctFromSwiftness >= 25f)
        {
            ctFromSwiftness = 25f;
        }
        _cachedBattleData.chargeGain = myStats.GetCurStat(StatTypes.CHARGETIME) + ctFromSwiftness;

        if (myStats == null || myEquipment == null)
        {
            Debug.Log("Warning: " + actorRefName + " floor " + dungeonFloor + " has no stats/equipment componnets.");
            return false;
        }

        if (myEquipment.GetArmorType() == ArmorTypes.LIGHT)
        {
            _cachedBattleData.chargeGain += 5f;
        }

        if (myStats.CheckHasStatusName("status_mmlosect2"))
        {
            _cachedBattleData.chargeGain -= 3f;
        }

        if (GameStartData.NewGamePlus >= 2 && GetActorType() == ActorTypes.HERO && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
        {
            float bonusOnly = _cachedBattleData.chargeGain - 100f;
            bonusOnly *= GameStartData.NGPLUSPLUS_CT_MODIFIER;
            _cachedBattleData.chargeGain = 100f + bonusOnly;
        }

        if (_cachedBattleData.chargeGain <= 100f && GetActorType() == ActorTypes.HERO)
        {
            _cachedBattleData.chargeGain = 100f;
        }

        return true;
    }

    public void OnAddSummon(Actor act)
    {
        int actorCount = 0;
        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            if (GetActorType() == ActorTypes.HERO && act.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
            {
                // corral pet doesnt count
                return;
            }
            actorCount = ReadActorData("any_summoned_creatures");
            if (actorCount < 0) actorCount = 0;
            actorCount++;
            SetActorData("any_summoned_creatures", actorCount);
        }
    }

    public void OnRemoveSummon(Actor act)
    {
        if (act.GetActorType() == ActorTypes.MONSTER)
        {
            int actorCount = ReadActorData("any_summoned_creatures");
            if (actorCount < 1) actorCount = 1;
            actorCount--;
            SetActorData("any_summoned_creatures", actorCount);
        }

        // if we're here, we must not have any summoned creatures.        
    }

    public void AddEffectInflicted(string key, int data)
    {
        effectsInflictedOnTurn.Add(key, data);
        effectsInflictedStringKeys.Add(key);
    }

    /// <summary>
    /// WARNING: Must make sure we already have this key, as it only changes the value!
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    public void SetEffectInflicted(string key, int data)
    {
        effectsInflictedOnTurn[key] = data;
        //effectsInflictedStringKeys.Add(key);
    }

    public void ResetEffectLogic(string effectRefName, int preTurnCounter, bool removeIfFalse)
    {
        if (removeIfFalse)
        {
            effectsInflictedOnTurn.Remove(effectRefName);
            effectsInflictedStringKeys.Remove(effectRefName);
        }
        else
        {
            SetEffectInflicted(effectRefName, preTurnCounter);
        }        
    }

    public float TryAdjustDamageTakenThisTurnToAvoidSpikes(float inputDamage)
    {
        if (GameStartData.NewGamePlus > 0) return inputDamage;

        // If we've already taken 25% of our health in damage, start scaling input down.
        if (damageTakenThisTurn >= myStats.GetMaxStat(StatTypes.HEALTH)*0.25f)
        {
            inputDamage *= 0.8f;
        }

        if (inputDamage >= myStats.GetMaxStat(StatTypes.HEALTH) * 0.33f && inputDamage < myStats.GetMaxStat(StatTypes.HEALTH) * 0.5f)
        {
            inputDamage *= 0.8f;
        }
        else if (inputDamage >= myStats.GetMaxStat(StatTypes.HEALTH) * 0.5f)
        {
            inputDamage *= 0.7f;
        }

        if (damageTakenThisTurn >= myStats.GetMaxStat(StatTypes.HEALTH) * 0.5f)
        {
            inputDamage *= 0.8f;
        }

        return inputDamage;
    }
}
