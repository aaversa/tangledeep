using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class SharaModeStuff {

    public static CharacterJobData jobData;

    public const float CHANCE_EXTRA_FOOD_FROM_MONSTERS = 0.18f;
    public const float CHANCE_MAGIC_GEAR_BONUS = 0.25f;
    public const int CAMPFIRE_FLOOR_INTERVAL = 3;
    public const int FINALBOSS_PHASE_2_TURNS = 50;
    public const float SHARA_HEALING_FROM_FOUNTAIN = 0.2f;
    public const float SHARA_PET_HEALING_FROM_FOUNTAIN = 0.2f;
    public const float FOUNTAIN_OVERFLOW_SHIELD_MULTIPLIER = 0.8f;

    static Dictionary<string, string> abilityBackupDescriptions;
    static bool abilitiesBackedUp;

    public static readonly int[] purchasesToLevelThresholds =
    {
        0, // 0 purchases for level 0
        0, // 0 purchases for level 1
        3, // Level 2
        7, // Level 3
        11, // Level 4
        15, // Level 5
        18, // Level 6
        22, // Level 7
        26, // Level 8
        30, // Level 9
        33, // Level 10
        37, // Level 11
        41, // Level 12
        46, // Level 13
        51, // Level 14
        55, // Level 15
        62, // Level 16
        68, // Level 17
        75, // Level 18
        82, // Level 19
        89, // Level 20
    };

    // These powers should not pop up as options in learn prompt.
    public static HashSet<string> nonLearnableSharaPowers = new HashSet<string>()
    {
        "exp_skill_booststrength",
        "exp_skill_boostswiftness",
        "exp_skill_boostdiscipline",
        "exp_skill_boostspirit",
        "exp_skill_boostguile"
    };

    public static HashSet<string> disallowSharaModeItems = new HashSet<string>()
    {
        "orb_itemworld",
        "item_monstermallet",
        "scroll_jobchange",
        "seeds_tree1",
        "seeds_tree2",
        "seeds_tree3",
        "seeds_tree4",
        "spice_rosepetals"
    };

    public static HashSet<string> disallowSharaModeMagicMods = new HashSet<string>()
    {
        "mm_companions_new",
        "mm_companions"
    };

    public static HashSet<string> disallowSharaModeRegularSkills = new HashSet<string>()
    {
        "skill_blooddebts"
    };

    public static List<string> possibleCampfireNPCs = new List<string>()
    {
        "npc_randomshop1",
        "npc_randomshop2",
        "npc_randomshop3",
        "npc_randomshop4",
        "npc_randomshop5",
        "npc_randomshop6"
    };

    public static int GetNumberOfDominatedCreatures()
    {
        int count = 0;
        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;            
            Monster mn = act as Monster;
            if (!mn.myStats.IsAlive()) continue;
            if (mn.myStats.CheckHasStatusName("exp_status_dominated"))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// As Shara gains levels and reaches new campfires, there is an increasing chance to encounter a weapon master statue.
    /// </summary>
    /// <returns></returns>
    public static bool CheckForWeaponMasterStatueAtCampfire()
    {
        if (GameMasterScript.heroPCActor.myStats.GetLevel() < 5) return false;
        int numWeaponMastersEncountered = ProgressTracker.CheckProgress(TDProgress.WEAPON_MASTER_STATUES, ProgressLocations.HERO);
        if (numWeaponMastersEncountered >= 3) return false;
        if (numWeaponMastersEncountered < 0) numWeaponMastersEncountered = 0;

        int campfiresSinceWeaponMaster = GameMasterScript.heroPCActor.ReadActorData("campfires_since_weaponmaster");
        if (campfiresSinceWeaponMaster < 0) campfiresSinceWeaponMaster = 0;        

        float baseChance = 0.1f;
        baseChance += (campfiresSinceWeaponMaster * 0.275f);
        if (UnityEngine.Random.Range(0,1f) > baseChance)
        {
            campfiresSinceWeaponMaster++;
            GameMasterScript.heroPCActor.SetActorData("campfires_since_weaponmaster", campfiresSinceWeaponMaster);
            return false;
        }

        GameMasterScript.heroPCActor.SetActorData("campfires_since_weaponmaster", 0);

        numWeaponMastersEncountered++;
        ProgressTracker.SetProgress(TDProgress.WEAPON_MASTER_STATUES, ProgressLocations.HERO, numWeaponMastersEncountered);
        return true;
    }


    public static bool SpawnLearnPowerDialog(bool sharaPowers = false)
    {
        Debug.Log("Preparing to spawn shara power dialog.");

        List<string> possiblePowers = null;

        string convoRef = "exp_choose_shara_ability";

        if (sharaPowers)
        {
            possiblePowers = GameMasterScript.masterUniqueSharaPowerList.Keys.ToList();
            convoRef = "exp_choose_shara_ability_unique";
        }
        else
        {
            possiblePowers = GameMasterScript.masterSharaPowerList.Keys.ToList();
        }

        int numPowersToLearn = sharaPowers ? 2 : 3; // For non-Shara powers, we want more options

        // Remove powers we already know
        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            possiblePowers.Remove(abil.refName);
        }

        if (possiblePowers.Count < numPowersToLearn)
        {
            // No powers to learn?
            return false;
        }

        List<string> strPowers = new List<string>();

        for (int i = 0; i < numPowersToLearn; i++)
        {
            string power = possiblePowers[UnityEngine.Random.Range(0, possiblePowers.Count)];
            while (nonLearnableSharaPowers.Contains(power) || strPowers.Contains(power))
            {
                power = possiblePowers[UnityEngine.Random.Range(0, possiblePowers.Count)];
            }
            strPowers.Add(power);
        }       
        
        Conversation chooseConvo = GameMasterScript.FindConversation(convoRef);
        TextBranch tb = chooseConvo.FindBranch("intro");
        tb.responses.Clear();

        AbilityScript[] powers = new AbilityScript[numPowersToLearn];
        for (int i = 0; i < numPowersToLearn; i++)
        {
            powers[i] = GameMasterScript.masterAbilityList[strPowers[i]];
        }

        for (int i = 0; i < numPowersToLearn; i++)
        {
            ButtonCombo bc = new ButtonCombo();
            bc.actionRef = powers[i].refName;
            bc.dialogEventScriptValue = powers[i].refName;
            bc.dbr = DialogButtonResponse.CONTINUE;
            bc.threeColumnStyle = true;
            bc.spriteRef = powers[i].iconSprite;
            bc.buttonText = powers[i].description;
            bc.headerText = powers[i].abilityName;
            bc.dialogEventScript = "LearnSharaPowerFromDialog";
            bc.extraVerticalPadding = 40;
            tb.responses.Add(bc);            
        }

        UIManagerScript.StartConversationByRef(chooseConvo.refName, DialogType.KEYSTORY, null);        
        return true;
    }

    public static void UpdateListOfKnownSharaPowers()
    {
        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (GameMasterScript.masterSharaPowerList.ContainsKey(abil.refName))
            {
                GameMasterScript.masterSharaPowerList.Remove(abil.refName);
            }            
        }
    }

    public static void CalculatePlayerLevelFromStatBoosts()
    {
        if (jobData == null)
        {
            jobData = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SHARA);
        }

        int numBoosts = 0;
        foreach (JobAbility ja in jobData.JobAbilities)
        {
            if (ja.innate) continue;
            // Cost increases by 100jp each time, so
            int existingBoosts = GameMasterScript.heroPCActor.ReadActorData(ja.abilityRef + "_purchased");
            if (existingBoosts < 0)
            {
                existingBoosts = 0;
            }
            numBoosts += existingBoosts;
        }

        int highestLevel = 0;
        for (int i = 0; i < purchasesToLevelThresholds.Length; i++)
        {
            if (numBoosts < purchasesToLevelThresholds[i])
            {
                break;
            }
            highestLevel = i;
        }

        GameMasterScript.heroPCActor.myStats.SetLevel(highestLevel);
    }

    public static void UpdateStatJPCostsInJobData()
    {
        if (jobData == null)
        {
            jobData = CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SHARA);
        }

        CheckForFirstTimeNameBackups();

        // Go through each "boost stat" ability and scale JP based on existing boosts.

        RefreshSharaStatBoostSkillDescriptions();

        foreach (JobAbility ja in jobData.JobAbilities)
        {
            if (ja.innate) continue;
            // Cost increases by 100jp each time, so
            int existingBoosts = GameMasterScript.heroPCActor.ReadActorData(ja.abilityRef + "_purchased");
            ja.jpCost = 100;
            if (existingBoosts >= 1)
            {
                ja.jpCost += existingBoosts * 50;
            }
        }
    }

    public static void DisableStairsToRiverstoneInCurrentMap()
    {
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.pointsToFloor == MapMasterScript.TOWN_MAP_FLOOR || st.NewLocation == MapMasterScript.singletonMMS.townMap)
            {
                st.DisableActor();
            }
        }   
    }

    /// <summary>
    /// Returns TRUE if we're shara and beat the second boss, triggering cutscene
    /// </summary>
    /// <returns></returns>
    public static bool CheckForBoss2Clear()
    {
        if (MapMasterScript.activeMap.floor == MapMasterScript.BOSS2_MAP_FLOOR && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) != 3)
            {
                bool scientistAlive = false;
                bool anySpiritsAlive = false;
                foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
                {
                    if (m.actorfaction != Faction.ENEMY) continue;
                    if (!m.myStats.IsAlive() || m.isInDeadQueue) continue;
                    if (!scientistAlive && m.actorRefName == "mon_scientist_summoner")
                    {
                        scientistAlive = true;
                        continue;
                    }
                    if (!anySpiritsAlive && (m.actorRefName == "mon_youngfireelemental" || m.actorRefName == "mon_younglightningelemental" || m.actorRefName == "mon_shadowelemental" || m.actorRefName == "mon_youngwaterelemental"))
                    {
                        anySpiritsAlive = true;
                        continue;
                    }
                }

                if (!scientistAlive && !anySpiritsAlive)
                {
                    GameEventsAndTriggers.SharaBoss2Victory();
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsSharaModeActive()
    {
        //Debug.Log(GameStartData.jobAsEnum + " " + GameStartData.gameInSharaMode + " " + GameStartData.slotInSharaMode[GameStartData.saveGameSlot]);

        return DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) &&
               (GameStartData.jobAsEnum == CharacterJobs.SHARA || // we are a shara
               GameStartData.gameInSharaMode || // or the game is flagged as being in shara mode
                GameStartData.slotInSharaMode[GameStartData.saveGameSlot] || // or the slot is
                (GameMasterScript.createdHeroPCActor && 
                //GameMasterScript.heroPCActor != null &&
                 //GameMasterScript.heroPCActor.myJob != null &&
                 GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)); // or our current job is a Shara
    }

    public static string GetDominateDisplayName()
    {
        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname4");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname3");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname2");
        }
        else
        {
            return StringManager.GetString("exp_shara_dominate_displayname1");
        }
    }

    public static string GetPreviousDominateDisplayName()
    {
        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname3");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname2");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_displayname1");
        }
        else
        {
            return StringManager.GetString("exp_shara_dominate_displayname1");
        }
    }

    public static int GetDominateCreatureCap()
    {
        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return 6;
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return 4;
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }

    public static string GetDominateVerb()
    {
        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_verb_dominate"); // verb
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_verb_command"); // verb
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_verb_recruit"); // verb
        }
        else
        {
            return StringManager.GetString("exp_verb_charm"); // verb
        }
    }

    public static string GetDominateExtraDescription()
    {
        StringManager.SetTag(0, GetDominateVerb());

        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_extradesc2");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_extradesc1");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return StringManager.GetString("exp_shara_dominate_extradesc1");
        }
        else
        {
            return StringManager.GetString("exp_shara_dominate_extradesc1");
        }
    }

    public static string GetDominateDescription()
    {
        StringManager.SetTag(0, GetDominateVerb());

        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            StringManager.SetTag(1, "6"); // number of creatures
            return StringManager.GetString("exp_shara_dominate_description2");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            StringManager.SetTag(1, "4"); // number of creatures
            return StringManager.GetString("exp_shara_dominate_description1");
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            StringManager.SetTag(1, "3"); // number of creatures
            return StringManager.GetString("exp_shara_dominate_description1");
        }
        else
        {
            StringManager.SetTag(1, "2"); // number of creatures
            return StringManager.GetString("exp_shara_dominate_description1");
        }
    }

    public static int GetDominateLevel() 
    {
        if (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 3)
        {
            return 3;
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) >= 3)
        {
            return 2;
        }
        else if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Adjusts skill description for our "Increase Strength" (etc) skills to reflect # of times purchased
    /// </summary>
    public static void RefreshSharaStatBoostSkillDescriptions()
    {
        foreach(JobAbility ja in GameMasterScript.heroPCActor.myJob.JobAbilities)
        {
            if (ja.innate) continue;
            string baseDesc = abilityBackupDescriptions[ja.abilityRef];
            int existingBoosts = GameMasterScript.heroPCActor.ReadActorData(ja.abilityRef + "_purchased");
            if (existingBoosts < 0)
            {
                existingBoosts = 0;
                ja.ability.description = baseDesc;
                continue;
            }
            StringManager.SetTag(0, existingBoosts.ToString());
            baseDesc += " " + StringManager.GetString("shara_stat_boost_counter");
            ja.ability.description = baseDesc;
        }
    }

    static void CheckForFirstTimeNameBackups()
    {
        if (abilitiesBackedUp) return;
        abilityBackupDescriptions = new Dictionary<string, string>();
        foreach (JobAbility ja in CharacterJobData.GetJobDataByEnum((int)CharacterJobs.SHARA).JobAbilities)
        {
            abilityBackupDescriptions.Add(ja.abilityRef, ja.ability.description);
        }
        abilitiesBackedUp = true;
    }

    public static void RefreshSharaAbilityNamesAndDescriptions()
    {
        CheckForFirstTimeNameBackups();

        AbilityScript dominate = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_dominate");
        dominate.abilityName = GetDominateDisplayName();
        dominate.description = GetDominateDescription();        
        dominate.extraDescription = GetDominateExtraDescription();
        dominate.shortDescription = GetDominateDescription();
        dominate.ParseNumberTags();

        dominate.listEffectScripts[0].effectName = dominate.abilityName;

        // Other abilities must also be updated to reflect name change

        AbilityScript playerImprovedDominate = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_improveddominate");
        if (playerImprovedDominate == null)
        {
            playerImprovedDominate = GameMasterScript.masterAbilityList["skill_improveddominate"];
        }
        playerImprovedDominate.description = CustomAlgorithms.ParseLiveMergeTags(playerImprovedDominate.description);

        AbilityScript playerCommandingPresence = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_commandingpresence");
        if (playerCommandingPresence == null)
        {
            playerCommandingPresence = GameMasterScript.masterAbilityList["skill_commandingpresence"];
        }
        playerCommandingPresence.description = CustomAlgorithms.ParseLiveMergeTags(playerCommandingPresence.description);
        playerCommandingPresence.extraDescription = CustomAlgorithms.ParseLiveMergeTags(playerCommandingPresence.extraDescription);

        AbilityScript playerCharisma = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef("skill_charisma");
        if (playerCharisma == null)
        {
            playerCharisma = GameMasterScript.masterAbilityList["skill_charisma"];
        }
        playerCharisma.description = CustomAlgorithms.ParseLiveMergeTags(playerCharisma.description);


        /* AbilityScript masterCopy = GameMasterScript.masterAbilityList["skill_dominate"];
        masterCopy.abilityName = dominate.abilityName;
        masterCopy.description = dominate.description;
        masterCopy.extraDescription = dominate.extraDescription;
        masterCopy.ParseNumberTags(); */
    }

    /// <summary>
    /// Shara behaves differently when she picks up a fountain. Instead of filling a flask, she regains resources
    /// immediately.
    /// </summary>
    public static void DoSharaFountainInteraction(Destructible fountain)
    {
        var shara = GameMasterScript.heroPCActor;
        
        // Restore 20% all core resources
        float stamina = shara.myStats.GetMaxStat(StatTypes.STAMINA) * SHARA_HEALING_FROM_FOUNTAIN;
        float energy = shara.myStats.GetMaxStat(StatTypes.ENERGY) * SHARA_HEALING_FROM_FOUNTAIN;
        shara.myStats.ChangeStat(StatTypes.STAMINA, stamina, StatDataTypes.CUR, true);
        shara.myStats.ChangeStat(StatTypes.ENERGY, energy, StatDataTypes.CUR, true);

        // Health is different, if we are going to overflow, spend the remainder on the shield.
        float maxHealth = shara.myStats.GetMaxStat(StatTypes.HEALTH);
        float currentHealth = shara.myStats.GetCurStat(StatTypes.HEALTH);
        float missing = maxHealth - currentHealth;
        
        //here's how much we should regain.
        float healthRegained = maxHealth * SHARA_HEALING_FROM_FOUNTAIN;
        int overflow = (int)(healthRegained - missing);
        
        //give it to the player, the overflow is ignored here
        GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.HEALTH, healthRegained, StatDataTypes.CUR, true);

        //But if we regained more than we have missing, pour the rest into the shield.
        if (overflow > 0)
        {
            overflow = (int)(overflow * FOUNTAIN_OVERFLOW_SHIELD_MULTIPLIER);

            //both flow and void shields use this variable to track power remaining.
            int shieldCurrent = shara.ReadActorData("flowshield_dmgleft");

            var hasShield = shara.myStats.CheckHasStatusName("status_flowshield") ||
                            shara.myStats.CheckHasStatusName("status_voidshield");
            
            //if we have NO shield up, create a new one, but don't change the power's cooldown
            //as that would punish the player for picking up a fountain at near-max health, giving them
            //a weaker shield than if they just cast one.
            if (hasShield)
            {
                shieldCurrent += overflow;

                // Don't increase max value tho. If we overflow, the bar can just sit at 100%.
                /* int shieldMax = shara.ReadActorData("flowshield_dmgmax");
                shieldMax += overflow;
                shara.SetActorData("flowshield_dmgmax", shieldMax); */
            }
            else
            {
                //add the status
                shara.myStats.AddStatusByRef("status_flowshield", shara, 99);
                
                //set the value to BE overflow, instead of the normal full amount
                shieldCurrent = overflow;
                
                //remove the cooldown from the ability
                var aref = shara.myAbilities.GetAbilityByRef("skill_flowshield");
                aref.SetCurCooldownTurns(0);

            }

            // should shield value be capped?

            shara.SetActorData("flowshield_dmgleft", shieldCurrent);
            shara.SetActorData("shieldinfo_dirty", 1);
        }
        
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirRecovery", null, true);
        GameLogScript.LogWriteStringRef("exp_log_healfountain");
        GameMasterScript.gmsSingleton.DestroyActor(fountain);

        // Also heal pets...
         foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;
            Monster m = act as Monster;
            if (!m.myStats.IsAlive() || m.isInDeadQueue) continue;
            m.myStats.ChangeStat(StatTypes.HEALTH, m.myStats.GetMaxStat(StatTypes.HEALTH) * SHARA_PET_HEALING_FROM_FOUNTAIN, StatDataTypes.CUR, true);
        }
    }

    /// <summary>
    /// Prints out Dominated creature healing plus damage popups in a single number to avoid log spam
    /// immediately.
    /// </summary>       
    public static void CheckForDominateHealthLog()
    {
        int dominateHealth = GameMasterScript.heroPCActor.ReadActorData("dominatehealth");
        if (dominateHealth > 0)
        {
            // we only want to show one heal number per turn even if we have lots of dominated creatures
            // lest our log be spammed
            string domHealth = dominateHealth.ToString();
            BattleTextManager.NewText(domHealth, GameMasterScript.heroPCActor.GetObject(), Color.green, -0.25f);
            StringManager.SetTag(0, domHealth);
            GameLogScript.LogWriteStringRef("log_shara_creatureheal", null, TextDensity.VERBOSE);
            GameMasterScript.heroPCActor.SetActorData("dominatehealth", 0);
        }
    }

    /// <summary>
    /// Returns TRUE if we swallow up and cancel the wandering monster check. This function monitors and caps # of wandering monster spawns in Shara mode.
    /// </summary>
    /// <returns></returns>
    public static bool SharaWanderingMonsterCheck(int initialSpawnedMonsters, bool tickCounter = true)
    {
        if (!SharaModeStuff.IsSharaModeActive()) return false;

        string floorCheck = "wandermon" + MapMasterScript.activeMap.floor;

        // Check "wandermon0" to see number of wandering monsters ever spawned on floor 0

        int wanderingMonstersSpawnedEver = GameMasterScript.heroPCActor.ReadActorData(floorCheck);
        int monCap = (int)(initialSpawnedMonsters * 1.5f); // Arbitrary calculation to cap off the number of monsters.

        if (wanderingMonstersSpawnedEver >= monCap)
        {
            if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_exp_sharamode_wanderingmon"))
            {
                Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_exp_sharamode_wanderingmon");
                UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
            }
            return true; // Cancel this spawn if we're at the cap.
        }

        // Otherwise, allow the spawn and tick the counter.

        if (wanderingMonstersSpawnedEver == -1)
        {
            wanderingMonstersSpawnedEver = 0;
        }

        if (tickCounter)
        {
            wanderingMonstersSpawnedEver++;
        }
        
        GameMasterScript.heroPCActor.SetActorData(floorCheck, wanderingMonstersSpawnedEver);

        return false;
    }

    /// <summary>
    /// Returns TRUE if we have calculated that a campfire floor should spawn. If FALSE, and destinationMap is main path, print a log message.
    /// </summary>
    /// <returns></returns>
    public static bool TickCampfireCounterAndCheckForSpawn(MapChangeInfo changeInfo)
    {
        if (!IsSharaModeActive()) return false;
        int floorsToCampfireFloor = GameMasterScript.heroPCActor.ReadActorData("floors_until_campfire");

        //if (Debug.isDebugBuild) Debug.Log("Floors to campfire: " + floorsToCampfireFloor);

        if (floorsToCampfireFloor < 0)
        {
            floorsToCampfireFloor = CAMPFIRE_FLOOR_INTERVAL - 1;
        }

        if (changeInfo.destinationMap.IsMainPath())
        {
            floorsToCampfireFloor--;

            //if (Debug.isDebugBuild) Debug.Log("Ticked down one on main path, floors left: " + floorsToCampfireFloor);

            if (floorsToCampfireFloor <= 0)
            {
                floorsToCampfireFloor = 3;
                GameMasterScript.heroPCActor.SetActorData("floors_until_campfire", floorsToCampfireFloor);
                GameMasterScript.heroPCActor.SetActorData("allow_campfire_cooking", 1);
                return true;
            }
            else
            {
                GameMasterScript.heroPCActor.SetActorData("floors_until_campfire", floorsToCampfireFloor);
                // Print log message noting X floors left.
                StringManager.SetTag(0, floorsToCampfireFloor.ToString());
                GameLogScript.LogWriteStringRef("exp_log_shara_floorstocampfire");

                if (Debug.isDebugBuild) Debug.Log("Set floors left in hero to " + floorsToCampfireFloor);

                return false;
            }
            
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Not main path.");
            return false;
        }
    }

    /// <summary>
    /// Returns TRUE if monster is dominated by Shara. Should be used if a friendly monster dies and we want credit for it.
    /// </summary>
    /// <param name="mon"></param>
    /// <returns></returns>
    public static bool CheckIfMonsterIsDominated(Monster mon)
    {
        if (!SharaModeStuff.IsSharaModeActive()) return false;
        if (mon.actorfaction != Faction.PLAYER) return false;
        if (mon.bufferedFaction != Faction.PLAYER) return false;
        if (mon.myStats.CheckHasActiveStatusName("exp_status_dominated")) return true;
        return false;
    }

    public static void RemoveAndHideMapsOrNPCsForShara()
    {
        foreach (Map m in MapMasterScript.dictAllMaps.Values)
        {
            if (m.floor == MapMasterScript.PREBOSS1_MAP_FLOOR || m.floor == MapMasterScript.BEASTLAKE_SIDEAREA ||
                m.floor == MapMasterScript.RESEARCH_ALCOVE_FROZEN || m.floor == MapMasterScript.ROMANCE_SIDEAREA ||
                m.floor == MapMasterScript.CASINO_BASEMENT || m.floor == MapMasterScript.CASINO)
            {
                bool destroy = m.floor != MapMasterScript.PREBOSS1_MAP_FLOOR;
                m.SetMapVisibility(false, forciblyRemove:destroy);
            }
            else
            {
                // All side areas should be visible for Shara.
                m.SetMapVisibility(true);
            }
        }

        Map regenerator = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_SIDEAREA_2);
        List<Actor> toRemove = new List<Actor>();
        foreach(Actor act in regenerator.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            if (act.actorRefName == "obj_regenquestobject")
            {
                toRemove.Add(act);
            }
        }
        foreach(Actor act in toRemove)
        {
            regenerator.RemoveActorFromMap(act);
        }

        Map elementalLair = MapMasterScript.theDungeon.FindFloor(MapMasterScript.ELEMENTAL_LAIR);
        Actor jorito = elementalLair.FindActor("npc_armormaster");
        if (jorito != null) elementalLair.RemoveActorFromMap(jorito);

        Map jellyGrotto = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JELLY_GROTTO);
        Actor langdon = jellyGrotto.FindActor("npc_farmergrotto");
        if (langdon != null) jellyGrotto.RemoveActorFromMap(langdon);

        Actor pinkSlime = jellyGrotto.FindActor("npc_pinkslimepet");
        if (pinkSlime != null) jellyGrotto.RemoveActorFromMap(pinkSlime);

        Map ruinedPassage = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PRE_BOSS3_MEETSHARA_MAP_FLOOR);
        Actor ruinedShara = ruinedPassage.FindActor("npc_shara_preboss3");
        if (ruinedShara != null) ruinedPassage.RemoveActorFromMap(ruinedShara);

        Map frogBog = MapMasterScript.theDungeon.FindFloor(102);
        Actor petTrainer = frogBog.FindActor("npc_pettrainer");
        if (petTrainer != null) frogBog.RemoveActorFromMap(petTrainer);

        Map banditLibrary = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BANDIT_LIBRARY_FLOOR);
        Actor dirtbeakLibrary = banditLibrary.FindActor("mon_dirtbeak_library");
        if (dirtbeakLibrary != null) banditLibrary.RemoveActorFromMap(dirtbeakLibrary);

        MapTileData stairsPos = ruinedPassage.GetTile(new Vector2(7f, 12f));
        if (ruinedPassage.GetStairsInTile(stairsPos.pos) == null)
        {
            // Also create new stairs to boss3 area in ruinedPassage
            Stairs st = new Stairs();
            st.stairsUp = false;
            st.prefab = "RuinedStairsUp";
            st.pointsToFloor = MapMasterScript.BOSS3_MAP_FLOOR;
            st.NewLocation = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR);
            st.newLocationID = st.NewLocation.mapAreaID;

            ruinedPassage.PlaceActor(st, stairsPos);
        }
    }

    public static void WaitThenPlayNormalSharaThemeIfStillInCampfire(float time)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(IWaitThenPlayNormalSharaThemeIfStillInCampfire(time));
    }

    static IEnumerator IWaitThenPlayNormalSharaThemeIfStillInCampfire(float time)
    {
        yield return new WaitForSeconds(time);
        if (MapMasterScript.activeMap.floor == MapMasterScript.CAMPFIRE_FLOOR)
        {
            MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("sharatheme1");
        }
    }

    public static bool IsMonsterFinalBoss(Monster m)
    {
        if (MapMasterScript.activeMap == null) return false;
        if (MapMasterScript.activeMap.floor != MapMasterScript.SHARA_FINALBOSS_FLOOR)
        {
            return false;
        }
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return false;
        }
        if (!m.isBoss)
        {
            return false;
        }
        if (m.actorRefName == "mon_shara_finalboss")
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns TRUE if we are Shara and beat boss1, ready to play cutscene.
    /// </summary>
    /// <returns></returns>
    public static bool CheckForBoss1Clear()
    {
        if (MapMasterScript.activeMap.floor == MapMasterScript.BOSS1_MAP_FLOOR && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            if (ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) != 3)            
            {
                // Shara's boss1 clear condition is when all monsters are defeated.
                if (MapMasterScript.activeMap.unfriendlyMonsterCount < 3)
                {
                    MapMasterScript.activeMap.RecountMonsters(false);
                }
                if (MapMasterScript.activeMap.unfriendlyMonsterCount == 0)
                {
                    GameEventsAndTriggers.SharaBoss1Victory();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// If the player has already met shara when we load the file, unlock Shara Mode
    /// </summary>
    public static void CheckForSharaModeUnlockBasedOnProgress()
    {
        if (Debug.isDebugBuild) Debug.Log("Check shara mode unlock? " + DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)  + " " +  SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE) + " " + ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) + " " + ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.META));

        if (!UIManagerScript.dialogBoxOpen 
            && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
            && !SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE) 
            && (ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.HERO) >= 2
            || ProgressTracker.CheckProgress(TDProgress.BOSS3, ProgressLocations.META) >= 2))
        {
            MetaProgressScript.UnlockSharaMode();
        }
    }

    /// <summary>
    /// If stairs have disappeared in PreBoss1, restore them.
    /// </summary>
    public static void EnsureConnectionsInPreBoss1Area()
    {
        Map preBoss1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PREBOSS1_MAP_FLOOR);
        bool stairsToCedar5 = false;
        bool stairsToDirtbeak = false;
        foreach(Stairs st in preBoss1.mapStairs)
        {
            st.EnableActor();
            if (st.pointsToFloor == 4 || st.NewLocation == MapMasterScript.theDungeon.FindFloor(4)) stairsToCedar5 = true;
            if (st.pointsToFloor == MapMasterScript.BOSS1_MAP_FLOOR || st.NewLocation == MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS1_MAP_FLOOR)) stairsToDirtbeak = true;
        }
    }

    /// <summary>
    /// If for whatever reason there are no stairs from Cedar 5 to pre boss 1, make them.
    /// </summary>
    public static void EnsureConnectionFromCedarToBoss1()
    {
        Map cedar5 = MapMasterScript.theDungeon.FindFloor(4);

        bool stairsFound = false;
        Stairs stairsToBoss = null;

        foreach (Stairs st in cedar5.mapStairs)
        {
            if (st.NewLocation != null && st.NewLocation.floor == MapMasterScript.PREBOSS1_MAP_FLOOR)
            {
                stairsFound = true;
                stairsToBoss = st;
                break;
            }
        }

        if (stairsFound)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("exp_friendlydirtbeak1_talk") >= 1)
            {
                stairsToBoss.EnableActor();
            }
            return;
        }
        

        // Oops, we don't have stairs? Let's fix that.

        Stairs makeStairs = cedar5.SpawnStairs(false, MapMasterScript.PREBOSS1_MAP_FLOOR);
        Debug.Log("Spawned stairs at " + makeStairs.GetPos());
        if (MapMasterScript.activeMap.floor == cedar5.floor)
        {
            MapMasterScript.singletonMMS.SpawnStairs(makeStairs);
        }

        if (GameMasterScript.heroPCActor.ReadActorData("exp_friendlydirtbeak1_talk") != 1)
        {
            makeStairs.DisableActor();
        }
    }

    /// <summary>
    /// Returns TRUE if we have unlocked the Sorceress job.
    /// </summary>
    /// <returns></returns>
    public static bool CheckForSorceressUnlock()
    {
        return SharedBank.CheckIfJobIsUnlocked(CharacterJobs.MIRAISHARA);

        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.SHARA_STORY_CLEARED) == 1 ||
            TDPlayerPrefs.GetInt(GlobalProgressKeys.SORCERESS_UNLOCKED) == 1)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if we CAN unlock the sorceress job.
    /// </summary>
    /// <returns></returns>
    public static bool CheckConditionsForSorceressUnlock()
    {
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.SHARA_STORY_CLEARED) == 1 
            || ProgressTracker.CheckProgress(TDProgress.NGPLUSPLUS_STARTED_ONCE, ProgressLocations.META) == 1
            || ProgressTracker.CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.META) >= 2)
        {
            if (!RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                return true;
            }            
        }
        return false;
    }

    /// <summary>
    /// This assumes we are in boss3 map.
    /// </summary>
    public static void HideStairsInBoss3Map()
    {
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (!st.stairsUp)
            {
                st.DisableActor();
                return;
            }
        }
    }
}
