using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Reflection;


public class InfluenceTurnEffect : EffectScript
{
    public float confuseChance;
    public float sleepChance;
    public float paralyzeChance;
    public float silenceChance;
    public float stunChance;
    public float rootChance;
    public float charmChance;
    public float fearChance;

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        InfluenceTurnEffect nTemplate = (InfluenceTurnEffect)template as InfluenceTurnEffect;
        confuseChance = nTemplate.confuseChance;
        sleepChance = nTemplate.sleepChance;
        paralyzeChance = nTemplate.paralyzeChance;
        silenceChance = nTemplate.silenceChance;
        stunChance = nTemplate.stunChance;
        rootChance = nTemplate.rootChance;
        charmChance = nTemplate.charmChance;
        fearChance = nTemplate.fearChance;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        InfluenceTurnEffect eff = compareEff as InfluenceTurnEffect;
        if (confuseChance != eff.confuseChance) return false;
        if (paralyzeChance != eff.paralyzeChance) return false;
        if (sleepChance != eff.sleepChance) return false;
        if (silenceChance != eff.silenceChance) return false;
        if (stunChance != eff.stunChance) return false;
        if (rootChance != eff.rootChance) return false;
        if (charmChance != eff.charmChance) return false;
        if (fearChance != eff.fearChance) return false;

        return true;
    }
    public override float DoEffect(int indexOfEffect = 0)
    {
        // Could there be animations with these?

        if (!VerifySelfActorIsFighterAndFix())
        {
            return 0f;
        }

        Fighter self = selfActor as Fighter;

        if (self.influenceTurnData == null)
        {
            self.influenceTurnData = new InfluenceTurnData();
        }
        if (confuseChance != 0)
        {
            self.influenceTurnData.confuseChance += confuseChance;
            self.influenceTurnData.anyChange = true;
        }
        if (sleepChance != 0)
        {
            self.influenceTurnData.sleepChance += sleepChance;
            self.influenceTurnData.anyChange = true;
        }
        if (stunChance != 0)
        {
            self.influenceTurnData.stunChance += stunChance;
            self.influenceTurnData.anyChange = true;
        }
        if (paralyzeChance != 0)
        {
            self.influenceTurnData.paralyzeChance += paralyzeChance;
            self.influenceTurnData.anyChange = true;
        }
        if (silenceChance != 0)
        {
            self.influenceTurnData.silenceChance += silenceChance;
            self.influenceTurnData.anyChange = true;
        }
        if (rootChance != 0)
        {
            self.influenceTurnData.rootChance += rootChance;
            self.influenceTurnData.anyChange = true;
        }
        if (charmChance != 0)
        {
            self.influenceTurnData.charmChance += charmChance;
            self.influenceTurnData.anyChange = true;
        }
        if (fearChance != 0)
        {
            self.influenceTurnData.fearChance += fearChance;
            self.influenceTurnData.anyChange = true;
        }

        // Any text needed? No?

        return 0.0f;
    }
}