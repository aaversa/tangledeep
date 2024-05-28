using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// All cutscenes and events related to the Tyrant (Beast) Dragon.

public partial class DialogEventsScript
{
    public static bool BeginTyrantDragonBossFight(string value)
    {
	
		MusicManagerScript.RequestPlayMusic("dragonboss",true);
	

        StartEncounterWithBoss("mon_beastdragon", TDProgress.DRAGON_BEAST, 1);
        return false;
    }

    public static bool CleanupAfterTyrantDragonFight(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.StartCoroutine(TyrantDragonStuff.CleanupAfterTyrantDragonFight());

        return false;
    }
}

public class TyrantDragonStuff
{
    public static void DoBeastDragonUnlockIntro()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(TyrantDragonStuff.BeastDungeonUnlockIntro());
    }
    
    static IEnumerator BeastDungeonUnlockIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Stairs beastDungeonStairs = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR);
        beastDungeonStairs.prefab = "MightyVine";
        MapMasterScript.singletonMMS.SpawnStairs(beastDungeonStairs);

        CustomAlgorithms.RevealTilesAroundPoint(beastDungeonStairs.GetPos(), 1, true);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("StoneMovement");
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);

        yield return new WaitForSeconds(0.75f);

        foreach (Stairs st in MapMasterScript.theDungeon.FindFloor(MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR).mapStairs)
        {
            if (st.pointsToFloor == 0 || st.stairsUp)
            {
                st.SetDestination(MapMasterScript.activeMap.floor);
            }
        }

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(beastDungeonStairs.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(2f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("beast_dungeon_unlocked", DialogType.STANDARD, null);
    }


    public static IEnumerator TyrantDragonIntro()
    {
        BossHealthBarScript.DisableBoss();
        Monster tyrantDragon = MapMasterScript.activeMap.FindActor("mon_beastdragon") as Monster;
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(1.2f);

        // Pan to Big Boy

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(tyrantDragon.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.0f);

        // Linger

        CombatManagerScript.GenerateSpecificEffectAnimation(tyrantDragon.GetPos(), "DeepSoundEmanation", null, true);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("tyrant_dragon_intro", DialogType.STANDARD, null);
        ProgressTracker.SetProgress(TDProgress.DRAGON_BEAST, ProgressLocations.HERO, 1);
    }

    public static IEnumerator TyrantDragonEnrage(Actor dragon, float waitTime)
    {
        yield return new WaitForSeconds(1f);

        int numRoars = 3;

        for (int i = 0; i < numRoars; i++)
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(dragon.GetPos(), "DeepSoundEmanation", null, true);
            yield return new WaitForSeconds(waitTime / numRoars);
        }
    }

    public static IEnumerator TyrantDragonDefeated()
    {
        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss";

        Monster beastDragon = MapMasterScript.activeMap.FindActor("mon_beastdragon") as Monster;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(beastDragon.GetPos(), 1f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.1f);

        //CombatManagerScript.GenerateSpecificEffectAnimation(beastDragon.GetPos(), "BigExplosionEffect", null, true);
        //UIManagerScript.FlashWhite(1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(beastDragon.GetPos(), "DeepSoundEmanation", null, true);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Beast_Dragon();


        //beastDragon.myMovable.FadeOutThenDie();
        //GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(beastDragon, beastDragon.GetObject(), 0.1f));        

        yield return new WaitForSeconds(1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(beastDragon.GetPos(), "DeepSoundEmanation", null, true);

        yield return new WaitForSeconds(1f);

        GameMasterScript.gmsSingleton.SetTempGameObject("tyrantdragon", beastDragon.GetObject());

        UIManagerScript.StartConversationByRef("tyrant_dragon_defeated", DialogType.KEYSTORY, null);
        
    }

    public static IEnumerator CleanupAfterTyrantDragonFight()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        GameObject dragon = GameMasterScript.gmsSingleton.ReadTempGameObject("tyrantdragon");

        UIManagerScript.FadeOut(0.98f);

        yield return new WaitForSeconds(1f);

        if (dragon != null)
        {
            dragon.GetComponent<Movable>().FadeOutThenDie();
        }

        foreach (Monster mn in MapMasterScript.activeMap.monstersInMap)
        {
            if (mn.actorfaction == Faction.ENEMY)
            {
                mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
                GameMasterScript.AddToDeadQueue(mn, true);
            }
        }

        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);

        DialogEventsScript.RecenterCameraOnPlayer("");

        UIManagerScript.FadeIn(0.75f);

        yield return new WaitForSeconds(0.5f);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);

        GameMasterScript.SetAnimationPlaying(false);

    }

    public static void EnrageScript(string[] args)
    {
        int dragonID = Int32.Parse(args[0]);
        Fighter actor = GameMasterScript.gmsSingleton.TryLinkActorFromDict(dragonID) as Fighter;

        if (actor == null) return;

        actor.SetActorData("enrage50", 1);
        GameMasterScript.SetAnimationPlaying(true, true);
        Conversation cEnrage = GameMasterScript.FindConversation("tyrant_dragon_enrage");
        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(actor.GetPos(), 0.75f, false);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(cEnrage, DialogType.STANDARD, null, 3.75f));
        GameMasterScript.gmsSingleton.StartCoroutine(TyrantDragonEnrage(actor, 2.25f));
        actor.myStats.AddStatusByRefAndLog("status_enrageattack", actor, 99);
        actor.myStats.AddStatusByRef("status_mmultraheavy", actor, 99);
    }
}

public partial class DLCCutscenes
{
    public static IEnumerator BeastDungeonIntroToMightyVine()
    {
        yield return new WaitForSeconds(0.5f);

        Stairs mightyVine = null;
        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (!st.stairsUp)
            {
                mightyVine = st;
                break;
            }
        }

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(mightyVine.GetPos(), 0.75f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.5f);

        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("beastdungeon_intro_part2", DialogType.STANDARD, null);
    }

    public static IEnumerator BeastDungeonFirstWavePart2()
    {
        yield return new WaitForSeconds(0.3f);
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos() + new Vector2(0f, 40f), "DeepSoundEmanation", null, true);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 4; i++)
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos() + new Vector2(0f, 40f), "SoundEmanation", null, true);
            yield return new WaitForSeconds(0.2f);
            UIManagerScript.PlayCursorSound("StoneMovement");
            yield return new WaitForSeconds(0.25f);
            CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        }

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.SetAnimationPlaying(false, false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("beastdungeon_firstwavestart_part2", DialogType.STANDARD, null);

    }

    public static IEnumerator FailedMonsterHordeLevel()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        string hordeCheck = "horde_" + MapMasterScript.activeMap.floor;

        GameMasterScript.heroPCActor.RemoveActorData("hordeclear_" + MapMasterScript.activeMap.floor);
        GameMasterScript.heroPCActor.RemoveActorData(hordeCheck + "_waves");
        GameMasterScript.heroPCActor.RemoveActorData(hordeCheck);

        yield return new WaitForSeconds(0.5f);

        GameLogScript.LogWriteStringRef("exp_log_monsterhorde_fail");

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FemaleSoundEmanation", null, true);

        yield return new WaitForSeconds(1f);

        UIManagerScript.FadeOut(1f);

        Stairs prevStairs = null;
        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.stairsUp)
            {
                prevStairs = st;
                break;
            }
        }

        MapMasterScript.CreateMap(MapMasterScript.activeMap.floor);

        DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR, MapMasterScript.NUM_BEASTDUNGEON_MAPS, EDLCPackages.EXPANSION2);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        Map targetLocation = null;
        if (prevStairs == null || prevStairs.NewLocation == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No previous stairs, or stairs were null. " + (prevStairs == null));

            targetLocation = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BEAST_DRAGON_DUNGEONSTART_FLOOR);
        }
        else
        {
            targetLocation = prevStairs.NewLocation;
        }

        TravelManager.TravelMaps(targetLocation, prevStairs, false);
    }

    public static IEnumerator BeatMonsterHordeLevel()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Monster vineToGrow = null;

        foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorfaction == Faction.PLAYER && m.actorRefName != "mon_xp_defensecrystal" && !GameMasterScript.heroPCActor.CheckSummon(m))
            {
                m.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
                GameMasterScript.AddToDeadQueue(m, true);
            }
            if (m.actorRefName == "mon_xp_defensecrystal" && m.myStats.IsAlive())
            {
                vineToGrow = m;
            }
        }

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        Stairs nextLevel = new Stairs();
        nextLevel.stairsUp = false;
        nextLevel.prefab = "MightyVine";
        nextLevel.pointsToFloor = MapMasterScript.activeMap.floor + 1;
        nextLevel.NewLocation = MapMasterScript.theDungeon.FindFloor(nextLevel.pointsToFloor);
        nextLevel.SetPos(vineToGrow.GetPos());

        MapMasterScript.activeMap.PlaceActor(nextLevel, MapMasterScript.GetTile(vineToGrow.GetPos()));

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(nextLevel.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.75f);

        UIManagerScript.PlayCursorSound("TimePassing");
        UIManagerScript.PlayCursorSound("PlantSeeds");
        GameMasterScript.cameraScript.AddScreenshake(0.4f);
        UIManagerScript.FlashWhite(0.75f);
        MapMasterScript.singletonMMS.SpawnStairs(nextLevel);

        vineToGrow.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
        vineToGrow.myMovable.FadeOutThenDie();

        int numChestRewards = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("numchestrewards");
        int numFountainRewards = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("numfountainrewards");
        for (int i = 0; i < numChestRewards + numFountainRewards; i++)
        {
            MapTileData tileToSpawn = MapMasterScript.activeMap.GetRandomEmptyTile(vineToGrow.GetPos(), 1, true, anyNonCollidable: true, preferLOS: true, avoidTilesWithPowerups: true, excludeCenterTile: true);
            string refToUse = ((i >= numChestRewards) ? "obj_regenfountain" : "obj_ornatechest");
            Destructible spawned = MapMasterScript.activeMap.CreateDestructibleInTile(tileToSpawn, refToUse);
            MapMasterScript.singletonMMS.SpawnDestructible(spawned);
        }

        yield return new WaitForSeconds(1.1f);

        DialogEventsScript.RecenterCameraOnPlayer("dummy");
        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        GameMasterScript.heroPCActor.SetActorData("hordeclear_" + MapMasterScript.activeMap.floor, 1);

    }

}
