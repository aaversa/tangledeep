using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;

public enum AutoEatState { NOT_CHECKED, INVALID, HEAL_HP, HEAL_ENERGY, HEAL_STAMINA, COUNT }

[System.Diagnostics.DebuggerDisplay("{actorRefName}({displayName}) x {quantity}")]
public class Consumable : Item
{
    public List<EffectScript> consumableEffects;
    public string effectDescription;
    public string unbakedEffectDescription;
    public int curUsesRemaining;
    public int maxUses;

    public AutoEatState cachedAutoEatState;

    static readonly List<string> stringDataStrings = new List<string> { "monsterletter_author", "monsterletter_recipient", "monsterletter_skill" };
    static readonly List<string> intDataStrings = new List<string> { "monsterletter_skill_minrange", "monsterletter_skill_maxrange", "monsterletter_skill_usestate", "monsterletter_skill_health", "monsterletter_skill_chance" };

    int quantity;
    public int Quantity
    {
        get
        {
            return quantity;
        }
        set
        {
            /* if (GameMasterScript.gameLoadSequenceCompleted)
            {
                Debug.Log("Setting " + actorUniqueID + " quantity to " + value);
            }   */         
            quantity = value;
        }
    }
    public bool isFood;
    public bool isHealingFood;
    public bool isDamageItem;
    public AbilityScript parentForEffectChildren;
    public KeyCode binding;
    public bool isTreeSeed;
    public bool spawnFromTree;
    public bool seasoning;
    public bool cookingIngredient;
    public string seasoningAttached;

    public Consumable()
    {
        consumableEffects = new List<EffectScript>();
        curUsesRemaining = 1;
        maxUses = 1;
        quantity = 1;
        isTreeSeed = false;
        spawnFromTree = false;
        seasoningAttached = "";
        effectDescription = "";
        unbakedDescription = "";
    }

    public override void ParseNumberTags()
    {
        base.ParseNumberTags();
        unbakedEffectDescription = effectDescription;
        if (!numberTags.Any()) return;
        for (int i = 0; i < numberTags.Count; i++)
        {            
            effectDescription = effectDescription.Replace("^number" + (i + 1) + "^", "<color=yellow>" + numberTags[i] + "</color>");
        }
    }

    public bool IsConsumableStackable(Consumable existingItem)
    {
        if (CheckTag(ItemFilters.DICT_NOSTACK))
        {
            foreach(string key in dictActorData.Keys)
            {
                if (existingItem.ReadActorData(key) != dictActorData[key])
                {
                    return false;
                }
            }
            foreach (string key in dictActorDataString.Keys)
            {
                if (existingItem.ReadActorDataString(key) != dictActorDataString[key])
                {
                    return false;
                }
            }
        }
        if (actorRefName == "orb_itemworld")
        {
            if (GetOrbMagicModRef() != existingItem.GetOrbMagicModRef()) return false;
            if (ReadActorData("nightmare_orb") != (existingItem.ReadActorData("nightmare_orb"))) return false;
        }
        else if (actorRefName == "item_lucidorb_shard")
        {
            if (GetOrbMagicModRef() != existingItem.GetOrbMagicModRef())
            {
                return false;
            }
        }
        else if (actorRefName == "item_monsterletter")
        {
            if (!IsMonsterLetterSameAsOtherLetter(existingItem)) return false;
        }
        if (dreamItem != existingItem.dreamItem) return false;
        if (seasoning != existingItem.seasoning) return false;
        if (rarity != existingItem.rarity) return false;
        return true;

    }

    public bool IsMonsterLetterSameAsOtherLetter(Consumable c)
    {
        foreach (string compareString in stringDataStrings)
        {
            if (ReadActorDataString(compareString) != c.ReadActorDataString(compareString))
            {
                //Debug.Log(ReadActorDataString(compareString) + " for " + compareString + " is not the same as " + c.ReadActorDataString(compareString));
                return false;
            }
        }
        foreach (string compareString in intDataStrings)
        {
            if (ReadActorData(compareString) != c.ReadActorData(compareString))
            {
                //Debug.Log(ReadActorData(compareString) + " for " + compareString + " is not the same as " + c.ReadActorData(compareString));
                return false;
            }
        }
        return true;
    }

    public void InscribeMonsterLetter(ReleasedMonster writer)
    {
        SetActorDataString("monsterletter_author", writer.displayName);
        SetActorDataString("monsterletter_recipient", writer.firstOwner);
        SetActorDataString("monsterletter_skill", writer.teachAbilityRef);

        SetActorData("monsterletter_skill_minrange", writer.mpd.minRange);
        SetActorData("monsterletter_skill_maxrange", writer.mpd.maxRange);
        SetActorData("monsterletter_skill_usestate", (int)writer.mpd.useState);
        SetActorData("monsterletter_skill_health", (int)(writer.mpd.healthThreshold * 100f));
        SetActorData("monsterletter_skill_chance", (int)(writer.mpd.chanceToUse * 100f));

        if (writer.mpd.useWithNoTarget)
        {
            SetActorData("monsterletter_skill_usewithnotarget", 1);
        }


        RebuildDisplayName();
    }

    // #todo - Data drive this
    public bool CheckIfItemCanBeUsed()
    {
        if (GameStartData.CheckGameModifier(GameModifiers.CONSUMABLE_COOLDOWN) && !isFood)
        {
            if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("status_consumableburnout"))
            {
                UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("log_error_consumableburnout");
                return false;
            }
        }
        switch (actorRefName)
        {
            case "item_dreamdrum":
                if (!MapMasterScript.activeMap.IsItemWorld())
                {
                    UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                    return false;
                }
                break;
            case "item_monsterletter":
                Monster pet = GameMasterScript.heroPCActor.GetMonsterPet();
                if (pet == null)
                {
                    UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                    GameLogScript.LogWriteStringRef("log_error_monsterletter_nopet");
                    return false;
                }
                string abilRef = ReadActorDataString("monsterletter_skill");
                if (pet.myAbilities.HasAbilityRef(abilRef))
                {
                    UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                    StringManager.SetTag(0, pet.displayName);
                    GameLogScript.LogWriteStringRef("log_error_monsterletter_pethasskill");
                    return false;
                }
                break;
        } 
        if (!string.IsNullOrEmpty(ReadActorDataString("teachskill")))
        {
            if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(ReadActorDataString("teachskill")))
            {
                UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
                GameLogScript.LogWriteStringRef("log_error_know_skill");
                return false;
            }
        }

        return true;
    }

    public override int GetQuantity()
    {
        return quantity;
    }

    public override bool CanBeUsed()
    {
        return !CheckTag((int)ItemFilters.VALUABLES);
    }

    public void AddSeasoningToName()
    {
        if (seasoning) return;

        switch (seasoningAttached)
        {
            case "spice_rosepetals":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_romantic") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_romantic") + " " + displayName;
                }
                
                break;
            case "spice_garlic":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_garlic") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_garlic") + " " + displayName;
                }
                break;
            case "spice_nutmeg":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_nutmeg") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_nutmeg") + " " + displayName;
                }
                break;
            case "spice_staranise":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_staranise") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_staranise") + " " + displayName;
                }
                break;
            case "spice_cilantro":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_cilantro") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_cilantro") + " " + displayName;
                }
                break;
            case "spice_cinnamon":
                if (StringManager.gameLanguage == EGameLanguage.es_spain)
                {
                    displayName = displayName + " (" + StringManager.GetString("misc_cinnamon") + ")";
                }
                else
                {
                    displayName = StringManager.GetString("misc_cinnamon") + " " + displayName;
                }
                break;
        }
    }

    public string GetFoodFullTurns()
    {
        if (!isFood) return "";
        if (parentForEffectChildren == null) return "";

        foreach (EffectScript eff in parentForEffectChildren.listEffectScripts)
        {
            if (eff.effectType == EffectType.ADDSTATUS)
            {
                AddStatusEffect ase = eff as AddStatusEffect;
                if (ase.statusRef == "status_foodfull")
                {
                    int dur = (int)ase.baseDuration;
                    dur -= GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("status_mmhungry");
                    StringManager.SetTag(0, dur.ToString());
                    return UIManagerScript.redHexColor + StringManager.GetString("misc_full_description") + "</color>";
                }
            }
        }

        return "";
    }

    public string EstimateItemDamage()
    {
        string builder = "";
        if (!isDamageItem) return builder;
        if (parentForEffectChildren == null) return builder;
        int count = 0;

        float multiplier = 1.0f;

        multiplier += 0.25f * GameMasterScript.heroPCActor.myStats.CheckStatusQuantity("status_itemdamageup");

        foreach (EffectScript eff in parentForEffectChildren.listEffectScripts)
        {
            if (eff.effectType == EffectType.DAMAGE)
            {
                DamageEffect de = eff as DamageEffect;

                Fighter origFighter = GameMasterScript.heroPCActor;

                float spiritMult = 1f + origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT);

                float calcSpiritPower = origFighter.cachedBattleData.spiritPower;
                if ((origFighter == GameMasterScript.heroPCActor) && (origFighter.myStats.CheckHasStatusName("status_kineticmagic")) && (origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.5f))
                {
                    calcSpiritPower *= 1.2f;
                }

                float attackerWP = origFighter.cachedBattleData.physicalWeaponDamage + (origFighter.cachedBattleData.physicalWeaponOffhandDamage * .75f);

                float value = 0;

                string strDamageAmount = "";

                if (de.anyDamageEquationVars)
                {
                    // Alternative to expression parser that is way, way, way faster
                    value += attackerWP * de.damageEquationVars[(int)EDamageEquationVars.ATK_WEAPON_POWER];
                    value += calcSpiritPower * de.damageEquationVars[(int)EDamageEquationVars.ATK_SPIRIT_POWER];
                    value += origFighter.myStats.GetCurStat(StatTypes.HEALTH) * de.damageEquationVars[(int)EDamageEquationVars.CUR_HP];
                    value += origFighter.myStats.GetLevel() * de.damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL];
                    value += de.effectPower;

                    float average = value;
                    if (de.damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
                    {
                        average += ((de.damageEquationVars[(int)EDamageEquationVars.RND_MIN] + de.damageEquationVars[(int)EDamageEquationVars.RND_MAX]) / 2f);
                        value += UnityEngine.Random.Range(de.damageEquationVars[(int)EDamageEquationVars.RND_MIN], de.damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
                    }

                    strDamageAmount = _InternalCalculateEffectWithoutEquation(origFighter, average, parentForEffectChildren, 1);
                }
                else
                {
                    EffectScript.fParser.AddConst("$AttackerWeaponPower", () => attackerWP);
                    EffectScript.fParser.AddConst("$EffectPower", () => de.effectPower);
                    EffectScript.fParser.AddConst("$AtkSpiritPower", () => calcSpiritPower);
                    EffectScript.fParser.AddConst("$AtkLevel", () => origFighter.myStats.GetLevel());
                    EffectScript.fParser.AddConst("$AtkHealthPercent", () => origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH));
                    value = (float)EffectScript.fParser.Evaluate(de.effectEquation);
                    strDamageAmount = GetStatRangeForDisplay(origFighter, de.effectEquation, parentForEffectChildren, 1);
                }


                

                string elemStr = StringManager.GetString("misc_dmg_" + de.damType.ToString().ToLowerInvariant());
                builder += StringManager.GetString("misc_basedamage") + ": </color>" + UIManagerScript.cyanHexColor + strDamageAmount + "</color><color=yellow> " + elemStr;
                count++;
            }
        }
        return builder;
    }

    public string EstimateFoodHealing()
    {
        string builder = "";
        if (!isFood && !isHealingFood)
        {
            return builder;
        }
        if (parentForEffectChildren == null)
        {
            return builder;
        }
        int count = 0;
        foreach (EffectScript eff in parentForEffectChildren.listEffectScripts)
        {
            if (eff.effectType == EffectType.ADDSTATUS)
            {
                AddStatusEffect ase = eff as AddStatusEffect;
                int duration = (int)ase.baseDuration;

                float modifier = GameMasterScript.heroPCActor.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE) * 0.33f;
                float addDur = duration * modifier;

                duration += (int)addDur;

                StatusEffect template = GameMasterScript.FindStatusTemplateByName(ase.statusRef);
                ChangeStatEffect cse = null;
                foreach (EffectScript eff2 in template.listEffectScripts)
                {
                    if (eff2.effectType == EffectType.CHANGESTAT)
                    {
                        cse = eff2 as ChangeStatEffect;
                        Fighter origFighter = GameMasterScript.heroPCActor;

                        string strHealAmount = "";

                        float maxStat = origFighter.myStats.GetStat(cse.stat, StatDataTypes.MAX); // Should this be truemax?

                        if (cse.anyDamageEquationVars)
                        {
                            // Use a much faster method than parsing the expression.
                            float value = cse.baseAmount;
                            value += cse.damageEquationVars[(int)EDamageEquationVars.MAX_STAT] * maxStat;
                            value += cse.damageEquationVars[(int)EDamageEquationVars.CUR_STAT] * origFighter.myStats.GetCurStat(cse.stat);
                            value += cse.effectPower;
                            value += cse.damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL] * origFighter.myStats.GetLevel();
                            if (cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
                            {
                                value += UnityEngine.Random.Range(cse.damageEquationVars[(int)EDamageEquationVars.RND_MIN], cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
                            }

                            if (cse.modBySpirit)
                            {
                                value += (value * origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT));
                            }
                            if (cse.modByDiscipline)
                            {
                                value += (value * origFighter.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE));
                            } 
                            strHealAmount = _InternalCalculateEffectWithoutEquation(origFighter, value, parentForEffectChildren, duration);
                        }
                        else
                        {
                            float calcSpiritPower = origFighter.cachedBattleData.spiritPower;
                            if ((origFighter.myStats.CheckHasStatusName("status_kineticmagic")) && (origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.5f))
                            {
                                calcSpiritPower *= 1.2f;
                            }
                            //ExpressionParser fParser = new ExpressionParser();
                            EffectScript.fParser.AddConst("$EffectPower", () => (float)cse.effectPower);
                            EffectScript.fParser.AddConst("$AtkSpiritPower", () => calcSpiritPower);
                            EffectScript.fParser.AddConst("$MaxStat", () => maxStat);
                            EffectScript.fParser.AddConst("$StrengthMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.STRENGTH));
                            EffectScript.fParser.AddConst("$SpiritMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT));
                            EffectScript.fParser.AddConst("$DisciplineMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE));
                            strHealAmount = GetStatRangeForDisplay(origFighter, cse.effectEquation, parentForEffectChildren, duration);
                        }

                        if (count > 0)
                        {
                            builder += "\n";
                        }
                        StringManager.SetTag(0, strHealAmount);
                        StringManager.SetTag(1, StringManager.GetString("stat_" + cse.stat.ToString().ToLowerInvariant()));
                        StringManager.SetTag(2, duration.ToString());

                        builder += StringManager.GetString("ui_misc_healovertime_description");
                        count++;
                    }
                }
                if (cse == null)
                {
                    continue;
                }
            }
            else if (eff.effectType == EffectType.CHANGESTAT)
            {
                ChangeStatEffect cse = eff as ChangeStatEffect;
                Fighter origFighter = GameMasterScript.heroPCActor;

                float calcSpiritPower = origFighter.cachedBattleData.spiritPower;
                if (origFighter.myStats.CheckHasStatusName("status_kineticmagic") && origFighter.myStats.GetCurStatAsPercentOfMax(StatTypes.STAMINA) <= 0.5f)
                {
                    calcSpiritPower *= 1.25f;
                }

                string strHealAmount = "";

                if (cse.anyDamageEquationVars)
                {
                    // Use a much faster method than parsing the expression.
                    float value = cse.baseAmount;
                    value += cse.damageEquationVars[(int)EDamageEquationVars.MAX_STAT] * origFighter.myStats.GetMaxStat(cse.stat);
                    value += cse.damageEquationVars[(int)EDamageEquationVars.CUR_STAT] * origFighter.myStats.GetCurStat(cse.stat);
                    value += cse.effectPower;
                    value += cse.damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL] * origFighter.myStats.GetLevel();
                    if (cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
                    {
                        value += UnityEngine.Random.Range(cse.damageEquationVars[(int)EDamageEquationVars.RND_MIN], cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX]);
                    }

                    if (cse.modBySpirit)
                    {
                        value += (value * origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT));
                    }
                    if (cse.modByDiscipline)
                    {
                        value += (value * origFighter.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE));
                    }

                    // display random range X ~ Y
                    if (cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX] != 0)
                    {
                        float baseValue = cse.damageEquationVars[(int)EDamageEquationVars.MAX_STAT] * origFighter.myStats.GetMaxStat(cse.stat);
                        baseValue += cse.damageEquationVars[(int)EDamageEquationVars.ATK_LEVEL] * origFighter.myStats.GetLevel();

                        float infusionMult = 1f;
                        if (GameMasterScript.heroPCActor.ReadActorData("schematist_infuse") == 1)
                        {
                            infusionMult = 1.25f;
                        }
                        strHealAmount = (int)((cse.damageEquationVars[(int)EDamageEquationVars.RND_MIN]+baseValue)* infusionMult) + FontManager.GetValueRangeCharacterByLanguage() + (int)((cse.damageEquationVars[(int)EDamageEquationVars.RND_MAX]+baseValue)* infusionMult);
                    }
                    else
                    {
                        strHealAmount = _InternalCalculateEffectWithoutEquation(origFighter, value, parentForEffectChildren, 1);
                    }                    
                    //strHealAmount = GetStatRangeForDisplay(origFighter, fakeyFormula, parentForEffectChildren);
                }
                else
                {
                    EffectScript.fParser.AddConst("$EffectPower", () => (float)cse.effectPower);
                    float maxStat = origFighter.myStats.GetStat(cse.stat, StatDataTypes.MAX); // Should this be truemax?
                    EffectScript.fParser.AddConst("$AtkSpiritPower", () => calcSpiritPower);
                    EffectScript.fParser.AddConst("$MaxStat", () => maxStat);
                    EffectScript.fParser.AddConst("$TargetMaxHealth", () => origFighter.myStats.GetStat(StatTypes.HEALTH, StatDataTypes.MAX));
                    EffectScript.fParser.AddConst("$StrengthMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.STRENGTH));
                    EffectScript.fParser.AddConst("$SpiritMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.SPIRIT));
                    EffectScript.fParser.AddConst("$DisciplineMod", () => 1.0f + origFighter.myStats.GetCurStatAsPercent(StatTypes.DISCIPLINE));

                    strHealAmount = GetStatRangeForDisplay(origFighter, cse.effectEquation, parentForEffectChildren);
                }

                if (count > 0)
                {
                    builder += "\n";
                }
                StringManager.SetTag(0, strHealAmount);
                StringManager.SetTag(1, StringManager.GetString("stat_" + cse.stat.ToString().ToLowerInvariant()));

                builder += StringManager.GetString("ui_misc_healinstant_description");
                count++;
            }
        }
        return builder;
    }

    /*
        //one day when I am big boy I will learn regex
        rnd\(.*(\d+).*,.*(\d+).*\) 
    */

    private string GetStatRangeForDisplay(Fighter user, string strFormula, AbilityScript parentAbility, int duration = 1)
    {
        while (strFormula.Contains("rnd"))
        {
            //example: $AttackerWeaponPower * 1.5 + rnd(4,8)
            int irndOpen = strFormula.IndexOf("rnd");
            int irndClose = strFormula.IndexOf(")", irndOpen);

            string strRandoSubstring = strFormula.Substring(irndOpen, (irndClose - irndOpen) + 1);

            //break this down into two pieces
            string strCarvedValue = strRandoSubstring;
            strCarvedValue = strCarvedValue.Replace("rnd(", "");
            strCarvedValue = strCarvedValue.Replace(")", "");

            string[] strNumArray = strCarvedValue.Split(',');

            //#todo: If someone uses "rnd(1)" or any other value that is just one number, the wheels fall off.
            int iMinRoll;
            int iMaxRoll;

            Int32.TryParse(strNumArray[0], out iMinRoll);
            Int32.TryParse(strNumArray[1], out iMaxRoll);

            string strMinRoll = strFormula.Replace(strRandoSubstring, iMinRoll.ToString());
            string strMaxRoll = strFormula.Replace(strRandoSubstring, iMaxRoll.ToString());

            //build a formula with the min, and one with the max
            string strBottom = CalculateEffectEquationIncludingHeroStatJibba(user, strMinRoll, parentAbility, duration);
            string strTop = CalculateEffectEquationIncludingHeroStatJibba(user, strMaxRoll, parentAbility, duration);

            //display both!            
            return strBottom + FontManager.GetValueRangeCharacterByLanguage() + strTop;
        }

        return CalculateEffectEquationIncludingHeroStatJibba(user, strFormula, parentAbility, duration);
    }

    string _InternalCalculateEffectWithoutEquation(Fighter user, float baseValue, AbilityScript parentAbil, int duration = 1)
    {
        float healAmount = baseValue;

        bool bIsPotion = parentAbil != null && parentAbil.abilityFlags[(int)AbilityFlags.POTION];

        //I have no idea how to tell if this is food, but I can see that it is not a potion
        if (!bIsPotion)
        {
            bool isFood = false;
            foreach(EffectScript eff in parentAbil.listEffectScripts)
            {
                if (eff.effectTags[(int)EffectTags.FOODHEAL])
                {
                    isFood = true;
                    break;
                }
            }
            if (isFood)
            {
                if (user.myStats.CheckHasStatusName("status_foodlover"))
                {
                    healAmount *= 1.18f;
                }
                if (user.myStats.CheckHasStatusName("status_mmgluttony"))
                {
                    healAmount *= 1.15f;
                }
                if (user.myStats.CheckHasStatusName("status_mmgluttony2"))
                {
                    healAmount *= 1.25f;
                }
            }
        }

        if (bIsPotion &&
            user.GetActorType() == ActorTypes.HERO &&
            user.ReadActorData("schematist_infuse") == 1)
        {
            healAmount *= 1.25f;
        }

        healAmount *= duration;
        return ((int)healAmount).ToString();
    }

    private string CalculateEffectEquationIncludingHeroStatJibba(Fighter user, string strFormula, AbilityScript parentAbil, int duration = 1)
    {
        float healAmount = (float)EffectScript.fParser.Evaluate(strFormula);

        return _InternalCalculateEffectWithoutEquation(user, healAmount, parentAbil, duration);
    }

    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        base.WriteToSave(writer);

        if (seasoningAttached != "")
        {
            writer.WriteElementString("seas", seasoningAttached);
        }

        if (quantity != 1)
        {
            writer.WriteElementString("qty", quantity.ToString());
        }

        if (maxUses != 1)
        {
            writer.WriteElementString("curuses", curUsesRemaining.ToString());
            writer.WriteElementString("maxuses", maxUses.ToString());
        }

        writer.WriteEndElement();
    }

    public override void CopyFromItem(Item preTemplate)
    {
        base.CopyFromItem(preTemplate);
        Consumable template = (Consumable)preTemplate as Consumable;
        isFood = template.isFood;
        isTreeSeed = template.isTreeSeed;
        effectDescription = template.effectDescription;
        if (template.parentForEffectChildren != null)
        {
            AbilityScript abil = new AbilityScript();
            AbilityScript.CopyFromTemplate(abil, template.parentForEffectChildren);
            parentForEffectChildren = abil;
        }
        foreach (EffectScript eff in template.consumableEffects)
        {
            EffectScript newEff = AbilityScript.GetNewEffectFromTemplate(eff);
            newEff.parentAbility = parentForEffectChildren;
            consumableEffects.Add(newEff);
        }

        if (preTemplate.GetActorDataDict() != null)
        {
            if (dictActorData == null)
            {
                dictActorData = new Dictionary<string, int>();
            }
            foreach (string key in preTemplate.GetActorDataDict().Keys)
            {
                if (!dictActorData.ContainsKey(key))
                {
                    SetActorData(key, preTemplate.ReadActorData(key));
                }
            }
        }

        if (preTemplate.GetActorDataStringDict() != null)
        {
            if (dictActorDataString == null)
            {
                dictActorDataString = new Dictionary<string, string>();
            }
            foreach (string key in preTemplate.GetActorDataStringDict().Keys)
            {
                if (!dictActorDataString.ContainsKey(key))
                {
                    SetActorDataString(key, preTemplate.ReadActorDataString(key));
                }
            }
        }

        curUsesRemaining = template.curUsesRemaining;
        maxUses = template.maxUses;
        spawnFromTree = template.spawnFromTree;
        isHealingFood = template.isHealingFood;
        isDamageItem = template.isDamageItem;
        cookingIngredient = template.cookingIngredient;
        seasoning = template.seasoning;
        seasoningAttached = template.seasoningAttached;        
    }

    /// <summary>
    ///  Returns FALSE if there are no items left in stack.
    /// </summary>
    /// <param name="amt"></param>
    /// <returns></returns>
    public override bool ChangeQuantity(int amt)
    {
        Quantity += amt;
        if (Quantity <= 0)
        {
            return false;
        }
        return true;
    }

    public override bool ValidateEssentialProperties()
    {
        if (!base.ValidateEssentialProperties())
        {
            return false;
        }

        if (string.IsNullOrEmpty(effectDescription) && parentForEffectChildren != null && !isHealingFood && !isDamageItem)
        {
#if UNITY_EDITOR
            //Debug.Log("Warning! Consumable ref " + actorRefName + " has a skill attached, and is not a healing OR damage item, but does not have an effectDescription.");
#endif
            //return false;
        }

        /* if ((isFood || isHealingFood) && parentForEffectChildren == null)
        {
            Debug.LogError("Consumable ref " + actorRefName + " is marked as food or healing, but does not have a skill attached.");
            return false;
        } */

        return true;
    }

    public override bool TryReadFromXml(XmlReader reader)
    {
        if (base.TryReadFromXml(reader))
        {
            return true;
        }
        switch(reader.Name)
        {
            case "ItemPower":
                parentForEffectChildren = AbilityScript.GetAbilityByName(reader.ReadElementContentAsString());
                return true;
            case "ItemGrantStatus":
                string unparsed = reader.ReadElementContentAsString();
                string[] parsed = unparsed.Split(',');
                if (parsed.Length < 2)
                {
                    Debug.Log("Error with syntax " + actorRefName + " node ItemGrantStatus. Did you use a command to separate status ref and duration? " + unparsed);
                    return true;
                }

                string statusRef = parsed[0];
                int duration = 0;
                if (!Int32.TryParse(parsed[1], out duration))
                {
                    Debug.Log("Error with syntax " + actorRefName + " node ItemGrantStatus. " + parsed[1] + " is not a valid duration.");
                    return true;
                }

                string spriteEffectRef = "";
                if (parsed.Length >= 3)
                {
                    spriteEffectRef = parsed[2];
                }

                if (parentForEffectChildren == null)
                {
                    // Create a new ability.
                    AbilityScript nAbil = new AbilityScript();
                    nAbil.abilityName = displayName; // Ability is named after the item.
                    nAbil.refName = "abil_autogen_" + actorRefName;
                    nAbil.range = 1;
                    nAbil.boundsShape = TargetShapes.POINT;
                    nAbil.targetForMonster = AbilityTarget.SELF;
                    nAbil.displayInList = true;

                    // Add standard tags for self-buffs
                    //nAbil.AddAbilityTag(AbilityTags.TARGETED);
                    nAbil.AddAbilityTag(AbilityTags.INSTANT);
                    nAbil.AddAbilityTag(AbilityTags.TVISIBLEONLY);
                    nAbil.AddAbilityTag(AbilityTags.HEROAFFECTED);
                    nAbil.AddAbilityTag(AbilityTags.PERTARGETANIM);
                    nAbil.AddAbilityTag(AbilityTags.GROUNDTARGET);
                    nAbil.AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                    nAbil.AddAbilityTag(AbilityTags.CENTERED);
                    parentForEffectChildren = nAbil;
                    if (string.IsNullOrEmpty(extraDescription))
                    {
                        nAbil.shortDescription = effectDescription;
                    }
                    else
                    {
                        nAbil.shortDescription = extraDescription;
                    }
                    
                }

                if (GameMasterScript.FindStatusTemplateByName(statusRef) == null)
                {
                    Debug.Log("WARNING: Status ref " + statusRef + " for item " + actorRefName + " does not exist. Skipping.");
                    return true;
                }

                // Now create the AddStatusEffect.
                AddStatusEffect ase = new AddStatusEffect();
                ase.effectRefName = "eff_" + parentForEffectChildren.refName;
                ase.effectType = EffectType.ADDSTATUS;
                ase.effectName = displayName;
                ase.tActorType = TargetActorType.ORIGINATING;
                ase.reqActorFaction = Faction.MYFACTION;
                ase.baseDuration = duration;
                ase.localDuration = duration;
                ase.statusRef = statusRef;
                if (spriteEffect != "")
                {
                    ase.spriteEffectRef = spriteEffectRef;
                    ase.playAnimation = true;
                    ase.animLength = 0.15f;
                }

                parentForEffectChildren.listEffectScripts.Add(ase);
                
                break;
            case "Food":
                //isFood = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                isFood = true;
                reader.Read();
                return true;
            case "HealingFood":
            case "HealingItem":
                isHealingFood = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "Healing":
                isHealingFood = true;
                reader.Read();
                return true;
            case "Ingredient":
                cookingIngredient = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "Ingr":
                cookingIngredient = true;
                reader.Read();
                return true;
            case "Seasoning":
                //seasoning = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                reader.Read();
                seasoning = true;
                return true;
            case "SeasoningAttached":
                seasoningAttached = reader.ReadElementContentAsString();
                return true;
            case "EffDesc":
            case "EffectDescription":
                effectDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                return true; 
            case "DamageItem":
                isDamageItem = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "Dmg":
                isDamageItem = true;
                reader.Read();
                return true;
            case "IsTreeSeed":
                isTreeSeed = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "SpawnFromTree":
                spawnFromTree = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                return true;
            case "TreeDrop":
                spawnFromTree = true;
                reader.Read();
                return true;
                
        }        

        return false;
    }

    public override int GenerateSubtypeAsInt()
    {
        int baseValue = 0;

        if (tags.Contains(ItemFilters.HEALHP))
        {
            baseValue = Item.HEALING_ITEM_SORT_BASE_VALUE;
        }
        else if (tags.Contains(ItemFilters.HEALSTAMINA))
        {
            baseValue = Item.HEALING_ITEM_SORT_BASE_VALUE + 10;
        }
        else if (tags.Contains(ItemFilters.HEALENERGY))
        {
            baseValue = Item.HEALING_ITEM_SORT_BASE_VALUE + 20;
        }            
        
        if (tags.Contains(ItemFilters.SELFBUFF))
        {
            baseValue = Item.BUFF_ITEM_SORT_BASE_VALUE;
        }

        if (isDamageItem)
        {
            baseValue = Item.DAMAGE_ITEM_BASE_VALUE;
        }

        if (baseValue == 0 && tags.Contains(ItemFilters.SUMMON))
        {
            baseValue = Item.SUMMON_ITEM_BASE_VALUE;
        }

        if (baseValue == 0 && (CheckTag(ItemFilters.VALUABLES) || CheckTag(ItemFilters.GEM)))
        {
            baseValue = VALUABLES_BASE_VALUE;
        }

        if (baseValue == 0 && CheckTag(ItemFilters.RECOVERY))
        {
            baseValue = Item.GENERIC_CONSUMABLE_BASE_VALUE;
        }

        if (baseValue == 0)
        {
            baseValue = Item.GENERIC_ITEM_BASE_VALUE;
        }

        return baseValue;
    }

    public override void SetTagsFromEffectIfAny()
    {
        if (parentForEffectChildren == null) return;

        foreach(EffectScript eff in parentForEffectChildren.listEffectScripts)
        {
            if (eff.effectType == EffectType.CHANGESTAT)
            {
                ChangeStatEffect cse = eff as ChangeStatEffect;
                if (cse.stat == StatTypes.HEALTH) tags.Add(ItemFilters.HEALHP);
                if (cse.stat == StatTypes.ENERGY) tags.Add(ItemFilters.HEALENERGY);
                if (cse.stat == StatTypes.STAMINA) tags.Add(ItemFilters.HEALSTAMINA);
            }
            else if (eff.effectType == EffectType.ADDSTATUS)
            {
                AddStatusEffect ase = eff as AddStatusEffect;
                StatusEffect statusRef = null;
                if (GameMasterScript.masterStatusList.TryGetValue(ase.statusRef, out statusRef))
                {
                    foreach(EffectScript subEffect in statusRef.listEffectScripts)
                    {
                        if (subEffect.effectType == EffectType.CHANGESTAT)
                        {
                            ChangeStatEffect subCSE = subEffect as ChangeStatEffect;
                            if (subCSE.stat == StatTypes.HEALTH) tags.Add(ItemFilters.HEALHP);
                            if (subCSE.stat == StatTypes.ENERGY) tags.Add(ItemFilters.HEALENERGY);
                            if (subCSE.stat == StatTypes.STAMINA) tags.Add(ItemFilters.HEALSTAMINA);
                        }
                    }
                }
                else
                {
                    if (Debug.isDebugBuild) Debug.Log(statusRef + " doesn't exist?");
                }
            }
        }
    }

    public AutoEatState IsValidForAutoEat(StatTypes stat)
    {
        if (cachedAutoEatState != AutoEatState.NOT_CHECKED)
        {
            return cachedAutoEatState;
        }

        Consumable con = this as Consumable;

        if (!CheckTag((int)ItemFilters.RECOVERY))
        {
            cachedAutoEatState = AutoEatState.INVALID;
            return cachedAutoEatState;
        }

        if (stat != StatTypes.COUNT)
        {
            // Must heal a specific stat.
            if (con.parentForEffectChildren == null)
            {
                cachedAutoEatState = AutoEatState.INVALID;
                return cachedAutoEatState;
            }
            foreach (EffectScript eff in con.parentForEffectChildren.listEffectScripts)
            {
                if (eff.effectType == EffectType.CHANGESTAT)
                {
                    cachedAutoEatState = AutoEatState.INVALID;
                    return cachedAutoEatState;
                }
                else if (eff.effectType == EffectType.ADDSTATUS)
                {
                    if (!string.IsNullOrEmpty(seasoningAttached))
                    {
                        cachedAutoEatState = AutoEatState.INVALID;
                        return cachedAutoEatState;
                    }
                    AddStatusEffect ase = eff as AddStatusEffect;
                    StatusEffect addedStatus = GameMasterScript.FindStatusTemplateByName(ase.statusRef);
                    if (!addedStatus.isPositive) continue;
                    foreach (EffectScript statusSubEffect in addedStatus.listEffectScripts)
                    {
                        if (statusSubEffect.effectType == EffectType.CHANGESTAT)
                        {
                            ChangeStatEffect cse = statusSubEffect as ChangeStatEffect;
                            if (cse.stat == stat)
                            {
                                switch (stat)
                                {
                                    case StatTypes.HEALTH:
                                        cachedAutoEatState = AutoEatState.HEAL_HP;
                                        break;
                                    case StatTypes.ENERGY:
                                        cachedAutoEatState = AutoEatState.HEAL_ENERGY;
                                        break;
                                    case StatTypes.STAMINA:
                                        cachedAutoEatState = AutoEatState.HEAL_STAMINA;
                                        break;
                                }

                                return cachedAutoEatState;
                            }
                        }
                    }
                }
            }
        }

        cachedAutoEatState = AutoEatState.INVALID;
        return cachedAutoEatState;

    }
}