using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine.Events;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
using UnityEngine.Analytics;
	using Steamworks;	
	using LapinerTools.Steam.Data;
	using LapinerTools.uMyGUI;
	using System.Security.Cryptography;
#endif

using UnityEngine.UI;
using System.Text;
using TMPro;
using System.Threading;
using Rewired.ComponentControls.Data;
using System.Reflection;
using System.Runtime;

#if UNITY_SWITCH
    using Rewired.Platforms.Switch;
#endif

public partial class GameMasterScript
{
    public static Weapon SwitchToFirstWeaponOfCondition(Func<Weapon, bool> f)
    {
        //max weapons value?
        for (int t = 0; t < UIManagerScript.hotbarWeapons.Length; t++)
        {
            Weapon w = UIManagerScript.hotbarWeapons[t];
            if (w != null && f(w))
            {
                if (!GameMasterScript.heroPCActor.myEquipment.IsCurrentWeaponRanged())
                {
                    heroPCActor.lastUsedMeleeWeapon = heroPCActor.myEquipment.GetWeapon();
                }

                heroPCActor.lastUsedWeapon = heroPCActor.myEquipment.GetWeapon();
                if (heroPCActor.myEquipment.GetOffhand() != null)
                {
                    heroPCActor.SetActorData("offhand_id_preswap", heroPCActor.myEquipment.GetOffhand().actorUniqueID);
                }
                else
                {
                    heroPCActor.RemoveActorData("offhand_id_preswap");
                }


                UIManagerScript.SwitchActiveWeaponSlot(t, false, false);

                GameMasterScript.gmsSingleton.SetTempGameData("hero_autoswitched_weapon", 1);

                if (UIManagerScript.hotbarWeapons[t] != null && UIManagerScript.hotbarWeapons[t].isRanged)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("hero_switchedto_rangedweap", 1);
                }

                return w;
            }
        }

        return null;
    }

    public static void SellAllItems(Rarity maxRarity)
    {
        int totalMoney = 0;
        List<Item> itemsToSell = new List<Item>();
        foreach (Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (itm.favorite) continue;
            if (itm.itemType == ItemTypes.EMBLEM || itm.ReadActorData("permabound") == 1) continue;

            if (itm.IsEquipment())
            {
                if (itm.legendary && !itm.vendorTrash) continue;

                Equipment eq = itm as Equipment;
                bool pairedWithAnything = false;
                for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
                {
                    if (UIManagerScript.hotbarWeapons[i] == null) continue;
                    if (eq.CheckIfPairedWithSpecificItem(UIManagerScript.hotbarWeapons[i]))
                    {
                        pairedWithAnything = true;
                        break;
                    }
                }

                if (pairedWithAnything) continue;

                if ((int)itm.rarity <= (int)maxRarity || itm.vendorTrash)
                {
                    if (itm.itemType == ItemTypes.WEAPON)
                    {
                        Weapon wp = itm as Weapon;
                        if (UIManagerScript.IsWeaponInHotbar(wp))
                        {
                            continue;
                        }
                    }
                    if (itm == GameMasterScript.heroPCActor.lastOffhandEquipped)
                    {
                        continue;
                    }
                    itemsToSell.Add(itm);
                }
            }
            if (itm.actorRefName.Contains("gem") || itm.actorRefName.Contains("cashcrop") || itm.vendorTrash)
            {
                itemsToSell.Add(itm);
            }
        }

        foreach (Item itm in itemsToSell)
        {
            UIManagerScript.currentConversation.whichNPC.myInventory.AddItemRemoveFromPrevCollection(itm, false);
            //int localSalePrice = itm.CalculateSalePrice();

            int localSalePrice = (int)itm.GetIndividualSalePrice();

            if (itm.itemType == ItemTypes.CONSUMABLE)
            {
                Consumable con = itm as Consumable;
                totalMoney += (localSalePrice * con.Quantity);
            }
            else
            {
                totalMoney += localSalePrice;
            }

            GameMasterScript.heroPCActor.OnItemSoldOrDropped(itm, soldItem: true);
        }

        if (UIManagerScript.currentConversation.whichNPC.GetShop() != null && UIManagerScript.currentConversation.whichNPC.GetShop().GetShop() != null)
        {
            //itm.CalculateShopPrice(UIManagerScript.currentConversation.whichNPC.GetShop().GetShop().valueMult, true);
            totalMoney = (int)(totalMoney * UIManagerScript.currentConversation.whichNPC.GetShop().GetShop().saleMult);
        }

        heroPCActor.ChangeMoney(totalMoney, doNotAlterFromGameMods: true);
        UIManagerScript.RefreshPlayerStats();
        StringManager.SetTag(0, itemsToSell.Count.ToString());
        StringManager.SetTag(1, totalMoney.ToString());
        GameLogScript.LogWriteStringRef("log_sell_items_bulk");
    }

    public static void LootAnItem(Item lootedItem, Fighter looter, bool itemIsOnMap)
    {
        bool stackItems = false;
        if (looter == heroPCActor)
        {
            stackItems = true;
            if (lootedItem.actorRefName == "spice_rosepetals" && ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO) <= 2)
            {
                ProgressTracker.SetProgress(TDProgress.ROMANCEQUEST, ProgressLocations.HERO, 3);
            }
        }
        stackItems = true; // Why would we NOT want to stack items...?

        bool playedOwnedItem =
            (lootedItem.ReadActorData("pwn") == 1 || lootedItem.ReadActorData("playerowned") == 1) &&
            looter.GetActorType() == ActorTypes.HERO;

        looter.myInventory.AddItemRemoveFromPrevCollection(lootedItem, stackItems);
        GameObject go = lootedItem.GetObject();
        if (go != null)
        {
            AudioStuff aSource = go.GetComponent<AudioStuff>();
            // Only play the clip if the actor is within vision range.
            if ((aSource != null) && (MapMasterScript.GetGridDistance(looter.GetPos(), heroPCActor.GetPos()) <= heroPCActor.myStats.GetStat(StatTypes.VISIONRANGE, StatDataTypes.CUR)))
            {
                // Generalize this.
                aSource.PlayCue("Pickup");
            }
        }

        bool leaveItemOnMap = false;

        int forceQuantity = 0;

        if (looter != heroPCActor)
        {
            StringManager.SetTag(0, looter.displayName);
            StringManager.SetTag(1, lootedItem.displayName);
            GameLogScript.GameLogWrite(StringManager.GetString("log_monster_pickup_item"), looter);
            if (looter.GetActorType() == ActorTypes.MONSTER)
            {
                Monster mn = looter as Monster;
                QuestScript qToComplete = null;
                QuestScript[] qToCheck = new QuestScript[heroPCActor.myQuests.Count];
                heroPCActor.myQuests.CopyTo(qToCheck);
                for (int i = 0; i < qToCheck.Length; i++)
                {
                    QuestScript qs = qToCheck[i];
                    if (qs == null || qs.complete) continue;
                    if (qs.qType == QuestType.APPEASEMONSTER)
                    {
                        if (qs.targetMonster == mn)
                        {
                            int qty = mn.myInventory.GetItemQuantity(qs.targetItem.actorRefName);
                            if (qty >= 1)
                            {
                                // Complete the quest!
                                QuestScript.CompleteQuest(qs);
                                qToComplete = qs;
                                break;
                            }
                            else
                            {
                                int roll = UnityEngine.Random.Range(0, 3);
                                StringManager.SetTag(0, mn.displayName);
                                StringManager.SetTag(1, qs.targetItem.GetPluralName());
                                GameLogScript.LogWriteStringRef("log_monster_wantsitem" + (roll + 1));
                            }
                        }
                    }
                }

                if (qToComplete != null)
                {
                    looter.myStats.SetStat(StatTypes.HEALTH, 0, StatDataTypes.CUR, true);
                    looter.myInventory.RemoveItemByRef(lootedItem.actorRefName);
                    looter.myInventory.ClearInventory();
                    AddToDeadQueue(looter);
                    if (lootedItem.itemType == ItemTypes.CONSUMABLE)
                    {
                        Consumable cn = lootedItem as Consumable;
                        forceQuantity = cn.Quantity - 1;
                        cn.ChangeQuantity(-1);
                        if (cn.Quantity <= 0)
                        {
                            leaveItemOnMap = false;
                        }
                        else
                        {
                            leaveItemOnMap = true;
                        }
                    }

                    StringManager.SetTag(0, looter.displayName);
                    StringManager.SetTag(1, qToComplete.targetItem.displayName);
                    GameLogScript.LogWriteStringRef("log_monster_gotwanteditem");
                }

                else
                {
                    if (mn.foodLovingMonster && lootedItem.actorRefName == mn.GetWantedItem()) // Food loving, but not a quest monster.
                    {
                        // Spawn some coins 
                        MapTileData coinTile = MapMasterScript.activeMap.GetRandomEmptyTile(mn.GetPos(), 1, true, anyNonCollidable: false, preferLOS: true);
                        MapMasterScript.SpawnCoins(MapMasterScript.GetTile(mn.GetPos()), coinTile, UnityEngine.Random.Range(5, 15));
                    }
                }
            }
        }
        else
        {
            // Spawn item pickup box above player and fade it away.
            //if (PlayerPrefs.GetInt("Pickup Display") == 1)
            if (PlayerOptions.pickupDisplay)
            {
                GameObject pickupBox = TDInstantiate("ItemPickupBox");
                pickupBox.transform.SetParent(canvasObject.transform);
                Sprite spr = lootedItem.GetSpriteForUI();
                Vector3 space = Camera.main.WorldToScreenPoint(heroPCActor.GetPos());
                pickupBox.GetComponent<FadingUIScript>().StartBox(space, spr, lootedItem.displayName);
            }

            // I can't figure out why we need to do this but I keep getting errors related to modifying the collection
            QuestScript[] copyOfQuests = new QuestScript[(int)heroPCActor.myQuests.Count];
            heroPCActor.myQuests.CopyTo(copyOfQuests);

            for (int i = 0; i < copyOfQuests.Length; i++)
            {
                QuestScript qs = copyOfQuests[i];
                if (qs == null) continue;
                if (qs.complete) continue;
                if (qs.qType == QuestType.FINDITEM)
                {
                    if (qs.targetItem == lootedItem)
                    {
                        QuestScript.CompleteQuest(qs);
                    }
                }
            }

            if (lootedItem.legendary && !playedOwnedItem)
            {
                LootGeneratorScript.OnLegendaryItemFound(lootedItem);
            }

            // Clear item world and return to town.
            // Exit item world
            if (MapMasterScript.itemWorldItem == lootedItem)
            {
                endingItemWorld = true;

                Dictionary<string, object> dreamVictoryInfo = null;
				bool fillOutDreamVictoryInfo = false;
                if (PlatformVariables.SEND_UNITY_ANALYTICS)
                {
					dreamVictoryInfo = new Dictionary<string, object>();
					fillOutDreamVictoryInfo = true;
                }

                if (Debug.isDebugBuild) Debug.Log("Looted the item world item! " + lootedItem.displayName);

                if (heroPCActor.ReadActorData("killed_nightmareprince") == 1)
                {
                    Conversation endItemWorld = FindConversation("itemworld_exit_nightmareprince");
                    UIManagerScript.StartConversation(endItemWorld, DialogType.STANDARD, null);
                    if (fillOutDreamVictoryInfo) dreamVictoryInfo.Add("boss", "prince");
                }
                else if (ItemDreamFunctions.IsItemDreamNightmare())
                {
                    Conversation endItemWorld = FindConversation("itemworld_exit_nightmareking");
                    gmsSingleton.statsAndAchievements.ItemNightmareCleared();
                    UIManagerScript.StartConversation(endItemWorld, DialogType.STANDARD, null);
                    if (fillOutDreamVictoryInfo) dreamVictoryInfo.Add("boss", "queen");
                }
                else
                {
                    Conversation endItemWorld = FindConversation("itemworld_exit");
                    UIManagerScript.StartConversation(endItemWorld, DialogType.STANDARD, null);
                    if (fillOutDreamVictoryInfo) dreamVictoryInfo.Add("boss", "normal");
                }

                int numHotbarSlotsUsed = GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed();

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                if (PlatformVariables.SEND_UNITY_ANALYTICS)
{
                dreamVictoryInfo.Add("item", lootedItem.actorRefName);
                dreamVictoryInfo.Add("hbslotsused", numHotbarSlotsUsed);
                Equipment eq = lootedItem as Equipment;
                dreamVictoryInfo.Add("irarity", lootedItem.rarity.ToString());
                dreamVictoryInfo.Add("upgrades", lootedItem.timesUpgraded);
                dreamVictoryInfo.Add("cv", lootedItem.challengeValue);
                dreamVictoryInfo.Add("mods", eq.GetNonAutomodCount());
                dreamVictoryInfo.Add("magicchance", MapMasterScript.itemWorldMagicChance);
                dreamVictoryInfo.Add("plvl", GameMasterScript.heroPCActor.myStats.GetLevel());
                dreamVictoryInfo.Add("ngplus", GameStartData.saveSlotNGP[GameStartData.saveGameSlot]);
                dreamVictoryInfo.Add("job", GameMasterScript.heroPCActor.myJob.jobEnum);
                endingItemWorld = true;

                Analytics.CustomEvent("itemdreamwin", dreamVictoryInfo);
}
#endif
            }

        }
        if (!leaveItemOnMap)
        {
            if (itemIsOnMap)
            {
                mms.RemoveActorFromMap(lootedItem);
            }
            if (go != null)
            {
                TryReturnChildrenToStack(go);
                ReturnActorObjectToStack(lootedItem, go, "GenericItemPrefab");
            }
        }
        else
        {
            if (forceQuantity > 0)
            {
                if (lootedItem.itemType == ItemTypes.CONSUMABLE)
                {
                    Consumable con = lootedItem as Consumable;
                    con.Quantity = forceQuantity;
                }
            }
        }
    }
}