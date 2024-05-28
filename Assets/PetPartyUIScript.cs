using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum UICommandArgument { REFRESH, DONTREFRESH, EXPAND, DONTEXPAND, COUNT }

public class PetPartyUIScript : MonoBehaviour {

    public static PetPartyUIScript singleton;
    public static bool tabIsExpanded;
    GameObject expandUIButton;
    GameObject collapseUIButton;
    GameObject petInfoBox;

    public List<Monster> fightersInParty;
    public List<HUDPetReadout> petReadoutButtons;

    static bool partyDirty; // if true, the contents of the party changed and must be fully refreshed
    static int partyCountLastRefresh = 0;

    private bool bHasControllerFocus;   //when this is true, the game is paused and controller input goes here.
    public bool HasFocus() {  return bHasControllerFocus; }
    private int  iSelectedPetIdx;       //which pet in the list are we looking at?
    const int MAX_PETS_DISPLAYABLE = 6;

    // Use this for initialization
    void Start () {
        tabIsExpanded = true;
        expandUIButton = GameObject.Find("ExpandHUDPetUI");
        collapseUIButton = GameObject.Find("CollapseHUDPetUI");
        petReadoutButtons = new List<HUDPetReadout>();
        fightersInParty = new List<Monster>();
        singleton = this;
        petInfoBox = GameObject.Find("PetInfoBox");
        petInfoBox.transform.localScale = new Vector3(1f, 1f, 1f);

    }

    public IEnumerator WaitThenTryRefreshParty(float time)
    {
        yield return new WaitForSeconds(time);
        RefreshContentsOfPlayerParty();
    }

    public static void RefreshContentsOfPlayerParty(UICommandArgument uiArg = UICommandArgument.COUNT, bool debug = false)
    {

#if UNITY_EDITOR
        debug = true;
#endif

        /* if (Debug.isDebugBuild)
        {
            if (debug) Debug.Log("Refreshing contents of player party " + uiArg);
        }  */

        //if (!tabIsExpanded && singleton.fightersInParty.Count > 0) return;

        // First, make sure we have the appropriate number of objects in our list of objects. Disable unused ones, or create new ones as needed.
        // It's auto-pooling!

        int countPartyMembers = 0;
        singleton.fightersInParty.Clear();

        Actor act;
        for (int i = 0; i < GameMasterScript.heroPCActor.summonedActors.Count; i++)
        {
            act = GameMasterScript.heroPCActor.summonedActors[i];
            if (act.GetActorType() == ActorTypes.MONSTER)
            {
                Monster m = act as Monster;
                if (m.myStats.IsAlive() && !m.destroyed)
                {
                    if (MapMasterScript.activeMap.FindActorByID(m.actorUniqueID) == null)
                    {
                        Debug.Log("WARNING: Player pet " + m.actorRefName + " " + m.actorUniqueID + " not in map!");                        
                        continue;
                    }
                    singleton.fightersInParty.Add(m);
                    countPartyMembers++;
                    //Debug.Log("Not alive or destroyed? " + m.destroyed);
                }
            }
        }

        if (countPartyMembers != partyCountLastRefresh)
        {
            partyDirty = true;
            partyCountLastRefresh = countPartyMembers;
        }

        if (countPartyMembers == 0)
        {
            if (singleton.petInfoBox.activeSelf)
            {
                singleton.petInfoBox.SetActive(false);
            }
            return;
        }
        else
        {
            if (!singleton.petInfoBox.activeSelf)
            {
                singleton.petInfoBox.SetActive(true);
            }            
        }

        //Debug.Log("Num party members: " + countPartyMembers + " sv. current count of " + singleton.petReadoutButtons.Count + " vs max displayable " + MAX_PETS_DISPLAYABLE);

        if (countPartyMembers < singleton.petReadoutButtons.Count)
        {
            for (int i = countPartyMembers; i < singleton.petReadoutButtons.Count; i++)
            {
                if (singleton.petReadoutButtons[i].gameObject.activeSelf)
                {
                    singleton.petReadoutButtons[i].gameObject.SetActive(false);
                }                
            }
        }
        else if (countPartyMembers >= singleton.petReadoutButtons.Count)
        {
            for (int i = singleton.petReadoutButtons.Count; i < countPartyMembers; i++)
            {
                if (i >= MAX_PETS_DISPLAYABLE) continue;
                GameObject newButton = GameObject.Instantiate(GameMasterScript.GetResourceByRef("HUDPetStats"));
                HUDPetReadout newReadout = newButton.GetComponent<HUDPetReadout>();
                newButton.transform.SetParent(singleton.petInfoBox.transform);
                newButton.transform.localScale = new Vector3(1f, 1f, 1f);
                singleton.collapseUIButton.transform.SetSiblingIndex(singleton.collapseUIButton.transform.GetSiblingIndex() + 1);
                singleton.petReadoutButtons.Add(newReadout);
            }
        }

        if (partyDirty)
        {
            singleton.collapseUIButton.transform.SetSiblingIndex(singleton.collapseUIButton.transform.GetSiblingIndex() + 1);
            partyDirty = false;
        }
        

        //Debug.Log("There are " + singleton.petReadoutButtons.Count + " buttons, and " + singleton.fightersInParty.Count + " fighters in party list. Counted members: " + countPartyMembers);

        // Now iterate through and populate the stats, portraits, names etc as needed.
        for (int i = 0; i < countPartyMembers; i++)
        {
            if (i >= MAX_PETS_DISPLAYABLE) continue; // can't display more than X pets            

            if (!singleton.petReadoutButtons[i].gameObject.activeSelf)
            {
                singleton.petReadoutButtons[i].gameObject.SetActive(true);
            }
            
            if (singleton.petReadoutButtons[i].attachedMonster != singleton.fightersInParty[i] 
                || singleton.petReadoutButtons[i].attachedMonster.displayName != singleton.fightersInParty[i].displayName
                || singleton.fightersInParty[i].recentlyNamedMonster)
            {
                singleton.fightersInParty[i].recentlyNamedMonster = false;
                singleton.petReadoutButtons[i].attachedMonster = singleton.fightersInParty[i];
                Sprite refSprite = null;
                try
                {
                    refSprite = singleton.fightersInParty[i].myAnimatable.myAnimations[0].mySprites[0].mySprite;
                    singleton.petReadoutButtons[i].monsterSprite.GetComponent<RectTransform>().sizeDelta = new Vector2(refSprite.rect.width, refSprite.rect.height);
                    singleton.petReadoutButtons[i].monsterSprite.color = Color.white;
                }
                catch(Exception e)
                {
                    Debug.Log("Fighter " + singleton.fightersInParty[i].actorRefName + " " + singleton.fightersInParty[i].actorUniqueID + " issue getting animatable.");
                    singleton.petReadoutButtons[i].monsterSprite.color = UIManagerScript.transparentColor;
                    singleton.StartCoroutine(singleton.WaitThenTryRefreshParty(0.25f));
                }
                
                singleton.petReadoutButtons[i].monsterSprite.sprite = refSprite;                
                singleton.petReadoutButtons[i].nameText.text = singleton.fightersInParty[i].displayName;
            }

            // Health, turns readouts

            if (singleton.fightersInParty[i].turnsToDisappear > 0)
            {
                string extraSpace = "";
                if (StringManager.DoesCurrentLanguageUseSpaces())
                {
                    extraSpace = " ";
                }
                singleton.petReadoutButtons[i].turnsText.text = singleton.fightersInParty[i].turnsToDisappear + extraSpace + StringManager.GetString("misc_turns");
            }
            else
            {
                singleton.petReadoutButtons[i].turnsText.text = "";
            }

            int minHealth = (int)singleton.fightersInParty[i].myStats.GetCurStat(StatTypes.HEALTH);
            int maxHealth = (int)singleton.fightersInParty[i].myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
            float healthAmt = singleton.fightersInParty[i].myStats.GetCurStat(StatTypes.HEALTH) / singleton.fightersInParty[i].myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);

            singleton.petReadoutButtons[i].healthBar.fillAmount = healthAmt;

            // We can just use "HP" for all languages.
            string healthText = minHealth + "/" + maxHealth + "HP";

            if (singleton.fightersInParty[i].actorUniqueID == GameMasterScript.heroPCActor.GetMonsterPetID())
            {
                // Insured pets would never have a turn countdown, so we can put the Insured text in the Turns text area
                if (GameMasterScript.heroPCActor.ReadActorData("petinsurance") == 1)
                {
                    if (StringManager.gameLanguage == EGameLanguage.en_us)
                    {
                        singleton.petReadoutButtons[i].turnsText.text = StringManager.GetString("ui_insured_abbr");
                    }
                    else
                    {
                        singleton.petReadoutButtons[i].turnsText.text = StringManager.GetString("ui_insured");
                    }
                    
                }
            }

            singleton.petReadoutButtons[i].healthText.text = healthText;
        }

        if (tabIsExpanded)
        {
            if (uiArg != UICommandArgument.DONTEXPAND)
            {
                singleton.ExpandPetPartyUI(UICommandArgument.DONTREFRESH);
            }            
        }
        else
        {
            singleton.CollapsePetPartyUI();
        }

    }

    public void OnClick_ExpandPetPartyUI()
    {
        ExpandPetPartyUI();
    }

    public void ExpandPetPartyUI(UICommandArgument uiArg = UICommandArgument.COUNT)
    {
        tabIsExpanded = true;
        if (expandUIButton.activeSelf)
        {
            expandUIButton.SetActive(false);
        }
        if (!collapseUIButton.activeSelf)
        {
            collapseUIButton.SetActive(true);
        }        

        for (int i = 0; i < fightersInParty.Count; i++)
        {
            if (i >= MAX_PETS_DISPLAYABLE) continue;
            petReadoutButtons[i].gameObject.SetActive(true);
        }
        petReadoutButtons.Sort( (a, b) => a.gameObject.activeInHierarchy ? -1 : 1 );

        if (uiArg != UICommandArgument.DONTREFRESH)
        {
            RefreshContentsOfPlayerParty(UICommandArgument.DONTEXPAND);
        }
        
    }

    public void CollapsePetPartyUI()
    {
        tabIsExpanded = false;
        expandUIButton.SetActive(true);
        collapseUIButton.SetActive(false);
        foreach(HUDPetReadout hpr in petReadoutButtons)
        {
            hpr.gameObject.SetActive(false);
        }
    }    

    public static void StartPetBehaviorConversationFromRef(Actor act)
    {
        UIManagerScript.singletonUIMS.CloseExamineMode();

        Monster mn = act as Monster;

        Conversation petConvo = GameMasterScript.FindConversation("pet_behavior_dialog");
        TextBranch mainTB = petConvo.allBranches[0];
        mainTB.responses.Clear();

        // What behaviors can a pet have?

        if (mn.cachedBattleData.maxMoveRange > 0)
        {
            if (act.ReadActorData("anchor_range") != 1)
            {
                ButtonCombo followClose = new ButtonCombo();
                followClose.dbr = DialogButtonResponse.CONTINUE;
                followClose.dialogEventScript = "SetPetBehavior";
                followClose.dialogEventScriptValue = "anchor_range|1";
                followClose.buttonText = StringManager.GetString("pet_behavior_follow_close");
                //StringManager.SetTag(1, followClose.buttonText);
                mainTB.responses.Add(followClose);
            }
            else
            {
                ButtonCombo wander = new ButtonCombo();
                wander.dbr = DialogButtonResponse.CONTINUE;
                wander.dialogEventScript = "SetPetBehavior";
                wander.dialogEventScriptValue = "anchor_range|3";
                wander.buttonText = StringManager.GetString("pet_behavior_follow_wander");
                //StringManager.SetTag(1, wander.buttonText);
                mainTB.responses.Add(wander);
            }
        }

        if (act.ReadActorData("pet_no_abilities") != 1)
        {
            ButtonCombo useAbilitiesNo = new ButtonCombo();
            useAbilitiesNo.dbr = DialogButtonResponse.CONTINUE;
            useAbilitiesNo.dialogEventScript = "SetPetBehavior";
            useAbilitiesNo.dialogEventScriptValue = "pet_no_abilities|1";
            useAbilitiesNo.buttonText = StringManager.GetString("pet_behavior_abilities_no");
            //StringManager.SetTag(1, useAbilitiesNo.buttonText);
            mainTB.responses.Add(useAbilitiesNo);
        }
        else
        {
            ButtonCombo useAbilitiesYes = new ButtonCombo();
            useAbilitiesYes.dbr = DialogButtonResponse.CONTINUE;
            useAbilitiesYes.dialogEventScript = "SetPetBehavior";
            useAbilitiesYes.dialogEventScriptValue = "pet_no_abilities|0";
            useAbilitiesYes.buttonText = StringManager.GetString("pet_behavior_abilities_yes");
            //StringManager.SetTag(1, useAbilitiesYes.buttonText);
            mainTB.responses.Add(useAbilitiesYes);
        }

        if (mn.cachedBattleData.maxMoveRange > 0)
        {
            ButtonCombo callToPlayer = new ButtonCombo();

            //check if PET can be called by PLAYER
            StatusEffect callCooldown = mn.myStats.GetStatusByRef("status_pet_call");
            if (callCooldown != null)
            {
                int iDuration = (int) callCooldown.curDuration;
                callToPlayer.actionRef = "call_pet_to_player_on_cooldown";
                callToPlayer.dbr = DialogButtonResponse.CONTINUE;
                callToPlayer.dialogEventScript = "CallPetToPlayerOnCooldown";
                StringManager.SetTag(0, iDuration.ToString());
                callToPlayer.buttonText = StringManager.GetString("pet_behavior_cometome_cooldown");
            }
            else
            {
                callToPlayer.actionRef = "call_pet_to_player";
                callToPlayer.dbr = DialogButtonResponse.CONTINUE;
                callToPlayer.dialogEventScript = "CallPetToPlayer";
                callToPlayer.buttonText = StringManager.GetString("pet_behavior_cometome");
            }
            mainTB.responses.Add(callToPlayer);
        }


        if (!act.actorRefName.Contains("runiccrystal"))
        {
            ButtonCombo petAttackToggle = new ButtonCombo();
            petAttackToggle.actionRef = "pet_attack";
            petAttackToggle.dbr = DialogButtonResponse.CONTINUE;
            petAttackToggle.dialogEventScript = "TogglePetAttackBehavior";
            if (act.ReadActorData("pet_no_attack") != 1) //
            {
                petAttackToggle.buttonText = StringManager.GetString("pet_behavior_attack_no");
            }
            else
            {
                petAttackToggle.buttonText = StringManager.GetString("pet_behavior_attack_yes");
            }

            mainTB.responses.Add(petAttackToggle);

            ButtonCombo petInitiateCombat = new ButtonCombo();
            petInitiateCombat.actionRef = "pet_initiate_combat";
            petInitiateCombat.dbr = DialogButtonResponse.CONTINUE;
            petInitiateCombat.dialogEventScript = "PetInitiateCombatTargeting";
            petInitiateCombat.buttonText = StringManager.GetString("misc_pet_attack");

            mainTB.responses.Add(petInitiateCombat);
        }

        // Dismiss summons at once?
        if (mn != GameMasterScript.heroPCActor.GetMonsterPet())
        {
            ButtonCombo dismiss = new ButtonCombo();
            dismiss.actionRef = "dismiss_pet";
            dismiss.dbr = DialogButtonResponse.CONTINUE;
            dismiss.dialogEventScript = "DismissPet";
            //dismiss.dialogEventScriptValue = "dimiss_pet|1";
            dismiss.buttonText = StringManager.GetString("pet_behavior_dismiss");
            mainTB.responses.Add(dismiss);
        }
        else
        {
            if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && !GameMasterScript.heroPCActor.myStats.CheckHasStatusName("pet_duel")) // duel strings not translated yet
            {
                ButtonCombo roughhouse = new ButtonCombo();
                roughhouse.actionRef = "fight_pet";
                roughhouse.dbr = DialogButtonResponse.CONTINUE;
                roughhouse.dialogEventScript = "DuelWithPet";
                roughhouse.buttonText = StringManager.GetString("pet_roughhouse");
                mainTB.responses.Add(roughhouse);
            }
        }

        ButtonCombo exit = new ButtonCombo();
        exit.actionRef = "exit";
        exit.dbr = DialogButtonResponse.EXIT;
        exit.buttonText = StringManager.GetString("misc_button_exit_normalcase");
        mainTB.responses.Add(exit);

        StringManager.SetTag(0, act.displayName);
        UIManagerScript.StartConversation(petConvo, DialogType.STANDARD, null);
        GameMasterScript.gmsSingleton.SetTempGameData("pet_behavior_convo", act.actorUniqueID);
    }

    public void OnLoseFocus()
    {
        bHasControllerFocus = false;
        UIManagerScript.HideDialogMenuCursor();
    }

    public void OnGetFocus()
    {
        //if we have no pets, then don't do this.
        if (petReadoutButtons.Count < 1)
        {
            return;
        }

        bHasControllerFocus = true;
        HighlightSelectedPetReadout();
    }

    /// <summary>
    /// Uses the iSelectedPetIdx to put the cursor somewhere.
    /// <returns>The selected index after clamping.</returns>
    /// </summary>
    int HighlightSelectedPetReadout()
    {
        ClampIndex();
        HUDPetReadout hpr = petReadoutButtons[iSelectedPetIdx];
        var uims = UIManagerScript.singletonUIMS;

        //force the cursor to show up, it doesn't want to otherwise.
        uims.EnableCursor(bForceDisplay:true);
        uims.uiDialogMenuCursor.transform.SetParent(hpr.gameObject.transform);
        UIManagerScript.AlignCursorPos(hpr.gameObject, -5.0f, 0f, false);

        //color effect
        hpr.nameText.color = Color.yellow;
        LeanTween.color(hpr.nameText.transform as RectTransform, Color.white, 1.0f).
            setEase(LeanTweenType.easeInBack).
            setOnUpdate( c => hpr.nameText.color = c);

        //boop
        var rt = hpr.monsterSprite.rectTransform;
        rt.localScale = new Vector3(1.2f,1.2f,1f);
        LeanTween.scale(rt, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBounce);

        //here's the pet we are looking at.
        return iSelectedPetIdx;
    }

    /// <summary>
    /// Keep the pointer locked between 0 and the last enabled button.
    /// </summary>
    void ClampIndex()
    {
        iSelectedPetIdx = Mathf.Max(0, Mathf.Min(iSelectedPetIdx, petReadoutButtons.Count - 1));
        while (iSelectedPetIdx >0  && !petReadoutButtons[iSelectedPetIdx].gameObject.activeInHierarchy)
        {
            iSelectedPetIdx--;
        }
        
    }

    /// <summary>
    /// Takes over controller input when this UI is selected.
    /// </summary>
    public void HandleInput()
    {
        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;

        //close?
        if (playerInput.GetButtonDown("Cancel"))
        {
            UIManagerScript.PlayCursorSound("Cancel");
            OnLoseFocus();
            return;
        }

        //select pet?
        if (playerInput.GetButtonDown("Confirm"))
        {
            //Sound OpenDialog will play, so don't play a sound here.
            petReadoutButtons[iSelectedPetIdx].StartConversation();
            OnLoseFocus();
            return;
        }

        int iOldIdx = iSelectedPetIdx;
        bool inputGet = false;
        //up, down
        if (playerInput.GetButtonRepeating("Move Up") || playerInput.GetButtonRepeating("RadialUp"))
        {
            iSelectedPetIdx--;
            inputGet = true;
        }
        else if (playerInput.GetButtonRepeating("Move Down")|| playerInput.GetButtonRepeating("RadialDown"))
        {
            iSelectedPetIdx++;
            inputGet = true;
        }

        //if we moved, make a noise and move the cursor
        if( HighlightSelectedPetReadout() != iOldIdx )
        {
            UIManagerScript.PlayCursorSound("Move");
        }
        else if (inputGet)
        {
            UIManagerScript.PlayCursorSound("UITock");
        }
    }

    /// <summary>
    /// Ends duel with the player's pet m
    /// </summary>
    /// <param name="m"></param>
    public static void EndPetDuel(Monster m)
    {
        if (m == null)
        {
            return;
        }
        if (Debug.isDebugBuild) Debug.Log("Time to end the duel.");
        GameMasterScript.heroPCActor.myStats.RemoveAllTemporaryEffects();
        m.myStats.RemoveAllTemporaryEffects();
        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("monsterundying_temp");
        GameMasterScript.heroPCActor.myStats.ForciblyRemoveStatus("pet_duel");
        m.myStats.ForciblyRemoveStatus("monsterundying_temp");
        GameLogScript.LogWriteStringRef("log_duel_stop");

        GameMasterScript.heroPCActor.myStats.RemoveTemporaryNegativeStatusEffects();
        m.myStats.RemoveTemporaryNegativeStatusEffects();

        m.actorfaction = Faction.PLAYER;
        m.bufferedFaction = Faction.PLAYER;
        m.RemoveTarget(GameMasterScript.heroPCActor);
        GameMasterScript.heroPCActor.RemoveTarget(m);

        float hpBeforeDuel = GameMasterScript.heroPCActor.ReadActorData("hp_preduel");
        GameMasterScript.heroPCActor.myStats.SetStat(StatTypes.HEALTH, hpBeforeDuel, StatDataTypes.CUR, true);

        hpBeforeDuel = m.ReadActorData("hp_preduel");
        m.myStats.SetStat(StatTypes.HEALTH, hpBeforeDuel, StatDataTypes.CUR, true);

        m.destroyed = false;        

        UIManagerScript.RefreshPlayerStats();

        PetPartyUIScript.RefreshContentsOfPlayerParty(UICommandArgument.REFRESH);

        if (m.summonedActors != null)
        {
            foreach (Actor act in m.summonedActors)
            {
                if (act == null) continue;
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster petMon = act as Monster;
                    petMon.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, true);
                }
                GameMasterScript.AddToDeadQueue(act, true);
            }
        }

        GameMasterScript.gmsSingleton.ProcessDeadQueue(MapMasterScript.activeMap);
    }
}

