using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;

public partial class HeroPC
{
    public override void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("hero"); // Begin hero writing

        string bufferData = GameMasterScript.ConstructHeroSaveStringFromData(displayName, RandomJobMode.IsCurrentGameInRandomJobMode(), myJob.DisplayName,
            myStats.GetLevel(), beatTheGame, lowestFloorExplored, daysPassed, MetaProgressScript.GetDisplayPlayTime(false, 0f), GameMasterScript.gmsSingleton.gameMode,
            newGamePlus, MapMasterScript.activeMap.GetName(), myJob.portraitSpriteRef);

        writer.WriteElementString("buffereddata", bufferData);

        string playerModBuilder = PlayerModManager.GetPlayerModStringForSerialization();
        if (!string.IsNullOrEmpty(playerModBuilder))
        {
            writer.WriteElementString("playermodsactive", playerModBuilder);
        }

        writer.WriteElementString("displayname", displayName);
        writer.WriteElementString("selectedprefab", selectedPrefab);
        if (RandomJobMode.IsCurrentGameInRandomJobMode()) writer.WriteElementString("randomjob", "yes");
        writer.WriteElementString("advm", GameMasterScript.gmsSingleton.adventureModeActive.ToString().ToLowerInvariant());
        writer.WriteElementString("bg", beatTheGame.ToString().ToLowerInvariant());
        
        if (GameStartData.challengeType != ChallengeTypes.COUNT && GameStartData.challengeType != ChallengeTypes.NONE && GameStartData.currentChallengeData != null)
        {
            writer.WriteElementString("challenge", GameStartData.challengeType.ToString().ToLowerInvariant());
            writer.WriteElementString("cday", GameStartData.challengeDay.ToString());
            writer.WriteElementString("cweek", GameStartData.challengeWeek.ToString());
        }
        writer.WriteElementString("mode", GameMasterScript.gmsSingleton.gameMode.ToString().ToLowerInvariant());

        if (GameStartData.NewGamePlus != newGamePlus)
        {
            newGamePlus = GameStartData.NewGamePlus;
        }
        writer.WriteElementString("seed", GameMasterScript.gmsSingleton.gameRandomSeed.ToString());
        writer.WriteElementString("newgameplus", newGamePlus.ToString());

        writer.WriteElementString("iwend", GameMasterScript.endingItemWorld.ToString().ToLowerInvariant());

        StringBuilder dBuilder = new StringBuilder();
        dBuilder.Append(damageTakenLastThreeTurns[0]);
        dBuilder.Append("|");
        dBuilder.Append(damageTakenLastThreeTurns[1]);
        dBuilder.Append("|");
        dBuilder.Append(damageTakenLastThreeTurns[2]);
        writer.WriteElementString("dmglast3turns", dBuilder.ToString());

        /* writer.WriteElementString("id", actorUniqueID.ToString());
        writer.WriteElementString("fl", dungeonFloor.ToString());
        writer.WriteElementString("mid", actorMap.mapAreaID.ToString()); */
        WriteCoreActorInfo(writer);

        writer.WriteElementString("casinotokenprogress", CasinoScript.totalNumChips.ToString());

        writer.WriteElementString("lowestfloorexplored", lowestFloorExplored.ToString());
        if (lastOffhandEquipped != null)
        {
            writer.WriteElementString("lastoffhandid", lastOffhandEquippedID.ToString());
        }

        dBuilder.Length = 0;
        for (int i = 0; i < (int)AdventureStats.COUNT; i++)
        {
            if (i < (int)AdventureStats.COUNT - 1)
            {
                dBuilder.Append(advStats[i] + "|");
            }
            else
            {
                dBuilder.Append(advStats[i].ToString());
            }
        }
        writer.WriteElementString("adventurestats", dBuilder.ToString());

        dBuilder.Length = 0;
        for (int i = 0; i < (int)WeaponTypes.COUNT; i++)
        {
            if (i < (int)WeaponTypes.COUNT - 1)
            {
                dBuilder.Append(championsKilledWithWeaponType[i] + "|");
            }
            else
            {
                dBuilder.Append(championsKilledWithWeaponType[i].ToString());
            }
        }
        writer.WriteElementString("champskilledwithweapon", dBuilder.ToString());

        writer.WriteElementString("lastweapontype", lastWeaponTypeUsed.ToString());

        writer.WriteElementString("monsterskilled", monstersKilled.ToString());
        writer.WriteElementString("championskilled", championsKilled.ToString());
        writer.WriteElementString("numpandorasboxesopened", numPandoraBoxesOpened.ToString());
        writer.WriteElementString("stepstaken", stepsTaken.ToString());

        writer.WriteElementString("portalx", portalX.ToString());
        writer.WriteElementString("portaly", portalY.ToString());
        writer.WriteElementString("portalmapid", portalMapID.ToString());
        //writer.WriteElementString("actormap", actorMap.mapAreaID.ToString());

        writer.WriteElementString("previousposx", previousPosition.x.ToString());
        writer.WriteElementString("previousposy", previousPosition.y.ToString());

        WriteCurrentPosition(writer);

        /* writer.WriteElementString("posX", GetPos().x.ToString());
		writer.WriteElementString("posY", GetPos().y.ToString()); 
        if (areaID != MapMasterScript.FILL_AREA_ID)
        {
            writer.WriteElementString("aid", areaID.ToString());
        } */

        WriteFighterStuffToSave(writer);

        if (MapMasterScript.itemWorldOpen)
        {
            writer.WriteElementString("iwmc", MapMasterScript.itemWorldMagicChance.ToString());
        }

        writer.WriteElementString("money", money.ToString());

        dBuilder.Length = 0;
        for (int i = 0; i < previousTurnActions.Length; i++)
        {
            dBuilder.Append((int)previousTurnActions[i]);
            if (i < previousTurnActions.Length - 1)
            {
                dBuilder.Append("|");
            }
        }
        writer.WriteElementString("previousturnactions", dBuilder.ToString());

        //playTimeInSeconds += Time.fixedTime - GameMasterScript.timeSinceStartOrSave;      
        writer.WriteElementString("levelupboostwaiting", levelupBoostWaiting.ToString());
        writer.WriteElementString("playtime", GetPlayTime().ToString());
        writer.WriteElementString("dayspassed", daysPassed.ToString());
        writer.WriteElementString("regenflaskuses", regenFlaskUses.ToString());

        writer.WriteElementString("timeshealedthislevel", timesHealedThisLevel.ToString());
        writer.WriteElementString("numjobchanges", numberOfJobChanges.ToString());
        writer.WriteElementString("currentjob", myJob.jobEnum.ToString().ToLowerInvariant());
        writer.WriteElementString("startingjob", startingJob.ToString().ToLowerInvariant());

        dBuilder.Length = 0;
        for (int i = 0; i < GameStartData.gameModifiers.Length; i++)
        {
            dBuilder.Append(GameStartData.gameModifiers[i].ToString().ToLowerInvariant());
            if (i < GameStartData.gameModifiers.Length - 1)
            {
                dBuilder.Append("|");
            }
        }
        writer.WriteElementString("gmods", dBuilder.ToString());

        if (shopkeepersThatRefresh.Count > 0)
        {
            string sBuilder = "";
            for (int i = 0; i < shopkeepersThatRefresh.Count; i++)
            {
                sBuilder += shopkeepersThatRefresh[i];
                if (i < shopkeepersThatRefresh.Count - 1)
                {
                    sBuilder += ",";
                }
            }
            writer.WriteElementString("shopkeepers", sBuilder);
        }

        if (relicsDroppedOnTheGroundOrSold.Count > 0)
        {
            string sBuilder = "";
            int count = relicsDroppedOnTheGroundOrSold.Values.Count;
            int index = 0;
            foreach (var kvp in relicsDroppedOnTheGroundOrSold)
            {
                sBuilder += kvp.Key + "," + kvp.Value;
                if (index < count - 1)
                {
                    sBuilder += '|';
                }
                index++;
            }
            writer.WriteElementString("droppedrelics", sBuilder);
        }


        if (myAbilities.abilitiesThatReserveEnergy.Count > 0)
        {
            string builder = "";
            for (int i = 0; i < myAbilities.abilitiesThatReserveEnergy.Count; i++)
            {
                builder += myAbilities.abilitiesThatReserveEnergy[i];
                if (i < myAbilities.abilitiesThatReserveEnergy.Count - 1)
                {
                    builder += ",";
                }
            }
            writer.WriteElementString("energyreservedabilities", builder);
        }
        if (myAbilities.abilitiesThatReserveStamina.Count > 0)
        {
            string builder = "";
            for (int i = 0; i < myAbilities.abilitiesThatReserveStamina.Count; i++)
            {
                builder += myAbilities.abilitiesThatReserveStamina[i];
                if (i < myAbilities.abilitiesThatReserveStamina.Count - 1)
                {
                    builder += ",";
                }
            }
            writer.WriteElementString("staminareservedabilities", builder);
        }

        WriteMysteryDungeonDataIfNecessary(writer);

        /* if (myFeats.Count > 0)
        {
            foreach(string s in myFeats)
            {
                writer.WriteElementString("feat", s);
            }
        } */
        // writer.WriteStartElement("exploredmaps"); 
        string mapBuilder = "";
        string mapFloorBuilder = "";

        bool first = true;
        foreach(int floor in mapFloorsExplored)
        {
            if (!first) mapFloorBuilder += ",";
            mapFloorBuilder += floor;
        }

        for (int i = 0; i < mapsExploredByMapID.Count; i++)
        {
            if (i == mapsExploredByMapID.Count - 1)
            {
                mapBuilder += mapsExploredByMapID[i].ToString();
            }
            else
            {
                mapBuilder += mapsExploredByMapID[i].ToString() + ",";
            }
        }
        if (mapBuilder != "")
        {
            writer.WriteElementString("xmaps", mapBuilder);
        }
        if (!string.IsNullOrEmpty(mapFloorBuilder))
        {
            writer.WriteElementString("xmapfloors", mapFloorBuilder);
        }

        mapBuilder = "";
        for (int i = 0; i < mapsCleared.Count; i++)
        {
            if (i == mapsExploredByMapID.Count - 1)
            {
                mapBuilder += mapsCleared[i].mapAreaID.ToString() + "|" + mapsCleared[i].mapFloor.ToString();
            }
            else
            {
                mapBuilder += mapsCleared[i].mapAreaID.ToString() + "|" + mapsCleared[i].mapFloor.ToString() + ",";
            }
        }
        if (mapBuilder != "")
        {
            writer.WriteElementString("cmapsnew", mapBuilder);
        }

        //writer.WriteEndElement();
        //writer.WriteStartElement("exploredareas");

        //string areaBuilder = "";
        dBuilder.Length = 0;

        if (GetActorArea() == null)
        {
            //Debug.Log("Hero in null area.");
            SetActorArea(MapMasterScript.activeMap.areaDictionary[MapMasterScript.activeMap.CheckMTDArea(MapMasterScript.GetTile(GameMasterScript.heroPCActor.GetPos()))]);
            if (GetActorArea() == null)
            {
                Debug.Log("Hero still in null area?");
            }
        }

        for (int i = 0; i < exploredAreas.Length; i++)
        {
            if (GetActorArea() == null)
            {
                //Debug.Log("Cannot save null area.");
                continue;
            }
            if (!exploredAreas[i] && GetActorArea().areaID == i)
            {
                //writer.WriteElementString("area", i.ToString());
                dBuilder.Append(i.ToString());
                dBuilder.Append(",");
            }
            if (exploredAreas[i])
            {
                //writer.WriteElementString("area", i.ToString());
                dBuilder.Append(i.ToString());
                dBuilder.Append(",");
            }
        }
        string areaBuilder = dBuilder.ToString();
        if (areaBuilder != "")
        {
            writer.WriteElementString("xareas", areaBuilder);
        }

        //writer.WriteEndElement();
        if (heroFeats.Count > 0)
        {
            writer.WriteStartElement("herofeats");
            for (int i = 0; i < heroFeats.Count; i++)
            {
                writer.WriteElementString("feat", heroFeats[i].ToLowerInvariant());
            }
            writer.WriteEndElement();
        }

        writer.WriteStartElement("herojobs");
        for (int i = 0; i < (int)CharacterJobs.COUNT - 2; i++)
        {
            writer.WriteElementString("jp", jobJP[i].ToString());
            writer.WriteElementString("jpspent", jobJPspent[i].ToString());
        }
        writer.WriteEndElement();

        if (lastUsedMeleeWeapon != null)
        {
            writer.WriteElementString("lastmelee", lastUsedMeleeWeapon.actorUniqueID.ToString());
        }
        if (lastUsedWeapon != null)
        {
            writer.WriteElementString("lastweap", lastUsedWeapon.actorUniqueID.ToString());
        }

        if (tempRevealTiles.Count > 0)
        {
            writer.WriteStartElement("revealtiles");
            Vector2 check = Vector2.zero;
            for (int i = 0; i < tempRevealTiles.Count; i++)
            {
                check = tempRevealTiles[i];
                writer.WriteStartElement("tile");
                writer.WriteElementString("x", check.x.ToString());
                writer.WriteElementString("y", check.y.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        if (MapMasterScript.activeMap != null)
        {
            string strName = MapMasterScript.activeMap.GetName();
            if (dungeonFloor == MapMasterScript.TOWN_MAP_FLOOR)
            {
                strName = StringManager.GetString("floor_100_cname");
            }
            else if (dungeonFloor == MapMasterScript.TOWN2_MAP_FLOOR)
            {
                strName = StringManager.GetString("floor_150_cname");
            }
            writer.WriteElementString("mapname", strName);
        }
        writer.WriteElementString("portraitsprite", myJob.portraitSpriteRef);

        // Fighter stuff.
        myStats.WriteToSave(writer, true);
        myInventory.WriteToSave(writer);
        myAbilities.WriteToSave(writer);
        myEquipment.WriteToSave(writer);

        foreach (Item itm in myInventory.GetInventory())
        {
            if ((itm.actorUniqueID == lastOffhandEquippedID) && (lastOffhandEquippedID != 0))
            {
                lastOffhandEquipped = itm as Equipment;
                break;
            }
        }

        foreach (QuestScript qs in myQuests)
        {
            qs.WriteToSave(writer);
        }

        foreach (PlayingCard pc in gamblerHand)
        {
            writer.WriteStartElement("card");
            writer.WriteElementString("face", ((int)pc.face).ToString());
            writer.WriteElementString("suit", ((int)pc.suit).ToString());
            writer.WriteEndElement();
        }

        Item pairedFistItem = myEquipment.defaultWeapon.GetPairedItem();
        if (pairedFistItem != null)
        {
            int id = pairedFistItem.actorUniqueID;
            SetActorData("pairedfistitem", id);
        }

        WriteActorDict(writer);

        int iwOrbID = ReadActorData("orbusedtoopenitemworld");

        if ((MapMasterScript.itemWorldOpen) && (iwOrbID >= 0))
        {
            Actor theOrb = GameMasterScript.gmsSingleton.TryLinkActorFromDict(iwOrbID);
            if (theOrb == null)
            {
                Debug.Log("Player's orb doesn't exist in actor dict!");
            }
            else
            {
                Consumable iwOrb = theOrb as Consumable;
                writer.WriteStartElement("orbusedtoopenitemworld");
                iwOrb.WriteToSave(writer);
                writer.WriteEndElement();
            }
        }

        if (jobTrial != null)
        {
            jobTrial.WriteToSave(writer);
        }

        if (MapMasterScript.itemWorldOpen && MapMasterScript.itemWorldItem != null)
        {
            writer.WriteElementString("itemworlditemid", MapMasterScript.itemWorldItem.actorUniqueID.ToString());
        }

        if (limitBreakAmount > 0f)
        {
            writer.WriteElementString("limitbreak", limitBreakAmount.ToString());
        }

        WriteItemDreamData(writer);

        // Do we need to write Influence Turn Data? Run statuses for that.

        writer.WriteEndElement(); // End hero writing
    }

    void WriteMysteryDungeonDataIfNecessary(XmlWriter writer)
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1)) return;
        if (myMysteryDungeonData == null) return;
        myMysteryDungeonData.WriteToSave(writer);
    }
}

public partial class GameMasterScript
{
    public static string ConstructHeroSaveStringFromData(string playerName, bool randomJobMode, string currentJob, int level,  
        bool beatTheGame, int lowestFloor, int daysPassed, string playTime, GameModes gMode, int ngp, string currentMap, string portraitSprite)
    {
        string returnString = "gmode!" + gMode;

        /* if (gMode == GameModes.ADVENTURE)
        {
            returnString = StringManager.GetString("misc_adventure_mode");
        }
        else if (gMode == GameModes.NORMAL)
        {
            returnString = StringManager.GetString("misc_heroic_mode");
        }
        else if (gMode == GameModes.HARDCORE)
        {
            returnString = StringManager.GetString("misc_hardcore_mode");
        } */

        bool clearedSavage = false;

        returnString += "|ngp!" + ngp + "|";

        /* if (ngp == 1)
        {
            modeExtra += " " + UIManagerScript.cyanHexColor + " (" + StringManager.GetString("ui_mm_new_game") + "+)</color>";
        }
        else if (ngp == 2)
        {
            modeExtra += " " + UIManagerScript.redHexColor + " (" + StringManager.GetString("difficulty_mode_savage") + ")</color>";
            clearedSavage = true;
        } */

        //saveDataBlockAsyncLoadOutput.iNewGamePlusRank = ngp;

        returnString += "name!" + playerName;

        returnString += "|job!" + currentJob;

        returnString += "|map!" + currentMap;

        returnString += "|lvl!" + level;

        /* 
        StringManager.SetTag(0, "<color=yellow>" + playerName + "</color>");
        StringManager.SetTag(1, randomJobMode ? StringManager.GetString("job_wanderer") : currentJob);
        StringManager.SetTag(2, level.ToString());
        StringManager.SetTag(3, modeExtra); */

        returnString += "|rjmode!" + randomJobMode;

        returnString += "|lfloor!" + lowestFloor;

        returnString += "|beat!" + beatTheGame;

        returnString += "|port!" + portraitSprite;

        /* 
        if (!beatTheGame)
        {
            StringManager.SetTag(4, StringManager.GetString("saveslot_highestfloor") + ": " + (lowestFloor + 1).ToString());
        }
        else
        {
            StringManager.SetTag(4, GetGameClearText(clearedSavage));

            saveDataBlockAsyncLoadOutput.strLocation += " " + GetGameClearText(clearedSavage);
            saveDataBlockAsyncLoadOutput.bGameClearSave = true;
        }

        saveDataBlockAsyncLoadOutput.strGameModeInfo = modeExtra; */

        returnString += "|dp!" + daysPassed;

        /* 
        StringManager.SetTag(5, daysPassed.ToString());
        StringManager.SetTag(6, MetaProgressScript.GetDisplayPlayTime(false, 0f)); */

        returnString += "|pt!" + MetaProgressScript.GetDisplayPlayTime(false, 0f);

        //string txtBuilder = StringManager.GetString("save_slot_string");

        //builder = StringManager.GetString("save_slot_string");

        returnString += "|chal!" + GameStartData.challengeTypeBySlot[(int)GameStartData.saveGameSlot];

        /* switch (GameStartData.challengeTypeBySlot[(int)GameStartData.saveGameSlot])
        {
            case ChallengeTypes.DAILY:
                builder += " (" + UIManagerScript.greenHexColor + StringManager.GetString("ui_btn_dailychallenge") + "</color>)";
                break;
            case ChallengeTypes.WEEKLY:
                //builder += " (" + UIManagerScript.greenHexColor + StringManager.GetString("ui_btn_weeklychallenge") + "</color>)";
                break;
            default:
                break;
        }

        return builder; */

        return returnString;
    }
}