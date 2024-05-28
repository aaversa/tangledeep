using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using System;

public enum SharedSlotProgressFlags
{
    RIVERSTONE_WATERWAY,
    SHARA_MODE,
    ESCAPED_FROG,
    COUNT
}

public partial class SharedBank
{

    public static List<Item> allItemsInBank;
    public static int goldBanked;
    public static bool[] jobsUnlocked;
    public static List<string> creationFeatsUnlocked;

    public static int bankerMaxItems;

    public static Dictionary<string, Item> allRelicTemplates;

    public static int generatedLegendaryCounter = 0;

    public static bool[] sharedProgressFlags;

    static bool initialized;

    public static Seasons customSeasonValue;

    public static void Initialize()
    {
        if (initialized) return;

        //Debug.Log("<color=green>INITIALIZED SHARED DATA</color>");

        jobsUnlocked = new bool[(int)CharacterJobs.COUNT];
        creationFeatsUnlocked = new List<string>();
        allItemsInBank = new List<Item>();
        sharedProgressFlags = new bool[(int)SharedSlotProgressFlags.COUNT];
        allRelicTemplates = new Dictionary<string, Item>();
        goldBanked = 0;
        bankerMaxItems = 0;

        UnlockStandardJobs();

        SharedCorral.Initialize();

        initialized = true;
    }

    static void UnlockStandardJobs()
    {
        
        for (int i = 0; i < jobsUnlocked.Length; i++)
        {
            jobsUnlocked[i] = true;
        }

        jobsUnlocked[(int)CharacterJobs.HUSYN] = false;
        jobsUnlocked[(int)CharacterJobs.WILDCHILD] = false;
        jobsUnlocked[(int)CharacterJobs.GAMBLER] = false;
        jobsUnlocked[(int)CharacterJobs.SHARA] = false;
        jobsUnlocked[(int)CharacterJobs.MIRAISHARA] = false;
    }

    public static bool CheckIfJobIsUnlocked(CharacterJobs cj)
    {
        return jobsUnlocked[(int)cj];
    }

    public static bool CheckSharedProgressFlag(SharedSlotProgressFlags flag)
    {
        return sharedProgressFlags[(int)flag];
    }

    public static void UnlockJob(CharacterJobs cj, bool forceUnlock = false)
    {
        if (CheckIfJobIsUnlocked(cj) && !forceUnlock) return;

        //Debug.Log("Unlocking job " + cj + " force? " + forceUnlock);

        if (GameMasterScript.gameLoadSequenceCompleted && RandomJobMode.IsCurrentGameInRandomJobMode()) return;

        UnlockJobNoPopup(cj);
        
        CharacterJobData cjd = CharacterJobData.GetJobDataByEnum((int)cj);

        if (GameMasterScript.gameLoadSequenceCompleted && GameMasterScript.heroPCActor.myJob.jobEnum == cj) return;

        StringManager.SetTag(0, cjd.DisplayName.ToUpperInvariant());
        GameMasterScript.gmsSingleton.SetTempStringData("unlockjob", StringManager.GetTag(0));
        GameLogScript.LogWriteStringRef("log_job_unlocked");
        UIManagerScript.StartConversationByRef("job_unlock", DialogType.KEYSTORY, null);
    }

    public static void UnlockJobNoPopup(CharacterJobs cj)
    {
        jobsUnlocked[(int)cj] = true;
    }

    public static void UnlockFeat(string featSkillRef)
    {
        if (creationFeatsUnlocked.Contains(featSkillRef)) return;
        creationFeatsUnlocked.Add(featSkillRef);

        CreationFeat findFeat = null;

        foreach (CreationFeat cf in GameMasterScript.masterFeatList)
        {
            if (cf.skillRef == featSkillRef)
            {
                findFeat = cf;
                break;
            }
        }

        StringManager.SetTag(0, findFeat.featName.ToUpperInvariant());
        GameMasterScript.gmsSingleton.SetTempStringData("unlockfeat", StringManager.GetTag(0));
        GameLogScript.LogWriteStringRef("log_feat_unlocked");
        UIManagerScript.StartConversationByRef("feat_unlock", DialogType.KEYSTORY, null);
    }

    public static bool IsFeatUnlocked(string featName)
    {
        return creationFeatsUnlocked.Contains(featName);
    }
   
    public static void AddGeneratedRelic(Item itemCreated)
    {
        allRelicTemplates.Add(itemCreated.actorRefName, itemCreated);
        itemCreated.saveSlotIndexForCustomItemTemplate = GameStartData.saveGameSlot;
    }

    public static int CalculateMaxBankableItems()
    {
        if (bankerMaxItems < GameMasterScript.DEFAULT_MAX_BANKABLE_ITEMS)
        {
            return GameMasterScript.DEFAULT_MAX_BANKABLE_ITEMS;
        }
        else
        {
            return bankerMaxItems;
        }
    }

    public static void UpgradeBankStorage()
    {
        bankerMaxItems = CalculateNextBankItemStorageTier();
    }

    public static int CalculateNextBankItemStorageTier()
    {
        int currentMaxItems = CalculateMaxBankableItems();

        int nextTier = currentMaxItems + 5;

        return nextTier;
    }

    public static int CalculateBankUpgradeStorageCost()
    {
        float itemDifferenceVsBase = (float)CalculateNextBankItemStorageTier() - GameMasterScript.DEFAULT_MAX_BANKABLE_ITEMS;

        float baseCostMult = Mathf.Pow(itemDifferenceVsBase, 1.2f);

        int finalCost = (int)(baseCostMult * 260f);

        if (finalCost > GameMasterScript.MAX_GOLD)
        {
            finalCost = GameMasterScript.MAX_GOLD - 1;
        }

        return finalCost;
    }

    static float timeAtLastSharedDataSave;

    public static void RemoveRelicsFromHeroOnGameOver()
    {
        for (int i = 0; i < GameMasterScript.heroPCActor.myEquipment.equipment.Length; i++)
        {
            Equipment eq = GameMasterScript.heroPCActor.myEquipment.equipment[i] as Equipment;
            if (eq == null) continue;
            RemoveItemIfCustomFromGenerator(eq);
        }

        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            RemoveItemIfCustomFromGenerator(itm);
        }

        TrySaveSharedProgress();
    }

    static void RemoveItemIfCustomFromGenerator(Item itm)
    {        
        if (!itm.customItemFromGenerator) return;

        allRelicTemplates.Remove(itm.actorRefName);
        if (Debug.isDebugBuild) Debug.Log("Removed relic template " + itm.actorRefName);
    }

    public static bool ShouldUseSharedBankForCurrentGame()
    {
        if (GameStartData.GetGameMode() == GameModes.HARDCORE ||
            RandomJobMode.IsCurrentGameInRandomJobMode() ||
            SharaModeStuff.IsSharaModeActive() || 
            GameStartData.challengeTypeBySlot[GameStartData.saveGameSlot] != ChallengeTypes.NONE)
        {
            return false;
        }

        return true;
    }

    public static void AddSharedProgressFlag(SharedSlotProgressFlags flag)
    {
        sharedProgressFlags[(int)flag] = true;
    }

    public static void SetCustomSeasonValue(Seasons seasonVal)
    {
        customSeasonValue = seasonVal;
#if UNITY_SWITCH
        if (Debug.isDebugBuild) Debug.Log("SEASONDEBUG set custom season value in SharedBank to " + customSeasonValue);
#endif
    }

    public static void MarkRelicTemplateForDeletion(string refName)
    {
        Item template;
        if (allRelicTemplates.TryGetValue(refName, out template))
        {
            template.SetActorData("loserelic", 1);
            if (Debug.isDebugBuild) Debug.Log("Relic template " + refName + " marked for deletion from shared bank.");
        }
    }

    public static void MarkRelicTemplateAsInUseOnCurrentSlot(string relicName)
    {
        Item template;
        if (allRelicTemplates.TryGetValue(relicName, out template))
        {
            template.saveSlotIndexForCustomItemTemplate = GameStartData.saveGameSlot;
            if (Debug.isDebugBuild) Debug.Log("Relic template " + relicName + " is now in use by slot " + template.saveSlotIndexForCustomItemTemplate);
        }
    }

    public static void MarkRelicTemplateAsReturnedToSharedBank(string relicName)
    {
        Item template;
        if (allRelicTemplates.TryGetValue(relicName, out template))
        {
            template.saveSlotIndexForCustomItemTemplate = 99;
            if (Debug.isDebugBuild) Debug.Log("Relic template " + relicName + " is no longer in use by any slot.");
        }
    }
}
