using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class DialogEventsScript
{
    public static bool SlimeDungeon_Tutorial_Part1(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.SlimeDungeon_Tutorial_Part1());

        return false;
    }

    public static bool FinishSlimeDungeonTutorial(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.FinishSlimeDungeonTutorial());

        return false;
    }

    public static bool BeginSlimeDragonBossFight(string value)
    {
        StartEncounterWithBoss("mon_jellydragon", TDProgress.DRAGON_JELLY, 3);
        return false;
    }
}

public class SlimeDragonStuff
{

    public static void CheckForSlimeDungeonUnlockInJellyGrotto()
    {
        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO) >= 1)
        {
            return;
        }
        if (ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) < 1)
        {
            return;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }


        List<Actor> actorsToRemove = new List<Actor>();

        foreach(Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.actorRefName == "npc_farmergrotto" || act.actorRefName == "npc_pinkslimepet")
            {
                actorsToRemove.Add(act);
            }
        }

        foreach(Actor act in actorsToRemove)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(act);
            act.myMovable.FadeOutThenDie();
        }

        ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO, 1);

        GameMasterScript.gmsSingleton.StartCoroutine(SlimeDragonStuff.SlimeDungeonUnlockIntro());        
    }

    public static IEnumerator SlimeDungeonUnlockIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Stairs slimeDungeonStairs = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR);
        slimeDungeonStairs.prefab = "EarthStairsUp";
        MapMasterScript.singletonMMS.SpawnStairs(slimeDungeonStairs);

        CustomAlgorithms.RevealTilesAroundPoint(slimeDungeonStairs.GetPos(), 1, true);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.PlayCursorSound("CookingSuccess");

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        UIManagerScript.PlayCursorSound("Mirage");
        GameMasterScript.cameraScript.AddScreenshake(1.0f);

        yield return new WaitForSeconds(2.25f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(slimeDungeonStairs.GetPos(), 1.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(2f);

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.StartConversationByRef("slime_dungeon_unlocked", DialogType.STANDARD, null);
    }

    public static IEnumerator SlimeDungeonIntroCutscene()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        if (MapMasterScript.activeMap.FindActor("npc_jellyboo_introdragon") == null)
        {
            NPC boo = NPC.CreateNPC("npc_jellyboo_introdragon");

            MapTileData emptyTile = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true, true, false, true);

            MapMasterScript.activeMap.PlaceActor(boo, emptyTile);

            MapMasterScript.singletonMMS.SpawnNPC(boo);
        }

        yield return new WaitForSeconds(1f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(1f);

        ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO, 2);
        UIManagerScript.StartConversationByRef("slimedragon_jellyboo_intro", DialogType.STANDARD, null);        
    }

    public static IEnumerator SlimeDungeonFinalAreaIntroCutscene()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(0.5f);

        ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.HERO, 3);
        UIManagerScript.StartConversationByRef("slimedungeon_finalarea_intro", DialogType.STANDARD, null);
    }

    public static IEnumerator SlimeDungeon_Tutorial_Part1()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(1f);

        Map_SlimeDungeon mSlime = MapMasterScript.activeMap as Map_SlimeDungeon;

        MapTileData mtd = mSlime.GetClosestTowerOfOpposingFaction(MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos()), Map_SlimeDungeon.SlimeStatus.Friendly);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(mtd.pos, 1.2f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(2.5f);

        UIManagerScript.StartConversationByRef("slimedungeon_tutorial_part1", DialogType.KEYSTORY, null);

    }

    public static IEnumerator FinishSlimeDungeonTutorial()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.5f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(GameMasterScript.heroPCActor.GetPos(), 0.75f, false);

        yield return new WaitForSeconds(0.75f);

        Actor jellySlime = MapMasterScript.activeMap.FindActor("npc_jellyboo_introdragon");

        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "WallJump", null, true);

        TDAnimationScripts.JumpActorToTargetPoint(jellySlime, GameMasterScript.heroPCActor.GetPos(), 0.8f, 360f, true);

        yield return new WaitForSeconds(1f);

        MapMasterScript.activeMap.RemoveActorFromMap(jellySlime);
        jellySlime.myMovable.FadeOutThenDie();

        yield return new WaitForSeconds(0.2f);

        GameMasterScript.SetAnimationPlaying(false, false);
    }

    public static IEnumerator WonCurrentMap()
    {
        if (MapMasterScript.activeMap.floor == MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR)
        {
            yield break;
        }

        GameMasterScript.heroPCActor.SetActorData("slimevic" + MapMasterScript.activeMap.floor, 1); // marks us as having defeated this map.

        GameMasterScript.SetAnimationPlaying(true, true);

        // Remove all the slimes from the map, except lieutenants.

        float time = Time.realtimeSinceStartup;

        RemoveAllSlimesFromMap();

        if (Time.realtimeSinceStartup - time >= GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            time = Time.realtimeSinceStartup;
        }

		MusicManagerScript.RequestPlayMusic("BossVictory",true);


        yield return new WaitForSeconds(1f);


        // Now create some stairs to the next level IF none exist.
        Stairs st = null;
        bool spawnStairs = false;
        foreach(Stairs checkStairs in MapMasterScript.activeMap.mapStairs)
        {
            if (!checkStairs.stairsUp)
            {
                st = checkStairs;
                break;
            }
        }
        if (st == null)
        {
            /* Actor centerActor = MapMasterScript.activeMap.FindActor("obj_slimemetalgate");
            if (centerActor == null)
            {
                centerActor = GameMasterScript.heroPCActor;
            } */
            MapTileData mtd = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true, true, true, true);
            st = MapMasterScript.activeMap.SpawnStairs(false, MapMasterScript.activeMap.floor + 1, mtd);
            MapMasterScript.singletonMMS.SpawnStairs(st);
        }
        
        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(st.GetPos(), 1f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.5f);
        
        if (spawnStairs)
        {
            MapMasterScript.singletonMMS.SpawnStairs(st);
        }
        else
        {
            st.EnableActor();
        }
        
        CombatManagerScript.GenerateSpecificEffectAnimation(st.GetPos(), "SmokePoof", null, true);

        UIManagerScript.PlayCursorSound("CookingSuccess");

        yield return new WaitForSeconds(1f);

        int numKeys = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("goldkeys");
        for (int i = 0; i < numKeys; i++)
        {
            Item key = LootGeneratorScript.CreateItemFromTemplateRef("item_slimegoldkey", 1f, 0f, false);
            MapTileData emptyTile = MapMasterScript.activeMap.GetRandomEmptyTile(st.GetPos(), 1, true, true, true, false, true);
            MapMasterScript.activeMap.PlaceActor(key, emptyTile);
            MapMasterScript.singletonMMS.SpawnItem(key);
            CombatManagerScript.GenerateSpecificEffectAnimation(emptyTile.pos, "SmokePoof", null, true);
        }

        yield return new WaitForSeconds(1.5f);

        UIManagerScript.StartConversationByRef("slimedungeon_victory", DialogType.KEYSTORY, null);                
    }

    public static IEnumerator FailedCurrentMap()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        MusicManagerScript.singleton.Fadeout(1f);

        yield return new WaitForSeconds(1f);

        UIManagerScript.PlayCursorSound("Failure");
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);
        GameMasterScript.cameraScript.AddScreenshake(0.5f);

        yield return new WaitForSeconds(1.5f);

        BattleTextManager.NewText(StringManager.GetString("popup_slimefail"), GameMasterScript.heroPCActor.GetObject(), Color.red, 2f);
        GameLogScript.LogWriteStringRef("log_slimefail");

        yield return new WaitForSeconds(2f);

        GameMasterScript.gmsSingleton.SetTempGameData("slimefail", 1);
        

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

        float time = Time.realtimeSinceStartup;

        MapMasterScript.CreateMap(MapMasterScript.activeMap.floor);

        GameMasterScript.heroPCActor.SetActorData("num_lieutenants", 0);
        GameMasterScript.heroPCActor.SetActorData("enemylieutenant_turns", 0);

        if (Time.realtimeSinceStartup - time >= GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            time = Time.realtimeSinceStartup;
        }

        DungeonGenerationAlgorithms.ConnectSeriesOfMapsPostGeneration(MapMasterScript.JELLY_DRAGON_DUNGEONSTART_FLOOR, 4, EDLCPackages.EXPANSION2);

        if (Time.realtimeSinceStartup - time >= GameMasterScript.MIN_FPS_DURING_LOAD)
        {
            yield return null;
            time = Time.realtimeSinceStartup;
        }

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;

        if (ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) > 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO) < 4)
        {
            ProgressTracker.SetProgress(TDProgress.DRAGON_JELLY, ProgressLocations.HERO, 1);
        }

        if (prevStairs == null)
        {
            // Go all the way back to jelly grotto.
            TravelManager.TravelMaps(MapMasterScript.theDungeon.FindFloor(MapMasterScript.JELLY_GROTTO), null, false);
        }
        else
        {
            TravelManager.TravelMaps(prevStairs.NewLocation, prevStairs, false);
        }        
    }

    public static IEnumerator SlimeDragonArrives()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        MusicManagerScript.singleton.FadeoutThenSetAllToZero(1f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(0.75f);

        MapTileData spawnPosition = MapMasterScript.GetTile(new Vector2(10f, 14f));

        if (!spawnPosition.IsEmpty())
        {
            spawnPosition = MapMasterScript.activeMap.GetRandomEmptyTile(spawnPosition.pos, 1, true, true, true, false);
        }

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spawnPosition.pos, 0.75f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        UIManagerScript.PlayCursorSound("Earthquake");
        GameMasterScript.cameraScript.AddScreenshake(0.75f);

        yield return new WaitForSeconds(0.8f);

        int numMudSplashes = 14;
        

        for (int i = 0; i < numMudSplashes; i++)
        {
            yield return new WaitForSeconds(0.1f);
            float xPos = spawnPosition.pos.x + UnityEngine.Random.Range(-1.2f, 1.2f);
            float yPos = spawnPosition.pos.y + UnityEngine.Random.Range(-1.2f, 1.2f);
            CombatManagerScript.GenerateSpecificEffectAnimation(new Vector2(xPos, yPos), "EnterMudSplash", null, true);            
        }

        Monster slimeDragon = MonsterManagerScript.CreateMonster("mon_jellydragon", true, true, false, 0f, false);
        MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, slimeDragon, false);
        MapMasterScript.activeMap.PlaceActor(slimeDragon, spawnPosition);
        MapMasterScript.singletonMMS.SpawnMonster(slimeDragon, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(slimeDragon.GetPos(), "LightningStrikeEffectBig", null, true);
        GameMasterScript.cameraScript.AddScreenshake(0.5f);

        yield return new WaitForSeconds(0.75f);

        CombatManagerScript.GenerateSpecificEffectAnimation(slimeDragon.GetPos(), "DeepSoundEmanation", null, true);        

        yield return new WaitForSeconds(1.4f);

        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchNoCrossfade("dragonboss");
        //MusicManagerScript.RequestPlayMusic("dragonboss",true);


        MapMasterScript.activeMap.musicCurrentlyPlaying = "dragonboss";

        GameMasterScript.SetAnimationPlaying(false);
        UIManagerScript.StartConversationByRef("slimedungeon_dragon_arrives", DialogType.KEYSTORY, null);
        
    }

    public static IEnumerator SlimeDragonDefeated()
    {
        GameMasterScript.heroPCActor.SetActorData("slimevic" + MapMasterScript.activeMap.floor, 1); // marks us as having defeated this map.

        GameMasterScript.SetAnimationPlaying(true, true);

        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss";

        MusicManagerScript.singleton.FadeoutThenSetAllToZero(0.5f);

        Monster slimeDragon = MapMasterScript.activeMap.FindActor("mon_jellydragon") as Monster;

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(slimeDragon.GetPos(), 2.0f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(0.5f);

        RemoveAllSlimesFromMap();

        // Play buildup of smoke and FX...

        int numPoofs = 21;
        float totalWaitTime = 3.5f;
        Vector2 centerPos = slimeDragon.GetPos();

        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(slimeDragon.GetPos(), "ChargingSkillParticles", null, false);
        particles.transform.SetParent(slimeDragon.GetObject().transform);
        particles.transform.localPosition = Vector3.zero;

        for (int i = 0; i <= numPoofs; i++)
        {
            centerPos = slimeDragon.GetPos();
            centerPos.x += UnityEngine.Random.Range(-0.45f, 0.45f);
            centerPos.y += UnityEngine.Random.Range(-0.2f, 0.4f);
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                if (UnityEngine.Random.Range(0,2) == 0)
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "SoundEmanation", null, true);
                }
                else
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "AcidSplash", null, true);
                }
                
            }
            else
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "EnterMudSplash", null, true);
            }

            if (i % 6 == 0)
            {
                GameMasterScript.cameraScript.AddScreenshake(0.3f);
                slimeDragon.myAnimatable.SetAnim("Idle");
            }

            if (i % 4 == 0)
            {
                slimeDragon.myAnimatable.SetAnim("TakeDamage");
            }

            yield return new WaitForSeconds(totalWaitTime / numPoofs);
        }

        CombatManagerScript.GenerateSpecificEffectAnimation(centerPos, "AcidExplosion3x", null, true);

        UIManagerScript.FlashWhite(1f);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);

        slimeDragon.myMovable.FadeOutThenDie();
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitToDestroyActorObject(slimeDragon, slimeDragon.GetObject(), 0.1f));


        GameMasterScript.gmsSingleton.statsAndAchievements.DLC2_Beat_Jelly_Dragon();


        yield return new WaitForSeconds(1.3f);

        DialogEventsScript.RecenterCameraOnPlayer("");
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

		MusicManagerScript.RequestPlayMusic("BossVictory",true);


        yield return new WaitForSeconds(1.05f);

        BattleTextManager.NewText(StringManager.GetString("exp_popup_dragondefeated"), GameMasterScript.heroPCActor.GetObject(), Color.green, 0.5f);
        GameMasterScript.cameraScript.AddScreenshake(0.4f);

        //UIManagerScript.PlayCursorSound("StoneMovement");
        //BanditDragonStuff.UnlockGatesOfIndex(999, MapMasterScript.activeMap, false);

        yield return new WaitForSeconds(0.45f);

        NPC pinkSlime = NPC.CreateNPC("npc_jellyboo_beatdragon");
        MapTileData nearHero = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true, true, true, true);
        MapMasterScript.activeMap.PlaceActor(pinkSlime, MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos()));

        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        MapMasterScript.singletonMMS.SpawnNPC(pinkSlime);

        TDAnimationScripts.JumpActorToTargetPoint(pinkSlime, nearHero.pos, 0.5f, 360f, true);

        yield return new WaitForSeconds(0.51f);

        MapMasterScript.activeMap.MoveActor(GameMasterScript.heroPCActor.GetPos(), nearHero.pos, pinkSlime);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "WallJump", null, true);

        yield return new WaitForSeconds(0.75f);

        UIManagerScript.StartConversationByRef("slimedragon_victory", DialogType.KEYSTORY, pinkSlime);
    }

    static void RemoveAllSlimesFromMap()
    {
        List<Actor> lAct = new List<Actor>();

        foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorRefName.Contains("_slime"))
            {
                lAct.Add(m);
            }
        }

        foreach (Actor a in lAct)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(a);
            GameMasterScript.AddToDeadQueue(a, true);
            a.myMovable.FadeOutThenDie();
        }

        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
    }
}
