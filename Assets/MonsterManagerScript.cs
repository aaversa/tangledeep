using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterManagerScript : MonoBehaviour {

    static GameMasterScript gms;
    static List<string> randomPetNames;

	// Use this for initialization
	void Start ()
    {
        GameObject go = GameObject.Find("MapMaster");
        go = GameObject.Find("GameMaster");
        gms = go.GetComponent<GameMasterScript>();
        if (randomPetNames == null)
        {
            randomPetNames = new List<string>();
        }
        
    }
	
	public static string GetRandomPetName()
    {
        //Debug.Log(randomPetNames.Count + " " + MetaProgressScript.tamedMonsters.Count);
        if (randomPetNames.Count > 0)
        {
            string newName = "";
            bool nameValid = false;
            string existingPetName = "";
            if (GameMasterScript.heroPCActor.GetMonsterPet() != null)
            {
                existingPetName = GameMasterScript.heroPCActor.GetMonsterPet().displayName;
            }
            int attempts = 0;
            while (!nameValid)
            {
                //get a name from the list
                newName = randomPetNames[UnityEngine.Random.Range(0, randomPetNames.Count)];
                
                //if we've been trying forever to come up with a name, it's possible that
                //some tryhard player has as many pets as there are random names, and has 
                //used the random name generator for each pet. If this is the case,
                //we should melt their Switch, but instead...
                attempts++;
                int fuckery = attempts;
                while (fuckery > 999)
                {
                    newName += " " + randomPetNames[UnityEngine.Random.Range(0, randomPetNames.Count)];
                    fuckery -= 1000;
                }
                
                if (newName == existingPetName) continue;
                nameValid = true;
                foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
                {
                    if (tcm.monsterObject.displayName == newName)
                    {
                        nameValid = false;
                        break;
                    }
                }
            }

            return newName;

            //return randomPetNames[UnityEngine.Random.Range(0, randomPetNames.Count)];
        }
        else
        {
            return "Dummy";
        }
        
    }

    public static void AddRandomPetName(string newName)
    {
        if (randomPetNames == null) randomPetNames = new List<string>();
        randomPetNames.Add(newName);
    }

    public static bool CheckTemplateRef(string templateRef)
    {
        MonsterTemplateData mtd = GetTemplateByRef(templateRef);
        if (mtd == null)
        {
            return false;
        }
        return true;
    }

    public static Monster CreateMonster(string templateRef, bool spawnWithLoot, bool rewards, bool gameLoadState, float alterCV, bool createdByPlayer, ItemWorldMetaData mapProperties = null)
    {
        return CreateMonster(templateRef, spawnWithLoot, rewards, gameLoadState, alterCV, 0f, createdByPlayer, mapProperties);
    }

    public static Monster CreateMonster(string templateRef, bool spawnWithLoot, bool rewards, bool gameLoadState, float alterCV, float bonusRewards, bool createdByPlayer, ItemWorldMetaData mapProperties = null) // monster name is the template ref
    {
        MonsterTemplateData monsterTemplate = GetTemplateByRef(templateRef);

        if (monsterTemplate == null) {
        	Debug.Log("WARNING: Couldn't find template " + templateRef);
        	return null;
        }
        Monster newMonActor = new Monster();
        newMonActor.SetUniqueIDAndAddToDict();
        CopyMonsterFromTemplate(newMonActor, monsterTemplate, spawnWithLoot, rewards, gameLoadState, alterCV, bonusRewards, createdByPlayer, mapProperties);
        return newMonActor;
    }

    public static MonsterTemplateData GetTemplateByRef(string mRef)
    {
        MonsterTemplateData mtd;
        if (GameMasterScript.masterMonsterList.TryGetValue(mRef, out mtd))
        {
            return mtd;
        }
        else
        {
            mRef += "_" + GameStartData.saveGameSlot;
            if (Debug.isDebugBuild) Debug.Log("Try " + mRef + " now?");
            if (GameMasterScript.masterMonsterList.TryGetValue(mRef, out mtd))
            {                
                return mtd;
            }

            if (Debug.isDebugBuild) Debug.Log("Still could not find it. Returning a jelly.");

            return GameMasterScript.masterMonsterList["mon_mossjelly"];
        }
    }

    public static void CopyMonsterFromTemplateRef(Monster newMon, string templateRef, bool spawnLoot, bool rewards, bool gameLoadState, float bonusRewards, bool createdByPlayer) {

        MonsterTemplateData mtd = GetTemplateByRef(templateRef);
        if (mtd == null)
        {
            Debug.Log("WARNING: Template " + templateRef + " not found.");
            return;
        }
        CopyMonsterFromTemplate(newMon, mtd, spawnLoot, rewards, gameLoadState, 0f, bonusRewards, createdByPlayer);    	
    }

    public static bool CheckIfRefCanBeChampion(string mRef)
    {
        MonsterTemplateData mtd = GetTemplateByRef(mRef);
        
        if (mtd != null)
        {
            return !mtd.cannotBeChampion;
        }

        return false;

    }

    public static string GetMonsterDisplayNameByRef(string mRef)
    {
        MonsterTemplateData mtd = GetTemplateByRef(mRef);
        if (mtd != null)
        {
            return mtd.monsterName;
        }
        Debug.Log("Couldn't find disp name for " + mRef);
        return "";
    }

    public static void CopyMonsterFromTemplate(Monster newMon, MonsterTemplateData monsterTemplate, bool spawnLoot, bool rewards, bool gameLoadState, float alterCV, float bonusRewards, bool createdByPlayer, ItemWorldMetaData mapProperties = null)
    {
        if (mapProperties == null)
        {
            mapProperties = new ItemWorldMetaData();
        }
        newMon.myTemplate = monsterTemplate;
        newMon.scriptTakeAction = monsterTemplate.scriptTakeAction;
        newMon.scriptOnDefeat = monsterTemplate.scriptOnDefeat;
        newMon.helpRange = monsterTemplate.helpRange;
    	newMon.actorRefName = monsterTemplate.refName;
        newMon.autoSpawn = monsterTemplate.autoSpawn;
        newMon.isBoss = monsterTemplate.isBoss;
        newMon.isInCorral = false;
        newMon.prefab = monsterTemplate.prefab;
        newMon.actorfaction = monsterTemplate.faction;
        newMon.bufferedFaction = monsterTemplate.faction;
        newMon.monFamily = monsterTemplate.monFamily;
        newMon.spriteRefOnSummon = monsterTemplate.spriteRefOnSummon;
        //newMon.myStats = new StatBlock();
        //newMon.myStats.owner = newMon;
        //newMon.myEquipment = new EquipmentBlock();
        //newMon.myEquipment.owner = newMon;
        //newMon.myInventory = new InventoryScript();
        //newMon.myAbilities = new AbilityComponent();
        //newMon.myAbilities.owner = newMon;
        StatBlock stats = newMon.myStats;
        EquipmentBlock equip = newMon.myEquipment;

        /* for (int i = 0; i < 10; i++)
        {
            if (monsterTemplate.monAttributes[i] != null)
            {
                newMon.AddAttribute(monsterTemplate.monAttributes[i]);
            }
        } */

        newMon.displayName = monsterTemplate.monsterName;
        newMon.moveRange = monsterTemplate.moveRange;
        newMon.myStats.SetStat(StatTypes.HEALTH, monsterTemplate.hp, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.STRENGTH, monsterTemplate.strength, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.SWIFTNESS, monsterTemplate.swiftness, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.DISCIPLINE, monsterTemplate.discipline, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.SPIRIT, monsterTemplate.spirit, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.GUILE, monsterTemplate.guile, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.ACCURACY, monsterTemplate.accuracy, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.CHARGETIME, monsterTemplate.chargetime, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.VISIONRANGE, monsterTemplate.visionRange, StatDataTypes.ALL, true, false);

        newMon.myStats.SetStat(StatTypes.STAMINA, monsterTemplate.stamina, StatDataTypes.ALL, true, false);
        newMon.myStats.SetStat(StatTypes.ENERGY, monsterTemplate.energy, StatDataTypes.ALL, true, false);
        newMon.myStats.SetMaxRegenRate(StatTypes.HEALTH, monsterTemplate.healthRegenRate);
        newMon.myStats.SetMaxRegenAmount(StatTypes.HEALTH, monsterTemplate.healthRegenAmount); // Expressed as %
        newMon.myStats.SetMaxRegenRate(StatTypes.STAMINA, monsterTemplate.staminaRegenRate);
        newMon.myStats.SetMaxRegenAmount(StatTypes.STAMINA, monsterTemplate.staminaRegenAmount); // Expressed as %
        newMon.myStats.SetMaxRegenRate(StatTypes.ENERGY, monsterTemplate.energyRegenRate);
        newMon.myStats.SetMaxRegenAmount(StatTypes.ENERGY, monsterTemplate.energyRegenAmount); // Expressed as %

        newMon.excludeFromHotbarCheck = monsterTemplate.excludeFromHotbarCheck;

        stats.SetLevel(1);

        newMon.challengeValue = monsterTemplate.challengeValue;
        newMon.lootChance = monsterTemplate.lootChance;
        newMon.xpMod = monsterTemplate.xpMod;
        newMon.aggroRange = monsterTemplate.aggroRange;
        newMon.stalkerRange = monsterTemplate.stalkerRange;

        if (!rewards)
        {
            newMon.xpMod = 0;
        }

        //newMon.monTemplate = monsterTemplate;
        newMon.SetMoveBehavior(monsterTemplate.monMoveType);
        newMon.SetMoveBoundary(monsterTemplate.monMoveBoundary);
        newMon.myBehaviorState = monsterTemplate.monBehavior;
        newMon.myStats.SetLevel(monsterTemplate.baseLevel);

        newMon.turnsToLoseInterest = monsterTemplate.turnsToLoseInterest;

        for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
        {
            if (monsterTemplate.monAttributes[i] > 0)
            {
                newMon.AddAttribute((MonsterAttributes)i,monsterTemplate.monAttributes[i]);
            }
        }

        Weapon thisWeapon = new Weapon();
        thisWeapon.actorRefName = monsterTemplate.weaponID;

        Actor findWeapon = GameMasterScript.GetItemFromRef(thisWeapon.actorRefName);
        if (findWeapon == null)
        {
            Debug.LogWarning("ERROR: Could not find weapon for " + newMon.actorRefName + " " + newMon.actorUniqueID + " " + thisWeapon.actorRefName);
        }

        Weapon templateWeapon = findWeapon as Weapon;

        thisWeapon.CopyFromItem(templateWeapon);

        bool scaleToNewGamePlus = GameStartData.NewGamePlus > 0 && MysteryDungeonManager.CheckIfNGPlusMonstersShouldBeScaled();

        if (scaleToNewGamePlus && newMon.actorfaction == Faction.PLAYER && createdByPlayer)
        {
            newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, (0.25f * GameStartData.NewGamePlus));
        }

        if (scaleToNewGamePlus && newMon.actorfaction != Faction.PLAYER && !createdByPlayer) // NEW GAME+ CHANGES
        {
            //Debug.Log("Scale " + newMon.actorRefName + " to NGplus");
            if (newMon.myStats.GetStat(StatTypes.CHARGETIME, StatDataTypes.MAX) < 99f)
            {
                newMon.myStats.SetStat(StatTypes.CHARGETIME, 99f, StatDataTypes.ALL, true, false);
            }
            if (newMon.myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.MAX) < 10f)
            {
                newMon.myStats.SetStat(StatTypes.VISIONRANGE, 10f, StatDataTypes.ALL, true, false);
            }
            if (newMon.myStats.GetStat(StatTypes.ACCURACY, StatDataTypes.MAX) < 99f)
            {
                newMon.myStats.SetStat(StatTypes.ACCURACY, 99f, StatDataTypes.ALL, true, false);
            }


            newMon.aggroRange += 1 + (1 * GameStartData.NewGamePlus);
            int minAggroRange = 6 + (1 * GameStartData.NewGamePlus);
            if (newMon.aggroRange < minAggroRange)
            {
                newMon.aggroRange = minAggroRange;
            }

            newMon.RemoveAttribute(MonsterAttributes.TIMID);
            newMon.RemoveAttribute(MonsterAttributes.PREDATOR);
            newMon.RemoveAttribute(MonsterAttributes.STALKER);

            thisWeapon.power *= (1f + (0.25f * GameStartData.NewGamePlus));

            float minWeaponPower = 35f + (GameStartData.NewGamePlus * 15f);

            if (thisWeapon.power < minWeaponPower)
            {
                thisWeapon.power = minWeaponPower;
            }

            newMon.turnsToLoseInterest = 999;

            if (!gameLoadState)
            {
                newMon.challengeValue += (0.3f * GameStartData.NewGamePlus);

                float minCV = 1.4f + (GameStartData.NewGamePlus * 0.3f);

                if (newMon.challengeValue < minCV)
                {
                    newMon.challengeValue = minCV;
                }

                //int levelsToAdd = 1 + (GameStartData.newGamePlus * 3);

                int levelsToAdd = 1;

                if (GameStartData.NewGamePlus == 1)
                {
                    levelsToAdd = 4;
                }
                if (GameStartData.NewGamePlus >= 2)
                {
                    levelsToAdd = 6;
                }

                //int minLevel = 9 + (GameStartData.newGamePlus * 3);
                int minLevel = 9;

                if (GameStartData.NewGamePlus == 1)
                {
                    minLevel = 9;
                }
                else if (GameStartData.NewGamePlus >= 2)
                {
                    minLevel = 15;
                }

                newMon.myStats.SetLevel(newMon.myStats.GetLevel() + levelsToAdd);

                if (newMon.myStats.GetLevel() < minLevel)
                {
                    newMon.myStats.SetLevel(minLevel);
                }

                // Adjust HP for realz

                /* if (newMon.isBoss)
                {
                    newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.15f);
                }
                else // Scale HP with expected values on our table
                {
                    newMon.myStats.SetStat(StatTypes.HEALTH, Monster.expectedMonsterHealth[(int)newMon.myStats.GetLevel()], StatDataTypes.ALL, true);
                } */

                float boostAmount = GetHealthBoostForNewGamePlus(newMon);

                newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, boostAmount);

                /* if (GameStartData.newGamePlus == 2)
                {
                    float boostAmountSW = 0.25f;
                    if (!newMon.isBoss)
                    {
                        boostAmountSW = 0.5f;
                    }
                    newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, boostAmountSW);
                } */

                thisWeapon.power = BalanceData.expectedMonsterWeaponPower[(int)newMon.myStats.GetLevel()];
                thisWeapon.power *= (1f + (GameStartData.NewGamePlus * 0.05f));
                if (thisWeapon.power > Weapon.MAX_WEAPON_POWER)
                {
                    thisWeapon.power = Weapon.MAX_WEAPON_POWER;
                }

                float minHealth = 400f + (GameStartData.NewGamePlus * 230f);

                if (GameStartData.NewGamePlus == 2)
                {
                    minHealth += 150f;
                }

                if (newMon.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) < minHealth)
                {
                    newMon.myStats.SetStat(StatTypes.HEALTH, minHealth, StatDataTypes.ALL, true);
                }

                if (newMon.isBoss)
                {
                    //newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, (1.0f * GameStartData.newGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.STRENGTH, (0.3f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.SWIFTNESS, (0.3f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.GUILE, (0.3f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.DISCIPLINE, (0.3f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.SPIRIT, (0.3f * GameStartData.NewGamePlus));
                }
                else
                {
                    newMon.myStats.BoostStatByPercent(StatTypes.STRENGTH, (0.2f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.SWIFTNESS, (0.2f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.GUILE, (0.2f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.DISCIPLINE, (0.2f * GameStartData.NewGamePlus));
                    newMon.myStats.BoostStatByPercent(StatTypes.SPIRIT, (0.2f * GameStartData.NewGamePlus));
                }

            }
        }

        //thisWeapon.SetUniqueIDAndAddToDict();

        equip.defaultWeapon = thisWeapon;
        equip.AddEquipment(EquipmentSlots.WEAPON, thisWeapon);        

        if (!string.IsNullOrEmpty(monsterTemplate.offhandWeaponID))
        {
            Weapon newOH = LootGeneratorScript.CreateItemFromTemplateRef(monsterTemplate.offhandWeaponID, 2.0f, 0f, false) as Weapon;
            equip.AddEquipment(EquipmentSlots.OFFHAND, newOH);
        }
        if (!string.IsNullOrEmpty(monsterTemplate.offhandArmorID))
        {
            Offhand newOH = LootGeneratorScript.CreateItemFromTemplateRef(monsterTemplate.offhandArmorID, 2.0f, 0f, false) as Offhand;
            equip.AddEquipment(EquipmentSlots.OFFHAND, newOH);
        }

        if (!string.IsNullOrEmpty(monsterTemplate.armorID))
        {
            Armor thisArmor = new Armor();            
            thisArmor.actorRefName = monsterTemplate.armorID;
            Armor templateArmor = GameMasterScript.GetItemFromRef(thisArmor.actorRefName) as Armor;
            thisArmor.CopyFromItem(templateArmor);
            equip.AddEquipment(EquipmentSlots.ARMOR, thisArmor);
        }

        if (monsterTemplate.monsterPowers.Count > 0)
        {
            newMon.monsterPowers = new List<MonsterPowerData>();
        }

        foreach (MonsterPowerData mpd in monsterTemplate.monsterPowers)
        {
            AbilityScript newAbil = new AbilityScript();
            AbilityScript template = mpd.abilityRef;

            if (template == null) continue; 

            // Passive status effects are already serialized.
            // However, if we're player-faction, then we DO want to always load those abilities.
            if (template.passiveAbility && gameLoadState && !createdByPlayer)
            {
                continue;
            }

            AbilityScript.CopyFromTemplate(newAbil, template);

            // ng++ does this work?
            if (scaleToNewGamePlus && GameStartData.NewGamePlus == 2 && !createdByPlayer)
            {
                int targetMaxCooldown = (int)(newAbil.maxCooldownTurns * 0.8f);
                if (targetMaxCooldown < 2)
                {
                    targetMaxCooldown = 2;
                }
                newAbil.SetMaxCooldown(targetMaxCooldown);
            }

            newMon.myAbilities.AddNewAbility(newAbil, !gameLoadState); // if we are loading the game, don't re-equip the passive ability.
            if (gameLoadState && newAbil.passiveAbility)
            {
                newAbil.active = true;
                newAbil.passiveEquipped = true;
            }

            MonsterPowerData newMPD = new MonsterPowerData();
            newMPD.CopyFromTemplate(mpd, newAbil);
            newMon.monsterPowers.Add(newMPD);
            newMon.OnMonsterPowerAdded(mpd, newAbil);
        }

        newMon.SetBattleDataDirty();

        if (scaleToNewGamePlus && !gameLoadState && newMon.actorfaction != Faction.PLAYER && !createdByPlayer)
        {
            newMon.cachedBattleData.critChanceMod += (0.05f * GameStartData.NewGamePlus);
            newMon.cachedBattleData.critDamageMod += 0.25f;
            newMon.allDamageMultiplier += 0.1f + (GameStartData.NewGamePlus * 0.1f);
            newMon.allMitigationAddPercent = newMon.allMitigationAddPercent - 0.05f - (0.05f * GameStartData.NewGamePlus);

            if (newMon.isBoss)
            {
                newMon.allDamageMultiplier += 0.15f;
            }

            DoExtraChangesForNGPlusPlus(newMon);            
        }

        // Maybe spawn with loot?

        if (newMon.actorRefName == "mon_goldfrog" || newMon.actorRefName == "mon_darkfrog")
        {
            if (GameMasterScript.heroPCActor == null) // Hacky compatibility case for old goldfrogs in corral
            {
                newMon.myStats.SetLevel(5);
            }
            else
            {
                newMon.myStats.SetLevel(GameMasterScript.heroPCActor.myStats.GetLevel());
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f && !gameLoadState)
                {
                    Item gem = LootGeneratorScript.GenerateLootFromTable(alterCV, newMon.lootChance, "gems");
                    newMon.myInventory.AddItem(gem, false);
                }
            }
            
            newMon.myStats.SetStat(StatTypes.HEALTH, newMon.myStats.GetLevel() * 100f, StatDataTypes.ALL, true);
            if (newMon.actorRefName == "mon_darkfrog")
            {
                newMon.allDamageMultiplier += 0.1f;
                newMon.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.25f, true);
            }
            newMon.challengeValue = alterCV;


        }

        //Debug.Log("Spawned " + newMon.actorRefName + " " + newMon.myStats.GetLevel() + " " + gameLoadState);
        foreach(string key in monsterTemplate.defaultActorData.Keys)
        {
            newMon.SetActorData(key, monsterTemplate.defaultActorData[key]);
        }
        foreach (string key in monsterTemplate.defaultActorDataString.Keys)
        {
            newMon.SetActorDataString(key, monsterTemplate.defaultActorDataString[key]);
        }

        if (GameMasterScript.actualGameStarted)
        {
            if (newMon.myStats.GetLevel() <= (GameMasterScript.heroPCActor.myStats.GetLevel()-4) && !newMon.isBoss && 
                (!MapMasterScript.activeMap.ScaleUpToPlayerLevel() || MapMasterScript.activeMap.dungeonLevelData.scaleMonstersToMinimumLevel > 0))
            {
                return;
            }
            
        }

        if (!spawnLoot)
        {
        	return;
        }

        float roll = UnityEngine.Random.Range(0, 1.0f);

        int items = 0; // Max is 3
        float runningChance = newMon.lootChance;

        //don't apply the global loot chance to bosses, as it unintentionally changes
        //the expectation of what we set in the XML for the boss.
        if (!newMon.isBoss)
        {
            runningChance *= gms.globalLootDropChance;
        }

        if (MysteryDungeonManager.InOrCreatingMysteryDungeon())
        {
            runningChance += MysteryDungeonManager.EXTRA_LOOT_DROP_CHANCE;            
        }

        // HARDCODED!
        if (GameMasterScript.playerIsScavenger)
        {
            runningChance += GameMasterScript.SCAVENGER_BONUS_LOOT_CHANCE;
        }

        if (GameMasterScript.gameLoadSequenceCompleted) // WAS initial awake, but that's bad
        {
            runningChance += GameMasterScript.heroPCActor.advStats[(int)AdventureStats.LOOTFIND];
        }

        runningChance += (bonusRewards * 0.33f);

        // END HARDCODED!

        float magicChance = newMon.lootChance;

        int maxItemCount = 3;

        float extraLootValue = 0f;

        if (SharaModeStuff.IsSharaModeActive())
        {
            runningChance *= 1.2f;
            magicChance += 0.1f;
            maxItemCount += 1;
            if (newMon.myStats.GetLevel() >= 4)
            {
                extraLootValue += 0.05f;
            }
            if (newMon.myStats.GetLevel() >= 10)
            {
                extraLootValue += 0.1f;
            }
            
        }

        int minItems = 0;

        if (newMon.isBoss)
        {
            minItems = 1;
            maxItemCount += 1;
        }

        //todo: decouple the idea of showing a creature's health on screen from making it drop more loot.
        if (newMon.myTemplate.showBossHealthBar)
        {
            minItems = 3;
            maxItemCount += 2;
        }

        if (GameStartData.NewGamePlus >= 1)
        {
            magicChance *= 1.5f;
            runningChance *= 1.25f;
            extraLootValue += 0.1f;
            if (runningChance < 0.15f) runningChance = 0.15f;
            if (GameStartData.NewGamePlus == 2)
            {
                maxItemCount++;
                runningChance *= 1.25f;
                extraLootValue += 0.1f;
                if (runningChance < 0.22f) runningChance = 0.22f;
                magicChance *= 1.5f;
            }
        }

        //Debug.Log(newMon.challengeValue + " " + newMon.actorRefName + " " + runningChance + " " + extraLootValue + " " + maxItemCount + " " + magicChance);

        while ((roll <= runningChance || minItems > 0) && items < maxItemCount)
        {
            //if this monster is a boss, don't reduce the chance of a drop if we are still under minimum items
            if (minItems <= 0)
            {
                runningChance *= 0.5f;
            }
            Item itm = LootGeneratorScript.GenerateLoot(newMon.challengeValue + extraLootValue, magicChance);
            if (itm != null)
            {
                float chanceToDiscardWhiteItems = 0.0f;

                if (newMon.challengeValue >= 1.4f) chanceToDiscardWhiteItems += 0.1f;
                if (newMon.challengeValue >= 1.5f) chanceToDiscardWhiteItems += 0.1f;
                if (newMon.challengeValue >= 1.6f) chanceToDiscardWhiteItems += 0.15f;
                if (newMon.challengeValue >= 1.8f) chanceToDiscardWhiteItems += 0.15f;

                if (itm.challengeValue >= 1.3f) chanceToDiscardWhiteItems -= 0.1f;
                if (itm.challengeValue >= 1.5f) chanceToDiscardWhiteItems -= 0.1f;
                if (itm.challengeValue >= 1.7f) chanceToDiscardWhiteItems -= 0.1f;

                if (itm.IsEquipment())
                {
                    Equipment eq = itm as Equipment;
                    int nonAutomodCount = eq.GetNonAutomodCount();
                    if (nonAutomodCount == 1) chanceToDiscardWhiteItems *= 0.5f;

                    //question: Should bosses drop white items? I am suggesting here that they do not.
                    if (newMon.isBoss)
                    {
                        chanceToDiscardWhiteItems = 1.0f;
                    }

                    if (GameStartData.NewGamePlus > 0)
                    {
                        chanceToDiscardWhiteItems += 0.25f;
                    }

                    if (nonAutomodCount <= 0 && !itm.legendary && UnityEngine.Random.Range(0, 1f) < chanceToDiscardWhiteItems)
                    {
                        GameMasterScript.dictAllActors.Remove(itm.actorUniqueID);
                        continue;
                    }
                }

                newMon.myInventory.AddItem(itm, false);

                if (itm.IsEquipment() && !itm.legendary)
                {
                    Equipment eq = itm as Equipment;
                    if (mapProperties.properties[(int)ItemWorldProperties.NIGHTMARE] && UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHANCE_NIGHTMARE_EXTRAMOD)
                    {
                        EquipmentBlock.MakeMagicalFromModFlag(eq, MagicModFlags.NIGHTMARE, false);
                    }
                    else if (UnityEngine.Random.Range(0,1f) < GameStartData.NewGamePlus * GameStartData.NGPLUSPLUS_CHANCE_MON_LOOT_EXTRAMAGIC)
                    {
                        EquipmentBlock.MakeMagical(eq, newMon.challengeValue, false);
                    }
                }
                //Debug.Log(itm.displayName + " " + itm.rarity + " " + itm.challengeValue);
            }
            roll = UnityEngine.Random.Range(0, 1.0f);
            items++;
            minItems--;
        }

        foreach (string guaranteeItem in monsterTemplate.guaranteeLoot)
        {
            Item itm = LootGeneratorScript.CreateItemFromTemplateRef(guaranteeItem, monsterTemplate.challengeValue, 0f, false);
            newMon.myInventory.AddItem(itm, false);
            itm.AddActorData("alwaysdrop", 1);
        }
    }

    /// <summary>
    /// Adjusts newMon so that it is about 20% weaker than its summoner's level - 1.
    /// </summary>
    /// <param name="summoner"></param>
    /// <param name="newMon"></param>
    public static void ScaleSummonedCreatureInTrialOrMysteryDungeon(Fighter summoner, Monster newMon)
    {
        int originalLevel = newMon.myStats.GetLevel();
        int targetLevel = summoner.myStats.GetLevel() - 1;
        newMon.myStats.SetLevel(targetLevel, false);

        if (targetLevel < 0)
        {
            Debug.Log("Why was requested value " + targetLevel + " for scale level less than 0? " + newMon.actorRefName);
            targetLevel = 0;            
        }
        if (targetLevel >= BalanceData.expectedMonsterHealth.Length)
        {
            Debug.Log("Why was requested value " + targetLevel + " for scale level too high? " + newMon.actorRefName);
            targetLevel = BalanceData.expectedMonsterHealth.Length - 1;
        }

        newMon.myStats.SetStat(StatTypes.HEALTH, BalanceData.expectedMonsterHealth[targetLevel] * 0.8f, StatDataTypes.ALL, true);
        
        foreach(StatTypes st in StatBlock.CORE_NON_RESOURCE_STATS)
        {
            newMon.myStats.SetStat(st, BalanceData.GetExpectedMonsterStatByLevel(targetLevel, st) * 0.8f, StatDataTypes.ALL, true);
        }

        newMon.myEquipment.GetWeapon().power = Weapon.expectedPetOrSummonWeaponPower[targetLevel];
        newMon.weaponScaled = true;
    }

    static void DoExtraChangesForNGPlusPlus(Monster newMon)
    {
        if (GameStartData.NewGamePlus < 2) return;

        // in NG++, melee-only monsters get bonuses
        if (!newMon.myEquipment.IsCurrentWeaponRanged() && !MysteryDungeonManager.InOrCreatingMysteryDungeon())
        {
            bool meleeOnly = true;
            foreach(MonsterPowerData mpd in newMon.monsterPowers)
            {
                if (mpd.abilityRef.range >= 2)
                {
                    if (mpd.abilityRef.targetForMonster == AbilityTarget.ENEMY || mpd.abilityRef.targetForMonster == AbilityTarget.SUMMONHAZARD)
                    {
                        meleeOnly = false;
                        break;
                    }                                        
                }
            }
            if (meleeOnly)
            {
                newMon.myStats.AddStatusByRef("permaparry15", newMon, 99, false);
                newMon.allDamageMultiplier += 0.15f;
            }            
        }

        // certain abilities get buffed
        foreach(MonsterPowerData mpd in newMon.monsterPowers)
        {
            if (mpd.abilityRef.passiveAbility) continue;
            switch(mpd.abilityRef.refName)
            {
                case "skill_clawrake":
                    mpd.abilityRef.range = 4;
                    mpd.abilityRef.AddAbilityTag(AbilityTags.CENTERED);
                    mpd.maxRange = 3;
                    break;
                case "skill_smalllightningcircle":
                    mpd.abilityRef.range = 2;
                    mpd.maxRange = 2;
                    break;
                case "skill_lightningcircle":
                    mpd.abilityRef.range = 2;
                    mpd.abilityRef.boundsShape = TargetShapes.RECT;
                    mpd.maxRange = 2;
                    break;
                case "skill_watercross":
                    mpd.abilityRef.range = 3;
                    mpd.abilityRef.boundsShape = TargetShapes.BURST;
                    mpd.maxRange = 3;
                    break;
                case "skill_laserclaw":
                    mpd.abilityRef.boundsShape = TargetShapes.FLEXCONE;
                    break;
                case "skill_xvortex":
                    mpd.abilityRef.boundsShape = TargetShapes.RECT;
                    break;
                case "skill_mortarfire":
                    mpd.abilityRef.targetRange = 2;
                    break;
            }
        }
    }

    public static float GetHealthBoostForNewGamePlus(Monster mn)
    {
        float baseBoostAmount = 0.5f * GameStartData.NewGamePlus;

        if (GameStartData.NewGamePlus == 2 && !mn.isBoss)
        {
            baseBoostAmount += 0.33f;
        }

        if (mn.isBoss)
        {
            baseBoostAmount += 1f;
        }

        return baseBoostAmount;
    }
}

