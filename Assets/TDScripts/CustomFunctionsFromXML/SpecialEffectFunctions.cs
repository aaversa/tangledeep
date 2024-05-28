using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

public partial class SpecialEffectFunctions
{
    public static Dictionary<string, Func<SpecialEffect, List<Actor>, EffectResultPayload>> dictDelegates;

    static bool initialized;

    public static void Initialize()
    {
        dictDelegates = new Dictionary<string, Func<SpecialEffect, List<Actor>, EffectResultPayload>>();
        initialized = true;

        poolStrings = new List<string>();
        poolAbilities = new List<AbilityScript>();
        afdList = new List<zirconAnim.AnimationFrameData>();
        possibleActors = new List<Actor>();
    }

    public static void CacheScript(string scriptName)
    {
        if (!initialized) Initialize();

        if (dictDelegates.ContainsKey(scriptName))
        {
            return;
        }

        MethodInfo sdsTakeAction = typeof(SpecialEffectFunctions).GetMethod(scriptName, new Type[] { typeof(SpecialEffect), typeof(List<Actor>) } );

        Func<SpecialEffect, List<Actor>, EffectResultPayload> converted = 
        (Func<SpecialEffect, List<Actor>, EffectResultPayload>)Delegate.CreateDelegate(typeof(Func<SpecialEffect, List<Actor>, EffectResultPayload>), sdsTakeAction);

        // Cache it.
        dictDelegates.Add(scriptName, converted);

    }

    public static EffectResultPayload ReduceCooldownsByTwo(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.GetCurCooldownTurns() > 0)
            {
                abil.ChangeCurrentCooldown(-1);
            }
            if (abil.GetCurCooldownTurns() > 0)
            {
                abil.ChangeCurrentCooldown(-1);
            }            
        }

        return erp;
    }

    public static EffectResultPayload GenerateGoldPile(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        int id = GameMasterScript.gmsSingleton.ReadTempGameData("deadmonid");

        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;

        if (!GameMasterScript.dictAllActors.TryGetValue(id, out Actor act))
        {
            return erp;
        }

        Monster mon = act as Monster;

        float droppedMoney = UnityEngine.Random.Range(15, 30) * mon.myStats.GetLevel();

        if (MapMasterScript.activeMap.IsMysteryDungeonMap())
        {
            droppedMoney = UnityEngine.Random.Range(15, 30) * (MapMasterScript.activeMap.floor - MapMasterScript.CUSTOMDUNGEON_START_FLOOR);
        }

        MapTileData origTile = MapMasterScript.GetTile(mon.GetPos());

        GenerateGoldPilesAroundTile(origTile, droppedMoney, 1);

        return erp;
    }

    static void GenerateGoldPilesAroundTile(MapTileData centerTile, float droppedMoney, int numPiles)
    {
        List<MapTileData> possible = null;
        int radius = 2;
        int attempts = 0;
        while (true)
        {
            attempts++;
            possible = MapMasterScript.activeMap.GetListOfTilesAroundPoint(centerTile.pos, radius);
            List<MapTileData> pool_MTD = new List<MapTileData>();
            foreach (MapTileData mtd in possible)
            {
                if (mtd.tileType == TileTypes.WALL || mtd.playerCollidable || mtd.AreItemsOrDestructiblesInTile())
                {
                    pool_MTD.Add(mtd);
                }
            }
            foreach (MapTileData mtd in pool_MTD)
            {
                possible.Remove(mtd);
            }

            if (possible.Count == 0)
            {
                radius++;
            }
            else
            {
                break;
            }

            if (attempts > 6)
            {
                break;
            }
        }

        possible.Shuffle();
        
        for (int i = 0; i < numPiles; i++)
        {
            float localMoney = UnityEngine.Random.Range(droppedMoney*0.8f, droppedMoney*1.2f);

            MapTileData tileForMoney = possible.GetRandomElement();
            MapMasterScript.SpawnCoins(centerTile, tileForMoney, (int)localMoney);        
            possible.Remove(tileForMoney);
            if (possible.Count == 0) break;
        }
    }

    public static EffectResultPayload ObserveStatusOfPetDuel(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;

        bool endDuel = false;

        Actor pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo"));
        Monster m = pet as Monster;

        if (MapMasterScript.activeMap.floor != MapMasterScript.TOWN2_MAP_FLOOR)
        {
            endDuel = true;
        }

        if (!GameMasterScript.heroPCActor.myStats.CheckHasStatusName("monsterundying_temp") || !m.myStats.CheckHasStatusName("monsterundying_temp"))
        {
            //Debug.Log(GameMasterScript.heroPCActor.myStats.CheckHasStatusName("monsterundying_temp") + " " + m.myStats.CheckHasStatusName("monsterundying_temp"));
            endDuel = true;
        }

        if (endDuel)
        {
            PetPartyUIScript.EndPetDuel(m);
        }

        return erp;
    }

    public static EffectResultPayload ReduceDamageFromSummons(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload.atk.turnsToDisappear > 0)
        {
            CombatManagerScript.damagePayload.currentDamageValue *= 0.5f;
        }
        
        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload IncreaseDamageToSummons(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload.def.turnsToDisappear > 0)
        {
            CombatManagerScript.damagePayload.currentDamageValue *= 1.25f;
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    static void ConvertDamageDealtElement(Fighter ft, DamageTypes dmgFrom, DamageTypes dmgTo)
    {
        ft.cachedBattleData.damageTypeDealtConversions[(int)dmgFrom] = dmgTo;
    }    

    public static EffectResultPayload AddFireToShadowDamageDealtConversion(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter ft = effect.originatingActor as Fighter;

        ConvertDamageDealtElement(ft, DamageTypes.FIRE, DamageTypes.SHADOW);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload IncreaseDamageOfBleedsAndPoisons(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float dmg = CombatManagerScript.damagePayload.currentDamageValue;

        Fighter ft = effect.originatingActor as Fighter;

        if (CombatManagerScript.damagePayload.damage.damType == DamageTypes.POISON &&
            CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            dmg *= 1.15f;
        }

        CombatManagerScript.damagePayload.currentDamageValue = dmg;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload ReduceCooldownsByOneTurn(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.GetCurCooldownTurns() > 0)
            {
                abil.ChangeCurrentCooldown(-1);
            }
        }

        return erp;
    }

    public static EffectResultPayload OpenSpellshapeDialog(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        // Construct a conversion with responses where each response is a Spellshape / Spellshift we know
        // Use asterisks and colors to show which are toggled and which are not        

        EffectResultPayload erp = new EffectResultPayload();

        Conversation spellshapeConvo = GameMasterScript.FindConversation("dialog_spellshape_toggle");
        TextBranch main = spellshapeConvo.FindBranch("main");
        main.responses.Clear();

        // Get rid of (TOGGLE) text, in case we didn't scrub it from our files
        string toggleStringByLanguage = "(Toggle) ";
        switch(StringManager.gameLanguage)
        {
            case EGameLanguage.jp_japan:
                toggleStringByLanguage = "（切り替え）";
                break;
            case EGameLanguage.zh_cn:
                toggleStringByLanguage = "（触发能力）";
                break;
        }

        List<ButtonCombo> spellshifts = new List<ButtonCombo>();

        foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (!abil.CheckAbilityTag(AbilityTags.SPELLSHAPE)) continue;            

            // If this has a modified version, skip it; we only want to show the modified version
            if (GameMasterScript.heroPCActor.cachedBattleData.GetRemappedAbilityIfExists(abil, GameMasterScript.heroPCActor) != abil)
            {
                continue;
            }

            ButtonCombo bc = new ButtonCombo();
            bc.threeColumnStyle = true;
            bc.headerText = abil.abilityName;
            bc.spriteRef = abil.iconSprite;

            if (abil.passiveAbility)
            {
                bc.buttonText = abil.GetPassiveDescription().Replace(toggleStringByLanguage, String.Empty);
            }
            else
            {
                bc.buttonText = abil.description.Replace(toggleStringByLanguage, String.Empty);
            }
            

            bc.toggled = abil.toggled;

            // The DialogEventScript for each button is what will execute the toggle and add/remove the status on hero
            bc.dialogEventScript = "ToggleSpellshapeFromDialog";
            bc.dialogEventScriptValue = abil.refName;
            bc.actionRef = "toggle";
            bc.dbr = DialogButtonResponse.TOGGLE;
            bc.extraVerticalPadding = 22;
            if (bc.dialogEventScriptValue == "skill_spellshiftpenetrate" || bc.dialogEventScriptValue.Contains("skill_spellshiftmaterialize"))
            {
                spellshifts.Add(bc);
                continue;
            }
            main.responses.Add(bc);
        }

        foreach(ButtonCombo bc in spellshifts)
        {
            main.responses.Add(bc); // keep spellshifts separated from Spellshapes in the list
        }

        ButtonCombo exit = new ButtonCombo();
        exit.dbr = DialogButtonResponse.EXIT;
        exit.actionRef = "exit";
        exit.buttonText = StringManager.GetString("misc_button_exit_normalcase");
        exit.extraVerticalPadding = 15;
        main.responses.Add(exit);
      
        UIManagerScript.StartConversation(spellshapeConvo, DialogType.STANDARD, null);
        DialogEventsScript.RefreshSpellshapeDialog("dummy");

        //Debug.Log(spellshapeConvo.allBranches.Count + " " + UIManagerScript.currentTextBranch.branchRefName);

        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload ConsumeMarkOnTarget(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        // Consume weakness marked buff
        if (actorsToProcess.Count >= 2)
        {
            Fighter target = actorsToProcess[1] as Fighter;
            target.myStats.RemoveStatusByRef("status_detectedweakness");
        }        

        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload IncreaseDamage50p(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float dmg = CombatManagerScript.damagePayload.currentDamageValue;

        Fighter ft = effect.originatingActor as Fighter;

        if (CombatManagerScript.damagePayload.def.GetActorType() == ActorTypes.MONSTER)
        {
            dmg *= 1.5f;
        }

        CombatManagerScript.damagePayload.currentDamageValue = dmg;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload TargetMonsterAndDestructibleSummons(EffectScript effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        MapTileData clickedMTD = MapMasterScript.GetTile(effect.centerPosition);

        foreach (Actor act in clickedMTD.GetAllActors())
        {
            bool actorDestroyed = false;
            if (act.summoner != null && act.turnsToDisappear > 0 && act.maxTurnsToDisappear > 0)
            {
                bool valid = true;
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.isBoss || mn.isChampion)
                    {
                        valid = false;
                    }
                    else
                    {
                        DestroyActorEffect.DestroyTargetMonster(mn, 9999f, false);
                        actorDestroyed = true;
                    }
                }
                else // Destructible
                {
                    GameMasterScript.AddToDeadQueue(act);
                    actorDestroyed = true;
                }
            }
            if (actorDestroyed)
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(act.GetPos(), "ShadowReverberateEffect", effect, true);
            }
        }

        BattleTextManager.NewText(StringManager.GetString("misc_begone"), effect.originatingActor.GetObject(), Color.yellow, 1.2f);

        erp.waitTime = addWaitTime;
        //erp.actorsToProcess = newActorsToProcess;
        return erp;
    }

    public static EffectResultPayload BasicTemplate(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload ReduceSummonTime(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        bool anyAffected = false;

        foreach (Actor act in actorsToProcess)
        {
            if (act.actorfaction != Faction.PLAYER) continue;
            if (act == GameMasterScript.heroPCActor.GetMonsterPet()) continue;
            if (act.GetActorType() == ActorTypes.HERO) continue;
            if (act.turnsToDisappear <= 0) continue;
            act.turnsToDisappear -= (act.maxTurnsToDisappear / 3);
            if (act.turnsToDisappear < 0)
            {
                act.turnsToDisappear = 0;
            }
            CombatManagerScript.GenerateSpecificEffectAnimation(act.GetPos(), "FervirDebuff", effect, false);
            anyAffected = true;
        }

        if (anyAffected && GameMasterScript.heroPCActor.ReadActorData("shara_pet_callout") != 1)
        {
            GameMasterScript.heroPCActor.SetActorData("shara_pet_callout", 1);
            GameMasterScript.SetAnimationPlaying(true);
            Conversation c = GameMasterScript.FindConversation("shara_taunt_pet");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(c, DialogType.STANDARD, null, 0.4f));
        }

        erp.waitTime = addWaitTime;
        return erp;

    }

    public static EffectResultPayload AttemptBanishPlayerSummons(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();
        erp.actorsToProcess = new List<Actor>();

        foreach (Actor act in actorsToProcess)
        {
            if (act.actorfaction != Faction.PLAYER) continue;
            if (act == GameMasterScript.heroPCActor.GetMonsterPet()) continue;
            if (act.GetActorType() == ActorTypes.HERO) continue;
            if (act.summoner == null) continue;
            if (act.summoner.GetActorType() != ActorTypes.HERO) continue;
            erp.actorsToProcess.Add(act);
        }

        foreach (Actor act in erp.actorsToProcess)
        {
            if (act.turnsToDisappear <= 0) // strike for XX% health
            {
                Monster m = act as Monster;
                float dmg = m.myStats.GetMaxStat(StatTypes.HEALTH) * 0.5f;
                m.TakeDamage(dmg, DamageTypes.PHYSICAL);
                BattleTextManager.NewDamageText((int)dmg, false, Color.red, m.GetObject(), 0.2f, 1f);
                LoseHPPackage lhp = GameLogDataPackages.GetLoseHPPackage();
                lhp.damageAmount = dmg;
                lhp.abilityUser = effect.originatingActor.displayName;
                lhp.damageEffectSource = StringManager.GetString("abil_skill_banish_name");
                lhp.dType = DamageTypes.PHYSICAL;
                lhp.damageSpriteString = "<sprite=0>";
                lhp.gameActor = act;
                GameLogScript.CombatEventWrite(lhp);
            }
            else
            {
                act.turnsToDisappear -= (act.maxTurnsToDisappear / 3);
                if (act.turnsToDisappear < 0)
                {
                    act.turnsToDisappear = 0;
                }
            }

            CombatManagerScript.GenerateSpecificEffectAnimation(act.GetPos(), "FervirDebuff", effect, true);
        }

        BattleTextManager.NewText(StringManager.GetString("misc_begone"), effect.originatingActor.GetObject(), Color.red, 1.2f);
        CombatManagerScript.GenerateSpecificEffectAnimation(effect.originatingActor.GetPos(), "ShadowReverberateEffect", effect, true);

        erp.waitTime = addWaitTime;
        return erp;
    }

    public static EffectResultPayload AddCritBuffAfterBigHit(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (!GameMasterScript.gameLoadSequenceCompleted) return erp;

        float curDamage = 0;
        if (CombatManagerScript.damagePayload == null)
        {
            // Why would this ever happen
            
        }
        else
        {
            curDamage = CombatManagerScript.damagePayload.currentDamageValue;
        }

        if (curDamage >= (GameMasterScript.heroPCActor.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX) * 0.15f))
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("critchanceup25", GameMasterScript.heroPCActor, 7);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload EnhanceBleedBy20(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;
        if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            DamageEffect de = CombatManagerScript.damagePayload.effParent as DamageEffect;
            if (de != null && de.damFlags[(int)DamageEffectFlags.BLEED])
            {
                curdamage *= 1.2f;
            }
        }
        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload DoubleItemDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;
        if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            DamageEffect de = CombatManagerScript.damagePayload.effParent as DamageEffect;
            if (de != null && de.damageItem)
            {
                curdamage *= 2f;
                GameLogScript.LogWriteStringRef("log_lucky_itemdamage");
            }
        }
        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload DoubleHealingEffects(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.bufferedCombatData != null)
        {
            GameLogScript.LogWriteStringRef("log_lucky_doubleheal");
            CombatManagerScript.bufferedCombatData.healValue *= 2f;
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload CoinBurstFromRangedAttack(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        UIManagerScript.PlayCursorSound("Buy Item");
        BattleTextManager.NewText(StringManager.GetString("misc_coinparry"), actorsToProcess[1].GetObject(), Color.yellow, 1.2f);

        int numCoinsLost = GameMasterScript.heroPCActor.myStats.GetLevel() * 50;
        if (numCoinsLost > GameMasterScript.heroPCActor.GetMoney())
        {
            numCoinsLost = GameMasterScript.heroPCActor.GetMoney();
        }

        int numPiles = UnityEngine.Random.Range(1, 4);

        if (numCoinsLost >= 3)
        {
            int numCoinsPerPile = numCoinsLost / numPiles;
            int finalLossValue = numCoinsPerPile * numPiles;
            GameMasterScript.heroPCActor.ChangeMoney(-1 * finalLossValue);
            for (int i = 0; i < numPiles; i++)
            {
                MapTileData tileForMoney = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 1, true, true, false, true, false);
                MapMasterScript.SpawnCoins(MapMasterScript.activeMap.GetTile(GameMasterScript.heroPCActor.GetPos()), tileForMoney, numCoinsPerPile);
            }
        }

        return erp;

    }

    public static EffectResultPayload TryAddEmblemWrath(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        string statusRef = "";
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_paladinemblem_tier1_wrathelemdmg"))
        {
            statusRef = "wrath_elemdmg";
        }
        else if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("emblem_paladinemblem_tier1_wrathelemdef"))
        {
            statusRef = "wrath_elemdef";
        }

        if (statusRef != "")
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRef(statusRef, GameMasterScript.heroPCActor, 99);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload PullMonsterEchoes(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        List<Actor> actorsToPull = new List<Actor>();

        foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            if (act.actorRefName == "monsterspirit")
            {
                Vector2 origPos = act.GetPos();
                if (MapMasterScript.GetGridDistance(act.GetPos(), GameMasterScript.heroPCActor.GetPos()) > 1)
                {
                    CustomAlgorithms.GetPointsOnLineNoGarbage(act.GetPos(), GameMasterScript.heroPCActor.GetPos());
                    Vector2 moveTile = Vector2.zero;
                    if (CustomAlgorithms.numPointsInLineArray >= 2) // Orig tile, something in between, ____
                    {
                        moveTile = CustomAlgorithms.pointsOnLine[1];
                    }
                    MapTileData checkTile = MapMasterScript.GetTile(moveTile);
                    if (checkTile.tileType == TileTypes.GROUND && moveTile != GameMasterScript.heroPCActor.GetPos())
                    {
                        MapMasterScript.singletonMMS.MoveAndProcessActor(act.GetPos(), moveTile, act);
                        act.myMovable.AnimateSetPosition(moveTile, 0.12f, false, 0f, 0f, MovementTypes.LERP);
                        //Debug.Log(act.actorRefName + " now in " + moveTile + " " + checkTile.HasActor(act) + " " + checkTile.pos + " from " + origPos + " " + MapMasterScript.GetTile(origPos).HasActor(act));
                    }
                }

            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload InstantKillTarget(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Monster mn = CombatManagerScript.bufferedCombatData.defender as Monster;
        if (mn != null && !mn.isBoss && !mn.isChampion && CombatManagerScript.bufferedCombatData.atkType == AttackType.ATTACK
            && mn.actorRefName != "mon_targetdummy")
        {
            mn.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
            StringManager.SetTag(0, mn.displayName);
            GameLogScript.LogWriteStringRef("log_reaper");
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload AddDefensePenaltyForBleedingTarget(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        int targetID = GameMasterScript.gmsSingleton.ReadTempGameData("target_of_addstatus");
        Fighter ft = GameMasterScript.gmsSingleton.TryLinkActorFromDict(targetID) as Fighter;
        if (ft != null && ft.myStats.IsAlive())
        {
            if (GameMasterScript.gmsSingleton.ReadTempGameData("last_status_bleed") == 1) // We added a bleed recently.
            {
                ft.myStats.AddStatusByRefAndLog("status_defdown_12", GameMasterScript.heroPCActor, 6); // #todo - attach this to bleed duration
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }



    public static EffectResultPayload RemoveAllAggroFromHero(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();


        foreach (AggroData ad in GameMasterScript.heroPCActor.combatTargets)
        {
            if (ad.combatant.myStats.IsAlive() && ad.combatant.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = ad.combatant as Monster;
                mn.SetState(BehaviorState.NEUTRAL);
                mn.RemoveTarget(GameMasterScript.heroPCActor);
            }
        }
        GameMasterScript.heroPCActor.ClearCombatTargets();


        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload SpendEnergyToIncreaseRangedDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (GameMasterScript.heroPCActor.ReadActorData("lastenergyspent") > 0)
        {
            GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("phasma_rangedattackboost", GameMasterScript.heroPCActor, 9);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload WarpCloserToPlayer(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.bufferedCombatData == null)
        {
            return erp;
        }

        Fighter attacker = CombatManagerScript.bufferedCombatData.attacker;

        if (attacker == CombatManagerScript.bufferedCombatData.defender)
        {
            attacker = GameMasterScript.heroPCActor;
        }

        int distance = MapMasterScript.GetGridDistance(effect.originatingActor.GetPos(), attacker.GetPos());
        if (distance > 1)
        {
            Vector2 destination = Vector2.zero;

            // Move at least 2 squares closer.

            bool foundPoint = false;

            if (distance >= 4)
            {
                CustomAlgorithms.GetPointsOnLineNoGarbage(effect.originatingActor.GetPos(), attacker.GetPos());
                destination = CustomAlgorithms.pointsOnLine[2]; // This should work, given the distance MUST be 4...

                MapTileData destinationTile = MapMasterScript.GetTile(destination);

                if (destinationTile != null && destinationTile.tileType == TileTypes.GROUND && !destinationTile.IsCollidable(effect.originatingActor))
                {
                    foundPoint = true;
                }

            }

            if (!foundPoint)
            {
                CustomAlgorithms.GetNonCollidableTilesAroundPoint(attacker.GetPos(), 1, effect.originatingActor, MapMasterScript.activeMap);
                float bestDist = 99;
                Vector2 bestTile = Vector2.zero;
                for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
                {
                    float checkDist = Vector2.Distance(attacker.GetPos(), CustomAlgorithms.nonCollidableTileBuffer[i].pos);
                    if (checkDist < bestDist)
                    {
                        bestDist = checkDist;
                        bestTile = CustomAlgorithms.nonCollidableTileBuffer[i].pos;
                    }
                }
                destination = bestTile;
            }

            MapMasterScript.singletonMMS.MoveAndProcessActor(effect.originatingActor.GetPos(), destination, effect.originatingActor);
            effect.originatingActor.myMovable.AnimateSetPosition(destination, 0.1f, false, 0f, 0f, MovementTypes.LERP);
            CombatManagerScript.WaitThenGenerateSpecificEffect(destination, "DustCloudLanding", null, 0.15f, true);
            BattleTextManager.NewText(StringManager.GetString("abil_skill_reactwarptoplayer_name"), effect.originatingActor.GetObject(), Color.yellow, 0.5f);
            StringManager.SetTag(0, effect.originatingActor.displayName);
            StringManager.SetTag(1, StringManager.GetString("abil_skill_reactwarptoplayer_name"));
            GameLogScript.LogWriteStringRef("log_ability_used", effect.originatingActor);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload TryAddSongbladeSong(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter ft = effect.originatingActor as Fighter;

        List<string> possibleThaneSongs = new List<string> { "song_might_3_songblade", "song_spirit_3_songblade", "song_endurance_3_songblade" };

        foreach (StatusEffect se in ft.myStats.GetAllStatuses())
        {
            if (se.refName.Contains("might"))
            {
                possibleThaneSongs.Remove("song_might_3_songblade");
            }
            else if (se.refName.Contains("spirit"))
            {
                possibleThaneSongs.Remove("song_spirit_3_songblade");
            }
            else if (se.refName.Contains("endurance"))
            {
                possibleThaneSongs.Remove("song_endurance_3_songblade");
            }
        }

        if (possibleThaneSongs.Count > 0)
        {
            StatusEffect songbladeSong = ft.myStats.AddStatusByRefAndLog(possibleThaneSongs[UnityEngine.Random.Range(0, possibleThaneSongs.Count)], ft, 7);
            if (songbladeSong != null)
            {
                CombatManagerScript.GenerateSpecificEffectAnimation(ft.GetPos(), "FervirBuff", null, true);
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload TryTeachMonsterPowerFromLetter(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
        Item monsterLetter = GameMasterScript.itemBeingUsed;
        AbilityScript template = AbilityScript.GetAbilityByName(monsterLetter.ReadActorDataString("monsterletter_skill"));
        AbilityScript newAbil = new AbilityScript();
        AbilityScript.CopyFromTemplate(newAbil, template);

        // What is the MPD info? Have to store this...

        int minRange = monsterLetter.ReadActorData("monsterletter_skill_minrange");
        int maxRange = monsterLetter.ReadActorData("monsterletter_skill_maxrange");
        float threshold = monsterLetter.ReadActorData("monsterletter_skill_health") / 100f;
        float chanceToUse = monsterLetter.ReadActorData("monsterletter_skill_chance") / 100f;
        BehaviorState useState = (BehaviorState)(monsterLetter.ReadActorData("monsterletter_skill_usestate"));
        bool useWithNoTarget = monsterLetter.ReadActorData("monsterletter_skill_usewithnotarget") == 1;

        pet.myAbilities.AddNewAbility(newAbil, true);
        MonsterPowerData newMPD = new MonsterPowerData();
        newMPD.abilityRef = newAbil;
        newMPD.minRange = minRange;
        newMPD.maxRange = maxRange;
        newMPD.healthThreshold = threshold;
        newMPD.useState = useState;
        newMPD.useWithNoTarget = useWithNoTarget;

        pet.monsterPowers.Add(newMPD);
        pet.OnMonsterPowerAdded(newMPD, newAbil);

        StringManager.SetTag(0, pet.displayName);
        StringManager.SetTag(1, newAbil.abilityName);

        GameLogScript.LogWriteStringRef("log_pet_learnedability");
        UIManagerScript.FlashWhite(0.5f);
        UIManagerScript.PlayCursorSound("Heavy Learn");

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    static void DoGamblerStraightOrHigher()
    {
        // heal to full
        GameMasterScript.heroPCActor.myStats.HealToFull();

        // super haste
        StringManager.SetTag(0, GameMasterScript.heroPCActor.displayName);

        GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("status_hasted2", GameMasterScript.heroPCActor, 6);

        GameMasterScript.heroPCActor.myStats.AddStatusByRefAndLog("status_sharpeyes", GameMasterScript.heroPCActor, 6);
    }

    static void DoGamblerFlushOrHigher(bool bestHand)
    {
        foreach (Monster mn in MapMasterScript.activeMap.monstersInMap)
        {
            if (mn.actorfaction == Faction.PLAYER) continue;
            if (GameMasterScript.heroPCActor.visibleTilesArray[(int)mn.GetPos().x, (int)mn.GetPos().y])
            {
                mn.myStats.AddStatusByRef("status_asleep", GameMasterScript.heroPCActor, 12);
            }
            else
            {
                mn.myStats.AddStatusByRef("status_asleep", GameMasterScript.heroPCActor, 20);
            }

            if (bestHand)
            {
                mn.myStats.SetStat(StatTypes.HEALTH, 1f, StatDataTypes.CUR, true);
            }
        }
    }

    public static EffectResultPayload GamblerStraight(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        DoGamblerStraightOrHigher();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GamblerFlush(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        DoGamblerStraightOrHigher();

        DoGamblerFlushOrHigher(false);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GamblerBestHand(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        DoGamblerStraightOrHigher();
        DoGamblerFlushOrHigher(true);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload ReduceGroundDamageViaThane(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY)
        {
            DamageEffect de = CombatManagerScript.damagePayload.effParent as DamageEffect;
            if (de != null)
            {
                if (de.parentAbility.CheckAbilityTag(AbilityTags.GROUNDBASEDEFFECT))
                {
                    float curdamage = CombatManagerScript.damagePayload.currentDamageValue;
                    curdamage *= 0.66f;
                    CombatManagerScript.damagePayload.currentDamageValue = curdamage;
                }
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload CheckForIgnoreDamage3Percent(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;

        Fighter ft = effect.selfActor as Fighter;
        float threshold = ft.myStats.GetMaxStat(StatTypes.HEALTH) * 0.03f;

        if (curdamage <= threshold)
        {
            StringManager.SetTag(0, ft.displayName);
            StringManager.SetTag(1, ((int)curdamage).ToString());
            GameLogScript.LogWriteStringRef("log_damage_cap_reduce");
            curdamage = 0f;
        }

        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    public static EffectResultPayload TitanMeleeEnhanceDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;

        if (MapMasterScript.GetGridDistance(CombatManagerScript.damagePayload.atk.GetPos(), CombatManagerScript.damagePayload.def.GetPos()) == 1)
        {
            // Melee range, that's good right?
            if (CombatManagerScript.damagePayload.aType == AttackType.ATTACK)
            {
                curdamage *= 1.2f;
            }
            else if (CombatManagerScript.damagePayload.damage.damType == DamageTypes.PHYSICAL)
            {
                curdamage *= 1.2f;
            }
        }

        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload CandleskullEnhanceDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;

        int numDebuffs = 0;
        foreach (StatusEffect se in CombatManagerScript.damagePayload.def.myStats.GetAllStatuses())
        {
            if (!se.isPositive && !se.durStatusTriggers[(int)StatusTrigger.PERMANENT])
            {
                numDebuffs++;
            }
        }

        if (numDebuffs > 5) numDebuffs = 5;
        float extraDamage = (numDebuffs * 0.06f) * curdamage;
        curdamage += extraDamage;

        CombatManagerScript.damagePayload.currentDamageValue = curdamage;

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static float CapDamageFromHealthPercent(Fighter defender, float damagePercentLimit)
    {
        float curdamage = CombatManagerScript.damagePayload.currentDamageValue;

        float heroThresh = defender.myStats.GetMaxStat(StatTypes.HEALTH) * damagePercentLimit;

        float reduction = 0;

        if (curdamage > heroThresh)
        {
            reduction = curdamage - heroThresh;
            curdamage = heroThresh;
        }

        CombatManagerScript.damagePayload.currentDamageValue = curdamage;
        return reduction;
    }

    public static EffectResultPayload AlwaysReduceDamageToZero(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        CombatManagerScript.damagePayload.currentDamageValue = 0f;

        GameMasterScript.gmsSingleton.SetTempFloatData("dmg", 0f);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    /// <summary>
    /// Similar to below function - sets damage to zero if it would kill us
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="actorsToProcess"></param>
    /// <returns></returns>
    public static EffectResultPayload SetDamageToZeroIfItWouldKillUs(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        Fighter ft = effect.originatingActor as Fighter;
        float curHealth = ft.myStats.GetCurStat(StatTypes.HEALTH);

        float dmg = GameMasterScript.gmsSingleton.ReadTempFloatData("dmg");
        if (dmg > curHealth - 1f)
        {
            dmg = curHealth - 2f;
        }
        if (dmg <= 0) dmg = 0f;

        GameMasterScript.gmsSingleton.SetTempFloatData("dmg", dmg);
        EffectResultPayload erp = new EffectResultPayload();

        return erp;
    }

    public static EffectResultPayload SetDamageToZero(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float curHealth = CombatManagerScript.damagePayload.def.myStats.GetCurStat(StatTypes.HEALTH);

        if (CombatManagerScript.damagePayload.currentDamageValue >= curHealth - 1f)
        {
            CombatManagerScript.damagePayload.currentDamageValue = curHealth - 2f;
            if (CombatManagerScript.damagePayload.currentDamageValue <= 0f)
            {
                CombatManagerScript.damagePayload.currentDamageValue = 0f;
            }
        }

        if (effect.effectRefName == "monsterundying_chance")
        {
            StringManager.SetTag(0, CombatManagerScript.damagePayload.def.displayName);
            GameLogScript.LogWriteStringRef("exp_log_resilientmonster");
            BattleTextManager.NewText(StringManager.GetExcitedString("skill_expmon_resilient_name"), CombatManagerScript.damagePayload.def.GetObject(), Color.yellow, 0.25f);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }


    public static EffectResultPayload CapDamageAt20Percent(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float reduction = CapDamageFromHealthPercent(CombatManagerScript.damagePayload.def, 0.2f);

        if (reduction > 0)
        {
            StringManager.SetTag(0, CombatManagerScript.damagePayload.def.displayName);
            StringManager.SetTag(1, ((int)reduction).ToString());
            GameLogScript.LogWriteStringRef("log_damage_cap_reduce");
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload CapDamageAt25Percent(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        float reduction = CapDamageFromHealthPercent(CombatManagerScript.damagePayload.def, 0.25f);

        if (reduction > 0)
        {
            StringManager.SetTag(0, CombatManagerScript.damagePayload.def.displayName);
            StringManager.SetTag(1, ((int)reduction).ToString());
            GameLogScript.LogWriteStringRef("log_damage_cap_reduce");
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload CheckForPlayerBloodlust(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter hero = GameMasterScript.heroPCActor;

        if (!hero.myStats.CheckHasStatusName("status_playerbloodfrenzy"))
        {
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.IsFighter())
                {
                    Fighter ft = act as Fighter;
                    if (MapMasterScript.GetGridDistance(ft.GetPos(), hero.GetPos()) <= 3f)
                    {
                        if (ft.myStats.IsBleeding())
                        {
                            // Add the status of Blood Frenzy
                            hero.myStats.AddStatusByRef("status_playerbloodfrenzy", hero, 3);
                            break;
                        }
                    }
                }
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }



    public static EffectResultPayload OpulentSpendGold(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (GameMasterScript.heroPCActor.ReadActorData("opulent_turn") == GameMasterScript.turnNumber)
        {
            return erp;
        }

        GameMasterScript.heroPCActor.SetActorData("opulent_turn", GameMasterScript.turnNumber);

        int goldCost = GameMasterScript.heroPCActor.myStats.GetLevel() * 25;
        GameMasterScript.heroPCActor.ChangeMoney(goldCost * -1);

        //BattleTextManager.NewText("-" + goldCost + StringManager.GetString("misc_moneysymbol"), GameMasterScript.heroPCActor.GetObject(), Color.yellow, 0.3f);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload AdaptResistanceToDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter ft = effect.originatingActor as Fighter;

        string damType = ft.lastDamageTypeReceived.ToString().ToLowerInvariant();

        string addref = "resist" + damType + "_major";
        if (!ft.myStats.CheckHasStatusName(addref))
        {
            ft.myStats.RemoveStatusesByFlag(StatusFlags.BOOSTRESISTANCE);
            ft.myStats.AddStatusByRef(addref, ft, 9999);
            StringManager.SetTag(0, ft.displayName);
            StringManager.SetTag(1, StringManager.GetString("misc_dmg_" + damType));
            GameLogScript.LogWriteStringRef("log_misc_gainresistance");
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload AlacrityReduceCooldowns(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (UnityEngine.Random.Range(0, 1f) <= 0.33f)
        {
            foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
            {
                abil.ChangeCurrentCooldown(-1);
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    static void TryPierceResistance(DamageTypes element, string requiredStatusName, float resistMult)
    {
        //if (CombatManagerScript.damagePayload.aType == AttackType.ABILITY && CombatManagerScript.damagePayload.damage.damType == element)
        if (CombatManagerScript.damagePayload.damage.damType == element)
        {
            bool wasAbsorb = CombatManagerScript.damagePayload.absorbDamage == true;
            CombatManagerScript.damagePayload.absorbDamage = false;

            // Let's say they have 75% resistance. Resist mult 0.25f.
            if (CombatManagerScript.damagePayload.resistMult < 1f)
            {
                float modified = 1f - CombatManagerScript.damagePayload.resistMult; // Now at 0.75

                float reduceResistanceToThisValue = resistMult;
                if (wasAbsorb)
                {
                    reduceResistanceToThisValue = 0.5f;
                }

                modified *= reduceResistanceToThisValue; // Now at 0.1875
                float finalRes = 1f - modified; // Final value 0.8125
                CombatManagerScript.damagePayload.resistMult = finalRes;
                CombatManagerScript.damagePayload.absorbDamage = false;
            }
        }
    }

    public static EffectResultPayload PenetrateResistancesWithShadow(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        TryPierceResistance(DamageTypes.SHADOW, "emblem_dualwielderemblem_tier1_shadow", 0.25f);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload PenetrateHalfResistancesWithFire(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();
        // Changed back to 50%.
        TryPierceResistance(DamageTypes.FIRE, "emblem_sworddanceremblem_tier1_flames", 0.5f);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload PenetrateHalfResistances(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if ((CombatManagerScript.damagePayload.aType == AttackType.ABILITY) && (CombatManagerScript.damagePayload.damage.damType != DamageTypes.PHYSICAL))
        {
            if (CombatManagerScript.damagePayload.resistMult < 1f)
            {
                // eg. Resist mult 0.901f
                float modified = 1f - CombatManagerScript.damagePayload.resistMult;
                modified *= 0.5f;
                float finalRes = 1f - modified; // This should be 0.95f ????
                CombatManagerScript.damagePayload.resistMult = finalRes;
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload ItemDreamDrum(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        // #todo - Some kinda drum beating sound effect
        TravelManager.ExitItemDream(withItem: true);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }


    public static EffectResultPayload MonsterStealFood(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter origFighter = effect.originatingActor as Fighter;

        foreach (Actor act in actorsToProcess)
        {
            if (act == effect.originatingActor) continue;

            Fighter ft = act as Fighter;

            Item foodStolen = TryStealFoodFromActor(origFighter, ft, true, true);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }        

    static Item TryStealFoodFromActor(Fighter origFighter, Fighter ft, bool printMessage, bool transferToAttackerInventory) 
    {
        // our pet should not eat our stuff
        if (origFighter.actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
        {
            return null;
        }

        Item itemToSteal = ft.myInventory.GetRandomFoodItem();

        if (itemToSteal != null)
        {
            if (printMessage) 
            {
                StringManager.SetTag(0, origFighter.displayName);
                StringManager.SetTag(1, ft.displayName);
                StringManager.SetTag(2, itemToSteal.displayName);
                GameLogScript.LogWriteStringRef("log_monster_pilfered_item", origFighter);
                BattleTextManager.NewText(StringManager.GetString("stole_food_bt"), origFighter.GetObject(), Color.green, 0.0f);
            }            
            
            Item oneUnitOfFood = ft.myInventory.GetItemAndSplitIfNeeded(itemToSteal, 1);

            if (transferToAttackerInventory)
            {
                origFighter.myInventory.AddItemRemoveFromPrevCollection(oneUnitOfFood, true);
            }            

            // Do anim!
            Sprite itemSpr = itemToSteal.GetSpriteForUI();
            
            TDAnimationScripts.TossItemSprite(itemSpr, ft, origFighter, 0.5f);
        }        

        return itemToSteal;
    }

    public static EffectResultPayload RunicChargeExecute(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter sa = effect.selfActor as Fighter;
        int charges = sa.myStats.CheckStatusQuantity("runic_charge");

        if (charges < 5)
        {
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }

        if (!sa.myStats.CheckHasStatusName("runic_crystal2_buff"))
        {
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }

        EffectScript combatBooster = GameMasterScript.GetEffectByRef("add_runic_combat_boost");
        combatBooster.CopyLiveData(effect);
        addWaitTime += combatBooster.DoEffect();

        EffectScript explosion = GameMasterScript.GetEffectByRef("runic_crystal_burst");
        explosion.CopyLiveData(effect);
        addWaitTime += explosion.DoEffect();

        sa.myStats.RemoveAllStatusByRef("runic_charge");

        sa.VerifyWrathBarIsActive();
        sa.wrathBarScript.UpdateWrathCount(0);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload RemoveArmoredTortoise(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        int numShards = GameMasterScript.heroPCActor.CountSummonRefs("obj_playericeshard_2");
        int numDefStacks = GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("status_defenseup10");

        if (numShards < numDefStacks)
        {
            int difference = numDefStacks - numShards;
            for (int i = 0; i < difference; i++)
            {
                GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("status_defenseup10");
            }

        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload PotionBuffSchematist(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        GameMasterScript.heroPCActor.SetActorData("schematist_infuse", 1);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirGrandRecovery", effect, true);

        GameLogScript.LogWriteStringRef("log_schematist_infuse");

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload FlaskBuffApple(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        GameMasterScript.heroPCActor.SetActorData("flask_apple_infuse", 1);
        CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirRecovery", effect, true);

        GameLogScript.LogWriteStringRef("log_apple_infuse");

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GenerateDreamCrystalAura(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter crystal = effect.originatingActor as Fighter;

        // Define item world crystal possible effects.
        int roll = UnityEngine.Random.Range(0, (int)ItemWorldAuras.COUNT - 1); // exclude "blessed pool"
        crystal.AddActorData("itemworldaura", roll);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload NegateRangedAttack(EffectScript effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();

        if (CombatManagerScript.bufferedCombatData == null) // ????
        {
            return erp;
        }

        StringManager.SetTag(0, effect.originatingActor.displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("exp_log_arrowbounce_shell"), effect.originatingActor);

        CombatManagerScript.ModifyBufferedDamageAsPercent(0f);
        CombatManagerScript.ModifyBufferedDamage(-9999f);

        erp.waitTime = 0f;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload MonsterMallet(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter origFighter = effect.originatingActor as Fighter;

        actorsToProcess.Remove(effect.originatingActor);
        if (actorsToProcess.Count == 0)
        {
            erp.waitTime = 0.0f;
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }
        Monster target = actorsToProcess[0] as Monster;

        float baseMalletPercent = GameMasterScript.heroPCActor.GetMonsterMalletThreshold();

        float curHealthPercent = target.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH);
        StringManager.SetTag(0, target.displayName);
        if (curHealthPercent > baseMalletPercent)
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_mallet_monsterhighhp"), GameMasterScript.heroPCActor);
            erp.waitTime = 0.0f;
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }
        if (target.isChampion || target.isBoss)
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_mallet_monsterstrong"), GameMasterScript.heroPCActor);
            erp.waitTime = 0.0f;
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }

        if (UnityEngine.Random.Range(0,100) < target.CheckAttribute(MonsterAttributes.NO_KNOCKOUT) ||
            target.turnsToDisappear > 0 || GameMasterScript.heroPCActor.myStats.CheckHasStatusName("monsterundying_temp") || target.myStats.CheckHasStatusName("monsterundying_temp"))
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_mallet_crystal"), GameMasterScript.heroPCActor);
            erp.waitTime = 0.0f;
            erp.actorsToProcess = actorsToProcess;
            return erp;
        }

        Actor removeActor = null;
        if (origFighter.summonedActors != null)
        {
            foreach (Actor act in origFighter.summonedActors)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.myStats.IsAlive())
                    {
                        if (mn.surpressTraits && mn.actorRefName != "mon_runiccrystal")
                        {
                            mn.ReverseMalletEffect();
                            removeActor = mn;
                            StringManager.SetTag(0, mn.displayName);
                            GameLogScript.GameLogWrite(StringManager.GetString("log_monster_mallet_wakeup"), origFighter);
                            break;
                        }
                    }
                }
            }
        }
        if (removeActor != null)
        {
            origFighter.RemoveSummon(removeActor);
        }

        target.HitWithMallet();

        LootGeneratorScript.DropItemsFromInventory(target, true, false, target.GetPos(), true, false, 0f);
        GameMasterScript.heroPCActor.DisconnectMimicsIfNecessary();

        if (MapMasterScript.activeMap.floor != MapMasterScript.TOWN2_MAP_FLOOR)
        {
            // Knocked out monster in dungeon for first time, tutorial popup?
            if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_pet_portal_escape") && PlayerOptions.tutorialTips)
            {                
                StringManager.SetTag(4, StringManager.GetPortalBindingString());
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_pet_portal_escape");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
            }
        }
        else
        {
            // Knock out harmless frog, probably?
            if (target.actorRefName == "mon_harmlessfungaltoad")
            {
                GameEventsAndTriggers.PlayerKnockedOutHarmlessFrog();
            }
        }

        if (target.actorRefName == "mon_goldfrog")
        {
            if (target.ReadActorData("coolfrog") == 1)
            {
                GameMasterScript.gmsSingleton.statsAndAchievements.CoolfrogCaptured();
            }
        }

        GameMasterScript.heroPCActor.SetActorData("knockedoutmonster", target.actorUniqueID);

        origFighter.AddSummon(target);
        origFighter.RemoveTarget(target);
        StringManager.SetTag(0, target.displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("log_monster_mallet_knockout"), origFighter);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;

        GameEventsAndTriggers.CheckForPainterQuestCompletion();

        return erp;
    }

    public static EffectResultPayload SummonRandomMoney(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        int numPiles = UnityEngine.Random.Range(1,5);

/*
        if (numPiles > 1 && UnityEngine.Random.Range(1,3) == 1)
        {
            numPiles--;
            // throw a gem instead

        }
        */

        float jpAmount = 50 + (UnityEngine.Random.Range(10,60) * GameMasterScript.heroPCActor.myStats.GetLevel());

        GameMasterScript.gmsSingleton.AwardJP(jpAmount);

        float moneyAmount = GameMasterScript.heroPCActor.myStats.GetLevel() * 100f;

        if (numPiles > 1) moneyAmount *= 0.75f;
        if (numPiles > 2) moneyAmount *= 0.66f;
        if (numPiles > 3) moneyAmount *= 0.5f;

        GenerateGoldPilesAroundTile(MapMasterScript.GetTile(effect.originatingActor.GetPos()), moneyAmount, numPiles);

        EffectResultPayload erp = new EffectResultPayload();

        return erp;
    }

    public static EffectResultPayload SummonFood(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        Fighter origFighter = effect.originatingActor as Fighter;

        effect.positions.Remove(origFighter.GetPos());
        CombatManagerScript.GenerateSpecificEffectAnimation(origFighter.GetPos(), "BellTingEffect", effect);

        // Reduce number of attempts to improve performance

        int maxAttempts = 50;
        if (PlatformVariables.OPTIMIZE_MONSTER_BEHAVIOR)
        {
            maxAttempts = 25;
        }
		
        foreach (Vector2 v2 in effect.positions)
        {
            if (!MapMasterScript.InBounds(v2)) continue;
            MapTileData mtd = MapMasterScript.GetTile(v2);
            if (mtd == null) continue;
            if (mtd.tileType != TileTypes.GROUND) continue;
            if (mtd.playerCollidable) continue;

            Consumable food = LootGeneratorScript.GenerateLootFromTable(1.3f, 0f, "food_and_meals") as Consumable;
            int attempts = 0;
            while (food == null 
                || (MapMasterScript.activeMap.IsMysteryDungeonMap() && DungeonMaker.disallowedItemsInMysteryDungeons.Contains(food.actorRefName))
                || (SharaModeStuff.IsSharaModeActive() && SharaModeStuff.disallowSharaModeItems.Contains(food.actorRefName))
                || (RandomJobMode.IsCurrentGameInRandomJobMode() && RandomJobMode.disallowRandomJobModeItems.Contains(food.actorRefName)))
            {
                attempts++;
                food = LootGeneratorScript.GenerateLootFromTable(1.3f, 0f, "food_and_meals") as Consumable;
                if (attempts > maxAttempts)
                {
                    break;
                }
            }
            if (attempts >= maxAttempts)
            {
                Debug.Log("WARNING: Could not find food to spawn?");
                continue;
            }
            MapMasterScript.activeMap.PlaceActor(food, mtd);
            MapMasterScript.singletonMMS.SpawnItem(food);
            food.myMovable.SetInSightAndSnapEnable(true);
        }
        BattleTextManager.NewText(StringManager.GetString("bell_ring_bt"), origFighter.GetObject(), Color.green, 0f);

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload RemovePositiveBuffs(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        List<StatusEffect> effectsToRemove = new List<StatusEffect>();
        foreach (Actor act in actorsToProcess)
        {
            if (act.actorfaction != effect.originatingActor.actorfaction)
            {
                if (act.IsFighter())
                {
                    Fighter ft = act as Fighter;
                    effectsToRemove.Clear();
                    foreach (StatusEffect se in ft.myStats.GetAllStatuses())
                    {
                        if (se.isPositive && !se.CheckDurTriggerOn(StatusTrigger.PERMANENT))
                        {
                            effectsToRemove.Add(se);
                        }
                    }
                    foreach (StatusEffect se in effectsToRemove)
                    {
                        ft.myStats.RemoveStatus(se, true);
                    }
                    if (effectsToRemove.Count > 0)
                    {
                        StringManager.SetTag(0, effect.originatingActor.displayName);
                        StringManager.SetTag(1, ft.displayName);
                        GameLogScript.GameLogWrite(StringManager.GetString("log_mon_clean_buffs"), effect.originatingActor);
                    }
                    CombatManagerScript.ProcessGenericEffect(effect.originatingActor as Fighter, ft, effect, false, true);
                }
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload CleanDebuffs(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        List<StatusEffect> effectsToRemove = new List<StatusEffect>();
        foreach (Actor act in actorsToProcess)
        {
            if (act.actorfaction == effect.originatingActor.actorfaction)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    effectsToRemove.Clear();
                    foreach (StatusEffect se in mn.myStats.GetAllStatuses())
                    {
                        if (!se.isPositive)
                        {
                            effectsToRemove.Add(se);
                        }
                    }
                    foreach (StatusEffect se in effectsToRemove)
                    {
                        mn.myStats.RemoveStatus(se, true);
                    }
                    if (effectsToRemove.Count > 0)
                    {
                        StringManager.SetTag(0, effect.originatingActor.displayName);
                        StringManager.SetTag(1, mn.displayName);
                        GameLogScript.GameLogWrite(StringManager.GetString("log_mon_clean_debuffs"), effect.originatingActor);
                    }
                    CombatManagerScript.ProcessGenericEffect(effect.originatingActor as Fighter, mn, effect, false, true);
                }
            }
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload RandomElementalShielding(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectScript customFX = null;
        EffectResultPayload erp = new EffectResultPayload();

        DamageTypes rand = (DamageTypes)UnityEngine.Random.Range(0, (int)DamageTypes.COUNT);
        switch (rand)
        {
            case DamageTypes.PHYSICAL:
                customFX = GameMasterScript.GetEffectByRef("physresist");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.FIRE:
                customFX = GameMasterScript.GetEffectByRef("fireresist");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.WATER:
                customFX = GameMasterScript.GetEffectByRef("waterresist");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.LIGHTNING:
                customFX = GameMasterScript.GetEffectByRef("lightningresist");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.POISON:
                customFX = GameMasterScript.GetEffectByRef("poisonresist");
                customFX.CopyLiveData(effect);
                break;
            case DamageTypes.SHADOW:
                customFX = GameMasterScript.GetEffectByRef("shadowresist");
                customFX.CopyLiveData(effect);
                break;
        }
        addWaitTime += customFX.DoEffect();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GamblerWildCards(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectScript customFX = null;
        EffectResultPayload erp = new EffectResultPayload();


        AbilityScript parentAbility = effect.parentAbility;
        Actor originatingActor = effect.originatingActor;

        PokerHands getHand = PlayingCard.theDeck[0].EvaluatePlayerHand();
        StringManager.SetTag(0, PlayingCard.handNames[(int)getHand]);

        //"You've got <color=yellow>" + PlayingCard.handNames[(int)getHand] + "</color>!", originatingActor);
        GameLogScript.GameLogWrite(StringManager.GetString("gambler_explain_hand"), originatingActor);

        foreach (PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
        {
            PlayingCard.ReturnCard(pc);
        }
        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, true);
        int numCards = GameMasterScript.heroPCActor.gamblerHand.Count;
        GameMasterScript.heroPCActor.gamblerHand.Clear();
        switch (getHand)
        {
            case PokerHands.ROYALFLUSH:
            case PokerHands.STRAIGHTFLUSH:
            case PokerHands.FULLHOUSE:
            case PokerHands.FOUROFAKIND:
                customFX = GameMasterScript.GetEffectByRef("gamblerbesthand");
                customFX.CopyLiveData(effect);
                customFX.extraTempData = numCards;
                break;
            case PokerHands.FLUSH:
                customFX = GameMasterScript.GetEffectByRef("gamblerflush");
                customFX.CopyLiveData(effect);
                customFX.extraTempData = numCards;
                break;
            case PokerHands.STRAIGHT:
                customFX = GameMasterScript.GetEffectByRef("gamblerstraight");
                customFX.CopyLiveData(effect);
                customFX.extraTempData = numCards;
                break;
            case PokerHands.THREEOFAKIND:
                customFX = GameMasterScript.GetEffectByRef("gamblerthreeofakind");
                customFX.CopyLiveData(effect);
                customFX.extraTempData = numCards;
                break;
            case PokerHands.TWOPAIR:
                customFX = GameMasterScript.GetEffectByRef("gamblerlionsummon");
                customFX.CopyLiveData(effect);
                if (customFX.positions.Count > 4)
                {
                    List<Vector2> newPos = new List<Vector2>();
                    while (newPos.Count < 4)
                    {
                        Vector2 randPos = customFX.positions[UnityEngine.Random.Range(0, customFX.positions.Count)];
                        newPos.Add(randPos);
                        customFX.positions.Remove(randPos);
                    }
                    customFX.positions = newPos;
                }
                customFX.extraTempData = numCards;
                break;
            case PokerHands.ONEPAIR:
                customFX = GameMasterScript.GetEffectByRef("gamblersharpeyes");
                customFX.CopyLiveData(effect);
                customFX.extraTempData = numCards;
                break;
            case PokerHands.HIGHCARD:
                customFX = GameMasterScript.GetEffectByRef("gamblerhighcard");
                customFX.CopyLiveData(effect);
                actorsToProcess.Remove(originatingActor);
                //parentAbility.AddAbilityTag(AbilityTags.PROJECTILE);
                if (actorsToProcess.Count > 0)
                {
                    Actor standalone = actorsToProcess[UnityEngine.Random.Range(0, actorsToProcess.Count)];
                    customFX.targetActors.Clear();
                    customFX.targetActors.Add(standalone);
                }
                customFX.extraTempData = numCards;
                break;
        }

        if (customFX != null)
        {
            addWaitTime += customFX.DoEffect();
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload AddPartingGifts(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();

        if (GameMasterScript.heroPCActor.summonedActors != null)
        {
            foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
            {
                if (act.GetActorType() == ActorTypes.MONSTER && act.actorUniqueID != GameMasterScript.heroPCActor.GetMonsterPetID())
                {
                    Monster mn = act as Monster;
                    if (mn.myStats.IsAlive() && (!mn.surpressTraits || mn.actorRefName.Contains("runiccrystal")))
                    {
                        mn.myStats.AddStatusByRef("status_partinggift", GameMasterScript.heroPCActor, 999);
                        CombatManagerScript.GenerateSpecificEffectAnimation(mn.GetPos(), "FervirBuff", null);
                    }
                }
            }

            CombatManagerScript.GenerateSpecificEffectAnimation(GameMasterScript.heroPCActor.GetPos(), "FervirBuff", effect, true);
        }

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GamblerRollDice_Alt(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();
        EffectScript customFX = null;
        int dice1 = UnityEngine.Random.Range(1, 7);
        int dice2 = UnityEngine.Random.Range(1, 7);
        BattleTextManager.NewText(dice1 + " " + StringManager.GetString("misc_and") + " " + dice2 + "!", GameMasterScript.heroPCActor.GetObject(), Color.yellow, 0.2f, 1.5f);
        UIManagerScript.PlayCursorSound("Roll Dice");

        customFX = GameMasterScript.GetEffectByRef("gamblerdarts2");
        customFX.CopyLiveData(effect);
        customFX.extraTempData = dice1 + dice2;
        //effect.parentAbility.AddAbilityTag(AbilityTags.PROJECTILE);

        StringManager.SetTag(0, effect.originatingActor.displayName);
        StringManager.SetTag(1, dice1.ToString());
        StringManager.SetTag(2, dice2.ToString());
        GameLogScript.GameLogWrite(StringManager.GetString("log_player_rolldice"), effect.originatingActor);

        BattleTextManager.NewText(StringManager.GetString("gambler_darts"), effect.originatingActor.GetObject(), Color.blue, 1.2f);

        customFX.extraWaitTime = 0.4f;
        addWaitTime = customFX.DoEffect();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload GamblerRollDice(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        GameMasterScript.SetAnimationPlaying(true);
        float addWaitTime = 0;
        EffectScript customFX = null;
        EffectResultPayload erp = new EffectResultPayload();
        int roll1 = UnityEngine.Random.Range(1, 7);
        int roll2 = UnityEngine.Random.Range(1, 7);
        BattleTextManager.NewText(roll1 + " " + StringManager.GetString("misc_and") + " " + roll2 + "!", GameMasterScript.heroPCActor.GetObject(), Color.yellow, 0.2f, 1.5f);
        UIManagerScript.PlayCursorSound("Roll Dice");
        int total = roll1 + roll2;
        string extraText = "";

        switch (total)
        {
            case 2: // 2.7%
                extraText = "<color=orange>" + StringManager.GetString("misc_snakeeyes") + "</color>";
                //effect.parentAbility.RemoveAbilityTag(AbilityTags.PROJECTILE);
                customFX = GameMasterScript.GetEffectByRef("gamblerdeath");
                customFX.CopyLiveData(effect);
                BattleTextManager.NewText(StringManager.GetString("gambler_death"), effect.originatingActor.GetObject(), Color.red, 1.2f);
                break;
            case 3:
            case 4:
            case 5:
                customFX = GameMasterScript.GetEffectByRef("gamblerdarts");
                customFX.CopyLiveData(effect);
                //effect.parentAbility.AddAbilityTag(AbilityTags.PROJECTILE);
                BattleTextManager.NewText(StringManager.GetString("gambler_darts"), effect.originatingActor.GetObject(), Color.blue, 1.2f);
                break;
            case 6:
                customFX = GameMasterScript.GetEffectByRef("gamblercurse");
                //BattleTextManager.NewText(StringManager.GetString("gambler_curse"), effect.originatingActor.GetObject(), Color.red, 1.2f);
                customFX.CopyLiveData(effect);

                break;
            case 7: // 16%
                extraText = "<color=#40b843>" + StringManager.GetString("misc_lucky") + "</color>";
                //effect.parentAbility.RemoveAbilityTag(AbilityTags.PROJECTILE);
                customFX = GameMasterScript.GetEffectByRef("gamblerladyluck");
                BattleTextManager.NewText(StringManager.GetString("gambler_lucky"), effect.originatingActor.GetObject(), Color.yellow, 1.2f);
                customFX.CopyLiveData(effect);
                break;
            case 8:
            case 9:
            case 10:
            case 11:
                customFX = GameMasterScript.GetEffectByRef("gamblerwaterdarts");
                customFX.CopyLiveData(effect);
                //effect.parentAbility.AddAbilityTag(AbilityTags.PROJECTILE);
                BattleTextManager.NewText(StringManager.GetString("gambler_icedarts"), effect.originatingActor.GetObject(), Color.blue, 1.2f);
                break;
            case 12: // 2.7%
                extraText = "<color=orange>" + StringManager.GetString("misc_maximum") + "</color>";
                //effect.parentAbility.RemoveAbilityTag(AbilityTags.PROJECTILE);
                customFX = GameMasterScript.GetEffectByRef("gamblerheal");
                customFX.CopyLiveData(effect);
                break;
        }
        StringManager.SetTag(0, effect.originatingActor.displayName);
        StringManager.SetTag(1, roll1.ToString());
        StringManager.SetTag(2, roll2.ToString());
        GameLogScript.GameLogWrite(StringManager.GetString("log_player_rolldice") + " " + extraText, effect.originatingActor);

        customFX.extraWaitTime = 0.4f;
        addWaitTime = customFX.DoEffect();

        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;
    }

    public static EffectResultPayload LifesapDamage(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        EffectResultPayload erp = new EffectResultPayload();
        Fighter target = null;

        float addWaitTime = 0f;

        foreach (Actor act in actorsToProcess)
        {
            if (act.GetActorType() == ActorTypes.HERO || act.actorfaction == Faction.PLAYER)
            {
                target = act as Fighter;
                break;
            }
        }

        if (target != null)
        {
            float damage = target.myStats.GetCurStat(StatTypes.HEALTH) * 0.2f;
            target.myStats.ChangeStat(StatTypes.HEALTH, damage * -1f, StatDataTypes.CUR, true);
            
            if (target.GetActorType() == ActorTypes.HERO)
            {
                GameMasterScript.heroPCActor.CheckForLimitBreakOnDamageTaken(damage);
            }

            BattleTextManager.NewDamageText((int)damage, false, Color.red, target.GetObject(), 0.5f, 1.2f);

            CombatManagerScript.GenerateSpecificEffectAnimation(target.GetPos(), "VitalBleedEffect", null);

            StringManager.SetTag(0, effect.originatingActor.displayName);
            StringManager.SetTag(3, "20" + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT));
            StringManager.SetTag(1, ((int)damage).ToString());
            StringManager.SetTag(2, target.displayName);
            GameLogScript.LogWriteStringRef("log_lifesap_dmg");
        }
        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    static List<Destructible> destructiblesToClear;

    public static EffectResultPayload DestroyGroundObjects(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        Actor casterActor = effect.originatingActor;

        CustomAlgorithms.GetNonCollidableTilesAroundPoint(casterActor.GetPos(), 2, casterActor, MapMasterScript.activeMap);

        Faction searchFaction = Faction.PLAYER;

        if (destructiblesToClear == null) destructiblesToClear = new List<Destructible>();
        destructiblesToClear.Clear();

        if (casterActor.actorfaction == Faction.PLAYER)
        {
            searchFaction = Faction.ENEMY;
        }

        for (int i = 0; i < CustomAlgorithms.numNonCollidableTilesInBuffer; i++)
        {
            MapTileData mtd = CustomAlgorithms.nonCollidableTileBuffer[i];

            foreach(Actor act in mtd.GetAllActors())
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
                Destructible dt = act as Destructible;
                if (dt.destroyed || dt.isDestroyed || dt.isInDeadQueue) continue;
                if (dt.actorfaction != searchFaction) continue;
                destructiblesToClear.Add(dt);
            }
        }

        foreach(Destructible dt in destructiblesToClear)
        {
            dt.RemoveImmediately();
            GameMasterScript.AddToDeadQueue(dt);
            MapTileData mtd = MapMasterScript.activeMap.GetTile(dt.GetPos());
            mtd.RemoveActor(dt);
            CombatManagerScript.GenerateSpecificEffectAnimation(mtd.pos, "DirtParticleExplosion", null, false, 0f, true);
            // any animation needed?
        }

        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = 0f;

        return erp;
    }

    public static EffectResultPayload FirePhotoCannonVisual(SpecialEffect effect, List<Actor> actorsToProcess)
    {
        //Find out who's doing the shooting
        Actor casterActor = effect.originatingActor;
        Actor optionActor = null;

        if (casterActor == GameMasterScript.heroPCActor)
        {
            optionActor = MapMasterScript.activeMap.FindActor("mon_runiccrystal");
        }

        //Get the most recent line draw direction, that's probably it.
        Vector2 vBeamDir = MapMasterScript.xDirections[(int)UIManagerScript.singletonUIMS.GetLineDir()];
        Vector2 vDest = casterActor.GetPos() + (vBeamDir * 5.0f);

        //we end up adding an offset to start positions for vanity's sake
        Vector2 vShotOffset = vBeamDir * 0.25f;

        //pew pew
        GameMasterScript.StartWatchedCoroutine(FireAndPulseBeam("InstantLaserEffect", casterActor.GetPos() + vShotOffset, vDest, 0.6f, 1.0f, 0.5f, 0.0f, "StartPurpleLaser"));

        //pew pew from our crystal as well
        if (optionActor != null)
        {
            vDest = optionActor.GetPos() + (vBeamDir * 5.0f);
            GameMasterScript.StartWatchedCoroutine(FireAndPulseBeam("InstantLaserEffect", optionActor.GetPos() + vShotOffset, vDest, 0.6f, 1.0f, 0.5f, 0.0f, "StartPurpleLaser"));
        }

        //return values for TD
        float addWaitTime = 0;
        EffectResultPayload erp = new EffectResultPayload();
        erp.waitTime = addWaitTime;
        erp.actorsToProcess = actorsToProcess;
        return erp;

    }

    //fires and pulses a beam between two points
    static IEnumerator FireAndPulseBeam(string strBeamEffectRef, Vector2 vStartPos, Vector2 vEndPos,
        float fBeamLifetime, float fBaseWidth = 1.0f, float fPulseVariance = 0.0f, float fPreBeamSkinnyChargeUpTime = 0f, string strOriginFireFlashParticleRef = "")
    {
        //beam direction
        Vector2 vBeamDirNormalized = (vEndPos - vStartPos);
        float fDistance = vBeamDirNormalized.magnitude;
        vBeamDirNormalized.Normalize();

        //Generate a laser beam
        GameObject laserObject = CombatManagerScript.GenerateSpecificEffectAnimation(vStartPos, strBeamEffectRef, null, false);
        var laserAnimatable = laserObject.GetComponent<Animatable>();

        //place it in the dead center between start and end positions
        Vector2 middlePosition = vStartPos + (vBeamDirNormalized * fDistance * 0.5f);
        laserObject.transform.position = new Vector3(middlePosition.x, middlePosition.y, laserObject.transform.position.z);

        //the yscale is the distance from start to end
        laserAnimatable.ToggleIgnoreScale();
        laserObject.transform.localScale = new Vector3(1f, fDistance, 1f);

        //rotate it correctly
        float targetAngle = CombatManagerScript.GetAngleBetweenPoints(vStartPos, vEndPos) * -1f;
        Vector3 eulerang = new Vector3(laserObject.transform.eulerAngles.x, laserObject.transform.eulerAngles.y, laserObject.transform.eulerAngles.z + targetAngle);
        laserObject.transform.eulerAngles = eulerang;

        float fCurrentTime = 0f;
        float fMaxLifetime = fBeamLifetime + fPreBeamSkinnyChargeUpTime;

        //if we do a pre-warmup, don't show the flash until then
        bool bWarmupComplete = false;
        GameObject flashObject = null;

        while (fCurrentTime < fMaxLifetime)
        {
            //determine width this frame
            float fWidth = 1f;

            //if we're charging up
            if (fCurrentTime < fPreBeamSkinnyChargeUpTime)
            {
                fWidth = 0.02f;
            }
            //otherwise pulse as requested
            else
            {
                //if this is our first time post warmup
                if (!bWarmupComplete)
                {
                    bWarmupComplete = true;

                    //maybe we have a flashing particle
                    if (!string.IsNullOrEmpty(strOriginFireFlashParticleRef))
                    {
                        flashObject = CombatManagerScript.GenerateSpecificEffectAnimation(vStartPos, strOriginFireFlashParticleRef, null, false);
                        flashObject.transform.rotation = laserObject.transform.rotation;

                    }
                }

                fWidth = fBaseWidth + (UnityEngine.Random.value - 0.5f) * (fPulseVariance * 2.0f);
            }

            laserObject.transform.localScale = new Vector3(fWidth, fDistance, 1f);

            fCurrentTime += Time.deltaTime;
            yield return null;

            //tell it to live until we allow it to die.
            laserAnimatable.OverrideCompletionBehavior("Loop");
            if (flashObject != null)
            {
                Animatable anm = flashObject.GetComponentInChildren<Animatable>();
                if (anm == null)
                {
                    //Debug.Log(flashObject.gameObject.name + " has no animatable child?");
                }
                else
                {
                    anm.OverrideCompletionBehavior("Loop");
                }
                
            }

        }

        //clean up
        laserAnimatable.OverrideCompletionBehavior("Stop");
        laserAnimatable.StopAnimation();
        laserObject.GetComponent<SpriteEffect>().CleanUpAndReturnToStack();

        if (flashObject != null)
        {
            Animatable anm = flashObject.GetComponentInChildren<Animatable>();
            if (anm == null)
            {
                //Debug.Log(flashObject.gameObject.name + " has no animatable child?");
            }
            else
            {
                flashObject.GetComponentInChildren<Animatable>().OverrideCompletionBehavior("Stop");
                flashObject.GetComponentInChildren<SpriteEffect>().CleanUpAndReturnToStack();                
                GameMasterScript.ReturnToStack(flashObject, strOriginFireFlashParticleRef);
            }
        }


    }
}
