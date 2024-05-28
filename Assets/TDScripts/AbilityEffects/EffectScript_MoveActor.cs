using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class MoveActorEffect : EffectScript
{
    public bool pullActor; // This is used by monsters.
    public int distance; // used for push effects
    public bool spin;
    public bool moveThroughObstacles;
    public float arcMult;
    public Directions forceDirection;
    public bool moveToLandingTile;
    public bool flankingMovement;
    public bool swapPlaces;
    public int randomRange;
    public string impactDamageEffect;
    public string script_preRunConditional;
    public string script_postMove;
    public bool afterImages;

    public MoveActorEffect()
    {
        arcMult = 1.0f;
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        MoveActorEffect nTemplate = (MoveActorEffect)template as MoveActorEffect;
        script_postMove = nTemplate.script_postMove;
        tActorType = nTemplate.tActorType;
        pullActor = nTemplate.pullActor;
        distance = nTemplate.distance;
        spin = nTemplate.spin;
        moveToLandingTile = nTemplate.moveToLandingTile;
        moveThroughObstacles = nTemplate.moveThroughObstacles;
        forceDirection = nTemplate.forceDirection;
        swapPlaces = nTemplate.swapPlaces;
        randomRange = nTemplate.randomRange;
        impactDamageEffect = nTemplate.impactDamageEffect;
        afterImages = nTemplate.afterImages;
        script_preRunConditional = nTemplate.script_preRunConditional;
        flankingMovement = nTemplate.flankingMovement;
        if (nTemplate.arcMult != 0.0f)
        {
            arcMult = nTemplate.arcMult;
        }
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        MoveActorEffect eff = compareEff as MoveActorEffect;
        if (pullActor != eff.pullActor) return false;
        if (distance != eff.distance) return false;
        if (spin != eff.spin) return false;
        if (moveThroughObstacles != eff.moveThroughObstacles) return false;
        if (arcMult != eff.arcMult) return false;
        if (forceDirection != eff.forceDirection) return false;
        if (moveToLandingTile != eff.moveToLandingTile) return false;
        if (swapPlaces != eff.swapPlaces) return false;
        if (randomRange != eff.randomRange) return false;

        return true;
    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        affectedActors.Clear();
        results.Clear();

        if (!VerifyOriginatingActorIsFighterAndFix())
        {
            //Debug.LogError("Check 0: originating actor is not a fighter");
            return 0f;
        }

        Monster mon = originatingActor as Monster;

        if (!string.IsNullOrEmpty(script_preRunConditional))
        {
            MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(MoveActorCustomFunctions), script_preRunConditional);
            object[] paramList = new object[1];
            paramList[0] = this;
            object returnObj = runscript.Invoke(null, paramList);
        }

        List<Actor> actorsToProcess = new List<Actor>();

        if (UnityEngine.Random.Range(0, 1.0f) > procChance)
        {
            //Debug.LogError("Check 0.5: proc chance not rolled");
            return 0.0f;
        }

        bool valid = false;

        GetTargetActorsAndUpdateBuildActorsToProcess(indexOfEffect);  

        foreach(Actor act in buildActorsToProcess)
        {
            actorsToProcess.Add(act);
        }
        
        if (EvaluateTriggerCondition(actorsToProcess))
        {
            valid = true;
        }

        if (!valid)
        {
            //Debug.LogError("Check 1: actor cannot be moved");
            return 0.0f;
        }

        // Make list of actors to process.

        float localAnimLength = animLength;

        if (actorsToProcess.Count == 0)
        {
            //Debug.LogError("Check 2: no actors to process");
            return 0.0f;
        }

        if (randomRange > 0)
        {
            //Debug.Log("Random move.");
            List<MapTileData> possible = MapMasterScript.GetNonCollidableTilesAroundPoint(originatingActor.GetPos(), randomRange, originatingActor);
            possible.Shuffle();
            if (possible.Count > 0)
            {
                positions.Clear();
                if (effectRefName.Contains("mmvanishing"))
                {
                    // Don't teleport player into dangerous tiles
                    foreach (MapTileData mtd in possible)
                    {
                        if (!mtd.IsDangerous(originatingActor as Fighter) && !mtd.CheckTag(LocationTags.DUGOUT) && !mtd.IsCollidableActorInTile(originatingActor))
                        {
                            positions.Add(mtd.pos);
                        }
                    }
                    if (positions.Count == 0)
                    {
                        //positions.Add(possible[UnityEngine.Random.Range(0, possible.Count)].pos);
                        //Debug.LogError("Check 2.5: no possible positions");
                        return 0.0f;
                    }
                }
                else
                {
                    positions.Add(possible[UnityEngine.Random.Range(0, possible.Count)].pos);
                }

            }
            else
            {
                //Debug.LogError("Check 3: no possible tiles available");
                return 0.0f;
            }
        }

        Fighter actToProcess = null;

        if (actorsToProcess.Count == 0)
        {
            actToProcess = originatingActor as Fighter;
        }
        else
        {
            actToProcess = actorsToProcess[0] as Fighter;
        }

        bool moveToLocalPositionIfAvailable = false;

        bool perTargetAnim = parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM);
        bool forceSilent = false;

        if (originatingActor.GetActorType() == ActorTypes.MONSTER && GameMasterScript.gmsSingleton.ReadTempGameData("monincombat") == 0)
        {
            forceSilent = true;
        }

        if (playAnimation && !parentAbility.CheckAbilityTag(AbilityTags.PERTARGETANIM))
        {
            perTargetAnim = false;
            // Just play ONE animation for the entire thing.
            CombatManagerScript.GenerateEffectAnimation(originatingActor.GetPos(), centerPosition, this, actToProcess.GetObject(), forceSilent);
        }
        else if (!playAnimation)
        {
            perTargetAnim = false;
        }

        List<Vector2> localPositionsToUse = positions;

        if (effectRefName == "thaumdelayteleport")
        {
            int floor = GameMasterScript.heroPCActor.ReadActorData("delayedteleportfloor");
            if (GameMasterScript.heroPCActor.dungeonFloor != floor)
            {
                return 0f;
            }            
        }

        if (effectRefName == "icebullrush")
        {
            // try to push the target into an ice block
            localPositionsToUse.Clear();
            foreach (Actor act in mon.summonedActors)
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                Destructible dt = act as Destructible;
                if (dt.objectFlags[(int)ObjectFlags.WATER])
                {                    
                    if (!CustomAlgorithms.CheckBresenhamsLOS(mon.GetPos(), dt.GetPos(), MapMasterScript.activeMap)) continue;
                    if (MapMasterScript.GetGridDistance(mon.GetPos(), dt.GetPos()) > distance) continue;
                    if (CombatManagerScript.GetAngleBetweenPoints(mon.GetPos(), dt.GetPos()) < 0) continue; // dont hit something behind us                    
                    localPositionsToUse.Add(dt.GetPos());
                }
            }
            if (localPositionsToUse.Count > 0)
            {
                moveToLocalPositionIfAvailable = true;
            }
        }

        if (effectRefName == "summonhotstreak")
        {
            int maxDist = 0;
            foreach (Vector2 v2 in positions)
            {
                int dist = MapMasterScript.GetGridDistance(originatingActor.GetPos(), v2);
                if (dist > maxDist)
                {
                    maxDist = dist;
                }
            }

            List<Vector2> top3positions = new List<Vector2>();

            foreach (Vector2 v2 in positions)
            {
                int dist = MapMasterScript.GetGridDistance(originatingActor.GetPos(), v2);
                if (dist == maxDist)
                {
                    top3positions.Add(v2);
                }
                if (top3positions.Count == 3)
                {
                    break;
                }
            }

            localPositionsToUse = new List<Vector2>();
            localPositionsToUse.Add(top3positions[UnityEngine.Random.Range(0, top3positions.Count)]);
            if (localPositionsToUse[0] == Vector2.zero)
            {
                localPositionsToUse[0] = originatingActor.GetPos();
            }
        }

        bool anyVitalPoint = false;

        bool currentMapHasHoles = MapMasterScript.activeMap.dungeonLevelData.GetMetaData("hasholes") == 1;

        Fighter originatingFighter = originatingActor as Fighter;
        foreach (Actor act in actorsToProcess)
        {
            if (act.actorRefName == "mon_targetdummy") // THE DUMMY CAN NOT BE MOVED
            {
                continue;
            }

            // After vanishing triggers, don't allow any other vanishing effects for the rest of the turn.
            // Hopefully this will fix some periodic visual issues with vanishing.
            if (act.GetActorType() == ActorTypes.HERO && effectRefName.Contains("vanishing"))
            {
                if (act.ReadActorData("nomore_forcedmove_thisturn") == 1)
                {
                    continue;
                }
            }

            if (swapPlaces)
            {
                MapTileData mtd = MapMasterScript.GetTile(localPositionsToUse[0]);
                if (mtd != null)
                {
                    Actor target = mtd.GetTargetable();
                    if (target != null && target.actorRefName == "mon_targetdummy") continue;
                }
            }


            if (randomRange > 0 && act.GetActorType() == ActorTypes.HERO)
            {
                StringManager.SetTag(0, act.displayName);
                GameLogScript.LogWriteStringRef("log_player_randomwarp");
                GameMasterScript.heroPCActor.SetActorData("nomore_forcedmove_thisturn", 1);
            }

            Vector2 oldPos = act.GetPos();
            Vector2 newPos = new Vector2(0, 0);
            Fighter actorToProcess = act as Fighter;

            bool actorIsHeroAndStartedNextToAttacker = false;

            if (originatingActor.GetActorType() == ActorTypes.HERO && originatingActor != actorToProcess && MapMasterScript.GetGridDistance(originatingActor.GetPos(), actorToProcess.GetPos()) <= 1)
            {
                actorIsHeroAndStartedNextToAttacker = true;
            }

            //If the hero is trying to be moved by someone else, it's possible we're too stronk and won't move
            if (originatingActor != actorToProcess)
            {
                if (!CanBeMovedByForce(actorToProcess))
                {
                    StringManager.SetTag(0, actorToProcess.displayName);
                    GameLogScript.LogWriteStringRef("log_actor_immovable");
                    continue;
                }
            }

            if (effectRefName == "budokakick")
            {
                // Budoka vital point triggers
                anyVitalPoint = EffectScript.EvaluateVitalPointCombo(parentAbility, actorToProcess, originatingFighter);
            }


            if (localPositionsToUse.Count == 0 && distance == 0)
            {
                localPositionsToUse.Add(originatingActor.GetPos());
            }

            bool flankingDestinationTileFound = false;
            if (flankingMovement)
            {
                nearbyTiles = MapMasterScript.GetNonCollidableTilesAroundPoint(localPositionsToUse[0], 1, originatingActor);
                MapTileData bestMTD = null;
                int highestDistanceFromMe = 0;
                foreach (MapTileData mtd in nearbyTiles)
                {
                    int dist = MapMasterScript.GetGridDistance(originatingActor.GetPos(), mtd.pos);
                    if (dist > highestDistanceFromMe)
                    {
                        bestMTD = mtd;
                        highestDistanceFromMe = dist;
                    }
                }

                if (bestMTD != null)
                {
                    flankingDestinationTileFound = true;
                    newPos = bestMTD.pos;
                }
            }

            if (moveToLandingTile)
            {
                newPos = GameMasterScript.bufferedLandingTile;
                //Debug.Log("Move to landing tile");
            }
            else if (distance > 0 && !pullActor)
            {
                MapTileData newTileForActor = null;

                if (!moveToLocalPositionIfAvailable || localPositionsToUse.Count == 0)
                {
                    // Push effect.
                    Directions directionOfPush = Directions.NEUTRAL;
                    if (forceDirection == Directions.OPPOSETARGET)
                    {

                    }
                    else if (forceDirection == Directions.OPPOSE_MONSTERTARGETDIRECTION)
                    {
                        if (originatingActor.GetActorType() == ActorTypes.MONSTER)
                        {
                            Monster m = originatingActor as Monster;
                            if (m.myTarget != null)
                            {
                                Directions targetDir = CombatManagerScript.GetDirection(originatingActor, m.myTarget);
                                directionOfPush = MapMasterScript.oppositeDirections[(int)targetDir];
                            }
                        }
                    }
                    else
                    {
                        directionOfPush = CombatManagerScript.GetDirection(originatingFighter, actorToProcess);
                    }

                    Vector2 moveModifier = MapMasterScript.xDirections[(int)directionOfPush];
                    newTileForActor = MapMasterScript.GetTile(actorToProcess.GetPos());
                    newPos = newTileForActor.pos;
                    for (int i = 0; i < distance; i++)
                    {
                        Vector2 check = new Vector2(actorToProcess.GetPos().x + (moveModifier.x * (i + 1)), actorToProcess.GetPos().y + (moveModifier.y * (i + 1)));
                        MapTileData checkMTD = MapMasterScript.GetTile(check);
                        if (checkMTD.IsCollidable(act) || !MapMasterScript.InBounds(checkMTD.pos))
                        {
                            break;
                        }
                        else
                        {
                            newTileForActor = checkMTD;
                            newPos = checkMTD.pos;
                        }
                    }
                }
                else
                {
                    newTileForActor = MapMasterScript.GetTile(localPositionsToUse.GetRandomElement());
                    newPos = newTileForActor.pos;
                }

                // We hit something.
                if (MapMasterScript.GetGridDistance(newTileForActor.pos, actorToProcess.GetPos()) < distance)
                {                    
                    if (!string.IsNullOrEmpty(impactDamageEffect))
                    {
                        actorToProcess.SetCurPos(newTileForActor.pos); // We must set this here, otherwise it dies on the wrong tile.

                        animLength += ExecuteDamageEffect(impactDamageEffect, actorToProcess);
                        
                        if (!actorToProcess.myStats.IsAlive())
                        {
                            actorToProcess.whoKilledMe = originatingFighter;
                            GameMasterScript.AddToDeadQueue(actorToProcess);
                        }
                    }
                    else if (effectRefName == "icebullrush")
                    {
                        Destructible iceBlock = newTileForActor.GetActorRef("obj_monstericeblock") as Destructible;
                        if (iceBlock != null)
                        {
                            // shatter it and deal damage!
                            CombatManagerScript.WaitThenGenerateSpecificEffect(iceBlock.GetPos(), "IceShatter2x", null, animLength, true);
                            iceBlock.myMovable.WaitThenFadeOut(animLength, 0.1f);
                            GameMasterScript.gmsSingleton.DestroyActor(iceBlock);

                            animLength += ExecuteDamageEffect("eff_evokeiceshards2", actorToProcess);

                            if (!actorToProcess.myStats.IsAlive())
                            {
                                actorToProcess.whoKilledMe = originatingFighter;
                                GameMasterScript.AddToDeadQueue(actorToProcess);
                            }
                        }
                    }
                }

            }
            else if (!flankingDestinationTileFound)
            {
                List<Actor> actorsInPath = null;

                if (CombatManagerScript.bufferedCombatData != null && effectRefName != "mmvanishing")
                {
                    CombatManagerScript.EnqueueSpecificEffect(act.GetPos(), "DustEffect", 0.4f); // Hardcoded jump, pull, or push effect length
                }
                else
                {
                    CombatManagerScript.GenerateSpecificEffectAnimation(act.GetPos(), "DustEffect", null);
                }
                newPos = act.GetPos(); // This is for a jump or pull effect, moving self actor.
                
                if (swapPlaces)
                {
                    newPos = localPositionsToUse[0];
                }
                else
                {

                    // Pulls were working weirdly. They should always pull toward orig actor.
                    if (pullActor)
                    {
                        localPositionsToUse[0] = originatingActor.GetPos();
                    }

                    actorsInPath = new List<Actor>();

                    Vector2 usePosition = localPositionsToUse[0];

                    if (!pullActor)
                    {
                        if (MapMasterScript.GetGridDistance(act.GetPos(), localPositionsToUse[0]) < MapMasterScript.GetGridDistance(act.GetPos(), localPositionsToUse[localPositionsToUse.Count - 1]))
                        {
                            usePosition = localPositionsToUse[localPositionsToUse.Count - 1];
                        }
                    }

                    CustomAlgorithms.GetPointsOnLineNoGarbage(act.GetPos(), usePosition);                    

                    for (int i = 0; i < CustomAlgorithms.numPointsInLineArray; i++)
                    {
                        int tries = 0;
                        Vector2 point = CustomAlgorithms.pointsOnLine[i];

                        tries++;
                        if (point == act.GetPos() || point == newPos)
                        {
                            continue;
                        }
                        if (MapMasterScript.GetTile(point).tileType == TileTypes.GROUND)
                        {
                            // Add actors in path
                            foreach (Actor actInPath in MapMasterScript.GetTile(point).GetAllTargetable())
                            {
                                actorsInPath.Add(actInPath);
                            }
                            // Stop adding
                        }

                        if (!MapMasterScript.GetTile(point).IsCollidable(act))
                        {
                            newPos = point;
                        }
                        else if (!moveThroughObstacles && !currentMapHasHoles)
                        {                            
                            break;
                        }

                        if (tries >= 50)
                        {
                            Debug.Log("WARNING! " + originatingActor.actorRefName + " " + effectRefName + " error moving along jump/pull line.");
                        }
                    }

                    // Hit all actors along path!

                    if (!String.IsNullOrEmpty(effectRefName) && actorsInPath.Count > 0 && effectRefName.Contains("crystalshift"))
                    {
                        string dmgEffect = "crystalshiftdamage";
                        if (effectRefName == "crystalshift2")
                        {
                            dmgEffect = "crystalshiftdamage_2";
                        }
                        DamageEffect template = GameMasterScript.GetEffectByRef(dmgEffect) as DamageEffect;
                        template.CopyLiveData(this);
                        template.targetActors = actorsInPath;

                        localAnimLength += template.DoEffect(); // This was commented out... uh, why
                    }

                }

                // Hacky special case because of the order of effects...
                bool righteousCharge = false;
                if (originatingActor.GetActorType() == ActorTypes.HERO && (parentAbility.refName == "skill_righteouscharge" || parentAbility.refName == "skill_righteouscharge_2"))
                {
                    righteousCharge = true;
                }

                if (localPositionsToUse.Count > 0 && !pullActor && act == originatingActor)
                {
                    // This is a little janky. Was supposed to fix Wild Horse not moving through obstacles, but it broke other things like pulls and teleports to empty.
                    if (moveThroughObstacles && MapMasterScript.GetGridDistance(act.GetPos(), localPositionsToUse[0]) > 1 && MapMasterScript.GetTile(localPositionsToUse[0]).IsCollidable(act))
                    {
                        //Debug.Log("Reposition.");
                        List<MapTileData> possible = MapMasterScript.GetNonCollidableTilesAroundPoint(localPositionsToUse[0], 1, act);

                        if (righteousCharge && actorsInPath != null)
                        {
                            foreach(Actor collideAct in actorsInPath)
                            {
                                possible.Add(MapMasterScript.GetTile(collideAct.GetPos()));
                            }
                        }

                        float shortest = 999f;
                        MapTileData best = null;
                        if (possible.Count > 0)
                        {
                            //newPos = possible[UnityEngine.Random.Range(0, possible.Count - 1)].pos;
                            foreach (MapTileData mtd in possible)
                            {
                                float dist = Vector2.Distance(originatingActor.GetPos(), mtd.pos);
                                if (dist < shortest)
                                {
                                    shortest = dist;
                                    best = mtd;
                                }
                            }
                            newPos = best.pos;
                        }
                    }
                }
            }


            if (swapPlaces)
            {
                newPos = localPositionsToUse[0];
                //Debug.Log("Try swap.");
            }
            

#if UNITY_EDITOR
            if (mon != null && actorToProcess != GameMasterScript.heroPCActor)
            {
                mon.Debug_AddMessage("Pulled " + actorToProcess + " from " + oldPos + " to " + newPos);
                mon.Debug_AddMessage("Hero was at " + GameMasterScript.heroPCActor.GetPos());
            }
#endif



            //Shep 13 Dec 2017: But wait! If we're a hero and being pulled, we want a short delay -- don't worry,
            //we'll still play the anim and spin if necessary, scroll down a few lines.
            if (!(pullActor && actorToProcess.GetActorType() == ActorTypes.HERO))
            {
                actorToProcess.SetCurPos(newPos);
                Movable mv = actorToProcess.myMovable;

                if (playAnimation)
                {
                    float rotation = spin ? 360.0f : 0.0f;
                        
                    mv.AnimateSetPosition(newPos, localAnimLength, false, rotation, arcMult, MovementTypes.SMOOTH); // Straightforward move here.
                    
                }
                else
                {
                    //mv.SetPosition(newPos
                    mv.AnimateSetPosition(newPos, 0.01f, false, 0f, arcMult, MovementTypes.SMOOTH); // Straightforward move here.

                    
                }

            }

            bool heroInvolved = false;
            bool movementProcessedOnMap = false;

            if (swapPlaces)
            {
                //Debug.Log("Swapping places");
                Actor targ = null;
                if (originatingFighter != GameMasterScript.heroPCActor)
                {
                    targ = MapMasterScript.GetTile(newPos).GetTargetableForMonster();
                }
                else
                {
                    targ = MapMasterScript.GetTile(newPos).GetTargetable();
                }

                if (targ == GameMasterScript.heroPCActor)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("hero_swapped", 1);
                    heroInvolved = true;
                }
                if (targ != null && !targ.destroyed)
                {
                    
                    if (animLength == 0)
                    {
                        if (Debug.isDebugBuild) Debug.Log("Warning! Swapped actor animation length for " + effectRefName + " " + parentAbility.refName + " is 0?!");
                    }

                    // AA: 1/5/18, why was NoPush used...? This isn't moving anchors!
                    //MapMasterScript.singletonMMS.MoveAndProcessActorNoPush(newPos, oldPos, targ);
                    MapMasterScript.singletonMMS.MoveAndProcessActor(newPos, oldPos, targ, true);

                    movementProcessedOnMap = true;
                    targ.GetObject().GetComponent<Movable>().AnimateSetPosition(oldPos, localAnimLength, false, 0.0f, 0.0f, MovementTypes.SMOOTH); // Straightforward move here.                    
                }
            }

            if (oldPos == Vector2.zero || newPos == Vector2.zero)
            {
                Debug.Log("EXCEPTION: " + originatingActor.actorRefName + " trying to use " + parentAbility.refName + " effect " + effectRefName + " old/new pos are " + oldPos + " " + newPos);
                continue;
            }

            //play various animations depending on the actor type
            if (pullActor)
            {
                switch (actorToProcess.GetActorType())
                {
                    case ActorTypes.HERO:
                        TDVisualEffects.FancyPullAnimation(actorToProcess, oldPos, newPos, spin, localAnimLength, arcMult);
                        break;
                    case ActorTypes.MONSTER:
                        actorToProcess.myAnimatable.SetAnimConditional(actorToProcess.myAnimatable.defaultTakeDamageAnimationName);
                        GameMasterScript.mms.MoveAndProcessActor(oldPos, newPos, actorToProcess);
                        break;
                    default:
                        actorToProcess.myAnimatable.SetAnimConditional(actorToProcess.myAnimatable.defaultTakeDamageAnimationName);
                        GameMasterScript.mms.MoveAndProcessActor(oldPos, newPos, actorToProcess);
                        break;
                }
                movementProcessedOnMap = true;
            }
            else //If not pulled, move as normal -- the above check is just so that the hero behaves differently
            {
                GameMasterScript.mms.MoveAndProcessActor(oldPos, newPos, actorToProcess);
                movementProcessedOnMap = true;
            }

            CombatManagerScript.ProcessGenericEffect(originatingFighter, actorToProcess, this, false, perTargetAnim, forceSilent);
            if (actorToProcess == GameMasterScript.heroPCActor)
            {
                GameMasterScript.gmsSingleton.SetTempFloatData("bufferx", newPos.x);
                GameMasterScript.gmsSingleton.SetTempFloatData("buffery", newPos.y);
                TileInteractions.HandleEffectsForHeroMovingIntoTile(MapMasterScript.GetTile(newPos), true);
                TileInteractions.CheckAndRunTileOnMove(MapMasterScript.GetTile(newPos), GameMasterScript.heroPCActor);

                CameraController.UpdateCameraPosition(newPos, true);
                if (oldPos.y == newPos.y)
                {
                    CameraController.horizontalOnlyMovement = true;
                }
                else
                {
                    CameraController.horizontalOnlyMovement = false;
                }
            }
            else if (heroInvolved) // Hero was swapped.
            {
                TileInteractions.HandleEffectsForHeroMovingIntoTile(MapMasterScript.GetTile(oldPos), true);
                GameMasterScript.gmsSingleton.SetTempFloatData("bufferx", newPos.x);
                GameMasterScript.gmsSingleton.SetTempFloatData("buffery", newPos.y);
                TileInteractions.CheckAndRunTileOnMove(MapMasterScript.GetTile(oldPos), GameMasterScript.heroPCActor);
                CameraController.UpdateCameraPosition(oldPos, true);
                if (oldPos.y == newPos.y)
                {
                    CameraController.horizontalOnlyMovement = true;
                }
                else
                {
                    CameraController.horizontalOnlyMovement = false;
                }
            }

            if (originatingActor == GameMasterScript.heroPCActor && actorToProcess == GameMasterScript.heroPCActor)
            {
                if (GameMasterScript.heroPCActor.myEquipment.GetArmorType() == ArmorTypes.HEAVY && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_armortraining"))
                {
                    EffectScript landingDmg = GameMasterScript.GetEffectByRef("eff_heavysmash");
                    landingDmg.originatingActor = GameMasterScript.heroPCActor;
                    landingDmg.centerPosition = GameMasterScript.heroPCActor.GetPos();
                    landingDmg.positions.Clear();
                    landingDmg.targetActors.Clear();
                    landingDmg.buildActorsToProcess.Clear();
                    landingDmg.positions.Add(GameMasterScript.heroPCActor.GetPos());
                    landingDmg.parentAbility = parentAbility;
                    CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "GroundStompEffect", null);
                    localAnimLength += landingDmg.DoEffect();
                }
            }

            if (afterImages)
            {
                if (GameMasterScript.heroPCActor.visibleTilesArray[(int)newPos.x, (int)newPos.y] && GameMasterScript.heroPCActor.visibleTilesArray[(int)oldPos.x, (int)oldPos.y])
                {
                    GameObject afterImageCreator = GameMasterScript.TDInstantiate("AfterImageCreator");
                    afterImageCreator.transform.SetParent(actorToProcess.GetObject().transform);
                    afterImageCreator.transform.localPosition = Vector3.zero;
                    afterImageCreator.GetComponent<AfterImageCreatorScript>().Initialize(newPos, animLength + 0.05f, Vector2.Distance(oldPos, newPos), actorToProcess.mySpriteRenderer);
                }
            }

            if (oldPos != newPos)
            {
                MapMasterScript.activeMap.RemoveActorFromLocation(oldPos, actorToProcess); // 1/10/18 necessary to clean up swap places garbage
            }

            // This really should have been handled by all of the above
            // But for some reason, sometimes the actor being touched just doesn't appear anywhere.
            MapMasterScript.activeMap.AddActorToLocation(newPos, actorToProcess);

            if (!string.IsNullOrEmpty(script_postMove))
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(PostMoveActorCustomFunctions), script_postMove);
                object[] paramList = new object[4];
                paramList[0] = this;
                paramList[1] = actorToProcess;
                paramList[2] = oldPos;
                paramList[3] = newPos;
                object returnObj = runscript.Invoke(null, paramList);
            }

            if (distance > 0 || pullActor)
            {
                actorToProcess.influenceTurnData.movedByExternalActorThisTurn = true;
            }            

            // Now let's make sure a monster (such as a friendly Runic Crystal) did not end up on the player's square.
            if (actorToProcess.GetActorType() != ActorTypes.HERO && actorToProcess.GetPos() == GameMasterScript.heroPCActor.GetPos())
            {                
                MapTileData repositionFriendlyPos = MapMasterScript.activeMap.GetRandomEmptyTile(actorToProcess.GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
                MapMasterScript.activeMap.MoveActor(actorToProcess.GetPos(), repositionFriendlyPos.pos, actorToProcess);
                actorToProcess.myMovable.AnimateSetPosition(repositionFriendlyPos.pos, 0.08f, false, 360f, 0f, MovementTypes.LERP);
            }
            else if (actorToProcess.GetActorType() == ActorTypes.HERO)
            {
                // But at the same time, don't stack on top of our friendly pets!
                foreach(Actor summonAct in GameMasterScript.heroPCActor.summonedActors)
                {
                    if (summonAct.GetActorType() == ActorTypes.MONSTER && summonAct.GetPos() == actorToProcess.GetPos())
                    {
                        MapTileData repositionFriendlyPos = MapMasterScript.activeMap.GetRandomEmptyTile(summonAct.GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
                        MapMasterScript.activeMap.MoveActor(summonAct.GetPos(), repositionFriendlyPos.pos, summonAct);
                        summonAct.myMovable.AnimateSetPosition(repositionFriendlyPos.pos, 0.08f, false, 360f, 0f, MovementTypes.LERP);
                    }
                }
            }

            // for dragon tail, test our originating position as well as our finishing position.
            Vector2 dragonTailPos = actorToProcess.GetPos();            
            int newDistance = MapMasterScript.GetGridDistance(originatingActor.GetPos(), dragonTailPos);
            if (newDistance > 1)
            {
                dragonTailPos = newPos;
                newDistance = MapMasterScript.GetGridDistance(originatingActor.GetPos(), dragonTailPos);
            }
            
            if ((actorToProcess.GetActorType() == ActorTypes.HERO && originatingActor != actorToProcess && newDistance <= 1)
                || actorIsHeroAndStartedNextToAttacker)
            {
                if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_dragontailpassive"))
                {
                    Fighter target = actorToProcess;
                    if (originatingFighter.GetActorType() == ActorTypes.MONSTER)
                    {
                        target = originatingFighter;
                    }

                    CombatResultPayload thisAttackResult = CombatManagerScript.Attack(GameMasterScript.heroPCActor, target);
                    if (thisAttackResult.result == CombatResult.DODGE)
                    {
                        StringManager.SetTag(0, GameMasterScript.heroPCActor.displayName);
                        StringManager.SetTag(1, originatingActor.displayName);
                        GameLogScript.LogWriteStringRef("log_attack_miss");
                    }
                }
            }

            // :thinking: Not sure why monsters are sometimes put into screwy states
            // after being hit by Armor Training. Can't reproduce but let's be
            // ABSOLUTELY SURE that IF they are dead, they are added to queue... and
            // Not pre-emptively marked as destroyed for any reason.
            if (actorToProcess.destroyed)
            {
                actorToProcess.destroyed = false;
            }
            if (!actorToProcess.myStats.IsAlive())
            {
                GameMasterScript.AddToDeadQueue(actorToProcess);
            }

            // Now check for ANY stacked actors between monster/hero.
            // Why isn't this being handled properly elsewhere? Not sure, but we need the safety here.
            MapMasterScript.activeMap.CheckForStackedActors(newPos, actorToProcess, true);
            MapMasterScript.activeMap.CheckForStackedActors(originatingFighter.GetPos(), originatingFighter, true);
        }

        if (anyVitalPoint)
        {
            originatingFighter.ChangeCT(100f);
            BattleTextManager.NewText(StringManager.GetString("misc_combo").ToUpperInvariant() + "!", originatingFighter.GetObject(), Color.cyan, 0.5f);
        }


        if (PlayerOptions.animSpeedScale != 0)
        {
            localAnimLength *= PlayerOptions.animSpeedScale;
            animLength *= PlayerOptions.animSpeedScale;
        }


        CombatManagerScript.accumulatedCombatWaitTime += localAnimLength;

        //Debug.LogError("Check 4: completed movement function");

        return animLength;
    }

    //Checks to see if this fighter can be tossed around against thier will
    bool CanBeMovedByForce(Fighter victim)
    {
        //SuperHeavy only prevents forced motion half the time
        if (UnityEngine.Random.Range(0, 1f) <= 0.5f && victim.myStats.CheckHasStatusName("status_mmsuperheavy"))
        {
            return false;
        }
        if (victim.myStats.CheckHasStatusName("status_mmultraheavy"))
        {
            return false;
        }
        if (victim.myStats.CheckHasStatusName("player_tempimmune_move"))
        {
            return false;
        }

        return true;
    }

    float ExecuteDamageEffect(string effectRef, Actor actorToProcess)
    {
        EffectScript template = GameMasterScript.GetEffectByRef(effectRef);
        DamageEffect impactDamage = new DamageEffect();
        impactDamage.CopyFromTemplate(template);
        impactDamage.originatingActor = originatingActor;
        impactDamage.selfActor = originatingActor;
        impactDamage.targetActors.Add(actorToProcess);
        impactDamage.centerPosition = originatingActor.GetPos();
        impactDamage.positions.Add(actorToProcess.GetPos());
        impactDamage.parentAbility = parentAbility;

        return impactDamage.DoEffect();
    }
}