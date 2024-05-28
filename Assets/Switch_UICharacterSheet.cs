using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rewired;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Switch_UICharacterSheet : ImpactUI_Base
{
    public enum ECharacterSheetTab
    {
        core,
        adventure,
        max
    }

    public enum ECharacterSheetValueType
    {
        space = 0,
        health,
        stamina,
        energy,
        stremf,
        swiftness,
        spirit,
        discipline,
        guile,
        weapon_power,
        spirit_power,
        crit_chance,
        crit_damage,
        charge_time,
        parry_chance,
        block_chance,
        dodge_chance,
        all_damage_mod,
        all_defense_mod,
        powerup_drop,
        element_physical,
        element_fire,
        element_poison,
        element_water,
        element_lightning,
        element_shadow,
        highest_floor,
        highest_floor_ever,
        days_passed,
        monsters_killed,
        champs_killed,
        steps_taken,
        favorite_job,
        total_characters,
        playtime,
        pandora_boxes,
        label_elements,
        textbox_feats,
        textbox_statuseffects,
        pet_info,
        /*
        pet_name,
        pet_species,
        pet_health,
        pet_weaponpower,
        label_pet_skills,
        textbox_pet_skills,
        label_pet_bonuses,
        textbox_pet_bonuses,
        pet_insured,
        */

        MAX,
    }

    public static string[] labelTextBySheetValueType;

    [Header("Tabs for stat sections")]
    // Toggles between core stats & adventure stats
    public Button btn_coreStats;
    public Button btn_adventureStats;
    public Sprite sprite_tabSelected;
    public Sprite sprite_tabNotSelected;

    private List<UIManagerScript.UIObject> listTabButtonShadowObjects;


    [Header("Column Anchors")]
    public List<GameObject>     list_columnAnchors;

    private ECharacterSheetTab activeTab;

    //oh boy
    //The first list is per-tab. Right now it's just Core and Adventure.
    //The second list is a container for the three columns in each tab.
    //The core list is the list of infolines in the column
    [HideInInspector]
    public List<List<List<UIManagerScript.UIObject>>> listAllInfoLines;
    Dictionary<UIManagerScript.UIObject, ECharacterSheetValueType> dictValuesByShadowObject;

    //here are misc game objects that need to be enabled / disabled with each tab
    public List<List<GameObject>> listMiscGameObjectsInTabs;

    [Header("InfoLine information")]
    public GameObject prefab_infoLine;
    public GameObject prefab_elementalStatsLine;
    public float      spaceBetweenLines;
    public List<CharacterSheetElementalSpriteParing> elementSprites;
    private static Dictionary<ECharacterSheetValueType, Sprite> dictSpritesForElement;

    [Header("TextBox Prefabs")]
    public GameObject prefab_StatusEffectsBox;
    public GameObject prefab_FeatsBox;
    public GameObject prefab_LabelElements;

    private TextMeshProUGUI txt_Feats;
    private TextMeshProUGUI txt_StatusEffects;

    [Header("Tooltip Scroll")]
    public TextMeshProUGUI txt_TooltipScroll;
    ECharacterSheetValueType currentSelectedValueType;

    [Header("Pet Info")]
    public GameObject prefab_petInfoBlock;
    private CharacterSheetPetInfoBlock petInfoBlock;

    private List<UIManagerScript.UIObject> listShadowObjects;

    public override bool InitializeDynamicUIComponents()
    {
        if (!base.InitializeDynamicUIComponents())
        {
            return false;
        }

        labelTextBySheetValueType = new string[(int)ECharacterSheetValueType.MAX];
        labelTextBySheetValueType[(int)ECharacterSheetValueType.health] = StringManager.GetString("stat_health");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.stamina] = StringManager.GetString("stat_stamina");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.energy] = StringManager.GetString("stat_energy");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.stremf] = StringManager.GetString("stat_strength");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.swiftness] = StringManager.GetString("stat_swiftness");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.guile] = StringManager.GetString("stat_guile");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.discipline] = StringManager.GetString("stat_discipline");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.spirit] = StringManager.GetString("stat_spirit");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.swiftness] = StringManager.GetString("stat_swiftness");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.weapon_power] = StringManager.GetString("ui_equipment_weaponpower");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.spirit_power] = StringManager.GetString("stat_spiritpower");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.crit_chance] = StringManager.GetString("stat_critchance");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.crit_damage] = StringManager.GetString("misc_crit_damage");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.block_chance] = StringManager.GetString("stat_blockchance");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.charge_time] = StringManager.GetString("misc_ct_gain");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.dodge_chance] = StringManager.GetString("stat_dodgechance");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.parry_chance] = StringManager.GetString("stat_parrychance");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.powerup_drop] = StringManager.GetString("stat_powerupdrop");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.all_damage_mod] = StringManager.GetString("stat_alldamage");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.all_defense_mod] = StringManager.GetString("stat_alldefense");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_fire] = StringManager.GetString("misc_dmg_fire");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_lightning] = StringManager.GetString("misc_dmg_lightning");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_physical] = StringManager.GetString("misc_dmg_physical");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_poison] = StringManager.GetString("misc_dmg_poison");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_shadow] = StringManager.GetString("misc_dmg_shadow");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.element_water] = StringManager.GetString("misc_dmg_water");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.champs_killed] = StringManager.GetString("champions_defeated");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.steps_taken] = StringManager.GetString("steps_taken");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.favorite_job] = StringManager.GetString("favorite_job");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.days_passed] = StringManager.GetString("misc_days_passed");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.highest_floor] = StringManager.GetString("saveslot_highestfloor");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.highest_floor_ever] = StringManager.GetString("highest_floor_ever");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.monsters_killed] = StringManager.GetString("monsters_defeated");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.pandora_boxes] = StringManager.GetString("ui_pandora_opened");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.total_characters] = StringManager.GetString("total_characters");
        labelTextBySheetValueType[(int)ECharacterSheetValueType.playtime] = StringManager.GetString("total_playtime");

        listShadowObjects = new List<UIManagerScript.UIObject>();
        listMiscGameObjectsInTabs = new List<List<GameObject>>();
        dictValuesByShadowObject= new Dictionary<UIManagerScript.UIObject, ECharacterSheetValueType>();

        dictSpritesForElement = new Dictionary<ECharacterSheetValueType, Sprite>();
        foreach (CharacterSheetElementalSpriteParing csp in elementSprites)
        {
            dictSpritesForElement.Add(csp.elementInfo, csp.sprite);
        }

        //Generate the tabs, the columns in the tab, and the holders for the lines in each column
        listAllInfoLines = new List<List<List<UIManagerScript.UIObject>>>();
        for(int t=0; t < (int) ECharacterSheetTab.max; t++)
        {
            listMiscGameObjectsInTabs.Add(new List<GameObject>());
            listAllInfoLines.Add(new List<List<UIManagerScript.UIObject>>());
            for (int idx = 0; idx < list_columnAnchors.Count; idx++)
            {
                listAllInfoLines[t].Add( new List<UIManagerScript.UIObject>());
            }
        }

        //Tabs at the top
        btn_coreStats.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("charsheet_tab1");
        btn_adventureStats.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("charsheet_tab2");

        //Connect them to tangledeep input
        listTabButtonShadowObjects = new List<UIManagerScript.UIObject>();

        UIManagerScript.UIObject shadowObj = new UIManagerScript.UIObject();
        shadowObj.gameObj = btn_coreStats.gameObject;
        shadowObj.onSubmitValue = (int)ECharacterSheetTab.core;
        shadowObj.mySubmitFunction = ActivateTabViaShadowObject;
        listTabButtonShadowObjects.Add(shadowObj);

        shadowObj = new UIManagerScript.UIObject();
        shadowObj.gameObj = btn_adventureStats.gameObject;
        shadowObj.onSubmitValue = (int)ECharacterSheetTab.adventure;
        shadowObj.mySubmitFunction = ActivateTabViaShadowObject;
        listTabButtonShadowObjects.Add(shadowObj);

        //make the tabs talk to each other
        listTabButtonShadowObjects[0].neighbors[(int) Directions.EAST] = listTabButtonShadowObjects[1];
        listTabButtonShadowObjects[1].neighbors[(int) Directions.WEST] = listTabButtonShadowObjects[0];

        activeTab = ECharacterSheetTab.core;

        //assign values to each column in each tab
        //0 == core sheet, 0 == first column
        float yOffset = spaceBetweenLines;
        int iLineIdx = 0;

        //resources
        AddInfoLine(0, 0, ECharacterSheetValueType.health, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.stamina, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.energy, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(0, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        //core stats   
        AddInfoLine(0, 0, ECharacterSheetValueType.stremf, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.swiftness, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.spirit, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.discipline, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.guile, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(0, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        //POWA  
        AddInfoLine(0, 0, ECharacterSheetValueType.weapon_power, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.spirit_power, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 0, ECharacterSheetValueType.crit_chance, yOffset -= spaceBetweenLines, prefab_infoLine); ;
        AddInfoLine(0, 0, ECharacterSheetValueType.crit_damage, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(0, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        //gotta go fast
        AddInfoLine(0, 0, ECharacterSheetValueType.charge_time, yOffset -= spaceBetweenLines, prefab_infoLine);

        //back to the top for the next column
        yOffset = spaceBetweenLines;

        //elemental info  
        AddInfoLine(0, 1, ECharacterSheetValueType.label_elements, yOffset -= spaceBetweenLines, prefab_LabelElements);
        AddInfoLine(0, 1, ECharacterSheetValueType.element_physical, yOffset -= spaceBetweenLines, prefab_elementalStatsLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.element_fire, yOffset -= spaceBetweenLines, prefab_elementalStatsLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.element_poison, yOffset -= spaceBetweenLines, prefab_elementalStatsLine); ;
        AddInfoLine(0, 1, ECharacterSheetValueType.element_water, yOffset -= spaceBetweenLines, prefab_elementalStatsLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.element_lightning, yOffset -= spaceBetweenLines, prefab_elementalStatsLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.element_shadow, yOffset -= spaceBetweenLines, prefab_elementalStatsLine);

        AddInfoLine(0, 1, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        //Combat numbers
        AddInfoLine(0, 1, ECharacterSheetValueType.parry_chance, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.block_chance, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.dodge_chance, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.all_damage_mod, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(0, 1, ECharacterSheetValueType.all_defense_mod, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(0, 1, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        //powa up
        AddInfoLine(0, 1, ECharacterSheetValueType.powerup_drop, yOffset -= spaceBetweenLines, prefab_infoLine);

        //baaaaack to the top for column 3!
        yOffset = spaceBetweenLines;
        AddInfoLine(0, 2, ECharacterSheetValueType.textbox_statuseffects, yOffset -= spaceBetweenLines, prefab_StatusEffectsBox);

        // Tab 2, Adventure Stats
        yOffset = spaceBetweenLines;
        AddInfoLine(1, 0, ECharacterSheetValueType.highest_floor, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.highest_floor_ever, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.days_passed, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.monsters_killed, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.champs_killed, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.steps_taken, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.favorite_job, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.total_characters, yOffset -= spaceBetweenLines, prefab_infoLine);
        AddInfoLine(1, 0, ECharacterSheetValueType.playtime, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.space, yOffset -= spaceBetweenLines, prefab_infoLine);

        AddInfoLine(1, 0, ECharacterSheetValueType.pandora_boxes, yOffset -= spaceBetweenLines, prefab_infoLine);

        // Tab 2, Column 1, Feets
        yOffset = spaceBetweenLines;
        AddInfoLine(1, 1, ECharacterSheetValueType.textbox_feats, yOffset -= spaceBetweenLines, prefab_FeatsBox);

        // Tab 2, Column 2, Pet!
        yOffset = spaceBetweenLines;
        AddInfoLine(1, 2, ECharacterSheetValueType.pet_info, yOffset -= spaceBetweenLines, prefab_petInfoBlock);


        /*
         *         highest_floor,
        highest_floor_ever,up
        days_passed,
        monsters_killed,
        champs_killed,
        steps_taken,
        favorite_job,
        total_characters,
        playtime,
        pandora_boxes,
        */


        //attach the pet info box to the middle column of the second tab, which is just feats!
        petInfoBlock.InitializeDynamicUIComponents();
        listAllInfoLines[1][2] = petInfoBlock.petInfoPoints;

        /*
        listAllInfoLines[1][0][0].shadowObject.neighbors[(int)Directions.EAST] = petObjects[0];
        foreach (var o in petObjects)
        {
            o.neighbors[(int)Directions.WEST] = listAllInfoLines[1][0][0].shadowObject;
        }
        */


        //make neighbors out of all this
        for (int iTabIndex = 0; iTabIndex < listAllInfoLines.Count; iTabIndex++)
        {
            //woow
            int iMaxColumns = listAllInfoLines[iTabIndex].Count;
            for (int iColumnIndex = 0; iColumnIndex < iMaxColumns; iColumnIndex++)
            {
                //wooooow
                int iMaxRows = listAllInfoLines[iTabIndex][iColumnIndex].Count;
                List<UIManagerScript.UIObject> leftColumn = iColumnIndex == 0 ? null : listAllInfoLines[iTabIndex][iColumnIndex -1];
                List<UIManagerScript.UIObject> rightColumn = iColumnIndex >= iMaxColumns - 1 ? null : listAllInfoLines[iTabIndex][iColumnIndex + 1];
                for (int iRowIndex = 0; iRowIndex < iMaxRows; iRowIndex++)
                {
                    UIManagerScript.UIObject csThis = listAllInfoLines[iTabIndex][iColumnIndex][iRowIndex];
                    UIManagerScript.UIObject csPrev = iRowIndex <= 0 ? null : listAllInfoLines[iTabIndex][iColumnIndex][iRowIndex-1];
                    UIManagerScript.UIObject csNext = iRowIndex >= iMaxRows - 1 ? null : listAllInfoLines[iTabIndex][iColumnIndex][iRowIndex + 1];

                    if (csPrev != null)
                    {
                        csThis.neighbors[(int) Directions.NORTH] = csPrev;
                    }
                    if (csNext != null)
                    {
                        csThis.neighbors[(int) Directions.SOUTH] = csNext;
                    }

                    //tie to left and right
                    int iNeighborIdx;
                    if (leftColumn != null)
                    {
                        iNeighborIdx = Math.Min(iRowIndex, leftColumn.Count - 1);
                        if (iNeighborIdx >= 0)
                        {
                            csThis.neighbors[(int) Directions.WEST] = leftColumn[iNeighborIdx];
                        }
                    }

                    if (rightColumn != null)
                    {
                        iNeighborIdx = Math.Min(iRowIndex, rightColumn.Count - 1);
                        if (iNeighborIdx >= 0)
                        {
                            csThis.neighbors[(int)Directions.EAST] = rightColumn[iNeighborIdx];
                        }
                    }

                }
            }
        }

        FontManager.LocalizeMe(btn_coreStats.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);
        FontManager.LocalizeMe(btn_adventureStats.GetComponentInChildren<TextMeshProUGUI>(), TDFonts.WHITE);

        FontManager.LocalizeMe(txt_Feats, TDFonts.WHITE);
        FontManager.LocalizeMe(txt_StatusEffects, TDFonts.WHITE);
        FontManager.LocalizeMe(txt_TooltipScroll, TDFonts.WHITE);

        bHasBeenInitialized = true;

        return true;
    }

    void ActivateTabViaShadowObject(int submitValue)
    {
        if (Debug.isDebugBuild) Debug.Log("Attempting to activate tab: " + submitValue);
        ActivateTab((ECharacterSheetTab)submitValue);
    }
    //Make sure we show/hide the correct game objects
    void ActivateTab(ECharacterSheetTab thisTab)
    {
        activeTab = thisTab;

        for (int iTabIdx = 0; iTabIdx < (int) ECharacterSheetTab.max; iTabIdx++)
        {
            bool bShouldEnable = (int)activeTab == iTabIdx;

            listAllInfoLines[iTabIdx].ForEach(column => 
                                              column.ForEach( row => 
                                              row.gameObj.SetActive(bShouldEnable)));

            //Turn off text boxes and non-info line objects. Like labels!
            listMiscGameObjectsInTabs[iTabIdx].ForEach( obj => obj.SetActive(bShouldEnable));

            //attach the tabs to the new first object
            listTabButtonShadowObjects[iTabIdx].neighbors[(int) Directions.SOUTH] =
                listAllInfoLines[(int) activeTab][0][0];
        }

        //below -- bad code! Relies on the metaknowledge that we only have two tabs.
        btn_adventureStats.GetComponent<Image>().sprite = activeTab == ECharacterSheetTab.adventure ? sprite_tabSelected : sprite_tabNotSelected;
        btn_coreStats.GetComponent<Image>().sprite = activeTab == ECharacterSheetTab.core ? sprite_tabSelected : sprite_tabNotSelected;

        //attach the top lines to the buttons above
        for (int iRowIndex = 0; iRowIndex < listAllInfoLines[(int) activeTab].Count; iRowIndex++)
        {
            if (listAllInfoLines[(int) activeTab][iRowIndex].Count < 1)
            {
                continue;
            }

            UIManagerScript.UIObject topShadowObject = listAllInfoLines[(int)activeTab][iRowIndex][0];
            if (topShadowObject != null)
            {
                topShadowObject.neighbors[(int) Directions.NORTH] = listTabButtonShadowObjects[(int) activeTab];
            }

        }

        //focus on the tab button if not already
        UIManagerScript.ChangeUIFocusAndAlignCursor(listTabButtonShadowObjects[(int)activeTab]);

        UpdateContent();


    }

    void AddInfoLine(int iTabIndex, int iColumnIndex, ECharacterSheetValueType valueType, float yOffset, GameObject prefab)
    {
        //space just means space, create nothing here.
        if (valueType == ECharacterSheetValueType.space)
        {
            return;
        }

        //Create the new gameobject and position it accordingly
        GameObject obj = Instantiate(prefab, list_columnAnchors[iColumnIndex].transform);

        TextMeshProUGUI[] meshes = obj.GetComponentsInChildren<TextMeshProUGUI>();



        for (int i = 0; i < meshes.Length; i++)
        {
            FontManager.LocalizeMe(meshes[i], TDFonts.WHITE);

            meshes[i].text = labelTextBySheetValueType[(int)valueType];

            if (valueType == ECharacterSheetValueType.label_elements)
            {
                if (i == 0)
                {
                    meshes[i].text = StringManager.GetString("misc_generic_damage");
                }
                else
                {
                    meshes[i].text = StringManager.GetString("misc_generic_defense");
                }
                
            }
        }



        RectTransform rt = obj.transform as RectTransform;
        rt.anchoredPosition = new Vector2(0, yOffset);
        
        UIManagerScript.UIObject shadowObject; 
        
        //If we created a label or textbox, just use a regular ui object
        switch (valueType)
        {
            //No UIObject needed here. 
            case ECharacterSheetValueType.label_elements:
                listMiscGameObjectsInTabs[iTabIndex].Add(obj);                
                return;
            case ECharacterSheetValueType.textbox_feats:
                listMiscGameObjectsInTabs[iTabIndex].Add(obj);
                txt_Feats = obj.GetComponent<TextMeshProUGUI>();
                shadowObject = new UIManagerScript.UIObject();
                break;
            case ECharacterSheetValueType.textbox_statuseffects:
                listMiscGameObjectsInTabs[iTabIndex].Add(obj);
                txt_StatusEffects = obj.GetComponent<TextMeshProUGUI>();
                shadowObject = new UIManagerScript.UIObject();
                break;
            case ECharacterSheetValueType.pet_info:
                listMiscGameObjectsInTabs[iTabIndex].Add(obj);
                petInfoBlock = obj.GetComponent<CharacterSheetPetInfoBlock>();
                shadowObject = new UIManagerScript.UIObject();
                break;
            default:
                shadowObject = new CharacterSheetInfoPoint(obj);
                var chip = (CharacterSheetInfoPoint)shadowObject;
                chip.SetValue(valueType);
                dictValuesByShadowObject[shadowObject] = valueType;
                break;
        }

        shadowObject.gameObj = obj;
        shadowObject.onSelectValue = (int)valueType;
        shadowObject.myOnSelectAction = Switch_UICharacterSheet.OnHoverCharacterSheetInfoLine;
        listShadowObjects.Add(shadowObject);
        
        //mouse over?
        EventTrigger et = obj.GetComponent<EventTrigger>();
        if (et != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((eventData) => { shadowObject.FocusOnMe(); });
            et.triggers.Add(entry);
        }

        //place it in the list.
        listAllInfoLines[iTabIndex][iColumnIndex].Add(shadowObject);
        
    }

    //Used to make the pointer point at lines and display info
    private static void OnHoverCharacterSheetInfoLine(int iStoredValue)
    {
        ECharacterSheetValueType valueType = (ECharacterSheetValueType)iStoredValue;
    }

    public override void UpdateContent(bool adjustSorting = true)
    {
        //determine which tab we're on
        int iTabIndex = (int) activeTab;

        //todo: make sure other columns aren't being displayed

        //here is a list of columns
        List<List<UIManagerScript.UIObject>> listColumns = listAllInfoLines[iTabIndex];

        //now, for each column update each line
        listColumns.ForEach( column => column.ForEach(cLine => cLine.Update()));

        if (activeTab == ECharacterSheetTab.core)
        {
            UpdateStatusEffectsText();
        }
        if (activeTab == ECharacterSheetTab.adventure)
        {
            UpdateFeatsText();
            UpdatePetContent();
        }
    }

    private void UpdateStatusEffectsText()
    {
        txt_StatusEffects.text = GameMasterScript.heroPCActor.GetStatusEffectsTextForCharacterSheet();
        RectTransform rt = txt_StatusEffects.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(530f, rt.sizeDelta.y);
    }

    private void UpdateFeatsText()
    {
        txt_Feats.text = GameMasterScript.heroPCActor.GetFeatsTextForCharacterSheet();
        RectTransform rt = txt_StatusEffects.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(630f, rt.sizeDelta.y);
    }

    private void UpdatePetContent()
    {
        petInfoBlock.UpdateContent();
    }

    public override void TurnOn()
    {
        base.TurnOn();

        //add all our connective shadow objects 
        UIManagerScript.allUIObjects.Clear();
        AddAllUIObjects(UIManagerScript.allUIObjects);

        //clear the tooltip's default or old text
        txt_TooltipScroll.text = "";

        //turn on the Core tab
        ActivateTab(ECharacterSheetTab.core);
    }

    protected override void AddAllUIObjects(List<UIManagerScript.UIObject> allObjects)
    {
        //add the objects from the columns
        listShadowObjects.ForEach( allObjects.Add );

        //add the buttons at the top
        listTabButtonShadowObjects.ForEach( allObjects.Add );
    }

    public override bool TryTurnOff()
    {
        //currently no sub-state that you can't just close out of 
        return true;
    }

    public override void OnDialogClose()
    {
        //No dialog yet, hmm.
    }

    public override void TryShowSelectableObject(ISelectableUIObject obj)
    {
        //lol
    }

    public override void StartAssignObjectToHotbarMode(int iButtonIndex)
    {
        //lol
    }


    public override UIManagerScript.UIObject GetDefaultUiObjectForFocus()
    {
        throw new NotImplementedException();
    }

    public override Switch_InvItemButton GetButtonContainingThisObject(ISelectableUIObject obj)
    {
        //we may not be using this
        return null;
    }

    public override void StartAssignObjectToHotbarMode(ISelectableUIObject content)
    {
        //lol
    }

    #region  UI Callbacks

    public void OnClickAdventureTab()
    {
        ActivateTab(ECharacterSheetTab.adventure);
    }

    public void OnClickCoreTab()
    {
        ActivateTab(ECharacterSheetTab.core);
    }

    #endregion

    public override void Update()
    {
        base.Update();

        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        CursorBounce cb = UIManagerScript.singletonUIMS.uiDialogMenuCursorBounce;

        if (UIManagerScript.uiObjectFocus == null) return; // :| dont love doing null checks

        // If we are pointing at the tabs, adjust our direction appropriately
        if (UIManagerScript.uiObjectFocus.gameObj == btn_coreStats.gameObject || UIManagerScript.uiObjectFocus.gameObj == btn_adventureStats.gameObject)
        {
            float multiplier = Screen.width / 1920f;

            float fButtonWidth = ((RectTransform)UIManagerScript.uiObjectFocus.gameObj.transform).sizeDelta.x * multiplier; //because tangledeep
            UIManagerScript.ChangeUIFocusAndAlignCursor(UIManagerScript.uiObjectFocus, fButtonWidth * 0.5f, 0f);
            cb.SetFacing(Directions.NORTH);

            txt_TooltipScroll.text = "";
            return;
        }

        if (listShadowObjects.Contains(UIManagerScript.uiObjectFocus))
        {
            //if we are focused on an object in our sheet, point at the text.

            AlignCursorToLeftOfText(UIManagerScript.uiObjectFocus);

            if (dictValuesByShadowObject.ContainsKey(UIManagerScript.uiObjectFocus))
            {
                ECharacterSheetValueType newValue = dictValuesByShadowObject[UIManagerScript.uiObjectFocus];
                if (newValue != currentSelectedValueType)
                {
                    currentSelectedValueType = newValue;

                    //Directly update the tooltip text
                    SetTooltipForCharacterSheetOption(currentSelectedValueType);
                }
            }
            else
            {
                //Clear the tooltip if mousing over a button or top tab
                txt_TooltipScroll.text = "";
            }
        }
    }

    //This assumes we have a TMPro Text as a child. Hope so!
    void AlignCursorToLeftOfText(UIManagerScript.UIObject shadowObject)
    {
        TextMeshProUGUI textChild = shadowObject.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textChild != null)
        {
            //This value is the X delta from the center of the text object. Multiply by 1.5
            //because tangledeep weird 150% canvas scale.
            float fCharacterDelta = textChild.textInfo.characterInfo[0].bottomLeft.x / 1.5f;
 
            //so our offset is the center of the text object + that value - a few PX so the hand is to the left of the 
            //word
            float fXOffset = ((RectTransform) textChild.transform).sizeDelta.x / 2 + fCharacterDelta;

            fXOffset -= 48f;

            UIManagerScript.AlignCursorPos(UIManagerScript.singletonUIMS.uiDialogMenuCursor, textChild.gameObject,
                fXOffset, 0f, false);
        }
    }

    public static void SetInformationForValue(ECharacterSheetValueType valueType, CharacterSheetInfoPoint infoPointWithDynamicValues )
    {
        TextMeshProUGUI txtLabel = infoPointWithDynamicValues.txt_label;
        TextMeshProUGUI txtValue = infoPointWithDynamicValues.txt_value;
        TextMeshProUGUI txtSecondaryValue = infoPointWithDynamicValues.txt_secondaryValue;
        Image imgDivider = infoPointWithDynamicValues.img_divider;

        string tryText = labelTextBySheetValueType[(int)valueType];
        if (!string.IsNullOrEmpty(tryText))
        {
            txtLabel.text = tryText;
        }


        HeroPC hero = GameMasterScript.heroPCActor;        
        txtValue.text = "--";

        switch (valueType)
        {
            case ECharacterSheetValueType.space:
                return;
            case ECharacterSheetValueType.health:
                txtValue.text = (int)hero.myStats.GetCurStat(StatTypes.HEALTH) + " / " + (int)hero.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX);
                break;
            case ECharacterSheetValueType.stamina:
                txtValue.text = (int)hero.myStats.GetCurStat(StatTypes.STAMINA) + " / " + (int)hero.myStats.GetStat(StatTypes.STAMINA, StatDataTypes.MAX);
                break;
            case ECharacterSheetValueType.energy:
                txtValue.text = (int)hero.myStats.GetCurStat(StatTypes.ENERGY) + " / " + (int)hero.myStats.GetStat(StatTypes.ENERGY, StatDataTypes.MAX);
                break;
            case ECharacterSheetValueType.stremf:
                txtValue.text = ((int)hero.myStats.GetCurStat(StatTypes.STRENGTH)).ToString();
                break;
            case ECharacterSheetValueType.swiftness:
                txtValue.text = ((int)hero.myStats.GetCurStat(StatTypes.SWIFTNESS)).ToString();
                break;
            case ECharacterSheetValueType.spirit:
                txtValue.text = ((int)hero.myStats.GetCurStat(StatTypes.SPIRIT)).ToString();
                break;
            case ECharacterSheetValueType.discipline:
                txtValue.text = ((int)hero.myStats.GetCurStat(StatTypes.DISCIPLINE)).ToString();
                break;
            case ECharacterSheetValueType.guile:
                txtValue.text = ((int)hero.myStats.GetCurStat(StatTypes.GUILE)).ToString();
                break;
            case ECharacterSheetValueType.weapon_power:
                txtValue.text = ((int)hero.cachedBattleData.physicalWeaponDamage).ToString();
                break;
            case ECharacterSheetValueType.spirit_power:
                txtValue.text = hero.GetSpiritPowerForCharacterSheet();
                break;
            case ECharacterSheetValueType.crit_chance:
                txtValue.text = hero.GetCritChanceForCharacterSheet();
                break;
            case ECharacterSheetValueType.crit_damage:
                txtValue.text = (int)((hero.cachedBattleData.critMeleeDamageMult - 1f) * 100f) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
                break;
            case ECharacterSheetValueType.charge_time:
                txtValue.text = ((int)hero.cachedBattleData.chargeGain - 100).ToString();
                break;
            case ECharacterSheetValueType.parry_chance:
                txtValue.text = (int)hero.CalculateAverageParry() + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
                break;
            case ECharacterSheetValueType.block_chance:
                txtValue.text = hero.GetBlockChanceForCharacterSheet();
                break;
            case ECharacterSheetValueType.dodge_chance:
                txtValue.text = (int)Math.Abs(hero.CalculateDodge()) + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT);
                break;
            case ECharacterSheetValueType.all_damage_mod:
                txtValue.text = hero.GetAllDamageModForCharacterSheet();
                break;
            case ECharacterSheetValueType.all_defense_mod:
                txtValue.text = hero.GetAllDefenseModForCharacterSheet();
                break;
            case ECharacterSheetValueType.powerup_drop:
                txtValue.text = hero.GetPowerupDropChanceForCharacterSheet();
                break;
            case ECharacterSheetValueType.element_physical:
            case ECharacterSheetValueType.element_fire:
            case ECharacterSheetValueType.element_poison:
            case ECharacterSheetValueType.element_water:
            case ECharacterSheetValueType.element_lightning:
            case ECharacterSheetValueType.element_shadow:
                string strResist;
                string strDamage;
                hero.GetElementResistAndDamageValuesForCharacterSheet(valueType, out strResist, out strDamage);
                txtValue.text = strResist;
                txtSecondaryValue.text = strDamage;
                imgDivider.sprite = dictSpritesForElement[valueType];
                break;

            case ECharacterSheetValueType.highest_floor:
                txtValue.text = (hero.lowestFloorExplored + 1).ToString();
                break;
            case ECharacterSheetValueType.highest_floor_ever:
                txtValue.text = (MetaProgressScript.lowestFloorReached + 1).ToString();
                break;
            case ECharacterSheetValueType.days_passed:
                txtValue.text = MetaProgressScript.totalDaysPassed.ToString();
                break;
            case ECharacterSheetValueType.monsters_killed:
                txtValue.text = hero.monstersKilled.ToString();
                break;
            case ECharacterSheetValueType.champs_killed:
                txtValue.text = hero.championsKilled.ToString();
                break;
            case ECharacterSheetValueType.steps_taken:
                txtValue.text = hero.stepsTaken.ToString();
                break;
            case ECharacterSheetValueType.favorite_job:
                txtValue.text = hero.GetFavoriteJob();
                break;
            case ECharacterSheetValueType.total_characters:
                txtValue.text = MetaProgressScript.totalCharacters.ToString();
                break;
            case ECharacterSheetValueType.playtime:
                txtValue.text = MetaProgressScript.GetDisplayPlayTime(false, hero.GetPlayTime());
                break;
            case ECharacterSheetValueType.pandora_boxes:
                txtValue.text = hero.numPandoraBoxesOpened.ToString();
                break;
            default:
                txtValue.text = "TRUE FACTS";
                break;
        }
    }

    public void SetTooltipForCharacterSheetOption(ECharacterSheetValueType thisValue)
    {
        string builder = "";
        float amount = 0;
        float baseAmount = 0;

        StatTypes checkStat = StatTypes.HEALTH;
        bool bAppendStatInfo = false;

        switch (thisValue)
        {
            case ECharacterSheetValueType.stremf:
                bAppendStatInfo = true;
                checkStat = StatTypes.STRENGTH;
                builder = StringManager.GetString("stat_strength").ToUpperInvariant() + " #CALC#: ";
                builder += StringManager.GetString("stat_strength_desc") + " ";

                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.STRENGTH);
                amount = (float) Math.Round(baseAmount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_base_melee_power") + ", ";

                amount = baseAmount / 2f;
                amount = (float) Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_dagger_damage") + ", ";

                amount = baseAmount;
                amount *= StatBlock.STRENGTH_PERCENT_PHYSICALRESIST_MOD;
                amount = (float) Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_physical_defense");
                break;

            case ECharacterSheetValueType.swiftness:
                bAppendStatInfo = true;
                checkStat = StatTypes.SWIFTNESS;
                builder = StringManager.GetString("stat_swiftness").ToUpperInvariant() + " #CALC#: ";
                builder += StringManager.GetString("stat_swiftness_desc") + " ";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SWIFTNESS);
                amount = (float)Math.Round(baseAmount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_base_ranged_power") + ", ";
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_crit_damage") + ", ";
                amount = baseAmount / 10f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + " " + StringManager.GetString("misc_ct_gain");
                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                break;
            case ECharacterSheetValueType.spirit:
                bAppendStatInfo = true;
                checkStat = StatTypes.SPIRIT;
                builder =  StringManager.GetString("stat_spirit").ToUpperInvariant() + " #CALC#: ";
                builder += StringManager.GetString("stat_spirit_desc") + " ";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.SPIRIT);
                amount = baseAmount * StatBlock.SPIRIT_PERCENT_SPIRITPOWER_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_spiritpower") + ", ";
                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_staff_damage") + ", ";
                amount = baseAmount;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_powerup_healing");
                break;
            case ECharacterSheetValueType.discipline:
                bAppendStatInfo = true;
                checkStat = StatTypes.DISCIPLINE;
                builder = StringManager.GetString("stat_discipline").ToUpperInvariant() + " #CALC#: ";
                builder += StringManager.GetString("stat_discipline_desc") + " ";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.DISCIPLINE);
                amount = baseAmount * StatBlock.DISCIPLINE_PERCENT_SPIRITPOWER_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_spiritpower") + ", ";
                amount = baseAmount * 0.33f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_elemental_resist") + ", ";
                amount = baseAmount * StatBlock.DISCIPLINE_PERCENT_ELEMRESIST_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_buff_duration") + ", ";
                amount = baseAmount / 2f;
                builder += "+" + (int)amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_staff_damage") + ", ";
                amount = (baseAmount * 0.5f);
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_pet_health");
                break;
            case ECharacterSheetValueType.guile:
                bAppendStatInfo = true;
                checkStat = StatTypes.GUILE;
                builder = StringManager.GetString("stat_guile").ToUpperInvariant() + " #CALC#: ";
                builder += StringManager.GetString("stat_discipline_guile") + " ";
                baseAmount = GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.GUILE);
                amount = baseAmount * StatBlock.GUILE_PERCENT_CRITCHANCE_MOD;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_critchance") + ", ";

                amount = baseAmount / 2f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("misc_dagger_damage") + ", ";

                amount = baseAmount * StatBlock.GUILE_PERCENT_PARRY_MOD;
                amount = (float)Math.Round(amount, 1);

                if (GameMasterScript.heroPCActor.myEquipment.IsCurrentWeaponRanged())
                {
                    amount = 0;
                }

                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_parrychance") + ", ";
                amount = baseAmount / 4f;
                amount = (float)Math.Round(amount, 1);
                builder += "+" + amount + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + " " + StringManager.GetString("stat_powerupdrop");
                break;
            case ECharacterSheetValueType.weapon_power:
                builder =  StringManager.GetString("ui_equipment_weaponpower").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_battle_power");
                break;
            case ECharacterSheetValueType.spirit_power:
                builder =  StringManager.GetString("stat_spiritpower").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_spirit_power");
                break;
            case ECharacterSheetValueType.crit_chance:
                builder =  StringManager.GetString("stat_critchance").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_critical_chance");
                break;
            case ECharacterSheetValueType.charge_time:
                builder = StringManager.GetString("stat_chargetime").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_chargetime");
                break;
            case ECharacterSheetValueType.powerup_drop:
                builder = StringManager.GetString("stat_powerupdrop").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_powerup_drop");
                break;
            case ECharacterSheetValueType.all_damage_mod:
                builder = StringManager.GetString("stat_alldamage").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_boost_alldamage");
                break;
            case ECharacterSheetValueType.all_defense_mod:
                builder = StringManager.GetString("stat_alldefense").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_boost_alldefense");
                break;
            case ECharacterSheetValueType.health:
                builder = StringManager.GetString("stat_health").ToUpperInvariant() + ": ";
                switch (GameMasterScript.gmsSingleton.gameMode)
                {
                    case GameModes.NORMAL:
                        builder += StringManager.GetString("desc_health");
                        break;
                    case GameModes.ADVENTURE:
                        builder += StringManager.GetString("desc_health_softcore");
                        break;
                    case GameModes.HARDCORE:
                        builder += StringManager.GetString("desc_health_ironman");
                        break;
                }
                break;
            case ECharacterSheetValueType.stamina:
                builder = StringManager.GetString("stat_stamina").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_stamina");
                break;
            case ECharacterSheetValueType.energy:
                builder = StringManager.GetString("stat_energy").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_energy");
                break;
            case ECharacterSheetValueType.crit_damage:
                builder = StringManager.GetString("misc_crit_damage").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_crit_damage");
                break;
            case ECharacterSheetValueType.block_chance:
                builder = StringManager.GetString("stat_blockchance") + ": ";
                builder += StringManager.GetString("desc_block_chance");
                break;
            case ECharacterSheetValueType.dodge_chance:
                builder = StringManager.GetString("stat_dodgechance").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_dodge_chance");
                break;
            case ECharacterSheetValueType.parry_chance:
                builder = StringManager.GetString("stat_parrychance").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_parry");
                break;
            case ECharacterSheetValueType.element_physical:
                builder = StringManager.GetString("misc_dmg_physical").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_physical");
                builder = builder.Replace("#elementicon#", "<sprite=0>     ");
                break;
            case ECharacterSheetValueType.element_fire:
                builder = StringManager.GetString("misc_dmg_fire").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_fire");
                builder = builder.Replace("#elementicon#", "<sprite=1>     ");
                break;
            case ECharacterSheetValueType.element_poison:
                builder = StringManager.GetString("misc_dmg_poison").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_poison");
                builder = builder.Replace("#elementicon#", "<sprite=2>     ");
                break;
            case ECharacterSheetValueType.element_water:
                builder = StringManager.GetString("misc_dmg_water").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_water");
                builder = builder.Replace("#elementicon#", "<sprite=3>     ");
                break;
            case ECharacterSheetValueType.element_lightning:
                builder = StringManager.GetString("misc_dmg_lightning").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_lightning");
                builder = builder.Replace("#elementicon#", "<sprite=4>     ");
                break;
            case ECharacterSheetValueType.element_shadow:
                builder = StringManager.GetString("misc_dmg_shadow").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_element_shadow");
                builder = builder.Replace("#elementicon#", "<sprite=5>     ");
                break;
            case ECharacterSheetValueType.highest_floor:
                builder = StringManager.GetString("saveslot_highestfloor").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_highest_floor");
                break;
            case ECharacterSheetValueType.highest_floor_ever:
                builder = StringManager.GetString("highest_floor_ever").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_highest_floor_ever");
                break;
            case ECharacterSheetValueType.days_passed:
                builder = StringManager.GetString("misc_days_passed").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_days_passed");
                break;
            case ECharacterSheetValueType.monsters_killed:
                builder = StringManager.GetString("monsters_defeated").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_monsters_killed");
                break;
            case ECharacterSheetValueType.champs_killed:
                builder = StringManager.GetString("champions_defeated").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_champs_killed");
                break;
            case ECharacterSheetValueType.steps_taken:
                builder = StringManager.GetString("steps_taken").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_steps_taken");
                break;
            case ECharacterSheetValueType.favorite_job:
                builder = StringManager.GetString("favorite_job").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_favorite_job");
                break;
            case ECharacterSheetValueType.total_characters:
                builder = StringManager.GetString("total_characters").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_total_characters");
                break;
            case ECharacterSheetValueType.playtime:
                builder = StringManager.GetString("playtime").ToUpperInvariant() + ": ";
                builder += StringManager.GetString("desc_playtime");
                break;
            case ECharacterSheetValueType.pandora_boxes:
                builder = StringManager.GetString("ui_pandora_opened").ToUpperInvariant() + ": ";
                float calcTreasureUp = GameMasterScript.PANDORA_BONUS_MAGICCHANCE * GameMasterScript.heroPCActor.numPandoraBoxesOpened;
                if (calcTreasureUp > GameMasterScript.PANDORA_BONUS_MAGICCHANCE_CAP)
                {
                    calcTreasureUp = GameMasterScript.PANDORA_BONUS_MAGICCHANCE_CAP;
                }
                calcTreasureUp *= 100f;
                StringManager.SetTag(0, "<color=yellow>+" + calcTreasureUp + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>");

                float calcGoldUp = GameMasterScript.PANDORA_BONUS_MONEY * GameMasterScript.heroPCActor.numPandoraBoxesOpened;
                calcGoldUp *= 100f;
                StringManager.SetTag(1, "<color=yellow>"+ + calcGoldUp + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>");

                float calcMonsterDefense = GameMasterScript.GetPandoraMonsterDefenseUpValue() * GameMasterScript.heroPCActor.numPandoraBoxesOpened;                
                if (calcMonsterDefense > GameMasterScript.GetPandoraMonsterDefenseCapValue())
                {
                    calcMonsterDefense = GameMasterScript.GetPandoraMonsterDefenseCapValue();
                }
                calcMonsterDefense *= 100f;
                StringManager.SetTag(3, UIManagerScript.redHexColor + "+" + calcMonsterDefense + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>");

                float calcDamageUp = GameMasterScript.GetPandoraMonsterDamageUpValue() * GameMasterScript.heroPCActor.numPandoraBoxesOpened;
                calcDamageUp *= 100f;
                StringManager.SetTag(2, UIManagerScript.redHexColor + "+" + calcDamageUp + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + "</color>");

                builder += StringManager.GetString("desc_pandora_boxes");
                break;
            default:
                builder = thisValue.ToString() + ": NYI";
                break;

        }

        // Core stat? Show base vs. modified
        if (bAppendStatInfo)
        {
            string strStatReplace = "";
            int baseValue = (int)GameMasterScript.heroPCActor.myStats.GetStat(checkStat, StatDataTypes.TRUEMAX);
            int modvalue = (int)GameMasterScript.heroPCActor.myStats.GetStat(checkStat, StatDataTypes.CUR);
            if (modvalue >= baseValue)
            {
                strStatReplace = UIManagerScript.greenHexColor + modvalue + "</color>";
            }
            else
            {
                strStatReplace = UIManagerScript.redHexColor + modvalue + "</color>";
            }
            strStatReplace += " (" + StringManager.GetString("ui_stats_base") + " " + baseValue + ")";

            builder = builder.Replace("#CALC#", strStatReplace);
        }

        txt_TooltipScroll.text = CustomAlgorithms.ParseLiveMergeTags(builder);
    }

}


[System.Serializable]
public class CharacterSheetElementalSpriteParing
{
    public Switch_UICharacterSheet.ECharacterSheetValueType elementInfo;
    public Sprite                                           sprite;
}

public class CharacterSheetInfoPoint : UIManagerScript.UIObject
{
    public TextMeshProUGUI txt_label;
    public TextMeshProUGUI txt_value;
    public TextMeshProUGUI txt_secondaryValue;
    public Image img_divider;
    private Switch_UICharacterSheet.ECharacterSheetValueType myValueType;

    //This setup would be a touch cleaner as a Component
    //where the values for the txtmeshpro objects can be assigned in the prefab.
    //However, we don't want the extra monobehaviors running when all they do is store data. 
    public CharacterSheetInfoPoint(GameObject obj)
    {
        gameObj = obj;
        TextMeshProUGUI[] txtObjects = gameObj.GetComponentsInChildren<TextMeshProUGUI>();

        //If we have only one text object (such as Pet info) it's where our values get written.
        txt_value = txtObjects[0];

        //But otherwise the first object is a label.
        if (txtObjects.Length > 1)
        {
            txt_label = txtObjects[0];
            txt_value = txtObjects[1];
        }

        //We won't always have this, but we might!
        if (txtObjects.Length > 2)
        {
            txt_secondaryValue = txtObjects[2];
        }

        Image[] childImages = gameObj.GetComponentsInChildren<Image>();
        if (childImages != null && childImages.Length > 0)
        {
            img_divider = childImages[0];
        }
    }

    //Determines what we are going to display
    public void SetValue(Switch_UICharacterSheet.ECharacterSheetValueType newValueType)
    {
        myValueType = newValueType;
    }

    public override void Update()
    {
        if (myValueType == Switch_UICharacterSheet.ECharacterSheetValueType.space)
        {
            return;
        }

        Switch_UICharacterSheet.SetInformationForValue(myValueType, this);
    }

}
 