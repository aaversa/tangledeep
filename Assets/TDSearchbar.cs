using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Text.RegularExpressions;

enum TDSearchResults { VALID, NOTVALID, KEEPSEARCHING, COUNT }

public class TDSearchbar : MonoBehaviour {

    static List<string> searchTerms;

    public TMP_InputField myInputField;
    public TextMeshProUGUI placeholderText;
    public TextMeshProUGUI inputAreaText;

    static bool initialized;
    static bool anyORTerms;

    public static string[] rarityStringRefList = new string[] 
    {
        "misc_rarity_0",
        "misc_rarity_1",
        "misc_rarity_2",
        "misc_rarity_3",
        "misc_rarity_4a",
        "misc_rarity_4b",
        "misc_rarity_5",
    };

    static HashSet<int> indicesOfOR;

    static Dictionary<int, string[]> dictORSearchTermsSplitByRegex;

    static List<string> localizedRarityWords;


    public static void ClearSearchTerms()
    {
        searchTerms.Clear();
        // Depends on UI type
    }

    public void OnSelectSearchBox()
    {
        GameMasterScript.SetAnimationPlaying(true);
    }

    public void OnDeselectSearchBox()
    {
        GameMasterScript.SetAnimationPlaying(false);
    }

    public void OnSearchBoxEndEdit()
    {
        OnDeselectSearchBox();
    }

    public void OnSearchBoxValueChanged()
    {
        searchTerms.Clear();
        indicesOfOR.Clear();
        dictORSearchTermsSplitByRegex.Clear();
        string unparsed = myInputField.text;
        if (string.IsNullOrEmpty(unparsed))
        {
            UpdateContent();
            return;
        }

        TutorialManagerScript.searchBarUsed = true;

        unparsed = unparsed.ToLowerInvariant();
        unparsed = unparsed.Replace(", ", ",");
        searchTerms = unparsed.Split(',').ToList();
        anyORTerms = false;

        for (int i = 0; i < searchTerms.Count; i++)
        {
            // let's say we searched: "sword, axe or mace, bow"
            // "axe or mace" is in in index 1
                
            if (searchTerms[i].Contains(" or "))
            {
                indicesOfOR.Add(i); // in this example, index 1 is marked as a special "or" index that needs sub-parsing
                string[] parsedRegex = Regex.Split(searchTerms[i], @"\s+or\s+");
                dictORSearchTermsSplitByRegex.Add(i, parsedRegex);
                anyORTerms = true;
            } 

        } 
        UpdateContent();
    }

    public void UpdateContent()
    {
        // Depends on UI type
        ImpactUI_Base fsUI = UIManagerScript.singletonUIMS.GetCurrentFullScreenUI();
        if (fsUI != null)
        {
            fsUI.UpdateContent();
            return;
        }

        // Welp, must be one of Andrew's bad UIs
        if (ShopUIScript.CheckShopInterfaceState())
        {
            UIManagerScript.listArrayIndexOffset = 0;
            ShopUIScript.UpdateShop();
            
        }
        else if (ItemWorldUIScript.itemWorldInterfaceOpen)
        {
            UIManagerScript.listArrayIndexOffset = 0;
            if (ItemWorldUIScript.isItemSelected)
            {
                ItemWorldUIScript.PopulateItemList(orbs: true);
                UIManagerScript.UpdateItemWorldList(true);
            }
            else
            {
                ItemWorldUIScript.PopulateItemList(orbs: false);
                UIManagerScript.UpdateItemWorldList(false);
            }             
        }
    }

    // Use this for initialization
    void Start () {
        searchTerms = new List<string>();
        dictORSearchTermsSplitByRegex = new Dictionary<int, string[]>();

		if (!PlatformVariables.SHOW_SEARCHBARS)
		{
        	gameObject.SetActive(false);
	        return;
		}

        indicesOfOR = new HashSet<int>();

        if (inputAreaText != null)
        {
            FontManager.LocalizeMe(inputAreaText, TDFonts.WHITE_NO_OUTLINE);
        }
        if (placeholderText != null)
        {
            FontManager.LocalizeMe(placeholderText, TDFonts.WHITE_NO_OUTLINE);
            placeholderText.text = StringManager.GetString("ui_misc_searchterms");
        }
    }

    void OnEnable()
    {
        myInputField.text = "";
        if (!initialized)
        {
            Initialize();
        }
    }

    static void Initialize()
    {
        localizedRarityWords = new List<string>();
        for (int i = 0; i < rarityStringRefList.Length; i++)
        {
            localizedRarityWords.Add(StringManager.GetString(rarityStringRefList[i]).ToLowerInvariant());
        }
    }

    public static bool CheckIfItemMatchesTerms(Item itm)
    {
        if (searchTerms.Count == 0) return true;

        if (!initialized)
        {
            Initialize();
        }        

        bool valid = false;

        for (int i = 0; i < searchTerms.Count; i++)
        {
            string searchTerm = searchTerms[i];
           
            if (anyORTerms && indicesOfOR.Contains(i)) 
            {
                // let's say we searched: "sword, axe or mace, bow"
                // and we are now evaluating "axe or mace" (index 1)
                bool anyORValid = false;
                for (int x = 0; x < dictORSearchTermsSplitByRegex[i].Length; x++)
                {
                    // iterate through "axe" and "mace"
                    // if EITHER of these are valid, stop entirely
                    TDSearchResults ORresult = EvaluateSearchTerm(itm, dictORSearchTermsSplitByRegex[i][x]);
                    if (ORresult == TDSearchResults.VALID)
                    {
                        anyORValid = true;
                    }
                }
                if (!anyORValid)
                {
                    valid = false;
                    break;
                }
                else
                {
                    valid = true;
                    continue;
                }
            }

            TDSearchResults result = EvaluateSearchTerm(itm, searchTerm);
            if (result == TDSearchResults.KEEPSEARCHING)
            {
                continue;
            }
            else if (result == TDSearchResults.VALID)
            {
                valid = true;
            }
            else
            {
                // if we are invalid here, just stop
                valid = false;
                break;
            }

        }

        if (!valid)
        {
            return false;
        }

        return true;
    }

    static TDSearchResults EvaluateSearchTerm(Item itm, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return TDSearchResults.KEEPSEARCHING;
        }

        bool negation = false;
        if (searchTerm[0] == '!')
        {
            negation = true;
            searchTerm = searchTerm.Substring(1, searchTerm.Length-1);
        }

        bool valid = false;
        Equipment eq = null;
        Consumable con = null;

        bool isEquipment = false;

        if (itm.IsEquipment())
        {
            eq = itm as Equipment;
            isEquipment = true;
        }
        else if (itm.itemType == ItemTypes.CONSUMABLE)
        {
            con = itm as Consumable;
        }

        if (itm.displayName.ToLowerInvariant().Contains(searchTerm)) // Check the item name
        {
            valid = true;
        }
        if (!valid && itm.newlyPickedUp && searchTerm == "new")
        {
            valid = true;
        }
        if (!valid && itm.vendorTrash && (searchTerm == "trash" || searchTerm == "garbage"))
        {
            valid = true;
        }
        if (!valid && itm.extraDescription.ToLowerInvariant().Contains(searchTerm)) // // Check the extra description
        {
            valid = true;
        }
        if (!valid && itm.GetDisplayItemType().ToLowerInvariant().Contains(searchTerm)) // Display type: Armor, weapon, accessory, etc...
        {
            valid = true;
        }
        if (!valid && !isEquipment && con != null)
        {
            if (con.effectDescription.ToLowerInvariant().Contains(searchTerm))
            {
                valid = true;
            }
            if (!valid && con.isFood && searchTerm == "food")
            {
                valid = true;
            }
            if (!valid && con.IsCurative() && searchTerm.Contains("heal") && searchTerm != "health")
            {
                valid = true;
            }
            if (!valid && con.IsCookingIngredient() && searchTerm == "ingredient")
            {
                valid = true;
            }
        }

        if (!valid && !isEquipment)
        {
            if (searchTerm == "gem" || searchTerm == "gems")
            {
                if (itm.tags.Contains(ItemFilters.GEM))
                {
                    valid = true;
                }
            }
        }

        if (!valid && isEquipment) // How about equipment subtype? Light, medium heavy, bow, special?
        {
            if (eq.itemType == ItemTypes.ARMOR)
            {
                Armor arm = eq as Armor;
                if (Armor.armorTypesVerbose[(int)arm.armorType].Contains(searchTerm))
                {
                    valid = true;
                }
            }
            else if (eq.itemType == ItemTypes.WEAPON)
            {
                Weapon w = eq as Weapon;
                if (Weapon.weaponTypesVerbose[(int)w.weaponType].Contains(searchTerm))
                {
                    valid = true;
                }
            }
            else if (eq.itemType == ItemTypes.OFFHAND)
            {
                Offhand oh = eq as Offhand;
                if (oh.allowBow && searchTerm.Contains("quiver")) valid = true;
                else if (oh.blockChance > 0f && searchTerm.Contains("shield")) valid = true;
                else if (searchTerm.Contains("book")) valid = true;
            }
        }
        if (!valid && isEquipment)
        {
            foreach (MagicMod mm in eq.mods) // Check every mod's display name, and mod description
            {
                if (mm.modName.ToLowerInvariant().Contains(searchTerm) || mm.GetDescription().ToLowerInvariant().Contains(searchTerm))
                {
                    valid = true;
                    break;
                }
            }
            if (!valid && itm.rarity == Rarity.LEGENDARY && itm.customItemFromGenerator)
            {
                if (searchTerm.Contains("relic"))
                {
                    valid = true;
                }
            }
        }
        if (!valid && itm.dreamItem && searchTerm == "dream") // Check for dream-only items
        {
            valid = true;
        }

        if (!valid && itm.favorite && searchTerm == "favorite")
        {
            valid = true;
        }

        if (!valid && searchTerm.Contains("rank"))
        {
            // rank:5   searches for all items of this rank specifically
            // rankabove:5  searches for all items of this rank and above
            // rankbelow:5  searches for all items of this rank and below

            string[] parsed = searchTerm.Split(':');
            if (parsed.Length != 2) return TDSearchResults.KEEPSEARCHING;
            int targetRank;
            if (int.TryParse(parsed[1], out targetRank))
            {
                int iRank = BalanceData.ConvertChallengeValueToRank(itm.challengeValue);
                switch (parsed[0])
                {
                    case "rank":
                        if (iRank == targetRank)
                        {
                            valid = true;
                        }
                        break;
                    case "rankabove":
                        if (iRank > targetRank)
                        {
                            valid = true;
                        }
                        break;
                    case "rankbelow":
                        if (iRank < targetRank)
                        {
                            valid = true;
                        }
                        break;
                }
            }
        }

        if (!valid && searchTerm.Contains("rarity"))
        {
            // rarity:5   searches for all items of this rarity specifically
            // rarityabove:5  searches for all items of this rarity and above
            // raritybelow:5  searches for all items of this rarity and below

            string[] parsed = searchTerm.Split(':');

            if (parsed.Length != 2) return TDSearchResults.KEEPSEARCHING;

            if (!localizedRarityWords.Contains(parsed[1])) // make sure the typed rarity is valid
            {
                return TDSearchResults.KEEPSEARCHING;
            }

            int iRarity = (int)itm.rarity;
            string checkRarity = parsed[1].ToUpperInvariant();
            if (checkRarity == "RARE") checkRarity = "ANCIENT";

            int iTargetRarity = 0;

            try
            {
                iTargetRarity = (int)(Rarity)Enum.Parse(typeof(Rarity), checkRarity);
            }
            catch(Exception)
            {
                return TDSearchResults.KEEPSEARCHING;
            }
            

            switch (parsed[0])
            {
                case "rarity":
                    if (iRarity == iTargetRarity)
                    {
                        valid = true;
                    }
                    break;
                case "rarityabove":
                    if (iRarity > iTargetRarity)
                    {
                        valid = true;
                    }
                    break;
                case "raritybelow":
                    if (iRarity < iTargetRarity)
                    {
                        valid = true;
                    }
                    break;
            }
        }


        // does negation work? :thonking:
        if (negation)
        {
            valid = !valid;
        }

        if (!valid) // Couldn't find the term, don't bother with other terms since we must match ALL.
        {
            bool keepSearching = false;
            /* if (anyORTerms)
            {
                foreach (int index in indicesOfOR)
                {
                    Debug.Log("Compare current index i to " + (index - 1));
                    // sword or axe
                    // we didn't find sword at index 0, the OR index is at 1
                    // we can keep looking
                    if (i == index - 1)
                    {
                        keepSearching = true;
                        break;
                    }
                }
            } */

            if (!keepSearching)
            {
                return TDSearchResults.NOTVALID;
            }
            else
            {
                return TDSearchResults.KEEPSEARCHING;
            }
        }
        else        
        {
            return TDSearchResults.VALID;
        }
    }

}
