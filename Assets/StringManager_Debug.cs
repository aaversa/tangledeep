using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.Serialization;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Globalization;
using Debug = UnityEngine.Debug;

public partial class StringManager
{ 

    public static Dictionary<string, string> Debug_GetDictionaryWithMiscAndLocalizationCombined()
    {
        var dictRet = new Dictionary<string, string>();

        //add in everything from the localization dictionary, as it is authoritative
        foreach (var kvp in dictStringsByLanguage[EGameLanguage.en_us])
        {
            dictRet[kvp.Key] = kvp.Value;
        }

        //now do misc strings
        string r = '\n'.ToString();

        foreach (var kvp in dictMiscStrings)
        {
            string strValue = kvp.Value.Replace("&#xA;", "\\n");
            strValue = strValue.Replace(r, "\\n");

            if (strValue.Contains('\n') ||
                strValue.Contains('\t') ||
                strValue.Contains('\r'))
            {
                Debugger.Break();
            }

            if (!dictRet.ContainsKey(kvp.Key))
            {
                dictRet[kvp.Key] = strValue;
            }
            else
            {
                DebugConsole.Log("Rejected " + kvp.Key + " from misc strings.");
            }
        }

        return dictRet;
    }

    public static object Debug_ConvertHardcodeInAbilitiesAndEffectsToLocalizedTags(params string[] args)
    {
        GameMasterScript.StartWatchedCoroutine(Debug_ConvertHardcodeInAbilitiesAndEffectsToLocalizedTags_Coroutine());

        return "Starting Localization Conversion!";
    }

    public static IEnumerator Debug_ConvertHardcodeInAbilitiesAndEffectsToLocalizedTags_Coroutine()
    {
        DebugConsole.Log("Converting misc_strings to localized format");
        yield return null;

        var dictMiscToLocal = Debug_GetDictionaryWithMiscAndLocalizationCombined();
        dictUniqueTextToTags = new Dictionary<string, string>();

        //reverse the misc-to-local so we track unique unlocalized text
        foreach (var kvp in dictMiscToLocal)
        {
            dictUniqueTextToTags[kvp.Value] = kvp.Key;
        }


        DebugConsole.Log("Done, on to converting game data...");
        yield return null;

        var helperList = new List<HardCodeToTagConversionHelper>();
        var dictFiles = new Dictionary<string, string>();
        foreach (var textAsset in GameMasterScript.gmsSingleton.abilityXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.dialogXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.championDataXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.itemXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.magicmodXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.dungeonXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.jobXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.mapObjectXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.monsterXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.statusXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.roomXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        foreach (var textAsset in GameMasterScript.gmsSingleton.npcXML)
        {
            dictFiles.Add(textAsset.name, textAsset.text);
        }
        //all abilities
        foreach (var kvp in GameMasterScript.masterAbilityList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var abil = kvp.Value;

            conversion.prefix = "abil_" + abil.refName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("DisplayName", abil.abilityName, "name", true);
            conversion.AddConversionIfNeeded("CombatLogText", abil.combatLogText, "clt");
            conversion.AddConversionIfNeeded("Description", abil.unbakedDescription, "desc");
            conversion.AddConversionIfNeeded("ExtraDescription", abil.unbakedExtraDescription, "extradesc");
            conversion.AddConversionIfNeeded("ShortDescription", abil.unbakedShortDescription, "shortdesc");
            conversion.AddConversionIfNeeded("ChargeText", abil.chargeText, "ctxt");

            helperList.Add(conversion);
        }

        DebugConsole.Log("Abilities converted!");
        yield return null;

        //all items
        foreach (var kvp in GameMasterScript.masterItemList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var itam = kvp.Value;

            conversion.prefix = "item_" + itam.actorRefName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("DisplayName", itam.displayName, "name", true);
            conversion.AddConversionIfNeeded("Description", itam.unbakedDescription, "desc");
            conversion.AddConversionIfNeeded("ExtraDescription", itam.unbakedExtraDescription, "extradesc");
            Consumable c = itam as Consumable;
            if (c != null)
            {
                conversion.AddConversionIfNeeded("EffectDescription", c.unbakedEffectDescription, "effectdesc");
            }

            helperList.Add(conversion);
        }

        DebugConsole.Log("Items converted!");
        yield return null;


        //effects
        foreach (var kvp in GameMasterScript.masterEffectList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var effect = kvp.Value;

            conversion.prefix = "effect_" + effect.effectRefName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("EffectName", effect.effectName, "name", true);
            conversion.AddConversionIfNeeded("BattleText", effect.battleText, "btxt");
            helperList.Add(conversion);
        }

        DebugConsole.Log("EffectScripts converted!");
        yield return null;


        //status effects, which are abilities 
        foreach (var kvp in GameMasterScript.masterStatusList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var status = kvp.Value;

            conversion.prefix = "status_" + status.refName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("DisplayName", status.abilityName, "name", true);
            conversion.AddConversionIfNeeded("CombatLogText", status.combatLogText, "clt");
            conversion.AddConversionIfNeeded("Description", status.unbakedDescription, "desc");
            conversion.AddConversionIfNeeded("ExtraDescription", status.unbakedExtraDescription, "extradesc");
            conversion.AddConversionIfNeeded("ShortDescription", status.unbakedShortDescription, "shortdesc");
            conversion.AddConversionIfNeeded("ChargeText", status.chargeText, "ctxt");

            helperList.Add(conversion);
        }

        DebugConsole.Log("StatusEffect converted!");
        yield return null;

        //quipz
        int idxQuips = 0;
        foreach (var quippo in GameMasterScript.masterMonsterQuipList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();

            conversion.prefix = "quip_";
            conversion.AddConversionIfNeeded("Text", quippo.text, "txt_" + idxQuips++);
            helperList.Add(conversion);
        }

        //monsters and NPCs
        foreach (var kvp in GameMasterScript.masterMonsterList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var mon = kvp.Value;
            conversion.prefix = "mon_" + mon.refName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("Name", mon.monsterName, "name", true);
            helperList.Add(conversion);
        }

        DebugConsole.Log("Monsters converted!");
        yield return null;

        foreach (var kvp in GameMasterScript.masterNPCList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var mans = kvp.Value;
            conversion.prefix = "npc_" + mans.actorRefName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("DisplayName", mans.displayName, "name", true);
            helperList.Add(conversion);
        }

        DebugConsole.Log("NPCs converted!");
        yield return null;

        //jobs
        foreach (var job in GameMasterScript.masterJobList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            conversion.prefix = "job_" + job.jobName;
            conversion.AddConversionIfNeeded("DisplayName", job.GetBaseDisplayName(), "dname", true);
            conversion.AddConversionIfNeeded("Description", job.jobDescription, "desc0", true);
            conversion.AddConversionIfNeeded("BonusDescription1", job.unbakedBonusDescription1, "desc1");
            conversion.AddConversionIfNeeded("BonusDescription2", job.unbakedBonusDescription2, "desc2");
            conversion.AddConversionIfNeeded("BonusDescription3", job.unbakedBonusDescription3, "desc3");
            helperList.Add(conversion);

            int idxJobAbilities = 0;
            foreach (var jobabil in job.JobAbilities)
            {
                conversion = new StringManager.HardCodeToTagConversionHelper();
                conversion.prefix = "jabil_" + job.jobName + "_" + idxJobAbilities++;
                conversion.AddConversionIfNeeded("AbilityName", jobabil.GetNameForUI(), "aname", false);
                helperList.Add(conversion);
            }

        }



        //feats
        foreach (var feat in GameMasterScript.masterFeatList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            conversion.prefix = "feat_" + feat.featName.ToLowerInvariant();
            conversion.AddConversionIfNeeded("FeatName", feat.featName, "fname");
            conversion.AddConversionIfNeeded("Description", feat.description, "desc");
            helperList.Add(conversion);
        }

        //objects
        foreach (var kvp in GameMasterScript.masterMapObjectDict)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var des = kvp.Value;

            conversion.prefix = "des_" + des.actorRefName;
            conversion.AddConversionIfNeeded("DisplayName", des.displayName, "dname", true);
            helperList.Add(conversion);
        }


        //monmods
        foreach (var kvp in GameMasterScript.masterChampionModList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var monmod = kvp.Value;

            conversion.prefix = "monmod_" + monmod.refName;
            conversion.AddConversionIfNeeded("ModName", monmod.displayName, "mname", true);
            helperList.Add(conversion);
        }

        //champ data 
        foreach (var kvp in GameMasterScript.masterChampionDataDict)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var champ = kvp.Value;

            conversion.prefix = "champ_" + kvp.Key;
            int idx = 0;
            champ.name1.ForEach(s => conversion.AddConversionIfNeeded("Name1", s, "name_" + (idx++)));
            champ.name2.ForEach(s => conversion.AddConversionIfNeeded("Name2", s, "name_" + (idx++)));
            champ.name3.ForEach(s => conversion.AddConversionIfNeeded("Name3", s, "name_" + (idx++)));
            champ.name4.ForEach(s => conversion.AddConversionIfNeeded("Name4", s, "name_" + (idx++)));
            champ.name5.ForEach(s => conversion.AddConversionIfNeeded("Name5", s, "name_" + (idx++)));
            champ.name6.ForEach(s => conversion.AddConversionIfNeeded("Name6", s, "name_" + (idx++)));

            helperList.Add(conversion);
        }

        //maps
        foreach (var kvp in GameMasterScript.masterDungeonLevelList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var floor = kvp.Value;
            conversion.prefix = "floor_" + floor.floor;
            conversion.AddConversionIfNeeded("CustomName", floor.customName, "cname", true);
            conversion.AddConversionIfNeeded("Name", floor.overlayDisplayName, "oname", true);
            conversion.AddConversionIfNeeded("Text", floor.overlayText, "otxt", true);
            helperList.Add(conversion);
        }

        DebugConsole.Log("Floors converted!");
        yield return null;

        // #TODO - Gear set text
        // #TODO - Recipe text


        foreach (var kvp in GameMasterScript.masterMagicModList)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var mm = kvp.Value;
            conversion.prefix = "mmod_" + mm.refName;
            conversion.AddConversionIfNeeded("ModName", mm.modName, "name", true);
            conversion.AddConversionIfNeeded("Description", mm.unbakedDescription, "desc", false); // WAS mm.description
            //BackupDescription is never displayed in game
            helperList.Add(conversion);
        }

        //yield return new WaitForSeconds(60f);

        DebugConsole.Log("MagicMods converted!");
        yield return null;

        //conversations!
        var conversationDict = new Dictionary<string, Conversation>();
        conversationDict = GameMasterScript.masterConversationList;
        foreach (var journalConvo in GameMasterScript.masterJournalEntryList)
        {
            conversationDict[journalConvo.refName] = journalConvo;
        }

        foreach (var kvp in conversationDict)
        {
            var conversion = new StringManager.HardCodeToTagConversionHelper();
            var dialogConversation = kvp.Value;

            conversion.prefix = "dialog_" + dialogConversation.refName.ToLowerInvariant();

            //only used if the branch has no name
            int idxBranch = 0;

            //get every branch, and all the things we say
            foreach (var branch in dialogConversation.allBranches)
            {
                string strBranchRef;

                if (branch.branchRefName == null)
                {
                    strBranchRef = "branch_" + idxBranch;
                    idxBranch++;
                }
                else
                {
                    strBranchRef = branch.branchRefName.ToLowerInvariant();
                }

                conversion.AddConversionIfNeeded("Text", branch.text, strBranchRef + "_txt");

                //and every button in response to the branch. Name is also the text, so we can't use that as a reference
                int idxButton = 0;
                foreach (var button in branch.responses)
                {
                    conversion.AddConversionIfNeeded("Name", button.buttonText, strBranchRef + "_btn_" + idxButton);
                    idxButton++;
                }
            }

            helperList.Add(conversion);
        }

        DebugConsole.Log("Conversations converted!");
        DebugConsole.Log("The slow part, rebuilding files.");
        yield return null;

        string strNewDirectory = "LocalizationConversions_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" +
                                 DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" +
                                 DateTime.Now.Second;

        Debug.Log("CreateDirectory string debug");

        string strPathToNewDir = Path.Combine(CustomAlgorithms.GetPersistentDataPath(), strNewDirectory);        

        Directory.CreateDirectory(strPathToNewDir);

        //recall that in AddConversionIfNeeded, we've already adjust for text that is duplicated
        //and we should not use more than one tag for each entry. 

        //in every file
        foreach (var kvp in dictFiles)
        {
            string strData = kvp.Value;

            //write it out.
            DebugConsole.Log("Writing " + kvp.Key + ".xml");
            yield return null;

            //remove all jank
            strData = strData.Replace("&#xA;", "\\n");
            strData = strData.Replace("##str,", "");
            strData = CustomAlgorithms.ParseRichText(strData, true);

            //for every ability
            foreach (var helper in helperList)
            {
                //and every field...
                foreach (var c in helper.listConversions)
                {
                    string strStartTag = "<" + c.strFieldName + ">";
                    string strEndTag = "</" + c.strFieldName + ">";

                    /* if (c.strUnlocalizedText.Contains("^number"))
                    {
                        Debugger.Break();
                    } */

                    strData = strData.Replace(
                        strStartTag + c.strUnlocalizedText + strEndTag,
                        strStartTag + c.strLocalizedTag + strEndTag);
                }
            }

            string strPath = Path.Combine(strPathToNewDir, kvp.Key + ".xml");
            using (StreamWriter sw = new StreamWriter(new FileStream(strPath, FileMode.Create)))
            {
                sw.Write(strData);
                sw.Close();
            }
        }

        DebugConsole.Log("Writing to a new add_me_to_en_us.txt...");
        yield return null;

        //write out to a new en_us addon. Don't write duplicate keys / tags
        HashSet<string> hashAlreadyWrittenKeys = new HashSet<string>();



        string finalPath = Path.Combine(strPathToNewDir, "en_us.txt");
        using (StreamWriter sw = new StreamWriter(new FileStream(finalPath, FileMode.Create)))
        {
            foreach (var kvp in dictUniqueTextToTags)
            {
                sw.WriteLine(kvp.Value + "\t" + kvp.Key);
            }
            /*
            foreach (var helper in helperList)
            {
                //and every field...
                foreach (var c in helper.listConversions)
                {
                    if (!hashAlreadyWrittenKeys.Contains(c.strLocalizedTag))
                    {
                        sw.WriteLine(c.strLocalizedTag + "\t" + c.strUnlocalizedText);
                        hashAlreadyWrittenKeys.Add(c.strLocalizedTag);
                    }
                }
            }
            */
            sw.Close();
        }

        DebugConsole.Log("Writing only_words_for_counting.txt...");
        yield return null;

        finalPath = Path.Combine(strPathToNewDir, "only_words_for_counting.txt");
        hashAlreadyWrittenKeys = new HashSet<string>();
        using (StreamWriter sw = new StreamWriter(new FileStream(finalPath, FileMode.Create)))
        {
            foreach (var kvp in dictUniqueTextToTags)
            {
                string s = DisenjankenString(kvp.Key, true);
                sw.WriteLine(s);
            }
            /*
            foreach (var helper in helperList)
            {
                //and every field...
                foreach (var c in helper.listConversions)
                {
                    if (!hashAlreadyWrittenKeys.Contains(c.strLocalizedTag))
                    {
                        //for words only, remove all < > and ^ ^ tags
                        string s = DisenjankenString(c.strUnlocalizedText, true);
                        sw.WriteLine(s);

                        //store the undisenjankened version 
                        hashAlreadyWrittenKeys.Add(c.strLocalizedTag);
                    }
                }
            }
            */
            sw.Close();
        }


        DebugConsole.Log("Done!");

    }
}
