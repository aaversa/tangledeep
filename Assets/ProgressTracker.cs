using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class should handle all the various quests, flags, and tasks that aren't "Rumors"
// For example, whether or not you've beaten bosses, herbalist quest, whether X/Y/Z progress has been achieved
// Right now this is all handled by Hero or MetaProgress string/int dictionaries, which is fine...
// But we don't want to have to remember random strings like "boss1defeated" vs. "boss1victory" and make typos
// So this script will help out.

public enum TDProgress { ANCIENTCUBE, HERBALIST, SHADOWSHARDS, BOSS1, BOSS2, BOSS3, BOSS4, CRAFTINGBOX, DRAGON_FROG, DRAGON_BANDIT,
    DRAGON_BEAST_HORDES, DRAGON_BEAST, DRAGON_KICKOFF_QUEST, MYSTERYKING_DEFEAT, WEAPON_MASTER_STATUES, ARMOR_MASTER_STATUES, WANDERER_JOURNEY,
    REALMGODS_UNLOCKED, BOSS4_PHASE2, ROMANCEQUEST, ROMANCE_MEALS_SHARED, SHARA_TOWN_CALLOUT, SHARA_FIRST_MEETING, ARMOR_MASTER_QUEST, NGPLUSPLUS_STARTED_ONCE,
    BEAT_SAVAGEWORLD_EVER, DRAGON_BANDIT_DUNGEON, DRAGON_BEAST_DUNGEON, DRAGON_SPIRIT_DUNGEON, DRAGON_SPIRIT, DRAGON_ROBOT, DRAGON_JELLY, DRAGON_ROBOT_DUNGEON, DRAGON_JELLY_DUNGEON,
    DRAGON_ROBOT_KICKOFF, DLC2_FIRSTTIME_RUN, DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, CORRALQUEST, SORCERESS_UNLOCKED, RANDOMJOBMODE_STARTED_ONCE,
    RANDOMJOBMODE_FIRST_BRANCH, ESCAPED_FROG, SHARA_UNLOCKED, AUTO_ABANDON_TUTORIAL, ROBOT_DUNGEON_INITIAL_ENTRY, TOWN_GENTLE_POINTER, 
    LUNAR_NEW_YEAR_QUEST, COUNT
}
public enum ProgressLocations { HERO, META, BOTH, COUNT }

public class ProgressTracker
{
    public static Dictionary<TDProgress, string> dictGameProgress = new Dictionary<TDProgress, string>()
    {
        { TDProgress.ANCIENTCUBE, "ancientcube" },
        { TDProgress.ARMOR_MASTER_QUEST, "armormaster_quest" },
        { TDProgress.HERBALIST, "herbalist_quest" },
        { TDProgress.CRAFTINGBOX, "crafting_box" },
        { TDProgress.SHADOWSHARDS, "mir_shadowshards" },
        { TDProgress.DRAGON_FROG, "dragon_frog" },
        { TDProgress.DRAGON_BANDIT, "dragon_bandit" },
        { TDProgress.DRAGON_BEAST, "dragon_beast" },
        { TDProgress.DRAGON_SPIRIT, "dragon_spirit" },
        { TDProgress.DRAGON_ROBOT, "dragon_robot" },
        { TDProgress.DRAGON_JELLY, "dragon_jelly" },
        { TDProgress.DRAGON_BEAST_HORDES, "dragon_beast_hordes" },
        { TDProgress.DRAGON_KICKOFF_QUEST, "dragon_kickoff_quest" }, // The thing that starts the entire Dragon quest line
        { TDProgress.DRAGON_ROBOT_KICKOFF, "dragon_robot_kickoff" }, // The thing that starts the entire Dragon quest line
        { TDProgress.MYSTERYKING_DEFEAT, "mysteryking_defeat" },
        { TDProgress.BOSS1, "boss1fight" },
        { TDProgress.BOSS2, "boss2fight" },
        { TDProgress.BOSS3, "boss3fight" },
        { TDProgress.BOSS4, "boss4fight" },
        { TDProgress.BOSS4_PHASE2, "boss4fight_phase2" },
        { TDProgress.WEAPON_MASTER_STATUES, "weaponmasterstatues" },
        { TDProgress.ARMOR_MASTER_STATUES, "armormasterstatues" },
        { TDProgress.WANDERER_JOURNEY, "wanderer_quest" },
        { TDProgress.REALMGODS_UNLOCKED, "realmgods_unlocked" },
        { TDProgress.ROMANCE_MEALS_SHARED, "romancemeals" },
        { TDProgress.ROMANCEQUEST, "romancequest" },
        { TDProgress.SHARA_TOWN_CALLOUT, "shara_meta_callout" },
        { TDProgress.SHARA_FIRST_MEETING, "shara_meet1_begin" },
        { TDProgress.NGPLUSPLUS_STARTED_ONCE, "ngpp_started_once" },
        { TDProgress.DRAGON_BANDIT_DUNGEON, "dragonbandit_dungeon" },
        { TDProgress.DRAGON_BEAST_DUNGEON, "dragonbeast_dungeon" },
        { TDProgress.DRAGON_SPIRIT_DUNGEON, "dragonspirit_dungeon" },
        { TDProgress.DRAGON_ROBOT_DUNGEON, "dragonrobot_dungeon" },
        { TDProgress.DRAGON_JELLY_DUNGEON, "dragonjelly_dungeon" },
        { TDProgress.DLC2_FIRSTTIME_RUN, "dlc2_firsttime_run" },
        { TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, "spiritdungeon_accessible" },
        { TDProgress.CORRALQUEST, "corralquest" },
        { TDProgress.SORCERESS_UNLOCKED, "sorceress_unlocked" },
        { TDProgress.RANDOMJOBMODE_STARTED_ONCE, "rjmode_once" },
        { TDProgress.RANDOMJOBMODE_FIRST_BRANCH, "rjbranches" },
        { TDProgress.ESCAPED_FROG, "begin_frog_event_grove" },
        { TDProgress.SHARA_UNLOCKED, "sharaunlocked" },
        { TDProgress.AUTO_ABANDON_TUTORIAL, "autoabandontutorial" },
        { TDProgress.ROBOT_DUNGEON_INITIAL_ENTRY, "firsttime_robotdungeon" },
        { TDProgress.TOWN_GENTLE_POINTER, "towngentletut" },
        { TDProgress.LUNAR_NEW_YEAR_QUEST, "lnyquest" }
    };

    public static void RemoveProgress(TDProgress tdq, ProgressLocations location)
    {
        string questString;
        if (dictGameProgress.TryGetValue(tdq, out questString))
        {
            if (location == ProgressLocations.HERO)
            {
                GameMasterScript.heroPCActor.RemoveActorData(questString);
            }
            else
            {
                MetaProgressScript.dictMetaProgress.Remove(questString);
            }
        }
    }

    public static int SetProgress(TDProgress tdq, ProgressLocations location, int value)
    {
        string questString;
        if (dictGameProgress.TryGetValue(tdq, out questString))
        {
            if (location == ProgressLocations.HERO || location == ProgressLocations.BOTH)
            {
                GameMasterScript.heroPCActor.SetActorData(questString, value);
            }
            if (location == ProgressLocations.META || location == ProgressLocations.BOTH)
            {
                MetaProgressScript.SetMetaProgress(questString, value);
            }
        }
        return -1;
    }

    public static int CheckProgress(TDProgress tdq, ProgressLocations location)
    {
        string questString;
        if (dictGameProgress.TryGetValue(tdq, out questString))
        {
            return location == ProgressLocations.HERO ? 
                GameMasterScript.heroPCActor.ReadActorData(questString) : MetaProgressScript.ReadMetaProgress(questString);
        }
        return -1;
    }

    /// <summary>
    /// Updates global-level unlock/progress data which we can use for PlayerOptions.globalUnlocks
    /// </summary>
    public static void SetPlayerPrefsFromLoadedProgress()
    {
        if (CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1)
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.BEAT_FIRST_BOSS, 1);
        }
        if ((CheckProgress(TDProgress.BOSS4, ProgressLocations.HERO) >= 1 || CheckProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.HERO) >= 1) && SharaModeStuff.IsSharaModeActive())
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_STORY_CLEARED, 1);
        }
    }
}
