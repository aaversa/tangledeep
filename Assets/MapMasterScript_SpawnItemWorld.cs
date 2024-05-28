using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class MapMasterScript : MonoBehaviour {

    static List<TDProgress> flagsThatCountTowardPossibleDragonDreamMaps = new List<TDProgress>()
    {
        TDProgress.DRAGON_BANDIT_DUNGEON,
        TDProgress.DRAGON_ROBOT_DUNGEON,
        TDProgress.CRAFTINGBOX,
        TDProgress.DRAGON_SPIRIT_DUNGEON,
        TDProgress.DRAGON_BEAST_DUNGEON,
        TDProgress.DRAGON_JELLY_DUNGEON,
    };

    // create item world create item dream spawn item world spawn item dream
    public Map[] SpawnItemWorld(Item itm, Item orbUsed, float magicChance, DungeonLevel forcedLevelType = null, ItemWorldMetaData forcedMetaData = null, bool debugTest = false)
    {
        float cv = 0;
        if (itm != null)
        {
            cv = itm.challengeValue;
        }

        // round cv correctly
        cv = CustomAlgorithms.RoundToNearestFiveHundredth(cv);

        List<int> specialFloorIDsUsed = new List<int>();

        int unlockedDragonFloorTypes = 0;
        float chanceOfDragonFloor = 0f;
        foreach(TDProgress checkFlag in flagsThatCountTowardPossibleDragonDreamMaps)
        {
            if (ProgressTracker.CheckProgress(checkFlag, ProgressLocations.META) >= 1)
            {
                unlockedDragonFloorTypes++;
            }
        }
        chanceOfDragonFloor = unlockedDragonFloorTypes * CHANCE_DRAGON_FLOOR_PER_UNLOCKED_DUNGEON;

        bool creatingNightmareWorld = false;

        if (orbUsed.ReadActorData("nightmare_orb") == 1)
        {
            creatingNightmareWorld = true;
            cv += 0.1f;
        }

        if (cv < 1.0f || cv > 500f)
        {
            cv = 1.0f;
        }

        float maxPossibleCV = LootGeneratorScript.GetMaxChallengeValueForItems();

        if (cv > maxPossibleCV)
        {
            cv = maxPossibleCV;
        }

        int numFloors = 0;
        float startCV = 1.0f;
        float cvIncrement = 0.5f;
        if (cv >= 1.0f && cv <= 1.2f)
        {
            numFloors = 2;
            startCV = cv - 0.1f;
            if (startCV < 1.0f) startCV = 1.0f;
            cvIncrement = 0.1f;
        }
        else if (cv > 1.2f && cv <= 1.4f)
        {
            numFloors = 3;
            startCV = cv - 0.1f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.4f && cv <= 1.6f)
        {
            numFloors = 4;
            startCV = cv - 0.15f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.6f && cv <= 1.9f)
        {
            numFloors = 5;
            startCV = cv - 0.15f;
            cvIncrement = 0.05f;
        }
        else if (cv > 1.9f && cv < 2.1f)
        {
            numFloors = 6;
            startCV = cv - 0.2f;
            cvIncrement = 0.05f;
        }
        else if (cv >= 2.1f)
        {
            numFloors = 7;
            startCV = cv - 0.2f;
            cvIncrement = 0.05f;
        }

        Equipment eq = itm as Equipment;
        ItemWorldMetaData meta = null;


        float chanceFountainFloor = CHANCE_FOUNTAIN_FLOOR;
        float chanceFoodFloor = CHANCE_FOOD_FLOOR;
        float localNKChance = CHANCE_ITEMWORLD_NIGHTMARE_PRINCE_SPAWN;
        float localCoolFrogChance = CHANCE_COOLFROG;
        float localGoldfrogFloorChance = CHANCE_GOLDFROG_FLOOR;
        float localCharmChance = CHANCE_ITEMWORLD_CHARM;
        float localBerserkFloorChance = CHANCE_BERSERK_FLOOR;
        float localBrawlFloorChance = CHANCE_ITEMWORLD_BRAWLFLOOR;


        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("luciddreamer"))
        {
            chanceFoodFloor *= 1.25f;
            chanceFountainFloor *= 1.25f;
            localNKChance *= 1.15f;
            localCoolFrogChance *= 1.2f;
            localGoldfrogFloorChance *= 1.2f;
            localCharmChance *= 1.25f;
            localBerserkFloorChance *= 1.25f;
            localBrawlFloorChance *= 1.18f;
        }


        //We will almost always be using an item so this is code that will certainly run
        //almost all the time almost.
        if (eq != null)
        {
            if ((eq.rarity == Rarity.GEARSET || eq.rarity == Rarity.ARTIFACT) && eq.challengeValue <= 1.2f)
            {
                numFloors = 3;
                startCV = cv - 0.1f;
                cvIncrement = 0.05f;
            }
            meta = eq.GetItemWorldProperties();
        }
        else
        {
            meta = forcedMetaData;
        }

        //If we get here with no item or forced metadata,
        //the wheels fell off
        if (meta == null)
        {
            meta = new ItemWorldMetaData();
            meta.properties = new bool[(int)ItemWorldProperties.COUNT];
        }

        if (startCV < 1.0f) startCV = 1.0f;

        bool addNightmareFloor = false;

        if (cv > 1.2f) localNKChance += 0.05f;
        if (cv > 1.4f) localNKChance += 0.05f;
        if (cv > 1.6f) localNKChance += 0.05f;
        if (cv > 1.8f) localNKChance += 0.05f;

        if (cv <= 1.35f) localNKChance = 0f;


        if (UnityEngine.Random.Range(0, 1f) <= localNKChance && !creatingNightmareWorld)
        {
            addNightmareFloor = true; // This is for nightmare PRINCES
            numFloors++;
        }

        bool[] itemWorldProperties = meta.properties;

        if (creatingNightmareWorld)
        {
            numFloors = 3;
            itemWorldProperties[(int)ItemWorldProperties.TYPE_NOCHAMPIONS] = false;
            // Maybe make more modifications?
            if (Debug.isDebugBuild) Debug.Log("Creating nightmare world with 3 floors");
            ItemDreamFunctions.InitializeNightmareWorldHeroData();
        }

        Map[] itemWorld = new Map[numFloors];

        bool workFloorIsNightmarePrinceFloor = false;

        if (itm != null)
        {
            itemWorldItem = itm;
            itemWorldItemID = itm.actorUniqueID;
            Debug.Log("<color=green>Setting item world item to " + itemWorldItem.actorRefName + "</color>");
        }
        else
        {
            Debug.Log("<color=red>No item world item specified?</color>");
        }

        float maxCV = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) ? GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION : GameMasterScript.MAX_CHALLENGE_RATING;

        // Create a data pack that we can pass to sub functions and break this big Chonker up
        ItemDreamCreationData iData = new ItemDreamCreationData();

        iData.chanceOfDragonFloor = chanceOfDragonFloor;

        for (int i = 0; i < numFloors; i++)
        {
            if (Debug.isDebugBuild) Debug.Log("Generating floor " + i + " out of " + numFloors);
            float localCV = startCV + (cvIncrement * i);
            if (localCV > maxCV)
            {
                localCV = maxCV;
            }

            bool validMapGenerated = false;
            DungeonLevel localMap = null;

            int attemptsToGenerateIWMap = 0;

            // This loop will run and ensure we don't generate a null map that breaks the game
            while (!validMapGenerated)
            {
                attemptsToGenerateIWMap++;

                //If we force a type, use that, otherwise roll one from the dictionary
                localMap = forcedLevelType;

                // If spawning a Nightmare floor, we want to use a specific template.
                if (i == numFloors - 1 && addNightmareFloor)
                {
                    workFloorIsNightmarePrinceFloor = true;

                    localMap = GameMasterScript.masterDungeonLevelList[NIGHTMARE_PRINCE_FLOOR];

                    // Use difficulty settings based on the hardest floor in the world.
                    localMap.expectedPlayerLevel = itemWorld[i - 1].dungeonLevelData.expectedPlayerLevel;
                    localMap.spawnTable = itemWorld[i - 1].dungeonLevelData.spawnTable;
                    localMap.challengeValue = itemWorld[i - 1].dungeonLevelData.challengeValue;
                    localCV = localMap.challengeValue;
                    localMap.maxChampionMods = itemWorld[i - 1].dungeonLevelData.maxChampionMods;
                    localMap.maxChampions = itemWorld[i - 1].dungeonLevelData.maxChampions;
                }

                if (creatingNightmareWorld)
                {
                    #region Nightmare World stuff.
                    switch (i)
                    {
                        case 0:
                            localMap = GameMasterScript.masterDungeonLevelList[NIGHTMARE_WORLD_FLOOR1];
                            break;
                        case 1:
                            localMap = GameMasterScript.masterDungeonLevelList[NIGHTMARE_WORLD_FLOOR2];
                            break;
                        case 2:
                            localMap = GameMasterScript.masterDungeonLevelList[NIGHTMARE_WORLD_FLOOR3];
                            break;
                    }

                    // Find other meta data for monsters, champs, spawntable etc. to use as reference
                    DungeonLevel templateDL = null;
                    List<DungeonLevel> possibleLevels = null;

                    if (localCV <= 1.9f)
                    {
                        foreach (float key in GameMasterScript.itemWorldMapDict.Keys)
                        {
                            if (Mathf.Abs(localCV - key) < 0.01f)
                            {
                                possibleLevels = GameMasterScript.itemWorldMapDict[key];
                                templateDL = possibleLevels[UnityEngine.Random.Range(0, possibleLevels.Count)];
                                break;
                            }
                        }

                        localMap.spawnTable = templateDL.spawnTable;
                        localMap.maxChampions = templateDL.maxChampions;
                        localMap.maxChampionMods = templateDL.maxChampionMods;
                        localMap.expectedPlayerLevel = templateDL.expectedPlayerLevel + 1;
                        localMap.challengeValue = templateDL.challengeValue;
                        localCV = localMap.challengeValue;
                    }
                    else // cv is above previous level cap / cv 1.9f, e.g. DLC 1
                    {
                        // For these just pick ANY existing level.                        
                        templateDL = GameMasterScript.itemWorldMapList.GetRandomElement();

                        // This is above the previous level cap, so scale things up.
                        localMap.maxChampions = BalanceData.GetMaxChampionsByCV(localCV);
                        localMap.maxChampionMods = 4;
                        localMap.challengeValue = localCV;
                        localMap.expectedPlayerLevel = BalanceData.GetExpectedPlayerLevelByCV(localCV);
                        localMap.spawnTable = BalanceData.GetSpawnTableByCV(localCV);
                    }
                    #endregion
                }

                bool templateFromDungeonMap = false;
                DungeonLevel fallbackLevelData = localMap;

                iData.fallbackLevelData = fallbackLevelData;
                iData.localMap = localMap;
                iData.localCV = localCV;
                iData.specialFloorIDsUsed = specialFloorIDsUsed;
                iData.forcedLevelType = forcedLevelType;
                iData.workFloorIsNightmarePrinceFloor = workFloorIsNightmarePrinceFloor;
                iData.itemWorldProperties = itemWorldProperties;
                iData.creatingNightmareWorld = creatingNightmareWorld;

                localMap = ItemDreamFunctions.FindTemplateMap(iData, out templateFromDungeonMap);

                if (Debug.isDebugBuild) Debug.Log("Found valid template map");
                // This data pack may have been altered by the function above
                fallbackLevelData = iData.fallbackLevelData;
                specialFloorIDsUsed = iData.specialFloorIDsUsed;


                // Sets the tile visual set for the localMap
                ItemDreamFunctions.SetTileVisualSet(iData);

                // For map generation, we must be aware that this map is a template from something else.
                // Don't spawn bosses, triggers, or other weird stuff.
                int genValue = templateFromDungeonMap ? 1 : 0;
                GameMasterScript.gmsSingleton.SetTempGameData("dream_map_from_template", genValue);

                itemWorld[i] = CreateNewMap(false, 400 + i, numFloors, localCV, localMap, meta);

                if (itemWorld[i] != null)
                {
                    validMapGenerated = true;
                }
                else
                {
                    // Something failed in map generated. Change parameters.
                    if (Debug.isDebugBuild) Debug.Log("Failed creating floor " + i + " nightmare? " + creatingNightmareWorld + " localmap? " + localMap.floor + " forced level type? " + forcedLevelType);
                    attemptsToGenerateIWMap++;
                    if (attemptsToGenerateIWMap > 20)
                    {
                        if (Debug.isDebugBuild) Debug.Log("Serious failure occurred in IW map generation despite many attempts.");
                        break;
                    }
                    continue;
                }
            }


            itemWorld[i].levelDataLink = localMap.floor;
            //Debug.Log("OF NUM FLOORS " + numFloors + " Created item world map CV " + localCV + " index " + i + " of " + numFloors + " Cols,Rows: " + itemWorld[i].columns + "," + itemWorld[i].rows + " size: " + localMap.size + " " + localMap.floor + " " + localMap.challengeValue + " " + localMap.layoutType);

            iData.itemWorld = itemWorld;
            iData.meta = meta;

            if (creatingNightmareWorld)
            {
                ItemDreamFunctions.PopulateNightmareFloor(iData, i);
            }

            iData.localBerserkFloorChance = localBerserkFloorChance;
            iData.localBrawlFloorChance = localBrawlFloorChance;
            iData.numFloors = numFloors;
            iData.localCoolFrogChance = localCoolFrogChance;
            iData.localCharmChance = localCharmChance;
            iData.localGoldfrogFloorChance = localGoldfrogFloorChance;
            iData.chanceFoodFloor = chanceFoodFloor;
            iData.chanceFountainFloor = chanceFountainFloor;

            ItemDreamFunctions.CheckForAndPopulateSpecialFloors(iData, i);
            ItemDreamFunctions.AddElementalMonsterStatusesFromDreamProperties(iData, i);

            theDungeon.maps.Add(itemWorld[i]);

            itemWorld[i].bonusRewards = meta.rewards;

            GameMasterScript.heroPCActor.SetActorData("iwbonus", (int)(meta.rewards * 100));

            itemWorld[i].RelocateMonstersFromStairs();
        }

        // Link up all the stairs, dog
        if (Debug.isDebugBuild) Debug.Log("Success! Finished generating item world.");

        int indexOfLastNormalFloor = numFloors - 1;
        if (addNightmareFloor)
        {
            indexOfLastNormalFloor = numFloors - 2;
        }

        for (int i = 0; i < numFloors; i++)
        {
            if (i == numFloors - 1 && addNightmareFloor)
            {
                workFloorIsNightmarePrinceFloor = true;
                itemWorld[i].challengeRating = itemWorld[i - 1].challengeRating;
            }
            else
            {
                workFloorIsNightmarePrinceFloor = false;
            }

            iData.workFloorIsNightmarePrinceFloor = workFloorIsNightmarePrinceFloor;

            if (i == 0)
            {
                //itemWorld[i].SpawnStairs(true); // These are already spawning through some other code.
                itemWorld[i].SpawnStairs(false);
                Stairs stRemove = null;
                foreach (Stairs st in itemWorld[i].mapStairs)
                {
                    if (st.stairsUp)
                    {
                        stRemove = st;
                    }
                    else
                    {
                        st.NewLocation = itemWorld[i + 1];
                        st.newLocationID = itemWorld[i + 1].mapAreaID;
                        itemWorld[i].GetTile(st.GetPos()).ChangeTileType(TileTypes.GROUND, itemWorld[i].mgd);
                    }
                }
                if (stRemove != null)
                {
                    itemWorld[i].RemoveActorFromMap(stRemove);
                    itemWorld[i].mapStairs.Remove(stRemove);
                }
            }
            else if (i == indexOfLastNormalFloor && creatingNightmareWorld)
            {
                // Don't spawn stairs in boss room.
                Stairs st = null;
                MapTileData tileForStairs = null;

                itemWorld[i].ClearAllCrystals();
                itemWorld[i].AddCrystalsToRoom(itemWorld[i].FindRoom("nightmareworldboss"));

                itemWorld[i].mapRooms.Shuffle();

                st = itemWorld[i].PlaceStairsInRoomTemplate("nightmareworldboss");

                st.NewLocation = itemWorld[i - 1];
                st.newLocationID = itemWorld[i - 1].mapAreaID;
                st.prefab = "NightmarishStairsDown";

                itemWorld[i].ClearStairsExceptRef(st);


            }
            else if (i == indexOfLastNormalFloor && !addNightmareFloor)
            {
                // Normally, this is the end of the dungeon.
                itemWorld[i].SpawnStairs(true);
                foreach (Stairs st in itemWorld[i].mapStairs)
                {
                    itemWorld[i].GetTile(st.GetPos()).ChangeTileType(TileTypes.GROUND, itemWorld[i].mgd);
                    if (st.stairsUp)
                    {
                        st.NewLocation = itemWorld[i - 1];
                        st.newLocationID = itemWorld[i - 1].mapAreaID;
                    }
                }
            }
            else if (addNightmareFloor && workFloorIsNightmarePrinceFloor)
            {
                // Don't spawn nightmare floor stairs in the boss room.
                Stairs st = new Stairs();
                MapTileData tileForStairs = null;

                itemWorld[i].ClearAllCrystals();
                itemWorld[i].AddCrystalsToRoom(itemWorld[i].FindRoom("shadowking"));

                itemWorld[i].mapRooms.Shuffle();

                st = itemWorld[i].PlaceStairsInRoomTemplate("shadowking");

                st.NewLocation = itemWorld[i - 1];
                st.newLocationID = itemWorld[i - 1].mapAreaID;
                st.prefab = "NightmarishStairsDown";
                st.stairsUp = true;

                itemWorld[i].ClearStairsExceptRef(st);
            }
            else
            {

                itemWorld[i].SpawnStairs(true);
                itemWorld[i].SpawnStairs(false);
                foreach (Stairs st in itemWorld[i].mapStairs)
                {
                    itemWorld[i].GetTile(st.GetPos()).ChangeTileType(TileTypes.GROUND, itemWorld[i].mgd);
                    if (st.stairsUp)
                    {
                        st.NewLocation = itemWorld[i - 1];
                        st.newLocationID = itemWorld[i - 1].mapAreaID;
                    }
                    else
                    {
                        st.NewLocation = itemWorld[i + 1];
                        st.newLocationID = itemWorld[i + 1].mapAreaID;
                    }
                }
            }
        }

        if (Debug.isDebugBuild) Debug.Log("Stairs linked.");
        ItemDreamFunctions.VerifyAllStairsAreAccessible(itemWorld, creatingNightmareWorld);

        Map finalMap = itemWorld[indexOfLastNormalFloor];

        //null item is usually not allowed but might be for debug work

        if (addNightmareFloor)
        {
            ItemDreamFunctions.CreateNightmarePrince(itemWorld);
        }

        ItemDreamFunctions.CreateItemBossIfNecessary(finalMap, creatingNightmareWorld, eq);

        //Debug.Log("Creating item world maps.");

        itemWorldMaps = itemWorld;
        itemWorldIDs = new int[itemWorld.Length];


        if (orbUsed == null)
        {
            Debug.Log("WARNING: Orb used is null, this should never happen.");
        }
        else
        {
            orbUsedToOpenItemWorld = orbUsed;
            GameMasterScript.heroPCActor.SetActorData("orbusedtoopenitemworld", orbUsedToOpenItemWorld.actorUniqueID);
        }
        itemWorldOpen = true;
        GameMasterScript.heroPCActor.SetActorData("item_dream_open", 1);
        itemWorldMagicChance = magicChance;

        // Make sure stairs are not surrounded by rock.
        for (int i = 0; i < itemWorld.Length; i++)
        {
            foreach (Stairs st in itemWorld[i].mapStairs)
            {
                itemWorld[i].StairSanityCheck(st.GetPos());
            }
        }

        return itemWorld;
    }
}
