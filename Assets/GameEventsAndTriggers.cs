using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
#endif

public partial class GameEventsAndTriggers : MonoBehaviour
{
    public static GameEventsAndTriggers singleton;
    public GameEventsAndTriggers()
    {
        singleton = this;
    }

    public static void CheckForBossClears()
    {
        if (!MapMasterScript.activeMap.IsBossFloor()) return;

        SharaModeStuff.CheckForBoss2Clear();
        SharaModeStuff.CheckForBoss2Clear();
    }

    /// <summary>
    /// Checks for scripts or triggerse that occur when a monster/boss hits a % health threshold for the first time
    /// </summary>
    /// <param name="mn"></param>
    public static void CheckForEventsOnMonsterDamage(Monster mn)
    {
        if (!GameEventsAndTriggers.ShouldCutscenesBeSkipped() && mn.isBoss && mn.actorRefName == "mon_banditwarlord" 
            && mn.actorfaction == Faction.ENEMY)
        {
            if (mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f && mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) >= 0.05f
                && GameMasterScript.heroPCActor.ReadActorData("banditbossinjured") != 1 && !mn.isInDeadQueue && !mn.destroyed &&
                ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) != 3)
            {
                if (Debug.isDebugBuild) Debug.Log("First boss wounded dialog is playing");
                Conversation c = GameMasterScript.FindConversation("first_boss_wounded");
                UIManagerScript.StartConversation(c, DialogType.KEYSTORY, null);
            }
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && SharaModeStuff.IsSharaModeActive())
        {
            if (mn.isBoss && mn.actorRefName == "mon_shara_finalboss" && mn.actorfaction == Faction.ENEMY)
            {
                if (mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f && mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) >= 0.01f &&
                    !mn.isInDeadQueue && GameMasterScript.heroPCActor.ReadActorData("finalbosshalfwaypoint") != 1)
                {
                    GameEventsAndTriggers.PrepareForSharaBoss4Phase2(mn);
                }
            }
        }
    }


    public static void RemoveTrapsFromBoss1Map()
    {
        List<Actor> toRemove = new List<Actor>();
        foreach(Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            if (act.actorRefName == "obj_floorspikes") toRemove.Add(act);
        }
        foreach(Actor act in toRemove)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(act);
            act.myMovable.FadeOutThenDie();
        }
    }

    public static void CheckForTown1MapEvents()
    {
        bool cutsceneStarted = false;

        GameMasterScript.heroPCActor.CheckForAndSetJobMasteryFlag();

        if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 3 
            && MetaProgressScript.ReadMetaProgress("corralquest") == -1 
            && MetaProgressScript.localTamedMonstersForThisSlot.Count < 3
            && !SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.ESCAPED_FROG)
            && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {
            // Begin escaped frog quest!
            Cutscenes.singleton.StartCoroutine(Cutscenes.singleton.StartEscapedFrogCutsceneInCamp());
            cutsceneStarted = true;
        }
        else if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) == 1 && !ShouldCutscenesBeSkipped() && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {
            // Callout telling you to go south, once you have received the Cube from dirtbeak
            Conversation cubeQuest = GameMasterScript.FindConversation("techcube_town");
            UIManagerScript.StartConversation(cubeQuest, DialogType.STANDARD, null);
            cutsceneStarted = true;
        }
        else if (!cutsceneStarted && GameMasterScript.heroPCActor.GetTotalJPGainedAndSpentInJob() >= 1000f &&
           GameMasterScript.heroPCActor.ReadActorData("weaponmaster_callout") < 1 && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {
            GameMasterScript.heroPCActor.SetActorData("weaponmaster_callout", 1);
            if (!ShouldCutscenesBeSkipped())
            {
                Cutscenes.IntroduceToWeaponMaster();
                cutsceneStarted = true;
            }
        }
        else if (GameplayScripts.DoesPlayerNeedHelpDueToLowResources() && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("need_percy_help") < 1)
            {
                GameMasterScript.heroPCActor.SetActorData("need_percy_help", 1);
                Cutscenes.PanToPercyForHelp();
                cutsceneStarted = true;
            }
            else if (GameMasterScript.heroPCActor.ReadActorData("need_percy_help") == 1)
            {
                GameMasterScript.heroPCActor.SetActorData("need_percy_help", 2);
            }
        }
        else if (ProgressTracker.CheckProgress(TDProgress.SHARA_TOWN_CALLOUT, ProgressLocations.META) != 1 && ProgressTracker.CheckProgress(TDProgress.SHARA_FIRST_MEETING, ProgressLocations.HERO) == 1 && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {            
            if (!ShouldCutscenesBeSkipped())
            {
                UIManagerScript.StartConversationByRef("self_callout_shara_town", DialogType.STANDARD, null);
                cutsceneStarted = true;
            }
            else
            {
                ProgressTracker.SetProgress(TDProgress.SHARA_TOWN_CALLOUT, ProgressLocations.META, 1);
            }

        }
        else  if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_cooking") && PlayerOptions.tutorialTips && !UIManagerScript.AnyInteractableWindowOpen() && GameMasterScript.heroPCActor.levelupBoostWaiting == 0)
        {
            int numIngredients = 0;
            foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
            {
                if (itm.IsItemFood())
                {
                    Consumable c = itm as Consumable;
                    numIngredients += c.Quantity;
                }
            }
            if (numIngredients >= 4)
            {
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_cooking");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                cutsceneStarted = true;
            }
        }
            
        if (RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            GameMasterScript.heroPCActor.SetActorData("jobtrial_callout", 1);
            MarkCurrentHeroJobMastered();
            
            GameMasterScript.heroPCActor.SetActorData("weaponmaster_callout", 1);
        }
        else if (GameMasterScript.heroPCActor.HasMasteredJob(GameMasterScript.heroPCActor.myJob))
        {
            if (GameMasterScript.heroPCActor.ReadActorData("jobtrial_callout") < 1 && !cutsceneStarted &&
                ProgressTracker.CheckProgress(TDProgress.BOSS1, ProgressLocations.HERO) >= 3)
            {
                if (!ShouldCutscenesBeSkipped())
                {
                    Cutscenes.StartJobTrialCallout();
                    cutsceneStarted = true;
                }
                GameMasterScript.heroPCActor.SetActorData("jobtrial_callout", 1);
                MarkCurrentHeroJobMastered();     
                GameMasterScript.heroPCActor.SetActorData("weaponmaster_callout", 1);
            }           
        }

        if (!cutsceneStarted)
        {
            GameMasterScript.tutorialManager.CheckForWardrobeTutorial();
        }
    }

    public static void MarkCurrentHeroJobNotMastered()
    {
        //if (Debug.isDebugBuild) Debug.Log("Hero current job not mastered");
        GameMasterScript.heroPCActor.SetActorData("mastered_current_job", 0);
    }

    public static void MarkCurrentHeroJobMastered()
    {
        //if (Debug.isDebugBuild) Debug.Log("Hero current job mastered");
        GameMasterScript.heroPCActor.SetActorData("mastered_current_job", 1);
    }

    public static void CheckForTown2MapEvents()
    {
        bool anyQuest = false;
        // Corral quest

        MetaProgressScript.UndestroyAllCorralMonsters();
        MetaProgressScript.ScatterCorralMonstersAroundTheMap();

        GameEventsAndTriggers.CheckForRiverstoneGroveSpawns();

        VerifyEscapedFrogQuestIsCompleteOrNot();

        MetaProgressScript.ClearQuestMarkersFromCorralPets();

        bool importantCutscene = false;


        if (!anyQuest && GameMasterScript.heroPCActor.myStats.GetLevel() >= 3 
            && MetaProgressScript.ReadMetaProgress("corralquest") == -1 
            && MetaProgressScript.localTamedMonstersForThisSlot.Count < 3 
            && !SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.ESCAPED_FROG))
        {
            bool doCutscene = !ShouldCutscenesBeSkipped();
            BeginEscapedFrogQuest(doCutscene);
            if (doCutscene)
            {
                anyQuest = true;
                importantCutscene = true;
            }
        }

        if (!anyQuest && ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) == 1)
        {
            // TODO - Replace machine graphics
            Cutscenes.DoTechCubeCutsceneInGrove();
            anyQuest = true;
            importantCutscene = true;
        }

        if (!anyQuest && !UIManagerScript.AnyInteractableWindowOpen() && DLCManager.DragonDefeatedCalloutPossible())
        {
            GameMasterScript.gmsSingleton.StartCoroutine(RobotDragonStuff.RobotDragonKickoff());
            anyQuest = true;
            importantCutscene = true;
        }

        if (!anyQuest && MetaProgressScript.localTamedMonstersForThisSlot.Count >= 2)
        {
            // We have not started the romance quest
            if (ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) <= 0 
                && GameMasterScript.heroPCActor.lowestFloorExplored >= 5 
                && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) != 1) // And we've never cooked a romantic meal
            {
                UIManagerScript.StartConversationByRef("quest_monster_romance", DialogType.KEYSTORY, MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC);
                anyQuest = true;
            }
            else if (ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) == 3 && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) != 1) // Got the petals, and we've never cooked a romantic meal
            {
                UIManagerScript.StartConversationByRef("quest_monster_romance_complete", DialogType.KEYSTORY, MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC);
                anyQuest = true;
            }
        }


        if (!anyQuest && MetaProgressScript.FoodCartCalloutPossible())
        {
            anyQuest = GameEventsAndTriggers.DoLangdonFoodCartCallout();
        }

        // UNCOMMENT THIS FOR NIGHTMARE THINGS
        if (!anyQuest && ProgressTracker.CheckProgress(TDProgress.SHADOWSHARDS, ProgressLocations.META) < 1
            && ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1)
        {
            int numRedShards = GameMasterScript.heroPCActor.myInventory.GetItemQuantity("item_shadoworb_piece");
            if (numRedShards >= 3)
            {
                UIManagerScript.StartConversationByRef("shadow_shard_callout", DialogType.KEYSTORY, null);
                anyQuest = true;
                importantCutscene = true;
            }
        }

        bool treeCalloutPossible = false;
        NPC memoryTree = MapMasterScript.activeMap.FindActor("npc_memorytree") as NPC;
        // Create tree of memories if needed

        if (MetaProgressScript.defeatHistory.Count > 0 && memoryTree == null)
        {
            memoryTree = NPC.CreateNPC("npc_memorytree");
            MapMasterScript.activeMap.PlaceActor(memoryTree, MapMasterScript.GetTile(new Vector2(37f, 16f)));
            MapMasterScript.singletonMMS.SpawnNPC(memoryTree);
            // Do callout here? Maybe?
            treeCalloutPossible = true;
        }
        else if (memoryTree != null)
        {
            treeCalloutPossible = true;
        }

        if (!anyQuest && treeCalloutPossible && MetaProgressScript.ReadMetaProgress("tree_callout") != 1 && !ShouldCutscenesBeSkipped())
        {
            DoMemoryTreeCallout(memoryTree);
            anyQuest = true;
            importantCutscene = true;
        }

        if (!anyQuest && FrogDragonStuff.FrogDragonQuestCalloutPossible())
        {
            FrogDragonStuff.DoFrogDragonQuestCallout();
            anyQuest = true;
            importantCutscene = true;
        }

        if (!anyQuest && GameMasterScript.heroPCActor.myStats.GetLevel() >= 5)
        {
            anyQuest = GameMasterScript.tutorialManager.CheckForMonsterLetterTutorial();
        }

        /* if (!anyQuest && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) >= 3 && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) >= 1)
        {
            anyQuest = GameMasterScript.tutorialManager.CheckForMonsterAttractionTutorial();
        } */

        if (MetaProgressScript.CountAllFoodInTrees() > 0)
        {
            if (!importantCutscene && !GameMasterScript.IsAnimationPlayingFromCutscene())
            {
                Conversation harvest = GameMasterScript.FindConversation("grovetreeharvest");
                UIManagerScript.StartConversation(harvest, DialogType.STANDARD, null);
                GameMasterScript.gmsSingleton.SetTempGameData("harvest_waiting", 0);
            }
            else
            {
                GameMasterScript.gmsSingleton.SetTempGameData("harvest_waiting", 1);
            }

        }

        MetaProgressScript.VerifyTamedMonstersHaveActorsOnGroveMapLoad();
    }

    static void VerifyEscapedFrogQuestIsCompleteOrNot()
    {
        if (ProgressTracker.CheckProgress(TDProgress.ESCAPED_FROG, ProgressLocations.META) == 1 || 
            SharedBank.CheckSharedProgressFlag(SharedSlotProgressFlags.ESCAPED_FROG))
        {
            Monster frog = MapMasterScript.activeMap.FindActor("mon_harmlessfungaltoad") as Monster;
            if (frog == null || frog.actorfaction == Faction.PLAYER)
            {
                ProgressTracker.SetProgress(TDProgress.ESCAPED_FROG, ProgressLocations.META, 0);
            }
        }
    }

    public static void DoMemoryTreeCallout(NPC memoryTree)
    {
        GameMasterScript.SetAnimationPlaying(true);
        GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
            memoryTree.GetPos(), 1.0f, 0.5f, false);

        Conversation treeConvo = GameMasterScript.FindConversation("langdon_memorytree");
        UIManagerScript.singletonUIMS.StartCoroutine(
            UIManagerScript.singletonUIMS.WaitThenStartConversation(treeConvo, DialogType.KEYSTORY, memoryTree, 2f));

        MetaProgressScript.SetMetaProgress("tree_callout", 1);
    }

    public static void CheckForPainterQuestCompletion()
    {
        // Must be on the right floor...
        if (MapMasterScript.activeMap.floor != GameMasterScript.heroPCActor.ReadActorData("painterquestfloor"))
        {
            return;
        }

        // Is the quest even active?
        if (GameMasterScript.heroPCActor.ReadActorData("painterquest") != 3)
        {
            return;
        }

        // Are we potentially close to finishing the quest?
        if (MapMasterScript.activeMap.unfriendlyMonsterCount < 4 && MapMasterScript.activeMap.unfriendlyMonsterCount > 0)
        {
            MapMasterScript.activeMap.RecountMonsters(ignoreKnockedOutMonsters:true);
        }

        // 2 or fewer? Good enough, that's a wrap!
        if (MapMasterScript.activeMap.unfriendlyMonsterCount <= 2)              
        {
            Conversation c = GameMasterScript.FindConversation("painter_done");
            UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
        }
    }

    // This will add the Pet Trainer NPC to saves where she has never been encountered
    public static void AddPetTrainerToFrogBog()
    {
        Map frogBog = MapMasterScript.theDungeon.FindFloor(102);
        if (frogBog.FindActor("npc_pettrainer") != null)
        {
            return;
        }
        NPC trainer = NPC.CreateNPC("npc_pettrainer");
        Stairs st = frogBog.mapStairs[UnityEngine.Random.Range(0, frogBog.mapStairs.Count)];
        MapTileData randomEmpty = frogBog.GetRandomEmptyTile(st.GetPos(), 1, true);
        frogBog.PlaceActor(trainer, randomEmpty);
        MapMasterScript.singletonMMS.SpawnNPC(trainer);
    }

    // Check for special NPC arrivals or events in the Grove
    public static void CheckForRiverstoneGroveSpawns()
    {
        if (MetaProgressScript.ReadMetaProgress("pet_trainer_quest") < 1)
        {
            return;
        }
        if (MapMasterScript.singletonMMS.townMap2.FindActor("npc_pettrainer") != null)
        {
            return;
        }

        // Let's spawn the pet trainer!
        Vector2 petTrainerPosition = new Vector2(8f, 4f);

        NPC petTrainer = NPC.CreateNPC("npc_pettrainer");
        MapMasterScript.singletonMMS.townMap2.PlaceActor(petTrainer, MapMasterScript.GetTile(petTrainerPosition));
        MapMasterScript.singletonMMS.SpawnNPC(petTrainer);
    }

    public static void CheckForFloodedTemple2FQuest(Actor act, Vector2 pos)
    {
        if (ProgressTracker.CheckProgress(TDProgress.HERBALIST, ProgressLocations.HERO) != 1)
        {
            return;
        }

        if (MapMasterScript.activeMap.floor != MapMasterScript.FLOODED_TEMPLE_2F)
        {
            return;
        }

        MapTileData mtd = MapMasterScript.GetTile(pos);

        if (mtd.CheckTag(LocationTags.WATER))
        {
            if (act.actorRefName.Contains("vine") || act.actorRefName == "mon_plantturret" || act.actorRefName == "obj_creepingdeath")
            {
                ProgressTracker.SetProgress(TDProgress.HERBALIST, ProgressLocations.HERO, 2);
                CombatManagerScript.GenerateSpecificEffectAnimation(pos, "FervirGrandRecovery", null, true);
                CombatManagerScript.GenerateSpecificEffectAnimation(pos, "LeafPoof", null, true);
                MapTileData nearby = MapMasterScript.GetRandomEmptyTile(pos, 1, true, false);
                Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(nearby, "quest_magical_herb");
                MapMasterScript.singletonMMS.SpawnDestructible(dt);
                CombatManagerScript.FireProjectile(pos, nearby.pos, dt.GetObject(), 0.4f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);
            }
        }

    }

    public static void BeatDimensionalRift()
    {
        GameMasterScript.gmsSingleton.statsAndAchievements.BeatDimRift();
        GameMasterScript.heroPCActor.RemoveActorData("dimrift_found");
        GameMasterScript.heroPCActor.SetActorData("beatdimrift", 1);

        MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");

        MapMasterScript.activeMap.musicCurrentlyPlaying = "postboss";
    }

    public static bool DoLangdonFoodCartCallout()
    {


        NPC langdon = MapMasterScript.activeMap.FindActor("npc_farmer") as NPC;

        Vector2 cartPos = new Vector2(langdon.GetPos().x - 1f, langdon.GetPos().y - 1f);
        MapTileData cartTile = MapMasterScript.GetTile(cartPos);

        if (MapMasterScript.activeMap.FindActor("npc_foodcart") == null)
        {
            NPC foodCart = MapMasterScript.activeMap.CreateNPCInTile(cartTile, "npc_foodcart");
            MapMasterScript.singletonMMS.SpawnNPC(foodCart);

            MapTileData blocker1 = MapMasterScript.GetTile(new Vector2(cartPos.x - 1f, cartPos.y));
            MapTileData blocker2 = MapMasterScript.GetTile(new Vector2(cartPos.x + 1f, cartPos.y));

            Destructible dtBlocker1 = MapMasterScript.activeMap.CreateDestructibleInTile(blocker1, "stb");
            Destructible dtBlocker2 = MapMasterScript.activeMap.CreateDestructibleInTile(blocker2, "stb");

            MapMasterScript.singletonMMS.SpawnDestructible(dtBlocker1);
            MapMasterScript.singletonMMS.SpawnDestructible(dtBlocker2);
        }
        else
        {
            return false;
        }

        if (MetaProgressScript.ReadMetaProgress("foodcart") < 1)
        {
            if (!ShouldCutscenesBeSkipped())
            {
                GameMasterScript.SetAnimationPlaying(true);
                GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(),
                    langdon.GetPos(), 1.0f, 0.5f, false);

                Conversation langdonConvo = GameMasterScript.FindConversation("foodcart_intro");
                UIManagerScript.singletonUIMS.StartCoroutine(
                    UIManagerScript.singletonUIMS.WaitThenStartConversation(langdonConvo, DialogType.KEYSTORY, langdon, 2f));
            }

            MetaProgressScript.SetMetaProgress("foodcart", 1);

            return true;
        }

        return false;
    }

    // "Gauntlet" level, destroy single boss
    public static void ClearFinalSideArea1()
    {
        List<Actor> removeActors = new List<Actor>();

        Map barricadeMap = MapMasterScript.theDungeon.FindFloor(208);

        foreach (Actor act in barricadeMap.actorsInMap)
        {
            if (act.actorRefName == "mon_phasmaturret")
            {
                removeActors.Add(act);
            }
        }

        foreach (Actor act in removeActors)
        {
            barricadeMap.RemoveActorFromMap(act);
            if (MapMasterScript.activeMap.floor == barricadeMap.floor)
            {
                act.myMovable.FadeOutThenDie();
            }            
        }

        GameMasterScript.heroPCActor.AddActorData("finalhubquest", 1);
        GameMasterScript.heroPCActor.SetActorData("final_sidearea1_clear", 1);


        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        removeActors.Clear();
        Map finalBossFight = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR);
        foreach (Actor act in finalBossFight.actorsInMap)
        {
            if (act.actorRefName == "mon_phasmaturret")
            {
                removeActors.Add(act);
            }
        }

        foreach (Actor act in removeActors)
        {
            finalBossFight.RemoveActorFromMap(act);
        }

        UIManagerScript.StartConversationByRef("final_sidearea1_clear", DialogType.STANDARD, null);
    }

    // Machine replicators
    public static void ClearFinalSideArea2()
    {
        List<Actor> removeActors = new List<Actor>();
        Map replicatorMap = MapMasterScript.theDungeon.FindFloor(211);

        foreach (Actor act in replicatorMap.actorsInMap)
        {
            if (act.actorRefName == "mon_fabricatorsentryassembler")
            {
                removeActors.Add(act);
            }
        }

        foreach (Actor act in removeActors)
        {
            replicatorMap.RemoveActorFromLocation(act.GetPos(), act);
            replicatorMap.RemoveActorFromMap(act);
            if (act.objectSet)
            {
                act.myMovable.FadeOutThenDie();
            }
        }

        GameMasterScript.heroPCActor.AddActorData("finalhubquest", 1);
        GameMasterScript.heroPCActor.SetActorData("final_sidearea2_clear", 1);

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        removeActors.Clear();
        Map finalBossFight = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR);
        foreach (Actor act in finalBossFight.actorsInMap)
        {
            if (act.actorRefName == "mon_finalsentryassembler")
            {
                removeActors.Add(act);
            }
        }
        foreach (Actor act in removeActors)
        {
            finalBossFight.RemoveActorFromMap(act);
        }

        UIManagerScript.StartConversationByRef("final_sidearea2_clear", DialogType.STANDARD, null);
    }

    // Phase 2 healers
    public static void ClearFinalSideArea3()
    {

        GameMasterScript.heroPCActor.AddActorData("finalhubquest", 1);
        GameMasterScript.heroPCActor.SetActorData("final_sidearea3_clear", 1);


        List<Actor> removeActors = new List<Actor>();

        Map regenerator = MapMasterScript.theDungeon.FindFloor(209);
        foreach (Actor act in regenerator.actorsInMap)
        {
            if (act.actorRefName == "mon_regeneratormedicspawner")
            {
                removeActors.Add(act);
            }
        }
        foreach (Actor act in removeActors)
        {
            regenerator.RemoveActorFromLocation(act.GetPos(), act);
            regenerator.RemoveActorFromMap(act);
            if (act.objectSet)
            {
                act.myMovable.FadeOutThenDie();
            }
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }

        removeActors.Clear();

        Map finalBossFight2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);
        foreach (Actor act in finalBossFight2.actorsInMap)
        {
            if (act.actorRefName == "mon_medicrobotspawner")
            {
                removeActors.Add(act);
            }
        }
        foreach (Actor act in removeActors)
        {
            finalBossFight2.RemoveActorFromMap(act);
        }



        UIManagerScript.StartConversationByRef("final_sidearea3_clear", DialogType.STANDARD, null);
    }

    // Phase 2 verdigrizzlies
    public static void ClearFinalSideArea4()
    {
        GameMasterScript.heroPCActor.AddActorData("finalhubquest", 1);
        GameMasterScript.heroPCActor.SetActorData("final_sidearea4_clear", 1);

        if (SharaModeStuff.IsSharaModeActive())
        {
            return;
        }
        List<Actor> removeActors = new List<Actor>();
        Map finalBossFight2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);
        foreach (Actor act in finalBossFight2.actorsInMap)
        {
            if (act.actorRefName == "mon_mossbeastboss")
            {
                removeActors.Add(act);
            }
        }
        foreach (Actor act in removeActors)
        {
            finalBossFight2.RemoveActorFromMap(act);
        }

        UIManagerScript.StartConversationByRef("final_sidearea4_clear", DialogType.STANDARD, null);
    }

    public static void CheckForBoss2Clear()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("secondbossdefeated_forsure") == 1)
        {

            foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
            {
                st.EnableActor();
                st.myMovable.SetInSightAndSnapEnable(true);
            }

            if (!MusicManagerScript.IsTrackPlaying("BossVictory") && ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) != 3)
            {
                MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");
            }

            ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 3);
            GameMasterScript.heroPCActor.SetActorData("secondbossdefeated", 1);

            return;
        }

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            // #todo - Check win condition for shara here.
            return;
        }
            
        Map processMap = MapMasterScript.activeMap;
        bool shadowsRemaining = false;
        foreach (Monster mn in processMap.monstersInMap)
        {
            //Debug.Log(mn.actorRefName + " " + mn.surpressTraits);
            if ((mn.actorRefName == "mon_shadowelementalboss" && !mn.surpressTraits && mn.myStats.IsAlive() && mn.actorfaction == Faction.ENEMY &&
                !mn.destroyed && (mn.lastTurnActed == 0 || (GameMasterScript.turnNumber - mn.lastTurnActed < 20)))
                || (mn.actorRefName == "mon_banditwarlord" && mn.actorfaction == Faction.ENEMY && !mn.destroyed))
            {
                shadowsRemaining = true;
                break;
            }

        }

        processMap.RecountMonsters();

        if (!shadowsRemaining || processMap.unfriendlyMonsterCount <= 0)
        {
            foreach (Stairs st in processMap.mapStairs)
            {
                st.EnableActor();
                st.myMovable.SetInSightAndSnapEnable(true);
            }

            if (PlatformVariables.SEND_UNITY_ANALYTICS)
            {
            #if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                            Analytics.CustomEvent("boss_defeated", new Dictionary<string, object>()
                        {
                            { "boss", "twinshadows" },
                            { "plvl", GameMasterScript.heroPCActor.myStats.GetLevel() },
                                { "job", GameMasterScript.heroPCActor.myJob.jobEnum.ToString() }
                        });
            #endif
            }

            if (!ShouldCutscenesBeSkipped())
            {
                Conversation victory = GameMasterScript.FindConversation("beatboss2");
                GameMasterScript.SetAnimationPlaying(true, true);
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(victory, DialogType.KEYSTORY, null, 1.5f));
            }
            
            BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

            MetaProgressScript.SetMetaProgress("secondbossdefeated", 1);
            GameMasterScript.heroPCActor.SetActorData("secondbossdefeated", 1);
            GameMasterScript.heroPCActor.SetActorData("secondbossdefeated_forsure", 1);
            ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 3);

            MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");

            GameMasterScript.heroPCActor.SetActorData("viewfloor" + processMap.floor.ToString(), 999);

            GameMasterScript.gmsSingleton.statsAndAchievements.Boss2Defeated();

            foreach (Actor act in processMap.actorsInMap)
            {
                if (act.actorRefName == "obj_mon_evokeshadow")
                {
                    Destructible dt = act as Destructible;
                    dt.myMovable.SetInSightAndForceFade(false);
                    dt.RemoveImmediately();
                    GameMasterScript.AddToDeadQueue(dt);
                }
            }
        }
    }

    IEnumerator WaitThenReturnSuplex(Actor atk, Actor def, float time)
    {
        yield return new WaitForSeconds(time);
        atk.myMovable.AnimateSetPositionNoChange(atk.GetPos(), 0.13f, false, 0f, 0f, MovementTypes.LERP);
        def.myMovable.AnimateSetPositionNoChange(def.GetPos(), 0.13f, false, 0f, 0f, MovementTypes.LERP);
        StartCoroutine(WaitThenFlipActorY(def, 0.24f));
    }

    IEnumerator WaitThenFlipActorY(Actor def, float time)
    {
        yield return new WaitForSeconds(time);
        def.myAnimatable.SetAnimConditional("TakeDamage");
        def.myAnimatable.FlipSpriteY();
    }

    public static void DoReturnSuplex(Actor atk, Actor def, float time)
    {
        def.myAnimatable.FlipSpriteY();
        singleton.StartCoroutine(singleton.WaitThenReturnSuplex(atk, def, time));
    }

    IEnumerator WaitThenStartSuplex(Actor atk, Actor target, float time)
    {
        yield return new WaitForSeconds(time);
        atk.myMovable.AnimateSetPositionNoChange(new Vector2(atk.GetPos().x, atk.GetPos().y + 7f), 0.18f, false, 0f, 0f, MovementTypes.LERP);
        target.myMovable.AnimateSetPositionNoChange(new Vector2(target.GetPos().x, target.GetPos().y + 7f), 0.18f, false, 0f, 0f, MovementTypes.LERP);

        GameEventsAndTriggers.DoReturnSuplex(atk, target, 0.9f);
    }

    IEnumerator WaitThenLiftSuplexTarget(Actor act, float time)
    {
        yield return new WaitForSeconds(time);
        act.myMovable.AnimateSetPositionNoChange(new Vector2(act.GetPos().x, act.GetPos().y + 0.5f), 0.15f, false, 0f, 0f, MovementTypes.LERP);
    }

    public static void DoStartSuplex(Actor atk, Actor def, float time)
    {
        singleton.StartCoroutine(singleton.WaitThenLiftSuplexTarget(def, 0.35f));
        singleton.StartCoroutine(singleton.WaitThenStartSuplex(atk, def, time));
    }

    public static void ClearFriendshipForest()
    {
        if (!SharedBank.CheckIfJobIsUnlocked(CharacterJobs.WILDCHILD))
        {
            UIManagerScript.StartConversationByRef("unlock_wildchild", DialogType.KEYSTORY, null);
        }
    }

    public static void FrozenAreaFinalUnthaw()
    {
        Map nMap = MapMasterScript.theDungeon.FindFloor(224);
        MapTileData getLocation = MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos());

        int floorOfConnectionToFrozenArea = 0;
        Map connectingMap = null;

        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.ITEM)
            {
                MapTileData newMapTile = nMap.GetTile(act.GetPos());
                nMap.PlaceActor(act, newMapTile);
            }
            else if (act.GetActorType() == ActorTypes.STAIRS)
            {
                Stairs st = act as Stairs;
                floorOfConnectionToFrozenArea = st.NewLocation.floor;
                connectingMap = st.NewLocation;
            }
        }

        foreach (Stairs st in connectingMap.mapStairs)
        {
            if (st.NewLocation.floor == 223) // Remap frozen to unfrozen area.
            {
                st.NewLocation = nMap;
                st.newLocationID = nMap.mapAreaID;
                st.pointsToFloor = 224;
                break;
            }
        }
        foreach (Stairs st in nMap.mapStairs)
        {
            st.pointsToFloor = connectingMap.floor;
            st.NewLocation = connectingMap;
            st.newLocationID = connectingMap.mapAreaID;
        }

        TravelManager.TravelMaps(nMap, null, false, getLocation);



        /*         
        MapMasterScript.singletonMMS.SpawnMapOverlays();
        List<Actor> removeActors = new List<Actor>();
        MapTileData positionOfScientist = null;
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.NPC) continue;
            if (act.actorRefName == "npc_frozenrobot")
            {
                removeActors.Add(act);
            }
            if (act.actorRefName == "npc_frozenscientist_2")
            {
                positionOfScientist = MapMasterScript.GetTile(act.GetPos());
                removeActors.Add(act);
            }
        }


        foreach (Actor act in removeActors)
        {
            MapTileData placement = MapMasterScript.GetTile(act.GetPos());
            GameMasterScript.gmsSingleton.DestroyActor(act);

            if (placement.pos != positionOfScientist.pos)
            {
                Monster friendlyMon = MonsterManagerScript.CreateMonster("mon_heavygolem", false, false, false, 0f, true);
                MapMasterScript.activeMap.PlaceActor(friendlyMon, placement);
                MapMasterScript.singletonMMS.SpawnMonster(friendlyMon);
                friendlyMon.bufferedFaction = Faction.PLAYER;
                friendlyMon.actorfaction = Faction.PLAYER;
            }           
        }

        NPC finalScientist = NPC.CreateNPC("npc_frozenscientist_3");
        MapMasterScript.activeMap.PlaceActor(finalScientist, positionOfScientist);
        MapMasterScript.singletonMMS.SpawnNPC(finalScientist); */
    }

    public static void BeginEscapedFrogQuest(bool doCutscene)
    {
        MetaProgressScript.SetMetaProgress("corralquest", 2);
        Map activeMap = MapMasterScript.activeMap;
        Item mallet = LootGeneratorScript.CreateItemFromTemplateRef("item_monstermallet", 1.0f, 0f, false);
        MapTileData emptyTile = activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true);
        activeMap.PlaceActor(mallet, emptyTile);

        Monster runMon = MonsterManagerScript.CreateMonster("mon_harmlessfungaltoad", false, false, false, 0f, false);

        // Spawn near mir
        Actor mir = MapMasterScript.activeMap.FindActor("npc_tinkerer");

        emptyTile = activeMap.GetRandomEmptyTile(mir.GetPos(), 2, false);
        activeMap.PlaceActor(runMon, emptyTile);

        MapMasterScript.singletonMMS.SpawnItem(mallet);
        MapMasterScript.singletonMMS.SpawnMonster(runMon);

        if (doCutscene)
        {
            GameMasterScript.SetAnimationPlaying(true);
            GameMasterScript.cameraScript.WaitThenSetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), runMon.GetPos(), 1.0f, 0.75f, false);
            NPC jesse = MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC;
            Conversation startQuest = GameMasterScript.FindConversation("monstercorral_startquest");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(startQuest, DialogType.KEYSTORY, jesse, 2.75f));
        }

        //UIManagerScript.StartConversationByRef("monstercorral_startquest", DialogType.STANDARD, null);
    }

    public static void CleanUpSharaPhasmaTiles()
    {
        // Upon re-entering the 2nd boss arena in adventure mode, we need to clean up these tiles since otherwise they don't render correctly.
        List<Actor> removeActors = new List<Actor>();
        Fighter sharaBoss = null;
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE)
            {
                if (act.actorRefName == "mon_finalboss2" && act.actorfaction == Faction.ENEMY)
                {
                    sharaBoss = act as Monster;
                }
                continue;
            }
            Destructible dt = act as Destructible;
            if (dt.mapObjType != SpecialMapObject.LASER) continue;
            removeActors.Add(dt);
        }       

        foreach(Actor act in removeActors)
        {
            act.myMovable.FadeOutThenDie();
            MapMasterScript.activeMap.RemoveActorFromLocation(act.GetPos(), act);
            MapMasterScript.activeMap.RemoveActorFromMap(act);
            sharaBoss.RemoveSummon(act);
        }
    }

    public static void PlayerKnockedOutHarmlessFrog()
    {
        // Switch music back away from the crazy frog theme
        if (MusicManagerScript.singleton.GetCurrentTrackName() != "grovetheme")
        {
            MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("grovetheme", false);
            //MusicManagerScript.singleton.LoadMusicByName_WithIntroloop("grovetheme", true, true);
        }

        GameMasterScript.SetAnimationPlaying(true, true);

        singleton.StartCoroutine(singleton.PlayerKnockOutHarmlessFrogPart2());                
    }

    IEnumerator PlayerKnockOutHarmlessFrogPart2()
    {
        ProgressTracker.SetProgress(TDProgress.ESCAPED_FROG, ProgressLocations.META, 0);

        if (ShouldCutscenesBeSkipped())
        {
            yield return new WaitForSeconds(0.5f);
            GameMasterScript.SetAnimationPlaying(false, true);
            yield break;
        }

        yield return new WaitForSeconds(1.3f); // give the music a second to breathe

        // Find and pan over to the monster rancher

        Actor jesseNPC = MapMasterScript.activeMap.FindActor("npc_monsterguy");

        GameMasterScript.cameraScript.SetCustomCameraAnimation(GameMasterScript.heroPCActor.GetPos(), jesseNPC.GetPos(), 1.25f, false);

        yield return new WaitForSeconds(1.5f);

        UIManagerScript.StartConversationByRef("dialog_quest_frogknockout", DialogType.STANDARD, jesseNPC as NPC);

        SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.ESCAPED_FROG);
    }

    public static void FixFinalBossFloor2Stairs()
    {
        Map finalBoss2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);
        foreach(Stairs st in finalBoss2.mapStairs)
        {
            if (st.isPortal || st.autoMove) continue;

            st.pointsToFloor = 999;
            st.SetActorData("finalstairs", 1);

            if (finalBoss2.FindActor("mon_finalboss2") != null)
            {
                st.DisableActor();
            }
        }
    }

    public static bool ShouldCutscenesBeSkipped()
    {
        if ((GameStartData.challengeType == ChallengeTypes.WEEKLY || GameStartData.challengeType == ChallengeTypes.DAILY || PlayerOptions.speedrunMode) && !SharaModeStuff.IsSharaModeActive())
        {
            return true;
        }

        if (GameStartData.NewGamePlus >= 1) return true;

        if (RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return true;
        }
        return false;
    }

    public static void CheckForDelayedLevelUpAndFlaskInfusionPrompts()
    {
        if (!UIManagerScript.dialogBoxOpen && GameMasterScript.heroPCActor.levelupBoostWaiting > 0 && !GameMasterScript.IsNextTurnPausedByAnimations()) // 12/5 anim playing new conditional
        {
            GameMasterScript.DisplayLevelUpDialog();
        }

        if (!UIManagerScript.dialogBoxOpen &&
            (GameMasterScript.heroPCActor.ReadActorData("infuse1") == 99
            || GameMasterScript.heroPCActor.ReadActorData("infuse2") == 99
            || GameMasterScript.heroPCActor.ReadActorData("infuse3") == 99))
        {
            if (GameMasterScript.heroPCActor.ReadActorData("infuse1") == 99)
            {
                Conversation flask1 = GameMasterScript.FindConversation("flask1_upgrade");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(flask1, DialogType.LEVELUP, null, 0.1f));
            }
            else if (GameMasterScript.heroPCActor.ReadActorData("infuse2") == 99)
            {
                Conversation flask2 = GameMasterScript.FindConversation("flask2_upgrade");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(flask2, DialogType.LEVELUP, null, 0.1f));
            }
            else if (GameMasterScript.heroPCActor.ReadActorData("infuse3") == 99)
            {
                Conversation flask3 = GameMasterScript.FindConversation("flask3_upgrade");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(flask3, DialogType.LEVELUP, null, 0.1f));
            }
        }
    }

    public static void CheckForJobTrialVictory()
    {
        if (GameMasterScript.heroPCActor.jobTrial.trialTierLevel == 0)
        {
            bool anyCrystalLeft = false;
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.actorRefName == "obj_jobtrial_crystal" && !act.destroyed && act.actorfaction != Faction.PLAYER)
                {
                    anyCrystalLeft = true;
                    break;
                }
            }
            if (!anyCrystalLeft)
            {
                JobTrialScript.BeatJobTrial();
            }
        }
    }

    public static void CleanupCampfireOnEntry()
    {
        if (MapMasterScript.activeMap.mapStairs.Count == 0)
        {
            Debug.Log("Uh oh. No stairs in campfire map. Bad.");
            Stairs st = new Stairs();
            st.stairsUp = true;
            st.prefab = "EarthStairsUp";
            st.NewLocation = MapMasterScript.singletonMMS.townMap;
            st.newLocationID = MapMasterScript.singletonMMS.townMap.mapAreaID;
            st.pointsToFloor = MapMasterScript.TOWN_MAP_FLOOR;
            MapTileData tileForStairs = MapMasterScript.activeMap.GetRandomEmptyTileForMapGen();
            MapMasterScript.activeMap.PlaceActor(st, tileForStairs);
            MapMasterScript.singletonMMS.SpawnStairs(st);
        }
    }

    public static void CheckForFinalBossFloorEvents()
    {
        Map activeMap = MapMasterScript.activeMap;

        if (activeMap.floor == MapMasterScript.SHARA_FINALBOSS_FLOOR)
        {
            GameEventsAndTriggers.SharaEntersFinalBossFloor();
        }

        if (activeMap.floor == MapMasterScript.FINAL_BOSS_FLOOR)
        {
            int finalboss1flag = GameMasterScript.heroPCActor.ReadActorData("finalboss1");
            if (finalboss1flag < 0)
            {
                Cutscenes.FinalBossPreBattleCutscene_Part1();
            }
            else if (finalboss1flag == 0 || finalboss1flag == 1)
            {
                Actor findSupervisor = MapMasterScript.activeMap.FindActor("mon_finalbossai");
                if (findSupervisor == null)
                {
                    Monster mSupervisor = MonsterManagerScript.CreateMonster("mon_finalbossai", true, true, false, 0f, false);
                    MapTileData tileToGet = MapMasterScript.GetTile(new Vector2(7f, 6f));
                    activeMap.PlaceActor(mSupervisor, tileToGet);
                    MapMasterScript.singletonMMS.SpawnMonster(mSupervisor);
                }

            }
            else if (finalboss1flag >= 2)
            {
                List<Actor> actorToRemove = new List<Actor>();
                Actor findShara = MapMasterScript.activeMap.FindActor("npc_shara_preboss3");
                if (findShara != null) actorToRemove.Add(findShara);
                Actor findShara2 = MapMasterScript.activeMap.FindActor("npc_shara1");
                if (findShara2 != null) actorToRemove.Add(findShara2);
                Actor findSupervisor = MapMasterScript.activeMap.FindActor("mon_finalbossai");
                if (findSupervisor != null) actorToRemove.Add(findSupervisor);

                foreach (Actor act in actorToRemove)
                {
                    MapMasterScript.activeMap.RemoveActorFromMap(act);
                    GameMasterScript.ReturnActorObjectToStack(act, act.GetObject());
                }

            }
            return;
        }

        if (activeMap.floor == MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("finalboss2") < 0)
            {
                Cutscenes.FinalBossPhase2_Part3();
            }
            else
            {
                GameEventsAndTriggers.CleanUpSharaPhasmaTiles();
            }
            GameEventsAndTriggers.FixFinalBossFloor2Stairs();
            return;
        }
    }

    public static void CheckForItemDreamEventsOnMapChange()
    {
        Map activeMap = MapMasterScript.activeMap;

        if (activeMap.IsItemWorld() && ItemDreamFunctions.IsItemDreamNightmare() && !ItemDreamFunctions.PlayerSawNightmareKingIntro())
        {
            UIManagerScript.StartConversationByRef("nightmare_world_entry", DialogType.STANDARD, null);
            ItemDreamFunctions.SetPlayerSawNightmareKingIntro(true);
        }

        if (activeMap.IsItemWorld() && GameMasterScript.heroPCActor.ReadActorData("iw_np_floor") > 0 && !ItemDreamFunctions.HasPlayerKilledNightmarePrince() && !ItemDreamFunctions.HasPlayerKilledMemoryKing())
        {
            if (MapMasterScript.itemWorldMaps.Length - 2 >= 0)
            {
                Map preMap = MapMasterScript.itemWorldMaps[MapMasterScript.itemWorldMaps.Length - 2];
                if (activeMap == preMap)
                {
                    UIManagerScript.StartConversationByRef("popup_nightmareprince", DialogType.STANDARD, null);
                }
            }
        }

        if (activeMap.IsItemWorld())
        {
            GameMasterScript.heroPCActor.SetActorData("iw_map_" + activeMap.mapAreaID, 1);
            if (activeMap.floor >= 401 && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_dream_portal") && PlayerOptions.tutorialTips)
            {
                StringManager.SetTag(4, StringManager.GetPortalBindingString());
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_dream_portal");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
            }
        }
    }
}


