using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Rewired.Platforms.Switch;
using TMPro;
using UnityEngine;
using Object = System.Object;

public class Switch_DebugMenu : MonoBehaviour
{
    public static bool globalPollAndDebugDPadInput;

    public struct SwitchDebugMenu_Label
    {
        public string strText;
        public Func<string> funcOnSelect;

        public SwitchDebugMenu_Label(string s, Func<string> f)
        {
            strText = s;
            funcOnSelect = f;
        }
    }

    public GameObject goFirstLabel;
    public int iOptionsPerPage;
    public int iPxBetweenOptions;
    public TMP_InputField secretInputField;

    private List<TextMeshProUGUI> listCurrentOptions;

    //if we end up making more options than we can fit on one page, we will make multiple pages.
    private List<List<SwitchDebugMenu_Label>> listPages;
    private int idxCurrentPage;
    private int idxSelectedOption;
    private int iMaxPages = 1;
    private static Switch_DebugMenu instance;

    private bool bInitialized = false;
    private bool bEverActivatedByPlayer = false;

    //#TGS: Optionally prevent saves to allow for demo mode play.
    public static bool tgs_AllowSaveData = true;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        listCurrentOptions = new List<TextMeshProUGUI>();
        listPages = new List<List<SwitchDebugMenu_Label>>();
        listPages.Add(new List<SwitchDebugMenu_Label>());

        Switch_DebugMenu.RegisterSwitchDebugFunction("Toggle Undying", SwitchDebug_ToggleUndying);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Reveal Map", SwitchDebug_RevealMap);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Warp Stairs Previous", SwitchDebug_WarpStairsUp);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Warp Stairs Next", SwitchDebug_WarpStairsDown);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Detonate Monsters", SwitchDebug_Detonate);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Freeze Monsters", SwitchDebug_FreezeMonsters);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Spawn Item", SwitchDebug_SpawnItem);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Skip Floors", SwitchDebug_SkipFloors);
        Switch_DebugMenu.RegisterSwitchDebugFunction("List Hidden Skills", SwitchDebug_ListPassives);
        //Switch_DebugMenu.RegisterSwitchDebugFunction("Spawn 100 Item Dreams", SwitchDebug_SpawnItemDreams);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Poll DPad Input", SwitchDebug_TogglePollDPadInput);

        //Switch_DebugMenu.RegisterSwitchDebugFunction("F With Joycon Grip", SwitchDebug_ChangeJoyconGrip);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Advance One Day", SwitchDebug_NextDay);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Change Job", SwitchDebug_ChangeJob);
        Switch_DebugMenu.RegisterSwitchDebugFunction("+100 JP", SwitchDebug_Add100JP);
        Switch_DebugMenu.RegisterSwitchDebugFunction("+10k JP", SwitchDebug_Add10kJP);

        Switch_DebugMenu.RegisterSwitchDebugFunction("Toggle Allow Saving", SwitchDebug_ToggleAllowSaving);
        Switch_DebugMenu.RegisterSwitchDebugFunction("Generate Quests", SwitchDebug_GenerateTestQuests);

        Switch_DebugMenu.RegisterSwitchDebugFunction("Unlock Notorious", SwitchDebug_UnlockNotorious);

        //toss random item

        //spawn random item

        //now immediately hide self
        if (!bEverActivatedByPlayer)
        {
            instance.gameObject.SetActive(false);
        }
    }

    // Use this for initialization
    void Start ()
	{
		InitializeTextObjects();
	}

    /// <summary>
    /// Create a list of text objects based on the information in the prefab
    /// </summary>
    void InitializeTextObjects()
    {
        if (bInitialized)
        {
            return;
        }

        bInitialized = true;

        //the first one already exists
        listCurrentOptions.Add(goFirstLabel.GetComponent<TextMeshProUGUI>());

        //place the next few accordingly
        for (int t = 1; t < iOptionsPerPage; t++)
        {
            //parent it, to get the scale right, then immediately de-parent it
            //so that we don't make children of children of children when we loop
            var go = Instantiate(goFirstLabel, goFirstLabel.transform);
            go.transform.SetParent(null);
            listCurrentOptions.Add(go.GetComponent<TextMeshProUGUI>());
        }

        for (int t = 1; t < iOptionsPerPage; t++)
        {
            listCurrentOptions[t].gameObject.transform.SetParent(goFirstLabel.transform);
            listCurrentOptions[t].gameObject.transform.localPosition = new Vector3(0, -iPxBetweenOptions * t, 0);
        }

        //set the page up, we don't know if there have been options added before Start, so just do this anyway
        PlaceCurrentPageInOptionsList();
    }

    /// <summary>
    /// Updates the list of text labels to match our current page
    /// </summary>
    void PlaceCurrentPageInOptionsList()
    {
        var currentPage = listPages[idxCurrentPage];
        for (int iTxtIndex = 0; iTxtIndex < iOptionsPerPage; iTxtIndex++)
        {
            //if we have a label, draw it, otherwise do not
            if (iTxtIndex < currentPage.Count)
            {
                listCurrentOptions[iTxtIndex].enabled = true;
                listCurrentOptions[iTxtIndex].text = currentPage[iTxtIndex].strText;
            }
            else
            {
                listCurrentOptions[iTxtIndex].enabled = false;
                listCurrentOptions[iTxtIndex].text = "";
            }
        }
    }


    /// <summary>
    /// Add a function to collection of callable debug functions.
    /// It will add it to the bottom of the last page, unless that page is full,
    /// In which case a new page will be created.
    /// </summary>
    /// <param name="strLabel"></param>
    /// <param name="funcOnSelect"></param>
    public static void RegisterSwitchDebugFunction(string strLabel, Func<string> funcOnSelect)
    {
        instance.RegisterSwitchDebugFunction_Internal(strLabel, funcOnSelect);
    }

    private void RegisterSwitchDebugFunction_Internal(string strLabel, Func<string> funcOnSelect)
    {
        //is our current page full?
        var lasPage = listPages[iMaxPages - 1];
        if (lasPage.Count >= iOptionsPerPage)
        {
            //new page!
            lasPage = new List<SwitchDebugMenu_Label>();
            listPages.Add(lasPage);
            iMaxPages++;
        }

        //add this to the collection
        lasPage.Add(new SwitchDebugMenu_Label(strLabel, funcOnSelect));
    }

    void Update()
    {

        //if this is empty, don't crash.
        if (idxSelectedOption >= listCurrentOptions.Count)
        {
            return;
        }

        //make sure dialog cursor is pointing at correct object
        UIManagerScript.ShowDialogMenuCursor(true);
        UIManagerScript.AlignCursorPos(listCurrentOptions[idxSelectedOption].gameObject, -5.0f, 0.0f, false);
        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(listCurrentOptions[idxSelectedOption].transform);
    }

    /// <summary>
    /// Run the function that is at the current index of the current page
    /// </summary>
    private void ExecuteSelectedFunction()
    {
        string strResult = listPages[idxCurrentPage][idxSelectedOption].funcOnSelect();
        if (!string.IsNullOrEmpty(strResult))
        {
            GameLogScript.GameLogWrite(CustomAlgorithms.ParseRichText("#cp#Debug:#ec#" + strResult, false), null);
        }

        //play a sound?
    }

    /// <summary>
    /// Takes controller input and does something with it.
    /// </summary>
    /// <returns>True if the debug menu is active, even if no input was handled, because we want the game to handle no other input if this window is open.</returns>
    public static bool HandleInput()
    {

        instance.HandleInput_Internal();

        return instance.gameObject.activeInHierarchy;
    }

    void HandleInput_Internal()
    {
        if (!Debug.isDebugBuild) return;

        var rewiredPlayer = GameMasterScript.gmsSingleton.player;

        //if we are inactive, check to see if we should be active
        if (gameObject.activeInHierarchy == false)
        {
            if (rewiredPlayer.GetButton("Diagonal Move Only") &&
                rewiredPlayer.GetButtonDown("Toggle Ring Menu"))// || rewiredPlayer.GetButtonDown("List Page Down"))
            {
                bEverActivatedByPlayer = true;
                gameObject.SetActive(true);
            }

            return;
        }

        //if this is empty, don't crash.
        if (listCurrentOptions.Count == 0)
        {
            return;
        }

        //if our current object is a command, and confirm is pressed,
        if (rewiredPlayer.GetButtonDown("Confirm"))
        {
            ExecuteSelectedFunction();
            return;
        }

        //check for menu closing
        if (rewiredPlayer.GetButtonDown("Cancel"))
        {
            UIManagerScript.PlayCursorSound("Cancel");
            UIManagerScript.HideDialogMenuCursor();
            gameObject.SetActive(false);
            return;
        }

        //left/right swap pages
        if (rewiredPlayer.GetButtonRepeating("RadialRight"))
        {
            SwapPages(true);
            UIManagerScript.PlayCursorSound("UITick");
            return;
        }

        if (rewiredPlayer.GetButtonRepeating("RadialLeft"))
        {
            SwapPages(false);
            UIManagerScript.PlayCursorSound("UITock");
            return;
        }

        if (rewiredPlayer.GetButtonRepeating("RadialUp"))
        {
            //move up
            idxSelectedOption--;

            //if <0, wrap to the bottom of the previous page
            if (idxSelectedOption < 0)
            {
                //assume max, but this'll be corrected in the swap
                idxSelectedOption = iOptionsPerPage;
                SwapPages(false);
                UIManagerScript.PlayCursorSound("UITock");
                return;
            }

            UIManagerScript.PlayCursorSound("Move");
        }

        if (rewiredPlayer.GetButtonRepeating("RadialDown"))
        {
            //move up
            idxSelectedOption++;

            //if past the bottom, wrap to top of next page
            if (idxSelectedOption >= listPages[idxCurrentPage].Count)
            {
                //tippity top
                idxSelectedOption = 0;
                SwapPages(true);
                UIManagerScript.PlayCursorSound("UITick");
                return;
            }

            UIManagerScript.PlayCursorSound("Move");
        }
    }

    /// <summary>
    /// Moves to the next or previous page, if there is one. There may not be one, in which case we loop around.
    /// </summary>
    /// <param name="bNextPage"></param>
    void SwapPages(bool bNextPage)
    {
        int idxNextPage = idxCurrentPage + (bNextPage ? 1 : -1);

        //wrap around 
        if (idxNextPage < 0)
        {
            idxNextPage = iMaxPages - 1;
        }

        if (idxNextPage >= iMaxPages)
        {
            idxNextPage = 0;
        }

        //determine our index in the option list. Unless it is greater than the number of options
        //in that list, it won't change.
        var newPage = listPages[idxNextPage];
        if (idxSelectedOption >= newPage.Count)
        {
            idxSelectedOption = newPage.Count - 1;
        }

        idxCurrentPage = idxNextPage;

        PlaceCurrentPageInOptionsList();
    }

    public static string SwitchDebug_UnlockNotorious()
    {
        SharedBank.UnlockFeat("skill_champfinder");
        return "Done!";
    }

    public static string SwitchDebug_GenerateTestQuests()
    {
        for (int t = 0; t < 100; t++)
        {
            QuestScript qs = QuestScript.CreateNewQuest();
            qs.GenerateQuestText();
        }

        return "Quests generated, no crashes";
    }

    public static string SwitchDebug_ToggleUndying()
    {
        GameMasterScript.debug_neverDie = !GameMasterScript.debug_neverDie;
        return "Undying==" + GameMasterScript.debug_neverDie;
    }

    public static string SwitchDebug_Detonate()
    {
        GameMasterScript.Debug_DetonateAllMonsters();
        return "FOOM!";
    }

    public static string SwitchDebug_FreezeMonsters()
    {
        GameMasterScript.debug_freezeMonsters = !GameMasterScript.debug_freezeMonsters;
        return "Monsters Frozen==" + GameMasterScript.debug_freezeMonsters;
    }

    public static string SwitchDebug_RevealMap()
    {
        UIManagerScript.dbRevealMode = true;
        MapMasterScript.activeMap.ExploreAllTiles();
        return "Map revealed, step to clear FOW!";
    }

    public static string SwitchDebug_WarpStairsUp()
    {
        Map searchMap = MapMasterScript.activeMap;

        foreach (Stairs st in searchMap.mapStairs)
        {
            if (st.stairsUp) // && st.NewLocation.IsMainPath())
            {
                if (GameMasterScript.heroPCActor.GetPos() != st.GetPos())
                {
                    MapMasterScript.MoveActorAndChangeCamera(GameMasterScript.heroPCActor, st.GetPos());
                    return "Moved to stairs up for main path.";
                }
            }
        }

        return "No stairs up on the main path here.";
    }

    public static string SwitchDebug_WarpStairsDown()
    {
        Map searchMap = MapMasterScript.activeMap;

        foreach (Stairs st in searchMap.mapStairs)
        {
            if (!st.stairsUp) // && st.NewLocation.IsMainPath())
            {
                if (GameMasterScript.heroPCActor.GetPos() != st.GetPos())
                {
                    MapMasterScript.MoveActorAndChangeCamera(GameMasterScript.heroPCActor, st.GetPos());
                    return "Moved to stairs down for main path.";
                }
            }
        }

        return "No stairs down on the main path here.";
    }

    /// <summary>
    /// Some debug options will allow us to enter text in order to add parameters
    /// such as spawning a given item.
    /// </summary>
    /// <param name="actionOnSubmitText">Function that runs on submit</param>
    void ActivateInputFieldListener(UnityEngine.Events.UnityAction<string> actionOnSubmitText)
    {
        secretInputField.ActivateInputField();
        secretInputField.onSubmit.RemoveAllListeners();
        secretInputField.onSubmit.AddListener(actionOnSubmitText);
        secretInputField.onEndEdit.AddListener(actionOnSubmitText);
    }

    public static string SwitchDebug_SpawnItem()
    {
        instance.ActivateInputFieldListener(SwitchDebug_SpawnItem_OnSubmit);
        return null;
    }

    private static void SwitchDebug_SpawnItem_OnSubmit(string s)
    {
        //check this string to see if it ends in a number
        int iQuantity = 1;
        string[] split = s.Split(' ');

        //if we're asking for multiples, please do this
        if (split.Length > 1)
        {
            Int32.TryParse(split[ split.Length -1 ], out iQuantity);
            if (iQuantity != 0)
            {
                s = s.Replace(" " + iQuantity, "");
                s = s.Trim();
            }
        }

        string retString = DebugConsole.SpawnItem(s, iQuantity) as string;
        GameLogScript.GameLogWrite(CustomAlgorithms.ParseRichText("#cp#Debug:#ec#" + retString, false), null);
    }

    public static string SwitchDebug_TogglePollDPadInput()
    {
        globalPollAndDebugDPadInput = !globalPollAndDebugDPadInput;

        return null;
    }

    public static string SwitchDebug_SpawnItemDreams()
    {
        GameMasterScript.gmsSingleton.StartCoroutine(SpawnLotsofItemDreams());

        return null;
    }    

    static IEnumerator SpawnLotsofItemDreams()
    {
        Item orb = LootGeneratorScript.CreateItemFromTemplateRef("orb_itemworld", 2f, 0f, false);

        for (int i = 0; i < 100; i++)
        {
            Item randomEquipment = LootGeneratorScript.GenerateLootFromTable(2.0f, 1f, "equipment", 1f);
            int attempts = 0;
            while (!randomEquipment.IsEquipment())
            {
                randomEquipment = LootGeneratorScript.GenerateLootFromTable(2.0f, 1f, "equipment", 1f);
                attempts++;
                if (attempts > 100)
                {
                    Debug.LogError("COULDN'T SPAWN GEAR???");
                    break;
                }
            }

            GameMasterScript.TryBeginItemWorld(randomEquipment, orb, 0.5f, debugTest:true);

            yield return null;

            for (int x = 0; x < MapMasterScript.itemWorldMaps.Length; x++)
            {
                MapMasterScript.itemWorldMaps[x].clearedMap = true;
                MapMasterScript.theDungeon.maps.Remove(MapMasterScript.itemWorldMaps[x]);
                MapMasterScript.dictAllMaps.Remove(MapMasterScript.itemWorldMaps[x].mapAreaID);
                MapMasterScript.OnMapRemoved(MapMasterScript.itemWorldMaps[x]);
            }
            MapMasterScript.itemWorldMaps = null; // No item world maps

            MapMasterScript.itemWorldOpen = false;
            GameMasterScript.heroPCActor.RemoveActorData("item_dream_open");
            GameMasterScript.endingItemWorld = false;

            yield return null;
        }                
    }

    public static string SwitchDebug_ListPassives()
    {
        string abilities = "";
        foreach(AbilityScript abil in GameMasterScript.heroPCActor.myAbilities.GetAbilityList())
        {
            if (!abil.displayInList)
            {
                abilities += abil.refName + ", "; 
            }
        }
        abilities += "\n\n";
        foreach(StatusEffect se in GameMasterScript.heroPCActor.myStats.GetAllStatuses())
        {
            if (!se.displayInList)
            {
                abilities += "[S]" + se.refName + ", ";
            }
        }
        Debug.Log(abilities);
        return null;
    }

    public static string SwitchDebug_SkipFloors()
    {
        instance.ActivateInputFieldListener(SwitchDebug_SkipFloors_OnSubmit);
        return null;
    }

    private static void SwitchDebug_SkipFloors_OnSubmit(string s)
    {
        string retString = DebugConsole.SkipFloors("lol", s) as string;
        GameLogScript.GameLogWrite(CustomAlgorithms.ParseRichText("#cp#Debug:#ec#" + retString, false), null);
    }
    /// <summary>
    /// Changes the joycon grip from V to H and back to test for janky janks
    /// </summary>
    private static string SwitchDebug_ChangeJoyconGrip()
    {
#if !UNITY_SWITCH
        return "Only works on Switch, alas!";
#else
        var gStyle = SwitchInput.Npad.GetJoyConGripStyle();
        string retString = "Grip was: " + gStyle + ", Grip is: ";

        switch (gStyle)
        {
            case JoyConGripStyle.Vertical:
                SwitchInput.Npad.SetJoyConGripStyle(JoyConGripStyle.Horizontal);
                break;
            case JoyConGripStyle.Horizontal:
                SwitchInput.Npad.SetJoyConGripStyle(JoyConGripStyle.Vertical);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //new style
        retString += SwitchInput.Npad.GetJoyConGripStyle();

        return retString;
#endif
    }


    /// <summary>
    /// Advances the game's day clock by 1.
    /// </summary>
    /// <returns></returns>
    private static string SwitchDebug_NextDay()
    {
        GameMasterScript.gmsSingleton.TickGameTime(1, true);
        return "Day++";
    }

    /// <summary>
    /// Lets you enter a job name, and then become that job.
    /// </summary>
    /// <returns></returns>
    private static string SwitchDebug_ChangeJob()
    {
        instance.ActivateInputFieldListener(SwitchDebug_ChangeJob_OnSubmit);
        return null;
    }

    private static void SwitchDebug_ChangeJob_OnSubmit(string s)
    {
        string retString = "";
        string strJobbyJob = s.ToUpperInvariant();
        if (!GameMasterScript.heroPCActor.ChangeJobs(strJobbyJob, null))
        {
            retString = "'" + s + "' is not a valid job.";
        }
        else
        {
            retString = "You are now a " + strJobbyJob;

        }

        GameLogScript.GameLogWrite(CustomAlgorithms.ParseRichText("#cp#Debug:#ec#" + retString, false), null);
    }

    private static string SwitchDebug_Add100JP()
    {
        GameMasterScript.gmsSingleton.AwardJP(100.0f);
        return "+100 JP";
    }

    private static string SwitchDebug_Add10kJP()
    {
        GameMasterScript.gmsSingleton.AwardJP(10000.0f);
        return "Get learnt";
    }
    
    private static string SwitchDebug_ToggleAllowSaving()
    {
        tgs_AllowSaveData = !tgs_AllowSaveData;
        return "TGS! AllowSaveData==" + tgs_AllowSaveData;
    }
}
