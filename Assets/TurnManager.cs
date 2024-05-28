using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


// Handles things that happen in the giant GameNextTurn / TryNextTurn function to help make it more manageable

public class TurnManager
{

    /// <summary>
    /// If the player used an ability for the first time, handle setup related to that.
    /// </summary>
    /// <param name="tData"></param>
    /// <param name="heroPCActor"></param>
    public static void CheckForAbilityUseFirstTimeSetup(TurnData tData, HeroPC heroPCActor)
    {
        if (tData.GetTurnType() == TurnTypes.ABILITY)
        {
            // *** First time an ability is executed in the turn logic.

            heroPCActor.SetActorData("buffer_thanesong_level", heroPCActor.GetThaneSongLevel());
            if (tData.tAbilityToTry.abilityFlags[(int)AbilityFlags.THANESONG])
            {
                if (heroPCActor.GetThaneSongLevel() >= 1)
                {
                    GameMasterScript.gmsSingleton.SetTempGameData("playerswitchedsong", 1);
                }
                heroPCActor.myStats.RemoveStatusesByFlag(StatusFlags.THANESONG);
            }

            if (!string.IsNullOrEmpty(tData.tAbilityToTry.script_onPreAbilityUse))
            {
                MethodInfo runscript = CustomAlgorithms.TryGetMethod(typeof(AbilitySpecialFunctions), tData.tAbilityToTry.script_onPreAbilityUse);
                object[] paramList = new object[3];
                paramList[0] = GameMasterScript.heroPCActor;
                paramList[1] = tData.tAbilityToTry;
                paramList[2] = null;
                runscript.Invoke(null, paramList);
            }

            if (tData.tAbilityToTry.refName == "skill_divineretribution") // Convert this to AbilitySpecialFunctions
            {
                int wrathCount = heroPCActor.myStats.CheckStatusQuantity("wrathcharge");
                heroPCActor.myStats.RemoveAllStatusByRef("wrathcharge");
                tData.tAbilityToTry.repetitions = wrathCount;
            }

            if (tData.tAbilityToTry.refName == "skill_furiouscrescendo") // Convert this to AbilitySpecialFunctions
            {
                GameMasterScript.gmsSingleton.SetTempGameData("buffer_thanesong_level", heroPCActor.GetThaneSongLevel());
                heroPCActor.myStats.RemoveStatusesByFlag(StatusFlags.THANESONG);
            }

            if (tData.tAbilityToTry.repetitions > 0)
            {
                List<TargetData> origTDList = new List<TargetData>();
                foreach (TargetData td in GameMasterScript.bufferTargetData)
                {
                    origTDList.Add(td);
                }
                for (int x = 0; x < tData.tAbilityToTry.repetitions; x++)
                {
                    foreach (TargetData td in origTDList)
                    {
                        GameMasterScript.gmsSingleton.AddBufferTargetData(td, true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clears dead actors, removes ghosts, resets turn data per actor, ticks effect cooldown counters (but not statuses)
    /// </summary>
    public static void InitializeActorsAtStartOfTurn()
    {
        // Reset actor data.
        for (int a = 0; a < GameMasterScript.actorListCopy.Count; a++)
        {
            Actor act = GameMasterScript.actorListCopy[a];
            act.ResetTurnData();
            if (act.GetActorType() == ActorTypes.HERO || act.GetActorType() == ActorTypes.MONSTER)
            {
                Fighter ft = act as Fighter;
                if (!ft.myStats.IsAlive())
                {
                    GameMasterScript.AddToDeadQueue(ft);
                    continue;
                }

                ft.damageTakenThisTurn = 0f;

                // New experimental code to prevent ghost actors, implemented 1/19/18
                if (MapMasterScript.InBounds(ft.positionAtStartOfTurn) && ft.positionAtStartOfTurn != ft.GetPos())
                {
#if UNITY_EDITOR
                    if (MapMasterScript.activeMap.RemoveActorFromLocation(ft.positionAtStartOfTurn, ft))
                    {
                        Debug.LogError("Caught a ghost! " + ft.displayName + "(" + ft.actorUniqueID + ") was secretly in " + ft.positionAtStartOfTurn + " but its current position is " + ft.GetPos());
                    }
#else
                        MapMasterScript.activeMap.RemoveActorFromLocation(ft.positionAtStartOfTurn, ft);
#endif
                }
                ft.positionAtStartOfTurn = ft.GetPos();                
                // Pretty simple right?

                ft.TickEffectCooldownCounters();
            }
        }
    }

    /// <summary>
    /// Returns TRUE if the player attempts to hit a damaging destructible for the first time, exiting the turn
    /// </summary>
    /// <param name="tData"></param>
    /// <returns></returns>
    public static bool CheckForPromptHitDangerousDestructible(TurnData tData)
    {
        // New check to warn the player about breaking damaging destructibles.
        Destructible destructibleTarget = null;
        if (tData.GetTurnType() == TurnTypes.ATTACK)
        {
            destructibleTarget = tData.GetTargetIfDestructibleAndOnlyDestructible() as Destructible;
        }
        else if (tData.GetTurnType() == TurnTypes.MOVE)
        {
            destructibleTarget = 
                MapMasterScript.GetTile(tData.newPosition).GetBreakableCollidableIfNoMonsterTargets(GameMasterScript.heroPCActor);
        }

        // We're moving or attacking a destructible that does something bad when destroyed
        // We haven't *already* confirmed we want to attack, and we've never been informed of this object's effects before.
        if (destructibleTarget != null && destructibleTarget.HasHarmfulDeathStatusEffectAtRange(MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), destructibleTarget.GetPos())) &&
            GameMasterScript.gmsSingleton.ReadTempGameData("dt_confirm_destroy") != 1) 
            //&& MetaProgressScript.ReadMetaProgress("dt_attack_" + destructibleTarget.actorRefName) != 1)
        {
            // So pop up a confirm dialogue. If the player says "YES", continue the turn. If not, nothing happens.
            // But don't prompt the player about this object again.
            StringManager.SetTag(0, destructibleTarget.displayName);
            UIManagerScript.StartConversationByRef("confirm_hit_damaging_destructible", DialogType.KEYSTORY, null);
            GameMasterScript.bufferedTurnData = tData;
            MetaProgressScript.SetMetaProgress("dt_attack_" + destructibleTarget.actorRefName, 1);
            return true;
        }

        return false;
    }

    public static bool CheckForToggleAndFreeAbility(TurnData tData, List<EffectScript> localTurnEffectsFromPlayer)
    {
        foreach (EffectScript eff in tData.tAbilityToTry.listEffectScripts)
        {
            //Debug.Log("Adding " + eff.effectRefName + " From abil " + tData.tAbilityToTry.myID);
            localTurnEffectsFromPlayer.Add(eff);
        }
        bool toggle = false;
        AbilityScript freeAbility = null;

        if (tData.tAbilityToTry.CheckAbilityTag(AbilityTags.CANTOGGLE) && (tData.tAbilityToTry.chargeTime == 100 || tData.tAbilityToTry.chargeTime == 200))
        {
            toggle = true;
            bool skipTurnFromToggle = GameMasterScript.gmsSingleton.ProcessAbilityToggle(tData, out freeAbility);
            if (skipTurnFromToggle)
            {
                GameMasterScript.gmsSingleton.turnExecuting = false;
                GameMasterScript.SetAnimationPlaying(false);
                GameMasterScript.gmsSingleton.SetItemBeingUsed(null);
                UIManagerScript.RefreshStatuses(true);
                return true;
            }
        }

        if (tData.tAbilityToTry.refName == "skill_regenflask" ||
            tData.tAbilityToTry.refName == "skill_managespellshapes")
        {
            freeAbility = tData.tAbilityToTry;
            toggle = true;
        }

        // **************************
        // FREE ABILITY CODE - REGEN FLASK ETC
        // **************************

        if (freeAbility != null)
        {
            GameMasterScript.gmsSingleton.ProcessFreeAbility(tData, freeAbility);

            if (toggle)
            {
                GameMasterScript.gmsSingleton.turnExecuting = false;
                GameMasterScript.SetAnimationPlaying(false);
                GameMasterScript.gmsSingleton.SetItemBeingUsed(null);
                UIManagerScript.RefreshStatuses(true);
                GuideMode.CheckIfFoodAndFlaskShouldBeConsumedAndToggleIndicator();
                return true;
            }

            // Execute end of turn code...        
            UIManagerScript.RefreshStatuses(true);
        }


        return false;
    }

    /// <summary>
    /// Returns true if our movement would start a conversation with an NPC.
    /// </summary>
    /// <param name="tData"></param>
    /// <returns></returns>
    public static bool CheckForNPCConversations(TurnData tData)
    {
        if (tData.GetTurnType() != TurnTypes.MOVE)
        {
            return false;
        }

        MapTileData checkTile = MapMasterScript.activeMap.GetTile(tData.newPosition);

        if (TileInteractions.CheckForAndOpenConversationInInteractedTile(checkTile))
        {
            return true;
        }

        return false;
    }

    public static bool CheckForLimitBreak(TurnData tData)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2)) return false;

        if (!GameMasterScript.heroPCActor.LimitBreakAvailable())
        {
            return false;
        }

        if (tData.GetTurnType() == TurnTypes.ATTACK)
        {
            if (GameMasterScript.heroPCActor.myStats.CheckIfSealed())
            {
                return false;
            }
            return true;
        }
        else if (tData.GetTurnType() == TurnTypes.MOVE)
        {
            bool checkCollide = MapMasterScript.CheckCollision(tData.newPosition, GameMasterScript.heroPCActor);
            MapTileData checkTile = MapMasterScript.GetTile(tData.newPosition);
            if (!checkCollide) return false;
            foreach(Actor act in checkTile.GetAllActors())
            {
                if (act.GetActorType() != ActorTypes.MONSTER) continue;
                if (act.GetActorType() == ActorTypes.MONSTER && act.actorfaction != Faction.PLAYER)
                {
                    Monster mn = act as Monster;
                    if (mn.myStats.IsAlive())
                    {
                        if (GameMasterScript.heroPCActor.myStats.CheckIfSealed())
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
        }

        

        return false;
    }

    /// <summary>
    /// Opens up the targeting UI for limit break.
    /// </summary>
    /// <param name="tData"></param>
    public static void TriggerLimitBreak(TurnData tData)
    {
        string passiveSoulRefName = GameMasterScript.heroPCActor.myAbilities.GetFirstEquippedPassiveAbilityOfTag(AbilityTags.DRAGONSOUL).refName;
        string abilityBreakName = "";
        
        switch (passiveSoulRefName)
        {
            case "skill_frogdragonsoul":
                abilityBreakName = "dragonbreak_frog_power";
                break;
            case "skill_banditdragonsoul":
                abilityBreakName = "dragonbreak_bandit_power";
                break;
            case "skill_beastdragonsoul":
                abilityBreakName = "dragonbreak_beast_power";
                break;
            case "skill_slimedragonsoul":
                abilityBreakName = "dragonbreak_slime_power";
                break;
            case "skill_spiritdragonsoul":
                abilityBreakName = "dragonbreak_spirit_power";
                break;
            case "skill_robotdragonsoul":
                abilityBreakName = "dragonbreak_robot_power";
                break;
        }

        TDInputHandler.DelayMouseInput(0.25f);
        GameMasterScript.gmsSingleton.CheckAndTryAbility(GameMasterScript.masterAbilityList[abilityBreakName]);
    }
}
