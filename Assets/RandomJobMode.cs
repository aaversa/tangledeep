using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Text;
using System.Linq;

public partial class RandomJobMode
{
    public const int RANDOM_JOB_MODE_PASSIVE_JP_COST = 700;
    public const int RANDOM_JOB_MODE_ACTIVE_JP_COST = 400;
    public const int RANDOM_JOB_MODE_STARTER_JP_COST = 125;

    public static List<JobAbility> randomizedJobAbilitiesForThisRun;
    public static JobAbility[] innatesForThisRun;

    static bool currentGameSaveIsRandomJobMode;

    static List<JobAbility> allPossiblePassives;
    static List<JobAbility> allPossibleActives;
    static Dictionary<string, JobAbility> dictAllJobAbilities;

    const int MAX_PASSIVES = 6;
    const int MAX_ACTIVES = 12;

    static Dictionary<string, List<string>> dictAbilitiesToPairedRequiredAbilities;

    static List<string>[] allPossibleInnateAbilities;

    const int MINIMUM_PAIRED_ACTIVES = 1;
    const int MAX_ATTEMPTS_TO_FIND_SKILL = 250;

    static Dictionary<string, string> dictInnateRefsToLocalizationStrings;

    public static bool preparingEntryForRandomJobMode;
    public static bool creatingCharacterNotInRJMode;

    public static HashSet<string> passivesThatShouldNeverTakeASlot = new HashSet<string>()
    {
        "skill_bleedbonus",
        "skill_brigandbomber",
        "skill_brigandbonus2",
        "skill_brigandstatbonus",
        "skill_budokastatbonus",
        "skill_champfinder",
        "skill_compost",
        "skill_dancermove",
        "skill_dangermagnet",
        "skill_dropsoul",
        "skill_dropsoul2",
        "skill_dualwielderbonus1",
        "skill_dualwielderbonus2",
        "skill_dualwielderbonus3",
        "skill_dualwielderstatbonus",
        "skill_edgethanestatbonus",
        "skill_effortlessparry",
        "skill_entrepreneur",
        "skill_fastlearner",
        "skill_floraconda2",
        "skill_floraconda3",
        "skill_foodlover",
        "skill_gamblerbonus2",
        "skill_gamblerbonus3",
        "skill_gamblercrit",
        "skill_gamblerstatbonus",
        "skill_hunterct",
        "skill_hunterstatbonus",
        "skill_husynstatbonus",
        "skill_inkcollector",
        "skill_intimidating",
        "skill_keeneyes",
        "skill_luciddreamer",
        "skill_miraisharabonus1",
        "skill_miraisharabonus2",
        "skill_miraisharabonus3",
        "skill_paladinblockbuff",
        "skill_paladinbonus2",
        "skill_paladinstatbonus",
        "skill_preciseshot",
        "skill_rager",
        "skill_reactexplode",
        "skill_recharge",
        "skill_runic2",
        "skill_scavenger",
        "skill_sorceressstatbonus",
        "skill_soulkeeperstatbonus",
        "skill_spellshaperbonus2",
        "skill_spellshaperpowerupbuff",
        "skill_spellshaperstatbonus",
        "skill_spiritcollector",
        "skill_spiritmaster",
        "skill_sworddancerbonus2",
        "skill_sworddancerstatbonus",
        "skill_thanebonus1",
        "skill_thanebonus2",
        "skill_thanebonus3",
        "skill_thirstquencher",
        "skill_toughness",
        "skill_wildcards_passive",
        "skill_wildchildbonus1",
        "skill_wildchildbonus2",
        "skill_wildchildbonus3",
        "skill_wildchildstatbonus"
    };


    public static List<string> ignoreAbilities = new List<string>()
    {
        "skill_hundredfists",
        "skill_powerkick",
        "skill_combatbiography"
    };

    public static HashSet<string> disallowRandomJobModeItems = new HashSet<string>()
    {
        "scroll_jobchange"
    };

    public static bool IsCurrentGameInRandomJobMode()
    {
        return currentGameSaveIsRandomJobMode;
    }

    public static void Initialize()
    {
        CreateForcePairedInnateDictionary();

        if (randomizedJobAbilitiesForThisRun == null) randomizedJobAbilitiesForThisRun = new List<JobAbility>();
        randomizedJobAbilitiesForThisRun.Clear();

        InitializeDataForLocalization();
        InitializeAllPossibleAbilities();
        InitializeAllInnateAbilities();
        CreatePairingDictionary();
    }

    public static void CreateRandomizedJobAbilityList()
    {
        Initialize();

        int numActivesAdded = SetInnateAbilities();
        numActivesAdded += AddAllPassives();
        AddAllActives(numActivesAdded);

        // Let's sort everything with passives in the back.

        randomizedJobAbilitiesForThisRun.Sort((a,b) => a.ability.passiveAbility.CompareTo(b.ability.passiveAbility));
    }

    static void InitializeAllInnateAbilities()
    {
        if (allPossibleInnateAbilities != null) return;
        allPossibleInnateAbilities = new List<string>[3];
        for (int i = 0; i < 3; i++)
        {
            allPossibleInnateAbilities[i] = new List<string>();
        }

        allPossibleInnateAbilities[0].AddRange(new List<string>(){"skill_dualwielderbonus1", "skill_miraisharabonus1", "skill_thanebonus1", "skill_dropsoul", "skill_recharge",
            "skill_gamblercrit", "skill_preciseshot", "skill_paladinblockbuff", "skill_spellshaperpowerupbuff", "skill_dancermove", "skill_compost",
            "skill_bleedbonus" });

        allPossibleInnateAbilities[1].AddRange(new List<string>(){"skill_miraisharabonus2", "skill_thanebonus2", "skill_dropsoul2", "skill_runic2", "skill_gamblerbonus2",
            "skill_gamblerbonus3", "skill_hunterct", "skill_paladinbonus2", "skill_spellshaperbonus2", "skill_sworddancerbonus2", "skill_floraconda2",
            "skill_brigandbonus2" });

        allPossibleInnateAbilities[2].AddRange(new List<string>(){"skill_miraisharabonus3", "skill_thanebonus3", "skill_spiritmaster", "skill_reactexplode", "skill_hunterwolf",
            "skill_lethalfists", "skill_sanctuary", "skill_unstablemagic", "skill_effortlessparry", "skill_floraconda3", "skill_brigandbomber" });

    }

    static int SetInnateAbilities()
    {
        if (randomizedJobAbilitiesForThisRun == null) randomizedJobAbilitiesForThisRun = new List<JobAbility>();
        randomizedJobAbilitiesForThisRun.Clear();

        innatesForThisRun = new JobAbility[3];

        int numOtherAbilitiesAdded = 0;
        for (int i = 0; i < 3; i++)
        {
            string jaPick = allPossibleInnateAbilities[i].GetRandomElement();

            //if (Debug.isDebugBuild) Debug.Log("Innate pick " + i + " is: " + jaPick);
            JobAbility ja = dictAllJobAbilities[jaPick];            
            numOtherAbilitiesAdded += TryAddPairedAbilities(jaPick);

            innatesForThisRun[i] = ja;
        }

        return numOtherAbilitiesAdded;
    }

    /// <summary>
    /// Returns the number of active abilities added incidentally
    /// </summary>
    /// <returns></returns>
    static int AddAllPassives()
    {
        int activesAdded = 0;

        for (int i = 0; i < MAX_PASSIVES; i++)
        {
            JobAbility passive = allPossiblePassives.GetRandomElement();
            int attempts = 0;
            while (randomizedJobAbilitiesForThisRun.Contains(passive) || ignoreAbilities.Contains(passive.abilityRef))
            {
                attempts++;
                if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL) break;
                passive = allPossiblePassives.GetRandomElement();                
            }

            if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL)
            {
                if (Debug.isDebugBuild) Debug.Log("Failed adding passive " + i);
            }

            randomizedJobAbilitiesForThisRun.Add(passive);
            
            int numberOfPairedAbilitiesAdded = TryAddPairedAbilities(passive.abilityRef);
            activesAdded += numberOfPairedAbilitiesAdded;
        }

        return activesAdded;
    }

    static int TryAddPairedAbilities(string abilityRef)
    {
        int abilitiesAdded = 0;
        List<string> requiredSkillListOptions;
        if (dictAbilitiesToPairedRequiredAbilities.TryGetValue(abilityRef, out requiredSkillListOptions))
        {
            //if (Debug.isDebugBuild) Debug.Log("Getting required list for " + abilityRef);
            for (int x = 0; x < MINIMUM_PAIRED_ACTIVES; x++)
            {
                string activeRef = requiredSkillListOptions.GetRandomElement();
                //Debug.Log("Active ref is: " + activeRef);
                JobAbility ja = dictAllJobAbilities[activeRef];
                int attempts = 0;
                while (randomizedJobAbilitiesForThisRun.Contains(ja) || ignoreAbilities.Contains(ja.abilityRef))
                {
                    attempts++;
                    if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL) break;
                    activeRef = requiredSkillListOptions.GetRandomElement();
                    ja = dictAllJobAbilities[activeRef];
                }

                if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL)
                {
                    if (Debug.isDebugBuild) Debug.Log("Failed pairing ability " + x + " for " + abilityRef);
                    continue;
                }

                randomizedJobAbilitiesForThisRun.Add(ja);
                abilitiesAdded++;

            }            
        }

        return abilitiesAdded;
    }

    static void AddAllActives(int numPreviouslyAdded)
    {
        for (int i = numPreviouslyAdded; i < MAX_ACTIVES; i++)
        {
            JobAbility active = allPossibleActives.GetRandomElement();
            int attempts = 0;
            while (randomizedJobAbilitiesForThisRun.Contains(active) || ignoreAbilities.Contains(active.abilityRef))
            {
                attempts++;
                if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL) break;
                active = allPossibleActives.GetRandomElement();
            }

            if (attempts >= MAX_ATTEMPTS_TO_FIND_SKILL)
            {
                if (Debug.isDebugBuild) Debug.Log("Failed finding active skill " + i);
                continue;
            }

            randomizedJobAbilitiesForThisRun.Insert(0, active);

            int numAdded = TryAddPairedAbilities(active.abilityRef);
            i += numAdded;
        }
    }

    static void InitializeAllPossibleAbilities()
    {
        if (Debug.isDebugBuild) Debug.Log("INITIALIZING ALL POSSIBLE ABILITIES FOR RANDOM JOB MODE");
        if (allPossibleActives != null) return;
        
        dictAllJobAbilities = new Dictionary<string, JobAbility>();

        allPossibleActives = new List<JobAbility>();
        allPossiblePassives = new List<JobAbility>();

        InitializeSpecialJobAbilities();

        foreach (CharacterJobData cjd in GameMasterScript.masterJobList)
        {
            if (cjd.jobEnum == CharacterJobs.SHARA) continue;
            foreach (JobAbility ja in cjd.GetBaseJobAbilities())
            {
                if (ja.innate)
                {
                    dictAllJobAbilities[ja.abilityRef] = ja;
                    //Debug.Log("added innate " + ja.abilityRef);
                    continue;
                }
                if (ja.ability.passiveAbility)
                {
                    EvaluatePassiveForAdditionToMasterList(ja);
                }
                else EvaluateActiveForAdditionToMasterList(ja);
            }
        }
    }

    static void EvaluatePassiveForAdditionToMasterList(JobAbility ja)
    {
        if (ja.innate) return;
        if (allPossiblePassives.Contains(ja)) return;
        allPossiblePassives.Add(ja);
        dictAllJobAbilities[ja.abilityRef] = ja;
    }

    static void EvaluateActiveForAdditionToMasterList(JobAbility ja)
    {
        if (ja.innate) return;
        if (allPossibleActives.Contains(ja)) return;
        allPossibleActives.Add(ja);
        dictAllJobAbilities[ja.abilityRef] = ja;
    }

    public static void EnterRandomJobMode(bool forceReset = false)
    {
        currentGameSaveIsRandomJobMode = true;

        CreateRandomizedJobAbilityList();

        GameMasterScript.heroPCActor.myJob = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND);
        GameStartData.playerJob = "brigand";
        GameStartData.jobAsEnum = CharacterJobs.BRIGAND;

        InitializeGameStartDataForRandomJobMode();

        //GameLogScript.LogWriteStringRef("Entered! Check your job ability list please.");
        if (Debug.isDebugBuild) Debug.Log("Entered random job mode for the first time!");

        RandomJobMode.preparingEntryForRandomJobMode = false;
    }

    public static void InitializeGameStartDataForRandomJobMode()
    {
        GameStartData.playerJob = "brigand";
        GameStartData.jobAsEnum = CharacterJobs.BRIGAND;
    }

    public static void OnDungeonGenerationFinished()
    {
        RemoveHerbalistFromGame();
        SeedDungeonWithCoolRandomMonsters();
        SeedRelicsInBosses();

        RemoveBranchesAtRandom();
    }

    static void RemoveHerbalistFromGame()
    {
        Map floodedTemple1f = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FLOODED_TEMPLE_2F - 1);
        if (floodedTemple1f == null) return;

        Actor herbalist = floodedTemple1f.FindActor("npc_herbalist");

        if (herbalist == null) return;

        floodedTemple1f.RemoveActorFromMap(herbalist);
    }

    public static void SeedRelicsInBosses()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("rj_bossrelics") == 1) return;

        // Dirtbeak's relic
        Monster dirtbeak = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS1_MAP_FLOOR).FindActor("mon_banditwarlord") as Monster;
        if (dirtbeak == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No dirtbeak? what?!");
        }
        else
        {
            TryAddRelicTreasureToBoss(dirtbeak, 1.3f);
        }

        // Robitt
        Monster sentryBoss = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR).FindActor("mon_ancientsteamgolem") as Monster;
        if (sentryBoss == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No sentry boss? what?!");
        }
        else
        {
            TryAddRelicTreasureToBoss(sentryBoss, 1.5f);
        }

        // Final boss relic
        Monster finalboss2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR).FindActor("mon_finalbossai") as Monster;
        if (finalboss2 == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No final boss 2? what?!");
        }
        else
        {
            TryAddRelicTreasureToBoss(finalboss2, 1.6f);
        }

        GameMasterScript.heroPCActor.SetActorData("rj_bossrelics", 1);
    }

    public static void TryAddRelicTreasureToBoss(Monster mon, float cv)
    {
        float addCV = 0f;
        if (GameStartData.NewGamePlus == 1) addCV += 0.3f;
        if (GameStartData.NewGamePlus == 2) addCV += 0.3f;

        if (mon == null) return;

        float cap = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? 2.2f : 1.9f;

        Item legRelic = LegendaryMaker.CreateNewLegendaryItem(Mathf.Min(cv + addCV, cap));
        Item copy = LootGeneratorScript.CreateItemFromTemplateRef(legRelic.actorRefName, 2f, 0f, false, true);
        copy.SetActorData("grc", 1);

        mon.myInventory.AddItem(copy, false);

        //if (Debug.isDebugBuild) Debug.Log("Added " + copy.actorRefName + " relic to " + mon.actorRefName);
    }

    static void SetProgressFlagsForRandomJobMode()
    {
        GameMasterScript.heroPCActor.SetActorData("swordmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("axemastery", 3);
        GameMasterScript.heroPCActor.SetActorData("spearmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("daggermastery", 3);
        GameMasterScript.heroPCActor.SetActorData("macemastery", 3);
        GameMasterScript.heroPCActor.SetActorData("bowmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("fistmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("staffmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("clawmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("whipmastery", 3);
        GameMasterScript.heroPCActor.SetActorData("mastered_current_job", 1);
    }

    public static void OnReturnToTitleScreen()
    {
        currentGameSaveIsRandomJobMode = false;
    }

    static void InitializeDataForLocalization()
    {
        if (dictInnateRefsToLocalizationStrings != null) return;

        dictInnateRefsToLocalizationStrings = new Dictionary<string, string>();

        dictInnateRefsToLocalizationStrings["skill_thanebonus1"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.EDGETHANE).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_thanebonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.EDGETHANE).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_thanebonus3"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.EDGETHANE).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_dropsoul"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SOULKEEPER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_dropsoul2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SOULKEEPER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_spiritmaster"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SOULKEEPER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_recharge"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUSYN).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_runic2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUSYN).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_reactexplode"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUSYN).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_gamblercrit"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.GAMBLER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_gamblerbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.GAMBLER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_gamblerbonus3"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.GAMBLER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_preciseshot"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUNTER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_hunterct"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUNTER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_hunterwolf"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.HUNTER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_lethalfists"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BUDOKA).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_paladinblockbuff"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.PALADIN).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_paladinbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.PALADIN).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_sanctuary"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.PALADIN).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_spellshaperpowerupbuff"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SPELLSHAPER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_spellshaperbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SPELLSHAPER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_unstablemagic"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SPELLSHAPER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_dancermove"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SWORDDANCER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_sworddancerbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SWORDDANCER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_effortlessparry"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SWORDDANCER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_compost"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.FLORAMANCER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_floraconda2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.FLORAMANCER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_floraconda3"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.FLORAMANCER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_bleedbonus"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_brigandbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_brigandbomber"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.BRIGAND).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_dualwielderbonus1"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.DUALWIELDER).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_dualwielderbonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.DUALWIELDER).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_dualwielderbonus3"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.DUALWIELDER).GetBonusDescription(2);

        dictInnateRefsToLocalizationStrings["skill_miraisharabonus1"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.MIRAISHARA).GetBonusDescription(0);
        dictInnateRefsToLocalizationStrings["skill_miraisharabonus2"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.MIRAISHARA).GetBonusDescription(1);
        dictInnateRefsToLocalizationStrings["skill_miraisharabonus3"] = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.MIRAISHARA).GetBonusDescription(2);

        
    }

    public static string GetBonusDescription(int tier)
    {
        //if (Debug.isDebugBuild) Debug.Log("Get description for " + innatesForThisRun[tier].abilityRef);
        return dictInnateRefsToLocalizationStrings[innatesForThisRun[tier].abilityRef];
    }

    public static void AddJobAbilityFromSave(string abilRef)
    {
        if (randomizedJobAbilitiesForThisRun == null)
        {
            randomizedJobAbilitiesForThisRun = new List<JobAbility>();
        }
        randomizedJobAbilitiesForThisRun.Add(dictAllJobAbilities[abilRef]);
    }

    public static void AddInnateAbilityFromSave(string abilRef, int tier)
    {
        if (innatesForThisRun == null)
        {
            innatesForThisRun = new JobAbility[3];
        }
        innatesForThisRun[tier] = dictAllJobAbilities[abilRef];
    }

    public static bool AllowEntryIntoRandomJobMode()
    {
        if (GameStartData.gameInSharaMode) return false;
        
        return true;
    }
    public static void SetGameToRandomJobMode(bool value)
    {
        //if (Debug.isDebugBuild) Debug.Log("Set current game in random job/ronin mode to " + value);
        currentGameSaveIsRandomJobMode = value;
    }

    public static int GetSkillCost(JobAbility ja)
    {
        if (ja.ability.passiveAbility) return RANDOM_JOB_MODE_PASSIVE_JP_COST;

        int numRJLearned = GameMasterScript.heroPCActor.ReadActorData("numrjabils_learned");

        if (numRJLearned < 0) numRJLearned = 0;

        if (numRJLearned < 2) return RANDOM_JOB_MODE_STARTER_JP_COST;

        return RANDOM_JOB_MODE_ACTIVE_JP_COST;
    }

    public static void OnAbilityLearned(string refName)
    {
        if (!IsCurrentGameInRandomJobMode()) return;

        for (int i = 0; i < innatesForThisRun.Length; i++)
        {
            if (innatesForThisRun[i].abilityRef == refName) return;
        }

        string forcePaired = "";

        /*if (Debug.isDebugBuild)
        {
            Debug.Log("On ability learned: " + refName + ", innate 0 is null? " + (innatesForThisRun[0] == null));
            if (innatesForThisRun[0] != null)
            {
                Debug.Log(innatesForThisRun[0].abilityRef + " is abil ref. Null or empty? " + string.IsNullOrEmpty(innatesForThisRun[0].abilityRef));
            }
        } */

        if (forcePairedInnates.TryGetValue(innatesForThisRun[0].abilityRef, out forcePaired))
        {
            if (forcePaired == refName) return;
        }

        //if (Debug.isDebugBuild) Debug.Log("Use slot from " + refName);

        int numRJLearned = GameMasterScript.heroPCActor.ReadActorData("numrjabils_learned");

        if (numRJLearned < 0) numRJLearned = 0;

        numRJLearned++;
        
        GameMasterScript.heroPCActor.SetActorData("numrjabils_learned", numRJLearned);
    }

    public static JobAbility GetMasterAbility()
    {
        return innatesForThisRun[2];
    }

    public static void SeedDungeonWithCoolRandomMonsters()
    {
        // Let's make a bunch of monsters

        if (MonsterMaker.uniqueMonstersSpawnedInSaveFile.Count == 0)
        {
            if (Debug.isDebugBuild) Debug.Log("Creating cool new random monsters!");
            MysteryDungeon testMD = new MysteryDungeon("test");
            testMD.monsterFamilies.AddToTable("frogs", 100);
            testMD.monsterFamilies.AddToTable("bandits", 100);
            testMD.monsterFamilies.AddToTable("hybrids", 100);
            testMD.monsterFamilies.AddToTable("robots", 100);
            testMD.monsterFamilies.AddToTable("snakes", 100);
            testMD.monsterFamilies.AddToTable("insects", 100);
            testMD.monsterFamilies.AddToTable("beasts", 100);
            testMD.monsterFamilies.AddToTable("spirits", 100);

            int max = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? 13 : 10;
            for (int i = 0; i < max; i++)
            {
                
                float cv = 1.0f + (i * 0.1f);

                cv = Mathf.Clamp(cv, 1f, BalanceData.GetMaxChallengeValueForItems());

                int level = BalanceData.GetMonsterLevelByCV(cv, false);

                if (level == 1) level += UnityEngine.Random.Range(0, 2);
                else if (level == 4) level += UnityEngine.Random.Range(0, 2);
                else if (level == 7) level += UnityEngine.Random.Range(0, 2);
                else if (level == 12) level += UnityEngine.Random.Range(0, 2);

                int numberOfMonsters = UnityEngine.Random.Range(1, 3);

                for (int x = 0; x < numberOfMonsters; x++)
                {
                    MonsterTemplateData mtd = MonsterMaker.CreateNewMonster(level, testMD);
                    if (Debug.isDebugBuild) Debug.Log("Created " + mtd.refName);
                }                
            }
        }

        foreach(Map m in MapMasterScript.theDungeon.maps)
        {
            if (m.dungeonLevelData.safeArea) continue;
            if (m.IsTownMap()) continue;
            if (m.IsJobTrialFloor()) continue;
            if (m.IsMysteryDungeonMap()) continue;
            if (m.IsBossFloor()) continue;
            if (m.dungeonLevelData.spawnTable == null) continue;
            if (m.floor >= MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR && m.floor <= MapMasterScript.BEAST_DRAGON_DUNGEONEND_FLOOR) continue;
            if (m.floor >= MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR && m.floor <= MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR) continue;

            int numMonsters = (int)(m.dungeonLevelData.minMonsters * 0.25f);
            if (numMonsters < 2) numMonsters = 2;

            //Debug.Log("Adding " + numMonsters + " monstars to " + m.dungeonLevelData.customName + " " + m.GetName() + " " + m.floor);

            for (int i = 0; i < numMonsters; i++)
            {
                Monster mon = m.SpawnRandomMonster(false, true, MonsterMaker.GetMonsterOfCV(m.dungeonLevelData.challengeValue), true);
                //Debug.Log("Spawned a " + mon.actorRefName);
            }
        }
    }

    public static string GetRandoMonsterRefForMap(float cv)
    {
        return MonsterMaker.GetMonsterOfCV(cv);
    }

    public static void CheckForInitLearnInnatesOnCharacterStart()
    {
        CreateForcePairedInnateDictionary();

        if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(innatesForThisRun[0].abilityRef)) return;

        if (innatesForThisRun[0] == null) return; // lol
        GameMasterScript.heroPCActor.LearnAbility(innatesForThisRun[0], false, true);

        string firstInnate = innatesForThisRun[0].abilityRef;
        GameMasterScript.heroPCActor.LearnAbility(dictAllJobAbilities[forcePairedInnates[firstInnate]], false, true);
    }

    public static bool DoesAbilityTakeSlotInRandomJobMode(JobAbility ja)
    {
        return DoesAbilityTakeSlotInRandomJobMode(ja.abilityRef);
    }

    public static bool DoesAbilityTakeSlotInRandomJobMode(string refName)
    {
        return !passivesThatShouldNeverTakeASlot.Contains(refName);
    }

    public static string GetRandomMonsterTableForSummoningBasedOnPlayerLevel()
    {
        int lvl = GameMasterScript.heroPCActor.myStats.GetLevel();

        if (lvl >= 0 && lvl < 6)
        {
            return "random1";
        }
        else if (lvl >= 6 && lvl < 12)
        {
            return "random2";
        }
        else return "random3";

    }

    public static float GetRJGoldMultiplier()
    {
        //Debug.Log("RJ gold multiplier is: " + 1.33f);
        return 1.33f;
    }

    public static float GetRJBonusReverieChance()
    {
        return 0.075f;
    }

    public static float GetChanceOfMagicalGear()
    {
        return 0.1f;
    }
}

public partial class DebugConsole
{
    object EnterRandomJobMode(params string[] args)
    {
        RandomJobMode.EnterRandomJobMode(forceReset:true);
        return "Done!";
    }
}

public partial class MetaProgressScript
{

    private static void WriteRandomJobModeStuff(XmlWriter metaWriter)
    {
        if (!RandomJobMode.IsCurrentGameInRandomJobMode()) return;
        metaWriter.WriteElementString("randomjob", "yes");

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < RandomJobMode.randomizedJobAbilitiesForThisRun.Count; i++)
        {
            JobAbility ja = RandomJobMode.randomizedJobAbilitiesForThisRun[i];
            sb.Append(ja.abilityRef);
            if (i < RandomJobMode.randomizedJobAbilitiesForThisRun.Count - 1)
            {
                sb.Append(',');
            }
        }

        metaWriter.WriteElementString("rjabils", sb.ToString());

        sb.Length = 0;

        sb.Append(RandomJobMode.innatesForThisRun[0].abilityRef);
        sb.Append(',');
        sb.Append(RandomJobMode.innatesForThisRun[1].abilityRef);
        sb.Append(',');
        sb.Append(RandomJobMode.innatesForThisRun[2].abilityRef);

        metaWriter.WriteElementString("rjinnates", sb.ToString());

        string excludeLevelBuilder = "";
        bool first = true;

        if (RandomJobMode.removedBranchFloors != null)
        {
            foreach (int remFloor in RandomJobMode.removedBranchFloors)
            {
                if (!first) excludeLevelBuilder += ",";
                excludeLevelBuilder += remFloor.ToString();
                first = false;
            }
        }


        metaWriter.WriteElementString("rjxfloors", excludeLevelBuilder);
    }

    private static void ReadRandomJobModeStuff(XmlReader metaReader, string key)
    {
        switch(key)
        {
            case "randomjob":
                string nothing = metaReader.ReadElementContentAsString();
                if (!RandomJobMode.creatingCharacterNotInRJMode)
                {
                    Debug.Log("Loading a file in random job mode in metaprogress.");
                    RandomJobMode.SetGameToRandomJobMode(true);
                }                
                RandomJobMode.Initialize();
                break;
            case "rjabils":
                string contents = metaReader.ReadElementContentAsString();
                string[] split = contents.Split(',');
                for (int i = 0; i < split.Length; i++)
                {
                    RandomJobMode.AddJobAbilityFromSave(split[i]);
                }
                break;
            case "rjinnates":
                contents = metaReader.ReadElementContentAsString();
                split = contents.Split(',');
                for (int i = 0; i < split.Length; i++)
                {
                    RandomJobMode.AddInnateAbilityFromSave(split[i], i);
                }
                break;
            case "rjxfloors":
                contents = metaReader.ReadElementContentAsString();
                split = contents.Split(',');
                if (RandomJobMode.removedBranchFloors == null) RandomJobMode.removedBranchFloors = new List<int>();
                for (int i = 0; i < split.Length; i++)
                {
                    int floor = 0;
                    if (int.TryParse(split[i], out floor))
                    {
                        RandomJobMode.removedBranchFloors.Add(floor);
                    }
                    
                }
                break;
        }
    }

    
}