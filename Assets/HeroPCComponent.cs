using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Linq;
using System.Text;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
#endif

public enum ActorFlags { PARRYNEXTATTACK, EXTRADAMAGEFROMAXE, EMPOWERPALADINMOVES, TRACKED, STARTCOMBATLUCKY, GREEDYFORQUEST, NEARVINES, TRACKDAMAGE, COUNT }

public enum AdventureStats { LOOTFIND, MAGICFIND, GOLDFIND, SHOPBONUS, CORRALPETBONUS, COUNT }

public class MapClearDataPackage
{
    public int mapAreaID;
    public int mapFloor;

    public MapClearDataPackage(int mapID, int floor)
    {
        mapAreaID = mapID;
        mapFloor = floor;
    }
}


[System.Serializable]
public partial class HeroPC : Fighter
{
    public string selectedPrefab;
    public zirconAnim idleAnimation;
    public bool[] exploredAreas;
    public int lowestFloorExplored;
    public Stairs lastStairsTraveled;

    public int lastMainhandWeaponIDAttackedWith;
    public int lastOffhandEquippedID;
    public Equipment lastOffhandEquipped;

    public int regenFlaskUses;

    public float[] jobJP;
    public float[] jobJPspent;

    int money;
    public int daysPassed;

    public int monstersKilled;
    public int championsKilled;
    public int stepsTaken;

    public int stepsInDifficultTerrain;
    public bool visitedMerchant;
    public List<string> heroFeats;
    public static int[] xpCurve;

    public static int[] xpCurveMysteryDungeon = new int[21] 
    {
        0, // unused
        60,  // level 1
        160, 
        370,
        710,
        1130, // level 5
        1750,
        2550,
        3600,
        5000,
        6600, // level 10
        8700,
        11000,
        14000,
        19000,
        24000, // level 15
        32000,
        40000,
        52000,
        60000,
        70000
    };


    public List<QuestScript> myQuests;
    public List<int> mapsExploredByMapID;
    public List<int> mapFloorsExplored;
    //public List<string> myFeats;

    public int portalX = 0;
    public int portalY = 0;
    public int portalMapID = 0;
    public int timesHealedThisLevel = 0;
    public int numberOfJobChanges = 0;
    public List<QuestScript> qToRemove;
    public List<PlayingCard> gamblerHand;
    public int levelupBoostWaiting = 0;

    public float playTimeAtGameLoad;

    public GameObject diagonalOverlay;
    [HideInInspector]
    public SpriteRenderer analogTargetingOverlayRenderer;
    [HideInInspector]
    public SpriteRenderer examineModeIconRenderer;
    public Item lastEquippedItem;
    public bool beatTheGame;

    public int numPandoraBoxesOpened;

    public int newGamePlus;

    //public int lastTurnActed;

    public Vector2 portalLocationFromItemWorld;
    public CharacterJobs startingJob;

    public float[] advStats;

    public int[] championsKilledWithWeaponType;

    //public List<int> mapsCleared;
    public List<MapClearDataPackage> mapsCleared;   

    public List<Vector2> tempPetRevealTiles;

    public Weapon lastUsedMeleeWeapon;
    public Weapon lastUsedWeapon;
    public int idOfLastUsedMeleeWeapon;
    public int idOfLastUsedWeapon;

    public GameModes localGameMode; // This is used only for keeping track when going into NG+

    public JobTrialScript jobTrial;

    public List<int> shopkeepersThatRefresh;
    public TurnTypes[] previousTurnActions;

    public List<string> playerModsSavedLast;

    // Map ID : Floor Data
    public Dictionary<int, ItemDreamFloorData> dictDreamFloorData;

    public bool refreshStatusesAtEndOfTurn; // So we don't refresh UI multiple times during an attack.

    // Mystery dungeon stuff
    public MysteryDungeonSaveData myMysteryDungeonData;

    public const float PERCENT_OF_HEALTH_LIMITBREAK = 1.5f;
    public float limitBreakAmount;
    bool limitBreakDirty;

    public Dictionary<string, int> relicsDroppedOnTheGroundOrSold;

    public WeaponTypes lastWeaponTypeUsed;

    public HeroPC()
    {        
        Init();
        mapsCleared = new List<MapClearDataPackage>();
        advStats = new float[(int)AdventureStats.COUNT];
        relicsDroppedOnTheGroundOrSold = new Dictionary<string, int>();
        championsKilledWithWeaponType = new int[(int)WeaponTypes.COUNT];

        xpCurve = new int[21];
        xpCurve[0] = 0; // Not used
        xpCurve[1] = 60;
        xpCurve[2] = 160;
        xpCurve[3] = 370;
        xpCurve[4] = 800;
        xpCurve[5] = 1330;
        xpCurve[6] = 2100;
        xpCurve[7] = 3100;
        xpCurve[8] = 4400;
        xpCurve[9] = 6200;
        xpCurve[10] = 8250;
        xpCurve[11] = 10800;
        xpCurve[12] = 14000;
        xpCurve[13] = 17600;
        xpCurve[14] = 24000;
        xpCurve[15] = 31000;
        xpCurve[16] = 38000;
        xpCurve[17] = 47000;
        xpCurve[18] = 56000;
        xpCurve[19] = 66000;
        xpCurve[20] = 80000;   
               

        playerModsSavedLast = new List<string>();

        // Below shouldn't be used


        monsterCollidable = true;
        qToRemove = new List<QuestScript>();
        //myFeats = new List<string>();
        gamblerHand = new List<PlayingCard>();
        numPandoraBoxesOpened = 0;
        shopkeepersThatRefresh = new List<int>();

        previousTurnActions = new TurnTypes[3];
        for (int i = 0; i < previousTurnActions.Length; i++)
        {
            previousTurnActions[i] = TurnTypes.REST;
        }

        dictDreamFloorData = new Dictionary<int, ItemDreamFloorData>();
    }

    public static int GetXPCurve(int level)
    {
        // In Mystery Dungeons, our XP curve changes.
        if (GameMasterScript.gameLoadSequenceCompleted 
            && MapMasterScript.activeMap.IsMysteryDungeonMap() 
            && !MysteryDungeonManager.GetActiveDungeon().resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            return xpCurveMysteryDungeon[level];
        }
        else
        {
            if (level >= xpCurve.Length)
            {
                return xpCurve[xpCurve.Length - 1];
            }
            return xpCurve[level];
        }
    }

    public int GetMoney()
    {
        return money;
    }

    public bool HasMonsterPet()
    {
        if (GetMonsterPetID() <= 0)
        {
            return false;
        }
        Actor findPet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GetMonsterPetID());

        if (findPet != null)
        {
            return true;
        }
        return false;
    }

    // Used to clear all data about the player's current CORRAL pet
    public void ResetPetData()
    {        
        RemoveActorData("monsterpetid");
        RemoveActorData("monsterpetsharedid");
        RemoveActorData("pet_bog_frog");        
    }

    public int GetMonsterPetID()
    {
        return ReadActorData("monsterpetid");
    }

    public int GetMonsterPetSharedID()
    {
        return ReadActorData("monsterpetsharedid");
    }    

    public Monster GetMonsterPet()
    {
        int id = GetMonsterPetID();
        if (id <= 0)
        {
            return null;
        }
        Actor findPet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(id);

        if (findPet != null)
        {
            return findPet as Monster;
        }
        return null;
    }

    protected override void Init()
    {
        if (initialized)
        {
            return;
        }
        base.Init();
        actorFlags = new bool[(int)ActorFlags.COUNT];
        flagData = new int[(int)ActorFlags.COUNT];
        for (int i = 0; i < actorFlags.Length; i++)
        {
            actorFlags[i] = false;
        }
        exploredAreas = new bool[250000];
        SetActorType(ActorTypes.HERO);
        jobJP = new float[(int)CharacterJobs.COUNT - 2];
        jobJPspent = new float[(int)CharacterJobs.COUNT - 2];

        myQuests = new List<QuestScript>();
        mapsExploredByMapID = new List<int>();
        mapFloorsExplored = new List<int>();

        heroFeats = new List<string>();

        TurnsSinceLastCombatAction = 999;

        physicalWeaponDamageAddFlat = 0;
        physicalWeaponDamageAddPercent = 1.0f;
        allMitigationAddPercent = 1.0f;
        allDamageMultiplier = 1.0f;
        targetable = true;
        monsterCollidable = true;
        lowestFloorExplored = 0;

        if (tempRevealTiles == null)
        {
            tempRevealTiles = new List<Vector2>();
        }
        if (tempPetRevealTiles == null)
        {
            tempPetRevealTiles = new List<Vector2>();
        }
    }

    

    void CopyPreMysteryDungeonDataToCoreHeroData(MysteryDungeon md)
    {
        if (GameMasterScript.heroPCActor == null) GameMasterScript.heroPCActor = this;

        List<Item> inventoryInDungeon = null;
        EquipmentBlock gearInDungeon = null;

        MysteryDungeonSaveData mdd = myMysteryDungeonData;

        if (mdd == null)
        {
            if (Debug.isDebugBuild) Debug.Log("There is no mystery dungeon data. Why are we in this function?");
            return;
        }

        RemoveActorData("floor_of_last_mdcampfire");
        RemoveActorData("dont_scale_md_monsters_ngplus");
        myJob = CharacterJobData.GetJobDataByEnum((int)myMysteryDungeonData.jobPriorToEntry);
        MysteryDungeonManager.RestoreHeroDictActorData(md, myMysteryDungeonData);
        MysteryDungeonManager.RestoreHeroFlaskAndPandoraBoxes(md, myMysteryDungeonData);
        int moneyInDungeon = MysteryDungeonManager.RestoreHeroMoney(md, mdd);
        inventoryInDungeon = MysteryDungeonManager.RestoreHeroInventory(md, mdd);
        gearInDungeon = MysteryDungeonManager.RestoreHeroGear(md, mdd);

        RefreshEquipmentCollectionOwnership();

        MysteryDungeonManager.RestoreHeroSkills(md, mdd);
        MysteryDungeonManager.RestoreHeroStats(md, mdd);

        numPandoraBoxesOpened = mdd.pandoraBoxesPriorToEntry;

        SetDefaultWeapon(false);
        //MetaProgressScript.RemoveUnusedCustomItems(force: true);

        try
        {
            MysteryDungeonManager.CleanupRebuildSpawnTables(md);
        }
        catch(Exception e)
        {
            Debug.Log("Couldn't rebuild spawn tables.");
        }
        
        try
        {
            MysteryDungeonManager.CleanupRemoveMonsters(md);
        }
        catch(Exception e)
        {
            Debug.Log("Couldn't remove monsters.");
        }
        

        md.spawnTables.Clear();
        md.monstersInDungeon.Clear();

        // remove unique items as needed too.

        ClearBattleDataAndStatuses();

        myMysteryDungeonData = null; // null it out so it doesn't save.
        ValidateAndFixStats(true);
        TryLinkAllPairedItems();

        MysteryDungeonManager.RemoveMDItemTagsFromHeroInventoryAndEquipment();        

        GameMasterScript.gmsSingleton.SetTempGameData("finishmysterydungeonturn", GameMasterScript.turnNumber);

        RemoveActorData("allow_campfire_cooking");
    }
    public void TryLearnMonsterSkill(AbilityScript originatingAbility, string monsterSkillName)
    {
        if (myAbilities.HasAbilityRef(monsterSkillName)) return;


        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("learnmonsterskills"))
        {
            if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHANCE_WILDCHILD_LEARN_ENEMYSKILL)
            {
                StringManager.SetTag(0, originatingAbility.abilityName);
                AbilityScript template = GameMasterScript.masterAbilityList[monsterSkillName];
                AbilityScript learnt = LearnAbility(template, true, false, true);
                if (!learnt.passiveAbility && learnt.displayInList)
                {
                    // Force add to hotbar
                    UIManagerScript.AddAbilityToOpenSlot(learnt);
                }
                BattleTextManager.NewText(StringManager.GetString("learned_monster_skill"), GetObject(), Color.green, 1.2f);
                UIManagerScript.PlayCursorSound("Skill Learnt");
            }
            else
            {
                GameLogScript.LogWriteStringRef("log_almost_learned_skill");
            }
        }




    }



    public int GetActionTimerDisplay()
    {
        return (int)(actionTimer - 100);
    }

    

    public float GetPlayTime()
    {
        float playTime = playTimeAtGameLoad + (Time.fixedTime - GameMasterScript.timeAtGameStartOrLoad);
        return playTime;
    }

    public void SetActionThisTurn(TurnTypes tt)
    {
        previousTurnActions[2] = previousTurnActions[1];
        previousTurnActions[1] = previousTurnActions[0];
        previousTurnActions[0] = tt;
    }

    public void DoPassTurnStuff()
    {
        if (previousTurnActions[0] == TurnTypes.PASS && previousTurnActions[1] == TurnTypes.PASS)
        {
            if (myStats.CheckHasStatusName("emblem_hunteremblem_tier1_shadow") && !myStats.CheckHasStatusName("emblem_hunter_stealth"))
            {
                myStats.AddStatusByRef("emblem_hunter_stealth", this, 5);
                myStats.AddStatusByRefAndLog("emblem_hunter_dodge", this, 5);
            }
        }
    }

    public override void ValidateAndFixStats(bool writeFixedStats)
    {
        base.ValidateAndFixStats(writeFixedStats);
        CleanStuckVisualFX();
        TryRelinkMonsterPet(onGameLoad:false);
        myStats.RefreshAndReturnActiveSongStatus();
    }

    public void SetMonsterPetID(int id)
    {
        if (Debug.isDebugBuild) Debug.Log("Setting hero's monster pet ID to " + id);
        SetActorData("monsterpetid", id);
    }


    public void SetMonsterPetSharedID(int id)
    {
        SetActorData("monsterpetsharedid", id);
    }    

    public void TryRelinkMonsterPetToSpecificObject(Monster mon)
    {
        if (Debug.isDebugBuild) Debug.Log("Try relink monster pet to specific object " + mon.PrintCorralDebug());

        int petID = GetMonsterPetID();
        if (petID != -1)
        {
            if (Debug.isDebugBuild) Debug.Log("There is an orphaned monster corral pet " + mon.PrintCorralDebug() + " HOWEVER hero already HAS a pet ID: " + petID + " This many actors: " + summonedActors.Count + " IDs: " + summonedActorIDs.Count);

            if (GameMasterScript.dictAllActors.TryGetValue(petID, out Actor checkPet))
            {
                GameMasterScript.heroPCActor.AddSummon(checkPet);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Orphaned pet ID " + petID + " belongs to hero but doesn't even exist in the master dict?!");
            }

            UIManagerScript.UpdatePetInfo(debug:true);
            return;
        }

        SetMonsterPetID(mon.actorUniqueID);        
        
        if (mon.tamedMonsterStuff != null)
        {
            SetMonsterPetSharedID(mon.tamedMonsterStuff.sharedBankID);
        }        

        if (GameMasterScript.heroPCActor.CheckSummon(mon))
        {
            if (Debug.isDebugBuild) Debug.Log("Somehow the hero already has " + mon.PrintCorralDebug() + " as a summon but NOT a pet?");
        }
        else
        {
            GameMasterScript.heroPCActor.AddSummon(mon);
        }

        if (Debug.isDebugBuild) Debug.Log("Orphaned monster is being added as our buddied corral pet. " + mon.PrintCorralDebug());

        
        UIManagerScript.UpdatePetInfo(debug: true);
    }

    public void TryRelinkMonsterPet(bool onGameLoad)
    {
        foreach (Actor act in summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster m = act as Monster;
            if (m.turnsToDisappear > 0 && m.maxTurnsToDisappear > 0) continue;
            if (m.surpressTraits) continue;
            if (m.tamedMonsterStuff != null)
            {
                if (Debug.isDebugBuild) Debug.Log("On relink monster pet to hero, setting hero's pet ID value to " + m.actorUniqueID + " shared to " + m.tamedMonsterStuff.sharedBankID);
                SetMonsterPetID(m.actorUniqueID);
                SetMonsterPetSharedID(m.tamedMonsterStuff.sharedBankID);                

                if (onGameLoad)
                {
                    foreach(TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
                    {
                        if (tcm.monsterObject.actorUniqueID == m.actorUniqueID)
                        {
                            if (Debug.isDebugBuild) Debug.Log("Pet " + m.PrintCorralDebug() + " matches ID of existing shared corral creature " + tcm.monsterObject.PrintCorralDebug());
                            summonedActorIDs.Remove(m.actorUniqueID);
                            GameMasterScript.AssignActorID(m, 900000);
                            GameMasterScript.AddActorToDict(m);
                            SetMonsterPetID(m.actorUniqueID);
                            SetMonsterPetSharedID(tcm.sharedBankID);
                            if (Debug.isDebugBuild) Debug.Log("On load, fixed hero pet unique id to " + m.actorUniqueID);
                            summonedActorIDs.Add(m.actorUniqueID);
                        }
                    }
                }

                break;
            }
        }
    }

    // TEST function for determining probabilities
    public void Test_DrawAndEvaluateHands(int numTests)
    {
        int[] handResults = new int[(int)PokerHands.COUNT];
        PlayingCard.CreateDeck();
        Debug.Log("Initial deck size is " + PlayingCard.theDeck.Count);
        for (int i = 0; i < numTests; i++)
        {
            for (int x = 0; x < 5; x++)
            {
                PlayingCard pc = PlayingCard.DrawCard();
                while (!PlayingCard.IsCardOkForHand(pc))
                {
                    PlayingCard.ReturnCard(pc);
                    pc = PlayingCard.DrawCard();
                }
                gamblerHand.Add(pc);
            }
            PokerHands result = PlayingCard.theDeck[0].EvaluatePlayerHand();
            handResults[(int)result]++;
            //Debug.Log("Hand is drawn for test " + i + ", deck size is " + PlayingCard.theDeck.Count + " while hand size is " + gamblerHand.Count);
            foreach (PlayingCard cardToReturn in GameMasterScript.heroPCActor.gamblerHand)
            {
                PlayingCard.ReturnCard(cardToReturn);
            }
            GameMasterScript.heroPCActor.gamblerHand.Clear();
            //Debug.Log("End of test " + i + ", deck size is " + PlayingCard.theDeck.Count + " hand size is " + GameMasterScript.heroPCActor.gamblerHand.Count);
        }
        string results = "";
        for (int i = 0; i < handResults.Length; i++)
        {
            float probability = (float)handResults[i] / numTests;
            results += (PokerHands)i + ": " + handResults[i] + " (" + probability + ")\n";
        }
        Debug.Log(numTests + " hands yields " + results);
    }

    public void DrawWildCard(bool redrawingHand = false)
    {
        if (gamblerHand.Count >= 5)
        {
            GameLogScript.LogWriteStringRef("log_gambler_hand_full", GameMasterScript.heroPCActor, TextDensity.VERBOSE);
            return;
        }

        //Draw cards until we are OK with the card drawn
        //If we come up with some lousy precondition that rejects all possible card types
        //the game will crash and player's HD will be formatted and launched into the sun
        PlayingCard pc = PlayingCard.DrawCard();
        while (!PlayingCard.IsCardOkForHand(pc))
        {
            // AA 12/8/2017 - We need to return cards that aren't used to the deck, right...?
            PlayingCard.ReturnCard(pc);
            pc = PlayingCard.DrawCard();
        }
        gamblerHand.Add(pc);
        //Debug.Log("Adding " + pc.face + " " + pc.suit + " to player hand, GLEVEL: " + PlayingCard.GetGamblerLevel());

        StringManager.SetTag(0, PlayingCard.faceNames[(int)pc.face]);
        StringManager.SetTag(1, PlayingCard.suitNames[(int)pc.suit]);

        if (!redrawingHand)
        {
            string txt = StringManager.GetString("card_num_of_suit") + "!";
            BattleTextManager.NewText(txt, GetObject(), Color.green, 1.0f);
            if (myStats.CheckHasStatusName("emblem_gambleremblem_tier2_luck"))
            {
                if (gamblerHand.Count == 5)
                {
                    if (gamblerHand[0].EvaluatePlayerHand() == PokerHands.HIGHCARD)
                    {
                        PlayingCard.DiscardAndRedrawHand();
                        actionTimer = 200;
                        GameLogScript.LogWriteStringRef("log_redraw_pokerhand");
                        BattleTextManager.NewText(StringManager.GetString("misc_poker_mulligan"), GetObject(), Color.green, 1.25f);
                    }
                }
            }
            UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(false, false);
        }
    }

    public void CheckUpdateQuestHP(int amount)
    {
        if (MapMasterScript.activeMap.IsJobTrialFloor()) return;

        for (int i = 0; i < myQuests.Count; i++)
        {
            QuestScript qs = myQuests[i];
            if (qs.complete) continue;
            for (int x = 0; x < qs.qRequirements.Count; x++)
            {
                if (qs.qRequirements[x].qrType == QuestRequirementTypes.DAMAGETAKEN)
                {
                    qs.qRequirements[x].damageTaken += amount;
                    if (qs.qRequirements[x].damageTaken > qs.qRequirements[x].maxDamageTaken)
                    {
                        QuestScript.HeroFailedQuest(qs);
                    }
                }
            }
        }

    }

    public bool WouldChangingEquipmentFailQuest()
    {
        if (!MapMasterScript.activeMap.IsJobTrialFloor())
        {
            foreach (QuestScript qs in myQuests)
            {
                if (qs.complete) continue;
                foreach (QuestRequirement qr in qs.qRequirements)
                {
                    if (qr.qrType == QuestRequirementTypes.SAMEGEAR)
                    {
                        return true;
                    }
                }
            }
        }   
        return false;                 
    }

    public void CheckUpdateQuestSteps()
    {
        if (MapMasterScript.activeMap.IsJobTrialFloor()) return;
        if (MapMasterScript.activeMap.IsItemWorld()) return;
        if (MapMasterScript.activeMap.IsMysteryDungeonMap()) return;
        
        for (int i = 0; i < myQuests.Count; i++)
        {
            QuestScript qs = myQuests[i];
            if (qs.complete) continue;
            for (int x = 0; x < qs.qRequirements.Count; x++)
            {
                if (qs.qRequirements[x].qrType == QuestRequirementTypes.STEPSINDUNGEON)
                {
                    qs.qRequirements[x].stepsTaken++;
                    if (qs.qRequirements[x].stepsTaken > qs.qRequirements[x].maxStepsInDungeon)
                    {
                        QuestScript.HeroFailedQuest(qs);
                    }
                }
            }
        }
    }

    // Pass a day
    // Days passed
    public void SetLowestFloorExplored(int newFloor)
    {
        bool wandering = false;
        if (newFloor != lowestFloorExplored)
        {
            DungeonLevel dl = DungeonLevel.GetSpecificLevelData(newFloor);
            if (dl == null)
            {
                Debug.Log("WARNING: Could not get DLD for " + newFloor);
                return;
            }
            if ((!dl.bossArea) && (newFloor > 0))
            {
                //wandering = GameMasterScript.gmsSingleton.TrySpawnWanderingMerchant();                
                wandering = true;
            }
        }

        bool debug = false;

#if UNITY_EDITOR
        debug = false;
#endif

        if ((newFloor != MapMasterScript.TUTORIAL_FLOOR_2 && newFloor != MapMasterScript.JOB_TRIAL_FLOOR && newFloor != MapMasterScript.JOB_TRIAL_FLOOR2)
                || debug)
        {
            lowestFloorExplored = newFloor;
            GameMasterScript.gmsSingleton.statsAndAchievements.SetLowestFloorExplored(newFloor);

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
            if (PlatformVariables.SEND_UNITY_ANALYTICS)
            {
                Analytics.CustomEvent("explore_new_floor", new Dictionary<string, object>()
                {
                    { "level", newFloor },
                    { "plvl", myStats.GetLevel() },
                    { "job", myJob.jobEnum.ToString() },
                    { "hbslotsused", GetNumHotbarSlotsUsed() }
                });
            }
#endif
        }

        GameMasterScript.gmsSingleton.TickGameTime(1, wandering);
        GameMasterScript.heroPCActor.SetActorData("floorid_highestfloor", MapMasterScript.activeMap.floor);

        MetaProgressScript.ResetMonsterQuips();
        if (lowestFloorExplored > MetaProgressScript.lowestFloorReached)
        {
            MetaProgressScript.lowestFloorReached = lowestFloorExplored;
        }
        if ((daysPassed > 3) && (!wandering) && ((PlayerOptions.tutorialTips)) && (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_timepassing")))
        {
            if (!MapMasterScript.activeMap.IsBossFloor())
            {
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_timepassing");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
            }
        }

    }

    /// <summary>
    /// Careful - this ignores all other caps and checks
    /// </summary>
    /// <param name="amount"></param>
    public void SetMoneyUnsafe(int amount)
    {
        money = amount;
        UIManagerScript.RefreshPlayerStats();
    }


    public int ChangeMoney(int amount, bool doNotAlterFromGameMods = false)
    {
        if (MapMasterScript.activeMap.IsItemWorld() && amount > 0)
        {
            GameMasterScript.heroPCActor.AddActorData("dream_gold", amount);
        }

        if (amount > 0 && !doNotAlterFromGameMods)
        {
            amount = (int)(amount * PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.GOLD_GAIN));
            if (RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                amount = (int)(amount * RandomJobMode.GetRJGoldMultiplier());
            }
        }        

        if (money + amount < 0)
        {
            //Debug.Log("WARNING: Something lowered money below 0. WE have " + money + " but change is " + amount);
            money = 0;

        }
        else
        {
            money += amount;
            if (money >= GameMasterScript.MAX_GOLD)
            {
                money = GameMasterScript.MAX_GOLD;
            }
        }

        UIManagerScript.RefreshPlayerStats();

        //Debug.Log("Returning " + amount + " to prev function");

        return amount;
    }

    public float CalculateAverageResistance()
    {
        float baseNumber = 1f;
        float sum = 0f;
        for (int i = 0; i < cachedBattleData.resistances.Length; i++)
        {
            sum += cachedBattleData.resistances[i].multiplier;
        }

        float avgResistance = sum / cachedBattleData.resistances.Length;

        return avgResistance;
    }

    public float CalculateDodge()
    {
        float accuracyFlat = 0.0f;

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.CheckRunTriggerOn(StatusTrigger.ATTACKED))
            {
                for (int i = 0; i < se.listEffectScripts.Count; i++)
                {
                    if (se.listEffectScripts[i].procChance != 1.0f) continue;
                    if (se.listEffectScripts[i].effectType == EffectType.ATTACKREACTION)
                    {
                        AttackReactionEffect area = se.listEffectScripts[i] as AttackReactionEffect;
                        if (area.reactCondition != AttackConditions.ANY) continue;
                        // This is approximation math, not exactly calculated as it is otherwise.
                        if (area.alterAccuracyFlat != 0.0f)
                        {
                            accuracyFlat += area.alterAccuracyFlat;
                        }
                    }
                }
            }
        }

        accuracyFlat -= myEquipment.GetDodgeFromArmor();

        return accuracyFlat;
    }

    public float CalculateAverageParry()
    {
        //float avg = (cachedBattleData.parryMeleeChance + cachedBattleData.parryRangedChance) / 2f;
        float avg = cachedBattleData.parryMeleeChance;

        float parryDisp = (float)Math.Round(avg, 2) * 100f;
        float parryFlatAdd = 0.0f;
        float parryPercent = 1.0f;

        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.CheckRunTriggerOn(StatusTrigger.ATTACKED))
            {
                for (int i = 0; i < se.listEffectScripts.Count; i++)
                {
                    if (se.listEffectScripts[i].procChance != 1.0f) continue;
                    if (se.listEffectScripts[i].effectType == EffectType.ATTACKREACTION)
                    {
                        AttackReactionEffect area = se.listEffectScripts[i] as AttackReactionEffect;
                        if (area.reactCondition != AttackConditions.ANY) continue;
                        // This is approximation math, not exactly calculated as it is otherwise.
                        if (area.alterParryFlat != 0.0f)
                        {
                            parryFlatAdd += (area.alterParryFlat * 100f);
                        }
                        if (area.alterParry != 1.0f)
                        {
                            parryPercent += area.alterParry;
                        }
                    }
                }
            }
        }

        parryDisp = (parryDisp * parryPercent) + parryFlatAdd;
        if (parryDisp < 0)
        {
            parryDisp = 0;
        }

        return parryDisp;
    }

    /// <summary>
    /// Returns non-innate abilities the hero owns in the job she is currently taking.
    /// </summary>
    /// <returns></returns>
    public int NumberOfAbilitiesPurchasedInCurrentJob()
    {
        var listo = myJob.JobAbilities.Where(ja => !ja.innate && myAbilities.HasAbility(ja.ability)).ToList();
        return listo.Count;
    }
    public bool HasEnoughJPForSkillAndCanPurchase()
    {
        if ((int)myJob.jobEnum >= jobJP.Length)
        {
            return false;
        }
        int iCurrentJP = (int)jobJP[(int)myJob.jobEnum];
        bool bInStartingJob = myJob.jobEnum == startingJob;

        foreach (JobAbility ja in myJob.JobAbilities)
        {
            //don't count against any innate abilities
            if (ja.innate)
            {
                continue;
            }
            if (ja.ability == null) // 312019 - Should never be the case, but maybe it is somehow happening.
            {
                continue;
            }

            //Starting job costs are cheaper, so make sure we track that.
            int localCost = ja.jpCost;
            if (!bInStartingJob && localCost < 250 && !SharaModeStuff.IsSharaModeActive())
            {
                localCost = 250;
            }

            if (RandomJobMode.IsCurrentGameInRandomJobMode()) localCost = RandomJobMode.GetSkillCost(ja);

            //If we don't have the ability yet but can afford it, return true
            if ((!myAbilities.HasAbility(ja.ability) || ja.repeatBuyPossible) && localCost <= iCurrentJP)
            {
                if (GameMasterScript.heroPCActor.GetActorMap().floor != MapMasterScript.TOWN_MAP_FLOOR)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int GetCostForAbilityBecauseWeDoStuffIfWeArentInOurStartingJob(JobAbility ja)
    {
        if (RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return RandomJobMode.GetSkillCost(ja);            
        }
        bool bInStartingJob = myJob.jobEnum == startingJob;
        //Starting job costs are cheaper, so make sure we track that.
        int localCost = ja.jpCost;
        if (!bInStartingJob && localCost < 250)
        {
            localCost = 250;
        }

        return localCost;
    }

    public float ProcessJPGain(float amount)
    {
        if (myStats.CheckHasStatusName("status_blessjp"))
        {
            amount *= 1.15f;
            amount += 1;
        }

        if (myStats.CheckHasStatusName("jpgainup1"))
        {
            amount *= 1.15f;
            amount += 1;
        }
        if (myStats.CheckHasStatusName("status_jp10perm"))
        {
            amount *= 1.1f;
        }
        if (myStats.CheckHasStatusName("scholar"))
        {
            amount *= 1.1f;
        }

        amount *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.JP_GAIN);

        if (GameStartData.CheckGameModifier(GameModifiers.JP_HALF))
        {
            amount /= 2f;
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            if (MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.NO_JP_GAIN))
            {
                amount = 0f;
            }
            else
            {
                amount *= 1.25f;
            }
        }

        /* int emblemLevel = myEquipment.GetEmblemLevel();
        if (emblemLevel == 1)
        {
            amount *= 0.75f;
        }
        else if (emblemLevel == 2)
        {
            amount *= 0.5f;
        }
        if (emblemLevel == 3)
        {
            amount = 0.25f;
        } */
        return amount;
    }

    /// <summary>
    /// SETS JP in current job. DOES NOT add. DOES NOT include bonuses.
    /// </summary>
    /// <param name="amount"></param>
    public void SetJP(float amount)
    {
        UIManagerScript.jpGainedSinceJobScreenToggled = Mathf.Round(amount);
        jobJP[(int)myJob.jobEnum] = Mathf.Round(amount);

        CheckForJPTutorialsOrNotifications();
    }

    


    public void AddJP(float amount)
    {
        UIManagerScript.jpGainedSinceJobScreenToggled += Mathf.Round(amount);

        if (MapMasterScript.activeMap.IsItemWorld() && amount > 0)
        {
            GameMasterScript.heroPCActor.AddActorData("dream_jp", (int)amount);
        }

        jobJP[(int)myJob.jobEnum] += Mathf.Round(amount);

        if (jobJP[(int)myJob.jobEnum] >= 99999)
        {
            jobJP[(int)myJob.jobEnum] = 99999;
        }

        CheckForJPTutorialsOrNotifications();

        if (GetTotalJPGainedAndSpentInJob() >= 1000f)
        {
            SetActorData("kjpspent", 1);
        }
    }

    public void AddJP(CharacterJobs whichJob, float amount)
    {
        jobJP[(int)whichJob] += amount;

        CheckForJPTutorialsOrNotifications();
    }

    public float GetCurJP()
    {
        return jobJP[(int)myJob.jobEnum];
    }


    public bool TryLearnAbility(JobAbility newAbility)
    {
        if (newAbility.jobParent != myJob.jobEnum && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            StringManager.SetTag(0, newAbility.ability.abilityName);
            GameLogScript.GameLogWrite(StringManager.GetString("cant_learn_wrong_job"), GameMasterScript.heroPCActor);
            return false;
        }

        int cost = 0;

        bool bAlreadyHas = myAbilities.HasAbility(newAbility.ability);

        //if we have it, but haven't mastered it, grab the master cost
        if (bAlreadyHas &&
            newAbility.masterCost != 0 &&
            !myAbilities.HasMasteredAbility(newAbility.ability))
        {
            cost = newAbility.masterCost;
        }
        //if we don't have it, grab the base JP cost, kicking it up to X... wait why
        // BECAUSE cheap starter abilities are only cheap for your current job, not for multiclassing
        else if (!bAlreadyHas || newAbility.repeatBuyPossible)
        {
            cost = newAbility.jpCost;
            if (myJob.jobEnum != startingJob && cost < GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST && myJob.jobEnum != CharacterJobs.SHARA)
            {
                cost = GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST;
            }
            if (RandomJobMode.IsCurrentGameInRandomJobMode()) cost = RandomJobMode.GetSkillCost(newAbility);
        }
        //if we get here, already have the ability, and it can't be mastered
        else
        {
            return false;
        }

        if (jobJP[(int)myJob.jobEnum] < cost)
        {
            GameLogScript.GameLogWrite(StringManager.GetString("cant_learn_no_jp"), this);
            return false;
        }

        if (newAbility.repeatBuyPossible)
        {
            int curBuys = ReadActorData(newAbility.abilityRef + "_purchased");
            if (curBuys < 0) curBuys = 0;
            curBuys++;
            SetActorData(newAbility.abilityRef + "_purchased", curBuys);
        }

        LearnAbility(newAbility, true, false);

        jobJP[(int)myJob.jobEnum] -= cost;
        jobJPspent[(int)myJob.jobEnum] += cost;

        if (GetTotalJPGainedAndSpentInJob() >= 1000f)
        {
            SetActorData("kjpspent", 1);
        }

        ScanForAndLearnInnates();

        TryLearnJobMasterAbility();
        if (myJob.jobEnum == CharacterJobs.SHARA)
        {
            SharaModeStuff.UpdateStatJPCostsInJobData();
            SharaModeStuff.UpdateListOfKnownSharaPowers();
        }
        else
        {
            myJob.UpdateStatJPCostsInJobData();
        }
        if (UIManagerScript.GetUITabSelected() == UITabs.SKILLS)
        {
            UIManagerScript.singletonUIMS.GetCurrentFullScreenUI().UpdateContent();
            UIManagerScript.GetUISkillSheet().DisplayItemInfoOfLastFocusedButton();
        }
        SetBattleDataDirty();

        foreach (JobAbility ja in myJob.JobAbilities)
        {
            int localCost = ja.jpCost;
            if (myJob.jobEnum != startingJob && localCost < GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST)
            {
                localCost = GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST;
            }
            if (!myAbilities.HasAbility(ja.ability) && localCost <= jobJP[(int)myJob.jobEnum] && !ja.innate)
            {
                if (UIManagerScript.jpGainedSinceJobScreenToggled >= 100)
                {
                    string binding = "";
                    if (!PlatformVariables.GAMEPAD_ONLY)
                    {
                        binding = PlayerOptions.showControllerPrompts ? "#MENU#" : "#SKILLS#";
                        binding = CustomAlgorithms.ParseButtonAssignments(binding);
                    }
                    else
                    {
                        binding = CustomAlgorithms.ParseButtonAssignments("#MENU#");
                    }

                    StringManager.SetTag(0, binding);

                    //StringManager.SetTag(0, CustomAlgorithms.GetButtonAssignment("View Skills"));                    
                    GameLogScript.LogWriteStringRef("log_enough_jp_learn");
                    UIManagerScript.ShowLearnSkillIndicator();
                    return true;
                }
            }
        }
        UIManagerScript.HideLearnSkillIndicator();

        return true;
    }

    public void ScanForAndLearnInnates()
    {
        foreach (JobAbility ja in myJob.JobAbilities)
        {
            if (ja.innate && !myAbilities.HasAbility(ja.ability))
            {
                if (ja.innateReq <= jobJPspent[(int)myJob.jobEnum])
                {
                    LearnAbility(ja, false, true); // Does this work the way I think it works? Dunno
                    if (myJob.jobEnum == CharacterJobs.GAMBLER)
                    {
                        PlayingCard.RefreshDeck();
                    }
                }
            }
        }

        if (RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            for (int i = 0; i < RandomJobMode.innatesForThisRun.Length; i++)
            {
                JobAbility ja = RandomJobMode.innatesForThisRun[i];
                if (ja.innate && !myAbilities.HasAbility(ja.ability))
                {
                    if (ja.innateReq <= jobJPspent[(int)myJob.jobEnum])
                    {
                        LearnAbility(ja, false, true); // Does this work the way I think it works? Dunno
                        if (myJob.jobEnum == CharacterJobs.GAMBLER)
                        {
                            PlayingCard.RefreshDeck();
                        }
                    }
                }
            }
        }
    }

    // Returns newly learned thing.
    public AbilityScript LearnAbility(JobAbility newAbility, bool master, bool autoActivatePassive, bool learnFromJob = true, bool verboseMessage = false)
    {
        //Debug.Log("Request learn " + newAbility.abilityRef + " " + autoActivatePassive + " " + learnFromJob + " " + verboseMessage);

        AbilityScript abil = new AbilityScript();
        abil.SetUniqueIDAndAddToDict();
        AbilityScript.CopyFromTemplate(abil, newAbility.ability);
        bool learnt = myAbilities.HasAbility(abil);
        if (master)
        {
            myAbilities.MasterAbility(abil);
        }
        else
        {            
            myAbilities.AddNewAbility(abil, autoActivatePassive, learnFromJob, verboseMessage);
        }

        if (!abil.passiveAbility && !learnt)
        {
            // TEMPORARY - FORCE ADD TO HOTBAR
            UIManagerScript.AddAbilityToOpenSlot(abil);
        }

        if (!string.IsNullOrEmpty(newAbility.extraSkillRef))
        {
            if (!myAbilities.HasAbilityRef(newAbility.extraSkillRef))
            {
                AbilityScript newExtraAbility = GameMasterScript.masterAbilityList[newAbility.extraSkillRef];
                LearnAbility(newExtraAbility, true, true, true, verboseMessage);
            }
        }

        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.ABIL_LEARNED_EVER) == 0)
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.ABIL_LEARNED_EVER, 1);
        }
        myAbilities.VerifyEquippedPassivesAreActivated();
        SetBattleDataDirty();
        EnableWrathBarIfNeeded();

        if (learnFromJob) RandomJobMode.OnAbilityLearned(newAbility.abilityRef);

        return abil;
    }

    public AbilityScript LearnAbility(AbilityScript newAbility, bool master, bool autoActivatePassive, bool learnFromJob = true, bool verboseMessage = false)
    {
        AbilityScript abil = new AbilityScript();
        abil.SetUniqueIDAndAddToDict();
        AbilityScript.CopyFromTemplate(abil, newAbility);
        
        if (autoActivatePassive && newAbility.passiveAbility && newAbility.UsePassiveSlot)
        {
            // Don't use a passive slot here 
            int usedSlots = myAbilities.GetPassiveSlotsUsed();
            if (usedSlots == GameMasterScript.MAX_PASSIVE_SKILLS_EQUIPPABLE)
            {
                autoActivatePassive = false;
            }
        }
        
        myAbilities.AddNewAbility(abil, autoActivatePassive, learnFromJob, verboseMessage);
        //Shep: Keep hidden abilities hidden from the hot bar too
        if (!abil.passiveAbility && abil.displayInList)
        {
            // TEMPORARY - FORCE ADD TO HOTBAR
            UIManagerScript.AddAbilityToOpenSlot(abil);
        }
        
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.ABIL_LEARNED_EVER) == 0)
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.ABIL_LEARNED_EVER, 1);
        }
        SetBattleDataDirty();
        EnableWrathBarIfNeeded();
        return abil;
    }

    public void RemoveAbility(AbilityScript abil)
    {
        myAbilities.RemoveAbility(abil);
        UIManagerScript.TryRemoveAbilityFromHotbar(abil);
    }

    public void RemovePositiveTemporaryAndToggledStatuses()
    {
        List<StatusEffect> statusesToRemove = new List<StatusEffect>();
        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (!se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
            {
                statusesToRemove.Add(se);
                continue;
            }
            bool foundStatus = false;
            foreach (AbilityScript abil in myAbilities.GetAbilityList())
            {
                if (foundStatus) break;
                if (abil.toggled)
                {
                    foreach (EffectScript eff in abil.listEffectScripts)
                    {
                        if (eff.effectType != EffectType.ADDSTATUS) continue;
                        AddStatusEffect ase = eff as AddStatusEffect;
                        if (ase.statusRef == se.refName)
                        {
                            statusesToRemove.Add(se);
                            foundStatus = true;
                            abil.Toggle(false);
                            break;
                        }
                    }
                }
            }
        }

        foreach (StatusEffect se in statusesToRemove)
        {
            //Debug.Log("Removing " + se.refName);
            myStats.RemoveStatus(se, true);
        }
    }

    public override void EnableWrathBarIfNeeded()
    {
        if (myAbilities.IsDragonSoulEquipped())
        {
            PlayerHUDStatsComponent.ToggleLimitBreak(true);
            PlayerHUDStatsComponent.SetLimitBreakAmount(limitBreakAmount);
        }
        else
        {
            PlayerHUDStatsComponent.ToggleLimitBreak(false);
        }

        if (wrathBarScript == null)
        {
            Debug.Log("Player no wrathbar yet, skipping enable for now.");
            return;
        }
        if (!PlayerOptions.playerHealthBar)
        {
            wrathBarScript.ToggleWrathBar(false);
            return;
        }

        foreach (AbilityScript abil in myAbilities.abilities)
        {
            if (abil.refName == "skill_smiteevil" || abil.refName == "skill_heavyguard" || abil.refName == "skill_righteouscharge_2")
            // || abil.refName == "skill_essencestorm"
            {
                wrathBarScript.ToggleWrathBar(true);
                return;
            }
        }
        wrathBarScript.ToggleWrathBar(false);
    }

    /* public bool IsWeaponInActive(Weapon wp)
    {
        for (int i = 0; i < activeWeapons.Length; i++)
        {
            if (activeWeapons[i] == wp)
            {
                return true;
            }
        }
        return false;
    } 

    public void TryRemoveWeaponFromActives(Weapon weap)
    {
        myEquipment.Equip(myEquipment.defaultWeapon, false);
        for (int i = 0; i < activeWeapons.Length; i++)
        {
            if (activeWeapons[i] == weap)
            {
                activeWeapons[i] = null;
                UIManagerScript.RemoveWeaponFromActiveSlot(weap, i);
            }
        }
    } */

    // Auto equip algorithm.
    public void UpdateActiveGear(Item pickedUp)
    {

        if (!pickedUp.IsEquipment())
        {
            return;
        }

        if (pickedUp.itemType == ItemTypes.WEAPON && !PlayerOptions.autoEquipWeapons)
        {
            return;
        }

        if (pickedUp.itemType == ItemTypes.OFFHAND && !PlayerOptions.autoEquipBestOffhand)
        {
            return;
        }

        Equipment eq = pickedUp as Equipment;
        bool actuallyEquipped = false;

        eq.RebuildDisplayName(); // New to deal with item dream item names not always auto-updating

        switch (eq.slot)
        {
            case EquipmentSlots.WEAPON:                
                Weapon wp = eq as Weapon;
                if (myJob.jobEnum == CharacterJobs.BUDOKA && (wp.weaponType != WeaponTypes.NATURAL || wp.ReadActorData("monkweapon") != 1)) return;
                bool addedWeapon = UIManagerScript.AddWeaponToOpenSlot(wp);
                if (addedWeapon)
                {
                    if (myEquipment.GetWeapon() == null || myEquipment.IsDefaultWeapon(myEquipment.GetWeapon(), onlyActualFists: true) || myEquipment.GetWeapon().actorRefName == "weapon_fists")
                    {
                        myEquipment.Equip(wp, SND.PLAY, 0, true);
                        actuallyEquipped = true;
                    }
                    UIManagerScript.UpdateActiveWeaponInfo();
                }

                break;
            case EquipmentSlots.ARMOR:
                if (myEquipment.equipment[(int)EquipmentSlots.ARMOR] == null)
                {
                    Armor arm = (Armor)eq as Armor;
                    myEquipment.Equip(arm, SND.SILENT, 0, true);
                    actuallyEquipped = true;
                }
                break;
            case EquipmentSlots.OFFHAND:
                if (myJob.jobEnum == CharacterJobs.BUDOKA) return;
                //if (myStats.CheckHasStatusName("status_mmknightgloves")) return;
                Offhand offr = (Offhand)eq as Offhand;
                if (myEquipment.equipment[(int)EquipmentSlots.OFFHAND] == null && (!myEquipment.GetWeapon().twoHanded || (myEquipment.GetWeapon().IsWeaponBow() && offr.allowBow)))
                {
                    myEquipment.Equip(offr, SND.PLAY, 0, true);
                    actuallyEquipped = true;
                }
                break;
            case EquipmentSlots.ACCESSORY:
                if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY] == null)
                {
                    Accessory acc = (Accessory)eq as Accessory;
                    if (acc.uniqueEquip)
                    {
                        if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2] != null)
                        {
                            if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2].actorRefName != acc.actorRefName)
                            {
                                myEquipment.Equip(acc, SND.PLAY, 0, true);
                                actuallyEquipped = true;
                            }
                        }
                        else
                        {
                            myEquipment.Equip(acc, SND.PLAY, 0, true);
                            actuallyEquipped = true;
                        }
                    }
                    else
                    {
                        myEquipment.Equip(acc, SND.PLAY, 0, true);
                        actuallyEquipped = true;
                    }
                }
                else if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2] == null)
                {
                    Accessory acc = (Accessory)eq as Accessory;
                    if (acc.uniqueEquip)
                    {
                        if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY] != null)
                        {
                            if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY].actorRefName != acc.actorRefName)
                            {
                                myEquipment.Equip(acc, SND.PLAY, 1, true);
                                actuallyEquipped = true;
                            }
                        }
                        else
                        {
                            myEquipment.Equip(acc, SND.PLAY, 1, true);
                            actuallyEquipped = true;
                        }
                    }
                    else
                    {
                        myEquipment.Equip(acc, SND.PLAY, 1, true);
                        actuallyEquipped = true;
                    }
                }
                break;
            case EquipmentSlots.ACCESSORY2:
                if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY2] == null)
                {
                    Accessory acc = (Accessory)eq as Accessory;
                    if (acc.uniqueEquip)
                    {
                        if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY] != null)
                        {
                            if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY].actorRefName != acc.actorRefName)
                            {
                                myEquipment.Equip(acc, SND.PLAY, 1, true);
                                actuallyEquipped = true;
                            }
                        }
                        else
                        {
                            myEquipment.Equip(acc, SND.PLAY, 1, true);
                            actuallyEquipped = true;
                        }
                    }
                    else
                    {
                        myEquipment.Equip(acc, SND.PLAY, 1, true);
                        actuallyEquipped = true;
                    }
                }
                break;
        }

        if (actuallyEquipped)
        {
            if (GameMasterScript.heroPCActor.lastEquippedItem != eq)
            {
                StringManager.SetTag(0, eq.displayName);
                GameLogScript.LogWriteStringRef("log_auto_equip");
            }
            GameMasterScript.heroPCActor.SetBattleDataDirty();
        }



        return;
    }

    public void HeroStart(bool newGame)
    {
        // Set starting stats
        if (newGame)
        {
            SetUniqueIDAndAddToDict();
            regenFlaskUses = 3;
            money = 100;
        }

        myStats.SetMaxRegenRate(StatTypes.HEALTH, 5);
        myStats.SetMaxRegenAmount(StatTypes.HEALTH, 0.015f); // Expressed as %
        myStats.SetMaxRegenRate(StatTypes.STAMINA, 3);
        myStats.SetMaxRegenAmount(StatTypes.STAMINA, 0.02f); // Expressed as %
        myStats.SetMaxRegenRate(StatTypes.ENERGY, 5);
        myStats.SetMaxRegenAmount(StatTypes.ENERGY, 0.02f); // Expressed as %

        myStats.SetHeroBaseStats();
        actionTimer = 100f;

        dungeonFloor = 0;

        myEquipment.SetHeroDefaults(true);

        // Learn class abilities
        // TODO: Make this a separate function, differentiate between STARTING abilities and INNATE - innate will not transfer from one class to the other.

        if (newGame)
        {
            InitializeJPAndStartAbilities(true, null);
            myStats.HealToFull();
            UIManagerScript.RefreshStatuses();
            if (myJob.jobEnum != CharacterJobs.SHARA)
            {
                GameStartData.gameInSharaMode = false;
                GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;
            }
        }
    }

    List<AbilityScript> skillsToRemove;

    public bool ChangeJobs(string jobName, Consumable scroll)
    {
        //Debug.Log("Changing jobs to " + jobName);
        CharacterJobData cjd = CharacterJobData.GetJobData(jobName);
        if (cjd == null)
        {
            Debug.LogWarning("Tried to change job to " + jobName + " and this is not job.");
            return false;
        }

        if (GameMasterScript.jobChangeFromNPC)
        {
            GameMasterScript.heroPCActor.ChangeMoney(-1 * GameMasterScript.GetJobChangeCost());
            numberOfJobChanges++;

            GameLogScript.LogWriteStringRef("log_percy_jobchange");
        }

        StringManager.SetTag(0, cjd.DisplayName);
        GameLogScript.LogWriteStringRef("log_player_jobschanged");

        UIManagerScript.FlashWhite(1.2f);

        if (scroll != null)
        {
            if (!scroll.ChangeQuantity(-1))
            {
                myInventory.RemoveItem(scroll);
                UIManagerScript.RemoveItemFromHotbar(scroll);
            }
        }

        GameMasterScript.gmsSingleton.statsAndAchievements.IncrementLocalJobChanges();

        // Remove old job stuff.
        if (skillsToRemove == null) skillsToRemove = new List<AbilityScript>();
        skillsToRemove.Clear();        
        foreach (JobAbility ja in myJob.JobAbilities)
        {
            if (ja.innate)
            {
                AbilityScript toRemove = myAbilities.GetAbilityByRef(ja.abilityRef);
                if (toRemove != null)
                {
                    skillsToRemove.Add(toRemove);
                }
            }
        }
        foreach (AbilityScript abil in skillsToRemove)
        {
            myAbilities.RemoveAbility(abil);
        }

        GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("envenom");

        UIManagerScript.RefreshHotbarSkills();


        Emblem emb = myEquipment.GetEmblem();
        if (emb != null)
        {
            if (emb.jobForEmblem != cjd.jobEnum && !RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                myStats.RemoveStatusByRef("emblemwellrounded1");
                myStats.RemoveStatusByRef("emblemwellrounded2");
                myStats.RemoveStatusByRef("emblemwellrounded3");
            }
            else
            {
                foreach (MagicMod mm in emb.mods)
                {
                    if (mm.refName.Contains("emblemwellrounded"))
                    {
                        foreach (StatusEffect se in mm.modEffects)
                        {
                            if (se.refName.Contains("emblemwellrounded"))
                            {
                                myStats.AddStatus(se, this, emb);
                            }
                        }
                        break;
                    }
                }
            }

        }

        // Reset our "change clothes" prefab when changing jobs
        GameMasterScript.heroPCActor.selectedPrefab = "";

        myJob = cjd;
        GameMasterScript.gmsSingleton.UpdateHeroObject();

        if (overlays != null && overlays.Count > 0)
        {
            foreach (OverlayData od in overlays)
            {
                if (od.overlayGO != null)
                {
                    //GameObject.Destroy(od.overlayGO);
                    GameMasterScript.ReturnToStack(od.overlayGO, od.overlayGO.name.Replace("(Clone)", String.Empty));
                }
            }
            overlays.Clear();
            foreach (StatusEffect se in myStats.GetAllStatuses())
            {
                if (se.HasIngameSprite())
                {
                    se.AddSpawnedOverlayRef(this, se.direction);
                }
            }
        }

        LearnInnateAbilitiesForCurrentJob();

        TryLearnJobMasterAbility();

        EnableWrathBarIfNeeded();

        // new as of 1/25/18 to make sure stuff like Gambler's innates auto-equip
        VerifyAbilities();
        myAbilities.VerifyEquippedPassivesAreActivated();
        // ~ fin

        GameModifiersScript.CheckForInvalidBuffsAndSummons();

        CheckForJPTutorialsOrNotifications(false);

        PlayingCard.CreateDeck();
        PlayingCard.RefreshDeck();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshStatuses();
        UIManagerScript.RefreshStatusCooldowns();

        return true;
    }

    public Item GetItemByID(int itemID)
    {
        Item retItem = null;
        if (myInventory != null)
        {
            retItem = myInventory.GetItemByID(itemID);
        }

        //if it's not in the bag, maybe it's strapped to our stremfy body
        if (retItem == null && myEquipment != null)
        {
            retItem = myEquipment.GetItemByID(itemID);
        }

        return retItem;
    }

    public void TryEquip(Equipment eq, SND play, bool showText)
    {
        bool success;
        if (eq == null)
        {
            Debug.Log("Trying to equip null?");
            success = myEquipment.Equip(myEquipment.defaultWeapon, play, 0, showText);
        }
        else
        {
            success = myEquipment.Equip(eq, play, 0, true);
        }
        if (success == true)
        {

        }
        else
        {
            if (eq != null)
            {
                //Debug.Log("Failed to equip " + eq.displayName);
            }
            else
            {
                Debug.Log("Failed to equip null equipment");
            }

        }
    }

    public void CheckForAndSetJobMasteryFlag()
    {
        // Only use code below when we're ready to unveil Job Trials & Emblems
        if (HasMasteredJob(myJob) || RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            if (myJob.jobEnum == CharacterJobs.DUALWIELDER)
            {
                GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Calligrapher_Mastered();
            }

            if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3 || GameStartData.NewGamePlus > 0 ||
                RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                GameEventsAndTriggers.MarkCurrentHeroJobMastered();
            }            
            else
            {
                GameEventsAndTriggers.MarkCurrentHeroJobNotMastered();
                
            }
        }
        else
        {
            GameEventsAndTriggers.MarkCurrentHeroJobNotMastered();            
        }
    }

    public void TryLearnJobMasterAbility()
    {
        CheckForAndSetJobMasteryFlag();

        if (myAbilities.HasAbilityRef(myJob.MasterAbility.abilityRef))
        {
            return;
        }
        foreach (JobAbility ja in myJob.JobAbilities)
        {
            if (ja == myJob.MasterAbility) continue;

            if (ja.postMasteryAbility) continue;

            if (!myAbilities.HasAbilityRef(ja.abilityRef))
            { 
                return;
            }
        }

        GameMasterScript.gmsSingleton.statsAndAchievements.JobFullyMastered();
        LearnAbility(myJob.MasterAbility, true, true, true);
    }

    public void Unequip(EquipmentSlots slot, bool showText)
    {
        myEquipment.Unequip(slot, true, SND.PLAY, showText);
    }

    public void Unequip(Item itm, bool showText = false)
    {
        for (EquipmentSlots es = EquipmentSlots.WEAPON; es <= EquipmentSlots.ANY; es++)
        {
            if (myEquipment.equipment[(int)es] == itm)
            {
                myEquipment.Unequip(es, true, SND.PLAY, showText);
            }
        }
    }

    // Deprecated probably
    public bool TryCookRecipe(string recipeRef)
    {
        Dictionary<Item, int> itemsUsed = CookingScript.CheckRecipe(recipeRef, myInventory.GetInventory());
        if (itemsUsed == null)
        {
            GameLogScript.LogWriteStringRef("log_error_ingredients");
            return false;
        }
        foreach (Item itm in itemsUsed.Keys)
        {
            myInventory.ChangeItemQuantityAndRemoveIfEmpty(itm, -(1 * itemsUsed[itm]));
        }
        Recipe r = CookingScript.FindRecipe(recipeRef);
        Item newFood = Item.GetItemTemplateFromRef(r.itemCreated);
        Item newItem = new Consumable();
        newItem.CopyFromItem(newFood);
        newItem.SetUniqueIDAndAddToDict();
        myInventory.AddItem(newItem, true);
        StringManager.SetTag(0, newItem.displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("cook_success"), this);
        return true;
    }

    public zirconAnim GetIdleAnim()
    {
        return idleAnimation;

    }

    public List<string> GetMissingSetPieces()
    {
        List<GearSet> incompleteSets = new List<GearSet>();
        List<string> ownedSetItems = new List<string>();
        GearSet evaluateSet;
        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[i];
            if (eq != null)
            {
                if (eq.gearSet != null)
                {
                    ownedSetItems.Add(eq.actorRefName);
                    evaluateSet = eq.gearSet;
                    if (!incompleteSets.Contains(eq.gearSet))
                    {
                        if (GameMasterScript.heroPCActor.ReadActorData(evaluateSet.refName) < evaluateSet.gearPieces.Count)
                        {
                            //Debug.Log(evaluateSet.refName + " is an incomplete set, prioritize this.");
                            incompleteSets.Add(evaluateSet);
                        }
                    }
                }
            }
        }

        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.IsEquipment())
            {
                Equipment eq = itm as Equipment;
                if (eq != null)
                {
                    if (eq.gearSet != null)
                    {
                        ownedSetItems.Add(eq.actorRefName);
                    }
                }
            }
        }

        //Debug.Log("Incomplete set count " + incompleteSets.Count);
        List<string> priorityRefs = new List<string>();
        foreach (GearSet gs in incompleteSets)
        {
            foreach (Item itm in gs.gearPieces)
            {
                if (!ownedSetItems.Contains(itm.actorRefName))
                {
                    priorityRefs.Add(itm.actorRefName);
                }
            }
        }

        return priorityRefs;
    }

    public void ClearBattleDataAndStatuses()
    {
        List<StatusEffect> remover = new List<StatusEffect>();
        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (!se.isPositive && !se.passiveAbility)
            {
                remover.Add(se);
            }
            if (se.isPositive && se.curDuration > 1)
            {
                remover.Add(se);
            }
        }
        foreach (StatusEffect se in remover)
        {
            myStats.RemoveStatus(se, true);
        }

        ClearCombatTargets();
        ClearCombatAllies();

        // Don't destroy the player's pet, lol.

        if (summonedActors != null)
        {
            List<Actor> toDestroy = new List<Actor>();
            foreach (Actor act in summonedActors)
            {
                if (act != GameMasterScript.heroPCActor.GetMonsterPet())
                {
                    toDestroy.Add(act);
                }                
            }
            //summonedActors.Clear();
            foreach (Actor act in toDestroy)
            {
                GameMasterScript.gmsSingleton.DestroyActor(act);
                summonedActors.Remove(act);
            }
        }
    }

    public void ResetHeroTurnData()
    {
        previousPosition = GetPos();
        TurnsSinceLastCombatAction++;
        if (turnsSinceLastStun >= 0)
        {
            turnsSinceLastStun++;
        }
        turnsSinceLastDamaged++;
        turnsSinceLastSlow++;
        acted = false;

    }

    public void ResetMapsAndAreasExplored()
    {
        exploredAreas = new bool[250000];
        mapsExploredByMapID.Clear();
        mapsCleared.Clear();
        mapFloorsExplored.Clear();
    }

    public void TryLinkAllPairedItems()
    {
        // Load fist pair.
        int idOfFistPair = ReadActorData("pairedfistitem");
        if (idOfFistPair >= 0)
        {
            Equipment findGear = myInventory.GetItemByID(idOfFistPair) as Equipment;
            if (findGear == null) 
            {
                // Item is not in our inventory, so check in all our *equipped* items
                findGear = myEquipment.GetItemByID(idOfFistPair) as Equipment;
            }
            if (findGear != null && myEquipment.defaultWeapon != null)
            {
                myEquipment.defaultWeapon.PairWithItem(findGear, true, true);
            }
        }

        // Does current equipped offhand have a paired item?
        Equipment offhand = myEquipment.GetOffhand();
        if (offhand != null)
        {

            offhand.LinkAllPairedItems();
        }

        // Current weapons?
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            if (UIManagerScript.hotbarWeapons[i] != null)
            {
                UIManagerScript.hotbarWeapons[i].LinkAllPairedItems();
            }
        }

        // How about every other item in the inventory?
        foreach (Item itm in myInventory.GetInventory())
        {
            Equipment eq = itm as Equipment;
            if (eq != null)
            {
                eq.LinkAllPairedItems();
            }
        }
    }

    public int GetMaxThaneSongDuration()
    {
        int baseValue = 7;
        if (myStats.CheckHasStatusName("thanebonus2"))
        {
            baseValue = 10;
        }
        return baseValue;
    }

    public int GetThaneSongLevel()
    {
        return myStats.CountStatusesByFlag(StatusFlags.THANESONG);
    }

    public void IncreaseSongLevel(StatusEffect latestSong = null)
    {
        if (latestSong == null)
        {
            foreach (StatusEffect song in StatBlock.activeSongs)
            {
                latestSong = song;
            }
        }

        if (latestSong == null) return;

        int songIntensity = ReadActorData("songintensity");

        if (songIntensity < 0)
        {
            songIntensity = 0;
        }

        int levelNumber;

        string numParsed = latestSong.refName.Substring(latestSong.refName.Length - 1);
        if (Int32.TryParse(numParsed, out levelNumber))
        {
            if (levelNumber >= 3)
            {
                return;
            }
            levelNumber++;
            // This should go from song_endurance_1 to song_endruance_2
            string searchRefName = latestSong.refName.Substring(0, latestSong.refName.Length - 1) + levelNumber;
            myStats.AddStatusByRef(searchRefName, this, (int)latestSong.curDuration);
            CombatManagerScript.GenerateSpecificEffectAnimation(GetPos(), "FervirBuff", null, true);
            StringManager.SetTag(0, latestSong.abilityName);
            GameLogScript.LogWriteStringRef("log_song_extended");
        }

        songIntensity = 0;

        SetActorData("songintensity", songIntensity);

        // Song level went up

        myStats.CheckRunAndTickAllStatuses(StatusTrigger.THANESONG_LEVELUP);
    }

    public int GetMaxWeaponRange()
    {
        int maxRange = 1;
        Weapon wLongest = myEquipment.GetWeapon();
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            int localRange = UIManagerScript.hotbarWeapons[i].range;
            if (IsOverdrawingActiveOnWeaponOrPairedQuiver(UIManagerScript.hotbarWeapons[i]))
            //if (UIManagerScript.hotbarWeapons[i].HasModByRef("mm_overdraw"))
            //if (myStats.CheckHasStatusName("status_overdraw"))
            {
                localRange++;
            }
            if (localRange > maxRange)
            {
                maxRange = localRange;
                wLongest = UIManagerScript.hotbarWeapons[i];
            }
        }

        return maxRange;
    }

    public Weapon GetHighestRangedWeaponFromHotbar()
    {
        int maxRange = 1;
        Weapon wLongest = myEquipment.GetWeapon();
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            int localRange = UIManagerScript.hotbarWeapons[i].range;
            //if (UIManagerScript.hotbarWeapons[i].HasModByRef("mm_overdraw"))
            //if (myStats.CheckHasStatusName("status_overdraw"))
            if (IsOverdrawingActiveOnWeaponOrPairedQuiver(UIManagerScript.hotbarWeapons[i]))
            {
                localRange++;
            }
            if (localRange > maxRange)
            {
                maxRange = localRange;
                wLongest = UIManagerScript.hotbarWeapons[i];
            }
        }
        return wLongest;
    }

    public bool HasAnyRangedWeapon()
    {
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            if (myEquipment.IsWeaponRanged(UIManagerScript.hotbarWeapons[i]))
            {
                return true;
            }
        }
        return false;
    }

    public void TryIncreaseSongDuration(int extraDur)
    {
        int lastTurnSongExtended = ReadActorData("turnsongextended");
        if (lastTurnSongExtended != GameMasterScript.turnNumber)
        {
            int maxThaneSongDuration = GetMaxThaneSongDuration();
            if (StatBlock.activeSongs.Count > 0)
            {
                // extend the duration of songblade songs, which are their own thing and don't have intensities at all            
                // List<StatusEffect> songbladeSongs = myStats.GetAllStatuses().Where(a => a.statusFlags[(int)StatusFlags.SONGBLADE]).ToList();
                // songbladeSongs.ForEach(a => a.ExtendDuration(extraDur, maxThaneSongDuration));

                StatusEffect latestSong = null;

                StatBlock.activeSongs.ForEach(a => a.ExtendDuration(extraDur, maxThaneSongDuration));
                latestSong = StatBlock.activeSongs[StatBlock.activeSongs.Count - 1];

                SetActorData("turnsongextended", GameMasterScript.turnNumber);

                if (StatBlock.activeSongs.Count < 3)
                {
                    int songIntensity = ReadActorData("songintensity");

                    if (songIntensity < 0)
                    {
                        songIntensity = 0;
                    }

                    songIntensity += extraDur;

                    int localThresh = CombatManagerScript.THANESONG_INTENSITY_ADVANCE_THRESHOLD;
                    if (myStats.CheckHasStatusName("thanebonus2"))
                    {
                        localThresh = CombatManagerScript.THANESONG_INTENSITY_ADVANCE_THRESHOLD_WITH_MASTERY;
                    }

                    if (songIntensity >= localThresh)
                    {
                        IncreaseSongLevel(latestSong);
                        songIntensity = 0;
                    }

                    SetActorData("songintensity", songIntensity);
                }
            }
        }
    }

    public void TryHealThroughThaneSong(int resourceAmount)
    {
        if (resourceAmount <= 0) return;

        if (!myStats.CheckHasStatusName("song_spirit_3") && !myStats.CheckHasStatusName("song_spirit_3_songblade")) return;

        float percentRestore = resourceAmount * 0.002f;

        float healAmt = (myStats.GetMaxStat(StatTypes.HEALTH) * percentRestore) + UnityEngine.Random.Range(1f, 5f);

        myStats.ChangeStat(StatTypes.HEALTH, healAmt, StatDataTypes.CUR, true);

        BattleTextManager.NewDamageText((int)healAmt, true, Color.green, GetObject(), 0f, 1f);
        StringManager.SetTag(0, ((int)healAmt).ToString());
        GameLogScript.LogWriteStringRef("log_heal_songspirit");
    }

    public bool CheckTempRevealTile(Vector2 v2)
    {
        if (tempRevealTiles.Contains(v2)) return true;

        if (tempPetRevealTiles.Contains(v2)) return true;

        return false;
    }

    public bool CheckIfTileIsTrulyVisible(Vector2 coords, bool viewerIsHero = false, bool treatForceFieldsAsBlocking = false)
    {
        //This was changed to avoid false positives due to the temp reveal system. The temp reveal lists
        //were not being updated correctly, and this CheckTempRevealTile would return FALSE for a tile that
        //was revealed by a pet but also not in the list for whatever reason.


        // AA: For some reason I'm still using Bresenhams for tile visibility
        // That should be the last ditch check, for the hero.        
        bool standardLOSCheck = MapMasterScript.CheckTileToTileLOS(GetPos(), coords, this, MapMasterScript.activeMap, treatForceFieldsAsBlocking);
        //Debug.Log("Check if " + coords + " is visible? " + standardLOSCheck);
        if (viewerIsHero)
        {
            if (standardLOSCheck)
            {
                return true;
            }
            else
            {
                if (!MapMasterScript.activeMap.GetTile(coords).IsEmpty()) // Is there something potentially hittable here?
                {
                    //Debug.Log(coords + " is not empty!");
                    // Well let's be generous and use the old LOS method then, which is still used elsewhere.
                    return CustomAlgorithms.CheckBresenhamsLOS(GetPos(), coords, MapMasterScript.activeMap, treatForceFieldsAsBlocking);
                }
                else
                {
                    return standardLOSCheck;
                }

            }
        }

        //Here, we have objective truth.
        return standardLOSCheck;
    }

    public float GetTotalJPGainedAndSpentInJob(CharacterJobs specificJob = CharacterJobs.COUNT)
    {
        if (specificJob == CharacterJobs.COUNT)
        {
            return jobJP[(int)myJob.jobEnum] + jobJPspent[(int)myJob.jobEnum];
        }
        else
        {
            return jobJP[(int)specificJob] + jobJPspent[(int)specificJob];
        }

    }

    public void UpdatePrefab()
    {
        if (!string.IsNullOrEmpty(selectedPrefab))
        {
            GameMasterScript.gmsSingleton.UpdateHeroObject();
            GameMasterScript.heroPCActor.EnableWrathBarIfNeeded();
        }
    }

    public bool CanCollectWildCards()
    {
        if (myStats.CheckHasStatusName("status_collect_wildcards"))
        {
            return true;
        }
        return false;
    }

    public void CheckForNewInfusion()
    {
        if (myStats.GetLevel() == 5)
        {
            SetActorData("infuse1", 99);
        }
        else if (myStats.GetLevel() == 10)
        {
            SetActorData("infuse2", 99);
        }
        else if (myStats.GetLevel() == 15)
        {
            SetActorData("infuse3", 99);

        }
    }

    public void CheckForSpearMastery3(Monster mn)
    {
        float tailChance = CombatManagerScript.CHANCE_SPEARMASTERY3;

        tailChance += (0.07f * GameMasterScript.heroPCActor.turnsInSamePosition);
        if (tailChance > 0.3f) tailChance = 0.3f;

        if (UnityEngine.Random.Range(0, 1f) > tailChance) return;
        if (myEquipment.GetWeaponType() != WeaponTypes.SPEAR) return;
        if (!myStats.CheckHasStatusName("spearmastery3")) return;

        if (MapMasterScript.GetGridDistance(mn.GetPos(), GetPos()) > 1) return;
        if (mn.actorfaction == Faction.PLAYER) return;
        BattleTextManager.NewText(StringManager.GetString("misc_scorpiontail"), GetObject(), Color.yellow, 0.5f);
        CombatManagerScript.Attack(this, mn);
    }

    public bool HasPlayerLearnedUltimateWeaponTechnique()
    {
        if (ReadActorData("learned_ultimate_weapontech") == 1)
        {
            return true;
        }
        return false;
    }

    public void SetPlayerLearnedUltimateWeaponTechnique(bool value)
    {
        if (value)
        {
            SetActorData("learned_ultimate_weapontech", 1);
        }
        else
        {
            RemoveActorData("learned_ultimate_weapontech");
        }
    }

    public void KilledChampionWithWeapon()
    {
        championsKilledWithWeaponType[(int)myEquipment.GetWeaponType()]++;

        if (championsKilledWithWeaponType[(int)myEquipment.GetWeaponType()] >= GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE)
        {
            SetActorData("champskilled_ready_" + myEquipment.GetWeaponType().ToString().ToLowerInvariant(), 1);
        }
    }

    public void ClearMap(Map mapToClear)
    {
        if (!CheckIfMapCleared(mapToClear))
        {
            MapClearDataPackage mcdp = new MapClearDataPackage(mapToClear.mapAreaID, mapToClear.floor);
            mapsCleared.Add(mcdp);
        }
    }

    public bool CheckIfMapClearedByFloor(int floor)
    {
        foreach (MapClearDataPackage checkMap in mapsCleared)
        {
            if (floor == checkMap.mapFloor)
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckIfMapCleared(Map m)
    {
        foreach (MapClearDataPackage checkMap in mapsCleared)
        {
            if (m.mapAreaID == checkMap.mapAreaID || m.floor == checkMap.mapFloor)
            {
                return true;
            }
        }

        return false;
    }

    public float GetMonsterMalletThreshold()
    {
        float basicThreshold = 0.1599f;
        if (GameMasterScript.heroPCActor.ReadActorData("beastlakequest") == 2)
        {
            basicThreshold = 0.20999f;
        }
        return basicThreshold;
    }

    /// <summary>
    /// Returns FALSE if there are no items left in stack.
    /// </summary>
    /// <param name="itm"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool TryChangeQuantity(Item itm, int amount)
    {
        Consumable c = itm as Consumable;
        if (!c.IsItemFood())
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.5f && myStats.CheckHasStatusName("status_bountiful") && !itm.legendary)
            {
                StringManager.SetTag(0, itm.displayName);
                GameLogScript.LogWriteStringRef("log_item_recycle");
                return true;
            }
        }

        return c.ChangeQuantity(amount);
    }

    public void BoostStatFromLevelup(StatTypes stat)
    {
        GameMasterScript.heroPCActor.levelupBoostWaiting--;
        GameMasterScript.heroPCActor.CheckForNewInfusion();
        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
        GameMasterScript.heroPCActor.myStats.ChangeStatAndSubtypes(stat, 3f, StatDataTypes.ALL);
        StringManager.SetTag(0, StringManager.GetString("stat_" + stat.ToString().ToLowerInvariant()));
        GameLogScript.GameLogWrite(StringManager.GetString("boost_stat_level_up"), GameMasterScript.heroPCActor);
        UIManagerScript.CheckForInfusionDialog();
    }

    public void ResetAbilityCooldownWithModifiers(AbilityScript abil)
    {
        abil.ResetCooldown();

        // Light armor mastery 1 check
        if (abil.GetCurCooldownTurns() > 2 && myEquipment.GetArmorType() == ArmorTypes.LIGHT && abil.abilityFlags[(int)AbilityFlags.MOVESELF])
        {
            abil.ChangeCurrentCooldown(-1);
        }
    }
    public void CheckForNotorious()
    {
        if (!MapMasterScript.mapLoaded) return;

        DungeonLevel dl = MapMasterScript.activeMap.dungeonLevelData;
        if (!dl.safeArea && !MapMasterScript.activeMap.clearedMap && !MapMasterScript.activeMap.IsBossFloor() && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("notorious"))
        {            
            GameMasterScript.heroPCActor.AddActorData("notorious_turn", 1);
            if (GameMasterScript.heroPCActor.ReadActorData("notorious_turn") >= GameMasterScript.NOTORIOUS_NEWCHAMP_SPAWN_DELAY)
            {
                if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.NOTORIOUS_NEWCHAMP_SPAWNCHANCE)
                {
                    GameMasterScript.heroPCActor.RemoveActorData("notorious_turn");
                    if (dl.spawnTable == null || dl.spawnTable.GetNumActors() == 0)
                    {
                        return;
                    }

                    // Shara mode can swallow this if we are at our cap of wandering monsters spawned.
                    if (SharaModeStuff.SharaWanderingMonsterCheck(MapMasterScript.activeMap.initialSpawnedMonsters, false))
                    {
                        return;
                    }

                    string champRef = dl.spawnTable.GetRandomActorRef();
                    Monster champ = MonsterManagerScript.CreateMonster(champRef, true, true, false, 0f, false);
                    MapTileData mtd = MapMasterScript.activeMap.GetRandomNonCollidableTile(GameMasterScript.heroPCActor.GetPos(), 10, true, false);
                    while (MapMasterScript.GetGridDistance(mtd.pos, GameMasterScript.heroPCActor.GetPos()) <= 5)
                    {
                        mtd = MapMasterScript.activeMap.GetRandomNonCollidableTile(GameMasterScript.heroPCActor.GetPos(), 10, true, false);
                    }
                    int maxChampMods = 1;
                    if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 7) maxChampMods++;
                    if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 12) maxChampMods++;
                    if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 15) maxChampMods++;
                    for (int i = 0; i < maxChampMods; i++)
                    {
                        if (i > 0 && UnityEngine.Random.Range(0, 2) == 0) break; // 50% chance for each new mod above the first
                        champ.MakeChampion(true);
                    }
                    champ.AddAggro(GameMasterScript.heroPCActor, 99f);
                    champ.SetMyTarget(this);
                    champ.myTargetUniqueID = actorUniqueID;
                    champ.turnsToLoseInterest = 999;
                    champ.xpMod *= 1.15f;

                    if (UnityEngine.Random.Range(0,1f) <= GameMasterScript.NOTORIOUS_SPECIAL_ITEM_CHANCE)
                    {
                        Item usefulItem = null;
                        if (UnityEngine.Random.Range(0,2) == 0)
                        {
                            usefulItem = ItemWorldUIScript.CreateItemWorldOrb(champ.challengeValue, false, false);
                        }
                        else
                        {
                            usefulItem = LootGeneratorScript.GenerateLootFromTable(champ.challengeValue, 0f, "notorious_items");
                        }
                        
                        champ.myInventory.AddItem(usefulItem, true);
                    }

                    MapMasterScript.activeMap.PlaceActor(champ, mtd);
                    MapMasterScript.singletonMMS.SpawnMonster(champ);

                }
            }
        }
    }

    public int NumberOfPassiveSlotsTaken()
    {
        //Count how many are both equipped and use a passive slot
        List<AbilityScript> abil = myAbilities.abilities.FindAll(ab => ab.passiveEquipped && ab.passiveAbility && ab.UsePassiveSlot && !ab.CheckAbilityTag(AbilityTags.DRAGONSOUL));
        foreach(AbilityScript a in abil)
        {
            Debug.Log(a.refName + " " + a.abilityName + " " + a.passiveAbility + " " + a.passiveEquipped);
        }
        return abil.Count;
    }

    public int GetNumHotbarSlotsUsed()
    {
        int numAbil = 0;
        for (int i = 0; i < UIManagerScript.hotbarAbilities.Length; i++)
        {
            if (UIManagerScript.hotbarAbilities[i].actionType != HotbarBindableActions.NOTHING)
            {
                numAbil++;
            }
        }
        return numAbil;
    }

    public void InheritVisionFromPets()
    {
        tempPetRevealTiles.Clear();
        foreach (Actor act in summonedActors)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster m = act as Monster;
                if (m.bufferedFaction == Faction.PLAYER)
                {
                    CustomAlgorithms.GetTilesAroundPoint(m.GetPos(), 1, MapMasterScript.activeMap);
                    for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                    {
                        if (!MapMasterScript.InBounds(CustomAlgorithms.tileBuffer[i].pos)) continue;
                        //visibleTilesArray[(int)CustomAlgorithms.tileBuffer[i].pos.x, (int)CustomAlgorithms.tileBuffer[i].pos.y] = true;                        
                        tempPetRevealTiles.Add(CustomAlgorithms.tileBuffer[i].pos);
                        MapMasterScript.activeMap.exploredTiles[(int)CustomAlgorithms.tileBuffer[i].pos.x, (int)CustomAlgorithms.tileBuffer[i].pos.y] = true;
                    }

                }
            }
        }
        tempPetRevealTiles = tempPetRevealTiles.Distinct().ToList();
    }
    public void VerifyAbilities()
    {
        if (myJob.jobEnum == CharacterJobs.SHARA)
        {
            SharaModeStuff.UpdateStatJPCostsInJobData();
            SharaModeStuff.UpdateListOfKnownSharaPowers();
        }
        else
        {
            myJob.UpdateStatJPCostsInJobData();
        }

        // Remove statuses caused by job-specific passive abilities
        if (myJob.jobEnum != CharacterJobs.EDGETHANE)
        {
            myStats.RemoveStatusByRef("edgethane_survive50");
        }

        // Now verify that hero is equipping all skills that should be equipped.
        foreach (AbilityScript abil in myAbilities.GetAbilityList())
        {
            if (!abil.passiveAbility) continue;
            if (abil.UsePassiveSlot) continue;
            if (!abil.passiveEquipped && !abil.displayInList)
            {
                myAbilities.EquipPassiveAbility(abil);
            }
        }

        // Also connect Delayed Teleport destination square. Yeah this is a dumb special case.

        StatusEffect se = myStats.GetStatusByRef("status_teleporting");
        if (se != null)
        {
            se.listEffectScripts[0].positions.Clear();
            int posX = ReadActorData("delayedteleportposx");
            if (posX >= 1)
            {
                int posY = ReadActorData("delayedteleportposy");
                if (posY >= 1)
                {
                    se.listEffectScripts[0].positions.Add(new Vector2((float)posX, (float)posY));
                    RemoveActorData("delayedteleportposx");
                    RemoveActorData("delayedteleportposy");
                }
            }
        }
    }

    public bool CanHeroParry(AttackType aType)
    {
        if (aType == AttackType.ABILITY && !myStats.CheckHasStatusName("emblem_sworddanceremblem_tier2_parry"))
        {
            return false;
        }

        if (myEquipment.IsCurrentWeaponRanged()) return false;

        /* if (myEquipment.GetWeaponType() == WeaponTypes.SWORD || myStats.CheckHasStatusName("status_alwaysriposte"))            
        {
            return true;
        } */

        return true;

    }

    public void TrySwitchToPreviousUsedWeapon()
    {
        if (GameMasterScript.gmsSingleton.ReadTempGameData("hero_autoswitched_weapon") == 1)
        {
            Weapon lastUsed = lastUsedWeapon;
            int idOfLastOffhand = ReadActorData("offhand_id_preswap");
            Item offhand = null;
            if (idOfLastOffhand > 0)
            {
                offhand = GameMasterScript.gmsSingleton.TryLinkActorFromDict(idOfLastOffhand) as Item;
            }
            

            if (GameMasterScript.gmsSingleton.ReadTempGameData("hero_switchedto_rangedweap") == 1)
            {
                lastUsed = lastUsedMeleeWeapon;
            }

            if (lastUsed != null)
            {
                if (UIManagerScript.IsWeaponInHotbar(lastUsed))
                {
                    int indexOfSlot = UIManagerScript.GetWeaponHotbarIndex(lastUsed);
                    UIManagerScript.SwitchActiveWeaponSlot(indexOfSlot, false, false);
                }
            }

            if (offhand != null && offhand != myEquipment.GetOffhand())
            {
                myEquipment.Equip(offhand as Equipment, SND.SILENT, EquipmentSlots.OFFHAND, false);
            }

            GameMasterScript.gmsSingleton.SetTempGameData("hero_autoswitched_weapon", 0);
            GameMasterScript.gmsSingleton.SetTempGameData("hero_switchedto_rangedweap", 0);
        }
    }

    public void VerifyStatusesAddedByGearAreValid()
    {
        List<StatusEffect> statusesToRemove = new List<StatusEffect>();
        foreach (StatusEffect se in myStats.GetAllStatuses())
        {
            if (se.sourceOfEffectIsEquippedGear && se.addedByActorID > 0)
            {
                Actor gear = GameMasterScript.gmsSingleton.TryLinkActorFromDict(se.addedByActorID);
                if (gear == null || gear.GetActorType() != ActorTypes.ITEM)
                {
                    Debug.Log("<color=red>Source of " + se.refName + " on player just doesn't exist. Removing it.</color>");
                    statusesToRemove.Add(se);
                }
                else
                {
                    Equipment eq = gear as Equipment;
                    if (eq == null)
                    {
                        statusesToRemove.Add(se);
                    }
                    else if (!myEquipment.IsEquipped(eq))
                    {
                        Debug.Log("<color=red>Source of " + se.refName + " on player is not equipped anymore. Removing it.</color>");
                        statusesToRemove.Add(se);
                    }
                    else
                    {
                        if (!eq.CheckIfGrantsStatusViaMod(se.refName))
                        {
                            Debug.Log("<color=red>Source of " + se.refName + " is equipped, but somehow doesn't cause this status. Removing it...</color>");
                            statusesToRemove.Add(se);
                        }
                    }
                    
                }
            }
        }
        foreach (StatusEffect se in statusesToRemove)
        {
            myStats.RemoveStatus(se, true);
        }
    }

    public Sprite GetPortrait()
    {
        Sprite fromMods = PlayerModManager.TryGetPortraitSpriteFromMods(myJob.jobEnum);
        if (fromMods != null)
        {
            return fromMods;
        }
        return myJob.PortraitSprite;
    }

    public string GetAbilityInfoWithModifiers(AbilityScript ability)
    {
        string newText = "";
        AbilityScript modifiedAbility = null;
        GameMasterScript.gmsSingleton.SetAbilityToTryWithModifiedCostsAndInformation(ability, false, ref modifiedAbility);
        int cd = modifiedAbility.maxCooldownTurns;

        string cooldownAbbr = StringManager.GetString("misc_cooldown_abbreviation");

        string adder = " (" + cooldownAbbr + ": <color=#40b843>";
        if (modifiedAbility.GetCurCooldownTurns() > 0)
        {
            cd = modifiedAbility.GetCurCooldownTurns();
            adder = " (" + StringManager.GetString("misc_cooldown_remaining") + ": <color=red>";
        }
        newText = UIManagerScript.cyanHexColor + modifiedAbility.abilityName + "</color>" + adder + cd + "</color>)\n";

        //get the modified cost of the ability here

        int stamCost = modifiedAbility.staminaCost;
        int energyCost = modifiedAbility.energyCost;
        int healthCost = modifiedAbility.healthCost;
        int spiritsRequired = modifiedAbility.spiritsRequired;

        if (stamCost > 0)
        {
            newText += StringManager.GetString("stat_stamina") + ": <color=#40b843>" + stamCost + " </color> ";
        }
        if (energyCost > 0)
        {
            newText += StringManager.GetString("stat_energy") + ": <color=yellow>" + energyCost + " </color> ";
        }
        if (modifiedAbility.energyReserve > 0)
        {
            StringManager.SetTag(6, modifiedAbility.energyReserve.ToString());
            StringManager.SetTag(7, UIManagerScript.cyanHexColor + StringManager.GetString("stat_energy") + "</color>");
            newText += "\n" + StringManager.GetString("misc_reserve_stat");
        }
        if (modifiedAbility.staminaReserve > 0)
        {
            StringManager.SetTag(6, modifiedAbility.staminaReserve.ToString());
            StringManager.SetTag(7, UIManagerScript.greenHexColor + StringManager.GetString("stat_stamina") + "</color>");
            newText += "\n" + StringManager.GetString("misc_reserve_stat");
        }
        if (healthCost > 0)
        {
            newText += StringManager.GetString("misc_healthcost") + ": " + UIManagerScript.redHexColor + healthCost + " </color> ";
        }
        if (spiritsRequired > 0)
        {
            newText += StringManager.GetString("misc_echoes_required") + ": " + UIManagerScript.greenHexColor + spiritsRequired + " </color> ";
        }
        if (modifiedAbility.chargeTime != 0)
        {
            if (modifiedAbility.chargeTime == 200)
            {
                newText += StringManager.GetString("misc_free_turn");
            }
            else
            {
                StringManager.SetTag(0, modifiedAbility.chargeTime.ToString());
                newText += StringManager.GetString("ui_ct_bonus") + " ";
            }
        }

        if (modifiedAbility.reqWeaponType != WeaponTypes.ANY)
        {
            newText += "\n<color=yellow>" + StringManager.GetString("ui_req_weapon") + ": " + Weapon.weaponTypesVerbose[(int)modifiedAbility.reqWeaponType] + "</color>\n";
        }

        newText += "\n" + modifiedAbility.description;

        return newText;
    }

    public void CheckForMissingEmblemAbilities()
    {
        Emblem eToUse = null;

        if (myEquipment.GetEmblem() != null)
        {
            if (myEquipment.GetEmblem().jobForEmblem == myJob.jobEnum)
            {
                eToUse = myEquipment.GetEmblem();
                eToUse.VerifyEmblemHasStatMod();
            }
        }

        foreach (Item itm in myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.EMBLEM) continue;

            Emblem checkE = itm as Emblem;

            if (checkE.jobForEmblem == myJob.jobEnum)
            {
                if (eToUse == null)
                {
                    eToUse = checkE;
                }
            }

            checkE.VerifyEmblemHasStatMod();
        }

        if (eToUse != null)
        {
            // Let's make sure we have the proper upgrade for this item.
            if (eToUse.mods.Count <= eToUse.emblemLevel + 1)
            {
                JobTrialScript.PromptPlayerForEmblemUpgrade(eToUse);
            }
        }

    }

    public bool HasMasteredJob(CharacterJobs jobEnum)
    {
        throw new NotImplementedException();
    }

    public bool FoundLegItem(string actorRefName)
    {
        if (MysteryDungeonManager.AllowDuplicateLegendaries())
        {
            return false;
        }

        if (ReadActorData("legfound_" + actorRefName) == 1 ||
            ReadActorData(actorRefName) == 1)
        {
            return true;
        }
        return false;
    }

    public bool HasMasteredJob(CharacterJobData cjd)
    {
        //Look at every ability the job offers.
        //If we don't have one, then we haven't mastered the job.
        //The exception is the Master Ability, which you can only get if you have
        //... wait for it...

        //...

        //... keeep waiting
        foreach (JobAbility ja in cjd.JobAbilities)
        {
            if (ja == myJob.MasterAbility)
            {
                continue;
            }
            if (ja.postMasteryAbility) continue;

            if (!myAbilities.HasAbilityRef(ja.abilityRef) && !ja.abilityRef.Contains("statbonus"))
            {
                //if (Debug.isDebugBuild) Debug.Log(ja.abilityRef + " we don't have it.");
                return false;
            }
        }

        return true;
    }

    public bool CheckIfTileIsVisibleInArray(Vector2 pos)
    {
        if (!MapMasterScript.InBounds(pos)) return false;

        return visibleTilesArray[(int)pos.x, (int)pos.y];
    }

    public ItemDreamFloorData GetItemDreamFloorDataPack(Map checkMap)
    {
        // Get the existing floor event data, or create it if none exists
        ItemDreamFloorData floorData;
        if (!dictDreamFloorData.TryGetValue(checkMap.mapAreaID, out floorData))
        {
            floorData = new ItemDreamFloorData();
            floorData.dreamMap = checkMap;
            floorData.iDreamMapID = checkMap.mapAreaID;
            dictDreamFloorData.Add(checkMap.mapAreaID, floorData);
        }

        return floorData;
    }
    
    
    // Goes through our item dream meta data (floor events like big mode, costume party, etc) and links IDs to actual maps
    public void LinkItemDreamDataMaps()
    {
        List<int> toRemove = new List<int>(); // remove stuff we can't link (shouldn't ever happen)
        foreach (ItemDreamFloorData floorData in dictDreamFloorData.Values)
        {
            Map retrieveMap;
            if (MapMasterScript.dictAllMaps.TryGetValue(floorData.iDreamMapID, out retrieveMap))
            {
                floorData.dreamMap = retrieveMap;
            }
            else
            {
                toRemove.Add(floorData.iDreamMapID);
            }
        }

        foreach (int id in toRemove)
        {
            dictDreamFloorData.Remove(id);
        }
    }

    // Stuff that happens after a turn has executed completely.
    public void TurnEndCleanup()
    {
        if (limitBreakDirty)
        {
            PlayerHUDStatsComponent.SetLimitBreakAmount(limitBreakAmount);
            limitBreakDirty = false;
        }

        if (ReadActorData("shieldinfo_dirty") == 1)
        {
            UIManagerScript.RefreshEnergyShield();
        }

        if (ReadActorData("equipment_dirty") == 1)
        {
            CheckForDoublebiteCleanup();
            SetActorData("equipment_dirty", 1);
        }

        if (clearTrackDamageFlagAtEndOfTurn)
        {
            SetFlag(ActorFlags.TRACKDAMAGE, false);
			clearTrackDamageFlagAtEndOfTurn = false;
        }

        GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
        

        myStats.RemoveQueuedStatuses();
        RemoveActorData("cheatdeath_on_turn");
        myQuests.RemoveAll(a => a.complete);
        UpdateSpriteOrder(turnEnd: true);
        influenceTurnData.Reset();
        VerifySpritePositionIsAccurate(); // Hopefully this will fix issues where the player is warped via Vanishing armor, but their position desyncs from sprite
        myAbilities.TryCleanAbilitiesThatReserveEnergy();
        myAbilities.TryCleanAbilitiesThatReserveStamina();

        // If we missed a 'harvest all food' dialog for some reason, we'll check on the next step we take.
        if (!GameMasterScript.playerDied && !UIManagerScript.AnyInteractableWindowOpen() 
            && !GameMasterScript.IsAnimationPlaying() && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && 
            GameMasterScript.gmsSingleton.ReadTempGameData("harvest_waiting") == 1)
        {
            UIManagerScript.StartConversationByRef("grovetreeharvest", DialogType.STANDARD, null);
            GameMasterScript.gmsSingleton.SetTempGameData("harvest_waiting", 0);
        }
    }

    // Takes duplicate legendaries and turns them into something else
    public void SwapInvalidItemsInQuests()
    {
        foreach (QuestScript qs in myQuests)
        {
            if (qs.itemReward != null && qs.itemReward.legendary && FoundLegItem(qs.itemReward.actorRefName) && !qs.complete)
            {
                Item oldReward = qs.itemReward;
                qs.itemReward = LootGeneratorScript.GenerateLoot(qs.itemReward.challengeValue, 0.2f);
                qs.itemRewardID = qs.itemReward.actorUniqueID;
                Debug.Log("Swapped quest reward " + oldReward.actorRefName + " for valid one " + qs.itemReward.actorRefName);
            }
        }
    }

    public bool CheckForQuestsWithRequirement(QuestRequirementTypes qrType)
    {
        foreach (QuestScript qs in myQuests)
        {
            foreach (QuestRequirement qr in qs.qRequirements)
            {
                if (qr.qrType == qrType)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void TryRefreshStatuses()
    {
        if (GameMasterScript.gmsSingleton.turnExecuting)
        {
            refreshStatusesAtEndOfTurn = true;
        }
        else
        {
            UIManagerScript.RefreshStatuses();
        }
    }



    public void InitializeJPAndStartAbilities(bool doFeats, CharacterJobData cjd = null)
    {        
        if (RandomJobMode.preparingEntryForRandomJobMode)
        {
            RandomJobMode.EnterRandomJobMode();
        }

        if (cjd == null)
        {
            myJob = CharacterJobData.GetJobData(GameStartData.playerJob.ToUpperInvariant()); // Clunky way of doing this.
        }
        else
        {
            myJob = cjd;
        }

        for (int i = 0; i < jobJP.Length; i++)
        {
            jobJP[i] = 0;
            jobJPspent[i] = 0;
        }
        
        startingJob = myJob.jobEnum;
        GameMasterScript.gmsSingleton.UpdateHeroObject();
        if (RandomJobMode.IsCurrentGameInRandomJobMode()) RandomJobMode.CheckForInitLearnInnatesOnCharacterStart();
        foreach (JobAbility ja in myJob.JobAbilities)
        {
            //Debug.Log("Checking " + ja.innate + " " + ja.jpCost + " " + ja.abilityRef);
            if (ja.jpCost == 0)
            {
                // This ability is learned for free.
                if (ja.innate && ja.innateReq > 0)
                {
                    continue;
                }
                //Debug.Log("Learnding it");
                LearnAbility(ja, false, true);
            }
        }
        if (GameStartData.playerFeats != null && GameStartData.playerFeats.Count > 0 && doFeats)
        {
            foreach (string skill in GameStartData.playerFeats)
            {
                //myFeats.Add(skill);
                AbilityScript template = AbilityScript.GetAbilityByName(skill);
                if (template != null && !myAbilities.HasAbilityRef(template.abilityName))
                {
                    AbilityScript abil = new AbilityScript();
                    AbilityScript.CopyFromTemplate(abil, template);
                    LearnAbility(abil, false, true);
                    if (!heroFeats.Contains(abil.refName))
                    {
                        heroFeats.Add(abil.refName);
                    }
                }
                else
                {
                    Debug.Log("Could not find feat " + skill);
                }
            }

            if (myStats.CheckHasStatusName("status_toughness"))
            {
                myStats.ChangeStat(StatTypes.HEALTH, 30f, StatDataTypes.ALL, true);
            }
        }

        if (myJob.jobEnum == CharacterJobs.GAMBLER)
        {
            money += 100;
        }       
    }

    public void SetDefaultWeapon(bool newGame)
    {
        Weapon startingWeapon = new Weapon();
        Weapon wTemplate = GameMasterScript.GetItemFromRef("weapon_fists") as Weapon;
        startingWeapon.CopyFromItem(wTemplate);
        startingWeapon.collection = GameMasterScript.heroPCActor.myInventory;
        if (newGame)
        {
            GameMasterScript.heroPCActor.TryEquip(startingWeapon, SND.SILENT, false);
        }

        myEquipment.defaultWeapon = startingWeapon;
    }

    public void PruneMapsExploredThatDontExist()
    {
        mapsExploredByMapID.RemoveAll(a => !MapMasterScript.dictAllMaps.ContainsKey(a));
    }
    /// <summary>
    /// Checks the monster status and other conditions to see if we should draw a card, then draws one if possible!
    /// </summary>
    /// <param name="mon"></param>
    public void TryDrawWildCard(Monster mon)
    {
        if (!CanCollectWildCards()) return;
        if (mon == null) return;

        //trivial monsters should not reward us as much as beefy on level ones.
        float cardChance = mon.GetXPModToPlayer();

        //Job trials don't give XP or coin, but we still want a shot at a card.
        if (MapMasterScript.activeMap.IsJobTrialFloor())
        {
            cardChance = 0.75f;
        }

        if (UnityEngine.Random.Range(0.01f, 1f) <= cardChance)
        {
            DrawWildCard();
        }
    }
    /// <summary>
    /// This ensures all equipped items have the correct inventory collection - ours!
    /// </summary>
    public void RefreshEquipmentCollectionOwnership()
    {
        for (int i = 0; i < myEquipment.equipment.Length; i++)
        {
            if (myEquipment.equipment[i] == null) continue;
            myEquipment.equipment[i].collection = myInventory;
        }
    }

    /// <summary>
    /// Ensures our values for gold find etc. match the bonuses granted by our gear.
    /// </summary>
    public void ValidateAdventureStats()
    {
        for (int i = 0; i < advStats.Length; i++)
        {
            advStats[i] = 0f;
        }
        for (int i = 0; i < myEquipment.equipment.Length; i++)
        {
            if (myEquipment.equipment[i] == null || myEquipment.IsDefaultWeapon(myEquipment.equipment[i], onlyActualFists: true)) continue;
            Equipment item = myEquipment.equipment[i];
            foreach (MagicMod mod in item.mods)
            {
                for (int x = 0; x < mod.adventureStats.Length; x++)
                {
                    if (mod.adventureStats[x] != 0f)
                    {
                        GameMasterScript.heroPCActor.advStats[x] += mod.adventureStats[x];
                    }                    
                }
            }
        }
        
    }

    public void LearnInnateAbilitiesForCurrentJob()
    {
        if (RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            RandomJobMode.CheckForInitLearnInnatesOnCharacterStart();
            return;
        }

        foreach (JobAbility ja in myJob.JobAbilities)
        {
            if (ja.jpCost == 0 && !myAbilities.HasMasteredAbilityByRef(ja.abilityRef) && !myAbilities.HasAbilityRef(ja.abilityRef))
            {
                if (ja.innate && ja.innateReq > jobJPspent[(int)myJob.jobEnum]) // Locked until we hit that JP amount
                {
                    continue;
                }
                LearnAbility(ja, false, true);
            }
        }
    }


    /// <summary>
    /// Checks and fixes wonkiness in our stat block and ability block, as well as any non-rumor quest flags
    /// </summary>
    public void VerifyStatsAbilitiesStatuses()
    {
        VerifyAbilities();
        myAbilities.VerifyEquippedPassivesAreActivated();
        VerifyStatusesAddedByGearAreValid();

        if (myAbilities.HasAbilityRef("skill_lightarmormastery1") || myAbilities.HasAbilityRef("skill_mediumarmormastery1") || myAbilities.HasAbilityRef("skill_heavyarmormastery1"))
        {
            // if we already know an armor mastery, make sure our armor mastery quest flag is 3
            if (GameStartData.NewGamePlus < 1)
            {
                ProgressTracker.SetProgress(TDProgress.ARMOR_MASTER_QUEST, ProgressLocations.HERO, 3);
            }
        }
    }

    /// <summary>
    /// Returns TRUE if any summon was removed.
    /// </summary>
    /// <returns></returns>
    public bool RemoveAllSummons()
    {
        bool anyDesummons = false;
        foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(act);
            GameMasterScript.dictAllActors.Remove(act.actorUniqueID);
            anyDesummons = true;
        }
        GameMasterScript.heroPCActor.summonedActors.Clear();
        return anyDesummons;
    }

    /// <summary>
    /// Displays tutorial popup the first time we can learn something, and/or the red exclamation by our portrait.
    /// </summary>
    public void CheckForJPTutorialsOrNotifications(bool forceJPNotify = false)
    {
        if (HasEnoughJPForSkillAndCanPurchase())
        {
            if (PlayerOptions.tutorialTips && !GameStartData.gameInSharaMode && !UIManagerScript.dialogBoxOpen)
            {
                if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_jpkeyboard") && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_jpcontroller"))
                {
                    Conversation newConvo = null;
                    if (PlayerOptions.showControllerPrompts || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
                    {
                        newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_jpcontroller");
                    }
                    else
                    {
                        newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_jpkeyboard");
                    }

                    UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                }
            }

            if (UIManagerScript.jpGainedSinceJobScreenToggled >= 100 &&
                HasEnoughJPForSkillAndCanPurchase())
            {
                int lastJPTurnLog = GameMasterScript.gmsSingleton.ReadTempGameData("lastjplog");
                if (lastJPTurnLog - GameMasterScript.turnNumber > 5 || forceJPNotify)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("lastjplog", (int)UIManagerScript.jpGainedSinceJobScreenToggled);
                    StringManager.SetTag(0, BakedInputBindingDisplay.GetControlBinding(TDControlBindings.VIEW_SKILLS));
                    GameLogScript.LogWriteStringRef("log_enough_jp_learn");
                }

                UIManagerScript.ShowLearnSkillIndicator();
            }
        }
        else
        {
            UIManagerScript.HideLearnSkillIndicator();
        }
    }



    public bool IsOverdrawingActiveOnWeaponOrPairedQuiver(Weapon w)
    {
        if (w.HasModByRef("mm_overdraw")) return true;
        if (myStats.CheckHasStatusName("status_overdraw")) return true;
        Equipment quiver = w.GetPairedItem();
        if (quiver != null)
        {
            if (quiver.HasModByRef("mm_overdraw")) return true;
        }
        return false;
    }

    public void VerifyMiscellaneousFlagsAndData()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            SetActorData("dlc2installed", 1);
        }
        else
        {
            RemoveActorData("dlc2installed");
        }
    }

    /// <summary>
    /// Special magic mod based on our actiontimer status.
    /// </summary>
    public void CheckForSerenityBuff()
    {
        if (!myStats.CheckHasStatusName("xp2_serenity"))
        {
            return;
        }
        bool heroHasBuff = GameMasterScript.heroPCActor.ReadActorData("serenitybuff") == 1;

        int ct = GameMasterScript.heroPCActor.GetActionTimerDisplay();

        if (heroHasBuff && ct < 50)
        {
            GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("serenitybuff");
            GameMasterScript.heroPCActor.SetActorData("serenitybuff", 0);
        }
        else if (!heroHasBuff && ct >= 50)
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("serenitybuff", GameMasterScript.heroPCActor, 99, false);
            GameMasterScript.heroPCActor.SetActorData("serenitybuff", 1);
        }
    }

    public void CheckForLimitBreakOnDamageTaken(float dmgAmount)
    {
        if (!myAbilities.IsDragonSoulEquipped()) return;
        float dmgAsPercentOfLife = dmgAmount / myStats.GetMaxStat(StatTypes.HEALTH);
        
        limitBreakAmount += dmgAsPercentOfLife;
        
        limitBreakDirty = true;        
    }

    public void ResetLimitBreak()
    {
        if (!myAbilities.IsDragonSoulEquipped()) return;
        limitBreakAmount = 0;
        limitBreakDirty = true;

        SetActorData("limitbreakactive", 0);
    }

    public bool LimitBreakAvailable()
    {
        if (!myAbilities.IsDragonSoulEquipped()) return false;

        if (ReadActorData("limitbreakactive") != 1) return false;

        return true;
    }

    /// <summary>
    /// Do visual FX / log for obtaining your limit break!
    /// </summary>
    public void OnLimitBreakReached()
    {
        if (myStats.GetFirstActiveStatusOfTag(AbilityTags.DRAGONSOUL) != null)
        {
            return; // we must already have a soul status active.
        }
        string passiveSoulRefName = myAbilities.GetFirstEquippedPassiveAbilityOfTag(AbilityTags.DRAGONSOUL).refName;
        string newStatusName = "dragonbreak_icon";

        myStats.AddStatusByRef(newStatusName, this, 15, false);
        GameLogScript.LogWriteStringRef("log_limitbreak_reached");

        SetActorData("limitbreakactive", 1);
    }

    public int GetVisualLimitBreakAmount()
    {
        int limit = (int)((limitBreakAmount * 100f) / 1.5f);
        if (limit > 100)
        {
            limit = 100;
        }
        return limit;
    }

    public void ValidateInvincibleFrog()
    {
        int id = ReadActorData("knockedoutmonster");
        if (id > 0)
        {
            Actor act = GameMasterScript.gmsSingleton.TryLinkActorFromDict(id);
            if (act != null && act.GetActorType() == ActorTypes.MONSTER)
            {
                if (act.actorRefName == "mon_harmlessfungaltoad")
                {
                    Monster mn = act as Monster;
                    mn.myStats.ForciblyRemoveStatus("monsterundying");
                }
            }
        }
        
    }

    

    public void OnItemSoldOrDropped(Item thingToDrop, bool soldItem)
    {
        if (!thingToDrop.customItemFromGenerator) return;

        int floor = MapMasterScript.activeMap.floor;

        if (soldItem)
        {
            floor = -1;
        }

        if (!GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.ContainsKey(thingToDrop.actorRefName))
        {
            GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.Add(thingToDrop.actorRefName, floor);
        }
        GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold[thingToDrop.actorRefName] = floor;
    }

    public void OnItemPickedUpOrPurchased(Item thingObtained, bool purchased)
    {
        if (!thingObtained.customItemFromGenerator) return;

        GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.Remove(thingObtained.actorRefName);
    }

    public void OnMapsChanged(Map newMap)
    {
        Monster mPet = GetMonsterPet();
        if (mPet == null) return;

        if (newMap.IsTownMap() || newMap.floor == MapMasterScript.CAMPFIRE_FLOOR)
        {
            mPet.myStats.HealToFull();
        }

        if (mPet.myStats.GetXP() < mPet.myStats.GetXPToCurrentLevel())
        {
            mPet.myStats.SetXPFlat(mPet.myStats.GetXPToCurrentLevel());
        }

        if (!mPet.myStats.CheckHasActiveStatusName("petheal_overtime"))
        {
            mPet.myStats.AddStatusByRef("petheal_overtime", mPet, 999, false);
        }

    }

    public bool HasExploredMapFloorRange(int low, int high)
    {
        foreach(int floor in mapFloorsExplored)
        {
            if (floor >= low && floor <= high) return true;
        }

        return false;
    }

    static List<QuestScript> questsToRemoveDueToLowLevel;

    public void EvaluateTrivialRumors()
    {
        if (questsToRemoveDueToLowLevel == null) questsToRemoveDueToLowLevel = new List<QuestScript>();
        questsToRemoveDueToLowLevel.Clear();

        foreach(QuestScript qs in myQuests)
        {
            if (qs.complete) continue;

            //Debug.Log("Checking quest " + qs.displayName + " " + qs.qType);

            if (qs.qType == QuestType.APPEASEMONSTER || qs.qType == QuestType.KILLCHAMPION || qs.qType == QuestType.TAMEMONSTER
                || qs.qType == QuestType.KILLMONSTERELEMENTAL || qs.qType == QuestType.FINDITEM)
            {
                bool anyValidFactors = false;

                // Don't skip over legendary or set items.
                if (qs.itemReward != null)
                {
                    if (qs.itemReward.rarity == Rarity.LEGENDARY || qs.itemReward.rarity == Rarity.GEARSET)
                    {
                        //Debug.Log("Item reward is legendary or gearset, so not skipping.");
                        anyValidFactors = true;
                    }
                }

                if (!anyValidFactors && qs.targetMap != null && qs.targetMap == MapMasterScript.activeMap)
                {
                    anyValidFactors = true;
                }

                if (!anyValidFactors && qs.targetItem != null)
                {
                    if (qs.targetItem.rarity == Rarity.LEGENDARY || qs.targetItem.rarity == Rarity.GEARSET)
                    {
                        //Debug.Log("Target item is legendary or gearset, so not skipping.");
                        anyValidFactors = true;
                    }
                }

                if (!anyValidFactors && qs.qType == QuestType.KILLMONSTERELEMENTAL)
                {
                    if (!string.IsNullOrEmpty(qs.targetRef))
                    {
                        MonsterTemplateData mTemplate = GameMasterScript.masterMonsterList[qs.targetRef];

                        int baseLevel = mTemplate.baseLevel;

                        if (GameStartData.NewGamePlus >= 1) baseLevel += 10;
                        if (GameStartData.NewGamePlus >= 2) baseLevel += 2;

                        if (baseLevel >= myStats.GetLevel())
                        {
                            anyValidFactors = true;
                        }
                        else
                        {

                            float xpModToPlayer = BalanceData.playerMonsterRewardTable[myStats.GetLevel(), baseLevel];

                            if (xpModToPlayer > 0.1f)
                            {
                                //Debug.Log("Item XP mod is " + xpModToPlayer + " so not skipping");
                                anyValidFactors = true;
                            }
                            else
                            {
                                if (Debug.isDebugBuild) Debug.Log("Quest " + qs.displayName + " mon xp mod is " + xpModToPlayer + " so abandon");
                            }

                        }
                    }
                    
                }

                if (!anyValidFactors && qs.targetMonster != null)
                {
                    if (qs.targetMonster.myStats.GetLevel() >= myStats.GetLevel())
                    {
                        anyValidFactors = true;
                    }
                    else
                    {
                        float xpMod = qs.targetMonster.GetXPModToPlayer();

                        if (qs.targetMonster.isChampion)
                        {
                            if (xpMod > 0.7f)
                            {
                                //Debug.Log("Target champion monster xp mod is " + xpMod + " so not skipping");
                                anyValidFactors = true;
                            }
                            else
                            {
                                if (Debug.isDebugBuild) Debug.Log("Quest " + qs.displayName + " champ mon xp mod is " + xpMod + " so abandon");
                            }
                        }
                        else 
                        {
                            if (!qs.targetMonster.isChampion && xpMod > 0.15f)
                            {
                                //Debug.Log("Target monster xp mod is " + xpMod + " so not skipping");
                                anyValidFactors = true;
                            }
                            else
                            {
                                if (Debug.isDebugBuild) Debug.Log("Quest " + qs.displayName + " nonchamp xp mod is " + xpMod + " so abandon");
                            }
                            
                        }
                    }
                }

                if (!anyValidFactors && qs.qType == QuestType.FINDITEM)
                {
                    if (qs.targetItem != null)
                    {
                        // If we're here, it's an item that is NOT legendary and NOT gear set, and the area might be too easy at this point
                        int itemCVToLevel = BalanceData.GetMonsterLevelByCV(qs.targetItem.challengeValue, true);

                        float xpModToPlayer = BalanceData.playerMonsterRewardTable[myStats.GetLevel(), itemCVToLevel];

                        if (xpModToPlayer > 0.15f || itemCVToLevel >= myStats.GetLevel())
                        {
                            //Debug.Log("Item XP mod is " + xpModToPlayer + " so not skipping");
                            anyValidFactors = true;
                        }
                        else
                        {
                            if (Debug.isDebugBuild) Debug.Log("Quest " + qs.displayName + " item xp mod is " + xpModToPlayer + " and cv to level is " + itemCVToLevel + " so abandon");
                        }
                    }

                }

                int age = MetaProgressScript.totalDaysPassed - qs.dayReceived;

                if (age >= 2 && !anyValidFactors)
                {
                    questsToRemoveDueToLowLevel.Add(qs);
                }
            }
        }

        foreach(QuestScript qs in questsToRemoveDueToLowLevel)
        {
            myQuests.Remove(qs);

            QuestScript.OnQuestFailedOrAbandoned(qs);

            StringManager.SetTag(0, qs.displayName);

            GameLogScript.LogWriteStringRef("log_skipquest");

            if (ProgressTracker.CheckProgress(TDProgress.AUTO_ABANDON_TUTORIAL, ProgressLocations.META) != 1)
            {
                ProgressTracker.SetProgress(TDProgress.AUTO_ABANDON_TUTORIAL, ProgressLocations.META, 1);
                if (!GameMasterScript.IsAnimationPlayingFromCutscene())
                {
                    UIManagerScript.StartConversationByRef("skipquest_auto_tutorial", DialogType.STANDARD, null);
                }                
            }
        }

        if (questsToRemoveDueToLowLevel.Count > 0)
        {
            RumorTextOverlay.OnRumorCompletedOrFailed();
        }
    }

    public void SetRegenFlaskUses(int value)
    {
        regenFlaskUses = value;

        GuideMode.CalculateThenCheckFlaskPulse();
    }

    public void ChangeRegenFlaskUses(int value)
    {
        regenFlaskUses += value;

        GuideMode.CalculateThenCheckFlaskPulse();
    }

    public bool IsFriendlyFirePossible()
    {
        if (GameStartData.CheckGameModifier(GameModifiers.FRIENDLY_FIRE)) return true;
        return myStats.CheckHasStatusName("status_confused50");
    }

    public bool HasSpecificMonsterInFoodLovingQuests(Monster m)
    {
        foreach(QuestScript qs in myQuests)
        {
            if (qs.complete) continue;
            if (qs.qType != QuestType.APPEASEMONSTER) continue;
            if (qs.targetMonster == m || qs.targetMonsterID == m.actorUniqueID) return true;            
        }

        return false;
    }

    public bool HasDoubleBiteSwappedThisTurn() 
    {
        //Debug.Log(ReadActorData("dbiteturn") + " is last turn swapped, current turn is " + GameMasterScript.turnNumber);
        return ReadActorData("dbiteturn") == GameMasterScript.turnNumber;
    }

    public void SetDoubleBiteSwappedThisTurn()
    {
        SetActorData("dbiteturn", GameMasterScript.turnNumber);
    }

    public bool HasKnockedOutMonster()
    {
        return ReadActorData("knockedoutmonster") > 0;
    }

    public void CheckForDoublebiteCleanup()
    {
        bool hasAnyDoublebite = false;
        
        for (int i = 0; i < myEquipment.equipment.Length; i++)
        {
            var eq = myEquipment.equipment[i];
            if (eq == null) continue;
            if (!eq.IsEquipment()) continue;
            Equipment eqp = eq as Equipment;
            if (eqp.HasModByRef("mm_doublebite"))
            {
                hasAnyDoublebite = true;
                break;
            }
        }

        if (hasAnyDoublebite) 
        {
            bool hasShadow = myStats.CheckHasStatusName("doublebite_shadow");
            bool hasPhysical = myStats.CheckHasStatusName("doublebite_physical");

            if (!hasShadow && !hasPhysical)
            {
                myStats.AddStatusByRef("doublebite_shadow", this, 99, false);
            }
        }
    }

    public void ValidateNoDuplicatePetsOnLoad()
    {
        List<int> sharedIDs = new List<int>();
        List<Monster> monstersToRemove = new List<Monster>();
        foreach(Actor act in summonedActors)
        {
            if (act == null) continue;
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster mn = act as Monster;

            if (mn.tamedMonsterStuff == null) continue;

            sharedIDs.Add(mn.tamedMonsterStuff.sharedBankID);

            //Debug.Log("Compare " + GetMonsterPetID() + " vs " + mn.actorUniqueID + " " + act.displayName);

            if (GetMonsterPetID() == mn.actorUniqueID) continue;

            if (sharedIDs.Contains(mn.tamedMonsterStuff.sharedBankID))
            {
                monstersToRemove.Add(mn);
                continue;
            }

            
        }

        foreach(Monster mn in monstersToRemove)
        {
            Debug.Log("Removing " + mn.actorRefName);
            summonedActors.Remove(mn);
            summonedActorIDs.Remove(mn.actorMapID);

            MapMasterScript.activeMap.RemoveActorFromMap(mn);
            GameMasterScript.dictAllActors.Remove(mn.actorUniqueID);

            if (mn.GetObject() != null) mn.myMovable.FadeOutThenDie();
        }
    }
}
