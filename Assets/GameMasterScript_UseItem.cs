using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class GameMasterScript : MonoBehaviour
{
    public void PlayerUseConsumable(Consumable consume)
    {
        if (playerDied)
        {
            return;
        }
        if (consume == null)
        {
            return;
        }
        if (turnExecuting)
        {
            return;
        }

        if (!JobTrialScript.CanPlayerUseConsumable())
        {
            StringManager.SetTag(0, heroPCActor.myJob.DisplayName);
            GameLogScript.LogWriteStringRef("log_error_jobtrial_consumable");
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if ((SharaModeStuff.IsSharaModeActive() || RandomJobMode.IsCurrentGameInRandomJobMode())
            && consume.actorRefName == "scroll_jobchange")
        {
            UIManagerScript.DisplayPlayerError(heroPCActor);
            return;
        }

        if (consume.actorRefName == "item_tent")
        {
            if (!heroPCActor.GetActorMap().IsTownMap())
            {
                StringManager.SetTag(0, consume.displayName);
                GameLogScript.LogWriteStringRef("log_error_tent_notintown");
                return;
            }
            heroPCActor.TryChangeQuantity(consume, -1);
            if (consume.Quantity <= 0)
            {
                heroPCActor.myInventory.RemoveItem(consume);
                UIManagerScript.RemoveItemFromHotbar(consume);
            }
            UseTent();
            return;
        }
        if (consume.actorRefName == "item_wallbreaker" && MapMasterScript.activeMap.dungeonLevelData.unbreakableWalls)
        {
            GameLogScript.LogWriteStringRef("log_error_wallbreaker_town");
            return;
        }

        if (!consume.CheckIfItemCanBeUsed())
        {
            return;
        }

        if (consume.actorRefName == "item_monstermallet")
        {
            int localCount = MetaProgressScript.localTamedMonstersForThisSlot.Count;

            if (GameMasterScript.heroPCActor.HasMonsterPet())
            {
                localCount++;
            }

            if (localCount >= MonsterCorralScript.MAX_MONSTERS_IN_CORRAL)
            {
                GameLogScript.LogWriteStringRef("log_corral_error_full");
                return;
            }
        }
        else if (consume.actorRefName == "scroll_jobchange")
        {
            if (JobTrialScript.IsJobTrialActive())
            {
                UIManagerScript.DisplayPlayerError(heroPCActor);
                StringManager.SetTag(0, heroPCActor.myJob.DisplayName);
                GameLogScript.LogWriteStringRef("log_error_jobtrial_changejob");
                return;
            }

            if (GameStartData.CheckGameModifier(GameModifiers.NO_JOBCHANGE) || RandomJobMode.IsCurrentGameInRandomJobMode())
            {
                UIManagerScript.DisplayPlayerError(heroPCActor);
                GameLogScript.LogWriteStringRef("log_error_nojobchange");
                return;
            }

            // Special case.
            jobChangeFromNPC = false;
            SetItemToUse(consume);
            GameMasterScript.jobChangeFromNPC = false;
            //UIManagerScript.ForceCloseFullScreenUIWithNoFade(true);
            CharCreation.singleton.BeginCharCreation_JobSelection();
            StartCoroutine(UIManagerScript.singletonUIMS.WaitThenAlignCursor(0.1f, CharCreation.jobButtons[0]));
            return;
        }
        else if (!String.IsNullOrEmpty(consume.seasoningAttached))
        {
            if (consume.seasoningAttached == "spice_rosepetals")
            {
                UIManagerScript.StartConversationByRef("try_eat_romanticmeal", DialogType.STANDARD, null);
                return;
            }
        }
        else if (consume.actorRefName == "item_dungeonmap")
        {
            BattleTextManager.NewText(StringManager.GetString("knowledge_bt"), heroPC, Color.yellow, 0.0f);
            MapMasterScript.activeMap.ExploreAllTiles();
            GameLogScript.LogWriteStringRef("log_item_dungeonmap");

            if (!heroPCActor.TryChangeQuantity(consume, -1))
            {
                //Debug.Log(consume.quantity);
                heroPCActor.myInventory.RemoveItem(consume);
                UIManagerScript.RemoveItemFromHotbar(consume);
            }
            return;

        }

        ActuallyUseConsumable(consume);
    }

    public void ActuallyUseConsumable(Consumable consume)
    {
        if (consume.parentForEffectChildren != null)
        {
            bool addsChargeTimeOrCTStatus = false;

            if (consume.parentForEffectChildren.chargeTime > 0)
            {
                addsChargeTimeOrCTStatus = true;
            }

            foreach (EffectScript es in consume.parentForEffectChildren.listEffectScripts)
            {
                if (consume.isFood || es.effectTags[(int)EffectTags.FOODHEAL])
                {
                    if (heroPCActor.myStats.CheckHasStatusName("status_foodfull"))
                    {
                        GameLogScript.LogWriteStringRef("log_error_foodfull");
                        return;
                    }
                }

                if (!addsChargeTimeOrCTStatus && GameStartData.NewGamePlus >= 2 && !MysteryDungeonManager.InOrCreatingMysteryDungeon()) // in NG++, items that don't modify CT should reduce our CT to 0
                {
                    if (es.effectType == EffectType.CHANGESTAT) // does this directly modify CT
                    {
                        ChangeStatEffect cse = es as ChangeStatEffect;
                        if (cse.stat == StatTypes.CHARGETIME) addsChargeTimeOrCTStatus = true;
                    }
                    else if (es.effectType == EffectType.ADDSTATUS) // or add a status that gives us CT
                    {
                        AddStatusEffect ase = es as AddStatusEffect;
                        if (string.IsNullOrEmpty(ase.statusRef)) continue;
                        StatusEffect se;
                        if (masterStatusList.TryGetValue(ase.statusRef, out se))
                        {
                            if (!se.isPositive) continue;
                            foreach (EffectScript subEff in se.listEffectScripts)
                            {
                                if (subEff.effectType != EffectType.CHANGESTAT) continue;
                                ChangeStatEffect cse = subEff as ChangeStatEffect;
                                if (cse.stat == StatTypes.CHARGETIME) addsChargeTimeOrCTStatus = true;
                            }
                        }
                    }
                }
            }

            // in ng++, if we use an item that doesn't directly give us CT, it reduces our CT to 0
            if (GameStartData.NewGamePlus >= 2 && !addsChargeTimeOrCTStatus && GameMasterScript.heroPCActor.actionTimer > 0)
            {
                GameMasterScript.heroPCActor.actionTimer = 0;
            }

            SetItemBeingUsed(consume);
            SetTempGameData("id_itembeingused", consume.actorUniqueID);
            abilityToTry = consume.parentForEffectChildren;
            unmodifiedAbility = null;
            int bonusSummonTurns = itemBeingUsed.ReadActorData("bonusturns");
            if (bonusSummonTurns > 0)
            {
                SetTempGameData("bonusturns", bonusSummonTurns);
            }

            //Debug.Log("Player triggering ability for item " + consume.actorRefName + " " + consume.actorUniqueID + " which currently has qty " + consume.GetQuantity() + " " + consume.collection.owner.actorRefName);
            TryAbility(consume.parentForEffectChildren);
        }
    }

    void SetItemToUse(Consumable con)
    {
        itemToUse = con;
    }

    public static Consumable GetItemToUse()
    {
        Consumable con = itemToUse;
        itemToUse = null;
        return con;
    }


    public bool TryUseItemViaShortcut(string itemRef)
    {
        Item checkForItem = heroPCActor.myInventory.GetItemByRef(itemRef);

        if (checkForItem != null && checkForItem.itemType == ItemTypes.CONSUMABLE)
        {
            PlayerUseConsumable(checkForItem as Consumable);
            return true;
        }
        else
        {
            StringManager.SetTag(0, masterItemList[itemRef].displayName);
            GameLogScript.LogWriteStringRef("log_error_no_item");
            return false;
        }
    }

}