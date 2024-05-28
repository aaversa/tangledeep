using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PreSummonFunctions
{
    public static List<Vector2> ChangeIceBlockByFaction(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        switch (sae.originatingActor.actorfaction)
        {
            case Faction.PLAYER:
                sae.summonActorRef = "obj_monstericeblock_friendly";
                break;
            default:
                sae.summonActorRef = "obj_monstericeblock";
                break;
        }

        return summonPositions;
    }

    public static List<Vector2> RandomizePosition(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        if (summonPositions.Count > 0)
        {
            Vector2 pos = summonPositions[0];
            summonPositions.Clear();

            int randomDir = UnityEngine.Random.Range(0, 8);
            pos += MapMasterScript.xDirections[randomDir];           
            
            if (UnityEngine.Random.Range(0,2) == 0)
            {
                randomDir = UnityEngine.Random.Range(0, 8);
                pos += MapMasterScript.xDirections[randomDir];
            }

            summonPositions.Add(pos);
        }

        return summonPositions;
    }

    public static List<Vector2> SetSludgeDeath2Tiles(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        // Summon in 5x5 square.
        summonPositions.Clear();

        Vector2 basePos = sae.originatingActor.GetPos();
        
        for (int x = (int)basePos.x-2; x < (int)basePos.x+2; x++)
        {
            for (int y = (int)basePos.y - 2; y < (int)basePos.y + 2; y++)
            {
                if (x <= 0 || y <= 0 || x >= MapMasterScript.activeMap.columns || y >= MapMasterScript.activeMap.rows) continue;
                Vector2 nPos = new Vector2(x, y);
                if (MapMasterScript.GetTile(nPos).tileType == TileTypes.GROUND)
                {
                    summonPositions.Add(nPos);
                }
            }
        }


        return summonPositions;
    }

    public static List<Vector2> SetHergonTiles(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        // Can summon only on corners relative to the hero.
        summonPositions.Clear();

        Vector2 seTile = GameMasterScript.heroPCActor.GetPos() + MapMasterScript.xDirections[(int)Directions.SOUTHWEST];
        Vector2 nwTile = GameMasterScript.heroPCActor.GetPos() + MapMasterScript.xDirections[(int)Directions.NORTHWEST];

        summonPositions.Add(seTile);
        summonPositions.Add(nwTile);

        return summonPositions;
    }

    public static List<Vector2> PickRandomAdjacentTileForCampfire(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        Monster user = sae.originatingActor as Monster;

        TDAnimationScripts.MakeActorJumpUpAndDown(user, 0.35f, 360f, true);

        UIManagerScript.PlayCursorSound("CookingSuccess");

        CustomAlgorithms.GetTilesAroundPoint(sae.originatingActor.GetPos(), 1, MapMasterScript.activeMap);
        float bestDistance = 99f;
        Vector2 best = Vector2.zero;
        Vector2 secondBest = Vector2.zero;
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType != TileTypes.GROUND) continue;

            // Targeting hero is best
            if (GameMasterScript.heroPCActor.GetPos() == CustomAlgorithms.tileBuffer[i].pos)
            {
                best = GameMasterScript.heroPCActor.GetPos();
                break;
            }

            // Targeting a friendly thing is pretty good
            foreach(Actor act in CustomAlgorithms.tileBuffer[i].GetAllActors())
            {
                if (act.GetActorType() == ActorTypes.MONSTER && act.actorfaction == Faction.PLAYER)
                {
                    best = CustomAlgorithms.tileBuffer[i].pos;
                }
            }

            float dist = MapMasterScript.GetGridDistance(CustomAlgorithms.tileBuffer[i].pos, GameMasterScript.heroPCActor.GetPos());
            if (dist < bestDistance)
            {
                secondBest = CustomAlgorithms.tileBuffer[i].pos;
                bestDistance = dist;
            }
        }

        if (best != Vector2.zero)
        {
            summonPositions.Add(best);
        }
        else
        {
            summonPositions.Add(secondBest);
        }

        return summonPositions;
    }

    public static List<Vector2> PickRandomTilesForForbiddance(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();
        int strokeCount = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("brushstroke_charge");
        GameMasterScript.heroPCActor.myStats.RemoveAllStatusByRef("brushstroke_charge");
        GameMasterScript.heroPCActor.SetActorData("last_brushstrokesused", strokeCount);
        sae.numRandomSummons = strokeCount;

        //CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 3, MapMasterScript.activeMap);

        List<Vector2> tilesToUse = new List<Vector2>();

        foreach(Vector2 v2 in sae.positions)
        {
            MapTileData mtd = MapMasterScript.activeMap.GetTile(v2);
            if (mtd.tileType == TileTypes.WALL) continue;
            if (v2 == GameMasterScript.heroPCActor.GetPos() && sae.numRandomSummons < 6) continue;
            if (mtd.HasActorByRef("exp_obj_forbiddance")) continue;
            tilesToUse.Add(v2);
        }

        /* for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.WALL) continue;
            tilesToUse.Add(CustomAlgorithms.tileBuffer[i].pos);
        } */

    
        for (int i = 0; i <= sae.numRandomSummons; i++)
        {
            int indexToUse = UnityEngine.Random.Range(0, tilesToUse.Count);
            Vector2 pos = tilesToUse[indexToUse];
            summonPositions.Add(pos);
            tilesToUse.Remove(pos);
            if (tilesToUse.Count == 0) break;
        }

        return summonPositions;
    }

    public static List<Vector2> SummonToRandomNearbyTile(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        bool foundTile = false;
        MapTileData tileToUse = null;
        while (!foundTile)
        {
            tileToUse = MapMasterScript.activeMap.GetRandomNonCollidableTile(GameMasterScript.heroPCActor.GetPos(), 3, true, false);
            if (CustomAlgorithms.CheckBresenhamsLOS(GameMasterScript.heroPCActor.GetPos(), tileToUse.pos, MapMasterScript.activeMap))
            {
                foundTile = true;
                break;
            }
        }

        summonPositions.Add(tileToUse.pos);

        return summonPositions;
    }

    public static List<Vector2> SpewSmokeOnOriginating(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        CombatManagerScript.GenerateSpecificEffectAnimation(sae.originatingActor.GetPos(), "SmokeSpewEffect", sae, true);

        return summonPositions;
    }

    public static List<Vector2> AddPreviousHeroLocationToPositions(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();
        summonPositions.Add(GameMasterScript.heroPCActor.previousPosition);

        return summonPositions;
    }

    public static List<Vector2> SummonInDirectionTowardAttacker(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        float defAngle = CombatManagerScript.GetAngleBetweenPoints(sae.originatingActor.GetPos(), CombatManagerScript.bufferedCombatData.attacker.GetPos());
        Directions summonDir = MapMasterScript.GetDirectionFromAngle(defAngle);
        Vector2 posOffset = MapMasterScript.xDirections[(int)summonDir];
        Vector2 summonPos = sae.originatingActor.GetPos() + posOffset;
        summonPositions.Add(summonPos);

        return summonPositions;
    }

    public static List<Vector2> SelectRandomMaterializedElement(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        Dictionary<string,bool> possibleObjects = new Dictionary<string, bool>() {
            {  "obj_ss_evokefire", true }, // ref, summon on collidable
            {  "obj_ss_evokeice", false },
            {  "obj_ss_evokeacid", true },
            {  "obj_ss_evokeshadow", true }
            };

        sae.summonActorRef = possibleObjects.ElementAt(UnityEngine.Random.Range(0, possibleObjects.Keys.Count)).Key;
        sae.summonOnCollidable = possibleObjects[sae.summonActorRef];

        return summonPositions;
    }

    public static List<Vector2> StoreWeaponIDUsedForAbility(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        Fighter ft = sae.originatingActor as Fighter;
        if (ft != null)
        {
            float offhandPower = ft.cachedBattleData.physicalWeaponOffhandDamage;
            offhandPower *= 0.25f;
            float weaponPower = ft.cachedBattleData.physicalWeaponDamage;

            float finalWeaponValue = weaponPower + offhandPower;

            sae.originatingActor.SetActorData("cached_ability_weaponpower", (int)finalWeaponValue);
        }
        
        return summonPositions;
    }

    public static List<Vector2> TargetAdjacentTiles(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();
        CustomAlgorithms.GetTilesAroundPoint(sae.originatingActor.GetPos(), 1, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            summonPositions.Add(CustomAlgorithms.tileBuffer[i].pos);
        }
        return summonPositions;
    }


    public static List<Vector2> CreatePhasmaRings(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
        {
            Vector2 checkPos = sae.originatingActor.GetPos() + MapMasterScript.xDirections[i];
            summonPositions.Remove(checkPos);
        }

        int startX = (int)sae.originatingActor.GetPos().x - 3;
        int endX = (int)sae.originatingActor.GetPos().x + 3;
        int startY = (int)sae.originatingActor.GetPos().y - 3;
        int endY = (int)sae.originatingActor.GetPos().y + 3;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (x == startX || x == endX || y == startY || y == endY)
                {
                    summonPositions.Remove(new Vector2(x, y));
                }
            }
        }

        return summonPositions;
    }

    public static List<Vector2> RemoveTilesAroundOriginating(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
        {
            Vector2 checkPos = sae.originatingActor.GetPos() + MapMasterScript.xDirections[i];
            summonPositions.Remove(checkPos);
        }

        return summonPositions;
    }

    public static List<Vector2> CreateTornadoPositionList(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        List<Vector2> tempPositionList = new List<Vector2>();

        int numTornadosAroundMonster = 6;
        int numTornadosAroundPlayer = 4;

        // More tornados if it's a harder champ.
        Monster mn = sae.originatingActor as Monster;
        if (mn == null) return summonPositions; // should never happen...
        if (mn.isChampion && mn.HasChampionMod("monmod_hurricane"))
        {
            numTornadosAroundMonster += 4;
            numTornadosAroundPlayer += 2;
        }

        CustomAlgorithms.GetTilesAroundPoint(sae.originatingActor.GetPos(), 5, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND)
            {
                tempPositionList.Add(CustomAlgorithms.tileBuffer[i].pos);
            }
        }
        tempPositionList.Shuffle();
        for (int i = 0; i < tempPositionList.Count; i++)
        {
            if (i == numTornadosAroundMonster-1) break;
            summonPositions.Add(tempPositionList[i]);
        }
        tempPositionList.Clear();

        CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 2, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType == TileTypes.GROUND)
            {
                tempPositionList.Add(CustomAlgorithms.tileBuffer[i].pos);
            }
        }

        tempPositionList.Shuffle();
        for (int i = 0; i < tempPositionList.Count; i++)
        {
            if (i == numTornadosAroundPlayer-1) break;
            // If monster can't see position, % chance that tornado will not spawn.
            //if (UnityEngine.Random.Range(0,1f) <= 0.4f && !CustomAlgorithms.CheckBresenhamsLOS(mn.GetPos(), tempPositionList[i], MapMasterScript.activeMap))
            if (!CustomAlgorithms.CheckBresenhamsLOS(mn.GetPos(), tempPositionList[i], MapMasterScript.activeMap))
            {
                continue;
            }

            summonPositions.Add(tempPositionList[i]);
        }

        return summonPositions;
    }

    public static List<Vector2> ReviveMonsterSpecialStuff(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        if (sae.parentAbility.refName == "skill_revivemonster_herge")
        {
            Vector2 newPos = Vector2.zero;
            if (summonPositions.Count > 0)
            {
                newPos = MapMasterScript.GetRandomEmptyTile(summonPositions[0], 1, true, true, false, true, true).pos;
            }
            else
            {
                newPos = MapMasterScript.GetRandomNonCollidableTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true).pos;                
            }
            summonPositions.Add(newPos);
            sae.summonActorPerTile = true;         
        }        

        return summonPositions;
    }

    

    public static List<Vector2> IceCrossSurroundTiles(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        for (int i = 0; i < sae.numRandomSummons; i++)
        {
            int x = UnityEngine.Random.Range(2, MapMasterScript.activeMap.columns - 2);
            int y = UnityEngine.Random.Range(2, MapMasterScript.activeMap.rows - 2);
            Vector2 newPos = new Vector2(x, y);
            while (summonPositions.Contains(newPos) || MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(),newPos) <= 1)
            {
                x = UnityEngine.Random.Range(2, MapMasterScript.activeMap.columns - 2);
                y = UnityEngine.Random.Range(2, MapMasterScript.activeMap.rows - 2);
                newPos = new Vector2(x, y);
            }
            summonPositions.Add(newPos);
        }

        List<Vector2> positionsToAdd = new List<Vector2>();
        foreach (Vector2 v2 in summonPositions)
        {
            Vector2 basePosition = v2;
            for (int i = 0; i < MapMasterScript.directions.Length; i++)
            {
                Vector2 newPos = basePosition + MapMasterScript.directions[i];
                positionsToAdd.Add(newPos);
            }
        }

        // Add prison around player also

        int playerX = (int)GameMasterScript.heroPCActor.GetPos().x;
        int playerY = (int)GameMasterScript.heroPCActor.GetPos().y;
        int minX = playerX - 4;
        int maxX = playerX + 4;
        int minY = playerY - 4;
        int maxY = playerY + 4;
        if (minX < 1) minX = 1;
        if (maxX > MapMasterScript.activeMap.columns - 2) maxX = MapMasterScript.activeMap.columns - 2;
        if (minY < 1) minY = 1;
        if (maxY > MapMasterScript.activeMap.rows - 2) maxY = MapMasterScript.activeMap.rows - 2;
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x == minX || x == maxX || y == minY || y == maxY)
                {
                    positionsToAdd.Add(new Vector2(x, y));
                    continue;
                }
                if (UnityEngine.Random.Range(0,1f) <= 0.25f)
                {
                    positionsToAdd.Add(new Vector2(x, y));
                }
            }
        }

        positionsToAdd = positionsToAdd.Distinct().ToList();

        foreach (Vector2 v2 in positionsToAdd)
        {
            MapTileData checkMTD = MapMasterScript.GetTile(v2);
            if (checkMTD.IsCollidable(GameMasterScript.genericMonster)) continue;
            summonPositions.Add(v2);            
        }

        return summonPositions;

    }

    public static List<Vector2> MortarFireSurroundTiles(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        List<Vector2> positionsToAdd = new List<Vector2>();
        foreach (Vector2 v2 in summonPositions)
        {
            Vector2 basePosition = v2;
            for (int i = 0; i < MapMasterScript.xDirections.Length; i++)
            {
                Vector2 newPos = basePosition + MapMasterScript.xDirections[i];
                positionsToAdd.Add(newPos);
            }
        }
        foreach (Vector2 v2 in positionsToAdd)
        {
            if (!summonPositions.Contains(v2))
            {
                summonPositions.Add(v2);
            }
        }

        return summonPositions;
    }

    public static List<Vector2> MeteorTileSelection(SummonActorEffect sae, List<Vector2> summonPositions)
    {
        summonPositions.Clear();

        int centerX = (int)sae.originatingActor.GetPos().x;
        int centerY = (int)sae.originatingActor.GetPos().y;

        Vector2 checkVector = Vector2.zero;

        for (int i = 0; i < sae.numRandomSummons; i++)
        {
            int posX = UnityEngine.Random.Range(centerX - 2, centerX + 3);
            int posY = UnityEngine.Random.Range(centerY - 2, centerY + 3);
            MapTileData checkTile = MapMasterScript.activeMap.mapArray[posX, posY];
            checkVector.x = posX;
            checkVector.y = posY;
            while (!MapMasterScript.InBounds(checkVector) || checkTile.tileType == TileTypes.WALL || 
                summonPositions.Contains(checkTile.pos))
            {
                posX = UnityEngine.Random.Range(centerX - 2, centerX + 3);
                posY = UnityEngine.Random.Range(centerY - 2, centerY + 3);
                checkVector.x = posX;
                checkVector.y = posY;
                if (MapMasterScript.InBounds(checkVector))
                {
                    checkTile = MapMasterScript.activeMap.mapArray[posX, posY];
                }
                
            }
            summonPositions.Add(checkTile.pos);
        }

        int numAdditionalTargets = 2;

        List<Actor> possibleAdditionalTargets = new List<Actor>();

        if (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), sae.originatingActor.GetPos()) <= 4)
        {
            possibleAdditionalTargets.Add(GameMasterScript.heroPCActor);
        }

        foreach(AggroData ad in GameMasterScript.heroPCActor.combatAllies)
        {
            if (ad.combatant == null) continue;
            if (!possibleAdditionalTargets.Contains(ad.combatant))
            {
                if (MapMasterScript.GetGridDistance(ad.combatant.GetPos(), sae.originatingActor.GetPos()) <= 4)
                {
                    possibleAdditionalTargets.Add(ad.combatant);
                }
            }
        }

        possibleAdditionalTargets.Shuffle();

        int targetCount = possibleAdditionalTargets.Count;
        for (int i = 0; i < numAdditionalTargets; i++)
        {
            if (i >= targetCount) break;
            summonPositions.Add(possibleAdditionalTargets[i].GetPos());
        }

        // Now we have all meteor targets. Mark these tiles as being relevant for the bigass meteor animation before
        // surrounding them with other stuff. This data will be put into the summoned actor(s).
        int counter = 0;
        foreach(Vector2 pos in summonPositions)
        {
            GameMasterScript.gmsSingleton.SetTempFloatData("meteor" + counter + "x", pos.x);
            GameMasterScript.gmsSingleton.SetTempFloatData("meteor" + counter + "y", pos.y);
            counter++;
        }
        GameMasterScript.gmsSingleton.SetTempGameData("num_meteors", summonPositions.Count);
        GameMasterScript.gmsSingleton.SetTempGameData("meteor_counter", 0);

        summonPositions = MortarFireSurroundTiles(sae, summonPositions);

        return summonPositions;
    }

}
