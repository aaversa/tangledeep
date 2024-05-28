using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ItemDreamFloorValues { NOTCHECKED, FALSE, TRUE, COUNT }

public class ItemDreamFunctions : MonoBehaviour
{
    static List<Map> possibleFloorTemplates;
    static List<DungeonLevel> possibleDungeonLevelTemplates;

    /// <summary>
    /// Used by the Mini Dreamcaster in Mystery Dungeons.
    /// </summary>
    /// <param name="eq"></param>
    /// <returns></returns>
    public static int CalculateMiniDreamcasterCost(Equipment eq)
    {
        if (eq.timesUpgraded >= Equipment.GetMaxUpgrades()) return 0; // already at maxx

        float cost = 200;

        // base cost is from base rank
        int itemRank = BalanceData.ConvertChallengeValueToRank(eq.GetBaseItemRank());
        cost *= itemRank;

        // so far:
        // Rank 3 item = 600 gold
        // Rank 8 item = 1600 gold

        //COMMON, UNCOMMON, MAGICAL, ANCIENT, ARTIFACT, LEGENDARY
        // Increase cost by 25% per rarity level
        cost += ((int)eq.rarity * 0.25f * cost);

        // Rank 3 item with one mod = 750g

        // Increase cost by 20% per existing upgrade
        cost += (eq.timesUpgraded * 0.2f * cost);

        // So a rank 3 item with 1 mod and 1 upgrade is 900g
        // Rank 8 item with 3 mods and 0 upgrades would be 2800g

        return (int)cost;

    }

    public static bool NightmareKingAlive()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("nightmare_world") != 1)
        {
            return false;
        }
        if (GameMasterScript.heroPCActor.ReadActorData("iw_killed_nk") == 1)
        {
            return false;
        }
        return true;
    }

    public static int GetNightmareCrystalsRemaining()
    {
        return GameMasterScript.heroPCActor.ReadActorData("nightmare_crystals_remaining");
    }

    public static void InitializeNightmareWorldHeroData()
    {
        GameMasterScript.heroPCActor.RemoveActorData("killed_memory_king");
        GameMasterScript.heroPCActor.SetActorData("iw_killed_nk", 0);
        GameMasterScript.heroPCActor.SetActorData("nightmare_world", 1);
        GameMasterScript.heroPCActor.SetActorData("nightmare_crystals_remaining", 3); // always start with 3
        GameMasterScript.heroPCActor.RemoveActorData("nightmare_king_id");
        SetPlayerSawNightmareKingIntro(false);
    }

    public static void SetPlayerSawNightmareKingIntro(bool value)
    {
        if (value)
        {
            GameMasterScript.heroPCActor.SetActorData("saw_nk_intro", 1);
        }
        else
        {
            GameMasterScript.heroPCActor.SetActorData("saw_nk_intro", 0);
        }
    }

    public static bool PlayerSawNightmareKingIntro()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("saw_nk_intro") == 1)
        {
            return true;
        }
        return false;
    }

    public static void InitializeItemDreamHeroData()
    {
        GameMasterScript.heroPCActor.RemoveActorData("iw_np_floor");
        GameMasterScript.heroPCActor.RemoveActorData("killed_memory_king");
        GameMasterScript.heroPCActor.RemoveActorData("killed_nightmareprince");
        GameMasterScript.heroPCActor.RemoveActorData("nightmare_world");
        GameMasterScript.heroPCActor.RemoveActorData("nightmare_crystals_remaining");
        GameMasterScript.heroPCActor.RemoveActorData("nightmare_king_id");
    }

    public static bool IsItemDreamNightmare()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("nightmare_world") == 1)
        {
            return true;
        }
        return false;
    }

    public static void RelocateKingToPlayer()
    {
        if (!MapMasterScript.activeMap.IsItemWorld()) return;
        if (!IsItemDreamNightmare()) return;

        if (!NightmareKingAlive())
        {
            return;
        }
        
        Monster king = FindNightmareKing();

        MapMasterScript.RelocateMonsterToPlayerWithinTileRange(king, 3);
    }

    public static int FindNightmareKingID()
    {
        if (!MapMasterScript.itemWorldOpen) return -1;
        if (GameMasterScript.heroPCActor.ReadActorData("nightmare_world") != 1) return -1;
        return GameMasterScript.heroPCActor.ReadActorData("nightmare_king_id");
    }

    public static Monster FindNightmareKing()
    {
        Monster nk = null;

        if (!MapMasterScript.itemWorldOpen) return null;

        Actor act = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.heroPCActor.ReadActorData("nightmare_king_id"));
        if (act == null || act.actorRefName != "mon_nightmareking")
        {
            Debug.Log("Player linked NK ID is invalid.");
            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                Map m = MapMasterScript.itemWorldMaps[i];
                for (int x = 0; x < m.monstersInMap.Count; x++)
                {
                    if (m.monstersInMap[x].actorRefName == "mon_nightmareking")
                    {
                        return m.monstersInMap[x];
                    }
                }
            }
        }
        else
        {
            return act as Monster;
        }



        return nk;
    }

    public static void BuffNightmareKing(float playerHealth)
    {
        Monster nk = FindNightmareKing();
        if ((playerHealth > 0.25f) && (playerHealth <= 0.5f))
        {
            if (nk.myStats.GetCurStat(StatTypes.CHARGETIME) <= 68f)
            {
                nk.myStats.SetStat(StatTypes.CHARGETIME, 75f, StatDataTypes.ALL, true, true);
                GameLogScript.LogWriteStringRef("log_nightmareking_taunt1");
            }
        }
        else if (playerHealth <= 0.25f)
        {
            if (nk.myStats.GetCurStat(StatTypes.CHARGETIME) <= 85.1f)
            {
                nk.myStats.SetStat(StatTypes.CHARGETIME, 85.15f, StatDataTypes.ALL, true, true);
                GameLogScript.LogWriteStringRef("log_nightmareking_taunt2");
            }
        }
    }

    static List<MagicMod> possibleMagicModsForItemDreamItem;

    public static void UpgradeItemDreamItem(bool nightmare)
    {
        bool debug = true;

        Equipment eq = MapMasterScript.itemWorldItem as Equipment;

        if (debug) Debug.Log("Try upgrade the item dream item.");

        if (eq == null)
        {
            if (debug) Debug.Log("There is no item world item???");
            MapMasterScript.itemWorldItem = LootGeneratorScript.GenerateLootFromTable(BalanceData.LEVEL_TO_CV[GameMasterScript.heroPCActor.myStats.GetLevel()], 0f, "legendary");
            if (MapMasterScript.itemWorldItem.itemType == ItemTypes.CONSUMABLE) // must be no legendaries left.
            {
                MapMasterScript.itemWorldItem = LootGeneratorScript.GenerateLootFromTable(BalanceData.LEVEL_TO_CV[GameMasterScript.heroPCActor.myStats.GetLevel()], 0f, "equipment");
            }
            eq = MapMasterScript.itemWorldItem as Equipment;
            if (debug) Debug.Log("Spawned " + MapMasterScript.itemWorldItem.actorRefName);
        }

        bool itemUpgraded = false;
        if (eq.timesUpgraded < Equipment.GetMaxUpgrades())
        {
            if (debug) Debug.Log("Upgrading the item dream item, as current upgrade level is " + eq.timesUpgraded);
            eq.UpgradeItem(debug);
            itemUpgraded = true;
        }

        bool skillOrb = false;

        if (MapMasterScript.orbUsedToOpenItemWorld != null)
        {
            skillOrb = MapMasterScript.orbUsedToOpenItemWorld.IsJobSkillOrb();
        }

        float roll = UnityEngine.Random.Range(0, 1f);

        if (debug) Debug.Log("EQ can handle mods? " + eq.CanHandleMoreMagicMods() + ", chance is " + MapMasterScript.itemWorldMagicChance + ", roll is " + roll);

        if (roll <= MapMasterScript.itemWorldMagicChance
            && (eq.CanHandleMoreMagicMods() || (skillOrb && eq.CanHandleFreeSkillOrb())))
        {
            bool useRandomMod = true;
            if (MapMasterScript.orbUsedToOpenItemWorld != null)
            {
                string checkMod = MapMasterScript.orbUsedToOpenItemWorld.GetOrbMagicModRef();
                if (!string.IsNullOrEmpty(checkMod))
                {
                    MagicMod mmTemplate = MagicMod.FindModFromName(checkMod);
                    EquipmentBlock.MakeMagicalFromMod(eq, mmTemplate, true, true, true);
                    useRandomMod = false;
                    GameMasterScript.heroPCActor.SetActorDataString("dreamitem_modgained", mmTemplate.modName);
                    if (debug) Debug.Log("Adding mod " + checkMod + " to item dream item.");
                }
                else
                {
                    if (debug) Debug.Log("Put in an orb, but it had no mod attached.");
                }
            }
            if (useRandomMod)
            {
                if (!nightmare)
                {
                    MagicMod mm = EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                    GameMasterScript.heroPCActor.SetActorDataString("dreamitem_modgained", mm.modName);
                    if (debug) Debug.Log("Picked mod for item: " + mm.modName);
                }
                else
                {
                    if (possibleMagicModsForItemDreamItem == null) possibleMagicModsForItemDreamItem = new List<MagicMod>();
                    possibleMagicModsForItemDreamItem.Clear();

                    foreach (MagicMod mm in GameMasterScript.dictMagicModsByFlag[MagicModFlags.NIGHTMARE])
                    {
                        if (mm.slot != EquipmentSlots.ANY && mm.slot != eq.slot) continue;
                        if (eq.IsModValidForMe(mm) != MagicModCompatibility.POSSIBLE) continue;
                        possibleMagicModsForItemDreamItem.Add(mm);
                    }
                    if (possibleMagicModsForItemDreamItem.Count > 0)
                    {
                        MagicMod toUse = possibleMagicModsForItemDreamItem[UnityEngine.Random.Range(0, possibleMagicModsForItemDreamItem.Count)];
                        EquipmentBlock.MakeMagicalFromMod(eq, toUse, true, true, true);
                        GameMasterScript.heroPCActor.SetActorDataString("dreamitem_modgained", toUse.modName);
                        if (debug) Debug.Log("Gained nightmare mod " + toUse.modName);
                    }
                    else
                    {                        
                        MagicMod modSelected = EquipmentBlock.MakeMagical(eq, eq.challengeValue, true);
                        GameMasterScript.heroPCActor.SetActorDataString("dreamitem_modgained", modSelected.modName);
                        if (debug) Debug.Log("No possible nightmare mods available, so pick " + modSelected.modName);
                    }

                }
            }

            //Debug.Log(eq.actorRefName + " made magical, is now " + eq.displayName);
        }

        if (itemUpgraded)
        {
            GameMasterScript.heroPCActor.SetActorDataString("dreamitem_itemname", eq.displayName); // Indicates item has been upgraded.
        }
    }

    public static void SetNightmareKingAbilities(Monster nk)
    {
        // #todo - Data drive this
        // Movement possibilities
        List<string> movementPossibilities = new List<string>();
        movementPossibilities.Add("skill_monshadowstep");
        movementPossibilities.Add("skill_monwildhorse");
        movementPossibilities.Add("skill_monhotstreak");

        // Passive possibilities
        List<string> passivePossibilities = new List<string>();
        passivePossibilities.Add("skill_alwaysriposte");
        passivePossibilities.Add("skill_immunology");
        passivePossibilities.Add("skill_arrowcatch");
        passivePossibilities.Add("skill_ppextradamage");
        passivePossibilities.Add("skill_armortraining");


        // Actives
        List<string> activePossibilities = new List<string>();
        activePossibilities.Add("skill_fanofknives");
        activePossibilities.Add("skill_monfireevocation");
        activePossibilities.Add("skill_monhailofarrows");
        activePossibilities.Add("skill_tornadostance");
        activePossibilities.Add("skill_mondivineretribution");
        activePossibilities.Add("skill_flameserpent");
        activePossibilities.Add("skill_summonlivingvine");
        activePossibilities.Add("skill_gravitysurge");
        activePossibilities.Add("skill_hundredfists");

        string movement = movementPossibilities[UnityEngine.Random.Range(0, movementPossibilities.Count)];
        AbilityScript template = GameMasterScript.masterAbilityList[movement];
        nk.LearnNewPower(movement, 1.0f, 1.0f, 2, template.range);

        for (int i = 0; i < 2; i++)
        {
            string passive = passivePossibilities[UnityEngine.Random.Range(0, passivePossibilities.Count)];
            passivePossibilities.Remove(passive);
            nk.LearnNewPower(passive, 1.0f, 1.0f, 1, 99);
        }

        for (int i = 0; i < 3; i++)
        {
            string active = activePossibilities[UnityEngine.Random.Range(0, activePossibilities.Count)];
            template = GameMasterScript.masterAbilityList[active];
            activePossibilities.Remove(active);
            nk.LearnNewPower(active, 1.0f, 1.0f, 1, template.range);
        }

        List<ChampionMod> possibleSingleMods = new List<ChampionMod>();

        possibleSingleMods.Add(Monster.FindMod("monmod_harrier"));
        possibleSingleMods.Add(Monster.FindMod("monmod_frozen"));
        possibleSingleMods.Add(Monster.FindMod("monmod_blazing"));
        possibleSingleMods.Add(Monster.FindMod("monmod_hurricane"));

        nk.AddChampPowers(possibleSingleMods[UnityEngine.Random.Range(0, possibleSingleMods.Count)]);



    }
    public static bool IsNightmareKingInvincible()
    {
        if (!IsItemDreamNightmare()) return false;
        if (GetNightmareCrystalsRemaining() == 0) return false;
        return true;
    }

    public static void PlayerKilledNightmareKing(Monster mon)
    {
        GameMasterScript.heroPCActor.SetActorData("iw_killed_nk", 1);

		MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("BossVictory");


        ItemDreamFunctions.UpgradeItemDreamItem(true);
    }
    public static void PlayerDestroyedNightmareCrystal()
    {
        int crystals = GetNightmareCrystalsRemaining();
        crystals--;
        GameMasterScript.heroPCActor.SetActorData("nightmare_crystals_remaining", crystals);

        // Do popups etc here

        Monster queen = FindNightmareKing();
        queen.myStats.ChangeStat(StatTypes.CHARGETIME, 10f, StatDataTypes.ALL, true);

        if (crystals <= 0)
        {
            UIManagerScript.FlashWhite(0.8f);
            // Unlock the Nightmare King! She's killable now.
            // Also we should warp the player.
            UIManagerScript.StartConversationByRef("nightmare_king_vulnerable", DialogType.STANDARD, null);
            Monster king = FindNightmareKing();
            king.myStats.RemoveStatusByRef("status_invincible_heal");
            king.myStats.RemoveStatusByRef("status_invincible_def");
            king.myStats.RemoveStatusByRef("status_invincible_dmg");
            king.myStats.RemoveAllStatusByRef("status_silentsealed");
            king.myStats.SetStat(StatTypes.CHARGETIME, 99f, StatDataTypes.ALL, true, true);

            king.myStats.AddStatusByRef("monster_resource_heal", king, 99);
            king.myStats.AddStatusByRef("status_parry20", king, 99);
        }
    }

    public static bool HasPlayerKilledNightmarePrince()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("killed_nightmareprince") == 1)
        {
            return true;
        }
        return false;
    }

    public static bool HasPlayerKilledNightmareKing()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("killed_nightmareking") == 1)
        {
            return true;
        }
        return false;
    }

    public static bool HasPlayerKilledMemoryKing()
    {
        if (GameMasterScript.heroPCActor.ReadActorData("killed_memory_king") == 1)
        {
            return true;
        }
        return false;
    }

    public static RoomAndMapIDDatapack GetNightmareKingArena()
    {
        RoomAndMapIDDatapack returnData = new RoomAndMapIDDatapack();
        for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
        {
            foreach (Room testRoom in MapMasterScript.itemWorldMaps[i].mapRooms)
            {
                if ((testRoom.internalTiles.Count < 200) || (returnData.room == null))
                {
                    returnData.room = testRoom;
                    returnData.map = MapMasterScript.itemWorldMaps[i];
                    returnData.mapID = returnData.map.mapAreaID;
                }
            }
        }

        return returnData;
    }

    public static void MovePlayerAndNKToArena(MapTileData playerSquare, MapTileData bossSquare)
    {
        Monster nk = FindNightmareKing();
        MapMasterScript.singletonMMS.MoveAndProcessActor(GameMasterScript.heroPCActor.GetPos(), playerSquare.pos, GameMasterScript.heroPCActor);
        MapMasterScript.singletonMMS.MoveAndProcessActor(nk.GetPos(), bossSquare.pos, nk);
        GameMasterScript.heroPCActor.myMovable.SetPosition(playerSquare.pos);

        Monster nightmarePrince1 = MonsterManagerScript.CreateMonster(MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
        nightmarePrince1.MakeNightmareBoss(MapMasterScript.activeMap.dungeonLevelData.maxChampionMods, false, true);

        MapTileData tileForPrince = MapMasterScript.activeMap.GetRandomEmptyTile(bossSquare.pos, 4, false);
        MapMasterScript.activeMap.PlaceActor(nightmarePrince1, tileForPrince);

        Monster nightmarePrince2 = MonsterManagerScript.CreateMonster(MapMasterScript.activeMap.dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
        nightmarePrince2.MakeNightmareBoss(MapMasterScript.activeMap.dungeonLevelData.maxChampionMods, false, true);

        tileForPrince = MapMasterScript.activeMap.GetRandomEmptyTile(bossSquare.pos, 4, false);
        MapMasterScript.activeMap.PlaceActor(nightmarePrince2, tileForPrince);
        MapMasterScript.singletonMMS.SpawnMonster(nightmarePrince1);
        MapMasterScript.singletonMMS.SpawnMonster(nightmarePrince2);

        nk.myMovable.SetPosition(bossSquare.pos);
        CameraController.UpdateCameraPosition(playerSquare.pos, true);
        MapMasterScript.singletonMMS.UpdateMapObjectData();

        // Hopefully this works below?
		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("bosstheme2");


        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme2";

    }

    public static void EndItemWorld()
    {
        if (MapMasterScript.itemWorldMaps == null)
        {
            Debug.Log("WARNING: Item world end is attempted, but there are no item world maps?");
            return;
        }

        if (!MapMasterScript.itemWorldOpen)
        {
            return;
        }

        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 0);

        //Debug.Log("Ending item world in turn cleanup.");
        GameMasterScript.heroPCActor.AddActorData("dreams_defeated", 1);

        GameMasterScript.gmsSingleton.statsAndAchievements.IncrementItemWorldsCleared(1);

        float chanceOfTickTime = 0.5f;
        float otherPossibleTime = 0.5f;

        if (MapMasterScript.itemWorldMaps == null)
        {
            if (Debug.isDebugBuild) Debug.Log("No item world maps?");
        }
        else
        {
            chanceOfTickTime = GameMasterScript.GetChallengeModToPlayer(
                MapMasterScript.itemWorldMaps[MapMasterScript.itemWorldMaps.Length - 1].GetChallengeRating());

            otherPossibleTime = GameMasterScript.GetChallengeModToPlayer(MapMasterScript.itemWorldMaps[MapMasterScript.itemWorldMaps.Length - 2].GetChallengeRating());
        }

        if (otherPossibleTime > chanceOfTickTime)
        {
            chanceOfTickTime = otherPossibleTime;
        }

        GameMasterScript.heroPCActor.dictDreamFloorData.Clear();

        int iwBonus = GameMasterScript.heroPCActor.ReadActorData("iwbonus");
        if (iwBonus == -1)
        {
            iwBonus = 0;
        }

        float calculateRewardBonusForTimePass = (iwBonus / 100f) + 1f;
        GameMasterScript.heroPCActor.RemoveActorData("iwbonus");
        chanceOfTickTime *= calculateRewardBonusForTimePass;

        if (UnityEngine.Random.Range(0, 1) < chanceOfTickTime)
        {
            GameMasterScript.gmsSingleton.TickGameTime(1, trySpawnWanderingMerchant: true, showWanderingMessage: false);
        }

        if (MapMasterScript.itemWorldMaps != null)
        {
            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                MapMasterScript.itemWorldMaps[i].clearedMap = true;
                MapMasterScript.theDungeon.maps.Remove(MapMasterScript.itemWorldMaps[i]);
                MapMasterScript.dictAllMaps.Remove(MapMasterScript.itemWorldMaps[i].mapAreaID);
                MapMasterScript.OnMapRemoved(MapMasterScript.itemWorldMaps[i]);
            }
        }

        MapMasterScript.itemWorldMaps = null; // No item world maps.

        Actor remover = null;
        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.STAIRS)
            {
                Stairs st = act as Stairs;
                //Debug.Log(st.actorRefName + " " + st.actorUniqueID + " " + st.pointsToFloor + " " + st.newLocation.floor);
                if (st.NewLocation == null)
                {
                    remover = st;
                }
                else if (st.NewLocation.IsItemWorld())
                {
                    remover = st;
                    break;
                }
            }
        }

        if (remover != null)
        {
            MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(remover);
            if (remover.GetObject() != null && remover.GetObject().activeSelf)
            {
                MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(remover.GetObject());
                GameMasterScript.ReturnActorObjectToStack(remover, remover.GetObject(), remover.prefab);
            }
        }

        if (MapMasterScript.itemWorldItem != null)
        {
            Equipment eq = MapMasterScript.itemWorldItem as Equipment;
            GameMasterScript.gmsSingleton.statsAndAchievements.SetMaxItemModsFound(eq.GetNonAutomodCount());
        }

        CloseCurrentItemDreamAndRemoveItems();

        ValidateDreamResults();

        UIManagerScript.StartConversationByRef("dream_results", DialogType.STANDARD, null);
        if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_beatitemworld") && PlayerOptions.tutorialTips)
        {
            Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_beatitemworld");
            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
        }

        SharedBank.UnlockFeat("skill_luciddreamer");

        GameMasterScript.heroPCActor.AddActorData("dreamsdefeated", 1);
        TutorialManagerScript.turnDreamDefeated = GameMasterScript.turnNumber;

        GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
    }

    public static void EndItemWorldNoRewards(bool retrieveItem = true)
    {
        MapMasterScript.itemWorldOpen = false;
        GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
        if (MapMasterScript.itemWorldItem == null)
        {
            Debug.Log("No player item in item world..?");
            return;
        }

        ProgressTracker.SetProgress(TDProgress.DRAGON_SPIRIT_DUNGEON_ACCESSIBLE, ProgressLocations.HERO, 0);

        GameMasterScript.heroPCActor.dictDreamFloorData.Clear();

        StringManager.SetTag(0, MapMasterScript.itemWorldItem.displayName);

        if (retrieveItem)
        {
            GameLogScript.GameLogWrite(StringManager.GetString("item_world_closed"), GameMasterScript.heroPCActor);
        }
        else
        {
            GameLogScript.GameLogWrite(StringManager.GetString("item_world_closed_noitem"), GameMasterScript.heroPCActor);
        }


        Item itemWorldItem = MapMasterScript.itemWorldItem;

        if (retrieveItem)
        {
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itemWorldItem, false);
        }


        GameMasterScript.heroPCActor.myInventory.RemoveAllDreamItems();

        if (MapMasterScript.itemWorldMaps != null)
        {
            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                if (MapMasterScript.itemWorldMaps[i] == null) continue;
                MapMasterScript.itemWorldMaps[i].clearedMap = true;
                MapMasterScript.theDungeon.maps.Remove(MapMasterScript.itemWorldMaps[i]);
                MapMasterScript.dictAllMaps.Remove(MapMasterScript.itemWorldMaps[i].mapAreaID);
                MapMasterScript.OnMapRemoved(MapMasterScript.itemWorldMaps[i]);
            }
        }

        MapMasterScript.itemWorldMaps = null; // No item world maps.
        Actor remover = null;

        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.STAIRS)
            {
                Stairs st = act as Stairs;
                //Debug.Log(st.actorRefName + " " + st.actorUniqueID + " " + st.pointsToFloor + " " + st.newLocation.floor);
                if (st.NewLocation == null) continue;
                if (st.NewLocation.IsItemWorld())
                {
                    remover = st;
                    break;
                }
            }
        }
        if (remover != null)
        {
            MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(remover);
            if (remover.objectSet)
            {
                if (remover.GetObject().activeSelf)
                {
                    MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(remover.GetObject());
                    GameMasterScript.ReturnActorObjectToStack(remover, remover.GetObject(), remover.prefab);
                }
            }
        }

        MapMasterScript.itemWorldOpen = false;
        GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
        GameMasterScript.heroPCActor.myInventory.RemoveAllDreamItems();
        GameMasterScript.endingItemWorld = false;
        MapMasterScript.itemWorldItem = null;
        RemoveItemWorldMapsFromDictionary();
    }
    /// <summary>
    /// Purge all itemworld maps from the dictionary. Caution: if any actors still have itemworld IDs cached on them,
    /// they will point to null maps 💔
    /// </summary>
    public static void RemoveItemWorldMapsFromDictionary()
    {
        //remove all itemworld maps from dictionary
        if (MapMasterScript.itemWorldMaps != null)
        {
            for (int i = 0; i < MapMasterScript.itemWorldMaps.Length; i++)
            {
                if (MapMasterScript.itemWorldMaps[i] == null) continue;
                MapMasterScript.itemWorldMaps[i].clearedMap = true;
                MapMasterScript.theDungeon.maps.Remove(MapMasterScript.itemWorldMaps[i]);
                MapMasterScript.dictAllMaps.Remove(MapMasterScript.itemWorldMaps[i].mapAreaID);
                MapMasterScript.OnMapRemoved(MapMasterScript.itemWorldMaps[i]);
            }
        }

        MapMasterScript.itemWorldMaps = null; // No item world maps.
    }

    public static void TryCreateDreamWeaponRumorBoss()
    {
        foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
        {
            if (qs.qType != QuestType.DREAMWEAPON_BOSS) continue;
            if (qs.targetMonster != null) continue; // already have a target for this quest
            if (qs.targetItem.actorRefName == MapMasterScript.itemWorldItem.actorRefName)
            {
                // Matched the item! Let's create a monster.
                List<Map> possibleMapsForElemBoss = new List<Map>();
                for (int i = 0; i < MapMasterScript.itemWorldMaps.Length - 1; i++)
                {
                    bool validMap = true;
                    foreach (Monster mn in MapMasterScript.itemWorldMaps[i].monstersInMap)
                    {
                        if (mn.ReadActorData("nightmareprince") == 1)
                        {
                            validMap = false;
                            break;
                        }
                    }
                    if (!validMap) continue;
                    possibleMapsForElemBoss.Add(MapMasterScript.itemWorldMaps[i]);
                }
                Map mapToSpawnBoss = possibleMapsForElemBoss[UnityEngine.Random.Range(0, possibleMapsForElemBoss.Count)];
                foreach (Monster mon in mapToSpawnBoss.monstersInMap)
                {
                    if (!mon.isChampion && !mon.myTemplate.cannotBeChampion)
                    {
                        mon.MakeElementalBoss(qs.damType);
                        Debug.Log("Made elemental boss " + mon.displayName + " on floor " + mapToSpawnBoss.floor);
                        qs.targetActor = mon;
                        qs.targetActorID = mon.actorUniqueID;
                        qs.targetMonster = mon;
                        qs.targetMonsterID = mon.actorUniqueID;
                        qs.targetMap = mapToSpawnBoss;
                        qs.targetMapID = mapToSpawnBoss.mapAreaID;
                        break;
                    }
                }
            }
        }
    }

    public static void ResetDreamData()
    {
        GameMasterScript.heroPCActor.SetActorData("dream_gold", 0);
        GameMasterScript.heroPCActor.RemoveActorData("dream_numitems");
        GameMasterScript.heroPCActor.SetActorData("dream_numfountains", 0);
        GameMasterScript.heroPCActor.SetActorData("dream_xp", 0);
        GameMasterScript.heroPCActor.SetActorData("dream_jp", 0);
        GameMasterScript.heroPCActor.SetActorData("dream_gilfrogs", 0);
        GameMasterScript.heroPCActor.RemoveActorDataString("dreamitem_modgained");
        GameMasterScript.heroPCActor.RemoveActorDataString("dreamitem_itemname");
    }

    public static void CheckForAndWriteInfoAboutDreamFloor()
    {
        OverlayTextData levelOTD = new OverlayTextData();
        levelOTD.refName = "dreamfloor_info";
        levelOTD.showOnlyOnce = false; // We can re-use this same refname over and over for new dreams/floors
        //levelOTD.headerText = StringManager.GetString("misc_dream_specialfloor");
        levelOTD.headerText = "";
        levelOTD.descText = "";
        ItemDreamFloorData floorData = GameMasterScript.heroPCActor.GetItemDreamFloorDataPack(MapMasterScript.activeMap);
        if (floorData.viewed) return;
        bool first = true;
        for (int i = 0; i < floorData.dreamEvents.Length; i++)
        {
            if (floorData.dreamEvents[i] == ItemDreamFloorValues.TRUE)
            {
                // GOLDFROG_FLOOR, BIGMODE, COSTUMEPARTY, SPINMODE, FOUNTAINS, EXTRAFOOD, BERSERK, BRAWL
                // format: dreamtext_goldfrog_floor
                string descText = StringManager.GetString("dreamtext_" + ((ItemDreamFloorTypes)i).ToString().ToLowerInvariant());
                if (!first)
                {
                    descText = "\n" + descText;
                }
                else
                {
                    first = false;
                }
                levelOTD.descText += descText; // Todo: Replace with actual string refs
            }
            floorData.viewed = true;
        }
        if (!string.IsNullOrEmpty(levelOTD.descText))
        {
            UIManagerScript.WriteOverlayText(levelOTD);
        }        
    }

    public static void CheckForCostumeParty(float localCostumePartyChance)
    {
        if (!MapMasterScript.activeMap.IsItemWorld() || ItemDreamFunctions.IsItemDreamNightmare() || MapMasterScript.activeMap.mapAreaID != GameMasterScript.heroPCActor.ReadActorData("iw_np_floor")) return;

        // Have we checked this map for parties already?

        if (GetItemDreamFloorValue(MapMasterScript.activeMap, ItemDreamFloorTypes.COSTUMEPARTY) != ItemDreamFloorValues.NOTCHECKED)
        {
            return;
        }

        // Our first time exploring, is it possibly a party?
        if (UnityEngine.Random.Range(0, 1f) <= localCostumePartyChance)
        {
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.COSTUMEPARTY, ItemDreamFloorValues.TRUE);
            MapMasterScript.activeMap.costumeParty = true;
            GameLogScript.LogWriteStringRef("log_costumeparty");
        }
        else
        {
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.COSTUMEPARTY, ItemDreamFloorValues.FALSE);
        }
    }

    public static void CheckForSpinMode(float spinChance)
    {
        if (!MapMasterScript.activeMap.IsItemWorld()) return;

        // Already checked this floor? Don't check again
        if (GetItemDreamFloorValue(MapMasterScript.activeMap, ItemDreamFloorTypes.SPINMODE) != ItemDreamFloorValues.NOTCHECKED)
        {
            return;
        }

        if (UnityEngine.Random.Range(0, 1f) <= spinChance)
        {
            MapMasterScript.activeMap.alwaysSpin = true;
            GameLogScript.LogWriteStringRef("log_spinmode");
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.SPINMODE, ItemDreamFloorValues.TRUE);
            //UIManagerScript.StartConversationByRef("spinmode", DialogType.TUTORIAL, null);
        }
        else
        {
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.SPINMODE, ItemDreamFloorValues.FALSE);
        }
    }

    public static void CheckForBigMode(float localBigModeChance)
    {
        if (!MapMasterScript.activeMap.IsItemWorld() || ItemDreamFunctions.IsItemDreamNightmare() || MapMasterScript.activeMap.mapAreaID != GameMasterScript.heroPCActor.ReadActorData("iw_np_floor")) return;

        // Have we stored this map as Big Mode already?

        if (GetItemDreamFloorValue(MapMasterScript.activeMap, ItemDreamFloorTypes.BIGMODE) != ItemDreamFloorValues.NOTCHECKED)
        {
            return;
        }

        // Our first time exploring, is it possibly Big Mode?
        if (UnityEngine.Random.Range(0, 1f) <= localBigModeChance)
        {
            // Rolled the dice, this floor is - in fact - big mode.
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.BIGMODE, ItemDreamFloorValues.TRUE);
            MapMasterScript.activeMap.bigMode = true;
            GameLogScript.LogWriteStringRef("log_bigmode");
            foreach (Monster m in MapMasterScript.activeMap.monstersInMap)
            {
                if (m.actorRefName == "mon_itemworldcrystal" || m.actorRefName == "mon_nightmarecrystal") continue;
                try { m.myAnimatable.SetAllSpriteScale(1.45f); }
                catch  //#questionable_try_block
                {
                    Debug.Log("Failed to set scale for " + m.actorRefName);
                }
            }
        }
        else
        {
            SetItemDreamFloorType(MapMasterScript.activeMap, ItemDreamFloorTypes.BIGMODE, ItemDreamFloorValues.FALSE);
        }
    }
    
    // Marks a floor type (Berserker, Goldfrog, Bigmode, Costumeparty, etc) as the given value: Notchecked, true, false   
    public static void SetItemDreamFloorType(Map dreamMap, ItemDreamFloorTypes floorType, ItemDreamFloorValues value)
    {
        ItemDreamFloorData floorData = GameMasterScript.heroPCActor.GetItemDreamFloorDataPack(dreamMap);
        floorData.dreamEvents[(int)floorType] = value;
    }

    // Returns info on the desired dream map floor event type, possible values are NOTCHECKED, TRUE, FALSE
    public static ItemDreamFloorValues GetItemDreamFloorValue(Map dreamMap, ItemDreamFloorTypes floorType)
    {
        ItemDreamFloorData floorData = GameMasterScript.heroPCActor.GetItemDreamFloorDataPack(dreamMap);
        return floorData.dreamEvents[(int)floorType];
    }

    public static DungeonLevel FindLevelDataFromDragonFloor(DungeonLevel fallbackLevel, List<int> floorsAlreadyUsed)
    {
        if (possibleFloorTemplates == null)
        {
            possibleFloorTemplates = new List<Map>();            
        }
        if (possibleDungeonLevelTemplates == null)
        {
            possibleDungeonLevelTemplates = new List<DungeonLevel>(); 
        }

        possibleFloorTemplates.Clear();
        possibleDungeonLevelTemplates.Clear();

        int floorIndex = UnityEngine.Random.Range(MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_START, MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_END + 1);

        while (floorsAlreadyUsed.Contains(floorIndex))
        {
            floorIndex = UnityEngine.Random.Range(MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_START, MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_END + 1);
        }


        for (int i = MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_START; i <= MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_END; i++)
        {
            if (floorsAlreadyUsed.Contains(i)) continue;

            DungeonLevel dl = GameMasterScript.masterDungeonLevelList[i];

            if (dl.GetMetaData("spiritdungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_SPIRIT_DUNGEON, ProgressLocations.META) < 1)
            {
                continue;
            }
            if (dl.GetMetaData("robotdungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_ROBOT_DUNGEON, ProgressLocations.META) < 1)
            {
                continue;
            }
            if (dl.GetMetaData("banditdungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_BANDIT_DUNGEON, ProgressLocations.META) < 1)
            {
                continue;
            }
            if (dl.GetMetaData("jellydungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_JELLY_DUNGEON, ProgressLocations.META) < 1)
            {
                continue;
            }
            if (dl.GetMetaData("frogdungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META) < 1)
            {
                continue;
            }
            if (dl.GetMetaData("beastdungeon") == 1 && ProgressTracker.CheckProgress(TDProgress.DRAGON_BEAST_DUNGEON, ProgressLocations.META) < 1)
            {
                continue;
            }

            possibleDungeonLevelTemplates.Add(dl);
        }

        if (possibleDungeonLevelTemplates.Count == 0)
        {
            return fallbackLevel;
        }

        DungeonLevel dlTemplate = possibleDungeonLevelTemplates.GetRandomElement();

        //if (Debug.isDebugBuild) Debug.Log("Use " + dlTemplate.floor);
        
        DungeonLevel newLevelData = new DungeonLevel();
        newLevelData = MapMasterScript.theDungeon.CopyDungeonLevelFromTemplate(dlTemplate);

        CleanUpCopiedLevelTemplate(newLevelData, dlTemplate, fallbackLevel);

        return newLevelData;
    }

    // Take ANY non-dream map the player has visited, even boss areas and side areas
    // And use this as a template for an Item Dream. ~ Wacky ~
    public static DungeonLevel FindLevelDataFromVisitedFloor(DungeonLevel fallbackLevel, List<int> floorsAlreadyUsed)
    {
        if (possibleFloorTemplates == null)
        {
            possibleFloorTemplates = new List<Map>();
        }

        possibleFloorTemplates.Clear();

        GameMasterScript.heroPCActor.PruneMapsExploredThatDontExist();

        foreach(int floorExplored in GameMasterScript.heroPCActor.mapsExploredByMapID)
        {
            Map gMap = MapMasterScript.dictAllMaps[floorExplored];
            if (gMap.IsItemWorld()) continue; // We already have item dreams as possibilities
            if (gMap.floor == MapMasterScript.CAMPFIRE_FLOOR) continue;
            if (gMap.floor == MapMasterScript.TOWN2_MAP_FLOOR) continue; // too many problems possible here
            if (gMap.floor >= MapMasterScript.ROBOT_DRAGON_DUNGEONEND_FLOOR && gMap.floor <= MapMasterScript.JELLY_DRAGON_DUNGEONEND_FLOOR) continue;
            if (gMap.IsMainPath() && UnityEngine.Random.Range(0, 1f) <= 0.5f) continue; // main path maps aren't as interesting
            if (floorsAlreadyUsed.Contains(gMap.floor)) continue;
            possibleFloorTemplates.Add(gMap);            
        }

        if (possibleFloorTemplates.Count == 0)
        {            
            return fallbackLevel;
        }

        Map template = possibleFloorTemplates[UnityEngine.Random.Range(0, possibleFloorTemplates.Count)];

        DungeonLevel dlTemplate = template.dungeonLevelData;

        DungeonLevel newLevelData = new DungeonLevel();
        newLevelData = MapMasterScript.theDungeon.CopyDungeonLevelFromTemplate(dlTemplate);

        CleanUpCopiedLevelTemplate(newLevelData, dlTemplate, fallbackLevel);

        return newLevelData;
    }
    
    static void CleanUpCopiedLevelTemplate(DungeonLevel newLevelData, DungeonLevel dlTemplate, DungeonLevel fallbackLevel)
    {
        int totalSizeOfFallback = fallbackLevel.size * fallbackLevel.size;
        int totalSizeOfNewLevel = newLevelData.size * newLevelData.size;

        float monsterDensityOfFallback = (float)(fallbackLevel.maxMonsters) / (float)(totalSizeOfFallback);
        int targetMonstersInNewLevel = (int)(totalSizeOfNewLevel * monsterDensityOfFallback);
        if (targetMonstersInNewLevel < 4)
        {
            targetMonstersInNewLevel = 4; // Minimum possible
        }

#if UNITY_EDITOR
        Debug.Log("Fallback had " + fallbackLevel.maxMonsters + " with total size " + totalSizeOfNewLevel + " given size " + fallbackLevel.size + " so total density is " + monsterDensityOfFallback + " with target " + targetMonstersInNewLevel);
#endif

        // But we want spawn, combat data to follow our original Item Dream-friendly settings.
        newLevelData.noSpawner = fallbackLevel.noSpawner;
        newLevelData.evenMonsterDistribution = true;
        newLevelData.spawnTable = fallbackLevel.spawnTable;

        if (newLevelData.spawnTable == null)
        {
            newLevelData.spawnTable = BalanceData.GetSpawnTableByCV(fallbackLevel.challengeValue);
            Debug.Log(fallbackLevel.challengeValue + " " + newLevelData.spawnTable.refName);
        }

        newLevelData.minMonsters = (targetMonstersInNewLevel - 1);
        newLevelData.maxMonsters = (targetMonstersInNewLevel + 1);
        newLevelData.maxChampionMods = fallbackLevel.maxChampionMods;
        newLevelData.maxChampions = fallbackLevel.maxChampions;
        
        //newLevelData.sideArea = fallbackLevel.sideArea;
        newLevelData.sideArea = false;

        //newLevelData.itemWorld = fallbackLevel.itemWorld;
        newLevelData.itemWorld = true;

        //newLevelData.altPath = fallbackLevel.altPath;
        newLevelData.altPath = 0;

        //newLevelData.safeArea = fallbackLevel.safeArea;
        newLevelData.safeArea = false;

        newLevelData.musicCue = fallbackLevel.musicCue;
        newLevelData.showRewardSymbol = fallbackLevel.showRewardSymbol;
        newLevelData.noRewardPopup = fallbackLevel.noRewardPopup;
        newLevelData.customName = fallbackLevel.customName;
        //Debug.Log("Name: " + newLevelData.customName);
        newLevelData.fastTravelPossible = fallbackLevel.fastTravelPossible;
        newLevelData.challengeValue = fallbackLevel.challengeValue;

        //Debug.Log(newLevelData.floor + " should have " + newLevelData.minMonsters + " to " + newLevelData.maxMonsters + " from table " + newLevelData.spawnTable + " from floor " + fallbackLevel.floor);
    }

    public static void CleanMapOfNPCsBossesAndTriggers(Map mapToClean)
    {
        foreach(Actor act in mapToClean.actorsInMap)
        {

        }
    }
    
    public static DungeonLevel FindTemplateMap(ItemDreamCreationData dreamData, out bool templateFromDungeonMap)
    {        
        templateFromDungeonMap = false;
        List<DungeonLevel> possibleLevels = new List<DungeonLevel>();
        if (dreamData.localMap == null)
        {
            if (dreamData.localCV > 1.9f)
            {
                // For super high level content, first pick ANY existing item dream map.
                dreamData.localMap = GameMasterScript.itemWorldMapList.GetRandomElement();

                // This is above the previous level cap, so scale things up.
                dreamData.localMap.maxChampions = BalanceData.GetMaxChampionsByCV(dreamData.localCV);
                dreamData.localMap.maxChampionMods = 4;
                dreamData.localMap.challengeValue = dreamData.localCV;
                dreamData.localMap.expectedPlayerLevel = BalanceData.GetExpectedPlayerLevelByCV(dreamData.localCV);
                dreamData.localMap.spawnTable = BalanceData.GetSpawnTableByCV(dreamData.localCV);
            }
            else // we're less than 1.9f, so we have a defined set of maps to pick from
            {
                foreach (float key in GameMasterScript.itemWorldMapDict.Keys)
                {
                    if (Mathf.Abs(dreamData.localCV - key) < 0.01f)
                    {
                        possibleLevels = GameMasterScript.itemWorldMapDict[key];
                        dreamData.localMap = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];

                        break;
                    }
                }
            }

            bool debugForceMemoryLevel = false;
            bool debugForceDragonLevel = false;
#if UNITY_EDITOR
            //debugForceMemoryLevel = true;
            //debugForceDragonLevel = true;
#endif

            // If we have encountered Dragons, we can use dragon maps!
            if ((DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2) && UnityEngine.Random.Range(0,1f) < dreamData.chanceOfDragonFloor) || debugForceDragonLevel)
            {
                dreamData.fallbackLevelData = dreamData.localMap;
                int floorTemplateID = dreamData.localMap.floor;
                dreamData.localMap = FindLevelDataFromDragonFloor(dreamData.localMap, dreamData.specialFloorIDsUsed);

                if (dreamData.localMap.floor != floorTemplateID)
                {
                    templateFromDungeonMap = true;
                    dreamData.specialFloorIDsUsed.Add(dreamData.localMap.floor);
                }

                dreamData.localMap.spawnTable = dreamData.fallbackLevelData.spawnTable;
                dreamData.localMap.safeArea = false;
                dreamData.localMap.sideArea = false;
                dreamData.localMap.itemWorld = true;
            }

            // We can potentially use a template from ANY area we've visited, even side areas, in item dreams!
            // Store the generic info we might want about # of spawns in fallbackLevelData
            else if ((DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_MEMORY_FLOOR) || debugForceMemoryLevel)
            {
                dreamData.fallbackLevelData = dreamData.localMap;
                int floorTemplateID = dreamData.localMap.floor;
                dreamData.localMap = FindLevelDataFromVisitedFloor(dreamData.localMap, dreamData.specialFloorIDsUsed);
                int attempts = 0;
                while (dreamData.localMap.GetMetaData("noitemdreams") == 1)
                {
                    attempts++;
                    dreamData.localMap = FindLevelDataFromVisitedFloor(dreamData.localMap, dreamData.specialFloorIDsUsed);
                    if (attempts > 100)
                    {
                        if (Debug.isDebugBuild) Debug.Log("Probably no valid floor here.");
                        dreamData.localMap = dreamData.fallbackLevelData;
                        return dreamData.localMap;
                    }
                }

#if UNITY_EDITOR
                if (debugForceMemoryLevel)
                {
                    dreamData.localMap = GameMasterScript.masterDungeonLevelList[100];
                }
#endif

                if (dreamData.localMap.floor != floorTemplateID)
                {
                    templateFromDungeonMap = true;
                    dreamData.specialFloorIDsUsed.Add(dreamData.localMap.floor);
                }

                dreamData.localMap.spawnTable = dreamData.fallbackLevelData.spawnTable;
                dreamData.localMap.safeArea = false;
                dreamData.localMap.sideArea = false;
                dreamData.localMap.itemWorld = true;
            }

        }

        return dreamData.localMap;
    }

    public static void SetTileVisualSet(ItemDreamCreationData dreamData)
    {
        // Tile set mod
        // But if we've forced a type, don't f with it
        if (dreamData.forcedLevelType == null && dreamData.localMap.tileVisualSet != TileSet.SPECIAL)
        {
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_FIRE])
            {
                dreamData.localMap.tileVisualSet = TileSet.VOLCANO;
            }
            else if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_WATER])
            {
                dreamData.localMap.tileVisualSet = TileSet.VOID;
            }
            else
            {
                dreamData.localMap.tileVisualSet = (TileSet)UnityEngine.Random.Range(0, (int)TileSet.COUNT);
                bool tileSetvalid = false;
                while (!tileSetvalid)
                {
                    dreamData.localMap.tileVisualSet = (TileSet)UnityEngine.Random.Range(0, (int)TileSet.COUNT);
                    if (dreamData.localMap.tileVisualSet == TileSet.SPECIAL || dreamData.localMap.tileVisualSet == TileSet.NIGHTMARISH)
                    {
                        continue;
                    }
                    if (GameMasterScript.heroPCActor.ReadActorData("ruby_witch_quest") < 1 && dreamData.localMap.tileVisualSet == TileSet.SAND)
                    {
                        continue;
                    }
                    if (GameMasterScript.heroPCActor.ReadActorData("frozen_quest") < 5 && dreamData.localMap.tileVisualSet == TileSet.SNOW)
                    {
                        continue;
                    }
                    if (GameMasterScript.heroPCActor.lowestFloorExplored <= 12 && dreamData.localMap.tileVisualSet == TileSet.RUINED)
                    {
                        continue;
                    }
                    if (GameMasterScript.heroPCActor.lowestFloorExplored <= 16 && dreamData.localMap.tileVisualSet == TileSet.FUTURE)
                    {
                        continue;
                    }
                    if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && dreamData.localMap.tileVisualSet == TileSet.TREETOPS)
                    {
                        continue;
                    }
                    if (dreamData.localMap.tileVisualSet == TileSet.MOUNTAINGRASS) // super special! don't ever use this in dreams.
                    {
                        continue;
                    }
                    tileSetvalid = true;
                }
            }
        }

        if (dreamData.workFloorIsNightmarePrinceFloor)
        {
            dreamData.localMap.tileVisualSet = TileSet.NIGHTMARISH;
        }
        if (dreamData.creatingNightmareWorld)
        {
            dreamData.localMap.tileVisualSet = TileSet.NIGHTMARISH;
        }
    }

    public static void PopulateNightmareFloor(ItemDreamCreationData dreamData, int floorIndex)
    {        
        dreamData.meta.properties[(int)ItemWorldProperties.NIGHTMARE] = true;
        dreamData.itemWorld[floorIndex].challengeRating = dreamData.itemWorld[floorIndex].dungeonLevelData.challengeValue;
        if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_NIGHTMAREWORLD_DARKFROG_SPAWN)
        {
            Monster mn = MonsterManagerScript.CreateMonster("mon_darkfrog", true, true, false, dreamData.localCV, dreamData.meta.rewards, false);

            mn.ScaleWithDifficulty(dreamData.localCV);
            mn.MakeChampion();
            mn.MakeChampion();

            Item lucidOrb = ItemWorldUIScript.CreateItemWorldOrb(dreamData.localCV, true, false);
            mn.myInventory.AddItemRemoveFromPrevCollection(lucidOrb, true);

            if (UnityEngine.Random.Range(0, 1f) <= MapMasterScript.CHANCE_DARKFROG_LEGENDARY)
            {
                Item randomLeg = LootGeneratorScript.GenerateLootFromTable(dreamData.localCV, 0f, "legendary");
                mn.myInventory.AddItemRemoveFromPrevCollection(randomLeg, true);
            }

            dreamData.itemWorld[floorIndex].PlaceActorAtRandom(mn);
        }
        if (floorIndex == 0) // Spawn NK near player
        {
            Monster nk = MonsterManagerScript.CreateMonster("mon_nightmareking", true, true, false, 0f, false);
            nk.CopyStatsAndWeaponFromTemplate(dreamData.itemWorld[floorIndex].dungeonLevelData.spawnTable.GetRandomActorRef());
            dreamData.itemWorld[floorIndex].PlaceActorAtRandom(nk);
            nk.myStats.AddStatusByRef("status_invincible_def", nk, 99);
            nk.myStats.AddStatusByRef("status_invincible_dmg", nk, 99);
            nk.myStats.AddStatusByRef("status_invincible_heal", nk, 99);
            nk.myStats.AddStatusByRef("status_silentsealed", nk, 99);
            GameMasterScript.heroPCActor.SetActorData("nightmare_king_id", nk.actorUniqueID);
            nk.MakeNightmareBoss(dreamData.itemWorld[floorIndex].dungeonLevelData.maxChampionMods + 1, true, false);

            Item upgrade = MapMasterScript.itemWorldItem;
            nk.myInventory.AddItemRemoveFromPrevCollection(upgrade, false);
            Debug.Log("Gave " + upgrade.actorRefName + " " + upgrade.actorUniqueID + " to " + nk.actorRefName);
        }
    }

    public static void CheckForAndPopulateSpecialFloors(ItemDreamCreationData dreamData, int floorIndex)
    {
        int i = floorIndex;

        if (!dreamData.workFloorIsNightmarePrinceFloor && !dreamData.creatingNightmareWorld)
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.25f)
            {
                Monster mn = MonsterManagerScript.CreateMonster("mon_goldfrog", true, true, false, dreamData.localCV, dreamData.meta.rewards, false);

                if (UnityEngine.Random.Range(0, 1f) <= dreamData.localCoolFrogChance)
                {
                    mn.TurnIntoCoolfrog(dreamData.itemWorld[i]);
                }

                dreamData.itemWorld[i].PlaceActorAtRandom(mn);
            }


            if (UnityEngine.Random.Range(0, 1f) <= dreamData.chanceFountainFloor)
            {
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.FOUNTAINS, ItemDreamFloorValues.TRUE);
                int numFountains = UnityEngine.Random.Range(7, 11);
                for (int f = 0; f < numFountains; f++)
                {
                    dreamData.itemWorld[i].SpawnFountainInMap();
                }
            }
            else
            {
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.FOUNTAINS, ItemDreamFloorValues.FALSE);
            }
            if (UnityEngine.Random.Range(0, 1f) <= dreamData.chanceFoodFloor)
            {
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.EXTRAFOOD, ItemDreamFloorValues.TRUE);
                int numFood = UnityEngine.Random.Range(26, 34);
                for (int f = 0; f < numFood; f++)
                {
                    dreamData.itemWorld[i].SpawnFoodInMap();
                }
            }
            else
            {
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.EXTRAFOOD, ItemDreamFloorValues.FALSE);
            }
            int numDreamPotions = UnityEngine.Random.Range(0, 5);
            numDreamPotions += UnityEngine.Random.Range(0, dreamData.itemWorld[i].columns / 6);
            for (int x = 0; x < numDreamPotions; x++)
            {
                Item nPotion = LootGeneratorScript.GenerateLootFromTable(dreamData.itemWorld[i].challengeRating, 0f, "potions");
                if (nPotion.itemType != ItemTypes.CONSUMABLE)
                {
                    Debug.Log("WARNING: Why was non-consumable generated in Item Dream from POTIONS table?");
                }
                nPotion.dreamItem = true;
                nPotion.RebuildDisplayName();
                dreamData.itemWorld[i].PlaceItemAtRandom(nPotion);
            }

            float localGoldfloorChance = dreamData.localGoldfrogFloorChance;
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("luciddreamer"))
                localGoldfloorChance *= 2f;

            if (floorIndex < dreamData.numFloors - 1 && UnityEngine.Random.Range(0, 1f) <= localGoldfloorChance)
            {
                int count = dreamData.itemWorld[i].unfriendlyMonsterCount;
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.GOLDFROG_FLOOR, ItemDreamFloorValues.TRUE);
                List<Actor> rem = new List<Actor>();
                foreach (Actor act in dreamData.itemWorld[i].actorsInMap)
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        if (act.actorRefName != "mon_goldfrog")
                        {
                            rem.Add(act);
                        }
                    }
                }
                foreach (Actor act in rem)
                {
                    dreamData.itemWorld[i].RemoveActorFromMap(act);
                }
                for (int x = 0; x < rem.Count; x++)
                {
                    Monster mn = MonsterManagerScript.CreateMonster("mon_goldfrog", true, true, false, dreamData.localCV, 0f, false);

                    if (UnityEngine.Random.Range(0, 1f) <= dreamData.localCoolFrogChance)
                    {
                        mn.TurnIntoCoolfrog(dreamData.itemWorld[i]);
                    }

                    dreamData.itemWorld[i].PlaceActorAtRandom(mn);
                }
            }
            else
            {
                ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.GOLDFROG_FLOOR, ItemDreamFloorValues.FALSE);
                bool berserkFloor = false;
                bool brawlFloor = false;
                if (UnityEngine.Random.Range(0, 1f) <= dreamData.localBerserkFloorChance)
                {
                    berserkFloor = true;
                    ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.BERSERK, ItemDreamFloorValues.TRUE);
                }
                else if (UnityEngine.Random.Range(0, 1f) <= dreamData.localBrawlFloorChance)
                {
                    ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.BERSERK, ItemDreamFloorValues.FALSE);
                    ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.BRAWL, ItemDreamFloorValues.TRUE);
                    brawlFloor = true;
                }
                else
                {
                    ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.BERSERK, ItemDreamFloorValues.FALSE);
                    ItemDreamFunctions.SetItemDreamFloorType(dreamData.itemWorld[i], ItemDreamFloorTypes.BRAWL, ItemDreamFloorValues.FALSE);
                }

                bool championsPossible = !dreamData.itemWorldProperties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS];

                if (brawlFloor)
                {
                    int extraMonsters = UnityEngine.Random.Range(6, 9);
                    for (int x = 0; x < extraMonsters; x++)
                    {
                        dreamData.itemWorld[floorIndex].SpawnRandomMonster(false, false, "", championsPossible);
                    }

                    int extraRandomItems = UnityEngine.Random.Range(14, 18);
                    for (int x = 0; x < extraRandomItems; x++)
                    {
                        float itemCV = dreamData.itemWorld[floorIndex].challengeRating - 0.3f;
                        if (itemCV <= 1.0f)
                        {
                            itemCV = 1.0f;
                        }
                        Item newItem = LootGeneratorScript.GenerateLoot(itemCV, 0.05f);
                        MapTileData mtdToPlaceItem = null;
                        if (newItem != null)
                        {
                            mtdToPlaceItem = dreamData.itemWorld[floorIndex].GetRandomEmptyTileForMapGen();
                            if (mtdToPlaceItem != null)
                            {
                                dreamData.itemWorld[floorIndex].PlaceActor(newItem, mtdToPlaceItem);
                            }                            
                        }

                        if (UnityEngine.Random.Range(0, 1f) <= 0.5f)
                        {
                            Item fooder = LootGeneratorScript.GenerateLootFromTable(1.0f, 0f, "consumables");
                            if (fooder != null)
                            {
                                mtdToPlaceItem = dreamData.itemWorld[floorIndex].GetRandomEmptyTileForMapGen();
                                if (mtdToPlaceItem != null)
                                {
                                    dreamData.itemWorld[floorIndex].PlaceActor(fooder, mtdToPlaceItem);
                                }                                
                            }
                        }

                    }
                }



                foreach (Actor act in dreamData.itemWorld[floorIndex].actorsInMap)
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mn = act as Monster;
                        if (!mn.isChampion && mn.actorRefName != "mon_goldfrog" && !mn.isBoss && mn.actorRefName != "mon_itemworldcrystal")
                        {
                            if (UnityEngine.Random.Range(0, 1f) <= dreamData.localCharmChance)
                            {
                                mn.actorfaction = Faction.PLAYER;
                                mn.myStats.AddStatusByRef("status_permacharmed", mn, 0);
                            }
                            else if (berserkFloor)
                            {
                                mn.AddAttribute(MonsterAttributes.BERSERKER, 90);
                            }
                            else if (brawlFloor)
                            {
                                mn.AddAttribute(MonsterAttributes.GREEDY, 100);
                                mn.AddAttribute(MonsterAttributes.BERSERKER, 25);
                                mn.aggroRange--;
                                mn.aggroRange--;
                                if (mn.aggroRange == 0)
                                {
                                    mn.aggroRange = 1;
                                }

                            }
                        }
                    }
                }

                // Brawl floor - spawn lots of items

            }
        }
        else // Nightmare King floor
        {
            if (!dreamData.creatingNightmareWorld)
            {
                GameMasterScript.heroPCActor.SetActorData("iw_np_floor", dreamData.itemWorld[floorIndex].mapAreaID);
            }
        }
    }

    public static void AddElementalMonsterStatusesFromDreamProperties(ItemDreamCreationData dreamData, int floorIndex)
    {
        foreach (Monster mn in dreamData.itemWorld[floorIndex].monstersInMap)
        {
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_FIRE])
            {
                mn.myStats.AddStatusByRef("dream_mon_fire", mn, 99);
            }
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_WATER])
            {
                mn.myStats.AddStatusByRef("dream_mon_water", mn, 99);
            }
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_LIGHTNING])
            {
                mn.myStats.AddStatusByRef("dream_mon_lightning", mn, 99);
            }
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_POISON])
            {
                mn.myStats.AddStatusByRef("dream_mon_poison", mn, 99);
            }
            if (dreamData.itemWorldProperties[(int)ItemWorldProperties.ELEM_SHADOW])
            {
                mn.myStats.AddStatusByRef("dream_mon_shadow", mn, 99);
            }
        }
    }

    public static void VerifyAllStairsAreAccessible(Map[] itemWorld, bool creatingNightmareWorld)
    {
        // Make sure all stairs are accessible.
        for (int i = 0; i < itemWorld.Length; i++)
        {
            if (creatingNightmareWorld)
            {
                itemWorld[i].ClearAllCrystals();
                itemWorld[i].SpawnRandomMonster(false, true, "mon_nightmarecrystal");
                if (i == itemWorld.Length - 1)
                {
                    // place the final crystal in the boss room? maybe?
                }
            }

            Map m = itemWorld[i];
            Stairs stDown = m.GetStairsDown();
            if (stDown == null) continue;
            Stairs stUp = m.GetStairsUp();
            if (stUp == null) continue;
        }
    }

    public static void CreateNightmarePrince(Map[] itemWorld)
    {
        Map nightmarePrinceMap = itemWorld[itemWorld.Length - 1];
        Room bossRoom = null;
        foreach (Room rm in nightmarePrinceMap.mapRooms)
        {
            if (rm.GetTemplateName().Contains("shadowking")) // The boss room.
            {
                float shortest = 999f;
                Vector2 roomCenter = rm.center;
                MapTileData best = null;

                foreach (MapTileData mtd in rm.internalTiles)
                {
                    if (!mtd.IsEmpty()) continue;
                    if (mtd.tileType != TileTypes.GROUND) continue;
                    float dist = MapMasterScript.GetGridDistance(mtd.pos, roomCenter);
                    if (dist < shortest)
                    {
                        shortest = dist;
                        best = mtd;
                    }
                }

                Monster nightmarePrince = MonsterManagerScript.CreateMonster(nightmarePrinceMap.dungeonLevelData.spawnTable.GetRandomActorRef(), true, true, false, 0f, false);
                nightmarePrince.MakeNightmareBoss(nightmarePrinceMap.dungeonLevelData.maxChampionMods, false, false);
                nightmarePrinceMap.PlaceActor(nightmarePrince, best);
                nightmarePrince.sleepUntilSeehero = true;
                Debug.Log("Created the nightmare prince " + nightmarePrince.displayName + " at " + best.pos);
                break;
            }
        }
    }

    public static void CreateItemBossIfNecessary(Map finalMap, bool creatingNightmareWorld, Equipment eq)
    {
        bool madeItemBoss = false;
        if (!creatingNightmareWorld)
        {
            foreach (Actor act in finalMap.actorsInMap)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (!mn.isChampion && mn.actorfaction != Faction.PLAYER && mn.actorRefName != "mon_itemworldcrystal")
                    {

                        if (!mn.MakeMemoryKing(finalMap.dungeonLevelData.maxChampionMods, eq))
                        {
                            continue;
                        }
                        madeItemBoss = true;
                        break;
                    }
                }
            }
            if (!madeItemBoss)
            {
                Monster boss = finalMap.SpawnRandomMonster(true, true);
                boss.MakeMemoryKing(finalMap.dungeonLevelData.maxChampionMods, eq);
                madeItemBoss = true;
                Debug.Log("Making " + boss.actorRefName + " " + boss.actorUniqueID + " the item boss, position " + boss.GetPos());

            }
        }
    }

    public static MiniDreamcasterDataPack GetItemForMiniDreamcasterFromDialogString(string value)
    {
        MiniDreamcasterDataPack mddp = new MiniDreamcasterDataPack();

        EquipmentSlots slot = EquipmentSlots.COUNT;
        Equipment checkItem = null;
        int hbSlot = -1;

#if UNITY_EDITOR
        //Debug.Log("Request upgrade value of " + value);
#endif

        if (value.Contains("HOTBAR"))
        {
            hbSlot = Int32.Parse(value.Substring(6));
            checkItem = UIManagerScript.hotbarWeapons[hbSlot];
        }
        else
        {
            slot = (EquipmentSlots)Enum.Parse(typeof(EquipmentSlots), value);
            checkItem = GameMasterScript.heroPCActor.myEquipment.GetEquipmentInSlot(slot);
        }

        mddp.slot = slot;
        mddp.checkItem = checkItem;
        mddp.hbSlot = hbSlot;

        return mddp;
    }

    // Ensures we don't display dream results with values less than 0
    public static void ValidateDreamResults()
    {
        int dream_numfountains = GameMasterScript.heroPCActor.ReadActorData("dream_numfountains");
        if (dream_numfountains < 0) GameMasterScript.heroPCActor.SetActorData("dream_numfountains", 0);
        int dream_numitems = GameMasterScript.heroPCActor.ReadActorData("dream_numitems");
        if (dream_numitems < 0) GameMasterScript.heroPCActor.SetActorData("dream_numitems", 0);
        int dream_gilfrogs = GameMasterScript.heroPCActor.ReadActorData("dream_gilfrogs");
        if (dream_gilfrogs < 0) GameMasterScript.heroPCActor.SetActorData("dream_gilfrogs", 0);
    }

    static void CloseCurrentItemDreamAndRemoveItems()
    {
        MapMasterScript.itemWorldOpen = false;
        GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
        GameMasterScript.heroPCActor.myInventory.RemoveAllDreamItems();
        GameMasterScript.endingItemWorld = false;
    }

    public static bool SanityCheckThatItemDreamShouldBeOpen()
    {
        Item idItem = MapMasterScript.itemWorldItem;
        if (idItem != null)
        {
            if (idItem.collection == GameMasterScript.heroPCActor.myInventory)
            {
                GameMasterScript.endingItemWorld = true;
                if (Debug.isDebugBuild) Debug.Log("IW item is in player's inventory.");
                return true;
            }
            else
            {
                if (GameMasterScript.heroPCActor.myEquipment.IsEquipped(idItem))
                {
                    GameMasterScript.endingItemWorld = true;
                    if (Debug.isDebugBuild) Debug.Log("IW item is equipped.");
                    return true;
                }
            }
        }

        return false;
    }
}

public class MiniDreamcasterDataPack
{
    public EquipmentSlots slot = EquipmentSlots.COUNT;
    public int hbSlot = -1;
    public Equipment checkItem;
}

public class ItemDreamCreationData
{
    public List<int> specialFloorIDsUsed;
    public float localCV;
    public DungeonLevel fallbackLevelData;
    public DungeonLevel localMap;
    public DungeonLevel forcedLevelType;
    public bool creatingNightmareWorld;
    public bool workFloorIsNightmarePrinceFloor;
    public bool[] itemWorldProperties;
    public ItemWorldMetaData meta;
    public Map[] itemWorld;
    public float chanceFountainFloor;
    public float localCoolFrogChance;
    public float chanceFoodFloor;
    public float localGoldfrogFloorChance;
    public int numFloors;
    public float localBerserkFloorChance;
    public float localBrawlFloorChance;
    public float localCharmChance;
    public float chanceOfDragonFloor;
}

// This class stores information per map on what attributes that dream floor has
// Such as costume party, big mode, fountain floor, berserk floor, etc.
public class ItemDreamFloorData
{
    public Map dreamMap;
    public int iDreamMapID;
    public ItemDreamFloorValues[] dreamEvents;
    public bool viewed;

    public ItemDreamFloorData()
    {
        dreamEvents = new ItemDreamFloorValues[(int)ItemDreamFloorTypes.COUNT];
    }
}
