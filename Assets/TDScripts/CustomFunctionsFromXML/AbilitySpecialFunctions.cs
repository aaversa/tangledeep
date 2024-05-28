using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AbilitySpecialFunctions
{

    public static void ResetLimitBreak(Fighter owner, AbilityScript ability, string[] extraArgs)
    {
        GameMasterScript.heroPCActor.myStats.RemoveStatusByRef("dragonbreak_icon");
    }

    public static void EnqueueGlideRefreshAtEndOfTurn(Fighter owner, AbilityScript ability, string[] extraArgs)
    {
        string[] args = new string[]
        {
            ability.refName,
            owner.actorUniqueID.ToString()
        };

        GameMasterScript.AddEndOfTurnFunction(TDGenericFunctions.RefreshSkillCooldownOnUniqueUse, args);
    }

    public static void RefreshInkstormRepetitions(Fighter owner, AbilityScript ability, string[] extraArgs)
    {
        int strokeCount = owner.myStats.CheckStatusQuantity("brushstroke_charge");
        owner.myStats.RemoveAllStatusByRef("brushstroke_charge");
        owner.SetActorData("last_brushstrokesused", strokeCount);
        ability.repetitions = strokeCount;
        // Possible to have NO repetitions at all.
    }

    public static void TeachPlayerAbility(Fighter owner, AbilityScript ability, string[] extraArgs)
    {
        // arg 0 = the ability name
        string abilRefToLearn = extraArgs[0];
        if (owner.myAbilities.HasAbilityRef(abilRefToLearn)) return;
        AbilityScript template = GameMasterScript.masterAbilityList[abilRefToLearn];
        GameMasterScript.heroPCActor.LearnAbility(template, true, true, true, true);
    }

    public static void BoostStat(Fighter owner, AbilityScript ability, string[] extraArgs)
    {
        // arg 0 = the stat
        // arg 1 = amount
        StatTypes stat = (StatTypes)Enum.Parse(typeof(StatTypes), extraArgs[0]);
        float amount = CustomAlgorithms.TryParseFloat(extraArgs[1]);
        owner.myStats.ChangeStat(stat, amount, StatDataTypes.ALL, true);

        if (stat != StatTypes.HEALTH)
        {
            owner.myStats.ChangeStat(StatTypes.HEALTH, 50f, StatDataTypes.ALL, true);
            StringManager.SetTag(0, StringManager.GetString("stat_health"));
            StringManager.SetTag(1, "50");
            GameLogScript.LogWriteStringRef("boost_stat_level_up_var");
            UIManagerScript.RefreshPlayerStats();
        }

        SharaModeStuff.CalculatePlayerLevelFromStatBoosts();
    }

}
