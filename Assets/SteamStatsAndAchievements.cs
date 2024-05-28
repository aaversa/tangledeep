using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System.ComponentModel;

// Enum for possible game states on the client
public enum EClientGameState
{
    k_EClientGameActive,
    k_EClientGameWinner,
    k_EClientGameLoser,
};

public class SteamStatsAndAchievements : MonoBehaviour
{

    const int MAX_PEDIA_MONSTERS = 62;
    enum Achievement : int
    {
        achievement_totalcharacters_1,
        achievement_jobchanger_3,
        achievement_fillcorral,
        achievement_killchampionslocal_10,
        achievement_flaskuses_many,
        achievement_merchant_buyer,
        achievement_charlevel10,
        achievement_itemdelver,
        achievement_recipefailer,
        achievement_survivor,
        achievement_nightmarecomplete,
        achievement_job_mastery,
        achievement_weaponmasteries,
        achievement_coolfrog_capture,
        achievement_boss1,
        achievement_boss2,
        achievement_boss3,
        achievement_boss4,
        achievement_boss4_ng,
        achievement_onepunch,
        achievement_founddimrift,
        achievement_beatdimriftboss,
        achievement_charlevel15,
        achievement_firstgearset,
        achievement_dirtbeakthrone,
        achievement_5itemmods,
        achievement_firstlegendary,
        achievement_recipemaster,
        achievement_petletter,
        achievement_firstmonsterhatched,
        achievement_monsterpedia,
        achievement_dailyfloor10,
        achievement_weeklyfloor10,
        achievement_boss4weekly,
        achievement_boss4daily,
        achievement_dlc1_sharaboss1,
        achievement_dlc1_sharaboss2,
        achievement_dlc1_sharaboss3,
        achievement_dlc1_sharaboss4,
        achievement_dlc1_charlevel20,
        achievement_dlc1_spiritstag,
        achievement_dlc1_towerordeals,
        achievement_dlc1_anyjourney,
        achievement_dlc1_runelearner,
        achievement_dlc1_calligrapher_allskills,
        achievement_dlc2_ultimatewhip,
        achievement_dlc2_beat_frogdragon,
        achievement_dlc2_beat_spiritdragon,
        achievement_dlc2_beat_jellydragon,
        achievement_dlc2_beat_beastdragon,
        achievement_dlc2_beat_banditdragon,
        achievement_dlc2_beat_alldragons,
        achievement_dlc2_frogcrafting,
        achievement_dlc2_beat_alldragons_ngplus,
        achievement_dlc2_beat_alldragons_ngplusplus,
    }

    private Achievement_t[] m_Achievements = new Achievement_t[] {
        new Achievement_t(Achievement.achievement_totalcharacters_1, "One More Run...", "Create at least 20 characters."),
        new Achievement_t(Achievement.achievement_jobchanger_3, "Jill of All Trades", "Switch jobs three times for a single character."),
        new Achievement_t(Achievement.achievement_fillcorral, "Monster Whisperer", "Fill the Monster Corral with monsters."),
        new Achievement_t(Achievement.achievement_killchampionslocal_10, "Big Game Hunter", "Defeat 10 Champions in a single run."),
        new Achievement_t(Achievement.achievement_flaskuses_many, "Always Thirsty", "Drink from your flask 250 times."),
        new Achievement_t(Achievement.achievement_merchant_buyer, "Capricious Capitalist", "Spend 10,000 gold in shops."),
        new Achievement_t(Achievement.achievement_charlevel10, "A Nice Round Number", "Reach experience level 10."),
        new Achievement_t(Achievement.achievement_itemdelver, "The Dream Warrior", "Clear 10 Item Dreams."),
        new Achievement_t(Achievement.achievement_recipefailer, "I Don't Need Recipes", "Create 10 Tangledeep Curries. Delicious. (Not really.)"),
        new Achievement_t(Achievement.achievement_survivor, "I'm A Survivor", "You didn't give up at 1 HP!"),
        new Achievement_t(Achievement.achievement_nightmarecomplete , "Vanquisher of Nightmares", "Clear an Item Nightmare and defeat its boss.!"),
        new Achievement_t(Achievement.achievement_job_mastery , "One Class Lass", "Learn every skill of a job."),
        new Achievement_t(Achievement.achievement_weaponmasteries  , "Ace of Armaments", "Learn all masteries for a single weapon type."),
        new Achievement_t(Achievement.achievement_coolfrog_capture  , "Caught a Coolfrog", "Find and knock out a Coolfrog with a Monster Mallet."),
        new Achievement_t(Achievement.achievement_boss1  , "Duke Dirtbeak Defeated", "Beat up Duke Dirtbeak and his Bird Boys."),
        new Achievement_t(Achievement.achievement_boss2  , "Shadow Slayer", "Defeat the twin Demon Spirits."),
        new Achievement_t(Achievement.achievement_boss3  , "A New Kind of Beast", "Defeat the third boss."),
        new Achievement_t(Achievement.achievement_boss4  , "Ascension", "Defeat the final boss."),
        new Achievement_t(Achievement.achievement_boss4_ng  , "Ascension Plus", "Defeat the final boss in New Game+ mode."),
        new Achievement_t(Achievement.achievement_onepunch   , "One Punch Woman", "One-shot a monster using only your fists."),
        new Achievement_t(Achievement.achievement_founddimrift   , "Distant Dream Dimension", "Discovered a Dream of Dungeons Distant."),
        new Achievement_t(Achievement.achievement_beatdimriftboss   , "Distant Dream Destroyer", "Defeated the boss of a Dream of Dungeons Distant."),
        new Achievement_t(Achievement.achievement_charlevel15   , "Max Power!", "Reach experience level 15."),
        new Achievement_t(Achievement.achievement_firstgearset   , "Gear Set Match", "Complete a Gear Set."),
        new Achievement_t(Achievement.achievement_dirtbeakthrone   , "This Is Mine Now", "Sit on Duke Dirtbeak's throne."),
        new Achievement_t(Achievement.achievement_5itemmods   , "Enchantress", "Max out the number of magic properties on an item."),
        new Achievement_t(Achievement.achievement_firstlegendary   , "Legendary Locator", "Find your first legendary item!"),
        new Achievement_t(Achievement.achievement_recipemaster   , "The Iron Chef", "Learn every recipe in the game."),
        new Achievement_t(Achievement.achievement_petletter   , "Corral Caretaker", "Receive a letter from a happily released pet."),
        new Achievement_t(Achievement.achievement_firstmonsterhatched   , "Where Did That Come From?", "Hatch a monster from the corral."),
        new Achievement_t(Achievement.achievement_monsterpedia   , "Monsterpediologist", "Add every monster in the game to your Monsterpedia."),
        new Achievement_t(Achievement.achievement_weeklyfloor10   , "Halfway There (Weekly)", "Reach the 11th floor of Tangledeep in a Weekly Challenge."),
        new Achievement_t(Achievement.achievement_dailyfloor10   , "Halfway There (Daily)", "Reach the 11th floor of Tangledeep in a Daily Challenge."),
        new Achievement_t(Achievement.achievement_boss4weekly   , "Ascension (Weekly)", "Defeat the final boss in a Weekly Challenge."),
        new Achievement_t(Achievement.achievement_boss4daily   , "Ascension (Daily)", "Defeat the final boss in a Daily Challenge."),
        // DLC1 achievements
        new Achievement_t(Achievement.achievement_dlc1_sharaboss1   , "Birds of a Feather", "Help Dirtbeak in Shara's story."),
        new Achievement_t(Achievement.achievement_dlc1_sharaboss2   , "Boss Lady", "Defeat the second boss in Shara's story."),
        new Achievement_t(Achievement.achievement_dlc1_sharaboss3   , "Clarified Purpose", "Defeat the third boss in Shara's story."),
        new Achievement_t(Achievement.achievement_dlc1_sharaboss4   , "Complete the Cycle", "Finish Shara's story."),
        new Achievement_t(Achievement.achievement_dlc1_charlevel20   , "Potential Unleashed", "Reach experience level 20."),
        new Achievement_t(Achievement.achievement_dlc1_spiritstag   , "Master Tamer", "Capture a **Wild Untamed** Eidolon."),
        new Achievement_t(Achievement.achievement_dlc1_towerordeals   , "The Ultimate Challenge!", "Conquer the Tower of Ordeals."),
        new Achievement_t(Achievement.achievement_dlc1_anyjourney   , "Journeywoman", "Complete any Wanderer's Journey."),
        new Achievement_t(Achievement.achievement_dlc1_calligrapher_allskills   , "Quill and Sword", "Master all Calligrapher skills."),
        new Achievement_t(Achievement.achievement_dlc1_runelearner   , "A Lady and a Scholar", "Learn ten skills from Runes of Knowledge."),
        // DLC2 achievements
        new Achievement_t(Achievement.achievement_dlc2_ultimatewhip   , "Whip It Good", "Learn the ultimate Whip mastery."),
        new Achievement_t(Achievement.achievement_dlc2_beat_frogdragon   , "Frog Dragon Conquered", "Defeat the legendary Frog Dragon."),
        new Achievement_t(Achievement.achievement_dlc2_beat_spiritdragon   , "Spirit Dragon Conquered", "Defeat the legendary Echo Dragon."),
        new Achievement_t(Achievement.achievement_dlc2_beat_jellydragon   , "Jelly Dragon Conquered", "Defeat the legendary Jelly Dragon."),
        new Achievement_t(Achievement.achievement_dlc2_beat_beastdragon   , "Tyrant Dragon Conquered", "Defeat the legendary Tyrant Dragon."),
        new Achievement_t(Achievement.achievement_dlc2_beat_banditdragon   , "Bandit Dragon Conquered", "Defeat the legendary Bandit Dragon."),
        new Achievement_t(Achievement.achievement_dlc2_frogcrafting   , "Fantastic Frog Friend", "Use Frogcrafting 10 times."),
        new Achievement_t(Achievement.achievement_dlc2_beat_alldragons   , "Dragon Dawn Defeated", "Defeat all six dragons!"),
        new Achievement_t(Achievement.achievement_dlc2_beat_alldragons_ngplus   , "Dragon Dawn Defeated+", "Defeat all six dragons on New Game+!"),
        new Achievement_t(Achievement.achievement_dlc2_beat_alldragons_ngplusplus   , "Dragon Dawn Defeated++", "Defeat all six dragons in Savage World!")
    };

    protected Callback<UserStatsReceived_t> m_UserStatsReceived;
    protected Callback<UserStatsStored_t> m_UserStatsStored;
    protected Callback<UserAchievementStored_t> m_UserAchievementStored;

    // Our GameID
    private CGameID m_GameID;

    // Did we get the stats from Steam?
    private bool m_bRequestedStats;
    private bool m_bStatsValid;

    // Should we store stats this frame?
    private bool m_bStoreStats;


    private int stat_maxlevel;
    private int stat_highestfloor;
    private int stat_numcharacters;
    private int stat_jobchangeslocal;
    private int stat_monstersincorral;
    private int stat_championskilledlocal;
    private int stat_flaskuses;
    private int stat_itemworldscleared;
    private int stat_recipefailer;
    private int stat_survive1hp;
    private int stat_merchantgoldspent;
    int stat_nightmarescomplete;
    int stat_singlejobmastered;
    int stat_weaponmasterytiers;
    int stat_coolfrogcapture;
    int stat_boss1defeated;
    int stat_boss2defeated;
    int stat_boss3defeated;
    public int stat_boss4defeated;
    int stat_boss4defeated_ng;
    int stat_onepunch;
    int stat_dimriftentered;
    int stat_dimriftbossdefeated;

    int stat_sitonthrone;
    int stat_maxitemmods;
    int stat_legendariesfound;
    int stat_recipeslearned;
    int stat_monstersknown;
    int stat_petletters;
    int stat_monstersbred;
    int stat_gearsets;

    int stat_weeklyhighestfloor;
    int stat_dailyhighestfloor;
    int stat_boss4weekly;
    int stat_boss4daily;

    int stat_dlc1_towerordeals_victory;
    int stat_dlc1_wandererjourneys_complete;
    int stat_dlc1_spiritstag_capture;
    int stat_dlc1_shara_boss1defeated;
    int stat_dlc1_shara_boss2defeated;
    int stat_dlc1_shara_boss3defeated;
    int stat_dlc1_shara_boss4defeated;
    int stat_dlc1_runeslearned;
    int stat_dlc1_calligraphermastered;

    int stat_dlc2_ultimatewhip_learned;
    int stat_dlc2_frogdragon_defeated;
    int stat_dlc2_spiritdragon_defeated;
    int stat_dlc2_jellydragon_defeated;
    int stat_dlc2_beastdragon_defeated;
    int stat_dlc2_banditdragon_defeated;
    int stat_dlc2_robotdragon_defeated;
    int stat_dlc2_robotdragon_defeated_ngp;
    int stat_dlc2_robotdragon_defeated_savage;
    int stat_dlc2_frogcrafting;


    void OnEnable()
    {
#if UNITY_PS4
        //on PS4 for trophies to work PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS must be false
        PS4_LoadStats();
        //in PS4 we need this to be true
        m_bStatsValid = true;
        return;
#endif

        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;

        if (!SteamManager.Initialized)
            return;

        // Cache the GameID for use in the Callbacks
        m_GameID = new CGameID(SteamUtils.GetAppID());

        m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
        m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

        // These need to be reset to get the stats upon an Assembly reload in the Editor.
        m_bRequestedStats = false;
        m_bStatsValid = false;
    }

    // Manually fix achievements that may have been borked before, and which the player cannot otherwise fix through normal gameplay
    public static void VerifyPlayerAchievementsOnLoad()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("finalboss1") == 2)
        {
            if (MetaProgressScript.GetMonstersDefeated("mon_finalbossai") == 0)
            {
                MetaProgressScript.TryAddMonsterFought("mon_finalbossai", 1);
            }
        }

        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;

        foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == null) continue;
            if (tcm.monsterObject.actorRefName == "mon_xp_spiritstag") // && tcm.monsterObject.ReadActorData("tcmrarityup") == 1)
            {
                GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_SpiritStagCapture();
            }
        }

        if (GameMasterScript.heroPCActor.CheckIfMapClearedByFloor(MapMasterScript.SPECIALFLOOR_DIMENSIONAL_RIFT))
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.FoundDimRift();
            GameMasterScript.gmsSingleton.statsAndAchievements.BeatDimRift();
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: We have stats data from Steam. It is authoritative, so update
    //			our data with those results now.
    //-----------------------------------------------------------------------------
    private void OnUserStatsReceived(UserStatsReceived_t pCallback)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        if (!SteamManager.Initialized)
            return;

        // we may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (EResult.k_EResultOK == pCallback.m_eResult)
            {
                if (Debug.isDebugBuild) Debug.Log("Received stats and achievements from Steam\n");

                m_bStatsValid = true;

                // load achievements
                foreach (Achievement_t ach in m_Achievements)
                {
                    bool ret = SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
                    if (ret)
                    {
                        ach.m_strName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
                        ach.m_strDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
                    }
                    else {
                        //Debug.Log("SteamUserStats.GetAchievement failed for Achievement " + ach.m_eAchievementID + "\nIs it registered in the Steam Partner site?");
                    }
                }

                // load stats
                SteamUserStats.GetStat("stat_maxlevel", out stat_maxlevel);
                SteamUserStats.GetStat("stat_highestfloor", out stat_highestfloor);
                SteamUserStats.GetStat("stat_numcharacters", out stat_numcharacters);
                SteamUserStats.GetStat("stat_jobchangeslocal", out stat_jobchangeslocal);
                SteamUserStats.GetStat("stat_monstersincorral", out stat_monstersincorral);
                SteamUserStats.GetStat("stat_championskilledlocal", out stat_championskilledlocal);
                SteamUserStats.GetStat("stat_flaskuses", out stat_flaskuses);
                SteamUserStats.GetStat("stat_merchantgoldspent", out stat_merchantgoldspent);
                SteamUserStats.GetStat("stat_itemworldscleared", out stat_itemworldscleared);
                SteamUserStats.GetStat("stat_recipefailer", out stat_recipefailer);
                SteamUserStats.GetStat("stat_survive1hp", out stat_survive1hp);

                SteamUserStats.GetStat("stat_nightmarescomplete", out stat_nightmarescomplete);
                SteamUserStats.GetStat("stat_singlejobmastered", out stat_singlejobmastered);
                SteamUserStats.GetStat("stat_weaponmasterytiers", out stat_weaponmasterytiers);
                SteamUserStats.GetStat("stat_coolfrogcapture", out stat_coolfrogcapture);
                SteamUserStats.GetStat("stat_boss1defeated", out stat_boss1defeated);
                SteamUserStats.GetStat("stat_boss2defeated", out stat_boss2defeated);
                SteamUserStats.GetStat("stat_boss3defeated", out stat_boss3defeated);
                SteamUserStats.GetStat("stat_boss4defeated", out stat_boss4defeated);
                SteamUserStats.GetStat("stat_boss4defeated_ng", out stat_boss4defeated_ng);
                SteamUserStats.GetStat("stat_onepunch", out stat_onepunch);

                SteamUserStats.GetStat("stat_dimriftentered	", out stat_dimriftentered);
                SteamUserStats.GetStat("stat_dimriftbossdefeated", out stat_dimriftbossdefeated);

                SteamUserStats.GetStat("stat_sitonthrone", out stat_sitonthrone);
                SteamUserStats.GetStat("stat_maxitemmods", out stat_maxitemmods);
                SteamUserStats.GetStat("stat_legendariesfound", out stat_legendariesfound);
                SteamUserStats.GetStat("stat_recipeslearned", out stat_recipeslearned);
                SteamUserStats.GetStat("stat_monstersknown", out stat_monstersknown);
                SteamUserStats.GetStat("stat_petletters", out stat_petletters);
                SteamUserStats.GetStat("stat_monstersbred", out stat_monstersbred);
                SteamUserStats.GetStat("stat_gearsets", out stat_gearsets);

                SteamUserStats.GetStat("stat_boss4weekly", out stat_boss4weekly);
                SteamUserStats.GetStat("stat_boss4daily", out stat_boss4daily);
                SteamUserStats.GetStat("stat_weeklyhighestfloor", out stat_weeklyhighestfloor);
                SteamUserStats.GetStat("stat_dailyhighestfloor", out stat_dailyhighestfloor);

                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                {
                    SteamUserStats.GetStat("stat_dlc1_towerordeals_victory", out stat_dlc1_towerordeals_victory);
                    SteamUserStats.GetStat("stat_dlc1_shara_boss1defeated", out stat_dlc1_shara_boss1defeated);
                    SteamUserStats.GetStat("stat_dlc1_shara_boss2defeated", out stat_dlc1_shara_boss2defeated);
                    SteamUserStats.GetStat("stat_dlc1_shara_boss3defeated", out stat_dlc1_shara_boss3defeated);
                    SteamUserStats.GetStat("stat_dlc1_shara_boss4defeated", out stat_dlc1_shara_boss4defeated);
                    SteamUserStats.GetStat("stat_dlc1_spiritstag_capture", out stat_dlc1_spiritstag_capture);
                    SteamUserStats.GetStat("stat_dlc1_wandererjourneys_complete", out stat_dlc1_wandererjourneys_complete);
                    SteamUserStats.GetStat("stat_dlc1_runeslearned", out stat_dlc1_runeslearned);
                    SteamUserStats.GetStat("stat_dlc1_calligraphermastered", out stat_dlc1_calligraphermastered);
                }

                if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
                {
                    SteamUserStats.GetStat("stat_dlc2_ultimatewhip_learned", out stat_dlc2_ultimatewhip_learned);
                    SteamUserStats.GetStat("stat_dlc2_frogdragon_defeated", out stat_dlc2_frogdragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_spiritdragon_defeated", out stat_dlc2_spiritdragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_jellydragon_defeated", out stat_dlc2_jellydragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_beastdragon_defeated", out stat_dlc2_beastdragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_banditdragon_defeated", out stat_dlc2_banditdragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_robotdragon_defeated", out stat_dlc2_robotdragon_defeated);
                    SteamUserStats.GetStat("stat_dlc2_robotdragon_defeated_ngp", out stat_dlc2_robotdragon_defeated_ngp);
                    SteamUserStats.GetStat("stat_dlc2_robotdragon_defeated_savage", out stat_dlc2_robotdragon_defeated_savage);
                    SteamUserStats.GetStat("stat_dlc2_frogcrafting", out stat_dlc2_frogcrafting);
                }

            }
            else {
                Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
            }
        }
    }

    private void Update()
    {
#if UNITY_PS4
        UpdatePS4StatsAndTrophies();
        return;
#endif

        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;

        UpdateSteamStatsAndAchievements();
    }


    //-----------------------------------------------------------------------------
    // Purpose: Our stats data was stored!
    //-----------------------------------------------------------------------------
    private void OnUserStatsStored(UserStatsStored_t pCallback)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        // we may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (EResult.k_EResultOK == pCallback.m_eResult)
            {
                //Debug.Log("StoreStats - success");
            }
            else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
            {
                // One or more stats we set broke a constraint. They've been reverted,
                // and we should re-iterate the values now to keep in sync.
                //Debug.Log("StoreStats - some failed to validate");
                // Fake up a callback here so that we re-load the values.
                UserStatsReceived_t callback = new UserStatsReceived_t();
                callback.m_eResult = EResult.k_EResultOK;
                callback.m_nGameID = (ulong)m_GameID;
                OnUserStatsReceived(callback);
            }
            else {
                //Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
            }
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: An achievement was stored
    //-----------------------------------------------------------------------------
    private void OnAchievementStored(UserAchievementStored_t pCallback)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        // We may get callbacks for other games' stats arriving, ignore them
        if ((ulong)m_GameID == pCallback.m_nGameID)
        {
            if (0 == pCallback.m_nMaxProgress)
            {
                Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
            }
            else {
                Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
            }
        }
    }

    private class Achievement_t
    {
        public Achievement m_eAchievementID;
        public string m_strName;
        public string m_strDescription;
        public bool m_bAchieved;

        /// <summary>
        /// Creates an Achievement. You must also mirror the data provided here in https://partner.steamgames.com/apps/achievements/yourappid
        /// </summary>
        /// <param name="achievement">The "API Name Progress Stat" used to uniquely identify the achievement.</param>
        /// <param name="name">The "Display Name" that will be shown to players in game and on the Steam Community.</param>
        /// <param name="desc">The "Description" that will be shown to players in game and on the Steam Community.</param>
        public Achievement_t(Achievement achievementID, string name, string desc)
        {
            m_eAchievementID = achievementID;
            m_strName = name;
            m_strDescription = desc;
            m_bAchieved = false;
        }
    }

    //-----------------------------------------------------------------------------
    // Purpose: Display the user's stats and achievements
    //-----------------------------------------------------------------------------
    public void Render()
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        if (!SteamManager.Initialized)
        {
            GUILayout.Label("Steamworks not Initialized");
            return;
        }

        GUILayout.Label("Max Level: " + stat_maxlevel);
        GUILayout.Label("Highest Floor Reached: " + stat_highestfloor);
        GUILayout.Label("Total Characters: " + stat_numcharacters);
        GUILayout.Label("Job Changes (This Run): " + stat_jobchangeslocal);
        GUILayout.Label("Monsters in Corral: " + stat_monstersincorral);
        GUILayout.Label("Champions Defeated (This Run): " + stat_championskilledlocal);

        GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, 800));
        foreach (Achievement_t ach in m_Achievements)
        {
            GUILayout.Label(ach.m_eAchievementID.ToString());
            GUILayout.Label(ach.m_strName + " - " + ach.m_strDescription);
            GUILayout.Label("Achieved: " + ach.m_bAchieved);
            GUILayout.Space(20);
        }

        // FOR TESTING PURPOSES ONLY!
        if (GUILayout.Button("RESET STATS AND ACHIEVEMENTS"))
        {
            SteamUserStats.ResetAllStats(true);
            SteamUserStats.RequestCurrentStats();
            OnGameStateChange(EClientGameState.k_EClientGameActive);
        }
        GUILayout.EndArea();
    }

    //-----------------------------------------------------------------------------
    // Purpose: Game state has changed
    //-----------------------------------------------------------------------------
    public void OnGameStateChange(EClientGameState eNewState)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        if (!m_bStatsValid)
            return;

        if (eNewState == EClientGameState.k_EClientGameActive)
        {
            // Reset per-game stats
            // Nothing here yet
        }
        else if (eNewState == EClientGameState.k_EClientGameWinner || eNewState == EClientGameState.k_EClientGameLoser)
        {
            // We want to update stats the next frame.
            m_bStoreStats = true;
        }
    }

    public void SetLowestFloorExplored(int floor)
    {
        if (!m_bStatsValid)
        {
            #if UNITY_EDITOR
                Debug.Log("Stats not valid.");
            #endif

            return;
        }
            
        bool forceShow = false;

#if UNITY_EDITOR
    //floor = stat_highestfloor + 1;
    forceShow = false;
#endif


        if (floor > stat_highestfloor)
        {
            stat_highestfloor = floor;
            m_bStoreStats = true;
        }

        if (Debug.isDebugBuild) Debug.Log(GameStartData.challengeType + " Day: " + GameStartData.challengeDay + " Current day: " + ChallengesAndLeaderboards.GetCurrentChallengeDay());

        if ((GameStartData.challengeType == ChallengeTypes.DAILY &&  
            GameStartData.challengeDay == ChallengesAndLeaderboards.GetCurrentChallengeDay()) || forceShow)
        {
            ChallengesAndLeaderboards.UploadScoreToLeaderboard(ChallengesAndLeaderboards.GetDailyChallengeLeaderboard(), floor);
            if (floor > stat_dailyhighestfloor)
            {
                stat_dailyhighestfloor = floor;
                m_bStoreStats = true;
            }
        }
        if ((GameStartData.challengeType == ChallengeTypes.WEEKLY && 
            GameStartData.challengeWeek == ChallengesAndLeaderboards.GetCurrentChallengeWeek()) || forceShow)
        {
            ChallengesAndLeaderboards.UploadScoreToLeaderboard(ChallengesAndLeaderboards.GetWeeklyChallengeLeaderboard(), floor);
            if (floor > stat_weeklyhighestfloor)
            {
                stat_weeklyhighestfloor = floor;
                m_bStoreStats = true;
            }
        }

    }

    public void SetHighestCharacterLevel(int level)
    {
        if (!m_bStatsValid)
            return;

        if (level <= stat_maxlevel) return;

        stat_maxlevel = level;
        m_bStoreStats = true;
    }

    public void IncrementItemWorldsCleared(int level)
    {
        if (!m_bStatsValid)
            return;

        stat_itemworldscleared++;
        m_bStoreStats = true;
    }

    public void SetTotalCharacters(int numCharacters)
    {
        if (!m_bStatsValid)
            return;

        if (numCharacters <= stat_numcharacters) return;

        stat_numcharacters = numCharacters;
        m_bStoreStats = true;
    }

    public void SetLocalJobChanges(int changes)
    {
        if (!m_bStatsValid)
            return;

        stat_jobchangeslocal = changes;
        m_bStoreStats = true;
    }

    public void SetMonstersInCorral(int number)
    {
        if (!m_bStatsValid)
            return;

        //Debug.Log("There are " + number + " monsters in the corral.");

        stat_monstersincorral = number;
        m_bStoreStats = true;
    }

    public void IncrementFlaskUses()
    {
        if (!m_bStatsValid)
            return;

        stat_flaskuses++;
        m_bStoreStats = true;
    }

    public void AddMerchantGoldSpent(int amount)
    {
        if (!m_bStatsValid)
            return;

        stat_merchantgoldspent += amount;
        m_bStoreStats = true;
    }

    public void Boss1Defeated()
    {
        if (!m_bStatsValid)
            return;

        stat_boss1defeated = 1;
        m_bStoreStats = true;
    }

    public void MonsterPunchedOut()
    {
        if (!m_bStatsValid)
            return;

        stat_onepunch = 1;
        m_bStoreStats = true;
    }

    public void ItemNightmareCleared()
    {
        if (!m_bStatsValid)
            return;

        stat_nightmarescomplete = 1;
        m_bStoreStats = true;
    }

    public void CoolfrogCaptured()
    {
        if (!m_bStatsValid)
            return;

        stat_coolfrogcapture = 1;
        m_bStoreStats = true;
    }

    public void SetHighestWeaponMasteryTier(int amount)
    {
        if (!m_bStatsValid)
            return;

        if (amount < stat_weaponmasterytiers)
        {
            return;
        }

        stat_weaponmasterytiers = amount;
        m_bStoreStats = true;
    }

    public void JobFullyMastered()
    {
        if (!m_bStatsValid)
            return;

        stat_singlejobmastered = 1;
        m_bStoreStats = true;
    }

    public void Boss2Defeated()
    {
        if (!m_bStatsValid)
            return;

        stat_boss2defeated = 1;
        m_bStoreStats = true;
    }

    public void Boss3Defeated()
    {
        if (!m_bStatsValid)
            return;

        stat_boss3defeated = 1;
        m_bStoreStats = true;
    }

    public void Boss4Defeated()
    {
        if (!m_bStatsValid)
            return;

        stat_boss4defeated = 1;
        if (GameStartData.challengeType == ChallengeTypes.DAILY)
        {
            stat_boss4daily = 1;
        }
        if (GameStartData.challengeType == ChallengeTypes.WEEKLY)
        {
            stat_boss4weekly = 1;
        }
        m_bStoreStats = true;
    }

    public void Boss4Defeated_NG()
    {
        if (!m_bStatsValid)
            return;

        stat_boss4defeated_ng = 1;
        m_bStoreStats = true;
    }

    public void SetChampionsDefeatedLocal(int number)
    {
        if (!m_bStatsValid)
            return;

        stat_championskilledlocal = number;
        m_bStoreStats = true;
    }

    public void IncrementChampionsDefeated()
    {
        if (!m_bStatsValid)
            return;

        stat_championskilledlocal++;
        m_bStoreStats = true;
    }

    public void IncrementLocalJobChanges()
    {
        if (!m_bStatsValid) return;

        stat_jobchangeslocal++;
        m_bStoreStats = true;
    }

    public void IncrementRecipesFailed()
    {
        if (!m_bStatsValid) return;

        stat_recipefailer++;
        m_bStoreStats = true;
    }

    public void Survive1HP()
    {
        if (!m_bStatsValid) return;

        stat_survive1hp = 1;
        m_bStoreStats = true;
    }

    public void FoundDimRift()
    {
        if (!m_bStatsValid) return;

        stat_dimriftentered = 1;
        m_bStoreStats = true;
    }

    public void BeatDimRift()
    {
        if (!m_bStatsValid) return;

        stat_dimriftentered = 1;
        stat_dimriftbossdefeated = 1;
        m_bStoreStats = true;
    }

    public void SetRecipesKnown(int amount)
    {
        if (!m_bStatsValid) return;

        if (amount >= stat_recipeslearned)
        {
            stat_recipeslearned = amount;
        }
        m_bStoreStats = true;
    }

    public void SatOnDirtbeakThrone()
    {
        if (!m_bStatsValid) return;

        stat_sitonthrone = 1;
        m_bStoreStats = true;
    }

    public void IncrementMonstersHatched()
    {
        if (!m_bStatsValid) return;

        stat_monstersbred++;
        m_bStoreStats = true;
    }

    public void CompletedGearSet()
    {
        if (!m_bStatsValid) return;

        stat_gearsets = 1;
        m_bStoreStats = true;
    }

    public void IncrementLegendariesFound()
    {
        if (!m_bStatsValid) return;

        stat_legendariesfound++;
        m_bStoreStats = true;
    }

    public void SetMaxItemModsFound(int amount)
    {
        if (!m_bStatsValid) return;

        if (amount >= stat_maxitemmods)
        {
            stat_maxitemmods = amount;
        }
        m_bStoreStats = true;
    }

    public void SetMonstersKnown(int amount)
    {
        if (!m_bStatsValid) return;

        if (amount >= stat_monstersknown)
        {
            stat_monstersknown = amount;
        }        
        m_bStoreStats = true;
    }

    public void IncrementMonsterLettersRead()
    {
        if (!m_bStatsValid) return;

        stat_petletters++;
        m_bStoreStats = true;
    }

    public void DLC1_Shara_Boss1Defeated()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_shara_boss1defeated = 1;
        m_bStoreStats = true;
    }

    public void DLC1_Shara_Boss2Defeated()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_shara_boss2defeated = 1;
        m_bStoreStats = true;
    }

    public void DLC1_Shara_Boss3Defeated()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_shara_boss3defeated = 1;
        m_bStoreStats = true;
    }

    public void DLC1_Shara_Boss4Defeated()
    {
        TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_STORY_CLEARED, 1);
        if (!m_bStatsValid) return;

        stat_dlc1_shara_boss4defeated = 1;
        m_bStoreStats = true;
    }

    public void DLC1_MysteryDungeon_Complete()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_wandererjourneys_complete++;
        m_bStoreStats = true;
    }

    public void DLC1_RuneLearned()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_runeslearned++;
        m_bStoreStats = true;
    }

    public void DLC1_Ordeals_Complete()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_towerordeals_victory++;
        m_bStoreStats = true;
    }

    public void DLC1_SpiritStagCapture()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_spiritstag_capture++;
        m_bStoreStats = true;
    }

    public void DLC1_Calligrapher_Mastered()
    {
        if (!m_bStatsValid) return;

        stat_dlc1_calligraphermastered++;
        m_bStoreStats = true;
    }

    public void DLC2_UltimateWhip_Learned()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_ultimatewhip_learned++;
        m_bStoreStats = true;
    }

    public void DLC2_Frogcrafting_Used()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_frogcrafting++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Frog_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_frogdragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Beast_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_beastdragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Spirit_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_spiritdragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Bandit_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_banditdragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Jelly_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_jellydragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Robot_Dragon()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_robotdragon_defeated++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Robot_Dragon_NGPlus()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_robotdragon_defeated_ngp++;
        m_bStoreStats = true;
    }

    public void DLC2_Beat_Robot_Dragon_Savage()
    {
        if (!m_bStatsValid) return;

        stat_dlc2_robotdragon_defeated_savage++;
        m_bStoreStats = true;
    }

    //-----------------------------------------------------------------------------
    // Purpose: Unlock this achievement
    //-----------------------------------------------------------------------------
    private void UnlockAchievement(Achievement_t achievement)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        achievement.m_bAchieved = true;

        // the icon may change once it's unlocked
        //achievement.m_iIconImage = 0;

        // mark it down
        SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());

        // Store stats end of frame
        m_bStoreStats = true;
    }

    public void UpdateSteamStatsAndAchievements(bool debug = false)
    {
        if (debug) Debug.Log("Requesting stats/achievements update.");

        if (!SteamManager.Initialized)
        {
            if (debug) Debug.Log("Steam manager is not initialized.");
            return;
        }

        if (!m_bRequestedStats)
        {
            // Is Steam Loaded? if no, can't get stats, done
            if (!SteamManager.Initialized)
            {
                m_bRequestedStats = true;
                if (debug) Debug.Log("Steam manager is not initialized.");
                return;
            }

            // If yes, request our stats
            bool bSuccess = SteamUserStats.RequestCurrentStats();

            // This function should only return false if we weren't logged in, and we already checked that.
            // But handle it being false again anyway, just ask again later.
            m_bRequestedStats = bSuccess;
        }
        
        if (!m_bStatsValid)
        {
            if (debug) Debug.Log("Stats are not valid.");
            return;
        }
            

        // just let people earn achievements dummo
        /* foreach(GameModifierData gmd in GameStartData.gameModifierDataList)
        {
            if (GameStartData.gameModifiers[(int)gmd.mod] && !gmd.enableAchievements)
            {
                return;
            }
        } */

        // Get info from sources

        // Evaluate achievements
        foreach (Achievement_t achievement in m_Achievements)
        {
            if (achievement.m_bAchieved)
            {
                if (debug) Debug.Log(achievement.m_strName + " already obtained");
                continue;
            }
                
            switch (achievement.m_eAchievementID)
            {
                case Achievement.achievement_nightmarecomplete:
                    if (debug) Debug.Log(achievement.m_strName + " nightmare stat: " + stat_nightmarescomplete);
                    if (stat_nightmarescomplete >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_job_mastery:
                    if (debug) Debug.Log(achievement.m_strName + " single job mastered: " + stat_singlejobmastered);
                    if (stat_singlejobmastered >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_weaponmasteries:
                    if (debug) Debug.Log(achievement.m_strName + " weapon mastery tiers: " + stat_weaponmasterytiers);
                    if (stat_weaponmasterytiers >= 4)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_coolfrog_capture:
                    if (debug) Debug.Log(achievement.m_strName + " coolfrogs captured: " + stat_coolfrogcapture);
                    if (stat_coolfrogcapture >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_onepunch:
                    if (debug) Debug.Log(achievement.m_strName + " one punch: " + stat_onepunch);
                    if (stat_onepunch >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_totalcharacters_1:
                    if (debug) Debug.Log(achievement.m_strName + " num characters: " + stat_numcharacters);
                    if (stat_numcharacters >= 20)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss1:
                    if (debug) Debug.Log(achievement.m_strName + " boss1: " + stat_boss1defeated);
                    if (stat_boss1defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss4:
                    if (debug) Debug.Log(achievement.m_strName + " boss4: " + stat_boss4defeated);
                    if (stat_boss4defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss2:
                    if (debug) Debug.Log(achievement.m_strName + " boss2: " + stat_boss2defeated);
                    if (stat_boss2defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss3:
                    if (debug) Debug.Log(achievement.m_strName + " boss3: " + stat_boss3defeated);
                    if (stat_boss3defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss4_ng:
                    if (debug) Debug.Log(achievement.m_strName + " boss4ng: " + stat_boss4defeated_ng);
                    if (stat_boss4defeated_ng >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_jobchanger_3:
                    if (debug) Debug.Log(achievement.m_strName + " job changes local: " + stat_jobchangeslocal);
                    if (stat_jobchangeslocal >= 3)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_fillcorral:
                    if (debug) Debug.Log(achievement.m_strName + " corral mobs: " + stat_monstersincorral);
                    if (stat_monstersincorral == 12)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_killchampionslocal_10:
                    if (debug) Debug.Log(achievement.m_strName + " champs killed local: " + stat_championskilledlocal);
                    if (stat_championskilledlocal >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_flaskuses_many:
                    if (debug) Debug.Log(achievement.m_strName + " flask uses: " + stat_flaskuses);
                    if (stat_flaskuses >= 250)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_merchant_buyer:
                    if (debug) Debug.Log(achievement.m_strName + " gold spent: " + stat_merchantgoldspent);
                    if (stat_merchantgoldspent >= 10000)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_charlevel10:
                    if (debug) Debug.Log(achievement.m_strName + " max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_itemdelver:
                    if (debug) Debug.Log(achievement.m_strName + " dreams cleared: " + stat_itemworldscleared);
                    if (stat_itemworldscleared >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_recipefailer:
                    if (debug) Debug.Log(achievement.m_strName + " recipes failed: " + stat_recipefailer);
                    if (stat_recipefailer >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_survivor:
                    if (debug) Debug.Log(achievement.m_strName + " survive1hp: " + stat_survive1hp);
                    if (stat_survive1hp >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_founddimrift:
                    if (debug) Debug.Log(achievement.m_strName + " found dim rift: " + stat_dimriftentered);
                    if (stat_dimriftentered >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_beatdimriftboss:
                    if (debug) Debug.Log(achievement.m_strName + " beat dim rift: " + stat_dimriftbossdefeated);
                    if (stat_dimriftbossdefeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_charlevel15:
                    if (debug) Debug.Log(achievement.m_strName + " char max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 15)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_charlevel20:
                    if (debug) Debug.Log(achievement.m_strName + " DLC max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 20 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dirtbeakthrone:
                    if (debug) Debug.Log(achievement.m_strName + " sit on throne: " + stat_sitonthrone);
                    if (stat_sitonthrone >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_firstgearset:
                    if (debug) Debug.Log(achievement.m_strName + " gear sets: " + stat_gearsets);
                    if (stat_gearsets >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_firstlegendary:
                    if (debug) Debug.Log(achievement.m_strName + " legs found: " + stat_legendariesfound);
                    if (stat_legendariesfound >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_recipemaster:
                    if (debug) Debug.Log(achievement.m_strName + " recipes learned: " + stat_recipeslearned);
                    if (stat_recipeslearned >= 18)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_monsterpedia:
                    if (debug) Debug.Log(achievement.m_strName + " max pedia monsters: " + MAX_PEDIA_MONSTERS);
                    if (stat_monstersknown >= MAX_PEDIA_MONSTERS)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_petletter:
                    if (debug) Debug.Log(achievement.m_strName + " pet letters: " + stat_petletters);
                    if (stat_petletters >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_5itemmods:
                    if (debug) Debug.Log(achievement.m_strName + " max item mods: " + stat_maxitemmods);
                    if (stat_maxitemmods >= 5)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_firstmonsterhatched:
                    if (debug) Debug.Log(achievement.m_strName + " mobs hatched: " + stat_monstersbred);
                    if (stat_monstersbred >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss4weekly:
                    if (debug) Debug.Log(achievement.m_strName + " boss4 weekly: " + stat_boss4weekly);
                    if (stat_boss4weekly >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_boss4daily:
                    if (debug) Debug.Log(achievement.m_strName + " boss4 daily: " + stat_boss4daily);
                    if (stat_boss4daily >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dailyfloor10:
                    if (debug) Debug.Log(achievement.m_strName + " daily highest: " + stat_dailyhighestfloor);
                    if (stat_dailyhighestfloor >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_weeklyfloor10:
                    if (debug) Debug.Log(achievement.m_strName + " weekly highest: " + stat_weeklyhighestfloor);
                    if (stat_weeklyhighestfloor >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_anyjourney:
                    if (debug) Debug.Log(achievement.m_strName + " journeys: " + stat_dlc1_wandererjourneys_complete);
                    if (stat_dlc1_wandererjourneys_complete >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_towerordeals:
                    if (debug) Debug.Log(achievement.m_strName + " tower ordeals: " + stat_dlc1_towerordeals_victory);
                    if (stat_dlc1_towerordeals_victory >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_spiritstag:
                    if (debug) Debug.Log(achievement.m_strName + " spirit stag: " + stat_dlc1_spiritstag_capture);
                    if (stat_dlc1_spiritstag_capture >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss1:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 1 defeated: " + stat_dlc1_shara_boss1defeated);
                    if (stat_dlc1_shara_boss1defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss2:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 2 defeated: " + stat_dlc1_shara_boss2defeated);
                    if (stat_dlc1_shara_boss2defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss3:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 3 defeated: " + stat_dlc1_shara_boss3defeated);
                    if (stat_dlc1_shara_boss3defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss4:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 4 defeated: " + stat_dlc1_shara_boss4defeated);
                    if (stat_dlc1_shara_boss4defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_calligrapher_allskills:
                    if (debug) Debug.Log(achievement.m_strName + " calli master: " + stat_dlc1_calligraphermastered);
                    if (stat_dlc1_calligraphermastered >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc1_runelearner:
                    if (debug) Debug.Log(achievement.m_strName + " rune learner: " + stat_dlc1_runeslearned);
                    if (stat_dlc1_runeslearned >= BakedItemDefinitions.NUM_RUNES_KNOWLEDGE && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_alldragons:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons: " + stat_dlc2_robotdragon_defeated);
                    if (stat_dlc2_robotdragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_alldragons_ngplus:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons ng+: " + stat_dlc2_robotdragon_defeated_ngp);
                    if (stat_dlc2_robotdragon_defeated_ngp >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_alldragons_ngplusplus:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons ng++: " + stat_dlc2_robotdragon_defeated_savage);
                    if (stat_dlc2_robotdragon_defeated_savage >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_banditdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat bandit: " + stat_dlc2_banditdragon_defeated);
                    if (stat_dlc2_banditdragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_frogdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat frog: " + stat_dlc2_frogdragon_defeated);
                    if (stat_dlc2_frogdragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_spiritdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat spirit: " + stat_dlc2_spiritdragon_defeated);
                    if (stat_dlc2_spiritdragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_jellydragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat jelly: " + stat_dlc2_jellydragon_defeated);
                    if (stat_dlc2_jellydragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_beat_beastdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat beast: " + stat_dlc2_beastdragon_defeated);
                    if (stat_dlc2_beastdragon_defeated >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_frogcrafting:
                    if (debug) Debug.Log(achievement.m_strName + " frogcrafting: " + stat_dlc2_frogcrafting);
                    if (stat_dlc2_frogcrafting >= 10)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
                case Achievement.achievement_dlc2_ultimatewhip:
                    if (debug) Debug.Log(achievement.m_strName + " whip: " + stat_dlc2_ultimatewhip_learned);
                    if (stat_dlc2_ultimatewhip_learned >= 1)
                    {
                        UnlockAchievement(achievement);
                    }
                    break;
            }
        }

        //Store stats in the Steam database if necessary
        if (m_bStoreStats)
        {
            // already set any achievements in UnlockAchievement

            // set stats
            SteamUserStats.SetStat("stat_highestfloor", stat_highestfloor);
            SteamUserStats.SetStat("stat_maxlevel", stat_maxlevel);
            SteamUserStats.SetStat("stat_numcharacters", stat_numcharacters);
            SteamUserStats.SetStat("stat_jobchangeslocal", stat_jobchangeslocal);
            SteamUserStats.SetStat("stat_monstersincorral", stat_monstersincorral);
            SteamUserStats.SetStat("stat_championskilledlocal", stat_championskilledlocal);
            SteamUserStats.SetStat("stat_flaskuses", stat_flaskuses);
            SteamUserStats.SetStat("stat_merchantgoldspent", stat_merchantgoldspent);
            SteamUserStats.SetStat("stat_itemworldscleared", stat_itemworldscleared);
            SteamUserStats.SetStat("stat_recipefailer", stat_recipefailer);
            SteamUserStats.SetStat("stat_survive1hp", stat_survive1hp);

            SteamUserStats.SetStat("stat_weaponmasterytiers", stat_weaponmasterytiers);
            SteamUserStats.SetStat("stat_nightmarescomplete", stat_nightmarescomplete);
            SteamUserStats.SetStat("stat_singlejobmastered", stat_singlejobmastered);
            SteamUserStats.SetStat("stat_coolfrogcapture", stat_coolfrogcapture);
            SteamUserStats.SetStat("stat_boss1defeated", stat_boss1defeated);
            SteamUserStats.SetStat("stat_boss2defeated", stat_boss2defeated);
            SteamUserStats.SetStat("stat_boss3defeated", stat_boss3defeated);
            SteamUserStats.SetStat("stat_boss4defeated", stat_boss4defeated);
            SteamUserStats.SetStat("stat_boss4defeated_ng", stat_boss4defeated_ng);
            SteamUserStats.SetStat("stat_onepunch", stat_onepunch);

            SteamUserStats.SetStat("stat_dimriftentered", stat_dimriftentered);
            SteamUserStats.SetStat("stat_dimriftbossdefeated", stat_dimriftbossdefeated);

            SteamUserStats.SetStat("stat_sitonthrone", stat_sitonthrone);
            SteamUserStats.SetStat("stat_maxitemmods", stat_maxitemmods);
            SteamUserStats.SetStat("stat_legendariesfound", stat_legendariesfound);
            SteamUserStats.SetStat("stat_recipeslearned", stat_recipeslearned);
            SteamUserStats.SetStat("stat_monstersknown", stat_monstersknown);
            SteamUserStats.SetStat("stat_petletters", stat_petletters);
            SteamUserStats.SetStat("stat_monstersbred", stat_monstersbred);
            SteamUserStats.SetStat("stat_gearsets", stat_gearsets);

            SteamUserStats.SetStat("stat_boss4weekly", stat_boss4weekly);
            SteamUserStats.SetStat("stat_boss4daily", stat_boss4daily);
            SteamUserStats.SetStat("stat_weeklyhighestfloor", stat_weeklyhighestfloor);
            SteamUserStats.SetStat("stat_dailyhighestfloor", stat_dailyhighestfloor);

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                SteamUserStats.SetStat("stat_dlc1_calligraphermastered", stat_dlc1_calligraphermastered);
                SteamUserStats.SetStat("stat_dlc1_runeslearned", stat_dlc1_runeslearned);
                SteamUserStats.SetStat("stat_dlc1_shara_boss1defeated", stat_dlc1_shara_boss1defeated);
                SteamUserStats.SetStat("stat_dlc1_shara_boss2defeated", stat_dlc1_shara_boss2defeated);
                SteamUserStats.SetStat("stat_dlc1_shara_boss3defeated", stat_dlc1_shara_boss3defeated);
                SteamUserStats.SetStat("stat_dlc1_shara_boss4defeated", stat_dlc1_shara_boss4defeated);
                if (stat_dlc1_shara_boss4defeated >= 1)
                {
                    TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_STORY_CLEARED, 1);
                }
                SteamUserStats.SetStat("stat_dlc1_spiritstag_capture", stat_dlc1_spiritstag_capture);
                SteamUserStats.SetStat("stat_dlc1_towerordeals_victory", stat_dlc1_towerordeals_victory);
                SteamUserStats.SetStat("stat_dlc1_wandererjourneys_complete", stat_dlc1_wandererjourneys_complete);
            }

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
            {
                SteamUserStats.SetStat("stat_dlc2_banditdragon_defeated", stat_dlc2_banditdragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_beastdragon_defeated", stat_dlc2_beastdragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_frogcrafting", stat_dlc2_frogcrafting);
                SteamUserStats.SetStat("stat_dlc2_frogdragon_defeated", stat_dlc2_frogdragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_jellydragon_defeated", stat_dlc2_jellydragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_robotdragon_defeated", stat_dlc2_robotdragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_robotdragon_defeated_ngp", stat_dlc2_robotdragon_defeated_ngp);
                SteamUserStats.SetStat("stat_dlc2_robotdragon_defeated_savage", stat_dlc2_robotdragon_defeated_savage);
                SteamUserStats.SetStat("stat_dlc2_spiritdragon_defeated", stat_dlc2_spiritdragon_defeated);
                SteamUserStats.SetStat("stat_dlc2_ultimatewhip_learned", stat_dlc2_ultimatewhip_learned);
            }

            bool bSuccess = SteamUserStats.StoreStats();
            // If this failed, we never sent anything to the server, try
            // again later.
            m_bStoreStats = !bSuccess;
        }
    }

    public void UpdatePS4StatsAndTrophies(bool debug = false)
    {
#if UNITY_PS4

        if (!NP_PS4_Trophies.toolkitInit)
        {
            Debug.LogError("NP_PS4_Trophies is not Initialized!");
            return;
        }

        if (debug) Debug.Log("Requesting stats/achievements update.");


        // Evaluate achievements
        foreach (Achievement_t achievement in m_Achievements)
        {

            switch (achievement.m_eAchievementID)
            {
                case Achievement.achievement_nightmarecomplete:
                    if (debug) Debug.Log(achievement.m_strName + " nightmare stat: " + stat_nightmarescomplete);
                    if (stat_nightmarescomplete >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[11])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(11);
                        }
                    }
                    break;
                case Achievement.achievement_job_mastery:
                    if (debug) Debug.Log(achievement.m_strName + " single job mastered: " + stat_singlejobmastered);
                    if (stat_singlejobmastered >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[12])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(12);
                        }
                    }
                    break;
                case Achievement.achievement_weaponmasteries:
                    if (debug) Debug.Log(achievement.m_strName + " weapon mastery tiers: " + stat_weaponmasterytiers);
                    if (stat_weaponmasterytiers >= 4)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[13])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(13);
                        }
                    }
                    break;
                case Achievement.achievement_coolfrog_capture:
                    if (debug) Debug.Log(achievement.m_strName + " coolfrogs captured: " + stat_coolfrogcapture);
                    if (stat_coolfrogcapture >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[14])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(14);
                        }
                    }
                    break;
                case Achievement.achievement_onepunch:
                    if (debug) Debug.Log(achievement.m_strName + " one punch: " + stat_onepunch);
                    if (stat_onepunch >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[20])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(20);
                        }
                    }
                    break;
                case Achievement.achievement_totalcharacters_1:
                    if (debug) Debug.Log(achievement.m_strName + " num characters: " + stat_numcharacters);
                    if (stat_numcharacters >= 20)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[1])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(1);
                        }
                    }
                    break;
                case Achievement.achievement_boss1:
                    if (debug) Debug.Log(achievement.m_strName + " boss1: " + stat_boss1defeated);
                    if (stat_boss1defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[15])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(15);
                        }
                    }
                    break;
                case Achievement.achievement_boss4:
                    if (debug) Debug.Log(achievement.m_strName + " boss4: " + stat_boss4defeated);
                    if (stat_boss4defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[18])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(18);
                        }
                    }
                    break;
                case Achievement.achievement_boss2:
                    if (debug) Debug.Log(achievement.m_strName + " boss2: " + stat_boss2defeated);
                    if (stat_boss2defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[16])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(16);
                        }
                    }
                    break;
                case Achievement.achievement_boss3:
                    if (debug) Debug.Log(achievement.m_strName + " boss3: " + stat_boss3defeated);
                    if (stat_boss3defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[17])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(17);
                        }
                    }
                    break;
                case Achievement.achievement_boss4_ng:
                    if (debug) Debug.Log(achievement.m_strName + " boss4ng: " + stat_boss4defeated_ng);
                    if (stat_boss4defeated_ng >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[19])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(19);
                        }
                    }
                    break;
                case Achievement.achievement_jobchanger_3:
                    if (debug) Debug.Log(achievement.m_strName + " job changes local: " + stat_jobchangeslocal);
                    if (stat_jobchangeslocal >= 3)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[2])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(2);
                        }
                    }
                    break;
                case Achievement.achievement_fillcorral:
                    if (debug) Debug.Log(achievement.m_strName + " corral mobs: " + stat_monstersincorral);
                    if (stat_monstersincorral == 12)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[3])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(3);
                        }
                    }
                    break;
                case Achievement.achievement_killchampionslocal_10:
                    if (debug) Debug.Log(achievement.m_strName + " champs killed local: " + stat_championskilledlocal);
                    if (stat_championskilledlocal >= 10)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[4])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(4);
                        }
                    }
                    break;
                case Achievement.achievement_flaskuses_many:
                    if (debug) Debug.Log(achievement.m_strName + " flask uses: " + stat_flaskuses);
                    if (stat_flaskuses >= 250)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[5])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(5);
                        }
                    }
                    break;
                case Achievement.achievement_merchant_buyer:
                    if (debug) Debug.Log(achievement.m_strName + " gold spent: " + stat_merchantgoldspent);
                    if (stat_merchantgoldspent >= 10000)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[6])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(6);
                        }
                    }
                    break;
                case Achievement.achievement_charlevel10:
                    if (debug) Debug.Log(achievement.m_strName + " max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 10)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[7])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(7);
                        }
                    }
                    break;
                case Achievement.achievement_itemdelver:
                    if (debug) Debug.Log(achievement.m_strName + " dreams cleared: " + stat_itemworldscleared);
                    if (stat_itemworldscleared >= 10)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[8])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(8);
                        }
                    }
                    break;
                case Achievement.achievement_recipefailer:
                    if (debug) Debug.Log(achievement.m_strName + " recipes failed: " + stat_recipefailer);
                    if (stat_recipefailer >= 10)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[9])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(9);
                        }
                    }
                    break;
                case Achievement.achievement_survivor:
                    if (debug) Debug.Log(achievement.m_strName + " survive1hp: " + stat_survive1hp);
                    if (stat_survive1hp >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[10])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(10);
                        }
                    }
                    break;
                case Achievement.achievement_founddimrift:
                    if (debug) Debug.Log(achievement.m_strName + " found dim rift: " + stat_dimriftentered);
                    if (stat_dimriftentered >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[21])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(21);
                        }
                    }
                    break;
                case Achievement.achievement_beatdimriftboss:
                    if (debug) Debug.Log(achievement.m_strName + " beat dim rift: " + stat_dimriftbossdefeated);
                    if (stat_dimriftbossdefeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[22])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(22);
                        }
                    }
                    break;
                case Achievement.achievement_charlevel15:
                    if (debug) Debug.Log(achievement.m_strName + " char max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 15)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[23])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(23);
                        }
                    }
                    break;
                case Achievement.achievement_dlc1_charlevel20:
                    if (debug) Debug.Log(achievement.m_strName + " DLC max level: " + stat_maxlevel);
                    if (stat_maxlevel >= 20 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[36])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(36);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[40])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(40);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dirtbeakthrone:
                    if (debug) Debug.Log(achievement.m_strName + " sit on throne: " + stat_sitonthrone);
                    if (stat_sitonthrone >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[25])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(25);
                        }
                    }
                    break;
                case Achievement.achievement_firstgearset:
                    if (debug) Debug.Log(achievement.m_strName + " gear sets: " + stat_gearsets);
                    if (stat_gearsets >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[24])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(24);
                        }
                    }
                    break;
                case Achievement.achievement_firstlegendary:
                    if (debug) Debug.Log(achievement.m_strName + " legs found: " + stat_legendariesfound);
                    if (stat_legendariesfound >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[27])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(27);
                        }
                    }
                    break;
                case Achievement.achievement_recipemaster:
                    if (debug) Debug.Log(achievement.m_strName + " recipes learned: " + stat_recipeslearned);
                    if (stat_recipeslearned >= 18)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[28])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(28);
                        }
                    }
                    break;
                case Achievement.achievement_monsterpedia:
                    if (debug) Debug.Log(achievement.m_strName + " max pedia monsters: " + MAX_PEDIA_MONSTERS);
                    if (stat_monstersknown >= MAX_PEDIA_MONSTERS)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[31])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(31);
                        }
                    }
                    break;
                case Achievement.achievement_petletter:
                    if (debug) Debug.Log(achievement.m_strName + " pet letters: " + stat_petletters);
                    if (stat_petletters >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[29])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(29);
                        }
                    }
                    break;
                case Achievement.achievement_5itemmods:
                    if (debug) Debug.Log(achievement.m_strName + " max item mods: " + stat_maxitemmods);
                    if (stat_maxitemmods >= 5)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[26])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(26);
                        }
                    }
                    break;
                case Achievement.achievement_firstmonsterhatched:
                    if (debug) Debug.Log(achievement.m_strName + " mobs hatched: " + stat_monstersbred);
                    if (stat_monstersbred >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[30])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(30);
                        }
                    }
                    break;
                ///Daily and weekly Trophies
                /*
            case Achievement.achievement_boss4weekly:
                if (debug) Debug.Log(achievement.m_strName + " boss4 weekly: " + stat_boss4weekly);
                if (stat_boss4weekly >= 1)
                {
                    if (!NP_PS4_Trophies.alreadyAwarded[34])
                    {
                        NP_PS4_Trophies.instance.UnlockTrophy(34);
                    }
                }
                break;
            case Achievement.achievement_boss4daily:
                if (debug) Debug.Log(achievement.m_strName + " boss4 daily: " + stat_boss4daily);
                if (stat_boss4daily >= 1)
                {
                    if (!NP_PS4_Trophies.alreadyAwarded[35])
                    {
                        NP_PS4_Trophies.instance.UnlockTrophy(35);
                    }
                }
                break;
            case Achievement.achievement_dailyfloor10:
                if (debug) Debug.Log(achievement.m_strName + " daily highest: " + stat_dailyhighestfloor);
                if (stat_dailyhighestfloor >= 10)
                {
                    if (!NP_PS4_Trophies.alreadyAwarded[33])
                    {
                        NP_PS4_Trophies.instance.UnlockTrophy(33);
                    }
                }
                break;
            case Achievement.achievement_weeklyfloor10:
                if (debug) Debug.Log(achievement.m_strName + " weekly highest: " + stat_weeklyhighestfloor);
                if (stat_weeklyhighestfloor >= 10)
                {
                    if (!NP_PS4_Trophies.alreadyAwarded[32])
                    {
                        NP_PS4_Trophies.instance.UnlockTrophy(32);
                    }
                }
                break;
                */
                case Achievement.achievement_dlc1_anyjourney:
                    if (debug) Debug.Log(achievement.m_strName + " journeys: " + stat_dlc1_wandererjourneys_complete);
                    if (stat_dlc1_wandererjourneys_complete >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[39])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(39);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[43])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(43);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_towerordeals:
                    if (debug) Debug.Log(achievement.m_strName + " tower ordeals: " + stat_dlc1_towerordeals_victory);
                    if (stat_dlc1_towerordeals_victory >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[38])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(38);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[42])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(42);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_spiritstag:
                    if (debug) Debug.Log(achievement.m_strName + " spirit stag: " + stat_dlc1_spiritstag_capture);
                    if (stat_dlc1_spiritstag_capture >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[37])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(37);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[41])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(41);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss1:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 1 defeated: " + stat_dlc1_shara_boss1defeated);
                    if (stat_dlc1_shara_boss1defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[32])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(32);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[36])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(36);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss2:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 2 defeated: " + stat_dlc1_shara_boss2defeated);
                    if (stat_dlc1_shara_boss2defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[33])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(33);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[37])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(37);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss3:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 3 defeated: " + stat_dlc1_shara_boss3defeated);
                    if (stat_dlc1_shara_boss3defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[34])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(34);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[38])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(38);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_sharaboss4:
                    if (debug) Debug.Log(achievement.m_strName + " shara boss 4 defeated: " + stat_dlc1_shara_boss4defeated);
                    if (stat_dlc1_shara_boss4defeated >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[35])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(35);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[39])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(39);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_calligrapher_allskills:
                    if (debug) Debug.Log(achievement.m_strName + " calli master: " + stat_dlc1_calligraphermastered);
                    if (stat_dlc1_calligraphermastered >= 1 && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[40])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(40);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[44])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(44);
                        }
                        */
                    }
                    break;
                case Achievement.achievement_dlc1_runelearner:
                    if (debug) Debug.Log(achievement.m_strName + " rune learner: " + stat_dlc1_runeslearned);
                    if (stat_dlc1_runeslearned >= BakedItemDefinitions.NUM_RUNES_KNOWLEDGE && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[41])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(41);
                        }
                        /*
                        if (!NP_PS4_Trophies.alreadyAwarded[45])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(45);
                        }
                        */
                    }
                    break;
                ///Achievements from dlc2 Dawn of Dragons

                case Achievement.achievement_dlc2_beat_alldragons:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons: " + stat_dlc2_robotdragon_defeated);
                    if (stat_dlc2_robotdragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[49])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(49);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[53])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(53);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_alldragons_ngplus:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons ng+: " + stat_dlc2_robotdragon_defeated_ngp);
                    if (stat_dlc2_robotdragon_defeated_ngp >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[50])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(50);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[54])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(54);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_alldragons_ngplusplus:
                    if (debug) Debug.Log(achievement.m_strName + " beat all dragons ng++: " + stat_dlc2_robotdragon_defeated_savage);
                    if (stat_dlc2_robotdragon_defeated_savage >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[51])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(51);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[55])
                        //{
                        // NP_PS4_Trophies.instance.UnlockTrophy(55);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_banditdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat bandit: " + stat_dlc2_banditdragon_defeated);
                    if (stat_dlc2_banditdragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[47])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(47);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[51])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(51);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_frogdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat frog: " + stat_dlc2_frogdragon_defeated);
                    if (stat_dlc2_frogdragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[43])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(43);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[47])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(47);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_spiritdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat spirit: " + stat_dlc2_spiritdragon_defeated);
                    if (stat_dlc2_spiritdragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[44])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(44);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[48])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(48);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_jellydragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat jelly: " + stat_dlc2_jellydragon_defeated);
                    if (stat_dlc2_jellydragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[45])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(45);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[49])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(49);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_beat_beastdragon:
                    if (debug) Debug.Log(achievement.m_strName + " beat beast: " + stat_dlc2_beastdragon_defeated);
                    if (stat_dlc2_beastdragon_defeated >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[46])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(46);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[50])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(50);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_frogcrafting:
                    if (debug) Debug.Log(achievement.m_strName + " frogcrafting: " + stat_dlc2_frogcrafting);
                    if (stat_dlc2_frogcrafting >= 10)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[48])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(48);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[52])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(52);
                        //}

                    }
                    break;
                case Achievement.achievement_dlc2_ultimatewhip:
                    if (debug) Debug.Log(achievement.m_strName + " whip: " + stat_dlc2_ultimatewhip_learned);
                    if (stat_dlc2_ultimatewhip_learned >= 1)
                    {
                        if (!NP_PS4_Trophies.alreadyAwarded[42])
                        {
                            NP_PS4_Trophies.instance.UnlockTrophy(42);
                        }

                        //if (!NP_PS4_Trophies.alreadyAwarded[46])
                        //{
                        //NP_PS4_Trophies.instance.UnlockTrophy(46);
                        //}

                    }
                    break;

                default:
                    if (debug) Debug.Log("Trophie that doesn't exist: " + achievement.m_strName);
                    break;
            }
        }

        //Save stats
        if (m_bStoreStats)
        {
            if (debug) Debug.LogError("Saving Trophie stats");
            PS4_SaveStats();
            m_bStoreStats = false;
        }

#endif
    }

    private void PS4_LoadStats()
    {
#if UNITY_PS4
        //if we don't have saved stats, don't load them
        if (!PlayerPrefs.HasKey("stat_highestfloor"))
        {
            Debug.Log("no saved stats");
            return;
        }
        // load stats
        stat_highestfloor = PlayerPrefs.GetInt("stat_highestfloor");
        stat_maxlevel = PlayerPrefs.GetInt("stat_maxlevel");
        stat_numcharacters = PlayerPrefs.GetInt("stat_numcharacters");
        stat_jobchangeslocal = PlayerPrefs.GetInt("stat_jobchangeslocal");
        stat_monstersincorral = PlayerPrefs.GetInt("stat_monstersincorral");
        stat_championskilledlocal = PlayerPrefs.GetInt("stat_championskilledlocal");
        stat_flaskuses = PlayerPrefs.GetInt("stat_flaskuses");
        stat_merchantgoldspent = PlayerPrefs.GetInt("stat_merchantgoldspent");
        stat_itemworldscleared = PlayerPrefs.GetInt("stat_itemworldscleared");
        stat_recipefailer = PlayerPrefs.GetInt("stat_recipefailer");
        stat_survive1hp = PlayerPrefs.GetInt("stat_survive1hp");

        stat_weaponmasterytiers = PlayerPrefs.GetInt("stat_weaponmasterytiers");
        stat_nightmarescomplete = PlayerPrefs.GetInt("stat_nightmarescomplete");
        stat_singlejobmastered = PlayerPrefs.GetInt("stat_singlejobmastered");
        stat_coolfrogcapture = PlayerPrefs.GetInt("stat_coolfrogcapture");
        stat_boss1defeated = PlayerPrefs.GetInt("stat_boss1defeated");
        stat_boss2defeated = PlayerPrefs.GetInt("stat_boss2defeated");
        stat_boss3defeated = PlayerPrefs.GetInt("stat_boss3defeated");
        stat_boss4defeated = PlayerPrefs.GetInt("stat_boss4defeated");
        stat_boss4defeated_ng = PlayerPrefs.GetInt("stat_boss4defeated_ng");
        stat_onepunch = PlayerPrefs.GetInt("stat_onepunch");

        stat_dimriftentered = PlayerPrefs.GetInt("stat_dimriftentered");
        stat_dimriftbossdefeated = PlayerPrefs.GetInt("stat_dimriftbossdefeated");

        stat_sitonthrone = PlayerPrefs.GetInt("stat_sitonthrone");
        stat_maxitemmods = PlayerPrefs.GetInt("stat_maxitemmods");
        stat_legendariesfound = PlayerPrefs.GetInt("stat_legendariesfound");
        stat_recipeslearned = PlayerPrefs.GetInt("stat_recipeslearned");
        stat_monstersknown = PlayerPrefs.GetInt("stat_monstersknown");
        stat_petletters = PlayerPrefs.GetInt("stat_petletters");
        stat_monstersbred = PlayerPrefs.GetInt("stat_monstersbred");
        stat_gearsets = PlayerPrefs.GetInt("stat_gearsets");

        stat_boss4weekly = PlayerPrefs.GetInt("stat_boss4weekly");
        stat_boss4daily = PlayerPrefs.GetInt("stat_boss4daily");
        stat_weeklyhighestfloor = PlayerPrefs.GetInt("stat_weeklyhighestfloor");
        stat_dailyhighestfloor = PlayerPrefs.GetInt("stat_dailyhighestfloor");

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            stat_dlc1_calligraphermastered = PlayerPrefs.GetInt("stat_dlc1_calligraphermastered");
            stat_dlc1_runeslearned = PlayerPrefs.GetInt("stat_dlc1_runeslearned");
            stat_dlc1_shara_boss1defeated = PlayerPrefs.GetInt("stat_dlc1_shara_boss1defeated");
            stat_dlc1_shara_boss2defeated = PlayerPrefs.GetInt("stat_dlc1_shara_boss2defeated");
            stat_dlc1_shara_boss3defeated = PlayerPrefs.GetInt("stat_dlc1_shara_boss3defeated");
            stat_dlc1_shara_boss4defeated = PlayerPrefs.GetInt("stat_dlc1_shara_boss4defeated");
            stat_dlc1_spiritstag_capture = PlayerPrefs.GetInt("stat_dlc1_spiritstag_capture");
            stat_dlc1_towerordeals_victory = PlayerPrefs.GetInt("stat_dlc1_towerordeals_victory");
            stat_dlc1_wandererjourneys_complete = PlayerPrefs.GetInt("stat_dlc1_wandererjourneys_complete");
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            stat_dlc2_banditdragon_defeated = PlayerPrefs.GetInt("stat_dlc2_banditdragon_defeated");
            stat_dlc2_beastdragon_defeated = PlayerPrefs.GetInt("stat_dlc2_beastdragon_defeated");
            stat_dlc2_frogcrafting = PlayerPrefs.GetInt("stat_dlc2_frogcrafting");
            stat_dlc2_frogdragon_defeated = PlayerPrefs.GetInt("stat_dlc2_frogdragon_defeated");
            stat_dlc2_jellydragon_defeated = PlayerPrefs.GetInt("stat_dlc2_jellydragon_defeated");
            stat_dlc2_robotdragon_defeated = PlayerPrefs.GetInt("stat_dlc2_robotdragon_defeated");
            stat_dlc2_robotdragon_defeated_ngp = PlayerPrefs.GetInt("stat_dlc2_robotdragon_defeated_ngp");
            stat_dlc2_robotdragon_defeated_savage = PlayerPrefs.GetInt("stat_dlc2_robotdragon_defeated_savage");
            stat_dlc2_spiritdragon_defeated = PlayerPrefs.GetInt("stat_dlc2_spiritdragon_defeated");
            stat_dlc2_ultimatewhip_learned = PlayerPrefs.GetInt("stat_dlc2_ultimatewhip_learned");
        }
#endif
    }

    private void PS4_SaveStats()
    {
#if UNITY_PS4
        // save stats        
        PlayerPrefs.SetInt("stat_highestfloor", stat_highestfloor);
        PlayerPrefs.SetInt("stat_maxlevel", stat_maxlevel);
        PlayerPrefs.SetInt("stat_numcharacters", stat_numcharacters);
        PlayerPrefs.SetInt("stat_jobchangeslocal", stat_jobchangeslocal);
        PlayerPrefs.SetInt("stat_monstersincorral", stat_monstersincorral);
        PlayerPrefs.SetInt("stat_championskilledlocal", stat_championskilledlocal);
        PlayerPrefs.SetInt("stat_flaskuses", stat_flaskuses);
        PlayerPrefs.SetInt("stat_merchantgoldspent", stat_merchantgoldspent);
        PlayerPrefs.SetInt("stat_itemworldscleared", stat_itemworldscleared);
        PlayerPrefs.SetInt("stat_recipefailer", stat_recipefailer);
        PlayerPrefs.SetInt("stat_survive1hp", stat_survive1hp);

        PlayerPrefs.SetInt("stat_weaponmasterytiers", stat_weaponmasterytiers);
        PlayerPrefs.SetInt("stat_nightmarescomplete", stat_nightmarescomplete);
        PlayerPrefs.SetInt("stat_singlejobmastered", stat_singlejobmastered);
        PlayerPrefs.SetInt("stat_coolfrogcapture", stat_coolfrogcapture);
        PlayerPrefs.SetInt("stat_boss1defeated", stat_boss1defeated);
        PlayerPrefs.SetInt("stat_boss2defeated", stat_boss2defeated);
        PlayerPrefs.SetInt("stat_boss3defeated", stat_boss3defeated);
        PlayerPrefs.SetInt("stat_boss4defeated", stat_boss4defeated);
        PlayerPrefs.SetInt("stat_boss4defeated_ng", stat_boss4defeated_ng);
        PlayerPrefs.SetInt("stat_onepunch", stat_onepunch);

        PlayerPrefs.SetInt("stat_dimriftentered", stat_dimriftentered);
        PlayerPrefs.SetInt("stat_dimriftbossdefeated", stat_dimriftbossdefeated);

        PlayerPrefs.SetInt("stat_sitonthrone", stat_sitonthrone);
        PlayerPrefs.SetInt("stat_maxitemmods", stat_maxitemmods);
        PlayerPrefs.SetInt("stat_legendariesfound", stat_legendariesfound);
        PlayerPrefs.SetInt("stat_recipeslearned", stat_recipeslearned);
        PlayerPrefs.SetInt("stat_monstersknown", stat_monstersknown);
        PlayerPrefs.SetInt("stat_petletters", stat_petletters);
        PlayerPrefs.SetInt("stat_monstersbred", stat_monstersbred);
        PlayerPrefs.SetInt("stat_gearsets", stat_gearsets);

        PlayerPrefs.SetInt("stat_boss4weekly", stat_boss4weekly);
        PlayerPrefs.SetInt("stat_boss4daily", stat_boss4daily);
        PlayerPrefs.SetInt("stat_weeklyhighestfloor", stat_weeklyhighestfloor);
        PlayerPrefs.SetInt("stat_dailyhighestfloor", stat_dailyhighestfloor);

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            PlayerPrefs.SetInt("stat_dlc1_calligraphermastered", stat_dlc1_calligraphermastered);
            PlayerPrefs.SetInt("stat_dlc1_runeslearned", stat_dlc1_runeslearned);
            PlayerPrefs.SetInt("stat_dlc1_shara_boss1defeated", stat_dlc1_shara_boss1defeated);
            PlayerPrefs.SetInt("stat_dlc1_shara_boss2defeated", stat_dlc1_shara_boss2defeated);
            PlayerPrefs.SetInt("stat_dlc1_shara_boss3defeated", stat_dlc1_shara_boss3defeated);
            PlayerPrefs.SetInt("stat_dlc1_shara_boss4defeated", stat_dlc1_shara_boss4defeated);
            if (stat_dlc1_shara_boss4defeated >= 1)
            {
                TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_STORY_CLEARED, 1);
            }
            PlayerPrefs.SetInt("stat_dlc1_spiritstag_capture", stat_dlc1_spiritstag_capture);
            PlayerPrefs.SetInt("stat_dlc1_towerordeals_victory", stat_dlc1_towerordeals_victory);
            PlayerPrefs.SetInt("stat_dlc1_wandererjourneys_complete", stat_dlc1_wandererjourneys_complete);
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            PlayerPrefs.SetInt("stat_dlc2_banditdragon_defeated", stat_dlc2_banditdragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_beastdragon_defeated", stat_dlc2_beastdragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_frogcrafting", stat_dlc2_frogcrafting);
            PlayerPrefs.SetInt("stat_dlc2_frogdragon_defeated", stat_dlc2_frogdragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_jellydragon_defeated", stat_dlc2_jellydragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_robotdragon_defeated", stat_dlc2_robotdragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_robotdragon_defeated_ngp", stat_dlc2_robotdragon_defeated_ngp);
            PlayerPrefs.SetInt("stat_dlc2_robotdragon_defeated_savage", stat_dlc2_robotdragon_defeated_savage);
            PlayerPrefs.SetInt("stat_dlc2_spiritdragon_defeated", stat_dlc2_spiritdragon_defeated);
            PlayerPrefs.SetInt("stat_dlc2_ultimatewhip_learned", stat_dlc2_ultimatewhip_learned);
        }
        PlayerPrefs.Save();
#endif
    }

    private void PS4_Trophies_Test()
    {
#if UNITY_PS4
        /*
        //this is for testing - should be in Update()
        if (Rewired.ReInput.players.GetPlayer(0).GetButtonDown("Cycle Minimap"))
        {
            PS4_Trophies_Test();
        }
        */

        if (!Debug.isDebugBuild)
        {
            Debug.LogError("This should be called only in debug build!");
            return;
        }

        bool unlockAllTrophies = false;
        int cT = NP_PS4_Trophies.currentTrophieTest;

        //UnlockTrophy(46)
        //unlock Trophies by one
        if (unlockAllTrophies == false)
        {
            //NP_PS4_Trophies.instance.UnlockTrophy(cT);
            Debug.LogError("Unlocking Trophie with ID: " + cT);

            if (cT == 1)
            {
                SetTotalCharacters(20);
            }
            else if (cT == 2)
            {
                SetLocalJobChanges(3);
            }
            else if (cT == 3)
            {
                SetMonstersInCorral(12);
            }
            else if (cT == 4)
            {
                SetChampionsDefeatedLocal(10);
            }
            else if (cT == 5)
            {
                stat_flaskuses = 250; IncrementFlaskUses();
            }
            else if (cT == 6)
            {
                AddMerchantGoldSpent(10000);
            }
            else if (cT == 7)
            {
                SetHighestCharacterLevel(10);
            }
            else if (cT == 8)
            {
                stat_itemworldscleared = 10; IncrementItemWorldsCleared(10);
            }
            else if (cT == 9)
            {
                stat_recipefailer = 10; IncrementRecipesFailed();
            }
            else if (cT == 10)
            {
                Survive1HP();
            }
            else if (cT == 11)
            {
                ItemNightmareCleared();
            }
            else if (cT == 12)
            {
                JobFullyMastered();
            }
            else if (cT == 13)
            {
                SetHighestWeaponMasteryTier(4);
            }
            else if (cT == 14)
            {
                CoolfrogCaptured();
            }
            else if (cT == 15)
            {
                Boss1Defeated();
            }
            else if (cT == 16)
            {
                Boss2Defeated();
            }
            else if (cT == 17)
            {
                Boss3Defeated();
            }
            else if (cT == 18)
            {
                Boss4Defeated();
            }
            else if (cT == 19)
            {
                Boss4Defeated_NG();
            }
            else if (cT == 20)
            {
                MonsterPunchedOut();
            }
            else if (cT == 21)
            {
                FoundDimRift();
            }
            else if (cT == 22)
            {
                BeatDimRift();
            }
            else if (cT == 23)
            {
                SetHighestCharacterLevel(15);
            }
            else if (cT == 24)
            {
                CompletedGearSet();
            }
            else if (cT == 25)
            {
                SatOnDirtbeakThrone();
            }
            else if (cT == 26)
            {
                SetMaxItemModsFound(5);
            }
            else if (cT == 27)
            {
                IncrementLegendariesFound();
            }
            else if (cT == 28)
            {
                SetRecipesKnown(18);
            }
            else if (cT == 29)
            {
                IncrementMonsterLettersRead();
            }
            else if (cT == 30)
            {
                IncrementMonstersHatched();
            }
            else if (cT == 31)
            {
                SetMonstersKnown(MAX_PEDIA_MONSTERS);
            }
            /*
            else if (cT == 32)
            {
                stat_weeklyhighestfloor = 10; //SetLowestFloorExplored(10);
            }
            else if (cT == 33)
            {
                stat_dailyhighestfloor = 10; //SetLowestFloorExplored(10);
            }
            else if (cT == 34)
            {
                stat_boss4weekly = 1; //Boss4Defeated();
            }
            else if (cT == 35)
            {
                stat_boss4daily = 1; //Boss4Defeated();
            }
            */
            else if (cT == 32) //36
            {
                DLC1_Shara_Boss1Defeated();
            }
            else if (cT == 33) //37
            {
                DLC1_Shara_Boss2Defeated();
            }
            else if (cT == 34) //38
            {
                DLC1_Shara_Boss3Defeated();
            }
            else if (cT == 35) //39
            {
                DLC1_Shara_Boss4Defeated();
            }
            else if (cT == 36) //40
            {
                SetHighestCharacterLevel(20);
            }
            else if (cT == 37) //41
            {
                DLC1_SpiritStagCapture();
            }
            else if (cT == 38) //42
            {
                DLC1_Ordeals_Complete();
            }
            else if (cT == 39) //43
            {
                DLC1_MysteryDungeon_Complete();
            }
            else if (cT == 40) //44
            {
                DLC1_Calligrapher_Mastered();
            }
            else if (cT == 41) //45
            {
                stat_dlc1_runeslearned = BakedItemDefinitions.NUM_RUNES_KNOWLEDGE; DLC1_RuneLearned();
            }
            else if (cT == 42) //46
            {
                DLC2_UltimateWhip_Learned();
            }
            else if (cT == 43) //47
            {
                DLC2_Beat_Frog_Dragon();
            }
            else if (cT == 44) //48
            {
                DLC2_Beat_Spirit_Dragon();
            }
            else if (cT == 45) //49
            {
                DLC2_Beat_Jelly_Dragon();
            }
            else if (cT == 46) //50
            {
                DLC2_Beat_Beast_Dragon();
            }
            else if (cT == 47) //51
            {
                DLC2_Beat_Bandit_Dragon();
            }
            else if (cT == 48) //52
            {
                stat_dlc2_frogcrafting = 10; DLC2_Frogcrafting_Used();
            }
            else if (cT == 49) //53
            {
                DLC2_Beat_Robot_Dragon();
            }
            else if (cT == 50) //54
            {
                DLC2_Beat_Robot_Dragon_NGPlus();
            }
            else if (cT == 51) //55
            {
                DLC2_Beat_Robot_Dragon_Savage();
            }
            else
            {
                Debug.LogError("This Trophie doesn't exists!");
            }

            if (cT >= 52) //56
            {
                //nothing
            }
            else
            {
                NP_PS4_Trophies.currentTrophieTest++;
            }

        }

        //unlock all Trophies at once
        if (unlockAllTrophies == true)
        {
            Debug.LogError("All Trophies Unlocked!");

            SetHighestCharacterLevel(20);
            stat_itemworldscleared = 10; IncrementItemWorldsCleared(10);
            SetTotalCharacters(20);
            SetLocalJobChanges(3);
            SetMonstersInCorral(12);
            stat_flaskuses = 250; IncrementFlaskUses();
            AddMerchantGoldSpent(10000);
            Boss1Defeated();
            MonsterPunchedOut();
            ItemNightmareCleared();
            CoolfrogCaptured();
            SetHighestWeaponMasteryTier(4);
            JobFullyMastered();
            Boss2Defeated();
            Boss3Defeated();
            Boss4Defeated();
            Boss4Defeated_NG();
            SetChampionsDefeatedLocal(10);
            IncrementChampionsDefeated();
            IncrementLocalJobChanges();
            stat_recipefailer = 10; IncrementRecipesFailed();
            Survive1HP();
            FoundDimRift();
            BeatDimRift();
            SetRecipesKnown(18);
            SatOnDirtbeakThrone();
            IncrementMonstersHatched();
            CompletedGearSet();
            IncrementLegendariesFound();
            SetMaxItemModsFound(5);
            SetMonstersKnown(MAX_PEDIA_MONSTERS);
            IncrementMonsterLettersRead();
            ///Daily and Weekly
            //stat_weeklyhighestfloor = 10; //SetLowestFloorExplored(10);
            //stat_dailyhighestfloor = 10; //SetLowestFloorExplored(10);
            //stat_boss4weekly = 1; //Boss4Defeated();
            //stat_boss4daily = 1; //Boss4Defeated();
            //DLC1
            DLC1_Shara_Boss1Defeated();
            DLC1_Shara_Boss2Defeated();
            DLC1_Shara_Boss3Defeated();
            DLC1_Shara_Boss4Defeated();
            DLC1_MysteryDungeon_Complete();
            stat_dlc1_runeslearned = BakedItemDefinitions.NUM_RUNES_KNOWLEDGE; DLC1_RuneLearned();
            DLC1_Ordeals_Complete();
            DLC1_SpiritStagCapture();
            DLC1_Calligrapher_Mastered();
            //DLC2
            DLC2_UltimateWhip_Learned();
            stat_dlc2_frogcrafting = 10; DLC2_Frogcrafting_Used();
            DLC2_Beat_Frog_Dragon();
            DLC2_Beat_Beast_Dragon();
            DLC2_Beat_Spirit_Dragon();
            DLC2_Beat_Bandit_Dragon();
            DLC2_Beat_Jelly_Dragon();
            DLC2_Beat_Robot_Dragon();
            DLC2_Beat_Robot_Dragon_NGPlus();
            DLC2_Beat_Robot_Dragon_Savage();
        }
#endif
    }
}

public class SteamScript : MonoBehaviour
{
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;

    public static bool steamOverlayActive;

    private void OnEnable()
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        if (SteamManager.Initialized)
        {
            m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
        }
    }

    private void CloudSave()
    {

    }

    private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
    {
        if (!PlatformVariables.ALLOW_STEAM_ACHIEVEMENTS) return;
        if (pCallback.m_bActive != 0)
        {
            //Debug.Log("Steam Overlay has been activated");
            steamOverlayActive = true;
        }
        else {
            //Debug.Log("Steam Overlay has been closed");
            steamOverlayActive = false;
        }
    }


}