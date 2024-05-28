using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class DLCCutscenes
{
    public static IEnumerator SharaBoss4Victory_Part5()
    {
        // robot is destroyed? nah
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.5f);

        /* Monster monFinalBoss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss") as Monster;

        // static e
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = monFinalBoss.GetPos();
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.2f, 0.4f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "StaticShockEffect", null, 0.01f + i * 0.3f, true);
        }

        yield return new WaitForSeconds(1.4f);

        CombatManagerScript.GenerateSpecificEffectAnimation(monFinalBoss.GetPos(), "BigExplosionEffect", null, true);
        monFinalBoss.myMovable.FadeOutThenDie();
        UIManagerScript.FlashWhite(1.0f);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);

        CombatManagerScript.GenerateSpecificEffectAnimation(monFinalBoss.GetPos(), "MassiveShatterParticles", null, false);
         */ 
        yield return new WaitForSeconds(1.5f);

        Cutscenes.BeginEndgameCutscene(sharaMode: true);
    }

    public static IEnumerator SharaBoss4Victory_Part4()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.5f);

        Actor dirtbeak = MapMasterScript.activeMap.FindActor("npc_friendly_dirtbeak");

        CombatManagerScript.GenerateSpecificEffectAnimation(dirtbeak.GetPos(), "SmokePoof", null, true);
        dirtbeak.myMovable.FadeOutThenDie();

        yield return new WaitForSeconds(1.5f);

        // pan to robot
        Monster monFinalBoss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss") as Monster;

        Vector2 destPos = Vector2.zero;

        if (monFinalBoss == null)
        {
            destPos.x = GameMasterScript.gmsSingleton.ReadTempFloatData("sharabossposx");
            destPos.y = GameMasterScript.gmsSingleton.ReadTempFloatData("sharabossposy");
        }
        else
        {
            destPos = monFinalBoss.GetPos();
        }

        GameMasterScript.cameraScript.SetCustomCameraAnimation(dirtbeak.GetPos(), destPos, 2f, false);

        yield return new WaitForSeconds(3.0f);

        // static e
        for (int i = 0; i < 7; i++)
        {
            Vector2 pos = destPos;
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.2f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "StaticShockEffect", null, 0.01f + i * 0.3f, true);
        }

        yield return new WaitForSeconds(2.0f);

        UIManagerScript.FlashWhite(0.5f);

        yield return new WaitForSeconds(0.5f);

        UIManagerScript.StartConversationByRef("dialog_shara_postboss4victory_robot", DialogType.KEYSTORY, null);

    }

    // Shara goes away, then camera pans to dirtbeak
    public static IEnumerator SharaBoss4Victory_Part3()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(0.3f);

        Actor dirtbeak = MapMasterScript.activeMap.FindActor("npc_friendly_dirtbeak");

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(dirtbeak.GetPos(), GameMasterScript.heroPCActor.GetPos(), 1f, false);

        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "ChargingSkillParticles", null, false);
        UIManagerScript.PlayCursorSound("Mirage");

        yield return new WaitForSeconds(1.0f);

        GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));
        GameMasterScript.heroPCActor.myMovable.SetInSightAndForceFade(false);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "TeleportUp", null, false, 0f, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "SmokePoof", null);

        yield return new WaitForSeconds(1.3f);

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), dirtbeak.GetPos(), 1.5f, false);

        yield return new WaitForSeconds(2.25f);

        UIManagerScript.StartConversationByRef("dialog_shara_postboss4victory_dirtbeak", DialogType.KEYSTORY, null);
    }

    // Dirtbeak shows up
    public static IEnumerator SharaBoss4Victory_Part2()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Monster monFinalBoss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss") as Monster;

        NPC newDirtbeak = NPC.CreateNPC("npc_friendly_dirtbeak");

        MapTileData dirtbeakTile = MapMasterScript.activeMap.GetRandomEmptyTileForMapGen();
        while (MapMasterScript.GetGridDistance(dirtbeakTile.pos, GameMasterScript.heroPCActor.GetPos()) <= 2 || dirtbeakTile.IsCollidable(GameMasterScript.heroPCActor))
        {
            dirtbeakTile = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 3, false, anyNonCollidable: false, preferLOS: true);
        }
        MapMasterScript.activeMap.PlaceActor(newDirtbeak, dirtbeakTile);

        Vector2 panPos = GameMasterScript.heroPCActor.GetPos();

        if (monFinalBoss != null)
        {
            panPos = monFinalBoss.GetPos();
        }

        GameMasterScript.cameraScript.SetCustomCameraAnimation(panPos, dirtbeakTile.pos, 1f, false);

        yield return new WaitForSeconds(1.5f);

        MapMasterScript.singletonMMS.SpawnNPC(newDirtbeak);
        CombatManagerScript.GenerateSpecificEffectAnimation(dirtbeakTile.pos, "SmokePoof", null, true);

        yield return new WaitForSeconds(2f);

        UIManagerScript.StartConversationByRef("dialog_shara_postboss4victory_convo", DialogType.KEYSTORY, null);
    }

    public static IEnumerator SharaBoss4Victory_Part1()
    {
        MusicManagerScript.singleton.Fadeout(1.0f);
        GameMasterScript.SetAnimationPlaying(true, true);

        GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Shara_Boss4Defeated();
        ProgressTracker.SetProgress(TDProgress.BOSS4, ProgressLocations.HERO, 1);

        //Add all enemy monsters and powerups and monsterspirits to the DeadQueue, also set monster health to 0.
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.actorfaction != Faction.PLAYER || act.actorRefName.Contains("powerup") || act.actorRefName == "monsterspirit")
            {
                GameMasterScript.AddToDeadQueue(act);

                var mn = act as Monster;
                if (mn != null)
                {
                    mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.ALL, true);
                }
            }
        }
        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);

        // Now the final boss sprite should still be alive...
        Monster monFinalBoss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss") as Monster;
        
        if (monFinalBoss == null)
        {
            Vector2 finalBossFallbackPos = new Vector2(8, 7);
            monFinalBoss = MonsterManagerScript.CreateMonster("mon_shara_finalboss", false, false, false, 0f, false);
            MapMasterScript.activeMap.PlaceActor(monFinalBoss, MapMasterScript.GetTile(finalBossFallbackPos));
            MapMasterScript.singletonMMS.SpawnMonster(monFinalBoss, true);
            monFinalBoss.surpressTraits = true; // don't want to double-trigger cutscene
            monFinalBoss.AddAttribute(MonsterAttributes.CANTACT, 100);
            monFinalBoss.moveRange = 0;
        }

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), monFinalBoss.GetPos(), 2f, false);

        monFinalBoss.myStats.RemoveAllTemporaryEffects();

        UIManagerScript.PlayCursorSound("Massive Buildup");

        for (int i = 0; i < 34; i++)
        {
            Vector2 pos = monFinalBoss.GetPos();
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.44f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "SmallExplosionEffect", null, 0.01f + i * 0.155f, false, 0f, true);
        }
        yield return new WaitForSeconds(1.8f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        GameMasterScript.cameraScript.AddScreenshake(0.5f);
        UIManagerScript.FlashWhite(0.5f);
        CombatManagerScript.GenerateSpecificEffectAnimation(monFinalBoss.GetPos(), "BigExplosionEffect", null, true);

        yield return new WaitForSeconds(1.8f);

        GameMasterScript.cameraScript.AddScreenshake(0.5f);
        UIManagerScript.FlashWhite(0.5f);
        CombatManagerScript.GenerateSpecificEffectAnimation(monFinalBoss.GetPos(), "BigExplosionEffect", null, true);

        yield return new WaitForSeconds(1.8f);

        GameMasterScript.cameraScript.AddScreenshake(0.5f);
        CombatManagerScript.GenerateSpecificEffectAnimation(monFinalBoss.GetPos(), "BigExplosionEffect", null, true);

        monFinalBoss.myAnimatable.SetAnim("ShutdownIdle");

        yield return new WaitForSeconds(1.0f);

        // static e
        for (int i = 0; i < 3; i++)
        {
            Vector2 pos = monFinalBoss.GetPos();
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.44f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "StaticShockEffect", null, 0.01f + i * 0.3f, true, 0f, true);
        }
        yield return new WaitForSeconds(1.8f);

		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("sadness");

        GameMasterScript.SetAnimationPlaying(false);
        UIManagerScript.StartConversationByRef("dialog_shara_boss4victory", DialogType.KEYSTORY, null);
    }

    public static IEnumerator SharaFinalBossIntro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.75f);

        Actor finalBoss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss");

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), finalBoss.GetPos(), 2f, false);

        yield return new WaitForSeconds(3f);

        GameMasterScript.SetAnimationPlaying(false);
        UIManagerScript.StartConversationByRef("dialog_shara_boss4intro", DialogType.KEYSTORY, null);
    }

    public static void SharaBoss3Intro()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        BossHealthBarScript.DisableBoss();

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss3Intro_Part1());
    }

    public static IEnumerator SharaBoss3Intro_Part1()
    {
        // Health bars look uggo for this cutscene
        if (PlayerOptions.monsterHealthBars)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("monsterhealthbars", 1);
            PlayerOptions.monsterHealthBars = false;
            GameMasterScript.gmsSingleton.ToggleMonsterHealthBars();
        }

        yield return new WaitForSeconds(2f);

        GameMasterScript.heroPCActor.mySpriteRenderer.flipX = !GameMasterScript.heroPCActor.mySpriteRenderer.flipX;

        yield return new WaitForSeconds(2f);

        GameMasterScript.heroPCActor.mySpriteRenderer.flipX = !GameMasterScript.heroPCActor.mySpriteRenderer.flipX;

        yield return new WaitForSeconds(1f);

        // Shara walks slowly through the room to look at the bots
        Vector2 startPos = GameMasterScript.heroPCActor.GetPos();

        GameMasterScript.gmsSingleton.SetTempGameData("sharax", (int)startPos.x);
        GameMasterScript.gmsSingleton.SetTempGameData("sharay", (int)startPos.y);

        Vector2 nPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(0, 4f);
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 1.8f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 1.8f, false);

        // Pause after first movement, move more
        yield return new WaitForSeconds(2.6f);

        startPos = GameMasterScript.heroPCActor.GetPos();
        nPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(2f, 0f);
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 0.9f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 0.9f, false);

        yield return new WaitForSeconds(1.5f);

        startPos = GameMasterScript.heroPCActor.GetPos();
        nPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(0f, 2f);
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 0.9f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 0.9f, false);

        yield return new WaitForSeconds(1.2f);

        // Look around a bit

        GameMasterScript.heroPCActor.mySpriteRenderer.flipX = !GameMasterScript.heroPCActor.mySpriteRenderer.flipX;

        yield return new WaitForSeconds(0.8f);

        GameMasterScript.heroPCActor.mySpriteRenderer.flipX = !GameMasterScript.heroPCActor.mySpriteRenderer.flipX;

        yield return new WaitForSeconds(0.8f);

        UIManagerScript.StartConversationByRef("dialog_shara_preboss3_part1", DialogType.KEYSTORY, null);
    }

    public static IEnumerator SharaBoss3Intro_Part2()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        Vector2 startPos = GameMasterScript.heroPCActor.GetPos();
        Vector2 nPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(2f, 0f); // move in front of teleporter
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 1.8f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 1.8f, false);
        yield return new WaitForSeconds(2.6f);

        // Muse over the teleporter
        UIManagerScript.StartConversationByRef("dialog_shara_preboss3_part2", DialogType.KEYSTORY, null);

    }

    public static IEnumerator SharaBoss3Intro_Part3()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Vector2 boltPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(0, 1f); // bolt appears on teleporter
        CombatManagerScript.GenerateSpecificEffectAnimation(boltPos, "TeleportDown", null, true);
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        Vector2 startPos = GameMasterScript.heroPCActor.GetPos();
        Vector2 nPos = GameMasterScript.heroPCActor.GetPos() - new Vector2(1f, 0f); // quickly move away from bolt
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 0.4f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 0.4f, false);

        yield return new WaitForSeconds(1.2f);

        // Bolt summons a sentry bot
        CombatManagerScript.GenerateSpecificEffectAnimation(startPos, "TeleportPoof", null, true);
        Monster sentryBot = MonsterManagerScript.CreateMonster("mon_sentrybot", false, false, false, 0f, false);
        sentryBot.bufferedFaction = Faction.ENEMY;
        sentryBot.actorfaction = Faction.ENEMY;
        MapTileData botTile = MapMasterScript.activeMap.GetTile(startPos);
        MapMasterScript.activeMap.PlaceActor(sentryBot, botTile);
        MapMasterScript.singletonMMS.SpawnMonster(sentryBot);

        yield return new WaitForSeconds(2.0f);

        UIManagerScript.StartConversationByRef("dialog_shara_preboss3_part3", DialogType.KEYSTORY, null);
    }

    public static IEnumerator SharaBoss3Intro_Part4()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        Vector2 startPos = GameMasterScript.heroPCActor.GetPos();
        Vector2 nPos = GameMasterScript.heroPCActor.GetPos() - new Vector2(2f, 0f); // move in front of teleporter
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 0.8f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 0.8f, false);
        yield return new WaitForSeconds(0.85f);

        startPos = GameMasterScript.heroPCActor.GetPos();
        nPos = GameMasterScript.heroPCActor.GetPos() + new Vector2(0f, 6f); // move in front of teleporter
        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(nPos, 2.7f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(startPos, nPos, 2.7f, false);
        yield return new WaitForSeconds(3.2f);

        UIManagerScript.StartConversationByRef("dialog_shara_preboss3_part4", DialogType.KEYSTORY, null);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        // Make sure Shara is in the right game world location. This is the starting pos.
        int sharaX = GameMasterScript.gmsSingleton.ReadTempGameData("sharax");
        int sharaY = GameMasterScript.gmsSingleton.ReadTempGameData("sharay");
        Vector2 sharaStartPos = new Vector2(sharaX, sharaY);

        MapMasterScript.activeMap.RemoveActorFromLocation(sharaStartPos, GameMasterScript.heroPCActor);
        MapMasterScript.activeMap.AddActorToLocation(GameMasterScript.heroPCActor.GetPos(), GameMasterScript.heroPCActor);
    }

    public static IEnumerator SharaBoss2_BanditHelpArrives()
    {
        MusicManagerScript.singleton.FadeoutThenSetAllToZero(0.75f);

        List<Vector2> banditStartPositions = new List<Vector2>()
        {
            new Vector2(3f, 9f),
            new Vector2(3f, 10f),
            new Vector2(3f, 11f)
        };

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), banditStartPositions[0], 0.75f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        yield return new WaitForSeconds(1.05f);

        AudioStuff heroAudio = GameMasterScript.heroPCActor.GetObject().GetComponent<AudioStuff>();

        List<Monster> banditsCreated = new List<Monster>();
        for (int i = 0; i < 3; i++)
        {
            Monster nBandit = MonsterManagerScript.CreateMonster("mon_bandithunter", false, false, false, 0f, false);
            nBandit.bufferedFaction = Faction.PLAYER;
            nBandit.actorfaction = Faction.PLAYER;
            nBandit.myStats.BoostCoreStatsByPercent(0.2f);
            nBandit.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.4f);
            nBandit.allDamageMultiplier += 0.1f;
            nBandit.isChampion = true;
            nBandit.RemoveAttribute(MonsterAttributes.GREEDY);
            nBandit.RemoveAttribute(MonsterAttributes.BERSERKER);
            nBandit.RemoveAttribute(MonsterAttributes.TIMID);
            MapTileData newPosition = MapMasterScript.activeMap.GetTile(banditStartPositions[i]);
            MapMasterScript.activeMap.PlaceActor(nBandit, newPosition);
            MapMasterScript.singletonMMS.SpawnMonster(nBandit, true);
            nBandit.myMovable.SetInSightAndSnapEnable(true);
            CombatManagerScript.GenerateSpecificEffectAnimation(banditStartPositions[i], "DustCloudLanding", null, true);
            heroAudio.PlayCue("Footstep");
            yield return new WaitForSeconds(0.1f);
            heroAudio.PlayCue("Footstep");
            yield return new WaitForSeconds(0.1f);
            heroAudio.PlayCue("Footstep");
            yield return new WaitForSeconds(0.1f);
            banditsCreated.Add(nBandit);
        }

        GameMasterScript.cameraScript.SetCustomCameraAnimation(banditStartPositions[0], GameMasterScript.heroPCActor.GetPos(), 0.5f, false);
        yield return new WaitForSeconds(0.5f);

        List<Vector2> banditFinalPositions = new List<Vector2>()
        {
            new Vector2(8f,7f),
            new Vector2(7f, 6f),
            new Vector2(8f, 5f)
        };

        for (int i = 0; i < 3; i++)
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(banditsCreated[i].GetPos(), "WallJump", null, true);
            banditsCreated[i].myMovable.AnimateSetPosition(banditFinalPositions[i], 0.2f, false, 360f, 0f, MovementTypes.LERP);
            MapMasterScript.activeMap.MoveActor(banditsCreated[i].GetPos(), banditFinalPositions[i], banditsCreated[i]);
            MapMasterScript.activeMap.RemoveActorFromLocation(banditStartPositions[i], banditsCreated[i]);
            MapMasterScript.activeMap.AddActorToLocation(banditFinalPositions[i], banditsCreated[i]);
            yield return new WaitForSeconds(0.5f);
        }

		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("sharamode_boss1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "sharamode_boss1";

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("dialog_shara_preboss2_banditshelp", DialogType.KEYSTORY, null);
    }

    public static void SharaBoss2_FightIntro()
    {
        Actor mnBoss = MapMasterScript.activeMap.FindActor("mon_scientist_summoner");

        if (mnBoss == null)
        {
            Monster bandit = MonsterManagerScript.CreateMonster("mon_scientist_summoner", false, false, false, 0f, false);
            // 12, 5
            MapTileData banditMTD = MapMasterScript.activeMap.mapArray[12, 5];
            MapMasterScript.activeMap.PlaceActor(bandit, banditMTD);
            MapMasterScript.singletonMMS.SpawnMonster(bandit);
            mnBoss = bandit;
        }

        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(mnBoss.GetPos(), 1.0f, 1f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(GameMasterScript.FindConversation("dialog_shara_preboss2_fight"), DialogType.KEYSTORY, null, 3.5f));
        BossHealthBarScript.DisableBoss();
    }

    public static IEnumerator SharaBoss2_PreFight_Part1Cutscene()
    {
        List<Actor> bandits = MapMasterScript.activeMap.FindAllActors("mon_plundererboss");
        List<Actor> otherBandits = MapMasterScript.activeMap.FindAllActors("mon_fakeplunderer");
        List<Actor> allBandits = new List<Actor>();

        for (int i = 0; i < bandits.Count; i++)
        {
            allBandits.Add(bandits[i]);
        }
        for (int i = 0; i < otherBandits.Count; i++)
        {
            allBandits.Add(otherBandits[i]);
        }

        Actor device = MapMasterScript.activeMap.FindActor("mon_scientist_device");
        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(device.GetPos(), "ChargingSkillParticles", null, false);
        UIManagerScript.PlayCursorSound("LaserEnergyBuildup");

        for (int i = 0; i < 5; i++)
        {
            foreach (Actor act in allBandits)
            {
                Monster m = act as Monster;
                m.myAnimatable.SetAnim("TakeDamage");
                CombatManagerScript.SpawnChildSprite("AggroEffect", m, Directions.NORTHEAST, false);
            }

            yield return new WaitForSeconds(0.75f);
        }

        GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));
        UIManagerScript.FlashWhite(0.75f);
        GameMasterScript.cameraScript.AddScreenshake(0.6f);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FireBreathSFX", null, true);
        List<string> possibleMobs = new List<string>() { "mon_youngfireelemental", "mon_younglightningelemental", "mon_youngwaterelemental", "mon_shadowelemental" };
        for (int i = 0; i < allBandits.Count; i++)
        {
            Actor a = allBandits[i];
            a.myMovable.FadeOutThenDie();
            MapMasterScript.activeMap.RemoveActorFromMap(a);
            // One of each spirit
            Monster monSpirit = MonsterManagerScript.CreateMonster(possibleMobs[i], true, true, false, 0.15f, false);
            monSpirit.isBoss = true;
            monSpirit.ScaleToSpecificLevel(8, false);
            monSpirit.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.1f);
            monSpirit.allDamageMultiplier -= 0.15f;
            monSpirit.myInventory.AddItemRemoveFromPrevCollection(LootGeneratorScript.GenerateLoot(1.5f, 1.1f), false);
            monSpirit.aggroRange = 99;
            monSpirit.turnsToLoseInterest = 9999;
            monSpirit.RemoveAttribute(MonsterAttributes.BERSERKER);
            MapMasterScript.activeMap.PlaceActor(monSpirit, MapMasterScript.activeMap.GetTile(a.GetPos()));
            MapMasterScript.singletonMMS.SpawnMonster(monSpirit);
        }

        Conversation continueSpeak = GameMasterScript.FindConversation("dialog_shara_preboss2_fight");

        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(continueSpeak, DialogType.KEYSTORY, null, 2.8f, "main3"));
    }

    public static IEnumerator WaitThenCreateBoss1SafeHavenForShara(float time)
    {
        yield return new WaitForSeconds(time);
        // We are faded out. Now create some friendly monster bandits and food, plus NPC dirtbeak.
        // We could also move the player more to the center of the map near the throne?

        UIManagerScript.CloseDialogBox();

        Actor monDirtbeak = MapMasterScript.activeMap.FindActor("mon_banditwarlord");
        MapMasterScript.activeMap.RemoveActorFromMap(monDirtbeak);
        monDirtbeak.myMovable.FadeOutThenDie();

        NPC nDirtbeak = NPC.CreateNPC("npc_friendly_dirtbeak");
        // He's going on the throne
        MapMasterScript.activeMap.PlaceActor(nDirtbeak, MapMasterScript.GetTile(new Vector2(8f, 14f)));
        MapMasterScript.singletonMMS.SpawnNPC(nDirtbeak);

        MapMasterScript.MoveActorAndChangeCamera(GameMasterScript.heroPCActor, new Vector2(8f, 11f));

        List<string> possibleBandits = new List<string>() { "mon_plunderer", "mon_saboteur", "mon_alchemist", "mon_bandithunter" };

        for (int i = 0; i < 6; i++)
        {
            string localRef = possibleBandits[UnityEngine.Random.Range(0, possibleBandits.Count)];
            Monster bandito = MonsterManagerScript.CreateMonster(localRef, false, false, false, 0f, false);
            bandito.actorfaction = Faction.PLAYER;
            bandito.bufferedFaction = Faction.PLAYER;
            MapTileData tryTile = MapMasterScript.activeMap.GetRandomEmptyTile(new Vector2(8f, 13f), 4, false, anyNonCollidable: true, preferLOS: true);
            MapMasterScript.activeMap.PlaceActor(bandito, tryTile);
            MapMasterScript.singletonMMS.SpawnMonster(bandito, true);
            bandito.RemoveAttribute(MonsterAttributes.GREEDY);
            bandito.RemoveAttribute(MonsterAttributes.LOVESBATTLES);
        }

        for (int i = 0; i < 6; i++)
        {
            Item foodstuff = LootGeneratorScript.GenerateLootFromTable(1.0f, 0f, "food_and_meals");
            MapTileData tryTile = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
            MapMasterScript.activeMap.PlaceActor(foodstuff, tryTile);
            MapMasterScript.singletonMMS.SpawnItem(foodstuff);
        }

        yield return new WaitForSeconds(0.25f);

        UIManagerScript.FadeIn(1.0f);
        Conversation dContinued = GameMasterScript.FindConversation("dialog_shara_boss1_postfight");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(dContinued, DialogType.KEYSTORY, null, 2.75f));

    }

    public static void SharaBoss1Intro()
    {
        // Similar to original dirtbeak cutscene, but modified with new story
        // Make sure OUR dirtbeak is in the scene, and on our side.
        if (MapMasterScript.activeMap.FindActor("mon_banditwarlord") == null)
        {
            Monster myDirtbeak = MonsterManagerScript.CreateMonster("mon_banditwarlord", false, false, false, 0f, false);
            myDirtbeak.bufferedFaction = Faction.PLAYER;
            myDirtbeak.actorfaction = Faction.PLAYER;
            myDirtbeak.myStats.AddStatusByRef("status_invincible_heal", myDirtbeak, 99); // Dirtbeak cannot die
            myDirtbeak.allDamageMultiplier = 0.33f; // don't want him to be too stronk though
            myDirtbeak.myStats.SetStat(StatTypes.CHARGETIME, 70f, StatDataTypes.ALL, true);
            myDirtbeak.aggroMultiplier = 0.25f;

            MapTileData dirtbeakSpawn = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
            MapMasterScript.activeMap.PlaceActor(myDirtbeak, dirtbeakSpawn);
            MapMasterScript.singletonMMS.SpawnMonster(myDirtbeak, true);
        }

        // Now camera panning funsies
        GameMasterScript.SetAnimationPlaying(true, true);

        GameMasterScript.SetAnimationPlaying(true);
        Conversation bConvo = GameMasterScript.FindConversation("dialog_shara_boss1_prefight");

        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(bConvo, DialogType.KEYSTORY, null, 3.5f));
        Vector2 tryPos = new Vector2(8f, 11f);
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), tryPos, 1.75f, 0.5f, false);
        BossHealthBarScript.DisableBoss();
    }
    
    /// <summary>
    /// Called during the opening dialog after Shara has a dream of the Supervisor reaching out to her.
    /// </summary>
    public static void EndSharaOpeningDreamSequence()
    {
        //fade up 
        GameMasterScript.cameraScript.SetToGrayscale(false);
        UIManagerScript.FadeIn(1.0f);
        UIManagerScript.Dialog_DelayDialog(1.5f);
        
    }

    public static IEnumerator SlimeTowerConversion(Destructible towerToConvert, Map_SlimeDungeon.SlimeStatus convertToThis )
    {
        var towerPos = towerToConvert.GetPos();
        var heroPos = GameMasterScript.heroPCActor.GetPos();
        var level = MapMasterScript.activeMap as Map_SlimeDungeon;
        
        var cameraMoveTime = 2.0f;
        
        //move the camera to the tower
        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(towerPos, cameraMoveTime, false);
        yield return new WaitForSecondsRealtime(cameraMoveTime);

        //play an effect
        CombatManagerScript.GenerateSpecificEffectAnimation(towerPos, "LightningStrikeEffect", null, true);
        yield return new WaitForSecondsRealtime(0.3f);

        //slime the area around the tower
        level.SlimeAreaAroundTile(level.GetTile(towerPos), 2, convertToThis, true, 
            true, true);
        
        var controller = towerToConvert.GetObject().GetComponent<SlimeTowerController>();
        controller.DoConversion(convertToThis);
        
        //Immediately call EndOfTurn on the level to pretty up the ground slime
        level.OnEndOfTurn();
        
        //splash slime effects around
        for (int t = 0; t < 8; t++)
        {
            var splashPos = towerPos + Random.insideUnitCircle * Random.Range(0.5f, 2.0f);
            CombatManagerScript.GenerateSpecificEffectAnimation(splashPos, "GreenSmokePoof", null, true);
            yield return new WaitForSeconds(0.1f);
        }

        //back to hero
        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(heroPos, cameraMoveTime * 0.5f, false);
        yield return  new WaitForSeconds(cameraMoveTime * 0.5f);
        
        //done
    }
}
