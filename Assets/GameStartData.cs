using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameStartData
{
    public static string playerName;
    public static string playerJob;
    public static CharacterJobs jobAsEnum;
    public static bool newGame;
    public static List<string> playerFeats;
    public static int saveGameSlot = 0;
    public static float playTimeInSeconds;
    static GameModes theMode;
    public static int NewGamePlus
    {
        get 
        {
            return newGamePlus;
        }
        set 
        {
            //Debug.LogError("SETTING NEW GAME PLUS STATE TO " + value);
            newGamePlus = value;
        }
    }

    static int newGamePlus;
    public static GameModes[] gameModesSelectedBySlot;
    public static int[] saveSlotLevels;
    public static int[] saveSlotNGP;
    public static ChallengeTypes[] challengeTypeBySlot;
    public static int loadGameVer;
    public static bool[] beatGameStates;
    public static bool[] slotContainsMetaData;
    public static bool[] slotInRandomJobMode;
    public static bool[,] jobsUnlockedBySlot;
    public static List<string>[] featsUnlockedBySlot;

    //These are flags for individual saves
    public static bool[] slotInSharaMode;


    //This flag is set to true when Shara Mode is selected 
    public static bool gameInSharaMode;

    public static int worldSeed;

    public static List<string> allFeats;
    static LoadStates _currentLoadState;
    public static LoadStates CurrentLoadState
    {
        get
        {
            return _currentLoadState;
        }
        set
        {
            _currentLoadState = value;
        }
    }

    public static bool[] gameModifiers;
    public static GameModifierData[] gameModifierDataList;

    public static ChallengeDataPack currentChallengeData;

    public static ChallengeTypes challengeType;
    public static int challengeDay;
    public static int challengeWeek;

    public static List<string> miscGameStartTags;
    public static List<string>[] modsEnabledPerSlot;
    public static List<EDLCPackages>[] dlcEnabledPerSlot;

    public static bool speedRunModeActive;

    /// <summary>
    /// Set this value once ever while loading up meta data on the load screen.
    /// </summary>
    public static List<string> featsUnlockedAcrossAllSlots;

    /// <summary>
    /// Set this value once ever while loading up meta data on the load screen.
    /// </summary>
    public static bool[] jobsUnlockedAcrossAllSlots;

    public static HashSet<string> conversationsToIgnoreInSpeedrunMode;

    public const float NGPLUSPLUS_CT_MODIFIER = 0.66f;
    public const float NGPLUSPLUS_PARRY_DAMAGE_MODIFIER = 0.33f;
    public const float NGPLUSPLUS_POWERUPHEAL_MODIFIER = 0.5f;
    public const float NGPLUSPLUS_CHAMP_NIGHTMARE_SHARD_DROPCHANCE = 0.05f;
    public const float NGPLUSPLUS_CHANCE_EQ_UPGRADE_ON_DROP = 0.07f;
    public const float NGPLUSPLUS_CHANCE_UPGRADE_COMMON = 0.4f;
    public const float NGPLUS_ROSEPETALS_CHAMP_CHANCE = 0.05f;
    public const float NGPLUSPLUS_CHANCE_MON_LOOT_EXTRAMAGIC = 0.15f;

    public static void ResetGameModifiers()
    {
        gameModifiers = new bool[(int)GameModifiers.COUNT];
    }

    public static void ChangeGameMode(GameModes gm)
    {
        theMode = gm;
        //Debug.Log("Game mode changed to " + gm);
        if (GameMasterScript.gmsSingleton != null)
        {
            GameMasterScript.gmsSingleton.gameMode = gm;
            GameMasterScript.gmsSingleton.adventureModeActive = gm == GameModes.ADVENTURE;

        }
    }

    public static bool CheckGameModifier(GameModifiers mod)
    {
        if (mod == GameModifiers.JOB_SPECIALIST)
        {
            if (SharaModeStuff.IsSharaModeActive() || (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsMysteryDungeonMap()))
            {                
                return true;
            }
        }

        return gameModifiers[(int)mod];
    }
    public static void Initialize()
    {
        if (jobsUnlockedAcrossAllSlots == null)
        {
            jobsUnlockedAcrossAllSlots = new bool[(int)CharacterJobs.COUNT];
        }
        if (featsUnlockedAcrossAllSlots == null)
        {
            featsUnlockedAcrossAllSlots = new List<string>();
        }
        speedRunModeActive = PlayerOptions.speedrunMode;

        if (gameModesSelectedBySlot == null)
        {
            gameModesSelectedBySlot = new GameModes[GameMasterScript.kNumSaveSlots];
        }

        modsEnabledPerSlot = new List<string>[GameMasterScript.kNumSaveSlots];
        dlcEnabledPerSlot = new List<EDLCPackages>[GameMasterScript.kNumSaveSlots];
        for (int i = 0; i < modsEnabledPerSlot.Length; i++)
        {
            modsEnabledPerSlot[i] = new List<string>();
        }
        for (int i = 0; i < dlcEnabledPerSlot.Length; i++)
        {
            dlcEnabledPerSlot[i] = new List<EDLCPackages>();
        }
        challengeType = ChallengeTypes.NONE;
        worldSeed = 0;
        playerName = "";
        playerJob = "";
        newGame = false;
        NewGamePlus = 0;
        miscGameStartTags = new List<string>();
        theMode = GameModes.COUNT;
        currentChallengeData = null;
        gameModifiers = new bool[(int)GameModifiers.COUNT];
        challengeTypeBySlot = new ChallengeTypes[GameMasterScript.kNumSaveSlots];
        for (int i = 0; i < challengeTypeBySlot.Length; i++)
        {
            challengeTypeBySlot[i] = ChallengeTypes.NONE;
        }
        saveSlotLevels = new int[GameMasterScript.kNumSaveSlots];
        beatGameStates = new bool[GameMasterScript.kNumSaveSlots];
        saveSlotNGP = new int[GameMasterScript.kNumSaveSlots];
        slotContainsMetaData = new bool[GameMasterScript.kNumSaveSlots];
        slotInRandomJobMode = new bool[GameMasterScript.kNumSaveSlots];
        slotInSharaMode = new bool[GameMasterScript.kNumSaveSlots];

        if (playerFeats != null)
        {
            playerFeats.Clear();
        }


        jobsUnlockedBySlot = new bool[GameMasterScript.kNumSaveSlots, (int)CharacterJobs.COUNT]; 
        featsUnlockedBySlot = new List<string>[GameMasterScript.kNumSaveSlots];
        for (int i = 0; i < featsUnlockedBySlot.Length; i++)
        {
            featsUnlockedBySlot[i] = new List<string>();
        }

        if (gameModifierDataList == null)
        {
            gameModifierDataList = new GameModifierData[(int)GameModifiers.COUNT];
            for (int i = 0; i < gameModifierDataList.Length; i++)
            {
                gameModifierDataList[i] = new GameModifierData((GameModifiers)i, true);
            }
        }

        if (conversationsToIgnoreInSpeedrunMode == null)
        {
            conversationsToIgnoreInSpeedrunMode = new HashSet<string>()
            {
                "preboss2trigger", // callout before 2nd boss fight
                "dialog_bottleneck_angry", // bottleneck 'explainer'
                "shara_vista_callout" // shara's callout, who needs it
            };

        }

    }

    public static void FlushData()
    {
        gameInSharaMode = false;
        jobAsEnum = CharacterJobs.COUNT;
        challengeType = ChallengeTypes.NONE;
        worldSeed = 0;
        playerName = "";
        playerJob = "";
        newGame = false;
        NewGamePlus = 0;        
        theMode = GameModes.COUNT;
        currentChallengeData = null;
    }

    public static void SetCurrentChallengeData(ChallengeDataPack cdp)
    {
        currentChallengeData = cdp;
        playerJob = cdp.cJob.ToString();
        jobAsEnum = cdp.cJob;
        ClearFeats();
        foreach (string feat in cdp.playerFeats)
        {
            AddFeat(feat);
        }
        foreach (string mod in cdp.modifiersEnabled)
        {
            AddGameModifier(mod);
        }
        worldSeed = cdp.worldSeed;
        challengeType = cdp.cType;
        challengeDay = cdp.dayOfYear;
        challengeWeek = cdp.weekOfYear;
    }

    

    public static void ClearPlayerFeats()
    {
        if (playerFeats != null)
        {
            playerFeats.Clear();
        }
    }

    public static GameModes GetGameMode()
    {
        return theMode;
    }

    public static void ClearFeats()
    {
        if (playerFeats == null) return;
        playerFeats.Clear();
    }

    public static bool HasFeat(string feat)
    {
        if (playerFeats == null)
        {
            playerFeats = new List<string>();
        }
        return playerFeats.Contains(feat);
    }

    public static void AddFeat(string feat)
    {
        if (playerFeats == null)
        {
            playerFeats = new List<string>();
        }
        playerFeats.Add(feat);
    }

    public static void AddGameModifier(string strMod)
    {
        GameModifiers mod = (GameModifiers)Enum.Parse(typeof(GameModifiers), strMod);
        gameModifiers[(int)mod] = true;
    }

    public static void RemoveGameModifier(string strMod)
    {
        GameModifiers mod = (GameModifiers)Enum.Parse(typeof(GameModifiers), strMod);
        gameModifiers[(int)mod] = false;
    }

    public static void RemoveFeat(string feat)
    {
        if (playerFeats == null) return;

        playerFeats.Remove(feat);
    }

    public static int GetPlayerFeatCount()
    {
        if (playerFeats == null) return 0;
        return playerFeats.Count;
    }

    /// <summary>
    /// Checks if conversation is allowed in speedrun mode, or returns 'true' if we're not in speedrun mode
    /// </summary>
    /// <param name="convoName"></param>
    /// <returns></returns>
    public static bool IsConversationValid(string convoName)
    {
        if (!PlayerOptions.speedrunMode) return true;
        if (conversationsToIgnoreInSpeedrunMode.Contains(convoName))
        {
            return false;
        }
        return true;
    }

}