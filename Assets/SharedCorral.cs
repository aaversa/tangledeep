using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;

public class SharedCorral
{
    public static List<TamedCorralMonster> tamedMonstersSharedWithAllSlots;

    public static List<MonsterTemplateData> customMonsterTemplatesUsedByPets;

    static bool initialized;

    public static int globalSharedPetIDCounter;
    
    public static void Initialize()
    {
        if (initialized) return;

        if (Debug.isDebugBuild) Debug.Log("Shared corral initialized, everything is cleared.");

        tamedMonstersSharedWithAllSlots = new List<TamedCorralMonster>();
        customMonsterTemplatesUsedByPets = new List<MonsterTemplateData>();

        initialized = true;
    }


    public static void WriteAllTamedCorralMonstersToSave(XmlWriter metaWriter)
    {
        if (Debug.isDebugBuild) Debug.Log("Writing all tamed corral monsters to save. How many? " + tamedMonstersSharedWithAllSlots.Count);
        foreach (TamedCorralMonster tmc in tamedMonstersSharedWithAllSlots)
        {
            if (Debug.isDebugBuild) Debug.Log("Writing SharedBank tamed pet " + tmc.refName + "," + tmc.monsterObject.displayName + "," + tmc.monsterObject.actorUniqueID + "," + tmc.monsterObject.prefab + "," + tmc.sharedBankID);
            tmc.monsterObject.WriteToSave(metaWriter);
        }
    }

    public static void WriteAllCustomMonsterTemplatesToSave(XmlWriter metaWriter)
    {
        foreach(MonsterTemplateData mtd in customMonsterTemplatesUsedByPets)
        {
            MonsterTemplateSerializer.WriteCustomMonsterToSave(mtd, metaWriter);
        }
    }

    public static bool HasCustomMonsterTemplateOfNameAndPrefab(MonsterTemplateData mtd)
    {
        foreach(MonsterTemplateData myMTD in customMonsterTemplatesUsedByPets)
        {
            if (myMTD.refName == mtd.refName && myMTD.prefab == mtd.prefab && myMTD.monsterName == mtd.monsterName) return true;
        }

        return false;
    }

    public static bool HasCustomMonsterTemplateOfSameNameButDifferentContents(MonsterTemplateData mtd)
    {
        foreach (MonsterTemplateData myMTD in customMonsterTemplatesUsedByPets)
        {
            if (myMTD.refName == mtd.refName && myMTD.prefab != mtd.prefab && myMTD.monsterName != mtd.monsterName) return true;
        }

        return false;
    }

    public static bool HasCustomMonsterTemplateOfRefName(string refName)
    {
        foreach(MonsterTemplateData mtd in customMonsterTemplatesUsedByPets)
        {
            if (mtd.refName == refName) return true;
        }

        return false;
    }

    public static bool CheckForMonsterTemplateRefInListOfAllPets(string refName)
    {
        foreach(TamedCorralMonster tcm in tamedMonstersSharedWithAllSlots)
        {
            if (tcm.monsterObject.actorRefName == refName) return true;
        }

        return false;
    }

    public static int GetUniqueSharedPetID()
    {        
        globalSharedPetIDCounter++;
        Debug.Log("Returning a new global shared pet ID " + (globalSharedPetIDCounter-1));
        return (globalSharedPetIDCounter - 1);
    }
}
