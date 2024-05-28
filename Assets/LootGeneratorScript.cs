using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class LootGeneratorScript : MonoBehaviour {
    
    // Spawns items when a creature dies or a destructible (like a chest) is broken.
    public static void TryGenerateLoot(Actor monster, Vector2 position, bool throwLoot = false)
    {
        if (monster.GetActorType() == ActorTypes.MONSTER && monster.ReadActorData("illusion") == 1) return;

        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        bool mysteryDungeonCheck = (MapMasterScript.activeMap.IsMysteryDungeonMap() && MysteryDungeonManager.CanGetSeedsOrOrbs()) || !MapMasterScript.activeMap.IsMysteryDungeonMap();
        bool canMonstersDropItems = monster.GetActorType() == ActorTypes.DESTRUCTIBLE || MysteryDungeonManager.CanMonstersDropItems();
        
        // Always drop all items in inventory.        
        // Consider dropping an orb.
        bool sharaMode = DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.SHARA;

        CheckForAndAddItemWorldOrbAsLoot(monster, sharaMode, mysteryDungeonCheck);

        Monster sMon = monster as Monster;

        CheckForAndAddExtraSharaModeLoot(monster, sMon, sharaMode);

        CheckForAndAddExtraMysteryDungeonConsumables(monster, sMon);

        CheckForExtraSeasonalDrops(monster, sMon);

        CheckForAndAddSeedsAndBossItems(monster, sMon, sharaMode, mysteryDungeonCheck, canMonstersDropItems);

        bool pandoraBox = false;
        if (monster.GetActorType() == ActorTypes.DESTRUCTIBLE)
        {
            Destructible dt = monster as Destructible;
            if (dt.mapObjType == SpecialMapObject.MONSTERSPAWNER)
            {
                pandoraBox = true;
            }
            if (UnityEngine.Random.Range(0, 1f) <= dt.moneyChance && dt.moneyChance > 0f)
            {
                // Spawn money from destructible object

                float formula = Mathf.Pow(MapMasterScript.activeMap.GetChallengeRating() + .1f, 5f) * 30f;
                int finalAmount = (int)(UnityEngine.Random.Range(formula * 0.8f, formula * 1.1f));
                Destructible coins = MapMasterScript.activeMap.CreateDestructibleInTile(MapMasterScript.GetTile(dt.GetPos()), "obj_coins");
                coins.AddMoney(finalAmount);
                // Spawn coins
                GameMasterScript.mms.SpawnDestructible(coins);
                CombatManagerScript.FireProjectile(dt.GetPos(), coins.GetPos(), dt.GetObject(), 0.25f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);
            }
        }

        float pandoraBonusMagicChance = GameMasterScript.heroPCActor.numPandoraBoxesOpened * GameMasterScript.PANDORA_BONUS_MAGICCHANCE;

        if (pandoraBonusMagicChance >= GameMasterScript.PANDORA_BONUS_MAGICCHANCE_CAP)
        {
            pandoraBonusMagicChance = GameMasterScript.PANDORA_BONUS_MAGICCHANCE_CAP;
        }
        
        if (UnityEngine.Random.Range(0,1f) <= pandoraBonusMagicChance/2f && canMonstersDropItems)
        {
            Item randomItem = LootGeneratorScript.GenerateLoot(MapMasterScript.activeMap.challengeRating, pandoraBonusMagicChance);
            monster.myInventory.AddItemRemoveFromPrevCollection(randomItem, false);
        }

        DropItemsFromInventory(monster, canMonstersDropItems, sharaMode, position, throwLoot, pandoraBox, pandoraBonusMagicChance);

        if (monster.GetActorType() != ActorTypes.MONSTER)
        {
            return;
        }

        SpawnPowerupsIfPossible(monster);

    }

    public static void CheckForExtraSeasonalDrops(Actor act, Monster mon)
    {
        if (act.GetActorType() != ActorTypes.MONSTER) return;
        if (!mon.isChampion && !mon.isBoss)
        {
            return;
        }

        if (!GameMasterScript.seasonsActive[(int)Seasons.LUNAR_NEW_YEAR]) return;

        if (UnityEngine.Random.Range(0,60) == 0)
        {
            Item leg = LootGeneratorScript.GenerateLootFromTable(mon.challengeValue, 0f, "lunarnewyear_legendary", 1f);
            mon.myInventory.AddItemRemoveFromPrevCollection(leg, true);
        }

        if (UnityEngine.Random.Range(0,1f) > 0.42f) return;

        Item i = LootGeneratorScript.GenerateLootFromTable(1.0f, 0f, "lunarnewyear_consumables", 1f);
        mon.myInventory.AddItemRemoveFromPrevCollection(i, true);        

    }

    public static Item GenerateLootFromTable(float challengeValue, float magicLootModifier, string specificTable, float minimumCV = 1.0f)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        // Loot is to be generated! Pick from all possible items.
        GameMasterScript.possibleItems.Clear();

        string tableToUse = "";

        if (specificTable != "")
        {
            tableToUse = specificTable;
        }
        else
        {
            tableToUse = GameMasterScript.tableOfLootTables.GetRandomActorRef();
        }

        // For mystery dungeons, magic item chance is a bit higher since we don't have Item Dreams etc.
        if (MysteryDungeonManager.InOrCreatingMysteryDungeon() && !MysteryDungeonManager.GetActiveDungeon().resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR])
        {
            magicLootModifier += 0.25f;
        }

        if (SharaModeStuff.IsSharaModeActive())
        {
            magicLootModifier += SharaModeStuff.CHANCE_MAGIC_GEAR_BONUS;
        }

        // Ruby moon: higher chance for legendaries
        if (UnityEngine.Random.Range(0, 1f) <= 0.05f && GameMasterScript.gameLoadSequenceCompleted && tableToUse != "legendary" 
            && heroPCActor.myStats.CheckHasStatusName("rubymoon"))
        {
            tableToUse = GameMasterScript.tableOfLootTables.GetRandomActorRef();
        }

        if (challengeValue < 1.0f)
        {
            challengeValue = 1.0f;
        }

        if (challengeValue > GetMaxChallengeValueForItems())
        {
            challengeValue = GetMaxChallengeValueForItems();
        }

        challengeValue = (float)Math.Round(challengeValue, 1);

        ActorTable lootTable = GetLootTable(tableToUse);

        bool usingLegendaryTable = lootTable.refName == "legendary";

        // De-prioritize legendaries at lower levels
        if (challengeValue <= 1.3 && usingLegendaryTable && UnityEngine.Random.Range(0,1f) <= 0.9f)
        {
            //Shep: did this code do anything? 
            //lootTable = GetLootTable(tableToUse);
            lootTable = GetLootTable("consumables");

        }

        // De-prioritize food at higher levels
        if (challengeValue >= 1.4f && UnityEngine.Random.Range(0,1f) <= 0.5f && lootTable.refName == "food")
        {
            //Shep: did this code do anything? 
            //lootTable = GetLootTable(tableToUse);
            tableToUse = GameMasterScript.tableOfLootTables.GetRandomActorRef();
            lootTable = GetLootTable(tableToUse);
        }

        usingLegendaryTable = tableToUse == "legendary";

        bool makingRune = false;

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
            && GameMasterScript.gameLoadSequenceCompleted
            && UnityEngine.Random.Range(0, 1f) <= GameMasterScript.DLC1_CHANCE_RELIC_SUB_FOR_LEGENDARY
            && usingLegendaryTable
            && MapMasterScript.activeMap != null
            && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {

            // Maybe do a rune instead?
            if (UnityEngine.Random.Range(0, 1f) <= (MysteryDungeonManager.CHANCE_RUNE_INSTEAD_OF_LEGENDARY * 1.5f))
            {
                tableToUse = "runeteachskills";
                lootTable = GameMasterScript.masterLootTables["runeteachskills"];
                makingRune = true;
                //Debug.LogError("We're gonna spawn a rune!!!");
            }
            else
            {
                Item customLeg = LegendaryMaker.CreateNewLegendaryItem(challengeValue);

                customLeg.CalculateShopPrice(1.00f, false);
                customLeg.CalculateSalePrice();
                return customLeg;
            }
        }        

        if (usingLegendaryTable && !makingRune && MysteryDungeonManager.InOrCreatingMysteryDungeon() && UnityEngine.Random.Range(0,1f) <= MysteryDungeonManager.CHANCE_RUNE_INSTEAD_OF_LEGENDARY)
        {
            tableToUse = "runeteachskills";
            lootTable = GameMasterScript.masterLootTables["runeteachskills"];
            //Debug.LogError("We're gonna spawn a rune!!!");
        }

        ActorTable customTable = new ActorTable();
        customTable.refName = "GenTable" + challengeValue;

        bool bAllowRosepetals = ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) >= 1;

        foreach (Actor at in lootTable.actors)
        {
            if (at.actorRefName == "spice_rosepetals" && !bAllowRosepetals)
            {
                continue;
            }

            /* if ((lootTable.refName == "allitems" || lootTable.refName == "consumables"))
            {
                Debug.Log("Consider: " + lootTable.refName + " " + at.actorRefName);
            } */

            Item itm = at as Item;

            // Prevent NG+ only items from spawning if you don't meet the requirement of course
            if (itm.reqNewGamePlusLevel > GameStartData.NewGamePlus)
            {
                continue;
            }

            if ((SharaModeStuff.IsSharaModeActive() && SharaModeStuff.disallowSharaModeItems.Contains(itm.actorRefName)) ||
                (RandomJobMode.IsCurrentGameInRandomJobMode() && RandomJobMode.disallowRandomJobModeItems.Contains(itm.actorRefName)))
            {
                continue;
            }

            // Say our table is cv 1.3, and item is 1.3
            if (itm.challengeValue <= challengeValue)
            {
                float percentOfMax = itm.challengeValue / challengeValue;

                if (itm.challengeValue < minimumCV)
                {
                    percentOfMax = 0f;
                }
                else if (itm.itemType == ItemTypes.CONSUMABLE)
                {
                    percentOfMax = 1.0f;
                }
                else
                {
                    bool sharaModeActive = SharaModeStuff.IsSharaModeActive();

                    bool subParItem = false;

                    // Let's say item is cv 1.0, and table cv is 1.4
                    // item cv is less than 1.3, so reduce drop chance
                    if (itm.challengeValue >= challengeValue - 0.3f && itm.challengeValue <= challengeValue - 0.1f)
                    {
                        percentOfMax *= sharaModeActive ? 0.4f : 0.7f;
                        subParItem = true;
                    }

                    // If it's really bad, reduce the chance further
                    if (itm.challengeValue >= challengeValue - 0.6f && itm.challengeValue < challengeValue - 0.3f)
                    {
                        percentOfMax *= sharaModeActive ? 0.2f : 0.5f;
                        subParItem = true;

                    }
                    else if (itm.challengeValue < challengeValue - 0.6f)
                    {
                        percentOfMax *= sharaModeActive ? 0.1f : 0.3f;
                        subParItem = true;
                    }
                    else if (CustomAlgorithms.CompareFloats(itm.challengeValue,challengeValue)) // equal level
                    {
                        // This was previously 0.1f. What I didn't realize was that this case runs if the item is equal level to the table
                        // So by going to 0.2f in theory we will be dropping more/better stuff.
                        percentOfMax *= 0.2f;
                        for (int i = 0; i < GameStartData.NewGamePlus; i++)
                        {
                            percentOfMax *= 1.1f;
                        }
                    }

                    if (subParItem)
                    {
                        for (int i = 0; i < GameStartData.NewGamePlus; i++)
                        {
                            percentOfMax *= 0.75f;
                        }
                    }
                }

                if (percentOfMax == 0)
                {
                    continue;
                }

                int curAmount = 0;
                try { curAmount = lootTable.table[at.actorRefName]; }    //#questionable_try_block
                catch (Exception e)
                {
                    Debug.LogWarning("Couldn't find actor ref " + at.actorRefName + " in loot table " + lootTable.refName + ": " + e.ToString());
                }
                int finalAmount = (int)((float)(percentOfMax * curAmount));
                if (challengeValue >= 1.6f)
                {
                    //Debug.Log(at.actorRefName + " cv " + itm.challengeValue + " compare to check value of " + challengeValue + " P o f max: " + percentOfMax + " Add " + finalAmount + " instead of " + curAmount);
                }

                customTable.AddToTable(at.actorRefName, finalAmount);
            }
        }

        // This is because legendaries don't exist at all CV yet

        bool isLegendary = false;
        if (tableToUse == "legendary")
        {
            isLegendary = true;
        }        

        if (customTable.GetNumActors() == 0)
        {
            isLegendary = false;
            // Probably legendary, so do a consumable instead.
            lootTable = GetLootTable("consumables");
            customTable = new ActorTable();
            customTable.refName = "GenTable" + challengeValue;
            foreach (Actor at in lootTable.actors)
            {
                Item itm = at as Item;
                if (itm.challengeValue <= challengeValue)
                {
                    float percentOfMax = itm.challengeValue / challengeValue;

                    if (itm.itemType == ItemTypes.CONSUMABLE)
                    {
                        percentOfMax = 1.0f;
                    }

                    int curAmount = lootTable.table[at.actorRefName];
                    int finalAmount = (int)((float)(percentOfMax * curAmount));
                    customTable.AddToTable(at.actorRefName, finalAmount);
                }
            }
        }
        // End legendary / 0 actor protection block

        string itemRefToUse = customTable.GetRandomActorRef();

        Item template = Item.GetItemTemplateFromRef(itemRefToUse);
       
        while (!template.ValidForPlayer())
        {
            customTable = LootGeneratorScript.GetLootTable("allitems");
            itemRefToUse = customTable.GetRandomActorRef();
            template = Item.GetItemTemplateFromRef(itemRefToUse);
        }

        if (GameMasterScript.gameLoadSequenceCompleted && DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            while (DungeonMaker.disallowedItemsInMysteryDungeons.Contains(itemRefToUse))
            {
                customTable = LootGeneratorScript.GetLootTable("allitems");
                itemRefToUse = customTable.GetRandomActorRef();
                template = Item.GetItemTemplateFromRef(itemRefToUse);
            }
        }

        Item itemToSpawn = CreateItem(template, challengeValue, magicLootModifier, true); // Item is the template. Can make magical.

        itemToSpawn.CalculateShopPrice(1.00f, false);
        itemToSpawn.CalculateSalePrice();
        return itemToSpawn;
    }

    public static Item GenerateLoot(float challengeValue, float magicLootModifier, int floor = -1)
    {
        Item re = GenerateLootFromTable(challengeValue, magicLootModifier, "");

        /* if (floor == 0 || floor == 20)
        {
            Debug.Log(challengeValue + " " + floor + " " + re.actorRefName + " " + re.challengeValue);
        } */
        return re;
    }

    public static Item CreateItemFromTemplateRef(string templateRef, float challengeValue, float magicLootModifier, bool canMakeMagical, bool rebuildDisplayNameImmediately = true)
    {
        Item itemTemplate = GameMasterScript.GetItemFromRef(templateRef);
        if (itemTemplate != null)
        {
            return CreateItem(itemTemplate, challengeValue, magicLootModifier, canMakeMagical, rebuildDisplayNameImmediately);
        }
        return null;
    }

    public static Item CreateItem(Item template, float challengeValue, float magicLootModifier, bool canMakeMagical, bool rebuildDisplayNameImmediately = true)
    {
        if (template.actorRefName == "item_escapedungeon")
        {
            Debug.LogError("ESCAPE TORCH WAS CREATED");
        }

        // item is the template for the new thing.
        Item itemToSpawn = new Item();
        bool typeSelected = false;
        itemToSpawn.CopyFromItem(template);
        if (template.itemType == ItemTypes.WEAPON)
        {
            typeSelected = true;
            Weapon weaponTemplate = template as Weapon;
            Weapon weaponToSpawn = new Weapon();
            weaponToSpawn.CopyFromItem(weaponTemplate);
            int maxDur = weaponTemplate.maxDurability;
            int curDur = UnityEngine.Random.Range(maxDur / 2, maxDur + 1);
            weaponToSpawn.SetDurability(curDur);
            itemToSpawn = weaponToSpawn;
        }
        else if (template.itemType == ItemTypes.ARMOR)
        {
            typeSelected = true;
            Armor armorTemplate = template as Armor;
            Armor armorToSpawn = new Armor();
            armorToSpawn.CopyFromItem(armorTemplate);
            itemToSpawn = armorToSpawn;
        }
        else if (template.itemType == ItemTypes.OFFHAND)
        {
            typeSelected = true;
            Offhand offTemplate = template as Offhand;
            Offhand offToSpawn = new Offhand();
            offToSpawn.CopyFromItem(offTemplate);
            itemToSpawn = offToSpawn;
        }
        else if (template.itemType == ItemTypes.ACCESSORY)
        {
            typeSelected = true;
            Accessory accTemplate = template as Accessory;
            Accessory accToSpawn = new Accessory();
            accToSpawn.CopyFromItem(accTemplate);
            itemToSpawn = accToSpawn;
        }
        else if (template.itemType == ItemTypes.EMBLEM)
        {
            typeSelected = true;
            Emblem eTemplate = template as Emblem;
            Emblem eToSpawn = new Emblem();
            eToSpawn.CopyFromItem(eTemplate);
            itemToSpawn = eToSpawn;
        }
        else if (template.itemType == ItemTypes.CONSUMABLE)
        {
            typeSelected = true;
            Consumable consumeTemplate = template as Consumable;
            Consumable consumeToSpawn = new Consumable();
            consumeToSpawn.CopyFromItem(consumeTemplate);
            itemToSpawn = consumeToSpawn;
        }
        if (!typeSelected)
        {
            Debug.Log("WARNING: Item " + template.actorRefName + " no type assigned? " + template.itemType);
        }
        else
        {
            //Debug.Log("Creating item " + template.actorRefName + ", item to create is type " + itemToSpawn.itemType + " " + itemToSpawn.GetType().ToString());
        }

        float magicChance = GameMasterScript.gmsSingleton.globalMagicItemChance;

        if (GameMasterScript.playerIsScavenger)
        {
            magicChance += GameMasterScript.SCAVENGER_BONUS_MAGIC_CHANCE;
        }

        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            magicChance += MysteryDungeonManager.EXTRA_MAGIC_ITEM_CHANCE; // Mystery Dungeons have more magical items, simple as that.
        }

        if (GameMasterScript.heroPCActor != null) 
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("rubymoon"))
            {
                magicChance += GameMasterScript.RUBYMOON_BONUS_MAGIC_CHANCE;
            }
            magicChance += GameMasterScript.heroPCActor.advStats[(int)AdventureStats.MAGICFIND];            
        }

        magicChance *= magicLootModifier;

        if (UnityEngine.Random.Range(0, 1f) <= magicChance && canMakeMagical && CheckMagicSpawnChance(0, challengeValue))
        {
            // This will drop an uncommon item
            EquipmentBlock.MakeMagical(itemToSpawn, challengeValue, false);
            if (UnityEngine.Random.Range(0f, 1.1f) <= magicChance && CheckMagicSpawnChance(1, challengeValue))
            {
                // Magical item!
                EquipmentBlock.MakeMagical(itemToSpawn, challengeValue, false);
                if (UnityEngine.Random.Range(0f, 1.2f) <= magicChance && CheckMagicSpawnChance(2, challengeValue))
                {
                    // Artifact item!
                    EquipmentBlock.MakeMagical(itemToSpawn, challengeValue, false);
                }
            }
        }

        if (GameStartData.NewGamePlus == 2 && UnityEngine.Random.Range(0,1) < GameStartData.NGPLUSPLUS_CHANCE_UPGRADE_COMMON && itemToSpawn.rarity <= Rarity.UNCOMMON)
        {
            EquipmentBlock.MakeMagical(itemToSpawn, challengeValue, false);
        }

        itemToSpawn.SetUniqueIDAndAddToDict();
        if (rebuildDisplayNameImmediately)
        {
            itemToSpawn.RebuildDisplayName(); // new 11/9/17
        }      
        
        return itemToSpawn;
    }

    public static bool CheckMagicSpawnChance(int numExistingMods, float challengeValue)
    {
        if (numExistingMods >= 1 && challengeValue <= 1.25f)
        {
            return false;
        }
        if (numExistingMods >= 2 && challengeValue <= 1.35f)
        {
            return false;
        }
        return true;
    }

    public static ActorTable GetLootTable(string refName)
    {
        ActorTable returnValue;
        if (GameMasterScript.masterLootTables.TryGetValue(refName, out returnValue))
        {
            return returnValue;
        }
        Debug.Log("Couldn't find loot table " + refName);
        return null;
    }

    public static Destructible SpawnRandomPowerup(Monster mon, bool herbPowerup, bool forceAllowHealth = false)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;
        Vector2 position = mon.GetPos();
        float eChance = 0.5f;

        if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) < heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.ENERGY))
        {
            // More chance of stamina powerup.
            eChance = 0.35f;
            if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.ENERGY) == 1.0f)
            {
                eChance = -0.5f;
            }
        }
        else if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) > heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.ENERGY))
        {
            // More chance of energy.
            eChance = 0.65f;
            if (heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) == 1.0f)
            {
                eChance = 1.0f;
            }
        }


        if (heroPCActor.myStats.CheckHasStatusName("status_manaseeker"))
        {
            eChance = 1.0f;
        }

        Destructible pu = null;

       position = MapMasterScript.GetRandomEmptyTile(position, 1, true, true, true, true, true).pos;

        if (UnityEngine.Random.Range(0,1f) <= GameMasterScript.TREASURETRACKER_POWERUP_CONVERTCHANCE && heroPCActor.myStats.CheckHasStatusName("treasuretracker"))
        {
            int baseNumCoins = UnityEngine.Random.Range(8, 13);
            float exponent = 0.2f + mon.challengeValue;
            int coinsToGive = (int)Mathf.Pow(baseNumCoins, exponent);
            Destructible coinz = MapMasterScript.SpawnCoins(MapMasterScript.GetTile(mon.GetPos()), MapMasterScript.GetTile(position), coinsToGive);
            return coinz;
        }

        string[] possibleRefs = new string[3];

        int summonLength = GameMasterScript.GetPowerupLength();

        if (!herbPowerup)
        {
            possibleRefs[0] = "powerup_health";
            possibleRefs[1] = "powerup_energy";
            possibleRefs[2] = "powerup_stamina";
        }
        else
        {
            possibleRefs[0] = "powerup_healthherb";
            possibleRefs[1] = "powerup_energyherb";
            possibleRefs[2] = "powerup_staminaherb";
            summonLength = 4;
            forceAllowHealth = true;
            if (heroPCActor.myStats.CheckHasStatusName("emblem_wildchildemblem_tier1_forage"))
            {
                summonLength = 6;
            }
        }

        bool hasFindHealth = heroPCActor.myStats.CheckHasStatusName("findhealth");
        float hRoll = UnityEngine.Random.Range(0, 1f);

        if (hasFindHealth && hRoll <= 0.2f || (forceAllowHealth && UnityEngine.Random.Range(0,1f) <= 0.33f))
        {
            pu = GameMasterScript.SummonDestructible(heroPCActor, Destructible.FindTemplate(possibleRefs[0]), position, summonLength);
        }
        else
        {
            if (UnityEngine.Random.Range(0, 1f) <= eChance)
            {
                pu = GameMasterScript.SummonDestructible(heroPCActor, Destructible.FindTemplate(possibleRefs[1]), position, summonLength);
            }
            else
            {
                pu = GameMasterScript.SummonDestructible(heroPCActor, Destructible.FindTemplate(possibleRefs[2]), position, summonLength);
            }
        }

        //Debug.Log("Summoned destructible " + pu.actorRefName + " " + pu.GetPos());

        pu.anchorRange = 10;
        pu.anchor = pu;
        pu.anchorID = pu.actorUniqueID;
        pu.myAnimatable.SetAnim("Idle");
        pu.actorfaction = Faction.PLAYER;

        if (!herbPowerup)
        {
            pu.monsterAttached = mon.actorRefName;
        }
        

        if (!pu.objectSet)
        {
            MapMasterScript.singletonMMS.SpawnDestructible(pu);
        }

        CombatManagerScript.FireProjectile(mon.GetPos(), pu.GetPos(), pu.GetObject(), 0.25f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 0f, false);

        return pu;
    }

    public static void DropItemOnGround(Item thingToDrop, Vector2 pos, int quantity)
    {
        StringManager.SetTag(0, thingToDrop.displayName);
        //Debug.Log("Drop " + thingToDrop.displayName + " " + quantity);
        if (quantity > 1)
        {
            StringManager.SetTag(1, " (" + quantity + ") ");
            Consumable c = thingToDrop as Consumable;
            c.Quantity = quantity;
        }
        else
        {
            StringManager.SetTag(1, " ");
        }

        GameMasterScript.heroPCActor.OnItemSoldOrDropped(thingToDrop, soldItem: false);

        GameLogScript.LogWriteStringRef("log_playerdropitem");
        UIManagerScript.PlayCursorSound("Pickup");
        thingToDrop.SetSpawnPosXY((int)pos.x, (int)pos.y);
        GameMasterScript.SpawnItemAtPosition(thingToDrop, pos);
        thingToDrop.collection = null;
    }

    public static void RemoveLegendaryFromEverything(Item loot)
    {
        GetLootTable("legendary").RemoveFromTable(loot.actorRefName);
        GameMasterScript.heroPCActor.SetActorData("legfound_" + loot.actorRefName, 1);
        GameMasterScript.heroPCActor.myQuests.RemoveAll(a => !a.ItemsInvolvedStillValid());
        GameMasterScript.heroPCActor.SwapInvalidItemsInQuests();
    }

    public static void AutoAddPlayerModItemsToTables()
    {
        List<string> addToTheseTables = new List<string>();
        foreach (Item itm in GameMasterScript.masterItemList.Values)
        {
            if (itm.forceAddToLootTablesAtRate <= 0) continue;

            addToTheseTables.Clear();
            if (itm.legendary)
            {
                addToTheseTables.Add("legendary");
                LootGeneratorScript.GetLootTable("legendary").AddToTable(itm.actorRefName, itm.forceAddToLootTablesAtRate);
            }
            else
            {
                addToTheseTables.Add("allitems");
                if (itm.IsEquipment())
                {
                    addToTheseTables.Add("equipment");
                    switch (itm.itemType)
                    {
                        case ItemTypes.WEAPON:
                            addToTheseTables.Add("weapons");
                            break;
                        case ItemTypes.OFFHAND:
                        case ItemTypes.ARMOR:
                            addToTheseTables.Add("armor");
                            break;
                        case ItemTypes.ACCESSORY:
                            addToTheseTables.Add("accessories");
                            break;
                    }
                }
                else
                {
                    if (itm.itemType == ItemTypes.CONSUMABLE)
                    {
                        addToTheseTables.Add("consumables");
                        Consumable c = itm as Consumable;
                        if (c.isHealingFood && !c.isFood)
                        {
                            addToTheseTables.Add("restoratives");
                            addToTheseTables.Add("potions");
                        }
                        else if (c.isFood)
                        {
                            addToTheseTables.Add("food_and_meals");
                            addToTheseTables.Add("food");
                            addToTheseTables.Add("monster_food_loving");
                        }
                    }
                }
            }
            
            // OK! Now add to tables.
            foreach(string table in addToTheseTables)
            {
                GetLootTable(table).AddToTable(itm.actorRefName, itm.forceAddToLootTablesAtRate);
                GetLootTable(table).actors.Add(itm);
            }        
        }
    }

    public static void MakeSeedsMagicalIfPossible(Item seeds)
    {
        if (UnityEngine.Random.Range(0, 1f) <= (GameMasterScript.gmsSingleton.globalMagicItemChance + 0.15f))
        {
            seeds.rarity = Rarity.UNCOMMON;
            if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.gmsSingleton.globalMagicItemChance + 0.1f)
            {
                seeds.rarity = Rarity.MAGICAL;
            }
        }
    }

    static void CheckForAndAddItemWorldOrbAsLoot(Actor monster, bool sharaMode, bool mysteryDungeonCheck)
    {
        // Don't drop orbs if we're bringing in our original gear, and can't take out items.
        if (MysteryDungeonManager.InOrCreatingMysteryDungeon() 
            && MysteryDungeonManager.GetActiveDungeon().resourcesAtStart[(int)EMysteryDungeonPlayerResources.GEAR] 
            && MysteryDungeonManager.GetActiveDungeon().resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.ITEMS])
        {
            return;
        } 

        if (monster.GetActorType() == ActorTypes.MONSTER && !monster.GetActorMap().IsItemWorld() && !sharaMode && !monster.GetActorMap().IsJobTrialFloor())
        {
            Monster aMn = monster as Monster;
            if ((aMn.isChampion || aMn.isBoss) && !aMn.surpressTraits)
            {
                float baseChance = GameMasterScript.gmsSingleton.itemWorldOrbDropChance;

                baseChance *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.ORB_DROP_RATE);

                if (MysteryDungeonManager.InOrCreatingMysteryDungeon())
                {
                    baseChance *= 1.25f;
                }

                if (RandomJobMode.IsCurrentGameInRandomJobMode()) baseChance += RandomJobMode.GetRJBonusReverieChance();

                if (aMn.isBoss && aMn.myTemplate.showBossHealthBar) baseChance *= 2f;
                if (GameStartData.NewGamePlus > 0)
                {
                    baseChance += 0.1f;
                    if (GameStartData.NewGamePlus == 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
                    {
                        baseChance *= 2f;
                    }
                }

                baseChance *= aMn.GetXPModToPlayer();

                if (aMn.myTemplate.showBossHealthBar)
                {
                    baseChance = 500f;
                }
                if (aMn.actorRefName == "mon_banditwarlord" ||
                    (UnityEngine.Random.Range(0, 1f) <= baseChance && ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) >= 1))
                {
                    Consumable newOrb = null;

                    if (aMn.dungeonFloor >= 10 && aMn.myTemplate.showBossHealthBar)
                    {
                        newOrb = ItemWorldUIScript.CreateItemWorldOrb(aMn.challengeValue, true, true);
                    }
                    else
                    {
                        newOrb = ItemWorldUIScript.CreateItemWorldOrb(aMn.challengeValue, false);
                    }

                    monster.myInventory.AddItem(newOrb, false);
                }
            }
        }
    }

    static void CheckForAndAddExtraSharaModeLoot(Actor monster, Monster sMon, bool sharaMode)
    {
        if (sharaMode && monster.GetActorType() == ActorTypes.MONSTER)
        {
            int bonusFoodItems = 0;
            if (UnityEngine.Random.Range(0, 1f) <= SharaModeStuff.CHANCE_EXTRA_FOOD_FROM_MONSTERS && sMon.GetXPModToPlayer() >= 0.15f) // flat 5% chance of just getting food as long as the monster isn't worthless
            {
                bonusFoodItems++;
            }
            if (sMon.isChampion)
            {
                bonusFoodItems = UnityEngine.Random.Range(1, 3);
            }
            if (sMon.isBoss)
            {
                bonusFoodItems++;
            }
            for (int i = 0; i < bonusFoodItems; i++)
            {
                Item food = GenerateLootFromTable(sMon.challengeValue, 0f, "food");
                sMon.myInventory.AddItemRemoveFromPrevCollection(food, false);
            }

        }
    }

    static void CheckForAndAddExtraMysteryDungeonConsumables(Actor monster, Monster sMon)
    {
        // In gimmicky mystery dungeons, % chance to spawn an extra consumable on whatever you beat up
        if (MysteryDungeonManager.InMysteryDungeonWithExtraConsumables())
        {
            // But for monsters, the chance scales down as you out-level the monster.
            if (monster.GetActorType() == ActorTypes.DESTRUCTIBLE || monster.GetActorType() == ActorTypes.MONSTER && UnityEngine.Random.Range(0, 1f) <= sMon.GetXPModToPlayer())
            {
                Item nItem = MysteryDungeonManager.TryCreatingRandomExtraConsumable(MapMasterScript.activeMap.GetChallengeRating());
                if (nItem != null)
                {
                    monster.myInventory.AddItem(nItem, true);
                }
            }
        }
        if (monster.ReadActorData("mysteryking") == 1)
        {
            bool hasRelic = false;
            foreach(Item itm in monster.myInventory.GetInventory())
            {
                if (itm.customItemFromGenerator)
                {
                    hasRelic = true;
                    break;
                }
            }
            if (!hasRelic)
            {
                float targetCVForRelic = MapMasterScript.activeMap.dungeonLevelData.challengeValue;
                MysteryDungeon md = MysteryDungeonManager.GetActiveDungeon();
                if (!md.resourcesAtStart[(int)EMysteryDungeonPlayerResources.STATS])
                {
                    targetCVForRelic = GameMasterScript.heroPCActor.myMysteryDungeonData.statsPriorToEntry.GetLevel();
                }

                Item relic = LegendaryMaker.CreateNewLegendaryItem(targetCVForRelic);
                Debug.Log("Relic created for MK: " + relic.actorRefName);
                monster.myInventory.AddItem(relic, false);
                relic.SetActorData("grc", 1);
            }
        }
    }

    static void CheckForAndAddSeedsAndBossItems(Actor monster, Monster sMon, bool sharaMode, bool mysteryDungeonCheck, bool canMonstersDropItems)
    {
        // Consider dropping seeds and other goodies.
        if (monster.GetActorType() == ActorTypes.MONSTER && !sharaMode)
        {
            if (sMon.isBoss)
            {
                bool guaranteeLegendary =
                    (sMon.actorRefName == "mon_banditwarlord" && GameStartData.NewGamePlus > 0)
                    || sMon.actorRefName == "mon_ancientsteamgolem"
                    || sMon.actorRefName == "mon_finalboss2";

                if (!guaranteeLegendary && sMon.actorRefName == "mon_shadowelementalboss" && !sMon.surpressTraits && GameStartData.NewGamePlus > 0)
                {
                    //This section checks to see if we are fighting the mon_shadowelemental boss pair.
                    //If there's one left, and we killed it, then it should drop a legendary.
                    // https://i.imgur.com/V1gwqMY.png

                    int countShadows = 0;
                    foreach (Monster checkMon in MapMasterScript.activeMap.monstersInMap)
                    {
                        if (checkMon.actorRefName == "mon_shadowelementalboss" && !checkMon.surpressTraits) countShadows++;
                    }

                    //but wait.
                    //if the monster we killed has surpressTraits on, it doesn't count. Meaning if you kill clones,
                    //and there's only one Real Boss left, you get LOOT EVERY TIME.
                    // https://i.imgur.com/7XZZfFh.png

                    //so i feex by adding a check for sMon.surpressTraits above
                    //and now shadowclones have 0* chance of entering this code
                    if (countShadows <= 1)
                    {
                        //no
                        //guaranteeLegendary = true;
                        Item itmBanditEcho = Item.GetItemTemplateFromRef("potion_bandit_echo");
                        sMon.myInventory.AddItem(itmBanditEcho, false);
                    }
                }
                if (guaranteeLegendary)
                {
                    foreach (Item itm in sMon.myInventory.GetInventory())
                    {
                        if (itm.legendary)
                        {
                            guaranteeLegendary = false;
                            break;
                        }
                    }
                    if (guaranteeLegendary)
                    {
                        Item leg = GenerateLootFromTable(sMon.challengeValue, 0f, "legendary");
                        sMon.myInventory.AddItem(leg, false);
                    }
                }
            }

            if (!sMon.surpressTraits && sMon.xpMod != 0f && !MapMasterScript.activeMap.IsJobTrialFloor() && (sMon.isChampion || sMon.isBoss) && mysteryDungeonCheck)
            {
                float baseChance = GameMasterScript.gmsSingleton.seedsDropChance;
                if (sMon.isChampion)
                {
                    // No change
                    baseChance *= sMon.GetXPModToPlayer();
                }
                if (sMon.isBoss && sMon.myTemplate.showBossHealthBar)
                {
                    baseChance = 0.35f;
                }
                if (UnityEngine.Random.Range(0, 1f) <= baseChance && canMonstersDropItems)
                {
                    Item seeds = GenerateLootFromTable(1.0f, 0.0f, "seeds");
                    seeds.SetUniqueIDAndAddToDict();
                    MakeSeedsMagicalIfPossible(seeds);
                    seeds.RebuildDisplayName();
                    monster.myInventory.AddItem(seeds, false);
                }

                float petalsChance = (GameStartData.NewGamePlus * GameStartData.NGPLUS_ROSEPETALS_CHAMP_CHANCE);
                if (UnityEngine.Random.Range(0, 1f) <= baseChance && canMonstersDropItems && !sharaMode)
                {
                    Item petals = LootGeneratorScript.CreateItemFromTemplateRef("spice_rosepetals", 1.5f, 0f, false);
                    petals.SetUniqueIDAndAddToDict();
                    monster.myInventory.AddItem(petals, false);
                }
            }
        }
    }

    static void SpawnPowerupsIfPossible(Actor monster)
    {
        if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
        {
            return;
        }

        // If it's not a monster, no powerups.

        // Check for powerups
        float roll = UnityEngine.Random.Range(0.0f, 1.0f);

        float extraChance = 0f;
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_mmscavenging"))
        {
            extraChance = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("status_mmscavenging") * 0.04f; // Scavenging improves powerup drop chance by 4%
        }

        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("sthergebonus1"))
        {
            extraChance += 0.2f;
        }

        if (GameStartData.GetGameMode() == GameModes.ADVENTURE)
        {
            extraChance += 0.08f;
        }

        float powerupBaseChance = GameMasterScript.gmsSingleton.globalPowerupDropChance;
        if (GameStartData.NewGamePlus > 0 && MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            powerupBaseChance *= (2f * GameStartData.NewGamePlus);
            // normally in NG+ our powerup drop chance (global) is cut in half, but mystery dungeons shouldn't do this
            // so we're just accounting for that here.
        }

        float myChance = GameMasterScript.gmsSingleton.globalPowerupDropChance +
            (GameMasterScript.heroPCActor.myStats.GetCurStatAsPercent(StatTypes.GUILE) * StatBlock.GUILE_PERCENT_POWERUP_MOD) + extraChance;

        float usedResourcePercent = 1f - ((GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) + GameMasterScript.heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.ENERGY)) / 2f);
        usedResourcePercent *= 0.2f;

        Monster mn = monster as Monster;
        // Low level monsters, no powerups? Who cares?

        myChance *= mn.GetXPModToPlayer();

        int distance = MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), mn.GetPos());

        /* if (distance > 1)
        {
            float distanceFalloffModifier = 1f - (distance * 0.15f);
            myChance *= distanceFalloffModifier;
        } */

        if (mn.isChampion)
        {
            myChance = 1f;
        }
        if (mn.surpressTraits)
        {
            myChance = 0.0f;
        }

        int numPowerups = 0;

        bool finished = true;

        myChance += usedResourcePercent;

        if (roll <= myChance)
        {
            finished = false;
        }

        bool herbPossible = false;

        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_herbforaging") && !finished)
        {
            if (mn.GetXPModToPlayer() >= 0.25f && UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHANCE_HERB_FORAGE)
            {
                herbPossible = true;
            }
        }

        while (!finished)
        {
            // What kind of powerup?

            if (herbPossible)
            {
                SpawnRandomPowerup(mn, herbPowerup: true);
                herbPossible = false;
                continue;
            }

            SpawnRandomPowerup(mn, false);

            numPowerups++;

            finished = true;

            if (numPowerups == 1 && distance == 1)
            {
                myChance *= 0.33f;
                if (UnityEngine.Random.Range(0.0f, 1.0f) <= myChance)
                {
                    finished = false;
                }
            }
        }
    }

    public static void DropItemsFromInventory(Actor monster, bool canMonstersDropItems, bool sharaMode, Vector2 position, bool throwLoot, bool pandoraBox, float pandoraBonusMagicChance)
    {
        if (monster.GetActorType() == ActorTypes.MONSTER)
        {
            Monster mn = monster as Monster;
            foreach(string itmRef in mn.myTemplate.guaranteeLoot)
            {
                if (!mn.myInventory.HasItemByRef(itmRef))
                {
                    Item itm = LootGeneratorScript.CreateItemFromTemplateRef(itmRef, mn.challengeValue, 0f, false);
                    itm.AddActorData("alwaysdrop", 1);
                    mn.myInventory.AddItem(itm, false);
                }
            }            
        }

        foreach (Item itm in monster.myInventory.GetInventory())
        {            
            // If we are in a mystery dungeon where monsters can't drop items, and this is NOT a relic, pass
            if (!canMonstersDropItems && !itm.customItemFromGenerator) continue;

            if (itm.ReadActorData("alwaysdrop") != 1)
            {
                if (itm.collection == null)
                {
                    Debug.Log(itm.actorRefName + " no collection from " + monster.actorRefName + " skipping.");
                    continue;
                }
                if (itm.challengeValue > 500f)
                {
                    continue;
                }

                if ((sharaMode && SharaModeStuff.disallowSharaModeItems.Contains(itm.actorRefName)) ||
                    (RandomJobMode.IsCurrentGameInRandomJobMode() && RandomJobMode.disallowRandomJobModeItems.Contains(itm.actorRefName)))
                {
                    continue;
                }

                // Don't drop legendary/set items the player already found.
                if (!itm.ValidForPlayer())
                {
                    continue;
                }
            }
            else
            {
                itm.collection = monster.myInventory;
            }

            if (itm.IsEquipment())
            {
                if (!itm.legendary && (int)itm.rarity <= (int)Rarity.ARTIFACT)
                {
                    if (UnityEngine.Random.Range(0, 1f) < pandoraBonusMagicChance)
                    {
                        EquipmentBlock.MakeMagical(itm, itm.challengeValue, false);
                    }
                    else if (UnityEngine.Random.Range(0,1f) <= RandomJobMode.GetChanceOfMagicalGear())
                    {
                        EquipmentBlock.MakeMagical(itm, itm.challengeValue, false);
                    }
                }
                Equipment eq = itm as Equipment;

                /* if (UnityEngine.Random.Range(0, 1f) <= RandomJobMode.GetChanceToUpgradeMonsterDroppedGear() &&
                    eq.ReadActorData("rjup") != 1)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        eq.UpgradeItem();
                        if (UnityEngine.Random.Range(0,1f) > 0.33f)
                        {
                            break;
                        }
                    }                    
                } */

                if (GameStartData.NewGamePlus == 2 && eq.timesUpgraded == 0 && eq.ReadActorData("swup") != 1)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (UnityEngine.Random.Range(0,1f) < GameStartData.NGPLUSPLUS_CHANCE_EQ_UPGRADE_ON_DROP)
                        {
                            eq.UpgradeItem();
                            eq.SetActorData("swup", 1);
                        }
                    }
                }
            }

            itm.newlyPickedUp = true;
            // Loot goes flying!
            Item itemToSpawn = itm;

            Vector2 spawnPos = monster.GetPos();

            if (itemToSpawn.itemType == ItemTypes.CONSUMABLE && itemToSpawn.legendary && itemToSpawn.actorRefName.Contains("dragonsoul"))
            {
                // this should just... always spawn
            }
            else
            {
                spawnPos = MapMasterScript.FindNearbyEmptyTileForItem(position, true, throwLoot, pandoraBox).pos;
            }            

            bool itemSpawnedAtOriginalPosition = false;

            if (!MapMasterScript.GetTile(position).IsCollidable(GameMasterScript.heroPCActor) && !throwLoot)
            {
                itemToSpawn.SetSpawnPosXY((int)position.x, (int)position.y);
                GameMasterScript.SpawnItemAtPosition(itemToSpawn, position);
                itemSpawnedAtOriginalPosition = true;
            }
            else
            {
                itemToSpawn.SetSpawnPosXY((int)spawnPos.x, (int)spawnPos.y);
                GameMasterScript.SpawnItemAtPosition(itemToSpawn, spawnPos);
            }

            if (spawnPos != position && itemSpawnedAtOriginalPosition)
            {
                GameMasterScript.mms.MoveAndProcessActor(position, spawnPos, itemToSpawn);
            }

            CombatManagerScript.FireProjectile(monster.GetPos(), spawnPos, itemToSpawn.GetObject(), 0.25f, false, null, MovementTypes.TOSS, GameMasterScript.tossProjectileDummy, 360f, false);

            itemToSpawn.collection = null;
            itemToSpawn.RebuildDisplayName();

            // Dream item tracking for dream results screen
            if (MapMasterScript.activeMap.IsItemWorld())
            {
                itemToSpawn.SetActorData("fromdreammob", 1);
            }
        }
        monster.myInventory.ClearInventory();
    }

    public static float GetMaxChallengeValueForItems()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            return GameMasterScript.MAX_CHALLENGE_RATING - 0.1f;
        }
        else
        {
            return GameMasterScript.MAX_CHALLENGE_RATING_EXPANSION - 0.1f;
        }
    }

    public static void OnLegendaryItemFound(Item lootedItem)
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        // Coooool legendary FX
        zirconAnim useItem = heroPCActor.myAnimatable.FindAnim("UseItem");
        if (useItem != null)
        {
            heroPCActor.myAnimatable.SetAnim("UseItem"); // was last cardinal?
        }
        UIManagerScript.PlayCursorSound("Ultra Learn"); // Find legendary SFX
        TDVisualEffects.WaitThenPopupSprite(lootedItem.spriteRef, GameMasterScript.heroPCActor.GetObject().transform, 0.15f, lootedItem.GetSpriteForUI());
        GameMasterScript.SetAnimationPlaying(true);

        string convoRef = "legendary_found";

        bool guaranteeRelic = lootedItem.ReadActorData("grc") == 1 || lootedItem.ReadActorData("guaranteerelic") == 1; 

        if (lootedItem.customItemFromGenerator && !guaranteeRelic)
        {
            convoRef = "relic_found";
            GameMasterScript.gmsSingleton.SetTempGameData("id_relicfound", lootedItem.actorUniqueID);
        }

        if (Debug.isDebugBuild) Debug.Log("Found legendary item " + lootedItem.actorUniqueID + " ref " + lootedItem.actorRefName + " " + lootedItem.displayName + " Relic? " + lootedItem.customItemFromGenerator);

        Conversation legItemFound = GameMasterScript.FindConversation(convoRef);

        GameMasterScript.gmsSingleton.statsAndAchievements.IncrementLegendariesFound();

        GameMasterScript.gmsSingleton.QueueTempStringData("leg_item", lootedItem.displayName);
        GameMasterScript.gmsSingleton.QueueTempStringData("leg_type", lootedItem.GetDisplayItemType());
        GameMasterScript.gmsSingleton.QueueTempStringData("leg_desc", lootedItem.description);

        string leginfo = lootedItem.GetItemInformationNoName(false);

        if (!guaranteeRelic)
        {
            GameMasterScript.gmsSingleton.QueueTempStringData("leg_info", leginfo);
        }        

        //if (Debug.isDebugBuild) Debug.Log("Leg info for the above item is: " + leginfo);

        GameMasterScript.gmsSingleton.QueueTempStringData("legfoundsprite", lootedItem.spriteRef);
        GameMasterScript.gmsSingleton.QueueTempGameData("legfoundid", lootedItem.actorUniqueID);

        if (lootedItem.customItemFromGenerator)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("levelscaledrelicid", -1); // relevant for when we're comparing found relic vs. level-scaled version
        }

        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(legItemFound, DialogType.STANDARD, null, 0.6f));
    }
}

