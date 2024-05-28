using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public class NPC : Actor
{
    public bool interactable;
    public string dialogRef;
    public string shopRef;
    public Conversation convo;
    public string strOverrideConversationStartingBranch;
    public int playerLastLowestLevelVisited;
    public int questsRemaining;
    public bool givesQuests;
    public bool campfirePossible;
    public bool doNotRestockShop;
    public int money;
    public bool newStuff = false;
    public bool cookingPossible;
    public string statusIcon;
    public MagicTree treeComponent;
    public bool hoverDisplay;
    public bool displayNewItemSprite;
    public bool noBumpToTalk;

    public void CopyFromTemplate(NPC template)
    {
        if (template == null)
        {
            return;
        }
        hoverDisplay = template.hoverDisplay;
        displayNewItemSprite = template.displayNewItemSprite;
        doNotRestockShop = template.doNotRestockShop;
        cookingPossible = template.cookingPossible;
        interactable = template.interactable;
        actorRefName = template.actorRefName;
        prefab = template.prefab;
        displayName = template.displayName;
        monsterCollidable = template.monsterCollidable;
        playerCollidable = template.playerCollidable;
        dialogRef = template.dialogRef;
        shopRef = template.shopRef;
        givesQuests = template.givesQuests;
        questsRemaining = template.questsRemaining;
        campfirePossible = template.campfirePossible;
        statusIcon = template.statusIcon;
        noBumpToTalk = template.noBumpToTalk;
    }

    // Should the map master handle this?
    public void CheckForNewStuffAndSpawn()
    {
        if (doNotRestockShop) return;
        if (actorRefName == "npc_foodcart") return;
        if (!displayNewItemSprite) return;
        if (newStuff && MapMasterScript.activeMap.floor == dungeonFloor)
        {
            if (myInventory.GetInventory().Count == 0 && !givesQuests) return;

            GameObject nStuff = GameMasterScript.TDInstantiate("NewItemsDisplay");
            if (nStuff != null)
            {
                if (!objectSet)
                {
                    Debug.Log(actorRefName + " " + actorUniqueID + " has no object when trying to spawn NEWITEMS? Why?");
                    return;
                }
                nStuff.transform.SetParent(GetObject().transform);
                nStuff.transform.localPosition = Vector3.zero;
                nStuff.GetComponent<Animatable>().SetAnim("Idle");
                AddOverlay(nStuff, true);
                nStuff.GetComponent<SpriteRenderer>().enabled = true;
                nStuff.GetComponent<SpriteEffect>().SetBaseVisible(true);
                nStuff.GetComponent<SpriteEffect>().SetAlwaysVisible(true);
                nStuff.GetComponent<SpriteEffect>().SetCurVisible(true);
                GameMasterScript.AlignGameObjectToObject(nStuff, GetObject(), Directions.NORTH, 0f, 0f);
                MapMasterScript.singletonMMS.activeNonTileGameObjects.Add(nStuff);
            }
            else
            {
                Debug.Log("WARNING: Failed to instantiate NewItemsDisplay for " + actorRefName + " " + actorUniqueID + " " + dungeonFloor);
                // This should never happen anymore.
            }
        }
    }

    public int GetDaysSinceLastRestock()
    {
        int dateOfLastRestock = ReadActorData("rd");
        if (dateOfLastRestock < 0) dateOfLastRestock = MetaProgressScript.totalDaysPassed - 1;
        return MetaProgressScript.totalDaysPassed - dateOfLastRestock;
    }

    public int GetTreeSlot()
    {
        if (treeComponent == null)
        {
            Debug.Log("Warning: " + actorRefName + " is not a tree.");
            return -1;
        }
        return treeComponent.slot;
    }

    public void TryRestockGoods()
    {
        if (givesQuests || !String.IsNullOrEmpty(shopRef))
        {
#if UNITY_EDITOR
            RefreshShopInventory(GameMasterScript.heroPCActor.lowestFloorExplored);
#else
            try { RefreshShopInventory(GameMasterScript.heroPCActor.lowestFloorExplored); }
            catch(Exception e)
            {
                Debug.Log("Dire Error restocking shop of " + actorRefName + " on floor " + dungeonFloor + " With exception " + e);
            }
#endif
        }
    }

    public void RefreshShopInventory(int floor)
    {
        //This function refreshes the quest count on an NPC which gives quests.
        if (givesQuests)
        {
            questsRemaining += UnityEngine.Random.Range(2, 4);
            if (questsRemaining > 3)
            {
                questsRemaining = 3;
            }

            SetNewStuff(true);

            SetActorData("rd", MetaProgressScript.totalDaysPassed);

            //Debug.Log("<color=yellow>Quests restocked. " + actorRefName + " " + newStuff + "</color>");
            return;
        }

        //But if the NPC has no shop, leave early.
        if (string.IsNullOrEmpty(shopRef))
        {
            //Debug.Log(actorRefName + " does not appear to be a merchant; no inventory to restock.");
            return;
        }

        //The banker doesn't refresh its shop, if it did you would lose all your stuff.
        if (actorRefName == "npc_banker") return;

        if (myInventory == null)
        {
            CreateNewInventory();
        }

        SetActorData("rd", MetaProgressScript.totalDaysPassed);

        //Create a list of backup items just in case
        List<Item> backupList = new List<Item>();
        foreach (Item itm in myInventory.GetInventory())
        {
            backupList.Add(itm);
        }

        //This is unfortunate.
        ShopScript processShop = GetShop();
        if (processShop == null)
        {
            Debug.Log(actorRefName + " couldn't get its own shop.");
            return;
        }

        //shop a lop loppa shoppa
        ShopScript.ShopData whichShop = processShop.GetShop();

        if (whichShop == null)
        {
            Debug.Log("No shop for " + actorRefName);
            return;
        }

        //It is some how possible for a shop have 0 items
        int numItems = UnityEngine.Random.Range(whichShop.minItems, whichShop.maxItems);
        if (numItems == 0)
        {
            return;
        }

        numItems += (2 * GameStartData.NewGamePlus);

        //Clear out my inventory! It's ok, we made a backup.
        myInventory.ClearInventory();

        SetNewStuff(true);

        SetActorData("stockedonce", 1);
        List<MagicMod> possibleMods = new List<MagicMod>();
        float localChallengeValue = whichShop.challengeValue;

        // Shop CV changes based on player.
        if (whichShop.adaptChallengeValue)
        {
            float cv = 1.0f;
            float extraCV = whichShop.challengeValue; // Should be 0.1f for "randomany" shops

            // In mystery dungeons, we only use the CV of the floor we're currently on.
            if (GetActorMap() != null && GetActorMap().IsMysteryDungeonMap())
            {
                cv = GetActorMap().challengeRating; // This would be something like 1.0f for early MD maps
                localChallengeValue = cv; 
                if (MysteryDungeonManager.GetActiveDungeon().HasGimmick(MysteryGimmicks.HIGHER_MERCHANT_VALUE))
                {
                    extraCV += MysteryDungeonManager.HIGHER_MERCHANT_VALUE_CV;
                }
            }
            else if (SharaModeStuff.IsSharaModeActive() && GameMasterScript.heroPCActor != null)
            {
                // If game has started, base this on Shara's level
                cv = BalanceData.DICT_LEVEL_TO_CV[GameMasterScript.heroPCActor.myStats.GetLevel()];
                localChallengeValue = cv;
            }
            else
            {
                // Otherwise, our CV adapts to the lowest floor hero has explored
                Map lowestFloor = MapMasterScript.theDungeon.FindFloor(GameMasterScript.heroPCActor.lowestFloorExplored);
                if (lowestFloor != null)
                {
                    cv = lowestFloor.challengeRating;
                }

                if (cv > localChallengeValue)
                {
                    localChallengeValue = cv;
                }
            }

            cv += extraCV;
            localChallengeValue += extraCV;
        }

        //Build up the list of magic mods we are allowed to use on this shop
        if (whichShop.modLimited || whichShop.addPossibleModFlags.Count > 0)
        {
            foreach (MagicMod mm in GameMasterScript.masterMagicModList.Values)
            {
                bool modValid = false;

                // Allow for exclusive mods?
                if (mm.challengeValue > 500f)
                {
                    for (int i = 0; i < mm.modFlags.Length; i++)
                    {
                        if (!mm.modFlags[i]) continue;
                        if (whichShop.addPossibleModFlags.Contains((MagicModFlags)i))
                        {
                            modValid = true;
                            break;
                        }
                    }
                }

                //If this MM is only for orbs, don't use it.
                if (mm.lucidOrbsOnly)
                {
                    continue;
                }

                //If the mod is invalid because one of the modflags is false 
                //or the shop's list of possibleModFlags doesn't contain the mod
                //then discard the mod if the mod is outside of the localChallengeValue range.
                if (!modValid)
                {
                    if (mm.challengeValue > localChallengeValue) continue;
                    if (localChallengeValue > mm.maxChallengeValue) continue;
                }

                //if we are still not valid but somehow get here, compare a bunch of bools
                //and if any of them match, we are now valid
                if (whichShop.modLimited)
                {
                    for (int i = 0; i < mm.modFlags.Length; i++)
                    {
                        if (mm.modFlags[i] && whichShop.limitModFlags[i])
                        {
                            modValid = true;
                            break;
                        }
                    }
                }
                else
                {
                    //or just be valid.
                    modValid = true;
                }

                //add it to the list three times because Special mods show up more frequently
                if (modValid)
                {
                    possibleMods.Add(mm);
                    possibleMods.Add(mm); // Special mods show up more frequently
                    possibleMods.Add(mm); // Special mods show up more frequently
                }
            }
        }

        bool isMysteryDungeonMerchant = ReadActorData("mysterymerchant") == 1;

        //Note here, i <= numItems always ensures there will be one more than you asked for.
        for (int i = 0; i <= numItems; i++)
        {
            // Figure out which table to use.
            ActorTable tableToUse = whichShop.items;

            //if the shop has special tables, roll dice and grab one
            bool hasSpecialTables = whichShop.specialTables.GetNumActors() > 0 && UnityEngine.Random.Range(0, 1f) > whichShop.chanceToUseBaseTable;
            if (hasSpecialTables)
            {
                //Debug.Log(actorRefName + " has special tables.");

                //but if it doesn't for some reason, just got a regular random loot table
                var tableRef = whichShop.specialTables.GetRandomActorRef();
                if (!string.IsNullOrEmpty(tableRef))
                {
                    tableToUse = LootGeneratorScript.GetLootTable(tableRef);
                }
                else
                {
                    tableRef = "allitems";
                }
            }

            // Find a valid item. If we are limited to CV, keep searching until we find one.
            bool validItemFound = false;            
            Item template = null; // This is the item we found that will definitely be valid, for sure 100%
            int attempts = 0;
            while (!validItemFound && attempts <= 100)
            {
                if (attempts > 0 && hasSpecialTables) // Reset our table each time, if we're having trouble finding something valid.
                {
                    string tableRef = whichShop.specialTables.GetRandomActorRef();
                    if (!string.IsNullOrEmpty(tableRef))
                    {
                        tableToUse = LootGeneratorScript.GetLootTable(tableRef);
                    }
                    if (tableToUse == null || string.IsNullOrEmpty(tableRef))
                    {
                        tableRef = "allitems";
                        tableToUse = LootGeneratorScript.GetLootTable("allitems");
                    }
                }
                attempts++; // In the unlikely event we're at some weird combination of player level and item table selected, and nothing is valid???

                //Get an item from our table, and if there isn't one, well that's weird,
                //but go ahead and roll on the EVERYTABLE
                string itemRef = tableToUse.GetRandomActorRef();
            if (string.IsNullOrEmpty(itemRef))
                {
                    tableToUse = LootGeneratorScript.GetLootTable("allitems");
                    itemRef = tableToUse.GetRandomActorRef();
                }

                //If we get here and have no itemref, then there are no items in the
                //"allitems" table, and The Nothing has truly won.
                template = Item.GetItemTemplateFromRef(itemRef);

                // Special flag for Mystery Dungeon merchants
                bool sharaActive = SharaModeStuff.IsSharaModeActive();
                if (isMysteryDungeonMerchant || sharaActive)
                {
                    if (template.challengeValue > localChallengeValue) continue; // This item is too high level for us.

                    if (isMysteryDungeonMerchant && DungeonMaker.disallowedItemsInMysteryDungeons.Contains(itemRef))
                    {
                        // could be rose petals, tent, etc. skip!
                        continue;
                    }
                    if ((sharaActive && SharaModeStuff.disallowSharaModeItems.Contains(itemRef)) ||
                        (RandomJobMode.IsCurrentGameInRandomJobMode() && RandomJobMode.disallowRandomJobModeItems.Contains(itemRef)))
                    {
                        continue;
                    }
                }


                //Block rosepetals from dropping unless we have progressed far enough
                bool bAllowRosepetals = ProgressTracker.CheckProgress(TDProgress.ROMANCEQUEST, ProgressLocations.META) >= 1;
                while (!bAllowRosepetals && template.actorRefName == "spice_rosepetals" && whichShop.specialTables.GetTotalCount() > 0)
                {
                    var tableRef = whichShop.specialTables.GetRandomActorRef();
                    if (!string.IsNullOrEmpty(tableRef))
                    {
                        tableToUse = LootGeneratorScript.GetLootTable(tableRef);
                    }
                    itemRef = tableToUse.GetRandomActorRef();
                    template = Item.GetItemTemplateFromRef(itemRef);
                }

                // Ensure no duplicate legendaries, or legendaries the player has already found.
                //todo: Why not add the above check to ValidForPlayer?
                while (!template.ValidForPlayer())
                {
                    //if there is every a table with 1 legendary item, and the player has it already,
                    //this will be a bad time.
                    var tableRef = whichShop.specialTables.GetRandomActorRef();
                    if (!string.IsNullOrEmpty(tableRef))
                    {
                        tableToUse = LootGeneratorScript.GetLootTable(tableRef);
                    }
                else
                {
                    tableToUse = whichShop.items;
                }
                if (string.IsNullOrEmpty(tableToUse.refName))
                    {
                        tableToUse = LootGeneratorScript.GetLootTable("allitems");
                    }
                    template = Item.GetItemTemplateFromRef(tableToUse.GetRandomActorRef());
                }

                //Don't drop an item that requires NG+ if we aren't there. Instead, roll 
                //a different item, which could be rosepetals or a legendary you already have.
                while (template.reqNewGamePlusLevel > GameStartData.NewGamePlus)
                {
                    var tableRef = whichShop.specialTables.GetRandomActorRef();
                    if (!string.IsNullOrEmpty(tableRef))
                    {
                        tableToUse = LootGeneratorScript.GetLootTable(tableRef);
                    }
                    tableToUse = whichShop.items;
                    template = Item.GetItemTemplateFromRef(tableToUse.GetRandomActorRef());
                }

                validItemFound = true;
            }

#if UNITY_EDITOR
            if (!validItemFound)
            {
                Debug.Log("Uh oh, no item found for " + actorRefName + " on floor " + dungeonFloor + " " + localChallengeValue);
            }
#endif

            Item invItem = null;

            if (isMysteryDungeonMerchant && UnityEngine.Random.Range(0,1f) <= GameMasterScript.DLC1_CHANCE_RELIC_MD_MERCHANT)
            {
                if (UnityEngine.Random.Range(0,1f) <= MysteryDungeonManager.CHANCE_RUNE_INSTEAD_OF_LEGENDARY)
                {
                    invItem = LootGeneratorScript.GenerateLootFromTable(1.8f, 0f, "runeteachskills");
                    //Debug.Log("We made a rune!" + invItem.actorRefName);
                }
                else
                {
                    invItem = LegendaryMaker.CreateNewLegendaryItem(localChallengeValue);
                    //Debug.Log("Merchant has relic! " + invItem.displayName);
                }
                
            }
            else
            {
                //Now we are ready to make itam!
                invItem = LootGeneratorScript.CreateItem(template, localChallengeValue, 1.0f, false);
            }



            if (invItem == null)
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: " + actorRefName + " created a null item somehow?");
#else
                Debug.Log("ERROR: " + actorRefName + " created a null item somehow?");
#endif
                continue;
            }

            float localValueMultToUse = whichShop.valueMult;

            if (invItem.legendary && isMysteryDungeonMerchant && !MysteryDungeonManager.GetActiveDungeon().resourcesKeepAtEnd[(int)EMysteryDungeonPlayerResources.GEAR])
            {
                // legendaries are cheaper in MDs!
                localValueMultToUse *= MysteryDungeonManager.LEGENDARY_PRICE_MULTIPLIER;
            }

            //Do some science to fix up the prices
            invItem.CalculateShopPrice(localValueMultToUse, forceRecalculate: true, useBaseCost: true);
            invItem.CalculateSalePrice(forceRecalculateShopPrice: false);

            //Add it to our inventory
            myInventory.AddItem(invItem, true); // 10/28 merchants now stack stuff.
            if (!invItem.IsEquipment() || invItem.legendary)
            {
                invItem.RebuildDisplayName();
                continue;
            }

            //Here the item IsEquipment and is not legendary. 
            for (int m = 0; m < whichShop.maxMagicMods; m++)
            {
                //Start rolling for mods
                if (UnityEngine.Random.Range(0, 1f) <= whichShop.magicChance)
                {
                    //And if there's mods possible
                    if (whichShop.modLimited || whichShop.addPossibleModFlags.Count > 0)
                    {
                        //well maybe there arent?
                        if (possibleMods.Count == 0)
                        {
                            //Debug.Log("No mods available for " + shopRef);
                        }
                        else
                        {
                            //ok there are.
                            MagicMod possible = possibleMods[UnityEngine.Random.Range(0, possibleMods.Count)];
                            Equipment eq = invItem as Equipment;

                            //Make sure only the right mods show up on our gear. Reject ones
                            //that do not fit.
                            while (eq.IsModValidForMe(possible) != MagicModCompatibility.POSSIBLE)
                            {
                                possibleMods.Remove(possible);
                                if (possibleMods.Count == 0)
                                {
                                    break;
                                }
                                possible = possibleMods[UnityEngine.Random.Range(0, possibleMods.Count)];
                            }

                            //If the list wasn't exhausted, attatch the most recent mod to the item.
                            if (possibleMods.Count != 0)
                            {
                                EquipmentBlock.MakeMagicalFromMod(invItem, possible, true, true, false);
                            }
                        }

                    }
                    else
                    {
                        //If the shop isn't special, just do a regular roll.
                        EquipmentBlock.MakeMagical(invItem, localChallengeValue, false);
                    }

                }
                else
                {
                    //Here, we missed our roll for magic items. RIP.
                    break;
                }
            }
        }

        //If after all that we have stugatz in the inventory, grab our back up.
        if (myInventory.GetInventory().Count == 0)
        {
            //Debug.Log(actorRefName + " " + actorUniqueID + " restocked with no items? Previous inventory had " + backupList.Count + " so switching to that.");
            SetNewStuff(false);
            myInventory.SetInventoryList(backupList);
        }
        else
        {
            foreach(Item itm in backupList)
            {
                if (!itm.customItemFromGenerator) continue;
                GameMasterScript.heroPCActor.relicsDroppedOnTheGroundOrSold.Remove(itm.actorRefName);
                if (Debug.isDebugBuild) Debug.Log(itm.actorRefName + " marked for permanent removal.");
            }
        }

        // Done adding items to merchant.
        myInventory.SortMyInventory(InventorySortTypes.RARITY, true);

        //Debug.Log("Finished restocking " + actorRefName + " on " + dungeonFloor + " items: " + myInventory.GetActualInventoryCount());
    }

    public ShopScript GetShop()
    {
        ShopScript returnShop = null;

        if (GameMasterScript.masterShopList.TryGetValue(shopRef, out returnShop))
        {
            return returnShop;
        }
        else
        {
            if (Debug.isDebugBuild) Debug.LogError("NPC " + actorRefName + " could not find own shop ref " + shopRef);
            return null;
        }

        /* foreach (ShopScript ss in GameMasterScript.masterShopList)
        {
            if (ss.refName == shopRef)
            {
                returnShop = ss;
                break;
            }
        }
        if (returnShop == null)
        {
            Debug.Log("NPC " + actorRefName + " could not find own shop ref " + shopRef);
        }
        return returnShop; */
    }

    public Conversation GetConversation()
    {
        if (dialogRef == "" || dialogRef == null)
        {
            return null;
        }
        if (convo == null)
        {
            convo = GameMasterScript.FindConversation(dialogRef);
            if (convo != null)
            {
                convo.strOverrideStartingBranch = strOverrideConversationStartingBranch;
                return convo;
            }
            Debug.Log(actorRefName + " " + dialogRef + " conversation not found.");
            return null;
        }
        else
        {
            convo.strOverrideStartingBranch = strOverrideConversationStartingBranch;
            return convo;
        }
    }

    public NPC()
    {
        Init();
    }

    protected override void Init()
    {
        if (initialized) return;
        base.Init();
        SetActorType(ActorTypes.NPC);
        playerCollidable = true;
        monsterCollidable = true;
        displayNewItemSprite = true;
        hoverDisplay = true;
    }

    public void CopyFromTemplateRef(string tRef)
    {
        NPC template = FindTemplate(tRef);
        if (template != null)
        {
            CopyFromTemplate(template);
        }
    }

    public static NPC FindTemplate(string npcRef)
    {
        NPC getNPC;
       
        if (GameMasterScript.masterNPCList.TryGetValue(npcRef, out getNPC))
        {
            return getNPC;
        }

        Debug.Log("Couldn't find NPC template " + npcRef);
        return null;
    }

    public void ReadFromSave(XmlReader reader, bool addInvActorsToMasterDict = true)
    {
        reader.Read();

        int reads = 0;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            string strValue = reader.Name.ToLowerInvariant();
            reads++;
            if (reads > 15000)
            {
                Debug.Log("Breaking");
                break;
            }
            string txt;
            switch (strValue)
            {
                case "ref":
                case "refname":
                case "actorrefname":
                    actorRefName = reader.ReadElementContentAsString();
                    if (!GameMasterScript.masterNPCList.ContainsKey(actorRefName))
                    {
                        Debug.Log("Warning! " + actorRefName + " does not appear in dict.");
                        // just ignore this NPC.
                        while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "npc"))
                        {
                            reader.Read();
                        }
                        SetActorData("loadfail", 1);
                        reader.ReadEndElement();
                        return;
                    }
                    NPC template = FindTemplate(actorRefName);
                    CopyFromTemplate(template);
                    break;
                case "cr":
                    ReadCoreActorInfo(reader);
                    break;
                case "fl":
                case "floor":
                case "dungeonfloor":
                    dungeonFloor = reader.ReadElementContentAsInt();
                    break;
                case "mapid":
                case "actormap":
                    actorMapID = reader.ReadElementContentAsInt();
                    MapMasterScript.TryAssignMap(this, actorMapID);
                    break;
                case "id":
                case "uniqueid":
                    actorUniqueID = reader.ReadElementContentAsInt();
                    break;
                case "pos":
                    ReadCurrentPosition(reader);
                    spawnPosition.x = GetPos().x;
                    spawnPosition.y = GetPos().y;
                    break;
                case "posx":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosX(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "posy":
                    txt = reader.ReadElementContentAsString();
                    SetCurPosY(CustomAlgorithms.TryParseFloat(txt));
                    break;
                case "spawnposx":
                    txt = reader.ReadElementContentAsString();
                    spawnPosition.x = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "spawnposy":
                    txt = reader.ReadElementContentAsString();
                    spawnPosition.y = CustomAlgorithms.TryParseFloat(txt);
                    break;
                case "aid":
                case "areaid":
                    areaID = reader.ReadElementContentAsInt();
                    break;
                case "questsremaining":
                    questsRemaining = reader.ReadElementContentAsInt();
                    break;
                case "money":
                    money = reader.ReadElementContentAsInt();
                    break;
                case "newstuff":
                    SetNewStuff(reader.ReadElementContentAsBoolean());
                    break;
                case "inv":
                case "inventory":
                    reader.ReadStartElement();
                    if (reader.Name.ToLowerInvariant() != "item")
                    {
                        continue;
                    }

                    try
                    {
                        myInventory.ReadFromSave(reader, addInvActorsToMasterDict);
                    }
                    catch(Exception e)
                    {
                        Debug.Log("Failed to read NPC inventory " + e);
                    }
                    
                    break;
                case "movedir":
                    lastMovedDirection = (Directions)reader.ReadElementContentAsInt();
                    break;
                case "lmd":
                case "lastmoveddirection":
                    lastMovedDirection = (Directions)Enum.Parse(typeof(Directions), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "plowlvl":
                case "playerlastlowestlevelvisited":
                    playerLastLowestLevelVisited = reader.ReadElementContentAsInt();
                    break;
                case "magictree":
                    treeComponent = new MagicTree(this);
                    treeComponent.ReadFromSave(reader);
                    break;
                case "dad":
                case "dictactordata":
                    ReadActorDict(reader);
                    break;
                case "dads":
                case "dictactordatastring":
                case "dictactordatastrings":
                    ReadActorDictString(reader);
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        // Trees in town are a special exception, these need to get added at the END when all other game data has loaded.
        // Otherwise, trees from a previous character could be loaded and their IDs can conflict with newly-generated things.
        if (!actorRefName.Contains("town_tree"))
        {
            GameMasterScript.AddActorToDict(this);
            GameMasterScript.allLoadedNPCs.Add(this);
        }

        //Debug.Log("Read NPC " + actorRefName + " " + actorUniqueID + " " + dungeonFloor);

        reader.ReadEndElement();
    }

    public override void WriteToSave(XmlWriter writer)
    {
        if (GetActorMap() == null)
        {
            //Debug.Log(actorRefName + " " + dungeonFloor + " " + actorUniqueID + " no map, not saving.");
            return;
        }
        writer.WriteStartElement("npc");
        writer.WriteElementString("ref", actorRefName);

        WriteCoreActorInfo(writer);
        /* writer.WriteElementString("id", actorUniqueID.ToString());
        writer.WriteElementString("fl", dungeonFloor.ToString());
        writer.WriteElementString("mapid", actorMap.mapAreaID.ToString()); */

        WriteCurrentPosition(writer);

        bool isBanker = actorRefName == "npc_banker";

        /* writer.WriteElementString("posX", GetPos().x.ToString());
        writer.WriteElementString("posY", GetPos().y.ToString());
        if (areaID != MapMasterScript.FILL_AREA_ID)
        {
            writer.WriteElementString("aid", areaID.ToString());
        } */
        writer.WriteElementString("spawnposx", GetSpawnPos().x.ToString());
        writer.WriteElementString("spawnposy", GetSpawnPos().y.ToString());
        if (newStuff)
        {
            writer.WriteElementString("newstuff", "true");
        }

        if ((int)lastMovedDirection != 0)
        {
            writer.WriteElementString("movedir", ((int)lastMovedDirection).ToString());
        }
        

        if (questsRemaining > 0)
        {
            writer.WriteElementString("questsremaining", questsRemaining.ToString().ToLowerInvariant());
        }        

        if (money != 0 && !isBanker)
        {
            writer.WriteElementString("money", money.ToString().ToLowerInvariant());
        }
        
        if (playerLastLowestLevelVisited != 0)
        {
            writer.WriteElementString("plowlvl", playerLastLowestLevelVisited.ToString());
        }
        if (myInventory != null && !isBanker)
        {
            myInventory.WriteToSave(writer);
        }

        if (treeComponent != null)
        {
            treeComponent.WriteToSave(writer);
        }

        WriteActorDict(writer);

        writer.WriteEndElement();
    }


    public static NPC CreateNPC(string templateRef)
    {
        NPC created = null;
        NPC template = NPC.FindTemplate(templateRef.ToLowerInvariant());


        if (template == null)
        {
            Debug.Log("WARNING: Couldn't find NPC template " + templateRef);
            return null;
        }

        created = new NPC();
        created.CopyFromTemplate(template);
        created.SetUniqueIDAndAddToDict();
        //Debug.Log("Created npc " + created.actorRefName);

        return created;
    }

    public void SetNewStuff(bool value)
    {
        //Debug.Log(actorRefName + " " + actorUniqueID + " on " + dungeonFloor + " newstuff value is: " + value);
        newStuff = value;
    }
}