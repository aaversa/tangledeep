using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MetaProgressScript
{
    public static TamedCorralMonster ReleaseMonsterFromCorral(int index)
    {
        if (index >= localTamedMonstersForThisSlot.Count)
        {
            Debug.Log("Index " + index + " greater than corral count " + localTamedMonstersForThisSlot.Count);
            return null;
        }

        TamedCorralMonster tcm = localTamedMonstersForThisSlot[index];

        if (tcm.monsterObject == null)
        {
            Debug.Log(tcm.refName + " " + tcm.monsterID + " has no attached monster object?");
            return null;
        }

        localTamedMonstersForThisSlot.Remove(tcm);

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);

        StringManager.SetTag(0, tcm.monsterObject.displayName);
        GameLogScript.LogWriteStringRef("log_release_monster");

        if (tcm.happiness > 8 && UnityEngine.Random.Range(0, 1f) <= MonsterCorralScript.CHANCE_HAPPY_RLS_MONSTER_SAVE)
        {
            List<MonsterPowerData> allAbils = tcm.monsterObject.GetMonsterLetterWritableAbilities();
            if (allAbils.Count > 0)
            {
                ReleasedMonster rm = new ReleasedMonster();
                rm.displayName = tcm.monsterObject.displayName;
                rm.firstOwner = tcm.catcherName;
                if (string.IsNullOrEmpty(rm.firstOwner))
                {
                    rm.firstOwner = GameMasterScript.heroPCActor.displayName;
                }
                rm.dayReleased = MetaProgressScript.totalDaysPassed;

                MonsterPowerData selectedMPD = allAbils[UnityEngine.Random.Range(0, allAbils.Count)];

                rm.teachAbilityRef = selectedMPD.abilityRef.refName;

                rm.mpd = new MonsterPowerData();
                rm.mpd.CopyFromTemplate(selectedMPD, selectedMPD.abilityRef);

                MetaProgressScript.releasedMonsters.Add(rm);
                RemoveCorralMonsterFromAttractionList(tcm);
                //Debug.Log("Saved released monster: " + rm.displayName + " with power " + selectedMPD.abilityRef.refName);
            }

        }

        if (MapMasterScript.singletonMMS.townMap2 != MapMasterScript.activeMap)
        {
            MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(tcm.monsterObject);
        }
        else
        {
            Monster mn = tcm.monsterObject as Monster;
            /* mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
            GameMasterScript.AddToDeadQueue(mn);
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap); */
            MapMasterScript.activeMap.RemoveActorFromLocation(mn.GetPos(), mn);
            MapMasterScript.activeMap.RemoveActorFromMap(mn);
            mn.myMovable.FadeOutThenDie();

        }
        MetaProgressScript.ResetMonsterQuips();


        return tcm;
    }

    static void RemoveCorralMonsterFromAttractionList(TamedCorralMonster tcm)
    {
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        for (int i = 0; i < maxMonsterCount; i++)
        //foreach (TamedCorralMonster mon in localTamedMonstersForThisSlot)
        {
            TamedCorralMonster mon = localTamedMonstersForThisSlot[i];
            mon.RemoveMonsterFromAttractionDict(tcm.sharedBankID);
        }
    }

    public static void DevelopTamedMonsterRelationships(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

            //foreach (TamedCorralMonster monsterA in localTamedMonstersForThisSlot)
            for (int a = 0; a < maxMonsterCount; a++)
            {
                TamedCorralMonster monsterA = localTamedMonstersForThisSlot[a];
                //foreach (TamedCorralMonster monsterB in localTamedMonstersForThisSlot)
                for (int b = 0; b < maxMonsterCount; b++)
                {
                    TamedCorralMonster monsterB = localTamedMonstersForThisSlot[b];
                    if (monsterA == monsterB) continue;

                    if (!monsterA.attractionToMonsters.ContainsKey(monsterB.sharedBankID))
                    {
                        // Start a new relationship.
                        monsterA.StartRelationship(monsterB, true);
                    }
                    else
                    {
                        monsterA.DevelopRelationship(monsterB);
                    }
                }
            }
        }
    }

    public static void TickAllMonsters()
    {
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        //foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = localTamedMonstersForThisSlot[i];
            tcm.CheckTimePassed();
        }
    }

    public static bool CheckIfPlayerHasCreatureAsPet(string monsterRef)
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject.actorRefName == monsterRef) return true;
        }
        if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.heroPCActor.CheckSummonRefs(monsterRef))
        {
            return true;
        }
        return false;
    }

    public static bool IsMonsterInCorralByID(int id)
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterID == id)
            {
                return true;
            }
            if (tcm.monsterObject != null && tcm.monsterObject.actorUniqueID == id)
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsMonsterInCorral(Monster mn)
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == mn)
            {
                return true;
            }
        }

        return false;
    }

    public static void CreateTamedMonster(Monster corralMonster)
    {        
        if (corralMonster.tamedMonsterStuff != null)
        {
            if (Debug.isDebugBuild) Debug.Log(corralMonster.actorRefName + " " + corralMonster.actorUniqueID + " " + corralMonster.displayName + " already has TCM info.");
        }
        else
        {
            TamedCorralMonster cm = new TamedCorralMonster();
            cm.refName = corralMonster.actorRefName;
            cm.monsterObject = corralMonster;

            cm.sharedBankID = SharedCorral.GetUniqueSharedPetID();

            Debug.Log("Creating tamed monster: " + corralMonster.displayName + " with shared bank ID " + cm.sharedBankID);

            bool wildUntamed = corralMonster.ReadActorData("tcmrarityup") > 0;

            // If we've observed this creature as a Wild Child, we've also set its unique value, so use that instead.
            int existingRarityValue = corralMonster.ReadActorData("runiq");
            if (existingRarityValue >= 0)
            {
                cm.unique = existingRarityValue;
            }
            else if (wildUntamed)
            {
                cm.unique = UnityEngine.Random.Range(70, 101);
            }

            if (wildUntamed) cm.beauty = UnityEngine.Random.Range(70, 101);

            corralMonster.RemoveAllChampionModsAndAbilities();

            // Upon first being caught, let's give corral monsters more base health.
            // First, figure out where it's health lives on the existing curve
            float expectedHealth = BalanceData.expectedMonsterHealth[cm.monsterObject.myStats.GetLevel()];

            float delta = cm.monsterObject.myStats.GetMaxStat(StatTypes.HEALTH) - expectedHealth;

            cm.monsterObject.myStats.SetStat(StatTypes.HEALTH, BalanceData.expectedMonsterHealth[(int)cm.monsterObject.myStats.GetLevel()], StatDataTypes.ALL, true);
            cm.monsterObject.myStats.ChangeStat(StatTypes.HEALTH, delta, StatDataTypes.ALL, true);

            //cm.displayName = corralMonster.displayName;
            cm.catcherName = GameMasterScript.heroPCActor.displayName;
            cm.SetFoodPreferences();
            cm.family = corralMonster.monFamily;
            cm.monsterID = corralMonster.actorUniqueID;
            cm.AdjustWeightFromTemplate();
            cm.baseMonsterHealth = corralMonster.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.TRUEMAX);
            corralMonster.tamedMonsterStuff = cm;
            corralMonster.myStats.SetXPFlat(corralMonster.myStats.GetXPToCurrentLevel());
        }
        if (!localTamedMonstersForThisSlot.Contains(corralMonster.tamedMonsterStuff))
        {
            if (corralMonster.tamedMonsterStuff == null)
            {
                if (Debug.isDebugBuild) Debug.Log(corralMonster.actorRefName + " still has null TCM?");
                return;
            }

            AddPetToLocalSlotCorralList(corralMonster.tamedMonsterStuff);

            int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

            GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);
            if (Debug.isDebugBuild) Debug.Log("Adding new monster to corral! " + corralMonster.actorUniqueID + " " + corralMonster.actorRefName);
            UIManagerScript.PlayCursorSound("CookingSuccess");
        }
    }

    public static Monster SpawnMonsterActorInCorral(string monRef)
    {
        Monster corralMonster = MonsterManagerScript.CreateMonster(monRef, false, false, false, 0f, true);
        corralMonster.surpressTraits = true;
        corralMonster.actorfaction = Faction.PLAYER;

        MoveMonsterActorIntoCorral(corralMonster);

        if (corralMonster.actorUniqueID == 0)
        {
            corralMonster.SetUniqueIDAndAddToDict();
        }
        MapMasterScript.singletonMMS.townMap2.AddActorToMap(corralMonster);
        MetaProgressScript.ResetMonsterQuips();
        return corralMonster;
    }

    public static void MoveMonsterActorIntoCorral(Monster corralMonster)
    {
        //Debug.Log(corralMonster.actorRefName + " " + corralMonster.actorUniqueID + " " + corralMonster.dungeonFloor);
        MapMasterScript.singletonMMS.townMap2.RemoveActorFromLocation(corralMonster.GetPos(), corralMonster);
        int minX = 4;
        int maxX = 8;
        int minY = 6;
        int maxY = 7;
        int spawnPosX = UnityEngine.Random.Range(minX, maxX + 1);
        int spawnPosY = UnityEngine.Random.Range(minY, maxY + 1);
        Vector2 spawnPos = new Vector2(spawnPosX, spawnPosY);
        int tries = 0;
        while (MapMasterScript.singletonMMS.townMap2.mapArray[spawnPosX, spawnPosY].IsCollidable(corralMonster))
        {
            tries++;
            spawnPosX = UnityEngine.Random.Range(minX, maxX + 1);
            spawnPosY = UnityEngine.Random.Range(minY, maxY + 1);
            spawnPos = new Vector2(spawnPosX, spawnPosY);
            if (tries > 500)
            {
                //Debug.Log("No room for monster! They will overlap.");
                break;
            }
        }
        corralMonster.SetCurPos(spawnPos);
        corralMonster.SetSpawnPos(spawnPos);
        corralMonster.isInCorral = true;

        corralMonster.anchor = null;
        corralMonster.anchorID = 0;

        if (!MapMasterScript.singletonMMS.townMap2.IsActorObjectInMap(corralMonster))
        {
            MapMasterScript.singletonMMS.townMap2.AddActorToMap(corralMonster);
        }
        

        MapMasterScript.singletonMMS.townMap2.AddActorToLocation(spawnPos, corralMonster);

        if (MapMasterScript.activeMap != null)
        {
            if (!corralMonster.objectSet && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                MapMasterScript.singletonMMS.SpawnMonster(corralMonster);
            }
            if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                corralMonster.myMovable.SetPosition(corralMonster.GetPos());
            }
        }


        //Debug.Log("Now " + corralMonster.actorUniqueID + " is in " + MapMasterScript.singletonMMS.townMap2.floor);
        //Debug.Log(MapMasterScript.singletonMMS.townMap2.FindActorByID(corralMonster.actorUniqueID) == null);

    }

    public static void UndestroyAllCorralMonsters()
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject != null)
            {
                tcm.monsterObject.destroyed = false;
                tcm.monsterObject.surpressTraits = false;
            }
        }
        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.MONSTER && !GameMasterScript.heroPCActor.CheckSummon(act))
            {
                Monster m = act as Monster;
                m.destroyed = false;
                m.surpressTraits = false;
            }
        }
    }

    public static TamedCorralMonster GetTamedCorralMonsterByActorRef(Monster mon)
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == mon)
            {
                return tcm;
            }
        }

        Debug.Log("WARNING! Could not find " + mon.actorRefName + " in corral.");
        return null;
    }

    public static void AddExistingTamedMonsterActorToCorral(Monster monToAdd)
    {
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        if (Debug.isDebugBuild) Debug.Log("Adding existing tamed monster actor to corral. " + monToAdd.PrintCorralDebug());

        /*
        if (localTamedMonstersForThisSlot.Count == MonsterCorralScript.MAX_MONSTERS_IN_CORRAL)
        {
            Debug.Log("ERROR: Should not be possible to add another monster to corral. Why is this happening?");
            return;
        }
        */

        monToAdd.isInCorral = true;
        monToAdd.surpressTraits = false;
        if (monToAdd.tamedMonsterStuff == null)
        {
            Debug.LogError("WARNING: " + monToAdd.actorUniqueID + " " + monToAdd.actorRefName + " has null TCM.");
            return;
        }

        AddPetToLocalSlotCorralList(monToAdd.tamedMonsterStuff, 0);

        // update tamed corral monster's base health, as it may have leveled up during its adventuring
        monToAdd.tamedMonsterStuff.baseMonsterHealth = monToAdd.myStats.GetMaxStat(StatTypes.HEALTH);

        if (Debug.isDebugBuild) Debug.Log("Finished adding tamed monster to corral! " + monToAdd.PrintCorralDebug());
        MoveMonsterActorIntoCorral(monToAdd);

        maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);
    }

    public static void AddMonsterActorToCorral(Monster monToAdd, bool firstTimeAdded, bool createTamedComponent = true)
    {
        if (localTamedMonstersForThisSlot.Count >= MonsterCorralScript.MAX_MONSTERS_IN_CORRAL && firstTimeAdded)
        {
            Debug.Log("ERROR: Should not be possible to add another monster to corral. Why is this happening?");
            return;
        }

        if (firstTimeAdded)
        {
            monToAdd.ReverseMalletEffect();
            monToAdd.myStats.HealToFull();
            monToAdd.actorfaction = Faction.PLAYER;
            monToAdd.bufferedFaction = Faction.PLAYER;
            monToAdd.myStats.RemoveTemporaryNegativeStatusEffects(); // Hopefully doing this will not somehow kill the monster?
            GameMasterScript.heroPCActor.RemoveSummon(monToAdd);
            monToAdd.summoner = null;
            monToAdd.anchor = null;
            monToAdd.RemoveAttribute(MonsterAttributes.CANTACT);
            monToAdd.moveRange = 1;
            monToAdd.pushedThisTurn = false;
            monToAdd.cachedBattleData.maxMoveRange = 1;
        }

        monToAdd.isInCorral = true;
        monToAdd.surpressTraits = false;

        if (createTamedComponent)
        {
            CreateTamedMonster(monToAdd);
        }

        MoveMonsterActorIntoCorral(monToAdd);
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);

        foreach (QuestScript qs in GameMasterScript.heroPCActor.myQuests)
        {
            if (qs.qType == QuestType.TAMEMONSTER)
            {
                if (qs.targetMonster == monToAdd || qs.targetMonsterID == monToAdd.actorUniqueID)
                {
                    if (monToAdd.displayName.Contains(StringManager.GetString("misc_monster_wilduntamed")))
                    {
                        monToAdd.displayName = monToAdd.myTemplate.monsterName;
                    }
                    GameMasterScript.gmsSingleton.StartCoroutine(QuestScript.WaitThenCompleteQuest(qs, 0.05f));
                    break;
                }
            }
        }
    }

    public static void VerifyTamedMonstersHaveActorsOnGroveMapLoad()
    {
        // Verify that monsters in TamedCorral have actors too
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //if (Debug.isDebugBuild) Debug.Log("Compare " + SharedCorral.tamedMonstersSharedWithAllSlots.Count + " " + localTamedMonstersForThisSlot.Count + " " + (SharedCorral.tamedMonstersSharedWithAllSlots == localTamedMonstersForThisSlot));

        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (Debug.isDebugBuild) Debug.Log("Check that " + tcm.monsterObject.PrintCorralDebug() + " has an actor on grove map load.");
            Actor outActor;
            if (!MapMasterScript.activeMap.FindIdenticalActor(tcm.monsterObject, out outActor))
            {
                if (Debug.isDebugBuild) Debug.Log("Player has a pet " + tcm.monsterObject.PrintCorralDebug() + " in the shared corral, but that pet does not appear to be in the map."); 

                if (tcm.monsterObject == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Shared corral pet monster object is NULL? " + tcm.refName + "," + tcm.monsterID + "," + tcm.catcherName + " so we must create new one.");
                    Monster newMonster = MonsterManagerScript.CreateMonster(tcm.refName, false, false, false, 0f, false);
                    MapMasterScript.activeMap.AddActorToMap(newMonster);
                    newMonster.actorUniqueID = tcm.monsterID;
                    newMonster.tamedMonsterStuff = tcm;
                    AddMonsterActorToCorral(newMonster, true, false);
                    newMonster.surpressTraits = true;
                    newMonster.isInCorral = true;
                    newMonster.RemoveAttribute(MonsterAttributes.CANTACT);
                    newMonster.moveRange = 1;
                    newMonster.pushedThisTurn = false;
                }
                else
                {
                    MoveMonsterActorIntoCorral(tcm.monsterObject);
                    if (Debug.isDebugBuild) Debug.Log("Added the existing monster object " + tcm.monsterObject.PrintCorralDebug() + " from shared data, to the local file.");
                }
            }
            else
            {
                // We found a local monster object that has the same shared pet ID as our shared progress file
                // In this case, we need to update the local version based on shared progress, which may have changed
                //if (Debug.isDebugBuild) Debug.Log(tcm.monsterObject.actorRefName + "," + tcm.sharedBankID + "," + tcm.monsterObject.displayName + " already on the map, so let's make sure it loads from shared data");
                //GameObject go = outActor.GetObject();
                //outActor.CopyFromTemplate()
            }
        }
    }

    public static void ClearQuestMarkersFromCorralPets()
    {
        // Remove quest markers from pets because... yeah.
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == null) continue;
            tcm.monsterObject.myStats.ForciblyRemoveStatus("enemy_quest_target");
        }
    }

    public static void ValidateTamedMonstersInCorralOnGameLoad()
    {

        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.actorsInMap)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if (mn.surpressTraits && !GameMasterScript.heroPCActor.summonedActors.Contains(mn))
                {
                    bool foundMonInMeta = false;
                    foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
                    {
                        if (tcm.monsterID == act.actorUniqueID)
                        {
                            foundMonInMeta = true;
                            //Debug.Log("We found " + mn.actorUniqueID + " in townmap2, and matched in meta");
                            break;
                        }
                    }
                    if (!foundMonInMeta)
                    {
                        CreateTamedMonster(mn);
                        if (Debug.isDebugBuild) Debug.Log("Linked " + mn.actorRefName + " to NEW corral system");
                    }
                }
            }
        }

    }

    public static void AddAllTamedMonstersToDictionary()
    {
        if (GameMasterScript.gmsSingleton.ReadTempGameData("tamed_finished") == 1) return;

        foreach(TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (Debug.isDebugBuild) Debug.Log(tcm.monsterObject.PrintCorralDebug());
            GameMasterScript.AddActorToDict(tcm.monsterObject);
        }
    }

    public static void AddAllTamedSharedMonstersToDictionary()
    {
        if (Debug.isDebugBuild) Debug.Log("Adding all tamed shared monsters to dictionary, one-time.");
        foreach (TamedCorralMonster tcm in SharedCorral.tamedMonstersSharedWithAllSlots)
        {
            if (tcm.monsterObject.actorUniqueID < SharedBank.CORRAL_ID_ASSIGNER_BASE)
            {
                GameMasterScript.AssignActorID(tcm.monsterObject, 900000);
                Debug.Log("Reassigned monster ID while adding to corral, is now " + tcm.monsterObject.actorUniqueID + "/" + tcm.monsterID + ", new debug info is: " + tcm.monsterObject.PrintCorralDebug());
            }

            //if (Debug.isDebugBuild) Debug.Log(tcm.monsterObject.PrintCorralDebug());
            GameMasterScript.AddActorToDict(tcm.monsterObject);
        }

        GameMasterScript.gmsSingleton.SetTempGameData("tamed_finished", 1);
    }

    


    public static void VerifyMonstersInCorralShouldBeThere()
    {
        // also verify corral pet health.
        Monster mPet = GameMasterScript.heroPCActor.GetMonsterPet();
        if (mPet != null)
        {
            mPet.VerifyMaxHealthAsCorralPet();
        }

        if (Debug.isDebugBuild) Debug.Log("ON GAME LOAD: Verifying " + localTamedMonstersForThisSlot.Count + " corral monsters.");

        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            if (tcm.monsterObject == null) continue;
            //if (Debug.isDebugBuild) Debug.Log(tcm.refName + " " + tcm.monsterObject.displayName + " " + tcm.monsterObject.actorUniqueID);
            tcm.monsterObject.VerifyMaxHealthAsCorralPet();
            if (tcm.monsterObject.monFamily != tcm.family && !string.IsNullOrEmpty(tcm.family))
            {
                tcm.monsterObject.monFamily = tcm.family;
            }
        }

        List<Monster> actorsToRemove = new List<Monster>();
        
        foreach (Monster mon in MapMasterScript.singletonMMS.townMap2.monstersInMap)
        {
            if (mon.isInCorral)
            {
                //if (Debug.isDebugBuild) Debug.Log(mon.PrintCorralDebug() + " is in corral, but...");
                bool foundActor = false;
                foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
                {
                    //if (Debug.isDebugBuild) Debug.Log("Compare to: " + tcm.monsterObject.PrintCorralDebug());
                    if (tcm.monsterObject.actorRefName == mon.actorRefName &&
                        tcm.monsterObject.actorUniqueID == mon.actorUniqueID &&
                        tcm.monsterObject.displayName == mon.displayName)
                    {
                        if (mon.tamedMonsterStuff != null && mon.tamedMonsterStuff.sharedBankID < 0)
                        {
                            // This must be a ronin or hardcore monster that was saved locally
                            // We need to sync its shared bank ID.
                            mon.tamedMonsterStuff.sharedBankID = tcm.sharedBankID;
                            //if (Debug.isDebugBuild) Debug.Log("Assigned " + mon.actorUniqueID + "," + mon.displayName + " new shared bank ID of " + tcm.sharedBankID);
                        }
                        foundActor = true;
                        break;
                    }
                    if (tcm.monsterObject.QuickCompareTo(mon))
                    {                        
                            // What we need to do is not necessarily add grove monsters to map right away
                            // First we need to compare them to a serialized SharedCorral monster
                            // If they match, then DON'T load them, and instead load the serialized version
                        //if (Debug.isDebugBuild) Debug.Log("It's identical to another actor, so we'll sync up the actor IDs.");
                        mon.actorUniqueID = tcm.monsterObject.actorUniqueID;
                        mon.tamedMonsterStuff.monsterID = tcm.monsterObject.actorUniqueID;
                        foundActor = true;
                        break;
                    }
                }
                if (!foundActor)
                {
                    // In this case, there is a monster listed as in the corral
                    // However, it is not in our shared bank.
                    // Therefore, we should probably ADD IT to the shared bank...?
                    AddPetToLocalSlotCorralList(mon.tamedMonsterStuff);
                    //if (Debug.isDebugBuild) Debug.Log("Does not match anything in our tamed monster list, so it should probably be there.");

                    //if (Debug.isDebugBuild) Debug.Log("It doesn't match anything in the localTamedMonstersForThisSlot corral list.");
                    //actorsToRemove.Add(mon);
                }                        
                else
                {
                    //if (Debug.isDebugBuild) Debug.Log("We did find the actor.");
                }
            }
        }

        foreach (Monster mon in actorsToRemove)
        {
            if (Debug.isDebugBuild) Debug.Log("Removing " + mon.PrintCorralDebug());
            mon.RemoveSelfFromMap();
        }
    }


    /// <summary>
    /// Ensures our tamed corral monsters have the right ID to maintain relationships.
    /// </summary>
    public static void UpdateTamedCorralMonsterIDsOnNewGame()
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            Monster m = tcm.monsterObject;
            if (m == null)
            {
                Debug.Log("No monster object for " + tcm.refName + " " + tcm.parent1Name + " " + tcm.parent2Name + "?");
                m = MonsterManagerScript.CreateMonster(m.actorRefName, false, false, false, 0f, true);
            }
            else
            {
                //m.SetUniqueIDAndAddToDict();
            }
            m.bufferedFaction = Faction.PLAYER;
            m.actorfaction = Faction.PLAYER;

            /* 
            if (m.tamedMonsterStuff.monsterID != m.actorUniqueID)
            {
                // If there is a discrepancy between old and new ID, we must iterate through relationships and ensure they are correct.
                m.tamedMonsterStuff.UpdateRelationshipsWithNewID(m.actorUniqueID);
            } */
            m.tamedMonsterStuff.monsterID = m.actorUniqueID;
            MapMasterScript.singletonMMS.townMap2.AddActorToMap(m);
            MetaProgressScript.MoveMonsterActorIntoCorral(m);
        }
    }

    public static void OnTickGameTime()
    {
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = localTamedMonstersForThisSlot[i];
            if (tcm.monsterObject != null)
            {
                float healAmount = tcm.monsterObject.myStats.GetMaxStat(StatTypes.HEALTH) * MonsterCorralScript.DAILY_HEAL_PERCENT;
                tcm.monsterObject.myStats.ChangeStat(StatTypes.HEALTH, healAmount, StatDataTypes.CUR, true);
            }
        }
    }

    public static void LinkLoadedMonstersToTamedCorralMonsters()
    {
        if (MapMasterScript.singletonMMS == null) 
        {
            if (Debug.isDebugBuild) Debug.Log("MapMasterScript.singletonMMS is null!");
            return;
        }
        if (MapMasterScript.singletonMMS.townMap2 == null)
        {
            if (Debug.isDebugBuild) Debug.Log("MapMasterScript.singletonMMS.townMap2 is null!");
            return;
        }

        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            Actor getMon;
                            
            if (tcm == null) 
            {
                Debug.Log("Warning: Null TCM in localTamedMonstersForThisSlot during LinkLoadedMonstersToTamedCorralMonsters");
                continue;
            }
#if UNITY_SWITCH
            if (Debug.isDebugBuild)
            {
                Debug.Log("About to evaluate player TCM on load " + tcm.refName + " " + tcm.sharedBankID);
                if (tcm.monsterObject == null)
                {
                    Debug.Log("IT HAS NO MONSTER OBJECT???");
                }
            }
#endif

            if (GameMasterScript.dictAllActors == null)
            {
                if (Debug.isDebugBuild) Debug.Log("dictAllActors is null!");
                return;
            }

            if (GameMasterScript.dictAllActors.TryGetValue(tcm.monsterID, out getMon))
            {
                if (getMon == null)
                {
                    Debug.Log("Searched for " + tcm.monsterID + " and it's in dictAllActors, but ALSO null. wtf");
                    continue;
                }                
                tcm.monsterObject = getMon as Monster;    
                Monster m = getMon as Monster;

                if (m == null)
                {
                    Debug.Log("Searched for " + tcm.monsterID + " and it's in dictAllActors, but ALSO NOT a monster. wtf. It's " + getMon.actorRefName + " " + getMon.GetActorType());
                }

                if (Debug.isDebugBuild) Debug.Log("Linking up monster: " + m.PrintCorralDebug());

                m.tamedMonsterStuff = tcm;
                m.bufferedFaction = Faction.PLAYER;
                m.actorfaction = Faction.PLAYER;
                if (MapMasterScript.singletonMMS.townMap2.FindActorByID(m.actorUniqueID) == null)
                {
                    MapMasterScript.singletonMMS.townMap2.PlaceActor(m, MapMasterScript.singletonMMS.townMap2.GetTile(m.GetPos()));
                    if (Debug.isDebugBuild) Debug.Log("Placing TCM " + m.actorRefName + " " + m.actorUniqueID + " in corral.");
                }
            }
            else
            {
                Debug.Log("Could not link meta tamed monster ID " + tcm.monsterID);
            }
        }

        localTamedMonstersForThisSlot.RemoveAll(a => a == null);
    }


    public static void AssignIDsToMonsterCorralItems()
    {
        foreach (TamedCorralMonster tcm in localTamedMonstersForThisSlot)
        {
            Monster mObj = tcm.monsterObject;
            if (mObj == null) continue;
            foreach (Item itm in mObj.myInventory.GetInventory())
            {
                itm.SetUniqueIDAndAddToDict();
            }
        }
    }

    static List<Actor> monstersToMoveAround;

    public static void ScatterCorralMonstersAroundTheMap()
    {
        if (monstersToMoveAround == null) monstersToMoveAround = new List<Actor>();
        monstersToMoveAround.Clear();

        if (localTamedMonstersForThisSlot.Count < MonsterCorralScript.MAX_MONSTERS_IN_CORRAL) return;

        for (int i = MonsterCorralScript.MAX_MONSTERS_IN_CORRAL; i < localTamedMonstersForThisSlot.Count; i++)
        {
            Actor monsterToMove = localTamedMonstersForThisSlot[i].monsterObject;
            MapTileData randomTile = MapMasterScript.singletonMMS.townMap2.GetRandomEmptyTileForMapGen();
            while (randomTile.IsCollidable(monsterToMove))
            {
                randomTile = MapMasterScript.singletonMMS.townMap2.GetRandomEmptyTileForMapGen();
            }
            MapMasterScript.singletonMMS.MoveAndProcessActor(monsterToMove.GetPos(), randomTile.pos, monsterToMove, false);
            if (monsterToMove.myMovable != null)
            {
                monsterToMove.myMovable.ClearMovementQueue();
                monsterToMove.myMovable.AnimateSetPosition(randomTile.pos, 0.01f, false, 0f, 0f, MovementTypes.LERP);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log(monsterToMove.actorRefName + "," + monsterToMove.actorUniqueID + "," + monsterToMove.displayName + " is in excess of max corral monsters, and not spawned in.");
            }
        }
    }

    public static void AddPetToLocalSlotCorralList(TamedCorralMonster tcm, int insertionIndex = -1)
    {
        if (localTamedMonstersForThisSlot.Contains(tcm))
        {
            Debug.Log("Requested add a TCM to corral list, but it's already there. " + tcm.monsterObject.PrintCorralDebug());
            return;
        }

        // Check for duplicate IDs
        foreach(TamedCorralMonster checkMon in localTamedMonstersForThisSlot)
        {
            if (checkMon.monsterObject.actorUniqueID == tcm.monsterObject.actorUniqueID)
            {
                GameMasterScript.AssignActorID(tcm.monsterObject, 900000);
                Debug.Log("Reassigned monster ID while adding to corral, is now " + tcm.monsterObject.actorUniqueID + "/" + tcm.monsterID);
                break;
            }
            if (checkMon.sharedBankID == tcm.sharedBankID)
            {
                Debug.Log("A monster of ID " + tcm.sharedBankID + " already exists...");
                return;
            }
        }

        if (insertionIndex >= 0)
        {
            localTamedMonstersForThisSlot.Insert(insertionIndex, tcm);
        }
        else
        {
            localTamedMonstersForThisSlot.Add(tcm);
        }
        
        tcm.monsterObject.isInCorral = true;

        if (Debug.isDebugBuild) Debug.Log("Added this monster to corral list: " + tcm.monsterObject.PrintCorralDebug() + ".... There are now " + localTamedMonstersForThisSlot.Count + " monsters in the corral");
    }
}
