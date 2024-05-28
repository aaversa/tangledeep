using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInteractions : MonoBehaviour
{

    public static List<Actor> dtTileActors;
    public static List<Actor> pool_removeActors;
    public static List<Actor> pool_removeList;
    public static List<Item> pool_lootedItems;
    public static List<Actor> pool_powerups;

    public void Start()
    {
        ResetAllVariablesToGameLoad();
    }

    public static void ResetAllVariablesToGameLoad()
    {
        dtTileActors = new List<Actor>();
        pool_removeActors = new List<Actor>();
        pool_removeList = new List<Actor>();
        pool_lootedItems = new List<Item>();
        pool_powerups = new List<Actor>();
    }
    
    public static void TryFillFromFountain()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        GameObject heroPC = heroPCActor.GetObject();

        MapTileData mtd = MapMasterScript.GetTile(heroPCActor.GetPos());

        if (!mtd.specialMapObjectsInTile[(int)SpecialMapObject.FOUNTAIN]) return;

        Destructible dt = mtd.GetRegenFountain();

        if (dt == null) return;

        if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
        {
            SharaModeStuff.DoSharaFountainInteraction(dt);
            return;
        }

        if (heroPCActor.regenFlaskUses >= GameMasterScript.MAX_FLASK_CHARGES)
        {
            heroPCActor.SetRegenFlaskUses(GameMasterScript.MAX_FLASK_CHARGES);
            GameLogScript.LogWriteStringRef("log_flask_full");
        }
        else
        {
            if (MapMasterScript.activeMap.IsItemWorld())
            {
                GameMasterScript.heroPCActor.AddActorData("dream_numfountains", 1);
            }
            GameLogScript.LogWriteStringRef("log_fill_flask");
            BattleTextManager.NewText(StringManager.GetString("misc_filled_flask"), heroPC, Color.yellow, 0.0f);

            int gainAmount = 2;
            if (heroPCActor.regenFlaskUses >= GameMasterScript.FLASK_REDUCED_GAIN_THRESHOLD)
            {
                gainAmount = 1;
                if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_flask_reduced"))
                {
                    Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_flask_reduced");
                    UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
                }
            }
            heroPCActor.ChangeRegenFlaskUses(gainAmount);
            if (heroPCActor.regenFlaskUses >= GameMasterScript.MAX_FLASK_CHARGES)
            {
                heroPCActor.SetRegenFlaskUses(GameMasterScript.MAX_FLASK_CHARGES);
            }
            heroPC.GetComponent<AudioStuff>().PlayCue("Found Item");
            UIManagerScript.UpdateFlaskCharges();
            GameMasterScript.gmsSingleton.DestroyActor(dt);
        }
    }

    public static void TryGetMapObjectDialogOrCoins()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        MapTileData mtd = MapMasterScript.GetTile(heroPCActor.GetPos());

        dtTileActors = mtd.GetAllActors();

        pool_removeList.Clear();

        for (int i = 0; i < dtTileActors.Count; i++)
        {
            if (dtTileActors[i].bRemovedAndTakeNoActions)
            {
                continue;
            }

            if (dtTileActors[i].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                Destructible dt = dtTileActors[i] as Destructible;
                if (dt.destroyOnStep && dt.dtStatusEffect == null)
                {
                    pool_removeList.Add(dt);
                }
                if (dt.moneyHeld > 0)
                {
                    CheckForGoldPickupEvents();
                    dt.moneyHeld = heroPCActor.ChangeMoney(dt.moneyHeld);
                    StringManager.SetTag(0, dt.moneyHeld.ToString());
                    GameLogScript.LogWriteStringRef("log_findmoney");

                    BattleTextManager.NewText(StringManager.GetString("money_bt"), heroPCActor.GetObject(), Color.yellow, 0.1f);
                    //heroPCActor.GetObject().GetComponent<AudioStuff>().PlayCue("Found Item");
                    UIManagerScript.PlayCursorSound("GetMoney");
                    dt.moneyHeld = 0;
                }
                if (dt.hasDialog && GameStartData.IsConversationValid(dt.dialogRef))
                {
                    if (dt.dialogRef == "techcube_functionpickup")
                    {
                        // #todo - functionalize this, i.e. run functions on touch object
                        DialogEventsScript.StartTechCubePickupSequence("");
                        return; // Yes, I'm intending to return here
                    }
                    else
                    {
                        Conversation c = GameMasterScript.FindConversation(dt.dialogRef);
                        UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
                    }

                }
            }
        }

        if (pool_removeList.Count > 0)
        {
            foreach (Actor act in pool_removeList)
            {
                GameMasterScript.AddToDeadQueue(act);
            }
        }
    }

    public static bool TryGetStoryObject()
    {
        MapTileData mtd = MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos());

        if (!mtd.specialMapObjectsInTile[(int)SpecialMapObject.STORYOBJECT]) return false;

        Destructible dt = mtd.GetSpecialDestructible(SpecialMapObject.STORYOBJECT);
        if (dt == null) return false;
        float cv = MapMasterScript.activeMap.challengeRating;

        if (string.IsNullOrEmpty(dt.extraActorReference) && !string.IsNullOrEmpty(dt.dialogRef))
        {
            dt.extraActorReference = dt.dialogRef;
        }

        // figure out convo here.
        if (dt.extraActorReference == null)
        {
            Debug.Log(dt.actorRefName + " has no conversation attached?");
        }
        else
        {
            Conversation c = GameMasterScript.FindConversation(dt.extraActorReference);
            UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
            if (!MetaProgressScript.journalEntriesRead.Contains(c.journalEntry))
            {
                MetaProgressScript.ReadJournalEntry(c.journalEntry);
            }
        }


        GameMasterScript.gmsSingleton.DestroyActor(dt);
        return true;
    }

    public static void TryGetSparkles()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        MapTileData mtd = MapMasterScript.GetTile(heroPCActor.GetPos());

        if (!mtd.specialMapObjectsInTile[(int)SpecialMapObject.TREASURESPARKLE]) return;

        Destructible dt = mtd.GetSpecialDestructible(SpecialMapObject.TREASURESPARKLE);
        if (dt == null) return;

        // Found a loot sparkle.

        if (UnityEngine.Random.Range(0, 1f) <= 0.25f)
        {

            int goldAmount = (int)(UnityEngine.Random.Range(15, 21) + (Mathf.Pow(MapMasterScript.activeMap.GetChallengeRating(), 4f) * 20));

            if (heroPCActor.myStats.CheckHasStatusName("status_mmgoldfind1"))
            {
                float ng = goldAmount * 1.15f;
                goldAmount = (int)ng;
            }


            goldAmount = heroPCActor.ChangeMoney(goldAmount);
            StringManager.SetTag(0, goldAmount.ToString());
            GameLogScript.LogWriteStringRef("log_findmoney");
            string gText = goldAmount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.GOLD);
            BattleTextManager.NewText(gText, heroPCActor.GetObject(), Color.yellow, 0.1f);
        }
        else
        {
            pool_lootedItems.Clear();

            if (MysteryDungeonManager.InMysteryDungeonWithExtraConsumables())
            {
                Item nItem = MysteryDungeonManager.TryCreatingRandomExtraConsumable(MapMasterScript.activeMap.GetChallengeRating());
                if (nItem != null)
                {
                    dt.myInventory.AddItem(nItem, true);
                }
            }

            foreach (Item loot in dt.myInventory.GetInventory())
            {
                pool_lootedItems.Add(loot);
                StringManager.SetTag(0, loot.displayName);
                GameLogScript.GameLogWrite(StringManager.GetString("found_sparkle_log"), heroPCActor);
                BattleTextManager.NewText(StringManager.GetString("found_sparkle_bt"), heroPCActor.GetObject(), Color.blue, 0.1f);
            }
            foreach (Item loot in pool_lootedItems)
            {
                if (MapMasterScript.activeMap.IsItemWorld())
                {
                    loot.SetActorData("fromdreammob", 1);
                }
                GameMasterScript.LootAnItem(loot, heroPCActor, false);
                heroPCActor.UpdateActiveGear(loot); // Put this somewhere else? 
            }

        }

        heroPCActor.GetObject().GetComponent<AudioStuff>().PlayCue("Found Item");

        GameMasterScript.gmsSingleton.DestroyActor(dt);
    }

    public static bool TryPickupItemsInHeroTile(bool bSuppressDialogs = false)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        MapTileData mtd = MapMasterScript.GetTile(heroPCActor.GetPos());
        if (!mtd.AreItemsInTile())
        {
            return false;
        }
        else
        {
            // Loot the tile
            List<Item> itemsInTile = new List<Item>();            
            itemsInTile = MapMasterScript.singletonMMS.GetItemsInTile(heroPCActor.GetPos());
            InventoryScript playerInventoryScript = heroPCActor.myInventory;

            List<Item> lootedItems = new List<Item>();
            foreach (Item loot in itemsInTile)
            {
                if (!GameModifiersScript.CheckForValidItemPickup(loot))
                {
                    continue;
                }
                lootedItems.Add(loot);
                string lText = loot.displayName;
                if (loot.rarity == Rarity.COMMON)
                {
                    lText = UIManagerScript.silverHexColor + loot.displayName + "</color>";
                }
                string qtyText = "";
                if (loot.GetQuantity() > 1)
                {
                    qtyText = " (" + loot.GetQuantity() + ") ";
                }
                StringManager.SetTag(0, lText + qtyText);
                GameLogScript.GameLogWrite(StringManager.GetString("you_pick_up"), heroPCActor);
                GameMasterScript.heroPCActor.OnItemPickedUpOrPurchased(loot, purchased: false);
            }
            foreach (Item loot in lootedItems)
            {
                GameMasterScript.LootAnItem(loot, heroPCActor, true);
                if (!bSuppressDialogs)
                {
                    loot.CheckForAndInitiatePickupDialog();
                }

                heroPCActor.UpdateActiveGear(loot); // Put this somewhere else?
            }


            /* if ((!heroPCActor.visitedMerchant) && (!tutorialManager.WatchedTutorial("tutorial_sellitems")) && (!PlayerOptions.tutorialTips))
            {
                if (heroPCActor.myInventory.GetInventory().Count > 5)
                {
                    Conversation newConvo = tutorialManager.GetTutorial("tutorial_sellitems");
                    UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                }
            } */

        }
        //TurnData td = new TurnData();
        //td.turnType = TurnTypes.PASS;
        //NextTurn(td, true);
        return true;
    }

    public static void LootAllItemsInTile(Fighter looter, MapTileData mtd)
    {
        foreach (Item itm in mtd.GetItemsInTile())
        {
            if (looter.GetActorType() == ActorTypes.MONSTER &&
                (itm == MapMasterScript.itemWorldItem ||
                (MapMasterScript.activeMap.IsJobTrialFloor() && itm.actorRefName == "item_trialrelic")))
            {
                continue;
            }
            GameMasterScript.LootAnItem(itm, looter, true);
        }
    }

    public static IEnumerator WaitThenBreakDestructible(float time, Fighter attacker, Destructible dt, bool animation = true)
    {
        yield return new WaitForSeconds(time);
        BreakDestructible(attacker, dt, animation);
    }

    public static void BreakDestructible(Fighter attacker, Destructible dt, bool animation = true)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        // For now, all destructibles... Can be destroyed.
        GameObject destroyFX = null;
        bool silentImpact = false;
        if (!string.IsNullOrEmpty(dt.deathPrefab))
        {
            destroyFX = GameMasterScript.TDInstantiate(dt.deathPrefab);
            CombatManagerScript.TryPlayEffectSFX(destroyFX, dt.GetPos(), null);
            destroyFX.transform.position = dt.GetPos();
            silentImpact = true;
        }

        // Attacking a destructible object. This isn't really combat.
        if (animation && attacker != null) // Assume breaking with attack.
        {
            CombatDataPack cdp = new CombatDataPack();
            cdp.attackerWeapon = attacker.myEquipment.GetWeapon();
            CombatManagerScript.AddToCombatStack(cdp);

            if (attacker.myEquipment.GetWeapon() != null)// && (attacker.myEquipment.GetWeapon().isRanged))
            {
                CombatManagerScript.GetSwingEffect(attacker, dt, attacker.myEquipment.GetWeapon(), false);
            }

            if (attacker == heroPCActor)
            {
                attacker.myAnimatable.SetAnimDirectional("Attack", CombatManagerScript.GetDirection(attacker, dt), attacker.lastCardinalDirection);
            }

            Directions dir = CombatManagerScript.GetDirection(attacker, dt);
            attacker.myMovable.Jab(dir);
            GameObject impactSpriteObj = CombatManagerScript.GetImpactEffect(attacker, cdp.attackerWeapon, silentImpact);
            if (impactSpriteObj != null)
            {
                impactSpriteObj.transform.position = dt.GetPos();
                if (cdp.attackerWeapon.isRanged)
                {
                    // Fired a projectile, so rotate this impact FX.
                    float angle = CombatManagerScript.GetAngleBetweenPoints(attacker.GetPos(), dt.GetPos());
                    Directions directionOfAttack = MapMasterScript.GetDirectionFromAngle(angle);
                    impactSpriteObj.transform.Rotate(new Vector3(0, 0, MapMasterScript.directionAngles[(int)directionOfAttack]), Space.Self);
                }
            }

            CombatManagerScript.RemoveFromCombatStack(cdp);
        }
        if (dt.summoner != null)
        {
            dt.summoner.RemoveSummon(dt);
        }
        

        if (dt.dtStatusEffect != null)
        {
            bool dummy = false; // not relevant here as this doesn't happen during the turn thread
            float waitTime = 0.0f;
            // Special case for vine burst because we can't store multiple status effects in one destructible.... YET
            if (dt.dtStatusEffect.CheckRunTriggerOn(StatusTrigger.DESTROYED))
            {
                waitTime = GameMasterScript.gmsSingleton.RunTileEffect(dt, null, 0, out dummy);
            }
            else if (dt.mapObjType == SpecialMapObject.SWINGINGVINE && GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_floramancer_tier2_vineburst"))
            {
                // Special vine burst
                StatusEffect burstTemplate = GameMasterScript.FindStatusTemplateByName("status_vineburst");
                dt.dtStatusEffect = new StatusEffect();
                StatusEffect.CopyFromTemplate(dt.dtStatusEffect, burstTemplate);
                dt.dtStatusEffect.SetSubEffectsToOrigActor(GameMasterScript.heroPCActor);
                waitTime = GameMasterScript.gmsSingleton.RunTileEffect(dt, null, 0, out dummy);
            }
        }

        bool pandoraBox = false;

        if (dt.mapObjType == SpecialMapObject.MONSTERSPAWNER)
        {
            pandoraBox = true;
            MapMasterScript.activeMap.spawnerAlive = false;
            GameLogScript.GameLogWrite(StringManager.GetString("open_pandora_box"), heroPCActor);
            if (attacker == heroPCActor)
            {
                float jpAmount = UnityEngine.Random.Range(24f, 30f) + ((12 * heroPCActor.myStats.GetLevel() - 1));
                GameMasterScript.gmsSingleton.AwardJP(jpAmount);
                int newMoney = UnityEngine.Random.Range(80, 90) + ((50 * heroPCActor.myStats.GetLevel() - 1));

                GameMasterScript.heroPCActor.ChangeMoney(newMoney);
                StringManager.SetTag(0, newMoney.ToString());
                GameLogScript.GameLogWrite(StringManager.GetString("found_money"), heroPCActor);

                if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.PANDORA_DROP_ORB_CHANCE && !SharaModeStuff.IsSharaModeActive()
                    && ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1)
                {
                    Item newOrb = ItemWorldUIScript.CreateItemWorldOrb(MapMasterScript.activeMap.GetChallengeRating(), true, false);
                    MapTileData emptyTile = MapMasterScript.FindNearbyEmptyTileForItem(dt.GetPos());
                    MapMasterScript.activeMap.PlaceActor(newOrb, emptyTile);
                    MapMasterScript.singletonMMS.SpawnItem(newOrb);
                }
                float xpAmount = 0f;
                string battleText = "";
                if (heroPCActor.myJob.jobEnum == CharacterJobs.SHARA)
                {
                    SharaModeStuff.SpawnLearnPowerDialog(sharaPowers: false);
                    battleText = ((int)jpAmount).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP);
                }
                else
                {
                     xpAmount = UnityEngine.Random.Range(10f, 25f) + (30 * (heroPCActor.myStats.GetLevel() - 1));
                    GameMasterScript.gmsSingleton.AwardXPFlat(xpAmount, false);
                    battleText = ((int)xpAmount).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.XP) + " " + ((int)jpAmount).ToString() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP);
                }
                if (PlayerOptions.battleJPXPGain)
                {
                    BattleTextManager.NewText(battleText, heroPCActor.GetObject(), Color.yellow, 0.33f);
                }
            }
            if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_pandora") && PlayerOptions.tutorialTips)
            {

                GameMasterScript.SetAnimationPlaying(true);
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_pandora");
                UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(newConvo, DialogType.STANDARD, null, 2.1f));
                //UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
            }
            heroPCActor.numPandoraBoxesOpened++;
        }

        GameObject dgo = dt.GetObject();
        if (dgo == null)
        {
            Debug.Log("Destroyed dt " + dt.actorRefName + " at " + dt.GetPos() + " type: " + dt.mapObjType + " floor " + dt.dungeonFloor + " has no object");
        }
        else
        {
            if (destroyFX != null)
            {
                destroyFX.transform.position = dgo.transform.position;
            }
        }

        if (!dt.hasDestroyedState)
        {
            MapMasterScript.singletonMMS.RemoveActorFromMap(dt);
            if (dgo != null)
            {
                GameMasterScript.ReturnActorObjectToStack(dt, dgo);
            }
        }
        else
        {
            SetDestructibleStateToDestroyed(dt);
        }
        dt.MarkAsDestroyed();
        LootGeneratorScript.TryGenerateLoot(dt, dt.GetPos(), throwLoot:pandoraBox);

        
        if (dt.actorRefName == "obj_jobtrial_crystal")
        {
            GameEventsAndTriggers.CheckForJobTrialVictory();
        }

        if (dt.actorRefName == "obj_regenquestobject")
        {
            int aliveRegenQuestObjects = 0;
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.actorRefName == "obj_regenquestobject" && !act.destroyed)
                {
                    Debug.Log("One object alive...");
                    aliveRegenQuestObjects++;
                }
            }
            if (aliveRegenQuestObjects == 0)
            {
                GameEventsAndTriggers.ClearFinalSideArea3();
            }
            else
            {
                UIManagerScript.StartConversationByRef("final_sidearea3_inprogress", DialogType.STANDARD, null);
            }
        }

        MapTileData targetMTD = MapMasterScript.GetTile(dt.GetPos());
        targetMTD.UpdateVisionBlockingState();
    }

    public static void DoStaticTerrainEffects(MapTileData mtd)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        if (mtd.CheckTag(LocationTags.ISLANDSWATER) && PlayerOptions.autoPlanksInItemWorld)
        {
            if (heroPCActor.myInventory.GetItemQuantity("item_planks") > 0)
            {
                if (!mtd.CheckActorRef("obj_plank"))
                {
                    heroPCActor.myInventory.ChangeItemQuantityByRef("item_planks", -1);
                    Destructible dt = MapMasterScript.activeMap.CreateDestructibleInTile(mtd, "obj_plank");
                    MapMasterScript.singletonMMS.SpawnDestructible(dt);
                    dt.myMovable.SetInSightAndSnapEnable(true);
                }
            }
        }

        if (mtd.CheckTag(LocationTags.LAVA))
        {
            heroPCActor.stepsInDifficultTerrain++;
        }
        else if (mtd.CheckTag(LocationTags.ELECTRIC))
        {
            heroPCActor.stepsInDifficultTerrain++;
        }

        if (mtd.CheckTag(LocationTags.WATER))
        {
            heroPCActor.stepsInDifficultTerrain++;

            if (!heroPCActor.myStats.CheckHasStatusName("status_waterwalk") && !heroPCActor.myStats.CheckHasStatusName("oceangem"))
            {
                /* float decStam = (heroPCActor.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX) * -0.01f) - 2f; // was 3 and 4
                heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, decStam, StatDataTypes.CUR, true);
                string display = ((int)decStam).ToString();// + "stam";
                UIManagerScript.FlashStaminaBar(0.5f); */
            }
            else
            {
                heroPCActor.ChangeCT(5f);
            }
        }

        if (mtd.CheckTag(LocationTags.ISLANDSWATER) && MapMasterScript.activeMap.dungeonLevelData.layoutType == DungeonFloorTypes.ISLANDS)
        {
            if (mtd.GetActorRef("obj_plank") == null && !heroPCActor.myStats.CheckHasStatusName("oceangem") && heroPCActor.ReadActorData("turn_deadlyvoiddmg") != GameMasterScript.turnNumber)
            {
                int amount = (int)((float)(heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.015f));
                // Kalzarius mod can absorb dmg too.
                if (!(UnityEngine.Random.Range(0,1f) <= 0.5f && heroPCActor.myStats.CheckHasActiveStatusName("status_ignore3pdamage")))
                {
                    heroPCActor.SetActorData("turn_deadlyvoiddmg", GameMasterScript.turnNumber);
                    
                    UIManagerScript.FlashHealthBar(0.5f);
                    heroPCActor.TakeDamage(amount, DamageTypes.POISON);
                    BattleTextManager.NewDamageText(amount, false, Color.yellow, heroPCActor.GetObject(), 0.0f, 1.0f);
                    StringManager.SetTag(0, amount.ToString());
                    GameLogScript.LogWriteStringRef("log_deadlyvoid_dmg");
                    heroPCActor.stepsInDifficultTerrain++;
                }
                else
                {
                    StringManager.SetTag(0, heroPCActor.displayName);
                    StringManager.SetTag(1, amount.ToString());
                    GameLogScript.LogWriteStringRef("log_damage_cap_reduce");
                }
            }
        }

        if ((mtd.CheckTag(LocationTags.MUD) || mtd.CheckTag(LocationTags.SUMMONEDMUD)) && !heroPCActor.myStats.CheckHasStatusName("status_mmresistmud"))
        {
            heroPCActor.stepsInDifficultTerrain++;
        }
    }

    public static void HandleEffectsForHeroMovingIntoTile(MapTileData mtd, bool recomputeFOVFromBufferedHeroPosition)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        // Auto build planks

        DoStaticTerrainEffects(mtd);

        bool anyTutorial = false;

        anyTutorial = CheckForItemsInTileDoTutorial(mtd);

        bool pickupItems = false;

        if (PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
        {
            pickupItems = true;
        }
        else
        {
            pickupItems = PlayerOptions.autoPickupItems;
        }

        if (pickupItems)
        {
            TileInteractions.TryPickupItemsInHeroTile();
        }

        TryGetSparkles();
        TryFillFromFountain();
        TryGetStoryObject();
        TryGetMapObjectDialogOrCoins();

        if (recomputeFOVFromBufferedHeroPosition)
        {
            MapMasterScript.singletonMMS.UpdateMapObjectData(true);
            GameMasterScript.gmsSingleton.SetTempGameData("fov_recomputed", 1);
        }

        // Do we seriously need a special case for Phoenix Wing? It's an important ability, so yes.
        if (mtd.HasActorByRef("obj_phoenixwing") && GameMasterScript.heroPCActor.ReadActorData("phoenixwingturn") != GameMasterScript.turnNumber)
        {
            Destructible pwing = mtd.GetActorRef("obj_phoenixwing") as Destructible;
            StatusEffect pwingEff = pwing.dtStatusEffect;
            GameLogScript.BeginTextBuffer();
            foreach (EffectScript eff in pwingEff.listEffectScripts)
            {
                eff.positions.Clear();
                eff.positions.Add(GameMasterScript.heroPCActor.GetPos());
                eff.centerPosition = GameMasterScript.heroPCActor.GetPos();
                eff.originatingActor = GameMasterScript.heroPCActor;
                eff.selfActor = GameMasterScript.heroPCActor;                
                eff.DoEffect();
            }
            GameLogScript.EndTextBufferAndWrite();
        }
    }

    public static bool CheckForItemsInTileDoTutorial(MapTileData mtd)
    {
        MapMasterScript mms = MapMasterScript.singletonMMS;
        TutorialManagerScript tutorialManager = GameMasterScript.tutorialManager;

        bool anyTutorial = false;
        if (mtd.AreItemsInTile())
        {
            if (MapMasterScript.activeMap.IsItemWorld() && PlayerOptions.tutorialTips && !tutorialManager.WatchedTutorial("tutorial_dreamitems"))
            {
                foreach (Item itm in mtd.GetItemsInTile())
                {
                    if (itm.dreamItem)
                    {
                        Conversation newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_dreamitems");
                        UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                        break;
                    }
                }
            }
            if (MapMasterScript.activeMap == mms.townMap && !tutorialManager.WatchedTutorial("tutorial_rangedtactics")
                & MetaProgressScript.ReadMetaProgress("rangedtactics") != 1)
            {
                foreach (Item itm in mtd.GetItemsInTile())
                {
                    if (itm.itemType == ItemTypes.WEAPON)
                    {
                        Weapon wp = (Weapon)itm as Weapon;
                        if (wp.range > 1)
                        {
                            Conversation newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_rangedtactics");
                            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                            anyTutorial = true;
                            break;
                        }
                    }
                }
            }

            /* if ((!anyTutorial) && (!PlayerOptions.tutorialTips) && (!tutorialManager.WatchedTutorial("tutorial_toucheditemkeyboard")) && (!tutorialManager.WatchedTutorial("tutorial_toucheditemcontroller")))
            {
                Conversation newConvo = null;
                if (PlayerOptions.showControllerPrompts)
                {
                    newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_toucheditemcontroller");
                }
                else
                {
                    newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_toucheditemkeyboard");
                }

                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                anyTutorial = true;
                //touchedFirstItem = true;
            } */
            else if ((PlayerOptions.tutorialTips) && (!anyTutorial))
            {
                /* if ((!tutorialManager.WatchedTutorial("tutorial_rangedweap")) && (!PlayerOptions.tutorialTips))
                {
                    foreach (Item itm in mtd.GetItemsInTile())
                    {
                        if (itm.itemType == ItemTypes.WEAPON)
                        {
                            Weapon wp = (Weapon)itm as Weapon;
                            if (wp.range > 1)
                            {
                                Conversation newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_rangedweap");
                                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                                anyTutorial = true;
                                break;
                            }
                        }
                    }
                }
                else */
                if (!tutorialManager.WatchedTutorial("tutorial_skillorb"))
                {
                    foreach (Item itm in mtd.GetItemsInTile())
                    {
                        if (itm.IsJobSkillOrb())
                        {
                            MetaProgressScript.SetMetaProgress("foundanyskillorbs", 1);
                            Conversation newConvo = tutorialManager.GetTutorialAndMarkAsViewed("tutorial_skillorb");
                            UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                            anyTutorial = true;
                            break;
                        }
                    }
                }
                if (MetaProgressScript.ReadMetaProgress("foundanylucidorbs") != 1)
                {
                    foreach (Item itm in mtd.GetItemsInTile())
                    {
                        if (itm.IsLucidOrb())
                        {
                            MetaProgressScript.SetMetaProgress("foundanylucidorbs", 1);
                            break;
                        }
                    }
                }
            }
        }
        return anyTutorial;
    }

    public static void CheckAndRunTileOnMove(MapTileData mtd, Actor act)
    {
        GameMasterScript gmsSingleton = GameMasterScript.gmsSingleton;

        if (act == null || mtd == null)
        {
            Debug.Log("Can't check a null actor or tile.");
            return;
        }
        GameMasterScript.dtTileActors = mtd.GetAllActors();
        Destructible dt;       

        for (int x = 0; x < GameMasterScript.dtTileActors.Count; x++)
        {
            if (GameMasterScript.dtTileActors[x].GetActorType() == ActorTypes.DESTRUCTIBLE)
            {
                dt = GameMasterScript.dtTileActors[x] as Destructible;
                if (dt.dtStatusEffect == null) continue;
                if (dt.runEffectOnlyOnce && dt.ReadActorData("runeffect") == 1) continue;
                if (dt.dtStatusEffect.CheckAbilityTag(AbilityTags.REQHEROTRIGGER) && act != GameMasterScript.heroPCActor)
                {
                    continue;
                }
                else if (dt.dtStatusEffect.CheckAbilityTag(AbilityTags.REQHEROTRIGGER))
                {
                    if (dt.GetPos() != GameMasterScript.heroPCActor.GetPos())
                    {
                        continue;
                    }
                    else
                    {
                        if (!GameMasterScript.heroPCActor.movedLastTurn)
                        {
                            continue;
                        }
                    }
                }

                if (dt.dtStatusEffect.CheckRunTriggerOn(StatusTrigger.ENTERTILE))
                {
                    bool valid = false;
                    if (MapMasterScript.GetTile(dt.GetPos()).GetAllTargetable().Count > 0)
                    {
                        valid = true;
                    }
                    if (!valid)
                    {
                        for (int w = 0; w < dt.dtStatusEffect.listEffectScripts.Count; w++)
                        {
                            if (dt.dtStatusEffect.listEffectScripts[w].tActorType == TargetActorType.ADJACENT)
                            {
                                valid = true;
                                break;
                            }
                        }
                    }

                    float waitTime;

                    if (valid && dt.CheckStepTriggerCondition())
                    {
                        bool dummy = false; // not relevant here as we're not running this in the turn thread
                        waitTime = gmsSingleton.RunTileEffect(dt, null, 0, out dummy);

                        if (waitTime >= 0.0f)
                        {
                            GameMasterScript.combatManager.ProcessQueuedEffects();
                            GameMasterScript.combatManager.ProcessQueuedText();
                            if (dt.destroyOnStep) // Why was this commented out be fore?
                            {
                                dt.turnsToDisappear = 0;
                                GameMasterScript.AddToDeadQueue(dt);
                            }
                        }
                    }
                }
            }
        }
    }

    // This is used to check for powerups that spawned during the hero's turn, ON the hero. 
    // We can supply another position if we want to trigger powerups the hero isn't standing on.
    public static void CheckForPowerupsInHeroTile(Vector2 searchPosition)
    {
        if (!MapMasterScript.InBounds(searchPosition))
        {
            return;
        }
        MapTileData heroTile = MapMasterScript.GetTile(searchPosition);
        if (heroTile == null)
        {
            return;
        }
        pool_powerups = heroTile.GetPowerupsInTile();
        int countOfPowerups = pool_powerups.Count;

        for (int x = countOfPowerups-1; x >= 0; x--)
        {
            if (x >= pool_powerups.Count || x < 0) break; // double check that our length isnt desynced somehow?
            Destructible dt = pool_powerups[x] as Destructible;
            if (dt == null) continue;
            if (dt.destroyed) continue;
            if (dt.mapObjType != SpecialMapObject.POWERUP) continue;
            if (!dt.CheckIfCanUseStatus()) continue;
            if (dt.isInDeadQueue) continue;
            StatusEffect se = dt.dtStatusEffect;
            if (se == null)
            {
                continue;
            }
            foreach (EffectScript eff in se.listEffectScripts)
            {
                eff.targetActors.Clear();
                eff.targetActors.Add(GameMasterScript.heroPCActor);
                eff.centerPosition = GameMasterScript.heroPCActor.GetPos();
                eff.positions.Clear();
                eff.positions.Add(GameMasterScript.heroPCActor.GetPos());
                eff.DoEffect();
            }
            if (dt.destroyOnStep)
            {
                GameMasterScript.AddToDeadQueue(dt);
            }
        }
    }

    // If player has entered an Item Dream crystal aura, after NOT being in one previous turn,
    // Pop up some BattleText that explains aura effect (and write in game log)
    public static void CheckForItemDreamAuraNotification()
    {
        if (!MapMasterScript.activeMap.IsItemWorld()) return;
        if (!MapMasterScript.InBounds(GameMasterScript.heroPCActor.GetPos()))
        {
            return;
        }
        int iAura = MapMasterScript.GetItemWorldAura(GameMasterScript.heroPCActor.GetPos());
        if (iAura == -1) return;
        if (!MapMasterScript.InBounds(GameMasterScript.heroPCActor.previousPosition))
        {
            return;
        }
        int iAuraPrevious = MapMasterScript.GetItemWorldAura(GameMasterScript.heroPCActor.previousPosition);
        if (iAura != iAuraPrevious)
        {
            string auraDesc = EffectScript.itemWorldAuraDescriptions[iAura];
            BattleTextManager.NewText(auraDesc, GameMasterScript.heroPCActor.GetObject(), EffectScript.itemWorldAuraColors[iAura], 1.5f, 0.75f, BounceTypes.STANDARD, true);
            StringManager.SetTag(0, auraDesc);
            GameLogScript.LogWriteStringRef("misc_special_aura");
        }

    }

    public static void SetDestructibleStateToDestroyed(Destructible dt)
    {
        GameObject dgo = dt.GetObject();
        dt.isDestroyed = true;
        GameObject destroyedGO = GameMasterScript.TDInstantiate(dt.destroyedPrefab);

        if (dgo != null)
        {
            destroyedGO.transform.position = new Vector3(dgo.transform.position.x, dgo.transform.position.y, dgo.transform.position.z);
        }
        else
        {
            destroyedGO.transform.position = dt.GetPos();
        }

        if (dt.myMovable != null)
        {
            dt.myMovable.SetPosition(dt.GetPos());
        }
        MapMasterScript.singletonMMS.activeNonTileGameObjects.Add(destroyedGO); // This is bad

        if (dgo != null)
        {
            GameMasterScript.ReturnActorObjectToStack(dt, dgo);
            MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(dgo); // This is bad.
        }

        dt.SetObject(destroyedGO);
        dt.playerCollidable = false;
        dt.monsterCollidable = false;

        MapTileData mtd = MapMasterScript.GetTile(dt.GetPos());

        if (dt.mapObjType != SpecialMapObject.COUNT)
        {
            mtd.specialMapObjectsInTile[(int)dt.mapObjType] = false;
        }

        MapMasterScript.GetTile(dt.GetPos()).UpdateCollidableState();
        MapMasterScript.GetTile(dt.GetPos()).UpdateVisionBlockingState();
    }

    public static bool CheckForAndOpenConversationInInteractedTile(MapTileData checkTile, bool npcInSameTileAsPlayer = false)
    {
        if (UIManagerScript.GetFadeState() == EFadeStates.FADING_OUT) // maybe we're switching scenes? don't allow convo
        {
            return false;
        }
        NPC check = checkTile.GetInteractableNPC();
        if (check != null && (!check.noBumpToTalk || npcInSameTileAsPlayer)) // If it's "no bump", then we can't talk to the NPC without hitting Confirm
        {
            Conversation convo = check.GetConversation();
            if (convo != null)
            {
                UIManagerScript.StartConversation(convo, DialogType.STANDARD, check);
            }
            else
            {
                Debug.Log("No conversation to start, but there should be.");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// This is a bad pre-release kludge to remove ghosts from immediately around the hero
    /// </summary>
    public static void CleanupGhostsAroundHero()
    {        
        CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 1, MapMasterScript.activeMap);
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            if (CustomAlgorithms.tileBuffer[i].tileType != TileTypes.GROUND) continue;
            Actor toMove = null;
            bool foundSomethingToMove = false;
            foreach (Actor act in CustomAlgorithms.tileBuffer[i].GetAllActors())
            {
                if (act.GetPos() != CustomAlgorithms.tileBuffer[i].pos)
                {
                    toMove = act;
                    foundSomethingToMove = true;
                    break;
                }
            }
            if (foundSomethingToMove)
            {
                CustomAlgorithms.tileBuffer[i].RemoveActor(toMove);
            }
        }
    }

    private static void CheckForGoldPickupEvents()
    {
        if (!GameMasterScript.heroPCActor.myStats.CheckHasStatusName("goldpickup_power")) return;

        GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("goldpower_boost", GameMasterScript.heroPCActor, 10);
    }
}
