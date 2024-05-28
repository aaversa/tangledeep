using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Reflection;

public partial class DialogEventsScript
{
    /* public static bool PopulateFoundRelicText(string value)
    {
        TextBranch tb = UIManagerScript.currentTextBranch;

        StringBuilder txt = new StringBuilder();

        int itemIDInBox = GameMasterScript.gmsSingleton.ReadTempGameData("itemidinbox");
        Debug.Log("Item is " + itemIDInBox);
        Item linkItem = GameMasterScript.gmsSingleton.TryLinkActorFromDict(itemIDInBox) as Item;

        txt.Append(linkItem.GetItemInformationNoName(true));

        tb.text = txt.ToString();
        //UIManagerScript.UpdateDialogBox();
        
        return true;
    } */

    public static bool SaveRelicForAfterDungeon(string value)
    {
        // Store this relic in the bank for later!
        int scaledRelicID = GameMasterScript.gmsSingleton.ReadTempGameData("levelscaledrelicid");
        Item scaledRelic = GameMasterScript.gmsSingleton.TryLinkActorFromDict(scaledRelicID) as Item;

        // For now, stick it in the bank!
        NPC banker = MapMasterScript.singletonMMS.townMap.FindActor("npc_banker") as NPC;
        banker.myInventory.AddItem(scaledRelic, false);

        StringManager.SetTag(0, scaledRelic.displayName);
        GameLogScript.LogWriteStringRef("log_bankrelic");
                
        GameMasterScript.heroPCActor.myMysteryDungeonData.AddBankedRelic(scaledRelic);

        // We need to remove it from our inventory / equipment now though.
        int relicID = GameMasterScript.gmsSingleton.ReadTempGameData("id_relicfound");

        Item nonScaledRelic = GameMasterScript.gmsSingleton.TryLinkActorFromDict(relicID) as Item;

        GameMasterScript.heroPCActor.myInventory.RemoveItem(nonScaledRelic, true);

        Weapon w = nonScaledRelic as Weapon;
        if (w != null)
        {
            UIManagerScript.RemoveWeaponFromActives(w);
        }

        Item templateRelic = SharedBank.allRelicTemplates[nonScaledRelic.actorRefName];
        templateRelic.CopyFromItem(scaledRelic);

        if (Debug.isDebugBuild) Debug.Log("Wrote CV: " + templateRelic.challengeValue + " for " + templateRelic.actorRefName + " " + scaledRelic.challengeValue);

        //UIManagerScript.CloseDialogBox();
        return true;
    }

    public static bool KeepRelicOnlyInDungeon(string value)
    {
        // We are keeping the most recently picked up Relic item, but only in the current dungeon.
        int relicID = GameMasterScript.gmsSingleton.ReadTempGameData("id_relicfound");
        Item relic = GameMasterScript.gmsSingleton.TryLinkActorFromDict(relicID) as Item;
        if (relic == null)
        {
            Debug.LogError("Something really failed, the relic does not exist! " + relicID);
        }
        else
        {
            MysteryDungeonManager.MarkRelicForDeletion(relic);            
        }

        StringManager.SetTag(0, relic.displayName);
        GameLogScript.LogWriteStringRef("log_keeprelic");

        //UIManagerScript.CloseDialogBox();
        return true;
    }

    public static bool ShowLevelScaledRelic(string value)
    {
        // Find the ID of the item that is currently being shown.
        int itemID = GameMasterScript.gmsSingleton.ReadTempGameData("itemidinbox");
        
        Equipment foundRelic = GameMasterScript.gmsSingleton.TryLinkActorFromDict(itemID) as Equipment;

        if (Debug.isDebugBuild) Debug.Log("Let's look at item " + itemID + " to create scaled version. " + foundRelic.actorRefName + " " + foundRelic.displayName);

        Equipment scaledVersion = null;

        int scaledRelicID = GameMasterScript.gmsSingleton.ReadTempGameData("levelscaledrelicid");
        if (scaledRelicID > 0)
        {
            // we already have a scaled version.
            scaledVersion = GameMasterScript.gmsSingleton.TryLinkActorFromDict(scaledRelicID) as Equipment;
        }
        else
        {
            // don't have a scaled version yet, so make one
            scaledVersion = LegendaryMaker.CreateLevelScaledVersionOfRelic(foundRelic);
        }

        GameMasterScript.gmsSingleton.SetTempStringData("leg_origtype", foundRelic.GetDisplayItemType());
        GameMasterScript.gmsSingleton.SetTempStringData("leg_origname", foundRelic.displayName);        
        GameMasterScript.gmsSingleton.SetTempStringData("scaledleg_info", scaledVersion.GetItemInformationNoName(false));

        string originalItemText = UIManagerScript.myDialogBoxComponent.GetDialogText().text;
        GameMasterScript.gmsSingleton.SetTempStringData("nonscaled_legtext", originalItemText);

        GameMasterScript.gmsSingleton.SetTempGameData("levelscaledrelicid", scaledVersion.actorUniqueID);
        //TextBranch branch2 = UIManagerScript.currentConversation.FindBranch("main2");

        return true;
    }

    public static bool BeginSharaBoss4Phase2_Part3(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.StartCoroutine(GameEventsAndTriggers.SharaBoss4Phase2Begin_Part3());
        return false;
    }

    public static bool WandererTransformIntoEidolon(string value)
    {
        // Find the wanderer on the map - he's the guy we're talking to!
        NPC wanderer = UIManagerScript.currentConversation.whichNPC;

        UIManagerScript.CloseDialogBox();

        GameMasterScript.SetAnimationPlaying(true, true);

        GameMasterScript.gmsSingleton.StartCoroutine(DialogEventsScript.IWandererTransformIntoEidolon(wanderer));
        return false;
    }

    public static bool EidolonTransformIntoWandererAndUnlockRealm(string value)
    {
        NPC wanderer = UIManagerScript.currentConversation.whichNPC;
        UIManagerScript.CloseDialogBox();

        GameObject eidolon = GameMasterScript.gmsSingleton.ReadTempGameObject("wanderer_eidolon");
        if (eidolon != null)
        {
            GameMasterScript.gmsSingleton.WaitThenDestroyObject(eidolon, 0.01f);
        }

        UIManagerScript.FlashWhite(0.7f);
        UIManagerScript.PlayCursorSound("EnterItemWorld");
        UIManagerScript.PlayCursorSound("UltraLearn");

        wanderer.myMovable.FadeIn();

        ProgressTracker.SetProgress(TDProgress.REALMGODS_UNLOCKED, ProgressLocations.META, 1);

        DLCManager.CreateRealmOfGodsStairsInFinalBossRoomIfAllowed();
        
        return false;
    }

    public static IEnumerator IWandererTransformIntoEidolon(Actor wanderer)
    {
        // todo - play coooool music here?

        UIManagerScript.PlayCursorSound("Mirage");
        GameObject particles = CombatManagerScript.GenerateSpecificEffectAnimation(wanderer.GetPos(), "ChargingSkillParticles", null, false);
        yield return new WaitForSeconds(2f);

        UIManagerScript.FlashWhite(1.0f);
        GameMasterScript.cameraScript.AddScreenshake(0.7f);
        CombatManagerScript.GenerateSpecificEffectAnimation(wanderer.GetPos(), "TeleportDown", null, true);
        GameMasterScript.ReturnToStack(particles, "ChargingSkillParticles");
        wanderer.myMovable.ForceFadeOut();

        // Replace with Eidolon
        GameObject eidolon = GameMasterScript.TDInstantiate("MonsterSpiritMoose");
        eidolon.transform.position = wanderer.GetPos();
        eidolon.GetComponent<Animatable>().SetAnim("Idle");
        GameObject fadeawayParticles = GameMasterScript.TDInstantiate("FadeAwayParticles");
        fadeawayParticles.transform.position = eidolon.transform.position;

        GameMasterScript.gmsSingleton.SetTempGameObject("wanderer_eidolon", eidolon);

        GameMasterScript.gmsSingleton.WaitThenDestroyObject(fadeawayParticles, 9f);

        // linger...
        yield return new WaitForSeconds(3.0f);

        GameMasterScript.SetAnimationPlaying(false);

        UIManagerScript.StartConversationByRef("wanderer_realmofgods_continue", DialogType.KEYSTORY, wanderer as NPC);

    }
   
    /// <summary>
    /// Value is an enum that we can parse to determine what slot to upgrade. Slot is guaranteed to have gear in it.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool SelectGearForUpgrade(string value)
    {
        MiniDreamcasterDataPack mddp = ItemDreamFunctions.GetItemForMiniDreamcasterFromDialogString(value);

        int goldCost = ItemDreamFunctions.CalculateMiniDreamcasterCost(mddp.checkItem); // if 0, it's not upgradeable.
        Item getOrb = GameMasterScript.heroPCActor.myInventory.GetItemByRef("orb_itemworld");
        if (getOrb == null)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_iw_need_orb");
            return false;
        }
        if (goldCost > GameMasterScript.heroPCActor.GetMoney())
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("exp_misc_notenoughgold_upgrade");
            return false;
        }

        GameMasterScript.gmsSingleton.SetTempStringData("mditemvalue", value);

        TextBranch orbSelect = UIManagerScript.currentConversation.FindBranch("selectorb");
        orbSelect.responses.Clear();
        // update orbSelect responses based on what orbs we have access to
        List<Item> validOrbsToUse = new List<Item>();
        bool hasRegularOrb = false;
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (itm.actorRefName != "orb_itemworld") continue;
            if (itm.IsJobSkillOrb() || itm.IsLucidOrb())
            {
                validOrbsToUse.Add(itm);
            }
            else
            {
                if (!hasRegularOrb)
                {
                    validOrbsToUse.Add(itm);
                }
            }
        }

        foreach(Item orb in validOrbsToUse)
        {
            ButtonCombo bc = new ButtonCombo();
            bc.dialogEventScript = "UpgradeGearFromMiniDreamcaster";
            bc.dialogEventScriptValue = orb.actorUniqueID.ToString();
            bc.actionRef = "continue";
            bc.buttonText = orb.displayName;
            bc.dbr = DialogButtonResponse.CONTINUE;
            orbSelect.responses.Add(bc);
        }

        ButtonCombo backBC = new ButtonCombo();
        backBC.actionRef = "upgrade";
        backBC.dbr = DialogButtonResponse.CONTINUE;
        backBC.buttonText = StringManager.GetString("exp_wanderer_selectdungeon_no");
        orbSelect.responses.Add(backBC);

        UIManagerScript.SwitchConversationBranch(orbSelect);
        UIManagerScript.UpdateDialogBox();
        return false;
    }

    /// <summary>
    /// Passed in "value" is the int ID of the orb to use.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool UpgradeGearFromMiniDreamcaster(string value)
    {
        MiniDreamcasterDataPack mddp = ItemDreamFunctions.GetItemForMiniDreamcasterFromDialogString(GameMasterScript.gmsSingleton.ReadTempStringData("mditemvalue"));

        int goldCost = ItemDreamFunctions.CalculateMiniDreamcasterCost(mddp.checkItem);

        NPC dreamcaster = UIManagerScript.currentConversation.whichNPC;

        int parseOrbId = Int32.Parse(value);
        Item getOrb = GameMasterScript.gmsSingleton.TryLinkActorFromDict(parseOrbId) as Item;

        GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(getOrb, -1);
        GameMasterScript.heroPCActor.ChangeMoney(-1 * goldCost);

        bool wasEquipped = GameMasterScript.heroPCActor.myEquipment.IsEquipped(mddp.checkItem);
        if (wasEquipped && mddp.slot != EquipmentSlots.COUNT)
        {
            GameMasterScript.heroPCActor.myEquipment.Unequip(mddp.slot, true, SND.SILENT, false, true);
        }
        
        if (mddp.checkItem.timesUpgraded < Equipment.GetMaxUpgrades())
        {
            mddp.checkItem.UpgradeItem(); // we only need to rebuild display name here
        }

        string mmRef = getOrb.ReadActorDataString("magicmodref");
        MagicMod mmTemplate = null;
        if (!string.IsNullOrEmpty(mmRef))
        {
            mmTemplate = MagicMod.FindModFromName(mmRef);
        }
        
        bool upgraded = false;
        if (mddp.checkItem.CanHandleFreeSkillOrb() && mmTemplate != null && mmTemplate.bDontAnnounceAddedAbilities)
        {
            EquipmentBlock.MakeMagicalFromMod(mddp.checkItem, mmTemplate, true, true, true, false);
            upgraded = true;
        }
        if (!upgraded && mddp.checkItem.CanHandleMoreMagicMods())
        {
            if (mmTemplate != null)
            {
                EquipmentBlock.MakeMagicalFromMod(mddp.checkItem, mmTemplate, true, true, true, false);
            }
            else
            {
                EquipmentBlock.MakeMagical(mddp.checkItem, mddp.checkItem.challengeValue, true, false);
            }            
        }

        UIManagerScript.FlashWhite(0.6f);
        UIManagerScript.PlayCursorSound("Mirage");

        mddp.checkItem.RebuildDisplayName();

        StringManager.SetTag(0, mddp.checkItem.displayName);

        UIManagerScript.RefreshPlayerStats();
        GameLogScript.LogWriteStringRef("exp_misc_itemupgraded");

        GameMasterScript.gmsSingleton.WaitThenDestroyObject(dreamcaster.GetObject(), 1.0f);
        dreamcaster.myMovable.WaitThenFadeOut(0.75f, 0.25f);
        CombatManagerScript.WaitThenGenerateSpecificEffect(dreamcaster.GetPos(), "SmallExplosionEffect", null, 0.75f, true);
        MapMasterScript.activeMap.RemoveActorFromMap(dreamcaster);

        if (wasEquipped)
        {
            GameMasterScript.heroPCActor.myEquipment.EquipOnlyIfValid(mddp.checkItem, SND.PLAY, mddp.slot, false, true);
        }        
        UIManagerScript.CloseDialogBox();
        return false;

    }

    public static bool DetermineMiniDreamcasterGearEligibility(string value)
    {
        UIManagerScript.PlayCursorSound("SupervisorSound");

        TextBranch upgradeBranch = UIManagerScript.currentConversation.FindBranch("upgrade");

        bool haveAnOrbOfReverie = GameMasterScript.heroPCActor.myInventory.GetItemByRef("orb_itemworld") != null;

        // All buttons here with a branchref other than "exit" correspond to upgrading a piece of gear
        // We want to change the button text based on what we have equipped and the cost
        foreach(ButtonCombo bc in upgradeBranch.responses)
        {
            if (bc.actionRef == "exit") continue;

            Equipment checkItem = null;            

            int hbSlot = 0;
            if (bc.actionRef.Contains("HOTBAR"))
            {
                hbSlot = Int32.Parse(bc.actionRef.Substring(6));
                checkItem = UIManagerScript.hotbarWeapons[hbSlot];
                bc.dialogEventScriptValue = "HOTBAR" + hbSlot;
            }
            else
            {
                EquipmentSlots slot = (EquipmentSlots)Enum.Parse(typeof(EquipmentSlots), bc.actionRef);
                checkItem = GameMasterScript.heroPCActor.myEquipment.GetEquipmentInSlot(slot);
                bc.dialogEventScriptValue = slot.ToString();
            }
                        
            if (checkItem == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(checkItem, onlyActualFists: true))
            {
                bc.visible = false;
                continue;
            }
            bc.visible = true;

            int goldCost = ItemDreamFunctions.CalculateMiniDreamcasterCost(checkItem); // if 0, it's not upgradeable.

            if (goldCost > GameMasterScript.heroPCActor.GetMoney() || !haveAnOrbOfReverie)
            {
                // 2 expensive
                bc.buttonText = checkItem.displayName + ": " + UIManagerScript.redHexColor + goldCost + StringManager.GetString("misc_moneysymbol") + "</color>";
            }
            else
            {
                // we can afford it
                bc.buttonText = checkItem.displayName + ": " + goldCost + StringManager.GetString("misc_moneysymbol");
            }            
            //string slotNameForData = "dream" + slot.ToString().ToLowerInvariant();
            //GameMasterScript.heroPCActor.SetActorData(slotNameForData, goldCost);
        }


        return true;
    }

    public static bool SharaPowerEnhanceNotification(string value)
    {
        UIManagerScript.CloseDialogBox();

        UIManagerScript.StartConversationByRef("shara_ability_update", DialogType.STANDARD, null);

        if (!string.IsNullOrEmpty(value))
        {
            int pValue = 0;
            Int32.TryParse(value, out pValue);
            if (pValue == 1)
            {
                // this is our final upgrade, add one more notification
                UIManagerScript.StartConversationByRef("shara_ability_finalupdate", DialogType.STANDARD, null);
            }

        }

        

        return false;
    }

    public static bool LearnRandomArmorMastery(string value)
    {
        NPC statue = UIManagerScript.currentConversation.whichNPC;

        UIManagerScript.CloseDialogBox(false);

        string baseMastery = MasteriesBakedData.GetUnknownArmorMastery();

        if (string.IsNullOrEmpty(baseMastery))
        {
            // Player somehow knows everything, so do nothing.
            GameLogScript.LogWriteStringRef("exp_log_mastery_knowitall");
            return false;
        }

        AbilityScript abilTemplate = GameMasterScript.masterAbilityList[baseMastery];
        GameMasterScript.heroPCActor.LearnAbility(abilTemplate, true, true, false);        

        FlashEffectsAndRemoveActor(statue);

        return false;
    }

    public static bool LearnRandomWeaponMasteryTree(string value)
    {
        NPC statue = UIManagerScript.currentConversation.whichNPC;

        UIManagerScript.CloseDialogBox(false);

        string baseMastery = MasteriesBakedData.GetUnknownWeaponMastery();

        if (string.IsNullOrEmpty(baseMastery))
        {
            // Player somehow knows everything, so do nothing.
            GameLogScript.LogWriteStringRef("exp_log_mastery_knowitall");
            return false;
        }

        baseMastery = baseMastery.Substring(0, baseMastery.Length - 1); // skill_clawmastery1 becomes skill_clawmastery
        for (int i = 1; i < 5; i++)
        {
            // We want to learn skill_clawmastery1 through 4
            string skillName = baseMastery + i;
            AbilityScript abilTemplate = GameMasterScript.masterAbilityList[skillName];
            GameMasterScript.heroPCActor.LearnAbility(abilTemplate, true, true, false);
        }

        FlashEffectsAndRemoveActor(statue);

        return false;
    }

    static void FlashEffectsAndRemoveActor(Actor act)
    {
        UIManagerScript.FlashWhite(1f);
        GameMasterScript.cameraScript.AddScreenshake(0.6f);
        UIManagerScript.PlayCursorSound("Mirage");

        MapMasterScript.activeMap.RemoveActorFromMap(act);
        act.myMovable.FadeOutThenDie();
    }

    public static bool CloseMysteryDungeon(string value)
    {
        UIManagerScript.CloseDialogBox(false, true);

        GameMasterScript.heroPCActor.myMysteryDungeonData.dungeonVictory = false;
        GameMasterScript.gmsSingleton.SetTempGameData("losingmysterydungeon", 1);
        MysteryDungeonManager.CompleteActiveMysteryDungeon();

        return false;
    }

    public static bool BeginMysteryDungeon(string value)
    {
        UIManagerScript.CloseDialogBox();

        MysteryDungeonManager.EnterMysteryDungeon(value);

        return false;
    }

    public static bool StartCutscene(string value)
    {
        UIManagerScript.CloseDialogBox();

        MethodInfo cutsceneMethod = CustomAlgorithms.TryGetMethod(typeof(Cutscenes), value);
        IEnumerator cutsceneEnumerator = (IEnumerator)cutsceneMethod.Invoke(null, null);
        GameMasterScript.gmsSingleton.StartCoroutine(cutsceneEnumerator);

        return false;
    }

    public static bool BeastDungeonPreWave1(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.gmsSingleton.StartCoroutine(DLCCutscenes.BeastDungeonFirstWavePart2());
        return false;
    }

    public static bool PanToMightyVine(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.gmsSingleton.StartCoroutine(DLCCutscenes.BeastDungeonIntroToMightyVine());
        return false;
    }

    public static bool BeginSharaFinalBossPhase3(string value)
    {
        GameMasterScript.SetAnimationPlaying(false);
        UIManagerScript.CloseDialogBox();
        StartEncounterWithBoss("mon_shara_finalboss");
        return false;
    }

    public static void StartEncounterWithBoss(string actorRef, TDProgress progressKey = TDProgress.COUNT, int progressValue = 0)
    {
        UIManagerScript.CloseDialogBox();
        Monster bossActor = MapMasterScript.activeMap.FindActor(actorRef) as Monster;
        GameMasterScript.cameraScript.SetCustomCameraAnimation(bossActor.GetPos(), GameMasterScript.heroPCActor.GetPos(), 0.5f, true);
        BossHealthBarScript.EnableBossWithAnimation(bossActor);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        if (progressKey != TDProgress.COUNT)
        {
            ProgressTracker.SetProgress(progressKey, ProgressLocations.HERO, progressValue);
        }
        foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorfaction == Faction.PLAYER) continue;
            m.AddAggro(GameMasterScript.heroPCActor, 50f);
            m.myBehaviorState = BehaviorState.FIGHT;
        }
    }
    
    public static bool TryLearnSkillFromRuneStone(string value)
    {
        UIManagerScript.CloseDialogBox();

        // Get ref and cost based on the stone that was used
        string abilRef = GameMasterScript.gmsSingleton.ReadTempStringData("skillreftolearn");
        int jpCost = GameMasterScript.gmsSingleton.ReadTempGameData("itemskilljpcost");

        if (jpCost > GameMasterScript.heroPCActor.GetCurJP())
        {
            GameLogScript.LogWriteStringRef("cant_learn_no_jp");
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            return false;
        }

        GameMasterScript.heroPCActor.SetJP(GameMasterScript.heroPCActor.GetCurJP() - jpCost);

        int idOfItem = GameMasterScript.gmsSingleton.ReadTempGameData("id_itembeingused");
        Item runeStone = GameMasterScript.gmsSingleton.TryLinkActorFromDict(idOfItem) as Item;
        GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(runeStone, -1);

        StringManager.SetTag(0, runeStone.displayName);
        GameLogScript.LogWriteStringRef("exp_log_runestone_gone");
        AbilityScript abilTemplate = GameMasterScript.masterAbilityList[abilRef];

        UIManagerScript.FlashWhite(0.9f);
        GameMasterScript.cameraScript.AddScreenshake(0.75f);
        GameMasterScript.heroPCActor.LearnAbility(abilTemplate, true, false, false, false);
        UIManagerScript.PlayCursorSound("Ultra Learn");

        GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_RuneLearned();

        return false;
    }

    public static bool GenerateAllCraftingRecipeTextForDialog(string value)
    {
        // value is a CraftingRecipeCategory enum.
        CraftingRecipeCategory categoryToCheck = (CraftingRecipeCategory)Enum.Parse(typeof(CraftingRecipeCategory), value);

        TextBranch recipeTB = UIManagerScript.currentTextBranch;

        StringBuilder sb = new StringBuilder();
        sb.Append(StringManager.GetString("exp_dialog_crafting_recipeintro") + "\n\n");

        List<CraftingRecipe> recipes = CraftingRecipeManager.GetCraftingRecipesByCategory(categoryToCheck);
        foreach(CraftingRecipe cr in recipes)
        {
            sb.Append(cr.displayIngredients);
            sb.Append("\n");
        }

        recipeTB.text = sb.ToString();

        return true;
    }

    public static bool OpenCraftingInterface(string value)
    {
        UIManagerScript.CloseDialogBox();
        CraftingScreen.OpenCraftingUI();
        return false;
    }

    public static bool SharaBoss4Victory_Part5(string value)
    {
        UIManagerScript.CloseDialogBox();

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss4Victory_Part5());

        return false;
    }

    public static bool SharaBoss4Victory_Part4(string value)
    {
        UIManagerScript.CloseDialogBox();

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss4Victory_Part4());

        return false;
    }

    public static bool SharaBoss4Victory_Part3(string value)
    {
        UIManagerScript.CloseDialogBox();

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss4Victory_Part3());

        return false;
    }

    public static bool SharaBoss4Victory_Part2(string value)
    {
        UIManagerScript.CloseDialogBox();

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss4Victory_Part2());

        return false;
    }

    public static bool SharaBeginBossFight4(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.SetAnimationPlaying(false);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);

        Monster boss = MapMasterScript.activeMap.FindActor("mon_shara_finalboss") as Monster;
        BossHealthBarScript.EnableBossWithAnimation(boss);

        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("finalboss_phase2");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "finalboss_phase2";

        GameMasterScript.heroPCActor.SetActorData("finalboss1", 1);

        RecenterCameraOnPlayer("");

        return false;
    }

    public static bool PlaySharaVillainTheme(string value)
    {
        MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("sharaserious");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "sharaserious";

        
        return true;
    }

    public static bool SharaBoss3Victory_Part2(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameEventsAndTriggers.singleton.StartCoroutine(GameEventsAndTriggers.SharaBoss3Victory_Part2());

        return false;
    }
    
    public static bool SharaBoss3Victory_PickupDominatorHead(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameEventsAndTriggers.singleton.StartCoroutine(GameEventsAndTriggers.SharaBoss3Victory_PickupDominatorHead());

        return false;
    }
    /// <summary>
    /// A small robot appears next to Shara and moves towards her...
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool SharaBoss3Victory_HelloBabyRobot(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameEventsAndTriggers.singleton.StartCoroutine(GameEventsAndTriggers.SharaBoss3Victory_HelloBabyRobot());

        return false;
    }

    /// <summary>
    /// Shara dominates the robot.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool SharaBoss3Victory_DominatesRobot(string value)
    {
        UIManagerScript.CloseDialogBox();
        
        GameEventsAndTriggers.singleton.StartCoroutine(GameEventsAndTriggers.SharaBoss3Victory_DominatesRobot());
        
        return false;
    }

    public static bool SharaBoss3FightBegin(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.SetAnimationPlaying(false);
        Actor mnBoss = MapMasterScript.activeMap.FindActor("mon_xp_heavygolem");

        BossHealthBarScript.EnableBossWithAnimation(mnBoss as Monster);

        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("sharamode_boss1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "sharamode_boss1"; // was bosstheme2

        ProgressTracker.SetProgress(TDProgress.BOSS3, ProgressLocations.HERO, 1);
        

        return false;
    }

    public static bool SharaPreBoss3Part2(string value)
    {
        UIManagerScript.CloseDialogBox();
        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss3Intro_Part2());
        return false;
    }

    public static bool SharaPreBoss3Part3(string value)
    {
        UIManagerScript.CloseDialogBox();
        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss3Intro_Part3());
        return false;
    }

    public static bool SharaPreBoss3Part4(string value)
    {
        UIManagerScript.CloseDialogBox();
        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss3Intro_Part4());
        return false;
    }

    public static bool SharaConditionalOpenBoss2VictoryDialog(string value)
    {
        UIManagerScript.CloseDialogBox();
        if (GameMasterScript.gmsSingleton.ReadTempGameData("boss2scientistdefeated") == 1)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("boss2scientistdefeated", 0);
            // now continue with the rest of the scene & dialogue
            GameMasterScript.gmsSingleton.StartCoroutine(GameEventsAndTriggers.SharaBoss2Victory_Continued());
            return false;
        }
        if (ProgressTracker.CheckProgress(TDProgress.BOSS2, ProgressLocations.HERO) == 3)
        {
            UIManagerScript.StartConversationByRef("dialog_shara_boss2_victory", DialogType.KEYSTORY, null);
        }
        return false;
    }

    public static bool SharaBoss2_PreFight_Part1(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true, true);
        // Transform bandits into spirits..

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss2_PreFight_Part1Cutscene());

        return false;
    }

    public static bool SharaBoss2_PreFight_Part2(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.SetAnimationPlaying(true);

        ProgressTracker.SetProgress(TDProgress.BOSS2, ProgressLocations.HERO, 2);

        GameMasterScript gmsSingleton = GameMasterScript.gmsSingleton;
        Actor db = MapMasterScript.activeMap.FindActor("mon_scientist_summoner");
        Monster bossMon = db as Monster;

        Vector2 badHardcodedHeroPosition = new Vector2(10f, 6f);
        Vector2 cPos = new Vector2(GameMasterScript.cameraScript.gameObject.transform.position.x, GameMasterScript.cameraScript.gameObject.transform.position.y + 1f);

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.SharaBoss2_BanditHelpArrives());
        return false;
    }

    public static bool SharaBoss2_EnableFight(string value)
    {
        UIManagerScript.CloseDialogBox();                
        BossHealthBarScript.EnableBossWithAnimation(MapMasterScript.activeMap.FindActor("mon_scientist_summoner") as Monster);
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        return false;
    }

    public static bool SetupMealForShara(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.heroPCActor.SetActorData("sharaboss1", 2);

        UIManagerScript.FadeOut(1.0f);

        Cutscenes.singleton.StartCoroutine(DLCCutscenes.WaitThenCreateBoss1SafeHavenForShara(1.25f));

        return false;
    }

    public static bool SharaPrepareForFirstBossFight(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.SetAnimationPlaying(false);

        GameMasterScript.cameraScript.SetCustomCameraAnimation(new Vector2(8f, 11f), GameMasterScript.heroPCActor.GetPos(), 0.75f);

        //BossHealthBarScript.EnableBossWithAnimation(dirtbeak);
        MusicManagerScript.RequestPlayLoopingMusicFromScratchWithCrossfade("bosstheme1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme1";

        ProgressTracker.SetProgress(TDProgress.BOSS1, ProgressLocations.HERO, 2);

        return false;
    }


    public static bool FadeAndRemoveDirtbeakFromMap(string value)
    {
        NPC dirtbeak = MapMasterScript.activeMap.FindActor("npc_friendly_dirtbeak") as NPC;
        if (dirtbeak != null)
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(dirtbeak.GetPos(), dirtbeak);
            MapMasterScript.activeMap.RemoveActorFromMap(dirtbeak);
            UIManagerScript.singletonUIMS.FadeInAndOut(1f);
            dirtbeak.myMovable.WaitThenFadeOut(0.5f, 0.05f);
        }
        Map hideout = MapMasterScript.theDungeon.FindFloor(MapMasterScript.PREBOSS1_MAP_FLOOR);
        hideout.SetMapVisibility(true);
        foreach(Stairs st in MapMasterScript.activeMap.mapStairs)
        {
            if (st.NewLocation == hideout)
            {
                st.EnableActor();
            }
        }
        return true;
    }

    public static bool StartSharaTutorialDialog(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.SetTempGameData("nomapfade", 0);

        if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_exp_sharamode"))
        {
            Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_exp_sharamode");
            UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
        }

        return false;
    }

    public static bool SharaIntroScene2Continue(string value)
    {
        Conversation scene3 = GameMasterScript.FindConversation("dialog_shara_intro_scene3");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(scene3, DialogType.KEYSTORY, null, 5f));
        return true;
    }

    public static bool SharaIntroScene3Continue(string value)
    {
        GameMasterScript.SetAnimationPlaying(true, true);
        MusicManagerScript.singleton.Fadeout(3f);
        Map cedar1 = MapMasterScript.theDungeon.FindFloor(MapMasterScript.SHARA_START_CAMPFIRE_FLOOR);

        //Don't fade back in after the map switch
        GameMasterScript.gmsSingleton.SetTempGameData("nomapfade", 1);
        TravelManager.singleton.StartCoroutine(TravelManager.singleton.WaitThenFadeThenSwitchMaps(1f, 4f, cedar1, null, false, false, null, false));
        GameMasterScript.heroPCActor.SetActorData("tutorial_finished", 1);
        GameMasterScript.heroPCActor.SetActorData("enteredtutorial", 1);
        return true;
    }

    // Mirai jumps from the bridge in town into the water, taking her to Riverstone Waterways
    public static bool JumpIntoRiverstoneRiver(string value)
    {
        GameMasterScript.heroPCActor.SetActorData("entereddungeon", 1);
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true);

        UIManagerScript.PlayCursorSound("AltJump");

        Actor riverSparkles = MapMasterScript.activeMap.FindActor("npc_jumpintoriver");

        Vector2 destinationpos = riverSparkles.GetPos() + new Vector2(0f, -4f);

        GameObject dummyObject = GameMasterScript.TDInstantiate("TransparentStairs");

        dummyObject.transform.position = destinationpos;

        GameMasterScript.tossProjectileDummy.projectileTossHeight = 3f;
        GameMasterScript.tossProjectileDummy.animLength = 0.9f;

        CombatManagerScript.FireProjectile(
            GameMasterScript.heroPCActor.GetPos(),
            destinationpos,
            GameMasterScript.heroPCActor.GetObject(),
            0.95f, 
            false, 
            dummyObject,
            MovementTypes.TOSS, 
            GameMasterScript.tossProjectileDummy, 
            360f, 
            false
            );

        GameMasterScript.tossProjectileDummy.projectileTossHeight = 1.2f;
        GameMasterScript.tossProjectileDummy.animLength = 0.25f;
        
        GameMasterScript.gmsSingleton.WaitThenDestroyObject(dummyObject, 0.5f);

        CombatManagerScript.WaitThenGenerateSpecificEffect(destinationpos, "WaterExplosion", null, 1f, true);
        Map riverstoneWay = MapMasterScript.theDungeon.FindFloor(MapMasterScript.RIVERSTONE_WATERWAY_START);
        GameMasterScript.SetLevelChangeState(true);

        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenStopAnimation(1f));

        TravelManager.singleton.StartCoroutine(TravelManager.singleton.WaitThenFadeThenSwitchMaps(0.7f, GameMasterScript.gmsSingleton.levelTransitionTime + 0.5f, riverstoneWay, null, false, false));
        return true;
    }
}
