using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeStatModifierFunctions
{

    public static EffectResultPayload BasicTemplate(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        return erp;
    }

    public static EffectResultPayload DelayedPlayHealAnimation(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        CombatManagerScript.WaitThenGenerateSpecificEffect(ft.GetPos(), "FervirRecovery", effect, 0.25f, true);

        return erp;
    }

    public static EffectResultPayload NegativeValue(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        erp.value *= -1f;

        return erp;
    }

    public static EffectResultPayload SoulstealLoseStaminaEnergy(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        ft.myStats.ChangeStat(StatTypes.STAMINA, -7f, StatDataTypes.CUR, true);
        ft.myStats.ChangeStat(StatTypes.ENERGY, -7f, StatDataTypes.CUR, true);

        return erp;
    }

    

    public static EffectResultPayload PlayRecoveryFXOnSelf(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        CombatManagerScript.GenerateSpecificEffectAnimation(ft.GetPos(), "FervirRecoveryQuiet", effect, true);

        return erp;
    }

    public static EffectResultPayload SpellRefunder(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = GameMasterScript.heroPCActor.ReadActorData("lastenergyspent");

        if (erp.value < 0)
        {
            erp.value = 0;
        }

        return erp;
    }

    public static EffectResultPayload MajorClawRegeneration(ChangeStatEffect effect, Fighter ft, float baseValue)
    {
        EffectResultPayload erp = new EffectResultPayload();
        erp.value = baseValue;

        HeroPC hero = ft as HeroPC;
        float amountToHeal = hero.damageTakenLastThreeTurns[0] * 0.5f;

        erp.value = amountToHeal;

        if (erp.value == 0)
        {
            erp.cancel = true;
        }

        return erp;
    }

}

