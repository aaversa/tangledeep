using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public partial class HeroPC
{
    public void ReadFromSave(XmlReader reader, bool loadPreMysteryDataAsNormalData = false)
    {
        bool debugLoading = false;
#if UNITY_EDITOR
        
#endif

        while (reader.Name != "hero")
        {
            if (debugLoading) Debug.Log("Iterating until we hit hero: " + reader.Name + " " + reader.NodeType);
            reader.Read();
        }
        reader.ReadStartElement();

        Vector2 previousPos = Vector2.zero;
        int reads = 0;
        string txt;
        bool anyPreMysteryDungeonData = false;
        MysteryDungeon mysteryDungeonInSaveFile = null;
        //if (Debug.isDebugBuild) Debug.Log("Loading hero. Should we use loadPreMysteryDungeonData? " + loadPreMysteryDataAsNormalData);
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            reads++;
            if (debugLoading) Debug.Log(strValue + " " + reader.NodeType);
            if (reads > 15000)
            {
                Debug.Log("Breaking");
                break;
            }
            if (reader.NodeType == XmlNodeType.None || reader.NodeType == XmlNodeType.Whitespace)
            {
                reader.Read();
                continue;
            }

            if (debugLoading) Debug.Log("Hero pre fighter read stuff " + reader.Name + " " + reader.NodeType);

            if (GameStartData.loadGameVer < 104)
            {
                bool successRead = true;
                while (successRead)
                {
                    successRead = ReadFighterStuffFromSave(reader, false);
                }
            }

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            strValue = reader.Name.ToLowerInvariant();

            if (debugLoading) Debug.Log("Post fighter stuff " + reader.Name + " " + reader.NodeType);

            switch (strValue)
            {
                case "displayname":
                    displayName = reader.ReadElementContentAsString();
                    break;
                case "playermodsactive":
                    PlayerModManager.ParseSavedPlayerModsIntoList(playerModsSavedLast, reader.ReadElementContentAsString());
                    break;
                case "selectedprefab":
                    selectedPrefab = reader.ReadElementContentAsString();
                    break;
                case "cr":
                    ReadCoreActorInfo(reader);
                    break;
                case "fl":
                case "floor":
                    dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "dmglast3turns":
                    string unparsed = reader.ReadElementContentAsString();

                    string[] parsedDmg = unparsed.Split('|');
                    damageTakenLastThreeTurns[0] = CustomAlgorithms.TryParseFloat(parsedDmg[0]);
                    damageTakenLastThreeTurns[1] = CustomAlgorithms.TryParseFloat(parsedDmg[1]);
                    damageTakenLastThreeTurns[2] = CustomAlgorithms.TryParseFloat(parsedDmg[2]);
                    break;
                case "iwmc":
                    txt = reader.ReadElementContentAsString();
                    MapMasterScript.itemWorldMagicChance = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "portalx":
                    portalX = reader.ReadElementContentAsInt();
                    break;
                case "randomjob":
                    reader.ReadElementContentAsString();
                    break;
                case "portaly":
                    portalY = reader.ReadElementContentAsInt();
                    break;
                case "portalmapid":
                    portalMapID = reader.ReadElementContentAsInt();
                    break;
                case "casinotokenprogress":
                    txt = reader.ReadElementContentAsString();
                    CasinoScript.totalNumChips = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "lowestfloorexplored":
                    lowestFloorExplored = reader.ReadElementContentAsInt();
                    break;
                case "lastoffhandid":
                    lastOffhandEquippedID = reader.ReadElementContentAsInt();
                    break;
                case "turnssincelaststun":
                    turnsSinceLastStun = reader.ReadElementContentAsInt();
                    break;
                case "mid":
                case "mapid":
                case "actormap":
                    actorMapID = reader.ReadElementContentAsInt();
                    MapMasterScript.TryAssignMap(this, actorMapID);
                    break;
                case "id":
                case "uniqueid":
                    actorUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "levelupboostwaiting":
                    levelupBoostWaiting = reader.ReadElementContentAsInt();
                    break;
                case "money":
                    money = reader.ReadElementContentAsInt();
                    break;
                case "numpandorasboxesopened":
                    numPandoraBoxesOpened = reader.ReadElementContentAsInt();
                    break;
                case "dayspassed":
                    daysPassed = reader.ReadElementContentAsInt();
                    break;
                case "regenflaskuses":
                    regenFlaskUses = reader.ReadElementContentAsInt();
                    break;
                case "herofeats":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        heroFeats.Add(reader.ReadElementContentAsString());
                    }
                    reader.ReadEndElement();
                    break;
                case "pos":
                    ReadCurrentPosition(reader);
                    break;
                case "posx":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosX(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "posy":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosY(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "limitbreak":
                    txt = reader.ReadElementContentAsString();
                    limitBreakAmount = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "stepstaken":
                    stepsTaken = reader.ReadElementContentAsInt();
                    break;
                case "monsterskilled":
                    monstersKilled = reader.ReadElementContentAsInt();
                    break;
                case "championskilled":
                    championsKilled = reader.ReadElementContentAsInt();
                    break;
                case "aid":
                case "areaid":
                    areaID = reader.ReadElementContentAsInt();
                    break;
                case "iwend":
                    GameMasterScript.endingItemWorld = reader.ReadElementContentAsBoolean();
                    break;
                case "newgameplus":
                    newGamePlus = reader.ReadElementContentAsInt();
                    if (GameStartData.NewGamePlus == 0)
                    {
                        GameStartData.NewGamePlus = newGamePlus;
                    }
                    break;
                case "playtime":
                    txt = reader.ReadElementContentAsString();
                    playTimeAtGameLoad = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "startingjob":
                    startingJob = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), reader.ReadElementContentAsString().ToUpperInvariant());
                    GameStartData.jobAsEnum = startingJob;
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
                case "currentjob":
                    myJob = CharacterJobData.GetJobData(reader.ReadElementContentAsString()); // (CharacterJobs)Enum.Parse(typeof(CharacterJobs), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "timeshealedthislevel":
                    timesHealedThisLevel = reader.ReadElementContentAsInt();
                    break;
                case "numjobchanges":
                    numberOfJobChanges = reader.ReadElementContentAsInt();
                    break;
                case "lastweapontype":
                    string eParse = reader.ReadElementContentAsString();
                    Enum.TryParse(eParse, out lastWeaponTypeUsed);

                    break;
                case "champskilledwithweapon":
                    string toParse = reader.ReadElementContentAsString();
                    string[] parsedArray = toParse.Split('|');
                    for (int i = 0; i < parsedArray.Length; i++)
                    {
                        Int32.TryParse(parsedArray[i], out championsKilledWithWeaponType[i]);
                    }
                    break;
                case "herojobs":
                    reader.ReadStartElement();
                    for (int i = 0; i < (int)CharacterJobs.COUNT - 2; i++)
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        txt = reader.ReadElementContentAsString();
                        jobJP[i] = CustomAlgorithms.TryParseFloat(txt);
                        txt = reader.ReadElementContentAsString();
                        jobJPspent[i] = CustomAlgorithms.TryParseFloat(txt);
                    }
                    reader.ReadEndElement();
                    break;
                case "lastmelee":
                    idOfLastUsedMeleeWeapon = reader.ReadElementContentAsInt();
                    break;
                case "lastweap":
                    idOfLastUsedWeapon = reader.ReadElementContentAsInt();
                    break;
                case "revealtiles":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        Vector2 newTile = Vector2.zero;
                        reader.ReadStartElement(); // tile
                        txt = reader.ReadElementContentAsString();
                        newTile.x = CustomAlgorithms.TryParseFloat(txt);
                        txt = reader.ReadElementContentAsString();
                        newTile.y = CustomAlgorithms.TryParseFloat(txt);
                        tempRevealTiles.Add(newTile);
                        reader.ReadEndElement(); // end tile
                    }
                    reader.ReadEndElement();
                    break;
                case "sts":
                case "statblock":
                case "stats":
                    myStats.ReadFromSave(reader, true);
                    break;
                case "inv":
                case "inventory":
                    reader.ReadStartElement();
                    if (reader.Name.ToLowerInvariant() != "item")
                    {
                        continue;
                    }
                    myInventory.ReadFromSave(reader);
                    //reader.Read();
                    break;
                case "ability":
                    reader.ReadStartElement();
                    AbilityScript newAbil = new AbilityScript();
                    newAbil.ReadFromSave(reader, GameMasterScript.heroPCActor);
                    myAbilities.abilities.Add(newAbil);
                    if (!myAbilities.dictAbilities.ContainsKey(newAbil.refName))
                    {
                        myAbilities.dictAbilities.Add(newAbil.refName, newAbil);
                    }
                    else
                    {
                        Debug.Log("WARNING! Hero's ability component has duplicate ability loaded from save: " + newAbil.refName);
                    }
                    break;
                case "eq":
                case "equipmentblock":
                    reader.ReadStartElement();
                    myEquipment.ReadFromSave(reader, false);
                    //reader.Read();
                    break;
                case "gmods":
                    string unparsedMods = reader.ReadElementContentAsString();
                    string[] parsedMods = unparsedMods.Split('|');
                    for (int i = 0; i < parsedMods.Length; i++)
                    {
                        bool value;
                        if (Boolean.TryParse(parsedMods[i], out value))
                        {
                            GameStartData.gameModifiers[i] = value;
                        }
                    }
                    break;
                case "advm":
                    GameMasterScript.gmsSingleton.adventureModeActive = reader.ReadElementContentAsBoolean();
                    if (GameMasterScript.gmsSingleton.adventureModeActive)
                    {
                        GameMasterScript.gmsSingleton.gameMode = GameModes.ADVENTURE;
                        SetActorData("advm", 1);
                    }
                    break;
                case "mode":
                    GameModes readMode = (GameModes)Enum.Parse(typeof(GameModes), reader.ReadElementContentAsString().ToUpperInvariant());
                    localGameMode = readMode;

                    GameMasterScript.gmsSingleton.gameMode = readMode;

                    if (GameMasterScript.gmsSingleton.gameMode == GameModes.ADVENTURE)
                    {
#if UNITY_EDITOR
                        //Debug.LogError("Game mode is apparently adventure");
#endif
                        GameMasterScript.gmsSingleton.adventureModeActive = true;
                        SetActorData("advm", 1);
                    }
                    else
                    {
                        GameMasterScript.gmsSingleton.adventureModeActive = false;
                    }

                    GameStartData.ChangeGameMode(GameMasterScript.gmsSingleton.gameMode);

                    break;
                case "previousposx":
                    previousPos.x = reader.ReadElementContentAsInt();
                    break;
                case "previousposy":
                    previousPos.y = reader.ReadElementContentAsInt();
                    break;
                case "dreamfloor":
                    ReadItemDreamData(reader);
                    break;
                case "xareas":
                    string allAreas = reader.ReadElementContentAsString();
                    string[] parsed = allAreas.Split(',');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        int aID;
                        if (Int32.TryParse(parsed[i], out aID))
                        {
                            exploredAreas[aID] = true;
                        }
                    }
                    break;
                // deprecated
                case "exploredareas":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        exploredAreas[(reader.ReadElementContentAsInt())] = true;
                    }
                    reader.ReadEndElement();
                    break;
                case "xmaps":
                    string allmaps = reader.ReadElementContentAsString();
                    string[] mParsed = allmaps.Split(',');
                    for (int i = 0; i < mParsed.Length; i++)
                    {
                        int mID;
                        if (Int32.TryParse(mParsed[i], out mID))
                        {
                            mapsExploredByMapID.Add(mID);
                        }
                    }
                    break;
                case "cmapsnew":
                    string clearMapsFullStrings = reader.ReadElementContentAsString();
                    string[] cParsed1 = clearMapsFullStrings.Split(',');
                    for (int i = 0; i < cParsed1.Length; i++) // data packages
                    {
                        string[] subParse = cParsed1[i].Split('|');

                        int clearedMapID;
                        int clearedMapFloor;

                        if (Int32.TryParse(subParse[0], out clearedMapID))
                        {
                            if (Int32.TryParse(subParse[1], out clearedMapFloor))
                            {
                                MapClearDataPackage mcdp = new MapClearDataPackage(clearedMapID, clearedMapFloor); // unknown floor from legacy data
                                mapsCleared.Add(mcdp);
                            }
                        }
                    }
                    break;
                case "xmapfloors":
                    unparsed = reader.ReadElementContentAsString();
                    string[] parsedFloors = unparsed.Split(',');
                    for (int i = 0; i < parsedFloors.Length; i++)
                    {
                        int mapFloor;
                        if (Int32.TryParse(parsedFloors[i], out mapFloor))
                        {
                            mapFloorsExplored.Add(mapFloor);
                        }
                    }
                    break;
                case "clearedmaps": // legacy support
                    string clearMapsStrings = reader.ReadElementContentAsString();
                    string[] cParsed = clearMapsStrings.Split(',');
                    for (int i = 0; i < cParsed.Length; i++)
                    {
                        int mID;
                        if (Int32.TryParse(cParsed[i], out mID))
                        {
                            MapClearDataPackage mcdp = new MapClearDataPackage(mID, -1); // unknown floor from legacy data
                            mapsCleared.Add(mcdp);
                        }
                    }
                    break;
                // deprecated
                case "exploredmaps":
                    reader.ReadStartElement();
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        int mID = reader.ReadElementContentAsInt();
                        mapsExploredByMapID.Add(mID);
                    }
                    reader.ReadEndElement();
                    break;
                // Deprecated            if
                case "anchoredactorid":
                    if (anchoredActorsIDs == null)
                    {
                        anchoredActorsIDs = new List<int>();
                    }
                    anchoredActorsIDs.Add(reader.ReadElementContentAsInt());
                    break;
                // End deprecated
                case "quest":
                    QuestScript qs = new QuestScript();
                    qs.ReadFromSave(reader);

                    if (qs.ValidateData())
                    {
                        myQuests.Add(qs);
                    }
                    else
                    {
                        Debug.Log("Hero quest failed to load, type: " + qs.qType);
                    }


                    break;
                case "bg":
                    beatTheGame = reader.ReadElementContentAsBoolean();
                    break;
                case "card":
                    reader.ReadStartElement();
                    int face = reader.ReadElementContentAsInt();
                    int suit = reader.ReadElementContentAsInt();
                    PlayingCard pc = PlayingCard.DrawSpecificCard((CardSuit)suit, (CardFace)face);
                    if (pc != null)
                    {
                        gamblerHand.Add(pc);
                    }

                    reader.ReadEndElement();
                    break;

                case "seed":
                    int seed = reader.ReadElementContentAsInt();
                    GameMasterScript.SetRNGSeed(seed);
                    break;
                case "challenge":
                    ChallengeTypes cType = (ChallengeTypes)Enum.Parse(typeof(ChallengeTypes), reader.ReadElementContentAsString().ToUpperInvariant());
                    GameStartData.challengeType = cType;
                    GameStartData.currentChallengeData = new ChallengeDataPack();
                    GameStartData.currentChallengeData.cType = cType;
                    break;
                case "cday":
                    Int32.TryParse(reader.ReadElementContentAsString(), out GameStartData.challengeDay);
                    GameStartData.currentChallengeData.dayOfYear = GameStartData.challengeDay;
                    break;
                case "cweek":
                    Int32.TryParse(reader.ReadElementContentAsString(), out GameStartData.challengeWeek);
                    GameStartData.currentChallengeData.weekOfYear = GameStartData.challengeWeek;
                    break;
                case "adventurestats":
                    string advStatsRead = reader.ReadElementContentAsString();
                    string[] advParsed = advStatsRead.Split('|');
                    for (int i = 0; i < advParsed.Length; i++)
                    {
                        float fParsed = CustomAlgorithms.TryParseFloat(advParsed[i]);
                        advStats[i] = fParsed;
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
                case "jobtrial":
                    jobTrial = new JobTrialScript();
                    jobTrial.ReadFromSave(reader);
                    break;
                case "orbusedtoopenitemworld":
                    reader.ReadStartElement();
                    Consumable nOrb = new Consumable();
                    reader.ReadStartElement();
                    nOrb.ReadFromSave(reader);
                    reader.ReadEndElement();
                    break;
                case "itemworlditemid":
                    MapMasterScript.itemWorldItemID = reader.ReadElementContentAsInt();
                    break;
                case "previousturnactions":
                    string prevActions = reader.ReadElementContentAsString();
                    string[] parsedActions = prevActions.Split('|');
                    for (int i = 0; i < parsedActions.Length; i++)
                    {
                        int iVal;
                        if (Int32.TryParse(parsedActions[i], out iVal))
                        {
                            previousTurnActions[i] = (TurnTypes)iVal;
                        }

                    }
                    break;
                case "energyreservedabilities":
                    string abils = reader.ReadElementContentAsString();
                    string[] parsedAbils = abils.Split(',');
                    for (int i = 0; i < parsedAbils.Length; i++)
                    {
                        myAbilities.abilitiesThatReserveEnergy.Add(parsedAbils[i]);
                    }

                    break;
                case "staminareservedabilities":
                    abils = reader.ReadElementContentAsString();
                    parsedAbils = abils.Split(',');
                    for (int i = 0; i < parsedAbils.Length; i++)
                    {
                        myAbilities.abilitiesThatReserveStamina.Add(parsedAbils[i]);
                    }

                    break;
                case "shopkeepers":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split(',');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        int shopkeeperID;
                        if (Int32.TryParse(parsed[i], out shopkeeperID))
                        {
                            shopkeepersThatRefresh.Add(shopkeeperID);
                        }
                    }
                    break;
                case "mysterydungeondata":
                    myMysteryDungeonData = new MysteryDungeonSaveData();
                    myMysteryDungeonData.ReadFromSave(reader);
                    anyPreMysteryDungeonData = true;
                    MysteryDungeon activeMD = DungeonMaker.GetMysteryDungeonByRef(myMysteryDungeonData.dungeonRefName);
                    mysteryDungeonInSaveFile = activeMD;
                    MysteryDungeonManager.SetActiveDungeon(activeMD);
                    GameMasterScript.gmsSingleton.SetTempGameData("creatingmysterydungeon", 1);
                    break;

                case "droppedrelics":
                    unparsed = reader.ReadElementContentAsString();
                    parsed = unparsed.Split('|');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        string[] subparsed = parsed[i].Split(',');
                        int floorVal = 0;
                        if (Int32.TryParse(subparsed[1], out floorVal))
                        {
                            if (SharedBank.allRelicTemplates.ContainsKey(subparsed[0]))
                            {
                                relicsDroppedOnTheGroundOrSold.Add(subparsed[0], floorVal);
                            }
                        }
                    }
                    break;
                default:
                    string unusedKey = reader.Name;
                    string unusedString = reader.ReadElementContentAsString();
                    if (debugLoading) Debug.Log("Loading hero, but my key is " + unusedKey + " and the value is " + unusedString);
                    break;
                    //#endif
            }
        }
        GameMasterScript.AddActorToDict(this);
        reader.ReadEndElement();
        if (loadPreMysteryDataAsNormalData)
        {
            CopyPreMysteryDungeonDataToCoreHeroData(mysteryDungeonInSaveFile);
            if (Debug.isDebugBuild) Debug.Log("Restored mystery dungeon data to hero data.");
        }

        if (GameMasterScript.gmsSingleton.gameRandomSeed == 0)
        {
            GameMasterScript.SetRNGSeed(UnityEngine.Random.Range(1, 1000000));
        }

        if (debugLoading) Debug.Log("Done reading hero. name/node? " + reader.Name + " : " + reader.NodeType);

        RemoveErroneousAbilitiesLearnedFromOtherJobs();

        // Below block is to fix LEGACY saves, before certain statuses was added.

        bool learnManageSpellshapes = false;
        foreach (AbilityScript abil in myAbilities.GetAbilityList())
        {
            if (abil.CheckAbilityTag(AbilityTags.SPELLSHAPE))
            {
                if (!myAbilities.HasAbilityRef("skill_managespellshapes"))
                {
                    learnManageSpellshapes = true;
                    break;
                }
                else
                {
                    myAbilities.GetAbilityByRef("skill_managespellshapes").jobLearnedFrom = CharacterJobs.SPELLSHAPER;
                }
            }
        }
        if (learnManageSpellshapes)
        {
            AbilityScript template = GameMasterScript.masterAbilityList["skill_managespellshapes"];
            AbilityScript learnt = LearnAbility(template, true, true, true);
            if (learnt != null)
            {
                learnt.jobLearnedFrom = CharacterJobs.SPELLSHAPER;
                UIManagerScript.AddAbilityToOpenSlot(learnt);
            }

        }

        // Oops, our character is stored in a mystery dungeon... great, we need to fix this.
        if (myMysteryDungeonData != null && !DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {

        }
    }

    public void WriteItemDreamData(XmlWriter writer)
    {
        if (dictDreamFloorData == null) return;

        foreach (ItemDreamFloorData floorData in dictDreamFloorData.Values)
        {
            writer.WriteStartElement("dreamfloor");
            writer.WriteElementString("mapid", floorData.iDreamMapID.ToString());
            string data = "";
            for (int i = 0; i < floorData.dreamEvents.Length; i++)
            {
                data += i + "," + (int)floorData.dreamEvents[i];
                if (i < floorData.dreamEvents.Length - 1)
                {
                    data += "|";
                }
            }
            // Format of this string would be:
            // 0,0|1,0,|2,0   etc
            // Where the first number in the pair (comma separated) is the floor event type (bigmode, costume etc)
            // And the second number in the pair is the value (NOTCHECKED, TRUE, FALSE)
            // All converted to INT of course
            writer.WriteElementString("eventdata", data);
            writer.WriteElementString("viewed", floorData.viewed.ToString().ToLowerInvariant());
            writer.WriteEndElement();
        }
    }

    public void ReadItemDreamData(XmlReader reader)
    {
        reader.ReadStartElement(); // reads "dreamfloor"

        ItemDreamFloorData currentData = null;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "mapid":
                    int id = reader.ReadElementContentAsInt();
                    if (!dictDreamFloorData.ContainsKey(id))
                    {
                        currentData = new ItemDreamFloorData();
                        currentData.iDreamMapID = id;
                        dictDreamFloorData.Add(id, currentData);
                    }
                    break;
                case "eventdata":
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split('|');
                    // Now we are split into pairs. Each pair is a possible dream event type like BigMode, Costume Party etc
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        string[] subParsed = parsed[i].Split(',');
                        int iEventType = Int32.Parse(subParsed[0]);
                        int iEventValue = Int32.Parse(subParsed[1]);
                        currentData.dreamEvents[iEventType] = (ItemDreamFloorValues)iEventValue;
                    }
                    break;
                case "viewed":
                    currentData.viewed = reader.ReadElementContentAsBoolean();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();
    }

    /// <summary>
    /// Called during game load to link up our summon IDs with actual actors
    /// </summary>
    public void LoadAndValidateHeroSummons()
    {
        if (summonedActorIDs != null)
        {
            List<int> removeIDs = new List<int>();
            foreach (int i in summonedActorIDs)
            {
                Actor getAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(i);
                if (getAct != null && !summonedActors.Contains(getAct))
                {
                    bool addActorToSummons = true;
                    if (getAct.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = getAct as Monster;
                        if (!mn.myStats.IsAlive())
                        {
                            addActorToSummons = false;
                            Debug.Log(mn.actorRefName + " " + mn.actorUniqueID + " is dead, don't load it into hero summons.");
                        }
                        if (mn.actorfaction != Faction.PLAYER)
                        {
                            addActorToSummons = false;
                            Debug.Log(mn.actorRefName + " " + mn.actorUniqueID + " is not player faction. Don't load it.");
                        }
                    }

                    if (addActorToSummons && getAct.GetActorType() != ActorTypes.ITEM)
                    {
                        AddSummon(getAct);
                        //Debug.Log("Linked hero summon " + getAct.actorRefName);
                    }
                    else
                    {
                        removeIDs.Add(i);
                    }
                }
            }
            foreach (int id in removeIDs)
            {
                summonedActorIDs.Remove(id);
            }
        }
    }

    public void ValidateQuestsOnLoad()
    {
        foreach (QuestScript qs in myQuests)
        {
            bool removeThisQuest = false;
            if (qs.itemRewardID > 0)
            {
                Actor itm;
                if (GameMasterScript.dictAllActors.TryGetValue(qs.itemRewardID, out itm))
                {
                    qs.itemReward = itm as Item;
                }
                else
                {
                    Debug.Log("Could not link up item ID " + qs.itemRewardID + " for hero quest");
                    removeThisQuest = true;
                }
            }
            if (qs.targetMonsterID > 0)
            {
                Actor tMon;
                if (GameMasterScript.dictAllActors.TryGetValue(qs.targetMonsterID, out tMon))
                {
                    qs.targetMonster = tMon as Monster;
                }
                else
                {
                    Debug.Log("Could not link up monster ID " + qs.targetMonsterID + " for hero quest");
                    removeThisQuest = true;
                }
            }
            if (qs.targetActorID > 0)
            {
                Actor tAct;
                if (GameMasterScript.dictAllActors.TryGetValue(qs.targetActorID, out tAct))
                {
                    qs.targetActor = tAct;
                }
                else
                {
                    Debug.Log("Could not link up actor ID " + qs.targetActorID + " for hero quest");
                    removeThisQuest = true;
                }
            }
            if (qs.targetItemID > 0)
            {
                Actor getItem;
                if (GameMasterScript.dictAllActors.TryGetValue(qs.targetItemID, out getItem))
                {
                    qs.targetItem = getItem as Item;
                }
                else
                {
                    Debug.Log("Could not link up item ID " + qs.targetItemID + " for hero quest " + qs.qType);
                    removeThisQuest = true;
                }
            }
            if (qs.targetMapID > 0)
            {
                Map tMap;
                if (MapMasterScript.dictAllMaps.TryGetValue(qs.targetMapID, out tMap))
                {
                    qs.targetMap = tMap;
                }
                else
                {
                    Debug.Log("Could not link up map ID " + qs.targetMapID + " for hero quest");
                    removeThisQuest = true;
                }
            }
            try { qs.GenerateQuestText(); }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log("Loading quest failed.");
                removeThisQuest = true;
            }
            if (removeThisQuest)
            {
                qToRemove.Add(qs);
            }
        }

        foreach (QuestScript qs in qToRemove)
        {
            myQuests.Remove(qs);
        }

        foreach (QuestScript qs in myQuests)
        {
            qs.VerifyLinkedMapsAreEnabled();
        }
    }

    public void OnLoadCleanup()
    {
        if (lastOffhandEquippedID != 0)
        {
            Actor findAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(lastOffhandEquippedID);
            if (findAct != null)
            {
                lastOffhandEquipped = findAct as Equipment;
            }
        }

        TryLinkAllPairedItems();

        SetBattleDataDirty();

        if (!MapMasterScript.itemWorldOpen)
        {
            ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 0);
        }

        bool hasAnyDoubleBiteStatus = false;
        bool adjustedStatuses = false;

        if (myStats.CheckHasStatusName("doublebite_shadow"))
        {
            hasAnyDoubleBiteStatus = true;
            myStats.ReallyForciblyRemoveAllStatus("doublebite_physical");
            int attempts = 0;
            while(myStats.CheckStatusQuantity("doublebite_shadow") > 1)
            {
                attempts++;
                if (attempts > 100) {
                    //Debug.Log("Could not remove doublebite_physical status after 100 attempts.");
                    break;
                }                
                myStats.ReallyForciblyRemoveStatus("doublebite_shadow");
            }
        }
        
        if (myStats.CheckHasStatusName("doublebite_physical"))
        {
            hasAnyDoubleBiteStatus = true;
            myStats.ReallyForciblyRemoveAllStatus("doublebite_shadow");
            int attempts = 0;
            while(myStats.CheckStatusQuantity("doublebite_physical") > 1)
            {
                attempts++;
                if (attempts > 100) {
                    //Debug.Log("Could not remove doublebite_physical status after 100 attempts.");
                    break;
                }
                myStats.ReallyForciblyRemoveStatus("doublebite_physical");
            }
        }        

        if (!hasAnyDoubleBiteStatus)
        {
            for (int i = 0; i < myEquipment.equipment.Length; i++)
            {
                var eq = myEquipment.equipment[i];
                if (eq == null) continue;
                if (!eq.IsEquipment()) continue;
                Equipment eqp = eq as Equipment;
                if (eqp.HasModByRef("mm_doublebite"))
                {
                    myStats.AddStatusByRef("doublebite_shadow", this, 99, false);
                    adjustedStatuses = true;
                    break;
                }
            }
        }

        if (hasAnyDoubleBiteStatus || adjustedStatuses)
        {
            UIManagerScript.RefreshStatuses(true);
        }
    }

    List<AbilityScript> erroneousAbilitiesToRemove;

    /// <summary>
    /// Somehow, abilities that are passives from other jobs are sticking with us on the current job.
    /// This function will remove 'core' (innate) abilities that we should not have.
    /// </summary>
    void RemoveErroneousAbilitiesLearnedFromOtherJobs()
    {
        if (erroneousAbilitiesToRemove == null) erroneousAbilitiesToRemove = new List<AbilityScript>();
        erroneousAbilitiesToRemove.Clear();

        //float tStart = Time.realtimeSinceStartup;

        foreach(var abil in myAbilities.abilities)
        {
            // Ignore anything that is learned from our current job
            if (abil.jobLearnedFrom == myJob.jobEnum) continue;

            // Check if this is an innate
            if (abil.jobLearnedFrom == CharacterJobs.COUNT || abil.jobLearnedFrom == CharacterJobs.GENERIC)
            {
                continue;
            }

            CharacterJobData cjd = CharacterJobData.GetJobDataByEnum((int)abil.jobLearnedFrom);

            if (cjd == null) continue;

            // Compare to the abil we're checking
            foreach(var jobAbilityRef in cjd.JobAbilities)
            {
                // We only care about innates...
                if (!jobAbilityRef.innate) continue;
                if (abil.refName != jobAbilityRef.abilityRef) continue;
                
                erroneousAbilitiesToRemove.Add(abil);
                break;
            }
        }

        foreach(var abil in erroneousAbilitiesToRemove)
        {
            if (Debug.isDebugBuild) Debug.Log("Removing wrong ability " + abil.refName);
            RemoveAbility(abil);
        }

        //Debug.Log("Time passed: " + (Time.realtimeSinceStartup - tStart));

        erroneousAbilitiesToRemove.Clear();
    }
}