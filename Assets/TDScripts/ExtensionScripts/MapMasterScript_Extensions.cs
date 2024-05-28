using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MapMasterScript
{
	/// <summary>
	/// Checks conditions and rolls dice to see if we should make a campfire map.
	/// If so, it builds a map.
	/// </summary>
	/// <param name="originDestinationMap">Where we would go after this campfire.</param>
	/// <param name="husynForceEncounter">If true, we will build a campfire, and it will have the Husyn unlock.</param>
	/// <returns>A campfire map if created, otherwise null</returns>
	Map TryCreateCampfireMap(bool husynForceEncounter)
	{
	    //A campfire is not valid if...
	    // * we're heading to an item world and we roll > 25 on d100 or
	    // * we are currently in the town.
        bool campfireValid = !(currentMapChangeInfo.destinationMap.IsItemWorld() && Random.Range(0, 1f) > 0.25f ||
                               activeMap.IsTownMap());

        // If we're in a mystery dungeon, it's always possible!
        if (currentMapChangeInfo.destinationMap.IsMysteryDungeonMap() && activeMap.IsMysteryDungeonMap())
        {
            campfireValid = true;
        }

	    //no way to get a campfire if the above is false;
	    if (!campfireValid)
	    {
	        return null;
	    }

	    var hero = GameMasterScript.heroPCActor;
	    
	    
        // Chance to spawn campfire area based on previous adventure.
	    float localCampfireChance = campfireFloorChance;
        localCampfireChance += 0.2f * (hero.daysPassed - 
                                       hero.ReadActorData("lastcampfireday") - 1);

        // Mystery Dungeons use slightly different logic, as time does not pass there.
        if (currentMapChangeInfo.destinationMap.IsMysteryDungeonMap())
        {
            int floorOfLastCampfire = hero.ReadActorData("floor_of_last_mdcampfire");
            if (floorOfLastCampfire < MapMasterScript.CUSTOMDUNGEON_START_FLOOR) floorOfLastCampfire = MapMasterScript.CUSTOMDUNGEON_START_FLOOR;            
            localCampfireChance = 0.25f * (currentMapChangeInfo.destinationMap.floor - floorOfLastCampfire);
        }

        // Shara mode is also different. Guaranteed every three floors.
        if (SharaModeStuff.IsSharaModeActive())
        {
            if (SharaModeStuff.TickCampfireCounterAndCheckForSpawn(currentMapChangeInfo))
            {
                localCampfireChance = 1f;
            }
            else
            {
                campfireFloorChance = 0f;
                campfireValid = false;
                return null;
            }
        }


	    //Now we spawn a campfire if we roll correctly OR 
		// * we need to have one because husyn OR
		// * we are going to make an itemworld one.
		campfireValid = Random.Range(0, 1f) <= localCampfireChance ||
		                husynForceEncounter;


        if (!campfireValid)
	    {
	        return null;
	    }
	    
	    //We're campalampin'!

        hero.SetActorData("destinationfloor", currentMapChangeInfo.destinationMap.mapAreaID);
        hero.SetActorData("precampfirefloor", activeMap.mapAreaID);
        Map restMap = theDungeon.FindFloor(CAMPFIRE_FLOOR);

        // Take NPCs we find that aren't fires, and remove them.
        List<Actor> removeActors = new List<Actor>();
        foreach (Actor act in restMap.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.NPC && act.actorRefName != "npc_restfire")
            {
                removeActors.Add(act);
            }
        }
        foreach(Actor act in removeActors)
        {
            restMap.RemoveActorFromLocation(act.GetPos(), act);
            restMap.RemoveActorFromMap(act);
        }

	    //Do we have a campfire? Maybe we need to make one.
	    bool foundCampfire = restMap.actorsInMap.Any(a => a.actorRefName == "npc_restfire");

	    if (!foundCampfire)
        {
            NPC makeNPC = new NPC();
            makeNPC.CopyFromTemplateRef("npc_restfire");
            makeNPC.dungeonFloor = restMap.floor;
            restMap.PlaceActor(makeNPC, restMap.mapArray[4, 5]);
        }

        //Allow us to fudge the numbers if we want to force things
	    //Lower rolls mean we'll get an NPC visiting us.
        float fDieRoll = Random.Range(0, 1f);
	    
	    //Use this value for debug if we want to force-test an NPC
        string debug_ForcedCampfireNPC = ""; // "npc_muguzmo";

        if (!string.IsNullOrEmpty(debug_ForcedCampfireNPC))
        {
            //yes campfire
            fDieRoll = 0.0f;
        }

        if (fDieRoll <= CHANCE_CAMPFIRE_NPC || husynForceEncounter || SharaModeStuff.IsSharaModeActive())
        {
            List<NPC> possibleCampfireNPCs = new List<NPC>();
            NPC forceEncounterNPC = null;
            if (husynForceEncounter)
            {
                forceEncounterNPC = GameMasterScript.masterNPCList["npc_campfire_husyn"];
            }

            if (!string.IsNullOrEmpty(debug_ForcedCampfireNPC))
            {
                //If this crashes it's because your forcedDebugCampfireNPC doesn't exist.
                forceEncounterNPC = GameMasterScript.masterNPCList.Where(kvp => 
                    kvp.Value.actorRefName.ToLower() == debug_ForcedCampfireNPC.ToLower()).ToList()[0].Value;
            }

            //Look at all the campfire NPCs we have
            if (forceEncounterNPC == null)
            {
                if (SharaModeStuff.IsSharaModeActive())
                {
                    // Shara only meets random merchants.
                    foreach(string nRef in SharaModeStuff.possibleCampfireNPCs)
                    {
                        possibleCampfireNPCs.Add(GameMasterScript.masterNPCList[nRef]);
                    }
                }
                else
                {
                    foreach (NPC n1 in GameMasterScript.masterCampfireNPCList)
                    {
                        var alreadySpawnedThisNPC = hero.ReadActorData(n1.actorRefName + "_in_campfire") == 1 ||
                                                    hero.ReadActorData(n1.actorRefName) == 10;

                        //Checking to see if the actorRefName == 10 is the old way of
                        //making sure we don't spawn an actor in multiple campfires. 
                        //I'm gonna clean that up here just so it's easier to understand what the keys mean 
                        //in the actorData if we poke at in debug.
                        if (!alreadySpawnedThisNPC)
                        {
                            //Don't allow a craggan_merchant if we (I assume) haven't rescued him yet
                            if (n1.actorRefName == "craggan_merchant" && hero.ReadActorData("craggan_mine_rescue") < 2)
                            {
                                continue;
                            }

                            //Don't allow goldfrogs if we're in an item dream
                            if (currentMapChangeInfo.destinationMap.IsItemWorld() && n1.actorRefName == "npc_goldfrog")
                            {
                                continue;
                            }

                            possibleCampfireNPCs.Add(n1);
                        }
                    }
                }

            }

            if (possibleCampfireNPCs.Count > 0 || forceEncounterNPC != null)
            {
                NPC n = forceEncounterNPC;
                if (forceEncounterNPC == null)
                {
                    n = possibleCampfireNPCs[Random.Range(0, possibleCampfireNPCs.Count)];
                }

                List<NPC> npcsToSpawn = new List<NPC>() { n };

                if (SharaModeStuff.IsSharaModeActive() && SharaModeStuff.CheckForWeaponMasterStatueAtCampfire())
                {
                    npcsToSpawn.Add(GameMasterScript.masterNPCList["npc_weaponmaster_statue"]);
                }

                foreach(NPC spawnTemplate in npcsToSpawn)
                {
                    NPC newNPC = new NPC();
                    newNPC.CopyFromTemplate(spawnTemplate);

                    int spawnX = 0; //Random.Range(2, restMap.columns - 1);
                    int spawnY = 0; //Random.Range(2, restMap.rows - 1);
                    MapTileData mtd = null; //restMap.mapArray[spawnX, spawnY];

                    //The husyn always spawns in a given position
                    if (husynForceEncounter)
                    {
                        spawnX = 5;
                        spawnY = 7;
                        mtd = restMap.mapArray[spawnX, spawnY];
                    }

                    //otherwise look for a good and nice tile we can use.
                    //this shouldn't take long 
                    while (mtd == null)
                    {
                        spawnX = Random.Range(2, restMap.columns - 1);
                        spawnY = Random.Range(2, restMap.rows - 1);
                        mtd = restMap.mapArray[spawnX, spawnY];

                        //A valid tile is a nice empty ground tile that isn't where the hero spawns
                        if (mtd.tileType != TileTypes.GROUND ||
                            mtd.pos == restMap.heroStartTile ||
                            mtd.GetAllActors().Count != 0)
                        {
                            mtd = null;
                        }
                    }

                    restMap.PlaceActor(newNPC, mtd);
                    newNPC.RefreshShopInventory(hero.lowestFloorExplored);
                    hero.SetActorData(newNPC.actorRefName + "_in_campfire", 1);
                }
            }
        }

	    //If there's no fountain, make a fountain!
        if (restMap.FindActor("obj_regenfountain") == null)
        {
            restMap.SpawnFountainInMap();
        }

	    //create some stairs up and down
        foreach (Stairs st in restMap.mapStairs)
        {
            if (!st.stairsUp)
            {
                st.NewLocation = currentMapChangeInfo.destinationMap;
                st.newLocationID = currentMapChangeInfo.destinationMap.mapAreaID;
            }
            else
            {
                st.NewLocation = activeMap;
                st.newLocationID = activeMap.mapAreaID;
            }
        }

        hero.SetActorData("lastcampfireday", hero.daysPassed);
        if (currentMapChangeInfo.destinationMap.IsMysteryDungeonMap())
        {
            hero.SetActorData("floor_of_last_mdcampfire", currentMapChangeInfo.destinationMap.floor);
        }
        if (GameMasterScript.GAME_BUILD_VERSION >= 106)
        {
            // If we're MountainGrass, use LushGreen. Otherwise, use the previous map's tile set.
            restMap.dungeonLevelData.tileVisualSet =
                activeMap.dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS ? TileSet.LUSHGREEN : activeMap.dungeonLevelData.tileVisualSet;

            restMap.RefreshTileVisualIndices();
        }


        //if (Debug.isDebugBuild) Debug.Log("There should be a campfire now.");

		//we did it! This will be our new destination when we catch it in the parent call.
		return restMap;
	}
}