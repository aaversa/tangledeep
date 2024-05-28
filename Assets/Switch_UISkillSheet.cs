using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Globalization;

public enum ESkillSheetMode
{
    assign_abilities = 0,
    purchase_abilities,
    wild_child_abilities,
}

public class Switch_UISkillSheet : ImpactUI_Base
{
    [Header("Info columns")]
    public Switch_UIButtonColumn leftColumn;
    [HideInInspector]
    public Switch_UIButtonColumn rightColumn;
    [Tooltip("A second column is spawned dynamically and placed here")]
    public GameObject anchorRightColumn;

    [Header("Hotbar")]
    public GameObject hotbar_vertical;
    public int NumVerticalHotbarButtons;
    public int PixelsBetweenButtons_Hotbar;
    public Switch_InvItemButton btnSwapButton;
    private List<Switch_InvVerticalHotbarButton> listVerticalHotbarButtons;
    private static float fLastHotbarSwapTime;

    [Header("Mode Change Row")]
    public Switch_InvItemButton ModeChangeButton;
    public List<ESkillSheetMode> list_modeTypes;
    public int PixelsBetweenButtons_ModeChange;
    private List<Switch_InvItemButton> listModeChangeButtons;

    [Header("Selected Ability/Job Info")]
    public Image image_SelectedObject;
    public TextMeshProUGUI txt_SelectedObjectName;
    public TextMeshProUGUI txt_SelectedObjectInfo;

    private List<JobAbility> listJobAbilities;
    private List<AbilityScript> listActiveAbilities;
    private List<AbilityScript> listSupportAbilities;

    private ESkillSheetMode sheetMode;

    private AbilitySortTypes activeSortType = AbilitySortTypes.JOB;
    private AbilitySortTypes passiveSortType = AbilitySortTypes.PASSIVEEQUIPPED;

    private List<AbilityScript> currentEquippedPassives;

    private Switch_InvItemButton lastFocusedButton;

    public override void Awake()
    {
        base.Awake();
        myTabType = UITabs.SKILLS;
        currentEquippedPassives = new List<AbilityScript>();
    }


    public override void Start()
    {
        base.Start();
        InitializeDynamicUIComponents();
        FontManager.LocalizeMe(txt_SelectedObjectInfo, TDFonts.WHITE);
        FontManager.LocalizeMe(txt_SelectedObjectName, TDFonts.WHITE);
    }

    public override UIManagerScript.UIObject GetDefaultUiObjectForFocus()
    {
        return listModeChangeButtons[0].myUIObject;
    }

    public override void OnClickSwapHotbars(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        SwapVerticalHotbarList(listVerticalHotbarButtons);
    }

    public override void SwapHotbarViaGamepad()
    {
        SwapVerticalHotbarList(listVerticalHotbarButtons);
    }

    public override bool HandleInput(Directions dInput)
    {
        if (base.HandleInput(dInput))
        {
            return true;
        }

        Rewired.Player playerInput = GameMasterScript.gmsSingleton.player;

        if (leftColumn.HandleScrollInput(playerInput, dInput) || rightColumn.HandleScrollInput(playerInput, dInput))
        {
            return true;
        }


        // #todo Hotbar slot keys 1-8 are checked to hotkey ability you're hovering on.  
        
        // 1/9/18 AA change - Let's allow this in any mode, as long as you KNOW the skill
        //if (sheetMode == ESkillSheetMode.assign_abilities)
        {
            Switch_InvItemButton selectedButton = leftColumn.GetSelectedButtonInList();

            if (selectedButton != null)
            {
                bool tryAssign = false;
                if (sheetMode == ESkillSheetMode.purchase_abilities)
                {
                    JobAbility abil = selectedButton.GetContainedData() as JobAbility;
                    if (abil != null && !abil.ability.passiveAbility)
                    {
                        if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abil.abilityRef))
                        {
                            tryAssign = true;
                        }
                    }
                }
                else
                {
                    tryAssign = true;
                }

                // If we have the "Job Specialist" game modifier enabled, we can only modify hotbars in town/safe areas.
                bool gameModifierLimited = !GameModifiersScript.CanUseAbilitiesOutsideOfHotbar()
                    && !(MapMasterScript.activeMap.IsTownMap() || MapMasterScript.activeMap.dungeonLevelData.safeArea) 
                    && !RandomJobMode.IsCurrentGameInRandomJobMode();

                if (!PlatformVariables.GAMEPAD_ONLY && tryAssign)
                {
                    for (int t = 0; t < 8; t++)
                    {
                        if (playerInput.GetButtonDown("Use Hotbar Slot " + (t + 1)))
                        {
                            if (gameModifierLimited)
                            {
                                GameModifiersScript.PlayerTriedToAlterSkills();
                                UIManagerScript.PlayCursorSound("Error");
                                if (GameModifiersScript.CheckForSwitchAbilitiesTutorialPopup())
                                {
                                    UIManagerScript.ForceCloseFullScreenUIWithNoFade();
                                }
                            }
                            else if (AssignObjectToHotbarViaKeypress(selectedButton.GetContainedData(), t))
                            {
                                return true;
                            }
                        }                            
                    }
                }       
            }           
        } 

        return false;
    }

    public override bool AssignObjectToHotbarViaKeypress(ISelectableUIObject obj, int iHotbarIdx)
    {
        Switch_InvVerticalHotbarButton buttonTarget = listVerticalHotbarButtons[iHotbarIdx];
        buttonTarget.SetContentAndAddToHotbar(obj);
        UpdateContent();
        return true;
    }


    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        //make a right column
        GameObject go = Instantiate(leftColumn.gameObject, leftColumn.transform.parent);
        go.transform.position = anchorRightColumn.transform.position;
        rightColumn = go.GetComponent<Switch_UIButtonColumn>();

        //wake the columns up
        leftColumn.CreateStartingContent(0);
        rightColumn.CreateStartingContent(1);

        leftColumn.SetActionForEventOnButtons(EventTriggerType.PointerEnter, SetTooltipViaButtonByID);
        rightColumn.SetActionForEventOnButtons(EventTriggerType.PointerEnter, SetTooltipViaButtonByID);

        leftColumn.onCursorPositionInListUpdated = OnColumnUpdateFocus;
        rightColumn.onCursorPositionInListUpdated = OnColumnUpdateFocus;

        leftColumn.ConnectBottomButtonsToOtherColumn(rightColumn, Directions.EAST);

        //create the vertical buttons
        listVerticalHotbarButtons = CreateVerticalHotbar(hotbar_vertical, NumVerticalHotbarButtons, PixelsBetweenButtons_Hotbar, btnSwapButton, SwapHotbarViaGamepad);

        //horizontal mode bar
        listModeChangeButtons = CreateListOfButtons(ModeChangeButton, list_modeTypes, new Vector2(1, 0), PixelsBetweenButtons_ModeChange, OnSubmit_ModeChange);
        MakeListOfNeighbors(listModeChangeButtons, Directions.WEST, Directions.EAST);

        // In German, reduce character spacing for mode buttons.
        if (StringManager.gameLanguage == EGameLanguage.de_germany)
        {
            foreach(Switch_InvItemButton btn in listModeChangeButtons)
            {
                btn.GetTMPro().characterSpacing = 0;
            }
        }

        //hide the last top button for now
        //listModeChangeButtons[listModeChangeButtons.Count - 1].gameObject.SetActive(false);

        //attach columns to other buttons above and below
        leftColumn.neighborTop = listModeChangeButtons[0].myUIObject;
        rightColumn.neighborTop = listModeChangeButtons[1].myUIObject;

        leftColumn.neighborRight = rightColumn.GetTopUIObject();
        rightColumn.neighborLeft = leftColumn.GetTopUIObject();

        leftColumn.neighborLeft = listVerticalHotbarButtons[0].myUIObject;

        //Make sure all the buttons drop to the first column
        listModeChangeButtons[0].myUIObject.neighbors[(int)Directions.SOUTH] = leftColumn.GetTopUIObject();
        listModeChangeButtons[1].myUIObject.neighbors[(int)Directions.SOUTH] = leftColumn.GetTopUIObject();

        listModeChangeButtons[0].GetTMPro().text = StringManager.GetString("ui_skillsheet_setabilities").ToUpperInvariant();
        listModeChangeButtons[1].GetTMPro().text = StringManager.GetString("ui_skillsheet_learnabilities").ToUpperInvariant();

        //vertical hotbar, set right on gamepad to cancel
        foreach (Switch_InvVerticalHotbarButton vbtn in listVerticalHotbarButtons)
        {
            UIManagerScript.UIObject obj = vbtn.myUIObject;
            obj.directionalActions[(int)Directions.EAST] = LeaveVerticalHotbarViaGamepad;
            obj.myOnSelectAction = vbtn.OnSelectAction_FocusOnMe;
        }

        //connect top of hotbar to skills
        listVerticalHotbarButtons[0].myUIObject.neighbors[(int) Directions.NORTH] = listModeChangeButtons[0].myUIObject;

        //connect bottom to the Switch button
        listVerticalHotbarButtons[ listVerticalHotbarButtons.Count - 1  ].myUIObject.neighbors[(int)Directions.SOUTH] = btnSwapButton.myUIObject;
        btnSwapButton.myUIObject.neighbors[(int)Directions.NORTH] = listVerticalHotbarButtons[listVerticalHotbarButtons.Count - 1].myUIObject;

        bHasBeenInitialized = true;
        return true;
    }

    void UpdateVerticalHotbar()
    {
        foreach (Switch_InvVerticalHotbarButton vb in listVerticalHotbarButtons)
        {
            vb.UpdateInformation(true);            

        }
    }

    public void EnterNewMode(ESkillSheetMode newMode, bool bForceUpdate = false)
    {
        //Don't change anything if we are in the same mode unless we ask to
        if (newMode == sheetMode && !bForceUpdate)
        {
            return;
        }

        //Beep boop
        if (newMode != sheetMode)
        {
            UIManagerScript.PlayCursorSound("Organize");
        }        

        //Keep track of the value
        sheetMode = newMode;

        UpdateContent(); // this is new as of 1/15/18, necessary to update navigation in list

        //repopulate the vertical hotbar
        UpdateVerticalHotbar();

        //Clear the ability info when we swap
        ClearSelectedAbility();

        UpdateVerticalHotbar();
        listModeChangeButtons[0].ToggleButton(false);
        listModeChangeButtons[1].ToggleButton(false);

        switch (sheetMode)
        {
            case ESkillSheetMode.assign_abilities:
                OpenAssignAbilitiesMode();
                listModeChangeButtons[0].ToggleButton(true);
                break;
            case ESkillSheetMode.purchase_abilities:
                OpenPurchaseAbilitiesMode();
                listModeChangeButtons[1].ToggleButton(true);
                break;
            case ESkillSheetMode.wild_child_abilities:
                break;
        }
    }

    //Fills these ability lists for both the Open and Update functions of Abilities Mode
    void FillActiveAndSupportAbilities()
    {
        HeroPC hero = GameMasterScript.heroPCActor;
        if (listActiveAbilities == null)
        {
            listActiveAbilities = new List<AbilityScript>();
            listSupportAbilities = new List<AbilityScript>();
        }

        List<AbilityScript> tempActive = new List<AbilityScript>();
        List<AbilityScript> tempPassive = new List<AbilityScript>();

        //calculate purchased abilities, active + passive
        foreach (AbilityScript abil in hero.myAbilities.abilities)
        {
            //don't grab stuff we won't show
            if (!abil.displayInList)
            {
                continue;
            }
            if (!abil.passiveAbility)
            {
                tempActive.Add(abil);
            }
            else
            {
                tempPassive.Add(abil);
            }
        }

        // Skill list is marked dirty if the player has learned or unlearned a skill for any reason
        // In this case, we need to rebuild the list

        if (GameMasterScript.heroPCActor.myAbilities.CheckIfDirty())
        {
            listActiveAbilities = tempActive;
            listSupportAbilities = tempPassive;
        }

        GameMasterScript.heroPCActor.myAbilities.SetDirty(false);

        currentEquippedPassives = listSupportAbilities.Where(a => a.UsePassiveSlot && a.passiveEquipped && !a.CheckAbilityTag(AbilityTags.DRAGONSOUL)).ToList();
    }

    void SortAbilityList(List<AbilityScript> abilities, AbilitySortTypes sortType)
    {        
        switch (sortType)
        {
            case AbilitySortTypes.ALPHA:
                // We must be specific about exactly how the sort executes, since some cultures
                // have multiple sort types etc. This should be the best all-around set of parameters.
                CultureInfo culture = StringManager.GetCurrentCulture();
                StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase;

                abilities.Sort((a, b) => string.Compare(a.GetSortableName(), b.GetSortableName(), stringComparison));
                GameMasterScript.heroPCActor.SetActorData("last_activeskill_sort", (int)AbilitySortTypes.ALPHA);
                GameMasterScript.gmsSingleton.SetTempGameData("playersort_abils", 1);
                break;
            case AbilitySortTypes.JOB:
                abilities.Sort((a, b) =>
                {
                    int retJobber = a.jobLearnedFrom.CompareTo(b.jobLearnedFrom);
                    return retJobber == 0 ? string.Compare(a.abilityName, b.abilityName) : retJobber;
                });
                GameMasterScript.heroPCActor.SetActorData("last_activeskill_sort", (int)AbilitySortTypes.JOB);
                GameMasterScript.gmsSingleton.SetTempGameData("playersort_abils", 1);
                break;
            case AbilitySortTypes.PASSIVEEQUIPPED:
                abilities.Sort((a, b) =>
                {
                    int retEquipped = b.passiveEquipped.CompareTo(a.passiveEquipped);
                    int retNoSlot = b.UsePassiveSlot.CompareTo(a.UsePassiveSlot);
                    //fired
                    return retEquipped == 0 ?
                               retNoSlot == 0 ?
                                    a.jobLearnedFrom.CompareTo(b.jobLearnedFrom)
                                    : retNoSlot
                               : retEquipped;
                });
                break;
        }
        UpdateContent();
    }

    void OpenAssignAbilitiesMode()
    {
        FillActiveAndSupportAbilities();

        //since we are just opening for the first time, sort them
        if (listSupportAbilities.Count > 0)
        {
            SortAbilityList(listSupportAbilities, passiveSortType);
            //since we sorted the main list, sort the currentEquippedList so the numbers are correct.
            currentEquippedPassives = listSupportAbilities.Where(a => a.UsePassiveSlot && a.passiveEquipped && !a.CheckAbilityTag(AbilityTags.DRAGONSOUL)).ToList();
        }


        //put them in the list -- column one is active, column two is passive
        leftColumn.SetLabel(StringManager.GetString("ui_label_activeskills"));
        if (listActiveAbilities.Count == 0)
        {
            //no abilities yet! 
            leftColumn.SetTextInfoDisplayMode(StringManager.GetString("skill_sheet_no_abilities_active"));
        }
        else
        {
            if (GameMasterScript.gmsSingleton.ReadTempGameData("playersort_abils") != 1)
            {
                if (GameMasterScript.heroPCActor.ReadActorData("last_activeskill_sort") >= 0)
                {
                    SortAbilityList(listActiveAbilities, (AbilitySortTypes)GameMasterScript.heroPCActor.ReadActorData("last_activeskill_sort"));
                }
            }

            leftColumn.SetButtonDisplayMode();
            leftColumn.PlaceObjectsInList(listActiveAbilities.Cast<ISelectableUIObject>().ToList(), bForceResetOffset: false);
            leftColumn.SetActionForEventOnButtons(EventTriggerType.BeginDrag, leftColumn.BeginDragGenericObjectFromButton);
            leftColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, OnSubmit_ActiveSkill);
        }
        leftColumn.SetBottomButtonAction(0, EventTriggerType.PointerClick, OnClick_SortActiveAbilities);
        leftColumn.SetBottomButtonAction(1, EventTriggerType.PointerClick, OnClick_SortActiveAbilities);

        if (StringManager.gameLanguage != EGameLanguage.de_germany)
        {
            leftColumn.SetButtonInfo(0, true, StringManager.GetString("ui_button_job").ToUpperInvariant());
            leftColumn.SetButtonInfo(1, true, StringManager.GetString("ui_button_sortaz").ToUpperInvariant());
        }
        else
        {
            leftColumn.SetButtonInfo(0, true, StringManager.GetString("ui_button_job"));
            leftColumn.SetButtonInfo(1, true, StringManager.GetString("ui_button_sortaz"));
            leftColumn.GetButtonInList(0).GetTMPro().characterSpacing = 0;
            leftColumn.GetButtonInList(1).GetTMPro().characterSpacing = 0;

        }


        rightColumn.SetLabel(StringManager.GetString("ui_passive_abilities"));
        if (listSupportAbilities.Count == 0)
        {
            rightColumn.SetTextInfoDisplayMode(StringManager.GetString("skill_sheet_no_abilities_passive"));
        }
        else
        {
            rightColumn.SetButtonDisplayMode();
            rightColumn.PlaceObjectsInList(listSupportAbilities.Cast<ISelectableUIObject>().ToList());
            rightColumn.AdjustButtonInformationViaAction(Action_SetPassiveAbilitiesAsEquippedOrNot);
            rightColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, OnClick_PassiveSkill);
        }

        rightColumn.SetBottomButtonAction(0, EventTriggerType.PointerClick, OnClick_SortPassiveAbilities);
        rightColumn.SetBottomButtonAction(1, EventTriggerType.PointerClick, OnClick_SortPassiveAbilities);

        if (StringManager.gameLanguage != EGameLanguage.de_germany)
        {
            rightColumn.SetButtonInfo(0, true, StringManager.GetString("ui_button_job").ToUpperInvariant());
            rightColumn.SetButtonInfo(1, true, StringManager.GetString("ui_button_equipped").ToUpperInvariant());
        }
        else
        {
            rightColumn.SetButtonInfo(0, true, StringManager.GetString("ui_button_job"));
            rightColumn.SetButtonInfo(1, true, StringManager.GetString("ui_button_equipped"));
            rightColumn.GetButtonInList(0).GetTMPro().characterSpacing = 0;
            rightColumn.GetButtonInList(1).GetTMPro().characterSpacing = 0;
        }




        AbilitySortTypes aState = AbilitySortTypes.ALPHA;

        if (GameMasterScript.heroPCActor.ReadActorData("last_activeskill_sort") >= 0)
        {
            activeSortType = (AbilitySortTypes)GameMasterScript.heroPCActor.ReadActorData("last_activeskill_sort");
        }

        leftColumn.ForceBottomButtonToggleState(activeSortType == AbilitySortTypes.JOB, activeSortType == AbilitySortTypes.ALPHA);
        rightColumn.ForceBottomButtonToggleState(passiveSortType == AbilitySortTypes.JOB, passiveSortType == AbilitySortTypes.PASSIVEEQUIPPED);
    }

    private string GetStringForJobInnateBonuses()
    {
        HeroPC hero = GameMasterScript.heroPCActor;
        CharacterJobData cjd = GameMasterScript.heroPCActor.myJob;

        //string bonusText = "<size=42><color=yellow>" + StringManager.GetString("ui_job_innate_bonuses") + "</color></size>\n\n";
        //bonusText += "<color=#40b843>" + hero.myJob.displayName + " Passive</color>\n" + cjd.bonusDescription1;

        string bonusText = "<color=#40b843>" + StringManager.GetString("ui_job_innate_bonus1") + "</color>\n" + CustomAlgorithms.ParseRichText(cjd.BonusDescription1, false);

        if (!string.IsNullOrEmpty(cjd.BonusDescription2))
        {
            if (hero.jobJPspent[(int)hero.myJob.jobEnum] < 1000)
            {
                bonusText += "\n\n<color=yellow>" + StringManager.GetString("ui_job_innate_bonus2") + " " + StringManager.GetString("ui_job_bonus2_jp_req") + "</color>\n" + CustomAlgorithms.ParseRichText(cjd.BonusDescription2, false);
            }
            else
            {
                bonusText += "\n\n<color=#40b843>" + StringManager.GetString("ui_job_innate_bonus2") + "</color>\n" + CustomAlgorithms.ParseRichText(cjd.BonusDescription2, false);
                //bonusText += "\n\n<color=#40b843>Advanced Passive</color>\n" + cjd.bonusDescription2;
            }
        }

        if (!string.IsNullOrEmpty(cjd.BonusDescription3))
        {
            if (hero.HasMasteredJob(cjd))
            {
                bonusText += "\n\n<color=#40b843>" + StringManager.GetString("ui_job_innate_bonus3") + "</color>\n" + CustomAlgorithms.ParseRichText(cjd.BonusDescription3, false);
            }
            else
            {
                //bonusText += "\n\n<color=#40b843>MASTER Passive</color>\n" + cjd.bonusDescription3;
                bonusText += "\n\n<color=yellow>" + StringManager.GetString("ui_job_innate_bonus3") + " " + StringManager.GetString("ui_job_bonus3_jp_req") + "</color>\n" + CustomAlgorithms.ParseRichText(cjd.BonusDescription3, false);
            }
        }

        bool anyinfuse = false;

        switch (GameMasterScript.heroPCActor.ReadActorData("infuse1"))
        {
            case GameMasterScript.FLASK_BUFF_ATTACKDEF:
                bonusText += "\n\n<color=yellow><size=40>" + StringManager.GetString("misc_infusions") + "</size></color>\n\n" + StringManager.GetString("effect_infuse_citrus");
                anyinfuse = true;
                break;
            case GameMasterScript.FLASK_HEAL_STAMINAENERGY:
                bonusText += "\n\n<color=yellow><size=40>" + StringManager.GetString("misc_infusions") + "</size></color>\n\n" + StringManager.GetString("effect_infuse_tealeaf");
                anyinfuse = true;
                break;
        }
        switch (GameMasterScript.heroPCActor.ReadActorData("infuse2"))
        {
            case GameMasterScript.FLASK_HEAL_MORE:
                bonusText += "\n" + StringManager.GetString("effect_infuse_basil");
                break;
            case GameMasterScript.FLASK_INSTANT_HEAL:
                bonusText += "\n" + StringManager.GetString("effect_infuse_vanilla");
                break;
        }
        switch (GameMasterScript.heroPCActor.ReadActorData("infuse3"))
        {
            case GameMasterScript.FLASK_BUFF_DODGE:
                bonusText += "\n" + StringManager.GetString("effect_infuse_greenapple");
                break;
            case GameMasterScript.FLASK_HASTE:
                bonusText += "\n" + StringManager.GetString("effect_infuse_caramel");
                break;
        }

        if (GameMasterScript.heroPCActor.ReadActorData("flask_apple_infuse") == 1)
        {
            if (!anyinfuse)
            {
                bonusText += "\n\n<color=yellow><size=40>" + StringManager.GetString("misc_infusions") + "</size></color>\n";
            }
            bonusText += "\n" + StringManager.GetString("effect_infuse_apple");
            anyinfuse = true;
        }

        if (GameMasterScript.heroPCActor.ReadActorData("schematist_infuse") == 1)
        {
            if (!anyinfuse)
            {
                bonusText += "\n\n<color=yellow><size=40>" + StringManager.GetString("misc_infusions") + "</size></color>\n";
            }
            bonusText += "\n" + StringManager.GetString("effect_infuse_schematist");
        }


        return bonusText;

    }


    private void OnClick_SortActiveAbilities(int[] iSubmitValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Organize");
        activeSortType = iSubmitValue[0] == 0 ? AbilitySortTypes.JOB : AbilitySortTypes.ALPHA;
        SortAbilityList(listActiveAbilities, activeSortType);
        UpdateContent();
    }
    private void OnClick_SortPassiveAbilities(int[] iSubmitValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        GameMasterScript.heroPC.GetComponent<AudioStuff>().PlayCue("Organize");
        passiveSortType = iSubmitValue[0] == 0 ? AbilitySortTypes.JOB : AbilitySortTypes.PASSIVEEQUIPPED;
        SortAbilityList(listSupportAbilities, passiveSortType);
        UpdateContent();
    }

    void UpdateAssignAbilitiesMode()
    {
        FillActiveAndSupportAbilities();
        leftColumn.PlaceObjectsInList(listActiveAbilities.Cast<ISelectableUIObject>().ToList(), false);
        rightColumn.PlaceObjectsInList(listSupportAbilities.Cast<ISelectableUIObject>().ToList(), false);

        //Debug.Log(leftColumn.GetNumObjectsInList() + " in left column");

        leftColumn.ForceBottomButtonToggleState(activeSortType == AbilitySortTypes.JOB, activeSortType == AbilitySortTypes.ALPHA);
        rightColumn.ForceBottomButtonToggleState(passiveSortType == AbilitySortTypes.JOB, passiveSortType == AbilitySortTypes.PASSIVEEQUIPPED);
    }

    void OpenPurchaseAbilitiesMode()
    {
        FillJobAbilitiesList();
        leftColumn.SetButtonDisplayMode();
        StringManager.SetTag(0, GameMasterScript.heroPCActor.myJob.DisplayName);
        leftColumn.SetLabel(StringManager.GetString("ui_skillsheet_header_leftcolumn"));
        leftColumn.PlaceObjectsInList(listJobAbilities.Cast<ISelectableUIObject>().ToList(), false);
        leftColumn.SetActionForEventOnButtons(EventTriggerType.PointerClick, OnSubmit_ActiveSkill);
        leftColumn.AdjustButtonInformationViaAction(Action_CheckJobAbilityEligibility);
        leftColumn.SetActionForEventOnButtons(EventTriggerType.BeginDrag, null);


        rightColumn.SetLabel(StringManager.GetString("ui_skillsheet_header_rightcolumn"));
        rightColumn.SetTextInfoDisplayMode(GetStringForJobInnateBonuses());
    }

    void FillJobAbilitiesList()
    {
        HeroPC hero = GameMasterScript.heroPCActor;
        CharacterJobData cjd = hero.myJob;

        //caclulate job abilities
        if (listJobAbilities == null)
        {
            listJobAbilities = new List<JobAbility>();
        }
        listJobAbilities.Clear();

        //grab the ones we can master, but haven't
        foreach (JobAbility abil in cjd.JobAbilities)
        {
            //avoid innate abilities
            //but add all others, and the color of the ability
            //will be modified based on its purchase state and the cost
            if (abil.innate)
            {
                continue;
            }
            if (abil.postMasteryAbility && !GameMasterScript.heroPCActor.HasMasteredJob(GameMasterScript.heroPCActor.myJob))
            {
                continue;
            }
            /*
            //If it can be mastered, but we haven't done so yet, allow it on the list
            if (masterable && !hero.myAbilities.HasMasteredAbility(abil.ability))
            {
                bCanHas = true;
            }

            //otherwise, if it is not innate, and we don't have it, add it to the list
            else if (!hero.myAbilities.HasAbility(abil.ability) && !abil.innate)
            {
                bCanHas = true;
            }

            if (bCanHas)
            {
                listJobAbilities.Add(abil);
            }
            */

            listJobAbilities.Add(abil);

        }
    }

    void UpdatePurchaseAbilitiesMode()
    {
        FillJobAbilitiesList();
        leftColumn.PlaceObjectsInList(listJobAbilities.Cast<ISelectableUIObject>().ToList(), false);
        leftColumn.AdjustButtonInformationViaAction(Action_CheckJobAbilityEligibility);

        rightColumn.SetTextInfoDisplayMode(GetStringForJobInnateBonuses());

    }

    //First value of iArray is the number of passives already equipped in the list. If we are an equipped passive,
    //and we take up a passive slot, increment that value by 1.
    public void Action_SetPassiveAbilitiesAsEquippedOrNot(Switch_InvItemButton btn, int[] iArray, string[] sArray)
    {
        if (btn == null ||
            btn.gameObject.activeSelf == false)
        {
            return;
        }

        ISelectableUIObject containedData = btn.GetContainedData();
        AbilityScript ab = containedData as AbilityScript;
        if (ab == null)
        {
            return;
        }

        if (ab.passiveEquipped)
        {
            if (ab.UsePassiveSlot)
            {
                //Look in the list we stored earlier of equipped passives. If we're in that list, use the number
                bool foundAbil = false;
                for (int t = 0; t < currentEquippedPassives.Count; t++)
                {
                    if (currentEquippedPassives[t].refName == ab.refName)
                    {
                        btn.txtLabel.text = UIManagerScript.greenHexColor + "[" + (t + 1) + "] " + ab.GetNameForUI() + "</color>";
                        foundAbil = true;
                        break;
                    }
                }
                if (!foundAbil && ab.CheckAbilityTag(AbilityTags.DRAGONSOUL))
                {
                    btn.txtLabel.text = UIManagerScript.greenHexColor + "[D] " + ab.GetNameForUI() + "</color>";
                }
            }
            else
            {
                btn.txtLabel.text = UIManagerScript.greenHexColor + "* " + ab.GetNameForUI() + "</color>";
            }
        }

        rightColumn.text_TopLabel.text = StringManager.GetString("ui_passive_abilities") + " " + currentEquippedPassives.Count + " / 4";

    }

    //Make abilities we can't afford into RED text
    public void Action_CheckJobAbilityEligibility(Switch_InvItemButton btn, int[] iArray, string[] sArray)
    {
        if (btn == null ||
            btn.gameObject.activeSelf == false)
        {
            return;
        }

        ISelectableUIObject containedData = btn.GetContainedData();
        JobAbility jb = containedData as JobAbility;
        if (jb == null)
        {
            return;
        }

        HeroPC hero = GameMasterScript.heroPCActor;
        float myJP = GameMasterScript.heroPCActor.jobJP[(int)GameMasterScript.heroPCActor.myJob.jobEnum];

        bool bAlreadyHas = false;

        //If the ability can be mastered, and we have mastered it...
        if (jb.masterCost > 0 && hero.myAbilities.HasMasteredAbility(jb.ability))
        {
            bAlreadyHas = true;
        }
        //...or if it can't be mastered but we have it anyway,
        else if (hero.myAbilities.HasAbility(jb.ability) && !jb.repeatBuyPossible)
        {
            bAlreadyHas = true;
        }

        //If we already have it, list it as purchased
        if (bAlreadyHas)
        {
            btn.txtLabel.text = UIManagerScript.silverHexColor + jb.ability.abilityName + "</color>";
            if (jb.ability.GetCurCooldownTurns() > 0)
            {
                btn.txtLabel.text = "<color=yellow>" + jb.ability.abilityName + " [" + jb.ability.GetCurCooldownTurns() + "t]</color>";
            }
            else if (jb.ability.toggled)
            {
                btn.txtLabel.text = UIManagerScript.greenHexColor + "*" + jb.ability.abilityName + "*</color>";
            }
        }
        //if we cannot afford the job, turn the cost red
        else if (myJP < hero.GetCostForAbilityBecauseWeDoStuffIfWeArentInOurStartingJob(jb) ||
                 myJP < jb.masterCost)
        {
            btn.txtLabel.text = "<color=red>" + jb.ability.abilityName + "</color>";
        }
        //otherwise, display it as normal
        else
        {
            btn.txtLabel.text = jb.ability.abilityName;
        }

    }

    //Make sure we don't jump focus with the mouse when we are in the middle of using
    //KB or Controller put a power in the hotbar
    public override bool AllowFocus(GameObject obj)
    {
        if (!base.AllowFocus(obj))
        {
            return false;
        }

        switch (CursorOwnershipState)
        {
            case EImpactUICursorOwnershipState.normal:
                return true;
            case EImpactUICursorOwnershipState.vertical_hotbar_has_cursor:
                return obj.GetComponent<Switch_InvVerticalHotbarButton>() != null || obj == btnSwapButton.gameObject;
        }

        //?
        return true;
    }

    public override void OnDialogClose()
    {
        //close the submenu if it is still open
        if (CursorOwnershipState == EImpactUICursorOwnershipState.tooltip_has_cursor)
        {
            UIManagerScript.TooltipReleaseCursor();
        }

        //make sure we have control of the cursor again
        if (!UIManagerScript.allUIObjects.Contains(UIManagerScript.uiObjectFocus))
        {
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.GetDefaultUIFocus());
        }

        Update();

    }

    //Looks inside a button for an item or ability to put in a tooltip
    protected override void SetTooltipViaButtonByID(int[] args, Switch_InvItemButton.ELastInputSource inputSource)
    {
        Switch_UIButtonColumn col = args[1] == 0 ? leftColumn : rightColumn;

        //grab the button we passed in
        Switch_InvItemButton btn = col.GetButtonInList(args[0]);
        lastFocusedButton = btn;
        DisplayItemInfo(btn.GetContainedData(), null, false);
        FocusAndBounceButton(btn);
    }

    // Say we learn a skill and want to refresh just the tooltip of (whatever we just learned)
    public void DisplayItemInfoOfLastFocusedButton()
    {
        if (lastFocusedButton == null)
        {
            return;
        }
        DisplayItemInfo(lastFocusedButton.GetContainedData(), null, false);
    }

    public override void UpdateContent(bool adjustSorting = true)
    {
        base.UpdateContent();

        //Debug.Log("Updating content " + sheetMode);

        //repopulate the vertical hotbar
        UpdateVerticalHotbar();
        listModeChangeButtons[0].ToggleButton(false);
        listModeChangeButtons[1].ToggleButton(false);

        switch (sheetMode)
        {
            case ESkillSheetMode.assign_abilities:
                UpdateAssignAbilitiesMode();
                listModeChangeButtons[0].ToggleButton(true);
                break;
            case ESkillSheetMode.purchase_abilities:
                UpdatePurchaseAbilitiesMode();
                listModeChangeButtons[1].ToggleButton(true);
                break;
            case ESkillSheetMode.wild_child_abilities:
                break;
        }

        SetDefaultFocusBecauseSkillSheet();
    }

    //The skill sheet is a weird beast in that the main list might be empty,
    //so we need to check that before we try to assign to the first slot in that list
    void SetDefaultFocusBecauseSkillSheet()
    {
        UIManagerScript.UIObject defaultFocus = leftColumn.GetTopUIObject();
        UIManagerScript.SetDefaultUIFocus(defaultFocus ?? listModeChangeButtons[0].myUIObject);
    }

    /*
    void UpdateContent_AssignAbilities()
    {
        
    }

    void UpdateContent_PurchaseAbilities()
    {
        
    }
    */

    #region UI Callbacks

    public void OnSubmit_ModeChange(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        EnterNewMode((ESkillSheetMode)iValue[0]);

    }

    public void OnClick_PassiveSkill(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        ISelectableUIObject data = rightColumn.GetContainedDataFrom(iValue[0]);
        AbilityScript ab = data as AbilityScript;

        if (ab == null)
        {
            return;
        }

        if (ab.passiveEquipped)
        {
            UIManagerScript.PlayCursorSound("UITock");
            GameMasterScript.heroPCActor.myAbilities.UnequipPassiveAbility(ab);
        }
        else
        {
            if (ab.UsePassiveSlot && ab.CheckAbilityTag(AbilityTags.DRAGONSOUL) && GameMasterScript.heroPCActor.myAbilities.IsDragonSoulEquipped())
            {
                StartCoroutine(UIGenericItemTooltip.LerpTextColor(rightColumn.text_TopLabel, Color.red, Color.white, 0.5f));
                UIManagerScript.PlayCursorSound("Error");
                return;
            }

            //don't allow passive equipping if there are 4 taken slots
            if (ab.UsePassiveSlot && !ab.CheckAbilityTag(AbilityTags.DRAGONSOUL) && GameMasterScript.heroPCActor.NumberOfPassiveSlotsTaken() >= 4)
            {
                StartCoroutine(UIGenericItemTooltip.LerpTextColor(rightColumn.text_TopLabel, Color.red, Color.white, 0.5f));
                UIManagerScript.PlayCursorSound("Error");
                return;
            }

            // During a trial, you cannot equip non-job passives.
            if (JobTrialScript.IsJobTrialActive() && ab.jobLearnedFrom != GameMasterScript.heroPCActor.myJob.jobEnum)
            {
                UIManagerScript.PlayCursorSound("Error");
                return;
            }

            // If we're not in a safe area, don't allow equipping/unequipping of passives
            if (!GameModifiersScript.CanUseAbilitiesOutsideOfHotbar() && sheetMode == ESkillSheetMode.assign_abilities 
                && !SharaModeStuff.IsSharaModeActive() && !RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                // Are we in town? If not, can't mess around with hotbarz

                if (!PlatformVariables.CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR && !MapMasterScript.activeMap.dungeonLevelData.safeArea)
                {
                    GameModifiersScript.PlayerTriedToAlterSkills();
                    UIManagerScript.PlayCursorSound("Error");
                    if (GameModifiersScript.CheckForSwitchAbilitiesTutorialPopup())
                    {
                        UIManagerScript.ForceCloseFullScreenUIWithNoFade();
                    }
                    return;
                }		
            }

            UIManagerScript.PlayCursorSound("UITick");
            GameMasterScript.heroPCActor.myAbilities.EquipPassiveAbility(ab);
        }

        UpdateContent();
    }

    //Move the cursor to the left hotbar and begin the slot ability mode
    public void OnSubmit_ActiveSkill(int[] iValue, Switch_InvItemButton.ELastInputSource inputSource)
    {
        if (!UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            return;
        }

        if (!GameModifiersScript.CanUseAbilitiesOutsideOfHotbar()
            && sheetMode == ESkillSheetMode.assign_abilities && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            // Are we in town? If not, can't mess around with hotbarz
            if (!MapMasterScript.activeMap.dungeonLevelData.safeArea)
            {
                GameModifiersScript.PlayerTriedToAlterSkills();
                UIManagerScript.PlayCursorSound("Error");
                if (GameModifiersScript.CheckForSwitchAbilitiesTutorialPopup())
                {
                    UIManagerScript.ForceCloseFullScreenUIWithNoFade();
                }
                return;
            }            
        }

        //pick up the data from the button we pressed.
        Switch_UIButtonColumn col = iValue[1] == 0 ? leftColumn : rightColumn;
        ISelectableUIObject data = col.GetContainedDataFrom(iValue[0]);

        if (data == null)
        {
            return;
        }

        //If we mean to buy this ability, check elsewhere.
        if (sheetMode == ESkillSheetMode.purchase_abilities)
        {
            OnSelectSkill_PurchaseMode(data, col);
            return;
        }

        bool bShiftIsDown = GameMasterScript.gmsSingleton.player.GetButton("Compare Alternate") ||
                            GameMasterScript.gmsSingleton.player.GetButton("Diagonal Move Only");

        //If we clicked this with the mouse, or if we used a gamepad/keyboard but held shift,
        //we mean to use it in the real world.
        if (inputSource == Switch_InvItemButton.ELastInputSource.mouse || bShiftIsDown)
        {
            if (!GameModifiersScript.CanUseAbilitiesOutsideOfHotbar()
                && !RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                // lol can't use stuff from sheet with this modifier bro
                GameModifiersScript.PlayerTriedToAlterSkills();
                UIManagerScript.PlayCursorSound("Error");
                if (GameModifiersScript.CheckForSwitchAbilitiesTutorialPopup())
                {
                    UIManagerScript.ForceCloseFullScreenUIWithNoFade();
                }
                return; 
            }

            UIManagerScript.ForceCloseFullScreenUI();
            GameMasterScript.gmsSingleton.CheckAndTryAbility(data as AbilityScript);
            return;
        }

        //otherwise, enter placement mode!

        //set our mode to locked on the hotbar
        CursorOwnershipState = EImpactUICursorOwnershipState.vertical_hotbar_has_cursor;

        //point at the first hotbar
        listVerticalHotbarButtons[0].SetFocusOnMeForHotbarSlotting();

        //beep boop
        UIManagerScript.PlayCursorSound("StartDrag");

        //tell the cursor to hang on to that data
        UIManagerScript.SetHeldGenericObject(data);
    }

    #endregion

    void OnSelectSkill_PurchaseMode(ISelectableUIObject selectedSkill, Switch_UIButtonColumn col)
    {
        JobAbility abil = selectedSkill as JobAbility;

        if (abil == null)
        {
            return;
        }

        if (!GameMasterScript.heroPCActor.TryLearnAbility(abil))
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        UIManagerScript.PlayCursorSound("Ultra Learn"); // was positive achievement

        //Create a clone button
        Switch_InvItemButton selectedButton = col.GetButtonContaining(abil);
        GameObject go = Instantiate(image_SelectedObject.gameObject);

        //size it up to match the button we clicked
        go.transform.SetParent(selectedButton.transform);
        RectTransform newImageRect = go.transform as RectTransform;
        newImageRect.anchoredPosition = selectedButton.myUIObject.subObjectImage.rectTransform.anchoredPosition;

        //once positioned, parent it instead to the CSC block
        go.transform.SetParent(UIManagerScript.singletonUIMS.GetCSBlockImageTransform());

        //add the sprite image here
        Image img = go.GetComponent<Image>();
        img.sprite = abil.GetSpriteForUI();

        //everything in this UI scales
        img.transform.localScale = Vector3.one;

        //fly to the face in the corner of the screen
        LeanTween.moveX(img.rectTransform, 72f, 1.0f).setEaseInOutBack();
        LeanTween.moveY(img.rectTransform, -72f, 1.0f).setEaseInOutBack();
        LeanTween.scale(img.rectTransform, Vector3.zero, 1.0f).setEaseInBack();
        LeanTween.rotateAroundLocal(img.rectTransform, Vector3.forward, 1080.0f, 2.0f).setEaseOutQuart();

        //destroy it when done
        Destroy(go, 1.01f);

        //ok cool now flash the button
        Image borderImage = Instantiate(selectedButton.myUIObject.subObjectImage.gameObject, selectedButton.myUIObject.subObjectImage.gameObject.transform).GetComponent<Image>();
        borderImage.color = Color.cyan;
        LeanTween.color(borderImage.rectTransform, Color.white, 0.2f);

        borderImage.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
        LeanTween.scale(borderImage.rectTransform, new Vector3(1f, 1f, 1f), 0.2f).setEaseInOutBounce();
        Destroy(borderImage.gameObject, 0.3f);

        //flash the nice lady's face when she eats the powerup
        UIManagerScript.FlashCSPortrait(0.95f, Color.cyan);


        UpdateContent();
    }



    //Closed from the outside without calling ToggleActive
    public void ForceClose()
    {
        TurnOff();
    }

    public override void TryShowSelectableObject(ISelectableUIObject obj)
    {
        if (obj == null)
        {
            return;
        }

        DisplayItemInfo(obj, null, false);
    }

    //Used when we switch panels
    void ClearSelectedAbility()
    {
        image_SelectedObject.enabled = false;
        image_SelectedObject.sprite = null;

        txt_SelectedObjectName.text = "";
        txt_SelectedObjectInfo.text = "";

    }

    public override void TurnOn()
    {
        base.TurnOn();

        //check to see if player knows any abilities. If not, open the purchase tab first.
        EnterNewMode(
            GameMasterScript.heroPCActor.NumberOfAbilitiesPurchasedInCurrentJob() > 0
                ? ESkillSheetMode.assign_abilities
                : ESkillSheetMode.purchase_abilities, true);

        //update the old swap/drag values, even though we may not use them
        UIManagerScript.swappingSkill = false;
        UIManagerScript.skillToReplace = null;
        UIManagerScript.skillReplaceWith = null;
        UIManagerScript.swappingHotbarAction = false;

        //Add all our buttons and finery to the UI objects
        UIManagerScript.allUIObjects.Clear();
        AddUIObjectsFromListOfButtons(listModeChangeButtons, UIManagerScript.allUIObjects);

        //add buttons from the two labels
        leftColumn.AddButtonsToUIObjectMasterList();
        rightColumn.AddButtonsToUIObjectMasterList();

        //Focus on the top of the left column, unless it's empty, in which case focus on the first mode button.
        SetDefaultFocusBecauseSkillSheet();
        
        //focus on the first button 
        UIManagerScript.ChangeUIFocusAndAlignCursor(GetDefaultUiObjectForFocus());
    }

    public override bool TryTurnOff()
    {
        //Don't close if we are just trying to back out of the hotbar selection mode
        if (CursorOwnershipState == EImpactUICursorOwnershipState.vertical_hotbar_has_cursor)
        {
            //back out
            CursorOwnershipState = EImpactUICursorOwnershipState.normal;
            UIManagerScript.ClearAllHeldGenericObjects();
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.GetDefaultUIFocus());
            return false;
        }        

        return true;
    }

    public override void TurnOff()
    {
        base.TurnOff();

        if (txt_SelectedObjectName != null)
        {
            txt_SelectedObjectName.text = "";
        }
        if (txt_SelectedObjectInfo != null)
        {
            txt_SelectedObjectInfo.text = "";
        }

        if (image_SelectedObject != null)
        {
            image_SelectedObject.sprite = null;
            image_SelectedObject.enabled = false;
        }        
    }

    public override Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj)
    {
        /*
        if (eqpColumn != null)
        {
            return itemColumn.GetButtonContaining(obj);
        }
        */

        return null;
    }

    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
    }

    public override void StartAssignObjectToHotbarMode(ISelectableUIObject obj)
    {
    }

    //Here, the selectable objects will be Abilities or Job Information
    public override void DisplayItemInfo(ISelectableUIObject itemToDisplay, GameObject refObject, bool mouseHoverSource)
    {
        if (itemToDisplay == null)
        {
            return;
        }

        txt_SelectedObjectName.text = itemToDisplay.GetNameForUI();
        txt_SelectedObjectInfo.text = itemToDisplay.GetInformationForTooltip();

        image_SelectedObject.sprite = itemToDisplay.GetSpriteForUI();
        image_SelectedObject.enabled = true;

        /*
        JobAbility ja = itemToDisplay as JobAbility;
        AbilityScript ab = itemToDisplay as AbilityScript;

        if (ja != null)
        {
            txt_SelectedObjectInfo.text = ja.GetAbilityInformation();
        }
        else if (ab != null)
        {
            txt_SelectedObjectInfo.text = ab.GetAbilityInformation(); // This does not take into account ability mods. 
            // Put modified information here instead.
        }
        else
        {
            txt_SelectedObjectInfo.text = "";
        }
        */
    }
}


public partial class UIManagerScript
{
    private Switch_UISkillSheet switch_UISkillSheet;

    public static Switch_UISkillSheet GetUISkillSheet()
    {
        if (singletonUIMS.switch_UISkillSheet == null)
        {
            if (UIManagerScript.GetUITabSelected() == UITabs.SKILLS)
            {
                singletonUIMS.switch_UISkillSheet = singletonUIMS.GetCurrentFullScreenUI() as Switch_UISkillSheet;
            }

        }
        return singletonUIMS.switch_UISkillSheet;
    }

    public static object Debug_OpenSwitchSkillSheet(string[] args)
    {
        if (singletonUIMS.switch_UISkillSheet == null)
        {
            singletonUIMS.switch_UISkillSheet = GameObject.Find("Skill Sheet Switch").GetComponent<Switch_UISkillSheet>();
        }
        singletonUIMS.switch_UISkillSheet.gameObject.SetActive(true);

        return "poof";
    }

    public static object Debug_FillSwitchSkillSheet_Assign(string[] args)
    {
        singletonUIMS.switch_UISkillSheet.EnterNewMode(ESkillSheetMode.assign_abilities, true);

        return "beep";
    }

    public static object Debug_FillSwitchSkillSheet_Purchase(string[] args)
    {
        singletonUIMS.switch_UISkillSheet.EnterNewMode(ESkillSheetMode.purchase_abilities, true);

        return "beep";
    }

}



