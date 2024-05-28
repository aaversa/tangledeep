using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameEventsAndTriggers : MonoBehaviour
{
    public static IEnumerator SharaBoss4Phase2Ends()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        Vector2 spawnSquare = new Vector2(8f, 7f);
        MapTileData mtdSpawnTile = MapMasterScript.activeMap.GetTile(spawnSquare);
        if (mtdSpawnTile.IsCollidable(GameMasterScript.genericMonster))
        {
            mtdSpawnTile = MapMasterScript.activeMap.GetRandomEmptyTile(spawnSquare, 1, true, true, true, false);
            spawnSquare = mtdSpawnTile.pos;

        }


        GameObject fadeAway = GameMasterScript.TDInstantiate("FadeAwayParticles");

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(spawnSquare, 1.4f, false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);        
        fadeAway.transform.position = spawnSquare;

        GameMasterScript.gmsSingleton.WaitThenDestroyObject(fadeAway, 9f);

        yield return new WaitForSeconds(1.5f);

        Monster boss = MonsterManagerScript.CreateMonster("mon_shara_finalboss", false, false, false, 0f, false);

        // add a couple new abilities
        boss.LearnNewPower("skill_statusresist50", 1f, 1f, 1, 99);
        boss.LearnNewPower("skill_nopushpull", 1f, 1f, 1, 99);
        boss.LearnNewPower("skill_expmon_blackhole", 1f, 1f, 2, 5);
        boss.LearnNewPower("skill_droneswarm", 0.5f, 1f, 1, 8);

        MapMasterScript.activeMap.PlaceActor(boss, mtdSpawnTile);
        boss.AddAggro(GameMasterScript.heroPCActor, 100f);
        MapMasterScript.singletonMMS.SpawnMonster(boss);

        boss.CleanStuckVisualFX();

        UIManagerScript.FlashWhite(1.0f);
        UIManagerScript.PlayCursorSound("SupervisorSound");

        yield return new WaitForSeconds(1.5f);

        CombatManagerScript.GenerateSpecificEffectAnimation(spawnSquare, "FervirBuff", null, true);

        yield return new WaitForSeconds(1.0f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        UIManagerScript.StartConversationByRef("dialog_sharafinalboss_phase2end", DialogType.KEYSTORY, null);

    }

    /// <summary>
    /// Warp away the final boss, get ready to begin waves of monsters
    /// </summary>
    /// <returns></returns>
    public static IEnumerator SharaBoss4Phase2Begin_Part3()
    {
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        DialogEventsScript.RecenterCameraOnPlayer("");
        GameMasterScript.SetAnimationPlaying(false);

        int bossID = GameMasterScript.gmsSingleton.ReadTempGameData("sharafinalbossid");
        Actor bossMon = GameMasterScript.gmsSingleton.TryLinkActorFromDict(bossID);
        CombatManagerScript.GenerateSpecificEffectAnimation(bossMon.GetPos(), "TeleportUp", null, true);
        yield return new WaitForSeconds(0.05f);

        MapMasterScript.activeMap.RemoveActorFromMap(bossMon); // we'll re-add the final boss later as a new creature
        bossMon.myMovable.FadeOutThenDie();
        BossHealthBarScript.DisableBoss();
    }

    /// <summary>
    /// The Core Guardian has hit 50% health. Spice up the fight.
    /// </summary>
    /// <param name="mn"></param>
    public static void PrepareForSharaBoss4Phase2(Monster mn)
    {
        UIManagerScript.CloseDialogBox(); // just make sure we don't have anything open that could soak up input

        // Prevent Core Guardian from taking further damage.
        mn.myStats.AddStatusByRef("status_invincible_dmg", mn, 99);

        GameMasterScript.heroPCActor.SetActorData("finalbosshalfwaypoint", 1);

        // Enqueue function that kicks off the phase change, which will run at end of turn.
        GameMasterScript.AddEndOfTurnFunction(TDGenericFunctions.SharaBoss4Phase2Begin, new string[] { mn.actorUniqueID.ToString() });
    }

    public static void SharaBoss4Victory()
    {
        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss4Victory_Part1());
    }

    public static void SharaEntersFinalBossFloor()
    {
        int finalboss1flag = GameMasterScript.heroPCActor.ReadActorData("finalboss1");

        if (finalboss1flag <= 0) // First time entering, so play the cutscene.
        {
            Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaFinalBossIntro());
        }
    }

    public static void CheckForSharaBoss3Victory(Monster mon)
    {
        bool anyGolems = false;
        foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorRefName == "mon_xp_heavygolem" && m.myStats.IsAlive() && !m.isInDeadQueue)
            {
                anyGolems = true;
                break;
            }
        }
        if (!anyGolems && GameMasterScript.gmsSingleton.ReadTempGameData("sharaboss3victory") != 1)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("sharaboss3victory", 1);
            SharaBoss3Victory(mon); // mon is the golem that was just beaten            
        }
    }

    public static void SharaBoss3Victory(Monster mon)
    {
        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            st.EnableActor();
            st.myMovable.SetInSightAndSnapEnable(true);
        }

        List<Monster> removeActor = new List<Monster>();
        foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m != mon && m.actorfaction == Faction.ENEMY)
            {
                m.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.ALL, true);
                m.whoKilledMe = GameMasterScript.heroPCActor;
                GameMasterScript.AddToDeadQueue(m);
            }
        }

        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);

        ProgressTracker.SetProgress(TDProgress.BOSS3, ProgressLocations.HERO, 3);
        
        GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Shara_Boss3Defeated();
        SharaModeStuff.RefreshSharaAbilityNamesAndDescriptions();

        //GameMasterScript.musicManager.LoadMusicByName("BossVictory", true);
        //GameMasterScript.musicManager.Play(true, true);
        GameMasterScript.musicManager.Fadeout(1.2f);

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        GameMasterScript.gmsSingleton.statsAndAchievements.Boss3Defeated();

        singleton.StartCoroutine(singleton.SharaBoss3Victory_Part1(mon));
    }

    IEnumerator SharaBoss3Victory_Part1(Monster deadGolem)
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        // Create fakey robot for vfx
        Monster robo = MonsterManagerScript.CreateMonster("mon_heavygolem", false, false, false, 0f, true);
        Vector2 roboPos = deadGolem.GetPos();
        MapTileData mtd = MapMasterScript.activeMap.GetTile(roboPos);
        MapMasterScript.activeMap.PlaceActor(robo, mtd);
        MapMasterScript.singletonMMS.SpawnMonster(robo, true);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        // static e
        for (int i = 0; i < 6; i++)
        {
            Vector2 pos = roboPos;
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.44f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "StaticShockEffect", null, 0.01f + i * 0.3f, true);
        }        

        yield return new WaitForSeconds(0.6f);

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), roboPos, 0.75f, false);        

        yield return new WaitForSeconds(2.5f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        UIManagerScript.StartConversationByRef("dialog_shara_boss3victory_part1", DialogType.KEYSTORY, null);
    }

    public static IEnumerator SharaBoss3Victory_Part2()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        Actor golem = MapMasterScript.activeMap.FindActor("mon_heavygolem");

        for (int i = 0; i < 15; i++)
        {
            Vector2 pos = golem.GetPos();
            pos.x += UnityEngine.Random.Range(-0.4f, 0.4f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.44f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "SmallExplosionEffect", null, 0.01f + i * 0.155f, true);
        }
        yield return new WaitForSeconds(3.0f);

        MapMasterScript.activeMap.RemoveActorFromMap(golem);
        golem.myMovable.FadeOutThenDie();
        CombatManagerScript.GenerateSpecificEffectAnimation(golem.GetPos(), "BigExplosionEffect", null, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(golem.GetPos(), "FireBreathSFX", null, true);
        UIManagerScript.FlashWhite(0.6f);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);
        
        //Spawn in a golem head, have it fly into the air and bounce, which it will do thanks to code written
        //on the prefab.
        MapTileData mtd = MapMasterScript.GetRandomEmptyTile(golem.GetPos(), 1, true, true);
        Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(mtd, "exp1_obj_dominator_head");
        MapMasterScript.singletonMMS.SpawnDestructible(dt);
        dt.myMovable.SetInSightAndSnapEnable(true);

        yield return new WaitForSeconds(2.5f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        UIManagerScript.StartConversationByRef("dialog_shara_boss3victory_part2", DialogType.KEYSTORY, null);
    }

    // Shara pulls the head of the crushed boss up using TK, and examines it.
    public static IEnumerator SharaBoss3Victory_PickupDominatorHead()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        var dominatorHead = MapMasterScript.activeMap.FindActor("exp1_obj_dominator_head") as Destructible;
        
        if (dominatorHead != null)
        {
            // Make sure it renders on top
            dominatorHead.myMovable.SetInSightAndSnapEnable(true);
            dominatorHead.mySpriteRenderer.sortingOrder = 5000;
            
            //do your thing, little fancy head
            var dhcc = dominatorHead.GetObject().GetComponent<DominatorHeadCinematicComponent>();
            dhcc.StartFloatingAnimation();
            
            UIManagerScript.PlayCursorSound("Mirage");
            
            //player makes magic-esque animation happen
            GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Attack", Directions.SOUTH, Directions.SOUTH);

            //create emblem that player can equip!!
            Item gearHead = LootGeneratorScript.CreateItemFromTemplateRef("exp1_obj_dominator_head", 1.5f, 0f, false);
            Emblem emb = gearHead as Emblem;
            emb.emblemLevel = 1;
            MagicMod mmWellRounded = GameMasterScript.masterMagicModList["mm_emblemwellrounded2"];
            EquipmentBlock.MakeMagicalFromMod(emb, mmWellRounded, true, false, true, true);
            GameMasterScript.heroPCActor.myInventory.AddItem(gearHead, false);
            
            //wait
            yield return new WaitForSeconds(3.0f);
            
            //more jibba
            UIManagerScript.StartConversationByRef("dialog_shara_boss3victory_examine_device", DialogType.KEYSTORY, null);
            
        }

        yield return null;
    }
    
    
    /// <summary>
    /// A small robot appears and approaches Shara.
    /// </summary>
    /// <returns></returns>
    public static IEnumerator SharaBoss3Victory_HelloBabyRobot()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        var shara = GameMasterScript.heroPCActor;
        var activeMap = MapMasterScript.activeMap;

        //find a place to put the babby
        var babySummonTile = activeMap.GetRandomEmptyTile(shara.GetPos(), 2, preferClose: false,
                                        preferLOS: true, excludeCenterTile: true);
        
        //if that tile is Shara's tile for whatever reason, end this cutscene and move along.
        if (babySummonTile.pos == shara.GetPos())
        {
            GameMasterScript.SetAnimationPlaying(false);
            yield break;
        }

        var babyPos = babySummonTile.pos;
        
        //summon the baby into place
        CombatManagerScript.GenerateSpecificEffectAnimation(babyPos, "TeleportPoof", null, true);
        var babyBot = MonsterManagerScript.CreateMonster("mon_sentrybot", false, false,
            false, 0f, false);
        
        babyBot.bufferedFaction = Faction.ENEMY;
        babyBot.actorfaction = Faction.ENEMY;
        MapTileData botTile = MapMasterScript.activeMap.GetTile(babyPos);
        MapMasterScript.activeMap.PlaceActor(babyBot, botTile);
        MapMasterScript.singletonMMS.SpawnMonster(babyBot);
        
        //shara is surprised!
        CombatManagerScript.SpawnChildSprite("AggroEffect", shara, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(1.0f);
        
        //continue the conversation
        UIManagerScript.StartConversationByRef("dialog_shara_boss3victory_use_device", DialogType.KEYSTORY, null);
        


    }
    public static IEnumerator SharaBoss3Victory_DominatesRobot()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.5f);
        
        //Only do this if the head is still around.        
        var dominatorHead = MapMasterScript.activeMap.FindActor("exp1_obj_dominator_head") as Destructible;

        if (dominatorHead == null)
        {
            GameMasterScript.SetAnimationPlaying(false, true);
            yield break;
        }
        
        GameMasterScript.SetAnimationPlaying(true, true);
        var shara = GameMasterScript.heroPCActor;
        var activeMap = MapMasterScript.activeMap;
        var babbyRobot = MapMasterScript.activeMap.FindActor("mon_sentrybot");
        var robotPos = babbyRobot.GetPos();
        
        var dhcc = dominatorHead.GetObject().GetComponent<DominatorHeadCinematicComponent>();
        var beamStart = dhcc.StopFloatingAnimationAndReturnPosition();
        
        //wave hands
        shara.myAnimatable.SetAnimDirectional("Attack", Directions.SOUTH, Directions.SOUTH);

        //pew pew
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = robotPos;
            pos.x += UnityEngine.Random.Range(-0.1f, 0.1f);
            pos.y += UnityEngine.Random.Range(-0.1f, 0.1f);
            CombatManagerScript.WaitThenGenerateSpecificEffect(pos, "FervirShadowHit", null, 0.01f + i * 0.155f, true);
        }

        yield return new WaitForSeconds(0.4f);
        
        //clean up and make a new pet
        UIManagerScript.FlashWhite(0.7f);
        yield return new WaitForSeconds(0.1f);
        MapMasterScript.activeMap.RemoveActorFromMap(babbyRobot);
        babbyRobot.myMovable.FadeOutThenDie();
        dhcc.CleanUpAndRemove();
        yield return new WaitForSeconds(0.4f);
        
        //A new pet
        var petShadow = MonsterManagerScript.CreateMonster("mon_shadowelemental", false, false,
            false, 0f, false);
        
        petShadow.bufferedFaction = Faction.ENEMY;
        petShadow.actorfaction = Faction.ENEMY;
        MapTileData botTile = MapMasterScript.activeMap.GetTile(robotPos);
        activeMap.PlaceActor(petShadow, botTile);
        MapMasterScript.singletonMMS.SpawnMonster(petShadow);
        
        //big long code block to dominate pet
        petShadow.myStats.AddStatusByRef("status_permacharmed",shara, 999);
        shara.AddSummon(petShadow);
        shara.AddAnchor(petShadow);
        petShadow.anchor = GameMasterScript.heroPCActor;
        petShadow.summoner = GameMasterScript.heroPCActor;
        petShadow.actorfaction = Faction.PLAYER;
        petShadow.bufferedFaction = Faction.PLAYER;
        petShadow.anchorRange = 3;

        yield return new WaitForSeconds(1.0f);
        
        DialogEventsScript.RecenterCameraOnPlayer("");

        yield return new WaitForSeconds(1.0f);

        //continue the conversation
        UIManagerScript.StartConversationByRef("dialog_shara_boss3victory_finale", DialogType.KEYSTORY, null);
        
    }
    

    public static void SharaDefeatsBoss2Scientist(Monster mon)
    {
        singleton.StartCoroutine(SharaDefeatsBoss2ScientistContinued());        
    }

    static IEnumerator SharaDefeatsBoss2ScientistContinued()
    {
        GameMasterScript.gmsSingleton.SetTempGameData("scientist_defeat_sequence_begin", 1);
        GameMasterScript.SetAnimationPlaying(true);
        yield return new WaitForSeconds(1.0f);
        GameMasterScript.SetAnimationPlaying(false);

        if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2defeatstart") == 1)
        {
            // we're already handling this dialog elsewhere
            GameMasterScript.gmsSingleton.SetTempGameData("boss2scientistdefeated", 1);
            yield break;
        }

        var dd = GameMasterScript.heroPCActor.ReadActorData("boss2_device_destroyed");
        UIManagerScript.StartConversationByRef("dialog_shara_boss2_scientist_defeated", DialogType.KEYSTORY, null, 
            false, dd == 0 ? "device_alive" : "device_destroyed");
    }

    public static void SharaDestroysScientistDevice(Monster device)
    {
        if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2defeatstart") == 1)
        {
            GameMasterScript.heroPCActor.SetActorData("boss2_device_destroyed", 1);
            return;
        }
        GameMasterScript.SetAnimationPlaying(true);
        CombatManagerScript.GenerateSpecificEffectAnimation(device.GetPos(), "VoidShatterParticles", null, true);
        singleton.StartCoroutine(singleton.SharaDestroysScientistDevice_Part2());
        
        //mark data on the player that the device is well and truly chunked
        GameMasterScript.heroPCActor.SetActorData("boss2_device_destroyed", 1);
    }

    IEnumerator SharaDestroysScientistDevice_Part2()
    {
        yield return new WaitForSeconds(1.25f);

        if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2scientistdefeated") == 1 || GameMasterScript.gmsSingleton.ReadTempGameData("scientist_defeat_sequence_begin") == 1)
        {
            yield break;
        }

        bool anySpiritsAffected = false;
        foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.isBoss && m.actorfaction == Faction.ENEMY && m.monFamily == "spirits")
            {
                m.myAnimatable.SetAnim("TakeDamage");
                CombatManagerScript.GenerateSpecificEffectAnimation(m.GetPos(), "FervirDebuff", null, true);
                m.myStats.SetStat(StatTypes.HEALTH, m.myStats.GetCurStat(StatTypes.HEALTH) / 2f, StatDataTypes.ALL, true);
                if (m.healthBarScript != null)
                {
                    m.healthBarScript.UpdateBar(m.myStats.GetCurStatAsPercent(StatTypes.HEALTH));
                }
                anySpiritsAffected = true;
            }
        }

        if (anySpiritsAffected)
        {
            yield return new WaitForSeconds(1.0f);
            GameMasterScript.SetAnimationPlaying(false);

            if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2defeatstart") != 1)
            {
                UIManagerScript.StartConversationByRef("dialog_shara_boss2_devicedestroyed", DialogType.KEYSTORY, null);
            }            
        }
        else
        {
            GameMasterScript.SetAnimationPlaying(false);
        }
    }

    public static void SharaBoss2Victory() // SCIENTIST is defeated.
    {
        GameMasterScript.gmsSingleton.SetTempGameData("boss2defeatstart", 1);

        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            st.EnableActor();
            st.myMovable.SetInSightAndSnapEnable(true);
        }

        GameMasterScript.heroPCActor.SetActorData("secondbossdefeated", 1);
        ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 3);
        GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Shara_Boss2Defeated();
        SharaModeStuff.RefreshSharaAbilityNamesAndDescriptions();
        //Conversation victory = GameMasterScript.FindConversation("beatboss2");
        MetaProgressScript.SetMetaProgress("secondbossdefeated", 1);

        GameMasterScript.SetAnimationPlaying(true);

        MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");

        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        GameMasterScript.gmsSingleton.statsAndAchievements.Boss2Defeated();

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

        singleton.StartCoroutine(SharaBoss2Victory_Continued());
    }

    public static IEnumerator SharaBoss2Victory_Continued()
    {
        // Device MIGHT be destroyed
        GameMasterScript.SetAnimationPlaying(true, true);
        Actor device = MapMasterScript.activeMap.FindActor("mon_scientist_device");
        yield return new WaitForSeconds(1.5f);

        if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2scientistdefeated") == 1) // must play scientist dialogue first, as we didnt hear it already.
        {
            var dd = GameMasterScript.heroPCActor.ReadActorData("boss2_device_destroyed");
            UIManagerScript.StartConversationByRef("dialog_shara_boss2_scientist_defeated", DialogType.KEYSTORY, null,
                false, dd == 0 ? "device_alive" : "device_destroyed");
            yield break;
        }

        if (device != null)
        {
            Debug.Log("Device has not been destroyed.");
            // Pan to device and play cool buildup
            GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), device.GetPos(), 0.75f, false);
            GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);
            GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(device.GetPos(), "ChargingSkillParticles", null, false);
            UIManagerScript.PlayCursorSound("LaserEnergyBuildup");
            yield return new WaitForSeconds(3.0f);

            // Then flash white, shake, destroy it
            UIManagerScript.FlashWhite(0.5f);
            GameMasterScript.ReturnToStack(particles, particles.name.Replace("(Clone)", string.Empty));
            CombatManagerScript.GenerateSpecificEffectAnimation(device.GetPos(), "VoidShatterParticles", null, true);
            MapMasterScript.activeMap.RemoveActorFromMap(device);
            device.myMovable.FadeOutThenDie();
            GameMasterScript.cameraScript.AddScreenshake(0.7f);
            yield return new WaitForSeconds(1.5f);

            UIManagerScript.StartConversationByRef("dialog_shara_boss2_victory", DialogType.KEYSTORY, null, false, "device_alive");
        }
        else
        {
            Debug.Log("Device has been destroyed.");
            // Device is destroyed, nothing more to say.
            if (UIManagerScript.dialogBoxOpen && UIManagerScript.currentConversation != null)
            {
                // Must be some other convo open. 
            }
            else
            {
                UIManagerScript.StartConversationByRef("dialog_shara_boss2_victory", DialogType.KEYSTORY, null, false, "device_destroyed");
            }
        }
    }

    public static void SharaBoss1Victory()
    {
        GameEventsAndTriggers.RemoveTrapsFromBoss1Map();

        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);
        GameMasterScript.SetAnimationPlaying(true, true);

        Conversation victory = GameMasterScript.FindConversation("dialog_shara_boss1_postfight");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(victory, DialogType.KEYSTORY, null, 1.5f));
        ProgressTracker.SetProgress(TDProgress.BOSS1, ProgressLocations.HERO, 3);        
        GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Shara_Boss1Defeated();
        SharaModeStuff.RefreshSharaAbilityNamesAndDescriptions();
        MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");


        GameMasterScript.heroPCActor.SetActorData("viewfloor" + MapMasterScript.activeMap.floor.ToString(), 999);

        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            st.EnableActor();
            st.myMovable.SetInSightAndSnapEnable(true);
        }
    }

    public static void StartSharaIntroScene()
    {
        UIManagerScript.TogglePlayerHUD();

        GameMasterScript.cameraScript.SetToGrayscale(true);
        if (PlayerOptions.playerHealthBar && GameMasterScript.heroPCActor.healthBarScript.gameObject.activeSelf)
        {
            GameMasterScript.gmsSingleton.TogglePlayerHealthBar();
        }
        Conversation intro = GameMasterScript.FindConversation("dialog_shara_intro_scene");
        GameMasterScript.SetAnimationPlaying(true);
        GameMasterScript.heroPCActor.lastMovedDirection = Directions.SOUTH;
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Idle", Directions.SOUTH, Directions.SOUTH);
        Movable sharaM = GameMasterScript.heroPCActor.myMovable;
        singleton.StartCoroutine(singleton.WaitThenSetHeroAnimAndDirection("Idle", Directions.WEST, 3f));
        singleton.StartCoroutine(singleton.WaitThenSetHeroAnimAndDirection("Idle", Directions.EAST, 5f));
        singleton.StartCoroutine(singleton.WaitThenSetHeroAnimAndDirection("Idle", Directions.SOUTH, 7.5f));
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(intro, DialogType.KEYSTORY, null, 7.5f));
    }

    IEnumerator WaitThenSetHeroAnimAndDirection(string anim, Directions dir, float time)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.heroPCActor.UpdateLastMovedDirection(dir);
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional(anim, dir, dir);
    }


    public static void CheckForSharaGameStart()
    {
        if (GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.SHARA) return;

        if (GameMasterScript.heroPCActor.ReadActorData("shara_start") == 1 ||
            MetaProgressScript.ReadMetaProgress("shara_start") == 1) return;

        MetaProgressScript.SetMetaProgress("shara_start", 1);

        UIManagerScript.TogglePlayerHUD(); // turn hud back on.

        if (PlayerOptions.playerHealthBar && !GameMasterScript.heroPCActor.healthBarScript.gameObject.activeSelf)
        {
            GameMasterScript.gmsSingleton.TogglePlayerHealthBar();
        }

        // We are Shara and haven't seen our game start cutscene.
        // Fade to black for the dream sequence
        //UIManagerScript.FadeOut(0.25f);

        GameMasterScript.SetAnimationPlaying(true, false);
        Conversation intro = GameMasterScript.FindConversation("dialog_shara_gamestart_scene");
		UIManagerScript.PlaceDialogBoxInFrontOfFade(true);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(intro, DialogType.KEYSTORY, null, 2f));
        singleton.StartCoroutine(singleton.WaitThenSetSharaObjectToAdult(2f));

        Stairs stToRemove = null;
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.pointsToFloor == MapMasterScript.TOWN_MAP_FLOOR)
            {
                stToRemove = st;
                break;
            }
        }

        if (stToRemove != null)
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(stToRemove.GetPos(), stToRemove);
            MapMasterScript.activeMap.RemoveActorFromMap(stToRemove);
            stToRemove.myMovable.FadeOutThenDie();
        }        
    }

    IEnumerator WaitThenSetSharaObjectToAdult(float time)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.gmsSingleton.UpdateHeroObject("JobShara");
    }

    public static void SpawnNPCDirtbeakNearShara()
    {
        if (GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.SHARA) return;

        if (MapMasterScript.activeMap.FindActor("npc_friendly_dirtbeak") != null)
        {
            return;
        }

        NPC warlord = NPC.CreateNPC("npc_friendly_dirtbeak");
        MapTileData tile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 3, true, false, true, true, false);
        MapMasterScript.activeMap.PlaceActor(warlord, tile);
        MapMasterScript.singletonMMS.SpawnNPC(warlord);
    }
}
