using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public partial class SpecialEffectFunctions
{

	/// <summary>
	/// Utility function to grab the important members of an effect from the data passed in.
	/// </summary>
	/// <param name="effect">The effect, which has the caster stored as effect.destructibleOwnerEffect</param>
	/// <param name="actorsToProcess">A list which should not be empty</param>
	/// <param name="caster">Who is causing the effect</param>
	/// <param name="target">Who is receiving the effect</param>
	/// <returns></returns>
	public static bool GetCasterAndTarget(SpecialEffect effect, List<Actor> actorsToProcess, out Actor caster,
		out Actor target)
	{
		caster = null;
		target = null;
		
		//not sure how this could happen, but it will likely be caused by shipping the xpac
		//and installing it on hardware we have no access to
		if (actorsToProcess == null || actorsToProcess.Count == 0)
		{
			return false;
		}

		//the players involved 
		caster = effect.destructibleOwnerOfEffect;
		target = actorsToProcess[0];
		
		return caster != null && target != null;
	}

    /// <summary>
    /// Move the player to a new location because she stepped on a teleporter.
    /// </summary>
    /// <param name="effect">The teleport effect, from which we can garner what teleporter was stepped on.</param>
    /// <param name="actorsToProcess">probably the hero.</param>
    /// <returns></returns>
    public static EffectResultPayload TeleportHeroViaTeleporter(SpecialEffect effect, List<Actor> actorsToProcess)
	{        

		EffectResultPayload erp = new EffectResultPayload();

		Actor teleporterTile;
		Actor teleportee;

		if (!GetCasterAndTarget(effect, actorsToProcess, out teleporterTile, out teleportee))
		{
			return erp;
		}
		
		//only send the player
		if (teleportee != GameMasterScript.heroPCActor)
		{
			//Debug.Log("First actor in the teleport list is " + actorsToProcess[0].actorRefName);
			return erp;
		}
		
		//find out where the teleport is sending us
		Vector2 teleportDest = new Vector2(
			teleporterTile.ReadActorData("teleport_dest_x"),
			teleporterTile.ReadActorData("teleport_dest_y")
			);

        MapTileData mtd = MapMasterScript.GetTile(teleportDest);

        if (mtd.tileType != TileTypes.GROUND || mtd.CheckTag(LocationTags.HOLE) || mtd.IsCollidable(GameMasterScript.heroPCActor))
        {
            MapTileData betterMTD = MapMasterScript.activeMap.GetRandomEmptyTile(teleportDest, 1, true, true, true, false, true);
            teleportDest = betterMTD.pos;
        }

        //there we go!
        GameMasterScript.StartWatchedCoroutine(Cutscenes.GenericTeleportPlayer(teleportDest, 0.5f, 0.1f, effect.effectRefName));

		return erp;
	}

	/// <summary>
	/// Look to see if the player is within X tiles of me, and if so, explode and spawn a monster.
	/// </summary>
	/// <param name="effect"></param>
	/// <param name="actorsToProcess">The stasis tube monster</param>
	/// <returns></returns>
	public static EffectResultPayload PrototypeHusynStasisTick(SpecialEffect effect, List<Actor> actorsToProcess)
	{
		EffectResultPayload erp = new EffectResultPayload();

		if (actorsToProcess == null)
		{
			return erp;
		}
		
		//this is me
		var self = actorsToProcess[0];

		if (self == null)
		{
			return erp;
		}
		
		//my position
		var selfLoc = self.GetPos();
		
		//I fill a 2x2 space visually, even though my location and physical entity
		//only take up one tile. So my check radius has to be a little offset
		//
		// X : actual location
		// O : sprite occupied areas
		
		/*
		 *		......
		 *		......
		 *		..OO..
		 *		..XO..
		 *		......
		 *		......
		 * 
		 */
		
		//but wait! we may have set a different sense range on this prototype.
		//Assume 2 as the default, but perhaps there's more
		var senseRange = self.ReadActorData("sense_range");
		if (senseRange == -1)
		{
			senseRange = 2;
		}
		
		//now expand that range to fill a rectangle around the summon. The size is
		//range x 2 + 2, because the center is 2x2, and the range extends all around. See illustration above!
		//If senseRange is different, the math changes as well.

		var bottomLeftCornerOfArea = selfLoc - new Vector2(senseRange,senseRange);
		var fullRange = senseRange + senseRange + 1; // let's try 1 instead of 2 to make it more reasonable
		
		var heroLoc = GameMasterScript.heroPCActor.GetPos();

		if (heroLoc.x >= bottomLeftCornerOfArea.x &&
		    heroLoc.x <= bottomLeftCornerOfArea.x + fullRange - 1 &&
		    heroLoc.y >= bottomLeftCornerOfArea.y &&
		    heroLoc.y <= bottomLeftCornerOfArea.y + fullRange - 1
		)
		{
			//Make the object drop some slime parts, why not
			MonsterSpawnFunctions.AddRandomSlimeEggPartsToInventory(self.myInventory);
			MonsterSpawnFunctions.AddRandomSlimeEggPartsToInventory(self.myInventory);
			MonsterSpawnFunctions.AddRandomSlimeEggPartsToInventory(self.myInventory);
			
			LootGeneratorScript.TryGenerateLoot(self, selfLoc, true);
			
			//the hero is in range, so we explode and then spawn a monster
			GameMasterScript.WaitThenDestroyActorWithVFX(self, 0.0f, "BigExplosionEffect");
			
			//new robot, full of hate
			var currentMap = MapMasterScript.activeMap;
			var newMonLoc = selfLoc;
			var spawnTile = currentMap.GetTile(newMonLoc);
			
			newMonLoc.y += 1;
			newMonLoc.x += 1;

			Monster beastie = MonsterManagerScript.CreateMonster("mon_prototype_husyn", true, 
				true, true, 0f, false);

			//Call this a couple of times to add multiple champion mods.
			beastie.MakeChampion();
			beastie.MakeChampion();
			
			//get in there and kill!
			//currentMap.PlaceActor(beastie, currentMap.GetTile(newMonLoc));
			
			beastie.startAreaID = MapMasterScript.activeMap.CheckMTDArea(spawnTile);
            //beastie.SetSpawnPos(newMonLoc);
            //currentMap.AddActorToLocation(spawnTile.pos, beastie);
            //currentMap.AddActorToMap(beastie);
            currentMap.PlaceActor(beastie, spawnTile);
            currentMap.OnEnemyMonsterSpawned(currentMap, beastie, true);
            MapMasterScript.singletonMMS.SpawnMonster(beastie, true);					
		}

		return erp;
	}


}