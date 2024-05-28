using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public IEnumerator WaitThenTickGameTime(int days, bool trySpawnWanderingMerchant, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        TickGameTime(days, trySpawnWanderingMerchant);
    }

    public void TickGameTime(int days, bool trySpawnWanderingMerchant, bool showWanderingMessage = true)
    {
        MetaProgressScript.totalDaysPassed += days;
        
        if (!SharaModeStuff.IsSharaModeActive())
        {
            MetaProgressScript.AgeAllTrees(days);
            MetaProgressScript.DevelopTamedMonsterRelationships(days);
            MetaProgressScript.CheckForAndGenerateLetterFromReleasedMonster(days);
        }

        GameMasterScript.heroPCActor.daysPassed += days;
        int currentDay = (MetaProgressScript.totalDaysPassed + 1);
        StringManager.SetTag(0, currentDay.ToString());

        MetaProgressScript.OnTickGameTime();

        if (!playerDied)
        {
            GameLogScript.LogWriteStringRef("log_timepassing");
            NotificationUIScript.NewDay(currentDay);
        }

        if (MapMasterScript.activeMap.IsBossFloor())
        {
            trySpawnWanderingMerchant = false;
        }

        if (trySpawnWanderingMerchant)
        {
            TrySpawnWanderingMerchant(showWanderingMessage);
        }

        // Modify shop restock chance somehow here.
        foreach (Map m in MapMasterScript.dictAllMaps.Values)
        {
            float chanceToRestock = CHANCE_TOWN_RESTOCKGOODS;
            if (m.dungeonLevelData.fastTravelPossible)
            {
                chanceToRestock = CHANCE_SIDEAREA_RESTOCKGOODS;
            }
            else if (m.IsTownMap())
            {
                chanceToRestock = CHANCE_TOWN_RESTOCKGOODS;
            }
            else
            {
                continue;
            }

            foreach (Actor act in m.actorsInMap)
            {
                if (act.GetActorType() == ActorTypes.NPC)
                {
                    NPC n = act as NPC;
                    if (n.doNotRestockShop && n.ReadActorData("stockedonce") == 1) continue;
                    if (n.actorRefName == "npc_foodcart") continue;

                    if (n.shopRef != "" && !heroPCActor.shopkeepersThatRefresh.Contains(n.actorUniqueID))
                    {
                        heroPCActor.shopkeepersThatRefresh.Add(n.actorUniqueID);
                    }

                    int dayThresh = (int)((1f / chanceToRestock) + 1);

                    if (UnityEngine.Random.Range(0, 1f) <= chanceToRestock || n.GetDaysSinceLastRestock() >= (1f / chanceToRestock))
                    {
                        n.TryRestockGoods();

                        if (m == MapMasterScript.activeMap)
                        {
                            n.CheckForNewStuffAndSpawn();
                        }
                    }
                    else if (n.givesQuests)
                    {
                        if (m == MapMasterScript.activeMap)
                        {
                            n.CheckForNewStuffAndSpawn();
                        }
                    }
                }
            }
        }

    }

}