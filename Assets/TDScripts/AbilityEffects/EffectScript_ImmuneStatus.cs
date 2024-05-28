using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class ImmuneStatusEffect : EffectScript
{
    public List<string> immuneStatusRefs;
    public List<StatusFlags> immuneStatusFlags;
    public float chanceOfImmunity;
    public string refStringImmunityMessage;
    public bool resistAnyNegative;

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        ImmuneStatusEffect nTemplate = template as ImmuneStatusEffect;

        chanceOfImmunity = nTemplate.chanceOfImmunity;
        refStringImmunityMessage = nTemplate.refStringImmunityMessage;
        resistAnyNegative = nTemplate.resistAnyNegative;
        foreach (string str in nTemplate.immuneStatusRefs)
        {
            immuneStatusRefs.Add(str);
        }
        foreach (StatusFlags flg in nTemplate.immuneStatusFlags)
        {
            immuneStatusFlags.Add(flg);
        }
    }

    public ImmuneStatusEffect()
    {
        immuneStatusFlags = new List<StatusFlags>();
        immuneStatusRefs = new List<string>();
        chanceOfImmunity = 1.0f;
        refStringImmunityMessage = "log_actor_resisteffect";
    }

    public bool CheckForImmunity(StatusEffect se)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return false;
        if (UnityEngine.Random.Range(0, 1f) > chanceOfImmunity)
        {
            return false;
        }
        if (!se.isPositive)
        {
            if (resistAnyNegative && se.refName != "status_foodfull")
            {
                DisplayImmunityMessage(se);
                return true;
            }
        }
        if (immuneStatusRefs.Contains(se.refName))
        {
            if (effectRefName == "stickyresist") // use 3 stamina for this to work.
            {
                if (GameMasterScript.heroPCActor.myStats.GetCurStat(StatTypes.STAMINA) < 3) return false;
                GameMasterScript.heroPCActor.myStats.ChangeStat(StatTypes.STAMINA, -3f, StatDataTypes.CUR, true);
            }

            DisplayImmunityMessage(se);
            return true;
        }
        for (int i = 0; i < se.statusFlags.Length; i++)
        {
            if (se.statusFlags[i])
            {
                if (immuneStatusFlags.Contains((StatusFlags)i))
                {
                    DisplayImmunityMessage(se);
                    return true;
                }
            }
        }
        return false;
    }

    public void DisplayImmunityMessage(StatusEffect se)
    {
        Actor localAct = selfActor;
        if (selfActor == null)
        {
            localAct = originatingActor;
        }
        if (localAct == null)
        {
            return;
        }
        StringManager.SetTag(0, localAct.displayName);
        StringManager.SetTag(1, se.abilityName);

        GameLogScript.LogWriteStringRef(refStringImmunityMessage);

        BattleTextManager.NewText(StringManager.GetString("battletext_resiststatus"), originatingActor.GetObject(), Color.green, 0.15f);
    }

    public override float DoEffect(int indexOfEffect = 0)
    {
        return 0.0f;
    }
}