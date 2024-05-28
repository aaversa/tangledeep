using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.IO;
using UnityEngine.Scripting;

public partial class GameMasterScript
{ 

    private IEnumerator LoadAllStatusEffects()
    {
        if (masterStatusList != null && masterStatusList.Count > 0)
        {
            yield break;
        }

    masterStatusList = new Dictionary<string, StatusEffect>();

    AbilityScript.statusTriggersAsStrings = new string[(int)StatusTrigger.COUNT];
    for (int i = 0; i < (int)StatusTrigger.COUNT; i++)
    {
        AbilityScript.statusTriggersAsStrings[i] = ((StatusTrigger)i).ToString();
    }

    List<string> allStatusFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.STATUSEFFECTS);
    PlayerModManager.AddModFilesToList(allStatusFilesToLoad, PlayerModfileTypes.STATUSEFFECTS);

    foreach (string statusText in allStatusFilesToLoad)
    {
        if (string.IsNullOrEmpty(statusText)) continue;
        using (XmlReader reader = XmlReader.Create(new StringReader(statusText)))
        {
            reader.Read();

            float timeAtLastYield = Time.realtimeSinceStartup;

            while (reader.Read())
            {
                bool isStatusStart = false;
                if (reader.Name == "SE") // was "StatusEffect"
                {
                    isStatusStart = true;
                }

                if (reader.NodeType == XmlNodeType.Element && isStatusStart)
                {

                    StatusEffect se = new StatusEffect();

                    bool endOfStatus = false;
                    bool readEffectNode = false;
                    bool readRunStatusNode = false;
                    bool readDurStatusNode = false;
                    bool readConsumeStatusNode = false;
                    EffectScript abilEffect = new EffectScript();
                    // Found a new item in the file.

                    while (!endOfStatus)
                    {
                        // reader.Read();        
                        string strValue = reader.Name;

                        if (reader.NodeType != XmlNodeType.EndElement)
                        {
                            bool readSomething = false;

                            if (readEffectNode) // BEGIN COPY CODE FROM ABILITY LOADER
                            {
                                abilEffect = ReadEffectScriptFromXML(strValue, reader, abilEffect);
                            } // END COPY CODE FROM ABILITY LOADER                   


                            // BEGIN STATUS EFFECT LOADER
                            if (readRunStatusNode)
                            {
                                for (int c = 0; c < (int)StatusTrigger.COUNT; c++)
                                {
                                    if (strValue == AbilityScript.statusTriggersAsStrings[c])
                                    {
                                        se.runStatusTriggers[c] = true;
                                        reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                }
                            }
                            if (readDurStatusNode)
                            {
                                for (int c = 0; c < (int)StatusTrigger.COUNT; c++)
                                {
                                    if (strValue == AbilityScript.statusTriggersAsStrings[c])
                                    {
                                        se.durStatusTriggers[c] = true;
                                        reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                }
                            }
                            if (readConsumeStatusNode)
                            {
                                for (int c = 0; c < (int)StatusTrigger.COUNT; c++)
                                {
                                    if (strValue == AbilityScript.statusTriggersAsStrings[c])
                                    {
                                        se.consumeStatusTriggers[c] = true;
                                        reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                }
                            }

                            switch (strValue)
                            {
                                case "DName":
                                case "DisplayName": // deprecate
                                    se.abilityName = CustomAlgorithms.ParseRichText(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()), false);
                                    se.BuildSortableName(); // for non-english sort purposes
                                    readSomething = true;
                                    break;
                                case "SGrnd":
                                    reader.Read();
                                        se.runStatusTriggers[(int)StatusTrigger.ENTERTILE] = true;
                                        se.runStatusTriggers[(int)StatusTrigger.ENDTURNINTILE] = true;
                                        se.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
                                        readSomething = true;
                                    break;
                                    case "AnimTags":
                                        se.AddAbilityTag(AbilityTags.PERTARGETANIM);
                                        se.AddAbilityTag(AbilityTags.SIMULTANEOUSANIM);
                                        readSomething = true;
                                        reader.Read();
                                        break;
                                    case "Instanim":
                                        se.AddAbilityTag(AbilityTags.INSTANT);
                                        se.AddAbilityTag(AbilityTags.PERTARGETANIM);
                                        readSomething = true;
                                        reader.Read();
                                        break;
                                    case "PermaBuff":
                                    se.runStatusTriggers[(int)StatusTrigger.ONADD] = true;
                                    se.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
                                    se.isPositive = true;
                                    se.noRemovalOrImmunity = true;
                                    reader.Read();
                                    break;
                                case "StatBuff":
                                    ReadStatBuff(se, reader);
                                    break;
                                case "PermaStackBuff":
                                    se.runStatusTriggers[(int)StatusTrigger.ONADD] = true;
                                    se.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
                                    se.isPositive = true;
                                    se.stackMultipleEffects = true;
                                    se.noRemovalOrImmunity = true;
                                    reader.Read();
                                    break;
                                case "RefreshDontStack":
                                    se.stackMultipleEffects = false;
                                    se.stackMultipleDurations = false;
                                    se.refreshDurationOnCast = true;
                                    reader.Read();
                                    break;
                                case "PermaBuffNoRun":
                                    se.runStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
                                    se.durStatusTriggers[(int)StatusTrigger.PERMANENT] = true;
                                    se.isPositive = true;
                                    se.noRemovalOrImmunity = true;
                                    reader.Read();
                                    break;
                                case "ATags":
                                case "AbilityTags": // deprecate
                                    string unparsed = reader.ReadElementContentAsString();
                                    string[] parsed = unparsed.Split(',');
                                    for (int i = 0; i < parsed.Length; i++)
                                    {
                                            //AbilityTags aTag = (AbilityTags)Enum.Parse(typeof(AbilityTags), parsed[i]);
                                            AbilityTags aTag = CustomAlgorithms.dictStrToAbilityTagEnum[parsed[i]];
                                        se.AddAbilityTag(aTag);
                                    }
                                    se.abilityTagsRead = true;
                                    readSomething = true;
                                    break;
                                case "Script_RunOnAdd":
                                    se.script_runOnAddStatus = reader.ReadElementContentAsString();
                                    readSomething = true;
                                    break;
                                case "Script_RunOnRemove":
                                    se.script_runOnRemoveStatus = reader.ReadElementContentAsString();
                                    readSomething = true;
                                    break;
                                    case "EffRef":
                                    case "AbilEffectRef":
                                    string loadEff = reader.ReadElementContentAsString();
                                    if (!se.load_effectRefsToConnect.Contains(loadEff))
                                    {
                                        se.load_effectRefsToConnect.Add(loadEff);
                                    }
                                    readSomething = true;
                                    break;
                                case "ReqWeaponType":
                                    se.reqWeaponType = (WeaponTypes)Enum.Parse(typeof(WeaponTypes), reader.ReadElementContentAsString());
                                    readSomething = true;
                                    break;
                                case "Rf":
                                case "RefName":
                                    se.refName = reader.ReadElementContentAsString();
                                    if (masterStatusList.ContainsKey(se.refName))
                                    {
#if UNITY_EDITOR
                                            if (Debug.isDebugBuild) Debug.Log("WARNING: Master status dict already has " + se.refName);
#endif
                                    }
                                    else
                                    {
                                        masterStatusList.Add(se.refName, se);
                                    }
                                    readSomething = true;
                                    break;
                                case "NTag":
                                case "NumberTag": // deprecate
                                    se.numberTags.Add(reader.ReadElementContentAsString());
                                    readSomething = true;
                                    break;
                                case "Log":
                                case "CombatLogText":
                                        se.combatLogText = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                    se.combatLogText = CustomAlgorithms.ParseRichText(se.combatLogText, false);
                                    readSomething = true;
                                    break;
                                case "SFXOverride":
                                    se.sfxOverride = reader.ReadElementContentAsString();

                                    if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                    {
                                        resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                            {se.sfxOverride, "SpriteEffects/" + se.sfxOverride});
                                    }
                                    else
                                    {
                                        TryPreloadResourceNoBundles(se.sfxOverride, "SpriteEffects/" + se.sfxOverride);
                                    }
                                    readSomething = true;
                                    break;
                                case "DirectionFollowActor":
                                    se.directionFollowActor = simpleBool[reader.ReadElementContentAsInt()];
                                    readSomething = true;
                                    break;
                                case "Desc":
                                case "Description": // deprecate
                                    se.description = CustomAlgorithms.ParseRichText(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()), false);
                                    readSomething = true;
                                    break;
                                case "CombatOnly":
                                    se.combatOnly = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "SpriteAlwaysVisible":
                                    se.spriteAlwaysVisible = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "NoSpriteRotation":
                                    se.noSpriteRotation = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "Pos":
                                case "IsPositive": // deprecate
                                    se.isPositive = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "MaxStacks":
                                    se.maxStacks = reader.ReadElementContentAsInt();
                                    readSomething = true;
                                    break;
                                case "Direction":
                                    se.direction = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString());
                                    readSomething = true;
                                    break;
                                // Shep: what did I tell you about case sensitive
                                case "ISpr":
                                case "IngameSprite": // deprecate
                                case "InGameSprite": // deprecate
                                    se.ingameSprite = reader.ReadElementContentAsString();

                                    if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                        {
                                            resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                                {se.ingameSprite, "SpriteEffects/" + se.ingameSprite});
                                        }
                                    else
                                        {
                                            TryPreloadResourceNoBundles(se.ingameSprite, "SpriteEffects/" + se.ingameSprite);
                                        }
                                    readSomething = true;
                                    break;
                                case "StackEff":
                                case "StackMultipleEffects":
                                    se.stackMultipleEffects = true;
                                    reader.Read();
                                    se.refreshDurationOnCast = false;
                                    readSomething = true;
                                    break;
                                case "PersistentDuration":
                                    se.persistentDuration = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "StackDur":
                                case "StackMultipleDurations":
                                    se.stackMultipleDurations = true;
                                    reader.Read();
                                    se.refreshDurationOnCast = false;
                                    readSomething = true;
                                    break;
                                case "DestroyStatusOnRemove":
                                    string statusName = reader.ReadElementContentAsString();
                                    se.destroyStatusOnRemove.Add(statusName);
                                    break;
                                case "DestroyStatusOnAdd":
                                    string conflictName = reader.ReadElementContentAsString();
                                    se.destroyStatusOnAdd.Add(conflictName);
                                    break;
                                case "RefreshDurationOnCast":
                                case "RefreshDuration":
                                    se.refreshDurationOnCast = simpleBool[reader.ReadElementContentAsInt()];
                                    readSomething = true;
                                    break;
                                case "NoDurationExtension":
                                    se.noDurationExtensionFromStats = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "ExcludeFromHotbarCheck":
                                    se.excludeFromHotbarCheck = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "NoRemovalOrImmunity":
                                    se.noRemovalOrImmunity = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "EnergyTick":
                                    se.energyTick = reader.ReadElementContentAsInt();
                                    readSomething = true;
                                    break;
                                case "EnergyReq":
                                    se.energyReq = reader.ReadElementContentAsInt();
                                    readSomething = true;
                                    break;
                                case "SpiritsRequired":
                                    se.spiritsRequired = reader.ReadElementContentAsInt();
                                    break;
                                case "StaminaReq":
                                    se.staminaReq = reader.ReadElementContentAsInt();
                                    readSomething = true;
                                    break;
                                case "StaminaTick":
                                    se.staminaTick = reader.ReadElementContentAsInt();
                                    readSomething = true;
                                    break;
                                case "CommandsOnAdd":
                                case "OnAddCommands":
                                    se.commandsOnAdd = reader.ReadElementContentAsString();
                                    readSomething = true;
                                    break;
                                case "CommandsOnRemove":
                                case "OnRemoveCommands":
                                    se.commandsOnRemove = reader.ReadElementContentAsString();
                                    readSomething = true;
                                    break;
                                case "Icon":
                                case "StatusIconRef": // deprecate
                                    se.statusIconRef = reader.ReadElementContentAsString();
                                    readSomething = true;
                                    break;
                                case "Flag":
                                    StatusFlags sf = (StatusFlags)Enum.Parse(typeof(StatusFlags), reader.ReadElementContentAsString());
                                    se.statusFlags[(int)sf] = true;
                                    readSomething = true;
                                    break;
                                case "ShowIcon":
                                    se.showIcon = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "AEff":
                                case "AbilEffect": // deprecate
                                    abilEffect = new EffectScript();
                                    readEffectNode = true;
                                    reader.Read();
                                    break;
                                case "Run":
                                    // This should always be a start.
                                    readRunStatusNode = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "Dur":
                                    // This should always be a start.
                                    readDurStatusNode = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;
                                case "Eat":
                                    // This should always be a start.
                                    readConsumeStatusNode = true;
                                    reader.Read();
                                    readSomething = true;
                                    break;

                                //SHEP: Scripts based on events
                                case "Script_AttackBlock":
                                    {
                                        se.script_AttackBlock = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;

                                case "Script_SpecialTargeting":
                                    {
                                        se.script_SpecialTargeting = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;

                                case "Script_FighterBelowHalfHealth":
                                    {
                                        se.script_FighterBelowHalfHealth = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;
                                case "Script_FighterBelow60Health":
                                    {
                                        se.script_FighterBelow60Health = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;

                                case "Script_FighterBelow33Health":
                                    {
                                        se.script_FighterBelow33Health = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;
                                case "Script_FighterBelowQuarterHealth":
                                    {
                                        se.script_FighterBelowQuarterHealth = reader.ReadElementContentAsString();
                                        readSomething = true;
                                    }
                                    break;

                                case "EffectConditional":
                                    if (!se.hasConditions)
                                    {
                                        se.conditions = new List<EffectConditional>();
                                        se.hasConditions = true;
                                    }
                                    reader.ReadStartElement();
                                    EffectConditional ec = new EffectConditional();
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "Index":
                                                ec.index = reader.ReadElementContentAsInt();
                                                break;
                                            case "Conditional":
                                                ec.ec = (EffectConditionalEnums)Enum.Parse(typeof(EffectConditionalEnums), reader.ReadElementContentAsString());
                                                break;
                                            default:
                                                reader.Read();
                                                break;
                                        }
                                    }
                                    se.conditions.Add(ec);
                                    reader.ReadEndElement();
                                    break;
                            }
                            /* if (reader.NodeType == XmlNodeType.Element && !se.abilityTagsRead)
                            {
                                for (int c = 0; c < (int)AbilityTags.COUNT; c++)
                                {
                                    if (strValue == AbilityScript.abilityTagsAsStrings[c])
                                    {
                                        if (reader.ReadElementContentAsInt() > 0)
                                        {
                                            se.AddAbilityTag((AbilityTags)c);
                                        }


                                        //se.myAbilityTags[c] = simpleBool[reader.ReadElementContentAsInt()];
                                        //if (se.refName == "status_firebreath1") Debug.Log(se.abilityName + " Value for " + strValue.ToString() + " is " + se.myAbilityTags[c]);
                                        readSomething = true;
                                    }
                                }
                            } */
                            if (!readSomething)
                            {
                                reader.Read();
                            }
                        } // Not an end element
                        else
                        {
                            if (reader.Name == "Run")
                            {
                                readRunStatusNode = false;
                            }
                            if (reader.Name == "Dur")
                            {
                                readDurStatusNode = false;
                            }
                            if (reader.Name == "Eat")
                            {
                                readConsumeStatusNode = false;
                            }
                            if (reader.Name == "AEff")
                            {
                                readEffectNode = false;

                                if (abilEffect.effectRefName == "")
                                {
                                    Debug.Log(abilEffect.effectName + " of type " + abilEffect.effectType + " has no refname ");
                                }

                                se.AddEffectScript(abilEffect);
                                abilEffect.parentAbility = se;
                                //Debug.Log("Adding " + abilEffect.effectName + " to " + se.abilityName + " effect type " + abilEffect.effectType.ToString());
                                abilEffect = new EffectScript();
                            }
                            reader.Read();
                        }

                        bool isStatusEnd = false;

                        if (reader.Name == "SE") // was "StatusEffect"
                        {
                            isStatusEnd = true;
                        }

                        if (reader.NodeType == XmlNodeType.EndElement && isStatusEnd)
                        {
                            endOfStatus = true;
                            //Debug.Log("Finished with status " + se.abilityName + " has children effects " + se.effects.Count);
                            if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                            {
                                yield return null;
                                timeAtLastYield = Time.realtimeSinceStartup;
                            }
                        }
                    } // End of item while loop 

                } // End of read item  
            } // End of document
        }
    }
    BakedStatusEffectDefinitions.AddAllBakedStatusDefinitions();
    yield return null;
}

public bool LoadAbilitySwitch(XmlReader reader, AbilityScript abil)
{
    bool readSomething = false;
    string strValue = reader.Name;
    string txt;
     
        switch (strValue)
    {
            case "StDsc":
                string concatName = "abil_" + abil.refName + "_desc";
                abil.description = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                readSomething = true;
                reader.Read();
                break;
            case "StSdsc":
                concatName = "abil_" + abil.refName + "_shortdesc";
                abil.shortDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                readSomething = true;
                reader.Read();
                break;
            case "StEdsc":
                concatName = "abil_" + abil.refName + "_extradesc";
                abil.extraDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                if (abil.extraDescription.Contains("extradesc"))
                {
                    concatName = "abil_" + abil.refName + "_edesc";
                    abil.extraDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                }
                readSomething = true;
                reader.Read();
                break;
            case "StAlld":
                concatName = "abil_" + abil.refName + "_desc";
                abil.description = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                concatName = "abil_" + abil.refName + "_shortdesc";
                abil.shortDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                concatName = "abil_" + abil.refName + "_extradesc";
                abil.extraDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(concatName);
                readSomething = true;
                reader.Read();
                break;
        case "Psv":
            // This node is formatted: EffectRefName, StatusName.
            // It indicates the ability is passive, and has an effect that adds permanent passive status
            abil.passiveAbility = true;

            AddStatusEffect newPassiveEffect = new AddStatusEffect();
            newPassiveEffect.effectType = EffectType.ADDSTATUS;

            string unparsed = reader.ReadElementContentAsString();
            string[] parsed = unparsed.Split(',');
            newPassiveEffect.effectRefName = parsed[0];
            newPassiveEffect.statusRef = parsed[1];
            newPassiveEffect.tActorType = TargetActorType.ORIGINATING;
            newPassiveEffect.parentAbility = abil;
            abil.AddEffectScript(newPassiveEffect);
            masterEffectList.Add(newPassiveEffect.effectRefName, newPassiveEffect);
            readSomething = true;
            break;
        case "EffRef":
        case "AbilEffectRef":
            abil.load_effectRefsToConnect.Add(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "DName":
        case "DisplayName": // deprecate
                abil.abilityName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
            abil.BuildSortableName(); // for non-english sort purposes
            readSomething = true;
            break;
        case "ATags":
        case "AbilityTags": // deprecate
            unparsed = reader.ReadElementContentAsString();
            parsed = unparsed.Split(',');
            for (int i = 0; i < parsed.Length; i++)
            {
                    //AbilityTags aTag = (AbilityTags)Enum.Parse(typeof(AbilityTags), parsed[i]);
                    AbilityTags aTag = CustomAlgorithms.dictStrToAbilityTagEnum[parsed[i]];
                abil.AddAbilityTag(aTag);
            }
            abil.abilityTagsRead = true;
            readSomething = true;
            break;
        case "RemoveATags":
        case "RemoveAbilityTags":
            string unparsedTags = reader.ReadElementContentAsString();
            string[] parsedTags = unparsedTags.Split(',');
            for (int i = 0; i < parsedTags.Length; i++)
            {
                    //AbilityTags aTag = (AbilityTags)Enum.Parse(typeof(AbilityTags), parsedTags[i]);
                    AbilityTags aTag = CustomAlgorithms.dictStrToAbilityTagEnum[parsedTags[i]];
                abil.RemoveAbilityTag(aTag);
            }
            readSomething = true;
            break;
        case "ReqWeaponType":
            abil.reqWeaponType = (WeaponTypes)Enum.Parse(typeof(WeaponTypes), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "RefName":
            abil.refName = reader.ReadElementContentAsString();
            readSomething = true;
                break;
        case "ABILITYFLAG":
            AbilityFlags flagToRead = (AbilityFlags)Enum.Parse(typeof(AbilityFlags), reader.ReadElementContentAsString().ToUpperInvariant());
            abil.abilityFlags[(int)flagToRead] = true;
            break;
        case "Passive":
            abil.passiveAbility = true;
            reader.Read();
            readSomething = true;
            break;
        case "SpellShift":
        case "Spellshift":
            abil.spellshift = true;
            reader.Read();
            readSomething = true;
            break;
        case "BudokaMod":
            abil.budokaMod = true;
            reader.Read();
            readSomething = true;
            break;
        case "CombatOnly":
            abil.combatOnly = true;
            reader.Read();
            readSomething = true;
            break;
        case "ExclusionGroup":
            abil.exclusionGroup = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "Repetitions":
            abil.repetitions = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "UsePassiveSlot":
            abil.usePassiveSlot = true;
            reader.Read();
            readSomething = true;
            break;
        case "Script_OnLearn":
            abil.script_onLearn = reader.ReadElementContentAsString();
            readSomething = true;
            break;
        case "Script_OnPreAbilityUse":
            abil.script_onPreAbilityUse = reader.ReadElementContentAsString();
            readSomething = true;
            break;
        case "SFXOverride":
            abil.sfxOverride = reader.ReadElementContentAsString();

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {abil.sfxOverride, "SpriteEffects/" + abil.sfxOverride});
                }
            else
                {
                    TryPreloadResourceNoBundles(abil.sfxOverride, "SpriteEffects/" + abil.sfxOverride);
                }

            readSomething = true;
            break;
        case "Desc":
        case "Description": // deprecate
                abil.description = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "NTag":
        case "NumberTag": // deprecate
            abil.numberTags.Add(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "SDesc":
        case "ShortDescription": //deprecate
                abil.shortDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "EDesc":
        case "ExtraDescription": // deprecate
                abil.extraDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "Log":  
        case "CombatLogText":
            if (!titleScreenGMS)
            {
                    abil.combatLogText = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                abil.combatLogText = CustomAlgorithms.ParseRichText(abil.combatLogText, false);
            }
            else
            {
                reader.Read();
            }
            readSomething = true;
            break;
        case "ChargeText":
            if (!titleScreenGMS)
            {
                    abil.chargeText = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                abil.chargeText = CustomAlgorithms.ParseRichText(abil.chargeText, false);
            }
            else
            {
                reader.Read();
            }
            readSomething = true;
            break;
            case "StM":
        case "StaminaCost":
            abil.staminaCost = reader.ReadElementContentAsInt();
            break;
        case "TeachPlayerAbility":
            abil.teachPlayerAbility = reader.ReadElementContentAsString();
            break;
        case "SpiritsRequired":
            abil.spiritsRequired = reader.ReadElementContentAsInt();
            break;
        case "ChargeTime":
        case "Chargetime":
            abil.chargeTime = reader.ReadElementContentAsInt();
            break;
        case "Orientation":
            abil.lineDir = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "LandingTile":
            abil.landingTile = (LandingTileTypes)Enum.Parse(typeof(LandingTileTypes), reader.ReadElementContentAsString());
            readSomething = true;
            break;
            case "EnC":
        case "EnergyCost":
            abil.energyCost = reader.ReadElementContentAsInt();
            break;
        case "EnergyReserve":
            abil.energyReserve = reader.ReadElementContentAsInt();
            break;
        case "StaminaReserve":
            abil.staminaReserve = reader.ReadElementContentAsInt();
            break;
        case "HealthCost":
            abil.healthCost = reader.ReadElementContentAsInt();
            break;
        case "PercentMaxHealthCost":
            txt = reader.ReadElementContentAsString();
            abil.percentMaxHealthCost = CustomAlgorithms.TryParseFloat(txt);
            break;
        case "PercentCurHealthCost":
            txt = reader.ReadElementContentAsString();
            abil.percentCurHealthCost = CustomAlgorithms.TryParseFloat(txt);
            break;
        case "PassTurns":
            abil.passTurns = reader.ReadElementContentAsInt();
            break;
        case "ChargeTurns":
            abil.chargeTurns = reader.ReadElementContentAsInt();
            break;
        case "Power":
            txt = reader.ReadElementContentAsString();
            abil.power = CustomAlgorithms.TryParseFloat(txt);
            readSomething = true;
            break;
        case "Variance":
            txt = reader.ReadElementContentAsString();
            abil.variance = CustomAlgorithms.TryParseFloat(txt);
            readSomething = true;
            break;
        case "Icon":
        case "IconSprite": // deprecate
            abil.iconSprite = reader.ReadElementContentAsString();
            readSomething = true;
            break;
        case "InstantDirectionalAnimationRef":
            abil.instantDirectionalAnimationRef = reader.ReadElementContentAsString();

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {abil.instantDirectionalAnimationRef, "SpriteEffects/" + abil.instantDirectionalAnimationRef});
                }
            else
                {
                    TryPreloadResourceNoBundles(abil.instantDirectionalAnimationRef, "SpriteEffects/" + abil.instantDirectionalAnimationRef);
                }

            readSomething = true;
            break;
        case "StackProjectileFirstTile":
            abil.stackProjectileFirstTile = reader.ReadElementContentAsString();

            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {abil.stackProjectileFirstTile, "SpriteEffects/" + abil.stackProjectileFirstTile});
                }
            else
                {
                    TryPreloadResourceNoBundles(abil.stackProjectileFirstTile, "SpriteEffects/" + abil.stackProjectileFirstTile);
                }

            readSomething = true;
            break;

        case "REQUIRETARGET":
        case "RequireTarget":
            abil.AddRequiredTarget(reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "Direction":
            abil.direction = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "Range":
            abil.range = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "DisplayInList":
            abil.displayInList = simpleBool[reader.ReadElementContentAsInt()];
            readSomething = true;
            break;
        case "UseAnimation":
            abil.useAnimation = reader.ReadElementContentAsString();
            readSomething = true;
            break;
        case "SpritePopInfo":
            abil.spritePopInfo = reader.ReadElementContentAsString();
            readSomething = true;
            break;
        case "NumMultiTargets":
            abil.numMultiTargets = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "TRange":
        case "TargetRange": // depreccate
            abil.targetRange = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "RandomChance":
            txt = reader.ReadElementContentAsString();
            abil.randomChance = CustomAlgorithms.TryParseFloat(txt);
            readSomething = true;
            break;
        case "TargetOffsetX":
            abil.targetOffsetX = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "TargetOffsetY":
            abil.targetOffsetY = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "CD":
        case "CooldownTurns":
            abil.maxCooldownTurns = reader.ReadElementContentAsInt();
            readSomething = true;
                break;
        case "Keycode":
            abil.binding = (KeyCode)Enum.Parse(typeof(KeyCode), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "ATarg":
        case "AbilityTarget": // deprecate
            abil.targetForMonster = (AbilityTarget)Enum.Parse(typeof(AbilityTarget), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "TShape":
        case "TargetShape": // deprecate
            abil.targetShape = (TargetShapes)Enum.Parse(typeof(TargetShapes), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "Shape":
        case "BoundsShape": // deprecate
        case "Bounds":
            abil.boundsShape = (TargetShapes)Enum.Parse(typeof(TargetShapes), reader.ReadElementContentAsString());
            readSomething = true;
            break;
        case "TargetChangeCondition":
            abil.targetChangeCondition = reader.ReadElementContentAsInt();
            readSomething = true;
            break;
        case "EffectConditional":
            if (!abil.hasConditions)
            {
                abil.conditions = new List<EffectConditional>();
                abil.hasConditions = true;
            }
            reader.ReadStartElement();
            EffectConditional ec = new EffectConditional();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.Name)
                {
                    case "Index":
                        ec.index = reader.ReadElementContentAsInt();
                        break;
                    case "Conditional":
                        ec.ec = (EffectConditionalEnums)Enum.Parse(typeof(EffectConditionalEnums), reader.ReadElementContentAsString());
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
            abil.conditions.Add(ec);
            reader.ReadEndElement();
            break;
        case "ClearEffects":
            if (simpleBool[reader.ReadElementContentAsInt()])
            {
                abil.listEffectScripts.Clear();
            }
            abil.clearEffectsForSubAbilities = true;
            readSomething = true;
            break;

        //SHEP: Scripts based on events
        case "Script_AttackBlock":
            {
                abil.script_AttackBlock = reader.ReadElementContentAsString();
                readSomething = true;
            }
            break;
        case "Script_SpecialTargeting":
            {
                abil.script_SpecialTargeting = reader.ReadElementContentAsString();
                readSomething = true;
            }
            break;
    }

    return readSomething;
    }

    IEnumerator LoadAllAbilities()
    {
        if (Weapon.weaponTypesVerbose == null)
        {
            Weapon.weaponTypesVerbose = new string[(int)WeaponTypes.COUNT];
            Weapon.weaponTypesVerbose[(int)WeaponTypes.SWORD] = StringManager.GetString("weapontype_sword");

            if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION2))
            {
                Weapon.weaponTypesVerbose[(int)WeaponTypes.WHIP] = StringManager.GetString("weapontype_whip");
            }


            Weapon.weaponTypesVerbose[(int)WeaponTypes.CLAW] = StringManager.GetString("weapontype_claw");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.AXE] = StringManager.GetString("weapontype_axe");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.DAGGER] = StringManager.GetString("weapontype_dagger");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.BOW] = StringManager.GetString("weapontype_bow");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.SPEAR] = StringManager.GetString("weapontype_spear");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.SPECIAL] = StringManager.GetString("weapontype_special");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.MACE] = StringManager.GetString("weapontype_mace");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.SLING] = StringManager.GetString("weapontype_sling");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.NATURAL] = StringManager.GetString("weapontype_natural");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.STAFF] = StringManager.GetString("weapontype_staff");
            Weapon.weaponTypesVerbose[(int)WeaponTypes.ANY] = StringManager.GetString("weapontype_any");

            Armor.armorTypesVerbose = new string[(int)ArmorTypes.COUNT];
            Armor.armorTypesVerbose[(int)ArmorTypes.LIGHT] = StringManager.GetString("eq_armortype_light");
            Armor.armorTypesVerbose[(int)ArmorTypes.MEDIUM] = StringManager.GetString("eq_armortype_medium");
            Armor.armorTypesVerbose[(int)ArmorTypes.HEAVY] = StringManager.GetString("eq_armortype_heavy");
        }


        allAbilitiesLoaded = false;
        int indexInXMLList = 0;

        bool doLoading = true; ;

        // This was switch only, but probably don't need to load the abilities twice ever.

        if (masterAbilityList != null && masterAbilityList.Any())
        {
            doLoading = false;
        }

        if (masterAbilityList == null)
        {
            masterAbilityList = new Dictionary<string, AbilityScript>();
            masterEffectList = new Dictionary<string, EffectScript>();
            masterSharaPowerList = new Dictionary<string, AbilityScript>();
            masterUniqueSharaPowerList = new Dictionary<string, AbilityScript>();
        }

        if (doLoading)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;

            List<string> abilityFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.ABILITIES);

            foreach (string fileText in abilityFilesToLoad)
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(fileText), settings))
                {
                    reader.Read();
                    float timeAtLastYield = Time.realtimeSinceStartup;
                    while (reader.Read())
                    {
                        //Debug.Log(reader.Name + " " + reader.NodeType);
                        bool isAbilityStart = false;

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "EffectScript")
                        {
                            EffectScript newEff = new EffectScript();
                            reader.ReadStartElement();
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                if (reader.NodeType == XmlNodeType.Whitespace)
                                {
                                    reader.Read();
                                    continue;
                                }
                                newEff = ReadEffectScriptFromXML(reader.Name, reader, newEff);
                                if (Time.realtimeSinceStartup - timeAtLastYield >= GameMasterScript.MIN_FPS_DURING_LOAD)
                                {
                                    yield return null;
                                    timeAtLastYield = Time.realtimeSinceStartup;                                    
                                }
                            }
                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Abil") // was "Ability"
                        {
                            isAbilityStart = true;
                        }

                        if (reader.NodeType == XmlNodeType.Element && isAbilityStart)
                        {

                            AbilityScript abil = new AbilityScript();
                            AbilityScript origAbil = abil;


                            bool endOfAbility = false;
                            bool readEffectNode = false;
                            EffectScript abilEffect = new EffectScript();
                            AbilityScript subAbility = new AbilityScript();
                            // Found a new item in the file.

                            float startTime = Time.realtimeSinceStartup;

                            while (!endOfAbility)
                            {
                                // reader.Read();        
                                string strValue = reader.Name;

                                if (reader.NodeType != XmlNodeType.EndElement)
                                {
                                    bool readSomething = false;

                                    if (readEffectNode) // BEGIN COPY CODE FROM ABILITY LOADER
                                    {
                                        abilEffect = ReadEffectScriptFromXML(strValue, reader, abilEffect);
                                    } // END COPY CODE FROM ABILITY LOADER                   

                                    readSomething = LoadAbilitySwitch(reader, abil);

                                    switch (strValue)
                                    {
                                        case "AEff":
                                        case "AbilEffect":
                                            if (reader.NodeType == XmlNodeType.Element)
                                            {
                                                abilEffect = new EffectScript();
                                                readEffectNode = true;
                                                reader.Read();
                                            }
                                            break;
                                        case "ChangeLogic":
                                            if (reader.NodeType == XmlNodeType.Element)
                                            {
                                                subAbility = new AbilityScript();
                                                AbilityScript.CopyFromTemplate(subAbility, abil);
                                                abil.subAbilities.Add(subAbility);
                                                abil = subAbility;
                                                reader.Read();
                                            }
                                            readSomething = true;
                                            break;
                                    }

                                    if (!readSomething)
                                    {
                                        reader.Read();
                                    }
                                } // Not an end element
                                else
                                {
                                    if (strValue == "ChangeLogic")
                                    {
                                        abil = origAbil;
                                    }
                                    if (strValue == "AEff")
                                    {
                                        readEffectNode = false;

                                        if (abilEffect.effectRefName == "")
                                        {
                                            if (Debug.isDebugBuild) Debug.Log(abilEffect.effectName + " of type " + abilEffect.effectType + " has no refname");
                                        }

                                        abil.AddEffectScript(abilEffect);
                                        abilEffect.parentAbility = abil;
                                        abilEffect = new EffectScript();
                                    }
                                    reader.Read();
                                }

                                bool isAbilityEnd = false;

                                if (reader.Name == "Abil") // was "Ability"
                                {
                                    isAbilityEnd = true;
                                }

                                if (reader.NodeType == XmlNodeType.EndElement && isAbilityEnd)
                                {
                                    endOfAbility = true;


                                    if (masterAbilityList.ContainsKey(abil.refName))
                                    {
                                        if (Debug.isDebugBuild) Debug.Log("WARNING! Ability dict already contains " + abil.refName);
                                    }
                                    else
                                    {
                                        masterAbilityList.Add(abil.refName, abil);
                                        if (abil.CheckAbilityTag(AbilityTags.SHARAPOWER))
                                        {
                                            masterUniqueSharaPowerList.Add(abil.refName, abil);
                                        }
                                    }


                                    if (abil.subAbilities != null)
                                    {
                                        foreach (AbilityScript sub in abil.subAbilities)
                                        {
                                            sub.subAbilities = abil.subAbilities;
                                        }
                                    }

                                    //Debug.Log("Time elapsed after read ability: " + abil.refName + ": " + (Time.realtimeSinceStartup - startTime));

                                    if (Time.realtimeSinceStartup - timeAtLastYield >= GameMasterScript.MIN_FPS_DURING_LOAD)
                                    {
                                        yield return null;
                                        timeAtLastYield = Time.realtimeSinceStartup;
                                    }

                                }
                            } // End of item while loop 

                            // Here we have finished reading an ability.

                        } // End of read item  


                    } // End of document

                    // We've finished now.\                    
                }
            }
        }

        allAbilitiesLoaded = true;
    }

    private IEnumerator LoadAllDialogs()
    {
        masterConversationList = new Dictionary<string, Conversation>();
        masterJournalEntryList = new List<Conversation>();
        masterTutorialList = new List<Conversation>();
        masterMonsterQuipList = new List<MonsterQuip>();

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        //settings.IgnoreWhitespace = true;

        //update: read every dialog in the folder, get 'em all
        //dialogXML = Resources.LoadAll<TextAsset>("Dialogs/XML/");
        List<string> dialogsToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.DIALOGS);

        foreach (string dialogFileText in dialogsToLoad)
        {
            XmlReader reader = XmlReader.Create(new StringReader(dialogFileText), settings);

            using (reader)
            {
                reader.Read();
                Conversation convo = null;
                bool readConversation = false;
                //Debug.Log(reader.Name + " nt " + reader.NodeType);

                float timeAtLastYield = Time.realtimeSinceStartup;

                while (reader.Read())
                {

                    #region Read MonsterCorral dialogs
                    if (reader.Name == "MonsterCorral")
                    {
                        reader.ReadStartElement();

                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            switch (reader.Name)
                            {
                                case "Quip":
                                    MonsterQuip mq = new MonsterQuip();
                                    reader.ReadStartElement();
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "NumMonsters":
                                                mq.numMonsters = reader.ReadElementContentAsInt();
                                                break;
                                            case "Text":
                                                mq.text = reader.ReadElementContentAsString();
                                                break;
                                            default:
                                                reader.Read();
                                                break;
                                        }
                                    }
                                    reader.ReadEndElement();
                                    masterMonsterQuipList.Add(mq);
                                    break;
                                default:
                                    reader.Read();
                                    break;
                            }
                        }

                        reader.ReadEndElement();
                    }
                    #endregion

                    if (reader.Name == "Dialog" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        readConversation = true;
                        convo = new Conversation();
                        reader.ReadStartElement();
                    }
                    else if (reader.Name == "Dialog" && reader.NodeType == XmlNodeType.EndElement)
                    {
                        readConversation = false;
                        reader.ReadEndElement();
                        if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                        {
                            yield return null;
                            timeAtLastYield = Time.realtimeSinceStartup;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement || reader.NodeType == XmlNodeType.Document)
                    {
                        //Debug.Log(reader.Name + " " + reader.NodeType);
                        break;
                    }

                    if (readConversation)
                    {
                        string txt;

                        switch (reader.Name)
                        {
                            // Read the convo info                        
                            case "RefName":
                                convo.refName = reader.ReadElementContentAsString();
                                if (convo.refName.Contains("tutorial_"))
                                {
                                    masterTutorialList.Add(convo);
                                }
                                if (masterConversationList.ContainsKey(convo.refName))
                                {
                                    if (Debug.isDebugBuild) Debug.LogError("Dialog already contains " + convo.refName);
                                }
                                masterConversationList.Add(convo.refName, convo);
                                break;
                            case "IngameDialogue":
                                convo.ingameDialogue = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "StartScript":
                                convo.runScriptOnConversationStart = reader.ReadElementContentAsString();
                                break;
                            case "SpriteFontUsed":
                                convo.spriteFontUsed = reader.ReadElementContentAsString();
                                break;
                            case "TextInputField":
                                convo.textInputField = reader.ReadElementContentAsString();
                                break;
                            case "ForceTypewriter":
                                convo.forceTypewriter = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "HasLiveMergeTags":
                                convo.hasLiveMergeTags = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "StopAnim":
                            case "StopAnimationAndUnlockInput":
                                convo.stopAnimationAndUnlockInput = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "WriteInCombatLog":
                                convo.writeInCombatLog = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "OverrideSize":
                                convo.overrideSize = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "OverridePosition":
                                convo.overridePos = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "KeyStory":
                                convo.keyStory = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "JournalEntry":
                                convo.journalEntry = reader.ReadElementContentAsInt();
                                if (convo.journalEntry > 0)
                                {
                                    masterJournalEntryList.Add(convo);
                                }
                                break;
                            case "ReqEntry":
                                convo.reqEntries.Add(reader.ReadElementContentAsInt());
                                break;

                            case "ChallengeValue":
                                txt = reader.ReadElementContentAsString();
                                convo.challengeValue = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "SpriteFont":
                                convo.spriteFontUsed = reader.ReadElementContentAsString();
                                break;
                            case "ExtraWaitTime":
                                txt = reader.ReadElementContentAsString();
                                convo.extraWaitTime = CustomAlgorithms.TryParseFloat(txt);
                                break;
                            case "Centered":
                                convo.centered = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "FadeTime":
                                string fTime = reader.ReadElementContentAsString();
                                float time = CustomAlgorithms.TryParseFloat(fTime);
                                convo.fadeTime = time;
                                break;
                            case "WindowSizeX":
                                convo.windowSize.x = reader.ReadElementContentAsInt();
                                break;
                            case "WindowSizeY":
                                convo.windowSize.y = reader.ReadElementContentAsInt();
                                break;
                            case "WindowPosX":
                                convo.windowPos.x = reader.ReadElementContentAsInt();
                                break;
                            case "WindowPosY":
                                convo.windowPos.y = reader.ReadElementContentAsInt();
                                break;
                            case "TextBranch":
                                reader.ReadStartElement();
                                TextBranch tb = new TextBranch();
                                convo.allBranches.Add(tb);
                                while (reader.NodeType != XmlNodeType.EndElement)
                                {
                                    switch (reader.Name)
                                    {
                                        case "ShowFace":
                                            tb.strFaceSprite = reader.ReadElementContentAsString();
                                            break;
                                        //Some portraits will have special anim timing
                                        case "FaceAnimTiming":
                                            var splitTime = reader.ReadElementContentAsString().Split('|');
                                            var lemf = splitTime.Length;
                                            tb.optionalAnimTiming = new float[splitTime.Length];
                                            for (int t = 0; t < lemf; t++)
                                            {
                                                tb.optionalAnimTiming[t] = CustomAlgorithms.TryParseFloat(splitTime[t]);
                                            }

                                            break;

                                        case "AudioCommands":
                                            tb.strAudioCommands = reader.ReadElementContentAsString();
                                            break;
                                        case "ScriptBranchOpen":
                                            tb.strScriptOnBranchOpen = reader.ReadElementContentAsString();
                                            break;
                                        case "ScriptConvoEnd":
                                            tb.strSetScriptOnConvoEnd = reader.ReadElementContentAsString();
                                            break;
                                        case "BranchName":
                                            tb.branchRefName = reader.ReadElementContentAsString();
                                            break;
                                         case "DisplaySprite":
                                            tb.strSpriteToDisplay = reader.ReadElementContentAsString();
                                            break;
                                        case "DisplayPrefab":
                                            string[] s = reader.ReadElementContentAsString().Split(',');
                                            tb.strPrefabToDisplayInFrontOfDialog = s[0];
                                            tb.vPrefabToDisplayOffset = new Vector2(float.Parse(s[1]), float.Parse(s[2])); 
                                            break; 
                                        case "AlternateBranch":
                                            AlternateBranch ab = new AlternateBranch();
                                            reader.ReadStartElement();
                                            tb.altBranches.Add(ab);
                                            while (reader.NodeType != XmlNodeType.EndElement)
                                            {
                                                switch (reader.Name.ToLowerInvariant())
                                                {
                                                    case "altbranchref":
                                                        ab.altBranchRef = reader.ReadElementContentAsString();
                                                        break;
                                                    case "reqitem":
                                                        ab.reqItemInInventory = reader.ReadElementContentAsString();
                                                        break;
                                                    case "usereqitem":
                                                        ab.useReqItem = simpleBool[reader.ReadElementContentAsInt()];
                                                        break;
                                                    case "branchreqflag":
                                                        ab.branchReqFlag = reader.ReadElementContentAsString();
                                                        break;
                                                    case "branchreqflagmeta":
                                                        ab.branchReqFlagMeta = simpleBool[reader.ReadElementContentAsInt()];
                                                        break;
                                                    case "branchreqflagvalue":
                                                        ab.branchReqFlagValue = reader.ReadElementContentAsInt();
                                                        break;
                                                    default:
                                                        reader.Read();
                                                        break;
                                                }
                                            }
                                            reader.ReadEndElement();
                                            break;
                                        case "GrantRecipe":
                                            string rec = reader.ReadElementContentAsString();
                                            if (rec != null)
                                            {
                                                tb.grantRecipe.Add(rec);
                                            }
                                            else
                                            {
                                                Debug.Log("Nullref of TB recipe " + tb.branchRefName);
                                            }

                                            break;
                                        case "AddPlayerFlag":
                                            reader.ReadStartElement();
                                            tb.addFlag = new AddPlayerFlag();
                                            while (reader.NodeType != XmlNodeType.EndElement)
                                            {
                                                switch (reader.Name.ToLowerInvariant())
                                                {
                                                    case "flagname":
                                                        tb.addFlag.flagRef = reader.ReadElementContentAsString();
                                                        break;
                                                    case "flagvalue":
                                                        tb.addFlag.flagValue = reader.ReadElementContentAsInt();
                                                        break;
                                                    case "meta":
                                                        tb.addFlag.meta = reader.ReadElementContentAsBoolean();
                                                        break;
                                                    default:
                                                        reader.Read();
                                                        break;
                                                }
                                            }
                                            reader.ReadEndElement();
                                            break;
                                        case "Text":
                                            string textRef = reader.ReadElementContentAsString();
                                            if (LogoSceneScript.globalIsSolsticeBuild)
                                            {
                                                if (textRef == "dialog_corral_namemonster_main_txt")
                                                {
                                                    textRef = "dialog_corral_namemonster_random_txt";
                                                }
                                            }
                                            txt = StringManager.GetLocalizedStringOrFallbackToEnglish(textRef);
                                            txt = System.Text.RegularExpressions.Regex.Unescape(txt); // New 3/28 to remove "\n" characters not parsing at random
                                            txt = CustomAlgorithms.ParseRichText(txt, true);
                                            tb.text = txt;
                                            // displayName = Regex.Replace(localDisplay, "&lt;", "<");
                                            break;
                                        case "GrantItem":
                                            string iRef = reader.ReadElementContentAsString();
                                            tb.grantItemRef = iRef;
                                            break;
                                        case "KeyStory":
                                            tb.enableKeyStoryState = simpleBool[reader.ReadElementContentAsInt()];
                                            break;
                                        case "Script_TextBranchStart":
                                            tb.script_textBranchStart = reader.ReadElementContentAsString();
                                            break;
                                        case "Script_TextBranchStartValue":
                                            tb.script_textBranchStartValue = reader.ReadElementContentAsString();
                                            break;
                                        case "Button":
                                            reader.ReadStartElement();
                                            ButtonCombo bc = new ButtonCombo();

                                            while (reader.NodeType != XmlNodeType.EndElement)
                                            {
                                                switch (reader.Name)
                                                {
                                                    case "reqflag":
                                                    case "reqmetaflag":
                                                        {
                                                            string storedName = reader.Name;
                                                            string strFlagData = reader.ReadElementContentAsString();
                                                            string[] strSplit = strFlagData.Split(',');

                                                            DialogButtonResponseFlag newFlag = new DialogButtonResponseFlag();
                                                            newFlag.flagName = strSplit[0];
                                                            newFlag.flagMinValue = Int32.Parse(strSplit[1]);
                                                            newFlag.isMetaDataFlag = (storedName == "reqmetaflag");
                                                            bc.reqFlags.Add(newFlag);
                                                        }
                                                        break;

                                                    case "RequiredPlayerFlag":
                                                        reader.ReadStartElement();

                                                        DialogButtonResponseFlag dbFlag = new DialogButtonResponseFlag();

                                                        while (reader.NodeType != XmlNodeType.EndElement)
                                                        {
                                                            switch (reader.Name.ToLowerInvariant())
                                                            {
                                                                case "flag":
                                                                    dbFlag.flagName = reader.ReadElementContentAsString();
                                                                    break;
                                                                case "minvalue":
                                                                    dbFlag.flagMinValue = reader.ReadElementContentAsInt();
                                                                    break;
                                                                case "maxvalue":
                                                                    dbFlag.flagMaxValue = reader.ReadElementContentAsInt();
                                                                    break;
                                                                case "meta":
                                                                    dbFlag.isMetaDataFlag = simpleBool[reader.ReadElementContentAsInt()];
                                                                    break;
                                                                default:
                                                                    reader.Read();
                                                                    break;
                                                            }
                                                        }
                                                        bc.reqFlags.Add(dbFlag);
                                                        reader.ReadEndElement();
                                                        break;
                                                    case "RequiredItem":
                                                        reader.ReadStartElement();

                                                        string reqItemRef = "";

                                                        while (reader.NodeType != XmlNodeType.EndElement)
                                                        {
                                                            switch (reader.Name.ToLowerInvariant())
                                                            {
                                                                case "iref":
                                                                case "itemref":
                                                                    reqItemRef = reader.ReadElementContentAsString();
                                                                    break;
                                                                case "qty":
                                                                case "quantity":
                                                                    int quantity = reader.ReadElementContentAsInt();
                                                                    bc.reqItems.Add(reqItemRef, quantity);
                                                                    break;
                                                                default:
                                                                    reader.Read();
                                                                    break;
                                                            }
                                                        }
                                                        reader.ReadEndElement();
                                                        break;
                                                    case "Name":
                                                        string btxt = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                                        btxt = CustomAlgorithms.ParseRichText(btxt, false);
                                                        bc.buttonText = btxt;
                                                        break;
                                                    case "BranchRef":
                                                        bc.actionRef = reader.ReadElementContentAsString();
                                                        break;
                                                    case "SpriteRef":
                                                        bc.spriteRef = reader.ReadElementContentAsString();
                                                        break;
                                                    case "ThreeColumn":
                                                        bc.threeColumnStyle = simpleBool[reader.ReadElementContentAsInt()];
                                                        break;
                                                    case "DialogEventScript":
                                                        bc.dialogEventScript = reader.ReadElementContentAsString();
                                                        break;
                                                    case "DialogEventScriptValue":
                                                        bc.dialogEventScriptValue = reader.ReadElementContentAsString();
                                                        break;
                                                    case "DialogEvent":
                                                        // Contains Script AND Value, comma-separated. Genius!
                                                        string unparsed = reader.ReadElementContentAsString();
                                                        string[] parsed = unparsed.Split(',');
                                                        bc.dialogEventScript = parsed[0];
                                                        bc.dialogEventScriptValue = parsed[1];
                                                        break;
                                                    default:
                                                        reader.Read();
                                                        break;
                                                }
                                            } // End button reader

                                            if (bc.actionRef.ToLowerInvariant() == "exit")
                                            {
                                                bc.dbr = DialogButtonResponse.EXIT;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "shop_buy")
                                            {
                                                bc.dbr = DialogButtonResponse.SHOPBUY;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "shop_sell")
                                            {
                                                bc.dbr = DialogButtonResponse.SHOPSELL;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "casino_slots")
                                            {
                                                bc.dbr = DialogButtonResponse.CASINOSLOTS;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "casino_blackjack")
                                            {
                                                bc.dbr = DialogButtonResponse.CASINOBLACKJACK;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "casino_ceelo")
                                            {
                                                bc.dbr = DialogButtonResponse.CASINOCEELO;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "newquest")
                                            {
                                                bc.dbr = DialogButtonResponse.NEWQUEST;
                                            }
                                            /* else if (bc.actionRef.ToLowerInvariant() == "healme")
                                            {
                                                bc.dbr = DialogButtonResponse.HEALME;
                                            } */
                                            else if (bc.actionRef.ToLowerInvariant() == "changejobs")
                                            {
                                                bc.dbr = DialogButtonResponse.CHANGEJOBS;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "blessxp")
                                            {
                                                bc.dbr = DialogButtonResponse.BLESSXP;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "blessjp")
                                            {
                                                bc.dbr = DialogButtonResponse.BLESSJP;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "blessattack")
                                            {
                                                bc.dbr = DialogButtonResponse.BLESSATTACK;
                                            }
                                            else if (bc.actionRef.ToLowerInvariant() == "blessdefense")
                                            {
                                                bc.dbr = DialogButtonResponse.BLESSDEFENSE;
                                            }
                                            else
                                            {
                                                bc.dbr = DialogButtonResponse.CONTINUE; // Eventually do something else, have multiple DBR options in the XML and loader.
                                            }
                                            tb.responses.Add(bc);
                                            reader.ReadEndElement();
                                            break;
                                        default:
                                            reader.Read();
                                            break;
                                    }
                                }
                                reader.ReadEndElement();
                                break;
                        }
                    }
                }
            }
        }

        
    }

    private IEnumerator LoadAllLootTables()
    {
        masterLootTables = new Dictionary<string, ActorTable>();

        List<string> lootTableStringsToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.LOOTTABLES);
        PlayerModManager.AddModFilesToList(lootTableStringsToLoad, PlayerModfileTypes.LOOTTABLES);

        foreach (string lootFile in lootTableStringsToLoad)
        {
            if (string.IsNullOrEmpty(lootFile)) continue;

            using (XmlReader reader = XmlReader.Create(new StringReader(lootFile)))
            {
                reader.Read();
                while (reader.Read())
                {
                    if (reader.Name == "LootTable" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        ActorTable lootTable = new ActorTable();

                        bool success = true;
                        bool mergedWithExisting = false;
                        try { lootTable.ReadFromXML(reader, ActorTypes.ITEM, masterLootTables, out mergedWithExisting); }
                        catch (Exception e)
                        {
                            Debug.Log("Couldn't load loot table. " + e);
                            success = false;
                        }
                        if (success && !mergedWithExisting)
                        {
                            bool doAdd = true;
                            if (masterLootTables.ContainsKey(lootTable.refName))
                            {
                                if (lootTable.replaceRef)
                                {
                                    masterLootTables.Remove(lootTable.refName);
                                }
                                else
                                {
                                    if (Debug.isDebugBuild) Debug.Log("Not adding " + lootTable.refName + " to master loot table list, because key already exists.");
                                    doAdd = false;
                                }
                            }
                            if (doAdd)
                            {
                                masterLootTables.Add(lootTable.refName, lootTable);
                            }
                        }
                    }
                }
            }
        }

        tableOfLootTables = new ActorTable();
        tableOfLootTables.AddToTable("legendary", 15);
        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.NEWGAMEPLUS)
        {
            tableOfLootTables.AddToTable("legendary", 40);
            if (GameStartData.NewGamePlus == 2)
            {
                tableOfLootTables.AddToTable("legendary", 40);
            }
        }
        tableOfLootTables.AddToTable("seeds", 60);
        tableOfLootTables.AddToTable("gems", 400);
        tableOfLootTables.AddToTable("consumables", 1200);
        tableOfLootTables.AddToTable("weapons", 2000);
        tableOfLootTables.AddToTable("armor", 1650);
        tableOfLootTables.AddToTable("accessories", 1150);
        tableOfLootTables.AddToTable("food", 1350);

        if (seasonHalloweenEnabled)
        {
            ActorTable cTable = LootGeneratorScript.GetLootTable("consumables");
            ActorTable halloween1 = LootGeneratorScript.GetLootTable("halloween_consumables");
            foreach (string aRef in halloween1.table.Keys)
            {
                cTable.AddToTableIncludingItemActor(aRef, halloween1.table[aRef]);
            }
        }
        if (lunarNewYearEnabled)
        {
            ActorTable cTable = LootGeneratorScript.GetLootTable("consumables");
            ActorTable allItems = LootGeneratorScript.GetLootTable("allitems");
            ActorTable lunarNewYearConsumables = LootGeneratorScript.GetLootTable("lunarnewyear_consumables");
            foreach (string aRef in lunarNewYearConsumables.table.Keys)
            {
                cTable.AddToTableIncludingItemActor(aRef, lunarNewYearConsumables.table[aRef]);
                allItems.AddToTableIncludingItemActor(aRef, lunarNewYearConsumables.table[aRef]);

                //Debug.Log("Added " + aRef + " " + lunarNewYearConsumables.table[aRef] + " to table " + cTable.refName + " " + cTable.id);
            }

            ActorTable secondTable = LootGeneratorScript.GetLootTable("food");
            ActorTable lunarNewYearFood = LootGeneratorScript.GetLootTable("lunarnewyear_food");
            foreach (string aRef in lunarNewYearConsumables.table.Keys)
            {
                secondTable.AddToTableIncludingItemActor(aRef, lunarNewYearConsumables.table[aRef]);
            }            

            //Debug.LogError("Added LNY to loot tables!");
        }        

        yield break;
    }

    private IEnumerator LoadAllSpawnTables()
    {
        masterSpawnTableList = new Dictionary<string, ActorTable>();

        List<string> spawnTablesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.SPAWNTABLES);
        PlayerModManager.AddModFilesToList(spawnTablesToLoad, PlayerModfileTypes.SPAWNTABLES);

        foreach (string spawnStr in spawnTablesToLoad)
        {
            if (string.IsNullOrEmpty(spawnStr)) continue;

            using (XmlReader reader = XmlReader.Create(new StringReader(spawnStr)))
            {
                reader.Read();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.Name == "SpawnTable" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        ActorTable readingSpawnTable = new ActorTable();
                        bool success = true;
                        bool mergedWithExistingTable = false;

                        try { readingSpawnTable.ReadFromXML(reader, ActorTypes.MONSTER, masterSpawnTableList, out mergedWithExistingTable); }
                        catch (Exception e)
                        {
                            Debug.Log("Couldn't read monster spawn table file: " + e);
                            success = false;
                        }

                        if (success && !mergedWithExistingTable)
                        {
                            bool safeToAdd = false;
                            if (masterSpawnTableList.ContainsKey(readingSpawnTable.refName))
                            {
                                if (readingSpawnTable.replaceRef)
                                {
                                    masterSpawnTableList.Remove(readingSpawnTable.refName);
                                    safeToAdd = true;
                                }
                                else
                                {
                                    if (Debug.isDebugBuild) Debug.Log("Couldn't add spawn table " + readingSpawnTable.refName + " as key already exists.");
                                }
                            }
                            else
                            {
                                safeToAdd = true;
                            }
                            if (safeToAdd)
                            {
                                masterSpawnTableList.Add(readingSpawnTable.refName, readingSpawnTable);
                            }

                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
                reader.ReadEndElement();
            }
        }

        SeasonalFunctions.CheckForSeasonalAdjustmentsToSpawnTablesOnGameLoad();

        yield return null;
    }

    IEnumerator LoadAllShops()
    {
        masterShopTableList = new Dictionary<string, ActorTable>();
        masterShopList = new Dictionary<string, ShopScript>();
        List<string> allShopFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.SHOPS);
        PlayerModManager.AddModFilesToList(allShopFilesToLoad, PlayerModfileTypes.SHOPS);

        //if (Debug.isDebugBuild) Debug.Log("Loading all shops.");
        yield return LoadAllNPCOrShopDataLoop(allShopFilesToLoad);
    }

    IEnumerator LoadAllNPCs()
    {
        masterNPCList = new Dictionary<string, NPC>();
        masterCampfireNPCList = new List<NPC>();

        List<string> allNPCFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.NPCS);
        //if (Debug.isDebugBuild) Debug.Log("Loading all NPCs.");
        yield return LoadAllNPCOrShopDataLoop(allNPCFilesToLoad);
    }

    IEnumerator LoadAllNPCOrShopDataLoop(List<string> allNPCFilesToLoad)
    {
        float timeAtStart = Time.realtimeSinceStartup;

        foreach (string npcXMLText in allNPCFilesToLoad)
        {
            if (string.IsNullOrEmpty(npcXMLText)) continue;
            using (XmlReader reader = XmlReader.Create(new StringReader(npcXMLText)))
            {
                reader.Read();
                NPC makeNPC = null;
                bool readingNPC = false;
                while (reader.Read())
                {
                    if (reader.Name == "ShopTable" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        ActorTable shop = new ActorTable();

                        bool success = true;
                        bool mergedWithExisting = false;
                        try { shop.ReadFromXML(reader, ActorTypes.COUNT, masterShopTableList, out mergedWithExisting); } // Also reads start and end
                        catch (Exception e)
                        {
                            Debug.Log("Failed to read ShopTable data due to " + e);
                            success = false;
                        }
                        if (success && !mergedWithExisting)
                        {
                            if (masterShopTableList.ContainsKey(shop.refName))
                            {
                                if (shop.replaceRef)
                                {
                                    masterShopTableList.Remove(shop.refName);
                                    masterShopTableList.Add(shop.refName, shop);
                                    //Debug.Log("Replaced " + shop.refName + " with new data.");
                                }
                                else
                                {
#if UNITY_EDITOR
                                    //Debug.Log("Couldn't add shop table " + shop.refName + " to master dict because a key already exists.");
#endif
                                }
                            }
                            else
                            {
                                masterShopTableList.Add(shop.refName, shop);
                                //Debug.Log("Added shop to master shop table list: " + shop.refName);
                            }

                        }

                    }

                    //Debug.Log(reader.Name + " " + reader.NodeType);

                    // Now load the shops themselves.
                    if (reader.Name == "Shop" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        ShopScript npcShop = new ShopScript();

                        bool success = true;
                        try { npcShop.ReadFromXML(reader); }
                        catch (Exception e)
                        {
                            Debug.Log("Failed to read ShopScript due to error " + e);
                            success = false;
                        }
                        if (success && npcShop.ValidateAllShopDataOnLoad())
                        {
                            if (masterShopList.ContainsKey(npcShop.refName))
                            {
#if UNITY_EDITOR
                                //Debug.Log("Cannot add " + npcShop.refName + " to master shop list, as a shop of that name already exists.");
#endif
                            }
                            else
                            {
                                masterShopList.Add(npcShop.refName, npcShop);
                                //Debug.Log("Added npc shop to master list: " + npcShop.refName);
                            }
                        }

                    }

                    //Debug.Log(" 2 " + reader.Name + " " + reader.NodeType);

                    if (reader.Name == "NPC" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        readingNPC = true;
                        makeNPC = new NPC();
                        //masterNPCList.Add(makeNPC);
                        reader.ReadStartElement();
                    }
                    else if (reader.Name == "NPC" && reader.NodeType == XmlNodeType.EndElement)
                    {
                        //Debug.Log("Read NPC " + makeNPC.actorRefName + " " + makeNPC.displayName);
                        readingNPC = false;
                        reader.ReadEndElement();
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        break;
                    }

                    if (readingNPC)
                    {
                        switch (reader.Name)
                        {
                            case "DisplayName":
                                makeNPC.displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                break;
                            case "RefName":
                                makeNPC.actorRefName = reader.ReadElementContentAsString();
                                if (masterNPCList.ContainsKey(makeNPC.actorRefName))
                                {
                                    if (Debug.isDebugBuild) Debug.LogError("NPC dict already contains " + makeNPC.actorRefName);
                                }
                                else
                                {
                                    masterNPCList.Add(makeNPC.actorRefName, makeNPC);
                                }

                                break;
                            case "NoBumpToTalk":
                                makeNPC.noBumpToTalk = true; // indicates you must press confirm to talk to NPC, cannot just bump them
                                reader.ReadElementContentAsString();
                                break;
                            case "DialogRef":
                                makeNPC.dialogRef = reader.ReadElementContentAsString();
                                break;
                            case "ShopRef":
                                makeNPC.shopRef = reader.ReadElementContentAsString();
                                break;
                            case "Hvr":
                            case "HoverDisplay":
                                makeNPC.hoverDisplay = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "DisplayNewItemSprite":
                                makeNPC.displayNewItemSprite = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "Prefab":
                                makeNPC.prefab = reader.ReadElementContentAsString();

                                if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                {
                                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                        {makeNPC.prefab, "NPCs/" + makeNPC.prefab});
                                }
                                else
                                {
                                    TryPreloadResourceNoBundles(makeNPC.prefab, "NPCs/" + makeNPC.prefab);
                                }

                                break;
                            case "PCol":
                            case "PlayerCollidable":
                                makeNPC.playerCollidable = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "MCol":
                            case "MonsterCollidable":
                                makeNPC.monsterCollidable = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "Interactable":
                                makeNPC.interactable = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "CampfirePossible":
                                makeNPC.campfirePossible = simpleBool[reader.ReadElementContentAsInt()];
                                masterCampfireNPCList.Add(makeNPC);
                                break;
                            case "CookingPossible":
                                makeNPC.cookingPossible = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "DoNotRestockShop":
                                makeNPC.doNotRestockShop = simpleBool[reader.ReadElementContentAsInt()];
                                break;
                            case "StatusIcon":
                                makeNPC.statusIcon = reader.ReadElementContentAsString();

                                if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                {
                                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                        {makeNPC.statusIcon, "SpriteEffects/" + makeNPC.statusIcon});
                                }
                                else
                                {
                                    TryPreloadResourceNoBundles(makeNPC.statusIcon, "SpriteEffects/" + makeNPC.statusIcon);
                                }
                                break;
                            case "GivesQuests":
                                makeNPC.givesQuests = simpleBool[reader.ReadElementContentAsInt()];
                                if (makeNPC.givesQuests)
                                {
                                    makeNPC.questsRemaining = 3;
                                }
                                break;
                        }
                    }

                    if (Time.realtimeSinceStartup - timeAtStart >= MIN_FPS_DURING_LOAD)
                    {
                        yield return null;
                        timeAtStart = Time.realtimeSinceStartup;
                    }
                }
            }
        }
    }

    private IEnumerator LoadAllMonsters()
    {
        if (masterMonsterList != null && masterMonsterList.Count > 0)
        {
            yield break;
        }

        MonsterTemplateData.monsterAttributesAsString = new string[(int)MonsterAttributes.COUNT];
        for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
        {
            MonsterTemplateData.monsterAttributesAsString[i] = ((MonsterAttributes)i).ToString();
        }

        temp_actorsAddedToDictDuringLoad = new List<Actor>();

        masterMonsterList = new Dictionary<string, MonsterTemplateData>();
        monstersInPedia = new List<MonsterTemplateData>();
        sharaModeOnlyMonsters = new List<MonsterTemplateData>();
        monsterFamilyList = new List<string>();

        List<string> monsterFilesToLoad = new List<string>();
        for (int i = 0; i < monsterXML.Length; i++)
        {
            if (monsterXML[i] == null) continue;
            if (!DLCManager.IsFileValidToLoad(monsterXML[i].name, EDLCPackages.EXPANSION1)) continue;
            monsterFilesToLoad.Add(monsterXML[i].text);
        }

        temp_actorsAddedToDictDuringLoad.Clear();

        PlayerModManager.AddModFilesToList(monsterFilesToLoad, PlayerModfileTypes.MONSTERS);

        float timeAtLastFramePause = Time.realtimeSinceStartup;

        foreach (string monsterText in monsterFilesToLoad)
        {
            if (string.IsNullOrEmpty(monsterText)) continue;
            using (XmlReader reader = XmlReader.Create(new StringReader(monsterText)))
            {
                reader.Read();
                while (reader.NodeType != XmlNodeType.EndElement)
                {

                    if (Time.realtimeSinceStartup - timeAtLastFramePause >= MIN_FPS_DURING_LOAD) // don't dip below 30 fps
                    {
                        yield return null;
                        timeAtLastFramePause = Time.realtimeSinceStartup;
                        IncrementLoadingBar(0.006f);
                    }

                    string rName = reader.Name.ToLowerInvariant();
                    if (rName == "mon" || rName == "monster")
                    {
                        bool success = true;
                        MonsterTemplateData mon = new MonsterTemplateData();
                        try { mon.ReadFromXml(reader); }
                        catch (Exception e)
                        {
                            Debug.Log("Couldn't read monster due to " + e);
                            success = false;
                        }

                        if (!mon.ValidateMonsterTemplate())
                        {
                            if (Debug.isDebugBuild) Debug.LogError("WARNING: Could not validate monster template " + mon.refName);
                            success = false;
                        }

                        if (success)
                        {
                            bool safeToAdd = true;
                            if (masterMonsterList.ContainsKey(mon.refName))
                            {
                                if (mon.replaceRef)
                                {
                                    Debug.Log("Overwriting " + mon.refName + " with new version");
                                    masterMonsterList.Remove(mon.refName);
                                }
                                else
                                {
                                    Debug.Log("Warning! " + mon.refName + " exists in dict already.");
                                    safeToAdd = false;
                                }
                            }
                            if (safeToAdd)
                            {
                                masterMonsterList.Add(mon.refName, mon);
                            }
                            else
                            {
                                if (Debug.isDebugBuild) Debug.LogError("Couldn't read monster " + mon.refName);
                            }
                        }
                    } // End of read item  
                    else
                    {
                        reader.Read();
                    }
                } // End of document
            }
        }


        masterSpawnableMonsterList = new List<MonsterTemplateData>();

        foreach (MonsterTemplateData mtd in masterMonsterList.Values)
        {
            if (mtd.monFamily == null)
            {
                Debug.Log(mtd.refName + " has no family");
            }
            if (mtd.autoSpawn)
            {
                masterSpawnableMonsterList.Add(mtd);
            }
        }

    }

    private IEnumerator LoadAllMagicMods()
    {
        if (masterMagicModList != null &&
            masterMagicModList.Count > 0)
        {
            //if (Debug.isDebugBuild) Debug.Log("Magic mods already loaded");
            yield break;
        }

        masterMagicModList = new Dictionary<string, MagicMod>();
        dictMagicModIDs = new Dictionary<int, MagicMod>();
        listModsSortedByChallengeRating = new List<MagicMod>();
        dictMagicModsByFlag = new Dictionary<MagicModFlags, List<MagicMod>>();
        for (int i = 0; i < (int)MagicModFlags.COUNT; i++)
        {
            dictMagicModsByFlag.Add((MagicModFlags)i, new List<MagicMod>());
        }

        List<string> magicModFiles = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.MAGICMODS);

        foreach (string magicModFileText in magicModFiles)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(magicModFileText)))
            {
                reader.Read();

                MagicMod mmod = new MagicMod();

                float timeAtLastYield = Time.realtimeSinceStartup;

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    switch (reader.Name)
                    {
                        case "MAGICMOD":
                            mmod = new MagicMod();
                            string txt = "";
                            reader.ReadStartElement();

                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                switch (reader.Name)
                                {
                                    case "DisplayName":
                                    case "ModName":
                                        mmod.modName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                        break;
                                    case "RefName":
                                        mmod.refName = reader.ReadElementContentAsString();

                                        if (masterMagicModList.ContainsKey(mmod.refName))
                                        {
                                            Debug.Log("WARNING! " + mmod.refName + " is already in dict?");
                                        }
                                        else
                                        {
                                            masterMagicModList.Add(mmod.refName, mmod);
                                        }
                                        break;
                                    case "id":
                                        mmod.magicModID = reader.ReadElementContentAsInt();

                                        dictMagicModIDs[mmod.magicModID] = mmod;

                                        break;
                                    case "AddAbility":
                                        AbilityScript toCopy = AbilityScript.GetAbilityByName(reader.ReadElementContentAsString());
                                        if (toCopy != null)
                                        {
                                            mmod.addAbilities.Add(toCopy);
                                        }
                                        break;
                                    case "AddAbilitySilent":
                                        AbilityScript toCopySilent = AbilityScript.GetAbilityByName(reader.ReadElementContentAsString());
                                        if (toCopySilent != null)
                                        {
                                            mmod.addAbilities.Add(toCopySilent);
                                        }
                                        mmod.bDontAnnounceAddedAbilities = true;
                                        break;
                                    case "MonsterAllowed":
                                        mmod.monsterAllowed = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "LucidOrbsOnly":
                                        mmod.lucidOrbsOnly = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "Desc":
                                    case "Description":
                                        mmod.description = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                        break;
                                    case "BackupDescription":
                                        mmod.backupDescription = reader.ReadElementContentAsString();
                                        break;
                                    case "Prefix":
                                        mmod.prefix = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "ChangeResist":
                                        reader.ReadStartElement(); // Read "ChangeResist" node
                                        ResistanceData rd = new ResistanceData();
                                        rd.multiplier = 0;
                                        rd.flatOffset = 0;
                                        mmod.resists.Add(rd);
                                        while (reader.NodeType != XmlNodeType.EndElement)
                                        {
                                            switch (reader.Name)
                                            {
                                                case "ResistDamageType":
                                                    rd.damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                                                    break;
                                                case "ResistMultiplier":
                                                    txt = reader.ReadElementContentAsString();
                                                    rd.multiplier = CustomAlgorithms.TryParseFloat(txt);
                                                    break;
                                                case "ResistFlatOffset":
                                                    txt = reader.ReadElementContentAsString();
                                                    rd.flatOffset = CustomAlgorithms.TryParseFloat(txt);
                                                    break;
                                                case "ResistAbsorb":
                                                    rd.absorb = simpleBool[reader.ReadElementContentAsInt()];
                                                    break;
                                                default:
                                                    reader.Read();
                                                    break;
                                            }
                                        }
                                        reader.ReadEndElement(); // End "ChangeResist" node
                                        break;
                                    case "ChangePowerAsPercent":
                                        mmod.changePowerAsPercent = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "ChangeDamageType":
                                        mmod.changeDamageType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                                        break;
                                    case "ExclusionGroup":
                                        mmod.exclusionGroup = reader.ReadElementContentAsInt();
                                        break;
                                    case "Flag":
                                        MagicModFlags mmf = (MagicModFlags)Enum.Parse(typeof(MagicModFlags), reader.ReadElementContentAsString());
                                        mmod.modFlags[(int)mmf] = true;
                                        dictMagicModsByFlag[mmf].Add(mmod);
                                        break;
                                    case "NoNameChange":
                                        mmod.noNameChange = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "NoDescChange":
                                        mmod.noDescChange = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "ForceWriteDesc":
                                        mmod.forceWriteDesc = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "JobAbilityMod":
                                        mmod.jobAbilityMod = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "ChangePower":
                                        txt = reader.ReadElementContentAsString();
                                        mmod.changePower = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "ChangeBlock":
                                        txt = reader.ReadElementContentAsString();
                                        mmod.changeBlock = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "ChangeDurability":
                                        txt = reader.ReadElementContentAsString();
                                        mmod.changeDurability = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "ChallengeValue":
                                        txt = reader.ReadElementContentAsString();
                                        mmod.challengeValue = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "MaxCV":
                                        txt = reader.ReadElementContentAsString();
                                        mmod.maxChallengeValue = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "EquipmentSlot":
                                        EquipmentSlots es = (EquipmentSlots)Enum.Parse(typeof(EquipmentSlots), reader.ReadElementContentAsString());
                                        mmod.slot = es;
                                        break;
                                    case "NTag":
                                    case "NumberTag": // deprecate
                                        mmod.numberTags.Add(reader.ReadElementContentAsString());
                                        break;
                                    case "ModEffect":
                                        StatusEffect modEffect = new StatusEffect();
                                        mmod.modEffects.Add(modEffect);
                                        reader.ReadStartElement();
                                        while (reader.NodeType != XmlNodeType.EndElement)
                                        {
                                            switch (reader.Name)
                                            {
                                                case "StatusRef":
                                                    modEffect.refName = reader.ReadElementContentAsString();
                                                    // Now that we have the status name, find it in the master status list and duplicate it here.                                                
                                                    StatusEffect template = FindStatusTemplateByName(modEffect.refName);
                                                    modEffect.CopyStatusFromTemplate(template);
                                                    break;
                                                case "ProcChance":
                                                    // For anything
                                                    try
                                                    {
                                                        txt = reader.ReadElementContentAsString();
                                                        modEffect.listEffectScripts[0].procChance = CustomAlgorithms.TryParseFloat(txt);
                                                    }
                                                    catch (Exception e)  //#questionable_try_block
                                                    {
                                                        Debug.LogWarning("Parsed ProcChance but failed! " + reader.Name + " " + reader.NodeType + " " + mmod.refName + " Value was '" + txt + "' in mod named '" + modEffect.refName + "' ok, Exception: " + e.ToString());
                                                        reader.Read();
                                                    }
                                                    break;
                                                default:
                                                    reader.Read();
                                                    break;
                                            }

                                        }
                                        reader.ReadEndElement();
                                        break;
                                    case "AdventureStats": // Read "AdventureStats" node
                                        reader.ReadStartElement();
                                        while (reader.NodeType != XmlNodeType.EndElement)
                                        {
                                            if (!string.IsNullOrEmpty(reader.Name))
                                            {
                                                AdventureStats advStat = (AdventureStats)Enum.Parse(typeof(AdventureStats), reader.Name);
                                                string sValue = reader.ReadElementContentAsString();
                                                float parsed = CustomAlgorithms.TryParseFloat(sValue);
                                                mmod.adventureStats[(int)advStat] = parsed;
                                            }
                                            else
                                            {
                                                reader.Read();
                                            }
                                        }

                                        reader.ReadEndElement();    // End "AdventureStats" node
                                        break;
                                    default:
                                        reader.Read();
                                        break;
                                }
                            }
                            // We've finished reading magic mod.
                            //Debug.Log("Finish read " + mmod.refName + " " + reader.Name + " " + reader.NodeType);
                            reader.ReadEndElement();

                            if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                            {
                                yield return null;
                                timeAtLastYield = Time.realtimeSinceStartup;
                                GameMasterScript.IncrementLoadingBar(0.0075f);
                            }

                            break;
                        default:
                            reader.Read();
                            break;
                    } // End switch                    
                }

                reader.ReadEndElement();
            }
        }
        listModsSortedByChallengeRating = masterMagicModList.Values.OrderBy(m => m.challengeValue).ToList();

        listModsSortedByChallengeRating = masterMagicModList.Values.OrderBy(n => n.challengeValue).ToList();

        yield return null;

    } // End load magic mods

    private IEnumerator LoadAllJobs()
    {
        //Debug.Log("Loading all jobs.");

        masterJobList = new List<CharacterJobData>();
        masterFeatList = new List<CreationFeat>();

        StatBlock.statGrowthsAsString = new string[(int)StatGrowths.COUNT];
        for (int i = 0; i < (int)StatGrowths.COUNT; i++)
        {
            StatBlock.statGrowthsAsString[i] = ((StatGrowths)i).ToString();
        }

        List<string> jobFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.JOBS);

        foreach (string fileText in jobFilesToLoad)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(fileText)))
            {
                reader.Read();

                float timeAtLastYield = Time.realtimeSinceStartup;

                while (reader.Read())
                {
                    bool isJobStart = false;

                    if (reader.Name == "RandomNames")
                    {
                        reader.ReadStartElement();
                        CharCreation.randomNameList = new List<string>();
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            switch (reader.Name.ToLowerInvariant())
                            {
                                case "name":
                                    string defaultCharName = reader.ReadElementContentAsString();
                                    CharCreation.randomNameList.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(defaultCharName));
                                    break;
                                default:
                                    reader.Read();
                                    break;
                            }
                        }
                        reader.ReadEndElement();
                        //Debug.Log("All random names loaded!");
                    }

                    if (reader.Name == "CreationFeat")
                    {
                        CreationFeat cf = new CreationFeat();
                        masterFeatList.Add(cf);
                        reader.ReadStartElement();
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            switch (reader.Name.ToLowerInvariant())
                            {
                                case "featname":
                                    cf.featName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                    break;
                                case "description":
                                    cf.description = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                    break;
                                case "skillref":
                                    cf.skillRef = reader.ReadElementContentAsString();
                                    break;
                                case "sprite":
                                    cf.spriteRef = reader.ReadElementContentAsString();
                                    break;
                                case "requireunlock":
                                    cf.mustBeUnlocked = simpleBool[reader.ReadElementContentAsInt()];
                                    break;
                                //0 is the default, don't worry about flagging feats she can't use.
                                case "sharamode":
                                    cf.sharaModeStatus = reader.ReadElementContentAsInt();
                                    break;
                                default:
                                    reader.Read();
                                    break;
                            }
                        }
                        reader.ReadEndElement();
                    }

                    if (reader.Name == "JOB")
                    {
                        isJobStart = true;
                    }

                    if ((reader.NodeType == XmlNodeType.Element) && (isJobStart))
                    {

                        CharacterJobData cjd = new CharacterJobData();
                        masterJobList.Add(cjd);

                        bool endOfJob = false;
                        bool readJobAbility = false;
                        JobAbility jobAbil = null;

                        while (!endOfJob)
                        {
                            // reader.Read();        
                            string strValue = reader.Name;
                            //Debug.Log(strValue);
                            if (reader.NodeType != XmlNodeType.EndElement)
                            {
                                bool readSomething = false;

                                if (readJobAbility)
                                {
                                    //Debug.Log("RJA: " + strValue);
                                    switch (strValue)
                                    {
                                        case "AbilityName":
                                            reader.ReadElementContentAsString();
                                            readSomething = true;
                                            break;
                                        case "AbilityRef":
                                            string abilityRef = reader.ReadElementContentAsString();
                                            jobAbil.ability = AbilityScript.GetAbilityByName(abilityRef);
                                            jobAbil.abilityRef = abilityRef;
                                            readSomething = true;
                                            break;
                                        case "AddExtraSkillRef":
                                            jobAbil.extraSkillRef = reader.ReadElementContentAsString();
                                            readSomething = true;
                                            break;
                                        case "JPCost":
                                            // For anything
                                            jobAbil.jpCost = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "UpgradeCost":
                                            jobAbil.upgradeCost = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "MasterCost":
                                            // For anything
                                            jobAbil.masterCost = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "Innate":
                                            jobAbil.innate = simpleBool[reader.ReadElementContentAsInt()];
                                            readSomething = true;
                                            break;
                                        case "RepeatBuyPossible":
                                            jobAbil.repeatBuyPossible = simpleBool[reader.ReadElementContentAsInt()];
                                            readSomething = true;
                                            break;
                                        case "PostMasteryAbility":
                                            jobAbil.postMasteryAbility = simpleBool[reader.ReadElementContentAsInt()];
                                            readSomething = true;
                                            break;
                                        case "MaxBuys":
                                            jobAbil.maxBuysPossible = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "MasterAbility":
                                            jobAbil.jobMasterAbility = simpleBool[reader.ReadElementContentAsInt()];
                                            cjd.MasterAbility = jobAbil;
                                            readSomething = true;
                                            break;
                                        case "InnateReq":
                                            jobAbil.innateReq = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (strValue)
                                    {
                                        case "Name":
                                        case "JobName":
                                            cjd.jobName = reader.ReadElementContentAsString();
                                            cjd.jobEnum = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), cjd.jobName);
                                            readSomething = true;
                                            break;
                                        case "Prefab":
                                            cjd.prefab = reader.ReadElementContentAsString();

                                            bool isSwordDancer = cjd.prefab == "SwordDancer";

                                            if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                            {
                                                resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                                    {cjd.prefab, "Jobs/" + cjd.prefab});

                                                if (isSwordDancer)
                                                {
                                                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                                                        {cjd.prefab, "Jobs/LNY_" + cjd.prefab});                                                    
                                                }
                                            }
                                            else
                                            {
                                                TryPreloadResourceNoBundles(cjd.prefab, "Jobs/" + cjd.prefab);

                                                if (isSwordDancer) TryPreloadResourceNoBundles(cjd.prefab, "Jobs/LNY_" + cjd.prefab);
                                            }
                                            readSomething = true;
                                            break;
                                        case "Difficulty":
                                            cjd.difficulty = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "Description":
                                            cjd.jobDescription = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "DisplayName":
                                            cjd.DisplayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "NumberTag":
                                            cjd.numberTags.Add(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "BonusDescription1":
                                            cjd.BonusDescription1 = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "BonusDescription2":
                                            cjd.BonusDescription2 = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "BonusDescription3":
                                            cjd.BonusDescription3 = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "CapstoneAbility":
                                            cjd.capstoneAbilities.Add(reader.ReadElementContentAsString());
                                            readSomething = true;
                                            break;
                                        case "PortraitSprite":
                                            cjd.portraitSpriteRef = reader.ReadElementContentAsString();

                                            if (!PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                                            {
                                                cjd.PortraitSprite = Resources.Load<Sprite>("Portraits/" + cjd.portraitSpriteRef);
                                                if (cjd.PortraitSprite == null)
                                                {
                                                    Debug.Log("<color=red>Could not load portrait resource " + cjd.portraitSpriteRef + "</color>");
                                                }

                                                if (cjd.portraitSpriteRef == "SwordDancerPortrait")
                                                {
                                                    cjd.PortraitSprite = Resources.Load<Sprite>("Portraits/LNY_SwordDancerPortrait");
                                                }
                                            }
                                            readSomething = true;
                                            break;
                                        case "MasterJP":
                                            cjd.masterJP = reader.ReadElementContentAsInt();
                                            readSomething = true;
                                            break;
                                        case "JobAbility":
                                            jobAbil = new JobAbility();
                                            jobAbil.jobParent = cjd.jobEnum;
                                            readJobAbility = true;
                                            cjd.AddJobAbilityOnLoad(jobAbil);
                                            readSomething = true;
                                            break;
                                        case "StartingItem":
                                            string itemRef = reader.ReadElementContentAsString();
                                            cjd.startingItems.Add(itemRef);
                                            readSomething = true;
                                            break;
                                        case "StartingWeapon":
                                            string wRef = reader.ReadElementContentAsString();
                                            cjd.startingWeapon = wRef;
                                            readSomething = true;
                                            break;
                                        case "Emblem":
                                            reader.ReadStartElement();
                                            int emblemTier = 0;

                                            while (reader.NodeType != XmlNodeType.EndElement)
                                            {
                                                switch (reader.Name)
                                                {
                                                    case "Tier":
                                                        emblemTier = reader.ReadElementContentAsInt();
                                                        if (!cjd.emblemMagicMods.ContainsKey(emblemTier))
                                                        {
                                                            cjd.emblemMagicMods.Add(emblemTier, new List<string>());
                                                        }
                                                        break;
                                                    case "ModRef":
                                                        string modRef = reader.ReadElementContentAsString();
                                                        cjd.emblemMagicMods[emblemTier].Add(modRef);
                                                        //Debug.Log("Added " + modRef + " to tier " + emblemTier + " for " + cjd.displayName);
                                                        break;
                                                    default:
                                                        reader.Read();
                                                        break;
                                                }
                                            }
                                            reader.ReadEndElement();
                                            readSomething = true;
                                            break;
                                    }
                                    string txt;

                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        for (int c = 0; c < 5; c++) // HARDCODED: 5 stats
                                        {
                                            if (strValue == StatBlock.statGrowthsAsString[c])
                                            {
                                                txt = reader.ReadElementContentAsString();
                                                cjd.statGrowth[c] = CustomAlgorithms.TryParseFloat(txt);
                                                //Debug.Log(cjd.jobName + " " + (StatGrowths)c + " " + cjd.statGrowth[c]);
                                                cjd.statGrowth[c] = 1.5f; // for now, just set this standard
                                                readSomething = true;
                                            }
                                        }
                                    }

                                }

                                if (!readSomething)
                                {
                                    reader.Read();
                                }
                            }
                            else
                            {
                                if ((reader.NodeType == XmlNodeType.EndElement) && (reader.Name == "JobAbility"))
                                {
                                    readJobAbility = false;
                                    //Debug.Log(jobAbil.ability.abilityName + " loaded. Cost: " + jobAbil.jpCost);
                                }
                                reader.Read();
                            }

                            if ((reader.NodeType == XmlNodeType.EndElement) && (reader.Name != "JobAbility"))
                            {
                                endOfJob = true;
                                if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                                {
                                    yield return null;
                                    timeAtLastYield = Time.realtimeSinceStartup;
                                }
                            }
                        } // End of item while loop 

                    } // End of read item  
                } // End of document
            }
        }

        yield return null;
    } // End load jobs

    private IEnumerator LoadAllMapObjects()
    {
        masterMapObjectDict = new Dictionary<string, Destructible>();

        List<string> mapObjectStringsToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.MAPOBJECTS);
        PlayerModManager.AddModFilesToList(mapObjectStringsToLoad, PlayerModfileTypes.MAPOBJECTS);

        foreach (string loadObjString in mapObjectStringsToLoad)
        {
            if (string.IsNullOrEmpty(loadObjString)) continue;
            using (XmlReader reader = XmlReader.Create(new StringReader(loadObjString)))
            {
                reader.Read();

                float timeAtLastYield = Time.realtimeSinceStartup;

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.Name == "Destructible")
                    {
                        //Destructible dt = new Destructible();
                        Destructible dt = DTPooling.GetDestructible();
                        bool success = false;

                        try { success = dt.ReadFromXml(reader); }
                        catch (Exception e)
                        {
                            Debug.LogError("Failed to read destructible due to " + e);
                            success = false;
                        }

                        if (success)
                        {
                            bool safeToAdd = true;
                            if (masterMapObjectDict.ContainsKey(dt.actorRefName))
                            {
                                if (!dt.replaceRef)
                                {
                                    Debug.LogError("WARNING: " + dt.actorRefName + " already in map obj dict, not adding.");
                                    safeToAdd = false;
                                }
                                else
                                {
                                    masterMapObjectDict.Remove(dt.actorRefName);
                                }
                            }

                            if (safeToAdd)
                            {
                                masterMapObjectDict.Add(dt.actorRefName, dt);
                            }
                        }
                    }
                    else
                    {
                        reader.Read();
                    }

                    if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                    {
                        yield return null;
                        timeAtLastYield = Time.realtimeSinceStartup;
                    }
                }

                reader.ReadEndElement();
            }
        }

        masterSpawnableMapObjectList = new Dictionary<string, Destructible>();
        foreach (Destructible dt in masterMapObjectDict.Values)
        {
            if (dt.autoSpawn)
            {
                masterSpawnableMapObjectList.Add(dt.actorRefName, dt);
            }
        }

        foreach (string refName in masterBreakableSpawnTable.table.Keys)
        {
            Destructible template = Destructible.FindTemplate(refName);
            if (template != null)
            {
                masterBreakableSpawnTable.actors.Add(template);
            }
            else
            {
                Debug.Log("Couldn't link breakable/DT template " + refName);
            }
        }

        yield return new WaitForSeconds(0.01f);

    } // End load magic mods

    private IEnumerator LoadAllItems()
    {
        if( masterItemList != null && masterItemList.Count > 0 )
        {
            yield break;
        }

        masterItemList = new Dictionary<string, Item>();
        itemsAutoAddToShops = new List<Item>();
        masterFoodList = new List<Item>();
        masterGearSetList = new List<GearSet>();
        masterTreeFoodList = new List<Item>();
        listDuringLoadOfEqInGearSets = new List<Equipment>();
        CookingScript.masterRecipeList.Clear();

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        //settings.IgnoreWhitespace = true;        

        globalUniqueItemID = 1;

        List<string> allItemFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.ITEMS);
        PlayerModManager.AddModFilesToList(allItemFilesToLoad, PlayerModfileTypes.ITEMS);

        EquipmentBlock.InitStaticArrays();

        temp_itemsAddedToDictDuringLoad = new List<Item>();
        temp_gearSetsAddedToDictDuringLoad = new List<GearSet>();
        temp_recipesAddedToDictDuringLoad = new List<Recipe>();

        int index = -1;
        foreach (string itemText in allItemFilesToLoad)
        {
            index++;
            if (string.IsNullOrEmpty(itemText)) continue;
            XmlReader reader = XmlReader.Create(new StringReader(itemText), settings);

            temp_itemsAddedToDictDuringLoad.Clear();
            temp_gearSetsAddedToDictDuringLoad.Clear();
            temp_recipesAddedToDictDuringLoad.Clear();

            temp_errorDuringItemLoad = false;
            yield return Item.ReadEntireFileFromXml(reader, index);
            //yield return this.StartThrowingCoroutine(Item.ReadEntireFileFromXml(reader, index), success => temp_errorDuringItemLoad = false);

            if (temp_errorDuringItemLoad)
            {
                Debug.LogError("<color=red>WARNING: Failed to read item file during load</color>");
                foreach (Item itm in GameMasterScript.temp_itemsAddedToDictDuringLoad) // Clear out any items from that file, just in case. Who knows what went wrong.
                {
                    if (!string.IsNullOrEmpty(itm.actorRefName) && masterItemList.ContainsKey(itm.actorRefName))
                    {
                        masterItemList.Remove(itm.actorRefName);
                    }
                }
                foreach (GearSet gs in GameMasterScript.temp_gearSetsAddedToDictDuringLoad) // Clear out any items from that file, just in case. Who knows what went wrong.
                {
                    if (!string.IsNullOrEmpty(gs.refName) && masterGearSetList.Contains(gs))
                    {
                        masterGearSetList.Remove(gs);
                    }
                }
                foreach (Recipe recip in GameMasterScript.temp_recipesAddedToDictDuringLoad) // Clear out any items from that file, just in case. Who knows what went wrong.
                {
                    if (!string.IsNullOrEmpty(recip.refName) && CookingScript.masterRecipeList.Contains(recip))
                    {
                        CookingScript.masterRecipeList.Remove(recip);
                    }
                }
            }
        }

        foreach (var eq in listDuringLoadOfEqInGearSets)
                {
                    foreach (GearSet gs in masterGearSetList)
                    {
                        if (gs.refName == eq.gearSetRef)
                        {
                            eq.gearSet = gs;
                            break;
                        }
                    }
                }



        BakedItemDefinitions.AddAllBakedItemDefinitions();
        listDuringLoadOfEqInGearSets = null;
        DLCManager.UpdateItemDefinitionsAfterLoad();
    }

    private IEnumerator LoadAllChampionData()
    {
        masterChampionDataDict = new Dictionary<string, ChampionData>();
        masterChampionModList = new Dictionary<string, ChampionMod>();
        masterShadowKingChampModList = new List<ChampionMod>();
        masterMemoryKingChampModList = new List<ChampionMod>();
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        itemWorldMonsterDeathLines = new List<string>();

        Monster.familyNamesVerbose = new Dictionary<string, string>();
        Monster.attributeNamesLocalized = new string[(int)MonsterAttributes.COUNT];

        Monster.attributeNamesLocalized[(int)MonsterAttributes.ALWAYSUSEMOVEABILITIES] = StringManager.GetString("monster_attr_alwaysmoveabils");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.BERSERKER] = StringManager.GetString("monster_attr_berserker");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.CALLFORHELP] = StringManager.GetString("monster_attr_callforhelp");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.CANTACT] = StringManager.GetString("monster_attr_cantact");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.CANTATTACK] = StringManager.GetString("monster_attr_cantattack");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.COMBINABLE] = StringManager.GetString("monster_attr_combinable");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.FLYING] = StringManager.GetString("monster_attr_flying");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.HEALER] = StringManager.GetString("monster_attr_healer");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.GREEDY] = StringManager.GetString("monster_attr_greedy");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.LAZY] = StringManager.GetString("monster_attr_lazy");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.LOVESBATTLES] = StringManager.GetString("monster_attr_lovesbattles");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.LOVESLAVA] = StringManager.GetString("monster_attr_loveslava");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.LOVESELEC] = StringManager.GetString("monster_attr_loveselec");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.LOVESMUD] = StringManager.GetString("monster_attr_lovesmud");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.PACIFIST] = StringManager.GetString("monster_attr_pacifist");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.PREDATOR] = StringManager.GetString("monster_attr_predator");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.RONIN] = StringManager.GetString("monster_attr_ronin");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.SNIPER] = StringManager.GetString("monster_attr_sniper");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.STALKER] = StringManager.GetString("monster_attr_stalker");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.SUPPORTER] = StringManager.GetString("monster_attr_supporter");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.TIMID] = StringManager.GetString("monster_attr_timid");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.GANGSUP] = StringManager.GetString("monster_attr_gangsup");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.STARTASLEEP] = StringManager.GetString("monster_attr_startasleep");
        Monster.attributeNamesLocalized[(int)MonsterAttributes.NO_KNOCKOUT] = StringManager.GetString("monster_attr_no_knockout");
        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            Monster.attributeNamesLocalized[(int)MonsterAttributes.LIVEINWATER] = StringManager.GetString("monster_attr_liveinwater");
        }

        masterFamilyList = new List<MonsterFamily>();

        List<string> championDataFilesToLoad = new List<string>();
        for (int i = 0; i < championDataXML.Length; i++)
        {
            if (championDataXML[i] == null) continue;
            if (!DLCManager.IsFileValidToLoad(championDataXML[i].name, EDLCPackages.EXPANSION1)) continue;
            championDataFilesToLoad.Add(championDataXML[i].text);
        }

        //for (int i = 0; i < championDataXML.Length; i++)
        foreach (string champText in championDataFilesToLoad)
        {
            if (!string.IsNullOrEmpty(champText))
            {
                XmlReader reader = XmlReader.Create(new StringReader(champText), settings);
                using (reader)
                {
                    reader.Read();
                    reader.ReadStartElement();
                    bool finished = false;
                    int count = 0;
                    while (!finished) // was while read...
                    {
                        count++;
                        if (count >= 5000)
                        {
                            finished = true;
                            Debug.Log("Problem");
                        }

                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "ChampionMod"))
                        {
                            ChampionMod cmod = new ChampionMod();
                            reader.ReadStartElement();
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                string strValue = reader.Name;
                                string txt;

                                switch (strValue)
                                {
                                    case "ModName":
                                        cmod.displayName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                        break;
                                    case "RefName":
                                        cmod.refName = reader.ReadElementContentAsString();
                                        masterChampionModList.Add(cmod.refName, cmod);
                                        break;
                                    case "ChampionItem":
                                        cmod.accessoryRef = reader.ReadElementContentAsString();
                                        break;
                                    case "ChallengeValue":
                                        txt = reader.ReadElementContentAsString();
                                        cmod.challengeValue = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "MaxChallengeValue":
                                        txt = reader.ReadElementContentAsString();
                                        cmod.maxChallengeValue = CustomAlgorithms.TryParseFloat(txt);
                                        break;
                                    case "ShadowKingOnly":
                                        cmod.shadowKingOnly = simpleBool[(int)reader.ReadElementContentAsInt()];
                                        masterShadowKingChampModList.Add(cmod);
                                        break;
                                    case "MemoryKingOnly":
                                        cmod.memoryKingOnly = simpleBool[(int)reader.ReadElementContentAsInt()];
                                        masterMemoryKingChampModList.Add(cmod);
                                        break;
                                    case "NewGamePlusOnly":
                                        cmod.newGamePlusOnly = simpleBool[(int)reader.ReadElementContentAsInt()];
                                        break;
                                    case "ElementalAura":
                                        cmod.elementalAura = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                                        break;
                                    case "ExclusionGroup":
                                        cmod.exclusionGroup = reader.ReadElementContentAsInt();
                                        break;
                                    case "DisplayNameOnHover":
                                        cmod.displayNameOnHover = simpleBool[reader.ReadElementContentAsInt()];
                                        break;
                                    case "MonsterPower":
                                        reader.ReadStartElement();
                                        MonsterPowerData mpd = new MonsterPowerData();
                                        cmod.modPowers.Add(mpd);

                                        while (reader.NodeType != XmlNodeType.EndElement)
                                        {
                                            strValue = reader.Name;
                                            switch (strValue)
                                            {
                                                case "SkillRef":
                                                    string abilRef = reader.ReadElementContentAsString();
                                                    mpd.abilityRef = AbilityScript.GetAbilityByName(abilRef);
                                                    break;
                                                case "MinRange":
                                                    mpd.minRange = reader.ReadElementContentAsInt();
                                                    break;
                                                case "MaxRange":
                                                    mpd.maxRange = reader.ReadElementContentAsInt();
                                                    break;
                                                case "HealthThreshold":
                                                    txt = reader.ReadElementContentAsString();
                                                    mpd.healthThreshold = CustomAlgorithms.TryParseFloat(txt);
                                                    break;
                                                case "UseWithNoTarget":
                                                    mpd.useWithNoTarget = simpleBool[reader.ReadElementContentAsInt()];
                                                    break;
                                                case "ChanceToUse":
                                                    txt = reader.ReadElementContentAsString();
                                                    mpd.chanceToUse = CustomAlgorithms.TryParseFloat(txt);
                                                    break;
                                                case "UseState":
                                                case "BehaviorStateReq":
                                                    mpd.useState = (BehaviorState)Enum.Parse(typeof(BehaviorState), reader.ReadElementContentAsString());
                                                    break;
                                                default:
                                                    reader.Read();
                                                    break;

                                            }
                                        }
                                        reader.ReadEndElement();
                                        break;
                                    default: // NEW to avoid "Problem"
                                        reader.Read();
                                        break;
                                }

                            }
                            reader.ReadEndElement();

                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "MonsterQuip")
                        {
                            itemWorldMonsterDeathLines.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "PetName")
                        {
                            MonsterManagerScript.AddRandomPetName(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                        }

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Family")
                        {
                            reader.ReadStartElement();
                            ChampionData cd = new ChampionData();
                            cd.name1 = new List<string>();
                            cd.name2 = new List<string>();
                            cd.name3 = new List<string>();
                            cd.name4 = new List<string>();
                            cd.name5 = new List<string>();
                            cd.name6 = new List<string>();

                            string familyName = reader.ReadElementContentAsString().ToLowerInvariant();

                            MonsterFamily mf = new MonsterFamily();
                            mf.refName = familyName;
                            masterFamilyList.Add(mf);

                            masterChampionDataDict.Add(familyName, cd);

                            while ((reader.NodeType != XmlNodeType.EndElement))
                            {
                                string strValue = reader.Name;

                                bool readSomething = false;

                                switch (strValue)
                                {
                                    case "DisplayName":
                                        string dName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                                        mf.displayName = dName;
                                        Monster.familyNamesVerbose.Add(mf.refName, mf.displayName);
                                        readSomething = true;
                                        break;
                                    case "FamilyPower":
                                        reader.ReadStartElement();

                                        while (reader.NodeType != XmlNodeType.EndElement)
                                        {
                                            switch (reader.Name)
                                            {
                                                case "RefName":
                                                    reader.ReadElementContentAsString();
                                                    break;
                                                default:
                                                    reader.Read();
                                                    break;
                                            }
                                        }

                                        reader.ReadEndElement();

                                        readSomething = true;
                                        break;
                                    case "Name1":
                                        cd.name1.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;

                                    case "Name2":
                                        cd.name2.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;
                                    case "Name3":
                                        cd.name3.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;
                                    case "Name4":
                                        cd.name4.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;
                                    case "Name5":
                                        cd.name5.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;
                                    case "Name6":
                                        cd.name6.Add(StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString()));
                                        readSomething = true;
                                        break;
                                }
                                if (!readSomething)
                                {
                                    reader.Read();
                                }
                            }
                            reader.ReadEndElement(); // Done reading this family.
                        }


                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            finished = true;
                        }

                        if (reader.NodeType == XmlNodeType.Whitespace)
                        {
                            reader.Read();
                        }

                    } // End of document
                }
            }
        }

        yield return null;
    }

    private IEnumerator LoadAllMapGenerationData()
    {
        masterDungeonLevelList = new Dictionary<int, DungeonLevel>();
        allDungeonLevelsAsList = new List<DungeonLevel>();
        itemWorldMapDict = new Dictionary<float, List<DungeonLevel>>();
        itemWorldMapList = new List<DungeonLevel>();

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        // Load our core data first.
        List<string> dungeonFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.MAPGENERATION);

        foreach (string dungeonTextFile in dungeonFilesToLoad)
        {
            List<DungeonLevel> coreLevels = DungeonLevel.ReadAllLevelsFromText(dungeonTextFile, settings);
            foreach (DungeonLevel dl in coreLevels)
            {
                if (masterDungeonLevelList.ContainsKey(dl.floor))
                {
                    Debug.Log("Warning! " + dl.floor + " exists in dict already, skipping.");
                }
                else
                {
                    masterDungeonLevelList.Add(dl.floor, dl);
                }
            }
        }

        Dictionary<string, List<string>> dungeonLevelsByModName = new Dictionary<string, List<string>>();

        // Below block of code is duplicated from PlayerModManager. I know this is a little clumsy buuuut
        // For THIS specific type of loader code, I need to know exactly which mod each XML file came from 
        // Normally we don't care about this but for this we do
        foreach (ModDataPack mdp in PlayerModManager.GetAllLoadedPlayerMods())
        {
            if (!mdp.enabled) continue;
            if (mdp.dictPlayerModfiles.ContainsKey(PlayerModfileTypes.DUNGEONLEVELS))
            {
                foreach (string str in mdp.dictPlayerModfiles[PlayerModfileTypes.DUNGEONLEVELS])
                {
                    Debug.Log("Loading player mod file of type " + PlayerModfileTypes.DUNGEONLEVELS + ": " + str);
                    try
                    {
                        if (!dungeonLevelsByModName.ContainsKey(mdp.modName))
                        {
                            dungeonLevelsByModName.Add(mdp.modName, new List<string>());
                        }
                        dungeonLevelsByModName[mdp.modName].Add(System.IO.File.ReadAllText(str));
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Failed to add mod file of type " + PlayerModfileTypes.DUNGEONLEVELS + ": " + str);
                    }
                }
            }
        }

        if (dungeonLevelsByModName.Keys.Count > 0)
        {
            List<DungeonLevel> levelsForMod = new List<DungeonLevel>();
            Dictionary<int, int> dictModLevelIDToFloors = new Dictionary<int, int>();

            foreach (string modName in dungeonLevelsByModName.Keys)
            {
                levelsForMod.Clear();
                dictModLevelIDToFloors.Clear();

                // Iterate through each mod that could have N custom dungeon levels.
                foreach (string modText in dungeonLevelsByModName[modName])
                {
                    levelsForMod = levelsForMod.Concat(DungeonLevel.ReadAllLevelsFromText(modText, settings)).ToList();

                }
#if UNITY_EDITOR
                Debug.Log("Total new levels from mod " + modName + ": " + levelsForMod.Count);
#endif
                // Now that we have all these levels, we need to assign unused Floor numbers, while also tracking our conversion
                // of ModLevelIDs into real, Tangledeep-usable Floor numbers
                int actualFloor = 500;
                foreach (DungeonLevel dl in levelsForMod)
                {
                    while (masterDungeonLevelList.ContainsKey(actualFloor))
                    {
                        actualFloor++;
                    }
                    dl.floor = actualFloor;
                    if (dictModLevelIDToFloors.ContainsKey(dl.modLevelID))
                    {
                        Debug.LogError("WARNING! From player mod " + modName + ", ModPlayerID " + dl.modLevelID + " has been used more than once.");
                    }
                    else
                    {
                        dictModLevelIDToFloors.Add(dl.modLevelID, actualFloor);
                        masterDungeonLevelList.Add(actualFloor, dl);
#if UNITY_EDITOR
                        Debug.Log("Successfully added player floor ID " + dl.modLevelID + " to dict as actual TD floor " + actualFloor);
#endif

                        // Now, let's look through all RoomTemplates that might possibly reference a ModLevelID.
                        foreach (RoomTemplate rt in masterDungeonRoomlist.Values)
                        {
                            if (!rt.hasModPlayerIDDef) continue;

                            foreach (CharDefinitionForRoom cdfr in rt.dictCharDef.Values)
                            {
                                // Remap stairs that were pointing to ModLevelID
                                if (cdfr.pointToModLevelID >= 0)
                                {
                                    cdfr.pointToFloor = actualFloor;
                                }
                            }
                        }

                    }
                }

                // Then look through all other floors in this file and make sure they aren't pointed at ModLevelID. 
                // If they are, now that we have the full conversion of ModLevelID to Floor, we can convert them                       
                foreach (DungeonLevel dl in levelsForMod)
                {
                    if (dl.stairsDownToModLevelID >= 0)
                    {
                        dl.stairsDownToLevel = dictModLevelIDToFloors[dl.stairsDownToModLevelID];
                    }
                    if (dl.stairsUpToModLevelID >= 0)
                    {
                        dl.stairsUpToLevel = dictModLevelIDToFloors[dl.stairsUpToModLevelID];
                    }
                }

            }
        }

        // At this point, existing Tangledeep core data levels AND player mod stuff is now living in perfect harmony
        // Unique IDs have been assigned, the galaxy is at peace.

        foreach (DungeonLevel dl in masterDungeonLevelList.Values)
        {
            if (dl.itemWorld && (dl.floor < MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_START || dl.floor > MapMasterScript.DRAGON_ITEMWORLD_MAP_TEMPLATES_END))
            {
                if (itemWorldMapDict.ContainsKey(dl.challengeValue))
                {
                    itemWorldMapDict[dl.challengeValue].Add(dl);
                }
                else
                {
                    List<DungeonLevel> dunList = new List<DungeonLevel>();
                    dunList.Add(dl);
                    itemWorldMapDict.Add(dl.challengeValue, dunList);
                }

                itemWorldMapList.Add(dl); // keep these in a list as well as a dict so we can pull ANY level at random            
            }
        }

        allDungeonLevelsAsList = masterDungeonLevelList.Values.ToList();
        yield return null;
    }

    private EffectScript ReadEffectScriptFromXML(string strValue, XmlReader reader, EffectScript baseEffect)
    {
        if (string.IsNullOrEmpty(strValue))
        {
            iBadDataDuringLoadComboChain++;
            if (iBadDataDuringLoadComboChain >= 3)
            {
                throw new Exception("Bad data in ability somewhere, good luck with that. Catch me: " + baseEffect.effectRefName);
            }
        }
        else
        {
            iBadDataDuringLoadComboChain = 0;
        }


        bool readSomething = false;
        string txt;
        switch (strValue) // Get generic stuff for all effects
        {
            case "Type":
            case "EffectType": // deprecate
                //EffectType thisEffectType = (EffectType)Enum.Parse(typeof(EffectType), reader.ReadElementContentAsString());
                EffectType thisEffectType = CustomAlgorithms.dictStrToEffectTypeEnum[reader.ReadElementContentAsString()];
                switch (thisEffectType)
                {
                    case EffectType.MOVEACTOR:
                        baseEffect = new MoveActorEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.CHANGESTAT:
                        baseEffect = new ChangeStatEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.ADDSTATUS:
                        baseEffect = new AddStatusEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.REMOVESTATUS:
                        baseEffect = new RemoveStatusEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.DAMAGE:
                        baseEffect = new DamageEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.DESTROYACTOR:
                        baseEffect = new DestroyActorEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.IMMUNESTATUS:
                        baseEffect = new ImmuneStatusEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.SPECIAL:
                        baseEffect = new SpecialEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.DESTROYTILE:
                        baseEffect = new DestroyTileEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.SUMMONACTOR:
                        baseEffect = new SummonActorEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.ALTERBATTLEDATA:
                        baseEffect = new AlterBattleDataEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.INFLUENCETURN:
                        baseEffect = new InfluenceTurnEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.EMPOWERATTACK:
                        baseEffect = new EmpowerAttackEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.ATTACKREACTION:
                        baseEffect = new AttackReactionEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.ABILITYCOSTMODIFIER:
                        baseEffect = new AbilityModifierEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                    case EffectType.SPELLSHAPE:
                        baseEffect = new SpellShaperEffect();
                        baseEffect.effectType = thisEffectType;
                        break;
                }
                readSomething = true;
                break;
            case "Disp":
            case "EffectName": // deprecate
                baseEffect.effectName = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                readSomething = true;
                break;
            case "AtkWp":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.ATK_WEAPON_POWER);
                readSomething = true;
                break;
            case "AtkSp":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.ATK_SPIRIT_POWER);
                readSomething = true;
                break;
            case "AtkLv":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.ATK_LEVEL);
                readSomething = true;
                break;
            case "TMaxHP":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.DEF_MAX_HP);
                readSomething = true;
                break;
            case "TCurHP":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.DEF_CUR_HP);
                readSomething = true;
                break;
            case "AtkCurHP":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.CUR_HP);
                readSomething = true;
                break;
            case "OrigCurHP":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.ORIG_CUR_HP);
                readSomething = true;
                break;
            case "CmbDmg":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.COMBAT_DAMAGE);
                readSomething = true;
                break;
            case "BlockDmg":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.BLOCK_DMG);
                readSomething = true;
                break;
            case "BuffrDmg":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.BUFFER_DAMAGE);
                readSomething = true;
                break;
            case "BaseDmg":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.BASE_DAMAGE);
                readSomething = true;
                break;
            case "Rnd":
                string unp = reader.ReadElementContentAsString();
                string[] parsed = unp.Split('|');
                baseEffect.damageEquationVars[(int)EDamageEquationVars.RND_MIN] = CustomAlgorithms.TryParseFloat(parsed[0]);
                baseEffect.damageEquationVars[(int)EDamageEquationVars.RND_MAX] = CustomAlgorithms.TryParseFloat(parsed[1]);
                baseEffect.anyDamageEquationVars = true;
                break;
            /* case "RndMin":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.RND_MIN);
                readSomething = true;
                break;
            case "RndMax":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.RND_MAX);
                readSomething = true;
                break; */
            case "MaxStat":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.MAX_STAT);
                readSomething = true;
                break;
            case "CurStat":
                baseEffect.TryReadDamageEquationVarFromXml(reader, EDamageEquationVars.CUR_STAT);
                readSomething = true;
                break;
            case "EffectTag":
                EffectTags eTag = (EffectTags)Enum.Parse(typeof(EffectTags), reader.ReadElementContentAsString());
                baseEffect.effectTags[(int)eTag] = true;
                readSomething = true;
                break;
            case "AddFlag":
                ActorFlags f = (ActorFlags)Enum.Parse(typeof(ActorFlags), reader.ReadElementContentAsString());
                baseEffect.switchFlags[(int)f] = true;
                readSomething = true;
                break;
            case "Silent":
                baseEffect.silent = true;
                reader.Read();
                readSomething = true;
                break;
            case "CSOOA":
            case "CenterSpriteOnOriginatingActor":
                baseEffect.centerSpriteOnOriginatingActor = true;
                reader.Read();
                readSomething = true;
                break;
            case "CenterSpriteOnMiddlePosition":
                baseEffect.centerSpriteOnMiddlePosition = true;
                reader.Read();
                readSomething = true;
                break;
            case "DontTouchAffectedList":
                baseEffect.doNotAlterPreviousAffectedActorList = true;
                reader.Read();
                readSomething = true;
                break;                
            case "Faction":
            case "ReqActorFaction": // deprecate
                baseEffect.reqActorFaction = (Faction)Enum.Parse(typeof(Faction), reader.ReadElementContentAsString());
                readSomething = true;
                break;
            case "AdjacentRange":
                baseEffect.adjacentRange = reader.ReadElementContentAsInt();
                readSomething = true;
                break;
            case "TurnsSinceDamage":
                baseEffect.minimumTurnsSinceLastDamaged = reader.ReadElementContentAsInt();                
                readSomething = true;
                break;
            case "TCon":
            case "TriggerCondition": // deprecate
                baseEffect.triggerCondition = (AttackConditions)Enum.Parse(typeof(AttackConditions), reader.ReadElementContentAsString());
                readSomething = true;
                break;
            case "ReqTargetCondition":
                baseEffect.reqTargetCondition = reader.ReadElementContentAsString();
                readSomething = true;
                break;
            case "NoClearPositionsOnRun":
                baseEffect.noClearPositionsOnRun = simpleBool[reader.ReadElementContentAsInt()];
                readSomething = true;
                break;
            case "ProcessBufferIndex":
                baseEffect.processBufferIndex = reader.ReadElementContentAsInt();
                readSomething = true;
                break;
            case "BattleText":
                string bText = StringManager.GetLocalizedStringOrFallbackToEnglish(reader.ReadElementContentAsString());
                bText = CustomAlgorithms.ParseRichText(bText, false);
                baseEffect.battleText = bText;
                readSomething = true;
                break;
            case "RotateAnim":
            case "RotateAnimToTarget":
                baseEffect.rotateAnimToTarget = true;
                reader.Read();
                readSomething = true;
                break;
            case "ERef":
            case "Ref":
                baseEffect.effectRefName = reader.ReadElementContentAsString();
                if (masterEffectList.ContainsKey(baseEffect.effectRefName))
                {
                    Debug.Log("WARNING! Effect name " + baseEffect.effectName + " REFNAME " + baseEffect.effectRefName + " already exists.");
                }
                else
                {
                    masterEffectList.Add(baseEffect.effectRefName, baseEffect);
                }
                readSomething = true;
                break;
            case "Trg1":
                baseEffect.triggerPerTurns = 1;
                reader.ReadElementContentAsString();
                readSomething = true;
                break;
            case "TriggerPerTurns":
            case "TPT":
                baseEffect.triggerPerTurns = reader.ReadElementContentAsInt();
                readSomething = true;
                break;
            case "EnforceTPT":
                baseEffect.enforceTriggerPerTurns = true;
                reader.ReadElementContentAsString();
                readSomething = true;
                break;
            case "EffectRef":
            case "SpriteEffectRef":
                baseEffect.spriteEffectRef = reader.ReadElementContentAsString();

                if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                        {baseEffect.spriteEffectRef, "SpriteEffects/" + baseEffect.spriteEffectRef});
                }
                else
                {
                    TryPreloadResourceNoBundles(baseEffect.spriteEffectRef, "SpriteEffects/" + baseEffect.spriteEffectRef);
                }
                readSomething = true;
                break;
            case "AnimLength":
                txt = reader.ReadElementContentAsString();
                baseEffect.animLength = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "DelayBeforeAnimStart":
                txt = reader.ReadElementContentAsString();
                baseEffect.delayBeforeAnimStart = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "AttackerBelowHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.attackerBelowHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "Script_ProcessActorsPreEffect":
                baseEffect.script_processActorsPreEffect = reader.ReadElementContentAsString();
                readSomething = true;
                break;
            case "Script_TriggerCondition":
                baseEffect.script_triggerCondition = reader.ReadElementContentAsString();
                GenericTriggerConditionalFunction.CacheScript(baseEffect.script_triggerCondition);
                readSomething = true;
                break;
            case "AttackerAboveHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.attackerAboveHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "DefenderBelowHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.defenderBelowHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "DefenderAboveHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.defenderAboveHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "OrigBelowHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.origBelowHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "OrigAboveHealth":
                txt = reader.ReadElementContentAsString();
                baseEffect.origAboveHealth = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "Anim":
                // Up to three fields split by comma
                // RefName, AnimLength, (optional value of 1 = rotate anim to target)
                string unparsed = reader.ReadElementContentAsString();
                parsed = unparsed.Split(',');
                baseEffect.playAnimation = true;
                baseEffect.spriteEffectRef = parsed[0];

                if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
                {
                    try
                    {
                        resourcesToLoadAfterMainSceneThatWeUsedToPreload.Add(new string[]
                            {baseEffect.spriteEffectRef, "SpriteEffects/" + baseEffect.spriteEffectRef});
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Tried to add " + baseEffect.spriteEffectRef + " to a preload list and something broke. resourcesHash == null is: " + (resourcesToLoadAfterMainSceneThatWeUsedToPreload == null));
                    }
                }
                else
                {
                    TryPreloadResourceNoBundles(baseEffect.spriteEffectRef, "SpriteEffects/" + baseEffect.spriteEffectRef);
                }
                baseEffect.animLength = CustomAlgorithms.TryParseFloat(parsed[1]);
                if (parsed.Length == 3)
                {
                    baseEffect.rotateAnimToTarget = true;
                }
                readSomething = true;
                break;
            case "PlayAnimation":
                baseEffect.playAnimation = true;
                reader.Read();
                readSomething = true;
                break;
            case "IsProjectile":
                baseEffect.isProjectile = true;
                reader.Read();
                readSomething = true;
                break;
            case "PlayAnimationInstant":
                baseEffect.playAnimationInstant = true;
                reader.Read();
                readSomething = true;
                break;
            case "ProcChance":
                // For anything
                txt = reader.ReadElementContentAsString();
                baseEffect.procChance = CustomAlgorithms.TryParseFloat(txt);
                //Debug.Log(baseEffect.effectRefName + " " + baseEffect.effectName + " proc chance " + baseEffect.procChance);
                readSomething = true;
                break;
            case "ChanceToHitSpecificTarget":
                txt = reader.ReadElementContentAsString();
                baseEffect.chanceToHitSpecificTarget = CustomAlgorithms.TryParseFloat(txt);
                readSomething = true;
                break;
            case "Targ": // deprecate
            case "TargetActorType":
                baseEffect.tActorType = (TargetActorType)Enum.Parse(typeof(TargetActorType), reader.ReadElementContentAsString());
                readSomething = true;
                break;
            case "ReqStatus":
            case "ReqOrigStatus":
                baseEffect.requiredStatusForOrigFighter = reader.ReadElementContentAsString();
                readSomething = true;
                break;
            case "ReqOrigStatusStacks":
                baseEffect.requiredStatusStacks = reader.ReadElementContentAsInt();
                readSomething = true;
                break;
            case "RandomTargetRange":
                baseEffect.randTargetRange = reader.ReadElementContentAsInt();
                readSomething = true;
                break;
            case "ProjectileMoveType":
                baseEffect.projectileMovementType = (MovementTypes)Enum.Parse(typeof(MovementTypes), reader.ReadElementContentAsString());
                readSomething = true;
                break;
            case "ProjectileTossHeight":
                baseEffect.projectileTossHeight = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                readSomething = true;
                break;
        }
        if (readSomething)
        {
            return baseEffect;
        }
        // Now read per-effect things
        if (baseEffect.effectType == EffectType.DESTROYACTOR)
        {
            DestroyActorEffect dae = baseEffect as DestroyActorEffect;
            switch (strValue)
            {
                case "DestroySpecificActor":
                    string actorRef = reader.ReadElementContentAsString();
                    dae.destroySpecificActors.Add(actorRef);
                    readSomething = true;
                    break;
            }
            return dae;
        }

        if (baseEffect.effectType == EffectType.IMMUNESTATUS)
        {
            ImmuneStatusEffect ise = baseEffect as ImmuneStatusEffect;
            switch (strValue)
            {
                case "ChanceToResist":
                    string raw = reader.ReadElementContentAsString();
                    ise.chanceOfImmunity = CustomAlgorithms.TryParseFloat(raw);
                    readSomething = true;
                    break;
                case "ImmuneStatusRef":
                    ise.immuneStatusRefs.Add(reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "ImmuneMessageRef":
                    ise.refStringImmunityMessage = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "ResistAnyNegative":
                    ise.resistAnyNegative = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "ImmuneStatusFlag":
                    string flagRaw = reader.ReadElementContentAsString();
                    ise.immuneStatusFlags.Add((StatusFlags)Enum.Parse(typeof(StatusFlags), flagRaw));
                    readSomething = true;
                    break;
            }
            return ise;
        }

        if (baseEffect.effectType == EffectType.MOVEACTOR)
        {
            MoveActorEffect mv = (MoveActorEffect)baseEffect as MoveActorEffect;
            switch (strValue)
            {
                case "Script_PreRunConditional":
                    mv.script_preRunConditional = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "PullActor":
                    mv.pullActor = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Spin":
                    mv.spin = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SwapPlaces":
                    mv.swapPlaces = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Flank":
                    mv.flankingMovement = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "RandomRange":
                    mv.randomRange = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "MoveToLandingTile":
                    mv.moveToLandingTile = simpleBool[reader.ReadElementContentAsInt()];
                    readSomething = true;
                    break;
                case "ArcMult":
                    txt = reader.ReadElementContentAsString();
                    mv.arcMult = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ImpactDamageEffect":
                    mv.impactDamageEffect = reader.ReadElementContentAsString();
                    readSomething = true; // new 1/16
                    break;
                case "Script_PostMove":
                    mv.script_postMove = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "AfterImages":
                    mv.afterImages = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "MoveThroughObstacles":
                    mv.moveThroughObstacles = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Distance":
                    mv.distance = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "Direction":
                    mv.forceDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
            }
            return mv;
        }
        if (baseEffect.effectType == EffectType.CHANGESTAT)
        {
            ChangeStatEffect cs = (ChangeStatEffect)baseEffect as ChangeStatEffect;
            switch (strValue)
            {
                case "Stat":
                    cs.stat = (StatTypes)Enum.Parse(typeof(StatTypes), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "StatData":
                    cs.statData = (StatDataTypes)Enum.Parse(typeof(StatDataTypes), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "Eq":
                case "EffectEquation": // deprecate
                    cs.effectEquation = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Power":
                case "EffectPower": // deprecate
                    txt = reader.ReadElementContentAsString();
                    cs.effectPower = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FloorValue":
                    txt = reader.ReadElementContentAsString();
                    cs.floorValue = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "CeilingValue":
                    txt = reader.ReadElementContentAsString();
                    cs.ceilingValue = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ReverseOnEnd":
                    cs.reverseOnEnd = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Script_EffectModifier":
                    cs.script_effectModifier = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "ChangeSubtypes":
                    cs.changeSubtypes = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SpiritMod":
                    cs.modBySpirit = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DisciplineMod":
                    cs.modByDiscipline = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "BaseAmt":
                    cs.baseAmount = CustomAlgorithms.TryParseFloat(reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
            }
            return cs;
        }
        if (baseEffect.effectType == EffectType.EMPOWERATTACK)
        {
            EmpowerAttackEffect ea = (EmpowerAttackEffect)baseEffect as EmpowerAttackEffect;
            switch (strValue)
            {
                case "Eq":
                case "EffectEquation": // deprecate
                    ea.effectEquation = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Power":
                case "EffectPower": // deprecate
                    txt = reader.ReadElementContentAsString();
                    ea.effectPower = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AttackCondition":
                case "TriggerCondition":
                case "TCon":
                    ea.triggerCondition = (AttackConditions)Enum.Parse(typeof(AttackConditions), reader.ReadElementContentAsString());
                    ea.theConditions.Add(ea.triggerCondition);
                    readSomething = true;
                    break;
                case "MaxBonusDamage":
                    txt = reader.ReadElementContentAsString();
                    ea.maxExtraDamageAsPercent = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ScriptAttackModifier":
                    ea.script_attackModifier = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Silent":
                    ea.silentEmpower = simpleBool[reader.ReadElementContentAsInt()];
                    readSomething = true;
                    break;
                case "AlterCrit":
                    txt = reader.ReadElementContentAsString();
                    ea.extraChanceToCrit = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterCritFlat":
                    txt = reader.ReadElementContentAsString();
                    ea.extraChanceToCritFlat = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
            }
            return ea;
        }
        if (baseEffect.effectType == EffectType.ATTACKREACTION)
        {
            AttackReactionEffect area = (AttackReactionEffect)baseEffect as AttackReactionEffect;
            switch (strValue)
            {
                case "Eq":
                case "EffectEquation": // deprecate
                    area.effectEquation = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Power":
                case "EffectPower": // deprecate
                    txt = reader.ReadElementContentAsString();
                    area.effectPower = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterAccuracy":
                    txt = reader.ReadElementContentAsString();
                    area.alterAccuracy = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterParry":
                    txt = reader.ReadElementContentAsString();
                    area.alterParry = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterBlock":
                    txt = reader.ReadElementContentAsString();
                    area.alterBlock = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterAccuracyFlat":
                    txt = reader.ReadElementContentAsString();
                    area.alterAccuracyFlat = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterParryFlat":
                    txt = reader.ReadElementContentAsString();
                    area.alterParryFlat = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterBlockFlat":
                    txt = reader.ReadElementContentAsString();
                    area.alterBlockFlat = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterDamage":
                    txt = reader.ReadElementContentAsString();
                    area.alterDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterDamagePercent":
                    txt = reader.ReadElementContentAsString();
                    area.alterDamagePercent = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ReactCondition":
                    area.reactCondition = (AttackConditions)Enum.Parse(typeof(AttackConditions), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "Sscr":
                case "Script_Special":
                    area.script_Special = reader.ReadElementContentAsString();
                    SpecialEffectFunctions.CacheScript(area.script_Special);
                    readSomething = true;
                    break;
            }
            return area;
        }

        if (baseEffect.effectType == EffectType.INFLUENCETURN)
        {
            InfluenceTurnEffect ite = (InfluenceTurnEffect)baseEffect as InfluenceTurnEffect;
            switch (strValue)
            {
                case "StunChance":
                    txt = reader.ReadElementContentAsString();
                    ite.stunChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "RootChance":
                    txt = reader.ReadElementContentAsString();
                    ite.rootChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "SilenceChance":
                    txt = reader.ReadElementContentAsString();
                    ite.silenceChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ConfuseChance":
                    txt = reader.ReadElementContentAsString();
                    ite.confuseChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "SleepChance":
                    txt = reader.ReadElementContentAsString();
                    ite.sleepChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ParalyzeChance":
                    txt = reader.ReadElementContentAsString();
                    ite.paralyzeChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "CharmChance":
                    txt = reader.ReadElementContentAsString();
                    ite.charmChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FearChance":
                    txt = reader.ReadElementContentAsString();
                    ite.fearChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
            }
            return ite;
        }
        if (baseEffect.effectType == EffectType.SUMMONACTOR)
        {
            SummonActorEffect sae = baseEffect as SummonActorEffect;
            switch (strValue)
            {
                case "AType":
                case "SummonActorType":
                    sae.summonActorType = (ActorTypes)Enum.Parse(typeof(ActorTypes), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "ARef":
                case "SummonActorRef":
                    sae.summonActorRef = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "ARefs":
                case "SummonActorRefs":
                    string unparsed = reader.ReadElementContentAsString();
                    string[] parsed = unparsed.Split(',');
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        sae.possibleRefs.Add(parsed[i]);
                    }
                    readSomething = true;
                    break;
                case "SDur":
                case "SummonDuration":
                    sae.summonDuration = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "SummonOnBreakables":
                    sae.summonOnBreakables = true;
                    readSomething = true;
                    reader.Read();
                    break;
                case "SummonOnCollidable":
                    sae.summonOnCollidable = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SummonNoStacking":
                    sae.summonNoStacking = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "AllowExistingSummons":
                    sae.allowExistingSummons = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "RandomPositionsReqLineOfSight":
                    sae.randomPositionsRequireLineOfSight = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DoNotChangeFaction":
                    sae.doNotChangeFaction = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Anchor":
                    sae.anchorType = (TargetActorType)Enum.Parse(typeof(TargetActorType), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "AnchorRange":
                    sae.anchorRange = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "MaxRandomSummonRange":
                    sae.maxRandomSummonRange = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "ExpandRandomSummonRange":
                    sae.expandRandomSummonRange = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "NumRandomSummons":
                    sae.numRandomSummons = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "ScaleWithLevel":
                    sae.scaleWithLevel = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "CreateNewPositionListForRandomSummons":
                    sae.createNewPositionListForRandomSummons = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "Script_PreSummon":
                    sae.script_preSummon = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Script_PostSummon":
                    sae.script_postSummon = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "SummonOnSummoner":
                    sae.summonOnSummoner = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SummonOnWalls":
                    sae.summonOnSummoner = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DetachFromSummoner":
                    sae.detachFromSummoner = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SummonActorPerTile":
                    sae.summonActorPerTile = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "HideCharmVisual":
                    sae.hideCharmVisual = simpleBool[reader.ReadElementContentAsInt()];
                    readSomething = true;
                    break;
                case "UniqueSummon":
                    sae.uniqueSummon = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DestroySummon":
                    sae.destroySummons.Add(reader.ReadElementContentAsString());
                    break;
                case "FMaxDelayBeforeSummon":
                    string floatToParse = reader.ReadElementContentAsString();
                    sae.fMaxDelayBeforeSummon = CustomAlgorithms.TryParseFloat(floatToParse);
                    readSomething = true;
                    break;
                case "ActOnlyWithSummoner":
                    sae.actOnlyWithSummoner = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DieWithSummoner":
                    sae.dieWithSummoner = true;
                    reader.Read();
                    readSomething = true;
                    break;
            }
            return sae;
        }
        if (baseEffect.effectType == EffectType.DESTROYACTOR)
        {
            DestroyActorEffect dae = (DestroyActorEffect)baseEffect as DestroyActorEffect;
            // Switch stuff goes here
            return dae;
        }
        if (baseEffect.effectType == EffectType.SPECIAL)
        {
            SpecialEffect dae = baseEffect as SpecialEffect;
            // Switch stuff goes here
            switch (strValue)
            {
                case "Sscr":
                case "Script_Special":
                    dae.script_special = reader.ReadElementContentAsString();
                    SpecialEffectFunctions.CacheScript(dae.script_special);
                    readSomething = true;
                    break;
            }
            return dae;
        }
        if (baseEffect.effectType == EffectType.DESTROYTILE)
        {
            DestroyTileEffect dte = baseEffect as DestroyTileEffect;
            // Switch stuff goes here
            return dte;
        }
        if (baseEffect.effectType == EffectType.DAMAGE)
        {
            DamageEffect ds = baseEffect as DamageEffect;
            switch (strValue)
            {
                case "DamageType":
                    ds.damType = (DamageTypes)Enum.Parse(typeof(DamageTypes), reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "Eq":
                case "EffectEquation": // deprecate
                    ds.effectEquation = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "DamageEffectFlag":
                    DamageEffectFlags dparse = (DamageEffectFlags)Enum.Parse(typeof(DamageEffectFlags), reader.ReadElementContentAsString());
                    ds.damFlags[(int)dparse] = true;
                    break;
                case "Power":
                case "EffectPower": // deprecate
                    txt = reader.ReadElementContentAsString();
                    ds.effectPower = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FloorValue":
                    txt = reader.ReadElementContentAsString();
                    ds.floorValue = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "MissChance":
                    txt = reader.ReadElementContentAsString();
                    ds.missChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "CeilingValue":
                    txt = reader.ReadElementContentAsString();
                    ds.ceilingValue = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "CanCrit":
                    ds.canCrit = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "DamageItem":
                    ds.damageItem = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "CanBeParriedOrBlocked":
                    ds.canBeParriedOrBlocked = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "WDmt":
                case "InheritWeaponDamageType":
                    ds.inheritWeaponDamageType = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "NoDodgePossible":
                    ds.noDodgePossible = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "SCdm":
                case "Script_DamageModifier":
                    ds.script_modifyDamage = reader.ReadElementContentAsString();
                    DamageModifierFunctions.CacheScript(ds.script_modifyDamage);
                    readSomething = true;
                    break;
            }
            return ds;
        }
        if (baseEffect.effectType == EffectType.ALTERBATTLEDATA)
        {
            AlterBattleDataEffect abde = baseEffect as AlterBattleDataEffect;
            switch (strValue)
            {
                case "AlterStealth":
                    txt = reader.ReadElementContentAsString();
                    abde.alterStealthDuringCache = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterHealing":
                    txt = reader.ReadElementContentAsString();
                    abde.alterHealingDuringCache = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AlterEffectValues":
                    reader.ReadStartElement();
                    string effectRef = "";
                    float value = 0f;
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            effectRef = reader.Name;
                            string unparsed = reader.ReadElementContentAsString();
                            value = CustomAlgorithms.TryParseFloat(unparsed);
                            abde.alterEffectValues.Add(effectRef, value);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                    reader.ReadEndElement();
                    readSomething = true;
                    break;
                case "ChangeDurability":
                    abde.changeDurability = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "ChangePercentAllDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changePercentAllDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangePercentAllMitigation":
                    txt = reader.ReadElementContentAsString();
                    abde.changePercentAllMitigation = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeCritDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changeCritDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeCritChance":
                    txt = reader.ReadElementContentAsString();
                    abde.changeCritChance = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeSpiritPower":
                    txt = reader.ReadElementContentAsString();
                    abde.changeSpiritPower = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeSpiritPowerMult":
                    txt = reader.ReadElementContentAsString();
                    abde.changeSpiritPowerMult = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FireRes":
                case "ChangeFireResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changeFireResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "IceRes":
                case "ChangeWaterResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changeWaterResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "BoltRes":
                case "ChangeLightningResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changeLightningResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PhysRes":
                case "ChangePhysicalResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changePhysicalResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AcidRes":
                case "ChangePoisonResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changePoisonResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "DarkRes":
                case "ChangeShadowResist":
                    txt = reader.ReadElementContentAsString();
                    abde.changeShadowResist = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FireDmg":
                case "ChangeFireDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changeFireDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "IceDmg":
                case "ChangeWaterDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changeWaterDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "BoltDmg":
                case "ChangeLightningDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changeLightningDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PhysDmg":
                case "ChangePhysicalDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changePhysicalDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "AcidDmg":
                case "ChangePoisonDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changePoisonDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "DarkDmg":
                case "ChangeShadowDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.changeShadowDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;

                case "PierceFire":
                    txt = reader.ReadElementContentAsString();
                    abde.pierceFire = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PierceLightning":
                    txt = reader.ReadElementContentAsString();
                    abde.pierceLightning = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PiercePhysical":
                    txt = reader.ReadElementContentAsString();
                    abde.piercePhysical = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PiercePoison":
                    txt = reader.ReadElementContentAsString();
                    abde.piercePoison = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PierceShadow":
                    txt = reader.ReadElementContentAsString();
                    abde.pierceShadow = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "PierceWater":
                    txt = reader.ReadElementContentAsString();
                    abde.pierceWater = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;

                case "ChangeEnergyCosts":
                    txt = reader.ReadElementContentAsString();
                    abde.changeEnergyCosts = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeHealthCosts":
                    txt = reader.ReadElementContentAsString();
                    abde.changeHealthCosts = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ChangeStaminaCosts":
                    txt = reader.ReadElementContentAsString();
                    abde.changeStaminaCosts = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ForcedStaminaCosts":
                    txt = reader.ReadElementContentAsString();
                    abde.forcedStaminaCosts = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "ForcedEnergyCosts":
                    txt = reader.ReadElementContentAsString();
                    abde.forcedEnergyCosts = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FamilyDamage":
                    txt = reader.ReadElementContentAsString();
                    abde.familyDamage = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "FamilyDefense":
                    txt = reader.ReadElementContentAsString();
                    abde.familyDefense = CustomAlgorithms.TryParseFloat(txt);
                    readSomething = true;
                    break;
                case "MonsterFamily":
                    abde.monFamilyName = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                default:
                    //Debug.LogError("Alter Battle Data: Tag '" + strValue + "' doesn't exist.");
                    //reader.Read();
                    break;
            }
            return abde;
        }
        if (baseEffect.effectType == EffectType.ADDSTATUS)
        {
            AddStatusEffect adds = baseEffect as AddStatusEffect;
            switch (strValue)
            {
                case "Duration":
                    adds.baseDuration = reader.ReadElementContentAsInt();
                    readSomething = true;
                    break;
                case "StatusRef":
                    adds.statusRef = reader.ReadElementContentAsString();
                    readSomething = true;
                    break;
                case "Script_PreRunConditional":
                    adds.script_preRunConditional = reader.ReadElementContentAsString();
                    AddStatusCustomFunctions.CachePreStatusScript(adds.script_preRunConditional);
                    readSomething = true;
                    break;
                case "Script_ExtraPerActorFunction":
                    adds.script_extraPerActorFunction = reader.ReadElementContentAsString();
                    AddStatusCustomFunctions.CachePerActorScript(adds.script_extraPerActorFunction);
                    readSomething = true;
                    break;
            }
            return adds;
        }
        if (baseEffect.effectType == EffectType.REMOVESTATUS)
        {
            RemoveStatusEffect rems = (RemoveStatusEffect)baseEffect as RemoveStatusEffect;
            switch (strValue)
            {
                case "RemoveStatusRef":
                    rems.removableStatuses.Add(reader.ReadElementContentAsString());
                    readSomething = true;
                    break;
                case "RemoveStatusFlag":
                    StatusFlags sf = (StatusFlags)Enum.Parse(typeof(StatusFlags), reader.ReadElementContentAsString());
                    rems.removeFlags[(int)sf] = true;
                    readSomething = true;
                    break;
                case "RemoveAllNegative":
                    rems.removeAllNegative = true;
                    reader.Read();
                    readSomething = true;
                    break;
                case "RemoveAllPositive":
                    rems.removeAllPositive = true;
                    reader.Read();
                    readSomething = true;
                    break;
            }
            return rems;
        }

        if (baseEffect.effectType == EffectType.ABILITYCOSTMODIFIER || baseEffect.effectType == EffectType.SPELLSHAPE)
        {
            if (baseEffect.ReadNextNodeFromXML(reader))
            {
                readSomething = true;
            }
            else
            {
                Debug.LogError("Can't parse tag '" + strValue + "'");
                reader.Read();
            }
        }

        if (!readSomething && reader.NodeType != XmlNodeType.EndElement)
        {
            /* if (reader.NodeType != XmlNodeType.Whitespace && reader.Name != "")
            {
                Debug.Log("Couldnt understand node: " + reader.Name);
            } */
            reader.Read(); // **NEW CODE, CAREFUL**

        }
        return baseEffect;
    }

    private IEnumerator LoadAllDungeonRooms()
    {
        masterDungeonRoomlist = new Dictionary<string, RoomTemplate>();
        masterDungeonRoomsByLayout = new List<RoomTemplate>[(int)DungeonFloorTypes.COUNT];
        for (int i = 0; i < (int)DungeonFloorTypes.COUNT; i++)
        {
            masterDungeonRoomsByLayout[i] = new List<RoomTemplate>();
        }

        List<string> dungeonRoomFilesToLoad = DataLoadHelper.GetAllFilesToLoad(GameDataPaths.DUNGEONROOMS);
        PlayerModManager.AddModFilesToList(dungeonRoomFilesToLoad, PlayerModfileTypes.DUNGEONROOMS);

        foreach (string roomFile in dungeonRoomFilesToLoad)
        {
            if (string.IsNullOrEmpty(roomFile)) continue;
            using (XmlReader reader = XmlReader.Create(new StringReader(roomFile)))
            {
                reader.Read();
                float timeAtLastYield = Time.realtimeSinceStartup;
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.Name == "Room")
                    {
                        RoomTemplate rt = new RoomTemplate();
                        try { rt.ReadFromXml(reader); }
                        catch (Exception e)
                        {
                            Debug.LogError("Error reading a room template from file: " + e);
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                    if (Time.realtimeSinceStartup - timeAtLastYield >= MIN_FPS_DURING_LOAD)
                    {
                        yield return null;
                        timeAtLastYield = Time.realtimeSinceStartup;
                    }
                }
                reader.ReadEndElement();
            }
        }

        yield return null;
    }

    private void ReadStatBuff(StatusEffect se, XmlReader reader)
    {
        // We have come to the field labeled "StatBuff", let's build a custom effect from this.

        string strValue = reader.ReadElementContentAsString();
        // format is: EFFECT_REF_NAME|STAT|AMOUNT

        string[] parsed = strValue.Split('|');

        if (parsed.Length != 3)
        {
            Debug.Log("Error parsing stat buff: " + strValue);
            return;
        }

        if (!Enum.TryParse(parsed[1], out StatTypes stat))
        {
            Debug.Log("Error parsing stat buff stat type: " + strValue);
            return;
        }

        if (!int.TryParse(parsed[2], out int amount))
        {
            Debug.Log("Error parsing stat buff amount: " + strValue);
            return;
        }

        ChangeStatEffect cse = new ChangeStatEffect();
        cse.effectRefName = parsed[0];
        cse.tActorType = TargetActorType.SELF;
        cse.effectType = EffectType.CHANGESTAT;
        cse.changeSubtypes = true;
        cse.silent = true;
        cse.reverseOnEnd = true;
        cse.statData = StatDataTypes.MAX;
        cse.stat = stat;
        cse.effectPower = amount;
        cse.anyDamageEquationVars = true;

        se.AddEffectScript(cse);
        cse.parentAbility = se;

        if (masterEffectList.ContainsKey(cse.effectRefName))
        {
            Debug.Log("WARNING! Effect name " + cse.effectName + " REFNAME " + cse.effectRefName + " already exists.");
        }
        else
        {
            masterEffectList.Add(cse.effectRefName, cse);
        }        

    }
}
