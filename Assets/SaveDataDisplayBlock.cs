using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveDataDisplayBlock : MonoBehaviour
{
    public enum ESaveDataDisplayType
    {
        load_game = 0,
        no_character_but_world_exists,
        empty_af,
        max
    }

    public int slotIndex;

    [HideInInspector]
    public SaveDataDisplayBlockInfo saveInfo;

    [Header("Load Game")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtJobLevelAndMode;
    public TextMeshProUGUI txtLocation;
    public TextMeshProUGUI txtTimePlayed;

    public Image imgPortrait;
    public Image imgNGPlusIcon;
    public Image imgBackground;

    [Header("No Data / New Game")]
    public TextMeshProUGUI txtNewCharacter;
    public TextMeshProUGUI txtHeroesLost;
    public TextMeshProUGUI txtCampaignTime;
    public TextMeshProUGUI txtCampaignDifficulty;

    [HideInInspector]
    public bool bInfoIsDirty;

    private bool bInDeleteMode;

    [Header("Border Colors")]
    public Color colorBorderNormal;
    public Color colorBorderDeleteWarning;

    public ESaveDataDisplayType displayType { get; private set; }

    bool localizedEver;

    void Update ()
    {
        if (bInfoIsDirty)
        {
            UpdateCharacterInformation();
            bInfoIsDirty = false;
        }
	}

    void UpdateCharacterInformation()
    {
        //Debug.Log("Updating info for " + saveInfo.dataDisplayType + " " + slotIndex);

        SetDisplayType(saveInfo.dataDisplayType);

        //Change the way we display our name if we are in delete mode.
        if (bInDeleteMode)
        {
            StringManager.SetTag(0, saveInfo.strHeroName);
            txtName.text = StringManager.GetString("saveload_delete_specific"); // "Delete " + saveInfo.strHeroName + "?";
                
            if (displayType == ESaveDataDisplayType.no_character_but_world_exists)
            {
                //There's no hero name here, but we are showing campaign data.
                txtNewCharacter.text = StringManager.GetString("saveload_delete_nohero"); // "Delete this data?";
            }
            else
            {
                //otherwise, don't say "start new game here" because we cannot.
                txtNewCharacter.text = StringManager.GetString("saveload_file_empty"); // "File empty!";
            }
        }
        else
        {
            txtName.text = (slotIndex+1) + ". " + saveInfo.strHeroName;
        }
        txtJobLevelAndMode.text = StringManager.GetString("misc_xp_level") +
                                  " " + saveInfo.iHeroLevel +
                                  " " + saveInfo.strJobName + ", " +
                                  saveInfo.strGameModeInfo;

        txtLocation.text = saveInfo.strLocation;

        if (saveInfo.challengeMode != ChallengeTypes.NONE)
        {
            txtLocation.text += " (" + UIManagerScript.greenHexColor + StringManager.GetString("ui_btn_weeklychallenge") + ")";
        }

        //same value, but two different positions in the UI
        txtTimePlayed.text = StringManager.GetString("playtime") + ": " + saveInfo.strTimePlayed;
        StringManager.SetTag(0, saveInfo.strTimePlayed);
        txtCampaignTime.text = StringManager.GetString("saveload_total_time"); // "Total Adventure Time: " + saveInfo.strTimePlayed;
        txtCampaignDifficulty.text = saveInfo.strGameModeInfo;

        saveInfo.GenerateCampaignData();        

        if (string.IsNullOrEmpty(txtCampaignDifficulty.text))
        {
            txtCampaignDifficulty.text = StringManager.GetString("highest_floor_ever") + ": " + saveInfo.lowestFloor;
        }        

        txtHeroesLost.text = saveInfo.strCampaignData;

        imgPortrait.sprite = saveInfo.portrait;
        GameStartData.saveSlotNGP[slotIndex] = saveInfo.iNewGamePlusRank;

        GameStartData.beatGameStates[slotIndex] = saveInfo.bGameClearSave;

        imgNGPlusIcon.enabled = saveInfo.iNewGamePlusRank > 0;

        //pretty <3 
        if (imgNGPlusIcon.enabled)
        {
            StartCoroutine(PulseNGPlusStar(imgNGPlusIcon));
        }

        //Debug.Log(txtName.text + " " + txtJobLevelAndMode.text);

        if (localizedEver) return;

        localizedEver = true;

        FontManager.LocalizeMe(txtName, TDFonts.WHITE);
        FontManager.LocalizeMe(txtJobLevelAndMode, TDFonts.WHITE);
        FontManager.LocalizeMe(txtLocation, TDFonts.WHITE);
        FontManager.LocalizeMe(txtTimePlayed, TDFonts.WHITE);
        FontManager.LocalizeMe(txtNewCharacter, TDFonts.WHITE);
        FontManager.LocalizeMe(txtHeroesLost, TDFonts.WHITE);
        FontManager.LocalizeMe(txtCampaignTime, TDFonts.WHITE);
        FontManager.LocalizeMe(txtCampaignDifficulty, TDFonts.WHITE);
    }

    /// <summary>
    /// Make the star bounce a little, how cute.
    /// </summary>
    static IEnumerator PulseNGPlusStar(Graphic star)
    {
        while (star.enabled)
        {
            float fScale = 0.9f + 0.25f * Mathf.Sin((Time.realtimeSinceStartup * 6.28f) % 6.28f);
            star.rectTransform.localScale = new Vector3( fScale, fScale, fScale);
            yield return null;
        }
    }
    
    /// <summary>
    /// Sets the delete flag and updates the visuals as well.
    /// </summary>
    /// <param name="bShouldDelete"></param>
    public void SetDeleteMode(bool bShouldDelete)
    {
        bInDeleteMode = bShouldDelete;
        imgBackground.color = bInDeleteMode ? colorBorderDeleteWarning : colorBorderNormal;
    }


    /// <summary>
    /// Change the display type of this box. 
    /// </summary>
    /// <param name="newType">Sets one of three types: load existing hero, start hero in a pre-existing world, or start a new adventure.</param>
    public void SetDisplayType(ESaveDataDisplayType newType)
    {
        displayType = newType;
        StringManager.SetTag(0, (slotIndex+1).ToString());
        switch (displayType)
        {
            case ESaveDataDisplayType.load_game:
                txtName.enabled = true;
                txtJobLevelAndMode.enabled = true;
                txtLocation.enabled = true;
                txtTimePlayed.enabled = true;
                imgPortrait.enabled = true;

                txtNewCharacter.enabled = false;
                txtHeroesLost.enabled = false;
                txtCampaignTime.enabled = false;
                txtCampaignDifficulty.enabled = false;
                break;
            case ESaveDataDisplayType.empty_af:
                txtName.enabled = false;
                txtJobLevelAndMode.enabled = false;
                txtLocation.enabled = false;
                txtTimePlayed.enabled = false;
                imgPortrait.enabled = false;
                txtHeroesLost.enabled = false;
                txtCampaignTime.enabled = false;
                txtCampaignDifficulty.enabled = false;

                txtNewCharacter.enabled = true;
                txtNewCharacter.text = StringManager.GetString("saveload_new_file"); // "Start New Adventure!";
                break;
            case ESaveDataDisplayType.no_character_but_world_exists:
                txtName.enabled = false;
                txtJobLevelAndMode.enabled = false;
                txtLocation.enabled = false;
                txtTimePlayed.enabled = false;
                imgPortrait.enabled = false;

                txtHeroesLost.enabled = true;
                txtCampaignTime.enabled = true;
                txtCampaignDifficulty.enabled = true;
                txtNewCharacter.enabled = true;
                txtNewCharacter.text = StringManager.GetString("saveload_new_hero"); // "Create New Hero!";
                break;

        }
    }

}

//Display info only, none of this does anything except feed the class above.
public struct SaveDataDisplayBlockInfo
{
    public string strHeroName;
    public string strJobName;
    public int iHeroLevel;
    public string strTimePlayed;
    public string strLocation;
    public Sprite portrait;

    public ChallengeTypes challengeMode;

    // >0 means this is NG+
    public int iNewGamePlusRank;

    public int lowestFloor;
    public int daysPassed;

    // True if this game save will prompt us to launch into NG+
    public bool bGameClearSave;

    public string strGameModeInfo;
    public string strCampaignData;
    public SaveDataDisplayBlock.ESaveDataDisplayType dataDisplayType;

    public int numCharacters;

    public int id;

    public void Clear()
    {        
        strHeroName    = "";
        strJobName     = "";
        strGameModeInfo = "";
        strTimePlayed  = "";
        strLocation    = "";
        iHeroLevel = 1;
        numCharacters = 0;
        portrait = null;
        iNewGamePlusRank = 0;
        bGameClearSave = false;
        strCampaignData = "";
        SetDataDisplayType(SaveDataDisplayBlock.ESaveDataDisplayType.load_game);
        lowestFloor = 0;
        daysPassed = 0;
        challengeMode = ChallengeTypes.NONE;
    }

    public void SetDataDisplayType(SaveDataDisplayBlock.ESaveDataDisplayType eType)
    {
        //Debug.Log("Display type setting to " + eType);
        dataDisplayType = eType;
    }

    public void GenerateCampaignData()
    {
        strCampaignData = StringManager.GetString("misc_days_passed") + ": " + daysPassed + ", " + StringManager.GetString("total_characters") + ": " + numCharacters;
    }
}
