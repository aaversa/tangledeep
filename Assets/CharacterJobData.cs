using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class JobAbility : ISelectableUIObject
{
    public AbilityScript ability;
    public int jpCost;
    public int masterCost;
    public int upgradeCost;
    public CharacterJobs jobParent;
    public bool innate;
    public string abilityRef;
    public string extraSkillRef;
    public int innateReq;
    public bool jobMasterAbility;
    public bool repeatBuyPossible;
    public bool postMasteryAbility;
    public int maxBuysPossible;
    // Could put requirements here.

    #region Interface Jibba

    public Sprite GetSpriteForUI()
    {
        return ability.GetSpriteForUI();
    }

    public string GetNameForUI()
    {
        return ability.GetNameForUI();
    }

    public string GetInformationForTooltip()
    {
        return GetAbilityInformation();
    }

    #endregion

    public string GetAbilityInformation()
    {
        AbilityComponent ac = GameMasterScript.heroPCActor.myAbilities;
        string text = "<color=yellow>" + ability.abilityName + "</color>\n";

        int localCost = jpCost;
        if (GameMasterScript.heroPCActor.startingJob != GameMasterScript.heroPCActor.myJob.jobEnum 
            && localCost < GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST && jobParent != CharacterJobs.SHARA)
        {
            localCost = GameMasterScript.MINIMUM_NON_STARTING_JOB_JP_COST;
        }

        if (RandomJobMode.IsCurrentGameInRandomJobMode()) localCost = RandomJobMode.GetSkillCost(this);

        if (!ac.HasAbility(ability) || repeatBuyPossible)
        {
            text += StringManager.GetString("ui_cost_to_learn") + ": " + UIManagerScript.cyanHexColor + localCost + "</color>";
        }
        else
        {
            text += "<color=yellow>" + StringManager.GetString("ui_mastered_very_excite") + "</color>";
        }

        text += "\n\n" + ability.GetAbilityInformation();

        if (repeatBuyPossible)
        {
            text += "\n\n" + UIManagerScript.greenHexColor + StringManager.GetString("exp_misc_repeatbuy") + "</color>";
        }

        return text;
    }
}

public class CharacterJobData {

    public string jobName;
    public string jobDescription;

    string displayName;
    public string DisplayName
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? StringManager.GetString("job_wanderer") : displayName;
        }
        set
        {
            displayName = value;
        }
    }


    string bonusDescription1;

    public string BonusDescription1
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? RandomJobMode.GetBonusDescription(0) : bonusDescription1;
        }
        set
        {
            bonusDescription1 = value;
        }
    }

    string bonusDescription2;

    public string BonusDescription2
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? RandomJobMode.GetBonusDescription(1) : bonusDescription2;
        }
        set
        {
            bonusDescription2 = value;
        }
    }

    string bonusDescription3;

    public string BonusDescription3
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? RandomJobMode.GetBonusDescription(2) : bonusDescription3;
        }
        set
        {
            bonusDescription3 = value;
        }
    }

    public string unbakedBonusDescription1;
    public string unbakedBonusDescription2;
    public string unbakedBonusDescription3;

    public int difficulty;
    public string prefab;
    public string portraitSpriteRef;
    private Sprite _portraitSprite;
    public Sprite PortraitSprite
    {
        get
        {
            if (_portraitSprite != null)
            {
                return _portraitSprite;
            }
            if (string.IsNullOrEmpty(portraitSpriteRef))
            {
                return null;
            }
            if (portraitSpriteRef == "SwordDancerPortrait" && GameMasterScript.gmsSingleton.lunarNewYearEnabled)
            {
                portraitSpriteRef = "LNY_SwordDancerPortrait";
            }
            _portraitSprite = UIManagerScript.GetPortraitForDialog(portraitSpriteRef)[0];
            return _portraitSprite;
        }
        set
        {
            _portraitSprite = value;
        }
    }

    private List<JobAbility> jobAbilities;

    public List<JobAbility> JobAbilities
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? RandomJobMode.randomizedJobAbilitiesForThisRun : jobAbilities;
        }
        set
        {
            jobAbilities = value;
        }
    }


    JobAbility masterAbility;

    public JobAbility MasterAbility
    {
        get
        {
            return RandomJobMode.IsCurrentGameInRandomJobMode() ? RandomJobMode.GetMasterAbility() : masterAbility;
        }
        set
        {
            masterAbility = value;
        }
    }

    public CharacterJobs jobEnum;
    public List<string> startingItems;
    public string startingWeapon;
    public float[] statGrowth;
    public int masterJP;
    public List<string> capstoneAbilities;
    public Dictionary<int, List<string>> emblemMagicMods;
    public List<string> numberTags;

    public CharacterJobData()
    {        
        jobAbilities = new List<JobAbility>(20);
        capstoneAbilities = new List<string>(5);
        startingItems = new List<string>(3);
        statGrowth = new float[5]; // 5 core stats, not counting hp/stam/energy
        difficulty = 1;
        emblemMagicMods = new Dictionary<int, List<string>>(); // Each tier can have multiple possible mods available
        numberTags = new List<string>();
    }

    public string GetBaseDisplayName()
    {
        return displayName;
    }

    public void ParseNumberTags()
    {
        unbakedBonusDescription1 = bonusDescription1;
        unbakedBonusDescription2 = bonusDescription2;
        unbakedBonusDescription3 = bonusDescription3;
        if (!numberTags.Any()) return;
        for (int i = 0; i < numberTags.Count; i++)
        {
            bonusDescription1 = bonusDescription1.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
            bonusDescription2 = bonusDescription2.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
            bonusDescription3 = bonusDescription3.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
        }
    }

    public string GetBonusDescription(int tier)
    {
        switch(tier)
        {
            case 0:
            default:
                return bonusDescription1;
            case 1:
                return bonusDescription2;
            case 2:
                return bonusDescription3;
        }
    }

    public string GetDifficultyString()
    {
        string builder = "(<color=yellow>" + StringManager.GetString("ui_dreamcaster_difficulty") + ":</color> ";

        if (difficulty == 1)
        {
            builder += UIManagerScript.greenHexColor + StringManager.GetString("difficulty_4_soft");
        }
        else if (difficulty == 2)
        {
            builder += "<color=yellow>" + StringManager.GetString("difficulty_5");
        }
        else if (difficulty == 3)
        {
            builder += UIManagerScript.orangeHexColor + StringManager.GetString("difficulty_7");
        }
        else if (difficulty == 4)
        {
            builder += UIManagerScript.orangeHexColor + StringManager.GetString("difficulty_8");
        }

        builder += "</color>)";
        return builder;
    }

    public string GetFullJobReadout(string extraText)
    {
        ParseNumberTags();
        string buildText = "";
        
        buildText += FontManager.GetLargeSizeTagForCurrentLanguage() + "<color=yellow>" + displayName + extraText + "</color></size>\n\n";

        buildText += jobDescription + " " + GetDifficultyString() + "\n\n";
        buildText += "<color=yellow>" + StringManager.GetString("ui_job_innate_bonus1") + ": </color>" + bonusDescription1 + "\n\n";
        buildText += "<color=yellow>" + StringManager.GetString("ui_job_innate_bonus2") + ": </color>" + bonusDescription2 + "\n\n";
        buildText += "<color=yellow>" + StringManager.GetString("ui_job_innate_bonus3") + ": </color>" + bonusDescription3;

        return buildText;
    }

    public static CharacterJobData GetJobDataByEnum(int value)
    {
        if (GameMasterScript.masterJobList == null) return null;
        foreach (CharacterJobData cjd in GameMasterScript.masterJobList)
        {
            if ((int)cjd.jobEnum == value)
            {
                return cjd;
            }
        }
        Debug.Log("WARNING!!! JOB DATA " + value + " not found");
        return null;

    }

    public static CharacterJobData GetJobData(string jobName)
    {
        if (GameMasterScript.masterJobList == null) return null;
        foreach (CharacterJobData cjd in GameMasterScript.masterJobList)
        {
            if (cjd.jobName.ToLowerInvariant() == jobName.ToLowerInvariant())
            {
                return cjd;
            }
        }
#if UNITY_EDITOR
        if (jobName != "BERSERKER") Debug.Log(jobName + " not found"); // cruft, don't ask :[
#endif
        return null;
    }

    public void UpdateStatJPCostsInJobData()
    {
        // scale JP based on existing purchases.

        foreach (JobAbility ja in JobAbilities)
        {
            if (ja.innate) continue;
            if (!ja.postMasteryAbility) continue;
            // Cost increases by 100jp each time, so
            int existingBoosts = GameMasterScript.heroPCActor.ReadActorData(ja.abilityRef + "_purchased");
            ja.jpCost = 100;
            if (existingBoosts >= 1)
            {
                ja.jpCost += existingBoosts * 100;
            }
        }
    }

    public void AddJobAbilityOnLoad(JobAbility ja)
    {
        jobAbilities.Add(ja);
    }

    public List<JobAbility> GetBaseJobAbilities()
    {
        return jobAbilities;
    }

}
