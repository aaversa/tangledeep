using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationStartScripts : MonoBehaviour
{
    public static void SetSpecialEventChangePrice(string args)
    {
        int price = GameMasterScript.heroPCActor.myStats.GetLevel() * 500;
        if (GameMasterScript.heroPCActor.myStats.GetLevel() > 10)
        {
            price += 1000;
        }
        if (GameMasterScript.heroPCActor.myStats.GetLevel() > 15)
        {
            price += 1000;
        }

        GameMasterScript.gmsSingleton.SetTempGameData("eventgold", price);
        GameMasterScript.gmsSingleton.SetTempStringData("eventgold", price.ToString());
    }

    public static void CheckLunarNewYearQuestStatus(string args) 
    {
        int curValue = ProgressTracker.CheckProgress(TDProgress.LUNAR_NEW_YEAR_QUEST, ProgressLocations.HERO);

        if (Debug.isDebugBuild) Debug.Log("Checking moon rabbit quest: " + curValue);

        if (curValue != 1) return;
        
        List<string> required = new List<string>() {"food_sesamebuns", "food_dumpling", "food_spicepeanuts", "item_dianhongtea" };
        List<string> envelopes = new List<string>() {"item_luckyenvelope1", "item_luckyenvelope2", "item_luckyenvelope3"};
        bool hasEnvelopes = false;
        List<Item> inv = GameMasterScript.heroPCActor.myInventory.GetInventory();
        
        bool hasAllItems = false;

        foreach(Item i in inv)
        {
            int count = required.Count;
            if (count != 0 && required.Contains(i.actorRefName)) 
            {
                required.Remove(i.actorRefName);
                //Debug.Log("Found " + i.actorRefName);
            }
            if (!hasEnvelopes && envelopes.Contains(i.actorRefName)) 
            {                
                //Debug.Log("Found env " + i.actorRefName);
                hasEnvelopes = true;
            }

            if (hasEnvelopes && required.Count == 0) 
            {
                hasAllItems = true;
                break;
            }                   
        }

        if (Debug.isDebugBuild) Debug.Log(hasAllItems + " " + hasEnvelopes + " " + required.Count);

        if (!hasAllItems) return;
        
        if (curValue < 2) ProgressTracker.SetProgress(TDProgress.LUNAR_NEW_YEAR_QUEST, ProgressLocations.HERO, 2);

        if (Debug.isDebugBuild) Debug.Log("Yes we're good for LNY quest!");
    }
}
