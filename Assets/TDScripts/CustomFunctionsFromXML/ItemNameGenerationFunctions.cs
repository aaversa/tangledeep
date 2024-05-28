using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemNameGenerationFunctions
{

    public static string BuildRuneOfKnowledgeName(Item itemBeingNamed)
    {
        string baseName = StringManager.GetString("exp_item_runeofknowledge");
        string abilRef = itemBeingNamed.ReadActorDataString("teachskill");
        AbilityScript template = GameMasterScript.masterAbilityList[abilRef];
        baseName += ": " + template.abilityName;
        baseName = UIManagerScript.goldHexColor + baseName + "</color>";
        return baseName;
    }

}
