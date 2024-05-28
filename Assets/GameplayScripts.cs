using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shep 7 September 2017
//
// Let's use reflection to call specific scripts to execute unique and fiddly gameplay code that won't happen anywhere else.
//
// Examples:
//
// * You are wearing an item that prevents you from dying and then explodes.
// * You put on a hat that checks to see if there are any goldfrogs around and if so turns them to lead.
// * You put something in your backpack that hatches 200 turns later.
// * You use an item to cast Knights of the Round, creating three minutes of unskippable animations.
//
//
// Here's the psuedo code:
/*
 *  When an event happens, (such as situations that would cause a StatusTrigger, but it doesn't have to be)
 *      Check the objects involved to see if they have any scripts to fire for that event.
 *      If so, run the method and check the result.
 *      Sometimes the result matters, sometimes it does not. 
 * 
 * 
 * 
 */

//Note: The "GameplayScripts" class can be split into multiple scripts down the road if you like.
//You could have "ItemScripts" "CombatScripts" etc. Your call.
public static class GameplayScripts
{
    //Each StatusTrigger will have a certain type of parameter signature. You can modify these down the road, but when you do you must modify
    //all existing functions called by that StatusTrigger too.

    //ATTACKBLOCK: Called by the CombatManager when an attack has been blocked
    // atk: Fighter who started the attack
    // def: Fighter who blocked the attack
    // bPenetrate: Bool indicating penetration
    // Returns TRUE if the attack should continue as normal
    public static bool HeavyGuard_AttackBlock(Fighter atk, Fighter def, bool bPenetrate )
    {
        //Add wrath for blocking while under HeavyGuard if we are a cool hero
        if (def == GameMasterScript.heroPCActor)
        {
            def.myStats.AddStatusByRef("wrathcharge", def, 99);
        }

        //Drop 7 stamina from the block
        def.myStats.ChangeStat(StatTypes.STAMINA, -7f, StatDataTypes.CUR, true);
        return true;
    }

    public static bool AddVanishingDodgeHalfHealth(Fighter origActor)
    {
        if (!origActor.myStats.CheckHasStatusName("vanishing_lowhealthdodge"))
        {
            origActor.myStats.AddStatusByRef("vanishing_lowhealthdodge", origActor, 99);
            CombatManagerScript.GenerateSpecificEffectAnimation(origActor.GetPos(), "DustCloudLanding", null, false, 0f, true);
            CombatManagerScript.GenerateSpecificEffectAnimation(origActor.GetPos(), "FervirBuff", null, true);
        }
        return true;
    }

    public static bool AddEdgeThaneHalfHealthBuff(Fighter origActor)
    {
        if (!origActor.myStats.CheckHasStatusName("edgethane_survive50"))
        {
            origActor.myStats.AddStatusByRef("edgethane_survive50", origActor, 99);
        }
        return true;
    }

    public static bool DoesPlayerNeedHelpDueToLowResources()
    {
        if (GameMasterScript.heroPCActor.regenFlaskUses > 1) return false;
        if (GameMasterScript.heroPCActor.myStats.GetLevel() < 3 || GameMasterScript.heroPCActor.myStats.GetLevel() > 9) return false;
        if (GameMasterScript.gmsSingleton.gameMode != GameModes.ADVENTURE) return false;
        if (GameMasterScript.heroPCActor.ReadActorData("percy_timeshelped") >= 2) return false;
        int healingItemsInInventory = 0;
        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            Consumable c = itm as Consumable;
            if (c.isHealingFood)
            {
                healingItemsInInventory++;
            }
        }

        if (healingItemsInInventory < 6) return true;

        return false;
    }

    public static bool AddInstantAdaptation(Fighter origActor)
    {
        if (!origActor.myStats.CheckHasStatusName("status_instantadaptation"))
        {
            origActor.myStats.AddStatusByRef("status_instantadaptation", origActor, 9999);
            GameLogScript.LogWriteStringRef("log_finalboss_gainadaptation");
        }
        return true;
    }

    public static bool ResetDroneSwarmCooldown(Fighter origActor)
    {
        if (origActor == null) return true;
        AbilityScript swarm = origActor.myAbilities.GetAbilityByRef("skill_droneswarm");
        if (swarm == null) return true;
        if (swarm.GetCurCooldownTurns() < 985)
        {
            swarm.SetCurCooldownTurns(0);
        }
        
        return true;
    }

    public static bool EdgeThaneCooldownReset(Fighter origActor)
    {
        int lastTurnExec = GameMasterScript.heroPCActor.ReadActorData("thane_bonus3_executed_turn");
        if (lastTurnExec == -1) lastTurnExec = -999;

        if ((GameMasterScript.turnNumber - lastTurnExec) > CombatManagerScript.THANE_MASTERY3_COOLDOWN)
        {
            GameMasterScript.heroPCActor.myAbilities.ClearAllCooldowns();
            GameMasterScript.heroPCActor.SetActorData("thane_bonus3_executed_turn", GameMasterScript.turnNumber);
            GameLogScript.LogWriteStringRef("log_thane_survival3");
        }

        return true;
    }

    //Returns a TMS that correctly identifies tiles that are in line of effect from both the player
    //and her crystal if active.
    public static TargetingMeshScript PhotonCannonTargeting(TargetingMeshScript tms, Vector2 baseTile, Directions lineDir, AbilityScript abil)
    {
        tms.goodTiles.Clear();
        tms.badTiles.Clear();

        List<Vector2> vTargetingOrigins = new List<Vector2>();
        vTargetingOrigins.Add(baseTile);

        //find the crystal bro if we have one
        Actor act = GameMasterScript.heroPCActor.GetSummonByRef("mon_runiccrystal");
        if (act != null)
        {
            vTargetingOrigins.Add(act.GetPos());
        }

        //the range of the power in the ability is 2.
        //but when calculated in the UIManager_Targeting code,
        //a line is drawn where the center is 2 tiles away from the player,
        //and extends 2 tiles in both the line direction and the opposite.
        //
        //That is how abil.range == 2 creates a five tile long line.

        int abilRangeEqualsTwo = 5;

        //for each origin, draw a line, and then determine what we have line of effect to. 
        foreach (var vOrigin in vTargetingOrigins)
        {
            var listInLine = GetTilesInCardinalDirection(vOrigin, lineDir, abilRangeEqualsTwo);

            //march down the line in order. As soon as we hit one wall, it's over
            bool bHitAWall = false;
            foreach (var checkTile in listInLine)
            {
                //if a wall, or already a wall, we are a wall. Wall. Lol.
                if (bHitAWall || checkTile.BlocksLineOfEffect())
                {
                    bHitAWall = true;
                }

                if (bHitAWall)
                {
                    tms.badTiles.Add(checkTile.pos);
                }
                else
                {
                    tms.goodTiles.Add(checkTile.pos);
                }

            }
        }

        return tms;
    }

    //Doesn't collect tiles in an arbitrary line, must be one of the 8 directions.
    static List<MapTileData> GetTilesInCardinalDirection(Vector2 vOrigin, Directions dir, int iRange, bool bIncludeOrigin = false)
    {
        var listOfTiles = new List<MapTileData>();
        Vector2 vDir = MapMasterScript.xDirections[(int) dir];
        Vector2 vCheckLoc = vOrigin;
        if (!bIncludeOrigin)
        {
            vCheckLoc += vDir;
        }

        //march down the line picking up tiles as we go
        for (int t = 0; t < iRange; t++)
        {
            MapTileData getTile = MapMasterScript.GetTile(vCheckLoc);
            if (getTile == null)
            {
                break;
            }
            listOfTiles.Add(getTile);
            vCheckLoc += vDir;
        }

        return listOfTiles;
    }

    public static void TryFloramancerPlantGrowthAura()
    {
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_plantgrowthaura"))
        {
            if (UnityEngine.Random.Range(0, 1f) <= 0.065f)
            {
                // Get open tiles.
                GameMasterScript.pool_checkMTDs.Clear();
                GameMasterScript.pool_MTD.Clear();
                GameMasterScript.pool_checkMTDs = MapMasterScript.GetNonCollidableTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 3, GameMasterScript.heroPCActor);
                for (int x = 0; x < GameMasterScript.pool_checkMTDs.Count; x++)
                {
                    MapTileData check = GameMasterScript.pool_checkMTDs[x];
                    if (check.tileType != TileTypes.GROUND) continue;
                    {
                        if (check.CheckActorRef("obj_anchorvine"))
                        {
                            continue;
                        }
                        // 80% chance to ignore tiles that are not in LOS.
                        if (!GameMasterScript.heroPCActor.CheckIfTileIsVisibleInArray(check.pos) && UnityEngine.Random.Range(0,1f) >= 0.2f)
                        {
                            continue;
                        }
                        GameMasterScript.pool_MTD.Add(check);
                    }
                }
                if (GameMasterScript.pool_MTD.Count > 0)
                {
                    Vector2 spawnPoint = GameMasterScript.pool_MTD.GetRandomElement().pos;
                    Destructible template = Destructible.FindTemplate("obj_anchorvine");
                    Destructible summoned = GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, template, spawnPoint, 15, 0, silent: true);
                    summoned.turnsToDisappear = 15;
                    summoned.maxTurnsToDisappear = 15;
                }
            }
        }
    }

    public static void TryFloramancerPlantSynergy()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if (heroPCActor.myStats.CheckHasStatusName("status_plantsynergypassive"))
        {
            bool isPlantNearby = false;

            //List<MapTileData> nearbyTiles = MapMasterScript.GetTilesAroundPoint(heroPCActor.GetPos(), 1);
            CustomAlgorithms.GetTilesAroundPoint(heroPCActor.GetPos(), 1, MapMasterScript.activeMap);
            List<Actor> nearby = null;
            for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
            {
                if (isPlantNearby)
                {
                    break;
                }
                nearby = CustomAlgorithms.tileBuffer[i].GetAllTargetablePlusDestructibles();
                for (int x = 0; x < nearby.Count; x++)
                {
                    if (nearby[x].actorRefName == "mon_summonedlivingvine" || nearby[x].actorRefName == "mon_summonedbulllivingvine"
                        || nearby[x].actorRefName == "obj_anchorvine" || nearby[x].actorRefName == "mon_summonedlivingvine2")
                    {
                        isPlantNearby = true;
                        break;
                    }
                }
            }

            if (isPlantNearby)
            {
                if (!heroPCActor.myStats.CheckHasStatusName("status_absorbingnutrients"))
                {
                    StatusEffect se = GameMasterScript.FindStatusTemplateByName("status_absorbingnutrients");
                    se.maxDuration = 2;
                    se.curDuration = 2;
                    StatusEffect add = new StatusEffect();
                    add.CopyStatusFromTemplate(se);
                    heroPCActor.myStats.AddStatus(add, heroPCActor);
                }
            }
            else
            {
                heroPCActor.myStats.RemoveStatusByRef("status_absorbingnutrients");
            }
        }
    }

    public static void TryDetectWeaknessEffects()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        bool detectWeakness = heroPCActor.myStats.CheckHasStatusName("status_detectweakness");
        bool eyeShield = heroPCActor.myStats.CheckHasStatusName("eyeshield");
        if (detectWeakness || eyeShield)
        {
            List<Monster> mons = GameMasterScript.gmsSingleton.GetMonstersWithinSight();
            for (int i = 0; i < mons.Count; i++)
            {
                if (mons[i].actorfaction != Faction.ENEMY) continue;
                // Weakness mark check
                if (detectWeakness && UnityEngine.Random.Range(0, 1f) <= GameMasterScript.BRIGAND_DETECT_WEAKNESS_CHANCE)
                {
                    if ((!mons[i].CheckTarget(heroPCActor) || mons[i].myBehaviorState != BehaviorState.FIGHT) && !mons[i].myStats.CheckHasStatusName("status_detectedweakness"))
                    {
                        mons[i].AddStatusAtRandomAngle("status_detectedweakness");
                    }
                }
                if (eyeShield && UnityEngine.Random.Range(0, 1f) <= 0.02f) // Should be 5%? Or lower?
                {
                    if (!mons[i].myStats.CheckHasStatusName("status_detectedweakness2"))
                    {
                        mons[i].AddStatusAtRandomAngle("status_detectedweakness2");
                    }
                }
            }
        }
    }

    public static void TryRunicCrystalBoostEffects()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        bool hasRunic1 = heroPCActor.myAbilities.HasAbilityRef("skill_runiccrystal");
        bool hasRunic2 = heroPCActor.myAbilities.HasAbilityRef("skill_runiccrystal_2");

        if (hasRunic1 && !hasRunic2)
        {
            Actor runic = heroPCActor.GetSummonByRef("mon_runiccrystal");
            bool nearCrystal = false;
            if (runic != null)
            {
                int dist = MapMasterScript.GetGridDistance(heroPCActor.GetPos(), runic.GetPos());
                if (dist > 1)
                {
                    nearCrystal = false;
                }
                else
                {
                    if (!heroPCActor.myStats.CheckHasStatusName("status_runicboost"))
                    {
                        heroPCActor.myStats.AddStatusByRef("status_runicboost", heroPCActor, 0);
                    }
                    nearCrystal = true;
                }
            }
            if (!nearCrystal)
            {
                heroPCActor.myStats.RemoveStatusByRef("status_runicboost");
            }
        }
        else
        {
            heroPCActor.myStats.RemoveStatusByRef("status_runicboost");
        }
    }

    public static void CheckForSpellshiftSelfSealing(TurnData tData)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        if (tData.GetTurnType() == TurnTypes.ABILITY && tData.tAbilityToTry.spellshift)
        {
            if (heroPCActor.myStats.CheckHasStatusName("status_spellshiftpenetrate"))
            {
                StringManager.SetTag(0, heroPCActor.displayName);
                StatusEffect se = heroPCActor.myStats.AddStatusByRef("status_sealed", heroPCActor, 1, checkForImmunity:false);
                UIManagerScript.RefreshStatuses();
                if (se != null)
                {
                    StringManager.SetTag(1, se.abilityName);
                }                
            }
        }
    }
    /// <summary>
    /// Consumes the obj_oilslick in tile and creates an obj_oilslickfire in its place.
    /// </summary>
    /// <param name="mtd"></param>
    public static void ConsumeOilSlickAndCreateFire(MapTileData mtd)
    {        
        Actor oilSlick = mtd.GetActorRef("obj_oilslick");
        if (oilSlick == null) // Why would it ever be null? Who knows.
        {
            return;            
        }
        GameMasterScript.gmsSingleton.DestroyActor(oilSlick);
        GameMasterScript.SummonDestructible(GameMasterScript.heroPCActor, GameMasterScript.masterMapObjectDict["obj_oilslickfire"], mtd.pos, 6, 0.03f);      
    }
}
