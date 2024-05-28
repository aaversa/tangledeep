using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine.Events;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using Steamworks;
    using UnityEngine.Analytics;
    using LapinerTools.Steam.Data;
    using LapinerTools.uMyGUI;
    using System.Security.Cryptography;
#elif UNITY_SWITCH
    using nn.oe;
#endif

using UnityEngine.UI;
using System.Text;
using TMPro;
using System.Threading;
using Rewired.ComponentControls.Data;
using System.Reflection;
using System.Runtime;

public partial class GameMasterScript
{

    IEnumerator FirstTimeLoadRoutine()
    {
        dictTempStringData = new Dictionary<string, string>();
        dictTempFloatData = new Dictionary<string, float>();
        dictTempGameObjects = new Dictionary<string, GameObject>();

        ResetLoadingBar();
        if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.LOADGAME)
        {
            SetLoadingBarToLoadGameOnlyMode();
        }

        //put this away, we have fancy bar now
        LoadingWaiterManager.Hide(0.1f);

        UIManagerScript.SetToBlack();
        //if (Debug.isDebugBuild) Debug.Log("Start loading game data. " + Time.realtimeSinceStartup);

        if (dictObjectPools == null)
        {
            StartCoroutine(PreloadAllStaticResources());
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Resources already loaded.");
        }

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
                CacheStaticMaterial(ref spriteMaterialLit, "MyLighting");
                CacheStaticMaterial(ref spriteMaterialUnlit, "UnlitSprites");
                CacheStaticMaterial(ref spriteMaterial_DestructiblesLit, "MyLighting_Destructible");
                CacheStaticMaterial(ref spriteMaterial_DestructiblesUnLit, "UnlitSprites_Destructible");
        }
        else 
        {
#if UNITY_PS4 || UNITY_XBOXONE
            //not sure if it's because of unity version but the default sprite shader didn't work on consoles 
            yield return spriteMaterialLit = Resources.Load<Material>("MyLighting PS4_XBOXONE");
            yield return spriteMaterialUnlit = Resources.Load<Material>("UnlitSprites PS4_XBOXONE");
            yield return spriteMaterial_DestructiblesLit = Resources.Load<Material>("MyLighting_Destructible PS4_XBOXONE");
            yield return spriteMaterial_DestructiblesUnLit = Resources.Load<Material>("UnlitSprites_Destructible PS4_XBOXONE");
#else
            yield return spriteMaterialLit = Resources.Load<Material>("MyLighting");
            yield return spriteMaterialUnlit = Resources.Load<Material>("UnlitSprites");
            yield return spriteMaterial_DestructiblesLit = Resources.Load<Material>("MyLighting_Destructible");
            yield return spriteMaterial_DestructiblesUnLit = Resources.Load<Material>("UnlitSprites_Destructible");
#endif
        }

        yield return spriteMaterialGreyscale = Resources.Load<Material>("Textures Materials/LitBWSprites");
        yield return spriteMaterialHologram = Resources.Load<Material>("Textures Materials/HologramMat");
        yield return spriteMaterialFloorSlime = Resources.Load<Material>("SlimeLighting");

        IncrementLoadingBar(ELoadingBarIncrementValues.small);
        GetStringsFromAbilityXML();
        DTPooling.Initialize();
        GameLogDataPackages.Initialize();

        //if (Debug.isDebugBuild) Debug.Log("Preparing to load all abilities.");

        yield return LoadAllAbilities();
        IncrementLoadingBar(ELoadingBarIncrementValues.medium);


        if (masterMonsterList == null)
        {
            yield return LoadAllMonsters();
        }
        IncrementLoadingBar(ELoadingBarIncrementValues.small);

        bool statusesAlreadyLoaded = false;

        float timeAtStatusStart = Time.realtimeSinceStartup;

        if (masterStatusList == null)
        {
            yield return LoadAllStatusEffects();
        }
        else
        {
            //Debug.Log("Statuses already loaded.");
            statusesAlreadyLoaded = true;
        }
        IncrementLoadingBar(ELoadingBarIncrementValues.medium);

        if (!statusesAlreadyLoaded)
        {
            // Now, link up any ability effects that may not have been loaded.
            foreach (AbilityScript abil in masterAbilityList.Values)
            {
                if (string.IsNullOrEmpty(abil.iconSprite) || string.IsNullOrEmpty(abil.description))
                {
                    abil.displayInList = false;
                }
                abil.ConnectMissingReferencesAtLoad();
                abil.ParseNumberTags();
            }
            foreach (StatusEffect se in masterStatusList.Values)
            {
                se.ConnectMissingReferencesAtLoad();
                se.ParseNumberTags();
            }
        }

        if (masterMagicModList == null)
        {
            yield return LoadAllMagicMods();
        }

        IncrementLoadingBar(ELoadingBarIncrementValues.small);
        LegendaryMaker.Initialize();

        foreach (MagicMod mm in masterMagicModList.Values)
        {
            mm.ParseNumberTags();
        }

        float timeAtItemStart = Time.realtimeSinceStartup;

        if (masterItemList == null)
        {
            yield return LoadAllItems();
            if (Debug.isDebugBuild) Debug.Log("<color=green>Finished loading all items!</color>");
        }

        IncrementLoadingBar(ELoadingBarIncrementValues.small);
        foreach (Item itm in masterItemList.Values)
        {
            itm.ParseNumberTags();
        }
        foreach (GearSet gs in masterGearSetList)
        {
            gs.ParseNumberTags();
        }

        CreateSpecialEvocations();

        //if (Debug.isDebugBuild) Debug.Log("Special evocations created.");

        regenFlaskAbility = AbilityScript.GetAbilityByName("skill_regenflask");
        escapeTorchAbility = AbilityScript.GetAbilityByName("skill_escapedungeon");

        kickDummy = GetItemFromRef("weapon_kick") as Weapon;

        if (masterLootTables == null)
        {
            yield return LoadAllLootTables();
            LootGeneratorScript.AutoAddPlayerModItemsToTables();
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Loot tables already loaded.");
        }
        IncrementLoadingBar(ELoadingBarIncrementValues.small);

        if (masterSpawnTableList == null)
        {
            yield return LoadAllSpawnTables();
        }
        else
        {
            //if (Debug.isDebugBuild) Debug.Log("Spawn tables already loaded.");
        }


        if (masterConversationList == null)
        {
            yield return LoadAllDialogs();
            IncrementLoadingBar(ELoadingBarIncrementValues.small);

            FontManager.DialogLocalizationTweaks();
        }

            if (masterNPCList == null)
            {
                yield return LoadAllShops();
                yield return LoadAllNPCs();
                foreach (Item itm in masterItemList.Values)
                {
                    if (itm.addToShopRefs == null) continue;
                    foreach (string sRef in itm.addToShopRefs.Keys)
                    {
                        ActorTable aTable;
                        if (masterShopTableList.TryGetValue(sRef, out aTable))
                        {
                            aTable.AddToTable(itm.actorRefName, itm.addToShopRefs[sRef]);
                            //Debug.Log("Added " + itm.addToShopRefs[sRef] + " of " + itm.actorRefName + " to " + sRef);
                            if (!aTable.actors.Contains(itm))
                            {
                                aTable.actors.Add(itm); // Ehh this is maybe inefficient
                            }
                        }
                    }
                }
            }


            if (masterDungeonRoomlist == null)
            {
                yield return LoadAllDungeonRooms();
            }

        //if (Debug.isDebugBuild) Debug.Log("All dungeon rooms loaded");


            if (masterJobList == null)
            {
                yield return LoadAllJobs();
                allJobsLoaded = true;
            }

            foreach (CharacterJobData cjd in masterJobList)
            {
                cjd.ParseNumberTags();
            }

            if (masterSharaPowerList.Count == 0)
            {
                foreach (CharacterJobData cjd in masterJobList)
                {
                    if (cjd.jobEnum == CharacterJobs.MIRAISHARA) continue;

                    foreach (JobAbility ja in cjd.GetBaseJobAbilities())
                    {
                        if (ja.innate) continue;
                        if (ja.jobMasterAbility) continue;
                        if (ja.jpCost == 0) continue;
                        if (SharaModeStuff.disallowSharaModeRegularSkills.Contains(ja.abilityRef)) continue;
                        if (!masterSharaPowerList.ContainsKey(ja.abilityRef))
                        {
                            masterSharaPowerList.Add(ja.abilityRef, ja.ability);
                        }
                    }
                }
            }

            if (masterMapObjectDict == null)
            {
                yield return LoadAllMapObjects();
            }

            IncrementLoadingBar(ELoadingBarIncrementValues.small);

            // where else to put this? I dunno        

            PlayerModManager.AdjustGameBalanceFromModFiles();

            globalMagicItemChance *= PlayerModManager.GetBalanceAdjustment(BalanceAdjustments.MAGIC_ITEM_CHANCE);

            if (masterDungeonLevelList == null)
            {
                yield return LoadAllMapGenerationData();
            }

        //if (Debug.isDebugBuild) Debug.Log("Map gen data loaded");

        IncrementLoadingBar(ELoadingBarIncrementValues.small);
            if (masterChampionDataDict == null)
            {
                yield return LoadAllChampionData();
            }



            maxJPAllJobs = new int[(int)CharacterJobs.COUNT - 2];

            for (int x = 0; x < (int)CharacterJobs.COUNT - 2; x++)
            {
                CharacterJobData cjd = CharacterJobData.GetJobData(((CharacterJobs)x).ToString());
                if (cjd == null) continue;
                int jobJPMax = 0;
                int baseJobAbilCount = cjd.GetBaseJobAbilities().Count;
                for (int i = 0; i < baseJobAbilCount; i++)
                {
                    if (cjd.GetBaseJobAbilities()[i].jpCost > 0)
                    {
                        jobJPMax += cjd.GetBaseJobAbilities()[i].jpCost;
                    }
                }
                maxJPAllJobs[x] = jobJPMax;
            }

            CreateBakedGameDataObjectTemplates();

            // Below was in the Start() routine

            GameObject go;

            statsAndAchievements = GetComponent<SteamStatsAndAchievements>();
            deadActorsToSaveAndLoad = new List<Actor>();
            dictTempStringData = new Dictionary<string, string>();
            dictTempFloatData = new Dictionary<string, float>();
            dictTempGameObjects = new Dictionary<string, GameObject>();

if (!PlatformVariables.GAMEPAD_ONLY)
{
            cMapper = GameObject.Find("ControlMapper").GetComponent<ControlMapper>();

            if (cMapper != null)
            {
                cMapper.restoreDefaultsDelegate += OnRestoreDefaults;
            }
}

            SpriteEffectSystem.Initialize();
            player = ReInput.players.GetPlayer(0);

            reusableStringBuilder = new StringBuilder();


            if (PlayerOptions.animSpeedScale != 0f)
            {
                playerMoveSpeed *= PlayerOptions.animSpeedScale;
            }

            if (PlayerOptions.turnSpeedScale != 0f)
            {
                movementInputDelayTime *= PlayerOptions.turnSpeedScale;
            }

            if (PlayerOptions.animSpeedScale != 0f)
            {
                baseAttackAnimationTime *= PlayerOptions.animSpeedScale;
            }

            baseAttackAnimationTime = attackAnimationTime;

            go = GameObject.Find("MapMaster");
            canvasObject = GameObject.Find("Canvas");
            mms = go.GetComponent<MapMasterScript>();
            go = GameObject.Find("TutorialManager");
            tutorialManager = go.GetComponent<TutorialManagerScript>();
            go = GameObject.Find("Main Camera");
            cameraScript = go.GetComponent<CameraController>();
            go = GameObject.Find("CombatManager");
            combatManager = go.GetComponent<CombatManagerScript>();
            go = GameObject.Find("UIManager");
            uims = go.GetComponent<UIManagerScript>();
            go = GameObject.Find("AudioManager");
            musicManager = go.GetComponent<MusicManagerScript>();
            go = GameObject.Find("MonsterManager");

            theCasino = new CasinoScript();
            deadQueue = new Queue<Actor>();

            // Now run first update stuff.
            cameraScript.UpdateFOVFromOptionsValue();
            cameraScript.CheckLockToPlayer();
            UpdateFrameCapFromOptionsValue();
            UpdateCursorRepeatDelayFromOptionsValue();

            //if (Debug.isDebugBuild) Debug.Log("Preparing to load resources.");

            // We can't initialize the UI until all resources are *guaranteed* to be loaded 
            while (!allResourcesLoaded)
            {
                yield return null;
            }

            //if (Debug.isDebugBuild) Debug.Log("All resources loaded.");

            yield return uims.MainGameStart();
            HotbarHelper.Initialize(); // Sets up components needed for hotbar animation.

            musicManager.MusicManagerStart();


            if (GameStartData.currentChallengeData == null || GameStartData.challengeType == ChallengeTypes.COUNT || GameStartData.challengeType != ChallengeTypes.NONE)
            {
                MetaProgressScript.defeatHistory.Clear();

                yield return MetaProgressScript.LoadMetaProgress();

                LegendaryMaker.Initialize();

                MetaProgressScript.FlushUnusedCustomDataIfNecessary();
            }

            IncrementLoadingBar(0.1f);
            yield return null;
            mms.MainMapStart();

            if (UIManagerScript.globalDialogButtonResponse == DialogButtonResponse.LOADGAME)
            {
                framesToLoadGame = 5;
            }
            else
            {
                if (GameStartData.GetGameMode() == GameModes.HARDCORE)
                {
                    if (Debug.isDebugBuild) Debug.Log("Starting a NEW character in hardcore. Time to delete meta progress.");
                    MetaProgressScript.FlushAllDataExceptHardcoreProgress();
                }
            }

        if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            musicManager.FillStackWithTracksToLoad();
        }
        for (int i = 0; i < 30; i++)
        {
            yield return null;
        }

#if UNITY_EDITOR
        //Debug.Log("<color=green>Basic awake+start sequence done.</color>");
#endif
        initialGameAwakeComplete = true;

    }

    void CreateSpecialEvocations()
    {
        if (spellshaperEvocationEffects == null)
        {
            spellshaperEvocationEffects = new List<EffectScript>();
            AbilityScript specialEvo = AbilityScript.GetAbilityByName("skill_fireevocation");
            for (int i = 0; i < specialEvo.listEffectScripts.Count; i++)
            {
                spellshaperEvocationEffects.Add(specialEvo.listEffectScripts[i]);
            }

            specialEvo = AbilityScript.GetAbilityByName("skill_iceevocation");
            for (int i = 0; i < specialEvo.listEffectScripts.Count; i++)
            {
                spellshaperEvocationEffects.Add(specialEvo.listEffectScripts[i]);
            }

            specialEvo = AbilityScript.GetAbilityByName("skill_shadowevocation");
            for (int i = 0; i < specialEvo.listEffectScripts.Count; i++)
            {
                spellshaperEvocationEffects.Add(specialEvo.listEffectScripts[i]);
            }

            specialEvo = AbilityScript.GetAbilityByName("skill_acidevocation");
            for (int i = 0; i < specialEvo.listEffectScripts.Count; i++)
            {
                spellshaperEvocationEffects.Add(specialEvo.listEffectScripts[i]);
            }
        }
    }
}