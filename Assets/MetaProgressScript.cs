using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

public enum TreeAges { NOTHING, SEED, SEEDLING, SAPLING, ADULT, COUNT }
public enum TreeSpecies { OAK, ELM, ORCHID, FOOD_A, FOOD_B, SPICES, CASHCROPS, COUNT }

public class MonsterQuip
{
    public int numMonsters;
    public string text;
}

public partial class MetaProgressScript
{

    public bool saveRegionUsesCommasForFloats = false;

    public static List<TamedCorralMonster> localTamedMonstersForThisSlot;
    public static List<ReleasedMonster> releasedMonsters;
    public static List<Monster> monsterPetsAvailable;
    public bool metagameLoaded = false;
    public static int[] jobsStarted;
    public static int lowestFloorReached;
    public static int totalCharacters;
    public static bool initialized;
    public static string monsterQuips;
    public static float playTimeAtGameLoad;
    public static int totalDaysPassed;
    public static NPC[] treesPlanted;
    public static List<int> journalEntriesRead;
    public static Dictionary<string, int> dictMetaProgress;
    public static bool watchedFirstTutorial = false;
    public static List<string> recipesKnown;

    public static bool loadingMetaProgress = false;

    public const int DAYS_TO_BLOOM = 3;

    public const int NUM_TREES = 5;

    public static int loadedGameVersion;

    public static Dictionary<string, int> monstersDefeated;
    public static List<string> playerModsSavedLastInMeta;
    public static List<DefeatData> defeatHistory;

    public static List<string> relicRefsThatShouldNotBeDeleted;

    public static void FlushAllDataExceptHardcoreProgress()
    {
        if (Debug.isDebugBuild) Debug.Log("<color=red>Flushing ALL DATA from local progress.</color>");
        // Partial initialization for when we're starting a new game in HARDCORE after having previous meta progress
        totalDaysPassed = 0;
        lowestFloorReached = 0;
        dictMetaProgress.Clear();
        localTamedMonstersForThisSlot = null;
        localTamedMonstersForThisSlot = new List<TamedCorralMonster>();
        releasedMonsters = new List<ReleasedMonster>();
        monsterPetsAvailable = new List<Monster>();
        treesPlanted = new NPC[NUM_TREES];
        playerModsSavedLastInMeta = new List<string>();
        FlushUnusedCustomDataIfNecessary(force:true);        

        for (int i = 0; i < treesPlanted.Length; i++)
        {
            treesPlanted[i] = MagicTree.CreateTree(i);
        }
    }

    public static void FlushUnusedCustomDataIfNecessary(bool force = false)
    {
        //if (Debug.isDebugBuild) Debug.Log("Force flush? " + force + " " + UIManagerScript.globalDialogButtonResponse);
        if (UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.LOADGAME 
            && UIManagerScript.globalDialogButtonResponse != DialogButtonResponse.NEWGAMEPLUS || force)
        {
            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
            {
                RemoveCustomDungeonLevelsItemsAndNonPetMonsters();
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("Removing custom monsters, items.");
                RemoveCustomItemsAndNonPetMonsters(force);
            }
        }
    }

    public static void RemoveUnusedCustomItems(bool force = false)
    {
        return; // #todo - We MUST find a way to flush things.

        if (!LegendaryMaker.initialized)
        {
            return;
        }

        if (!force && (RandomJobMode.IsCurrentGameInRandomJobMode() || GameStartData.slotInRandomJobMode[GameStartData.saveGameSlot]))
        {
            if (Debug.isDebugBuild) Debug.Log("Don't delete unused custom items while we're still in random job mode and we're just loading the game.");
            return;
        }

        bool playerExists = GameMasterScript.heroPCActor != null;

        List<Item> itemsToRemove = new List<Item>();

        bool inMysteryDungeon = false;
        if (playerExists)
        {
            inMysteryDungeon = GameMasterScript.heroPCActor.myMysteryDungeonData != null;
            if (inMysteryDungeon && !force)
            {
                // Don't remove relics while we're still in the dungeon.
                return;
            }
        }
        
        bool dreamOpen = MapMasterScript.itemWorldItem != null;

        float timeStart = Time.realtimeSinceStartup;

        foreach (Item itm in SharedBank.allRelicTemplates.Values)
        {
            bool npcHasItem = false;
            foreach(NPC n in GameMasterScript.allLoadedNPCs)
            {
                if (n.myInventory.HasItemByRef(itm.actorRefName))
                {
                    npcHasItem = true;
                    break;
                }
            }

            if (npcHasItem) continue;

            if (dreamOpen && MapMasterScript.itemWorldItem.actorRefName == itm.actorRefName)
            {
                continue;
            }

            if (playerExists)
            {
                if (GameMasterScript.heroPCActor.myInventory.HasItemByRef(itm.actorRefName))
                {
                    continue;
                }
                if (inMysteryDungeon && GameMasterScript.heroPCActor.myMysteryDungeonData.inventoryPriorToEntry.HasItemByRef(itm.actorRefName))
                {
                    continue;
                }
                if (inMysteryDungeon && GameMasterScript.heroPCActor.myMysteryDungeonData.eqPriorToEntry.HasEquipmentByRef(itm.actorRefName))
                {
                    continue;
                }
                if (GameMasterScript.heroPCActor.myEquipment.HasEquipmentByRef(itm.actorRefName))
                {
                    continue;
                }
                int itemFloor = 0;

                // Maybe we dropped the item on the floor somewhere or sold it to someone.
                if (GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.TryGetValue(itm.actorRefName, out itemFloor))
                {
                    bool itemIsInATemporaryArea = false;
                    if (itemFloor >= 0)
                    {
                        itemIsInATemporaryArea = true;
                        if (itemFloor < MapMasterScript.ITEMWORLD_START_FLOOR || itemFloor > MapMasterScript.ITEMWORLD_END_FLOOR)
                        {
                            //if (Debug.isDebugBuild) Debug.Log(itm.actorRefName + " is not in an item world, so don't delete it");
                            itemIsInATemporaryArea = false;
                        }
                        else
                        {
                            // It IS an item world. But is the item world open?
                            if (MapMasterScript.itemWorldOpen)
                            {
                                itemIsInATemporaryArea = false;
                            }
                        }
                        /* if (itemFloor < MapMasterScript.CUSTOMDUNGEON_START_FLOOR || itemFloor > MapMasterScript.CUSTOMDUNGEON_END_FLOOR)
                        {
                            if (Debug.isDebugBuild) Debug.Log(itm.actorRefName + " is not in a mystery dungeon, so don't delete it.");
                            itemIsInATemporaryArea = false;
                        } */
                    }
                    if (!itemIsInATemporaryArea)
                    {
                        continue;
                    }
                }
            }
            
            // this item may be saved for.. some reason! in that case, don't ever get rid of it
            if (relicRefsThatShouldNotBeDeleted.Contains(itm.actorRefName))
            {
                continue;
            }

            GameMasterScript.masterItemList.Remove(itm.actorRefName);

            itemsToRemove.Add(itm);
        }

        bool pcActorExists = GameMasterScript.heroPCActor != null;

        foreach (Item itm in itemsToRemove)
        {
            SharedBank.allRelicTemplates.Remove(itm.actorRefName);
            if (pcActorExists)
            {
                GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.Remove(itm.actorRefName);
            }
#if UNITY_EDITOR
            if (Debug.isDebugBuild) Debug.Log("<color=yellow>Removing custom item permanently: " + itm.actorRefName + "</color>");
#endif
        }
    }

    public static void RemoveMysteryDungeonLevels()
    {
        if (!DungeonMaker.initialized) return;

        // Don't clear levels if we're in the dungeon.
        if (MysteryDungeonManager.InOrCreatingMysteryDungeon())
        {
            if (Debug.isDebugBuild) Debug.Log("Don't remove mystery dungeon levels while we're IN the dungeon!");
            return;
        }

        // Remove dungeon levels and their spawn tables from memory
        foreach (int floor in DungeonMaker.customDungeonLevelDataInSaveFile.Keys)
        {
            GameMasterScript.masterDungeonLevelList.Remove(floor);
            DungeonLevel dl = DungeonMaker.customDungeonLevelDataInSaveFile[floor];
            GameMasterScript.masterSpawnTableList.Remove(dl.spawnTable.refName);
        }
        DungeonMaker.customDungeonLevelDataInSaveFile.Clear();
        DungeonMaker.SetCustomDungeonLevelCounterFromLoadedLevels();
    }

    public static void RemoveNonPetMonstersAndTheirWeapons(bool force = false)
    {
        //if (Debug.isDebugBuild) Debug.Log("Removing ALL non pet monsters and their weapons. Forced? " + force);

        if (!MonsterMaker.initialized) return;

        if (MysteryDungeonManager.InOrCreatingMysteryDungeon() && !force)
        {
            //if (Debug.isDebugBuild) Debug.Log("Don't remove mystery dungeon monsters while we're IN the dungeon!");
            return;
        }

        if ((RandomJobMode.IsCurrentGameInRandomJobMode() || GameStartData.slotInRandomJobMode[GameStartData.saveGameSlot])
            && !force)
        {
            //if (Debug.isDebugBuild) Debug.Log("Don't remove mystery dungeon monsters in random job mode!");
            return;
        }

        // Remove monsters and their weapons, unless the player has somehow captured one as a pet
        List<MonsterTemplateData> monstersToRemove = new List<MonsterTemplateData>();
        foreach (MonsterTemplateData m in MonsterMaker.uniqueMonstersSpawnedInSaveFile)
        {
            if (!CheckIfPlayerHasCreatureAsPet(m.refName))
            {
                GameMasterScript.masterMonsterList.Remove(m.refName);
                GameMasterScript.masterItemList.Remove(m.weaponID);
                if (!string.IsNullOrEmpty(m.offhandWeaponID))
                {
                    GameMasterScript.masterItemList.Remove(m.weaponID);
                }
                monstersToRemove.Add(m);
            }
        }

        List<Actor> monsterReferencesToRemoveFromDeadActorList = new List<Actor>();

        bool deadActorsListExists = GameMasterScript.deadActorsToSaveAndLoad != null;

        foreach (MonsterTemplateData mon in monstersToRemove)
        {
#if UNITY_EDITOR
            //if (Debug.isDebugBuild) Debug.Log("Removing monster template " + mon.refName);
#endif
            monstersDefeated.Remove(mon.refName);
            MonsterMaker.uniqueMonstersSpawnedInSaveFile.Remove(mon);
            MonsterMaker.uniqueWeaponsSpawnedInSaveFile.Remove(mon.weaponID);
            if (!string.IsNullOrEmpty(mon.offhandWeaponID))
            {
                MonsterMaker.uniqueWeaponsSpawnedInSaveFile.Remove(mon.offhandWeaponID);
            }

            if (deadActorsListExists)
            {
                GameMasterScript.deadActorsToSaveAndLoad.RemoveAll(a => a.actorRefName == mon.refName);
            }
        }
    }

    public static void RemoveCustomDungeonLevelsItemsAndNonPetMonsters()
    {
        RemoveUnusedCustomItems();
        RemoveMysteryDungeonLevels();
        RemoveNonPetMonstersAndTheirWeapons();
    }

    public static void RemoveCustomItemsAndNonPetMonsters(bool force = false)
    {
        RemoveUnusedCustomItems(force);
        RemoveNonPetMonstersAndTheirWeapons(force);
    }

    public static void FlushAllData(int saveSlot = 0)
    {
        //Debug.Log("Meta progress flushed.");
        Initialize();
        totalDaysPassed = 0;
        SetTotalCharacters(0);
        lowestFloorReached = 0;
        dictMetaProgress.Clear();
        //if (Debug.isDebugBuild) Debug.Log("<color=red>Flushing ALL DATA, period.</color>");
        localTamedMonstersForThisSlot = null;
        localTamedMonstersForThisSlot = new List<TamedCorralMonster>();
        playerModsSavedLastInMeta = new List<string>();
        defeatHistory = new List<DefeatData>();        
        relicRefsThatShouldNotBeDeleted = new List<string>();
        GameStartData.ChangeGameMode(GameModes.COUNT); 
        if (journalEntriesRead != null)
        {
            journalEntriesRead.Clear();
        }
        if (monstersDefeated != null)
        {
            monstersDefeated.Clear();
        }

        FoodCartScript.ResetAllVariablesToGameLoad();

        FlushUnusedCustomDataIfNecessary(true);

        LegendaryMaker.FlushSaveFileData();
        MonsterMaker.FlushSaveFileData();
        DungeonMaker.FlushSaveFileData();
    }

    public static void TryAddMonsterFought(string refName, int amount = 1)
    {
        MonsterTemplateData mtd = GameMasterScript.masterMonsterList[refName];
        if (mtd == null) // should never ever happen but it might
        {
            return;
        }
        if (!mtd.showInPedia) return;
        if (monstersDefeated.ContainsKey(refName))
        {
            monstersDefeated[refName] += amount;
        }
        else
        {
            monstersDefeated.Add(refName, amount);
        }
        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersKnown(monstersDefeated.Keys.Count);
    }

    public static int GetMonstersDefeated(string refName)
    {
        int amt;
        if (monstersDefeated.TryGetValue(refName, out amt))
        {
            return amt;
        }
        else
        {
            return 0;
        }
    }

    public static bool RosePetalsAllowed()
    {
        return ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) >= 1;
    }

    // Must have at least one mature tree for this to work.
    public static bool FoodCartCalloutPossible()
    {        
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i] == null) continue;
            if (!treesPlanted[i].treeComponent.alive) continue;
            if ((int)treesPlanted[i].treeComponent.age >= (int)TreeAges.ADULT)
            {
                return true;
            }
        }

        return false;
    }

    public static void SetTotalCharacters(int amt)
    {
        //Debug.Log("Set total characters to " + amt + " was previously " + totalCharacters);
        totalCharacters = amt;
    }

    public static void ChangeTotalCharacters(int amt)
    {
        //Debug.Log("Set total characters to " + (amt+ totalCharacters) + " was previously " + totalCharacters);
        totalCharacters += amt;
    }

    public static void Initialize()
    {
        if (!initialized)
        {
            relicRefsThatShouldNotBeDeleted = new List<string>();
            defeatHistory = new List<DefeatData>();
            playerModsSavedLastInMeta = new List<string>();
            dictMetaProgress = new Dictionary<string, int>();
            monstersDefeated = new Dictionary<string, int>();
            if (Debug.isDebugBuild) Debug.Log("<color=green>Initializing meta progress for the first time.</color>");
            jobsStarted = new int[(int)CharacterJobs.COUNT];
            localTamedMonstersForThisSlot = new List<TamedCorralMonster>();
            releasedMonsters = new List<ReleasedMonster>();
            monsterPetsAvailable = new List<Monster>();
            treesPlanted = new NPC[NUM_TREES];
            journalEntriesRead = new List<int>();
            recipesKnown = new List<string>();
            SetTotalCharacters(0);
            totalDaysPassed = 0;
            lowestFloorReached = 0;

            for (int i = 0; i < treesPlanted.Length; i++)
            {
                treesPlanted[i] = new NPC();
                treesPlanted[i].treeComponent = new MagicTree(treesPlanted[i]);
                treesPlanted[i].treeComponent.alive = false;
                treesPlanted[i].treeComponent.age = TreeAges.NOTHING;
                treesPlanted[i].treeComponent.dayPlanted = 0;
                treesPlanted[i].treeComponent.slot = i;
                treesPlanted[i].treeComponent.species = TreeSpecies.OAK;

                int dispSlot = i + 1;

                treesPlanted[i].actorRefName = "town_tree" + dispSlot;
                treesPlanted[i].displayName = "Magic Tree";
                treesPlanted[i].prefab = "GroveTree";
                treesPlanted[i].dialogRef = "grovetree";
                treesPlanted[i].playerCollidable = true;
                treesPlanted[i].interactable = true;

                //treesPlanted[i].CopyFromTemplateRef("town_tree" + dispSlot);

            }

            initialized = true;
            watchedFirstTutorial = false;
            //Debug.Log("Meta progress script initialized");
            monsterQuips = "";
        }

        relicRefsThatShouldNotBeDeleted.Clear();
        defeatHistory.Clear();
        playerModsSavedLastInMeta.Clear();
        dictMetaProgress.Clear();
        monstersDefeated.Clear();
        
        for (int i = 0; i < jobsStarted.Length; i++)
        {
            jobsStarted[i] = 0;
        }

        localTamedMonstersForThisSlot = new List<TamedCorralMonster>();
        releasedMonsters.Clear();
        monsterPetsAvailable.Clear();

        journalEntriesRead.Clear();
        recipesKnown.Clear();
        SetTotalCharacters(0);
        totalDaysPassed = 0;
        lowestFloorReached = 0;

        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i] == null) treesPlanted[i] = new NPC();
            treesPlanted[i].actorUniqueID = 0;

            if (treesPlanted[i].treeComponent == null) treesPlanted[i].treeComponent = new MagicTree(treesPlanted[i]);

            treesPlanted[i].treeComponent.Initialize(treesPlanted[i]);

            treesPlanted[i].treeComponent.alive = false;
            treesPlanted[i].treeComponent.age = TreeAges.NOTHING;
            treesPlanted[i].treeComponent.dayPlanted = 0;
            treesPlanted[i].treeComponent.slot = i;
            treesPlanted[i].treeComponent.species = TreeSpecies.OAK;

            int dispSlot = i + 1;

            treesPlanted[i].actorRefName = "town_tree" + dispSlot;
            treesPlanted[i].displayName = "Magic Tree";
            treesPlanted[i].prefab = "GroveTree";
            treesPlanted[i].dialogRef = "grovetree";
            treesPlanted[i].playerCollidable = true;
            treesPlanted[i].interactable = true;

            //treesPlanted[i].CopyFromTemplateRef("town_tree" + dispSlot);

        }

        initialized = true;
        watchedFirstTutorial = false;
        //Debug.Log("Meta progress script initialized");
        monsterQuips = "";
    }

    public static void ResetMonsterQuips()
    {
        monsterQuips = "";
    }

    public static void LearnRecipe(string rRef)
    {
        if (String.IsNullOrEmpty(rRef))
        {
            Debug.Log("Trying to learn null/empty recipe?");
            return;
        }
        if (!recipesKnown.Contains(rRef))
        {
            recipesKnown.Add(rRef);
            if (GameMasterScript.gameLoadSequenceCompleted)
            {
                string dispName = CookingScript.FindRecipe(rRef).displayName;
                StringManager.SetTag(0, dispName);
                GameLogScript.GameLogWrite(UIManagerScript.greenHexColor + StringManager.GetString("log_learn_recipe") + "</color>", GameMasterScript.heroPCActor);
            }
        }
        GameMasterScript.gmsSingleton.statsAndAchievements.SetRecipesKnown(recipesKnown.Count);
    }

    public static void LinkAllActors()
    {
        if (MapMasterScript.singletonMMS.townMap2 == null)
        {
            //Debug.Log("Town map 2 is null?");
            return;
        }
        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.GetActorMap() == null)
            {
                act.SetActorMap(MapMasterScript.singletonMMS.townMap2);
            }
        }
    }



    public static void SpawnFoodFromTreeInventories()
    {
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i].treeComponent.alive)
            {
                foreach (Item itm in treesPlanted[i].myInventory.GetInventory())
                {
                    MapTileData mtd = MapMasterScript.singletonMMS.townMap2.GetRandomEmptyTile(treesPlanted[i].GetPos(), 1, true);
                    MapMasterScript.singletonMMS.townMap2.PlaceActor(itm, mtd);
                    if (MapMasterScript.activeMap == MapMasterScript.singletonMMS.townMap2)
                    {
                        MapMasterScript.singletonMMS.SpawnItem(itm);
                    }
                }
            }
        }
        //Debug.Log("Finished spawning food from tree inventories");

    }

    public static void AgeAllTrees(int amount)
    {
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i].treeComponent.alive)
            {
                treesPlanted[i].treeComponent.UpdateAgeOfTree();

                for (int t = 0; t < amount; t++)
                {
                    int currentFruit = treesPlanted[i].myInventory.GetActualInventoryCount();
                    for (int x = currentFruit; x < MagicTree.MAX_FRUIT[(int)treesPlanted[i].treeComponent.age]; x++)
                    {
                        float localChance = MagicTree.FRUIT_CHANCE;
                        switch (treesPlanted[i].treeComponent.treeRarity)
                        {
                            case Rarity.UNCOMMON:
                                localChance = 0.33f;
                                break;
                            case Rarity.MAGICAL:
                                localChance = 0.4f;
                                break;
                        }
                        if (UnityEngine.Random.Range(0, 1f) <= localChance)
                        {
                            // Success!
                            Consumable newFood = new Consumable();

                            string treeTable = "";

                            int rarityValue = (int)treesPlanted[i].treeComponent.treeRarity;
                            rarityValue++;

                            switch (treesPlanted[i].treeComponent.species)
                            {
                                case TreeSpecies.FOOD_A:
                                    treeTable = "treedrops_fooda" + rarityValue;
                                    break;
                                case TreeSpecies.FOOD_B:
                                    treeTable = "treedrops_foodb" + rarityValue;
                                    break;
                                case TreeSpecies.SPICES:
                                    treeTable = "treedrops_spices" + rarityValue;
                                    break;
                                case TreeSpecies.CASHCROPS:
                                    treeTable = "treedrops_valuables" + rarityValue;
                                    break;
                                default:
                                    treeTable = "treedrops_fooda" + rarityValue;
                                    break;
                            }

                            string iRef = LootGeneratorScript.GetLootTable(treeTable).GetRandomActorRef();
                            Item template = Item.GetItemTemplateFromRef(iRef);
                            newFood.CopyFromItem(template);
                            newFood.SetUniqueIDAndAddToDict();
                            treesPlanted[i].myInventory.AddItem(newFood, false);

                            MapTileData mtd = null;

                            mtd = MapMasterScript.singletonMMS.townMap2.GetRandomEmptyTile(treesPlanted[i].GetPos(), 1, true);
                            //Debug.Log("Spawning food around tree " + i + " at " + treesPlanted[i].GetPos() + " found loc: " + mtd.pos + " " + mtd.playerCollidable + " " + mtd.monCollidable);

                            MapMasterScript.singletonMMS.townMap2.PlaceActor(newFood, mtd);
                            if (MapMasterScript.activeMap == MapMasterScript.singletonMMS.townMap2)
                            {
                                MapMasterScript.singletonMMS.SpawnItem(newFood);
                            }

                            newFood.collection = treesPlanted[i].myInventory;
                        }
                    }
                }


            }
        }
    }

    public static void ChopTree(int slot)
    {
        if (treesPlanted[slot] == null)
        {
            Debug.Log("No tree in slot " + slot + ", can't chop it.");
            return;
        }

        if (treesPlanted[slot].GetObject() == null)
        {
            Debug.Log("Tree in slot " + slot + " has no object?!");
            return;
        }

        GameMasterScript.StartWatchedCoroutine(ChopDownTreeCoroutine(slot));       
    }

    /// <summary>
    /// Make the tree shake a bit, and then chop it down. Will also update player XP, JP, and other mechanical aspects.
    /// </summary>
    /// <param name="slot">index in the tree array of which tree to violate</param>
    /// <returns></returns>
    public static IEnumerator ChopDownTreeCoroutine(int slot)
    {
        //get the tree object
        NPC deadTree = treesPlanted[slot];
        Vector2 vTreePos = deadTree.GetObject().transform.position;

        //how old is this doomed tree?
        TreeAges amountOfHistoryWeAreRuiningInTheNameOfLoot = deadTree.treeComponent.age;

        //shake and wait a few times
        for (int t = 0; t < (int)amountOfHistoryWeAreRuiningInTheNameOfLoot; t++)
        {
            //grr, hero angery
            GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("Attack",
                CombatManagerScript.GetDirection(GameMasterScript.heroPCActor, treesPlanted[slot]),
                GameMasterScript.heroPCActor.lastCardinalDirection);

            //poof?
            //the taller the tree, the higher up the poof should play
            Vector2 vPoofPos = vTreePos;
            vPoofPos.y += 0.2f * (int)amountOfHistoryWeAreRuiningInTheNameOfLoot;
            CombatManagerScript.GenerateSpecificEffectAnimation(vPoofPos + UnityEngine.Random.insideUnitCircle * 0.2f, "LeafPoof", null, false, 0f, true);
            CombatManagerScript.GenerateSpecificEffectAnimation(vPoofPos + UnityEngine.Random.insideUnitCircle * 0.2f, "WoodBreak", null, false, 0f, true);

            //shake
            deadTree.myMovable.Jitter(0.1f + t * 0.05f);

            //if this is the last hit, and tree is/was big and old and verdant; boom, shake, shake, shake the screen
            //setting to >= in case one day we have a rank older than Adult.
            if (t == (int)amountOfHistoryWeAreRuiningInTheNameOfLoot - 1 &&
                amountOfHistoryWeAreRuiningInTheNameOfLoot >= TreeAges.ADULT)
            {
                GameMasterScript.cameraScript.AddScreenshake(0.25f);
                //chop noise
                UIManagerScript.PlayCursorSound("BigTreeFinalHit");
            }
            else
            {
                //chop noise
                UIManagerScript.PlayCursorSound("ChopTreeDown");
            }

            //wait
            yield return new WaitForSeconds(0.4f);
        }

        //final explosion
        CombatManagerScript.GenerateSpecificEffectAnimation(vTreePos + UnityEngine.Random.insideUnitCircle, "LeafPoof", null, false, UnityEngine.Random.value * 0.2f, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(vTreePos + UnityEngine.Random.insideUnitCircle, "LeafPoof", null, false, UnityEngine.Random.value * 0.2f, true);

        CombatManagerScript.GenerateSpecificEffectAnimation(vTreePos + UnityEngine.Random.insideUnitCircle, "WoodBreak", null, false, 0f, true);
        CombatManagerScript.GenerateSpecificEffectAnimation(vTreePos + UnityEngine.Random.insideUnitCircle, "WoodBreak", null, false, 0f, true);


        //then award loots and power
        GameLogScript.LogWriteStringRef("log_tree_chop");
        GameMasterScript.gmsSingleton.AwardJP(treesPlanted[slot].treeComponent.GetJPReward());
        GameMasterScript.gmsSingleton.AwardXPFlat(treesPlanted[slot].treeComponent.GetXPReward(), false);

        //remove it
        treesPlanted[slot].treeComponent.alive = false;
        treesPlanted[slot].treeComponent.age = TreeAges.NOTHING;
        string actorName = "town_tree" + (slot + 1);
        NPC theTree = MapMasterScript.activeMap.FindActor(actorName) as NPC;
        GameObject.Destroy(theTree.GetObject());

        // Make sure we are syncing the meta trees with actual trees on map.
        Vector2 oldPos = theTree.GetPos();
        MapMasterScript.activeMap.RemoveActorFromMap(theTree);
        MapTileData tileForTree = MapMasterScript.GetTile(oldPos);
        MapMasterScript.activeMap.PlaceActor(treesPlanted[slot], tileForTree);

        //replace it with a dirty hole
        MapMasterScript.singletonMMS.SpawnNPC(treesPlanted[slot]);
        treesPlanted[slot].myMovable.SetInSightAndSnapEnable(true);
        UIManagerScript.RefreshPlayerStats();

        //I beat nature!
        //yay!
        UIManagerScript.PlayCursorSound("Heavy Learn");
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("UseItem", Directions.SOUTH, Directions.SOUTH);

    }

    public static void PlantTree(int slot, Item seed)
    {
        Consumable c = seed as Consumable;
        c.ChangeQuantity(-1);
        if (c.Quantity == 0)
        {
            GameMasterScript.heroPCActor.myInventory.RemoveItem(c);
        }

        GameMasterScript.gmsSingleton.SetTempStringData("seedplanted", "(" + c.displayName + ")");

        GameLogScript.LogWriteStringRef("log_grove_planttree");

        MagicTree plantedTree = treesPlanted[slot].treeComponent;
        treesPlanted[slot].treeComponent.npcObject = treesPlanted[slot];
        switch (seed.actorRefName)
        {
            case "seeds_tree1":
                plantedTree.species = TreeSpecies.FOOD_A;
                break;
            case "seeds_tree2":
                plantedTree.species = TreeSpecies.FOOD_B;
                break;
            case "seeds_tree3":
                plantedTree.species = TreeSpecies.SPICES;
                break;
            case "seeds_tree4":
                plantedTree.species = TreeSpecies.CASHCROPS;
                break;
        }
        plantedTree.age = TreeAges.SEED;
        plantedTree.dayPlanted = MetaProgressScript.totalDaysPassed;
        plantedTree.alive = true;
        plantedTree.slot = slot;
        plantedTree.treeRarity = seed.rarity;

        plantedTree.whoPlanted = GameMasterScript.heroPCActor.displayName;

        //Play a sound
        UIManagerScript.PlayCursorSound("PlantSeeds");

        //Find the transform of the dirt with hole. We're gonna cheat to make sure that the newly planted seed
        //doesn't move the dirt. That's because the dirt with hole is hand-placed in the map,
        //but the new object will snap to a rounded tile position.
        Vector3 vDirtalertLocation = treesPlanted[slot].GetObject().transform.position;

        //remove the tree, then destroy the tree
        MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(treesPlanted[slot].GetObject());
        GameObject.Destroy(treesPlanted[slot].GetObject());

        //now create a new NPC object based on the tree data which is... I don't know
        MapMasterScript.singletonMMS.SpawnNPC(treesPlanted[slot]);
        treesPlanted[slot].myMovable.SetInSightAndSnapEnable(true);

        //yay!
        GameMasterScript.heroPCActor.myAnimatable.SetAnimDirectional("UseItem", Directions.SOUTH, Directions.SOUTH);

        //Now sit upon our positional throne of lies
        treesPlanted[slot].GetObject().transform.position = vDirtalertLocation;

        //make a poof
        CombatManagerScript.GenerateSpecificEffectAnimation(vDirtalertLocation, "LeafPoof", null, false, 0.15f, true);


    }

    public static string ParseQuip(string txt, string m1, string m2, string m3)
    {
        if (m1 != null)
        {
            txt = Regex.Replace(txt, "#m1#", "<color=yellow>" + m1 + "</color>");
        }
        if (m2 != null)
        {
            txt = Regex.Replace(txt, "#m2#", "<color=yellow>" + m2 + "</color>");
        }
        if (m3 != null)
        {
            txt = Regex.Replace(txt, "#m3#", "<color=yellow>" + m3 + "</color>");
        }
        return txt;
    }


    public static string GetMonsterQuips()
    {
        if (monsterQuips != "")
        {
            return monsterQuips;
        }
        string buildText = "";
        List<MonsterQuip> usedQuips = new List<MonsterQuip>();
        List<MonsterQuip> possible = new List<MonsterQuip>();

        string m1 = null;
        string m2 = null;
        string m3 = null;
        string holder = "";

        int numQuips = 0;

        switch (MetaProgressScript.localTamedMonstersForThisSlot.Count)
        {
            case 1:
                m1 = MetaProgressScript.localTamedMonstersForThisSlot[0].monsterObject.displayName;
                foreach (MonsterQuip mq in GameMasterScript.masterMonsterQuipList)
                {
                    if (mq.numMonsters == 1)
                    {
                        possible.Add(mq);
                    }
                }
                buildText = ParseQuip(possible[UnityEngine.Random.Range(0, possible.Count)].text, m1, m2, m3);

                break;
            case 2:
                m1 = localTamedMonstersForThisSlot[0].monsterObject.displayName;
                m2 = localTamedMonstersForThisSlot[1].monsterObject.displayName;
                numQuips = UnityEngine.Random.Range(2, 4);
                foreach (MonsterQuip mq in GameMasterScript.masterMonsterQuipList)
                {
                    if ((mq.numMonsters == 1) || (mq.numMonsters == 2))
                    {
                        possible.Add(mq);
                    }
                }
                for (int i = 0; i < numQuips; i++)
                {
                    MonsterQuip mq = possible[UnityEngine.Random.Range(0, possible.Count)];
                    possible.Remove(mq);
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        holder = m1;
                        m1 = m2;
                        m2 = holder;
                    }
                    buildText += ParseQuip(mq.text, m1, m2, m3);
                    if (i < numQuips - 1)
                    {
                        buildText += "\n\n";
                    }
                    else
                    {
                        buildText += "\n";
                    }
                }
                break;
            case 3:
                numQuips = UnityEngine.Random.Range(3, 5);
                foreach (MonsterQuip mq in GameMasterScript.masterMonsterQuipList)
                {
                    possible.Add(mq);
                }
                for (int i = 0; i < numQuips; i++)
                {
                    MonsterQuip mq = possible[UnityEngine.Random.Range(0, possible.Count)];
                    possible.Remove(mq);

                    // Shuffle monsters.
                    int nm1 = UnityEngine.Random.Range(0, 3);
                    int nm2 = UnityEngine.Random.Range(0, 3);
                    while (nm2 == nm1)
                    {
                        nm2 = UnityEngine.Random.Range(0, 3);
                    }
                    int nm3 = UnityEngine.Random.Range(0, 3);
                    while ((nm3 == nm1) || (nm3 == nm2))
                    {
                        nm3 = UnityEngine.Random.Range(0, 3);
                    }
                    m1 = localTamedMonstersForThisSlot[0].monsterObject.displayName;
                    m2 = localTamedMonstersForThisSlot[1].monsterObject.displayName;
                    m3 = localTamedMonstersForThisSlot[2].monsterObject.displayName;

                    buildText += ParseQuip(mq.text, m1, m2, m3);
                    if (i < numQuips - 1)
                    {
                        buildText += "\n\n";
                    }
                    else
                    {
                        buildText += "\n";
                    }
                }
                break;
        }

        monsterQuips = buildText;

        return buildText;
    }

    static List<ReleasedMonster> possibleWriters;

    public static void CheckForAndGenerateLetterFromReleasedMonster(int days)
    {
        if (!GameMasterScript.heroPCActor.myStats.IsAlive()) return;

        if (possibleWriters == null) possibleWriters = new List<ReleasedMonster>();
        possibleWriters.Clear();

        foreach(ReleasedMonster rm in releasedMonsters)
        {
            int calcDaysSinceReleased = totalDaysPassed - rm.dayReleased;
            float chanceToWrite = calcDaysSinceReleased * MonsterCorralScript.CHANCE_RELEASED_MONSTER_WRITELETTER;
            if (chanceToWrite > 0.5f) chanceToWrite = 0.5f;
            if (UnityEngine.Random.Range(0,1f) <= chanceToWrite)
            {
                possibleWriters.Add(rm);
            }
        }
        

        if (possibleWriters.Count > 0)
        {
            ReleasedMonster writer = possibleWriters[UnityEngine.Random.Range(0, possibleWriters.Count)];
            releasedMonsters.Remove(writer);
            // Now... write the letter.
            Item letter = LootGeneratorScript.CreateItemFromTemplateRef("item_monsterletter", 1.0f, 0f, false, false);
            Consumable c = letter as Consumable;
            c.InscribeMonsterLetter(writer);
            //Debug.Log("Generated letter from " + writer.displayName);


            Map mapToUse = MapMasterScript.activeMap;

            Vector2 pos = GameMasterScript.heroPCActor.GetPos();

            if (MapMasterScript.activeMap.IsMysteryDungeonMap())
            {
                mapToUse = MapMasterScript.singletonMMS.townMap;
                pos = MapMasterScript.singletonMMS.townMap.FindActor("npc_katje").GetPos();
            }

            MapTileData mtd = mapToUse.GetRandomEmptyTile(pos, 1, true);
            letter.SetPos(mtd.pos);
            letter.SetSpawnPos(mtd.pos);

            mapToUse.PlaceActor(letter, mtd);

            if (MapMasterScript.activeMap == mapToUse)
            {
                MapMasterScript.singletonMMS.SpawnItem(letter);
            }
            
        }

        
    }

    public static void ReadJournalEntry(int entryNumber)
    {
        if (!journalEntriesRead.Contains(entryNumber))
        {
            journalEntriesRead.Add(entryNumber);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("Trying to read already read journal entry? " + entryNumber);
        }
    }

    public static float GetPlayTime()
    {
        float playTime = playTimeAtGameLoad + (Time.fixedTime - GameMasterScript.timeAtGameStartOrLoad);
        return playTime;
    }

    public static void ReadCreationFeats(XmlReader metaReader)
    {
        if (metaCreationFeatsUnlocked == null) metaCreationFeatsUnlocked = new List<string>();
        metaCreationFeatsUnlocked.Clear();
        string unparsed = metaReader.ReadElementContentAsString();
        string[] parsed = unparsed.Split('|');
        for (int i = 0; i < parsed.Length; i++)
        {
            if (!metaCreationFeatsUnlocked.Contains(parsed[i]))
            {
                metaCreationFeatsUnlocked.Add(parsed[i]);
            }      
        }
    }


    public static void UnlockSharaMode()
    {
        SharedBank.UnlockJobNoPopup(CharacterJobs.SHARA);

        UIManagerScript.StartConversationByRef("sharamode_unlock", DialogType.KEYSTORY, null);

        //Keep track of this in the Forever Files, even if all the data is erased.
        if (GameMasterScript.actualGameStarted)
        {
            TDPlayerPrefs.SetInt(GlobalProgressKeys.SHARA_CAMPAIGN_UNLOCKED, 1);
            TDPlayerPrefs.SetString(GlobalProgressKeys.SHARA_WHO_UNLOCKED_ME, GameMasterScript.heroPCActor.displayName);
        }

        SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE);

        TDPlayerPrefs.SetLocalInt("show_shara_loading_screen_nexttime", 1);
    }
    
    public static void AssignIDsToTreeItemsAndTrees()
    {
        if (treesPlanted == null)
        {
            treesPlanted = new NPC[NUM_TREES];
        }
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i] == null) continue;
            if (treesPlanted[i].treeComponent == null) continue;
            if (!treesPlanted[i].treeComponent.alive) continue;

            if (treesPlanted[i].myInventory == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Tree " + i + " has no inventory?");
                continue;
            }
            if (treesPlanted[i].myInventory.GetInventory() == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Tree " + i + " has no inventory list?");
                continue;
            }

            foreach (Item itm in treesPlanted[i].myInventory.GetInventory())
            {
                itm.SetUniqueIDAndAddToDict();
            }
        }

        if (MapMasterScript.singletonMMS == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Singleton MMS is null somehow");
            return;
        }

        if (MapMasterScript.singletonMMS.townMap2 == null)
        {
            if (Debug.isDebugBuild) Debug.Log("Town map 2 is null somehow...");
            return;
        }

        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act == null)
            {
                if (Debug.isDebugBuild) Debug.Log("Null actor in ActorsInMap in town2?!");
                continue;
            }
            if (act.GetActorType() == ActorTypes.NPC)
            {
                NPC n = act as NPC;
                if (n.actorRefName.Contains("town_tree"))
                {
                    int oldID = n.actorUniqueID;
                    n.SetUniqueIDAndAddToDict();
                    //Debug.Log("Town tree " + n.actorRefName + " old ID was " + oldID + ", is now " + n.actorUniqueID);
                }
            }
        }
    }

    public static int ReadMetaProgress(string key)
    {
#if UNITY_EDITOR
        if (key == "shara_start") return 0;
#endif
        if (dictMetaProgress == null) return -1; 
        if (dictMetaProgress.ContainsKey(key))
        {
            return dictMetaProgress[key];
        }
        return -1;
    }

    public static void SetMetaProgress(string key, int value)
    {
        if (dictMetaProgress.ContainsKey(key))
        {
            dictMetaProgress[key] = value;
            return;
        }
        dictMetaProgress.Add(key, value);
    }

    public static void AddMetaProgress(string key, int value)
    {
        if (dictMetaProgress.ContainsKey(key))
        {
            dictMetaProgress[key] += value;
            return;
        }
        dictMetaProgress.Add(key, value);
    }

    public static void HarvestAllFoodFromTrees()
    {
        List<Item> fruits = null;

        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (!treesPlanted[i].treeComponent.alive) continue;

            fruits = treesPlanted[i].treeComponent.GetAllFoodFromTree();

            for (int x = 0; x < fruits.Count; x++)
            {
                Item droppedFruit = fruits[x];
                MapTileData mtd = MapMasterScript.singletonMMS.townMap2.mapArray[(int)droppedFruit.GetPos().x, (int)droppedFruit.GetPos().y];
                bool anyActor = false;
                Actor itemInMap = mtd.FindActorByRef(droppedFruit.actorRefName, out anyActor);
                if (anyActor) // Visual handling
                {
                    mtd.RemoveActorByRef(itemInMap.actorRefName);
                    MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(itemInMap);
                    MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(itemInMap.GetObject());
                    GameMasterScript.ReturnActorObjectToStack(itemInMap, itemInMap.GetObject(), "GenericItemPrefab");
                }
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(droppedFruit, true);
                StringManager.SetTag(0, droppedFruit.displayName);
                StringManager.SetTag(1, treesPlanted[i].treeComponent.GetSpeciesName());
                GameLogScript.GameLogWrite(StringManager.GetString("log_harvestfoodfromtree"), GameMasterScript.heroPCActor);

            }
        }
        UIManagerScript.PlayCursorSound("HarvestAll");
    }

    public static int CountAllFoodInTrees()
    {
        int foodCount = 0;
        for (int i = 0; i < treesPlanted.Length; i++)
        {
            if (treesPlanted[i].treeComponent.alive)
            {
                foodCount += treesPlanted[i].treeComponent.GetAllFoodFromTree().Count;
            }
        }

        return foodCount;
    }

    public static string GetDisplayPlayTime(bool total, float useThisTime)
    {
        // If false, just current character;

        float seconds = 0f;

        if (total)
        {
            seconds = GetPlayTime();
        }
        else
        {
            if (!GameMasterScript.actualGameStarted) // File select screen
            {
                seconds = GameStartData.playTimeInSeconds;
            }
            else
            {
                seconds = GameMasterScript.heroPCActor.GetPlayTime();
            }

        }

        if (useThisTime != 0f)
        {
            seconds = useThisTime;
        }

        seconds = (float)Math.Round(seconds, 0);

        // TEST: 

        float minutes = seconds / 60f;
        float hours = minutes / 60f;

        // Example: 6380 seconds.

        float dispSeconds = Mathf.Floor(seconds % 60f); // 6380 = 20 seconds
        float dispMinutes = Mathf.Floor(minutes % 60f); // 106 = 46 minutes
        float dispHours = Mathf.Floor(hours);

        string secondsText = "";
        string minutesText = "";
        string hoursText = "";

        if (dispSeconds < 10f)
        {
            secondsText = "0" + dispSeconds;
        }
        else
        {
            secondsText = dispSeconds.ToString();
        }

        if (dispMinutes < 10f)
        {
            minutesText = "0" + dispMinutes;
        }
        else
        {
            minutesText = dispMinutes.ToString();
        }

        if (dispHours < 10f)
        {
            hoursText = "0" + dispHours;
        }
        else
        {
            hoursText = dispHours.ToString();
        }

        string returnText = hoursText + ":" + minutesText + ":" + secondsText;

        return returnText;
    }

    public static int GetBankerBullionMaxInvestment(int playerLevel)
    {
        int value = ((playerLevel * playerLevel) * 55)+500;
        
        if (value > 15000)
        {
            value = 15000;
        }

        return value;
    }

    public static float GetBankerBullionInvestmentRate(int playerLevel)
    {
        float rate = 0.125f + (playerLevel * 0.015f);
        return rate;
    }

    public static int GetBankerBullionTime(int maxInvestment)
    {
        int minTime = 3;
        minTime += (int)(maxInvestment / 1000f);
        if (minTime > 7)
        {
            minTime = 7;
        }
        return minTime;
    }

    public static void AddDefeatData(DefeatData dd)
    {
        foreach(DefeatData checkDD in defeatHistory)
        {
            if (dd.dateAndTime == checkDD.dateAndTime)
            {
                return;
            }
        }
        defeatHistory.Add(dd);
    }

    public static bool CreateResponsesFromDefeatData()
    {
        Conversation defeatConvo = GameMasterScript.FindConversation("dialog_view_defeatdata");
        TextBranch main = defeatConvo.allBranches[0];
        main.responses.Clear();
        string branchName = "";
        int branchIndex = 0;

        // For each defeat data, we create a button that will display the defeat data contained
        for (int i = 0; i < MetaProgressScript.defeatHistory.Count; i++)
        {
            DefeatData dd = MetaProgressScript.defeatHistory[i];
            branchName = "defeat" + branchIndex;
            ButtonCombo viewData = new ButtonCombo();
            viewData.actionRef = branchName;
            viewData.dbr = DialogButtonResponse.CONTINUE;
            viewData.buttonText = "<color=yellow>" + dd.charName + "</color> (" + dd.dateAndTime + ")";
            viewData.dialogEventScript = "DisplayDefeatDataInDialogBox";
            viewData.dialogEventScriptValue = i.ToString();
            main.responses.Add(viewData);
        }
        ButtonCombo exit = new ButtonCombo();
        exit.actionRef = "exit";
        exit.buttonText = StringManager.GetString("dialog_banker_town_bullion_btn_6");
        exit.dbr = DialogButtonResponse.EXIT;

        main.responses.Add(exit);

        return true;
    }

    /// <summary>
    /// Flags a Relic refname so that it is NEVER removed for any reason. This will slowly inflate save files, but Mimics won't destroy items anymore.
    /// </summary>
    /// <param name="iRef"></param>
    public static void EnsureRelicRefIsNeverRemoved(string iRef)
    {
        if (!relicRefsThatShouldNotBeDeleted.Contains(iRef))
        {
            relicRefsThatShouldNotBeDeleted.Add(iRef);
        }
    }

    public static void CheckForFirstTimeDLC2Cleanup()
    {
        if (ProgressTracker.CheckProgress(TDProgress.DLC2_FIRSTTIME_RUN, ProgressLocations.META) == 1) return;

        ProgressTracker.SetProgress(TDProgress.DLC2_FIRSTTIME_RUN, ProgressLocations.META, 1);

        ProgressTracker.SetProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.META, -1);
        ProgressTracker.SetProgress(TDProgress.DRAGON_KICKOFF_QUEST, ProgressLocations.HERO, -1);
        /* ProgressTracker.SetProgress(TDProgress.CRAFTINGBOX, ProgressLocations.META, -1);
        ProgressTracker.SetProgress(TDProgress.DRAGON_FROG, ProgressLocations.META, -1);
        ProgressTracker.SetProgress(TDProgress.DRAGON_FROG, ProgressLocations.HERO, -1); */

        for (int i = 360; i <= 399; i++)
        {
            Map findMap = MapMasterScript.theDungeon.FindFloor(i);
            if (findMap != null)
            {
                int id = findMap.mapAreaID;
                GameMasterScript.heroPCActor.mapsExploredByMapID.Remove(id);
            }
            GameMasterScript.heroPCActor.mapsExploredByMapID.Remove(i);
        }

    }

    public static void OnReturnToTitleScreen()
    {
        if (bufferedMetaDataInAllSlots == null) bufferedMetaDataInAllSlots = new string[GameMasterScript.kNumSaveSlots];
        if (bufferedMetaDataDirty == null) bufferedMetaDataDirty = new bool[GameMasterScript.kNumSaveSlots];
        if (bufferedHeroDataInAllSlots == null) bufferedHeroDataInAllSlots = new string[GameMasterScript.kNumSaveSlots];
        if (bufferedHeroDataDirty == null) bufferedHeroDataDirty = new bool[GameMasterScript.kNumSaveSlots];

        for (int i = 0; i < bufferedMetaDataDirty.Length; i++)
        {
            bufferedMetaDataDirty[i] = true;
            bufferedHeroDataDirty[i] = true;
        }
    }

    public static void CopyMetaUnlocksIntoSharedProgress()
    {
        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.SORCERESS_UNLOCKED) == 1 ||
            TDPlayerPrefs.GetInt(GlobalProgressKeys.SHARA_CAMPAIGN_UNLOCKED) == 1 ||
            TDPlayerPrefs.GetInt("sharacampaignunlocked") == 1)
        {
            SharedBank.AddSharedProgressFlag(SharedSlotProgressFlags.SHARA_MODE);
        }

        if (TDPlayerPrefs.GetInt(GlobalProgressKeys.SORCERESS_UNLOCKED) == 1 ||
        ProgressTracker.CheckProgress(TDProgress.SORCERESS_UNLOCKED, ProgressLocations.META) == 1)
        {
            SharedBank.UnlockJobNoPopup(CharacterJobs.MIRAISHARA);
        }

        if (metaJobsUnlocked != null)
        {
            for (int i = 0; i < metaJobsUnlocked.Length; i++)
            {
                if (!metaJobsUnlocked[i]) continue;
                if (SharedBank.jobsUnlocked[i]) continue;

                SharedBank.UnlockJobNoPopup((CharacterJobs)i);
            }
        }

        if (metaCreationFeatsUnlocked != null)
        {
            foreach(string feat in metaCreationFeatsUnlocked)
            {
                if (!SharedBank.creationFeatsUnlocked.Contains(feat))
                {
                    SharedBank.creationFeatsUnlocked.Add(feat);
                }
            }
        }
    }

}
