using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All cutscenes and events related to the Frog Dragon.

public partial class DialogEventsScript
{
    public static bool BeginFrogDragonBossFight(string value)
    {
        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dragonboss");
        StartEncounterWithBoss("mon_frogdragon", TDProgress.DRAGON_FROG, 1);

        return false;
    }
}

public partial class Cutscenes : MonoBehaviour
{

    public static IEnumerator FinishFrogDragonIntroQuestCutscene()
    {
        //GameMasterScript.musicManager.FadeoutThenSetAllToZero(0.5f);
        GameMasterScript.SetAnimationPlaying(true, true);        

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        
        DialogEventsScript.RecenterCameraOnPlayer("dummy");
        yield return new WaitForSeconds(0.55f);

        ProgressTracker.SetProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META, 3);

        if (!GameMasterScript.heroPCActor.CheckIfMapCleared(MapMasterScript.theDungeon.FindFloor(MapMasterScript.MAP_FROG_BOG)))
        {
            Map frogBog = MapMasterScript.theDungeon.FindFloor(MapMasterScript.MAP_FROG_BOG);
            GameMasterScript.gmsSingleton.SetTempStringData("bogname", frogBog.GetName());
            GameMasterScript.gmsSingleton.SetTempStringData("frogbognearby", frogBog.GetNearbyPathFloor());
            UIManagerScript.StartConversationByRef("xp2_find_frogbog", DialogType.STANDARD, null);
        }

        GameMasterScript.SetAnimationPlaying(false);

        yield return null;
    }

    public static IEnumerator FinishFrogDragonVictory()
    {
        GameMasterScript.SetAnimationPlaying(true);

        Actor babyFrog = MapMasterScript.activeMap.FindActor("npc_babyfrogking");

        yield return new WaitForSeconds(0.4f);

        CombatManagerScript.GenerateSpecificEffectAnimation(babyFrog.GetPos(), "TeleportUp", null, true);
        GameMasterScript.cameraScript.AddScreenshake(0.33f);
        UIManagerScript.PlayCursorSound("Mirage");
        babyFrog.myMovable.FadeOutThenDie();
        MapMasterScript.activeMap.RemoveActorFromMap(babyFrog);

        yield return new WaitForSeconds(1f);

        DialogEventsScript.RecenterCameraOnPlayer("dummy");



        //GameMasterScript.SetAnimationPlaying(false);
        
    }

}

public class FrogDragonStuff
{
    const int MIN_LEVEL_FOR_QUEST_KICKOFF = 12;

    public static void DoFrogBogUnlockIntro()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(FrogDragonStuff.FrogBogUnlockIntro());
    }

    // Runs after Frog Bog has been cleared, and we have unlocked the Dragon quest line.
    // Spawn an entrance to the Frog Dungeon (Frogmire) with cutscene here.
    static IEnumerator FrogBogUnlockIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Stairs frogDungeonStairs = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.FROG_DRAGON_DUNGEONSTART_FLOOR);
        frogDungeonStairs.prefab = "EarthStairsUp";
        MapMasterScript.singletonMMS.SpawnStairs(frogDungeonStairs);

        CustomAlgorithms.RevealTilesAroundPoint(frogDungeonStairs.GetPos(), 1, true);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("StoneMovement");
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);        

        yield return new WaitForSeconds(0.75f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(frogDungeonStairs.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(2f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("frog_dungeon_unlocked", DialogType.STANDARD, null);
    }

    public static bool FrogDragonQuestCalloutPossible()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
        {
            return false;
        }
        if (GameMasterScript.heroPCActor.myStats.GetLevel() < MIN_LEVEL_FOR_QUEST_KICKOFF)
        {
            return false;
        }
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META) < 1)
        {
            return true;
        }

        return false;
    }

    // Pan to Langdon
    // Start the Frog Dragon dialog / quest line
    public static void DoFrogDragonQuestCallout()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(FrogDragonQuestCallout());
    }

    static IEnumerator FrogDragonQuestCallout()
    {
        Map frogBog = MapMasterScript.theDungeon.FindFloor(MapMasterScript.MAP_FROG_BOG);
        frogBog.SetMapVisibility(true); // Make sure frog bog is accessible, as quest step 2 goes here.

        Map beastlake = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BEASTLAKE_SIDEAREA);
        beastlake.SetMapVisibility(true); // beast dragon needs to be accessible here

        GameMasterScript.SetAnimationPlaying(true, true);       

        yield return new WaitForSeconds(0.5f);

        NPC langdon = MapMasterScript.activeMap.FindActor("npc_farmer") as NPC;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(langdon.GetPos(), 1f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.3f);

        MusicManagerScript.singleton.Fadeout(0.5f);
                
        UIManagerScript.PlayCursorSound("Earthquake");
        GameMasterScript.cameraScript.AddScreenshake(1.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", langdon, Directions.NORTHEAST, false);        

        yield return new WaitForSeconds(1.0f);

        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dragondread");


        //GameMasterScript.musicManager.LoadMusicByName("villainous_intro", true, true); // #todo - Unique dragon mystery cue?

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        CombatManagerScript.SpawnChildSprite("AggroEffect", langdon, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(1.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", langdon, Directions.NORTHEAST, false);
        CombatManagerScript.GenerateSpecificEffectAnimation(new Vector2(99f, 99f), "DeepSoundEmanation", null, true);

        yield return new WaitForSeconds(1.5f);

        UIManagerScript.StartConversationByRef("langdon_start_dragonquest", DialogType.STANDARD, langdon);
    }

    public static IEnumerator FrogDragonDefeated()
    {
        Monster frogDragon = MapMasterScript.activeMap.FindActor("mon_frogdragon") as Monster;
        MusicManagerScript.singleton.Fadeout(1f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(frogDragon.GetPos(), 2.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);        

        float totalWaitTime = 3.5f;
        int numPoofs = 21;

        Vector2 centerPos = frogDragon.GetPos();

        // Play buildup of smoke and FX...

        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(frogDragon.GetPos(), "ChargingSkillParticles", null, false);
        particles.transform.SetParent(frogDragon.GetObject().transform);
        particles.transform.localPosition = Vector3.zero;

        for (int i = 0; i <= numPoofs; i++)
        {
            centerPos = frogDragon.GetPos();
            centerPos.x += UnityEngine.Random.Range(-0.6f, 0.6f);
            centerPos.y += UnityEngine.Random.Range(-0.55f, 0.55f);
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "GreenSmokePoof", null, true);
            }
            else
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "AcidExplosion", null, true);
            }

            if (i % 6 == 0)
            {
                GameMasterScript.cameraScript.AddScreenshake(0.3f);
                frogDragon.myAnimatable.SetAnim("Idle");
            }

            if (i % 4 == 0)
            {
                frogDragon.myAnimatable.SetAnim("TakeDamage");
            }

            yield return new WaitForSeconds(totalWaitTime / numPoofs);
        }

        CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "BigExplosionEffect", null, true);

        yield return new WaitForSeconds(0.05f);

        Map jellyGrotto = MapMasterScript.theDungeon.FindFloor(MapMasterScript.JELLY_GROTTO);
        jellyGrotto.SetMapVisibility(true);

        CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "TeleportDown", null, true);

        // BOOM! Frog disappears, replaced with lil guy.

        UIManagerScript.FlashWhite(1.25f);
        GameMasterScript.cameraScript.AddScreenshake(1.0f);

        MapMasterScript.theDungeon.FindFloor(MapMasterScript.JELLY_GROTTO).SetMapVisibility(true);


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Frog_Dragon();


        // Find the anchor tile, which is one above the wall we want to destroy
        // Then destroy the wall by turning it into ground
        Actor anchorForWallBreak = MapMasterScript.activeMap.FindActor("stb");
        Vector2 posBelowAnchor = anchorForWallBreak.GetPos() - new Vector2(0f, 1f);
        MapTileData tileToDestroy = MapMasterScript.GetTile(posBelowAnchor);
        GameMasterScript.AddToDeadQueue(anchorForWallBreak, true);
        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
        tileToDestroy.ChangeTileType(TileTypes.GROUND);
        tileToDestroy.SetTileVisualType(VisualTileType.GROUND);
        tileToDestroy.UpdateCollidableState();
        tileToDestroy.UpdateVisionBlockingState();
        MapMasterScript.RebuildMapMesh();
        if (MinimapUIScript.GetOverlay())
        {
            MinimapUIScript.StopOverlay();
            MinimapUIScript.GenerateOverlay();
        }

        NPC miniFrog = NPC.CreateNPC("npc_babyfrogking");
        MapMasterScript.activeMap.PlaceActor(miniFrog, MapMasterScript.GetTile(frogDragon.GetPos()));
        MapMasterScript.singletonMMS.SpawnNPC(miniFrog);

        GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));
        frogDragon.myMovable.FadeOutThenDie();
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(frogDragon, frogDragon.GetObject(), 0.1f));

        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("funmerchant");

        MapMasterScript.activeMap.musicCurrentlyPlaying = "funmerchant";

        for (int i = 0; i < 6; i++)
        {
            miniFrog.myAnimatable.SetAnim("TakeDamage");
            yield return new WaitForSeconds(0.3f);
            miniFrog.myAnimatable.SetAnim("Idle");
            yield return new WaitForSeconds(0.25f);
        }

        ProgressTracker.SetProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META, 1);
        TDPlayerPrefs.SetInt(GlobalProgressKeys.DRAGON_DEFEAT, 1);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        GameMasterScript.gmsSingleton.SetTempGameData("frogdying", 0);

        UIManagerScript.StartConversationByRef("frog_dragon_defeated", DialogType.STANDARD, null);
    }

    public static IEnumerator FrogDragonIntro()
    {
        // Play intro cinematic for boss fight!
        BossHealthBarScript.DisableBoss();
        Monster frogDragon = MapMasterScript.activeMap.FindActor("mon_frogdragon") as Monster;
        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(1.2f);

        // Pan to Big Boy

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(frogDragon.GetPos(), 1.0f, false);

        yield return new WaitForSeconds(1.0f);

        // Linger
        CombatManagerScript.GenerateSpecificEffectAnimation(frogDragon.GetPos(), "DeepSoundEmanation", null, true);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("frog_dragon_intro", DialogType.STANDARD, null);
        ProgressTracker.SetProgress(TDProgress.DRAGON_FROG, ProgressLocations.HERO, 1);
    }

}
