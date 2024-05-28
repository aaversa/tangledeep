using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public partial class Monster
{
    static List<ChampionMod> mcPossibleMods;

    public void MakeChampion(bool markAsChampionAndChangeStats = true)
    {
        if (myTemplate.cannotBeChampion)
        {
            //Debug.Log(actorRefName + " should not be champion");
            if (!markAsChampionAndChangeStats)
            {
                return;
            }
        }

        if (mcPossibleMods == null) mcPossibleMods = new List<ChampionMod>();
        mcPossibleMods.Clear();


        // If we are a monster in Realm of the Gods, we use special ROG-only mods to kick ourselves off
        if (!isChampion && (dungeonFloor >= MapMasterScript.REALM_OF_GODS_START && dungeonFloor <= MapMasterScript.REALM_OF_GODS_END) ||
            ReadActorData("realmgod") == 1 && ReadActorData("godchampion") != 1)
        {
            SetActorData("godchampion", 1);
            ChampionMod newCM = GameMasterScript.masterChampionModList[ChampionMod.godMods.GetRandomElement()];
            MakeChampionFromMod(newCM, addMod: true, markAsChampionAndChangeStats: markAsChampionAndChangeStats);
            //Debug.Log("!!!Added " + newCM.refName + " to " + actorUniqueID);
            return;
        }

        // if we are a champion in Realm of the Gods, then we get special modifier(s)
        if (ReadActorData("godchampion") == 1)
        {
            ChampionMod newCM = GameMasterScript.masterChampionModList[ChampionMod.modsForGodRealm.GetRandomElement()];
            while (HasChampionMod(newCM.refName))
            {
                newCM = GameMasterScript.masterChampionModList[ChampionMod.modsForGodRealm.GetRandomElement()];
            }
            MakeChampionFromMod(newCM, addMod: true, markAsChampionAndChangeStats: markAsChampionAndChangeStats);
            //Debug.Log("Added " + newCM.refName + " to " + actorUniqueID);
            return;
        }

        bool[] exclusions = new bool[10]; // Number of champ mod exclusion groups
        if (championMods != null)
        {
            for (int c = 0; c < championMods.Count; c++)
            {
                if (championMods[c].exclusionGroup > 0)
                {
                    exclusions[championMods[c].exclusionGroup] = true;
                }
            }
        }
        foreach (ChampionMod cm in GameMasterScript.masterChampionModList.Values)
        {
            bool hasMod = false;
            if (exclusions[cm.exclusionGroup])
            {
                continue;
            }
            if (championMods != null)
            {
                for (int c = 0; c < championMods.Count; c++)
                {
                    if (championMods[c].refName == cm.refName)
                    {
                        hasMod = true;
                        break;
                    }
                }
            }

            if (cm.challengeValue <= challengeValue && challengeValue <= cm.maxChallengeValue && !hasMod)
            {
                if (cm.shadowKingOnly) continue;
                if (cm.memoryKingOnly) continue;
                if (cm.newGamePlusOnly && GameStartData.NewGamePlus == 0) continue;
                mcPossibleMods.Add(cm);
            }
        }

        if (mcPossibleMods.Count == 0)
        {
            Debug.Log(actorRefName + " champ attempt with cur mods " + championMods.Count + " cv " + challengeValue + " no CMs available");
        }
        else
        {
            ChampionMod newCM = mcPossibleMods[UnityEngine.Random.Range(0, mcPossibleMods.Count)];
            MakeChampionFromMod(newCM, addMod: true, markAsChampionAndChangeStats: markAsChampionAndChangeStats);
        }
    }

    public static ChampionMod FindMod(string modName)
    {
        ChampionMod outMod;

        if (GameMasterScript.masterChampionModList.TryGetValue(modName, out outMod))
        {
            return outMod;
        }
        else
        {
            Debug.Log("Could not find champ mod " + modName);
            return null;
        }
    }

    public void AddChampPowers(ChampionMod newCM)
    {
        if (monsterPowers == null)
        {
            monsterPowers = new List<MonsterPowerData>();
        }

        foreach (MonsterPowerData mpd in newCM.modPowers)
        {
            AbilityScript template = mpd.abilityRef;
            //This might happen if SerializedAbilities haven't been updated
            //usually because of new content merged in from the Steam Build
            if (template == null)
            {
                continue;
            }
            AbilityScript newAbil = new AbilityScript();
            newAbil.SetUniqueIDAndAddToDict();
            //Debug.Log(newAbil.refName + " " + newAbil.uniqueID);
            AbilityScript.CopyFromTemplate(newAbil, template);
            MonsterPowerData newMPD = new MonsterPowerData();
            newMPD.CopyFromTemplate(mpd, newAbil);
            monsterPowers.Add(newMPD);

            OnMonsterPowerAdded(newMPD, newAbil);

            myAbilities.AddNewAbility(newAbil, true);
        }
    }

    public bool HasChampionMod(string modRef)
    {
        if (!isChampion) return false;
        foreach (ChampionMod cd in championMods)
        {
            if (cd.refName == modRef) return true;
        }
        return false;
    }

    public void MakeChampionFromMod(ChampionMod newCM, bool addMod = true, bool markAsChampionAndChangeStats = true)
    {
        bool alreadyChampion = isChampion;

        if (markAsChampionAndChangeStats)
        {
            isChampion = true;
        }


        if (championMods == null)
        {
            championMods = new List<ChampionMod>();
        }

        if (!alreadyChampion && markAsChampionAndChangeStats)
        {
            allDamageMultiplier += 0.1f;
            allMitigationAddPercent -= 0.05f; // was additive? // Remove the 1
            xpMod *= 1.75f;
            myStats.ChangeStat(StatTypes.ENERGY, 1.0f, StatDataTypes.ALL, true);
            myStats.ChangeStat(StatTypes.STAMINA, 1.0f, StatDataTypes.ALL, true);

            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.5f);// + (0.25f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.STAMINA, 0.25f);// + (0.15f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.ENERGY, 0.25f);// + (0.15f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.STRENGTH, 0.2f);// + (0.1f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.SWIFTNESS, 0.2f);// + (0.1f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.GUILE, 0.2f);// + (0.1f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.DISCIPLINE, 0.2f);// + (0.1f * magicMods.Count));
            myStats.BoostStatByPercent(StatTypes.SPIRIT, 0.2f);// + (0.1f * magicMods.Count));
            myStats.SetLevel(myStats.GetLevel() + 1);//+(1*magicMods.Count));
            StatusEffect championSE = new StatusEffect();

            championSE.CopyStatusFromTemplate(GameMasterScript.FindStatusTemplateByName("championmonster"));
            myStats.AddStatus(championSE, this);
        }

        // Determine name!

        string origName = myTemplate.monsterName;

        if (markAsChampionAndChangeStats)
        {

            switch (championMods.Count)
            {
                case 0:
                    displayName = StringManager.GetString("champion_difficulty_1") + " ";
                    break;
                case 1:
                    displayName = StringManager.GetString("champion_difficulty_2") + " ";
                    break;
                case 2:
                    displayName = StringManager.GetString("champion_difficulty_3") + " ";
                    break;
                case 3:
                    displayName = StringManager.GetString("champion_difficulty_4") + " ";
                    break;
            }
        }

        // Hardcode stuff here???

        AddChampPowers(newCM);

        if (newCM.accessoryRef != null && newCM.accessoryRef != "")
        {
            Accessory acc = GameMasterScript.GetItemFromRef(newCM.accessoryRef) as Accessory;
            if (acc != null)
            {
                Accessory newItem = new Accessory();
                newItem.CopyFromItem(acc);
                newItem.SetUniqueIDAndAddToDict();
                if (myEquipment.equipment[(int)EquipmentSlots.ACCESSORY] != null)
                {
                    myEquipment.Equip(newItem, SND.SILENT, 1, true);
                }
                else
                {
                    myEquipment.Equip(newItem, SND.SILENT, 0, true);
                }

            }
        }

        championMods.Add(newCM);

        if (markAsChampionAndChangeStats)
        {
            string newName = GetChampionName(origName);

            displayName = "<color=yellow>" + newName + "</color>";
        }


        //Debug.Log(displayName);


        /* if (magicMods == null)
        {
            magicMods = new List<MagicMod>();
        }
    	List<MagicMod> possibleMods = new List<MagicMod>();
    	for (int i = 0; i < GameMasterScript.masterMagicModList.Count; i++) {
    		MagicMod mm = GameMasterScript.masterMagicModList[i];
    		if ((mm.challengeValue <= challengeValue) && (mm.monsterAllowed)) {
    			possibleMods.Add(mm);
    		}
    	}
    	if (possibleMods.Count == 0) {
    		Debug.Log("No monster magic mods available.");
    		return;
    	}
		MagicMod newMod = possibleMods[UnityEngine.Random.Range(0,possibleMods.Count)];
		while (magicMods.Contains(newMod)) {
			newMod = possibleMods[UnityEngine.Random.Range(0,possibleMods.Count)];
		}
		magicMods.Add(newMod);
		// How do we apply this to the monster?

		for (int m = 0; m < newMod.modEffects.Count; m++) {
			StatusEffect se = new StatusEffect();
			se.CopyStatusFromTemplate(newMod.modEffects[m]);                          
            myStats.AddStatus(se,this);
		}

		if (newMod.prefix) {
			displayName = newMod.modName + " " + displayName;
			numModPrefix++;
		}
		else {
			if (numModSuffix == 0) {
				displayName += " of " + newMod.modName;
			}
			else {
				displayName += " and " + newMod.modName;
			}
			numModSuffix++;
		} 	*/

        // Increase monster rewards

        // Increase raw stats

        if (markAsChampionAndChangeStats)
        {
            xpMod += 0.15f;
            challengeValue += 0.05f; // This is hacky
            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.12f);
            myStats.BoostStatByPercent(StatTypes.STAMINA, 0.15f);
            myStats.BoostStatByPercent(StatTypes.ENERGY, 0.15f);
            myStats.BoostStatByPercent(StatTypes.STRENGTH, 0.05f);
            myStats.BoostStatByPercent(StatTypes.SWIFTNESS, 0.05f);
            myStats.BoostStatByPercent(StatTypes.GUILE, 0.05f);
            myStats.BoostStatByPercent(StatTypes.DISCIPLINE, 0.05f);
            myStats.BoostStatByPercent(StatTypes.SPIRIT, 0.05f);
            myStats.BoostStatByPercent(StatTypes.ACCURACY, 0.05f);
            myStats.ChangeStat(StatTypes.VISIONRANGE, 1f, StatDataTypes.ALL, true);
            myStats.SetLevel(myStats.GetLevel() + 1);
            allDamageMultiplier += 0.02f;
            allMitigationAddPercent -= 0.005f; // was additive?

            if (challengeValue >= 1.4f)
            {
                allDamageMultiplier += 0.02f;
            }

            Item itm = LootGeneratorScript.GenerateLoot(challengeValue, 1.0f + (0.25f * championMods.Count));
            if (itm != null)
            {
                myInventory.AddItem(itm, false);
            }
        }
        /* if (myInventory.GetInventory().Count > 0) {
        	for (int x = 0; x < myInventory.GetInventory().Count; x++) {
        		Item thisItem = myInventory.GetInventory()[x];
				if ((thisItem.itemType  == ItemTypes.WEAPON) || (thisItem.itemType  == ItemTypes.ARMOR) || (thisItem.itemType  == ItemTypes.ACCESSORY)) {
					// Can add to this
					Equipment eq = thisItem as Equipment;
					if ((newMod.slot == eq.slot) || (newMod.slot == EquipmentSlots.ANY)) {
						EquipmentBlock.MakeMagicalFromMod(thisItem,newMod, true);
						break;
					}
				}
        	}
		} */

    }
    public string GetChampionName(string origName)
    {
        string returnName = "";
        if (monFamily != "" && monFamily != null)
        {
            //Debug.Log("Try make champion from " + monFamily + " floor " + dungeonFloor);
            ChampionData cd = GameMasterScript.masterChampionDataDict[monFamily.ToLowerInvariant()];
            int nameRoll = 0;
            switch (monFamily)
            {
                case "jelly":
                    string additive = "";
                    if (UnityEngine.Random.Range(0, 3) == 1)
                    {
                        returnName += cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                    }
                    if (UnityEngine.Random.Range(0, 3) == 1 && StringManager.gameLanguage == EGameLanguage.en_us)
                    {
                        additive = "s";
                    }
                    nameRoll = UnityEngine.Random.Range(0, 4);
                    switch (nameRoll)
                    {
                        case 0:
                            returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)];
                            break;
                        case 1:
                            returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                            break;
                        case 2:
                            returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                            break;
                        case 3:
                            returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + additive;
                            break;
                    }

                    if (UnityEngine.Random.Range(0, 6) == 0)
                    {
                        returnName = cd.name4[UnityEngine.Random.Range(0, cd.name4.Count)];
                    }

                    break;
                case "hybrids":
                case "spirits":
                    if (UnityEngine.Random.Range(0, 9) == 0)
                    {
                        returnName = cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                    }
                    else
                    {
                        if (StringManager.gameLanguage == EGameLanguage.es_spain)
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0)
                            {
                                // Salamander (Doombringer)
                                returnName = origName + " (" + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + ")";
                            }
                            else
                            {
                                // Doombringer II
                                returnName = cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                            }
                        }
                        else
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0)
                            {
                                if (StringManager.gameLanguage != EGameLanguage.jp_japan)
                                {
                                    returnName += origName + ", the " + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)];
                                }
                                else
                                {
                                    returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + origName;
                                }

                            }
                            else
                            {
                                returnName += origName + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + ", the " + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)];
                            }
                        }


                    }

                    break;
                case "beasts":
                    if (UnityEngine.Random.Range(0, 2) == 1)
                    {
                        switch (StringManager.gameLanguage)
                        {
                            case EGameLanguage.de_germany:
                                string lowerName = Char.ToLowerInvariant(origName[0]) + origName.Substring(1);
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + lowerName;
                                break;
                            case EGameLanguage.es_spain:
                                returnName = origName + " (" + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + ")";
                                break;
                            default:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + origName;
                                break;
                        }
                    }
                    else
                    {
                        if (StringManager.gameLanguage == EGameLanguage.es_spain)
                        {
                            returnName += cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                        }
                        else
                        {
                            returnName += cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + ", the " + origName;
                        }

                    }
                    break;
                case "insects":
                case "frogs":
                case "snakes":
                    switch (StringManager.gameLanguage)
                    {
                        case EGameLanguage.de_germany:
                            returnName += origName + " (" + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + ")";
                            break;
                        default:
                            returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + origName;
                            break;
                    }

                    break;
                case "robots":
                    nameRoll = UnityEngine.Random.Range(0, 2);
                    if (nameRoll == 0)
                    {
                        switch (StringManager.gameLanguage)
                        {
                            case EGameLanguage.de_germany:
                                returnName += origName + " (" + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + ")";
                                break;
                            case EGameLanguage.es_spain:
                                returnName += origName + " " + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)];
                                break;
                            default:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + origName;
                                break;
                        }
                    }
                    else if (nameRoll == 1)
                    {
                        returnName += cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + ", the " + origName;
                    }
                    break;
                case "bandits":
                    if (UnityEngine.Random.Range(0, 4) == 1)
                    {
                        returnName += cd.name4[UnityEngine.Random.Range(0, cd.name4.Count)] + " ";
                    }

                    if (StringManager.gameLanguage == EGameLanguage.es_spain)
                    {
                        nameRoll = UnityEngine.Random.Range(0, 2);
                        switch (nameRoll)
                        {
                            case 0:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                                break;
                            case 1:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + ", el " + cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                                break;
                            case 2:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + ", el " + cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                                break;
                        }
                    }
                    else
                    {
                        nameRoll = UnityEngine.Random.Range(0, 4);
                        switch (nameRoll)
                        {
                            case 0:
                                returnName += cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)] + " " + cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)];
                                returnName = StringManager.RemoveGermanArticles(returnName);
                                break;
                            case 1:
                                returnName += cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                                returnName = StringManager.RemoveGermanArticles(returnName);
                                break;
                            case 2:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)];
                                break;
                            case 3:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " the " + cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                                break;
                            case 4:
                                returnName += cd.name1[UnityEngine.Random.Range(0, cd.name1.Count)] + " " + cd.name2[UnityEngine.Random.Range(0, cd.name2.Count)] + " the " + cd.name3[UnityEngine.Random.Range(0, cd.name3.Count)];
                                break;
                        }
                    }


                    if (UnityEngine.Random.Range(0, 5) == 1) // One-off name.
                    {
                        returnName = cd.name5[UnityEngine.Random.Range(0, cd.name5.Count)];
                    }
                    break;

            }

        }
        else
        {
            Debug.Log("Error making champion name for " + actorRefName + " - family is null");
        }

        if (StringManager.gameLanguage != EGameLanguage.en_us)
        {
            returnName = returnName.Replace(", the ", String.Empty);
            returnName = returnName.Replace(" the ", String.Empty);
        }

        return returnName;
    }

    static List<ChampionMod> extraModsPossible;
    static List<string> extraMods;

    static bool elementalBossStuffInitialized;
    static void InitializeElementalBossStuff()
    {
        if (elementalBossStuffInitialized) return;

        extraModsPossible = new List<ChampionMod>();
        extraMods = new List<string>();
    }

    public void MakeElementalBoss(DamageTypes dType)
    {
        InitializeElementalBossStuff();

        string essentialMod = "";
        string essentialStatus = "";

        int pLevelToUse = GameMasterScript.heroPCActor.myStats.GetLevel() - 1;
        if (pLevelToUse < 1) pLevelToUse = 1;

        ScaleToSpecificLevel(BalanceData.monsterLevelByPlayerLevel[GameMasterScript.heroPCActor.myStats.GetLevel()], false);

        extraMods.Clear();
        extraModsPossible.Clear();

        string itemToGrant = "";

        switch (dType)
        {
            case DamageTypes.FIRE:
                essentialMod = "monmod_fireabsorb";
                extraMods.Add("monmod_blazing");
                extraMods.Add("monmod_fireburst");
                extraMods.Add("monmod_explosive");
                extraMods.Add("monmod_detonating");
                essentialStatus = "status_mmfirering";
                itemToGrant = "scroll_firekingbuff";
                break;
            case DamageTypes.SHADOW:
                essentialMod = "monmod_shadowabsorb";
                extraMods.Add("monmod_nightmare");
                extraMods.Add("monmod_truenightmare");
                extraMods.Add("monmod_vampire");
                essentialStatus = "status_mmshadowring";
                itemToGrant = "scroll_shadowkingbuff";
                break;
            case DamageTypes.WATER:
                essentialMod = "monmod_waterabsorb";
                extraMods.Add("monmod_frozen");
                extraMods.Add("monmod_icedaggers");
                extraMods.Add("monmod_chilled");
                essentialStatus = "status_mmicering";
                itemToGrant = "scroll_waterkingbuff";
                break;
            case DamageTypes.LIGHTNING:
                essentialMod = "monmod_lightningabsorb";
                extraMods.Add("monmod_electrified");
                extraMods.Add("monmod_shocking");
                extraMods.Add("monmod_blinking");
                essentialStatus = "status_mmlightningring";
                itemToGrant = "scroll_lightningkingbuff";
                break;
            case DamageTypes.POISON:
                essentialMod = "monmod_poisonabsorb";
                extraMods.Add("monmod_toxic");
                extraMods.Add("monmod_plague");
                extraMods.Add("monmod_acid");
                essentialStatus = "status_mmpoisonring";
                itemToGrant = "scroll_poisonkingbuff";
                break;
        }

        StringManager.SetTag(0, StringManager.GetString("misc_dmg_" + dType.ToString().ToLowerInvariant()));
        StringManager.SetTag(1, displayName);
        string newName = StringManager.GetString("monster_name_elemking");

        ChampionMod basic = GameMasterScript.masterChampionModList[essentialMod];
        MakeChampionFromMod(basic);

        extraModsPossible.Clear();

        int numExtraMods = 1;
        if (challengeValue >= 1.4f && challengeValue < 1.6f)
        {
            numExtraMods = 2;
        }
        else if (challengeValue >= 1.7f)
        {
            numExtraMods = 3;
        }

        foreach (string mRef in extraMods)
        {
            ChampionMod retrieve = GameMasterScript.masterChampionModList[mRef];
            bool modPossible = true;
            if (challengeValue >= retrieve.challengeValue && challengeValue <= retrieve.maxChallengeValue)
            {
                modPossible = false;
            }
            if (modPossible)
            {
                foreach (ChampionMod cm in championMods)
                {
                    if (cm.exclusionGroup == retrieve.exclusionGroup)
                    {
                        modPossible = false;
                        break;
                    }
                }
            }
            if (modPossible)
            {
                extraModsPossible.Add(retrieve);
            }
        }

        if (extraModsPossible.Count > 1)
        {
            extraModsPossible.Shuffle();
        }
        for (int i = 0; i < numExtraMods; i++)
        {
            extraModsPossible.RemoveAll(a => HasChampionMod(a.refName));

            if (extraModsPossible.Count > 0)
            {
                MakeChampionFromMod(extraModsPossible[UnityEngine.Random.Range(0, extraModsPossible.Count)]);
            }
            else
            {
                MakeChampion();
            }
        }

        myStats.AddStatusByRef(essentialStatus, this, 99);
        allDamageMultiplier -= 0.2f;
        allMitigationAddPercent += 0.1f;
        myStats.BoostStatByPercent(StatTypes.HEALTH, 0.1f);

        SetActorData("elementalking", (int)dType);

        Item itm = LootGeneratorScript.CreateItemFromTemplateRef(itemToGrant, 1.2f, 0f, false);
        myInventory.AddItemRemoveFromPrevCollection(itm, false);

        displayName = newName;
    }

    public void MakeNightmareBoss(int maxMods, bool nightmareKing, bool princeAddToQueen)
    {
        if (!nightmareKing)
        {
            ChampionMod nightmareMod = GameMasterScript.masterShadowKingChampModList[UnityEngine.Random.Range(0, GameMasterScript.masterShadowKingChampModList.Count)];
            MakeChampionFromMod(nightmareMod);
            MakeChampion();

            SetActorData("nightmareprince", 1);

            for (int i = 2; i < maxMods; i++)
            {
                MakeChampion();
            }
        }
        else
        {
            // Nightmare Queen!
            allMitigationAddPercent -= 0.075f; // was additive? // Remove the 1
            xpMod *= 1.75f;
            myStats.ChangeStat(StatTypes.ENERGY, 1.0f, StatDataTypes.ALL, true);
            myStats.ChangeStat(StatTypes.STAMINA, 1.0f, StatDataTypes.ALL, true);
            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.6f);
            myStats.BoostStatByPercent(StatTypes.STAMINA, 0.25f);
            myStats.BoostStatByPercent(StatTypes.ENERGY, 0.25f);
            myStats.BoostStatByPercent(StatTypes.STRENGTH, 0.15f);
            myStats.BoostStatByPercent(StatTypes.SWIFTNESS, 0.15f);
            myStats.BoostStatByPercent(StatTypes.GUILE, 0.15f);
            myStats.BoostStatByPercent(StatTypes.DISCIPLINE, 0.15f);
            myStats.BoostStatByPercent(StatTypes.SPIRIT, 0.15f);
            myStats.SetLevel(myStats.GetLevel() + 1);

            for (int i = 0; i < maxMods; i++)
            {
                xpMod += 0.15f;
                challengeValue += 0.05f;
                myStats.BoostStatByPercent(StatTypes.HEALTH, 0.2f);
                myStats.BoostStatByPercent(StatTypes.STAMINA, 0.15f);
                myStats.BoostStatByPercent(StatTypes.ENERGY, 0.15f);
                myStats.BoostStatByPercent(StatTypes.STRENGTH, 0.1f);
                myStats.BoostStatByPercent(StatTypes.SWIFTNESS, 0.1f);
                myStats.BoostStatByPercent(StatTypes.GUILE, 0.1f);
                myStats.BoostStatByPercent(StatTypes.DISCIPLINE, 0.1f);
                myStats.BoostStatByPercent(StatTypes.SPIRIT, 0.1f);
                myStats.BoostStatByPercent(StatTypes.ACCURACY, 0.05f);
                myStats.ChangeStat(StatTypes.VISIONRANGE, 1f, StatDataTypes.ALL, false);
                myStats.SetLevel(myStats.GetLevel() + 1);
                allDamageMultiplier += 0.04f;
                allMitigationAddPercent -= 0.01f; // was additive?
            }

            Item itm = LootGeneratorScript.GenerateLoot(challengeValue, 2.0f);
            if (itm != null)
            {
                myInventory.AddItem(itm, false);
            }
        }

        aggroRange = 15;
        turnsToLoseInterest = 999;

        allMitigationAddPercent -= 0.05f;
        allDamageMultiplier += 0.1f;
        myStats.BoostStatByPercent(StatTypes.HEALTH, 0.3f);
        myStats.SetStat(StatTypes.ACCURACY, 100f, StatDataTypes.ALL, true);


        if (!nightmareKing)
        {
            displayName = StringManager.GetString("mon_nightmare_prince_disp");
            myStats.SetStat(StatTypes.CHARGETIME, 99.9f, StatDataTypes.ALL, true);
        }
        else
        {
            displayName = StringManager.GetString("mon_nightmare_king_disp");
        }

        myStats.AddStatusByRef("status_shadowking", this, 99);

        challengeValue += 0.05f;
        myStats.SetLevel(myStats.GetLevel() + 1);

        if (nightmareKing)
        {
            challengeValue += 0.1f;
            myStats.SetLevel(myStats.GetLevel() + 1);
            myStats.BoostStatByPercent(StatTypes.HEALTH, 0.15f);
            myStats.BoostCoreStatsByPercent(0.15f);

            // Abilities! 

            ItemDreamFunctions.SetNightmareKingAbilities(this);
        }

        isItemBoss = true;

        if (!nightmareKing && !princeAddToQueen)
        {
            float chance = GetXPModToPlayer() - 0.05f;
            if (UnityEngine.Random.Range(0, 1f) <= chance)
            {
                Item shard = LootGeneratorScript.CreateItemFromTemplateRef("item_shadoworb_piece", 1.0f, 0f, false);
                myInventory.AddItem(shard, true);
            }
        }

        float localChance = GameMasterScript.CHANCE_NIGHTMAREPRINCE_LEGENDARY;

        if (nightmareKing)
        {
            foreach (Item itm in myInventory.GetInventory())
            {
                if (itm.IsEquipment() && !itm.legendary)
                {
                    Equipment eq = itm as Equipment;
                    EquipmentBlock.MakeMagicalFromModFlag(eq, MagicModFlags.NIGHTMARE, false);
                    break;
                }
            }

            localChance *= 3f;
            Item randomMagicalItem = LootGeneratorScript.GenerateLoot(challengeValue, 1.0f);
            if ((randomMagicalItem.IsEquipment()) && (!randomMagicalItem.legendary))
            {
                Equipment eq = randomMagicalItem as Equipment;
                EquipmentBlock.MakeMagicalFromModFlag(eq, MagicModFlags.NIGHTMARE, false);
            }
            myInventory.AddItem(randomMagicalItem, true);
            randomMagicalItem = LootGeneratorScript.GenerateLoot(challengeValue, 2.5f);
            if (randomMagicalItem.IsEquipment() && !randomMagicalItem.legendary)
            {
                Equipment eq = randomMagicalItem as Equipment;
                EquipmentBlock.MakeMagicalFromModFlag(eq, MagicModFlags.NIGHTMARE, false);
            }
            myInventory.AddItem(randomMagicalItem, true);
        }

        for (int i = 0; i < 2; i++)
        {
            // Drop from special NK table?
            if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHANCE_NIGHTMAREPRINCE_DROP_NKITEM)
            {
                Item nkItem = LootGeneratorScript.GenerateLootFromTable(challengeValue, 0.25f, "nightmare_prince_items");
                myInventory.AddItem(nkItem, true);
            }
            else
            {
                Item randomMagicalItem = LootGeneratorScript.GenerateLootFromTable(challengeValue, 1.5f, "equipment");
                myInventory.AddItem(randomMagicalItem, true);
            }

            if (!princeAddToQueen)
            {
                Item randomNKItem = LootGeneratorScript.GenerateLoot(challengeValue, 2.5f);
                if ((randomNKItem.IsEquipment()) && (!randomNKItem.legendary))
                {
                    Equipment eq = randomNKItem as Equipment;
                    EquipmentBlock.MakeMagical(eq, challengeValue, false);
                }
                myInventory.AddItem(randomNKItem, true);
            }
        }

        if (UnityEngine.Random.Range(0, 1f) <= GameMasterScript.CHANCE_NIGHTMAREPRINCE_LEGENDARY)
        {
            Item legItem = LootGeneratorScript.GenerateLootFromTable(challengeValue, 0.0f, "legendary");
            myInventory.AddItem(legItem, true);
        }

        // Should they be regular Bosses also? Maybe?
    }
}
