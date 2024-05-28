using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public static object Debug_TrySpawnWanderingMerchant(params string[] args)
    {
        gmsSingleton.TrySpawnWanderingMerchant(true);
        return "I tried.";
    }

    public bool TrySpawnWanderingMerchant(bool showWanderingMessage, bool bForceSpawnChanceToSucceed = false)
    {
        if (wanderingMerchantInTown != null)
        {
            durationOfWanderingMerchant--;
            //Tick down the timer, but force the old merchant to bounce if we are forcing the whole event
            if (durationOfWanderingMerchant == 0 || bForceSpawnChanceToSucceed)
            {
                MapMasterScript.singletonMMS.townMap.RemoveActorFromMap(wanderingMerchantInTown);
                if (MapMasterScript.activeMap == MapMasterScript.singletonMMS.townMap)
                {
                    if (wanderingMerchantInTown.objectSet)
                    {
                        mms.activeNonTileGameObjects.Remove(wanderingMerchantInTown.GetObject());
                        wanderingMerchantInTown.GetObject().SetActive(false);
                        ReturnActorObjectToStack(wanderingMerchantInTown, wanderingMerchantInTown.GetObject(), wanderingMerchantInTown.prefab);
                    }

                }
                wanderingMerchantInTown = null;
            }
        }
        float roll = UnityEngine.Random.Range(0, 1f);
        if (!bForceSpawnChanceToSucceed &&
            (wanderingMerchantInTown != null || roll > wanderingMerchantChance))
        {
            return false;
        }
        if (!UIManagerScript.dialogBoxOpen && !GameMasterScript.playerDied && !SharaModeStuff.IsSharaModeActive())
        {
            if (showWanderingMessage && !animationFromCutscene && !animationPlaying)
            {
                Conversation tut = FindConversation("town_newmerchant");
                UIManagerScript.StartConversation(tut, DialogType.STANDARD, null);
            }
            else
            {
                GameLogScript.LogWriteStringRef("dialog_town_newmerchant_intro_txt");
            }
        }

        // Hardcoded wandering merchants
        List<string> wanderRefs = new List<string>();
        wanderRefs.Add("npc_rangedguy");
        wanderRefs.Add("npc_armorguy");
        wanderRefs.Add("npc_jewelryguy");
        wanderRefs.Add("npc_slayweaponguy");
        wanderRefs.Add("npc_petshop");
        wanderRefs.Add("npc_magicbacker");
        wanderRefs.Add("npc_bombguy");
        wanderRefs.Add("npc_rubymoon_merchant");

        if (heroPCActor.ReadActorData("painterquest") < 0)
        {
            if (QuestScript.GetUnexploredCombatSideAreaCount() > 1)
            {
                wanderRefs.Add("npc_painter");
                wanderRefs.Add("npc_painter");
            }
        }

        NPC makeNPC = NPC.CreateNPC(wanderRefs[UnityEngine.Random.Range(0, wanderRefs.Count)]);
        wanderingMerchantInTown = makeNPC;
        makeNPC.dungeonFloor = 100;
        makeNPC.SetActorMap(MapMasterScript.singletonMMS.townMap);
        Vector2 wanderingMerchantPos = Vector2.zero;
        //wanderingMerchantPos.x = UnityEngine.Random.Range(7, 9);
        //wanderingMerchantPos.y = UnityEngine.Random.Range(7, 11);
        wanderingMerchantPos.x = 6f;
        wanderingMerchantPos.y = 11f;
        wanderingMerchantInTown.RefreshShopInventory(GameMasterScript.heroPCActor.dungeonFloor);
        makeNPC.SetPos(wanderingMerchantPos);
        makeNPC.SetCurPos(wanderingMerchantPos);
        MapMasterScript.singletonMMS.townMap.AddActorToMap(makeNPC);
        MapMasterScript.singletonMMS.townMap.AddActorToLocation(wanderingMerchantPos, makeNPC);
        if (MapMasterScript.activeMap == MapMasterScript.singletonMMS.townMap)
        {
            mms.SpawnNPC(makeNPC);
        }
        durationOfWanderingMerchant = 2; // Two new floors
        return true;
    }
}