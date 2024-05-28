using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class JobTrialScript {

    public int trialTierLevel;
    public bool completed;
    public int numConsumableUses;
    public int maxConsumableUses;
    public int characterJobForTrial;
    public int flaskUsesBeforeTrial;
    public int maxFlaskUsesDuringTrial;

    public const int MAX_POSSIBLE_TIERS = 3;
    public static readonly int[] TRIAL_COSTS = { 500, 1000, 1500 };

    // Static manager-style functions for Job Trials

    public static void ExitJobTrialCleanup()
    {
        GameMasterScript.gmsSingleton.SetTempGameData("confirm_jobtrial_useportal", 0);
        GameMasterScript.heroPCActor.SetRegenFlaskUses(GameMasterScript.heroPCActor.jobTrial.flaskUsesBeforeTrial);
        GameMasterScript.heroPCActor.jobTrial = null;
        GameMasterScript.heroPCActor.myInventory.RemoveItemByRef("item_trialrelic");
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.singletonUIMS.RefreshAbilityCooldowns();
    }

    public static void BeatJobTrial()
    {
        int jobTrialTier = GameMasterScript.heroPCActor.jobTrial.trialTierLevel;

        ExitJobTrialCleanup();

        int emblemID = GameMasterScript.heroPCActor.ReadActorData("currentemblem_id");
        Emblem myEmblem = null;

        if (emblemID > 0)
        {
            myEmblem = GameMasterScript.gmsSingleton.TryLinkActorFromDict(emblemID) as Emblem;
            if (myEmblem == null)
            {
                Debug.Log("Very Bad Error: Player should have emblem id " + emblemID + " but it doesn't exist in the dict anywhere.");
            }
            else
            {
                GameMasterScript.heroPCActor.myEquipment.UnequipByReference(myEmblem);
                myEmblem.IncreaseEmblemLevel();                
            }            
        }
        else
        {
            // Don't have our first emblem, so create it and give it to the player.
            myEmblem = LootGeneratorScript.CreateItemFromTemplateRef("emblem_jobtrial1", 1.0f, 0f, false) as Emblem;
            myEmblem.jobForEmblem = GameMasterScript.heroPCActor.myJob.jobEnum;
            myEmblem.emblemLevel = 0;
            myEmblem.RebuildDisplayName();
            myEmblem.challengeValue = 1.2f;
            myEmblem.rarity = Rarity.MAGICAL;
            emblemID = myEmblem.actorUniqueID;
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(myEmblem, false);
            EquipmentBlock.MakeMagicalFromMod(myEmblem, GameMasterScript.masterMagicModList["mm_emblemwellrounded1"], true, false, false);
        }
        GameMasterScript.heroPCActor.SetActorData("currentemblem_id", myEmblem.actorUniqueID);

        TravelManager.BackToTown(false);

        PromptPlayerForEmblemUpgrade(myEmblem);

        //UIManagerScript.StartConversation(c, DialogType.KEYSTORY, null);
    }

    public static void PromptPlayerForEmblemUpgrade(Emblem myEmblem)
    {
        if (SharaModeStuff.IsSharaModeActive()) return;

        StringManager.SetTag(0, GameMasterScript.heroPCActor.myJob.DisplayName);
        StringManager.SetTag(1, myEmblem.displayName);
        Conversation c = GameMasterScript.FindConversation("jobemblem_nexttier");
        TextBranch mainTB = c.FindBranch("emblem_addtier_power");
        mainTB.responses.Clear();

        List<string> modPossibilityRefs = PopulateListWithEmblemModsBasedOnJob(myEmblem);

        List<MagicMod> modPossibilities = new List<MagicMod>();        

        foreach (string str in modPossibilityRefs)
        {
            modPossibilities.Add(GameMasterScript.masterMagicModList[str]);
        }

        foreach (MagicMod mm in modPossibilities)
        {
            ButtonCombo bc = new ButtonCombo();
            bc.actionRef = mm.refName;
            bc.dialogEventScriptValue = mm.refName;
            bc.dialogEventScript = "AddModToEmblem";
            bc.buttonText = mm.description;
            mainTB.responses.Add(bc);
        }

        GameMasterScript.SetAnimationPlaying(true);

        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(c, DialogType.KEYSTORY, null, 0.65f, "", StringManager.GetCopyOfCurrentMergeTags()
            ));
    }

    public static void CheckForTrialTier3Clear()
    {
        //Debug.Log(MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR).unfriendlyMonsterCount);
        //Debug.Log(GameMasterScript.heroPCActor.myInventory.HasItemByRef("item_trialrelic"));
        Map trialFloor = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR);

        if (trialFloor.unfriendlyMonsterCount < 5 && trialFloor.unfriendlyMonsterCount > 0)
        {
            trialFloor.RecountMonsters();
        }

        if (trialFloor.unfriendlyMonsterCount <= 0 && GameMasterScript.heroPCActor.myInventory.HasItemByRef("item_trialrelic"))
        {
            BeatJobTrial();
        }
    }

    public static void FailedJobTrial()
    {
        if (GameMasterScript.heroPCActor.jobTrial == null)
        {
            Debug.Log("Trying to fail job trial, but player doesn't have one?");
            return;
        }
        StringManager.SetTag(0, GameMasterScript.heroPCActor.myJob.DisplayName);
        GameLogScript.LogWriteStringRef("log_failed_trial");
        ExitJobTrialCleanup();
    }

    public static void SetupJobTrial(int trialLevel)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("confirm_jobtrial_useportal", 0);
        GameMasterScript.heroPCActor.SetActorData("jobtrial_callout", 1);
        GameMasterScript.heroPCActor.SetActorData("weaponmaster_callout", 1);

        // Desummon
        bool anyDesummons = GameMasterScript.heroPCActor.RemoveAllSummons();

        if (anyDesummons)
        {
            GameLogScript.LogWriteStringRef("log_trial_petdesummon");
        }

        GameMasterScript.heroPCActor.RemovePositiveTemporaryAndToggledStatuses();

        GameMasterScript.heroPCActor.myStats.HealToFull();

        List<Map> trialMaps = new List<Map>();

        Map trialFloor1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR);

        if (trialFloor1 != null)
        {
            MapMasterScript.theDungeon.maps.Remove(trialFloor1);
            MapMasterScript.specialMaps.Remove(trialFloor1);
            MapMasterScript.dictAllMaps.Remove(trialFloor1.mapAreaID);
            MapMasterScript.OnMapRemoved(trialFloor1);
        }
            trialFloor1 = MapMasterScript.singletonMMS.CreateNewMap(false, MapMasterScript.JOB_TRIAL_FLOOR, 1, 1.0f, GameMasterScript.masterDungeonLevelList[MapMasterScript.JOB_TRIAL_FLOOR], null);
            MapMasterScript.theDungeon.maps.Add(trialFloor1);
            MapMasterScript.specialMaps.Add(trialFloor1);
            trialFloor1.levelDataLink = MapMasterScript.JOB_TRIAL_FLOOR;
        
        Map trialFloor2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR2);

        if (trialFloor2 != null)
        {
            MapMasterScript.theDungeon.maps.Remove(trialFloor2);
            MapMasterScript.specialMaps.Remove(trialFloor2);
            MapMasterScript.dictAllMaps.Remove(trialFloor2.mapAreaID);
            MapMasterScript.OnMapRemoved(trialFloor2);
        }
        trialFloor2 = MapMasterScript.singletonMMS.CreateNewMap(false, MapMasterScript.JOB_TRIAL_FLOOR2, 1, 1.0f, GameMasterScript.masterDungeonLevelList[MapMasterScript.JOB_TRIAL_FLOOR2], null);
            MapMasterScript.theDungeon.maps.Add(trialFloor2);
            MapMasterScript.specialMaps.Add(trialFloor2);
            trialFloor2.levelDataLink = MapMasterScript.JOB_TRIAL_FLOOR2;
        
        trialMaps.Add(trialFloor1);
        trialMaps.Add(trialFloor2);


        foreach (Map trialMap in trialMaps)
        {
            for (int x = 0; x < trialMap.columns; x++)
            {
                for (int y = 0; y < trialMap.rows; y++)
                {
                    trialMap.exploredTiles[x, y] = false;
                }
            }

            trialMap.challengeRating = BalanceData.LEVEL_TO_CV[GameMasterScript.heroPCActor.myStats.GetLevel()];
            trialMap.dungeonLevelData.challengeValue = trialMap.challengeRating;
            trialMap.dungeonLevelData.expectedPlayerLevel = GameMasterScript.heroPCActor.myStats.GetLevel() + trialLevel;

            List<Actor> actorsToClearFromMap = new List<Actor>();
            foreach (Actor act in trialMap.actorsInMap)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    actorsToClearFromMap.Add(act);
                }
                else if (act.GetActorType() == ActorTypes.ITEM)
                {
                    actorsToClearFromMap.Add(act);
                }
                else if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                {
                    Destructible dt = act as Destructible;
                    if (dt.mapObjType != SpecialMapObject.WATER && dt.mapObjType != SpecialMapObject.LAVA)
                    {
                        actorsToClearFromMap.Add(act);
                    }
                }
                else if (act.GetActorType() == ActorTypes.STAIRS)
                {
                    actorsToClearFromMap.Add(act);
                }
            }

            foreach (Actor act in actorsToClearFromMap)
            {
                trialMap.RemoveActorFromMap(act);
            }
        }


        JobTrialScript theTrial = new JobTrialScript();
        theTrial.trialTierLevel = trialLevel;
        theTrial.flaskUsesBeforeTrial = GameMasterScript.heroPCActor.regenFlaskUses;        
        theTrial.characterJobForTrial = (int)GameMasterScript.heroPCActor.myJob.jobEnum;

        int pLevel = GameMasterScript.heroPCActor.myStats.GetLevel();

        DungeonGenerationScripts.SeedJobTrial(trialMaps[0], pLevel, trialLevel);

        string requirementsRef = "jobtrial_requirements_" + trialLevel;

        switch (trialLevel)
        {
            case 0:                
                theTrial.maxFlaskUsesDuringTrial = 5;
                theTrial.maxConsumableUses = 3;
                break;
            case 1:
                theTrial.maxFlaskUsesDuringTrial = 6;
                theTrial.maxConsumableUses = 4;
                break;
            case 2:
                theTrial.maxFlaskUsesDuringTrial = 7;
                theTrial.maxConsumableUses = 6;
                ReseedTrialFloor2(trialMaps[1], pLevel);
                break;
        }

        GameMasterScript.heroPCActor.SetRegenFlaskUses(theTrial.maxFlaskUsesDuringTrial);
        GameMasterScript.heroPCActor.jobTrial = theTrial;

        if (trialLevel == 2)
        {
            CreateRelicAndStairsForTier3Trial(trialMaps);
        }

        TravelManager.TravelMaps(trialMaps[0], null, false);

        StringManager.SetTag(0, theTrial.maxFlaskUsesDuringTrial.ToString()); // Flask uses in ^tag1^
        StringManager.SetTag(1, theTrial.maxConsumableUses.ToString()); // Consumable in ^tag2^
        StringManager.SetTag(2, (trialLevel + 1).ToString());
        StringManager.SetTag(3, GameMasterScript.heroPCActor.myJob.DisplayName);
        StringManager.SetTag(4, StringManager.GetString(requirementsRef));

        GameMasterScript.SetAnimationPlaying(true);

        Conversation descConvo = GameMasterScript.FindConversation("jobtrial_desc");

        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(descConvo, DialogType.STANDARD, null, 0.65f, "", StringManager.GetCopyOfCurrentMergeTags()
            ));

        //UIManagerScript.StartConversation(descConvo, DialogType.KEYSTORY, null);

        GameMasterScript.heroPCActor.myStats.HealToFull();

        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshStatuses();
        PetPartyUIScript.RefreshContentsOfPlayerParty();
    }

    public static void CreateRelicAndStairsForTier3Trial(List<Map> trialMaps, bool spawnObjects = true)
    {
        foreach(Map m in trialMaps)
        {
            m.RemoveAllActorsOfType(ActorTypes.STAIRS);
        }


        Stairs st = new Stairs(); // stairs from level 1 to 2
        st.stairsUp = false;
        st.pointsToFloor = MapMasterScript.JOB_TRIAL_FLOOR2;
        st.NewLocation = trialMaps[1];
        st.newLocationID = trialMaps[1].mapAreaID;
        st.prefab = "StoneStairsUp";
        MapTileData randomEmpty = trialMaps[0].GetRandomEmptyTileForMapGen();
        int attempts = 0;
        while (MapMasterScript.GetGridDistance(randomEmpty.pos, trialMaps[0].heroStartTile) < 8)
        {
            attempts++;
            if (attempts > 200) break;
            randomEmpty = trialMaps[0].GetRandomEmptyTileForMapGen();
        }
        trialMaps[0].PlaceActor(st, randomEmpty);

        if (MapMasterScript.activeMap.floor == MapMasterScript.JOB_TRIAL_FLOOR)
        {
            MapMasterScript.singletonMMS.SpawnStairs(st);
        }

        Stairs st2 = new Stairs();
        st2.stairsUp = true;
        st2.pointsToFloor = MapMasterScript.JOB_TRIAL_FLOOR;
        st2.NewLocation = trialMaps[0];
        st2.newLocationID = trialMaps[0].mapAreaID;
        st2.prefab = "StoneStairsDown";

        Vector2 center = new Vector2(trialMaps[1].columns / 2f, trialMaps[1].rows / 2f);
        randomEmpty = trialMaps[1].GetRandomEmptyTile(center, 1, true, anyNonCollidable: true);
        trialMaps[1].PlaceActor(st2, randomEmpty);

        Item trialRelic = CreateRelicInMap(trialMaps[1]);

        if (MapMasterScript.activeMap.floor == MapMasterScript.JOB_TRIAL_FLOOR2)
        {
            MapMasterScript.singletonMMS.SpawnStairs(st2);
            MapMasterScript.singletonMMS.SpawnItem(trialRelic);
        }
    }

    static Item CreateRelicInMap(Map m)
    {
        MapTileData randomEmpty = null;
        Vector2 center = new Vector2(m.columns / 2f, m.rows / 2f);
        Item trialRelic = LootGeneratorScript.CreateItemFromTemplateRef("item_trialrelic", 1.0f, 0f, false);
        randomEmpty = m.GetRandomEmptyTileForMapGen();
        int attempts = 0;
        while (MapMasterScript.GetGridDistance(center, randomEmpty.pos) < 10)
        {
            attempts++;
            randomEmpty = m.GetRandomEmptyTileForMapGen();
            if (attempts > 1000) break;
        }
        m.PlaceActor(trialRelic, randomEmpty);
        Debug.Log("Placed trial relic at " + randomEmpty);
        return trialRelic;
    }

    public static void ReseedTrialFloor2(Map arenaFloor2Map, int playerLevel)
    {
        DungeonGenerationScripts.SeedJobTrial(arenaFloor2Map, playerLevel, 2);
    }

    public static void SpawnTrialRelicIfNeeded()
    {
        Map floor2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR2);
        if (floor2 == null) return;
        if (!GameMasterScript.heroPCActor.myInventory.HasItemByRef("item_trialrelic"))
        {
            if (floor2.FindActor("item_trialrelic") == null)
            {
                Item relic = CreateRelicInMap(floor2);
                GameLogScript.LogWriteStringRef("trial_tier3_relicappear");
                MapMasterScript.singletonMMS.SpawnItem(relic);
            }
        }
    }

    public static void VerifyJobTrialIsSetup()
    {
        if (GameMasterScript.heroPCActor.jobTrial == null)
        {
            return;
        }

        int trialLevel = GameMasterScript.heroPCActor.jobTrial.trialTierLevel;

        List<Map> trialMaps = new List<Map>();
        trialMaps.Add(MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR));
        Map floor2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JOB_TRIAL_FLOOR2);
        if (floor2 != null)
        {
            trialMaps.Add(floor2);
        }

        if (trialLevel == 2)
        {
            if (MapMasterScript.activeMap.floor == MapMasterScript.JOB_TRIAL_FLOOR)
            {
                // Are there stairs here?
                if (MapMasterScript.activeMap.mapStairs.Count == 0)
                {
                    CreateRelicAndStairsForTier3Trial(trialMaps, spawnObjects:true);
                    ReseedTrialFloor2(floor2, GameMasterScript.heroPCActor.myStats.GetLevel());
                    GameLogScript.LogWriteStringRef("trial_tier3_stairsvisible");
                }
            }
            else if (MapMasterScript.activeMap.floor == MapMasterScript.JOB_TRIAL_FLOOR2)
            {
                SpawnTrialRelicIfNeeded();
            }
        }


    }

    public static bool IsJobTrialActive()
    {
        if (GameMasterScript.heroPCActor.jobTrial == null || GameMasterScript.heroPCActor.jobTrial.completed)
        {
            return false;
        }

        return true;
    }

    public static bool CanPlayerUseAbilityDuringTrial(AbilityScript abil)
    {
        /* if (abil.refName.Contains("mastery"))
        {
            return false;
        } */

        if (abil.jobLearnedFrom != GameMasterScript.heroPCActor.myJob.jobEnum && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return false;
        }

        return true;
    }

    public static bool HasPlayerUnequippedNonJobAbilities()
    {
        if (RandomJobMode.IsCurrentGameInRandomJobMode()) return true;

        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (!abil.passiveAbility) continue;
            if (!abil.passiveEquipped) continue;
                        
            if (!CanPlayerUseAbilityDuringTrial(abil))
            {
                bool isFeat = false;
                foreach (CreationFeat cf in GameMasterScript.masterFeatList)
                {
                    if (cf.skillRef == abil.refName)
                    {
                        isFeat = true;
                        break;
                    }
                }
                if (!isFeat)
                {
                    return false;
                }
                
            }

        }

        return true;
    }

    public static bool CanPlayerUseConsumable()
    {
        if (!IsJobTrialActive())
        {
            return true;
        }

        if (GameMasterScript.heroPCActor.jobTrial.numConsumableUses >= GameMasterScript.heroPCActor.jobTrial.maxConsumableUses)
        {
            return false;
        }

        return true;
    }

    // Non-static functions

    public void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("jobtrial");

        writer.WriteElementString("trialtier", trialTierLevel.ToString());
        writer.WriteElementString("numitemuses", numConsumableUses.ToString());
        writer.WriteElementString("maxitemuses", maxConsumableUses.ToString());
        writer.WriteElementString("completed", completed.ToString().ToLowerInvariant());
        writer.WriteElementString("charjob", characterJobForTrial.ToString());
        writer.WriteElementString("flaskusesbeforetrial", flaskUsesBeforeTrial.ToString());
        writer.WriteElementString("maxflaskuses", maxFlaskUsesDuringTrial.ToString());

        writer.WriteEndElement();
    }

    public void ReadFromSave(XmlReader reader)
    {
        reader.ReadStartElement();

        while(reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name.ToLowerInvariant())
            {
                case "trialtier":
                    trialTierLevel = reader.ReadElementContentAsInt();
                    break;
                case "numitemuses":
                    numConsumableUses = reader.ReadElementContentAsInt();
                    break;
                case "maxitemuses":
                    maxConsumableUses = reader.ReadElementContentAsInt();
                    break;
                case "charjob":
                    characterJobForTrial = reader.ReadElementContentAsInt();
                    break;
                case "completed":
                    completed = reader.ReadElementContentAsBoolean();
                    break;
                case "flaskusesbeforetrial":
                    flaskUsesBeforeTrial = reader.ReadElementContentAsInt();
                    break;
                case "maxflaskuses":
                    maxFlaskUsesDuringTrial = reader.ReadElementContentAsInt();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();
    }

    public void IncreaseConsumableUsesAndPrint(int amount)
    {
        numConsumableUses += amount;
        StringManager.SetTag(0, numConsumableUses.ToString());
        StringManager.SetTag(1, maxConsumableUses.ToString());
        GameLogScript.LogWriteStringRef("log_jobtrial_consumable_count");
    }

    static List<string> PopulateListWithEmblemModsBasedOnJob(Emblem myEmblem)
    {
        if (GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.MIRAISHARA && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return GameMasterScript.heroPCActor.myJob.emblemMagicMods[myEmblem.emblemLevel];
        }
        List<string> randomEmblemMods = new List<string>();
        List<string> mustHaveAtleastOneOfTheseSkills = null;

        int attempts = 0;
        while (randomEmblemMods.Count < 4)
        {
            attempts++;
            if (attempts > 1000)
            {
                break;
            }
            CharacterJobs randomJob = (CharacterJobs)UnityEngine.Random.Range(0, (int)CharacterJobs.COUNT);
            while (randomJob == CharacterJobs.BERSERKER || randomJob == CharacterJobs.MIRAISHARA || randomJob == CharacterJobs.MONSTER || randomJob == CharacterJobs.SHARA
                || randomJob == CharacterJobs.GENERIC)
            {
                randomJob = (CharacterJobs)UnityEngine.Random.Range(0, (int)CharacterJobs.COUNT);
            }

            CharacterJobData cjd = CharacterJobData.GetJobDataByEnum((int)randomJob);

            string modRef = cjd.emblemMagicMods[myEmblem.emblemLevel][UnityEngine.Random.Range(0, 2)];

            if (RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                mustHaveAtleastOneOfTheseSkills = new List<string>();

                if (RandomJobMode.jobEmblemAbilityRequirements.TryGetValue(modRef, out mustHaveAtleastOneOfTheseSkills))
                {
                    bool valid = false;
                    foreach(string abilRef in mustHaveAtleastOneOfTheseSkills)
                    {
                        if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abilRef))
                        {
                            //Debug.Log("We have " + abilRef + " so it's ok.");
                            valid = true;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        continue;
                    }
                }
                else
                {
                    //Debug.Log("No reqs for " + modRef);
                }
            }

            if (randomEmblemMods.Contains(modRef)) continue;
            randomEmblemMods.Add(modRef);
        }

        return randomEmblemMods;
    }
}
