using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;
using System.Linq;


public class SummonActorEffect : EffectScript
{
    public ActorTypes summonActorType;
    public string summonActorRef;
    public int summonDuration;
    public bool summonOnCollidable;
    public bool summonOnBreakables;
    public TargetActorType anchorType;
    public int anchorRange;
    public bool scaleWithLevel;
    public bool summonActorPerTile;
    public bool detachFromSummoner; // When TRUE, creature is spawned to summoner's faction but otherwise is not added as a "summon". It just exists on its own in the world.
    public bool uniqueSummon;
    public bool summonOnSummoner;
    public bool actOnlyWithSummoner;
    public bool dieWithSummoner;
    public bool hideCharmVisual;
    public bool summonNoStacking;
    
    /// <summary>
    /// This ONLY matters when summonNoStacking is true. If this var is false (default), previous summons of the same type will be cleared when the new object is created.
    /// </summary>
    public bool allowExistingSummons;

    public int maxRandomSummonRange;
    public bool expandRandomSummonRange;
    public int numRandomSummons;
    public bool createNewPositionListForRandomSummons;
    public List<string> destroySummons;
    public bool randomPositionsRequireLineOfSight;
    public bool doNotChangeFaction;
    public bool summonOnWalls;

    public string script_preSummon;
    public string script_postSummon;

    public List<string> possibleRefs;
    public List<MapTileData> possibleTiles;
    public List<Vector2> localSummonPositions;

    //Set this value to have a 0->this range delay in seconds before
    //the object draws in the world
    public float fMaxDelayBeforeSummon;

    public SummonActorEffect()
    {
        destroySummons = new List<string>();
        anchorRange = 0;
        anchorType = TargetActorType.ORIGINATING;
        summonActorType = ActorTypes.DESTRUCTIBLE;
        summonOnCollidable = false;
        summonNoStacking = false;
        allowExistingSummons = false;
        summonActorPerTile = false;
        script_preSummon = "";
        script_postSummon = "";
        possibleRefs = new List<string>();
        possibleTiles = new List<MapTileData>();
        localSummonPositions = new List<Vector2>();
        summonActorRef = "";
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        SummonActorEffect temp = template as SummonActorEffect;
        detachFromSummoner = temp.detachFromSummoner;
        numRandomSummons = temp.numRandomSummons;
        summonActorType = temp.summonActorType;
        summonActorRef = temp.summonActorRef;
        summonOnBreakables = temp.summonOnBreakables;
        summonDuration = temp.summonDuration;
        summonOnCollidable = temp.summonOnCollidable;
        summonOnSummoner = temp.summonOnSummoner;
        summonOnWalls = temp.summonOnWalls;
        script_preSummon = temp.script_preSummon;
        script_postSummon = temp.script_postSummon;
        anchorRange = temp.anchorRange;
        anchorType = temp.anchorType;
        scaleWithLevel = temp.scaleWithLevel;
        uniqueSummon = temp.uniqueSummon;
        actOnlyWithSummoner = temp.actOnlyWithSummoner;
        dieWithSummoner = temp.dieWithSummoner;
        allowExistingSummons = temp.allowExistingSummons;
        summonNoStacking = temp.summonNoStacking;
        maxRandomSummonRange = temp.maxRandomSummonRange;
        summonActorPerTile = temp.summonActorPerTile;
        hideCharmVisual = temp.hideCharmVisual;
        createNewPositionListForRandomSummons = temp.createNewPositionListForRandomSummons;
        destroySummons = temp.destroySummons;
        fMaxDelayBeforeSummon = temp.fMaxDelayBeforeSummon;
        expandRandomSummonRange = temp.expandRandomSummonRange;
        randomPositionsRequireLineOfSight = temp.randomPositionsRequireLineOfSight;
        doNotChangeFaction = temp.doNotChangeFaction;
        foreach(string pRef in temp.possibleRefs)
        {
            possibleRefs.Add(pRef);
        }
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        SummonActorEffect eff = compareEff as SummonActorEffect;

        if (numRandomSummons != eff.numRandomSummons) return false;
        if (summonActorType != eff.summonActorType) return false;
        if (summonActorRef != eff.summonActorRef) return false;
        if (summonActorPerTile != eff.summonActorPerTile) return false;
        if (anchorRange != eff.anchorRange) return false;
        if (anchorType != eff.anchorType) return false;
        if (uniqueSummon != eff.uniqueSummon) return false;
        if (summonNoStacking != eff.summonNoStacking) return false;
        if (maxRandomSummonRange != eff.maxRandomSummonRange) return false;
        if (hideCharmVisual != eff.hideCharmVisual) return false;
        if (dieWithSummoner != eff.dieWithSummoner) return false;
        if (summonOnCollidable != eff.summonOnCollidable) return false;
        if (summonDuration != eff.summonDuration) return false;
        if (summonOnWalls != eff.summonOnWalls) return false;
        if (summonOnSummoner != eff.summonOnSummoner) return false;
        if (scaleWithLevel != eff.scaleWithLevel) return false;
        if (actOnlyWithSummoner != eff.actOnlyWithSummoner) return false;

        return true;
    }


    public override float DoEffect(int indexOfEffect = 0)
    {
        //Debug.Log("Run " + effectRefName);
        affectedActors.Clear();
        results.Clear();

        if (!VerifyOriginatingActorIsFighterAndFix())
        {
#if UNITY_EDITOR
            Debug.Log("WARNING: Orig actor for " + effectName + " " + effectRefName + " is not a fighter or anything at all");
#endif
            return 0f;
        }        

        Fighter origFighter = originatingActor as Fighter;
        StatBlock origStats = origFighter.myStats;
        EquipmentBlock origEquipment = origFighter.myEquipment;

        Fighter summonDefaultActor = null;

        if (tActorType == TargetActorType.SELF)
        {
            summonDefaultActor = selfActor as Fighter;
            if (selfActor == null)
            {
                summonDefaultActor = origFighter;
            }
        }
        else
        {
            summonDefaultActor = origFighter;
        }

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            return 0.0f;
        }

        buildActorsToProcess.Clear();
        if (!EvaluateTriggerCondition(buildActorsToProcess))
        {
            return 0.0f;
        }

        for (int i = 0; i < destroySummons.Count; i++)
        {
            if (origFighter.CheckSummonRefs(destroySummons[i]))
            {
                origFighter.DestroyAllSummonsByRef(destroySummons[i]);
            }
        }

        if (parentAbility == null)
        {
            parentAbility = GameMasterScript.rangedWeaponAbilityDummy;
        }

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.

            Vector2 centerPos = centerPosition;
            if (centerSpriteOnOriginatingActor)
            {
                centerPos = originatingActor.GetPos();
            }

            GameObject summonSpriteObj = CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPos, this, originatingActor.GetObject());

            if (summonSpriteObj != null)
            {
                if (rotateAnimToTarget)
                {
                    Directions dirOfSummon = UIManagerScript.singletonUIMS.GetLineDir();
                    //Debug.Log(dirOfSummon);

                    summonSpriteObj.transform.Rotate(new Vector3(0, 0, MapMasterScript.oppositeDirectionAngles[(int)dirOfSummon]), Space.Self);
                    // Reverse this?
                }
            }
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        // Summon the actor.

        localSummonPositions.Clear();

        // No position for some reason? use a random adjacent one.

        CheckForBuildTilesAroundPosition(origFighter);        

        BuildPositionListsForSpecialTargetActorTypes();
        CheckForArrowstorm(originatingActor);
        CheckForFungalSpores(origFighter);

        #region Create Position List
        if (positions == null || positions.Count == 0 || createNewPositionListForRandomSummons)
        {
            possibleTiles.Clear();

            if (maxRandomSummonRange == 0)
            {
                possibleTiles.Add(MapMasterScript.GetTile(summonDefaultActor.GetPos()));
            }
            else
            {
                possibleTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(summonDefaultActor.GetPos(), maxRandomSummonRange);
                //Debug.Log("MAX possible is " + possible.Count);
            }

            if (possibleTiles.Count == 0)
            {
                return 0.0f;
            }

            for (int x = 0; x < possibleTiles.Count; x++)
            {
                if (possibleTiles[x].tileType != TileTypes.GROUND && !summonOnWalls && effectName != "IceDaggers")
                {
                    continue;
                }
                if (!summonOnSummoner && possibleTiles[x].pos == summonDefaultActor.GetPos())
                {
                    continue;
                }
                if (!summonOnCollidable && possibleTiles[x].IsCollidable(summonDefaultActor))
                {
                    continue;
                }
                if (randomPositionsRequireLineOfSight && !CustomAlgorithms.CheckBresenhamsLOS(summonDefaultActor.GetPos(), possibleTiles[x].pos, MapMasterScript.activeMap))
                {
                    continue;
                }
                if (!summonOnBreakables)
                {
                    if (possibleTiles[x].HasBreakableCollidable(originatingActor))
                    {
                        continue;
                    }
                }

                /* 
                if (summonNoStacking && maxRandomSummonRange > 0 && !allowExistingSummons)
                {
                    //remove existing actors with the same ref
                    foreach (Actor checkDup in possible[x].GetAllActors())
                    {
                        if (checkDup.actorRefName == summonActorRef)
                        {
                            checkDup.RemoveImmediately();
                            GameMasterScript.AddToDeadQueue(checkDup);
                        }
                    }

                    //remove all instances from the physical tile
                    possible[x].RemoveAllActorsOfRef(summonActorRef);
                }
                 */
                positions.Add(possibleTiles[x].pos);
            }

            if (positions.Count == 0 && expandRandomSummonRange && maxRandomSummonRange > 0)
            {
                int searchRange = maxRandomSummonRange + 1;
                positions = CustomAlgorithms.ConvertMTDListToVector2(MapMasterScript.GetNonCollidableTilesAroundPoint(summonDefaultActor.GetPos(), searchRange, GameMasterScript.heroPCActor));
            }

            // is a script handling the positions?
            bool specialPositionHandling = !string.IsNullOrEmpty(script_preSummon);

            if (positions.Count == 0 && !specialPositionHandling)
            {
                if (summonOnSummoner)
                {
                    positions.Add(summonDefaultActor.GetPos());
                    //Debug.Log("No positions, so...");
                }
                else
                {
                    return 0.0f;
                }

            }
            else 
            {
                if (specialPositionHandling && positions.Count == 0)
                {
                    positions.Add(originatingActor.GetPos());
                }
                // SELF is used to originate the summons around the summonr or self actor.

                if (createNewPositionListForRandomSummons)
                {
                    positions.Clear();
                    List<MapTileData> allPossibleTiles = MapMasterScript.activeMap.GetListOfTilesAroundPoint(originatingActor.GetPos(), maxRandomSummonRange);
                    foreach (MapTileData mtd in allPossibleTiles)
                    {
                        if (mtd.tileType == TileTypes.GROUND)
                        {
                            positions.Add(mtd.pos);
                        }
                    }
                }

                if (tActorType != TargetActorType.SELF && numRandomSummons <= 1)
                {
                    Vector2 selected = positions[UnityEngine.Random.Range(0, positions.Count)];

                    // For Shadow stalk, place the summon in the direction(s) the player is facing
                    if (effectRefName.Contains("shadowclone"))
                    {
                        Vector2 best = Vector2.zero;
                        float shortestDistance = 999f;
                        Vector2 facingTile = originatingActor.GetPos() + MapMasterScript.xDirections[(int)originatingActor.lastMovedDirection];
                        foreach (Vector2 v2 in positions)
                        {
                            float distFromFacingTile = MapMasterScript.GetGridDistance(v2, facingTile);
                            if (distFromFacingTile < shortestDistance)
                            {
                                best = v2;
                                shortestDistance = distFromFacingTile;
                            }
                        }
                        selected = best;
                    }

                    Vector2 selected2 = Vector2.zero;

                    // SPECIAL HARDCODED CASE
                    if (effectRefName == "eff_champbombsummon" && positions.Count > 1 && origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.6f)
                    {
                        positions.Remove(selected);
                        selected2 = positions[UnityEngine.Random.Range(0, positions.Count)];
                    }

                    positions.Clear();
                    positions.Add(selected);

                    if (selected2 != Vector2.zero)
                    {
                        positions.Add(selected2);
                    }
                }
                else if (numRandomSummons > 1)
                {
                    //positions.Shuffle();                    
                    positions.RemoveAll(a => MapMasterScript.GetTile(a).IsCollidable(GameMasterScript.genericMonster));
                    int posCount = positions.Count;
                    while (posCount > numRandomSummons && posCount > 0)
                    {
                        positions.Remove(positions[UnityEngine.Random.Range(0,positions.Count)]);
                        posCount = positions.Count;
                    }
                }
            }
        }
        #endregion

        if (!summonActorPerTile)
        {
            if (positions.Count == 0)
            {
                positions.Add(originatingActor.GetPos());
            }
            localSummonPositions.Add(positions[0]);
        }
        else
        {
            foreach (Vector2 v2 in positions)
            {
                MapTileData checkTile = MapMasterScript.GetTile(v2);
                if (checkTile == null)
                {
                    continue;
                }

                if (summonNoStacking)
                {
                    foreach (Actor checkDup in checkTile.GetAllActors())
                    {
                        //Debug.Log("Check " + checkDup.actorRefName + " against " + summonActorRef);
                        if (checkDup.actorRefName == summonActorRef)
                        {
                            //Debug.Log("Remove Dis! " + checkDup.actorUniqueID);
                            checkDup.RemoveImmediately();
                            GameMasterScript.AddToDeadQueue(checkDup);
                        }
                    }

                    //remove all instances from the physical tile
                    checkTile.RemoveAllActorsOfRef(summonActorRef);
                }
                localSummonPositions.Add(v2);
                //Debug.Log("Adding " + v2 + " to possible summon list ");
            }
        }

        if (!summonOnSummoner)
        {
            localSummonPositions.Remove(originatingActor.GetPos());
        }

        int localSummonDuration = summonDuration;

        if (summonDuration > 0 && origFighter == GameMasterScript.heroPCActor)
        {
            int numConjurat = origFighter.myStats.CheckStatusQuantity("status_mmconjuration");
            if (numConjurat >= 1)
            {
                int extension = (int)(localSummonDuration * 0.2f * numConjurat);
                if (extension < 1) extension = 1;
                localSummonDuration += extension;
            }
            
        }

        bool displayed = false;

        if (!string.IsNullOrEmpty(script_preSummon))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(PreSummonFunctions), script_preSummon);
            object[] paramList = new object[2];
            paramList[0] = this;
            paramList[1] = localSummonPositions;
            object returnObj = runscript.Invoke(null, paramList);
            if (returnObj == null)
            {
                if (Debug.isDebugBuild) Debug.Log("No object for " + script_preSummon);
            }

            localSummonPositions = returnObj as List<Vector2>;
        }

        MethodInfo postSummonMethod = null;

        if (!string.IsNullOrEmpty(script_postSummon))
        {
            postSummonMethod = CustomAlgorithms.TryGetMethod(typeof(PostSummonFunctions), script_postSummon);
        }

        #region Terrain Summoning
        // HARDCODED - TERRAIN SUMMONING
        bool terrainSummon = false;
        LocationTags summonTag = LocationTags.COUNT;
        if (summonActorRef == "obj_mudtile")
        {
            summonTag = LocationTags.SUMMONEDMUD;
            terrainSummon = true;
        }
        else if (summonActorRef == "obj_electile")
        {
            summonTag = LocationTags.ELECTRIC;
            terrainSummon = true;
        }
        else if (summonActorRef == "obj_rivertile")
        {
            summonTag = LocationTags.WATER;
            terrainSummon = true;
        }
        else if (summonActorRef == "obj_lavatile" || summonActorRef == "obj_lavashieldtile")
        {
            summonTag = LocationTags.LAVA;
            terrainSummon = true;
        }
        else if (summonActorRef.Contains("phasmashieldtile"))
        {
            summonTag = LocationTags.LASER;
            terrainSummon = true;
        }

        if (terrainSummon)
        {
            //summonPositions.RemoveAll(a => (MapMasterScript.GetTile(a).AnyHazard()));
            localSummonPositions.RemoveAll(a => (MapMasterScript.GetTile(a).CheckActorRef("obj_phasmashieldtile")));
            localSummonPositions.RemoveAll(a => (MapMasterScript.GetTile(a).CheckActorRef("obj_weakerphasmashieldtile")));
            foreach (Vector2 sPos in localSummonPositions)
            {                
                MapMasterScript.GetTile(sPos).AddTag(summonTag);
            }
            foreach (Vector2 sPos in localSummonPositions)
            {
                MapMasterScript.activeMap.BeautifyTerrain(MapMasterScript.GetTile(sPos), summonTag, summonTag, summonTag);
            }
            // Now remove lava tags for lava shield tile, since it's not REAL lava...
            if (summonActorRef == "obj_lavashieldtile" || summonActorRef.Contains("phasmashieldtile"))
            {
                foreach (Vector2 sPos in localSummonPositions)
                {
                    MapMasterScript.GetTile(sPos).RemoveTag(summonTag);
                }
            }

        }
        // END HARDCODED
        #endregion



        // #todo - Clean up a lot of the code above...


        int index = -1;

        // Presummon function WAS here

        // Extra sanity check
        if (!summonOnCollidable)
        {
            localSummonPositions.RemoveAll(a => MapMasterScript.GetTile(a).IsCollidable(GameMasterScript.heroPCActor));
        }

        bool anyRandomRefs = possibleRefs.Count > 0;

        if (summonNoStacking && maxRandomSummonRange > 0 && !allowExistingSummons)
        {
            foreach (Vector2 summonPosition in localSummonPositions)
            {
                //remove existing actors with the same ref
                MapTileData checkMTD = MapMasterScript.GetTile(summonPosition);
                foreach (Actor checkDup in checkMTD.GetAllActors())
                {
                    if (checkDup.actorRefName == summonActorRef)
                    {
                        checkDup.RemoveImmediately();
                        GameMasterScript.AddToDeadQueue(checkDup);
                    }
                }

                //remove all instances from the physical tile
                checkMTD.RemoveAllActorsOfRef(summonActorRef);
            }
        }


        foreach (Vector2 summonPosition in localSummonPositions)
        {
            if (anyRandomRefs)
            {
                summonActorRef = possibleRefs.GetRandomElement();
            }

            index++;
            MapTileData mtd = MapMasterScript.GetTile(summonPosition);

            bool collidableForMonster = mtd.IsCollidable(GameMasterScript.genericMonster);

            if (collidableForMonster && !summonOnCollidable && !summonActorRef.Contains("iceshard") && !summonOnWalls) // Was just IsCollidable... now IsUnbreakableCollidable?
            {

            }
            else
            {
                if (mtd.HasBreakableCollidable(originatingActor))
                {
                }
                if (uniqueSummon)
                {
                    if (origFighter.CheckSummonRefs(summonActorRef))
                    {
                        GameMasterScript.gmsSingleton.DestroyActor(origFighter.GetSummonByRef(summonActorRef));
                    }
                    if (summonActorRef == "mon_summonedbulllivingvine")
                    {
                        if (origFighter.CheckSummonRefs("mon_summonedbulllivingvine"))
                        {
                            GameMasterScript.gmsSingleton.DestroyActor(origFighter.GetSummonByRef("mon_summonedbulllivingvine"));
                        }
                    }
                }

                Actor summonedActor = null;

                bool reverbateMonsterAbility = false;

                switch (summonActorType)
                {
                    case ActorTypes.DESTRUCTIBLE:
                        Destructible template = Destructible.FindTemplate(summonActorRef);
                        if (template == null)
                        {
                            Debug.Log("Summon object " + summonActorRef + " not found.");
                        }
                        else
                        {
                            if (template.movementType == Spread.NOSPREAD && mtd.CheckForSpecialMapObjectType(SpecialMapObject.BLOCKER))
                            {
                                // if it's a non-moving destructible, don't summon on holes
                                continue;
                            }

                            bool empowerHammer = false;
                            if (summonActorRef == "obj_blessedhammer" && originatingActor.GetActorType() == ActorTypes.HERO)
                            {
                                if (GameMasterScript.heroPCActor.myStats.CheckHasActiveStatusName("status_radiantaura"))
                                {
                                    if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("wrathcharge"))
                                    {
                                        GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("wrathcharge");
                                        GameLogScript.LogWriteStringRef("log_combat_empowerhammer");
                                        //summoned.SetActorData("empowerhammer", 1);
                                        GameMasterScript.gmsSingleton.SetTempGameData("empowerhammer", 1);
                                        empowerHammer = true;
                                    }
                                }
                            }

                            Destructible summoned = GameMasterScript.SummonDestructible(origFighter, template, summonPosition, localSummonDuration, UnityEngine.Random.value * fMaxDelayBeforeSummon);

                            // player-faction creatures should not have permanent summons
                            if (origFighter.actorfaction == Faction.PLAYER && origFighter.GetActorType() != ActorTypes.HERO && (localSummonDuration == 0 || localSummonDuration > 50))
                            {
                                summoned.turnsToDisappear = 50;
                                summoned.maxTurnsToDisappear = 50;
                            }

                            //Debug.Log("Summoned " + template.actorRefName + " with " + localSummonDuration + " " + summoned.maxTurnsToDisappear + " " + summoned.turnsToDisappear);

                            GameMasterScript.gmsSingleton.SetTempGameData("empowerhammer", 0);

                            Directions moveDir = MapMasterScript.oppositeDirections[(int)MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(summoned.GetPos(), origFighter.GetPos()))];
                            summoned.lastMovedDirection = moveDir;

                            //If this value is true, we're spawning a number of destructibles in a burst, not all at the same time,
                            //so let's set their animations to have some offset.
                            if (fMaxDelayBeforeSummon > 0f)
                            {
                                Animatable a = summoned.GetObject().GetComponent<Animatable>();
                                if (a != null)
                                {
                                    a.AdjustAnimTiming(UnityEngine.Random.value);
                                }
                            }

                            if (effectRefName == "summonicetortoise_random" || effectRefName == "summonicetortoise" 
                                || effectRefName == "summonicetortoise2" || effectRefName.Contains("icedaggers")) // Special case for ice shield
                            {
                                switch (index)
                                {
                                    case 0:
                                        summoned.lastMovedDirection = Directions.NORTH;
                                        break;
                                    case 1:
                                        summoned.lastMovedDirection = Directions.EAST;
                                        break;
                                    case 2:
                                        summoned.lastMovedDirection = Directions.WEST;
                                        break;
                                    case 3:
                                        summoned.lastMovedDirection = Directions.SOUTH;
                                        break;
                                }
                            }
                            else if (effectRefName == "eff_laserfield")
                            {
                                if (parentAbility.refName == "skill_laserfield")
                                {
                                    summoned.lastMovedDirection = Directions.SOUTH;
                                }
                                else
                                {
                                    summoned.lastMovedDirection = Directions.EAST;
                                }
                            }
                            else if (effectRefName == "directionallaser")
                            {
                                summoned.lastMovedDirection = MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(originatingActor.GetPos(), GameMasterScript.heroPCActor.GetPos()));
                                summoned.rotateToMoveDirection = true;
                            }

                            if (empowerHammer)
                            {
                                summoned.SetActorData("empowerhammer", 1);
                            }

                            if (summonActorRef == "obj_shadowblade")
                            {
                                int targetOfCaster = originatingActor.ReadActorData("targetofbladesid");
                                Vector2 centerPos = Vector2.zero;
                                if (targetOfCaster != -1)
                                {
                                    centerPos = GameMasterScript.gmsSingleton.TryLinkActorFromDict(targetOfCaster).GetPos();
                                }
                                else
                                {
                                    centerPos = GameMasterScript.heroPCActor.GetPos();
                                }
                                
                                Directions dirFromCenter = MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(centerPos, summoned.GetPos()));
                                summoned.lastMovedDirection = MapMasterScript.oppositeDirections[(int)dirFromCenter];
                            }

                            if (summoned.rotateToMoveDirection)
                            {
                                summoned.myMovable.UpdateObjectRotation(0f, summoned.lastMovedDirection);
                            }

                            if (!doNotChangeFaction)
                            {
                                summoned.actorfaction = origFighter.actorfaction;
                            }
                            
                            summoned.actOnlyWithSummoner = actOnlyWithSummoner; // Tie this into SummonDestructible?
                            summoned.anchorRange = anchorRange;
                            summoned.dieWithSummoner = dieWithSummoner;
                            switch (anchorType)
                            {
                                case TargetActorType.ORIGINATING:
                                    summoned.anchor = originatingActor;
                                    summoned.anchorID = originatingActor.actorUniqueID;
                                    break;
                            }

                            if (!displayed && !silent)
                            {
                                // Don't ever do this?
                                displayed = true;
                            }


                            summoned.myMovable.SetInSightAndSnapEnable(true);
                            
                            if (summoned.showDirection)
                            {
                                summoned.ShowDirection(true);
                            }

                            // For monster danger squares, special case.
                            if (Monster.lockedTargetForDangerTiles != null)
                            {
                                if (Monster.lockedTargetForDangerTiles.objectSet)
                                {
                                    //summoned.myMovable.SetPosition(Monster.lockedTargetForDangerTiles.GetPos());                                    
                                    summoned.GetObject().transform.SetParent(Monster.lockedTargetForDangerTiles.GetObject().transform);
                                    summoned.GetObject().transform.localPosition = Vector3.zero;
                                    Monster.lockedTargetForDangerTiles = null;
                                }

                            }

                            if (dieWithSummoner)
                            {
                                summoned.dieWithSummoner = true;
                            }

                            GameEventsAndTriggers.CheckForFloodedTemple2FQuest(summoned, summonPosition);
                            if (origFighter.GetActorType() == ActorTypes.HERO && parentAbility != null && !string.IsNullOrEmpty(parentAbility.refName) && !summoned.excludeFromHotbarCheck)
                            {
                                summoned.SetActorDataString("player_abil_summonref", parentAbility.refName);
                            }

                            summonedActor = summoned;
                        }

                        break;
                    case ActorTypes.MONSTER:
                        MonsterTemplateData monTemplate = null;

                        bool forceFactionToSummoner = false;
                        bool copyOfSelf = false;

                        if (effectRefName == "eff_rjrandommonster")
                        {
                            summonActorRef = RandomJobMode.GetRandomMonsterTableForSummoningBasedOnPlayerLevel();
                        }

                        // HARDCODED: Random summoning!
                        string localRef = summonActorRef;
                        if (summonActorRef == "random1")
                        {
                            localRef = DungeonLevel.GetSpecificLevelData(1).spawnTable.GetRandomActorRef();
                            while (localRef == "mon_toxicurchin" || localRef == "mon_bigurchin")
                            {
                                localRef = DungeonLevel.GetSpecificLevelData(1).spawnTable.GetRandomActorRef();
                            }
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "random2")
                        {
                            localRef = DungeonLevel.GetSpecificLevelData(3).spawnTable.GetRandomActorRef();
                            while (localRef == "mon_toxicurchin" || localRef == "mon_bigurchin")
                            {
                                localRef = DungeonLevel.GetSpecificLevelData(3).spawnTable.GetRandomActorRef();
                            }
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "random3")
                        {
                            localRef = DungeonLevel.GetSpecificLevelData(11).spawnTable.GetRandomActorRef();
                            while (localRef == "mon_plunderer")
                            {
                                localRef = DungeonLevel.GetSpecificLevelData(11).spawnTable.GetRandomActorRef();
                            }
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "elemspirit")
                        {
                            ObjectFlags preferredFlag = ObjectFlags.FIRE;
                            int highest = 0;
                            MapTileData cmtd = MapMasterScript.GetTile(summonPosition);

                            for (int i = 0; i < (int)ObjectFlags.COUNT; i++)
                            {
                                if (cmtd.GetObjectFlagAmount((ObjectFlags)i) > 0)
                                {
                                    preferredFlag = (ObjectFlags)i;
                                    highest = cmtd.GetObjectFlagAmount((ObjectFlags)i);
                                }
                            }
                            Destructible dt = cmtd.GetActorRef("obj_dangersquare") as Destructible;
                            if (dt != null && dt.summoner != null && dt.summoner.GetActorType() == ActorTypes.MONSTER)
                            {
                                Monster mn = dt.summoner as Monster;
                                if (mn.storeTurnData != null && mn.storeTurnData.tAbilityToTry != null)
                                {
                                    DamageEffect de = mn.storeTurnData.tAbilityToTry.TryGetEffectOfType(EffectType.DAMAGE) as DamageEffect;
                                    if (de != null)
                                    {
                                        if (de.damType != DamageTypes.PHYSICAL)
                                        {
                                            switch(de.damType)
                                            {
                                                case DamageTypes.FIRE:
                                                    preferredFlag = ObjectFlags.FIRE;
                                                    break;
                                                case DamageTypes.SHADOW:
                                                    preferredFlag = ObjectFlags.SHADOW;
                                                    break;
                                                case DamageTypes.POISON:
                                                    preferredFlag = ObjectFlags.POISON;
                                                    break;
                                                case DamageTypes.WATER:
                                                    preferredFlag = ObjectFlags.WATER;
                                                    break;
                                                case DamageTypes.LIGHTNING:
                                                    preferredFlag = ObjectFlags.LIGHTNING;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            switch (preferredFlag)
                            {
                                case ObjectFlags.FIRE:
                                    localRef = "mon_friendlyfirespirit";
                                    break;
                                case ObjectFlags.POISON:
                                    localRef = "mon_friendlymudspirit";
                                    break;
                                case ObjectFlags.SHADOW:
                                    localRef = "mon_soulshade";
                                    break;
                                case ObjectFlags.LIGHTNING:
                                    localRef = "mon_friendlythunderspirit";
                                    break;
                                case ObjectFlags.WATER:
                                default:
                                    localRef = "mon_friendlywaterspirit";
                                    break;
                            }
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "rando_frogs")
                        {
                            if (UnityEngine.Random.Range(0, 1f) <= 0.35f)
                            {
                                continue;
                            }
                            ActorTable possibleMons = new ActorTable();
                            possibleMons.AddToTable("mon_fungaltoad", 50);
                            possibleMons.AddToTable("mon_swamptoad", 50);
                            possibleMons.AddToTable("mon_goldfrog", 1);
                            possibleMons.AddToTable("mon_darkfrog", 1);
                            localRef = possibleMons.GetRandomActorRef();
                            if (UnityEngine.Random.Range(0, 1f) <= 0.4f)
                            {
                                forceFactionToSummoner = false;
                            }
                            else
                            {
                                forceFactionToSummoner = true;
                            }
                        }
                        else if (effectTags[(int)EffectTags.EGGHATCH])
                        {
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "revive" || effectRefName == "revivemonster")
                        {
                            localRef = GameMasterScript.gmsSingleton.ReadTempStringData("revivemonster");
                            /* MonsterTemplateData checkRefMTD = GameMasterScript.masterMonsterList[localRef];
                            if (checkRefMTD.isBoss)
                            {

                            } */
                            reverbateMonsterAbility = true;
                            forceFactionToSummoner = true;
                        }
                        else if (summonActorRef == "copyofself")
                        {
                            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("magicmirrors"))
                            {
                                Item eqWithMod = GameMasterScript.heroPCActor.myEquipment.GetItemWithMagicMod("mm_magicmirrors");
                                if (eqWithMod != null)
                                {
                                    StringManager.SetTag(0, eqWithMod.displayName);
                                    GameLogScript.LogWriteStringRef("log_mirrors_seethrough");
                                }                                
                                return 0.0f;
                            }
                            if (originatingActor.GetActorType() == ActorTypes.MONSTER)
                            {
                                Monster mn = originatingActor as Monster;
                                localRef = mn.myTemplate.refName;
                                forceFactionToSummoner = true;
                                copyOfSelf = true;
                            }
                            else
                            {
                                Debug.Log("Only monsters can create self illusions... for now.");
                                return 0.0f;
                            }

                        }
                        else
                        {
                            monTemplate = MonsterManagerScript.GetTemplateByRef(localRef);
                        }


                        if (mtd == null)
                        {
                            Debug.Log("Summon monster " + localRef + " not found.");
                        }
                        else
                        {
                            bool isPlayer = false;
                            if (originatingActor.actorfaction == Faction.PLAYER) isPlayer = true;
                            Monster newMon = MonsterManagerScript.CreateMonster(localRef, false, false, false, 0f, 0f, isPlayer);

                            if (summonActorRef == "rando_frogs" && !forceFactionToSummoner)
                            {
                                newMon.actorfaction = Faction.ENEMY;
                            }

                            if (newMon == null)
                            {
                                Debug.Log("WARNING: Summon effect " + effectName + " " + effectRefName + " monster ref: " + localRef + " could not be found.");
                            }

                            newMon.startAreaID = MapMasterScript.activeMap.CheckMTDArea(mtd);
                            newMon.areaID = MapMasterScript.activeMap.CheckMTDArea(mtd);
                            newMon.SetCurPos(summonPosition);
                            newMon.SetSpawnPosXY((int)summonPosition.x, (int)summonPosition.y);

                            MapMasterScript.activeMap.AddActorToLocation(summonPosition, newMon);
                            MapMasterScript.activeMap.AddActorToMap(newMon);
                            MapMasterScript.singletonMMS.SpawnMonster(newMon);

                            displayed = true;

                            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_soulkeeperemblem_tier1_summonlength"))
                            {
                                localSummonDuration = (int)(localSummonDuration * 1.2f);
                            }

                            newMon.turnsToDisappear = localSummonDuration;
                            newMon.maxTurnsToDisappear = localSummonDuration;

                            int bonusTurns = GameMasterScript.gmsSingleton.ReadTempGameData("bonusturns");

                            if (bonusTurns > 0)
                            {
                                GameMasterScript.gmsSingleton.SetTempGameData("bonusturns", 0);
                                newMon.turnsToDisappear += bonusTurns;
                                newMon.maxTurnsToDisappear += bonusTurns;
                            }

                            newMon.anchorRange = anchorRange;

                            // SPECIAL CASE Hardcode

                            if (newMon.actorRefName == "mon_summonedlivingvine" || newMon.actorRefName == "mon_summonedbulllivingvine" || newMon.actorRefName == "mon_summonedlivingvine2" || newMon.actorRefName == "mon_plantturret")
                            {
                                AbilityScript thornSkin = origFighter.myAbilities.GetAbilityByRef("skill_thornedskin");
                                if (thornSkin != null)
                                {
                                    AbilityScript copyThorns = new AbilityScript();
                                    AbilityScript.CopyFromTemplate(copyThorns, thornSkin);
                                    newMon.myAbilities.AddNewAbility(copyThorns, true);
                                    StringManager.SetTag(0, newMon.displayName);
                                    GameLogScript.LogWriteStringRef("log_grow_thorns");
                                }
                            }

                            if (origFighter.combatTargets != null)
                            {
                                foreach (AggroData ad in origFighter.combatTargets)
                                {
                                    newMon.AddAggro(ad.combatant, ad.aggroAmount);
                                    ad.combatant.AddTarget(newMon);
                                    float curAggro = ad.combatant.GetTargetAggro(origFighter);
                                    if (newMon.actorRefName.Contains("shadowclone"))
                                    {
                                        curAggro *= 1.3f;
                                    }
                                    ad.combatant.SetAggro(newMon, curAggro + UnityEngine.Random.Range(10f, 20f));
                                }
                            }

                            foreach (AggroData ad in origFighter.combatAllies)
                            {
                                newMon.AddAlly(ad.combatant);
                            }

                            if (origFighter.GetActorType() == ActorTypes.MONSTER)
                            {
                                Monster mn = origFighter as Monster;
                                if (mn.myBehaviorState == BehaviorState.FIGHT)
                                {
                                    newMon.myBehaviorState = BehaviorState.FIGHT;
                                }
                            }

                            CheckForSpecialMonsterScaling(originatingActor, origFighter, newMon);

                            if (originatingActor == GameMasterScript.heroPCActor)
                            {
                                ScaleSummonedMonsterByDisciplineAndEmblems(origFighter, newMon);
                            }
                            bool showCharm = false;

                            if (origFighter == GameMasterScript.heroPCActor)
                            {
                                showCharm = true;
                                CheckForHeroPostSummonModifications(origFighter, newMon);

                                if (newMon.actorfaction == Faction.ENEMY && !forceFactionToSummoner)
                                {
                                    showCharm = false;
                                }
                            }
                            else if (originatingActor.actorfaction == Faction.PLAYER && originatingActor.GetActorType() == ActorTypes.MONSTER)
                            {
                                // Always show charm here right???
                                showCharm = true;
                                hideCharmVisual = false;
                            }

                            if (!hideCharmVisual && showCharm)
                            {
                                StatusEffect charm = GameMasterScript.FindStatusTemplateByName("charmvisual");
                                StatusEffect ncharm = new StatusEffect();
                                ncharm.CopyStatusFromTemplate(charm);
                                ncharm.curDuration = 1;
                                ncharm.maxDuration = 1;
                                newMon.myStats.AddStatus(ncharm, GameMasterScript.heroPCActor);
                            }

                            switch (anchorType)
                            {
                                case TargetActorType.ORIGINATING:
                                    newMon.anchor = originatingActor;
                                    newMon.anchorID = originatingActor.actorUniqueID;
                                    break;
                            }
                            if (newMon.actorfaction == Faction.MYFACTION || forceFactionToSummoner)
                            {
                                newMon.actorfaction = originatingActor.actorfaction;
                                newMon.bufferedFaction = originatingActor.actorfaction;
                                //Debug.Log(newMon.actorRefName + " " + newMon.actorfaction);
                            }
                            newMon.summonerID = originatingActorUniqueID;
                            Fighter ft = originatingActor as Fighter;
                            newMon.summoner = ft;
                            newMon.actOnlyWithSummoner = actOnlyWithSummoner;
                            newMon.dieWithSummoner = dieWithSummoner;

                            if (summonActorRef != "rando_frogs" && !detachFromSummoner)
                            {
                                ft.AddSummon(newMon);
                            }

                            newMon.myMovable.SetInSightAndSnapEnable(true);
                            //Debug.Log("Summoned " + newMon.actorRefName + " " + newMon.actorfaction + " "+ forceFactionToSummoner);

                            if (dieWithSummoner && !detachFromSummoner)
                            {
                                newMon.dieWithSummoner = true;
                            }

                            if (summonActorRef == "mon_runiccrystal")
                            {
                                newMon.surpressTraits = true;
                            }

                            if (parentAbility != null && origFighter.GetActorType() == ActorTypes.HERO)
                            {
                                newMon.SetActorDataString("player_abil_summonref", parentAbility.refName);
                            }

                            if (copyOfSelf)
                            {
                                // For monsters only right now. Copies have very little power and no abilities.
                                newMon.myStats.SetStat(StatTypes.HEALTH, ft.myStats.GetCurStat(StatTypes.HEALTH), StatDataTypes.CUR, false);
                                newMon.myStats.SetStat(StatTypes.HEALTH, ft.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX), StatDataTypes.MAX, false);
                                newMon.allDamageMultiplier = 0.1f;
                                newMon.allMitigationAddPercent = 3f;
                                newMon.myStats.SetLevel(ft.myStats.GetLevel());
                                newMon.SetBattleDataDirty();
                                newMon.surpressTraits = true;
                                newMon.myAbilities.RemoveAllAbilities();
                                newMon.monsterPowers.Clear();
                                newMon.displayName = ft.displayName;
                                newMon.SetActorData("illusion", 1);

                                if (ft.myStats.CheckHasStatusName("enemy_quest_target"))
                                {
                                    newMon.myStats.AddStatusByRef("enemy_quest_target", newMon, 99);
                                }

                                Monster mn = ft as Monster;
                                if (mn.isChampion)
                                {
                                    newMon.isChampion = true;
                                    newMon.championMods = new List<ChampionMod>();
                                    foreach (ChampionMod cm in mn.championMods)
                                    {
                                        newMon.championMods.Add(cm);
                                    }
                                    newMon.myStats.AddStatus(GameMasterScript.FindStatusTemplateByName("championmonster"), newMon);
                                }
                                if (mn.isEnraged)
                                {
                                    newMon.isEnraged = true;
                                }
                        

                                if (UnityEngine.Random.Range(0, 2) == 0)
                                {
                                    Vector2 summonPos = newMon.GetPos();
                                    Vector2 origFighterPos = ft.GetPos();
                                    //newMon.SetPos(origFighterPos);
                                    //origFighter.SetPos(summonPos);
                                    MapMasterScript.singletonMMS.MoveAndProcessActorNoPush(summonPos, origFighterPos, newMon);
                                    MapMasterScript.singletonMMS.MoveAndProcessActorNoPush(origFighterPos, summonPos, ft);
                                    newMon.myMovable.SetPosition(origFighterPos);
                                    ft.myMovable.SetPosition(summonPos);
                                }

                                newMon.xpMod = 0;
                                newMon.lootChance = 0;
                            }
                            if (summonActorRef == "mon_frostedjellyboss")
                            {
                                newMon.MakeChampion();
                            }

                            MapMasterScript.activeMap.OnEnemyMonsterSpawned(MapMasterScript.activeMap, newMon, false);

                            //ensure that copiesOfSelf don't spawn any loot

                            GameMasterScript.CheckForSummonDelayAndSpriteEffect(newMon, summonPosition, fMaxDelayBeforeSummon);
                            GameEventsAndTriggers.CheckForFloodedTemple2FQuest(newMon, summonPosition);
                            summonedActor = newMon;
                        }
                        break;
                }

                if (summonedActor != null)
                {
                     if (!string.IsNullOrEmpty(script_postSummon))
                    {
                        object[] paramList = new object[2];
                        paramList[0] = this;
                        paramList[1] = summonedActor;
                        postSummonMethod.Invoke(null, paramList);
                    }
                     if (originatingActor.GetActorType() == ActorTypes.HERO && GameMasterScript.itemBeingUsed != null)
                    {
                        summonedActor.SetActorData("excludefromhotbarcheck", 1);
                    }
                }               
            }
        }

        float returnVal = 0.0f;
        if (!parentAbility.CheckAbilityTag(AbilityTags.SIMULTANEOUSANIM))
        {
            returnVal = animLength * localSummonPositions.Count;
        }
        else
        {
            returnVal = animLength;
        }

        returnVal = Mathf.Clamp(returnVal, -1f, 1.0f); // make sure summon animations don't take too long.

        if (PlayerOptions.animSpeedScale != 0)
        {
            returnVal *= PlayerOptions.animSpeedScale;
        }

        return returnVal;
    }

    void CheckForHeroPostSummonModifications(Fighter origFighter, Monster newMon)
    {
        if (origFighter.myStats.CheckHasStatusName("status_mmfamiliars"))
        {
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.1f * origFighter.myStats.CheckStatusQuantity("status_mmfamiliars"));
            newMon.allMitigationAddPercent -= (origFighter.myStats.CheckStatusQuantity("status_mmfamiliars") * 0.05f);
        }
        if (origFighter.myStats.CheckHasStatusName("wildnaturebonus1"))
        {
            if (newMon.actorRefName == "mon_summonedlivingvine" || newMon.actorRefName == "mon_summonedbulllivingvine"
                || newMon.actorRefName == "mon_summonedlivingvine2" || newMon.actorRefName == "mon_summonedlivingvine3")
            {
                newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.5f);
            }
        }
        if (UnityEngine.Random.Range(0, 1f) <= 0.25f && newMon.turnsToDisappear > 0 && origFighter.myStats.CheckHasStatusName("xp2_legends"))
        {
            newMon.MakeChampion(true);
        }
    }

    void ScaleSummonedMonsterByDisciplineAndEmblems(Fighter origFighter, Monster newMon)
    {
        float discMod = GameMasterScript.heroPCActor.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * 0.5f;
        newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, discMod);
        if (GameStartData.NewGamePlus > 0)
        {
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, (0.25f * GameStartData.NewGamePlus));
        }
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_floramancer_tier0_pethealth"))
        {
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.2f);
        }
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_soulkeeperemblem_tier0_pets"))
        {
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.15f);
            newMon.myStats.ChangeStat(StatTypes.HEALTH, 100f, StatDataTypes.ALL, true);
        }
    }

    void CheckForSpecialMonsterScaling(Actor originatingActor, Fighter origFighter, Monster newMon)
    {
        if (originatingActor.GetActorType() == ActorTypes.HERO && summonActorRef.Contains("livingvine"))
        {
            if (origFighter.myStats.CheckHasStatusName("status_floraconda2"))
            {
                newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, 20f, StatDataTypes.ALL);
                newMon.LearnNewPower("skill_constrict", 1.0f, 1.0f, 0, 1);
            }
            if (origFighter.myStats.CheckHasStatusName("status_floraconda3"))
            {
                newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, 20f, StatDataTypes.ALL);
                newMon.LearnNewPower("skill_floracondathorns", 1.0f, 1.0f, 0, 99);
            }
        }

        // In job trials or mystery dungeons, we have to use special scaling for monster-summoned creatures
        // Otherwise the scaling gets out of whack and the monsters are either too weak or too strong.
        bool specialScaling = originatingActor.GetActorType() != ActorTypes.HERO && (MapMasterScript.activeMap.IsJobTrialFloor() || MapMasterScript.activeMap.IsMysteryDungeonMap());

        if (scaleWithLevel && !specialScaling)
        {
            int levelDiff = origFighter.myStats.GetLevel() - newMon.myStats.GetLevel();
            newMon.myStats.SetLevel(newMon.myStats.GetLevel() + (levelDiff / 2));
            float healthPerLevelMult = 0.085f;
            newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, (levelDiff * 10f), StatDataTypes.ALL);

            newMon.allMitigationAddPercent -= 0.015f * levelDiff; // 1% more mitigation per level

            if (origFighter.GetActorType() == ActorTypes.HERO)
            {
                healthPerLevelMult = 0.1f;
                newMon.allMitigationAddPercent -= 0.01f * GameMasterScript.heroPCActor.myStats.GetLevel();
                newMon.allDamageMultiplier += (0.01f * levelDiff);
            }

            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, (levelDiff * healthPerLevelMult));

            newMon.myStats.SetLevel(newMon.myStats.GetLevel() + (levelDiff / 2));

            if (origFighter.GetActorType() == ActorTypes.MONSTER)
            {
                Monster origMon = origFighter as Monster;
                if (origMon.levelScaled)
                {
                    newMon.ScaleToSpecificLevel(origMon.targetScalingLevel - 1, false);
                }
            }

            int weaponLookupLevel = newMon.myStats.GetLevel();

            //if (originatingActor.actorfaction == Faction.ENEMY) Just do this for everything now.
            {
                weaponLookupLevel--;
                if (weaponLookupLevel < 1)
                {
                    weaponLookupLevel = 1;
                }
            }
            newMon.myEquipment.GetWeapon().power = Weapon.expectedPetOrSummonWeaponPower[weaponLookupLevel];
            newMon.weaponScaled = true;
            if (newMon.actorRefName == "mon_runiccrystal")
            {
                if (origFighter.myStats.CheckHasStatusName("husynrunicbuff"))
                {
                    newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, newMon.myStats.GetCurStat(StatTypes.HEALTH) * 0.5f, StatDataTypes.ALL);
                }
                if (origFighter.myStats.CheckHasStatusName("emblem_husynemblem_tier0_runic"))
                {
                    newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, newMon.myStats.GetCurStat(StatTypes.HEALTH) * 0.2f, StatDataTypes.ALL);
                }
            }
            else if (newMon.actorRefName == "mon_hunterwolf")
            {
                if (origFighter.myStats.CheckHasStatusName("emblem_hunteremblem_tier1_wolf"))
                {
                    newMon.myStats.ChangeStatAndSubtypes(StatTypes.HEALTH, newMon.myStats.GetCurStat(StatTypes.HEALTH) * 0.25f, StatDataTypes.ALL);
                    newMon.allMitigationAddPercent -= 0.08f;
                }
            }
            newMon.SetBattleDataDirty();
            //newMon.physicalWeaponDamageAddPercent += 0.05f * levelDiff; // 5% more damage per level
        }
        else if (effectRefName == "revivemonster")
        {
            newMon.allDamageMultiplier -= 0.25f;
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, -0.33f);
        }
        else if (specialScaling)
        {
            MonsterManagerScript.ScaleSummonedCreatureInTrialOrMysteryDungeon(origFighter, newMon);
        }
    }

    void CheckForBuildTilesAroundPosition(Fighter origFighter) 
    {
        bool hopSummonFire = effectRefName == "firefly_summon";

        if (effectRefName == "hopsummonice" || hopSummonFire)
        {
            positions.Clear();
            List<MapTileData> newTiles = null;

            if (hopSummonFire)
            {
                CustomAlgorithms.GetTilesAroundPoint(origFighter.GetPos(), 1, MapMasterScript.activeMap);
                newTiles = new List<MapTileData>();
                for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
                {
                    if (CustomAlgorithms.tileBuffer[i].tileType != TileTypes.GROUND) continue;
                    newTiles.Add(CustomAlgorithms.tileBuffer[i]);
                }
            }
            else 
            {
                newTiles = MapMasterScript.GetNonCollidableTilesAroundPoint(origFighter.GetPos(), 1, origFighter);
            }

            foreach (MapTileData m in newTiles)
            {
                positions.Add(m.pos);
            }
        }        
    }

    void CheckForArrowstorm(Actor originatingActor)
    {
        if (effectRefName == "eff_summon_arrowstorm_target")
        {
            CustomAlgorithms.GetPointsOnLineNoGarbage(originatingActor.GetPos(), CombatManagerScript.bufferedCombatData.defender.GetPos());
            Vector2 nPos = Vector2.zero;
            Vector2 nPos2 = Vector2.zero;
            for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
            {
                if (MapMasterScript.GetTile(CustomAlgorithms.pointsOnLine[i]).tileType != TileTypes.GROUND) continue;
                if (MapMasterScript.GetGridDistance(originatingActor.GetPos(), CustomAlgorithms.pointsOnLine[i]) == 1)
                {
                    nPos = CustomAlgorithms.pointsOnLine[i];
                }
                else if (MapMasterScript.GetGridDistance(originatingActor.GetPos(), CustomAlgorithms.pointsOnLine[i]) == 2)
                {
                    nPos2 = CustomAlgorithms.pointsOnLine[i];
                }
            }
            if (nPos != Vector2.zero)
            {
                positions.Add(nPos);
            }
            if (nPos2 != Vector2.zero)
            {
                positions.Add(nPos2);
            }
        }        
    }

    void CheckForFungalSpores(Fighter origFighter)
    {
        // Fungal spores. Fungal react.
        if (effectRefName == "fungalspores")
        {
            switch (origFighter.lastDamageTypeReceived)
            {
                case DamageTypes.FIRE:
                    summonActorRef = "obj_flameslash";
                    break;
                case DamageTypes.WATER:
                    summonActorRef = "obj_fungalice";
                    break;
                case DamageTypes.POISON:
                    summonActorRef = "obj_toxicfumes";
                    break;
                case DamageTypes.LIGHTNING:
                    summonActorRef = "obj_electrictile";
                    break;
            }
        }        
    }

    void BuildPositionListsForSpecialTargetActorTypes()
    {
        if (tActorType == TargetActorType.DEFENDER)
        {
            positions.Clear();
            positions.Add(CombatManagerScript.bufferedCombatData.defender.GetPos());
        }
        else  if (tActorType == TargetActorType.ATTACKER)
        {
            positions.Clear();
            positions.Add(CombatManagerScript.bufferedCombatData.attacker.GetPos());
        }

    }
}
