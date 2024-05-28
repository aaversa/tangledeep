using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class QuestScript
{
    static List<DamageTypes> possibleDamageTypes;
    static List<QuestType> possibleQuestTypes;

    public static QuestScript CreateNewQuest()
    {
        InitializeStaticPools();

        //Debug.Log("Creating new quest...");
        QuestScript qs = new QuestScript();

        possibleQuestTypes.Clear();


        Map targetMap = FindUndiscoveredMap();
        int tries = 0;
        if (targetMap == null)
        {

        }
        else
        {
            possibleQuestTypes.Add(QuestType.FINDAREA);
            possibleQuestTypes.Add(QuestType.FINDAREA);
            possibleQuestTypes.Add(QuestType.FINDAREA);
        }


        Item findItem = FindItemToFind();
        if (findItem == null)
        {

        }
        else
        {
            possibleQuestTypes.Add(QuestType.FINDITEM);
            possibleQuestTypes.Add(QuestType.FINDITEM);
        }

        Monster findChamp = FindChampionToKill(false);
        if (findChamp != null)
        {
            possibleQuestTypes.Add(QuestType.KILLCHAMPION);
            possibleQuestTypes.Add(QuestType.KILLCHAMPION);
        }

        Monster appeaseMonster = FindMonsterToAppease();
        if (appeaseMonster != null)
        {
            possibleQuestTypes.Add(QuestType.APPEASEMONSTER);
        }

        QuestMonsterMapPair elemMonPackage = FindMonsterToHuntForElementalLastHit();
        if (elemMonPackage != null)
        {
            possibleQuestTypes.Add(QuestType.KILLMONSTERELEMENTAL);
        }

        Monster tameMon = FindMonsterToTame();
        if (tameMon != null && GameMasterScript.heroPCActor.lowestFloorExplored >= 3)
        {
            possibleQuestTypes.Add(QuestType.TAMEMONSTER);
        }


        if (GameMasterScript.heroPCActor.lowestFloorExplored >= 10)
        {
            possibleQuestTypes.Add(QuestType.BOSSGANG);
        }

        Item dreamItem = null;

        if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) == 2 && GameMasterScript.heroPCActor.myStats.GetLevel() >= 5) // Dreamcaster unlocked
        {
            dreamItem = FindDreamWeaponToEnter();
            if (dreamItem != null)
            {
                possibleQuestTypes.Add(QuestType.DREAMWEAPON_BOSS);
            }
        }

        qs.qType = possibleQuestTypes[UnityEngine.Random.Range(0, possibleQuestTypes.Count)];

        int attempts = 0;
        while (qs.qType == lastGeneratedQuestType)
        {
            qs.qType = possibleQuestTypes[UnityEngine.Random.Range(0, possibleQuestTypes.Count)];
            attempts++;
            if (attempts > 25)
            {
                // No other possible rumor types probably.
                break;
            }
        }

        switch (qs.qType)
        {
            case QuestType.TAMEMONSTER:
                qs.targetMonster = tameMon;
                qs.targetMonsterID = tameMon.actorUniqueID;
                qs.GenerateQuestRewards(qs.targetMonster.challengeValue, true, 1);
                qs.challengeValue = qs.targetMonster.challengeValue;
                qs.targetMap = qs.targetMonster.GetActorMap();
                qs.targetMapID = qs.targetMap.mapAreaID;
                qs.qType = QuestType.TAMEMONSTER;
                break;
            case QuestType.DREAMWEAPON_BOSS:
                qs.targetItem = dreamItem;
                qs.qType = QuestType.DREAMWEAPON_BOSS;
                qs.targetItemID = dreamItem.actorUniqueID;
                qs.challengeValue = dreamItem.challengeValue;
                qs.damType = (DamageTypes)UnityEngine.Random.Range(0, (int)DamageTypes.COUNT);
                while (qs.damType == DamageTypes.PHYSICAL)
                {
                    qs.damType = (DamageTypes)UnityEngine.Random.Range(0, (int)DamageTypes.COUNT);
                }
                qs.GenerateQuestRewards(qs.challengeValue, true, 2);
                break;
            case QuestType.KILLMONSTERELEMENTAL:
                qs.targetRef = elemMonPackage.mtd.refName;
                qs.targetMap = elemMonPackage.map;
                qs.targetMapID = elemMonPackage.mapID;

                possibleDamageTypes.Clear();
                possibleDamageTypes.Add(DamageTypes.PHYSICAL);
                foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
                {
                    if (abil.passiveAbility) continue;
                    foreach (EffectScript eff in abil.listEffectScripts)
                    {
                        if (eff.effectType == EffectType.DAMAGE)
                        {
                            DamageEffect de = eff as DamageEffect;
                            if (!possibleDamageTypes.Contains(de.damType))
                            {
                                bool monsterImmune = false;
                                if (!string.IsNullOrEmpty(elemMonPackage.mtd.armorID))
                                {
                                    Armor arm = GameMasterScript.masterItemList[elemMonPackage.mtd.armorID] as Armor;
                                    if (arm != null)
                                    {
                                        foreach (ResistanceData rd in arm.resists)
                                        {
                                            if (rd.damType == de.damType && rd.absorb || rd.multiplier < 0.5f)
                                            {
                                                monsterImmune = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (!monsterImmune)
                                {
                                    possibleDamageTypes.Add(de.damType);
                                }

                            }
                        }
                    }
                }
                int dType = (int)possibleDamageTypes[UnityEngine.Random.Range(0, possibleDamageTypes.Count)];

                qs.damType = (DamageTypes)dType;
                qs.numTargetsRemaining = UnityEngine.Random.Range(2, 4);

                // Can't use challenge value directly... doesn't take into account NG+. 
                //Monster findMon = Dungeon.FindMonsterByRef(monRef.challengeValue, monRef.refName, false);
                qs.challengeValue = actualMonsterCV;
                qs.GenerateQuestRewards(qs.challengeValue, true, 1);
                break;
            case QuestType.BOSSGANG:
                qs.targetMonster = FindChampionToKill(true);
                if (qs.targetMonster == null)
                {
                    Debug.Log("Couldn't find boss gang for quest.");
                    return null;
                }
                qs.qType = QuestType.BOSSGANG;
                qs.targetMonsterID = qs.targetMonster.actorUniqueID;
                qs.GenerateQuestRewards(qs.targetMonster.challengeValue, true, 1);
                qs.challengeValue = qs.targetMonster.challengeValue;
                qs.targetMap = qs.targetMonster.GetActorMap();
                qs.targetMapID = qs.targetMap.mapAreaID;
                break;
            case QuestType.KILLCHAMPION:
                qs.targetMonster = findChamp;
                qs.targetMonsterID = qs.targetMonster.actorUniqueID;
                qs.GenerateQuestRewards(qs.targetMonster.challengeValue, true, 2);
                qs.challengeValue = qs.targetMonster.challengeValue;
                qs.targetMap = qs.targetMonster.GetActorMap();
                qs.targetMapID = qs.targetMap.mapAreaID;
                //qs.displayName = "<color=red>Slay</color> " + qs.targetMonster.displayName + "<color=red>!</color>";
                //qs.questText = qs.targetMonster.displayName + " is terrorizing " + MapMasterScript.theDungeon.FindFloor(qs.targetMonster.dungeonFloor).GetName() + ". Put an end to their evil reign once and for all!";
                break;
            case QuestType.APPEASEMONSTER:
                qs.targetMonster = appeaseMonster;
                if (qs.targetMonster == null)
                {
                    Debug.Log("Couldn't find appease monster.");
                    return null;
                }

                qs.targetMonster.ConvertToPacifiedGreedyForQuest(desiredItemByMonster);

                qs.targetMonsterID = qs.targetMonster.actorUniqueID;
                qs.GenerateQuestRewards(qs.targetMonster.challengeValue, true, 2);
                qs.challengeValue = qs.targetMonster.challengeValue;
                qs.targetItem = qs.targetMonster.wantsItem;
                qs.itemIsGeneric = true;
                qs.targetMap = qs.targetMonster.GetActorMap();
                qs.targetMapID = qs.targetMap.mapAreaID;
                break;
            case QuestType.FINDITEM:
                // TODO: Don't do this if there are no unexplored maps.
                qs.targetItem = findItem;
                qs.targetItemID = findItem.actorUniqueID;
                if (findItem.collection != null)
                {
                    qs.targetActor = findItem.collection.Owner;
                    qs.targetActorID = qs.targetActor.actorUniqueID;
                }
                else
                {
                    Debug.Log(findItem.actorRefName + " has no collection?");
                }

                qs.targetMap = qs.targetActor.GetActorMap();
                qs.targetMapID = qs.targetMap.mapAreaID;
                float acv = qs.targetMap.dungeonLevelData.challengeValue;
                acv -= 0.2f;
                if (acv <= 1f)
                {
                    acv = 1.0f;
                }
                qs.challengeValue = acv;
                qs.GenerateQuestRewards(acv, false, 1);
                //qs.displayName = "<color=yellow>Retrieve " + findItem.displayName + "!</color>";
                //qs.questText = "It is said that <color=yellow>" + qs.targetActor.displayName + "</color> in <color=yellow>" + qs.targetMap.GetName() + "</color> is in possesion of a powerful item. Find it, and it's yours!";
                break;
            case QuestType.FINDAREA:
                qs.targetMap = targetMap;
                if (qs.targetMap == null)
                {
                    Debug.Log("Couldn't find target map.");
                    return null;
                }
                qs.targetMapID = qs.targetMap.mapAreaID;
                float areaCV = qs.targetMap.dungeonLevelData.challengeValue;
                areaCV -= 0.2f;
                if (areaCV <= 1f)
                {
                    areaCV = 1.0f;
                }
                qs.challengeValue = areaCV;
                qs.GenerateQuestRewards(areaCV, false, 1);
                qs.lowestPossibleFloor = qs.targetMap.effectiveFloor - 1;
                qs.highestPossibleFloor = qs.targetMap.effectiveFloor + 1;
                if (qs.lowestPossibleFloor < 0) qs.lowestPossibleFloor = 0;
                qs.lowestPossibleFloor += 1;
                qs.highestPossibleFloor += 1;
                //qs.displayName = "<color=yellow>Discover " + qs.targetMap.GetName() + "!</color>";
                //qs.questText = "There are rumors of a place called <color=yellow>" + qs.targetMap.GetName() + "</color> somewhere between floors " + lowestPossibleFloor + " and " + highestPossibleFloor + ". <color=yellow>Seek it out</color> and see what lies in wait!";
                break;
        }

        if (UnityEngine.Random.Range(0, 1f) <= QUEST_REQUIREMENTS_CHANCE && qs.qType != QuestType.BOSSGANG && GameMasterScript.heroPCActor.myStats.GetLevel() >= 4) // Chance to add a requirement
        {
            QuestRequirementTypes qrt = (QuestRequirementTypes)UnityEngine.Random.Range(0, (int)QuestRequirementTypes.COUNT);

            if (qs.targetMap == null)
            {
                if (qs.targetActor != null)
                {
                    qs.targetMap = qs.targetActor.GetActorMap();
                }
                else if (qs.targetItem != null)
                {
                    qs.targetMap = qs.targetItem.GetActorMap();
                }
                else if (qs.targetMonster != null)
                {
                    qs.targetMap = qs.targetMonster.GetActorMap();
                }
            }

            if ((qs.targetMap == null) || (qs.targetMap.effectiveFloor >= GameMasterScript.heroPCActor.lowestFloorExplored + 2))
            {
                tries = 0;
                while (qrt == QuestRequirementTypes.STEPSINDUNGEON)
                {
                    tries++;
                    if (tries > 500) return null;
                    qrt = (QuestRequirementTypes)UnityEngine.Random.Range(0, (int)QuestRequirementTypes.COUNT);
                }
            }

            QuestRequirement qr = new QuestRequirement();
            qr.qrType = qrt;
            switch (qrt)
            {
                case QuestRequirementTypes.STEPSINDUNGEON:
                    int possibleSteps = UnityEngine.Random.Range(250, 300);
                    int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored;
                    int diff = qs.targetMap.effectiveFloor - lowestFloor;
                    if (diff < 0)
                    {
                        diff = 0;
                    }
                    possibleSteps += (diff * 100);

                    qr.maxStepsInDungeon = possibleSteps;
                    qr.stepsTaken = 0;
                    qs.GenerateQuestRewards(qs.challengeValue + 0.1f, true, 2);
                    break;
                case QuestRequirementTypes.DAMAGETAKEN:
                    int maxDamTaken = (int)(GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 1.1f);
                    qr.maxDamageTaken = maxDamTaken;
                    qr.damageTaken = 0;
                    qs.GenerateQuestRewards(qs.challengeValue + 0.1f, true, 2);
                    break;
                case QuestRequirementTypes.NOFLASK:
                    qs.GenerateQuestRewards(qs.challengeValue + 0.2f, true, 3);
                    break;
                case QuestRequirementTypes.SAMEGEAR:
                    qs.GenerateQuestRewards(qs.challengeValue + 0.1f, true, 1);
                    break;
            }
            qs.qRequirements.Add(qr);
            // qs.challengeValue += 0.1f;
            qs.GenerateQuestRewards(qs.challengeValue, true, 1);
        }

        if (qs.targetMap == null)
        {
            //Debug.Log(qs.qType + " has no target map.");
        }
        else
        {
            if (qs.targetMap.mapIsHidden)
            {
                MapMasterScript.EnableMap(qs.targetMap);
            }
        }

        lastGeneratedQuestType = qs.qType;
        if (qs.targetItem != null)
        {
            lastTargetItemID = qs.targetItem.actorUniqueID;
        }
        if (qs.targetMonster != null)
        {
            lastTargetMonsterID = qs.targetMonster.actorUniqueID;
        }

        // For debug purposes, why are some quests generating with no rewards?

        return qs;
    }

    public static QuestMonsterMapPair FindMonsterToHuntForElementalLastHit()
    {
        monsterRefsToMaps.Clear();
        possibleMonsters.Clear();
        // As we find possible monsters, make sure to track where that monster might be found.
        actualMonsterCV = 0f;
        int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored - 2;
        if (lowestFloor >= 17)
        {
            lowestFloor = 16;
        }
        int maxFloor = GameMasterScript.heroPCActor.lowestFloorExplored + 1;
        if (lowestFloor < 17)
        {
            if (maxFloor > 18)
            {
                maxFloor = 18;
            }
            foreach (Map m in MapMasterScript.theDungeon.maps)
            {
                if (!CheckMapValidForQuest(m, lowestFloor, maxFloor, false, false)) continue;
                if (m.floor == 200) continue;
                if (m.floor == 150) continue;
                if (m.clearedMap) continue; // don't send player to cleared side areas, monsters cannot spawn there!
                if (m.effectiveFloor >= lowestFloor && m.effectiveFloor <= maxFloor && !m.dungeonLevelData.bossArea && m.dungeonLevelData.spawnTable != null)
                {
                    string monsterRefName = m.dungeonLevelData.spawnTable.GetRandomActorRef();
                    MonsterTemplateData checkTemplate = GameMasterScript.masterMonsterList[monsterRefName];
                    if (checkTemplate.cannotBeRumorTarget) continue;

                    // If this floor is a side area, confirm the monster is actually there.
                    if (m.dungeonLevelData.sideArea)
                    {
                        bool foundAny = false;
                        foreach (Monster monmon in m.monstersInMap)
                        {
                            if (monmon.actorRefName == monsterRefName)
                            {
                                foundAny = true;
                                break;
                            }
                        }
                        if (!foundAny)
                        {
                            continue;
                        }
                    }

                    possibleMonsters.Add(monsterRefName);
                    if (!monsterRefsToMaps.ContainsKey(monsterRefName))
                    {
                        monsterRefsToMaps.Add(monsterRefName, m);
                    }
                    if (actualMonsterCV < 1.0f)
                    {
                        Actor mon = m.FindActor(monsterRefName);
                        if (mon != null)
                        {
                            Monster mn = mon as Monster;
                            actualMonsterCV = mn.challengeValue;
                        }
                    }
                }
            }
        }
        if (possibleMonsters.Count > 0)
        {
            string findMon = possibleMonsters[UnityEngine.Random.Range(0, possibleMonsters.Count)];
            foreach (MonsterTemplateData mtd in GameMasterScript.masterMonsterList.Values) // was master SPAWNABLE list.
            {
                if (mtd.refName == findMon)
                {
                    //Debug.Log("Found monster: " + mtd.refName);
                    QuestMonsterMapPair monMapPair = new QuestMonsterMapPair(mtd, monsterRefsToMaps[mtd.refName]);
                    return monMapPair;
                }
            }
            //Debug.Log(findMon + " not found in spawnable monster list.");
            return null;
        }
        else
        {
            Debug.Log("No possible monsters to hunt for elem quest");
            return null;
        }

    }

    public static Item FindDreamWeaponToEnter()
    {
        List<Item> possibleItems = GetEmptyItemList();
        List<Item> backupItems = GetEmptyItemList();

        float currentPlayerCV = BalanceData.LEVEL_TO_CV[GameMasterScript.heroPCActor.myStats.GetLevel()];

        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.WEAPON) continue;
            Weapon w = itm as Weapon;

            // We are cv 1.6
            // Weapon is 1.8
            // Check (1.6) to (2.0)

            if (currentPlayerCV >= w.challengeValue - 0.2f && currentPlayerCV <= w.challengeValue + 0.2f)
            {
                possibleItems.Add(w);
            }
            else
            {
                backupItems.Add(w);
            }
        }

        Weapon mainhand = GameMasterScript.heroPCActor.myEquipment.GetWeapon();
        if (mainhand != null && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(mainhand, onlyActualFists: true)) // && mainhand.ReadActorData("dream_rumor") == -1)
        {
            if (currentPlayerCV >= mainhand.challengeValue - 0.2f && currentPlayerCV <= mainhand.challengeValue + 0.2f)
            {
                possibleItems.Add(mainhand);
            }
            else
            {
                backupItems.Add(mainhand);
            }

        }
        Weapon offhand = GameMasterScript.heroPCActor.myEquipment.GetOffhandWeapon();
        if (offhand != null && !GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(offhand, onlyActualFists: true)) // && offhand.ReadActorData("dream_rumor") == -1)
        {
            if (currentPlayerCV >= offhand.challengeValue - 0.2f && currentPlayerCV <= offhand.challengeValue + 0.2f)
            {
                possibleItems.Add(offhand);
            }
            else
            {
                backupItems.Add(offhand);
            }
        }

        if (possibleItems.Count == 0)
        {
            if (backupItems.Count == 0)
            {
                //Debug.Log("No dream weapon possible to enter.");
                ReturnItemListToStack(possibleItems);
                ReturnItemListToStack(backupItems);
                return null;
            }
            else
            {
                foreach (Item i in backupItems)
                {
                    possibleItems.Add(i);
                }
            }
        }
        else if (possibleItems.Count == 1)
        {
            foreach (Item i in backupItems)
            {
                possibleItems.Add(i);
            }
        }

        Item retI = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)];
        ReturnItemListToStack(possibleItems);
        ReturnItemListToStack(backupItems);
        return retI;
    }

    public static Map FindUndiscoveredMap()
    {
        //Debug.Log("Find undiscovered map.");
        List<Map> possibleMaps = GetEmptyMapList();
        int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored;
        int maxFloor = lowestFloor + 1;
        if (maxFloor > 18)
        {
            maxFloor = 18;
        }

        foreach (Map m in MapMasterScript.theDungeon.maps)
        {
            if (!CheckMapValidForQuest(m, 0, maxFloor, true, false, excludeBossAreas: true, excludeTownMaps: true)) continue;
            if (m.floor == 200) continue;
                
            if (m.dungeonLevelData.floor > 100 && m.effectiveFloor <= maxFloor && m.dungeonLevelData.minSpawnFloor != 99)
            {
                if (!GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(m.mapAreaID))
                {
                    bool valid = true;
                    foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
                    {
                        if (qs.targetMap == m)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                    {
                        possibleMaps.Add(m);
                    }
                }
            }
            
        }

        if (possibleMaps.Count == 0)
        {
            ReturnMapListToStack(possibleMaps);
            return null;
        }

        Map retMap = possibleMaps[UnityEngine.Random.Range(0, possibleMaps.Count)];
        ReturnMapListToStack(possibleMaps);
        return retMap;
    }

    public static Item FindItemToFind()
    {
        Item retItem = null;
        int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored - 2;
        int maxFloor = GameMasterScript.heroPCActor.lowestFloorExplored + 1;

        List<Item> possibleItems = GetEmptyItemList();

        List<string> priorityRefs = GameMasterScript.heroPCActor.GetMissingSetPieces();

        if (lowestFloor < 0)
        {
            lowestFloor = 0;
        }
        if (lowestFloor < 17)
        {
            if (maxFloor > 18)
            {
                maxFloor = 18;
            }
            bool done = false;
            foreach (Map m in MapMasterScript.theDungeon.maps)
            {
                if (m.floor == 110) continue; // Skip casino.
                if (m.floor == 200 || m.floor == 150) continue;
                if (!CheckMapValidForQuest(m, lowestFloor, maxFloor, false, false, true, true)) continue;
                if (m.floor == MapMasterScript.BOTTLES_AND_BREWS) continue;

                if (m.effectiveFloor >= lowestFloor && m.effectiveFloor <= maxFloor)
                {
                    foreach (Actor act in m.actorsInMap)
                    {
                        if (act.GetActorType() == ActorTypes.DESTRUCTIBLE || act.GetActorType() == ActorTypes.MONSTER)
                        {
                            if (act.GetActorType() == ActorTypes.DESTRUCTIBLE)
                            {
                                Destructible dt = act as Destructible;
                                if (dt.mapObjType == SpecialMapObject.TREASURESPARKLE) continue;
                            }
                            else
                            {
                                Monster checkMon = act as Monster;
                                if (checkMon.isBoss) continue;
                                if (checkMon.actorUniqueID == lastTargetMonsterID)
                                {
                                    continue;
                                }
                            }
                            int iCount = act.myInventory.GetActualInventoryCount();
                            if (iCount > 0)
                            {
                                foreach (Item itm in act.myInventory.GetInventory())
                                {
                                    if (itm.actorUniqueID == lastTargetItemID)
                                    {
                                        continue;
                                    }
                                    if ((int)itm.rarity >= (int)Rarity.MAGICAL || itm.actorRefName == "scroll_jobchange")
                                    {
                                        bool valid = true;
                                        foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
                                        {
                                            if (qs.qType == QuestType.FINDITEM)
                                            {
                                                if (qs.targetItem == itm || qs.itemReward == itm || qs.targetItemID == itm.actorUniqueID || qs.targetItem.actorRefName == itm.actorRefName)
                                                {
                                                    valid = false;
                                                }
                                            }
                                        }

                                        if (valid && !itm.ValidForPlayer())
                                        {
                                            valid = false;
                                        }

                                        if (valid)
                                        {
                                            if (priorityRefs.Contains(itm.actorRefName))
                                            {
                                                possibleItems.Clear();
                                                possibleItems.Add(itm);
                                                done = true;
                                                break;
                                            }
                                            possibleItems.Add(itm);
                                            if (possibleItems.Count > 5)
                                            {
                                                done = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (done) break;
                    }
                }
                if (done) break;
            }
        }

        if (possibleItems.Count == 0)
        {
            //Debug.Log("No possible good items.");
            ReturnItemListToStack(possibleItems);
            return null;
        }

        retItem = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)];
        ReturnItemListToStack(possibleItems);
        return retItem;
    }

    public static Monster FindChampionToKill(bool bossGang = false)
    {
        if (GameMasterScript.heroPCActor == null)
        {
            return null;
        }
        int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored - 1;

        if (lowestFloor > 16)
        {
            lowestFloor = 16;
        }

        if (bossGang)
        {
            lowestFloor = 0;
        }

        int maxFloor = lowestFloor + 1;

        if (bossGang)
        {
            maxFloor = GameMasterScript.heroPCActor.lowestFloorExplored;
        }

        List<Map> possibleMaps = GetEmptyMapList();
        List<Map> backupMaps = GetEmptyMapList();
        if ((lowestFloor < 17) || (bossGang))
        {
            if (maxFloor > 18)
            {
                maxFloor = 18;
            }
            foreach (Map m in MapMasterScript.theDungeon.maps)
            {
                // Skip casino.
                if (m.floor == 110 || m.floor == 150) continue;

                // Skip spiny maze because it's tricky
                if (m.floor == 145) continue;

                if (!CheckMapValidForQuest(m, lowestFloor, maxFloor, false, false, true)) continue;

                if (m.floor == MapMasterScript.BOTTLES_AND_BREWS) continue;
                if (m.effectiveFloor >= lowestFloor && m.effectiveFloor <= maxFloor && m.dungeonLevelData.spawnTable != null)
                {
                    if (m.unfriendlyMonsterCount >= MONSTER_RUMOR_HARDCAP) // Too many monsters here!
                    {
                        continue;
                    }

                    if (m.unfriendlyMonsterCount >= MONSTER_RUMOR_SOFTCAP) // Lotta monsters here already, only use this map if we have to.
                    {
                        backupMaps.Add(m);
                        continue;
                    }

                    if (bossGang)
                    {
                        possibleMaps.Add(m);
                        backupMaps.Add(m);
                    }
                    else
                    {
                        backupMaps.Add(m);
                        if (m.championCount > 0)
                        {
                            possibleMaps.Add(m);
                        }
                    }
                }
            }
        }
        if (backupMaps.Count == 0)
        {
            if (Debug.isDebugBuild) Debug.Log("No possible maps for monster to kill.");
            ReturnMapListToStack(possibleMaps);
            ReturnMapListToStack(backupMaps);
            return null;
        }
        Map targetMap = backupMaps[UnityEngine.Random.Range(0, backupMaps.Count)];
        Monster mn = null;

        if (bossGang)
        {
            if (possibleMaps.Count > 0)
            {
                targetMap = possibleMaps[UnityEngine.Random.Range(0, possibleMaps.Count)];
            }


            // First, the big boss.
            Map findHighestFloor = MapMasterScript.theDungeon.FindFloor(maxFloor);
            if (findHighestFloor == null || findHighestFloor.dungeonLevelData == null || findHighestFloor.dungeonLevelData.spawnTable == null)
            {
                return null;
            }

            ActorTable spawnTable = findHighestFloor.dungeonLevelData.spawnTable;
            string bossToSpawn = spawnTable.GetRandomActorRef();
            string partner1 = spawnTable.GetRandomActorRef();
            string partner2 = spawnTable.GetRandomActorRef();

            Monster boss = MonsterManagerScript.CreateMonster(bossToSpawn, true, true, false, 0f, false);
            boss.isBoss = true;
            boss.MakeChampion();
            boss.MakeChampion();
            boss.MakeChampion();
            if (boss.challengeValue >= 1.6f)
            {
                boss.MakeChampion();
            }
            Monster bossPartner1 = MonsterManagerScript.CreateMonster(partner1, true, true, false, 0f, false);
            bossPartner1.MakeChampion();
            if (boss.challengeValue >= 1.6f)
            {
                bossPartner1.MakeChampion();
            }

            Monster bossPartner2 = MonsterManagerScript.CreateMonster(partner2, true, true, false, 0f, false);
            bossPartner2.MakeChampion();
            if (boss.challengeValue >= 1.6f)
            {
                bossPartner2.MakeChampion();
            }

            MapMasterScript.activeMap.OnEnemyMonsterSpawned(targetMap, boss, true);
            MapMasterScript.activeMap.OnEnemyMonsterSpawned(targetMap, bossPartner1, true);
            MapMasterScript.activeMap.OnEnemyMonsterSpawned(targetMap, bossPartner2, true);

            Stairs randomStaircase = targetMap.mapStairs[UnityEngine.Random.Range(0, targetMap.mapStairs.Count)];

            MapTileData bossPlace = targetMap.GetRandomEmptyTile(randomStaircase.GetPos(), 8, true, false);
            if (bossPlace == null) // 312019 - Make sure we have an empty tile to use.
            {
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            targetMap.PlaceActor(boss, bossPlace);

            MapTileData partnerPlace = targetMap.GetRandomEmptyTile(bossPlace.pos, 2, true, true);
            if (partnerPlace == null) // 312019 - Make sure we have an empty tile to use.
            {
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            targetMap.PlaceActor(bossPartner1, partnerPlace);
            partnerPlace = targetMap.GetRandomEmptyTile(bossPlace.pos, 2, true, true);
            if (partnerPlace == null) // 312019 - Make sure we have an empty tile to use.
            {
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            targetMap.PlaceActor(bossPartner2, partnerPlace);

            bossPartner1.anchorRange = 2;
            bossPartner1.anchorID = boss.actorUniqueID;
            bossPartner1.anchor = boss;

            bossPartner2.anchorRange = 2;
            bossPartner2.anchorID = boss.actorUniqueID;
            bossPartner2.anchor = boss;

            ReturnMapListToStack(possibleMaps);
            ReturnMapListToStack(backupMaps);
            return boss;

        }

        if (possibleMaps.Count == 0)
        {
            if (targetMap == null)
            {
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            mn = targetMap.SpawnRandomMonster(true, false);
            if (mn == null)
            {
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            mn.MakeChampion();
            for (int i = 1; i < targetMap.dungeonLevelData.maxChampionMods; i++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f)
                {
                    mn.MakeChampion();
                }
                else
                {
                    break;
                }
            }
            ReturnMapListToStack(possibleMaps);
            ReturnMapListToStack(backupMaps);
            return mn;
        }
        else
        {
            targetMap = possibleMaps[UnityEngine.Random.Range(0, possibleMaps.Count)];
            foreach (Actor act in targetMap.actorsInMap)
            {
                bool skip = false;
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    mn = act as Monster;
                    foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
                    {
                        if (qs.qType != QuestType.KILLCHAMPION && qs.qType != QuestType.BOSSGANG) continue;

                        if (mn == qs.targetMonster)
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;
                    if (mn.isChampion)
                    {
                        //if (Debug.isDebugBuild) Debug.Log("Found monster to kill: " + mn.actorRefName);
                        ReturnMapListToStack(possibleMaps);
                        ReturnMapListToStack(backupMaps);
                        return mn;
                    }
                }
            }

            targetMap = backupMaps[UnityEngine.Random.Range(0, backupMaps.Count)];
            mn = targetMap.SpawnRandomMonster(true, false);
            if (mn == null)
            {
                // No possible place to spawn this monster
                ReturnMapListToStack(possibleMaps);
                ReturnMapListToStack(backupMaps);
                return null;
            }
            mn.MakeChampion();
            for (int i = 1; i < targetMap.dungeonLevelData.maxChampionMods; i++)
            {
                if (UnityEngine.Random.Range(0, 1f) <= 0.5f)
                {
                    mn.MakeChampion();
                }
                else
                {
                    break;
                }
            }
            //if (Debug.isDebugBuild) Debug.Log("Found monster to kill: " + mn.actorRefName);
            ReturnMapListToStack(possibleMaps);
            ReturnMapListToStack(backupMaps);
            return mn;
        }

        /*
        //Shep: Actually it can't cause all your previous cases return values.
        Debug.Log("No champ monster found? This shouldn't ever happen");
        return null;
        */
    }

    public static Monster FindNonChampionMonsterForQuest()
    {
        Monster retMonster = null;
        int lowestFloor = GameMasterScript.heroPCActor.lowestFloorExplored - 1;
        int maxFloor = GameMasterScript.heroPCActor.lowestFloorExplored + 1;
        List<Monster> possibleMonsters = GetEmptyMonsterList();
        if (lowestFloor < 0)
        {
            lowestFloor = 0;
        }
        if (lowestFloor >= 17)
        {
            lowestFloor = 16;
        }
        if (lowestFloor < 17)
        {
            if (maxFloor > 18)
            {
                maxFloor = 18;
            }
            bool done = false;
            foreach (Map m in MapMasterScript.theDungeon.maps)
            {
                if (m.floor == 110) continue; // Skip casino.
                if (m.floor == 200 || m.floor == 150) continue;
                if (m.floor == MapMasterScript.BEASTLAKE_SIDEAREA) continue;
                if (m.floor == MapMasterScript.BOTTLES_AND_BREWS) continue;

                if (!CheckMapValidForQuest(m, lowestFloor, maxFloor, false, false, true, true)) continue;

                if (m.effectiveFloor >= lowestFloor && m.effectiveFloor <= maxFloor)
                {
                    foreach (Actor act in m.actorsInMap)
                    {
                        if (act.GetActorType() == ActorTypes.MONSTER)
                        {
                            Monster mn = act as Monster;
                            if (mn.isChampion) continue;
                            if (mn.isBoss) continue;
                            if (mn.moveRange == 0) continue;
                            if (mn.turnsToDisappear > 0) continue;
                            if (mn.myTemplate.cannotBeRumorTarget) continue;
                            if (mn.foodLovingMonster) continue;
                            if (mn.actorRefName == "mon_grovepest") continue;
                            if (mn.actorfaction == Faction.PLAYER) continue;
                            if (mn.ReadActorData("monstertotame") == 1) continue;
                            possibleMonsters.Add(mn);
                        }
                        if (done) break;
                    }
                }
                if (done) break;
            }
        }

        if (possibleMonsters.Count == 0)
        {
            //Debug.Log("No possible monsters to appease.");
            ReturnMonsterListToStack(possibleMonsters);
            return null;
        }

        retMonster = possibleMonsters[UnityEngine.Random.Range(0, possibleMonsters.Count)];

        SetItemForMonsterAppeasement();
        ReturnMonsterListToStack(possibleMonsters);
        return retMonster;
    }

    public static Monster FindMonsterToTame()
    {
        Monster mn = FindNonChampionMonsterForQuest();

        if (mn == null)
        {
            return null;
        }

        return mn;
    }

    public static Monster FindMonsterToAppease()
    {
        Monster mn = FindNonChampionMonsterForQuest();
        if (mn == null) return null;
        SetItemForMonsterAppeasement();
        return mn;
    }

    static void SetItemForMonsterAppeasement()
    {
        Item desireItem = null;
        ActorTable possibleDesireItems = LootGeneratorScript.GetLootTable("monster_food_loving");

        desireItem = GameMasterScript.masterItemList[possibleDesireItems.GetRandomActorRef()];
        desiredItemByMonster = desireItem;
        //Debug.Log("Find monster to appease: " + retMonster.actorRefName);
    }

    public static int GetUnexploredCombatSideAreaCount(bool scaleToLevel = false)
    {
        List<Map> m = GetUnexploredCombatSideAreas(scaleToLevel);
        return m.Count;
    }

    public static List<Map> GetUnexploredCombatSideAreas(bool scaleToLevel = false)
    {
        List<Map> possible = GetEmptyMapList();
        foreach (Map m in MapMasterScript.theDungeon.maps)
        {
            if (m.floor == 110) continue; // Skip casino.

            if (!CheckMapValidForQuest(m, 0, 99, true, false, true, true)) continue;

            if (m.floor == MapMasterScript.BOTTLES_AND_BREWS) continue;

            if (!m.IsMainPath() && !m.IsItemWorld() && !m.dungeonLevelData.safeArea)
            {
                if (!m.clearedMap && !GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(m.mapAreaID))
                {
                    if (m.unfriendlyMonsterCount > 0)
                    {
                        if (GameMasterScript.heroPCActor.lowestFloorExplored >= m.dungeonLevelData.minSpawnFloor)
                        {
                            if (scaleToLevel)
                            {
                                if (m.dungeonLevelData.effectiveFloor >= GameMasterScript.heroPCActor.lowestFloorExplored + 1)
                                {
                                    continue;
                                }
                                // Is the nearest main level floor too high for us? Also invalid.
                                Map connecting = m.GetNearbyPathMap();
                                if (connecting.effectiveFloor >= GameMasterScript.heroPCActor.lowestFloorExplored + 1)
                                {
                                    continue;
                                }
                            }
                            possible.Add(m);
                        }
                    }
                }
            }
        }
        return possible;
    }

    static bool CheckMapValidForQuest(Map m, int lowestFloor, int maxFloor, bool newMapDiscovery = false, 
        bool bossGang = false, bool excludeBossAreas = false, bool excludeTownMaps = false)
    {
        // Skip other special areas, or areas that are too hard or easy
        if (!m.ValidForQuest(lowestFloor, maxFloor)) return false;

        // Skip maps that are tagged... exclude
        if (m.dungeonLevelData.excludeFromRumors) return false;

        if (excludeTownMaps && m.IsTownMap()) return false;

        if (m.dungeonLevelData.bossArea && excludeBossAreas) return false;

        // Don't spawn new monsters into an area that has already been cleared
        if (m.dungeonLevelData.sideArea && bossGang) return false;

        // Skip Realm of the Gods if we haven't unlocked it yet
        if (m.IsRealmOfGodsAndNotUnlocked()) return false;

        // Skip null maps
        if (m.dungeonLevelData == null) return false;

        // Skip maps that have no spawn table at all
        if (m.dungeonLevelData.spawnTable == null) return false;

        // Skip inaccessible maps
        if (m.mapStairs.Count == 0) return false;

        if (RandomJobMode.IsCurrentGameInRandomJobMode() && !RandomJobMode.IsFloorEnabled(m.floor))
        {
            return false;
        }

        // If the player is exploring Fungal Caves, OR the bandit branch,
        // Don't spawn rumors in the other branch
        if (!newMapDiscovery)
        {
            if (!CheckForBranch1Fail(m)) return false;
            if (!CheckForBranch2Fail(m)) return false;
        }

        return true;
    }

    static bool CheckForBranch1Fail(Map m)
    {
        bool isPartOfFungalCaves = m.dungeonLevelData.floor >= 135 && m.dungeonLevelData.floor <= 138;
        bool isPartOfAmberStation = m.dungeonLevelData.floor >= 6 && m.dungeonLevelData.floor <= 9;

        if (isPartOfAmberStation || isPartOfFungalCaves)
        {
            bool hasPlayerExploredFungalCaves = GameMasterScript.heroPCActor.HasExploredMapFloorRange(135, 138);
            bool hasPlayerExploredAmberStation = GameMasterScript.heroPCActor.HasExploredMapFloorRange(6, 9);

            // The map we are evaluating is part of the amber station branch
            if (isPartOfAmberStation)
            {
                // See if the player has explored fungal caves
                if (hasPlayerExploredFungalCaves)
                {
                    // If we HAVE explored fungal caves
                    // But we've NEVER explored amber station, then this map should not be valid.
                    if (!hasPlayerExploredAmberStation)
                    {
                        //Debug.Log("Don't allow rumors in amber station because we've been to fungal, and not amber.");
                        return false;
                    }
                }
            }
            // The map we are evaluating is part of the fungal caverns branch
            else if (isPartOfFungalCaves)
            {
                // See if the player has explored amber station
                if (hasPlayerExploredAmberStation)
                {
                    // If we HAVE explored amber station
                    // But we've NEVER explored fungal caves, then this map should not be valid.
                    if (!hasPlayerExploredFungalCaves)
                    {
                        //Debug.Log("Don't allow rumors in fungal because we've been to amber, and not fungal.");
                        return false;
                    }
                }
            }
        }

        return true;
    }

    static bool CheckForBranch2Fail(Map m)
    {
        bool isPartOfAncientRuins = m.dungeonLevelData.floor >= 151 && m.dungeonLevelData.floor <= 154;
        bool isPartOfStonehewnHalls = m.dungeonLevelData.floor >= 11 && m.dungeonLevelData.floor <= 14;

        if (isPartOfStonehewnHalls || isPartOfAncientRuins)
        {
            bool hasPlayerExploredAncientRuins = GameMasterScript.heroPCActor.HasExploredMapFloorRange(151, 154);
            bool hasPlayerExploredStonehewnHalls = GameMasterScript.heroPCActor.HasExploredMapFloorRange(11, 14);

            // The map we are evaluating is part of the stonehewn halls branch
            if (isPartOfStonehewnHalls)
            {
                // See if the player has explored ancient ruins
                if (hasPlayerExploredAncientRuins)
                {
                    // If we HAVE explored ancient ruins
                    // But we've NEVER explored stonehewn halls, then this map should not be valid.
                    if (!hasPlayerExploredStonehewnHalls)
                    {
                        //Debug.Log("Don't allow rumors in stonehewn halls because we've been to ancient ruins, and not stonehewn.");
                        return false;
                    }
                }
            }
            // The map we are evaluating is part of the ancient ruins branch
            else if (isPartOfAncientRuins)
            {
                // See if the player has explored stonehewn halls
                if (hasPlayerExploredStonehewnHalls)
                {
                    // If we HAVE explored stonehewn halls
                    // But we've NEVER explored ancient ruins, then this map should not be valid.
                    if (!hasPlayerExploredAncientRuins)
                    {
                        //Debug.Log("Don't allow rumors in ancient ruins because we've been to stonehewn, and not ancient ruins.");
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
