using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Look at the player's inventory, skills, etc. on death and give them some advice based on their death state

public class PlayerAdvice : MonoBehaviour {

    public static string GetAdviceStringForPlayer()
    {
        string buildAdviceString = "";

        List<string> recoveryStrings = new List<string>();
        List<string> generalStrings = new List<string>();
        List<string> escapeStrings = new List<string>();
        List<string> equipmentStrings = new List<string>();

        // EQUIPMENT ADVICE
        if (!PlayerHasRangedWeaponOnHotbar())
        {
            generalStrings.Add(StringManager.GetString("deathtip_rangedweapon"));
        }

        if (PlayerDiedToPhysical() && (!PlayerHasShieldEquipped()))
        {
            generalStrings.Add(StringManager.GetString("deathtip_physical_shield"));
        }

        float healthLostLastRd = PlayerHealthLostLast3Turns();
        float percentLost = healthLostLastRd / GameMasterScript.heroPCActor.myStats.GetMaxStat(StatTypes.HEALTH);
        if (percentLost >= 0.2f)
        {
            int displayPercent = (int)(percentLost * 100f);
            int displayFlat = (int)healthLostLastRd;
            StringManager.SetTag(0, displayFlat.ToString());
            StringManager.SetTag(1, displayPercent.ToString());
            generalStrings.Add(StringManager.GetString("deathtip_damage_spike"));
        }

        /* if (equipmentStrings.Count > 0)
        {
            //buildAdviceString += StringManager.GetString("deathtip_equipment_header") + "\n\n";
            foreach(string tip in equipmentStrings)
            {
                buildAdviceString += "* " + tip + "\n\n";
            }
        } */

        // RECOVERY ADVICE
        if (PlayerHasUsableRecoveryItem())
        {
            if (PlayerIsFull())
            {
                generalStrings.Add(StringManager.GetString("deathtip_recovery_nofood"));
            }
            else
            {
                generalStrings.Add(StringManager.GetString("deathtip_recovery_withfood"));
            }            
        }

        if (!PlayerHealingFromFlask() && GameMasterScript.heroPCActor.regenFlaskUses > 0 && !SharaModeStuff.IsSharaModeActive())
        {
            generalStrings.Add(StringManager.GetString("deathtip_recovery_flask"));
        }

        /* if (recoveryStrings.Count > 0)
        {
            //buildAdviceString += StringManager.GetString("deathtip_recovery_header") + "\n\n";
            foreach (string tip in recoveryStrings)
            {
                buildAdviceString += "* " + tip + "\n\n";
            }
        } */

        // ESCAPE ADVICE
        if (PlayerMovementSkillAvailable())
        {
            generalStrings.Add(StringManager.GetString("deathtip_movement_skills"));
        }
        if (!PlayerEscapingDungeon() && !SharaModeStuff.IsSharaModeActive())
        {
            generalStrings.Add(StringManager.GetString("deathtip_movement_escape"));
        }

        /* if (escapeStrings.Count > 0)
        {
            //buildAdviceString += StringManager.GetString("deathtip_escape_header") + "\n\n";
            foreach (string tip in escapeStrings)
            {
                buildAdviceString += "* " + tip + "\n\n";
            }
        } */

        // GENERAL ADVICE
        if (RecentTurnsTooFast() )
        {
            generalStrings.Add(StringManager.GetString("deathtip_turns_toofast"));
        }


        if (DreamcasterAvailableButUnused() && !GameStartData.gameInSharaMode)
        {
            generalStrings.Add(StringManager.GetString("deathtip_general_dreamcaster"));
        }

        float levelDiff = GetPlayerStrengthComparedToFloorChallenge();

        if (levelDiff <= 0.1f && levelDiff > -0.15f)
        {
            generalStrings.Add(StringManager.GetString("deathtip_floor_hard"));
        }
        else if (levelDiff <= -0.15f)
        {
            generalStrings.Add(StringManager.GetString("deathtip_floor_veryhard"));
        }

        if (!PlayerHasPetNow() && !GameStartData.gameInSharaMode)
        {
            if (PlayerHasAvailablePets())
            {
                generalStrings.Add(StringManager.GetString("deathtip_pets_available"));
            }
            else if (!PlayerNeedsMonstersInCorral())
            {
                generalStrings.Add(StringManager.GetString("deathtip_pets_raise"));
            }
            else if (!GameStartData.gameInSharaMode)
            {
                generalStrings.Add(StringManager.GetString("deathtip_pets_capture"));
            }
        }

        /* if (generalStrings.Count > 0)
        {
            //buildAdviceString += StringManager.GetString("deathtip_general_header") + "\n\n";
            foreach (string tip in generalStrings)
            {
                buildAdviceString += "* " + tip + "\n\n";
            }
        } */

        if (generalStrings.Count > 4)
        {
            generalStrings.Shuffle();
            int diff = generalStrings.Count - 4;
            for (int i = 0; i < diff; i++)
            {
                generalStrings.RemoveAt(0);
            }
        }

        for (int i = 0; i < generalStrings.Count; i++)
        {
            buildAdviceString += generalStrings[i];
            if (i < generalStrings.Count-1)
            {
                buildAdviceString += "\n\n";
            }
            else
            {
                buildAdviceString += "\n";
            }
        }

        return buildAdviceString;
    }

    public static bool RecentTurnsTooFast()
    {
        float maxTimeThreshold = 1.2f; // anything faster is too fast!

        float avg = 0;
        for (int i = 0; i < GameMasterScript.gmsSingleton.trueTimeOfRecentTurns.Length; i++)
        {
            avg += GameMasterScript.gmsSingleton.trueTimeOfRecentTurns[i];
        }

        avg /= GameMasterScript.gmsSingleton.trueTimeOfRecentTurns.Length;

        if (avg < maxTimeThreshold)
        {
            return true;
        }
        return false;
    }

    public static bool PlayerEscapingDungeon()
    {
        return GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_escapedungeon");
    }

    public static bool DreamcasterAvailableButUnused()
    {
        if (ProgressTracker.CheckProgress(TDProgress.ANCIENTCUBE, ProgressLocations.META) < 1) return false;
        if (GameMasterScript.heroPCActor.ReadActorData("dreams_defeated") < 1) return true;
        return false;
    }

    public static bool PlayerHasPetNow()
    {
        if (GameMasterScript.heroPCActor.HasMonsterPet()) return true;
        return false;
    }

    public static bool PlayerNeedsMonstersInCorral()
    {
        if (GameMasterScript.heroPCActor.lowestFloorExplored < 3) return false;
        if (MetaProgressScript.localTamedMonstersForThisSlot.Count == 0) return true;
        return false;
    }

    public static bool PlayerHasAvailablePets()
    {
        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[i];
            if (tcm.CanMonsterBePet()) return true;
        }
        return false;
    }

    public static bool PlayerMovementSkillAvailable()
    {
        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (abil.passiveAbility) continue;
            if (!abil.CanActorUseAbility(GameMasterScript.heroPCActor)) continue;
            if (abil.abilityFlags[(int)AbilityFlags.MOVESELF]) return true;
        }

        return false;
    }

	public static bool PlayerHasRangedWeaponOnHotbar()
    {
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            if (UIManagerScript.hotbarWeapons[i] == null) continue;
            if (UIManagerScript.hotbarWeapons[i].isRanged)
            {
                return true;
            }
        }
        return false;
    }

    public static bool PlayerIsFull()
    {
        return GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_foodfull");
    }

    public static float PlayerHealthLostLast3Turns()
    {
        float total = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += GameMasterScript.heroPCActor.damageTakenLastThreeTurns[i];
        }
        return total;







    }

    public static bool PlayerHasUsableRecoveryItem()
    {
        bool playerFull = PlayerIsFull();
        foreach(Item itm in GameMasterScript.heroPCActor.myInventory.GetInventory())
        {
            if (playerFull && itm.IsItemFood()) continue;
            if (itm.IsCurative(StatTypes.HEALTH))
            {
                return true;
            }
        }
        return false;
    }

    public static bool PlayerHealingFromFlask()
    {
        return GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_regenflask");
    }

    public static bool PlayerDiedToPhysical()
    {
        /* if (!GameMasterScript.heroPCActor.HasActorDataString("killedbydam")) return false;
        DamageTypes dType = (DamageTypes)Enum.Parse(typeof(DamageTypes), GameMasterScript.heroPCActor.ReadActorDataString("killedbydam")); */
        DamageTypes dType = GameMasterScript.heroPCActor.lastDamageTypeReceived;
        if (dType != DamageTypes.PHYSICAL) return false;
        return true;
    }

    public static bool PlayerHasShieldEquipped()
    {
        float blockPercent = GameMasterScript.heroPCActor.myEquipment.GetOffhandBlock();
        if (blockPercent < 1f)
        {
            return false;
        }
        return true;
    }

    public static float GetPlayerStrengthComparedToFloorChallenge()
    {
        float floorCV = MapMasterScript.activeMap.GetChallengeRating();
        float playerCV = 1.0f + (0.05f * (GameMasterScript.heroPCActor.myStats.GetLevel() - 1));
        float diff = playerCV - floorCV;
        return diff;
    }
    
}
