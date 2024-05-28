using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

[System.Serializable]
public class ModMaker : MonoBehaviour {

    public static ModMaker singleton;
    public Dropdown dropdownContentType;

    [Header("Items")]
    public GameObject itemContainer;
    public Dropdown dropdownItemContentType;
    public InputField inputItemDisplayName;
    public InputField inputItemRefName;

    [Header("Equipment")]
    public GameObject equipmentContainer;
    public Dropdown dropdownEQSubType;

    bool initialized;

    PlayerModfileTypes currentContentType;

    // Use this for initialization
    void Start() {
        if (singleton != null && singleton != this)
        {
            return;
        }
        singleton = this;

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        List<int> exclusions = new List<int>() { (int)PlayerModfileTypes.COUNT };
        PopulateDropdownFromEnum(dropdownContentType, new PlayerModfileTypes(), exclusions);
        exclusions = new List<int>() { (int)ItemTypes.ANY, (int)ItemTypes.MAGICAL, (int)ItemTypes.EMBLEM, (int)ItemTypes.COUNT };
        PopulateDropdownFromEnum(dropdownItemContentType, new ItemTypes(), exclusions);        

        // Finished!

        initialized = true;
        currentContentType = PlayerModfileTypes.ITEMS;
        dropdownContentType.value = (int)PlayerModfileTypes.ITEMS;
        dropdownContentType.RefreshShownValue();
    }

    void PopulateDropdownFromEnum(Dropdown dd, Enum tEnum, List<int> exclusions = null)
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        var castTo = tEnum.GetType();
        var enumValues = Enum.GetValues(castTo);
        //var parsedEnum = Convert.ChangeType(Enum.Parse(castTo, valueFromSql), castTo);

        int i = 0;
        foreach (var value in enumValues)
        {
            bool skip = false;
            if (exclusions != null)
            {
                if (exclusions.Contains(i))
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;
            Dropdown.OptionData toAdd = new Dropdown.OptionData();
            options.Add(toAdd);
            toAdd.text = value.ToString();
            i++;
        }

        dd.ClearOptions();
        dd.AddOptions(options);


        /* for (int i = 0; i < enumValues.Length; i++)
        {
            bool skip = false;
            if (exclusions != null)
            {
                if (exclusions.Contains(i))
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;
            Dropdown.OptionData toAdd = new Dropdown.OptionData();
            //toAdd.text = ((parsedEnum)i).ToString();
            options.Add(toAdd);
        } */
    }

    public void OnItemTypeValueChanged()
    {
        if (!initialized) return;
        ItemTypes iType = ItemTypes.COUNT;
        try { iType = (ItemTypes)Enum.Parse(typeof(ItemTypes), dropdownItemContentType.captionText.text); }
        catch (Exception e)
        {
            Debug.Log("Could not parse enum value " + e);
            return;
        }

        List<int> exclusions = null;

        switch (iType)
        {
            case ItemTypes.ARMOR:
            case ItemTypes.WEAPON:
            case ItemTypes.OFFHAND:
            case ItemTypes.EMBLEM:
            case ItemTypes.ACCESSORY:
                equipmentContainer.SetActive(true);
                if (iType == ItemTypes.WEAPON)
                {
                    exclusions = new List<int>() { (int)WeaponTypes.ANY, (int)WeaponTypes.COUNT, (int)WeaponTypes.NATURAL, (int)WeaponTypes.SLING };
                    PopulateDropdownFromEnum(dropdownEQSubType, new WeaponTypes(), exclusions);
                }
                else if (iType == ItemTypes.ARMOR)
                {
                    exclusions = new List<int>() { (int)ArmorTypes.NATURAL, (int)ArmorTypes.COUNT };
                    PopulateDropdownFromEnum(dropdownEQSubType, new ArmorTypes(), exclusions);
                }
                break;
            default:
                equipmentContainer.SetActive(false);
                break;
        }
    }

    public void OnModTypeValueChanged()
    {
        if (!initialized) return;

        PlayerModfileTypes getValue = PlayerModfileTypes.COUNT;

        try { getValue = (PlayerModfileTypes)Enum.Parse(typeof(PlayerModfileTypes), dropdownContentType.captionText.text); }
        catch(Exception e)
        {
            Debug.Log("Could not parse enum value " + e);
            return;
        }

        if (getValue != currentContentType)
        {
            ClearEditingUI();
        }
        currentContentType = getValue;

        switch (getValue)
        {
            case PlayerModfileTypes.ITEMS:
                itemContainer.SetActive(true);
                break;
        }
    }

    void ClearEditingUI()
    {
        itemContainer.SetActive(false);
        equipmentContainer.SetActive(false);
    }
}
