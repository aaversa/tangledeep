using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Text.RegularExpressions;

//Used to modify ability costs, can be applied to specific abilities,
//specific JOB abilities
//and frog
//also used to straight remap one ability to another
public class AbilityModifierEffect : EffectScript
{
    //flat adjustments 
    public int changeStaminaCost;
    public int changeEnergyCost;
    public int changeMaxCooldownTurns;

    //ghooooosts
    public int changeEchoCost;

    //maybe one day powers can cost health or change CT 
    //when paid for. We'll see.
    public int changeHealthCost;
    public int changeCTCost;

    //if not GENERIC or COUNT, will apply to every ability that uses this job.
    public CharacterJobs jobGroupToModify;

    //if the job is GENERIC, look here for a specific list of refNames to apply the effect to
    public List<string> abilityRefsToModify;

    //Any additional text we want to add or remove from the ability description?
    public string strTextToAddToDescription;
    public string strTextToRemoveFromDescription;

    //replace one ability with another
    //such as replacing cloak and dagger with Cloaks and Daggerinos
    public string strRemapAbilitiesToThisRef;

    public AbilityModifierEffect()
    {
        abilityRefsToModify = new List<string>();

        //Ensure this power doesn't modify a job group you don't intend to.
        jobGroupToModify = CharacterJobs.COUNT;
    }

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);
        AbilityModifierEffect nTemplate = template as AbilityModifierEffect;
        changeStaminaCost = nTemplate.changeStaminaCost;
        changeEnergyCost = nTemplate.changeEnergyCost;
        changeMaxCooldownTurns = nTemplate.changeMaxCooldownTurns;
        changeEchoCost = nTemplate.changeEchoCost;
        changeHealthCost = nTemplate.changeHealthCost;
        changeCTCost = nTemplate.changeCTCost;
        jobGroupToModify = nTemplate.jobGroupToModify;

        abilityRefsToModify = new List<string>();
        foreach (string s in nTemplate.abilityRefsToModify)
        {
            abilityRefsToModify.Add(s);
        }

        strTextToAddToDescription = nTemplate.strTextToAddToDescription;
        strTextToRemoveFromDescription = nTemplate.strTextToRemoveFromDescription;
        strRemapAbilitiesToThisRef = nTemplate.strRemapAbilitiesToThisRef;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        AbilityModifierEffect eff = compareEff as AbilityModifierEffect;
        if (changeCTCost != eff.changeCTCost) return false;
        if (changeMaxCooldownTurns != eff.changeMaxCooldownTurns) return false;
        if (changeEchoCost != eff.changeEchoCost) return false;
        if (changeEnergyCost != eff.changeEnergyCost) return false;
        if (changeHealthCost != eff.changeHealthCost) return false;
        if (changeStaminaCost != eff.changeStaminaCost) return false;
        if (jobGroupToModify != eff.jobGroupToModify) return false;
        foreach (string aRef in abilityRefsToModify)
        {
            if (!eff.abilityRefsToModify.Contains(aRef)) return false;
        }
        if (strTextToAddToDescription != eff.strTextToAddToDescription) return false;
        if (strTextToRemoveFromDescription != eff.strTextToRemoveFromDescription) return false;
        if (strRemapAbilitiesToThisRef != eff.strRemapAbilitiesToThisRef) return false;

        return true;
    }

    public override bool ReadNextNodeFromXML(XmlReader reader)
    {
        switch (reader.Name.ToLowerInvariant())
        {
            case "changestaminacost":
                changeStaminaCost = reader.ReadElementContentAsInt();
                return true;
            case "changeenergycost":
                changeEnergyCost = reader.ReadElementContentAsInt();
                return true;
            case "changemaxcooldownturns":
                changeMaxCooldownTurns = reader.ReadElementContentAsInt();
                return true;
            case "changeechocost":
                changeEchoCost = reader.ReadElementContentAsInt();
                return true;
            case "changectcost":
                changeCTCost = reader.ReadElementContentAsInt();
                return true;
            case "changehealthcost":
                changeHealthCost = reader.ReadElementContentAsInt();
                return true;
            case "jobgroup":
                jobGroupToModify = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), reader.ReadElementContentAsString().ToUpperInvariant());
                return true;
            case "abilityrefs":
                string strAbilities = reader.ReadElementContentAsString();
                string[] strSplit = strAbilities.Split(',');
                for (int t = 0; t < strSplit.Length; t++)
                {
                    abilityRefsToModify.Add(strSplit[t]);
                }
                return true;
            case "additionaldesc":
            case "additionaldescription":
                strTextToAddToDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                strTextToAddToDescription = System.Text.RegularExpressions.Regex.Unescape(strTextToAddToDescription);
                return true;
            case "removefromdesc":
            case "removefromdescription":
                string contentRef = reader.ReadElementContentAsString();
                strTextToRemoveFromDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(contentRef);
                if (string.IsNullOrEmpty(strTextToRemoveFromDescription))
                {
                    Debug.Log(effectRefName + " has null strTextToRemove... " + contentRef);
                }
                else
                {
                    strTextToRemoveFromDescription = System.Text.RegularExpressions.Regex.Unescape(strTextToRemoveFromDescription);
                }
                
                return true;
            case "abilityremap":
                strRemapAbilitiesToThisRef = reader.ReadElementContentAsString();
                return true;
        }

        return base.ReadNextNodeFromXML(reader);
    }
}

/*
 * <DisplayName>Spellshape: Line</DisplayName>
 * <DisplayName>Spellshape: Cone</DisplayName>
 * <DisplayName>Spellshape: Ray</DisplayName>
 * <DisplayName>Spellshape: Square</DisplayName>
 * 
 * <AbilityName>Spellshift: Penetrate</AbilityName>
 * <AbilityName>Spellshift: Materialize</AbilityName>
 * <AbilityName>Spellshift: Aura</AbilityName>
 * <AbilityName>Spellshift: Barrier</AbilityName>
 *  
 */

public enum ESpellShape
{
    NONE = 0,
    LINE,
    CONE,
    BURST,
    RAY,
    SQUARE,
    CLAW,
    PENETRATE,
    MATERIALIZE,
    AURA,
    BARRIER,
    HORSESHOE,
    PARABOLA,
    SURPRISE_ME,
    COUNT
}

//Data to change the shape of a power. Mainly used by the
//spellshaper job (duh) but now we can tie to items! 
public class SpellShaperEffect : AbilityModifierEffect
{
    public ESpellShape spellShape;
    public string strAdditionalAudio;
    public int changeRange;

    public override void CopyFromTemplate(EffectScript template)
    {
        base.CopyFromTemplate(template);

        SpellShaperEffect nTemplate = template as SpellShaperEffect;
        spellShape = nTemplate.spellShape;
        strAdditionalAudio = nTemplate.strAdditionalAudio;
        changeRange = nTemplate.changeRange;
    }

    public override bool CompareToEffect(EffectScript compareEff)
    {
        bool checkBase = base.CompareToEffect(compareEff);
        if (!checkBase) return checkBase;

        SpellShaperEffect eff = compareEff as SpellShaperEffect;
        if (spellShape != eff.spellShape) return false;
        if (strAdditionalAudio != eff.strAdditionalAudio) return false;
        if (changeRange != eff.changeRange) return false;

        return true;
    }

    public override bool ReadNextNodeFromXML(XmlReader reader)
    {
        switch (reader.Name.ToLowerInvariant())
        {
            case "changerange":
                changeRange = reader.ReadElementContentAsInt();
                return true;
            case "spellshape":
                spellShape = (ESpellShape)Enum.Parse(typeof(ESpellShape), reader.ReadElementContentAsString().ToUpperInvariant());
                return true;
            case "additionalaudio":
                strAdditionalAudio = reader.ReadElementContentAsString();
                return true;
        }

        return base.ReadNextNodeFromXML(reader);
    }
}