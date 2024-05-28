using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
using UnityEngine.Analytics;
#endif

public partial class DialogEventsScript
{
    public static bool UnlockGambler(string value)
    {
        UIManagerScript.CloseDialogBox();

        SharedBank.UnlockJob(CharacterJobs.GAMBLER);

        return false;
    }

    public static bool UpdateCurrentJP(string value)
    {
        GameMasterScript.gmsSingleton.SetTempStringData("curjp", ((int)GameMasterScript.heroPCActor.GetCurJP()).ToString());
        return true;
    }

    public static bool GenericShowItemSpriteFromData(string value)
    {
        // we have to wait a frame because the dialog box isn't open yet.
        GameMasterScript.gmsSingleton.StartCoroutine(DialogEventsScript.WaitThenGenericShowItemSpriteFromData());
        return true;
    }

    static IEnumerator WaitThenGenericShowItemSpriteFromData()
    {
        yield return null; 
        string spriteRef = GameMasterScript.gmsSingleton.ReadTempStringData("itemsprite_for_dialog");
        Sprite sprToShow = UIManagerScript.GetItemSprite(spriteRef);
        UIManagerScript.ShowDialogBoxImage(sprToShow, 2f);
    }

    public static bool ConfirmCancelClawMastery(string value)
    {
        UIManagerScript.CloseDialogBox();

        bool silent = GameMasterScript.gmsSingleton.ReadTempGameData("weaponswitch_silent") == 1 ? true : false;
        bool playSFX = GameMasterScript.gmsSingleton.ReadTempGameData("weaponswitch_playsfx") == 1 ? true : false;

        UIManagerScript.SwitchActiveWeaponSlot(GameMasterScript.gmsSingleton.ReadTempGameData("weaponswitch_slot"),
            silent,
            //GameMasterScript.gmsSingleton.ReadTempGameData("weaponswitch_origslot"),
            UIManagerScript.GetActiveWeaponSlot(),
            playSFX
            );

        GameMasterScript.gmsSingleton.SetTempGameData("clawfrenzy_cancel", 0);

        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("status_clawfrenzy");
        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("status_immune_paralyze_temp");
        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("status_sealed_noimmune");

        return false;
    }

    public static bool ToggleSpellshapeFromDialog(string value)
    {
        // passed in value is the ref of the player's spellshape to toggle on/off
        AbilityScript spellshape = GameMasterScript.heroPCActor.myAbilities.GetAbilityByRef(value);
        spellshape.toggled = !spellshape.toggled;

        AddStatusEffect addStatus = spellshape.GetFirstEffectOfType(EffectType.ADDSTATUS) as AddStatusEffect;       

        foreach(ButtonCombo bc in UIManagerScript.currentTextBranch.responses)
        {
            // find the BC whose value matches this
            if (bc.dialogEventScriptValue == value)
            {
                if (spellshape.toggled)
                {
                    bc.toggled = true;
                    UIManagerScript.PlayCursorSound("UITick");

                    // Avoid conflicts with existing spellshapes by disabling them as needed.
                    foreach (AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
                    {
                        if (!abil.CheckAbilityTag(AbilityTags.SPELLSHAPE)) continue;
                        if (abil.refName == value) continue; // can't conflict with self
                        if (abil.exclusionGroup == spellshape.exclusionGroup && abil.toggled)
                        {
                            // gotta un-toggle this one, it conflicts with the one we just toggled.
                            abil.toggled = false;
                            AddStatusEffect oldAddStatus = abil.GetFirstEffectOfType(EffectType.ADDSTATUS) as AddStatusEffect;
                            GameMasterScript.heroPCActor.myStats.RemoveStatusByRef(oldAddStatus.statusRef);

                            // untoggle the button for that spellshape too
                            foreach (ButtonCombo abc in UIManagerScript.currentTextBranch.responses)
                            {
                                if (abc.dialogEventScriptValue == abil.refName)
                                {
                                    abc.toggled = false; 
                                }
                            }

                            break; // We would only ever have one conflict, as spellshapes are enabled one-by-one
                        }
                    }

                    // Now just add the status to hero
                    StatusEffect newShape = GameMasterScript.heroPCActor.myStats.AddStatusByRef(addStatus.statusRef, GameMasterScript.heroPCActor, 99);
                    newShape.addedByActorID = GameMasterScript.heroPCActor.actorUniqueID;
                    newShape.addedByAbilityRef = value;
                }
                else
                {
                    bc.toggled = false;
                    GameMasterScript.heroPCActor.myStats.RemoveStatusByRef(addStatus.statusRef);
                    UIManagerScript.PlayCursorSound("UITock");
                }

                // Now actually redraw the text.
                RefreshSpellshapeDialog("dummy");
                UIManagerScript.RefreshStatuses();
                return false;
            }
        }

        return true;
    }

    // Redraw buttons so that toggle state is correct
    public static bool RefreshSpellshapeDialog(string value)
    {
        foreach(UIManagerScript.UIObject obj in UIManagerScript.dialogUIObjects)
        {
            DialogButtonScript dbs = obj.gameObj.GetComponentInChildren<DialogButtonScript>();
            if (dbs == null) continue;
            if (obj.button.dbr == DialogButtonResponse.EXIT) continue;

            // Strip tags and colors first            
            obj.button.headerText = obj.button.headerText.Replace(UIManagerScript.greenHexColor, String.Empty);
            obj.button.headerText = obj.button.headerText.Replace("</color>", String.Empty);
            obj.button.buttonText = obj.button.buttonText.Replace("* ", String.Empty);

            // Then use colors/asterisks as needed
            if (obj.button.toggled)
            {
                obj.button.headerText = UIManagerScript.greenHexColor + obj.button.headerText + "</color>";
                obj.button.buttonText = "* " + obj.button.buttonText;
            }

            // Now update the TMPro
            dbs.bodyText.text = obj.button.buttonText;
            dbs.headerText.text = obj.button.headerText;
        }

        return true;
    }

    public static bool ConfirmPainterQuestDataIsCorrect(string value)
    {
        if (GameMasterScript.heroPCActor.ReadActorData("painterquest") == 1)
        {
            // We should have a destination floor. If we do not, then we have Problems
            if (GameMasterScript.heroPCActor.ReadActorData("painterquestfloor") == -1 || GameMasterScript.heroPCActor.ReadActorData("painterquestnearby") == -1)
            {
                // Problems detected - maybe we exited the Painter conversation too early earlier
                // Let's reroute to the part of the conversation that determines destination / nearby
                Debug.Log("Problems detected...");
                GameMasterScript.heroPCActor.RemoveActorData("painterquest");
                UIManagerScript.SwitchConversationBranch(UIManagerScript.currentConversation.FindBranch("painterselectmap"));
                UIManagerScript.UpdateDialogBox();
            }
        }

        return true;
    }

    public static bool AllowCameraToMove(string value)
    {
        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        RecenterCameraOnPlayer("");

        return true;
    }

    // Asks a pet to start combat with a monster 
    public static bool PetInitiateCombatTargeting(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.SetAbilityToTry(GameMasterScript.petAttackAbilityDummy);
        UIManagerScript.singletonUIMS.EnterTargeting(GameMasterScript.petAttackAbilityDummy, Directions.NEUTRAL);
        TDInputHandler.targetClicksMax = 1;
        TDInputHandler.targetClicksRemaining = 1;

        return false;
    }

    public static bool PlayMusicByRef(string value)
    {
        // value is the name of the tune

		MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade(value);


        return true;
    }

    public static bool TogglePetAttackBehavior(string value)
    {
        int petID = GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo");
        Monster thePet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(petID) as Monster;
        if (thePet != null)
        {
            int currentAttackValue = thePet.ReadActorData("pet_no_attack");
            if (currentAttackValue != 1)
            {
                thePet.SetActorData("pet_no_attack", 1);
                StringManager.SetTag(1, StringManager.GetString("pet_behavior_attack_no"));
            }
            else
            {
                thePet.SetActorData("pet_no_attack", 0);
                StringManager.SetTag(1, StringManager.GetString("pet_behavior_attack_yes"));
            }

            StringManager.SetTag(0, thePet.displayName);
            GameLogScript.LogWriteStringRef("log_pet_command");
            UIManagerScript.PlayCursorSound("Select");
        }

        UIManagerScript.CloseDialogBox();
        return false;
    }

    public static bool PlayRobotTalkSFX(string value)
    {
        UIManagerScript.PlayCursorSound("SupervisorSound");
        return true;
    }
    
    public static bool PlayBabyRobotTalkSFX(string value)
    {
        MusicManagerScript.PlayCutsceneSound("BabySentry");
        return true;
    }
    

    public static bool FadeOutMusic(string value)
    {
        MusicManagerScript.singleton.Fadeout(0.5f);

        return true;
    }

    public static bool QuitApplication(string value)
    {
        UIManagerScript.CloseDialogBox();
        Application.Quit();

        return false;
    }

    // used after the initial tech cube pickup cutscene post-dirtbeak
    public static bool RemoveTechCubeFromMap(string value)
    {
        UIManagerScript.CloseDialogBox();

        Actor findCube = MapMasterScript.activeMap.FindActor("obj_techcube");

        if (findCube != null)
        {
        findCube.myMovable.AnimateSetPositionNoChange(GameMasterScript.heroPCActor.GetPos(), 1.0f, false, 0f, 0f, MovementTypes.LERP);
        findCube.myMovable.FadeOutThenDie();
            MapMasterScript.activeMap.RemoveActorFromMap(findCube);
        }
        UIManagerScript.PlayCursorSound("Schwing");

        RecenterCameraOnPlayer("");

        return false;
    }

    public static bool StartTechCubePickupSequence(string value)
    {
        Cutscenes.singleton.StartCoroutine(Cutscenes.singleton.PickUpTechCubeFromDirtbeak());

        return false;
    }

    public static bool TechCubeRestoreMachine(string value)
    {
        UIManagerScript.CloseDialogBox();

        Cutscenes.singleton.StartCoroutine(Cutscenes.singleton.TechCubeRestoreSequence());

        return false;
    }

    public static bool FillOutOptionalDreamResultStrings(string value)
    {
        string builderForTag6 = "";
        string modGained = GameMasterScript.heroPCActor.ReadActorDataString("dreamitem_modgained");
        if (!string.IsNullOrEmpty(modGained))
        {
            StringManager.SetTag(0, modGained);
            builderForTag6 = StringManager.GetString("misc_dream_mod_added") + "\n";
        }

        string newItemName = GameMasterScript.heroPCActor.ReadActorDataString("dreamitem_itemname");
        if (!string.IsNullOrEmpty(newItemName))
        {
            StringManager.SetTag(0, newItemName);
            builderForTag6 += StringManager.GetString("misc_dream_power_up") + "\n";
        }

        if (builderForTag6 != "")
        {
            builderForTag6 += "\n";
        }

        StringManager.SetTag(5, builderForTag6);
        return true;
    }

    public static bool DisplayDefeatDataInDialogBox(string value)
    {
        int indexOfDefeatData = 0;
        Int32.TryParse(value, out indexOfDefeatData);
        if (indexOfDefeatData >= MetaProgressScript.defeatHistory.Count)
        {
            if (Debug.isDebugBuild) Debug.Log("WARNING: Tried to access defeat data index " + value + " which exceeded max.");
            return true;
        }
        DefeatData displayDD = MetaProgressScript.defeatHistory[indexOfDefeatData];

        TextMeshProUGUI dialogText = UIManagerScript.myDialogBoxComponent.GetDialogText();

        dialogText.color = UIManagerScript.transparentColor;
        dialogText.text = displayDD.GetPrintableString();

        if (Debug.isDebugBuild) Debug.Log(dialogText.text);

        UIManagerScript.myDialogBoxComponent.WaitThenMakeTextWhite(0.05f);
        return true;
    }

    // For dialogs with more than the max number of possible responses, this goes to the next batch of responses or previous
    public static bool ChangeDialogPages(string value)
    {
        if (value == "next")
        {
            UIManagerScript.ChangeDialogResponsePages(nextPage:true);
        }
        else
        {
            UIManagerScript.ChangeDialogResponsePages(nextPage:false);
        }
        return false;
    }

    public static bool ConfirmTravelMapsFromCampfire(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.gmsSingleton.SetTempGameData("confirm_campfirestairs", 1);
        TravelManager.TryTravelStairs();
        return false;
    }

    public static bool RemovePetTrainerFromMap(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.FadeOut(0.75f);
        Actor petTrainer = MapMasterScript.activeMap.FindActor("npc_pettrainer");
        MapMasterScript.activeMap.RemoveActorFromMap(petTrainer);
        MapMasterScript.activeMap.RemoveActorFromLocation(petTrainer.GetPos(), petTrainer);
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenReturnObjectToStack(petTrainer.GetObject(), petTrainer.prefab, 0.75f));
        UIManagerScript.singletonUIMS.WaitThenFadeIn(0.75f, 0.75f, true);
        return true;
    }

    public static bool TeachPetRandomSkillAndContinueTrainerQuest(string value)
    {
        UIManagerScript.CloseDialogBox();
        GameMasterScript.SetAnimationPlaying(true, true);
        GameMasterScript.cameraScript.AddScreenshake(0.3f);
        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
        pet.myAnimatable.SetAnim("TakeDamage");
        CombatManagerScript.GenerateSpecificEffectAnimation(pet.GetPos(), "AggroEffect", null);

        StringManager.SetTag(4, pet.displayName);
        GameMasterScript.gmsSingleton.SetTempStringData("playerpetname", pet.displayName);

        Conversation continueRef = GameMasterScript.FindConversation("dialog_pettrainer_part2");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(continueRef, DialogType.STANDARD, null, 0.75f));
        CombatManagerScript.WaitThenGenerateSpecificEffect(pet.GetPos(), "CharmEffectSystem", null, 0.25f);
        GameMasterScript.gmsSingleton.SetTempGameData("ignore_pet_tech_jpcost",1);
        TeachPlayerPetSkill("skill_mon_counterattack");
        MetaProgressScript.AddMetaProgress("pet_trainer_quest", 1);
        return true;
    }

    // passed in value is the skill ref.
    public static bool TeachPlayerPetSkill(string value)
    {
        NPC trainer = UIManagerScript.currentConversation.whichNPC;
        UIManagerScript.CloseDialogBox();

        float healthThreshold = 1.0f;
        if (value == "skill_mon_desperatestrike")
        {
            healthThreshold = 0.33f;
        }

        AbilityScript template = AbilityScript.GetAbilityByName(value);

        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();

        int cost = MonsterCorralScript.CalculateSkillJPCost(pet, template);

        if (GameMasterScript.gmsSingleton.ReadTempGameData("ignore_pet_tech_jpcost") == 1)
        {
            cost = 0;
            GameMasterScript.gmsSingleton.SetTempGameData("ignore_pet_tech_jpcost", 0);
        }

        StringManager.SetTag(4, trainer.displayName);

        if (cost > GameMasterScript.heroPCActor.GetCurJP())
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_pet_learnpower_notenoughjp", null);
            return false;
        }

        // If we've previously forgotten the skill, remove the evidence that we've forgotten it
        pet.abilitiesForgotten.Remove(value);

        GameMasterScript.heroPCActor.AddJP(cost * -1f);

        pet.LearnNewPower(value, healthThreshold, 1.0f, 0, 1);

        UIManagerScript.FlashWhite(0.5f);
        UIManagerScript.PlayCursorSound("Ultra Learn");
        CombatManagerScript.GenerateSpecificEffectAnimation(pet.GetPos(), "CharmEffectSystem", null);

        StringManager.SetTag(5, pet.displayName);
        StringManager.SetTag(6, template.abilityName);

        GameLogScript.LogWriteStringRef("log_pet_learn_newpower", null);

        return false;
    }


    // passed in value is the skill ref.
    public static bool ForgetPlayerPetSkill(string value)
    {
        GameMasterScript.gmsSingleton.StartCoroutine(CutsceneForgetPlayerPetSkill(value));

        return false;
    }

    public static IEnumerator CutsceneForgetPlayerPetSkill(string abilRef)
    {        
        NPC trainer = UIManagerScript.currentConversation.whichNPC;
        UIManagerScript.CloseDialogBox();
        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();

        AbilityScript toForget = pet.myAbilities.GetAbilityByRef(abilRef);

        int cost = MonsterCorralScript.CalculateForgetSkillJPCost(pet, toForget);

        StringManager.SetTag(0, pet.displayName);
        StringManager.SetTag(1, trainer.displayName);

        if (cost > GameMasterScript.heroPCActor.GetCurJP())
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_pet_forgetpower_notenoughjp", null);
            yield break;
        }

        GameMasterScript.SetAnimationPlaying(true, true);

        CombatManagerScript.SpawnChildSprite("AggroEffect", pet, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(0.5f);

        CombatManagerScript.SpawnChildSprite("AggroEffect", pet, Directions.NORTHEAST, false);

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.heroPCActor.AddJP(cost * -1f);

        pet.RemoveMonsterPowerByAbilityRef(toForget.refName);

        if (!pet.abilitiesForgotten.Contains(toForget.refName))
        {
            pet.abilitiesForgotten.Add(toForget.refName);
        }        

        //UIManagerScript.FlashWhite(0.5f);
        GameMasterScript.cameraScript.AddScreenshake(0.5f);
        //UIManagerScript.PlayCursorSound("Ultra Learn");
        CombatManagerScript.GenerateSpecificEffectAnimation(pet.GetPos(), "FervirBonkEffect", null);

        pet.myAnimatable.SetAnim("TakeDamage");

        StringManager.SetTag(0, pet.displayName);
        StringManager.SetTag(2, toForget.abilityName);

        GameLogScript.LogWriteStringRef("log_pet_forgetpower", null);

        yield return new WaitForSeconds(0.5f);

        GameMasterScript.SetAnimationPlaying(false);
    }

    // Used in the Pet Trainer NPC conversation to build a list of known pet skills vs. teachable ones.
    public static bool UpdateTeachablePetSkills(string value)
    {
        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();

        if (pet == null)
        {
            return true;
        }

        TextBranch teachBranch = UIManagerScript.currentConversation.FindBranch("teachpetskills");
        TextBranch forgetBranch = UIManagerScript.currentConversation.FindBranch("forgetpetskills");

        // Rebuild all the responses since we don't know what skills the CURRENT player pet has. Maybe all, maybe none.
        teachBranch.responses.Clear();

        // This branch will track what skills we know so we can forget them.
        forgetBranch.responses.Clear();

        List<string> teachableSkills = new List<string>()
        {
            "skill_mon_taunt",
            "skill_mon_fasthealing",
            "skill_mon_counterattack",
            "skill_mon_desperatestrike"
        };

        foreach(AbilityScript abil in pet.myAbilities.GetAbilityList())
        {
            int cost = MonsterCorralScript.CalculateForgetSkillJPCost(pet, abil); 
            StringManager.SetTag(0, abil.abilityName);
            StringManager.SetTag(1, cost.ToString());
            ButtonCombo bc = new ButtonCombo();
            bc.buttonText = StringManager.GetString("forget_petskill_buttontext");
            bc.actionRef = "continue";
            bc.dialogEventScript = "ForgetPlayerPetSkill";
            bc.dialogEventScriptValue = abil.refName;
            bc.dbr = DialogButtonResponse.CONTINUE;
            forgetBranch.responses.Add(bc);
        }

        teachableSkills.RemoveAll(a => pet.myAbilities.HasAbilityRef(a));

        foreach(string str in teachableSkills)
        {
            AbilityScript template = AbilityScript.GetAbilityByName(str);
            int cost = MonsterCorralScript.CalculateSkillJPCost(pet, template);
            ButtonCombo bc = new ButtonCombo();
            bc.buttonText = "<color=yellow>" + template.abilityName + "</color>: " + template.description + " (" + UIManagerScript.greenHexColor + cost + " " + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.JP) + "</color>)";
            bc.actionRef = "continue";
            bc.dialogEventScript = "TeachPlayerPetSkill";
            bc.dialogEventScriptValue = template.refName;
            bc.dbr = DialogButtonResponse.CONTINUE;
            teachBranch.responses.Add(bc);
        }

        if (teachableSkills.Count == 0)
        {
            teachBranch.text = StringManager.GetString("dialog_pettrainer_petknowsall");
        }
        else
        {
            teachBranch.text = "";
        }

        if (pet.myAbilities.GetAbilityList().Count == 0)
        {
            forgetBranch.text = StringManager.GetString("dialog_pettrainer_petnopowers");
        }
        else
        {
            forgetBranch.text = StringManager.GetString("dialog_pettrainer_forgetpower_desc");
        }

        ButtonCombo exit = new ButtonCombo();
        exit.buttonText = StringManager.GetString("dialog_banker_town_bullion_btn_6");
        exit.actionRef = "exit";
        exit.dbr = DialogButtonResponse.EXIT;
        teachBranch.responses.Add(exit);

        forgetBranch.responses.Insert(0, exit);

        return true;
    }

    // If the player confirms they WANT to break the Ice Block that will definitely kill them
    // Then execute that buffered turn! Let 'er rip!
    public static bool ConfirmBreakDestructible(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.SetTempGameData("dt_confirm_destroy", 1);
        GameMasterScript.gmsSingleton.TryNextTurn(GameMasterScript.bufferedTurnData, true);

        return false;
    }

    public static bool LearnSharaPowerFromDialog(string value)
    {
        AbilityScript template = GameMasterScript.masterAbilityList[value];
        GameMasterScript.heroPCActor.LearnAbility(template, true, true, true);
        UIManagerScript.FlashWhite(0.6f);
        if (template.CheckAbilityTag(AbilityTags.SHARAPOWER))
        {
            GameMasterScript.cameraScript.AddScreenshake(0.45f);
        }

        UIManagerScript.PlayCursorSound("Ultra Learn");

        if (GameMasterScript.heroPCActor.ReadActorData("sharapowerlearned") == 0)
        {
            UIManagerScript.StartConversationByRef("exp_sharapower_learned", DialogType.STANDARD, null);
            GameMasterScript.heroPCActor.SetActorData("sharapowerlearned", 1);
        }

        UIManagerScript.CloseDialogBox();
        return false;
    }

    public static bool TrySellFavoritedItem(string value)
    {
        UIManagerScript.CloseDialogBox();

        Actor getItem = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("sellitem"));

        if (getItem == null)
        {
            Debug.Log("Bad error - couldn't retrieve item player confirmed.");
            return false;
        }

        GameMasterScript.gmsSingleton.SetTempGameData("sellitem", -1);

        ShopUIScript.SellItem(getItem as Item, 1);
        UIManagerScript.PlayCursorSound("Buy Item");

        ShopUIScript.ReopenShop();

        return true;
    }

    public static bool CloseItemDream(string value)
    {
        UIManagerScript.CloseDialogBox();

        UIManagerScript.PlayCursorSound("EnterItemWorld");
        TravelManager.ExitItemDream(withItem:true);

        return true;
    }

    public static bool TryDestroyEquippedJobEmblem(string value)
    {
        UIManagerScript.CloseDialogBox();

        Emblem currentEmblem = GameMasterScript.heroPCActor.myEquipment.GetEmblem();
        if (currentEmblem == null)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_error_noemblem");
            return true;
        }

        GameMasterScript.heroPCActor.myEquipment.UnequipByReference(currentEmblem);
        GameMasterScript.heroPCActor.myInventory.RemoveItem(currentEmblem);
        UIManagerScript.PlayCursorSound("Ice Shatter");
        GameLogScript.LogWriteStringRef("log_jorito_destroyemblem");
        GameMasterScript.cameraScript.AddScreenshake(0.3f);

        return true;
    }

    public static bool HerbalistExplosion(string value)
    {
        NPC herbalist = UIManagerScript.currentConversation.whichNPC;
        UIManagerScript.CloseDialogBox();

        CombatManagerScript.SpawnChildSprite("AggroEffect", herbalist, Directions.NORTHEAST, false);
        CombatManagerScript.SpawnChildSprite("AggroEffect", GameMasterScript.heroPCActor, Directions.NORTHEAST, false);

        GameMasterScript.SetAnimationPlaying(true);
        CombatManagerScript.WaitThenGenerateSpecificEffect(herbalist.GetPos(), "BigExplosionEffect", null, 0.8f, true);

        Cutscenes.singleton.StartCoroutine(Cutscenes.singleton.WaitThenHerbalistPart2(herbalist, 0.8f));
        return false;
    }

    public static bool BoostStatViaLevelupBonus(string value)
    {
        UIManagerScript.CloseDialogBox();
        int vParse = Int32.Parse(value);
        StatTypes stat = StatTypes.STAMINA;
        switch (vParse)
        {
            case 0:
                stat = StatTypes.STRENGTH;
                break;
            case 1:
                stat = StatTypes.SWIFTNESS;
                break;
            case 2:
                stat = StatTypes.SPIRIT;
                break;
            case 3:
                stat = StatTypes.DISCIPLINE;
                break;
            case 4:
                stat = StatTypes.GUILE;
                break;
        }

        GameMasterScript.heroPCActor.myStats.ChangeStat(stat, 3f, StatDataTypes.ALL, true);

        UIManagerScript.PlayCursorSound("Heavy Learn");

        GameMasterScript.heroPCActor.AddActorData("levelup_bonuses_left", -1);

        int amount = GameMasterScript.heroPCActor.ReadActorData("levelup_bonuses_left");
        if (amount > 0)
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.ReadActorData("levelup_bonuses_left").ToString());
            UIManagerScript.StartConversationByRef("levelupstats_redistribute", DialogType.LEVELUP, null);
        }
        return false;
    }

    public static bool PercyResetStats(string value)
    {
        UIManagerScript.CloseDialogBox();

        bool hasOrb = false;

        Item orbToRemove = null;

        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (itm.actorRefName != "orb_itemworld") continue;
            if (itm.IsLucidOrb() || itm.IsJobSkillOrb()) continue;
            hasOrb = true;
            orbToRemove = itm;
            break;
        }        

        if (!hasOrb)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_percy_no_orb");
            return false;
        }

        if (GameMasterScript.heroPCActor.myStats.GetLevel() == 1)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_percy_level1");
            return false;
        }

        GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(orbToRemove, -1);

        GameMasterScript.heroPCActor.myStats.ResetStatBonusesFromLevelUps();
        GameLogScript.GameLogWrite(StringManager.GetString("percy_mutters"), GameMasterScript.heroPCActor);
        if (PlayerOptions.screenFlashes)
        {
            UIManagerScript.FlashWhite(0.3f);
        }

        StringManager.SetTag(0, GameMasterScript.heroPCActor.ReadActorData("levelup_bonuses_left").ToString());
        UIManagerScript.StartConversationByRef("levelupstats_redistribute", DialogType.LEVELUP, null);

        return false;
    }
    public static bool TryFastTravelMenu(string value)
    {
        UIManagerScript.CloseDialogBox();

        TravelManager.BeginFastTravelDialog();
        return false;
    }

    public static bool ConfirmDrinkFlask(string value)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("flask_confirm_fail", 1);
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.regenFlaskAbility);

        return false;
    }

    public static bool ConfirmChangeGear(string value)
    {
        GameMasterScript.gmsSingleton.SetTempGameData("equip_confirm_fail", 1);
        UIManagerScript.CloseDialogBox();

        GameLogScript.LogWriteStringRef("log_try_equip_gear");

        //GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.regenFlaskAbility);

        return false;
    }

    public static bool ConfirmHitCharmedEnemy(string value)
    {
        int mID = GameMasterScript.gmsSingleton.ReadTempGameData("hitfriendly");

        UIManagerScript.CloseDialogBox();

        Monster getMon = GameMasterScript.gmsSingleton.TryLinkActorFromDict(mID) as Monster;
        if (getMon != null)
        {
            bool alreadyEnemy = getMon.actorfaction == Faction.ENEMY;

            CombatManagerScript.SpawnChildSprite("AggroEffect", getMon, Directions.NORTHEAST, false);
            getMon.myAnimatable.SetAnim("TakeDamage");
            getMon.actorfaction = Faction.ENEMY;
            getMon.bufferedFaction = Faction.ENEMY;
            getMon.myStats.ForciblyRemoveStatus("status_permacharmed");
            getMon.myStats.ForciblyRemoveStatus("charmvisual");
            //GameLogScript.LogWriteStringRef("log_mon_turnedhostile");

            // This may have already been a hostile enemy, in which case, gain aggro.
            if (alreadyEnemy)
            {
                foreach(Actor act in GameMasterScript.heroPCActor.summonedActors)
                {
                    if (act.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster mPet = act as Monster;
                        if (mPet.myStats.IsAlive())
                        {
                            mPet.AddAggro(getMon, 10f);
                            getMon.AddAggro(mPet, 10f);
                        }
                    }
                }
            }
        }

        return false;

    }

    public static bool ReturnPetToCorralThenTryJobTrial(string value)
    {
        UIManagerScript.CloseDialogBox();

        MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();
        PetPartyUIScript.RefreshContentsOfPlayerParty();

        StartJobTrial("dummy");

        return false;
    }

    //EnterJobTrial Enter Job Trial
    public static bool StartJobTrial(string value)
    {
        UIManagerScript.CloseDialogBox();

        if (!JobTrialScript.HasPlayerUnequippedNonJobAbilities())
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_error_jobtrial_passives");
            return false;
        }

        int curLevel = GameMasterScript.heroPCActor.ReadActorData("currentemblem_level");

        curLevel++;

        if (curLevel <= 0) curLevel = 0;

        if (curLevel > 2)
        {
            curLevel = 2;
        }

        float jpCost = (float)JobTrialScript.TRIAL_COSTS[curLevel];

        if (jpCost > GameMasterScript.heroPCActor.GetCurJP())
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            int diff = (int)(jpCost - GameMasterScript.heroPCActor.GetCurJP());
            StringManager.SetTag(0, diff.ToString());
            GameLogScript.LogWriteStringRef("log_error_jobtrial_jp");
            return false;
        }

        if (GameMasterScript.heroPCActor.GetMonsterPet() != null)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_error_jobtrial_pet");
            UIManagerScript.StartConversationByRef("dialog_confirm_trial_removepet", DialogType.STANDARD, null);
            return false;
        }
                GameMasterScript.heroPCActor.AddJP(-1f * jpCost);

        JobTrialScript.SetupJobTrial(curLevel);

        return false;
    }
    public static bool CheckEmblemStatusAtJorito(string value)
    {
        Emblem emblemForCurrentJob = null;

        // First check equipment for an emblem.
        Emblem currentEmblem = GameMasterScript.heroPCActor.myEquipment.GetEmblem();
        if (currentEmblem != null)
        {
            if (currentEmblem.jobForEmblem == GameMasterScript.heroPCActor.myJob.jobEnum)
            {
                emblemForCurrentJob = currentEmblem;
            }
        }

        if (emblemForCurrentJob == null)
        {
            foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
            {
                if (itm.itemType != ItemTypes.EMBLEM) continue;
                Emblem e = itm as Emblem;
                if (e.jobForEmblem == GameMasterScript.heroPCActor.myJob.jobEnum)
                {
                    emblemForCurrentJob = e;
                    break;
                }
            }
        }

        GameMasterScript.heroPCActor.RemoveActorData("currentemblem_id");
        GameMasterScript.heroPCActor.RemoveActorData("currentemblem_level");       

        //StringManager.SetTag(0, GameMasterScript.heroPCActor.myJob.displayName);

        GameMasterScript.gmsSingleton.SetTempStringData("playerjob", GameMasterScript.heroPCActor.myJob.DisplayName);

        if (emblemForCurrentJob == null)
        {
            // Do nothing
            StringManager.SetTag(1, JobTrialScript.TRIAL_COSTS[0].ToString());
            GameMasterScript.gmsSingleton.SetTempStringData("emblemjpcost", JobTrialScript.TRIAL_COSTS[0].ToString());
        }
        else
        {
            GameMasterScript.heroPCActor.SetActorData("currentemblem_id", emblemForCurrentJob.actorUniqueID);
            GameMasterScript.heroPCActor.SetActorData("currentemblem_level", emblemForCurrentJob.emblemLevel);

            int eTier = emblemForCurrentJob.emblemLevel + 1;
            int jpCost = emblemForCurrentJob.GetNextTrialCost();

            StringManager.SetTag(1, eTier.ToString());
            StringManager.SetTag(2, jpCost.ToString());

            GameMasterScript.gmsSingleton.SetTempStringData("emblemjpcost", jpCost.ToString());
            GameMasterScript.gmsSingleton.SetTempStringData("emblemtier", eTier.ToString());
        }

        return true;
    }

    public static bool RemoveHubRepairStatusTriggers(string value)
    {
        List<Actor> toRemove = new List<Actor>();
        foreach(Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.actorRefName == "trigger_finalhubdialog")
            {
                toRemove.Add(act);
            }
        }
        foreach(Actor act in toRemove)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(act);
        }
        return true;
    }

    public static bool PlayerUseEscapePortal(string value)
    {
        UIManagerScript.CloseDialogBox();

        GameMasterScript.gmsSingleton.SetTempGameData("confirm_jobtrial_useportal", 1);
        GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.escapeTorchAbility);        

        return true;
    }
    public static bool CheckForItemDreamDimensionalRift(string value)
    {
        string builtText = "";

        if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 13 && GameMasterScript.heroPCActor.ReadActorData("dimrift_found") <= 1 && GameMasterScript.heroPCActor.ReadActorData("beatdimrift") != 1)
        {
            if (GameMasterScript.heroPCActor.ReadActorData("dimrift_check_day") != MetaProgressScript.totalDaysPassed)
            {
                GameMasterScript.heroPCActor.SetActorData("dimrift_check_day", MetaProgressScript.totalDaysPassed);
                if (UnityEngine.Random.Range(0,1f) <= 0.05f) // chance of finding rift
                {
                    builtText += "\n\n" + StringManager.GetString("dialog_strange_itemdream") + "\n";
                    GameMasterScript.heroPCActor.SetActorData("dimrift_found", 1);
                }
            }
        }

        StringManager.SetTag(0, builtText);
        PlayRobotTalkSFX("");
        return true;
    }

    public static bool IncrementPetLettersOpened(string value)
    {
        GameMasterScript.gmsSingleton.statsAndAchievements.IncrementMonsterLettersRead();
        return true;
    }

    public static bool SatOnDirtbeakThrone(string value)
    {
        GameMasterScript.gmsSingleton.statsAndAchievements.SatOnDirtbeakThrone();
        return true;
    }

    public static bool SpawnDimRiftBossEncounter(string value)
    {
        UIManagerScript.CloseDialogBox();

        NPC scientist = MapMasterScript.activeMap.FindActor("npc_dimrift_madscientist") as NPC;
        if (scientist != null)
        {
            scientist.myMovable.FadeOutThenDie();
            MapMasterScript.activeMap.RemoveActorFromLocation(scientist.GetPos(), scientist);
            MapMasterScript.activeMap.RemoveActorFromMap(scientist);
        }

        //do build

        GameMasterScript.SetAnimationPlaying(true);

        MusicManagerScript.singleton.FadeoutThenSetAllToZero(0.33f);

        Cutscenes.singleton.StartCoroutine(Cutscenes.singleton.DimRiftBossEncounter_Part2());
        return false;
    }
    public static bool CheckForUpdateFoodCart(string value)
    {
        FoodCartScript.CheckForUpdateFoodCart();      
        return true;
    }

    public static bool PanCameraToPlayer(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(GameMasterScript.heroPCActor.GetPos(), 0.65f, 0.1f, true);

        return false;
    }

    public static bool SwitchPlacesWithConversationNPC(string value)
    {
        NPC currentConvoNPC = UIManagerScript.currentConversation.whichNPC;
        UIManagerScript.CloseDialogBox(true);

        if (currentConvoNPC == null)
        {
            return false;
        }

        Vector2 heroPos = GameMasterScript.heroPCActor.GetPos();
        Vector2 npcPos = currentConvoNPC.GetPos();

        //Debug.Log("At start of swap: hero is at " + GameMasterScript.heroPCActor.GetPos() + " and NPC is at " + npcPos);

        MapMasterScript.activeMap.RemoveActorFromLocation(currentConvoNPC.GetPos(), currentConvoNPC);
        MapMasterScript.activeMap.RemoveActorFromLocation(GameMasterScript.heroPCActor.GetPos(), GameMasterScript.heroPCActor);
        MapMasterScript.activeMap.AddActorToLocation(npcPos, GameMasterScript.heroPCActor);
        MapMasterScript.activeMap.AddActorToLocation(heroPos, currentConvoNPC);

        currentConvoNPC.myMovable.AnimateSetPosition(heroPos, 0.1f, false, 0f, 0f, MovementTypes.LERP);

        GameMasterScript.heroPCActor.myMovable.AnimateSetPosition(npcPos, 0.1f, false, 0f, 0f, MovementTypes.LERP);
        
        GameMasterScript.heroPCActor.SetCurPos(npcPos);
        currentConvoNPC.SetCurPos(heroPos);
        currentConvoNPC.SetSpawnPos(heroPos);

        TileInteractions.TryPickupItemsInHeroTile();

        //Debug.Log("At end of swap: hero is at " + GameMasterScript.heroPCActor.GetPos() + " and NPC is at " + currentConvoNPC.GetPos());
        return false;
    }

    public static bool OpenMonsterCorralInterface(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        MonsterCorralScript.OpenCorralInterface();
        return false;
    }

    public static bool ReverseLevelAndAwardResources(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.heroPCActor.SetRegenFlaskUses(10);
        int numItems = 6;

        List<string> possibleHealingItems = new List<string>();
        possibleHealingItems.Add("potion_healing1");
        possibleHealingItems.Add("potion_stamina1");
        possibleHealingItems.Add("potion_energy1");
        if (GameMasterScript.heroPCActor.myStats.GetLevel() >= 5)
        {
            possibleHealingItems.Add("potion_healing2");
            possibleHealingItems.Add("potion_stamina2");
            possibleHealingItems.Add("potion_energy2");
        }

        for (int i = 0; i < numItems; i++)
        {
            Item create = LootGeneratorScript.GenerateLootFromTable(1.5f, 0f, "food_and_meals");
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(create, true);
            create = LootGeneratorScript.GenerateLootFromTable(1.5f, 0f, "food");
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(create, true);
            create = LootGeneratorScript.CreateItemFromTemplateRef(possibleHealingItems[UnityEngine.Random.Range(0, possibleHealingItems.Count)], 1.5f, 0f, false);
            GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(create, true);
        }
        GameMasterScript.heroPCActor.myStats.ReverseLevelUp();
        GameLogScript.LogWriteStringRef("log_sacrifice_level_forgoods");

        DialogEventsScript.RecenterCameraOnPlayer("");

        GameMasterScript.heroPCActor.AddActorData("percy_timeshelped", 1);

        return false;
    }

    public static bool RemoveDirtbeakHintTiles(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        List<Actor> tilesToRemove = new List<Actor>();
        foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
        {
            if (act.GetActorType() != ActorTypes.DESTRUCTIBLE) continue;
            if (act.actorRefName == "dirtbeak_bottleneck_explainer")
            {
                tilesToRemove.Add(act);
            }
        }
        foreach (Actor act in tilesToRemove)
        {
            MapMasterScript.activeMap.RemoveActorFromLocation(act.GetPos(), act);
            MapMasterScript.activeMap.RemoveActorFromMap(act);
        }
        return false;
    }
    public static bool DoUnlockWildChild(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.FlashWhite(0.75f);
        SharedBank.UnlockJob(CharacterJobs.WILDCHILD);
        return false;
    }

    public static bool DrinkStrangePotion(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.FlashWhite(0.5f);
        UIManagerScript.PlayCursorSound("ShamanHeal");

        GameMasterScript.heroPCActor.SetActorData("strangepotion", 1);

        List<string> possibleStatuses = new List<string> { "strangepotion_power", "strangepotion_defense", "strangepotion_regen" };

        StatusEffect se = GameMasterScript.heroPCActor.myStats.AddStatusByRef(possibleStatuses[UnityEngine.Random.Range(0, possibleStatuses.Count)], GameMasterScript.heroPCActor, 99);
        if (se != null)
        {
            StringManager.SetTag(0, GameMasterScript.heroPCActor.displayName);
            StringManager.SetTag(1, se.abilityName);
            GameLogScript.LogWriteStringRef("log_gainstatus_single_withtag");
        }

        se = GameMasterScript.heroPCActor.myStats.AddStatusByRef("monsterattract", GameMasterScript.heroPCActor, 99);
        if (se != null)
        {
            StringManager.SetTag(1, se.abilityName);
            GameLogScript.LogWriteStringRef("log_gainstatus_single_withtag");
        }

        return false;
    }

    public static bool SpawnFriendshipForestBoss(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Monster sandjaw = MonsterManagerScript.CreateMonster("mon_mottledsandjaw", true, true, false, 0f, false);
        sandjaw.MakeChampion();

        MapTileData emptyTile = MapMasterScript.GetRandomEmptyTile(new Vector2(7f, 7f), 1, true, true);

        MapMasterScript.activeMap.PlaceActor(sandjaw, emptyTile);
        MapMasterScript.singletonMMS.SpawnMonster(sandjaw);

        Item petals = LootGeneratorScript.CreateItemFromTemplateRef("spice_rosepetals", 1.0f, 0f, false);

        sandjaw.myInventory.AddItemRemoveFromPrevCollection(petals, false);

        sandjaw.SetActorData("friendship_boss", 1);

        CombatManagerScript.GenerateSpecificEffectAnimation(sandjaw.GetPos(), "SoundEmanation", null, true);

        return false;
    }

    public static bool RevealMap(string value)
    {
        UIManagerScript.CloseDialogBox();

        int mapID = Int32.Parse(value);

        Map mapToReveal = MapMasterScript.theDungeon.FindFloor(mapID);

        if (mapToReveal == null) return false;

        int mapAreaID = mapToReveal.mapAreaID;

        if (GameMasterScript.heroPCActor.mapsExploredByMapID.Contains(mapAreaID))
        {
            return false;
        }

        mapToReveal.SetMapVisibility(true);

        Map connectingMap = null;
        foreach (Stairs st in mapToReveal.mapStairs)
        {
            if (!st.NewLocation.dungeonLevelData.deepSideAreaFloor)
            {
                connectingMap = st.NewLocation;
                break;
            }
        }

        if (connectingMap != null)
        {
            StringManager.SetTag(0, mapToReveal.GetName());
            StringManager.SetTag(1, connectingMap.GetName());
            UIManagerScript.StartConversationByRef("map_unlock", DialogType.STANDARD, null);
        }

        return false;
    }

    public static bool HuSynUnlock(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        if (!SharedBank.CheckIfJobIsUnlocked(CharacterJobs.HUSYN))
        {
            UIManagerScript.FlashWhite(1.0f);
            SharedBank.UnlockJob(CharacterJobs.HUSYN);
        }

        return false;
    }

    public static bool LunarNewYearQuestReward(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Item legendary = LootGeneratorScript.GenerateLootFromTable(1.6f, 0f, "lunarnewyear_legendary");

        MapTileData tile = MapMasterScript.GetRandomEmptyTile(GameMasterScript.heroPCActor.GetPos(), 2, true, false, true, false);

        MapMasterScript.activeMap.PlaceActor(legendary, tile);
        MapMasterScript.singletonMMS.SpawnItem(legendary);

        return false;
    }

    public static bool CasinoQuestReward(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        if (!SharedBank.CheckIfJobIsUnlocked(CharacterJobs.GAMBLER))
        {
            UIManagerScript.FlashWhite(1.0f);
            SharedBank.UnlockJob(CharacterJobs.GAMBLER);
        }

        Item tokens = LootGeneratorScript.CreateItemFromTemplateRef("item_casinochip", 1.0f, 0f, false);
        Consumable t = tokens as Consumable;

        int quantity = UnityEngine.Random.Range(30, 50);

        t.Quantity = quantity;

        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(t, true);

        StringManager.SetTag(0, quantity.ToString());

        GameLogScript.LogWriteStringRef("log_casino_earnchips");

        return false;
    }

    //Can't call our pet to us because he is tuckered out.
    public static bool CallPetToPlayerOnCooldown(string value)
    {
        //oops! Cannot do.
        UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);

        //find our pet
        var pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo")) as Monster;
        if (pet == null) return false;

        //poor beastie is tuckered out
        StringManager.SetTag(0, pet.displayName);
        GameLogScript.LogWriteStringRef("log_pet_call_cooldown");

        return false;
    }

    public static bool CallPetToPlayer(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        var pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo")) as Monster;

        if (pet == null) return false;

        pet.SetMyTarget(GameMasterScript.heroPCActor);
        pet.myTargetTile = GameMasterScript.heroPCActor.GetPos();

        CombatManagerScript.SpawnChildSprite("AggroEffect", pet, Directions.NORTHEAST, false);

        StringManager.SetTag(0, pet.displayName);
        GameLogScript.LogWriteStringRef("log_pet_call");

        //If you want to revert this back to the old system, change the pet's state to FORCEDMOVE and don't do the stuff below
        //just return here

        //clear out any desire to do anything but jump to the player
        pet.SetState(BehaviorState.PETFORCEDRETURN);

        // If we're not anchored to the hero... well, why aren't we???
        if (pet.anchor == null && pet.actorfaction == Faction.PLAYER && pet.bufferedFaction == Faction.PLAYER) 
        {
            pet.anchor = GameMasterScript.heroPCActor;
            pet.anchorRange = 4;
        }

        //tell the pet he's been wandering for way too long
        pet.SetActorData("turnsoutofanchorrange", 9999);

        // When enabled, the pet will prioritize staying RIGHT NEXT TO YOU for a few turns.
        pet.SetActorData("stay_nextto_anchor", GameMasterScript.PET_STAY_NEAR_PLAYER_TURNS); 

        //next turn, our pet should dash to us ASAP!

        //add a cooldown if we are a tamed pet.
        if (pet.tamedMonsterStuff != null)
        {
            pet.tamedMonsterStuff.SetTuckeredOut(); // corral pets can have a shorter cooldown
        }
        else
        {
            pet.SetTuckeredOut(); // slightly different function for non-corral pets
        }

        return false;
    }

    public static bool DuelWithPet(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Actor pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo"));
        Monster m = pet as Monster;

        if (m != null)
        {
            m.myStats.AddStatusByRef("monsterundying_temp", m, 20, false);
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("monsterundying_temp", GameMasterScript.heroPCActor, 20, false);
            m.actorfaction = Faction.ENEMY;
            m.bufferedFaction = Faction.ENEMY;
            GameMasterScript.heroPCActor.myStats.AddStatusByRef("pet_duel", GameMasterScript.heroPCActor, 30, false);
            StringManager.SetTag(0, m.displayName);
            GameLogScript.LogWriteStringRef("log_pet_duel");

            GameMasterScript.heroPCActor.SetActorData("hp_preduel", (int)GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.HEALTH));
            m.SetActorData("hp_preduel", (int)m.myStats.GetCurStat(StatTypes.HEALTH));

            m.AddAggro(GameMasterScript.heroPCActor, 500f);            
        }

        return false;
    }

    public static bool DismissPet(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Actor pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo"));
        Monster m = pet as Monster;

        if (m != null)
        {
            MapMasterScript.activeMap.RemoveActorFromMap(m);
            MapMasterScript.activeMap.RemoveActorFromLocation(m.GetPos(), m);
            GameMasterScript.heroPCActor.summonedActors.Remove(m);
            m.myStats.SetStat(StatTypes.HEALTH, 0, StatDataTypes.CUR, false);
            m.destroyed = true;
            m.myMovable.FadeOutThenDie();
            GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
            GameMasterScript.heroPCActor.OnRemoveSummon(m);
        }

        StringManager.SetTag(0, pet.displayName);
        GameLogScript.LogWriteStringRef("log_summon_disappear");
        PetPartyUIScript.RefreshContentsOfPlayerParty();        

        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.energyReserve == 0 && abil.staminaReserve == 0) continue;
            abil.CheckCooldownConditions(GameMasterScript.heroPCActor);
        }

        GameMasterScript.heroPCActor.myAbilities.TryCleanAbilitiesThatReserveEnergy();
        GameMasterScript.heroPCActor.cachedBattleData.SetDirty();
        UIManagerScript.RefreshPlayerStats();

        return false;

    }
    public static bool SetPetBehavior(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Actor pet = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("pet_behavior_convo"));

        string[] parsed = value.Split('|');

        int flagValue = 0;
        Int32.TryParse(parsed[1], out flagValue);

        pet.SetActorData(parsed[0], flagValue);


        //GameLogScript.LogWriteStringRef("log_pet_command");

        switch (parsed[0])
        {
            case "anchor_range":
                if (flagValue == 1)
                {
                    StringManager.SetTag(1, StringManager.GetString("pet_behavior_follow_close"));
                }
                else
                {
                    StringManager.SetTag(1, StringManager.GetString("pet_behavior_follow_wander"));
                }
                pet.anchorRange = flagValue;
                break;
            case "pet_no_abilities":
                if (flagValue == 1)
                {
                    StringManager.SetTag(1, StringManager.GetString("pet_behavior_abilities_no"));
                }
                else
                {
                    StringManager.SetTag(1, StringManager.GetString("pet_behavior_abilities_yes"));
                }
                break;
        }

        StringManager.SetTag(0, pet.displayName);
        GameLogScript.LogWriteStringRef("log_pet_command");
        UIManagerScript.PlayCursorSound("Select");
        return false;
    }

    public static bool CragganReturnToEntrance(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Actor craggan = MapMasterScript.activeMap.FindActor("craggan_injuredworker");
        MapMasterScript.activeMap.RemoveActorFromLocation(craggan.GetPos(), craggan);
        MapMasterScript.activeMap.RemoveActorFromMap(craggan);

        GameObject.Destroy(craggan.GetObject());

        Map entrance = MapMasterScript.theDungeon.FindFloor(219);

        GameMasterScript.heroPCActor.SetActorData("craggan_mine_rescue", 3);

        Vector2 newPos = new Vector2(9f, 10f);

        craggan.SetCurPos(newPos);
        craggan.myMovable.position = newPos;
        craggan.SetSpawnPos(newPos);

        entrance.AddActorToMap(craggan);
        entrance.AddActorToLocation(craggan.GetPos(), craggan);

        return false;
    }

    public static bool SetPandoraCountFromNGP(string value)
    {
        int iVal = 0;
        Int32.TryParse(value, out iVal);

        switch(iVal)
        {
            case 0:
                GameMasterScript.heroPCActor.numPandoraBoxesOpened = 0;
                UIManagerScript.PlayCursorSound("ShamanHeal");
                break;
            case 1:
                UIManagerScript.PlayCursorSound("PandoraBox");
                break;
        }

        UIManagerScript.CloseDialogBox();
        return false;
    }

    public static bool BuyPetInsurance(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        int cost = MonsterCorralScript.GetPetInsuranceCost();

        if (GameMasterScript.heroPCActor.ReadActorData("petinsurance") == 1)
        {
            UIManagerScript.StartConversationByRef("corral_petinsurance_alreadyhave", DialogType.STANDARD, null);
            return false;
        }

        if (cost > GameMasterScript.heroPCActor.GetMoney())
        {
            UIManagerScript.StartConversationByRef("corral_petinsurance_notenoughgold", DialogType.STANDARD, null);
            return false;
        }

        GameMasterScript.heroPCActor.ChangeMoney(cost * -1);
        GameMasterScript.heroPCActor.SetActorData("petinsurance", 1);

        UIManagerScript.PlayCursorSound("Buy Item");

        GameLogScript.LogWriteStringRef("log_purchase_petinsurance");

        PetPartyUIScript.RefreshContentsOfPlayerParty();

        return false;
    }

    public static bool BringMonsterToCorral(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Monster bringMonster = null;
        if (GameMasterScript.heroPCActor.summonedActors == null || GameMasterScript.heroPCActor.summonedActors.Count == 0)
        {
            GameLogScript.LogWriteStringRef("log_corral_error_nomonster");
            return false;
        }
        foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
        {
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = act as Monster;
                if ((mn.surpressTraits) && (mn.actorRefName != "mon_runiccrystal"))
                {
                    bringMonster = mn;
                    break;
                }
            }
        }
        if (bringMonster == null)
        {
            GameLogScript.LogWriteStringRef("log_corral_error_nomonster");
            return false;
        }

        //bringMonster.DisplayAllPowerAndAbilities();

        MonsterCorralScript.NameMonsterThenAddToCorralForFirstTime(bringMonster, false);
        UIManagerScript.UpdatePetInfo();
        return false;
    }

    public static bool TryGiveGemToBanquo(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        Consumable gem = null;
        float highestCV = 0f;
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType != ItemTypes.CONSUMABLE) continue;
            if (!itm.CheckTag((int)ItemFilters.VALUABLES)) continue;
            if (itm.challengeValue > highestCV && (itm.actorRefName.Contains("gem") || itm.actorRefName.Contains("cashcrop")))
            {
                gem = itm as Consumable;
            }
        }

        if (gem == null)
        {
            UIManagerScript.StartConversationByRef("smith_banquo_nogem", DialogType.STANDARD, null);
        }
        else
        {
            Accessory accessoryToCreate = LootGeneratorScript.GenerateLootFromTable(gem.challengeValue + 0.1f, 2.0f + gem.challengeValue, "accessories") as Accessory;
            if (accessoryToCreate != null)
            {
                if (!gem.ChangeQuantity(-1))
                {
                    GameMasterScript.heroPCActor.myInventory.RemoveItem(gem);
                }
                GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("EquipItem");
                EquipmentBlock.MakeMagical(accessoryToCreate, gem.challengeValue, false);
                MapTileData emptyMTD = MapMasterScript.FindNearbyEmptyTileForItem(GameMasterScript.heroPCActor.GetPos());
                MapMasterScript.activeMap.PlaceActor(accessoryToCreate, emptyMTD);
                MapMasterScript.singletonMMS.SpawnItem(accessoryToCreate);
                GameMasterScript.heroPCActor.AddActorData("banquo_smithed", 1);
            }
            else
            {
                Debug.Log("Gem creation failed...? Spawned item was null?");
            }

        }

        return false;

        // Check for player gem of any kind
    }

    public static bool RandomizeCorralMonsterName(string value)
    {
        UIManagerScript.genericTextInputField.text = MonsterManagerScript.GetRandomPetName();
        //if (Debug.isDebugBuild) Debug.Log("Randomized corral monster name.");
        return true;
    }

    public static bool NameNewCorralMonster(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        string monsterName = UIManagerScript.genericTextInputField.text;


        Actor findAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("monsterbeingnamedforcorral"));

        Monster monBeingNamed = findAct as Monster;

        int autoSetHappinessLevel = -1;

        if (string.IsNullOrEmpty(monsterName))
        {
            string newName = MonsterManagerScript.GetRandomPetName();
            monsterName = newName;
        }

        int preActorID = monBeingNamed.actorUniqueID;

        if (monBeingNamed.actorRefName == "mon_fungaltoad" && ProgressTracker.CheckProgress(TDProgress.CORRALQUEST, ProgressLocations.META) != 3)
        {
            NPC jesse = MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC;
            StringManager.SetTag(0, monBeingNamed.displayName);
            autoSetHappinessLevel = 8;
            Conversation c = GameMasterScript.FindConversation("monstercorral_quest_success");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(c, DialogType.KEYSTORY, jesse, 0.01f));
        }
        else if (monBeingNamed.actorRefName == "mon_harmlessfungaltoad")
        {
            // Swap the fake toad with a real Fungal Toad.
            GameMasterScript.Destroy(monBeingNamed.GetObject());
            MapMasterScript.activeMap.RemoveActorFromMap(monBeingNamed);
            GameMasterScript.heroPCActor.RemoveSummon(monBeingNamed);
            Monster swapMon = MonsterManagerScript.CreateMonster("mon_fungaltoad", false, false, false, 0f, false);
            int testID = swapMon.actorUniqueID; 
            monBeingNamed = swapMon;
            GameMasterScript.dictAllActors.Remove(preActorID);
            GameMasterScript.dictAllActors.Remove(testID);
            swapMon.actorUniqueID = preActorID;
            GameMasterScript.dictAllActors.Add(preActorID, swapMon);
            autoSetHappinessLevel = 8;

            MapMasterScript.activeMap.AddActorToMap(swapMon);

            ProgressTracker.SetProgress(TDProgress.CORRALQUEST, ProgressLocations.META, 3);

            NPC jesse = MapMasterScript.activeMap.FindActor("npc_monsterguy") as NPC;
            StringManager.SetTag(0, monBeingNamed.displayName);
            Conversation c = GameMasterScript.FindConversation("monstercorral_quest_success");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(c, DialogType.KEYSTORY, jesse, 0.01f));
        }

        monBeingNamed.displayName = monsterName;
        
        MetaProgressScript.AddMonsterActorToCorral(monBeingNamed, true);

        if (autoSetHappinessLevel >= 0)
        {
            TamedCorralMonster tcm = MetaProgressScript.GetTamedCorralMonsterByActorRef(monBeingNamed);
            tcm.happiness = autoSetHappinessLevel;
        }

        StringManager.SetTag(0, monBeingNamed.displayName);
        GameLogScript.LogWriteStringRef("log_corral_bringmonster");

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (PlatformVariables.SEND_UNITY_ANALYTICS)
{
        Dictionary<string, object> petInfo = new Dictionary<string, object>();
        petInfo.Add("petref", monBeingNamed.actorRefName);
        petInfo.Add("plvl", GameMasterScript.heroPCActor.myStats.GetLevel());
        petInfo.Add("ngplus", GameStartData.saveSlotNGP[GameStartData.saveGameSlot]);
        petInfo.Add("floor_lowest", GameMasterScript.heroPCActor.lowestFloorExplored);
        petInfo.Add("currentpets", MetaProgressScript.localTamedMonstersForThisSlot.Count);

        Analytics.CustomEvent("corral_petcapture", petInfo);

        if (monBeingNamed.actorRefName == "mon_xp_spiritstag")// && monBeingNamed.ReadActorData("tcmrarityup") == 1)
        {
            GameMasterScript.gmsSingleton.statsAndAchievements.DLC1_SpiritStagCapture();
        }
}
#endif

        GameMasterScript.heroPCActor.RemoveActorData("knockedoutmonster");
        UIManagerScript.singletonUIMS.CloseAllDialogs();
        UIManagerScript.UpdatePetInfo();
        return false;
    }

    public static bool NameNewlyCreatedMonster(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        string monsterName = UIManagerScript.genericTextInputField.text;

        Actor findAct = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.gmsSingleton.ReadTempGameData("monsterbeingnamedforcorral"));

        Monster monBeingNamed = findAct as Monster;

        if (string.IsNullOrEmpty(monsterName))
        {
            string newName = MonsterManagerScript.GetRandomPetName();
            monBeingNamed.displayName = newName;
            monsterName = newName;
        }

        monBeingNamed.displayName = monsterName;

        StringManager.SetTag(0, monBeingNamed.displayName);
        GameLogScript.LogWriteStringRef("log_corral_newlybredmonsterincorral");
        UIManagerScript.singletonUIMS.CloseAllDialogs();
        return false;
    }



    public static bool ReturnPetToCorral(string value)
    {
        Debug.Log("Request return pet to corral. " + value);

        Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();

        if (pet == null)
        {
            Debug.Log("But hero has no pet.");
            return false;
        }

        StringManager.SetTag(0, pet.displayName);

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        if (MetaProgressScript.localTamedMonstersForThisSlot.Count == MonsterCorralScript.MAX_MONSTERS_IN_CORRAL)
        {
            GameLogScript.LogWriteStringRef("log_corral_error_fullforpet");
            UIManagerScript.PlayCursorSound("Error");
            return false;
        }

        // this would happen if we're dueling them
        if (pet.actorfaction == Faction.ENEMY)
        {
            UIManagerScript.PlayCursorSound("Error");
            return false;
        }

        GameMasterScript.heroPCActor.RemoveSummon(pet);
        pet.ClearAnchor();
        pet.myStats.RemoveTemporaryNegativeStatusEffects();

        if (pet.tamedMonsterStuff == null)
        {
            Debug.LogError("WARNING: Pplayer pet " + pet.actorUniqueID + " " + pet.actorRefName + " has NO TCM!");
            return false;
        }

        MetaProgressScript.AddPetToLocalSlotCorralList(pet.tamedMonsterStuff, 0);

        MetaProgressScript.MoveMonsterActorIntoCorral(pet);
        maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;
        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersInCorral(maxMonsterCount);
        GameMasterScript.heroPCActor.ResetPetData();

        GameLogScript.LogWriteStringRef("ui_corral_returnpet");
        PetPartyUIScript.RefreshContentsOfPlayerParty();
        UIManagerScript.CloseDialogBox(true);
        return false;
    }

    public static bool ShareRomanticMealAtCorral(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //int localCount = MetaProgressScript.localTamedMonstersForThisSlot.Count;
        if (GameMasterScript.heroPCActor.HasMonsterPet())
        {
            maxMonsterCount++;
        }

        if (maxMonsterCount >= MonsterCorralScript.MAX_MONSTERS_IN_CORRAL)
        {
            UIManagerScript.StartConversationByRef("corral_breed_maxmonsters", DialogType.STANDARD, null);
            return false;
        }

        Item findMeal = null;
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable c = itm as Consumable;
                if (c.seasoningAttached == "spice_rosepetals" && c.isFood)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("romanticmealitem", c.actorUniqueID);
                    findMeal = c;
                    break;
                }
            }
        }
        if (findMeal == null)
        {
            Debug.Log("WARNING: Player has no romantic meal, somehow?");
            return false;
        }
        CorralBreedScript.OpenBreedingInterface();
        return false;
    }

    public static bool PlantTreeType1(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        Item seeds = GameMasterScript.heroPCActor.myInventory.GetMostRareItemByRef("seeds_tree1");
        if (seeds == null)
        {
            Debug.Log("Player does not have seeds1?");
            return false;
        }
        int treeSlot = UIManagerScript.currentConversation.whichNPC.GetTreeSlot();
        MetaProgressScript.PlantTree(treeSlot, seeds);
        return false;
    }

    public static bool PlantTreeType2(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        Item seeds = GameMasterScript.heroPCActor.myInventory.GetMostRareItemByRef("seeds_tree2");
        if (seeds == null)
        {
            Debug.Log("Player does not have seeds2?");
            return false;
        }
        int treeSlot = UIManagerScript.currentConversation.whichNPC.GetTreeSlot();
        MetaProgressScript.PlantTree(treeSlot, seeds);
        return false;
    }

    public static bool PlantTreeType3(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        Item seeds = GameMasterScript.heroPCActor.myInventory.GetMostRareItemByRef("seeds_tree3");
        if (seeds == null)
        {
            Debug.Log("Player does not have seeds3?");
            return false;
        }
        int treeSlot = UIManagerScript.currentConversation.whichNPC.GetTreeSlot();
        MetaProgressScript.PlantTree(treeSlot, seeds);
        return false;
    }

    public static bool PlantTreeType4(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        Item seeds = GameMasterScript.heroPCActor.myInventory.GetMostRareItemByRef("seeds_tree4");
        if (seeds == null)
        {
            Debug.Log("Player does not have seeds4?");
            return false;
        }
        int treeSlot = UIManagerScript.currentConversation.whichNPC.GetTreeSlot();
        MetaProgressScript.PlantTree(treeSlot, seeds);
        return false;
    }

    public static bool FindFriendshipForest(string value)
    {
        UIManagerScript.CloseDialogBox(true);


        int floor = 0;

        Map romance = MapMasterScript.theDungeon.FindFloor(213);
        foreach (Stairs st in romance.mapStairs)
        {
            floor = st.NewLocation.floor;
            break;
        }

        while (romance.mapStairs.Count > 1)
        {
            romance.mapStairs.RemoveAt(0);
        }

        Map connectMap = romance.mapStairs[0].NewLocation;

        romance.SetMapVisibility(true);

        StringManager.SetTag(0, connectMap.GetName());
        UIManagerScript.StartConversationByRef("quest_monster_romance_find", DialogType.KEYSTORY, null);

        return false;
    }

    public static bool DirtbeakLibraryFightBegin(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.SetAnimationPlaying(false);

        Monster dirtbeak = MapMasterScript.activeMap.FindActor("mon_dirtbeak_library") as Monster;

        BossHealthBarScript.EnableBossWithAnimation(dirtbeak);
        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("bosstheme1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme1";

        return false;
    }

    public static bool ReturnToPreviousUIState(string value)
    {
        UIManagerScript.CloseDialogBox();
        if (ShopUIScript.CheckShopInterfaceState())
        {
            ShopUIScript.ReopenShop();            
        }
        GameMasterScript.gmsSingleton.SetTempGameData("dropitem", -1);

        return true;
    }

    public static bool DirtbeakLibraryDirtbeakLeaves(string value)
    {
        GameMasterScript.SetAnimationPlaying(true);
        Monster dirtbeak = MapMasterScript.activeMap.FindActor("mon_dirtbeak_library") as Monster;
        MapMasterScript.activeMap.RemoveActorFromMap(dirtbeak);
        Vector3 cPos = dirtbeak.GetPos();
        cPos.x += 30f;
        dirtbeak.myMovable.AnimateSetPositionNoChange(cPos, 1.2f, false, 360f, 0f, MovementTypes.LERP);
        GameMasterScript.gmsSingleton.WaitThenDestroyObject(dirtbeak.GetObject(), 2f);
        GameMasterScript.gmsSingleton.StartCoroutine(GameMasterScript.gmsSingleton.WaitThenStopAnimation(0.75f));
        foreach(Monster m in MapMasterScript.activeMap.monstersInMap)
        {
            if (m.actorfaction == Faction.ENEMY)
            {
                m.myAnimatable.SetAnim("TakeDamage");
                CombatManagerScript.SpawnChildSprite("AggroEffect", m, Directions.NORTHEAST, false);
            }
        }
        MapMasterScript.activeMap.RemoveActorFromLocation(dirtbeak.GetPos(), dirtbeak);
        dirtbeak.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
        BossHealthBarScript.DisableBoss();
        return true;
    }

    public static bool PrepareForFirstBossFight(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.SetAnimationPlaying(false);

        Monster dirtbeak = MapMasterScript.activeMap.FindActor("mon_banditwarlord") as Monster;

        GameMasterScript.cameraScript.SetCustomCameraAnimation(dirtbeak.GetPos(), GameMasterScript.heroPCActor.GetPos(), 0.75f);

        BossHealthBarScript.EnableBossWithAnimation(dirtbeak);
        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("bosstheme1");
        MapMasterScript.activeMap.musicCurrentlyPlaying = "bosstheme1";

        ProgressTracker.SetProgress(TDProgress.BOSS1, ProgressLocations.HERO, 2);        

        return false;
    }

    public static bool GoldfrogLevitateAtCampfire(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.SetAnimationPlaying(true);
        if (MapMasterScript.activeMap.floor == MapMasterScript.CAMPFIRE_FLOOR)
        {
            Cutscenes.StartGoldfrogSequence(UIManagerScript.currentConversation.whichNPC);
        }        
        return false;
    }

    public static bool BeastlakeCompleteQuest(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.SetAnimationPlaying(true);
        Cutscenes.StartBeastlakeParkSequence(UIManagerScript.currentConversation.whichNPC);
        return false;
    }

    public static bool CreateNightmareOrb(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        if (GameMasterScript.heroPCActor.myInventory.GetItemQuantity("item_shadoworb_piece") < 3)
        {
            UIManagerScript.StartConversationByRef("mir_shadowshard_needmore", DialogType.STANDARD, null);
            return false;
        }

        UIManagerScript.FlashRed(1.0f);
        GameLogScript.LogWriteStringRef("log_receive_shadow_orb");

        GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityByRef("item_shadoworb_piece", -3);

        Item reverie = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 1.0f, 0f, false);
        reverie.SetActorData("nightmare_orb", 1);
        reverie.rarity = Rarity.ARTIFACT;
        reverie.RebuildDisplayName();
        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(reverie, true);
        ProgressTracker.SetProgress(TDProgress.SHADOWSHARDS, ProgressLocations.META, 2);
        return false;
    }

    public static bool TryChangeHoliday(string value)
    {
        if (Debug.isDebugBuild) Debug.Log("Try change holiday to " + value);

        if (!Enum.TryParse<Seasons>(value, out Seasons trySeason))
        {
            UIManagerScript.PlayCursorSound("Error");
            UIManagerScript.CloseDialogBox(true);
            return true;
        }

        int cost = GameMasterScript.gmsSingleton.ReadTempGameData("eventgold");

        if (trySeason == Seasons.COUNT) cost = 0;

        StringManager.SetTag(0, cost.ToString());
        
        if (GameMasterScript.heroPCActor.GetMoney() < cost)
        {
            UIManagerScript.PlayCursorSound("Error");
            UIManagerScript.CloseDialogBox(true);
            GameLogScript.LogWriteStringRef("error_notenough_gold");
            return true;
        }

        if (cost != 0) GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);

        TDPlayerPrefs.SetInt(GlobalProgressKeys.CUSTOM_SEASON, (int)trySeason);
        SharedBank.SetCustomSeasonValue(trySeason);

        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG Successfully changed seasons!");

        UIManagerScript.SwitchConversationBranch(UIManagerScript.currentConversation.FindBranch("seasonsuccess"));
        UIManagerScript.UpdateDialogBox();

        GameLogScript.LogWriteStringRef("holiday_notification_txt");

        UIManagerScript.PlayCursorSound("Ultra Learn");

        return true;
    }

    public static bool TryChangeClothes(string value)
    {
        UIManagerScript.ToggleDialogBox(DialogType.CHANGECLOTHES, true, false);
        UIManagerScript.SetDialogPos(0, 0f);

        int jobsToCount = CharCreation.NUM_JOBS;

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            jobsToCount += 2;
        }

        for (int i = 0; i < jobsToCount; i++)
        {
            CharacterJobs cj = (CharacterJobs)i;
            if ((GameMasterScript.heroPCActor.GetTotalJPGainedAndSpentInJob(cj) > 0 
                || RandomJobMode.IsCurrentGameInRandomJobMode())
                && CharacterJobData.GetJobDataByEnum(i) != null
                && cj != CharacterJobs.SHARA)
            {
                UIManagerScript.CreateDialogOptionByInt("<color=yellow>" + StringManager.GetString("misc_changeclothes_to") + CharacterJobData.GetJobDataByEnum(i).GetBaseDisplayName() + "</color>", (i + 10));
            }
        }

        /* if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && PlayerPrefs.GetInt("sharastorycleared") == 1)
        {
            UIManagerScript.CreateDialogOptionByInt("<color=yellow>" + StringManager.GetString("misc_changeclothes_to") + CharacterJobData.GetJobData("MIRAISHARA").displayName + "</color>", (jobsToCount + 10));
        } */

        UIManagerScript.CreateDialogOption("<color=yellow>" + StringManager.GetString("misc_button_exit_normalcase") + "</color>", DialogButtonResponse.EXIT);
        string text = UIManagerScript.greenHexColor + StringManager.GetString("misc_changeclothes") + "</color>\n";
        UIManagerScript.DialogBoxWrite(text);
        UIManagerScript.UpdateDialogCursorPos();

        return false;
    }

    public static bool OpenJobChangeUI(string value)
    {

        int cost = GameMasterScript.GetJobChangeCost();
        if (GameMasterScript.heroPCActor.GetMoney() < cost)
        {
            UIManagerScript.PlayCursorSound("Error");
            StringManager.SetTag(0, cost.ToString());
            UIManagerScript.SwitchConversationBranch(UIManagerScript.currentConversation.FindBranch("jobs_not_enough_gold"));
            return true;
        }

        /* GameMasterScript.gmsSingleton.StartJobChange();
        return; */

        UIManagerScript.CloseDialogBox(true);
        GameMasterScript.jobChangeFromNPC = true;
        CharCreation.singleton.BeginCharCreation_JobSelection();
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenAlignCursor(0.05f, CharCreation.jobButtons[0]));
        return false;
    }

    public static bool RecenterCameraOnPlayer(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        GameMasterScript.cameraScript.LeaveCameraInCurrentLocationUntilNextAnimation(false);
        GameMasterScript.cameraScript.WaitThenSetCustomCameraMovement(GameMasterScript.heroPCActor.GetPos(), 1.0f, 0f);        

        return false;
    }

    public static bool BeastlakeLearnMalletSkill(string value)
    {
        GameMasterScript.SetAnimationPlaying(false);
        UIManagerScript.CloseDialogBox(true);
        int banditID = GameMasterScript.gmsSingleton.ReadTempGameData("beastlakebandit");
        Actor findBandit;
        if (GameMasterScript.dictAllActors.TryGetValue(banditID, out findBandit))
        {
            Monster bandit = findBandit as Monster;

            if (bandit.tamedMonsterStuff != null)
            {
                // Don't remove the bandit because it must already be a pet.
            }
            else
            {
                bandit.ReverseMalletEffect();
                bandit.myStats.HealToFull();
                GameMasterScript.heroPCActor.RemoveSummon(bandit);
                GameMasterScript.heroPCActor.ResetPetData();
                MetaProgressScript.localTamedMonstersForThisSlot.Remove(bandit.tamedMonsterStuff);                
                MapMasterScript.singletonMMS.townMap2.RemoveActorFromMap(bandit);

                bandit.actorfaction = Faction.PLAYER;
                bandit.bufferedFaction = Faction.PLAYER;
                PetPartyUIScript.RefreshContentsOfPlayerParty();
            }
        }
        else
        {
            Debug.Log("Couldn't find the knocked out bandit for beastlake quest.");
        }

        return false;
    }

    public static bool AbandonSelectedRumor(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        int questIndex = GameMasterScript.gmsSingleton.ReadTempGameData("abandonquestindex");
        QuestScript qs = GameMasterScript.heroPCActor.myQuests[questIndex];

        QuestScript.OnQuestFailedOrAbandoned(qs);

        GameMasterScript.heroPCActor.myQuests.RemoveAt(questIndex);
        StringManager.SetTag(0, qs.displayName);
        GameLogScript.LogWriteStringRef("log_abandon_rumor");

        return false;
    }

    public static bool ConfirmEnterItemDreamNoGold(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        int dreamItemID = GameMasterScript.gmsSingleton.ReadTempGameData("dreamcaster_item_selected");
        int dreamOrbID = GameMasterScript.gmsSingleton.ReadTempGameData("dreamcaster_orb_selected");

        Item orb = GameMasterScript.heroPCActor.myInventory.GetItemByID(dreamOrbID);
        Item dreamItemToEnter = GameMasterScript.heroPCActor.myInventory.GetItemByID(dreamItemID);
        if (dreamItemToEnter == null)
        {
            // Maybe it's equipped.
            dreamItemToEnter = GameMasterScript.heroPCActor.myEquipment.GetItemByIDIfEquipped(dreamItemID);
        }

        if (orb == null)
        {
            Debug.Log("No player orb?");
            return false;
        }
        if (dreamItemToEnter == null)
        {
            Debug.Log("No player item?");
            return false;
        }

        bool lucidSkillOrbSelected = GameMasterScript.simpleBool[GameMasterScript.gmsSingleton.ReadTempGameData("dreamcaster_lucidorb")];

        GameMasterScript.TryBeginItemWorld(dreamItemToEnter, orb, 0f);

        return false;
    }


    public static bool TryOpenItemWorldPortal(string value)
    {
        UIManagerScript.CloseDialogBox(true);

        if (MapMasterScript.itemWorldOpen)
        {
            GameLogScript.LogWriteStringRef("log_error_itemworld_open");
        }
        else
        {
            ItemWorldUIScript.OpenItemWorldInterface();
        }

        return false;
    }

    public static bool RestAtCampfire(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.FadeOut(2.9f);
        MusicManagerScript.RequestPlayNonLoopingMusicFromScratchWithCrossfade("resttheme");
        if (SharaModeStuff.IsSharaModeActive()) SharaModeStuff.WaitThenPlayNormalSharaThemeIfStillInCampfire(9f);
        MysteryDungeonManager.PlayerRestedAtFire();

        GameMasterScript.SetAnimationPlaying(true);
        GameMasterScript.heroPCActor.myStats.HealToFull();
        GameMasterScript.heroPCActor.HealAllSummonsToFull();
        UIManagerScript.currentConversation.whichNPC.SetActorData("fireused", 1);

        Cutscenes.PlayerRestedAtFire();

        return false;
    }

    public static bool TravelToDimensionalRift(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.PlayCursorSound("Mirage");

        TravelManager.TravelFromTownToFloor(MapMasterScript.SPECIALFLOOR_DIMENSIONAL_RIFT);
        return false;
    }

    public static bool TravelToTutorialFloor(string value)
    {
        UIManagerScript.CloseDialogBox(true); // wait to fade options
        int responseValue = Int32.Parse(value);

        if (responseValue == 0)
        {
            // Skip tutorial.
            GameMasterScript.heroPCActor.SetActorData("tutorial_finished", 1);
        }

        TravelManager.TravelFromTownToFloor(responseValue);
        return false;

    }

    public static bool CampfireCookItem(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.FadeOut(3f);
        MusicManagerScript.RequestPlayNonLoopingMusicFromScratchWithCrossfade("resttheme");
        if (SharaModeStuff.IsSharaModeActive()) SharaModeStuff.WaitThenPlayNormalSharaThemeIfStillInCampfire(9f);
        MysteryDungeonManager.PlayerRestedAtFire();

        GameMasterScript.SetAnimationPlaying(true);

        Item food = null;
        switch (value)
        {
            case "roastmeat":
                food = LootGeneratorScript.CreateItemFromTemplateRef("food_campfiremeat", 1.0f, 0f, false);
                break;
            case "roastcheese":
                food = LootGeneratorScript.CreateItemFromTemplateRef("food_campfirecheese", 1.0f, 0f, false);
                break;
            case "roastfruit":
                food = LootGeneratorScript.CreateItemFromTemplateRef("food_campfirefruit", 1.0f, 0f, false);
                break;
            case "roastdessert":
                food = LootGeneratorScript.CreateItemFromTemplateRef("food_campfiredessert", 1.0f, 0f, false);
                break;
        }

        StringManager.SetTag(0, food.displayName);
        GameLogScript.LogWriteStringRef("log_cook_campfire");

        GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(food, true);

        UIManagerScript.currentConversation.whichNPC.SetActorData("fireused", 1);

        Cutscenes.PlayerRestedAtFire();

        return false;
    }

    public static bool RobbedAtCampfire(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        MapMasterScript.RemoveActor("npc_muguzmo");
        UIManagerScript.PlaceDialogBoxInFrontOfFade(false);
        Conversation rob = GameMasterScript.FindConversation("campfire_robbed2");
        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(rob, DialogType.STANDARD, null, 3f));

        Debug.Log("Robbed by muguzmo. Let's fade in.");
        UIManagerScript.singletonUIMS.WaitThenFadeIn(1f, 3f);

        return false;
    }

    public static bool WarpPinkSlimeToLangdon(string value)
    {
        Actor findSlime = UIManagerScript.currentConversation.whichNPC;

        UIManagerScript.CloseDialogBox(true);

        Actor langdon = MapMasterScript.activeMap.FindActor("npc_farmergrotto");

        MapTileData tileNearLangdon = MapMasterScript.GetRandomEmptyTile(langdon.GetPos(), 1, true, true);

        //MapMasterScript.activeMap.MoveActor(findSlime.GetPos(), tileNearLangdon.pos, findSlime);
        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.FadeOut(0.75f);
        MapMasterScript.DoWaitThenMoveActor(findSlime, 0.75f, tileNearLangdon.pos);
        UIManagerScript.singletonUIMS.WaitThenFadeIn(0.85f, 0.75f, true);

        return false;
    }

    public static bool LearnArmorMastery(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        string abilRef = value;
        AbilityScript findAbil = GameMasterScript.masterAbilityList[abilRef];
        AbilityScript learnt = GameMasterScript.heroPCActor.LearnAbility(findAbil, true, true, false);

        //GameMasterScript.heroPCActor.myAbilities.EquipPassiveAbility(learnt);

        UIManagerScript.FlashWhite(0.75f);

        ProgressTracker.SetProgress(TDProgress.ARMOR_MASTER_QUEST, ProgressLocations.HERO, 3);        

        return true;
    }

    public static bool AcceptQuestFromTown(string value)
    {
        //Debug.Log("Attempting to accept quest " + value + " " + UIManagerScript.bufferQuest.displayName);
        GameMasterScript.heroPCActor.myQuests.Add(UIManagerScript.bufferQuest);

        UIManagerScript.bufferQuest.DoQuestSetup();

        UIManagerScript.bufferQuest.VerifyLinkedMapsAreEnabled();

        Monster tMon = UIManagerScript.bufferQuest.targetMonster;
        if (tMon != null)
        {
            tMon.myStats.AddStatusByRef("enemy_quest_target", GameMasterScript.heroPCActor, 99);
        }

        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.PlayCursorSound("Skill Learnt");
        return false;
    }

    public static bool HarvestAllFoodFromTrees(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        MetaProgressScript.HarvestAllFoodFromTrees();
        return false;
    }

    public static bool Boss2Transformation(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.FlashWhite(2f);
        GameMasterScript.cameraScript.AddScreenshake(0.75f);
        Cutscenes.DoPreBossFight2Stuff(MapMasterScript.activeMap, true);
        GameMasterScript.SetAnimationPlaying(true);
        UIManagerScript.PlayCursorSound("PandoraBox");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(GameMasterScript.FindConversation("second_boss_postintro"), DialogType.KEYSTORY, null, 2.35f));
        return false;
    }

    public static bool Boss2DirtbeakEscape(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        UIManagerScript.PlayCursorSound("Whirlwind");
        Cutscenes.DoDirtbeakEscape();
        return false;
    }

    public static bool WarpToNightmareKingRoom(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        RoomAndMapIDDatapack nkRoomData = ItemDreamFunctions.GetNightmareKingArena();

        MapTileData playerSquare = nkRoomData.room.GetEmptyTile();
        MapTileData bossSquare = nkRoomData.room.GetEmptyTile();
        while (MapMasterScript.GetGridDistance(playerSquare.pos, bossSquare.pos) < 3)
        {
            bossSquare = nkRoomData.room.GetEmptyTile();
        }

        Debug.Log(playerSquare + " " + bossSquare);
        if (nkRoomData.map != MapMasterScript.activeMap)
        {
            // warp the player to new map
            TravelManager.TravelMaps(nkRoomData.map, null, false);
            GameMasterScript.gmsSingleton.SetTempGameData("nk_boss_arena", 1);
            GameMasterScript.gmsSingleton.SetTempGameData("nk_playersquare_x", (int)playerSquare.pos.x);
            GameMasterScript.gmsSingleton.SetTempGameData("nk_playersquare_y", (int)playerSquare.pos.y);
            GameMasterScript.gmsSingleton.SetTempGameData("nk_bosssquare_x", (int)bossSquare.pos.x);
            GameMasterScript.gmsSingleton.SetTempGameData("nk_bosssquare_y", (int)bossSquare.pos.y);
        }
        else
        {
            ItemDreamFunctions.MovePlayerAndNKToArena(playerSquare, bossSquare);
        }

        return false;
    }

    public static bool ConfirmMonsterBreedTrainingJP(string value)
    {
        int amount = UIManagerScript.GetSliderValueInt();

        GameMasterScript.gmsSingleton.SetTempFloatData("mon_breed_jpspent", (float)amount);

        UIManagerScript.CloseDialogBox(true);
        CorralBreedScript.BeginMonsterBreedRoutine();

        return false;
    }

    public static bool ConfirmQuantityInDialog(string value)
    {      

        int amount = UIManagerScript.GetSliderValueInt();
        //Moved CloseDialogBox to bottom of call, that way
        //adjustments to inventory and stats can be represented by the UI
        //when the dialog is closed.

        Item itm = null;
        Item retrieve = null;
        Consumable c = null;

        bool reopenShop = false;

        switch (GameMasterScript.gmsSingleton.ReadTempStringData("adjustquantity"))
        {
            case "drop":
                itm = GameMasterScript.heroPCActor.myInventory.GetItemByID(GameMasterScript.gmsSingleton.ReadTempGameData("dropitem"));
                c = itm as Consumable;
                c.ChangeQuantity(amount * -1);
                if (c.Quantity == 0)
                {
                    GameMasterScript.heroPCActor.myInventory.RemoveItem(c);
                    LootGeneratorScript.DropItemOnGround(c, GameMasterScript.heroPCActor.GetPos(), amount);
                }
                else
                {
                    Consumable copyStack = new Consumable();
                    copyStack.CopyFromItem(c);
                    copyStack.Quantity = amount;
                    copyStack.SetUniqueIDAndAddToDict();
                    LootGeneratorScript.DropItemOnGround(copyStack, GameMasterScript.heroPCActor.GetPos(), amount);
                }
                UIManagerScript.ForceCloseFullScreenUI(); // probably needed, since we want to SEE dropping items...
                UIManagerScript.PlayCursorSound("Pickup");
                break;
            case "deposit":
                itm = GameMasterScript.heroPCActor.myInventory.GetItemByID(GameMasterScript.gmsSingleton.ReadTempGameData("dropitem"));
                c = itm as Consumable;
                retrieve = GameMasterScript.heroPCActor.myInventory.GetItemAndSplitIfNeeded(itm, amount);
                ShopUIScript.DepositItem(retrieve, amount);
                UIManagerScript.PlayCursorSound("HarvestAll");
                reopenShop = true;
                break;
            case "sellitem":
                itm = GameMasterScript.heroPCActor.myInventory.GetItemByID(GameMasterScript.gmsSingleton.ReadTempGameData("dropitem"));
                c = itm as Consumable;
                retrieve = GameMasterScript.heroPCActor.myInventory.GetItemAndSplitIfNeeded(itm, amount);
                ShopUIScript.SellItem(retrieve, amount);
                UIManagerScript.PlayCursorSound("Buy Item");
                reopenShop = true;
                break;
            case "withdraw":
                NPC wBanker = MapMasterScript.activeMap.FindActor("npc_banker") as NPC;
                itm = wBanker.myInventory.GetItemByID(GameMasterScript.gmsSingleton.ReadTempGameData("dropitem"));
                retrieve = wBanker.myInventory.GetItemAndSplitIfNeeded(itm, amount);
                //Debug.Log("Retrieved from the bank: " + retrieve.actorUniqueID + " with quantity " + retrieve.GetQuantity());
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(retrieve, true);
                StringManager.SetTag(0, retrieve.displayName + retrieve.GetQuantityText());
                GameLogScript.LogWriteStringRef("log_player_withdrawitem");
                UIManagerScript.PlayCursorSound("Pickup");
                reopenShop = true;
                break;
        }

        UIManagerScript.CloseDialogBox(true);

        //reopenShop = false;

        if (reopenShop)
        {
            ShopUIScript.ReopenShop();
        }

        GameMasterScript.gmsSingleton.SetTempGameData("dropitem", -1);

        return false;
    }

    public static bool CheckWhipUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.WHIP].ToString());

        return true;
    }

    public static bool CheckSwordUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.SWORD].ToString());

        return true;
    }

    public static bool CheckMaceUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.MACE].ToString());
        return true;
    }

    public static bool CheckDaggerUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.DAGGER].ToString());
        return true;
    }

    public static bool CheckAxeUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.AXE].ToString());
        return true;
    }

    public static bool CheckClawUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.CLAW].ToString());
        return true;
    }

    public static bool CheckStaffUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.STAFF].ToString());
        return true;
    }

    public static bool CheckFistUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.NATURAL].ToString());
        return true;
    }

    public static bool CheckBowUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.BOW].ToString());
        return true;
    }

    public static bool CheckSpearUltimateStatus(string value)
    {
        StringManager.SetTag(0, GameMasterScript.CHAMPS_KILLED_REQ_FOR_ULTIMATE.ToString());
        StringManager.SetTag(1, GameMasterScript.heroPCActor.championsKilledWithWeaponType[(int)WeaponTypes.SPEAR].ToString());
        return true;
    }

    public static bool TryExpandBankerStorage(string value)
    {
        UIManagerScript.CloseDialogBox();
        int nextItemStorageTier = SharedBank.CalculateNextBankItemStorageTier();
        int upgradeCost = SharedBank.CalculateBankUpgradeStorageCost();

        if (GameMasterScript.heroPCActor.GetMoney() < upgradeCost)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_error_bankupgrade_nogold");
            return false;
        }

        int currentTier = SharedBank.CalculateMaxBankableItems();

        StringManager.SetTag(0, currentTier.ToString());
        StringManager.SetTag(1, nextItemStorageTier.ToString());
        StringManager.SetTag(2, upgradeCost.ToString());

        SharedBank.UpgradeBankStorage();
        GameMasterScript.heroPCActor.ChangeMoney(upgradeCost * -1);
        UIManagerScript.FlashWhite(0.6f);
        UIManagerScript.PlayCursorSound("CookingSuccess");

        GameLogScript.LogWriteStringRef("log_bank_storage_upgrade");

        return false;
    }

    public static bool CalculateMaxBankerItems(string value)
    {
        int maxItems = SharedBank.CalculateMaxBankableItems();

        StringManager.SetTag(0, maxItems.ToString());

        // Upgrade possible?

        int nextItemStorageTier = SharedBank.CalculateNextBankItemStorageTier();
        int upgradeCost = SharedBank.CalculateBankUpgradeStorageCost();
        StringManager.SetTag(1, nextItemStorageTier.ToString());
        StringManager.SetTag(2, upgradeCost.ToString());

        return true;
    }

    public static bool CalculateBullionStuff(string value)
    {
        // First number: Max gold
        // Second number: Duration
        // Third number: rate of return

        int maxInvestment = MetaProgressScript.GetBankerBullionMaxInvestment(GameMasterScript.heroPCActor.myStats.GetLevel());
        int duration = MetaProgressScript.GetBankerBullionTime(maxInvestment);
        float returnRate = MetaProgressScript.GetBankerBullionInvestmentRate(GameMasterScript.heroPCActor.myStats.GetLevel());

        int profit = (int)(maxInvestment * returnRate);

        returnRate *= 100f; // for display purposes
        returnRate = (float)Math.Round(returnRate, 2);

        StringManager.SetTag(0, maxInvestment.ToString());
        StringManager.SetTag(1, duration.ToString());
        StringManager.SetTag(2, returnRate.ToString());
        StringManager.SetTag(3, profit.ToString());

        return true;
    }

    public static bool RemoveModifierFromItemViaDreamcaster(string value, bool destroyItemAndCreateShard)
    {

        int goldCost = GameMasterScript.gmsSingleton.ReadTempGameData("removemodcost");
        int itemToModifyID = GameMasterScript.gmsSingleton.ReadTempGameData("removemoditemid");

        string magicModStringRef = value;

        if (!GameMasterScript.masterMagicModList.ContainsKey(magicModStringRef))
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            UIManagerScript.CloseDialogBox(true);
            return false;
        }
        MagicMod mmRef = GameMasterScript.masterMagicModList[magicModStringRef];
                
        Equipment itemToModify = GameMasterScript.gmsSingleton.TryLinkActorFromDict(itemToModifyID) as Equipment;
        if (itemToModify == null)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            UIManagerScript.CloseDialogBox(true);
            return false;
        }

        int orbCost = ItemWorldUIScript.GetOrbCostByCV(mmRef.challengeValue, itemToModify.challengeValue, itemToModify.legendary, mmRef.IsSpecialMod());

        //Debug.Log("Calculated orb cost: " + orbCost);

        List<Item> backupOrbs = new List<Item>();
        Item plainOrbOfReverie = null;

        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.actorRefName == "orb_itemworld")
            {
                if (itm.IsJobSkillOrb() || itm.IsNightmareOrb() || itm.IsLucidOrb())
                {
                    backupOrbs.Add(itm);
                    continue;
                }
                plainOrbOfReverie = itm;
            }
        }

        if (plainOrbOfReverie == null)
        {
            UIManagerScript.CloseDialogBox();
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            StringManager.SetTag(0, orbCost.ToString());
            GameLogScript.LogWriteStringRef("log_notenoughorbs_modify");
            return false;
        }
        int totalQuantity = plainOrbOfReverie.GetQuantity();

        /* if (totalQuantity < orbCost)
        {
            foreach (Item itm in backupOrbs)
            {
                
            }
        } */

        if (totalQuantity < orbCost)
        {
            UIManagerScript.CloseDialogBox();
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            StringManager.SetTag(0, orbCost.ToString());
            GameLogScript.LogWriteStringRef("log_notenoughorbs_modify");
            return false;
        }

        GameMasterScript.heroPCActor.myInventory.ChangeItemQuantityAndRemoveIfEmpty(plainOrbOfReverie, -1 * orbCost);

        GameMasterScript.heroPCActor.ChangeMoney(-1 * goldCost);
        GameMasterScript.heroPCActor.myEquipment.UnequipByReference(itemToModify);
        itemToModify.RemoveMod(value); // was selected button action ref
        itemToModify.modsRemoved++;

        StringManager.SetTag(0, itemToModify.displayName);

        if (Debug.isDebugBuild) Debug.Log("Attempting to destroy item? " + destroyItemAndCreateShard);

        if (destroyItemAndCreateShard)
        {
            if (GameMasterScript.heroPCActor.myEquipment.IsEquipped(itemToModify))
            {
                GameMasterScript.heroPCActor.myEquipment.UnequipByReference(itemToModify);
            }
            if (itemToModify.itemType == ItemTypes.WEAPON)
            {
                Weapon wp = itemToModify as Weapon;
                if (UIManagerScript.IsWeaponInHotbar(wp)) {
                    UIManagerScript.RemoveWeaponFromActives(wp);                    
                }
            }
            
            GameMasterScript.heroPCActor.myInventory.RemoveItem(itemToModify);
            GameLogScript.GameLogWrite(StringManager.GetString("log_extractitemmod_destroyitem"), GameMasterScript.heroPCActor);

            int numShards = 1;

            float bonusShardChance = 0f;

            if (itemToModify.rarity == Rarity.UNCOMMON) bonusShardChance = 0.2f;
            if (itemToModify.rarity == Rarity.MAGICAL) bonusShardChance = 0.4f;
            if (itemToModify.rarity == Rarity.ANCIENT) bonusShardChance = 0.6f;
            if (itemToModify.rarity >= Rarity.ARTIFACT) bonusShardChance = 0.8f;

            if (UnityEngine.Random.Range(0, 1f) <= bonusShardChance) numShards++;

            if (Debug.isDebugBuild) Debug.Log("How many shards? " + numShards);

            for (int i = 0; i < numShards; i++)
            {
                Item lucidOrbShard = LootGeneratorScript.CreateItemFromTemplateRef("item_lucidorb_shard", 0f, 0f, false);
                lucidOrbShard.SetOrbMagicModRef(value);
                lucidOrbShard.RebuildDisplayName();
                bool fullyAdded = GameMasterScript.heroPCActor.myInventory.AddItem(lucidOrbShard, true);
                // Always print this message, right?
                //if (i == 0)
                {
                    StringManager.SetTag(1, lucidOrbShard.displayName);
                    GameLogScript.LogWriteStringRef("log_corral_pickup");
                }
            }

        }
        else
        {
            GameLogScript.GameLogWrite(StringManager.GetString("log_removeitemmod"), GameMasterScript.heroPCActor);
            itemToModify.CalculateShopPrice(1.0f);
            itemToModify.CalculateSalePrice();
            itemToModify.RebuildDisplayName();
        }


        UIManagerScript.FlashWhite(0.5f);

        UIManagerScript.CloseDialogBox(true);
        return false;
    }

    public static bool FrozenAreaMeltIce(string value)
    {
        UIManagerScript.CloseDialogBox(true);
        List<Actor> removeActors = new List<Actor>();
        MapTileData scienceTile = null;

        int iceLevel = GameMasterScript.heroPCActor.ReadActorData("last_frozenshard_used");
        iceLevel++;
        if (iceLevel <= 0)
        {
            iceLevel = 1;
        }

        {
            foreach (Actor act in MapMasterScript.activeMap.actorsInMap)
            {
                if (act.GetActorType() != ActorTypes.DESTRUCTIBLE && act.GetActorType() != ActorTypes.NPC) continue;
                if (act.actorRefName == "obj_unbreakablefrozen_" + iceLevel)
                {
                    removeActors.Add(act);
                }
                if (iceLevel == 1 && act.actorRefName == "npc_frozenscientist_1")
                {
                    removeActors.Add(act);
                    scienceTile = MapMasterScript.GetTile(act.GetPos());
                }
            }
            foreach (Actor act in removeActors)
            {
                GameMasterScript.gmsSingleton.DestroyActor(act);
            }

            if (scienceTile != null)
            {
                NPC halfFrozen = NPC.CreateNPC("npc_frozenscientist_2");
                MapMasterScript.activeMap.PlaceActor(halfFrozen, scienceTile);
                MapMasterScript.singletonMMS.SpawnNPC(halfFrozen);
            }
        }

        if (iceLevel < 4) // 4 is final.
        {
            UIManagerScript.FlashWhite(1.1f);
            UIManagerScript.PlayCursorSound("Mirage");
        }
        else
        {
            GameMasterScript.heroPCActor.SetActorData("frozen_quest", 6);
            UIManagerScript.PlayCursorSound("Mirage");
            //UIManagerScript.FlashWhite(1.0f);
        }

        GameMasterScript.heroPCActor.SetActorData("quest_frozen_flame_shard", 0);

        GameMasterScript.heroPCActor.SetActorData("last_frozenshard_used", iceLevel);

        if (iceLevel == 1) // First thawing of scientist, opening quest dialog
        {
            Conversation frozenDialog = GameMasterScript.FindConversation("npc_frozenscientist_2");
            GameMasterScript.SetAnimationPlaying(true);
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(frozenDialog, DialogType.KEYSTORY, null, 1.2f));
        }
        else if (iceLevel == 2) // Brought back shard to melt "2" level stuff
        {
            GameMasterScript.heroPCActor.SetActorData("frozen_quest", 4);
            GameMasterScript.SetAnimationPlaying(true);
            Conversation frozenDialog = GameMasterScript.FindConversation("npc_frozenscientist_2");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(frozenDialog, DialogType.KEYSTORY, null, 1.5f));
        }
        else if (iceLevel == 3) // Brought back shard to melt "3" level stuff
        {
            // Second to last shard.
            GameMasterScript.heroPCActor.SetActorData("frozen_quest", 5);
            GameMasterScript.SetAnimationPlaying(true);
            Conversation frozenDialog = GameMasterScript.FindConversation("npc_frozenscientist_2");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(frozenDialog, DialogType.KEYSTORY, null, 1.5f));
        }
        else if (iceLevel == 4) // Brought back shard to melt "3" level stuff
        {
            // Last shard used
            GameEventsAndTriggers.FrozenAreaFinalUnthaw();
            GameMasterScript.SetAnimationPlaying(true);
            Conversation frozenDialog = GameMasterScript.FindConversation("npc_frozenscientist_3");
            UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.WaitThenStartConversation(frozenDialog, DialogType.KEYSTORY, null, 2.1f));
        }

        return false;

    }

    public static bool SharaVistaTeleport(string value)
    {
        UIManagerScript.CloseDialogBox();

        UIManagerScript.FlashWhite(0.6f);
        Actor shara = MapMasterScript.activeMap.FindActor("npc_shara1");
        MapMasterScript.activeMap.RemoveActorFromLocation(shara.GetPos(), shara);
        MapMasterScript.activeMap.RemoveActorFromMap(shara);
        MapMasterScript.singletonMMS.activeNonTileGameObjects.Remove(shara.GetObject());
        GameMasterScript.Destroy(shara.GetObject());

        CombatManagerScript.GenerateSpecificEffectAnimation(shara.GetPos(), "SmokePoof", null, true);

        // Good spot for this?
        // For test purposes, we want to be able to kick the tires on this unlock repeatedly.

#if !UNITY_EDITOR
        if (PlayerPrefs.GetInt("sharacampaignunlocked") == 1) return false;
#endif

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            GameMasterScript.SetAnimationPlaying(true, true);
            GameMasterScript.gmsSingleton.StartCoroutine(DialogEventsScript.WaitThenUnlockSharaMode(1.6f));            
        }

        return false;
    }

    static IEnumerator WaitThenUnlockSharaMode(float time)
    {
        yield return new WaitForSeconds(time);
        UIManagerScript.PlaySound("Mirage");
        GameMasterScript.SetAnimationPlaying(false);
        MetaProgressScript.UnlockSharaMode();
    }

    public static bool BeginSharaConvo1(string value)
    {
        UIManagerScript.CloseDialogBox();
        NPC shara = MapMasterScript.activeMap.FindActor("npc_shara1") as NPC;
        Conversation sharaVista1 = GameMasterScript.FindConversation("shara_intro_dialog_tree");
        UIManagerScript.StartConversation(sharaVista1, DialogType.KEYSTORY, shara);
        return false;
    }

    public static bool HealAtPercy(string value)
    {
        UIManagerScript.CloseDialogBox();
        int cost = GameMasterScript.GetHealerCost();
        if (value == "jp")
        {
            cost = GameMasterScript.GetHealerCostJP();
            if ((int)GameMasterScript.heroPCActor.GetCurJP() < cost)
            {
                UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("log_heal_jpcost_toohigh");
                return false;
            }
        }
        else if (GameMasterScript.heroPCActor.GetMoney() < cost)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
            GameLogScript.LogWriteStringRef("log_heal_goldcost_toohigh");
            return false;
        }

        if (value == "jp")
        {
            GameMasterScript.heroPCActor.AddJP(-1f * cost);
        }
        else
        {
            GameMasterScript.heroPCActor.ChangeMoney(-1 * cost);
        }

        StringManager.SetTag(0, GameMasterScript.heroPCActor.displayName);
        GameLogScript.GameLogWrite(StringManager.GetString("percy_mutters"), GameMasterScript.heroPCActor);
        GameLogScript.GameLogWrite(StringManager.GetString("log_healtofull"), GameMasterScript.heroPCActor);
        GameMasterScript.heroPCActor.myStats.HealToFull();
        GameMasterScript.heroPCActor.HealAllSummonsToFull();
        UIManagerScript.PlayCursorSound("ShamanHeal");
        if (PlayerOptions.screenFlashes)
        {
            UIManagerScript.FlashWhite(0.3f);
        }

        UIManagerScript.RefreshPlayerStats();
        GameMasterScript.heroPCActor.timesHealedThisLevel++;
        return false;

    }

    public static bool ReturnToDungeon(string value)
    {
        UIManagerScript.CloseDialogBox();
        Stairs mainDungeonPortal = null;
        foreach (Actor act in MapMasterScript.singletonMMS.townMap.mapStairs)
        {
            if (act.GetActorType() == ActorTypes.STAIRS)
            {
                Stairs st = act as Stairs;
                if (st.isPortal)
                {
                    mainDungeonPortal = st;
                    break;
                }
            }
        }
        TravelManager.TravelToDungeonViaPortal(mainDungeonPortal);
        return false;
    }

    public static bool ReturnToItemDream(string value)
    {
        UIManagerScript.CloseDialogBox();
        Stairs itemWorldPortal = null;
        foreach (Actor act in MapMasterScript.singletonMMS.townMap2.mapStairs)
        {
            if (act.GetActorType() == ActorTypes.STAIRS)
            {
                Stairs st = act as Stairs;
                if (st.isPortal)
                {
                    itemWorldPortal = st;
                    break;
                }
            }
        }
        TravelManager.TravelToDungeonViaPortal(itemWorldPortal);
        return false;        
    }

    public static bool AddModToEmblem(string value)
    {
        List<ConversationData> toAdd = new List<ConversationData>();
        foreach(ConversationData cd in UIManagerScript.conversationQueue)
        {
            if (cd.conv.refName != "jobemblem_nexttier")
            {
                toAdd.Add(cd);
            }
        }
        UIManagerScript.conversationQueue.Clear();
        foreach(ConversationData cd in toAdd)
        {
            UIManagerScript.conversationQueue.Enqueue(cd);
        }

        UIManagerScript.CloseDialogBox();

        UIManagerScript.PlayCursorSound("Ultra Learn");

        Emblem playerEmblem = GameMasterScript.gmsSingleton.TryLinkActorFromDict(GameMasterScript.heroPCActor.ReadActorData("currentemblem_id")) as Emblem;

        MagicMod eMod = GameMasterScript.masterMagicModList[value];

        if (playerEmblem == null || eMod == null)
        {
            Debug.Log("WARNING: Bad error, couldn't find player's emblem to add the mod " + value + " or couldn't find the emblem itself.");
        }
        else
        {
            EquipmentBlock.MakeMagicalFromMod(playerEmblem, eMod, false, false, false);
            if (GameMasterScript.heroPCActor.myEquipment.IsEquipped(playerEmblem))
            {
                GameMasterScript.heroPCActor.myEquipment.UnequipByReference(playerEmblem);
                GameMasterScript.heroPCActor.myEquipment.Equip(playerEmblem, SND.SILENT, EquipmentSlots.EMBLEM, false);
            }
            StringManager.SetTag(0, playerEmblem.displayName);
            GameLogScript.LogWriteStringRef("log_addemblem_power");
        }

        return false;
    }

    public static bool TryPetGrooming(string value)
    {
        int i = 0;
        int.TryParse(value, out i);

        MonsterCorralScript.singleton.GroomMonster(i);

        UIManagerScript.CloseDialogBox();

        return false;
    }
}
