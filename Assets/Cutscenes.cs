using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Cutscenes : MonoBehaviour
{

    public static Cutscenes singleton;

    public void Start()
    {
        singleton = this;
    }

    public static void JuliaTutorialCutscene()
    {
        singleton.StartCoroutine(IJuliaTutorialCutscene());
    }

    static IEnumerator IJuliaTutorialCutscene()
    {
        GameMasterScript.SetAnimationPlaying(true, true);

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);        

        Actor julia = MapMasterScript.activeMap.FindActor("npc_julia");

        CombatManagerScript.SpawnChildSprite("AggroEffect", julia, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), julia.GetPos(), 1f, false);

        yield return new WaitForSeconds(1.5f);

        Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_juliatown");
        GameMasterScript.SetAnimationPlaying(false);        
        UIManagerScript.StartConversation(newConvo, DialogType.KEYSTORY, null);
    }

    public static void Boss2_FightIntro()
    {
        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 2);
            foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorRefName == "mon_shadowelementalboss")
                {
                    BossHealthBarScript.EnableBossWithAnimation(m);
                    break;
                }
            }

            return;
        }

        Actor mnBoss = MapMasterScript.activeMap.FindActor("mon_banditwarlord");

        if (mnBoss == null)
        {
            Monster bandit = MonsterManagerScript.CreateMonster("mon_banditwarlord", false, false, false, 0f, false);
            // 12, 5
            MapTileData banditMTD = MapMasterScript.activeMap.mapArray[12, 5];
            MapMasterScript.activeMap.PlaceActor(bandit, banditMTD);
            MapMasterScript.singletonMMS.SpawnMonster(bandit);
            mnBoss = bandit;
        }

        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(mnBoss.GetPos(), 1.0f, 1f, false);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(GameMasterScript.FindConversation("second_boss_intro"), DialogType.KEYSTORY, null, 3.5f));
        BossHealthBarScript.DisableBoss();

        // Make sure Mirai faces east to face dirtbeak
        GameMasterScript.heroPCActor.UpdateLastMovedDirection(Directions.EAST);
    }

    public IEnumerator PickUpTechCubeFromDirtbeak()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(true);

        // Create a new destructible on the map (the cube) if needed
        Destructible cubeTemplate = GameMasterScript.masterMapObjectDict["obj_techcube"];
        Vector2 summonPosition = GameMasterScript.heroPCActor.GetPos();
        // Offset from the hero position
        if (summonPosition.y + 2f >= MapMasterScript.activeMap.columns-1)
        {
            summonPosition.y -= 2f;
        }
        else
        {
            summonPosition.y += 2f;
        }

        UIManagerScript.PlayCursorSound("Mirage");

        Destructible tempCube = MapMasterScript.activeMap.FindActor("obj_techcube") as Destructible;
        if (tempCube == null)
        {
            tempCube = GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, cubeTemplate, GameMasterScript.heroPCActor.GetPos(), 50);
        }

        // Make sure it renders on top
        tempCube.myMovable.SetInSightAndSnapEnable(true);
        tempCube.mySpriteRenderer.sortingLayerName = "Actor";
        tempCube.mySpriteRenderer.sortingOrder = 5000;        

        tempCube.myMovable.AnimateSetPositionNoChange(summonPosition, 3f, false, 0f, 0f, MovementTypes.LERP);
        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), summonPosition, 3f, false);

        yield return new WaitForSeconds(4f);

        CombatManagerScript.GenerateSpecificEffectAnimation(summonPosition, "FadeAwayParticles", null, false);
        CombatManagerScript.GenerateSpecificEffectAnimation(summonPosition, "MindControl", null, false);

        yield return new WaitForSeconds(1.5f);

        UIManagerScript.StartConversationByRef("techcube", DialogType.KEYSTORY, null);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
    }

    public IEnumerator StartEscapedFrogCutsceneInCamp()
    {
        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            if (Debug.isDebugBuild) Debug.Log("Begin the frog grove event!");
            ProgressTracker.SetProgress(TDProgress.ESCAPED_FROG, ProgressLocations.META, 1);
            SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.ESCAPED_FROG);
            yield break;
        }

        GameMasterScript.SetAnimationPlaying(true, true);

        Vector2 cameraFocusPos = new Vector2(10f, 2f);

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), cameraFocusPos, 1.4f, false);

        yield return new WaitForSeconds(1.0f);

        Vector2 notVisiblePos = new Vector2(1f, 1f);

        CombatManagerScript.GenerateSpecificEffectAnimation(cameraFocusPos, "AggroEffect", null, false);

        CombatManagerScript.GenerateSpecificEffectAnimation(notVisiblePos, "WallJump", null, true);
        yield return new WaitForSeconds(0.4f);
        CombatManagerScript.GenerateSpecificEffectAnimation(notVisiblePos, "JumpAndImpactSFX", null, true);
        GameMasterScript.cameraScript.AddScreenshake(0.25f);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 4; i++)
        {
            UIManagerScript.PlayCursorSound("AltJump");
            GameMasterScript.cameraScript.AddScreenshake(0.1f);
            yield return new WaitForSeconds(0.25f);
        }

        CombatManagerScript.GenerateSpecificEffectAnimation(cameraFocusPos, "SoundEmanation", null, true);

        yield return new WaitForSeconds(0.9f);

        CombatManagerScript.GenerateSpecificEffectAnimation(cameraFocusPos, "SoundEmanation", null, true);

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.GenerateSpecificEffectAnimation(cameraFocusPos, "SoundEmanation", null, true);

        yield return new WaitForSeconds(0.9f);

        CombatManagerScript.GenerateSpecificEffectAnimation(new Vector2(30f, 1f), "FervirFastPunchEffect", null, true);
        yield return new WaitForSeconds(0.15f);
        CombatManagerScript.GenerateSpecificEffectAnimation(notVisiblePos, "JumpAndImpactSFX", null, true);
        GameMasterScript.cameraScript.AddScreenshake(0.75f);

        yield return new WaitForSeconds(1.25f);

        UIManagerScript.StartConversationByRef("dialog_callout_grove_frog", DialogType.KEYSTORY, null);
        ProgressTracker.SetProgress(TDProgress.ESCAPED_FROG, ProgressLocations.META, 1);
    }

    public static void DoTechCubeCutsceneInGrove()
    {
        Destructible cubeTemplate = GameMasterScript.masterMapObjectDict["obj_techcube"];
        Vector2 sPos = new Vector2(GameMasterScript.heroPCActor.GetPos().x, GameMasterScript.heroPCActor.GetPos().y - 1f);

        Destructible cutsceneCube = MapMasterScript.activeMap.FindActor("obj_techcube") as Destructible;

        if (cutsceneCube == null)
        {
             cutsceneCube = GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, cubeTemplate, GameMasterScript.heroPCActor.GetPos(), 5);
        }

        GameMasterScript.SetAnimationPlaying(true, true);
        singleton.StartCoroutine(singleton.TechCubeCutScenePart2(cutsceneCube));
    }

    public IEnumerator TechCubeRestoreSequence()
    {

        GameMasterScript.SetAnimationPlaying(true, true);
        NPC machine = MapMasterScript.activeMap.FindActor("npc_itemworld") as NPC;
        Vector2 nPos = machine.GetPos();

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), machine.GetPos(), 1.5f, false);

        // metal poofz
        for (int i = 0; i < 11; i++)
        {
            Vector2 localFXPos = new Vector2(nPos.x + UnityEngine.Random.Range(-1.5f, 1.5f), nPos.y + UnityEngine.Random.Range(-1.5f, 1.5f));
            CombatManagerScript.GenerateSpecificEffectAnimation(localFXPos, "MetalPoof", null, true);
            localFXPos = new Vector2(nPos.x + UnityEngine.Random.Range(-1.5f, 1.5f), nPos.y + UnityEngine.Random.Range(-1.5f, 1.5f));
            CombatManagerScript.GenerateSpecificEffectAnimation(localFXPos, "StaticShockEffect", null, true);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.3f));
        }

        Actor cubeObject = MapMasterScript.activeMap.FindActor("obj_techcube");
        MapMasterScript.activeMap.RemoveActorFromMap(cubeObject);
        MapMasterScript.activeMap.RemoveActorFromLocation(cubeObject.GetPos(), cubeObject);
        cubeObject.myMovable.FadeOutThenDie();

        yield return new WaitForSeconds(0.25f);

        GameMasterScript.ReturnToStack(machine.GetObject(), "ItemWorldMachineBroken");
        MapMasterScript.singletonMMS.SpawnNPC(machine);
        UIManagerScript.PlayCursorSound("EnterItemWorld");
        GameMasterScript.cameraScript.AddScreenshake(0.5f);
        UIManagerScript.FlashWhite(0.9f);

        yield return new WaitForSeconds(0.2f);

        // switch machine graphics

        MetaProgressScript.SetMetaProgress("machine_awake_cutscene", 1);

        yield return new WaitForSeconds(3f);

        Conversation cRef = GameMasterScript.FindConversation("techcube_restore");
        UIManagerScript.StartConversation(cRef, DialogType.KEYSTORY, null);
    }
    
    IEnumerator TechCubeCutScenePart2(Destructible cutsceneCube)
    {
        yield return new WaitForSeconds(2f);
        Actor mir = MapMasterScript.activeMap.FindActor("npc_tinkerer");
        Vector2 newPos = new Vector2(mir.GetPos().x -1f, mir.GetPos().y - 1f);
        cutsceneCube.myMovable.AnimateSetPositionNoChange(newPos, 2.0f, false, 360f, 0f, MovementTypes.LERP);
        UIManagerScript.PlayCursorSound("Mirage");
        yield return new WaitForSeconds(3.5f);

        Conversation cubeQuest = GameMasterScript.FindConversation("techcube_restore");
        UIManagerScript.StartConversation(cubeQuest, DialogType.STANDARD, null);
    }

    public IEnumerator WaitThenHerbalistPart2(NPC herbalist, float time)
    {
        yield return new WaitForSeconds(time);

        if (PlayerOptions.screenFlashes)
        {
            UIManagerScript.FlashWhite(0.4f);
        }
        GameMasterScript.cameraScript.AddScreenshake(0.4f);
        StartCoroutine(WaitThenHerbalistPart3(herbalist, 1.1f));
    }

    IEnumerator WaitThenHerbalistPart3(NPC herbalist, float time)
    {
        yield return new WaitForSeconds(time);
        for (int i = 0; i < 3; i++)
        {
            MapTileData nearby = MapMasterScript.GetRandomEmptyTile(herbalist.GetPos(), 1, true, false);
            Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(nearby, "quest_magical_herb2");
            MapMasterScript.singletonMMS.SpawnDestructible(dt);
            CombatManagerScript.FireProjectile(herbalist.GetPos(), nearby.pos, dt.GetObject(), 0.4f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);
        }

        GameMasterScript.SetAnimationPlaying(false);
    }


    public static void StartDimRiftCutscene()
    {                
        GameMasterScript.heroPCActor.SetActorData("dimrift", 1);
        MapTileData mtdNearby = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true, true, false, true);
        NPC scientist = MapMasterScript.activeMap.CreateNPCInTile(mtdNearby, "npc_dimrift_madscientist");
        MapMasterScript.singletonMMS.SpawnNPC(scientist);
        CombatManagerScript.GenerateSpecificEffectAnimation(mtdNearby.pos, "TeleportDown", null, true);
        UIManagerScript.FlashWhite(0.5f);
    }

    public IEnumerator DimRiftBossEncounter_Part2()
    {
        UIManagerScript.PlayCursorSound("LaserEnergyBuildup");

        UIManagerScript.FadeWhiteOutAndIn(5f);

        GameMasterScript.cameraScript.AddScreenshake(4f);
        yield return new WaitForSeconds(2.5f);
        

        int numBosses = 1 + GameStartData.NewGamePlus;

        List<Monster> panthrox = new List<Monster>();
        for (int i = 0; i < numBosses; i++)
        {
            MapTileData randomEmpty = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 3, false, false, false, true);
            Monster pPan = MonsterManagerScript.CreateMonster("mon_dimriftboss", true, true, false, 0f, false);
            if (i == 0 && RandomJobMode.IsCurrentGameInRandomJobMode()) RandomJobMode.TryAddRelicTreasureToBoss(pPan, 1.9f);
            MapMasterScript.activeMap.PlaceActor(pPan, randomEmpty);
            MapMasterScript.singletonMMS.SpawnMonster(pPan);
            panthrox.Add(pPan);
        }

        yield return new WaitForSeconds(3.5f);
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("dimriffboss");

        GameMasterScript.cameraScript.AddScreenshake(0.4f);
        CombatManagerScript.GenerateSpecificEffectAnimation(panthrox.GetRandomElement().GetPos(), "SoundEmanation", null, true);
        yield return new WaitForSeconds(0.09f);
        CombatManagerScript.GenerateSpecificEffectAnimation(panthrox.GetRandomElement().GetPos(), "SoundEmanation", null, true);
        yield return new WaitForSeconds(0.09f);
        CombatManagerScript.GenerateSpecificEffectAnimation(panthrox.GetRandomElement().GetPos(), "SoundEmanation", null, true);

        MapMasterScript.activeMap.musicCurrentlyPlaying = "dimriffboss";
        GameMasterScript.SetAnimationPlaying(false);

    }

    public static void BeginBoss3DialogueWithShara()
    {
        // Make sure any stairs leading to Ruined Passage redirect properly. 
        foreach (Map m in MapMasterScript.dictAllMaps.Values)
        {
            m.RedirectStairs(MapMasterScript.PRE_BOSS3_MEETSHARA_MAP_FLOOR, MapMasterScript.activeMap);
        }

        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            BeginBoss3Fight();
            return;
        }

        GameMasterScript.SetAnimationPlaying(true, true);

        MapMasterScript.singletonMMS.mapTileMesh.gameObject.GetComponent<SimpleTimedMove>().EndContinuousMovement();

        if (MapMasterScript.singletonMMS.secondaryTileMap != null)
        {
            MapMasterScript.singletonMMS.secondaryTileMap.gameObject.GetComponent<SimpleTimedMove>()
                .EndContinuousMovement();
        }

        // redirect there


        GameMasterScript.heroPCActor.myAnimatable.speedMultiplier = 1.0f;
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Idle", Directions.NORTH, Directions.NORTH, true);

        if (MapMasterScript.singletonMMS.secondaryTileMap != null)
        {
            GameObject.Destroy(MapMasterScript.singletonMMS.secondaryTileMap);
        }


        Conversation sharaConvo = GameMasterScript.FindConversation("shara_act_iii_dialog");

        Actor mnBoss = MapMasterScript.activeMap.FindActor("mon_ancientsteamgolem");
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            mnBoss.GetPos(), 1.75f, 0.25f, false);
        BossHealthBarScript.DisableBoss();

        mnBoss.myAnimatable.SetAnim("ShutdownIdle");

        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(sharaConvo, DialogType.KEYSTORY, null, 3.4f,
                "act3-boss-chamber-01"));
    }

    public static void Boss3_SharaWakesUpGolem()
    {
        UIManagerScript.myDialogBoxComponent.SetDelay(6.0f);
        GameMasterScript.StartWatchedCoroutine(Boss3_SharaWakesUpGolem_Coroutine());
    }

    static IEnumerator Boss3_SharaWakesUpGolem_Coroutine()
    {
        MusicManagerScript.singleton.Fadeout(0.5f);

        Actor bossActor = MapMasterScript.activeMap.FindActor("mon_ancientsteamgolem");
        Actor sharaActor = MapMasterScript.activeMap.FindActor("npc_shara_preboss3");

        //Launch the wakeup projectiles from the center mass of Shara
        Vector2 vProjectileStart = sharaActor.GetPos();
        vProjectileStart.y += 0.5f;

        //have shara launch some power into the golem
        int iNumShots = 0;
        int iMaxShots = 16;
        while (iNumShots < iMaxShots)
        {
            Vector2 vTargetPosition = bossActor.GetPos();
            vTargetPosition.x += Random.Range(-0.7f, 0.7f);
            vTargetPosition.y += Random.Range(0.3f, 0.6f);
            //GameObject shardProjectile = GameMasterScript.TDInstantiate("QiStrikeEffect");

            CombatManagerScript.GenerateEffectAnimation(vProjectileStart, vTargetPosition, GameMasterScript.GetEffectByRef("qistrike"), null );
            MusicManagerScript.PlayCutsceneSound("SharaEnergize");
            yield return new WaitForSeconds(0.07f);
            iNumShots++;
        }

        //lightning bursts forth from the golem
        iMaxShots = 6;
        iNumShots = 0;
        while (iNumShots < iMaxShots)
        {
            Vector2 vBoltDest = bossActor.GetPos() + Random.insideUnitCircle * Random.Range(0.1f, 0.3f);
            CombatManagerScript.GenerateDirectionalEffectAnimation(vBoltDest, bossActor.GetPos(), "LightningStrikeEffect", true);
            CombatManagerScript.GenerateSpecificEffectAnimation(vBoltDest, "StaticShockEffect", null, true);
            yield return new WaitForSeconds(0.05f);
            iNumShots++;
        }



        //wait
        yield return new WaitForSeconds(1.0f);

        //wakeup
        bossActor.myAnimatable.SetAnim("WakeUp");
        MusicManagerScript.PlayCutsceneSound("RobotWakeup");

        //Wait for the anim to finish
        yield return new WaitForSeconds(bossActor.myAnimatable.calcGameWaitTime);

        //flash and now be idle
        UIManagerScript.FlashWhite(0.25f);
        bossActor.myAnimatable.SetAnim("Idle");

		MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("sharaserious");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "sharaserious";


    }

    public static void BeginBoss3Fight()
    {
        GameMasterScript.SetAnimationPlaying(false);
        Monster golem = MapMasterScript.activeMap.FindActor("mon_ancientsteamgolem") as Monster;

        Actor shara = MapMasterScript.activeMap.FindActor("npc_shara_preboss3");

        if (shara != null)
        {
            shara.myMovable.FadeOutThenDie();
            MapMasterScript.activeMap.RemoveActorFromLocation(shara.GetPos(), shara);
            MapMasterScript.activeMap.RemoveActorFromMap(shara);
            MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(shara.GetObject());
            GameMasterScript.Destroy(shara.GetObject());
            CombatManagerScript.GenerateSpecificEffectAnimation(shara.GetPos(), "SmokePoof", null, true);
        }

        GameMasterScript.cameraScript.SetCustomCameraAnimation(golem.GetPos(), GameMasterScript.heroPCActor.GetPos(),
            0.75f);

        BossHealthBarScript.EnableBossWithAnimation(golem);
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("bosstheme2");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme2";
        ProgressTracker.SetProgress(TDProgress.BOSS3, ProgressLocations.HERO, 1);
    }

    public static void Preboss3WalkWithShara()
    {
        GameMasterScript.StartWatchedCoroutine(Preboss3WalkWithShara_Coroutine());
    }

    static IEnumerator Preboss3WalkWithShara_Coroutine()
    {
        //Set up our scrolling background map
        GameObject secondaryTileMap = Instantiate(MapMasterScript.singletonMMS.mapTileMesh.gameObject);
        MapMasterScript.singletonMMS.secondaryTileMap = secondaryTileMap;

        //make the hero walk
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Walk", Directions.EAST, Directions.EAST, true);
        GameMasterScript.heroPCActor.myAnimatable.speedMultiplier = 0.75f;
        var hero = GameMasterScript.heroPCActor;
        var vPos = hero.GetPos();
        float fHop = 1.0f;
        foreach (var pet in hero.summonedActors)
        {
            if (pet == null ||
                pet is Destructible ||
                pet.myMovable == null )
            {
                continue;
            }
            if (pet.GetObject() == null || pet.myMovable == null) // 312019 - if pets have no movable or aren't spawned, don't do anything
            {
                continue;
            }
            vPos.x--;
            pet.myMovable.SetPosition(vPos);
            pet.myMovable.StartCoroutine(Cutscenes.PetHopCoroutine(pet.myMovable, fHop));
            fHop += 0.2f;
        }
        
        Vector3 copyPos = MapMasterScript.singletonMMS.mapTileMesh.transform.position;
        copyPos.x += 46f;
        secondaryTileMap.transform.localPosition = copyPos;

        //scroll scroll scroll
        MapMasterScript.singletonMMS.mapTileMesh.gameObject.GetComponent<SimpleTimedMove>().BeginContinuousMovement(-0.03f, -46f, 46f);
        secondaryTileMap.gameObject.GetComponent<SimpleTimedMove>().BeginContinuousMovement(-0.03f, -46f, 46f);

        //wait a bit to let the scene set in
        yield return new WaitForSeconds(2.0f);

        //start talking
        Conversation sharaConvo = GameMasterScript.FindConversation("shara_act_iii_dialog");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(sharaConvo, DialogType.KEYSTORY, null, 0.6f,
                "act3-begin-hallway"));
    }

    static IEnumerator PetHopCoroutine(Movable pet, float fStartTicks)
    {
        float fTime = fStartTicks;
        Map currentMap = MapMasterScript.activeMap;
        var startOffset = pet.permanentYOffset;
        var petPos = pet.position;
        while (currentMap == MapMasterScript.activeMap)
        {
            while (fTime > 0f)
            {
                fTime -= Time.deltaTime;
                yield return null;
            }
            fTime = 0.3f;
            while (fTime > 0.0f)
            {
                float delta = fTime / 0.3f;
                pet.permanentYOffset = startOffset + Mathf.Sin(delta * 3.14f) * 0.75f;
                pet.SetPosition(petPos);
                fTime -= Time.deltaTime;
                yield return null;
            }
            pet.permanentYOffset = startOffset;
            pet.SetPosition(petPos);
            fTime = 8.0f;
        }
        pet.permanentYOffset = startOffset;
        pet.SetPosition(petPos);
    }

    //Let them walk in silence just for a little bit before the next beat
    public static void WalkWithShara_PauseBetweenConversationSteps()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        //hide the dialog for a bit
        float fDialogDelay = 3.0f;
        UIManagerScript.myDialogBoxComponent.SetDelay(fDialogDelay);
    }

    public static void DirtbeakLibraryIntro(Monster dirtbeak)
    {
        GameMasterScript.heroPCActor.SetActorData("dirtbeak_library", 1);

        GameMasterScript.SetAnimationPlaying(true, true);
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("funmerchant");

        singleton.StartCoroutine(singleton.DirtbeakLibrarySurprise(dirtbeak, 1.4f));
    }

    IEnumerator DirtbeakLibrarySurprise(Monster dirtbeak, float time)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.SetAnimationPlaying(true, true);
        dirtbeak.myAnimatable.SetAnim("TakeDamage");
        Conversation opening = GameMasterScript.FindConversation("dirtbeak_library_intro");        
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(opening, DialogType.KEYSTORY, null, 1.0f));
    }

    public static void PanToPercyForHelp()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC healer = MapMasterScript.activeMap.FindActor("npc_healer") as NPC;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            healer.GetPos(), 1.0f, 0.5f, false);

        Conversation healerHelp = GameMasterScript.FindConversation("healer_help_callout");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(healerHelp, DialogType.KEYSTORY, healer, 2f));
    }

    public static void StartJobTrialCallout()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC jorito = MapMasterScript.activeMap.FindActor("npc_weaponmaster") as NPC;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            jorito.GetPos(), 1.0f, 0.5f, false);

        Conversation jobtrials = GameMasterScript.FindConversation("jobtrial_callout");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(jobtrials, DialogType.KEYSTORY, jorito, 2f));
    }

    public static void IntroduceToWeaponMaster()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC jorito = MapMasterScript.activeMap.FindActor("npc_weaponmaster") as NPC;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            jorito.GetPos(), 1.0f, 0.5f, false);

        Conversation weaponmaster = GameMasterScript.FindConversation("weaponmaster_callout");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(weaponmaster, DialogType.KEYSTORY, jorito, 2f));
    }

    public static void DoPreBossFight2Stuff(Map boss2Map, bool objectsSpawned)
    {
        if (objectsSpawned)
        {
            ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 1);
        }        
        List<Actor> removeActors = new List<Actor>();
        List<Vector2> positions = new List<Vector2>();
        List<Actor> aggroActors = new List<Actor>();
        List<Vector2> positions2 = new List<Vector2>();
        foreach (Actor a in boss2Map.actorsInMap)
        {
            if (a.actorRefName == "shadowdummy")
            {
                if (objectsSpawned) GameMasterScript.AddToDeadQueue(a);
                positions2.Add(a.GetPos());
            }
            if (a.actorRefName == "mon_plundererboss")
            {
                removeActors.Add(a);
                positions.Add(a.GetPos());
            }
            if (a.actorRefName == "mon_fakeplunderer")
            {
                aggroActors.Add(a);
                if (objectsSpawned) a.myAnimatable.SetAnim("TakeDamage");
            }
        }

        foreach (Actor act in removeActors)
        {
            boss2Map.RemoveActorFromMap(act);
            Destroy(act.GetObject());
        }

        Monster shadowDemon = null;

        // in NG++, add a couple more shadows!
        if (GameStartData.NewGamePlus == 2)
        {
            for (int i = 0; i < 2; i++)
            {
                MapTileData mtd = MapMasterScript.activeMap.GetRandomEmptyTile(positions.GetRandomElement(), 2, false, true, true, false, true);
                positions.Add(mtd.pos);
                Debug.Log("Added a new demon at " + mtd.pos);

            }
        }

        bool firstDemon = true;
        foreach (Vector2 v2 in positions)
        {
            shadowDemon = MonsterManagerScript.CreateMonster("mon_shadowelementalboss", true, true, false, 0f, false);
            if (RandomJobMode.IsCurrentGameInRandomJobMode() && firstDemon)
            {
                RandomJobMode.TryAddRelicTreasureToBoss(shadowDemon, 1.4f);
            }
            boss2Map.PlaceActor(shadowDemon, MapMasterScript.GetTile(v2));
            if (objectsSpawned) GameMasterScript.mms.SpawnMonster(shadowDemon);
            foreach (Actor dummy in aggroActors)
            {
                shadowDemon.AddAggro(dummy, 1000f);
            }
            firstDemon = false;
        }

        foreach (Vector2 v2 in positions2)
        {
            Destructible dtNew =
                boss2Map.CreateDestructibleInTile(MapMasterScript.GetTile(v2), "obj_mon_evokeshadow");
            dtNew.actorfaction = Faction.ENEMY;
            StatusEffect se = dtNew.dtStatusEffect;
            foreach (EffectScript eff in se.listEffectScripts)
            {
                eff.originatingActor = shadowDemon;
                eff.originatingActorUniqueID = shadowDemon.actorUniqueID;
            }
            if (objectsSpawned) GameMasterScript.mms.SpawnDestructible(dtNew);
        }

        if (objectsSpawned) GameMasterScript.gmsSingleton.ProcessDeadQueue(boss2Map);
    }

    public static void DoDirtbeakEscape()
    {
        ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 2);

		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("bosstheme1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme1";


        GameMasterScript gmsSingleton = GameMasterScript.gmsSingleton;
        Actor db = MapMasterScript.activeMap.FindActor("mon_banditwarlord");


        db.myMovable.AnimateSetPositionNoChange(new Vector3(db.GetPos().x + 27f, db.GetPos().y), 2.5f, false, 360f, 0f,
            MovementTypes.LERP);

        Actor nObj = MapMasterScript.activeMap.FindActor("obj_bossdevice");
        nObj.myMovable.AnimateSetPositionNoChange(new Vector3(nObj.GetPos().x + 27f, nObj.GetPos().y), 2.5f, false,
            360f, 0f, MovementTypes.LERP);

        singleton.StartCoroutine(singleton.WaitThenUnpauseAndRemoveActor(1.3f, db, db.GetPos()));
        singleton.StartCoroutine(singleton.WaitThenUnpauseAndRemoveActor(1.3f, nObj, nObj.GetPos()));

        Vector2 badHardcodedHeroPosition = new Vector2(10f, 6f);
        Vector2 cPos = new Vector2(GameMasterScript.cameraScript.gameObject.transform.position.x, GameMasterScript.cameraScript.gameObject.transform.position.y + 1f);

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(cPos, badHardcodedHeroPosition, 0.5f,
            1.5f);

        foreach (Actor act in MapMasterScript.activeMap.monstersInMap)
        {
            if (act.actorRefName == "mon_fakeplunderer")
            {
                CombatManagerScript.SpawnChildSprite("AggroEffect", act, Directions.NORTHEAST, false);
                CombatManagerScript.SpawnChildSprite("AggroEffect", act, Directions.NORTHWEST, false);
                act.myAnimatable.SetAnim("TakeDamage");
            }
            //What is this
            bool first = false;
            if (act.actorRefName == "mon_shadowelementalboss")
            {
                if (first)
                {
                    BossHealthBarScript.EnableBossWithAnimation(act as Monster);
                }
                first = true;
            }
        }
    }

    public static void StartGoldfrogSequence(NPC n)
    {
        n.myMovable.AnimateSetPositionNoChange(new Vector3(n.GetPos().x, n.GetPos().y + 18f), 2.5f, false, 360f, 0f,
            MovementTypes.LERP);
        CombatManagerScript.GenerateSpecificEffectAnimation(n.GetPos(), "TransparentHoly", null);
        singleton.StartCoroutine(singleton.WaitThenUnpauseAndRemoveActor(1.3f, n, n.GetPos()));
    }

    public IEnumerator WaitThenUnpauseAndRemoveActor(float time, Actor n, Vector2 originalPos)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.cameraScript.StopAnimationFromPlaying();
        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.gmsSingleton.turnExecuting = false;
        UIManagerScript.CloseDialogBox();
        TDInputHandler.EnableInput();
        MapMasterScript.activeMap.RemoveActorFromLocation(originalPos, n);
        MapMasterScript.activeMap.RemoveActorFromMap(n);
        if ((n.objectSet) && (n.GetObject().activeSelf))
        {
            Destroy(n.GetObject());
        }
    }

    public static void PlayerRestedAtFire()
    {
        UIManagerScript uims = GameMasterScript.uims;

        NPC sharkRobber = null;
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.actorRefName == "npc_muguzmo")
            {
                sharkRobber = act as NPC;
                break;
            }
        }

        if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            singleton.StartCoroutine(singleton.WaitThenRunSharaPowerFunction(4f));
        }

        if (sharkRobber == null)
        {
            UIManagerScript.singletonUIMS.WaitThenFadeIn(3.1f, 3f);
        }
        else
        {
            if (UnityEngine.Random.Range(0, 2) == 0) // Not robbed
            {
                GameMasterScript.SetAnimationPlaying(true);
                UIManagerScript.singletonUIMS.WaitThenFadeIn(3.1f, 3f);
                Conversation rob = GameMasterScript.FindConversation("campfirethief");
                GameMasterScript.heroPCActor.SetActorData("muguzmo_fire", 1);
                uims.StartCoroutine(uims.WaitThenStartConversation(rob, DialogType.STANDARD, sharkRobber, 6f));
            }
            else // Robbed
            {
                GameMasterScript.SetAnimationPlaying(true);
                Conversation rob = GameMasterScript.FindConversation("campfire_robbed");
                uims.StartCoroutine(uims.WaitThenStartConversation(rob, DialogType.STANDARD, sharkRobber, 3f));
                UIManagerScript.PlaceDialogBoxInFrontOfFade(true);
                int moneyLost = GameMasterScript.heroPCActor.GetMoney() / 4;
                GameMasterScript.heroPCActor.ChangeMoney(-1 * moneyLost);
            }
        }
    }

    public IEnumerator WaitThenRunSharaPowerFunction(float time)
    {
        yield return new WaitForSeconds(time);
        SharaModeStuff.SpawnLearnPowerDialog(sharaPowers: true);
    }

    public IEnumerator WaitThenFinalBoss2(float time)
    {
        yield return new WaitForSeconds(time);
        Stairs portalToBoss2 = new Stairs();
        portalToBoss2.autoMove = true;
        portalToBoss2.stairsUp = false;
        portalToBoss2.pointsToFloor = MapMasterScript.FINAL_BOSS_FLOOR2;
        portalToBoss2.NewLocation = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);
        portalToBoss2.newLocationID = portalToBoss2.NewLocation.mapAreaID;
        portalToBoss2.prefab = "Portal";
        MapTileData portalMTD = MapMasterScript.GetTile(new Vector2(8, 5));
        MapMasterScript.activeMap.PlaceActor(portalToBoss2, portalMTD);
        MapMasterScript.singletonMMS.SwitchMaps(MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2),
            Vector2.zero, false);
    }

    public static void StartBeastlakeParkSequence(NPC rancher)
    {
        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.FlashWhite(0.75f);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(
            GameMasterScript.FindConversation("dialog_mvrancher_learnmallet"), DialogType.STANDARD, rancher, 0.8f));
    }

    IEnumerator WaitThenCursorSound(string cueName, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        UIManagerScript.PlayCursorSound(cueName);
    }

    public static void WaitThenPlayCursorSound(string cueName, float waitTime)
    {
        singleton.StartCoroutine(singleton.WaitThenCursorSound(cueName, waitTime));
    }

    IEnumerator WaitThenFlashWhiteFromUIMS(float waitTime, float flashTime)
    {
        yield return new WaitForSeconds(waitTime);
        UIManagerScript.FlashWhite(flashTime);
    }

    public static void WaitThenFlashWhite(float waitTime, float flashTime)
    {
        singleton.StartCoroutine(singleton.WaitThenFlashWhiteFromUIMS(waitTime, flashTime));
    }

    IEnumerator WaitThenGenerateRewards(float time, Monster mn)
    {
        yield return new WaitForSeconds(time);
        CombatResultsScript.CheckForPlayerRewards(mn, mn.GetXPModToPlayer());
        LootGeneratorScript.TryGenerateLoot(mn, mn.GetPos());
    }

    public static void WaitThenGenerateMonsterRewards(float time, Monster mn)
    {
        singleton.StartCoroutine(singleton.WaitThenGenerateRewards(time, mn));
    }

    public static void BeginEndgameCutscene(bool sharaMode = false)
    {
        GameMasterScript.SetAnimationPlaying(true);
        GameMasterScript.heroPCActor.beatTheGame = true;
        UIManagerScript.FadeOut(2.0f);
        MusicManagerScript.singleton.FadeoutThenSetAllToZero(1.5f);
        singleton.StartCoroutine(singleton.WaitThenEnableEndSequence(2.05f, sharaMode));
    }

    IEnumerator WaitThenEnableEndSequence(float time, bool sharaMode)
    {
        yield return new WaitForSeconds(time);
        if (!sharaMode) // moved the save game stuff here from BEFORE stepping on stairs, so it doesnt cause a hiccup
        {
            GameMasterScript.gmsSingleton.SaveTheGame(autoSave: false);
        }
        UIManagerScript.endingCanvas.gameObject.SetActive(true);
        UIManagerScript.singletonUIMS.endingCutscene.gameObject.SetActive(true);
        UIManagerScript.singletonUIMS.endingCutscene.BeginEndSequence(0.05f);
    }

    public static void test_w3()
    {
        GameMasterScript.StartWatchedCoroutine(coroutine_test_w3());
    }

    public static void FlashRedAndShake()
    {
        UIManagerScript.FlashRed(0.2f);
        GameMasterScript.cameraScript.AddScreenshake(0.5f);
    }

    static IEnumerator coroutine_test_w3()
    {
        //move the hero three steps to the right
        Fighter hero = GameMasterScript.heroPCActor;
        Vector3 vPos = hero.GetPos();
        Vector3 vDest = vPos + new Vector3(3, 0, 0);

        //tells the game to move the actor's game location immediately -- no animations here
        MapMasterScript.singletonMMS.MoveAndProcessActor(vPos, vDest, hero);

        //then animus
        float fMoveTime = 5.0f;
        hero.myMovable.AnimateSetPosition(vDest, fMoveTime, true, 0f, 0f, MovementTypes.SMOOTH);

        //wait for hero to finish moving
        yield return new WaitForSeconds(fMoveTime);

        Directions oldDir = Directions.NORTH;
        for (int t = 0; t < 12; t++)
        {
            Directions newDir = (Directions) UnityEngine.Random.Range(0, 8);
            hero.myAnimatable.SetAnimDirectional("Attack", newDir, oldDir);
            oldDir = newDir;

            //Attack animations have a special cute little hardcoded variable that makes them never say animComplete,
            //but instead goes right back to Idle.
            while (!hero.myAnimatable.animComplete && !hero.myAnimatable.animPlaying.animName.Contains("Idle"))
            {
                yield return null;
            }

        }

        //spawn a different dialog
        UIManagerScript.StartConversationByRef("shara_act_iii_dialog", DialogType.STANDARD, null);
    }

    public static void WarpToSharaScrollingScene()
    {
        //UIManagerScript.CloseDialogBox();

        Map findNextMap = null;

        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            findNextMap = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR);
            TravelManager.TravelMaps(findNextMap, null, false);
            return;
        }

        findNextMap = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PRE_BOSS3_WALKTOBOSS_FLOOR);
        GameMasterScript.SetAnimationPlaying(true);

        TravelManager.TravelMaps(findNextMap, null, false);
    }

    public static void WarpFromHallwayToBoss3()
    {
        //UIManagerScript.CloseDialogBox();

        Map findNextMap = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR);
        GameMasterScript.SetAnimationPlaying(true);

        TravelManager.TravelMaps(findNextMap, null, false);
    }

    #region Final Boss Phase 1

    public static void debug_fb()
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPreBattle_Part1_Coroutine());
    }

    //Start with Shara trying to dominate the supervisor
    public static void FinalBossPreBattleCutscene_Part1()
    {
        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            Map theMap = MapMasterScript.activeMap;
            Actor sharaActor = theMap.FindActor("npc_shara_preboss3");
            Actor supervisorActor = theMap.FindActor("mon_finalbossai");
            List<Actor> listMonstersToActivate = ActivateMonstersInFinalBossRoom(theMap, supervisorActor);
            singleton.StartCoroutine(PlayMonsterEnablingAnimations(listMonstersToActivate, withFX: false));
            BossHealthBarScript.EnableBoss(supervisorActor as Monster);
            GameMasterScript.heroPCActor.SetActorData("finalboss1", 1); // fight has began! Let's fighting        

            RemoveSharaFromMapWithFX(sharaActor, theMap);
            return;
        }
        GameMasterScript.StartWatchedCoroutine(FinalBossPreBattle_Part1_Coroutine());
    }

    //play soft music, have the heroine walk forward towards the supervisor
    public static void FinalBossPreBattleCutscene_Part2()
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPreBattle_Part2_Coroutine());
    }

    public static void FinalBossPreBattleCutscene_End()
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPreBattle_End_Coroutine());
    }

    public static IEnumerator FinalBossPreBattle_Part1_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        //dramatis personae

        Actor sharaActor = null;
        bool sharaInMapAlready = false;
        foreach(Actor act in theMap.actorsInMap)
        {
            if (act.actorRefName == "npc_shara_preboss3")
            {
                sharaActor = act;
                sharaInMapAlready = true;
                break;
            }
        }
        if (sharaActor == null)
        {
            sharaActor = NPC.CreateNPC("npc_shara_preboss3");
        }
        
        Fighter supervisorActor = theMap.FindActor("mon_finalbossai") as Fighter;
        HeroPC heroActor = GameMasterScript.GetHeroActor() as HeroPC;

        //play Shara's serious business theme
		//MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("sharaserious");
        //MapMasterScript.activeMap.musicCurrentlyPlaying = "sharaserious";

        //move the hero out of the scene
        Vector2 vHeroSpawnPosition = heroActor.GetPos();
        Vector2 vHeroRunStart = vHeroSpawnPosition;
        vHeroRunStart.y -= 1f;
        heroActor.myMovable.AnimateSetPositionNoChange(vHeroRunStart, 0.1f, false, 0f, 0f, MovementTypes.LERP);

        //and all the health meters
        BossHealthBarScript.DisableBoss();
        supervisorActor.SetHealthBarVisibility(false);
        heroActor.SetHealthBarVisibility(false);

        //pan the camera in slowly from top of map down to boss and Shara
        Vector2 vSupervisorPosition = supervisorActor.GetPos();
        Vector2 vCameraStart = vSupervisorPosition;
        vCameraStart.y += 9.0f;
        Vector2 vCameraDest = vSupervisorPosition;
        vCameraDest.y += 1.0f;

        float fCameraPanTime = 5.0f;
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(vCameraStart, vCameraDest, fCameraPanTime, 0.1f,
            true);

        //Place Shara in the world

        Vector2 vSharaSpawnPos = vSupervisorPosition;

        if (!sharaInMapAlready)
        {            
            vSharaSpawnPos.y -= 2;
            MapMasterScript.singletonMMS.SpawnNPC(sharaActor);
            theMap.PlaceActor(sharaActor, theMap.GetTile(vSharaSpawnPos));
        }

        //woosh
        sharaActor.GetObject().AddComponent<AfterImageCreatorScript>();

        //have Shara fly around blasting the Supervisor
        float fShootTime = fCameraPanTime + 4.0f;
        ImpactCoroutineWatcher watcherSharaFlyAbout;
        watcherSharaFlyAbout =
            GameMasterScript.StartWatchedCoroutine(
                SharaFlyAroundSupervisor_Coroutine(sharaActor, vSupervisorPosition, fShootTime));

        //while that's happening, make sure the Supervisor has some sadness going on
        Vector2 vSupervisorSmokeCenter = vSupervisorPosition;
        //closer to the top of the creature
        vSupervisorSmokeCenter.y += 1.0f;
        GameMasterScript.StartWatchedCoroutine(PlayEffectLoopInArea_Coroutine("SmokeSpewEffect", vSupervisorSmokeCenter,
            1.25f, fShootTime + 5.0f, 0.2f, 0.8f));

        //wait for the shooting to end
        while (!watcherSharaFlyAbout.IsFinished())
        {
            yield return null;
        }

        //when the shooting stops, move Shara back to a slight offset from the Supervisor
        vSharaSpawnPos.x -= 2f;
        vSharaSpawnPos.y += 1f;
        sharaActor.myMovable.AnimateSetPositionNoChange(vSharaSpawnPos, 2.0f, false, 0f, 0f, MovementTypes.LERP);

        //have the hero come running in
        heroActor.lastMovedDirection = Directions.NORTH;
        heroActor.myMovable.AnimateSetPosition(vHeroSpawnPosition, 2.0f, true, 0f, 0f, MovementTypes.SMOOTH);

        //move the camera from where it ended last time to one tile down
        Vector2 vCamNewPos = vCameraDest;
        vCameraDest.y -= 1f;
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(vCamNewPos, vCameraDest,
            2.0f, 0.1f, true);

        //wait for all that to end
        yield return new WaitForSeconds(3.0f);

        //talky talk
        Conversation thisJibba = GameMasterScript.FindConversation("final_prebattle_part_1");
        UIManagerScript.StartConversation(thisJibba, DialogType.KEYSTORY, (NPC) sharaActor);
        UIManagerScript.Dialog_SetScriptOnConvoEnd("FinalBossPreBattleCutscene_Part2");

    }

    private static IEnumerator FinalBossPreBattle_Part2_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        //dramatis personae
        Actor sharaActor = NPC.CreateNPC("npc_shara_preboss3");
        Actor supervisorActor = theMap.FindActor("mon_finalbossai");
        Actor heroActor = GameMasterScript.GetHeroActor();

        //play soft kind music
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("titlescreen");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "titlescreen";


        //walk the hero towards the supervisor
        Vector2 vHeroWalkDest = supervisorActor.GetPos();
        vHeroWalkDest.y -= 1f;

        float fWalkTime = 5.0f;
        heroActor.lastMovedDirection = Directions.NORTH;
        heroActor.myMovable.AnimateSetPosition(vHeroWalkDest, fWalkTime, true, 0, 0, MovementTypes.SMOOTH);

        yield return new WaitForSeconds(fWalkTime + 2.0f);

        //more jibba!
        Conversation thisJibba = GameMasterScript.FindConversation("final_prebattle_part_2");
        UIManagerScript.StartConversation(thisJibba, DialogType.KEYSTORY, (NPC) sharaActor);

        //When this is over, we need to summon the monsters and start the fight.
        UIManagerScript.Dialog_SetScriptOnConvoEnd("FinalBossPreBattleCutscene_End");

    }

    static List<Actor> ActivateMonstersInFinalBossRoom(Map theMap, Actor supervisorActor)
    {
        //Grab all the monsters we have hidden in the wings
        List<Actor> listMonstersToActivate = new List<Actor>();
        foreach (var a in theMap.actorsInMap)
        {
            if (a is Monster && !a.actorEnabled && a != supervisorActor)
            {
                listMonstersToActivate.Add(a);
            }
        }
        return listMonstersToActivate;
    }

    static IEnumerator PlayMonsterEnablingAnimations(List<Actor> listMonstersToActivate, bool withFX)
    {
        //as we pan down, activate the monsters
        foreach (var a in listMonstersToActivate)
        {
            //wake it up
            a.EnableActor();

            if (!withFX) continue;
            //play a little foom 
            CombatManagerScript.GenerateSpecificEffectAnimation(a.GetPos(), "MetalPoof", null, true);

            //wait for the next one
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }

    private static IEnumerator FinalBossPreBattle_End_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        //dramatis personae
        Actor sharaActor = theMap.FindActor("npc_shara_preboss3");
        Actor supervisorActor = theMap.FindActor("mon_finalbossai");
        Actor heroActor = GameMasterScript.GetHeroActor();

        List<Actor> listMonstersToActivate = ActivateMonstersInFinalBossRoom(theMap, supervisorActor);

        //pan the camera down to the start position
        Vector2 vCameraDest = heroActor.GetPos();
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(vCameraDest, 3.0f, 0.0f);

        yield return PlayMonsterEnablingAnimations(listMonstersToActivate, withFX: true);



        //poof poof paf

        //when this is done, back to work.
        yield return new WaitForSeconds(1.0f);

        //Restore the boss meter
        BossHealthBarScript.EnableBoss(supervisorActor as Monster);

        GameMasterScript.heroPCActor.SetActorData("finalboss1", 1); // fight has began! Let's fighting        

        RemoveSharaFromMapWithFX(sharaActor, theMap);
    }

    static void RemoveSharaFromMapWithFX(Actor sharaActor, Map theMap)
    {
        //Remove this Shara, we'll make another one later
        if (sharaActor == null) return;
        CombatManagerScript.GenerateSpecificEffectAnimation(sharaActor.GetObject().transform.position, "TeleportUp", null, true);
        sharaActor.myMovable.FadeOutThenDie();
        sharaActor.DisableActor();
        theMap.RemoveActorFromMap(sharaActor);
    }

    static IEnumerator SharaFlyAroundSupervisor_Coroutine(Actor sharaActor, Vector2 vSupervisorPosition,
        float fTotalTime)
    {
        Movable m = sharaActor.myMovable;
        float fTime = 0f;
        Vector2 vOrigin = sharaActor.GetPos();
        var trailMaker = sharaActor.GetObject().GetComponent<AfterImageCreatorScript>();

        //Have her fly from spot to spot, shooting lightning when she arrives
        while (fTime < fTotalTime)
        {
            float fDashTime = Random.Range(0.3f, 0.8f);

            //pick a spot near the supervisor
            Vector2 vDestNearSupervisor = Random.insideUnitCircle.normalized * Random.Range(2.0f, 4.0f);

            //But always below it
            vDestNearSupervisor.y = Mathf.Abs(vDestNearSupervisor.y) * -1f;

            //we were an offset, now we're a location
            vDestNearSupervisor += vSupervisorPosition;

            //move
            m.AnimateSetPositionNoChange(vDestNearSupervisor, fDashTime, false, 0f, 0f, MovementTypes.LERP);

            //woosh!
            trailMaker.Initialize(vDestNearSupervisor, fDashTime, Vector2.Distance(vDestNearSupervisor, vOrigin), m.sr,
                false, 0.5f);

            //wait
            yield return new WaitForSeconds(fDashTime + 0.2f);

            //shoot
            float fWaitBetweenShots = 0.1f;
            int iNumShots = Random.Range(1, 4);
            for (int t = 0; t < iNumShots; t++)
            {
                //don't always hit the direct center
                Vector2 vBoltDest = vSupervisorPosition + Random.insideUnitCircle * Random.Range(0.2f, 0.5f);
                CombatManagerScript.GenerateDirectionalEffectAnimation(vBoltDest, vDestNearSupervisor,
                    "LightningStrikeEffect", true);

                //on each hit, play some impact on the Supervisor
                CombatManagerScript.GenerateSpecificEffectAnimation(vBoltDest, "ConcussionEffect", null, true);

                //wait for the next shot
                yield return new WaitForSeconds(fWaitBetweenShots);
            }

            //wait
            yield return new WaitForSeconds(0.5f);

            //this is where we are now.
            vOrigin = vDestNearSupervisor;

            //loop -- the dash time, plus the delays from the shooting.
            fTime += fDashTime + 0.5f + fWaitBetweenShots * iNumShots;

        }
    }

    //Play a given effect around a point over and over for a set amount of time.
    static IEnumerator PlayEffectLoopInArea_Coroutine(string strPrefab, Vector2 vCenterPoint, float fMaxDriftFromCenter,
        float fTotalLoopTime, float fMinTimeBetweenEffects, float fMaxTimeBetweenEffects)
    {
        float fTime = 0f;
        while (fTime < fTotalLoopTime)
        {
            float fWaitThisLoop = Random.Range(fMinTimeBetweenEffects, fMaxTimeBetweenEffects);
            Vector2 vEffectLocThisLoop =
                vCenterPoint + Random.insideUnitCircle.normalized * Random.Range(0f, fMaxDriftFromCenter);

            //point the effect away from the center
            Vector2 vAwayFromCenter = vEffectLocThisLoop + (vEffectLocThisLoop - vCenterPoint);

            CombatManagerScript.GenerateDirectionalEffectAnimation(vEffectLocThisLoop, vAwayFromCenter, strPrefab,
                true);
            yield return new WaitForSeconds(fWaitThisLoop);

            fTime += fWaitThisLoop;
        }
    }

    //When she flips out and decides that even though she has learned The Truth she dgaf and is gonna
    //do what she's sworn to do anyway
    public static void SharaAngryDuringPreFinalBattle()
    {
        Map theMap = MapMasterScript.activeMap;

        Actor heroActor = GameMasterScript.GetHeroActor();
        Actor sharaActor = theMap.FindActor("npc_shara_preboss3");

        //hide the dialog for a bit
        float fDialogDelay = 2.25f;
        UIManagerScript.myDialogBoxComponent.SetDelay(fDialogDelay);

        //flash screen red
        UIManagerScript.FlashRed(0.1f);

        //shake it up
        GameMasterScript.cameraScript.AddScreenshake(0.5f);

        //Shara blasts lightning at the player
        CombatManagerScript.GenerateDirectionalEffectAnimation(sharaActor.GetPos(), heroActor.GetPos(),
            "LightningStrikeEffect", true);

        //play some effects at the explosion point
        CombatManagerScript.WaitThenGenerateSpecificEffect(
            heroActor.GetPos() + Random.insideUnitCircle * Random.Range(0.1f, 0.25f), "SmallExplosionEffect", null, 0f,
            true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(
            heroActor.GetPos() + Random.insideUnitCircle * Random.Range(0.1f, 0.25f), "SmallExplosionEffect", null,
            0.1f, true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(
            heroActor.GetPos() + Random.insideUnitCircle * Random.Range(0.1f, 0.25f), "SmallExplosionEffect", null,
            0.15f, true);

        //send her flying backwards
        Vector2 vHeroKBPos = heroActor.GetPos();
        vHeroKBPos.y = 3;
        heroActor.myMovable.AnimateSetPosition(vHeroKBPos, 0.5f, false, 0, 0, MovementTypes.TOSS);

        MusicManagerScript.singleton.SetAllVolumeToZero();
        MusicManagerScript.singleton.WaitThenPlay("bosstheme2", fDialogDelay, true);
    }

    #endregion

    //March through the actors on the map and remove all...
    // Destructibles that were summoned by something
    // Monsters that aren't aligned with the player
    public static void ClearOutMonstersAndDestructiblesFromCutscene()
    {
        Map theMap = MapMasterScript.activeMap;
        List<Actor> toRemove = new List<Actor>();

        foreach (Actor a in theMap.actorsInMap)
        {
            bool bShouldDestroy = false;

            if (a is Destructible && a.summoner != null)
            {
                bShouldDestroy = true;
            }
            else if (a is Monster)
            {
                Monster m = a as Monster;
                if (m.actorfaction != Faction.PLAYER)
                {
                    bShouldDestroy = true;
                }
                else if (m.turnsToDisappear > 0)
                {
                    // summoned pets, let's get rid of 'em?
                    GameMasterScript.heroPCActor.RemoveSummon(m);
                    bShouldDestroy = true;
                }
            }


            if (bShouldDestroy)
            {
                toRemove.Add(a);
            }
        }

        foreach(Actor a in toRemove)
        {
            //GameMasterScript.AddToDeadQueue(a, true);
            MapMasterScript.activeMap.RemoveActorFromLocation(a.GetPos(), a);
            MapMasterScript.activeMap.RemoveActorFromMap(a);
            a.myMovable.FadeOutThenDie();
            /* if (a.myMovable != null)
            {
                a.myMovable.FadeOutThenDie();
            }
            a.DisableActor(); */

            //Debug.Log("Disabling " + a.actorRefName + " " + a.actorUniqueID);
        }

    }

    //This will move all the treasure on the ground towards the player and put it
    //in her inventory. Useful when you kill a boss and want to run a cutscene
    public static IEnumerator SendAllLootToPlayerForCutscene(float fTossDelay = 0.25f, bool bStaggerToss = false)
    {
        Map theMap = MapMasterScript.activeMap;
        HeroPC hero = GameMasterScript.heroPCActor;

        foreach (Actor a in theMap.actorsInMap)
        {
            if (!(a is Item))
            {
                continue;
            }

            Item itam = a as Item;

            //send it flying over
            Vector2 vItemStartPosition = itam.GetPos();
            CombatManagerScript.FireProjectile(vItemStartPosition, hero.GetPos(), itam.GetObject(), fTossDelay, false,
                null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);
            GameMasterScript.mms.MoveAndProcessActor(vItemStartPosition, hero.GetPos(), itam, true);

            //chill out for one frame if we want to stagger this stuff.
            if (bStaggerToss)
            {
                yield return null;
            }
        }

        //get looooooots
        yield return new WaitForSeconds(fTossDelay + 0.01f);
        TileInteractions.TryPickupItemsInHeroTile();
    }

    static void TryDesperatelyToPreventLevelUpDialog()
    {
        //Since this scene starts with a monster death, force close any dialogs
        //such as the levelup dialog which is eager and friendly
        UIManagerScript.CloseDialogBox();
        UIManagerScript.Dialog_ClearQueuedConversations();

        //And, for good measure, nuke this coroutine if it is running.
        GameMasterScript.StopWatchedCoroutine("WaitThenStartConversation");
    }

    #region Final Boss Phase 2

    public static void FinalBossPhase2_Part1(Vector2 vSupervisorPosition)
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPhase2_Part1_Coroutine(vSupervisorPosition));
    }

    public static void FinalBossPhase2_Part2()
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPhase2_Part2_Coroutine());
    }

    public static void FinalBossPhase2_Part3()
    {
        GameMasterScript.StartWatchedCoroutine(FinalBossPhase2_Part3_Coroutine());
    }

    public static void FinalBossPhase2_Part4()
    {
        GameMasterScript.heroPCActor.SetActorData("finalboss2", 1); // Fight Has Began

        //Just send the camera down to the player
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(GameMasterScript.heroPCActor.GetPos(), 0.5f,
            0.01f);

        Monster mnBoss = MapMasterScript.activeMap.FindActor("mon_finalboss2") as Monster;

        BossHealthBarScript.EnableBossWithAnimation(mnBoss);
    }

    static IEnumerator FinalBossPhase2_Part1_Coroutine(Vector2 vSupervisorPosition)
    {
        //Since this scene starts with a monster death, force close any dialogs
        //such as the levelup dialog which is eager and friendly
        TryDesperatelyToPreventLevelUpDialog();

        Map theMap = MapMasterScript.activeMap;

        //remove all energy tiles and remaining monsters
        ClearOutMonstersAndDestructiblesFromCutscene();

        //turn the music down
        MusicManagerScript.singleton.Fadeout(0.5f);

        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            GameMasterScript.StartWatchedCoroutine(SendAllLootToPlayerForCutscene(0.2f, true));

            //Now wait a touch more
            yield return new WaitForSeconds(1.5f);
            yield return singleton.WaitThenFinalBoss2(0.25f);
            yield break;
        }

        //dramatis personae
        Actor heroActor = GameMasterScript.GetHeroActor();
        Actor sharaActor = NPC.CreateNPC("npc_shara_preboss3");
        Actor supervisorActor =
            MonsterManagerScript.CreateMonster("mon_finalbossai", false, false, true, 0f, 0f, false);

        UIManagerScript.PlayCursorSound("ExplosionRumbles");

        //do a quick flash white to indicate the combat is over
        UIManagerScript.FlashWhite(0.5f);
        yield return new WaitForSeconds(0.2f);

        UIManagerScript.FlashWhite(0.3f);
        yield return new WaitForSeconds(0.1f);

        UIManagerScript.FlashWhite(1.0f);
        yield return null;

        //return Shara to the board
        Vector2 vSharaSpawnPos = vSupervisorPosition;
        vSharaSpawnPos.y -= 2;
        theMap.PlaceActor(sharaActor, theMap.GetTile(vSharaSpawnPos));
        MapMasterScript.singletonMMS.SpawnNPC(sharaActor);

        //play a little poofy poof on her
        CombatManagerScript.GenerateSpecificEffectAnimation(sharaActor.GetPos(), "TeleportUp", null, true);

        //Place a Supervisor clone on the board
        theMap.PlaceActor(supervisorActor, theMap.GetTile(vSupervisorPosition));
        MapMasterScript.singletonMMS.SpawnMonster(supervisorActor);

        //Move the player into position
        Vector2 vHeroDest = vSupervisorPosition;
        vHeroDest.y -= 4;
        heroActor.myMovable.AnimateSetPosition(vHeroDest, 0.5f, false, 0, 0, MovementTypes.TOSS);

        //point the Camera at the scene near Shara
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(vSharaSpawnPos, 0.5f, 0.01f);

        //start playing a hurty effect on the Supervisor
        CombatManagerScript.GenerateSpecificEffectAnimation(vSupervisorPosition, "SmallExplosionEffectSystem", null,
            true);

        //give it a second, then pull all the loot to the player
        yield return new WaitForSeconds(1.0f);
        GameMasterScript.StartWatchedCoroutine(SendAllLootToPlayerForCutscene(0.2f, true));

        //Now wait a touch more
        yield return new WaitForSeconds(3.0f);

        //Open the dialog where Shara says bla bla bla true form.
        Conversation jibba = GameMasterScript.FindConversation("final_battle_phase_2_part_1");
        UIManagerScript.StartConversation(jibba, DialogType.KEYSTORY, (NPC) sharaActor);
        UIManagerScript.Dialog_SetScriptOnConvoEnd("FinalBossPhase2_Part2");
    }

    static IEnumerator FinalBossPhase2_Part2_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        Actor heroActor = GameMasterScript.GetHeroActor();
        Actor sharaActor = theMap.FindActor("npc_shara_preboss3");

        //make rays of light appear behind shara over time
        GameMasterScript.StartWatchedCoroutine(RaysOfLightOnShara(sharaActor, 1.35f, 8));

        UIManagerScript.PlayCursorSound("LaserEnergyBuildup");

        //move her upwards
        Vector2 vSharaDest = sharaActor.GetPos();
        vSharaDest.y += 3.0f;
        sharaActor.myMovable.AnimateSetPositionNoChange(vSharaDest, 5.0f, false, 0, 0, MovementTypes.SMOOTH);

        //and the camera
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(vSharaDest, 5.0f, 0f, true);

        //start chaining explosions around the room
        GameMasterScript.StartWatchedCoroutine(ExplosionsAroundBossRoom(vSharaDest, 11, 1.25f, 0.15f));

        //chill while this cool shit happens
        yield return new WaitForSeconds(5.0f);

        //eventually fade up to white
        float fFadeToWhiteTime = 5.0f;
        UIManagerScript.FadeWhiteOutAndIn(fFadeToWhiteTime * 2);

        //then move to next map
        GameMasterScript.StartWatchedCoroutine(singleton.WaitThenFinalBoss2(fFadeToWhiteTime));

    }

    static IEnumerator FinalBossPhase2_Part3_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        //dramatis personae
        Actor heroActor = GameMasterScript.GetHeroActor();
        Actor bossActor = theMap.FindActor("mon_finalboss2");
        Actor hiddenSharaActor = NPC.CreateNPC("npc_shara_preboss3");

        //fade in is already happening

        //point camera at boss
        Vector2 vBossLocation = bossActor.GetPos();
        GameMasterScript.cameraScript.SetCustomCameraAnimation(vBossLocation, vBossLocation, 0.01f, true);

        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            yield return new WaitForSeconds(2.0f);
            FinalBossPhase2_Part4();
            yield break;
        }

        //let her sink in
        yield return new WaitForSeconds(6.0f);

        //Shara and Mirai exchange a few last jibs
        Conversation jibba = GameMasterScript.FindConversation("final_battle_phase_2_part_2");
        UIManagerScript.StartConversation(jibba, DialogType.KEYSTORY, (NPC) hiddenSharaActor);

        //fight fight fight -- all this does is point the camera at the player
        UIManagerScript.Dialog_SetScriptOnConvoEnd("FinalBossPhase2_Part4");
    }



    //Shara glows with crazy radiance as she ascends
    static IEnumerator RaysOfLightOnShara(Actor sharaActor, float fInitialDelay, int iMaxRays)
    {
        int iNumRays = 0;
        float fCurrentDelay = fInitialDelay;
        while (iNumRays < iMaxRays)
        {
            //make a ray
            GameObject rayObject = GameMasterScript.TDInstantiate("RayOfLightEffect");
            rayObject.transform.SetParent(sharaActor.GetObject().transform);
            rayObject.transform.localPosition = Vector3.zero;

            //sound? 
            MusicManagerScript.PlayCutsceneSound("SpawnRayOfLight");

            //count up
            iNumRays++;

            //wait
            yield return new WaitForSeconds(fCurrentDelay);

            //wait less next time 
            fCurrentDelay *= Random.Range(0.6f, 0.8f);
        }
    }

    //As Shara ascends to crazytown, the room begins to explode
    static IEnumerator ExplosionsAroundBossRoom(Vector2 vCentralPosition, int iMaxExplosions,
        float fStartingDelayBetweenFooms, float fDelayReductionPerFoom)
    {
        int iFoomCount = 0;
        while (iFoomCount < iMaxExplosions)
        {
            iFoomCount++;

            //pick a spot near the supervisor to start blowing up
            Vector2 vFoomDest = vCentralPosition + Random.insideUnitCircle.normalized * Random.Range(3.0f, 6.0f);
            CombatManagerScript.GenerateSpecificEffectAnimation(vFoomDest, "SmallExplosionEffectSystem", null, true);

            //shake_the_street.mp3
            GameMasterScript.cameraScript.AddScreenshake(0.2f);

            //Wait for the next one, but wait less time
            yield return new WaitForSeconds(fStartingDelayBetweenFooms);
            fStartingDelayBetweenFooms -= fDelayReductionPerFoom;

            if (fStartingDelayBetweenFooms < 0.2f)
            {
                fStartingDelayBetweenFooms = 0.2f;
            }
        }

        //shake until the transition ends
        GameMasterScript.cameraScript.AddScreenshake(3.0f);
    }

    #endregion

    #region Post Final Boss

    public static void PostFinalBoss_Part1()
    {
        GameMasterScript.StartWatchedCoroutine(PostFinalBoss_Part1_Coroutine());
    }

    public static void PostFinalBoss_Part2()
    {
        GameMasterScript.StartWatchedCoroutine(PostFinalBoss_Part2_Coroutine());
    }

    static IEnumerator PostFinalBoss_Part1_Coroutine()
    {
        //Since this scene starts with a monster death, force close any dialogs
        //such as the levelup dialog which is eager and friendly
        TryDesperatelyToPreventLevelUpDialog();

        Map theMap = MapMasterScript.activeMap;

        //dramatis personae
        Actor heroActor = GameMasterScript.GetHeroActor();
        Actor bossActor = theMap.FindActor("mon_finalboss2");

        //quiet the music down
        MusicManagerScript.singleton.FadeoutThenSetAllToZero(0.8f);

        //pan over to the boss from our position
        GameMasterScript.cameraScript.SetCustomCameraAnimation(heroActor.GetPos(), bossActor.GetPos(), 2f);

        //start blowing up, then throw out some triangles 
        CombatManagerScript.GenerateSpecificEffectAnimation(new Vector2(bossActor.GetPos().x, bossActor.GetPos().y + 2f), "SmallExplosionEffectSystem", null, true);

        //Keep track of the data indicating we won
        ProgressTracker.SetProgress(TDProgress.BOSS4_PHASE2, ProgressLocations.BOTH, 2);

        //Build up noise
        UIManagerScript.PlayCursorSound("Massive Buildup");

        //shake and speed up
        GameMasterScript.StartWatchedCoroutine(FinalBossExplosionPrep(bossActor, 4.8f));

        //wait five seconds
        yield return new WaitForSeconds(5.0f);

        //play the shara death noise and vfx
        UIManagerScript.PlayCursorSound("SharaDeath");
        CombatManagerScript.GenerateSpecificEffectAnimation(bossActor.GetPos(), "MassiveShatterParticles", null, false);
        UIManagerScript.FlashWhite(3f);

        //Clear out the final boss actor
        Monster fboss2 = bossActor as Monster; //GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("finalboss_id")) as Monster;
        CombatResultsScript.CheckForPlayerRewards(fboss2, fboss2.GetXPModToPlayer());
        //LootGeneratorScript.TryGenerateLoot(fboss2, fboss2.GetPos());
        CombatResultsScript.CompletelyDestroyMonsterAndObject(theMap, fboss2);
        CombatResultsScript.DoMonsterDeathFX(fboss2, false);

        //spawn in a fallen Shara
        NPC fallenShara = NPC.CreateNPC("npc_fallen_shara");
        Vector2 vSharaSpawnPos = bossActor.GetPos();
        theMap.PlaceActor(fallenShara, theMap.GetTile(vSharaSpawnPos));
        MapMasterScript.singletonMMS.SpawnNPC(fallenShara);

        //Wait a beat for the triangles to fly off
        yield return new WaitForSeconds(2.0f);

        //sad music
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("sadness");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "sadness";


        // No need to walk to shara if we're already there.
        if (MapMasterScript.GetGridDistance(heroActor.GetPos(), vSharaSpawnPos) > 1)
        {
            //walk the player to the boss
            float fWalkTime = (heroActor.GetPos() - vSharaSpawnPos).magnitude;
            fWalkTime = Mathf.Min(fWalkTime, 5.0f);

            GameMasterScript.StartWatchedCoroutine(MoveHeroToFallenShara(vSharaSpawnPos, fWalkTime));

            //When the flash ends and the hero reaches Shara, hold a conversation
            yield return new WaitForSeconds(fWalkTime + 1.0f);
        }
        else
        {
            yield return new WaitForSeconds(1.1f);
        }


        Conversation finalConvo = GameMasterScript.FindConversation("post_final_battle_dialog");
        UIManagerScript.StartConversation(finalConvo, DialogType.KEYSTORY, fallenShara);
        UIManagerScript.Dialog_SetScriptOnConvoEnd("PostFinalBoss_Part2");

    }


    static IEnumerator PostFinalBoss_Part2_Coroutine()
    {
        Map theMap = MapMasterScript.activeMap;

        //dramatis personae
        Actor sharaActor = theMap.FindActor("npc_fallen_shara");
        Vector2 vSharaLoc = sharaActor.GetPos();

        //flash and make her vanish
        UIManagerScript.FlashWhite(0.25f);
        //play sparkles
        CombatManagerScript.GenerateSpecificEffectAnimation(sharaActor.GetPos(), "FadeAwayParticles", null, false);

        //let the sparkles sparkle for a bit before we fade her out
        yield return new WaitForSeconds(1.0f);

        //ok bye
        float fSharaFadeTime = 5.0f;
        sharaActor.myMovable.ForceFadeOut(fSharaFadeTime);
        theMap.RemoveActorFromLocation(sharaActor.GetPos(), sharaActor);
        theMap.RemoveActorFromMap(sharaActor);
        sharaActor.myMovable.AnimateSetPositionNoChange(new Vector2(vSharaLoc.x, vSharaLoc.y + 0.3f), fSharaFadeTime, false, 0, 0, MovementTypes.SMOOTH );

        yield return new WaitForSeconds(fSharaFadeTime);
        UIManagerScript.FlashWhite(0.5f);
        UIManagerScript.PlayCursorSound("Mirage");

        //Create the exit stairs for the player to walk in to
        Stairs endGameStairs = new Stairs();
        endGameStairs.prefab = "FutureStairsUp";
        endGameStairs.pointsToFloor = 999;
        endGameStairs.NewLocation = MapMasterScript.singletonMMS.townMap;
        endGameStairs.newLocationID = MapMasterScript.singletonMMS.townMap.mapAreaID;
        endGameStairs.stairsUp = false;
        endGameStairs.SetSpawnPos(vSharaLoc);
        endGameStairs.SetActorData("finalstairs", 1);
        MapMasterScript.activeMap.PlaceActor(endGameStairs, MapMasterScript.GetTile(vSharaLoc));
        MapMasterScript.singletonMMS.SpawnStairs(endGameStairs);
        endGameStairs.UpdateSpriteOrder(); // :thonking:

        // If there is any chance at all we have multiple stairs in the map make sure they are all FinalStairs
        // Also just hide any stairs going down, why are those even there?
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.autoMove) continue;
            if (st.stairsUp)
            {
                st.DisableActor();
                continue;
            }
            endGameStairs.SetActorData("finalstairs", 1);
        }


        // SOMEHOW, there was an actor stuck on the stairs after beating the final boss!! How did this happen? I don't know
        // No errors and we don't have an output_log or a save file so no way to debug it
        // Here's a complete sanity check.

        MapTileData stairsTile = MapMasterScript.GetTile(vSharaLoc);
        List<Actor> toRemove = new List<Actor>();
        foreach (Actor act in stairsTile.GetAllActors())
        {
            if (act.actorfaction != Faction.PLAYER && act.GetActorType() != ActorTypes.ITEM && act.GetActorType() != ActorTypes.STAIRS)
            {
                // It's not an item, player-spawned thing, or the stairs themselves, so remove it.                
                toRemove.Add(act);
            }
        }
        foreach(Actor act in toRemove)
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(stairsTile.pos, act);
            MapMasterScript.activeMap.RemoveActorFromMap(act);
        }

        DLCManager.CreateRealmOfGodsStairsInFinalBossRoomIfAllowed();
    }

    static IEnumerator MoveHeroToFallenShara(Vector2 vFallenSharaPos, float fWalkTime)
    {
        Actor heroActor = GameMasterScript.GetHeroActor();
        Vector2 vHeroPos = heroActor.GetPos();

        Directions endFacing = vHeroPos.x > vFallenSharaPos.x ? Directions.WEST : Directions.EAST;

        //Walk to the spot next to Shara.
        Vector2 vNewHeroPos = vFallenSharaPos;
        vNewHeroPos.x += endFacing == Directions.EAST ? -1.0f: 1.0f;

        //set our initial facing to the direction from here to there
        heroActor.lastMovedDirection = CombatManagerScript.GetDirection(vHeroPos, vNewHeroPos);

        //walk that way
        heroActor.myMovable.AnimateSetPosition(vNewHeroPos, fWalkTime, true, 0f, 0f, MovementTypes.SMOOTH);
        yield return new WaitForSeconds(fWalkTime + 0.01f);

        //look at Shara. LOOK AT HER.
        heroActor.lastMovedDirection = endFacing;
        heroActor.myAnimatable.SetAnimDirectional("Idle", endFacing, Directions.NORTH);

    }

    static IEnumerator FinalBossExplosionPrep(Actor bossActor, float fTotalTime)
    {
        float fTime = 0f;
        while (fTime < fTotalTime)
        {
            float fDelta = (fTime / fTotalTime);
            bossActor.myAnimatable.speedMultiplier = 1.0f + fDelta * 10.0f;
            bossActor.myMovable.Jitter(0.01f);
            fTime += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    public static void DirtbeakLibraryTeachReading()
    {
        UIManagerScript.myDialogBoxComponent.SetDelay(5.0f);
        GameMasterScript.StartWatchedCoroutine(DirtbeakTeachReading_Coroutine());
    }

    static IEnumerator DirtbeakTeachReading_Coroutine()
    {
        UIManagerScript.FadeOut(1.0f);
        yield return new WaitForSeconds(2.0f);
        GameMasterScript.gmsSingleton.AwardJP(200.0f);
        GameLogScript.GameLogWrite( StringManager.GetString("dirtbeak_gains_rp"), GameMasterScript.heroPCActor);
        UIManagerScript.FadeIn(1.0f);

    }

    public static void TryBoss1IntroCutscene()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            DLCCutscenes.SharaBoss1Intro();
            return;
        }

        Actor mnBoss = MapMasterScript.activeMap.FindActor("mon_banditwarlord");

        if (GameEventsAndTriggers.ShouldCutscenesBeSkipped())
        {
            BossHealthBarScript.EnableBoss(mnBoss as Monster);
            ProgressTracker.SetProgress(TDProgress.BOSS1, ProgressLocations.HERO, 2);
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("status_storydefenseup", GameMasterScript.heroPCActor, 15);
            StringManager.SetTag(0, StringManager.GetString("misc_generic_defense").ToUpperInvariant());
            return;
        }

        GameMasterScript.SetAnimationPlaying(true, true);
        Conversation bConvo = GameMasterScript.FindConversation("first_boss_intro");

        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(bConvo, DialogType.KEYSTORY, null, 3.5f));        
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), mnBoss.GetPos(), 1.75f, 0.5f, false);
        BossHealthBarScript.DisableBoss();
    }


    /// <summary>
    /// Bamf the player to a new location
    /// </summary>
    /// <param name="destination">Where to?</param>
    /// <param name="delayBeforeCameraMove">How long before the camera moves over</param>
    /// <param name="cameraSpeed">Camera movement speed in units per second</param>
    /// <param name="effectRefName">The effect ref that teleported the player (may use different VFX)</param>
    /// <returns></returns>
    public static IEnumerator GenericTeleportPlayer(Vector2 destination, float delayBeforeCameraMove, float cameraSpeed, string effectRefName = "")
    {
        //place the hero someplace new
        var hero = GameMasterScript.heroPCActor;
        var startPos = hero.GetPos();
        var destTile = MapMasterScript.GetTile(destination);
        
        // make the hero invisible for now
        GameMasterScript.heroPCActor.myMovable.SetInSightAndSnapEnable(false);

        string departEffectRef = "TeleportUp";
        string arrivalEffectRef1 = "TeleportDown";
        string arrivalEffectRef2 = "GroundStompEffect";
        if (effectRefName == "entermudteleporttile")
        {
            departEffectRef = "MudExplosion";
            arrivalEffectRef1 = "MudExplosion";
        }

        //show vfx for departure
        CombatManagerScript.GenerateSpecificEffectAnimation(startPos, departEffectRef, null, true, 0f, false);

        // AA: My convoluted camera code makes this method not work correctly, so I've commented it out.
        //var moveSpeed = cameraSpeed * (startPos - destination).magnitude;
        //GameMasterScript.cameraScript.MoveCameraToPositionFromCurrentCameraPosition(destination, delayBeforeCameraMove, true);

        // Wait before moving sprites or camera
        yield return new WaitForSeconds(delayBeforeCameraMove);

        //this moves the actor
        GameMasterScript.mms.MoveAndProcessActor(startPos, destination, hero, false);

        //this moves the sprite on the actor
        hero.myMovable.AnimateSetPosition(destination, 0.01f, false, 0f, 0f, MovementTypes.LERP);

        // This method will initiate camera movement to catch up to hero, given a smoothing time of "delayBeforeCameraMove"
        CameraController.UpdateCameraPosition(destination, true, delayBeforeCameraMove);

        // The camera update actually takes longer than the desired anim time (delayBeforeCameraMove)
        // This is because of my smoothing code. Therefore, we should wait a bit MORE than that before doing the
        // effects and code on final destination
        yield return new WaitForSeconds(delayBeforeCameraMove * 1.25f);
        
        //show vfx at arrival
        CombatManagerScript.GenerateSpecificEffectAnimation(destination, arrivalEffectRef1, null, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(destination, arrivalEffectRef2, null, false, 0f, true); 

        //this ensures our FOV is updated upon arrival AFTER we teleport
        GameMasterScript.gmsSingleton.SetTempFloatData("bufferx", destination.x);
        GameMasterScript.gmsSingleton.SetTempFloatData("buffery", destination.y);
        TileInteractions.HandleEffectsForHeroMovingIntoTile(destTile, true);
        TileInteractions.CheckAndRunTileOnMove(destTile, hero);

        // and pop up the hero again!
        GameMasterScript.heroPCActor.myMovable.SetInSightAndSnapEnable(true);

       
        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.MONSTER) continue;            
            {
                MapTileData empty = MapMasterScript.activeMap.GetRandomEmptyTile(destination, 1, true, true, true, false, true);
                MapMasterScript.singletonMMS.MoveAndProcessActor(act.GetPos(), empty.pos, act, false);
                act.myMovable.ClearMovementQueue();
                act.myMovable.AnimateSetPosition(empty.pos, 0.01f, false, 0f, 0f, MovementTypes.LERP);
            }
        }
    }

    public static void StartWardrobeCallout()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC wardrobe = MapMasterScript.activeMap.FindActor("npc_wardrobe") as NPC;

        if (wardrobe == null) return;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            wardrobe.GetPos(), 1.0f, 0.5f, false);

        Conversation wardrobeConvo = GameMasterScript.FindConversation("tutorial_wardrobe");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(wardrobeConvo, DialogType.TUTORIAL, wardrobe, 2f));
    }

    public static void DoMonsterLetterTutorialCallout()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC jesse = MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            jesse.GetPos(), 1.0f, 0.5f, false);

        Conversation monsterLetterTutorial = GameMasterScript.FindConversation("tutorial_monsterletters");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(monsterLetterTutorial, DialogType.STANDARD, jesse, 2f));
    }

    public static void DoMonsterAttractionCallout()
    {
        GameMasterScript.SetAnimationPlaying(true);

        NPC jesse = MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC;

        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            jesse.GetPos(), 1.0f, 0.5f, false);

        Conversation monsterLetterTutorial = GameMasterScript.FindConversation("tutorial_monsterattraction");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(monsterLetterTutorial, DialogType.STANDARD, jesse, 2f));
    }
}

