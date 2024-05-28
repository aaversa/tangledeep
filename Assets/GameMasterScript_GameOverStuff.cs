using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    using UnityEngine.Analytics;
#endif

public partial class GameMasterScript : MonoBehaviour
{

    public IEnumerator WaitThenContinueAdventureModeGameOver(float time)
    {
        playerDied = true;
        yield return new WaitForSeconds(time);

        SetAnimationPlaying(false);

        TickGameTime(1, true);
        /* MetaProgressScript.totalDaysPassed += 1; 
        MetaProgressScript.AgeAllTrees(1); */

        float trackJPLost = 0;
        if (SharaModeStuff.IsSharaModeActive())
        {
            trackJPLost = heroPCActor.jobJP[(int)CharacterJobs.SHARA] * 0.5f;
            heroPCActor.jobJP[(int)CharacterJobs.SHARA] = trackJPLost;
        }
        else
        {
            for (int i = 0; i < (int)CharacterJobs.COUNT - 2; i++)
            {
                float halfsies = heroPCActor.jobJP[i] * 0.5f;
                trackJPLost += halfsies;
                heroPCActor.jobJP[i] = halfsies;
            }
        }

        float halfMoney = heroPCActor.GetMoney() / 2f;
        int loseMoney = (int)(-1 * halfMoney);
        heroPCActor.ChangeMoney(loseMoney);

        int baseXP = HeroPC.GetXPCurve(heroPCActor.myStats.GetLevel() - 1);

        int curXPOverBase = heroPCActor.myStats.GetXP() - baseXP;

        heroPCActor.myStats.SetXPFlat(baseXP);
        heroPCActor.myStats.ChangeExperience(curXPOverBase / 2, false);

        UIManagerScript.ClearConversation();

        UIManagerScript.currentConversation = new Conversation();

        GameLogScript.LogWriteStringRef("log_event_knockedout");

        //UIManagerScript.ToggleDialogBox(DialogType.KNOCKEDOUT, true, false);
        //UIManagerScript.SetDialogPos(0, 0);
        heroPCActor.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
        UIManagerScript.RefreshPlayerStats();

        string text = "";

        if (heroPCActor.whoKilledMe != null)
        {
            StringManager.SetTag(0, heroPCActor.whoKilledMe.displayName);
        }
        else
        {
            StringManager.SetTag(0, "?????");
        }

        StringManager.SetTag(1, UIManagerScript.uiDungeonName.text);


        string textBuilder = "";
        if (SharaModeStuff.IsSharaModeActive())
        {
            //"She escaped, ain't she great"
            textBuilder = StringManager.GetString("exp_shara_ko") + "\n\n";

            //"She lost this much money and spent this much JP recovering
            StringManager.SetTag(0, ((int)halfMoney).ToString());
            StringManager.SetTag(1, ((int)trackJPLost).ToString());
            textBuilder += StringManager.GetString("exp_shara_ko_result") + "\n\n";
        }
        else
        {
            textBuilder = StringManager.GetString("desc_knockout_actor") + "\n\n";
            StringManager.SetTag(0, ((int)halfMoney).ToString());
            textBuilder += StringManager.GetString("desc_die_adventuremode") + "\n\n";

        }

        textBuilder += PlayerAdvice.GetAdviceStringForPlayer();

        StringManager.SetTag(0, textBuilder);

        UIManagerScript.CloseDialogBox();
        UIManagerScript.StartConversationByRef("gameover_ko", DialogType.KNOCKEDOUT, null, false, "", true);
        UIManagerScript.OverrideDialogWidth(1700f, 660f);

        if (SharaModeStuff.IsSharaModeActive())
        {
            heroPCActor.SetActorData("shara_defeated", 1);
        }
    }

    public IEnumerator WaitThenContinueGameOver(float time)
    {
        GameMasterScript.gmsSingleton.CurrentSaveGameState = SaveGameState.SAVE_IN_PROGRESS;

        yield return new WaitForSeconds(time);

        SetAnimationPlaying(false);

        if (gameMode != GameModes.HARDCORE)
        {
            TickGameTime(7, false);
        }

        UIManagerScript.ClearConversation();
        GameLogScript.LogWriteStringRef("log_event_playerdied");
        playerDied = true;
        heroPCActor.myStats.SetStat(StatTypes.HEALTH, 0f, StatDataTypes.CUR, false);
        UIManagerScript.RefreshPlayerStats();
        string text = "";

        StringManager.SetTag(0, heroPCActor.displayName);
        if (SharaModeStuff.IsSharaModeActive())
        {
            text = StringManager.GetString("exp_shara_died") + "\n\n";
        }
        else
        {
            switch (gameMode)
            {
                case GameModes.NORMAL:
                    text = StringManager.GetString("desc_died_heroic") + "\n\n";
                    break;
                case GameModes.HARDCORE:
                    text = StringManager.GetString("desc_died_hardcore") + "\n\n";
                    break;
            }
        }

        if (heroPCActor.whoKilledMe != null)
        {
            StringManager.SetTag(0, heroPCActor.whoKilledMe.displayName);
        }
        else
        {
            StringManager.SetTag(0, "?????");
        }

        StringManager.SetTag(1, UIManagerScript.uiDungeonName.text);
        text += StringManager.GetString("desc_defeated_actor") + "\n\n";
        StringManager.SetTag(0, heroPCActor.myJob.DisplayName);
        StringManager.SetTag(1, heroPCActor.myStats.GetLevel().ToString());
        text += StringManager.GetString("desc_died_joblevel") + "\n";
        StringManager.SetTag(0, heroPCActor.stepsTaken.ToString());
        text += StringManager.GetString("desc_died_stepstaken") + "\n";
        StringManager.SetTag(0, heroPCActor.monstersKilled.ToString());
        text += StringManager.GetString("desc_died_monstersdefeated") + "\n";
        StringManager.SetTag(0, heroPCActor.championsKilled.ToString());
        text += StringManager.GetString("desc_died_championsdefeated") + "\n";
        StringManager.SetTag(0, (heroPCActor.lowestFloorExplored + 1).ToString());
        text += StringManager.GetString("desc_died_highestfloor") + "\n\n";

        DefeatData dd = new DefeatData();
        dd.InitializeFromHeroData();
        MetaProgressScript.AddDefeatData(dd);

        int numHealingItems = 0;
        foreach (Item itm in heroPCActor.myInventory.GetInventory())
        {
            if (itm.IsCurative(StatTypes.HEALTH))
            {
                Consumable con = itm as Consumable;
                numHealingItems += con.Quantity;
                //numHealingItems++;
            }
        }

        

        text += PlayerAdvice.GetAdviceStringForPlayer();

        StringManager.SetTag(0, text);

        UIManagerScript.CloseDialogBox();
        UIManagerScript.StartConversationByRef("gameover_forreal", DialogType.GAMEOVER, null, false, "", true);
        UIManagerScript.OverrideDialogWidth(1750f, 700f);
        
        bool saveSuccess = true;
        if (gameMode == GameModes.HARDCORE)
        {
            SharedBank.RemoveRelicsFromHeroOnGameOver();
            gmsSingleton.TrySaveMinimalMetaProgress(); 
        }
        else
        {
            /* try 
            {                 
                
            }
            catch (Exception e)
            {
                Debug.Log("Meta progress save issue: " + e);
                saveSuccess = false;
            } */

            yield return gmsSingleton.TrySaveMetaProgress();             
        }

        yield return SharedBank.TrySaveSharedProgress();

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (saveSuccess)
        {
            string sPath = CustomAlgorithms.GetPersistentDataPath() + "/metaprogressCopy.xml";
            string path2 = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";
            File.Copy(sPath, path2, true);
        }
#endif

        GameMasterScript.gmsSingleton.CurrentSaveGameState = SaveGameState.NOT_SAVING;
    }

    public static void GameOver()
    {
        UIManagerScript.singletonUIMS.CloseAllDialogs();
        if (playerDied) return;
        playerDied = true;

        lastAutoSaveTime = Time.realtimeSinceStartup - 301.0;
        // New: IMMEDIATELY delete files.
        //Shep: Switch save/load

#if UNITY_SWITCH
        var sdh = Switch_SaveDataHandler.GetInstance();

        if (!gmsSingleton.adventureModeActive && gmsSingleton.gameMode != GameModes.ADVENTURE && heroPCActor.ReadActorData("advm") != 1)
        {
            //delete the save game
            sdh.DeleteSwitchDataFile("savedGame" + +GameStartData.saveGameSlot + ".xml");
            //delete the map
            sdh.DeleteSwitchDataFile("savedMap" + +GameStartData.saveGameSlot + ".dat");
        }

#elif UNITY_PS4
        if (!gmsSingleton.adventureModeActive && gmsSingleton.gameMode != GameModes.ADVENTURE && heroPCActor.ReadActorData("advm") != 1)
        {           
            //delete the save game
            PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedGame" + GameStartData.saveGameSlot + ".xml");
            //delete the map
            PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "savedMap" + GameStartData.saveGameSlot + ".dat");            
        }
#elif UNITY_XBOXONE
        if (!gmsSingleton.adventureModeActive && gmsSingleton.gameMode != GameModes.ADVENTURE && heroPCActor.ReadActorData("advm") != 1)
        {
            //delete the save game
            XboxSaveManager.instance.DeleteKey("savedGame" + GameStartData.saveGameSlot + ".xml");
            //delete the map
            XboxSaveManager.instance.DeleteKey("savedMap" + GameStartData.saveGameSlot + ".dat");
            XboxSaveManager.instance.Save();
        }
#else
        string path = CustomAlgorithms.GetPersistentDataPath() + "/savedGame" + GameStartData.saveGameSlot + ".xml";
        if (File.Exists(path) && !gmsSingleton.adventureModeActive && gmsSingleton.gameMode != GameModes.ADVENTURE && GameMasterScript.heroPCActor.ReadActorData("advm") != 1)
        {
            File.Delete(path);
        }
        path = CustomAlgorithms.GetPersistentDataPath() + "/savedMap" + GameStartData.saveGameSlot + ".dat";
        if (File.Exists(path) && !gmsSingleton.adventureModeActive && gmsSingleton.gameMode != GameModes.ADVENTURE && GameMasterScript.heroPCActor.ReadActorData("advm") != 1)
        {
            File.Delete(path);
        }
        MetaProgressScript.FlushUnusedCustomDataIfNecessary(true);
#endif

        if (DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) && gmsSingleton.gameMode != GameModes.ADVENTURE)
        {
            MysteryDungeonManager.MarkAllRelicsOnHeroForRemovalOnHardcoreOrHeroicDeath();
        }

        //if hardcore, delete metaprogress
        if (gmsSingleton.gameMode == GameModes.HARDCORE)
        {

#if UNITY_SWITCH
            sdh.DeleteSwitchDataFile("metaprogress" + GameStartData.saveGameSlot + ".xml");
#elif UNITY_PS4
            PS4SaveManager.instance.DeleteFile(PS4SaveManager.ROOT_DIR, "metaprogress" + GameStartData.saveGameSlot + ".xml");
#elif UNITY_XBOXONE
            XboxSaveManager.instance.DeleteKey("metaprogress" + GameStartData.saveGameSlot + ".xml");
            XboxSaveManager.instance.Save();
#else

            MetaProgressScript.totalDaysPassed = 0;
            MetaProgressScript.dictMetaProgress.Remove("ancientcube");
            path = CustomAlgorithms.GetPersistentDataPath() + "/metaprogress" + GameStartData.saveGameSlot + ".xml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
#endif
        }
        else
        {

        }


        UIManagerScript.FlashRed(1.25f);
        cameraScript.SetToGrayscale(true);
        MusicManagerScript.RequestPlayLoopingMusicImmediatelyFromScratchWithCrossfade("gameover");

        MonsterCorralScript.ReturnPlayerPetToCorralAfterDeath();

        heroPCActor.myAnimatable.StopAnimation();

        int numHotbarSlotsUsed = GameMasterScript.heroPCActor.GetNumHotbarSlotsUsed();

        if (PlatformVariables.SEND_UNITY_ANALYTICS)
        {
            #if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
                    Dictionary<string, object> gameOverInfo = new Dictionary<string, object>();
                gameOverInfo.Add("hbslotsused", numHotbarSlotsUsed);
                gameOverInfo.Add("plvl", heroPCActor.myStats.GetLevel());
                gameOverInfo.Add("floor", heroPCActor.dungeonFloor);
                gameOverInfo.Add("job", heroPCActor.myJob.DisplayName);
                gameOverInfo.Add("mode", gmsSingleton.gameMode.ToString());
                gameOverInfo.Add("ngplus", GameStartData.saveSlotNGP[GameStartData.saveGameSlot]);
                gameOverInfo.Add("weaptype", heroPCActor.myEquipment.GetWeaponType());
                if (GameMasterScript.heroPCActor.whoKilledMe == null || heroPCActor.whoKilledMe.GetActorType() != ActorTypes.MONSTER)
                {
                    gameOverInfo.Add("whokilled", "unknown");
                }
                else
                {
                    Monster mn = heroPCActor.whoKilledMe as Monster;
                    gameOverInfo.Add("killer_ref", heroPCActor.whoKilledMe.actorRefName);
                    gameOverInfo.Add("killer_lvl", mn.myStats.GetLevel());
                    gameOverInfo.Add("killer_champ", mn.isChampion);
                }

                Analytics.CustomEvent("gameover", gameOverInfo);
            #endif
        }

        if (gmsSingleton.adventureModeActive || heroPCActor.ReadActorData("advm") == 1 || gmsSingleton.gameMode == GameModes.ADVENTURE)
        {
            gmsSingleton.StartCoroutine(gmsSingleton.WaitThenContinueAdventureModeGameOver(1.25f));
        }
        else
        {
            gmsSingleton.StartCoroutine(gmsSingleton.WaitThenContinueGameOver(1.25f));
        }

    }
}
