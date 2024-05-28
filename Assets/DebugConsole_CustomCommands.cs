using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class DebugConsole
{
    void RegisterCustomTangledeepCommands()
    {
        this.RegisterCommandCallback("lunartest", SetLunarNewYearActive);

#if UNITY_EDITOR
        this.RegisterCommandCallback("checktrees", CheckTreeData);
        this.RegisterCommandCallback("checktile", CheckTreeData);
#endif
        this.RegisterCommandCallback("reveal", RevealMapCommand);
#if UNITY_EDITOR
        this.RegisterCommandCallback("checkreveal", CheckRevealStatus);
#endif
        this.RegisterCommandCallback("checksteam", CheckSteamStatsAndAchievements);

        this.RegisterCommandCallback("createrelic", GenerateRelic);
        this.RegisterCommandCallback("createmonster", GenerateMonster);
#if UNITY_EDITOR
        this.RegisterCommandCallback("checktiledir", CheckTile);
#endif
        this.RegisterCommandCallback("levelup", LevelUp);
        this.RegisterCommandCallback("undying", Undying);
        this.RegisterCommandCallback("cheat", NeverDie);
        this.RegisterCommandCallback("imrich", AwardMoney);
#if UNITY_EDITOR
        this.RegisterCommandCallback("sharamodeinfo", SharaModeInfo);
#endif
        this.RegisterCommandCallback("reveal", Reveal);
        this.RegisterCommandCallback("awardjp", AwardJP);
        this.RegisterCommandCallback("awardxp", AwardXP);
#if UNITY_EDITOR
        this.RegisterCommandCallback("setlevel", SetMyLevel);
#endif
        this.RegisterCommandCallback("allmaps", PrintAllMaps);
        this.RegisterCommandCallback("sm", SpawnMonster);
        this.RegisterCommandCallback("sn", SpawnNPC);
        this.RegisterCommandCallback("sd", SpawnDestructible);
        this.RegisterCommandCallback("smc", SpawnChampionMonster);
        this.RegisterCommandCallback("si", SpawnItem);
        this.RegisterCommandCallback("sso", SpawnSkillOrb);
        this.RegisterCommandCallback("spawnlucidorb", SpawnLucidOrb);
        this.RegisterCommandCallback("checkmods", CheckItemMods);
        
        this.RegisterCommandCallback("simm", SpawnItemMM);
        
        this.RegisterCommandCallback("sirmm", SpawnItemRMM);
        this.RegisterCommandCallback("randomitem", RandomItem);
        this.RegisterCommandCallback("randomitems", RandomItems);

        this.RegisterCommandCallback("skipfloors", SkipFloors);
        this.RegisterCommandCallback("checktile", CheckTileInfo);

        this.RegisterCommandCallback("floorinfo", GetFloorInfo);
#if UNITY_EDITOR
        this.RegisterCommandCallback("printstairs", PrintAllStairs);
#endif
        this.RegisterCommandCallback("spawnstairsup", SpawnStairsUp);
        this.RegisterCommandCallback("ticktime", TickGameTime);
        this.RegisterCommandCallback("detonate", Detonate);
        this.RegisterCommandCallback("freezemonsters", FreezeMonsters);
        this.RegisterCommandCallback("drawcard", DrawCard);
#if UNITY_EDITOR
        this.RegisterCommandCallback("dialogstate", CheckDialogBoxState);
#endif


        this.RegisterCommandCallback("job", ChangeJob);
        this.RegisterCommandCallback("createmap", CreateMapByID);

#if UNITY_EDITOR
        this.RegisterCommandCallback("addstatus", Debug_AddStatusEffect);
#endif

        RegisterCommandCallback("setheroflag", SetHeroProgressFlag);
#if UNITY_EDITOR
        RegisterCommandCallback("setflag", SetHeroProgressFlag);
#endif
        RegisterCommandCallback("setmetaflag", SetMetaProgressFlag);

#if UNITY_EDITOR
        RegisterCommandCallback("findconnections", FindConnections);
#endif

        RegisterCommandCallback("randomtreasure", SpawnRandomLoots);

#if UNITY_EDITOR
        RegisterCommandCallback("ttshow", UIManagerScript.Debug_ShowGenericTooltip);
        RegisterCommandCallback("tthide", UIManagerScript.Debug_HideGenericTooltip);
        RegisterCommandCallback("ttitem", UIManagerScript.Debug_SetRandomItemGenericTooltip);
        RegisterCommandCallback("ttshowsub", UIManagerScript.Debug_GenericTooltipShowSubmenu);
        RegisterCommandCallback("tthidesub", UIManagerScript.Debug_GenericTooltipHideSubmenu);
        RegisterCommandCallback("ttwidth", UIManagerScript.Debug_GenericTooltipSetWidth);

        RegisterCommandCallback("ssopen", UIManagerScript.Debug_OpenSwitchSkillSheet);
        RegisterCommandCallback("ssassign", UIManagerScript.Debug_FillSwitchSkillSheet_Assign);
        RegisterCommandCallback("sspurchase", UIManagerScript.Debug_FillSwitchSkillSheet_Purchase);
#endif

        RegisterCommandCallback("learnskill", LearnSkill);

        RegisterCommandCallback("testdialog", StartConversation);

#if UNITY_EDITOR
        RegisterCommandCallback("holdup", GameMasterScript.Debug_WhatsTheHoldUp);
#endif
        //RegisterCommandCallback("delaygame", GameMasterScript.Debug_TestAddDelayCoroutine);

        //RegisterCommandCallback("monlist", GenerateMonsterList);

        //RegisterCommandCallback("dialogshowfaces", UIManagerScript.Debug_ToggleDialogFacesValue);
        //RegisterCommandCallback("dialogscriptonend", UIManagerScript.Debug_SetScriptOnNextDialogEnd);
        RegisterCommandCallback("screenshake", ScreenShake);

#if UNITY_EDITOR
        RegisterCommandCallback("runcutscene", RunCutscene);
#endif

        RegisterCommandCallback("vfx", SpawnVFX);

        RegisterCommand("wanderingmerchant", GameMasterScript.Debug_TrySpawnWanderingMerchant);

        RegisterCommand("addstatus", AddStatusToSelf);

        RegisterCommand("setlanguage", SetLanguage);
        RegisterCommand("petlevelup", PetLevelUp);

        RegisterCommand("scaleplayer", ScalePlayerToLevelOrCV);
#if UNITY_EDITOR
        RegisterCommand("bamf", TeleportPlayerToLocation);
        RegisterCommand("find", FindThing);
#endif
        RegisterCommand("unlockjob", UnlockJob);
        RegisterCommand("fixrealmofgods", RebuildRealmOfTheGods);
        RegisterCommand("whereami", WhereAmI);
#if UNITY_EDITOR
        RegisterCommand("monsterlove", MonsterLove);
#endif
        RegisterCommand("changename", ChangePlayerName);
        RegisterCommand("debugmouse", DebugMouse);
        //RegisterCommand("checkalleffects", CheckAllEffects);
        //RegisterCommand("setplayerprefsint", SetPlayerPrefsInt);
#if UNITY_EDITOR
        RegisterCommand("slimewin", SlimeDungeonMapWin);
        RegisterCommand("slimelose", SlimeDungeonMapLose);
        RegisterCommand("debugcheck", CheckForDebugThing);
        RegisterCommand("checkactors", CheckForAllActorsInMap);
#endif
        RegisterCommand("bubble", CheckBubbleCooldown);
        RegisterCommand("randomjobmode", EnterRandomJobMode);
        RegisterCommand("debugstairs", DebugStairsAccessibility);
        RegisterCommand("resettutorials", ResetTutorials);
        RegisterCommand("getlocalflag", GetLocalFlag);        
        //RegisterCommand("printlegweapons", PrintAllLegendaryItems);
        //RegisterCommand("printlnyitems", PrintLunarNewYearItems);        
    }

     public static HashSet<string> lnyRefs = new HashSet<string>() { "food_sesamebuns", "food_dumpling", "food_spicepeanuts", "item_luckyenvelope1", "item_luckyenvelope2", "item_luckyenvelope3"};
    
    object SetLunarNewYearActive(string[] args)
    {
            int value = PlayerPrefs.GetInt("lunartest");
            if (value != 1) PlayerPrefs.SetInt("lunartest", 1);
            else PlayerPrefs.SetInt("lunartest", 0);
                return "Lunar new year value set to: " + PlayerPrefs.GetInt("lunartest");
    }
    
    object PrintLunarNewYearItems(string[] args)
    {            
            foreach(var map in MapMasterScript.dictAllMaps)
            {
                    int count = 0;
                    int totalCount = 0;
                    Map actualMap = map.Value;
                    foreach(Actor act in actualMap.actorsInMap)
                    {
                        foreach(Item i in act.myInventory.GetInventory())
                        {
                                //Debug.Log(i.actorRefName);
                                if (lnyRefs.Contains(i.actorRefName))
                                {
                                        count++;
                                }
                                totalCount++;
                        }
                    }

                    if (count > 0) Debug.Log("LNY items in " + actualMap.GetName() + ": " + count + " out of " + totalCount);
                    //else Debug.Log("No LNY items in " + actualMap.GetName() + " out of " + totalCount);
            }
            return "Done!";
    }

    object PrintAllLegendaryItems(string[] args)
    {
        List<Item>[] allWeaponsByType = new List<Item>[(int)WeaponTypes.COUNT];

        for (int i = 0; i < (int)WeaponTypes.COUNT; i++)
        {
            allWeaponsByType[i] = new List<Item>();
        }

        foreach(var kvp in GameMasterScript.masterItemList)
        {
            if (!kvp.Value.legendary) continue;
            if (kvp.Value.itemType != ItemTypes.WEAPON) continue;
            Weapon w = kvp.Value as Weapon;
            allWeaponsByType[(int)w.weaponType].Add(kvp.Value);
        }

        string builder = "";

        for (int i = 0; i < (int)WeaponTypes.COUNT; i++)
        {
            builder = ((WeaponTypes)i) + " (" + allWeaponsByType[i].Count + "): ";
            allWeaponsByType[i].Sort((x, y) => x.challengeValue.CompareTo(y.challengeValue));
            foreach(var item in allWeaponsByType[i])
            {
                //builder += item.displayName + "|" + item.challengeValue + " ,";
                builder += item.actorRefName + "|" + item.challengeValue + " ,";
            }

            Debug.Log(builder);
        }   

        List<Item> allAccessories = new List<Item>();

        foreach(var kvp in GameMasterScript.masterItemList)
        {
            if (!kvp.Value.legendary) continue;
            if (kvp.Value.itemType != ItemTypes.ACCESSORY) continue;
            allAccessories.Add(kvp.Value);
        }

        builder = "Accessories (" + allAccessories.Count + "): ";
        allAccessories.Sort((x, y) => x.challengeValue.CompareTo(y.challengeValue));
        foreach(var item in allAccessories)
        {
            //builder += item.displayName + "|" + item.challengeValue + " ,";
            builder += item.actorRefName + "|" + item.challengeValue + " ,";
        }

        Debug.Log(builder);   

        return "Done!";
    }
}
