using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class MapMasterScript
{

    public void SpawnAllStairs(Map mapToSpawn)
    {
        List<Stairs> stToRemove = new List<Stairs>();
        foreach (Stairs st in mapToSpawn.mapStairs)
        {
            bool success = SpawnStairs(st);
            if (!success)
            {
                stToRemove.Add(st);
            }
        }
        foreach (Stairs st in stToRemove)
        {
            mapToSpawn.RemoveActorFromMap(st);
        }
        // Checks tiles around staircases to make sure they aren't falsely flagged for generating transparencies 
        // For example if a Tree was spawned but then removed, that might be causing tiles above it to have TransLayers enabled
        // Even though that tree is no longer there.
        foreach (Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.myMovable != null && st.actorEnabled)
            {
                // This sets a flag in the stair's Movable which is read by its TransLayer child
                // If true, then the TransLayer activates its own self in SpriteTransLayer.cs Update()
                try { st.myMovable.CheckTransparencyBelow(); } // 312019 - This function can fail.
                catch (Exception)
                {
                    Debug.Log("Stair transparency check failed.");
                }
            }
        }
    }

    public void SpawnAllProps(Map mapToSpawn)
    {
        int dtCount = 9;
        foreach (Actor act in mapToSpawn.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                SpawnDestructible(act);
                dtCount++;
            }
        }
        //Debug.Log("Done spawning " + dtCount + " destructibles.");
    }

    public void SpawnAllDecor(Map mapToSpawn)
    {
        MapTileData mtd = null;
        GameObject go = null;
        for (int x = 0; x < activeMap.columns; x++)
        {
            for (int y = 0; y < activeMap.rows; y++)
            {
                mtd = activeMap.mapArray[x, y];
                if (mtd.CheckMapTag(MapGenerationTags.WALLDECOR3X3START))
                {
                    int pick = 1 + UnityEngine.Random.Range(0, 3); // Max number 3x3 chunks
                    go = Instantiate(GameMasterScript.GetResourceByRef("Decor3x3 " + pick));
                    go.transform.position = new Vector2(x, y);
                    activeNonTileGameObjects.Add(go);
                }
                else if (mtd.CheckMapTag(MapGenerationTags.WALLDECOR2X2START))
                {
                    string refOfDecor = "";

                    if (activeMap.dungeonLevelData.tileVisualSet == TileSet.EARTH)
                    {
                        int pick = 1 + UnityEngine.Random.Range(0, 6); // Max number 2x2 chunks
                        refOfDecor = "Decor2x2 " + pick;
                    }
                    else if (activeMap.dungeonLevelData.tileVisualSet == TileSet.STONE)
                    {
                        int pick = 1 + UnityEngine.Random.Range(0, 3); // Max number 2x2 chunks
                        refOfDecor = "StoneDecor2x2 " + pick;
                    }
                    else if (activeMap.dungeonLevelData.tileVisualSet == TileSet.SLATE)
                    {
                        int pick = 1 + UnityEngine.Random.Range(0, 3); // Max number 2x2 chunks
                        refOfDecor = "SlateDecor2x2 " + pick;
                    }

                    if (string.IsNullOrEmpty(refOfDecor))
                    {
                        Debug.Log("No ref of 2x2 wall decor for " + activeMap.dungeonLevelData.floor + " " + activeMap.dungeonLevelData.tileVisualSet);
                    }
                    else
                    {
                        go = Instantiate(GameMasterScript.GetResourceByRef(refOfDecor));
                        if (go == null)
                        {
                            Debug.Log("WARNING: Couldn't find decor chunk ref " + refOfDecor);
                        }
                        else
                        {
                            go.transform.position = new Vector2(x, y);
                            activeNonTileGameObjects.Add(go);
                        }
                    }
                }

                if ((mtd.CheckTag(LocationTags.SOLIDTERRAIN) || mtd.CheckTag(LocationTags.TREE))
                    && activeMap.dungeonLevelData.tileVisualSet != TileSet.SPECIAL && mtd.tileVisualSet != TileSet.SPECIAL)
                {
                    // Sanity check: Maybe this was a tree tile that got converted to ground somewhere.
                    if (mtd.tileType != TileTypes.WALL)
                    {
                        mtd.RemoveTag(LocationTags.SOLIDTERRAIN);
                        mtd.RemoveTag(LocationTags.TREE);
                        continue;
                    }

                    string resource = "";
                    if (mtd.CheckTag(LocationTags.SOLIDTERRAIN))
                    {
                        resource = mtd.GetSolidTileReplacement();
                    }
                    else if (mtd.CheckTag(LocationTags.TREE))
                    {
                        // mountaingrass has special rules as it does not use SolidTerrain normally.
                        if (activeMap.dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
                        {
                            resource = mtd.GetSolidTileReplacement();
                        }
                        else
                        {
                            if (mtd.wallReplacementIndex >= justTrees.Length || mtd.wallReplacementIndex < 0)
                            {
                                mtd.wallReplacementIndex = UnityEngine.Random.Range(0, justTrees.Length);
                            }
                            resource = justTrees[mtd.wallReplacementIndex];
                        }
                    }

                    //Debug.Log(mtd.tileVisualSet + " " + activeMap.dungeonLevelData.tileVisualSet);

                    if (string.IsNullOrEmpty(resource))
                    {
                        /* mtd.RemoveTag(LocationTags.SOLIDTERRAIN);
                        mtd.RemoveTag(LocationTags.TREE);
                        singleWallTileReplacementExists = false; */

                        // This should probably not happen, means I screwed up something in map gen
                        resource = lushGreenWallSingleReplacements[UnityEngine.Random.Range(0, lushGreenWallSingleReplacements.Length)];
                        if (Debug.isDebugBuild) Debug.Log("Our solid terrain resource was null for " + mtd.pos + " " + mtd.CheckTag(LocationTags.TREE) + " " + mtd.CheckTag(LocationTags.SOLIDTERRAIN) + " " + mtd.tileVisualSet);
                    }

                    try
                    {
                        go = GameMasterScript.TDInstantiate(resource);
                        CheckForExtraHeightTiles(go, mtd, x, y);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("ERROR when attempting to spawn solidterrain decor " + e + " " + mtd.CheckTag(LocationTags.SOLIDTERRAIN) + " " + mtd.wallReplacementIndex);
                        Debug.Log("Contd: MTD tileset is " + mtd.tileVisualSet + " floor " + activeMap.floor + " tile " + mtd.pos);
                    }

                    // hacky special case
                    if (activeMap.dungeonLevelData.tileVisualSet == TileSet.MOUNTAINGRASS)
                    {
                        // we could have an animatable.
                        Animatable anim = go.GetComponent<Animatable>();
                        if (anim != null)
                        {
                            anim.SetAnim("Idle");
                        }
                    }

                }
            }
        }
    }

    public void SpawnAllMonsters(Map mapToSpawn)
    {
        int monCount = 0;
        foreach (Actor mon in mapToSpawn.actorsInMap)
        {
            if (mon.GetActorType() == ActorTypes.MONSTER)
            {
                SpawnMonster(mon);
                monCount++;
            }
        }
    }

    public void SpawnAllNPCs(Map mapToSpawn)
    {
        int monCount = 0;
        pool_actorList.Clear();
        foreach (Actor mon in mapToSpawn.actorsInMap)
        {
            if (mon.GetActorType() == ActorTypes.NPC)
            {
                if (!SpawnNPC(mon))
                {
                    pool_actorList.Add(mon);
                }
                monCount++;
            }
        }
        foreach (Actor act in pool_actorList)
        {
            activeMap.RemoveActorFromMap(act);
        }
    }

    public void SpawnAllItems(Map mapToSpawn)
    {
        foreach (Actor itm in mapToSpawn.actorsInMap)
        {
            if (itm.GetActorType() == ActorTypes.ITEM)
            {
                SpawnItem(itm);
            }
        }
        //Debug.Log("Done spawning items");
    }

    /// <summary>
    /// Returns FALSE if the actor doesn't exist
    /// </summary>
    /// <param name="mon"></param>
    /// <returns></returns>
    public bool SpawnNPC(Actor mon)
    {
        if (mon.prefab == null || mon.prefab == "")
        {
            Debug.Log("No prefab for this NPC, " + mon.actorRefName + " " + mon.displayName + " at " + mon.GetPos());
            return false;
        }

        string prefabToUse = mon.prefab;

        bool isTree = false;

        if (mon.prefab == "GroveTree")
        {
            isTree = true;
            NPC n = mon as NPC;
            MagicTree mt = n.treeComponent;
            switch (mt.age)
            {
                case TreeAges.NOTHING:
                    prefabToUse = "GroveTree_Dirt";
                    break;
                case TreeAges.SEED:
                    prefabToUse = "GroveTree_Seed";
                    break;
                case TreeAges.SEEDLING:
                    prefabToUse = "GroveTree_Seedling";
                    break;
                case TreeAges.SAPLING:
                    if (mt.treeRarity == Rarity.MAGICAL)
                    {
                        prefabToUse = "RareTreeSapling";
                    }
                    else
                    {
                        prefabToUse = "GroveTree_Sapling";
                    }

                    break;
                case TreeAges.ADULT:
                    switch (mt.treeRarity)
                    {
                        case Rarity.COMMON:
                            switch (mt.species)
                            {
                                case TreeSpecies.OAK:
                                case TreeSpecies.FOOD_A:
                                    prefabToUse = "Tree1";
                                    break;
                                case TreeSpecies.FOOD_B:
                                    prefabToUse = "Tree2";
                                    break;
                                case TreeSpecies.SPICES:
                                    prefabToUse = "Tree4";
                                    break;
                                case TreeSpecies.CASHCROPS:
                                    prefabToUse = "Tree3";
                                    break;
                            }
                            break;
                        case Rarity.UNCOMMON:
                            switch (mt.species)
                            {
                                case TreeSpecies.OAK:
                                case TreeSpecies.FOOD_A:
                                case TreeSpecies.FOOD_B:
                                case TreeSpecies.CASHCROPS:
                                    prefabToUse = "UncommonTreeAdult";
                                    break;
                                default:
                                    prefabToUse = "UncommonTreeAdult2";
                                    break;
                            }

                            break;
                        case Rarity.MAGICAL:
                            prefabToUse = "RareTreeAdult";
                            break;
                    }

                    break;
            }
        }

        //Debug.Log("Spawning " + mon.actorRefName + " at " + mon.GetPos() + " " + prefabToUse + " " + mon.actorUniqueID);

        if (mon.prefab == "ItemWorldMachine")
        {
            if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) < 2)
            {
                prefabToUse = "ItemWorldMachineBroken";
            }
            else
            {
                prefabToUse = "ItemWorldMachineFixed";
            }
        }

        GameObject go = null;

        GameObject resource = GameMasterScript.GetResourceByRef(prefabToUse);
        if (resource != null)
        {
            go = GameMasterScript.TDInstantiate(prefabToUse);
        }
        else
        {
            Debug.Log("Couldn't find game object prefab " + prefabToUse);
        }


        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(mon.actorRefName, go, SpriteReplaceTypes.NPC);

        mon.SetObject(go);
        Movable move = go.GetComponent<Movable>();
        NPC mn = mon as NPC;
        move.Initialize();

        if (UnityEngine.Random.Range(0, 2) == 0 && mn.myMovable.startXFlippedAtRandom)
        {
            mn.mySpriteRenderer.flipX = !mn.mySpriteRenderer.flipX;
        }
        if (!mn.noBumpToTalk)
        {
            mn.playerCollidable = true;
        }
        mn.monsterCollidable = true;
        move.SetPosition(mon.GetSpawnPos());
        mn.SetCurPos(mon.GetSpawnPos());

        MapTileData spawnMTD = GetTile(mon.GetSpawnPos());

        spawnMTD.extraHeightTiles = move.extraHeightTiles;
        spawnMTD.diagonalBlock = move.diagonalBlock;
        spawnMTD.diagonalLBlock = move.diagonalLBlock;

        mn.UpdateSpriteOrder();
        move.rememberTurns = 2;
        //UpdateMapObjectTile(mon.GetPos());
        activeNonTileGameObjects.Add(go);
        Animatable anm = go.GetComponent<Animatable>();
        mn.myAnimatable = anm;
        if (anm != null)
        {
            anm.SetAnim("Idle");
        }
        /* try { anm.SetAnim("Idle"); }
        catch   //#questionable_try_block
        {
            Debug.Log(mn.actorRefName + " has no animatable for some reason.");
        } */

        mn.CheckForNewStuffAndSpawn();

        //Shep: we would like to hide NPCs if disableActor is set in their spawn condition
        //We can't do this during mapgen because there is not a Unity GameActor attached yet.
        if (mn.actorEnabled == false)
        {
            mn.DisableActor();
        }


        if (isTree)
        {
            Movable mv = go.GetComponent<Movable>();
            if (mv != null)
            {
                MapTileData mtd = GetTile(mon.GetPos());
                mtd.extraHeightTiles = mv.extraHeightTiles;
                mtd.diagonalBlock = mv.diagonalBlock;
                mtd.diagonalLBlock = mv.diagonalLBlock;
            }
        }

        if (!String.IsNullOrEmpty(mn.statusIcon))
        {
            GameObject nStuff = GameMasterScript.TDInstantiate(mn.statusIcon);
            nStuff.transform.SetParent(go.transform);
            nStuff.transform.localPosition = Vector3.zero;
            nStuff.GetComponent<Animatable>().SetAnim("true");
            mn.AddOverlay(nStuff, false);
            nStuff.GetComponent<SpriteRenderer>().enabled = true;
            nStuff.GetComponent<SpriteEffect>().SetBaseVisible(true);
            nStuff.GetComponent<SpriteEffect>().SetAlwaysVisible(true);
            nStuff.GetComponent<SpriteEffect>().SetCurVisible(true);
            GameMasterScript.AlignGameObjectToObject(nStuff, go, Directions.NORTHWEST, 0f, 0f);
        }

        if (activeMap.dungeonLevelData.revealAll)
        {
            mn.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
        }
        if (mn.actorRefName == "npc_hologram")
        {
            mn.mySpriteRenderer.material = GameMasterScript.spriteMaterialHologram;
        }

        BattleTextManager.AddObjectToDict(go);
        return true;
    }

    public void SpawnMonster(Actor mon, bool spawnAfterMapLoad = false)
    {
        if (mon.prefab == null || mon.prefab == "")
        {
            Debug.Log("No prefab for this monster, " + mon.prefab + " at " + mon.GetPos());
            return;
        }

        if (activeMap.costumeParty && mon.actorRefName != "mon_itemworldcrystal" && mon.actorRefName != "mon_nightmarecrystal" && mon.actorfaction != Faction.PLAYER)
        {
            mon.SetActorData("costumeparty", 1);
        }

        if (activeMap.bigMode && mon.actorRefName != "mon_itemworldcrystal" && mon.actorRefName != "mon_nightmarecrystal"
            && mon.actorfaction != Faction.PLAYER)
        {
            mon.SetActorData("bigmode", 1);
        }

        string localPrefab = mon.prefab;

        if (mon.actorRefName == "mon_goldfrog")
        {
            if (mon.ReadActorData("coolfrog") == 1)
            {
                localPrefab = "MonsterCoolfrog";
            }
        }

        if (mon.ReadActorData("costumeparty") == 1 && mon.actorfaction != Faction.PLAYER)
        {
            MonsterTemplateData mtd = GameMasterScript.masterSpawnableMonsterList[UnityEngine.Random.Range(0, GameMasterScript.masterSpawnableMonsterList.Count)];
            while (mtd.prefab == null)
            {
                mtd = GameMasterScript.masterSpawnableMonsterList[UnityEngine.Random.Range(0, GameMasterScript.masterSpawnableMonsterList.Count)];
            }
            localPrefab = mtd.prefab;
        }

        Monster mn = mon as Monster;
        foreach (SeasonalPrefabData spd in mn.myTemplate.seasonalPrefabReplacements)
        {
            if (GameMasterScript.seasonsActive[(int)spd.whichSeason])
            {
                localPrefab = spd.prefab;
                break;
            }
        }

        GameObject go = GameMasterScript.TDInstantiate(localPrefab);

        go.transform.localScale = Vector3.one;

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(mon.actorRefName, go, SpriteReplaceTypes.MONSTER);

        mon.SetObject(go);
        Movable move = go.GetComponent<Movable>();
        move.position = mon.GetSpawnPos();

        if (UnityEngine.Random.Range(0, 2) == 0 && mn.myMovable.startXFlippedAtRandom)
        {
            mn.mySpriteRenderer.flipX = !mn.mySpriteRenderer.flipX;
        }

        mn.UpdateSpriteOrder();
        mn.playerCollidable = true;
        mn.monsterCollidable = true;
        move.SetPosition(mon.GetSpawnPos());
        mn.SetCurPos(mon.GetSpawnPos());
        mn.UpdateSpriteOrder();

        // Wandering monsters shouldn't momentarily flicker into view, they should be invisible at time of spawn.
        if (spawnAfterMapLoad && MapMasterScript.InBounds(mon.GetPos()) && !GameMasterScript.heroPCActor.visibleTilesArray[(int)mon.GetPos().x, (int)mon.GetPos().y])
        {
            move.SetInSightAndSnapEnable(false);
        }

        move.rememberTurns = 2;
        activeNonTileGameObjects.Add(go);
        Animatable anm = go.GetComponent<Animatable>();

        mn.myAnimatable = anm;
        anm.SetAnim("Idle");
        mn.myAnimatable.SetOwner(mn);

        mn.myAnimatable.SetAllSpriteScale(1.0f); // This will mess up mini panthox, so do it in photoshop

        //Shep: we would like to hide NPCs if disableActor is set in their spawn condition
        //We can't do this during mapgen because there is not a Unity GameActor attached yet.
        if (mn.actorEnabled == false)
        {
            mn.DisableActor();
        }

        if (mon.ReadActorData("bigmode") == 1 && mon.actorRefName != "mon_itemworldcrystal")
        {
            try { mon.myAnimatable.SetAllSpriteScale(1.45f); }   //#questionable_try_block
            catch (Exception e)
            {
                Debug.Log("On spawn, couldn't set bigmode " + mon.actorRefName + ": " + e.ToString());
            }
        }

        // Ensure that flipped, rotated creatures don't get re-spawned while flipped or knocked around
        mn.mySpriteRenderer.flipX = false;
        mn.mySpriteRenderer.flipY = false;

        if (mn.flipSpriteY)
        {
            mn.myAnimatable.FlipSpriteY();
        }
        if (mn.noAnimation)
        {
            mn.myAnimatable.StopAnimation();
        }


        if (activeMap.IsItemWorld())
        {
            if (mn.myMovable != null && mn.actorRefName != "mon_goldfrog" && mn.actorRefName != "mon_darkfrog" && !mn.myMovable.usePublicForceColor)
            {
                mn.myMovable.SetColor(new Color(UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f)));
            }
        }

        for (int i = 0; i < mn.myStats.GetAllStatuses().Count; i++)
        {
            StatusEffect se = mn.myStats.GetAllStatuses()[i];
            if (se.refName == "invisible" || se.refName == "spiritwalk")
            {
                mn.SetOpacity(1.0f);
            }
            if (se.refName == "championmonster")
            {
                se.AddSpawnedOverlayRef(mn, Directions.NORTHWEST); // Was northwest?
                continue; // This was break, why lol???????
            }
            if (se.ingameSprite != null && se.ingameSprite != "")
            {
                se.AddSpawnedOverlayRef(mn, se.direction);
            }

            if (se.refName == "itemworldcrystalaura")
            {
                mn.BuildItemWorldAuraIfNeeded();
            }
        }

        if (mn.actorRefName == "mon_nightmarecrystal")
        {
            mn.BuildNightmareCrystalAuraIfNeeded();
        }

        // HARDCODED
        if (mn.actorRefName == "mon_sentryassembler")
        {
            mn.AddAggro(GameMasterScript.heroPCActor, 2000f);
            mn.SetMyTarget(GameMasterScript.heroPCActor);
            mn.myTargetTile = new Vector2(4f, 4f);
        }
        else if (mn.actorRefName == "mon_runiccrystal")
        {
            if (mn.myStats.CheckHasStatusName("runic_crystal2_buff"))
            {
                bool alreadyHasWrathBar = false;
                foreach (Transform t in mn.GetObject().transform)
                {
                    if (t.gameObject.name.Contains("WrathBar"))
                    {
                        alreadyHasWrathBar = true;
                        mn.wrathBarScript = t.gameObject.GetComponent<WrathBarScript>();
                        break;
                    }
                }
                if (!alreadyHasWrathBar)
                {
                    GameObject wrathBar = GameMasterScript.TDInstantiate("PlayerWrathBar");
                    mn.wrathBarScript = wrathBar.GetComponent<WrathBarScript>();
                    mn.wrathBarScript.gameObject.transform.SetParent(go.transform);
                    mn.wrathBarScript.gameObject.transform.localPosition = new Vector3(0f, -0.84f, 1f);
                    mn.EnableWrathBarIfNeeded();
                }

                int runicCount = mn.myStats.CheckStatusQuantity("runic_charge");
                mn.wrathBarScript.UpdateWrathCount(runicCount);

            }
        }

        GameObject healthBarObject;
        if (mn.HasHealthBarObject())
        {
            healthBarObject = mn.healthBarScript.gameObject;
        }
        else
        {
            healthBarObject = GameMasterScript.TDInstantiate("HealthBar");
            mn.healthBarScript = healthBarObject.GetComponent<HealthBarScript>();
        }

        healthBarObject.transform.rotation = Quaternion.identity;

        healthBarObject.transform.SetParent(go.transform);
        healthBarObject.transform.localScale = Vector3.one;

        //Debug.Log(healthBarObject.transform.parent.name + " is parent of " + mn.actorRefName + "'s healthbar");

        mn.healthBarScript.UpdateBar(mn.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH));
        mn.healthBarScript.parentSR = mn.mySpriteRenderer;
        mn.healthBarScript.gameObject.SetActive(PlayerOptions.monsterHealthBars);

        //Debug.Log(mn.actorRefName + " has a healthbar " + mn.healthBarScript.gameObject.name + " Mon's actual object is " + mn.GetObject().name);

        BattleTextManager.AddObjectToDict(go);

        mn.dangerousTilesToMe = new TileDangerStates[activeMap.columns, activeMap.rows];

        if (activeMap.dungeonLevelData.revealAll)
        {
            mn.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
        }
        else
        {
            mn.mySpriteRenderer.material = GameMasterScript.spriteMaterialLit;
        }

        try { mn.myMovable.CheckTransparencyBelow(); }  //#questionable_try_block
        catch (Exception e)
        {
            Debug.Log("Spawn monster transparency check failed, " + mn.actorRefName + " " + mn.GetPos() + " " + mn.dungeonFloor + ": " + e.ToString());
        }

        if (mn.isChampion && mn.championMods.Count > 0)
        {
            // do we have any auras?
            DamageTypes dTypeAura = DamageTypes.COUNT;
            foreach (ChampionMod cm in mn.championMods)
            {
                dTypeAura = cm.elementalAura;
            }
            if (dTypeAura != DamageTypes.COUNT && dTypeAura != DamageTypes.PHYSICAL)
            {
                mn.CreateNewElementalAura(dTypeAura, true);
            }
        }

        if (mn.myStats.CheckHasStatusName("status_undead"))
        {
            mn.mySpriteRenderer.material = GameMasterScript.spriteMaterialGreyscale;
        }

        if (mn.isBoss && mn.dungeonFloor == MapMasterScript.SPIRIT_DRAGON_DUNGEONEND_FLOOR && mn.actorRefName == "mon_xp_spiritdragon")
        {
            SpiritDragonStuff.ChangeDragonAnimationByForm(mn, mn.myStats.CheckHasStatusName("spiritdragon_physical"));
        }
    }

    public bool SpawnStairs(Stairs st)
    {
        if (st.prefab == null || st.prefab == "")
        {
            Debug.Log("No prefab for this stairs, " + st.prefab + " at " + st.GetPos());
            return false;
        }
        if (st == null)
        {
            Debug.Log("WARNING: Cannot instantiate a null actor");
            return false;
        }
        if (st.NewLocation == null)
        {
            TryRelinkStairs(st);

            return false;
        }
        GameObject go = null;

        if (st.isPortal)
        {
            if (st.NewLocation.IsItemWorld())
            {
                st.prefab = "AltPortal";
            }
        }

        try
        {
            go = GameMasterScript.TDInstantiate(st.prefab);
        }
        catch
        {
            Debug.LogWarning("Tried to spawn Prefab " + st.prefab + " but it was not found.");
        }

        // WHY was this commented out before? Shoot me

        if (go == null)
        {
            Debug.Log("No gameobject for " + st.actorRefName + " " + st.prefab);
        }

        st.SetObject(go);
        Movable move = go.GetComponent<Movable>();
        if (UnityEngine.Random.Range(0, 2) == 0 && st.myMovable.startXFlippedAtRandom)
        {
            st.mySpriteRenderer.flipX = !st.mySpriteRenderer.flipX;
        }
        go.transform.localScale = Vector3.one;
        st.SetCurPos(st.GetSpawnPos());
        move.Initialize();
        move.SetPosition(st.GetSpawnPos());
        if (st.myAnimatable != null)
        {
            st.myAnimatable.SetAnim("Default");
        }

        //Debug.Log(st.actorUniqueID + " " + st.newLocation.floor + " " + st.actorEnabled);

        if (!st.actorEnabled)
        {
            st.DisableActor(); // Do this for other actor types
            if (st.myMovable != null)
            {
                st.myMovable.SetBShouldBeVisible(false);
                st.myMovable.SetBVisible(false);
                st.myMovable.inSight = false;
            }

            //Debug.Log(st.GetPos() + " " + st.newLocation.floor + " is now disabled.");
        }
        else
        {
            MapTileData mtd = activeMap.GetTile(st.GetPos());
            Movable mv = st.myMovable;
            mtd.extraHeightTiles = mv.extraHeightTiles;
            mtd.diagonalBlock = mv.diagonalBlock;
            mtd.diagonalLBlock = mv.diagonalLBlock;
        }

        activeNonTileGameObjects.Add(go);

        try { st.myMovable.CheckTransparencyBelow(); }  //#questionable_try_block
        catch
        {
            Debug.Log("Stair spawn transparency check failed, " + st.GetPos() + " " + st.dungeonFloor);
        }

        if (activeMap.dungeonLevelData.revealAll)
        {
            st.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
        }

        return true;
    }

    public void SpawnItem(Actor itm)
    {
        /* if ((itm.prefab == null) || (itm.prefab == ""))
        {
            Debug.Log("No prefab for this item, " + itm.prefab + " at " + itm.GetPos());
            return;
        } */
        if (itm == null)
        {
            Debug.Log("Trying to spawn a null item.");
            Debug.Log(itm.actorRefName);
            return;
        }
        GameObject go = GameMasterScript.TDInstantiate("GenericItemPrefab");

        Item localItem = itm as Item;

        itm.SetObject(go);
        itm.myMovable.Initialize();
        itm.mySpriteRenderer.sprite = localItem.GetSpriteForUI();
        go.GetComponent<Transform>().localEulerAngles = Vector3.zero;
        itm.playerCollidable = false;
        itm.monsterCollidable = false;
        //move.collidable = false;

        // Hacky workaround for items that are dropped and do not spawn.
        if (itm.GetSpawnPos() == Vector2.zero)
        {
            itm.myMovable.SetPosition(itm.GetPos());
        }
        else
        {
            itm.myMovable.SetPosition(itm.GetSpawnPos());
            itm.SetCurPos(itm.GetSpawnPos());
        }

        itm.UpdateSpriteOrder();

        // Spawn SPARKLES
        Item item = itm as Item;

        bool spawnSparkles = true;
        //Debug.Log(item.actorRefName + " " + activeMap.floor);
        if (item.actorRefName == "item_goldkey" && activeMap.floor == 139)
        {
            spawnSparkles = false;
        }

        if ((int)item.rarity >= (int)Rarity.COMMON && spawnSparkles)
        {
            GameObject sparkles = null;
            switch (item.rarity)
            {
                case Rarity.COMMON:
                    sparkles = GameMasterScript.TDInstantiate("ItemSparkleSystem");
                    break;
                case Rarity.UNCOMMON:
                    sparkles = GameMasterScript.TDInstantiate("BlueItemSparkleSystem");
                    break;
                case Rarity.MAGICAL:
                    sparkles = GameMasterScript.TDInstantiate("ItemSparkleSystem");
                    break;
                case Rarity.ANCIENT:
                    sparkles = GameMasterScript.TDInstantiate("OrangeSparkles");
                    break;
                case Rarity.ARTIFACT:
                case Rarity.LEGENDARY:
                    sparkles = GameMasterScript.TDInstantiate("GoldSparkles");
                    break;
                case Rarity.GEARSET:
                    sparkles = GameMasterScript.TDInstantiate("GreenSparkles");
                    break;
            }

            sparkles.transform.SetParent(go.transform);
            sparkles.transform.localPosition = Vector3.zero;

            if (activeMap.dungeonLevelData.revealAll)
            {
                itm.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
            }

            // Good time to play SFX or modify drop sound.
        }
        else
        {
        }

        activeNonTileGameObjects.Add(go);

        try { itm.myMovable.CheckTransparencyBelow(); }  //#questionable_try_block
        catch
        {
            Debug.Log("Item spawn transparency check failed, " + itm.GetPos() + " " + itm.dungeonFloor);
        }

        BattleTextManager.AddObjectToDict(go);
        List<Item> itemsInTile = GetTile(itm.GetPos()).GetItemsInTile();
        if (itemsInTile.Count > 1)
        {
            float counter = 0.01f;
            Vector3 tPos;
            foreach (Item checkItem in itemsInTile)
            {
                if (!checkItem.objectSet) continue;
                tPos = checkItem.GetObject().transform.position;
                tPos.z = counter;
                checkItem.GetObject().transform.position = tPos;
                counter += 0.01f;
            }
        }
    }

    // Deprecated?
    public void SpawnDecor(Actor act)
    {
        //GameObject go = Instantiate(GameMasterScript.GetResourceByRef("DungeonStuff"));
        GameObject go = GameMasterScript.TDInstantiate("DungeonStuff");
        act.SetObject(go);
        Movable move = go.GetComponent<Movable>();
        move.Initialize();
        //move.collidable = false;
        move.defaultCollidable = false;
        move.SetPosition(act.GetSpawnPos());
        act.SetCurPos(act.GetSpawnPos());
        MapTileData mtd = activeMap.mapArray[(int)act.GetPos().x, (int)act.GetPos().y];
        //DungeonStuff decor = mtd.GetDecor();
        //go.GetComponent<SpriteRenderer>().sprite = decor.mySprite;
        //UpdateMapObjectTile(act.GetPos());
        activeNonTileGameObjects.Add(go);
        BattleTextManager.AddObjectToDict(go);
    }

    IEnumerator IWaitThenSpawnProp(Actor prop, float time)
    {
        yield return new WaitForSeconds(time);
        singletonMMS.SpawnDestructible(prop);
    }

    public void WaitThenSummonProp(Actor prop, float time)
    {
        StartCoroutine(IWaitThenSpawnProp(prop, time));
    }

    public void SpawnDestructible(Actor prop)
    {
        // This populates a door game object with door data

        Destructible dt = prop as Destructible;

        string prefab = null;
        if (dt.hasDestroyedState && dt.isDestroyed)
        {
            prefab = dt.destroyedPrefab;
        }
        else
        {
            prefab = dt.prefab;
        }

        if (prop.actorfaction == Faction.PLAYER)
        {
            prefab = dt.playerPrefab;
        }

        if (dt.prefabOptions.Count > 0)
        {
            prefab = dt.GetRandomPrefab();
        }

        // With new object pooling for ALL objects, this is no longer necessary.
        /* if (dt.objectSet && dt.GetObject().activeSelf)
        {
            return;
        } */

        bool terrainSprite = false;
        int spriteIndex = 0;

        bool doTransLayer = true;

        if (prefab == "TerrainTile" || prefab == "MudTile" || prefab == "ElectricTile" || prefab == "LaserTile")
        {
            MapTileData mtd = GetTile(prop.GetPos());
            terrainSprite = true;
            spriteIndex = mtd.indexOfTerrainSpriteInAtlas;
            if (mtd.overlayIndexOfTerrainSprite >= 0 && prefab != "TerrainTile")
            {
                spriteIndex = mtd.overlayIndexOfTerrainSprite;
            }
            doTransLayer = false;
        }
        else if (prefab == "Bookshelf" || prefab == "Shelves")
        {
            prefab += " " + UnityEngine.Random.Range(1, 5);
        }
        else if (GameMasterScript.gmsSingleton.ReadTempGameData("empowerhammer") == 1 && prefab == "Blessed Hammer")
        {
            prefab = "Charged Blessed Hammer";
        }
        else if (UnityEngine.Random.Range(0, 1f) <= 0.5f && prefab == "BudokaBuffTile")
        {
            prefab = "BudokaBuffTile2";
        }

        if (prefab == "EnemyTargeting")
        {
            if (dt.actorfaction == Faction.PLAYER)
            {
                prefab = "PlayerTargeting";
            }
            doTransLayer = false;
        }


        //Debug.Log("Trying to spawn MapObjects/" + prefab + " for " + dt.displayName + " " + dt.actorRefName + " " + dt.actorfaction); 
        //GameObject go = (GameObject)Instantiate(GameMasterScript.GetResourceByRef(prefab));
        GameObject go = GameMasterScript.TDInstantiate(prefab);

        if (go == null)
        {
            Debug.Log("No prefab for this prop, " + prop.prefab);
            return;
        }

        PlayerModManager.TryReplaceMonsterOrObjectOrNPCSprites(dt.actorRefName, go, SpriteReplaceTypes.DESTRUCTIBLE);

        if (terrainSprite)
        {
            Animatable localAnim = go.GetComponent<Animatable>();

            for (int i = 0; i < localAnim.myAnimations[0].mySprites.Count; i++)
            {
                zirconAnim.AnimationFrameData afd = localAnim.myAnimations[0].mySprites[i];
                if (prop.actorRefName == "obj_mudtile")
                {
                    afd.mySprite = terrainAtlas[spriteIndex];
                }
                else
                {
                    int lookup = spriteIndex + (i * 20);
                    afd.mySprite = terrainAtlas[lookup];
                }
            }

        }
        else
        {
            if (doTransLayer)
            {
                foreach (Transform t in go.transform)
                {
                    if (t.gameObject.name.Contains("TransLayer"))
                    {
                        doTransLayer = false;
                        break;
                    }
                }
                if (doTransLayer)
                {
                    GameObject transLayer = GameMasterScript.TDInstantiate("TransLayer");
                    transLayer.transform.SetParent(go.transform);
                    transLayer.GetComponent<SpriteTransLayer>().transMult = 0.35f;
                }
            }
        }
        prop.SetObject(go, doTransLayer);

        // will this interfere with any destructible prefabs? hopefully not, but it should fix pooling issues
        go.transform.localScale = Vector3.one;

        prop.SetCurPos(prop.GetSpawnPos());
        Movable move = go.GetComponent<Movable>();
        move.Initialize();
        move.SetPosition(prop.GetSpawnPos());

        if (prefab == "Coins")
        {
            if (dt.moneyHeld > 100 && dt.moneyHeld < 300)
            {
                go.GetComponent<SpriteRenderer>().sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, "assorteditems_416");
            }
            else if (dt.moneyHeld >= 300)
            {
                go.GetComponent<SpriteRenderer>().sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, "assorteditems_417");
            }
        }

        if (!terrainSprite)
        {
            prop.UpdateSpriteOrder();
            MapTileData mtd = activeMap.GetTile(prop.GetSpawnPos());
            CheckForExtraHeightTiles(go, mtd, mtd.pos.x, mtd.pos.y);
        }
        dt.UpdateSpriteOrder();

        if (activeMap.dungeonLevelData.revealAll)
        {
            if (PlatformVariables.OPTIMIZE_SPRITE_MATERIALS)
            {
                dt.mySpriteRenderer.material = GameMasterScript.spriteMaterial_DestructiblesUnLit;
            }
            else
            {
                dt.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
            }
        }
        else
        {

            if (PlatformVariables.OPTIMIZE_SPRITE_MATERIALS)
            {
                dt.mySpriteRenderer.material = GameMasterScript.spriteMaterial_DestructiblesLit;
            }
            else
            {
                dt.mySpriteRenderer.material = GameMasterScript.spriteMaterialLit;
            }
        }

        Animatable anm = go.GetComponent<Animatable>();
        if (anm != null)
        {
            anm.SetAnim("Default");
            anm.updatedAtLeastOnce = false;
        }

        activeNonTileGameObjects.Add(go);
        if (dt.showDirection)
        {
            dt.ShowDirection(true);
        }

        if (dt.rotateToMoveDirection)
        {
            CustomAlgorithms.RotateGameObject(go, dt.lastMovedDirection);
        }

        if (dt.actorRefName == "obj_coins" || dt.actorRefName == "obj_monsterspawner")
        {
            GameObject sparkles = GameMasterScript.TDInstantiate("ItemSparkleSystem");
            sparkles.transform.SetParent(go.transform);
            sparkles.transform.localPosition = Vector3.zero;
        }

        if (dt.mapObjType == SpecialMapObject.BOMB_ATTACK)
        //if (dt.actorRefName == "obj_enemybomb" || dt.actorRefName == "obj_shrapnelbomb")
        {
            dt.BuildDangerMeshIfNeeded();
        }

        BattleTextManager.AddObjectToDict(go);

        if (activeMap.dungeonLevelData.revealAll)
        {
            dt.mySpriteRenderer.material = GameMasterScript.spriteMaterialUnlit;
        }

        if (!dt.actorEnabled)
        {
            dt.myAnimatable.StopAnimation();
            dt.DisableActor(); // Do this for other actor types
            if (dt.myMovable != null)
            {
            dt.myMovable.SetBVisible(false);
            dt.myMovable.SetBShouldBeVisible(false);
            dt.myMovable.inSight = false;
        }
        }
        /* if (dt.mapObjType == SpecialMapObject.BLESSEDPOOL)
        {
            dt.BuildAura(2, (int)ItemWorldAuras.BLESSEDPOOL);
        } */

        if (dt.GetPos().y > 0)
        {
            try { dt.myMovable.CheckTransparencyBelow(); }  //#questionable_try_block
            catch
            {
                Debug.Log("DT spawn transparency check failed, " + dt.GetPos() + " " + dt.dungeonFloor);
            }
        }
        else
        {
            //Debug.Log("Odd destructible position: " + dt.GetPos() + " " + dt.actorRefName + " " + dt.dungeonFloor);
        }
    }

    // deprecated?
    /* public void SpawnDoor(Actor dd)
    {
        // This populates a door game object with door data
        GameObject go = (GameObject)Instantiate(Resources.Load(dd.prefab));
        if (go == null)
        {
            Debug.Log("No prefab for this door, " + dd.prefab);
            return;
        }
        dd.SetObject(go);
        Movable move = go.GetComponent<Movable>();
        move.Initialize();
        DoorData dmd = dd as DoorData;
        move.SetPosition(dmd.GetSpawnPos());
        dmd.UpdateState(false);
        activeNonTileGameObjects.Add(go);
        BattleTextManager.AddObjectToDict(go);
    } */
}

