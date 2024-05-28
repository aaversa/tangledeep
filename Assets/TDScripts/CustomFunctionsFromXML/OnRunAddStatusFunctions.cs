using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnRunAddStatusFunctions {

    public static void ResetLimitBreak(Fighter owner, StatusEffect se)
    {
        GameMasterScript.heroPCActor.ResetLimitBreak();
    }

    public static void AddDangerMagnetFlag(Fighter owner, StatusEffect se)
    {
        owner.SetActorData("dangermagnet", 1);
    }

    public static void RemoveDangerMagnetFlag(Fighter owner, StatusEffect se)
    {
        owner.RemoveActorData("dangermagnet");
    }

    public static void RemoveShieldBar(Fighter owner, StatusEffect se)
    {
        owner.SetActorData("shieldinfo_dirty", 1);
        owner.SetActorData("flowshield_dmgleft", 0);
        owner.SetActorData("flowshield_dmgmax", 0);

        if (!GameMasterScript.gmsSingleton.turnExecuting)
        {
            UIManagerScript.RefreshPlayerStats();
        }        
    }

    /// <summary>
    /// Set starting values for a shield that soaks damage against fighter health.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="shieldAmount"></param>
    static void SetShieldHealthValueForFighter(Fighter owner, int shieldAmount)
    {
        owner.SetActorData("flowshield_dmgleft", shieldAmount);
        //owner.SetActorData("flowshield_dmgmax", shieldAmount);
        owner.SetActorData("flowshield_dmgmax", (int)owner.myStats.GetMaxStat(StatTypes.HEALTH));
        owner.SetActorData("shieldinfo_dirty", 1);
    }

    public static void SetFlowShieldHealth(Fighter owner, StatusEffect se)
    {
        int shieldAmount = (int)(owner.myStats.GetMaxStat(StatTypes.HEALTH) * 0.4f);

        if (se.refName == "bubbleshield")
        {
            shieldAmount = owner.myStats.GetLevel() * 30;
        }

        SetShieldHealthValueForFighter(owner, shieldAmount);
        owner.myStats.AddStatusByRef("flowshield_destroy_fx", owner, 99);

        StringManager.SetTag(0, owner.displayName);
        GameLogScript.LogWriteStringRef("exp_energyshield_add");

        owner.myAbilities.ResetCooldownForAbilityByRef("skill_voidshield");
    }

    public static void SetEssenceStormAmmo(Fighter owner, StatusEffect se)
    {
        owner.SetActorData("essencestorm_ammo", 5);
    }    

    public static void SetVoidShieldHealth(Fighter owner, StatusEffect se)
    {
        int shieldAmount = (int)(owner.myStats.GetMaxStat(StatTypes.HEALTH) * 0.3f);
        SetShieldHealthValueForFighter(owner, shieldAmount);

        owner.myStats.AddStatusByRef("voidshield_destroy_fx", owner, 99);
        owner.myStats.AddStatusByRef("voidshieldbolts", owner, 99);

        owner.myAbilities.ResetCooldownForAbilityByRef("skill_flowshield");
    }

    public static void AddCharmedToSelf(Fighter owner, StatusEffect se)
    {
        if (owner.GetActorType() != ActorTypes.MONSTER) return;
        Monster mn = owner as Monster;        
        if (mn.isBoss || mn.isChampion) return;

        int dominatedCreatures = SharaModeStuff.GetNumberOfDominatedCreatures();
        int dominatedCreatureCap = SharaModeStuff.GetDominateCreatureCap();

        //Debug.Log("Creature cap is: " + dominatedCreatureCap + " vs dom creatures " + dominatedCreatures);

        if (dominatedCreatures >= dominatedCreatureCap)
        {
            StringManager.SetTag(0, dominatedCreatureCap.ToString());
            GameLogScript.LogWriteStringRef("log_shara_maxcreatures");
            if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_exp_sharamode_maxcreatures"))
            {
                GameMasterScript.gmsSingleton.SetTempStringData("sharaabilityname", SharaModeStuff.GetDominateDisplayName());
                GameMasterScript.gmsSingleton.SetTempStringData("sharamaxcreatures", dominatedCreatureCap.ToString());
                Conversation c = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_exp_sharamode_maxcreatures");
                UIManagerScript.StartConversation(c, DialogType.STANDARD, null);
            }
            owner.myStats.RemoveStatusByRef("exp_status_dominated");
            return;
        }

        if (dominatedCreatureCap == 6 && mn.GetXPModToPlayer() >= 0.25f)
        {
            // Ability is maxed out, so this creature now gets a status that heals Shara on death.
            mn.myStats.AddStatusByRef("dominate_healondeath", GameMasterScript.heroPCActor, 99);            
        }        

        owner.myStats.AddStatusByRefAndLog("status_permacharmed", GameMasterScript.heroPCActor, 999);
        GameMasterScript.heroPCActor.AddSummon(owner);
        GameMasterScript.heroPCActor.AddAnchor(owner);
        owner.anchor = GameMasterScript.heroPCActor;
        owner.summoner = GameMasterScript.heroPCActor;
        owner.actorfaction = Faction.PLAYER;
        mn.bufferedFaction = Faction.PLAYER;
        owner.anchorRange = 3;
        BattleTextManager.NewText(StringManager.GetString("exp_misc_dominated"), owner.GetObject(), Color.green, 0.25f, 1f);
    }

    public static void IncreaseUnfriendlyMonsterCount(Fighter owner, StatusEffect se)
    {
        if (MapMasterScript.activeMap != null && owner.dungeonFloor == MapMasterScript.activeMap.floor)
        {
            MapMasterScript.activeMap.unfriendlyMonsterCount++;
        }
    }

    public static void ReduceUnfriendlyMonstersAndCheckForSideAreaClear(Fighter owner, StatusEffect se)
    {
        if (MapMasterScript.activeMap != null && owner.dungeonFloor == MapMasterScript.activeMap.floor)
        {
            MapMasterScript.activeMap.unfriendlyMonsterCount--;
            CombatResultsScript.CheckForSideAreaClear(MapMasterScript.activeMap);
        }
    }

    public static void CheckForChefBuff(Fighter owner, StatusEffect se)
    {
        if (owner.myStats.CheckHasStatusName("mmchef"))
        {
            owner.myStats.AddStatusByRefAndLog("mmchef_buff", owner, 99);
        }
    }

    public static void CheckForRemoveChefBuff(Fighter owner, StatusEffect se)
    {
        owner.myStats.ForciblyRemoveStatus("mmchef_buff");
    }

    public static void RevertFireToShadowDamageDealtConversion(Fighter owner, StatusEffect se)
    {
        owner.cachedBattleData.damageTypeDealtConversions[(int)DamageTypes.FIRE] = DamageTypes.FIRE;
    }

}
