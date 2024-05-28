using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum GameModifiers
{
    PLAYER_REGEN, PLAYER_RESOURCEREGEN, PETS_DONTDIE, CONSUMABLE_COOLDOWN, MONSTER_REGEN, JP_HALF,
    MONSTERS_MIN_1POWER, NO_GOLD_DROPS, JOB_SPECIALIST, FAST_FULLNESS, MULTI_PANDORA, FRIENDLY_FIRE, NO_PANDORA,
    FREE_JOBCHANGE, NO_JOBCHANGE, FEWER_POWERUPS, GOLDFROGS_ANYWHERE,
    COUNT
}

public class GameModifiersScript {

    static HashSet<string> abilitiesThatSummonActors;
    static HashSet<string> playerBuffAbilities;
    static List<Actor> actorsToDestroy;
    static List<StatusEffect> statusesToRemove;
    static bool staticVarsInitialized;
    static bool playerTriedToAlterAbilities;

    const float GOLDFROG_CHANCE = 0.13f;

    public static bool CheckForValidItemPickup(Item itm)
    {
        return true;
        /* if (GameStartData.CheckGameModifier(GameModifiers.ITEM_LIMIT_9STACKS))
        {
            if (itm.IsEquipment()) return true;
            Consumable c = itm as Consumable;
            int curQuantity = GameMasterScript.heroPCActor.myInventory.GetItemQuantity(c.actorRefName);
            if (curQuantity > 9)
            {
                StringManager.SetTag(0, c.displayName);
                GameLogScript.LogWriteStringRef("log_carry_limit");
                return false;
            }
            return true;
        }
        else
        {
            return true;
        } */
    }

    static void InitializeStaticVars()
    {
        abilitiesThatSummonActors = new HashSet<string>();
        playerBuffAbilities = new HashSet<string>();
        actorsToDestroy = new List<Actor>();
        statusesToRemove = new List<StatusEffect>();
    }

    public static bool CheckForSwitchAbilitiesTutorialPopup()
    {
        if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_hotbar_switching"))
        {
            Conversation tut = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_hotbar_switching");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(tut, DialogType.STANDARD, null, 0.05f));
            return true;
        }
        return false;
    }

    // Removes buffs on the player and summons if these abilities are no longer on hotbar.
    public static void CheckForInvalidBuffsAndSummons()
    {
        if (!staticVarsInitialized)
        {
            InitializeStaticVars();
        }

        if (CanUseAbilitiesOutsideOfHotbar() || RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return;
        }

        if (playerTriedToAlterAbilities)
        {
            playerTriedToAlterAbilities = false;
            CheckForSwitchAbilitiesTutorialPopup();
        }

        abilitiesThatSummonActors.Clear();
        playerBuffAbilities.Clear();
        actorsToDestroy.Clear();
        statusesToRemove.Clear();

        bool playerHasManageSpellshapesEquipped = false;

        for (int i = 0; i < UIManagerScript.hotbarAbilities.Length; i++)
        {
            if (UIManagerScript.hotbarAbilities[i].actionType != HotbarBindableActions.ABILITY)
            {
                continue;
            }            

            AbilityScript abilToCheck = UIManagerScript.hotbarAbilities[i].ability;
            AbilityScript remapped = GameMasterScript.heroPCActor.cachedBattleData.GetRemappedAbilityIfExists(abilToCheck, GameMasterScript.heroPCActor, false);

            if (abilToCheck.refName == "skill_managespellshapes") playerHasManageSpellshapesEquipped = true;

            foreach (EffectScript eff in abilToCheck.listEffectScripts)
            {
                if (eff.effectType == EffectType.SUMMONACTOR)
                {
                    abilitiesThatSummonActors.Add(abilToCheck.refName);                    
                    if (remapped != null)
                    {
                        abilitiesThatSummonActors.Add(remapped.refName);
                    }
                    
                }
                else if (eff.effectType == EffectType.ADDSTATUS)
                {
                    AddStatusEffect ase = eff as AddStatusEffect;
                    StatusEffect template = GameMasterScript.FindStatusTemplateByName(ase.statusRef);
                    if (template != null && template.isPositive)
                    {
                        playerBuffAbilities.Add(abilToCheck.refName);
                        if (remapped != null)
                        {
                            playerBuffAbilities.Add(remapped.refName);
                        }
                    }
                }
            }
        }

        foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.isInDeadQueue || act.destroyed) continue;
            if (act.excludeFromHotbarCheck) continue;
            if (act.ReadActorData("excludefromhotbarcheck") == 1) continue;
            string actorSummonEffect = act.ReadActorDataString("player_abil_summonref");
            if (!string.IsNullOrEmpty(actorSummonEffect) && !abilitiesThatSummonActors.Contains(actorSummonEffect))
            {
                //Debug.Log("Player does not have " + actorSummonEffect + " in their hotbar!");
                actorsToDestroy.Add(act);
            }
        }        

        foreach (StatusEffect se in GameMasterScript.heroPCActor.myStats.GetAllStatuses())
        {
            if (!se.isPositive) continue;
            if (!se.statusFlags[(int)StatusFlags.FORCEHOTBARCHECK])
            {
                if (se.CheckDurTriggerOn(StatusTrigger.PERMANENT) && se.CheckRunTriggerOn(StatusTrigger.ONADD)) continue;
                if (se.noRemovalOrImmunity || !se.showIcon) continue;
                if (se.sourceOfEffectIsEquippedGear) continue;
                if (se.excludeFromHotbarCheck) continue;
            }
            if (se.addedByActorID != GameMasterScript.heroPCActor.actorUniqueID) continue;
            if (!string.IsNullOrEmpty(se.addedByAbilityRef) // If the status was added by an ability
                && !playerBuffAbilities.Contains(se.addedByAbilityRef)  // And we don't have that in our ability list
                && !GameMasterScript.heroPCActor.myStats.CheckHasActiveStatusName(se.addedByAbilityRef)) // OR our status list (statuses can add other statuses)
            {
                AbilityScript abil = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(se.addedByAbilityRef);
                if (abil != null)
                {
                    // Normally, this doesn't apply to passives. However, spellshapes are not like normal passives.
                    // If a spellshape is active and we removed Manage Spellshapes, then the spellshape should be disabled.
                    if ((abil.CheckAbilityTag(AbilityTags.SPELLSHAPE)  || abil.refName == "skill_spellshiftbarrier")
                        && playerHasManageSpellshapesEquipped) continue;
                    if (abil.passiveAbility) continue;
                    abil.toggled = false;
                    se.toggled = false;       
                }
                
                //Debug.Log("Player does not have " + se.addedByAbilityRef + " in their hotbar for buffz!");
                statusesToRemove.Add(se);
            }
        }

        foreach(Actor act in actorsToDestroy)
        {
            GameMasterScript.AddToDeadQueue(act, true);
            act.destroyed = true;
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            GameMasterScript.heroPCActor.RemoveSummon(act);
        }
        foreach(StatusEffect se in statusesToRemove)
        {
            GameMasterScript.heroPCActor.myStats.RemoveStatus(se, true);
        }

        PetPartyUIScript.RefreshContentsOfPlayerParty(UICommandArgument.REFRESH);

        bool tutorialMessagePossible = false;

        if (statusesToRemove.Count > 0)
        {
            UIManagerScript.RefreshStatusCooldowns();
            tutorialMessagePossible = true;
        }
        if (actorsToDestroy.Count > 0)
        {
            tutorialMessagePossible = true;
        }

        if (tutorialMessagePossible && !GameMasterScript.tutorialManager.WatchedTutorial("tutorial_hotbardisable"))
        {
            Conversation tut = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_hotbardisable");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(tut, DialogType.STANDARD, null, 0.05f));
        }
    }

    public static void PlayerTriedToAlterSkills()
    {
        playerTriedToAlterAbilities = true;
    }

    public static void OnNewGameStarted()
    {
        if (GameStartData.CheckGameModifier(GameModifiers.FEWER_POWERUPS))
        {
            GameMasterScript.gmsSingleton.globalPowerupDropChance *= 0.5f;
        }

        GameMasterScript.gmsSingleton.globalPowerupDropChance *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.POWERUP_RATE);

        if (GameStartData.CheckGameModifier(GameModifiers.NO_JOBCHANGE) || RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            RemoveJobChangeOptionFromPercy();
        }

        if (GameStartData.CheckGameModifier(GameModifiers.GOLDFROGS_ANYWHERE))
        {
            foreach(Map m in MapMasterScript.theDungeon.maps)
            {
                if (!m.IsMainPath() || m.floor == MapMasterScript.SHARA_START_FOREST_FLOOR || m.floor == MapMasterScript.SHARA_START_CAMPFIRE_FLOOR) continue;

                int maxGoldFrogs = 2;
                for (int i = 0; i < maxGoldFrogs; i++)
                {
                    if (UnityEngine.Random.Range(0,1f) <= GOLDFROG_CHANCE)
                    {
                        DungeonGenerationScripts.SpawnGoldfrogPerFloor(m);
                    }
                }

            }
        }

        DoSavageModeModificationsIfNeeded();
    }

    public static void OnGameLoaded()
    {
        if (GameStartData.CheckGameModifier(GameModifiers.FEWER_POWERUPS))
        {
            GameMasterScript.gmsSingleton.globalPowerupDropChance *= 0.5f;
        }

        GameMasterScript.gmsSingleton.globalPowerupDropChance *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.POWERUP_RATE);

        if (GameStartData.CheckGameModifier(GameModifiers.NO_JOBCHANGE) || RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            RemoveJobChangeOptionFromPercy();
        }        
    }

    static void RemoveJobChangeOptionFromPercy()
    {
        Conversation percyConvo = GameMasterScript.FindConversation("healer_town");
        foreach(TextBranch tb in percyConvo.allBranches)
        {
            tb.responses.RemoveAll(a => a.actionRef == "askjobs"); // Remove all references to askjobs branch.
        }
    }

    static void DoSavageModeModificationsIfNeeded()
    {
        if (GameStartData.NewGamePlus < 2) return;

        Map boss1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS1_MAP_FLOOR);
        List<string> enemyRefs = new List<string>()
        {
            "mon_alchemistboss", "mon_plundererboss"
        };
        for (int i = 0; i < 4; i++)
        {
            // add more bandits to this fight in the lower half of the map
            Monster bandit = MonsterManagerScript.CreateMonster(enemyRefs.GetRandomElement(), true, true, false, 0f, false);
            bandit.MakeChampion();
            bool validPos = false;
            Vector2 pos = new Vector2(0, 0);
            int attempts = 0;
            MapTileData mtd = null;
            while (!validPos)
            {
                attempts++;
                if (attempts > 250) break;
                int x = UnityEngine.Random.Range(1, boss1.columns - 1);
                int y = UnityEngine.Random.Range(1, boss1.rows / 2);
                pos = new Vector2(x, y);
                mtd = boss1.GetTile(pos);
                if (mtd.tileType != TileTypes.GROUND) continue;
                if (mtd.IsCollidableActorInTile(bandit)) continue;
                validPos = true;
            }
            if (!validPos) continue;
            boss1.PlaceActor(bandit, mtd);
        }

        Map boss3 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.BOSS3_MAP_FLOOR);
        enemyRefs.Clear();
        enemyRefs.Add("mon_neutralizer");
        enemyRefs.Add("mon_neutralizer");
        enemyRefs.Add("mon_heavysentrybotboss");
        enemyRefs.Add("mon_heavysentrybotboss");
        enemyRefs.Add("mon_heavysentrybotboss");
        Vector2 searchSpawnPos = new Vector2((boss3.columns / 2) - 3, (boss3.rows / 2) + 3);
        for (int i = 0; i < enemyRefs.Count; i++)
        {            
            MapTileData getTile = boss3.GetRandomEmptyTile(searchSpawnPos, 2, true, true, false, false);
            Monster makeboss = MonsterManagerScript.CreateMonster(enemyRefs[i], true, true, false, 0.3f, false);
            makeboss.isBoss = true;
            makeboss.MakeChampion();
            makeboss.myStats.BoostCoreStatsByPercent(0.3f);
            makeboss.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.5f);
            boss3.PlaceActor(makeboss, getTile);
        }

        Map boss4 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR);
        enemyRefs.Clear();
        enemyRefs.Add("mon_neutralizer");
        enemyRefs.Add("mon_guardianspider");
        enemyRefs.Add("mon_guardianorbiter");
        enemyRefs.Add("mon_heavyfightersentry");

        Vector2 finalBossPos = boss4.FindActor("mon_finalbossai").GetPos();
        List<Vector2> spawnPositions = new List<Vector2>()
        {
            new Vector2(finalBossPos.x - 2f, finalBossPos.y -2f),
            new Vector2(finalBossPos.x - 1f, finalBossPos.y -2f),
            new Vector2(finalBossPos.x + 1f, finalBossPos.y -2f),
            new Vector2(finalBossPos.x + 2f, finalBossPos.y -2f)
        };

        for (int i = 0; i < enemyRefs.Count; i++)
        {
            MapTileData getTile = boss4.GetTile(spawnPositions[i]);
            Monster makeboss = MonsterManagerScript.CreateMonster(enemyRefs[i], true, true, false, 0.3f, false);
            makeboss.isBoss = true;
            makeboss.MakeChampion();
            makeboss.myStats.BoostCoreStatsByPercent(0.3f);
            makeboss.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.65f);
            makeboss.allMitigationAddPercent -= 0.05f;
            makeboss.allDamageMultiplier += 0.05f;
            makeboss.DisableActor();
            boss4.PlaceActor(makeboss, getTile);
        }

        Map finalBoss2 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.FINAL_BOSS_FLOOR2);
        enemyRefs.Clear();
        spawnPositions.Clear();
        enemyRefs.Add("mon_sideareaboss1");
        enemyRefs.Add("mon_sideareaboss2");
        spawnPositions.Add(new Vector2(7f, 7f));
        spawnPositions.Add(new Vector2(15f, 7f));
        for (int i = 0; i < enemyRefs.Count; i++)
        {
            MapTileData getTile = finalBoss2.GetTile(spawnPositions[i]);
            Monster makeboss = MonsterManagerScript.CreateMonster(enemyRefs[i], true, true, false, 0.3f, false);            
            makeboss.myStats.BoostCoreStatsByPercent(0.35f);
            makeboss.myStats.BoostStatByPercent(StatTypes.HEALTH, 0.65f);
            makeboss.allMitigationAddPercent -= 0.05f;
            makeboss.allDamageMultiplier += 0.05f;
            finalBoss2.PlaceActor(makeboss, getTile);
        }
    }

    public static bool CanUseAbilitiesOutsideOfHotbar()
    {
        if (SharaModeStuff.IsSharaModeActive()) return true;
        if (!PlatformVariables.CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR)
        {
            return GameStartData.CheckGameModifier(GameModifiers.JOB_SPECIALIST);
        }
        return !GameStartData.CheckGameModifier(GameModifiers.JOB_SPECIALIST);


    }
}
