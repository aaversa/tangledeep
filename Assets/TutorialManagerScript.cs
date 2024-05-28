using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class TutorialManagerScript : MonoBehaviour {

    public GameObject hotbarHighlight;
    public bool tutorialTouchedFirstItem = false;
    public bool tutorialDroppedFirstPowerup = false;
    public bool tutorialTouchedFirstStairs = false;
    public bool tutorialTouchedFirstRangedWeapon = false;
    public bool tutorialHotbarHighlight = false;
    public bool tutorialGotJPForAbility = false;

    private Dictionary<string, TutorialData> allTutorials;

    public static List<string> allGameTips;

    public static List<string> watchedTutorials; // Strings read from save. Match up with conversations read from XML.

    const int SHARA_MODE_TIPS_COUNT = 8;

    /// <summary>
    /// The last turn a powerup was picked up (auto used)
    /// </summary>
    public static int powerUpFoundOnTurn;

    /// <summary>
    /// The last turn the player hit a ranged monster in melee
    /// </summary>
    public static int hitRangedMonsterInMeleeTurn;

    /// <summary>
    /// How many times ranged monsters have hit the player with ranged attacks
    /// </summary>
    public static int rangedMonsterHitPlayerCount;

    /// <summary>
    /// The last turn the player defeated a dream
    /// </summary>
    public static int turnDreamDefeated;

    public static bool markedItemAsFavoriteOrTrash;

    /// <summary>
    /// Player has typed something into the search bar.
    /// </summary>
    public static bool searchBarUsed;

    static bool checkedForSearchBarTutorialThisSession;

    static bool doSearchbarTutorialAtEndOfNextTurn;

    static TutorialManagerScript singleton;

    void Start()
    {
        watchedTutorials = new List<string>();
        singleton = this;
    }

    public static void InitializeGameTipList()
    {
        if (allGameTips != null) return;
        allGameTips = new List<string>();
    }

    public static void AddGameTip(string tipRef)
    {
        allGameTips.Add(tipRef);
    }

    public static string GetNextGameTip()
    {
        if (allGameTips.Count == 0)
        {
            if (Debug.isDebugBuild) Debug.Log("No game tips? Why");
            return "game_tips_1";
        }

        int tipNum = TDPlayerPrefs.GetInt(GlobalProgressKeys.LAST_TIP);
        int maxTips = allGameTips.Count;

        bool sharaMode = SharaModeStuff.IsSharaModeActive();

        if (sharaMode)
        {
            maxTips = SHARA_MODE_TIPS_COUNT-1;
        }

        if (tipNum >= maxTips)
        {
            tipNum = 0;
        }
        string strToReturn = allGameTips[tipNum];

        if (sharaMode)
        {
            strToReturn = "exp_sharamode_tips" + tipNum;
        }

        tipNum++;
        if (tipNum >= maxTips)
        {
            tipNum = 0;
        }
        TDPlayerPrefs.SetInt(GlobalProgressKeys.LAST_TIP, tipNum);
        return strToReturn;
    }

    public class TutorialData
    {
        public Conversation tutorialRef;
        public bool viewed;
        public int index;

        public TutorialData(Conversation tut, int nIndex)
        {
            tutorialRef = tut;
            viewed = false;
            index = nIndex;
        }
    }

    // Use this for initialization

    public static void ReadFromSave(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch(reader.Name.ToLowerInvariant())
            {
                case "tut":
                    string tutName = reader.ReadElementContentAsString();
                    watchedTutorials.Add(tutName);
                    break;
                default:
                    reader.Read();
                    break;
            }
        }
        reader.ReadEndElement();
    }

    public static void WriteToSave(XmlWriter writer)
    {
        writer.WriteStartElement("tutswatched");

        foreach(string tut in watchedTutorials)
        {
            writer.WriteElementString("tut", tut);
        }

        writer.WriteEndElement();
    }

    public void TutorialStart()
    {
        hotbarHighlight = GameObject.Find("HotbarHighlight");
        allTutorials = new Dictionary<string, TutorialData>();
        TutorialData td;
        int counter = 0;
        foreach(Conversation convo in GameMasterScript.masterTutorialList)
        {
            td = new TutorialData(convo,counter);
            counter++;
            allTutorials.Add(td.tutorialRef.refName, td);

            if (watchedTutorials.Contains(td.tutorialRef.refName))
            {
                td.viewed = true;
            }

            /* if (PlayerPrefs.GetInt(td.tutorialRef.refName) == 1)
            {
                td.viewed = true;
            }  */
        }
    }

    public bool WatchedTutorial(string refName)
    {

        // During Challenges, we don't ever need to display tutorial messages.
        if (GameStartData.challengeType == ChallengeTypes.WEEKLY || GameStartData.challengeType == ChallengeTypes.DAILY || PlayerOptions.speedrunMode)
        {
            return true;
        }


        TutorialData watchedTut;
        if (allTutorials.TryGetValue(refName, out watchedTut))
        {
            return watchedTut.viewed;
        }
        return false;
    }

    public void MarkTutorialWatched(string refName)
    {
        TutorialData td;
        if (allTutorials.TryGetValue(refName, out td))
        {
            td.viewed = true;
            if (!watchedTutorials.Contains(td.tutorialRef.refName))
            {
                watchedTutorials.Add(td.tutorialRef.refName);
            } 
            //PlayerPrefs.SetInt(td.tutorialRef.refName, 1);
            return;
        }
        Debug.Log("Couldn't find tutorial " + refName);
    }

    public void ViewHelp()
    {
        UIManagerScript.ForceCloseFullScreenUIWithNoFade();
        Conversation tut = GameMasterScript.FindConversation("tutorial"); // was viewalltutorials
        if (tut == null)
        {
            return;
        }
        UIManagerScript.StartConversation(tut, DialogType.STANDARD, null);
    }

    public static void ResetTutorialData()
    {
        foreach (TutorialData td in singleton.allTutorials.Values)
        {
            td.viewed = false;
            //PlayerPrefs.SetInt(td.tutorialRef.refName, 0);
        }
        TDPlayerPrefs.SetInt(GlobalProgressKeys.ABIL_LEARNED_EVER, 0);
        MarkTutorialsUnwatched();
    }

    public static void MarkTutorialsUnwatched()
    {
        watchedTutorials.Clear();
    }

    public Conversation GetTutorialAndMarkAsViewed(string refName)
    {
        TutorialData td;

        if (allTutorials.TryGetValue(refName, out td))
        {
            td.viewed = true;
            //PlayerPrefs.SetInt(td.tutorialRef.refName, 1);
            if (!watchedTutorials.Contains(td.tutorialRef.refName))
            {
                watchedTutorials.Add(td.tutorialRef.refName);
            }
            return td.tutorialRef;
        }
        
        Debug.Log("Couldn't find tutorial " + refName);
        return null;
    }

    public void CheckForEndOfTurnTutorials()
    {
        bool forceTutorial = false;

        if (!forceTutorial && !PlayerOptions.tutorialTips) return;
        if (MapMasterScript.activeMap.floor == MapMasterScript.TOWN_MAP_FLOOR)
        {
            if (WatchedTutorial("tutorial_autocook")) return;
            if (MetaProgressScript.recipesKnown.Count == 0) return;
            
            Actor campfire = MapMasterScript.activeMap.FindActor("npc_cookingfire");
            if (MapMasterScript.GetGridDistance(GameMasterScript.heroPCActor.GetPos(), campfire.GetPos()) < 4)
            {
                Conversation cookTutorial = GetTutorialAndMarkAsViewed("tutorial_autocook");
                UIManagerScript.StartConversation(cookTutorial, DialogType.TUTORIAL, null);
            }            
        }
        else if (forceTutorial || (!MapMasterScript.activeMap.IsTownMap() && MapMasterScript.activeMap.effectiveFloor >= 2 && !SharaModeStuff.IsSharaModeActive()))
        {
            if (!forceTutorial && MetaProgressScript.ReadMetaProgress("portalused") == 1) return;
            int stepsAtStartOfFloor = GameMasterScript.heroPCActor.ReadActorData("steps_startfloor");
            if (forceTutorial || GameMasterScript.heroPCActor.stepsTaken - stepsAtStartOfFloor >= 25)
            {
                if (!forceTutorial && WatchedTutorial("tutorial_returntotown")) return;
                StringManager.SetTag(4, StringManager.GetPortalBindingString());
                Conversation portalTut = GetTutorialAndMarkAsViewed("tutorial_returntotown");
                UIManagerScript.StartConversation(portalTut, DialogType.TUTORIAL, null);
            }

        }
    }

    public void CheckForAnyTutorialsThisTurn()
    {
        HeroPC heroPCActor = GameMasterScript.heroPCActor;

        bool tutThisTurn = false;

        bool anyActiveMap = MapMasterScript.activeMap != null;

        if (anyActiveMap && MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_FOREST_FLOOR)
        {
            return;
        }

        if (anyActiveMap && MapMasterScript.activeMap.floor == MapMasterScript.TUTORIAL_FLOOR)
        {
            if ((heroPCActor.GetPos().x == 14 || heroPCActor.GetPos().x == 15) && heroPCActor.GetPos().y == 8)
            {
                if (!WatchedTutorial("tutorial_wait") && PlayerOptions.tutorialTips)
                {
                    if (PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
                    {
                        GameMasterScript.gmsSingleton.SetTempStringData("help_diagonalwait", StringManager.GetString("dialog_tutorial_wait_main_txt_switch", true));
                    }
                    else
                    {
                        GameMasterScript.gmsSingleton.SetTempStringData("help_diagonalwait", StringManager.GetString("dialog_tutorial_wait_main_txt", true));
                    }                    
                    Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_wait");
                    UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                    tutThisTurn = true;
                }
            }
        }

        bool forceTutorial = false;

        if (forceTutorial || (!tutThisTurn && heroPCActor.myStats.GetCurStatAsPercentOfMax(StatTypes.HEALTH) <= 0.5f))
        {
            if (forceTutorial || (!WatchedTutorial("tutorial_flask") && PlayerOptions.tutorialTips && !SharaModeStuff.IsSharaModeActive()))
            {
                if (PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
                {
                    GameMasterScript.gmsSingleton.SetTempStringData("help_flask", StringManager.GetString("dialog_tutorial_flask_main_txt_switch", true));                    
                }
                else
                {
                    GameMasterScript.gmsSingleton.SetTempStringData("help_flask", StringManager.GetString("dialog_tutorial_flask_main_txt", true));
                }
                
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_flask");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                tutThisTurn = true;
            }
        }

        if (anyActiveMap && MapMasterScript.activeMap.IsTownMap()) // We're in town
        {
            if (heroPCActor.GetPos().y <= 4f && GameMasterScript.heroPCActor.myStats.GetLevel() == 1 && ProgressTracker.CheckProgress(TDProgress.TOWN_GENTLE_POINTER, ProgressLocations.META) != 1
                && !GameMasterScript.heroPCActor.mapFloorsExplored.Contains(0) && !GameMasterScript.heroPCActor.mapFloorsExplored.Contains(200)
                && !GameMasterScript.heroPCActor.mapFloorsExplored.Contains(150) && !GameMasterScript.heroPCActor.mapFloorsExplored.Contains(350))
            {
                tutThisTurn = true;
                Conversation helpfulPointer = GameMasterScript.FindConversation("town_helpful_pointer");
                ProgressTracker.SetProgress(TDProgress.TOWN_GENTLE_POINTER, ProgressLocations.META, 1);
                UIManagerScript.StartConversation(helpfulPointer, DialogType.STANDARD, null);
            }

            if (heroPCActor.GetPos().x == 20f && heroPCActor.GetPos().y == 11f)
            {
                if (heroPCActor.ReadActorData("startfood") != 1)
                {
                    heroPCActor.SetActorData("startfood", 1);
                    Consumable startFood1 = LootGeneratorScript.CreateItemFromTemplateRef("food_meatkebabs", 1.0f, 0f, false) as Consumable;
                    Consumable startFood2 = LootGeneratorScript.CreateItemFromTemplateRef("food_campfiremeat", 1.0f, 0f, false) as Consumable;
                    Consumable startFood3 = LootGeneratorScript.CreateItemFromTemplateRef("food_cheeseflan", 1.0f, 0f, false) as Consumable;
                    Consumable startFood4 = LootGeneratorScript.CreateItemFromTemplateRef("food_juicyapple", 1.0f, 0f, false) as Consumable;
                    Consumable startFood5 = LootGeneratorScript.CreateItemFromTemplateRef("food_juicyapple", 1.0f, 0f, false) as Consumable;
                    if (GameMasterScript.gmsSingleton.gameMode == GameModes.ADVENTURE)
                    {
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood1, true);
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood2, true);
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood3, true);
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood4, true);
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood5, true);
                        GameLogScript.LogWriteStringRef("log_julia_freeitems");
                    }
                    else if (GameMasterScript.gmsSingleton.gameMode == GameModes.NORMAL)
                    {
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood4, true);
                        heroPCActor.myInventory.AddItemRemoveFromPrevCollection(startFood5, true);
                        GameLogScript.LogWriteStringRef("log_julia_freeitems");
                    }
                }
                if (!MetaProgressScript.watchedFirstTutorial && GameStartData.currentChallengeData == null && !RandomJobMode.IsCurrentGameInRandomJobMode())
                {
                    MetaProgressScript.watchedFirstTutorial = true;
                    Cutscenes.JuliaTutorialCutscene();
                    tutThisTurn = true;
                }
            }
        }


        if (!tutThisTurn && PlayerOptions.tutorialTips && !GameMasterScript.playerIsResting
            && heroPCActor.stepsInDifficultTerrain >= 3 && PlayerOptions.tutorialTips && GameMasterScript.turnNumber >= 100
            && !MapMasterScript.activeMap.IsTownMap() && MapMasterScript.activeMap.GetTile(GameMasterScript.heroPCActor.GetPos()).IsTerrain())
        {
            if (!WatchedTutorial("tutorial_terrain"))
            {
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_terrain");
                UIManagerScript.StartConversation(newConvo, DialogType.TUTORIAL, null);
                tutThisTurn = true;
            }
        }

        if (!tutThisTurn && PlayerOptions.tutorialTips && (GameMasterScript.turnNumber - powerUpFoundOnTurn) <= 1 && GameMasterScript.turnNumber > 20)
        {
            if (MapMasterScript.activeMap.floor != MapMasterScript.TUTORIAL_FLOOR && !WatchedTutorial("tutorial_powerups") &&
                !MapMasterScript.activeMap.IsTownMap() &&
                (StringManager.gameLanguage != EGameLanguage.zh_cn) // temp conditional
                )
            {
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_powerups");
                UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
                tutThisTurn = true;
            }
        }

        if (!tutThisTurn && PlayerOptions.tutorialTips && ((GameMasterScript.turnNumber - hitRangedMonsterInMeleeTurn <= 1) || rangedMonsterHitPlayerCount > 5) && GameMasterScript.heroPCActor.lowestFloorExplored >= 2)
        {
            if (MapMasterScript.activeMap.floor != MapMasterScript.TUTORIAL_FLOOR && !WatchedTutorial("tutorial_meleefighting") &&
                (StringManager.gameLanguage != EGameLanguage.zh_cn) // temp conditional
                )
            {
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_meleefighting");
                UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
                tutThisTurn = true;
            }
        }

        if (!tutThisTurn && MapMasterScript.activeMap.floor == MapMasterScript.TOWN2_MAP_FLOOR && (GameMasterScript.turnNumber - turnDreamDefeated == 2) && GameMasterScript.turnNumber > 20) 
        {
            if (GameMasterScript.heroPCActor.ReadActorData("dreamsdefeated") >= 2 && !WatchedTutorial("tutorial_dreamdifficulty") &&
                (StringManager.gameLanguage != EGameLanguage.zh_cn) // temp conditional
                )
            {
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_dreamdifficulty");
                UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
                tutThisTurn = true;
            }
        }

		if (PlatformVariables.SHOW_SEARCHBARS)
		{
    	    if (!tutThisTurn && doSearchbarTutorialAtEndOfNextTurn)
	        {
	            doSearchbarTutorialAtEndOfNextTurn = false;
	            Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_searchbar");
	            UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
	        }
		}

        /* if (!tutThisTurn && markedItemAsFavoriteOrTrash && GameMasterScript.heroPCActor.lowestFloorExplored >= 1)
        {
            if (!WatchedTutorial("tutorial_organization"))
            {
                Conversation newConvo = GetTutorialAndMarkAsViewed("tutorial_organization");
                UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
                tutThisTurn = true;
            }
        } */
    }

    public void CheckForWardrobeTutorial()
    {
        if (StringManager.gameLanguage == EGameLanguage.zh_cn)
        {
            return; // temp conditional until localized
        }

        int jobsSpent = 0;
        for (int i = 0; i < GameMasterScript.heroPCActor.jobJPspent.Length; i++)
        {
            if (GameMasterScript.heroPCActor.jobJPspent[i] > 0)
            {
                jobsSpent++;
            }
            if (jobsSpent == 2) break;
        }

        if (jobsSpent == 2)
        {
            if (!WatchedTutorial("tutorial_wardrobe"))
            {
                MarkTutorialWatched("tutorial_wardrobe");
                Cutscenes.StartWardrobeCallout();
            }
        }
    }

    public bool CheckForMonsterLetterTutorial()
    {
        if (MetaProgressScript.localTamedMonstersForThisSlot.Count == 0) return false;
        if (WatchedTutorial("tutorial_monsterletters")) return false;

        if (StringManager.gameLanguage == EGameLanguage.zh_cn)
        {
            return false; // temp conditional until localized
        }

        bool tutorialPossible = false;

        int maxMonsterCount = MetaProgressScript.localTamedMonstersForThisSlot.Count <= 12 ? MetaProgressScript.localTamedMonstersForThisSlot.Count : MonsterCorralScript.MAX_MONSTERS_IN_CORRAL;

        //foreach (TamedCorralMonster tcm in MetaProgressScript.localTamedMonstersForThisSlot)
        for (int i = 0; i < maxMonsterCount; i++)
        {
            TamedCorralMonster tcm = MetaProgressScript.localTamedMonstersForThisSlot[i];
            if (tcm.happiness > 8)
            {
                tutorialPossible = true;
                GameMasterScript.gmsSingleton.SetTempStringData("petname", tcm.monsterObject.displayName);
                break;
            }
        }

        if (!tutorialPossible)
        {
            return false;
        }

        MarkTutorialWatched("tutorial_monsterletters");

        Cutscenes.DoMonsterLetterTutorialCallout();

        return true;
    }

    public static void OnUIClosed()
    {
		if (!PlatformVariables.SHOW_SEARCHBARS) return;
        if (searchBarUsed && !checkedForSearchBarTutorialThisSession)
        {
            if (StringManager.gameLanguage != EGameLanguage.en_us) return;
            if (!PlayerOptions.tutorialTips) return;
            if (UIManagerScript.singletonUIMS.abilityInTargeting != null) return;
            if (GameMasterScript.tutorialManager.WatchedTutorial("tutorial_searchbar"))
            {
                checkedForSearchBarTutorialThisSession = true;
                return;
            }
            if (GameMasterScript.IsGameInCutsceneOrDialog())
            {
                return;
            }
            if (CharCreation.creationActive)
            {
                return;
            }
            if (UIManagerScript.AnyInteractableWindowOpenExceptDialog())
            {
                return;
            }

            doSearchbarTutorialAtEndOfNextTurn = true;
        }

    }

    public static void CheckForPetCommandTutorialOnMapChange()
    {
        if (!GameMasterScript.tutorialManager.WatchedTutorial("tutorial_petcommands") && PlayerOptions.tutorialTips
            && GameMasterScript.heroPCActor.lowestFloorExplored >= 3 && !MapMasterScript.activeMap.IsBossFloor())
        {
            bool anyMonster = false;
            foreach (Actor act in GameMasterScript.heroPCActor.summonedActors)
            {
                if (act.GetActorType() == ActorTypes.MONSTER)
                {
                    Monster mn = act as Monster;
                    if (mn.bufferedFaction == Faction.PLAYER && mn.actorfaction == Faction.PLAYER)
                    {
                        anyMonster = true;
                        break;
                    }
                }
            }
            if (anyMonster)
            {
                Conversation newConvo = GameMasterScript.tutorialManager.GetTutorialAndMarkAsViewed("tutorial_petcommands");
                if (PlatformVariables.GAMEPAD_ONLY || TDInputHandler.lastActiveControllerType == Rewired.ControllerType.Joystick)
                {
                    GameMasterScript.gmsSingleton.SetTempStringData("help_petcommands", StringManager.GetString("dialog_tutorial_petcommands_main_txt_switch", true));
                }
                else
                {
                    GameMasterScript.gmsSingleton.SetTempStringData("help_petcommands", StringManager.GetString("dialog_tutorial_petcommands_main_txt", true));
                }
                
                UIManagerScript.StartConversation(newConvo, DialogType.STANDARD, null);
            }
        }
    }
}
