using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All cutscenes and events related to the Robot Dragon.

public partial class DialogEventsScript
{
    public static bool BeginRobotDragonBossFight(string value)
    {
        Actor hologram = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("hologram_id"));

        CombatManagerScript.GenerateSpecificEffectAnimation(hologram.GetPos(), "TeleportUp", null, true);

        UIManagerScript.FlashWhite(0.6f);

        MapMasterScript.activeMap.RemoveActorFromMap(hologram);
        hologram.myMovable.FadeOutThenDie();        

        StartEncounterWithBoss("mon_robotdragon", TDProgress.DRAGON_ROBOT, 1);

		MusicManagerScript.RequestPlayMusic("finalboss_phase2", true);

        
        MapMasterScript.activeMap.musicCurrentlyPlaying = "finalboss_phase2";        

        return false;
    }

    public static bool FinalDragonCalloutInterrupted(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.RobotDungeonRumbleInTown());
        return false;
    }

    public static bool RobotDragonPrefight_Part1(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.RobotScanEffects());
        return false;
    }

    public static bool RobotDragonPrefight_Part2(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.HologramAppears());
        return false;
    }

    public static bool FinishRobotDragonDefeat(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.FinishRobotDragonDefeat());
        return false;
    }
}

public class RobotDragonStuff
{

    public static IEnumerator RobotDragonDefeated()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.musicManager.Fadeout(0.5f);

        yield return new WaitForSeconds(0.5f);

        //GameMasterScript.musicManager.Play(true, true);

        //Monster robotDragon = MapMasterScript.activeMap.FindActor("mon_robotdragon") as Monster;

        GameObject robotDragonObj = GameMasterScript.gmsSingleton.ReadTempGameObject("robotdragonobj");

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(robotDragonObj.transform.position, 2.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameObject obj = CombatManagerScript.GenerateSpecificEffectAnimation(robotDragonObj.transform.position, "ChargingSkillParticles", null);

        GameMasterScript.gmsSingleton.SetTempGameObject("dragon_particles", obj);
        UIManagerScript.PlayCursorSound("LaserEnergyBuildup");

        yield return new WaitForSeconds(0.5f);
        

        int numExplosion = 22;

        for (int i = 0; i < numExplosion; i++)
        {
            Vector2 pos = robotDragonObj.transform.position;
            pos.x += UnityEngine.Random.Range(-0.6f, 0.6f);
            pos.y += UnityEngine.Random.Range(-0.4f, 0.6f);
            CombatManagerScript.GenerateSpecificEffectAnimation(pos, "SmallExplosionEffect", null, true);
            yield return new WaitForSeconds(0.15f);

            if (i % 3 == 0)
            {
                GameMasterScript.cameraScript.AddScreenshake(0.3f);
            }
            if (i % 6 == 0)
            {
                UIManagerScript.FlashWhite(0.45f);
            }
        }

        yield return new WaitForSeconds(1.5f);

        //GameMasterScript.musicManager.LoadMusicByName("titlescreen", true);

        /* CombatManagerScript.GenerateSpecificEffectAnimation(robotDragon.GetPos(), "BigExplosionEffect", null, true);
        UIManagerScript.PlayCursorSound("Earthquake");

        robotDragon.myMovable.FadeOutThenDie();
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(robotDragon, robotDragon.GetObject(), 0.1f)); */

        //yield return new WaitForSeconds(0.75f);

        float timeStart = Time.realtimeSinceStartup;

        MapTileData mtd = null;
        bool found = false;
        int attempts = 0;
        while (!found)
        {
            if (attempts > 500)
            {
                break;
            }

            mtd = MapMasterScript.activeMap.GetRandomEmptyTileForMapGen();

            if (MapMasterScript.GetGridDistance(mtd.pos, GameMasterScript.heroPCActor.GetPos()) >= 2)
            {
                if (MapMasterScript.GetGridDistance(mtd.pos, GameMasterScript.heroPCActor.GetPos()) <= 5)
                {
                    found = true;
                    break;
                }
            }

            if (Time.realtimeSinceStartup - timeStart >= (GameMasterScript.MIN_FPS_DURING_LOAD * 2f))
            {
                yield return null;
                timeStart = Time.realtimeSinceStartup;
            }
        }        
        

        NPC hologram = NPC.CreateNPC("npc_hologram");
        MapMasterScript.activeMap.PlaceActor(hologram, mtd);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(mtd.pos, 1f, false);

        yield return new WaitForSeconds(1f);

        GameMasterScript.gmsSingleton.SetTempGameData("hologram_id", hologram.actorUniqueID);
        CombatManagerScript.GenerateSpecificEffectAnimation(mtd.pos, "TeleportDown", null, true);
        MapMasterScript.singletonMMS.SpawnNPC(hologram);
        
        yield return new WaitForSeconds(2.5f);
        
        UIManagerScript.StartConversationByRef("robotdragon_defeat", DialogType.KEYSTORY, null);

        /* robotDragon.myMovable.FadeOutThenDie();
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(robotDragon, robotDragon.GetObject(), 0.1f));

        yield return new WaitForSeconds(1.3f);

        DialogEventsScript.RecenterCameraOnPlayer("");
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false; */
    }

    public static IEnumerator FinishRobotDragonDefeat()
    {
        MusicManagerScript.singleton.Fadeout(0.5f);

        //Monster robotDragon = MapMasterScript.activeMap.FindActor("mon_robotdragon") as Monster;

        GameObject robotDragonObj = GameMasterScript.gmsSingleton.ReadTempGameObject("robotdragonobj");
        
        yield return new WaitForSeconds(0.25f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(robotDragonObj.transform.position, 0.75f, false);
        UIManagerScript.PlayCursorSound("SupervisorSound");

        yield return new WaitForSeconds(0.5f);        

        UIManagerScript.FlashWhite(0.5f);
        UIManagerScript.PlayCursorSound("SupervisorSound");

        yield return new WaitForSeconds(0.25f);

        UIManagerScript.PlayCursorSound("SupervisorSound");

        yield return new WaitForSeconds(0.25f);

        UIManagerScript.PlayCursorSound("SupervisorSound");

        int strikes = 15;

        for (int i = 0; i < strikes; i++)
        {
            Vector2 pos = robotDragonObj.transform.position;
            pos.x += UnityEngine.Random.Range(-0.8f, 0.8f);
            pos.y += UnityEngine.Random.Range(-0.8f, 0.8f);
            CombatManagerScript.GenerateSpecificEffectAnimation(pos, "LightningStrikeEffectBig", null, true);            
            if (i % 5 == 0)
            {
                UIManagerScript.PlayCursorSound("SupervisorSound");
                CombatManagerScript.GenerateSpecificEffectAnimation(robotDragonObj.transform.position, "BigExplosionEffect", null, true);
            }
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.1f);

        GameObject particles = GameMasterScript.gmsSingleton.ReadTempGameObject("dragon_particles");

        GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));

        Actor hologram = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("hologram_id"));

        hologram.myMovable.FadeOutThenDie();

        CombatManagerScript.GenerateSpecificEffectAnimation(robotDragonObj.transform.position, "BigExplosionEffect", null, true);

        yield return new WaitForSeconds(0.2f);
        UIManagerScript.PlayCursorSound("Earthquake");
        GameMasterScript.cameraScript.AddScreenshake(2f);
        UIManagerScript.FlashWhite(1.5f);
        robotDragonObj.GetComponent<Movable>().FadeOutThenDie();


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Robot_Dragon();

        if (GameStartData.NewGamePlus >= 1)
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Robot_Dragon_NGPlus();
        }
        if (GameStartData.NewGamePlus >= 2)
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Robot_Dragon_Savage();
        }


        yield return new WaitForSeconds(3f);

        DialogEventsScript.RecenterCameraOnPlayer("");
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false; 

		MusicManagerScript.RequestPlayMusic("BossVictory", true);


    }

    public static IEnumerator RobotDragonIntro()
    {
        BossHealthBarScript.DisableBoss();
        Monster robotDragon = MapMasterScript.activeMap.FindActor("mon_robotdragon") as Monster;
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(1.2f);

        // Pan to Big Boy

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(robotDragon.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.0f);

        // Linger

        ///CombatManagerScript.GenerateSpecificEffectAnimation(spiritDragon.GetPos(), "SndMirageEffect", null, true);
        UIManagerScript.PlayCursorSound("SupervisorSound");

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("robot_dragon_intro", DialogType.STANDARD, null);
        ProgressTracker.SetProgress(TDProgress.DRAGON_ROBOT, ProgressLocations.HERO, 1);
    }

    /// <summary>
    /// Starts the quest that unlocks the robot dungeon
    /// </summary>
    /// <returns></returns>
    public static IEnumerator RobotDragonKickoff()
    {
        ProgressTracker.SetProgress(TDProgress.DRAGON_ROBOT_KICKOFF, ProgressLocations.HERO, 1);

        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(0.6f);

        NPC froggo = MapMasterScript.activeMap.FindActor("npc_babyfrogking") as NPC;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(froggo.GetPos(), 1.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.6f);

        UIManagerScript.StartConversationByRef("final_dragon_callout_part1", DialogType.KEYSTORY, null);
    }

    public static IEnumerator RobotDungeonRumbleInTown()
    {
        GameMasterScript.gmsSingleton.SetTempStringData("levelname", MapMasterScript.theDungeon.FindFloor(151).GetName());

        MusicManagerScript.singleton.FadeoutThenSetAllToZero(0.5f);

        NPC mir = MapMasterScript.activeMap.FindActor("npc_tinkerer") as NPC;
        NPC frog = MapMasterScript.activeMap.FindActor("npc_babyfrogking") as NPC;

        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(1f);

        UIManagerScript.PlayCursorSound("StoneMovement");
        GameMasterScript.cameraScript.AddScreenshake(0.5f);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "AggroEffect", null);
        CombatManagerScript.GenerateSpecificEffectAnimation(mir.GetPos(), "AggroEffect", null);
        CombatManagerScript.GenerateSpecificEffectAnimation(frog.GetPos(), "AggroEffect", null);

        yield return new WaitForSeconds(1f);

        //UIManagerScript.PlayCursorSound("StoneMovement");
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FireBreathSFX", null, true, 0f);
        GameMasterScript.cameraScript.AddScreenshake(1f);

        yield return new WaitForSeconds(0.25f);
        UIManagerScript.PlayCursorSound("StoneMovement");
        yield return new WaitForSeconds(1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "AggroEffect", null);
        CombatManagerScript.GenerateSpecificEffectAnimation(mir.GetPos(), "AggroEffect", null);
        CombatManagerScript.GenerateSpecificEffectAnimation(frog.GetPos(), "AggroEffect", null);
        UIManagerScript.PlayCursorSound("Earthquake");

        GameMasterScript.cameraScript.AddScreenshake(1f);

        yield return new WaitForSeconds(1.2f);

        int numStrikes = 12;

        for (int i = 0; i < numStrikes; i++)
        {
            yield return new WaitForSeconds(0.125f);
            Vector2 spawnPos = GameMasterScript.heroPCActor.GetPos();
            spawnPos.x += UnityEngine.Random.Range(-6f, 6f);
            spawnPos.y += UnityEngine.Random.Range(-6f, 6f);
            CombatManagerScript.GenerateSpecificEffectAnimation(spawnPos, "LightningStrikeEffect", null, true);
        }

        yield return new WaitForSeconds(0.75f);

		MusicManagerScript.RequestPlayMusic("dragondread",true);


        UIManagerScript.StartConversationByRef("final_dragon_callout_part2", DialogType.KEYSTORY, null);
    }

    public static void CheckForRobotDungeonUnlockInGuardianRuins()
    {

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_ROBOT_KICKOFF, ProgressLocations.HERO) != 1 ||
            ProgressTracker.CheckProgress(TDProgress.DRAGON_ROBOT_DUNGEON, ProgressLocations.HERO) == 1)
        {
            return;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        ProgressTracker.SetProgress(TDProgress.DRAGON_ROBOT_DUNGEON, ProgressLocations.HERO, 1);

        GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.RobotDungeonUnlockIntro());
    }

    static IEnumerator RobotDungeonUnlockIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Stairs robotDungeonStairs = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.ROBOT_DRAGON_DUNGEONSTART_FLOOR);
        robotDungeonStairs.prefab = "FutureStairsUp";
        MapMasterScript.singletonMMS.SpawnStairs(robotDungeonStairs);

        CustomAlgorithms.RevealTilesAroundPoint(robotDungeonStairs.GetPos(), 1, true);
        
        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("SupervisorSound");

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        UIManagerScript.PlayCursorSound("Earthquake");
        GameMasterScript.cameraScript.AddScreenshake(1.0f);

        yield return new WaitForSeconds(2.25f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(robotDungeonStairs.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(2f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("robot_dungeon_unlocked", DialogType.STANDARD, null);
    }

    public static IEnumerator RobotScanEffects()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Actor robotDragon = MapMasterScript.activeMap.FindActor("mon_robotdragon");

        yield return new WaitForSeconds(0.25f);

        UIManagerScript.PlayCursorSound("SupervisorSound");

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(GameMasterScript.heroPCActor.GetPos(), 0.75f, false);

        yield return new WaitForSeconds(0.75f);

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.GenerateSpecificEffectAnimation(robotDragon.GetPos(), "RobotScanRayEffect", null, true);

        yield return new WaitForSeconds(1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirDebuff", null, false, 0f, true);

        yield return new WaitForSeconds(1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirBuffSilent", null, false, 0f, true);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("robot_dragon_intro_2", DialogType.KEYSTORY, null);

    }

    public static IEnumerator HologramAppears()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Actor robotDragon = MapMasterScript.activeMap.FindActor("mon_robotdragon");

        Vector2 hologramSpawnPos = Vector2.zero;

        if (robotDragon == null)
        {
            hologramSpawnPos = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 4, false, true, true, true, true).pos;
        }
        else
        {
            hologramSpawnPos = robotDragon.GetPos();
            hologramSpawnPos.y -= 2f;
        }
                
        MapTileData spawnMTD = MapMasterScript.activeMap.GetTile(hologramSpawnPos);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("RobotScan");
        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(spawnMTD.pos, "ChargingSkillParticles", null, false);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spawnMTD.pos, 1f, false);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.5f);

        UIManagerScript.FlashWhite(1f);

        NPC hologram = NPC.CreateNPC("npc_hologram");
        MapMasterScript.activeMap.PlaceActor(hologram, spawnMTD);

        GameMasterScript.gmsSingleton.SetTempGameData("hologram_id", hologram.actorUniqueID);

        UIManagerScript.PlayCursorSound("Mirage");

        GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.GenerateSpecificEffectAnimation(hologramSpawnPos, "TeleportDown", null, true);
        MapMasterScript.singletonMMS.SpawnNPC(hologram);

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "AggroEffect", null);

        yield return new WaitForSeconds(3.5f);

        UIManagerScript.StartConversationByRef("robot_dragon_intro_3", DialogType.KEYSTORY, null);

    }
}
