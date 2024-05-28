using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterDeathEventScripts : MonoBehaviour {

    // Functions that run when a monster is defeated or destroyed for any reason.
    // This is called during the CheckCombatResult() function of CombatResultsScript
    // AFTER most processing but before rewards are doled out, and before the sprite is destroyed
    // If we return FALSE, then we should NOT destroy the physical GameObject/sprite (so we can play around with it.)

    public static bool OnMonsterDeathTemplate(Monster mon)
    {
        // mon is the monster that died
        // The combat results function will continue as normal regardless: return TRUE to destroy sprite as normal
        // However, you could modify creature inventory, XPMod, etc
        // Or add quest hooks

        return true;
    }

    public static bool CheckForLieutenantDeath(Monster mon)
    {
        if (mon.actorfaction == Faction.PLAYER) return true;

        if (mon.ReadActorData("lieutenant") == 1)
        {
            int playerCount = GameMasterScript.heroPCActor.ReadActorData("num_lieutenants");
            playerCount--;
            GameMasterScript.heroPCActor.SetActorData("num_lieutenants", playerCount);
            //Debug.Log("THere are now " + playerCount + " lieutenants");
        }

        return true;
    }

    /// <summary>
    /// Runs in the spirit dungeon. If a monster died *outside* of a special Spirit Pool, summon a ghost.
    /// </summary>
    /// <param name="mon"></param>
    /// <returns></returns>
    public static bool CreateSpiritMonster(Monster mon)
    {
        MapTileData diedMTD = MapMasterScript.GetTile(mon.GetPos());

        if (mon.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            return true;
        }

        if (mon.isBoss)
        {
            return true;
        }

        if (mon.actorfaction == Faction.PLAYER)
        {
            return true;
        }

        if (diedMTD.CheckForSpecialMapObjectType(SpecialMapObject.BLESSEDPOOL))
        {
            StringManager.SetTag(0, mon.displayName);
            GameLogScript.LogWriteStringRef("log_ghost_putatrest");

            /* if (GameMasterScript.tutorialManager.WatchedTutorial("spirit_dungeon_tutorial"))
            {
                Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("spirit_dungeon_tutorial");
                UIManagerScript.StartConversation(c, DialogType.TUTORIAL, null);
            } */

            return true;
        }

        if (mon.myStats.CheckHasStatusName("status_undead"))
        {
            return true;
        }

        // Create the ghost, which has lower stats and gives no xp.        

        Monster newMon = MonsterManagerScript.CreateMonster(mon.actorRefName, false, false, false, 0f, false);
        newMon.displayName = StringManager.GetString("mon_xp_genericghost_name");
        MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, newMon, true);        
        //newMon.monsterPowers.Clear();
        //newMon.myAbilities.RemoveAllAbilities();
        newMon.myStats.AddStatusByRef("status_undead", newMon, 99);
        newMon.myStats.BoostCoreStatsByPercent(-0.25f);
        newMon.allMitigationAddPercent += 0.1f;
        newMon.allDamageMultiplier -= 0.1f;
        newMon.xpMod = 0f;

        StringManager.SetTag(0, newMon.displayName);
        StringManager.SetTag(1, mon.displayName);
        GameLogScript.LogWriteStringRef("log_ghost_restless");

        MapTileData tileForNewMon = MapMasterScript.activeMap.GetRandomEmptyTile(mon.GetPos(), 1, true, true, true, false, true);
        MapMasterScript.activeMap.PlaceActor(newMon, tileForNewMon);

        MapMasterScript.singletonMMS.SpawnMonster(newMon, true);

        CombatManagerScript.GenerateSpecificEffectAnimation(newMon.GetPos(), "SmokePoof", null, true);

        newMon.xpMod = 0f;

        /* if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_spiritdungeon"))
        {
            Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_spiritdungeon");
            UIManagerScript.StartConversation(c, DialogType.TUTORIAL, null);
        } */

        return true;
    }


    public static bool UnlockGatesOfGatekeeperIndex(Monster mon)
    {
        int myGateIndex = mon.ReadActorData("gatekeeper");

        if (myGateIndex < 0)
        {
            Debug.Log("Warning! " + mon.displayName + " " + mon.actorRefName + " " + mon.actorUniqueID + " has no linked gate index?");
            return true;
        }

        BanditDragonStuff.UnlockGatesOfIndex(myGateIndex, MapMasterScript.activeMap, true);

        return true;
    }

    public static bool RemoveRunicBoostFromPlayer(Monster mon)
    {
        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("status_runicboost");
        UIManagerScript.RefreshStatuses();

        return true;
    }

    public static bool RobotDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_ROBOT, ProgressLocations.HERO, 2);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        GameMasterScript.gmsSingleton.SetTempGameObject("robotdragonobj", mon.GetObject());

        Cutscenes.singleton.StartCoroutine(RobotDragonStuff.RobotDragonDefeated());

        return false;
    }

    public static bool FrogDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_FROG, ProgressLocations.HERO, 2);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        GameMasterScript.gmsSingleton.SetTempGameData("frogdying", 1);

        Cutscenes.singleton.StartCoroutine(FrogDragonStuff.FrogDragonDefeated());

        return false;
    }

    public static bool BanditDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_BANDIT, ProgressLocations.HERO, 2);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

		MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        Cutscenes.singleton.StartCoroutine(BanditDragonStuff.BanditDragonDefeated());

        return false;
    }

    public static bool SlimeDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO, 4);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        Cutscenes.singleton.StartCoroutine(SlimeDragonStuff.SlimeDragonDefeated());

        return false;
    }

    public static bool TyrantDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_BEAST, ProgressLocations.HERO, 2);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

		MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        Cutscenes.singleton.StartCoroutine(TyrantDragonStuff.TyrantDragonDefeated());

        return false;
    }

    public static bool SpiritDragonDefeated(Monster mon)
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT, ProgressLocations.HERO, 2);
        GameMasterScript.SetAnimationPlaying(true, true);

        // #todo - Steam stats/achievements for this

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        Cutscenes.singleton.StartCoroutine(SpiritDragonStuff.SpiritDragonDefeated());

        return false;
    }
}
