using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;
using System.Linq;

public class MysteryDungeonManager
{
    static MysteryDungeon activeDungeon;
    const int ID_NO_WEAPON = -1;
    const int MYSTERY_DUNGEON_STRING_TIPS = 4;

    public const float EXTRA_MAGIC_ITEM_CHANCE = 0.3f; // applies to any items in any mystery dungeon
    public const float EXTRA_LOOT_DROP_CHANCE = 0.2f;
    public const float EXTRA_BREAKABLE_LOOT_VALUE = 0.15f;
    public const float JP_FROM_CAMPFIRES = 400f;
    public const float CHANCE_RUNE_INSTEAD_OF_LEGENDARY = 0.25f;
    public const float HIGHER_MERCHANT_VALUE_CV = 0.25f;
    public const float BONUS_MD_XP_MULTIPLIER = 1.12f;
    public const float ELEMENTAL_DAMAGE_REDUCTION_MULT = 0.85f;
    public const float LEGENDARY_PRICE_MULTIPLIER = 0.35f;

    public static bool exitingMysteryDungeon = false;

    static HashSet<string> dictActorDataToBackup = new HashSet<string>()
    {
        "infuse1", "infuse2", "infuse3", "flask_apple_infuse", "schematist_infuse"
    };

    public static MysteryDungeon GetActiveDungeon()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return null;

        return activeDungeon;
    }
    public static void SetActiveDungeon(MysteryDungeon md)
    {
        activeDungeon = md;
    }

    public static bool CheckMysteryDungeonPlayerResources(EMysteryDungeonPlayerResources res)
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        if (activeDungeon == null) return false;
        return activeDungeon.resourcesAtStart[(int)res];
    }

    public static void RelocateChaserToPlayerIfNeeded()
    {
        if (activeDungeon == null) return;
        if (!activeDungeon.HasGimmick(MysteryGimmicks.MONSTER_CHASER)) return;

        Actor chaser;
        if (GameMasterScript.dictAllActors.TryGetValue(GameMasterScript.heroPCActor.ReadActorData("mystery_king_id"), out chaser))
        {
            Fighter ft = chaser as Fighter;
            if (!ft.myStats.IsAlive())
            {
                return;
            }
            MapMasterScript.RelocateMonsterToPlayerWithinTileRange(ft, 4, minimumRange: 3);
        }
    }

    public static bool InOrCreatingMysteryDungeon()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        if (GameMasterScript.gmsSingleton.ReadTempGameData("creatingmysterydungeon") == 1)
        {
            return true;
        }
        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsMysteryDungeonMap()) return true;
        return false;
    }

    public static bool CheckIfNGPlusMonstersShouldBeScaled()
    {        
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return true;

        if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.heroPCActor != null)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("dont_scale_md_monsters_ngplus") == 1) return false;
        }

        if (GameMasterScript.gmsSingleton.ReadTempGameData("creatingmysterydungeon") != 1) return true;

        if (activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            return true; // Allow NG+ monsters to be scaled only if we keep our previous level/stats.
        }
        // We're in a mystery dungeon, but didn't start with our normal stats. So don't scale them.        
        return false;
    }

    /// <summary>
    /// IF we are in a campfire part of a Mystery Dungeon, and that dungeon grants JP on rest/cook, then grant JP here.
    /// </summary>
    public static void PlayerRestedAtFire()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;
        if (activeDungeon == null) return;
        if (activeDungeon.HasGimmick(MysteryGimmicks.JP_CAMPFIRES))
        {
            float jpAmount = MysteryDungeonManager.JP_FROM_CAMPFIRES;
            GameMasterScript.heroPCActor.AddJP(jpAmount);
            StringManager.SetTag(0, ((int)jpAmount).ToString());
            GameLogScript.LogWriteStringRef("log_gain_jp");
        }
    }

    public static bool InMysteryDungeonWithExtraConsumables()
    {
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && MapMasterScript.activeMap.IsMysteryDungeonMap() && MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.EXTRA_CONSUMABLES))
        {
            return true;
        }
        return false;
    }

    public static Item TryCreatingRandomExtraConsumable(float cv)
    {
        Item nItem = null;
        if (UnityEngine.Random.Range(0, 1f) <= MysteryDungeon.CHANCE_OFFENSE_ITEM)
        {
            nItem = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "offense_summon_consumables");
        }
        else if (UnityEngine.Random.Range(0, 1f) <= MysteryDungeon.CHANCE_EXTRA_ANY_CONSUMABLE)
        {
            nItem = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "consumables_plus_eggs");
        }        
        else if (UnityEngine.Random.Range(0, 1f) <= MysteryDungeon.CHANCE_EXTRA_FOOD)
        {
            nItem = LootGeneratorScript.GenerateLootFromTable(cv, 0f, "food_and_meals");
        }
        return nItem;
    }

    public static void SpawnStartingItemsNearPlayer()
    {
        if (activeDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            return; // Don't spawn starting items if we keep ours
        }
        
        List<Item> startItems = new List<Item>();
        if (!string.IsNullOrEmpty(GameMasterScript.heroPCActor.myJob.startingWeapon))
        {
            startItems.Add(LootGeneratorScript.CreateItemFromTemplateRef(GameMasterScript.heroPCActor.myJob.startingWeapon, 1.0f, 0f, false));
        }        
        foreach (string startItemRef in GameMasterScript.heroPCActor.myJob.startingItems)
        {
            startItems.Add(LootGeneratorScript.CreateItemFromTemplateRef(startItemRef, 1.0f, 0f, false));
        }
        foreach(Item created in startItems)
        {
            MapTileData createTile = MapMasterScript.activeMap.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, anyNonCollidable: true, preferLOS: true, avoidTilesWithPowerups: false, excludeCenterTile: true);
            MapMasterScript.activeMap.PlaceActor(created, createTile);
            MapMasterScript.singletonMMS.SpawnItem(created);
            //Debug.Log("Spawned starting item " + created.actorRefName + " at " + createTile.pos);

        }
    }

    public static bool CanMonstersDropItems()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return true;
        if (!MapMasterScript.activeMap.IsMysteryDungeonMap()) return true;
        MysteryDungeon md = MysteryDungeonManager.GetActiveDungeon();
        if (md.HasGimmick(MysteryGimmicks.NO_ITEM_DROPS))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Returns TRUE if we are in a Mystery Dungeon where we DON'T keep non-relic gear at the end.
    /// </summary>
    /// <returns></returns>
    public static bool AllowDuplicateLegendaries()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return false;
        MysteryDungeon md = MysteryDungeonManager.GetActiveDungeon();
        if (md == null) return false;
        if (md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            return false;
        }
        return true;        
    }

    public static bool CanGetSeedsOrOrbs()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return true;
        if (!MapMasterScript.activeMap.IsMysteryDungeonMap()) return true;
        if (!GetActiveDungeon().resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            return true;
        }
        return false; // If we don't keep items at the end, no point in getting orbs or seeds.
    }

    public static void RestoreHeroDictActorData(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        if (!md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.STATS])
        {
            GameMasterScript.heroPCActor.RemoveActorData("flask_apple_infuse");
        }

        foreach(var kvp in mdd.dictSavedActorData)
        {
            GameMasterScript.heroPCActor.SetActorData(kvp.Key, kvp.Value);
        }
    }

    public static void RestoreHeroFlaskAndPandoraBoxes(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        // If we lost our initial flask, restore it - always
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.FLASK])
        {
            GameMasterScript.heroPCActor.SetRegenFlaskUses(mdd.flaskUsesPriorToEntry);
        }

        GameMasterScript.heroPCActor.numPandoraBoxesOpened = mdd.pandoraBoxesPriorToEntry;
    }

    public static int RestoreHeroMoney(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        int moneyInDungeon = GameMasterScript.heroPCActor.GetMoney();
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.MONEY])
        {
            GameMasterScript.heroPCActor.SetMoneyUnsafe(mdd.moneyPriorToEntry);
        }
        return moneyInDungeon;
    }

    public static List<Item> RestoreHeroInventory(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.ITEMS])
        {
            List<Item> inventoryInDungeon = GameMasterScript.heroPCActor.myInventory.GetInventory();
            GameMasterScript.heroPCActor.myInventory = mdd.inventoryPriorToEntry;
            GameMasterScript.heroPCActor.myInventory.Owner = GameMasterScript.heroPCActor;

            List<Item> itemsToTransfer = new List<Item>();

            // Restore hotbar weapons, paired offhands.
            foreach(Item itm in inventoryInDungeon)
            {
                if (itm.ReadActorData("md_entry_gear") != 1) continue;
                itemsToTransfer.Add(itm);
            }
            foreach(Item itm in itemsToTransfer)
            {
                itm.RemoveActorData("md_entry_gear");
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itm, false);
            }

            return inventoryInDungeon;
        }
        return null;
    }

    public static EquipmentBlock RestoreHeroGear(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            EquipmentBlock gearInDungeon = GameMasterScript.heroPCActor.myEquipment;
            GameMasterScript.heroPCActor.myEquipment = mdd.eqPriorToEntry;
            GameMasterScript.heroPCActor.myEquipment.owner = GameMasterScript.heroPCActor;
            if (GameMasterScript.heroPCActor.myEquipment.defaultWeapon == null)
            {
                GameMasterScript.heroPCActor.myEquipment.defaultWeapon = LootGeneratorScript.CreateItemFromTemplateRef("weapon_fists", 1.0f, 0f, false) as Weapon;
            }
            for (int i = 0; i < mdd.hotbarWeaponsPriorToEntry.Length; i++)
            {
                UIManagerScript.hotbarWeapons[i] = null;
                if (mdd.hotbarWeaponsPriorToEntry[i] >= 0)
                {
                    Actor weapon;
                    if (GameMasterScript.dictAllActors.TryGetValue(mdd.hotbarWeaponsPriorToEntry[i], out weapon))
                    {
                        UIManagerScript.hotbarWeapons[i] = weapon as Weapon;
                    }
                }
            }

            for (int i = 0; i < GameMasterScript.heroPCActor.advStats.Length; i++)
            {
                GameMasterScript.heroPCActor.advStats[i] = mdd.advStatsPriorToEntry[i];
            }

            return gearInDungeon;
        }
        else
        {
            // Don't keep NEW gear we found.
            if (!md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR] || !mdd.dungeonVictory)
            {
                for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
                {
                    Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[i];
                    if (eq == null) continue;
                    if (eq.ReadActorData("mdgear") != 1) continue;
                    if (eq.ReadActorData("md_entry_gear") == 1) continue;
                    // ok, this must be a temporary item, so get rid of it

                    GameMasterScript.heroPCActor.myEquipment.UnequipByReference(eq);

                    for (int x = 0; x < UIManagerScript.hotbarWeapons.Length; x++)
                    {
                        if (UIManagerScript.hotbarWeapons[x] == eq)
                        {
                            UIManagerScript.hotbarWeapons[x] = GameMasterScript.heroPCActor.myEquipment.defaultWeapon;
                        }
                    }

                    if (!md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS] || !mdd.dungeonVictory)
                    {
                        GameMasterScript.heroPCActor.myInventory.RemoveItem(eq);
                    }
                }
            }
        }
        return null;
    }

    public static void RestoreHeroStats(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            GameMasterScript.heroPCActor.myStats = mdd.statsPriorToEntry;
            GameMasterScript.heroPCActor.myStats.SetOwner(GameMasterScript.heroPCActor);
            GameMasterScript.heroPCActor.allMitigationAddPercent = mdd.allMitigationPriorToEntry;
            GameMasterScript.heroPCActor.allDamageMultiplier = mdd.allDamageMultiplierPriorToEntry;
            GameMasterScript.heroPCActor.SetCachedBattleData(mdd.fighterDataPriorToEntry);
            GameMasterScript.heroPCActor.cachedBattleData.SetDirty();

            for (int i = 0; i < (int)ActorFlags.COUNT; i++)
            {
                GameMasterScript.heroPCActor.actorFlags[i] = mdd.actorFlagsPriorToEntry[i];
            }
            if (Debug.isDebugBuild) Debug.Log("Restored hero stats.");
        }
        else
        {
            GameMasterScript.heroPCActor.myStats.RemoveAllTemporaryEffects();
            if (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.25f)
            {
                GameMasterScript.heroPCActor.myStats.SetStat(StatTypes.HEALTH, GameMasterScript.heroPCActor.myStats.GetMaxStat(StatTypes.HEALTH) * 0.25f, StatDataTypes.CUR, true);
            }
            if (Debug.isDebugBuild) Debug.Log("Didn't restore hero stats.");
        }
    }

    public static void RestoreHeroSkills(MysteryDungeon md, MysteryDungeonSaveData mdd)
    {
        if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS])
        {
            UIManagerScript.ClearEntireHotbar();
            GameMasterScript.heroPCActor.myAbilities = mdd.abilitiesPriorToEntry;
            GameMasterScript.heroPCActor.myAbilities.owner = GameMasterScript.heroPCActor;
            for (int i = 0; i < mdd.hotbarBindingsPriorToEntry.Length; i++)
            {
                UIManagerScript.hotbarAbilities[i] = mdd.hotbarBindingsPriorToEntry[i];
                switch (UIManagerScript.hotbarAbilities[i].actionType) // we must re-add each hotbar item to refresh gfx
                {
                    case HotbarBindableActions.ABILITY:
                        // Remap the ability to the one in our actual AbilityComponent block
                        AbilityScript abil = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(UIManagerScript.hotbarAbilities[i].ability.refName);
                        UIManagerScript.hotbarAbilities[i].ability = abil;
                        UIManagerScript.AddAbilityToSlot(UIManagerScript.hotbarAbilities[i].ability, i, true);
                        break;
                    case HotbarBindableActions.CONSUMABLE:
                        UIManagerScript.AddItemToSlot(UIManagerScript.hotbarAbilities[i].consume, i, true);
                        break;
                }
            }
            GameMasterScript.heroPCActor.myAbilities.SetDirty(true);

            int jobLength = GameMasterScript.heroPCActor.jobJP.Length;
            for (int i = 0; i < mdd.jobJPPriortoEntry.Length; i++)
            {
                if (i >= jobLength) break;
                GameMasterScript.heroPCActor.jobJP[i] = mdd.jobJPPriortoEntry[i];
                GameMasterScript.heroPCActor.jobJPspent[i] = mdd.jobJPSpentPriorToEntry[i];
            }

        }
    }

    public static void CleanupRebuildSpawnTables(MysteryDungeon md)
    {
        if (md.spawnTables.Count == 0)
        {             
            int startFloor = MapMasterScript.CUSTOMDUNGEON_START_FLOOR + 1;
            int endFloor = md.floors + startFloor;
            // Reconstitute our list of spawn tables
            for (int i = startFloor; i < endFloor; i++)
            {
                Map m = MapMasterScript.theDungeon.FindFloor(i);
                md.spawnTables.Add(i, m.dungeonLevelData.spawnTable);
            }
        }
    }

    public static void CleanupRemoveDungeonLevels(MysteryDungeon md, List<Map> mysteryDungeonMaps)
    {
        // Remove dungeon levels, maps from memory
        foreach (Map m in mysteryDungeonMaps)
        {
            int mapAreaID = m.mapAreaID;
            MapMasterScript.theDungeon.RemoveMapByFloor(m.floor);
            MapMasterScript.dictAllMaps.Remove(mapAreaID);
            GameMasterScript.masterDungeonLevelList.Remove(m.floor);
            MapMasterScript.OnMapRemoved(m);
        }

        DungeonMaker.customDungeonLevelDataInSaveFile.Clear();
        DungeonMaker.SetCustomDungeonLevelCounterFromLoadedLevels();

        /* for (int i = 0; i < md.floors; i++)
        {
            int mapFloorID = MapMasterScript.CUSTOMDUNGEON_START_FLOOR + i + 1;
            int mapAreaID = MapMasterScript.theDungeon.FindFloor(mapFloorID).mapAreaID;
            MapMasterScript.theDungeon.RemoveMapByFloor(mapFloorID);
            MapMasterScript.dictAllMaps.Remove(mapAreaID);
            GameMasterScript.masterDungeonLevelList.Remove(mapFloorID);
        } */
    }

    public static void CleanupRemoveMonsters(MysteryDungeon md)
    {
        // Remove spawn tables from memory and monsters contained therein, plus their items
        foreach (ActorTable table in md.spawnTables.Values)
        {
            GameMasterScript.masterSpawnTableList.Remove(table.refName);
        }

        MetaProgressScript.RemoveNonPetMonstersAndTheirWeapons(force:true);

        return;
    }

    /// <summary>
    /// Returns hero to original state, grants rewards (if victorious) returns to town.
    /// </summary>    
    public static void CompleteActiveMysteryDungeon()
    {
        // FinishMystery ExitMystery CompleteMystery Exit Mystery LeaveMystery Leave Mystery
        MysteryDungeon md = MysteryDungeonManager.GetActiveDungeon();

        exitingMysteryDungeon = true;

        GameMasterScript.heroPCActor.RemoveActorData("floor_of_last_mdcampfire");
        GameMasterScript.heroPCActor.RemoveActorData("dont_scale_md_monsters_ngplus");

        if (md == null) return;
        MysteryDungeonSaveData mdd = GameMasterScript.heroPCActor.myMysteryDungeonData;
        if (mdd == null) return;

        bool victory = mdd.dungeonVictory;

        GameMasterScript.heroPCActor.myJob = CharacterJobData.GetJobDataByEnum((int)mdd.jobPriorToEntry);

        RestoreHeroDictActorData(md, mdd);

        RestoreHeroFlaskAndPandoraBoxes(md, mdd);

        List<Item> inventoryInDungeon = null;
        EquipmentBlock gearInDungeon = null;

        DestroyTemporaryRelics();

        int moneyInDungeon = RestoreHeroMoney(md, mdd);
        inventoryInDungeon = RestoreHeroInventory(md, mdd);
        gearInDungeon = RestoreHeroGear(md, mdd);

        GameMasterScript.heroPCActor.RefreshEquipmentCollectionOwnership();

        RestoreHeroSkills(md, mdd);
        RestoreHeroStats(md, mdd);

        GameMasterScript.heroPCActor.numPandoraBoxesOpened = mdd.pandoraBoxesPriorToEntry;

        if (mdd.dungeonVictory)
        {
            ProcessEndOfDungeonRewards(md, mdd, inventoryInDungeon, gearInDungeon, moneyInDungeon);
            GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_MysteryDungeon_Complete();
            Debug.Log("Completed dungeon " + md.refName + " " + md.displayName);
            if (md.refName == "dungeon6")
            {
                GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_Ordeals_Complete();
            }

            if (ProgressTracker.CheckProgress(TDProgress.WANDERER_JOURNEY, ProgressLocations.META) < 3)
            {
                ProgressTracker.SetProgress(TDProgress.WANDERER_JOURNEY, ProgressLocations.META, 3);
            }
            GameMasterScript.gmsSingleton.TickGameTime(1, true, true);
        }
        else
        {
            RemoveBankedRelicsFromBankerOnJourneyLost(mdd);
        }

        GameMasterScript.heroPCActor.SetDefaultWeapon(false);
        MetaProgressScript.RemoveUnusedCustomItems(force:true);

        List<Map> mysteryDungeonMapsToRemove = FindMysteryDungeonMapsInMemory();

        ScanMysteryDungeonMapsAndMarkUnusedRelicsForRemoval(mysteryDungeonMapsToRemove);

        CleanupRebuildSpawnTables(md);
        CleanupRemoveDungeonLevels(md, mysteryDungeonMapsToRemove);
        CleanupRemoveMonsters(md);

        md.spawnTables.Clear();
        md.monstersInDungeon.Clear();

        // remove unique items as needed too.

        GameMasterScript.heroPCActor.ClearBattleDataAndStatuses();

        GameMasterScript.gmsSingleton.UpdateHeroObject();


        GameMasterScript.heroPCActor.myMysteryDungeonData = null; // null it out so it doesn't save.

        // Only travel if we died, since in that case, we did not use a portal.
        if (!victory)
        {
            UIManagerScript.PlayCursorSound("EnterItemWorld");
        }
        TravelManager.BackToTown_Part2(MapMasterScript.singletonMMS.townMap, actuallyTravel:!victory);

        GameMasterScript.heroPCActor.ValidateAndFixStats(true);

        RestorePlayerGamblerHand(mdd);

        // Restore player's old hotbars
        UIManagerScript.singletonUIMS.RefreshAbilityCooldowns();
        UIManagerScript.UpdateWeaponHotbarGraphics();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshStatuses();

        GameMasterScript.heroPCActor.TryLinkAllPairedItems();

        RemoveMDItemTagsFromHeroInventoryAndEquipment();

        Debug.Log("<color=green>Mystery dungeon completed! Victory status: " + mdd.dungeonVictory + "</color>");

        GameMasterScript.gmsSingleton.SetTempGameData("finishmysterydungeonturn", GameMasterScript.turnNumber);

        GameMasterScript.heroPCActor.RemoveActorData("allow_campfire_cooking");        
    } 

    public static void RemoveMDItemTagsFromHeroInventoryAndEquipment()
    {
        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            if (GameMasterScript.heroPCActor.myEquipment.equipment[i] == null) continue;
            GameMasterScript.heroPCActor.myEquipment.equipment[i].RemoveActorData("mdgear");
        }
        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            itm.RemoveActorData("mdgear");
        }
    }

    static void RemoveBankedRelicsFromBankerOnJourneyLost(MysteryDungeonSaveData mdd)
    {
        NPC banker = MapMasterScript.singletonMMS.townMap.FindActor("npc_banker") as NPC;

        foreach(int id in mdd.idsOfBankedRelicsForVictory)
        {
            Item itemFromDict = GameMasterScript.gmsSingleton.TryLinkActorFromDict(id) as Item;
            if (itemFromDict != null)
            {
                banker.myInventory.RemoveItem(itemFromDict);
                itemFromDict.saveSlotIndexForCustomItemTemplate = GameStartData.saveGameSlot;
                SharedBank.allRelicTemplates.Remove(itemFromDict.actorRefName);
            }
            else
            {
                Debug.LogError("Banker could not find item ID " + id + " which should be banked!");
            }            
        }
    }

    static void RestorePlayerGamblerHand(MysteryDungeonSaveData mdd)
    {
        foreach (PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
        {
            PlayingCard.ReturnCard(pc);
        }
        GameMasterScript.heroPCActor.gamblerHand.Clear();
        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, true);

        foreach (PlayingCard pc in mdd.playingCardsPriorToEntry)
        {
            PlayingCard drawPC = PlayingCard.DrawSpecificCard(pc.suit, pc.face);
            GameMasterScript.heroPCActor.gamblerHand.Add(drawPC);
        }
        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(false, false);
    }

    public static void EnterMysteryDungeon(string dungeonRef)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(WaitThenEnterMysteryDungeon(dungeonRef));        
    }

    static IEnumerator WaitThenEnterMysteryDungeon(string dungeonRef)
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        MysteryDungeon theDungeon = DungeonMaker.masterMysteryDungeonDict[dungeonRef];
        MysteryDungeonSaveData mdd = new MysteryDungeonSaveData();
        GameMasterScript.heroPCActor.myMysteryDungeonData = mdd;
        activeDungeon = theDungeon;
        mdd.dungeonRefName = theDungeon.refName;        

        Debug.Log("Prepare to enter mystery dungeon: " + theDungeon.refName);

        BackupHeroDictActorData(mdd);

        mdd.jobPriorToEntry = GameMasterScript.heroPCActor.myJob.jobEnum;

		MusicManagerScript.RequestPlayMusic("wanderer", true);



        UIManagerScript.FadeOut(0.75f);
        UIManagerScript.PlayCursorSound("EnterItemWorld");
        yield return new WaitForSeconds(0.8f);

        // Flush out previous mystery dungeon data.
        MetaProgressScript.RemoveCustomDungeonLevelsItemsAndNonPetMonsters();

        ProgressTracker.RemoveProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO);

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.FLASK])
        {
            mdd.flaskUsesPriorToEntry = GameMasterScript.heroPCActor.regenFlaskUses;
            GameMasterScript.heroPCActor.SetRegenFlaskUses(3);
        }
        
        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.PET])
        {
            MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();            
        }

        GameMasterScript.heroPCActor.RemoveAllSummons();

        PetPartyUIScript.RefreshContentsOfPlayerParty();

        mdd.pandoraBoxesPriorToEntry = GameMasterScript.heroPCActor.numPandoraBoxesOpened;
        GameMasterScript.heroPCActor.numPandoraBoxesOpened = 0;

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.MONEY])
        {
            mdd.moneyPriorToEntry = GameMasterScript.heroPCActor.GetMoney();
            GameMasterScript.heroPCActor.SetMoneyUnsafe(100);
        }
        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            ResetHeroStatsForMysteryDungeon(mdd);
        }
        else
        {
            GameMasterScript.heroPCActor.RemoveActorData("dont_scale_md_monsters_ngplus");
            GameMasterScript.heroPCActor.myStats.RemoveAllTemporaryEffects();
            GameMasterScript.heroPCActor.myStats.HealToFull();
        }

        RemoveMDItemTagsFromHeroInventoryAndEquipment();

        BackupHeroInventoryIfNecessary(mdd, theDungeon);

        BackupHeroGearIfNecessary(mdd, theDungeon);
        
        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.SKILLS])
        {
            BackupHeroSkills(mdd);
        }

        if (theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR] && !theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
        {
            // necessary to re-apply bonuses when we bring gear but NOT stats
            GameMasterScript.heroPCActor.myEquipment.UnequipAndReequipAllGear();
            GameMasterScript.heroPCActor.myStats.HealToFull();
        }

        VerifyHeroHasFeatsAndExecuteEffectsIfNecessary(theDungeon);
        GameMasterScript.heroPCActor.VerifyStatsAbilitiesStatuses(); 

        GameMasterScript.heroPCActor.SetActorData("allow_campfire_cooking", 1);

        UIManagerScript.UpdateWeaponHotbarGraphics();
        UIManagerScript.RefreshHotbarItems();
        UIManagerScript.RefreshHotbarSkills();
        UIManagerScript.RefreshPlayerStats();
        UIManagerScript.RefreshStatuses();

        UIManagerScript.TurnOnPrettyLoading(0.33f, 1.0f);
        string tipString = StringManager.GetString("exp_mysterydungeon_tip" + UnityEngine.Random.Range(0, MYSTERY_DUNGEON_STRING_TIPS));
        UIManagerScript.WriteLoadingText(tipString);

        foreach (PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
        {
            mdd.AddPlayingCard(pc);
            PlayingCard.ReturnCard(pc);
        }
        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, true);
        GameMasterScript.heroPCActor.gamblerHand.Clear();

        yield return new WaitForSeconds(0.75f);

        GameMasterScript.heroPCActor.myStats.RemoveAllStatusByRef("status_escapedungeon");

        UIManagerScript.FillLoadingBar(0f);

        GameMasterScript.gmsSingleton.SetTempGameData("creatingmysterydungeon", 1);

        yield return DungeonMaker.CreateAndPopulateMysteryDungeon(theDungeon);

        GameMasterScript.gmsSingleton.SetTempGameData("creatingmysterydungeon", 0);

        if (Debug.isDebugBuild) Debug.Log("Mystery dungeon has been created!");

        UIManagerScript.TurnOffPrettyLoading(0.5f, 0.25f);

        GameMasterScript.gmsSingleton.SetTempGameData("enteringmysterydungeon", 1);

        TravelManager.TravelMaps(theDungeon.mapsInDungeon[0], null, false);
        GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
        
    }

    static void BackupHeroDictActorData(MysteryDungeonSaveData mdd)
    {
        foreach(string key in dictActorDataToBackup)
        {
            int value = GameMasterScript.heroPCActor.ReadActorData(key);
            if (value >= 0)
            {
                mdd.dictSavedActorData.Add(key, value);
                GameMasterScript.heroPCActor.RemoveActorData(key);
            }
        }
    }

    static void ProcessEndOfDungeonRewards(MysteryDungeon md, MysteryDungeonSaveData mdd, List<Item> inventoryInDungeon, EquipmentBlock gearInDungeon, int moneyInDungeon)
    {

        List<Item> itemsGained = null;
        List<Item> itemsToRemove = new List<Item>();

        // Combine money we earned from dungeon with money the player was carrying before
        if (md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.MONEY])
        {
            GameMasterScript.heroPCActor.ChangeMoney(moneyInDungeon, doNotAlterFromGameMods:true);
        }
        
        // If we had a "new" equipment block in the dungeon and this flag is active, add all these items to player inventory.
        if (md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR])
        {            
            if (gearInDungeon != null)
            {
                itemsGained = new List<Item>();
                for (int i = 0; i < gearInDungeon.equipment.Length; i++)
                {
                    if (gearInDungeon.equipment[i] != null && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(gearInDungeon.equipment[i], onlyActualFists: true))
                    {
                        itemsGained.Add(gearInDungeon.equipment[i]);
                    }
                }
                foreach (Item itm in itemsGained)
                {
                    GameMasterScript.heroPCActor.myInventory.AddItem(itm, false);
                }
            }
            else
            {
                // If we only brought our OWN equipment in, then we need to prune equipped stuff that is tagged as "mdnokeep"                
                List<Item> itemsToRemoveFromEquipment = new List<Item>();
                for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
                {
                    Item evalItem = GameMasterScript.heroPCActor.myEquipment.equipment[i];
                    if (evalItem != null && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(evalItem, onlyActualFists: true) && evalItem.ReadActorData("mdnokeep") == 1)
                    {
                        //GameMasterScript.heroPCActor.myInventory.RemoveItem(evalItem, true);
                        itemsToRemove.Add(evalItem);
                    }
                }
                foreach(Item itm in itemsToRemoveFromEquipment)
                {
                    GameMasterScript.heroPCActor.myInventory.RemoveItem(itm, true);
                }
            }
        }

        // If we had a "new" inventory in the dungeon and this flag is active, add all these items to player inventory.
        if (md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS])
        {            
            if (inventoryInDungeon != null)
            {
                foreach (Item itm in inventoryInDungeon)
                {
                    GameMasterScript.heroPCActor.myInventory.AddItem(itm, true); // this was 'removefrompreviouscollection', but we don't really care about that
                }
                inventoryInDungeon.Clear();
            }
            else
            {
                // If we only brought our OWN inventory in, then we actually need to prune everything tagged as "mdnokeep"
                itemsToRemove.Clear();
                foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
                {
                    if (itm.ReadActorData("mdnokeep") == 1)
                    {
                        itemsToRemove.Add(itm);
                    }
                }
                foreach (Item itm in itemsToRemove)
                {
                    GameMasterScript.heroPCActor.myInventory.RemoveItem(itm);
                }
            }
        }

        // This only applies if we had a NEW inventory and/or gear list, and we want to parse it for Relics gained
        if (md.resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.RELICS])
        {
            if (inventoryInDungeon != null)
            {
                foreach (Item itm in inventoryInDungeon)
                {
                    if (itm.customItemFromGenerator || itm.ReadActorData("exprelic") == 1)
                    {
                        GameMasterScript.heroPCActor.myInventory.AddItem(itm, true);
                    }
                }
            }
            if (gearInDungeon != null)
            {
                for (int i = 0; i < gearInDungeon.equipment.Length; i++)
                {
                    Item evalItem = gearInDungeon.equipment[i];
                    if (evalItem != null && (evalItem.customItemFromGenerator || evalItem.ReadActorData("exprelic") == 1))
                    {
                        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(evalItem, true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when player DIES in a mystery dungeon and handles all gameover stuff from there.
    /// </summary>
    public static void MysteryDungeonGameOver()
    {
        if (Debug.isDebugBuild) Debug.Log("Died in mystery dungeon.");
        UIManagerScript.singletonUIMS.CloseAllDialogs();

        if (GetActiveDungeon().dieInRealLife)
        {
            // If we die for real, just do normal game over stuff.
            GameMasterScript.GameOver();
            return;
        }

        if (GameMasterScript.playerDied) return;
        GameMasterScript.playerDied = true;

        UIManagerScript.FlashRed(1.25f);
        GameMasterScript.cameraScript.SetToGrayscale(true);
		MusicManagerScript.RequestPlayMusic("gameover",false,false);


        MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();

        GameMasterScript.heroPCActor.myAnimatable.StopAnimation();

        GameMasterScript.gmsSingleton.StartCoroutine(MysteryDungeonManager.WaitThenContinueMysteryDungeonGameOver(1.25f));
    }

    static IEnumerator WaitThenContinueMysteryDungeonGameOver(float time)
    {
        yield return new WaitForSeconds(time);
        GameMasterScript.SetAnimationPlaying(false);
               
        UIManagerScript.ClearConversation();

        UIManagerScript.currentConversation = new Conversation();

        GameLogScript.LogWriteStringRef("log_event_knockedout");

        GameMasterScript.heroPCActor.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
        UIManagerScript.RefreshPlayerStats();

        string text = "";

        if (GameMasterScript.heroPCActor.whoKilledMe != null)
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.whoKilledMe.displayName);
        }
        else
        {
            StringManager.SetTag(0, "?????");
        }

        StringManager.SetTag(1, MapMasterScript.activeMap.GetName());

        string textBuilder = "";
        textBuilder = StringManager.GetString("desc_knockout_actor") + "\n";
        /* textBuilder += StringManager.GetString("exp_desc_die_mysterydungeon") + "\n";
         textBuilder += PlayerAdvice.GetAdviceStringForPlayer(); */
        StringManager.SetTag(0, textBuilder); 
        if (Debug.isDebugBuild) Debug.Log("About to start MD gameover convo.");

        UIManagerScript.StartConversationByRef("gameover_mysterydungeon", DialogType.KNOCKEDOUT, null, false, "", true);
        UIManagerScript.OverrideDialogWidth(1280f, 660f);
    }

    public static void CheckForMysteryDungeonEvents(Monster mon, Map processMap)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;
        if (!MapMasterScript.activeMap.IsMysteryDungeonMap()) return;

        // Player defeated the mystery king, hooray!
        if (mon.ReadActorData("mysteryking") == 1)
        {
            ProgressTracker.SetProgress(TDProgress.MYSTERYKING_DEFEAT, ProgressLocations.HERO, 1);
            GameMasterScript.gmsSingleton.StartCoroutine(MysteryDungeonManager.MysteryDungeonVictoryCutscene());
        }
    }

    static IEnumerator MysteryDungeonVictoryCutscene()
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        yield return new WaitForSeconds(0.5f);
        BattleTextManager.NewText(StringManager.GetString("misc_ui_victory"), GameMasterScript.heroPCActor.GetObject(), Color.green, 1.5f, 2f);

		MusicManagerScript.RequestPlayMusic("BossVictory",true,false);

        yield return new WaitForSeconds(1.5f);
        UIManagerScript.StartConversationByRef("mysterydungeon_bossdefeated", DialogType.STANDARD, null);
        GameMasterScript.heroPCActor.myMysteryDungeonData.dungeonVictory = true;
    }

    static void ResetHeroStatsForMysteryDungeon(MysteryDungeonSaveData mdd)
    {
        mdd.statsPriorToEntry = GameMasterScript.heroPCActor.myStats;
        mdd.fighterDataPriorToEntry = GameMasterScript.heroPCActor.cachedBattleData;
        GameMasterScript.heroPCActor.ClearAllFighterBattleData();
        GameMasterScript.heroPCActor.cachedBattleData.SetDirty();

        for (int i = 0; i < GameMasterScript.heroPCActor.actorFlags.Length; i++)
        {
            mdd.actorFlagsPriorToEntry[i] = GameMasterScript.heroPCActor.actorFlags[i];
            GameMasterScript.heroPCActor.actorFlags[i] = false;
        }

        mdd.allDamageMultiplierPriorToEntry = GameMasterScript.heroPCActor.allDamageMultiplier;
        mdd.allMitigationPriorToEntry = GameMasterScript.heroPCActor.allMitigationAddPercent;
        GameMasterScript.heroPCActor.allDamageMultiplier = 1f;
        GameMasterScript.heroPCActor.allMitigationAddPercent = 1f;

        GameMasterScript.heroPCActor.effectsInflictedOnTurn.Clear();
        GameMasterScript.heroPCActor.effectsInflictedStringKeys.Clear();
        GameMasterScript.heroPCActor.TurnsSinceLastCombatAction = 0;
        GameMasterScript.heroPCActor.turnsInSamePosition = 0;
        GameMasterScript.heroPCActor.turnsSinceLastSlow = 0;
        GameMasterScript.heroPCActor.turnsSinceLastDamaged = 0;
        GameMasterScript.heroPCActor.turnsSinceLastStun = 0;

        GameMasterScript.heroPCActor.myStats = new StatBlock();
        GameMasterScript.heroPCActor.myStats.SetOwner(GameMasterScript.heroPCActor);
        GameMasterScript.heroPCActor.myStats.SetHeroBaseStats();

        // JP was here, but it should really be in Skills instead.

        // Do we have to reset anything else?
        GameMasterScript.heroPCActor.SetActorData("dont_scale_md_monsters_ngplus", 1);
    }

    static void VerifyHeroHasFeatsAndExecuteEffectsIfNecessary(MysteryDungeon md)
    {
        foreach(string feat in GameMasterScript.heroPCActor.heroFeats)
        {
            if (!GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(feat))
            {
                AbilityScript template = GameMasterScript.masterAbilityList[feat];
                GameMasterScript.heroPCActor.myAbilities.AddNewAbility(template, true, false, true);
            }
            if (feat == "skill_toughness" && !md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
            {
                GameMasterScript.heroPCActor.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, 30f, StatDataTypes.ALL);
            }
        }
    }

    static void BackupHeroSkills(MysteryDungeonSaveData mdd)
    {
        for (int i = 0; i < UIManagerScript.hotbarAbilities.Length; i++)
        {
            mdd.hotbarBindingsPriorToEntry[i] = new HotbarBindable();
            mdd.hotbarBindingsPriorToEntry[i].actionType = UIManagerScript.hotbarAbilities[i].actionType;
            mdd.hotbarBindingsPriorToEntry[i].consume = UIManagerScript.hotbarAbilities[i].consume;
            mdd.hotbarBindingsPriorToEntry[i].ability = UIManagerScript.hotbarAbilities[i].ability;
        }
        mdd.abilitiesPriorToEntry = GameMasterScript.heroPCActor.myAbilities;
        GameMasterScript.heroPCActor.myAbilities = new AbilityComponent();
        GameMasterScript.heroPCActor.myAbilities.owner = GameMasterScript.heroPCActor;

        int jobLength = GameMasterScript.heroPCActor.jobJP.Length;
        for (int i = 0; i < mdd.jobJPPriortoEntry.Length; i++)
        {
            if (i >= jobLength) break;
            mdd.jobJPPriortoEntry[i] = GameMasterScript.heroPCActor.jobJP[i];
            mdd.jobJPSpentPriorToEntry[i] = GameMasterScript.heroPCActor.jobJPspent[i];
        }
        GameMasterScript.heroPCActor.SetJP(300f);

        GameMasterScript.heroPCActor.InitializeJPAndStartAbilities(false, GameMasterScript.heroPCActor.myJob);
        GameMasterScript.heroPCActor.myAbilities.SetDirty(true);
        GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum] = 300f;
    }

    static void BackupHeroGearIfNecessary(MysteryDungeonSaveData mdd, MysteryDungeon theDungeon)
    {
        GameMasterScript.heroPCActor.myEquipment.RemoveGearTag("md_entry_gear");
        GameMasterScript.heroPCActor.myEquipment.RemoveGearTag("mdgear");

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
            {
                if (UIManagerScript.hotbarWeapons[i] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(UIManagerScript.hotbarWeapons[i], onlyActualFists: true))
                {
                    mdd.hotbarWeaponsPriorToEntry[i] = ID_NO_WEAPON;
                }
                else
                {
                    mdd.hotbarWeaponsPriorToEntry[i] = UIManagerScript.hotbarWeapons[i].actorUniqueID;
                    UIManagerScript.hotbarWeapons[i] = GameMasterScript.heroPCActor.myEquipment.defaultWeapon;
                }
            }
            mdd.eqPriorToEntry = GameMasterScript.heroPCActor.myEquipment;
            GameMasterScript.heroPCActor.myEquipment = new EquipmentBlock();
            GameMasterScript.heroPCActor.myEquipment.owner = GameMasterScript.heroPCActor;
            GameMasterScript.heroPCActor.myEquipment.SetHeroDefaults(true);
            UIManagerScript.SwitchActiveWeaponSlot(0, true);

            for (int i = 0; i < GameMasterScript.heroPCActor.advStats.Length; i++)
            {
                mdd.advStatsPriorToEntry[i] = GameMasterScript.heroPCActor.advStats[i];
                GameMasterScript.heroPCActor.advStats[i] = 0;
            }
        }
        else
        {
            GameMasterScript.heroPCActor.myEquipment.TagAllGear("md_entry_gear", 1);
            GameMasterScript.heroPCActor.RefreshEquipmentCollectionOwnership();
            // Preserve our hotbar weapons so they don't disappear after the dungeon ends.
            for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
            {
                if (UIManagerScript.hotbarWeapons[i] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(UIManagerScript.hotbarWeapons[i], onlyActualFists: true)) continue;
                UIManagerScript.hotbarWeapons[i].SetActorData("md_entry_gear", 1);
            }
        }
    }

    static void BackupHeroInventoryIfNecessary(MysteryDungeonSaveData mdd, MysteryDungeon theDungeon)
    {
        // first clear out all the junk from our inventory
        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            itm.RemoveActorData("md_entry_gear");
            itm.RemoveActorData("mdgear");
        }        

        if (!theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.ITEMS])
        {
            List<Item> itemsToKeepInInventory = new List<Item>();

            // Flag paired equipment and don't remove it, but only if we bring our EQUIPMENT in.

            foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
            {
                if (!itm.IsEquipment()) continue;
                Equipment eq = itm as Equipment;

                bool hotbarWeaponToSave = false;

                if (eq.itemType == ItemTypes.WEAPON)
                {
                    Weapon w = eq as Weapon;
                    if (UIManagerScript.IsWeaponInHotbar(w))
                    {
                        hotbarWeaponToSave = true;
                    }
                }
                if ((eq.GetPairedItem() != null || hotbarWeaponToSave) && theDungeon.resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR])
                {
                    eq.SetActorData("md_entry_gear", 1);
                    itemsToKeepInInventory.Add(eq);
                }
                else
                {
                    eq.RemoveActorData("md_entry_gear");
                }
            }

            mdd.inventoryPriorToEntry = GameMasterScript.heroPCActor.myInventory;
            GameMasterScript.heroPCActor.CreateNewInventory();

            foreach (Item itm in itemsToKeepInInventory)
            {
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(itm, false);
            }

            if (theDungeon.HasGimmick(MysteryGimmicks.MONSTER_INFIGHTING_AND_LEVELUP))
            {
                for (int i = 0; i < 10; i++)
                {
                    Item perfume = LootGeneratorScript.CreateItemFromTemplateRef("potion_stealth1", 1.2f, 0f, false);
                    GameMasterScript.heroPCActor.myInventory.AddItem(perfume, true);
                }
            }
        }
    }

    public static List<Map> FindMysteryDungeonMapsInMemory()
    {
        List<Map> theMaps = new List<Map>();

        foreach(Map m in MapMasterScript.theDungeon.maps)
        {
            if (m.floor >= MapMasterScript.CUSTOMDUNGEON_START_FLOOR)
            {
                theMaps.Add(m);
            }
        }

        return theMaps;
    }

    /// <summary>
    /// If we kept a relic for the dungeon ONLY, it's marked as temporary. This destroys all temporary ones from inv, eq, hotbar.
    /// </summary>
    public static void DestroyTemporaryRelics()
    {
        List<Item> relicsToRemove = new List<Item>();

        // Destroy all relics that were temporary.
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (!itm.customItemFromGenerator) continue;
            if (itm.ReadActorData("loserelic") == 1)
            {
                SharedBank.MarkRelicTemplateForDeletion(itm.actorRefName);
                Debug.Log("Marking " + itm.actorRefName + " for deletion because it was just a temporary relic in inventory.");
                relicsToRemove.Add(itm);
                if (itm.itemType == ItemTypes.WEAPON)
                {
                    Weapon w = itm as Weapon;
                    if (UIManagerScript.IsWeaponInHotbar(w))
                    {
                        UIManagerScript.RemoveWeaponFromActives(w);
                    }
                }
            }
        }

        foreach (Item itm in relicsToRemove)
        {
            GameMasterScript.heroPCActor.myInventory.RemoveItem(itm);
            //Debug.Log("Removed temp relic " + itm.actorRefName + " from our inventory");
            SharedBank.allRelicTemplates.Remove(itm.actorRefName);
        }
        relicsToRemove.Clear();

        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[i];
            if (eq == null) continue;
            if (eq.itemType == ItemTypes.WEAPON)
            {
                if (GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(eq, onlyActualFists: true))
                {
                    continue;
                }
            }
            if (!eq.customItemFromGenerator) continue;
            if (eq.ReadActorData("loserelic") == 1)
            {
                SharedBank.MarkRelicTemplateForDeletion(eq.actorRefName);
                Debug.Log("Marking " + eq.actorRefName + " for deletion because it was just a temporary relic in equipment.");
                GameMasterScript.heroPCActor.myEquipment.UnequipByReference(eq);
                //Debug.Log("Unequipping " + eq.actorRefName);
                relicsToRemove.Add(eq);
            }
        }

        foreach (Item itm in relicsToRemove)
        {
            GameMasterScript.heroPCActor.myInventory.RemoveItem(itm);
            Debug.Log("Removed previously equipped temp relic " + itm.actorRefName + " from our inventory");
            SharedBank.allRelicTemplates.Remove(itm.actorRefName);
        }
    }

    public static void ScanMysteryDungeonMapsAndMarkUnusedRelicsForRemoval(List<Map> mapsToRemove)
    {
        foreach(Map m in mapsToRemove)
        {
            foreach(Actor a in m.actorsInMap)
            {
                if (a.GetActorType() == ActorTypes.HERO) continue;

                if (a.GetActorType() == ActorTypes.ITEM)
                {
                    Item i = a as Item;
                    if (!i.customItemFromGenerator) continue;
                    MarkRelicForDeletion(i, alsoMarkTemplates: true); 
                }
                else
                {
                    List<Item> inv = a.myInventory.GetInventory();
                    foreach(Item invItem in inv)
                    {
                        if (!invItem.customItemFromGenerator) continue;
                        MarkRelicForDeletion(invItem, alsoMarkTemplates: true);
                    }                    
                }
            }
        }
    }

    public static void MarkRelicForDeletion(Item i, bool alsoMarkTemplates = false)
    {
        i.SetActorData("loserelic", 1);
        
        Debug.Log("Marked relic " + i.actorRefName + " for deletion.");

        if (alsoMarkTemplates)
        {
            Item template;
            if (SharedBank.allRelicTemplates.TryGetValue(i.actorRefName, out template))
            {
                template.SetActorData("loserelic", 1);
                Debug.Log("Clean up the template version too.");
            }
        }

    }

    public static void MarkAllRelicsOnHeroForRemovalOnHardcoreOrHeroicDeath()
    {
        List<Item> inventory = GameMasterScript.heroPCActor.myInventory.GetInventory();
        foreach (Item i in inventory)
        {
            if (!i.customItemFromGenerator) continue;
            MarkRelicForDeletion(i, alsoMarkTemplates:true);
        }

        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            Equipment e = GameMasterScript.heroPCActor.myEquipment.equipment[i];
            if (e == null) continue;
            if (!e.customItemFromGenerator) continue;
            MarkRelicForDeletion(e, alsoMarkTemplates: true);
        }


    }
}



public partial class Map
{
    public bool ShouldSpawnWithLootOnThisFloor()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return true;
        if (floor >= MapMasterScript.CUSTOMDUNGEON_START_FLOOR && floor <= MapMasterScript.CUSTOMDUNGEON_END_FLOOR && MysteryDungeonManager.GetActiveDungeon() != null)
        {
            if (MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.NO_ITEM_DROPS))
            {
                return false;
            }
        }
        return true;
    }

}