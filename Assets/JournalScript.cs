using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;
using System.IO;

public enum JournalTabs { RECIPES = 0, RUMORS = 1, COMBATLOG = 2, MONSTERPEDIA = 3, COUNT }

[System.Serializable]
public class JournalScript : MonoBehaviour {
    
    public bool mainGame;
    public Scrollbar combatLogScrollbar;

    public static JournalTabs journalState;

    static GameObject monsterPediaParent;
    static TextMeshProUGUI monsterPediaText;

    public static UIManagerScript.UIObject rumorsTab;
    public static UIManagerScript.UIObject recipesTab;
    public static UIManagerScript.UIObject logTab;
    public static UIManagerScript.UIObject monsterPediaTab;

    static UIManagerScript.UIObject[] monsterButtonList;

    static UIManagerScript.UIObject[] recipeButtonList;
    static GameObject recipeStuff;
    public static int numTotalRecipes;
    static TextMeshProUGUI recipeText;
    public static JournalScript singleton;
    static GameObject combatLogStuff;
    static TextMeshProUGUI combatLogText;

    static GameObject questStuff;
    static UIManagerScript.UIObject quest1;
    static TextMeshProUGUI quest1Text;
    static UIManagerScript.UIObject quest1Exit;
    static UIManagerScript.UIObject quest2;
    static TextMeshProUGUI quest2Text;
    static UIManagerScript.UIObject quest2Exit;
    static UIManagerScript.UIObject quest3;
    static TextMeshProUGUI quest3Text;
    static UIManagerScript.UIObject quest3Exit;

    static UIManagerScript.UIObject[] questExitButtons;

    static TextMeshProUGUI quest1RewardText;
    static TextMeshProUGUI quest2RewardText;
    static TextMeshProUGUI quest3RewardText;

    bool initialized = false;

    static Image pediaDownButton;
    static Image pediaUpButton;

    const int NUM_MONSTERS_PER_ROW = 10;
    const int MAX_MONSTER_ROWS = 6;
    const int MONSTER_PIXEL_SPACE_X = 116;
    const int MONSTER_PIXEL_SPACE_Y = 120;
    const int BASE_X_POS = -818;
    const int BASE_Y_POS = 229;

    static Color compendiumButtonBGColor = new Color(0.2156f, 1f, 1f, 0.14059f);

    static int monsterPediaOffset; // Must be in increments of (NUM_MONSTERS_PER_ROW)

    public void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public IEnumerator Initialize()
    {
        if (!mainGame) yield break;
        if (singleton == null)
        {
            singleton = this;
        }
        if (initialized) yield break;
        initialized = true;
        numTotalRecipes = 18;

        questStuff = GameObject.Find("QuestStuff");

        quest1 = new UIManagerScript.UIObject();
        quest1.gameObj = GameObject.Find("Quest Sheet Quest 1");

        quest1Text = GameObject.Find("Quest Sheet Text 1").GetComponent<TextMeshProUGUI>();

        quest2 = new UIManagerScript.UIObject();
        quest2.gameObj = GameObject.Find("Quest Sheet Quest 2");
        quest2Text = GameObject.Find("Quest Sheet Text 2").GetComponent<TextMeshProUGUI>();

        quest3 = new UIManagerScript.UIObject();
        quest3.gameObj = GameObject.Find("Quest Sheet Quest 3");

        quest3Text = GameObject.Find("Quest Sheet Text 3").GetComponent<TextMeshProUGUI>();

        quest1RewardText = GameObject.Find("Quest Sheet Rewards 1").GetComponent<TextMeshProUGUI>();
        quest2RewardText = GameObject.Find("Quest Sheet Rewards 2").GetComponent<TextMeshProUGUI>();
        quest3RewardText = GameObject.Find("Quest Sheet Rewards 3").GetComponent<TextMeshProUGUI>();

        quest1Exit = new UIManagerScript.UIObject();
        quest1Exit.gameObj = GameObject.Find("Rumor 1 Exit");
        quest1Exit.mySubmitFunction = TryQuitRumor;
        quest1Exit.onSubmitValue = 0;

        quest2Exit = new UIManagerScript.UIObject();
        quest2Exit.gameObj = GameObject.Find("Rumor 2 Exit");
        quest2Exit.mySubmitFunction = TryQuitRumor;
        quest2Exit.onSubmitValue = 1;

        quest3Exit = new UIManagerScript.UIObject();
        quest3Exit.gameObj = GameObject.Find("Rumor 3 Exit");
        quest3Exit.mySubmitFunction = TryQuitRumor;
        quest3Exit.onSubmitValue = 2;

        questExitButtons = new UIManagerScript.UIObject[]
        {
            quest1Exit,
            quest2Exit,
            quest3Exit
        };

if (!PlatformVariables.GAMEPAD_ONLY)
{
        for(int idx = 0; idx < questExitButtons.Length; idx++)
        {
            //grab the event trigger out of our shadow object
            var btn = questExitButtons[idx];
            EventTrigger et = btn.gameObj.GetComponent<EventTrigger>();
            
            //create a new event trigger entry to handle pointer enter
            EventTrigger.Entry newEntry = new EventTrigger.Entry();
            newEntry.eventID = EventTriggerType.PointerEnter;
            
            //call our function using whatever value was assigned to the shadow object.
            newEntry.callback.AddListener(ped => { OnHoverRumorExitButton(btn.onSubmitValue); });
            et.triggers.Add(newEntry);
        }
}

        quest1Exit.neighbors[(int)Directions.WEST] = quest1;
        quest1Exit.neighbors[(int)Directions.NORTH] = rumorsTab;
        quest1Exit.neighbors[(int)Directions.SOUTH] = quest2Exit;

        quest2Exit.neighbors[(int)Directions.WEST] = quest2;
        quest2Exit.neighbors[(int)Directions.NORTH] = quest1Exit;
        quest2Exit.neighbors[(int)Directions.SOUTH] = quest3Exit;

        quest3Exit.neighbors[(int)Directions.WEST] = quest3;
        quest3Exit.neighbors[(int)Directions.NORTH] = quest2Exit;
        quest3Exit.neighbors[(int)Directions.SOUTH] = rumorsTab;

        recipeStuff = GameObject.Find("RecipeStuff");

        rumorsTab = new UIManagerScript.UIObject();
        rumorsTab.gameObj = GameObject.Find("RumorsButton");
        rumorsTab.mySubmitFunction = SwitchJournalTab;
        rumorsTab.onSubmitValue = (int)JournalTabs.RUMORS;

        recipesTab = new UIManagerScript.UIObject();
        recipesTab.gameObj = GameObject.Find("RecipesButton");
        recipesTab.mySubmitFunction = SwitchJournalTab;
        recipesTab.onSubmitValue = (int)JournalTabs.RECIPES;

        logTab = new UIManagerScript.UIObject();
        logTab.gameObj = GameObject.Find("LogButton");
        logTab.mySubmitFunction = SwitchJournalTab;
        logTab.onSubmitValue = (int)JournalTabs.COMBATLOG;

        monsterPediaTab = new UIManagerScript.UIObject();
        monsterPediaTab.gameObj = GameObject.Find("MonsterPediaButton");
        monsterPediaTab.mySubmitFunction = SwitchJournalTab;
        monsterPediaTab.onSubmitValue = (int)JournalTabs.MONSTERPEDIA;

        GameObject go = GameObject.Find("MonsterPediaDownArrow");
        if (go != null)
        {
            pediaDownButton = go.GetComponent<Image>();
        }
        go = GameObject.Find("MonsterPediaUpArrow");
        if (go != null)
        {
            pediaUpButton = go.GetComponent<Image>();
        }

        // Rather than the monstersInPedia count, let's build the # of buttons based on max allowable
        monsterButtonList = new UIManagerScript.UIObject[MAX_MONSTER_ROWS * NUM_MONSTERS_PER_ROW];

        monsterPediaParent = GameObject.Find("Monsterpedia");
        monsterPediaText = GameObject.Find("Monsterpedia Desc Text").GetComponent<TextMeshProUGUI>();

        int row = 0;
        int column = 0;
        int maxRow = MAX_MONSTER_ROWS - 1;

        UpdateMonsterPediaGraphics(firstTimeSetup:true);

        column = 0;
        row = 0;

        //Debug.Log("Max index " + monsterButtonList.Length);
        float fDelayTimer = Time.realtimeSinceStartup;

        for (int i = 0; i < monsterButtonList.Length; i++)
        {
            if (Time.realtimeSinceStartup - fDelayTimer > 0.015f)
            {
                yield return null;
                fDelayTimer = Time.realtimeSinceStartup;
            }
            if (row == 0)
            {
                int tryToSeek = column + (maxRow * NUM_MONSTERS_PER_ROW);
                if (tryToSeek >= monsterButtonList.Length)
                {
                    tryToSeek = monsterButtonList.Length - 1;
                }
                monsterButtonList[i].directionalActions[(int)Directions.NORTH] = TryScrollPediaUp;
                monsterButtonList[i].neighbors[(int)Directions.NORTH] = monsterPediaTab;
            }
            else
            {
                monsterButtonList[i].neighbors[(int)Directions.NORTH] = monsterButtonList[column + ((row-1) * NUM_MONSTERS_PER_ROW)];
            }

            if (row == maxRow)
            {
                monsterButtonList[i].neighbors[(int)Directions.SOUTH] = monsterButtonList[column];
                monsterButtonList[i].directionalActions[(int)Directions.SOUTH] = TryScrollPediaDown;
            }
            else
            {
                int iToSeek = column + ((row + 1) * NUM_MONSTERS_PER_ROW);
                if (iToSeek >= monsterButtonList.Length)
                {
                    iToSeek = monsterButtonList.Length - 1;
                }
                monsterButtonList[i].neighbors[(int)Directions.SOUTH] = monsterButtonList[iToSeek];
            }

            if (column == 0)
            {
                int iToSeek = NUM_MONSTERS_PER_ROW - 1 + (row * NUM_MONSTERS_PER_ROW);
                //Debug.Log("Column " + column + " row " + row + " we seek " + iToSeek);
                if (iToSeek >= monsterButtonList.Length)
                {
                    iToSeek = monsterButtonList.Length - 1;
                }
                monsterButtonList[i].neighbors[(int)Directions.WEST] = monsterButtonList[iToSeek];
            }
            else
            {
                monsterButtonList[i].neighbors[(int)Directions.WEST] = monsterButtonList[i-1];
            }

            if (column == NUM_MONSTERS_PER_ROW-1)
            {
                monsterButtonList[i].neighbors[(int)Directions.EAST] = monsterButtonList[row * NUM_MONSTERS_PER_ROW];
            }
            else
            {
                int iToSeek = i + 1;
                if (iToSeek >= monsterButtonList.Length)
                {
                    iToSeek = (row * NUM_MONSTERS_PER_ROW);
                }
                monsterButtonList[i].neighbors[(int)Directions.EAST] = monsterButtonList[iToSeek];
            }

            column++;

            if (column >= NUM_MONSTERS_PER_ROW)
            {
                column = 0;
                row++;
            }
        }


        recipeText = GameObject.Find("Recipe Desc Text").GetComponent<TextMeshProUGUI>();

        combatLogStuff = GameObject.Find("JournalCombatLog");
        combatLogText = GameObject.Find("JournalCombatLogText").GetComponent<TextMeshProUGUI>();

        quest1.neighbors[(int)Directions.EAST] = quest1Exit;
        quest1.neighbors[(int)Directions.NORTH] = rumorsTab;
        quest1.neighbors[(int)Directions.SOUTH] = quest2;

        quest2.neighbors[(int)Directions.EAST] = quest2Exit;
        quest2.neighbors[(int)Directions.NORTH] = quest1;
        quest2.neighbors[(int)Directions.SOUTH] = quest3;

        quest3.neighbors[(int)Directions.EAST] = quest3Exit;
        quest3.neighbors[(int)Directions.NORTH] = quest2;
        quest3.neighbors[(int)Directions.SOUTH] = rumorsTab;

        

        recipeButtonList = new UIManagerScript.UIObject[numTotalRecipes];

        for (int i = 0; i < recipeButtonList.Length; i++)
        {
            if (Time.realtimeSinceStartup - fDelayTimer > 0.015f)
            {
                yield return null;
                fDelayTimer = Time.realtimeSinceStartup;
            }
            recipeButtonList[i] = new UIManagerScript.UIObject();
            recipeButtonList[i].gameObj = GameObject.Find("Recipe" + (i + 1));
            recipeButtonList[i].subObjectImage = GameObject.Find("Recipe" + (i + 1) + "Sprite").GetComponent<Image>();
            recipeButtonList[i].myOnSelectAction = GetRecipeInfo;
            recipeButtonList[i].mySubmitFunction = GetRecipeInfoAndTryCook;
            recipeButtonList[i].onSubmitValue = i;
            recipeButtonList[i].onSelectValue = i;
        }

        row = 0;
        column = 0;

        recipesTab.neighbors[(int)Directions.EAST] = rumorsTab;
        recipesTab.neighbors[(int)Directions.WEST] = monsterPediaTab;

        rumorsTab.neighbors[(int)Directions.EAST] = logTab;
        rumorsTab.neighbors[(int)Directions.WEST] = recipesTab;

        logTab.neighbors[(int)Directions.EAST] = monsterPediaTab;
        logTab.neighbors[(int)Directions.WEST] = rumorsTab;

        monsterPediaTab.neighbors[(int)Directions.EAST] = recipesTab;
        monsterPediaTab.neighbors[(int)Directions.WEST] = logTab;

        recipeButtonList[0].neighbors[(int)Directions.NORTH] = rumorsTab;
        recipeButtonList[0].neighbors[(int)Directions.EAST] = recipeButtonList[1];
        recipeButtonList[0].neighbors[(int)Directions.WEST] = recipeButtonList[2];
        recipeButtonList[0].neighbors[(int)Directions.SOUTH] = recipeButtonList[3];

        recipeButtonList[1].neighbors[(int)Directions.NORTH] = rumorsTab;
        recipeButtonList[1].neighbors[(int)Directions.EAST] = recipeButtonList[2];
        recipeButtonList[1].neighbors[(int)Directions.WEST] = recipeButtonList[0];
        recipeButtonList[1].neighbors[(int)Directions.SOUTH] = recipeButtonList[4];

        recipeButtonList[2].neighbors[(int)Directions.NORTH] = rumorsTab;
        recipeButtonList[2].neighbors[(int)Directions.EAST] = recipeButtonList[0];
        recipeButtonList[2].neighbors[(int)Directions.WEST] = recipeButtonList[1];
        recipeButtonList[2].neighbors[(int)Directions.SOUTH] = recipeButtonList[5];

        recipeButtonList[3].neighbors[(int)Directions.NORTH] = recipeButtonList[0];
        recipeButtonList[3].neighbors[(int)Directions.EAST] = recipeButtonList[4];
        recipeButtonList[3].neighbors[(int)Directions.WEST] = recipeButtonList[5];
        recipeButtonList[3].neighbors[(int)Directions.SOUTH] = recipeButtonList[6];

        recipeButtonList[4].neighbors[(int)Directions.NORTH] = recipeButtonList[1];
        recipeButtonList[4].neighbors[(int)Directions.EAST] = recipeButtonList[5];
        recipeButtonList[4].neighbors[(int)Directions.WEST] = recipeButtonList[3];
        recipeButtonList[4].neighbors[(int)Directions.SOUTH] = recipeButtonList[7];

        recipeButtonList[5].neighbors[(int)Directions.NORTH] = recipeButtonList[2];
        recipeButtonList[5].neighbors[(int)Directions.EAST] = recipeButtonList[3];
        recipeButtonList[5].neighbors[(int)Directions.WEST] = recipeButtonList[4];
        recipeButtonList[5].neighbors[(int)Directions.SOUTH] = recipeButtonList[8];

        recipeButtonList[6].neighbors[(int)Directions.NORTH] = recipeButtonList[3];
        recipeButtonList[6].neighbors[(int)Directions.EAST] = recipeButtonList[7];
        recipeButtonList[6].neighbors[(int)Directions.WEST] = recipeButtonList[8];
        recipeButtonList[6].neighbors[(int)Directions.SOUTH] = recipeButtonList[9];

        recipeButtonList[7].neighbors[(int)Directions.NORTH] = recipeButtonList[4];
        recipeButtonList[7].neighbors[(int)Directions.EAST] = recipeButtonList[8];
        recipeButtonList[7].neighbors[(int)Directions.WEST] = recipeButtonList[6];
        recipeButtonList[7].neighbors[(int)Directions.SOUTH] = recipeButtonList[10];

        recipeButtonList[8].neighbors[(int)Directions.NORTH] = recipeButtonList[5];
        recipeButtonList[8].neighbors[(int)Directions.EAST] = recipeButtonList[6];
        recipeButtonList[8].neighbors[(int)Directions.WEST] = recipeButtonList[7];
        recipeButtonList[8].neighbors[(int)Directions.SOUTH] = recipeButtonList[11];

        recipeButtonList[9].neighbors[(int)Directions.NORTH] = recipeButtonList[6];
        recipeButtonList[9].neighbors[(int)Directions.EAST] = recipeButtonList[10];
        recipeButtonList[9].neighbors[(int)Directions.WEST] = recipeButtonList[11];
        recipeButtonList[9].neighbors[(int)Directions.SOUTH] = recipeButtonList[12];

        recipeButtonList[10].neighbors[(int)Directions.NORTH] = recipeButtonList[7];
        recipeButtonList[10].neighbors[(int)Directions.EAST] = recipeButtonList[11];
        recipeButtonList[10].neighbors[(int)Directions.WEST] = recipeButtonList[9];
        recipeButtonList[10].neighbors[(int)Directions.SOUTH] = recipeButtonList[13];

        recipeButtonList[11].neighbors[(int)Directions.NORTH] = recipeButtonList[8];
        recipeButtonList[11].neighbors[(int)Directions.EAST] = recipeButtonList[9];
        recipeButtonList[11].neighbors[(int)Directions.WEST] = recipeButtonList[10];
        recipeButtonList[11].neighbors[(int)Directions.SOUTH] = recipeButtonList[14];

        recipeButtonList[12].neighbors[(int)Directions.NORTH] = recipeButtonList[9];
        recipeButtonList[12].neighbors[(int)Directions.EAST] = recipeButtonList[13];
        recipeButtonList[12].neighbors[(int)Directions.WEST] = recipeButtonList[14];
        recipeButtonList[12].neighbors[(int)Directions.SOUTH] = recipeButtonList[15];

        recipeButtonList[13].neighbors[(int)Directions.NORTH] = recipeButtonList[10];
        recipeButtonList[13].neighbors[(int)Directions.EAST] = recipeButtonList[14];
        recipeButtonList[13].neighbors[(int)Directions.WEST] = recipeButtonList[12];
        recipeButtonList[13].neighbors[(int)Directions.SOUTH] = recipeButtonList[16];

        recipeButtonList[14].neighbors[(int)Directions.NORTH] = recipeButtonList[11];
        recipeButtonList[14].neighbors[(int)Directions.EAST] = recipeButtonList[12];
        recipeButtonList[14].neighbors[(int)Directions.WEST] = recipeButtonList[13];
        recipeButtonList[14].neighbors[(int)Directions.SOUTH] = recipeButtonList[17];

        recipeButtonList[15].neighbors[(int)Directions.NORTH] = recipeButtonList[12];
        recipeButtonList[15].neighbors[(int)Directions.EAST] = recipeButtonList[16];
        recipeButtonList[15].neighbors[(int)Directions.WEST] = recipeButtonList[17];
        recipeButtonList[15].neighbors[(int)Directions.SOUTH] = recipesTab;

        recipeButtonList[16].neighbors[(int)Directions.NORTH] = recipeButtonList[13];
        recipeButtonList[16].neighbors[(int)Directions.EAST] = recipeButtonList[17];
        recipeButtonList[16].neighbors[(int)Directions.WEST] = recipeButtonList[15];
        recipeButtonList[16].neighbors[(int)Directions.SOUTH] = recipesTab;

        recipeButtonList[17].neighbors[(int)Directions.NORTH] = recipeButtonList[14];
        recipeButtonList[17].neighbors[(int)Directions.EAST] = recipeButtonList[15];
        recipeButtonList[17].neighbors[(int)Directions.WEST] = recipeButtonList[16];
        recipeButtonList[17].neighbors[(int)Directions.SOUTH] = recipesTab;

        UIManagerScript.CloseQuestSheet();
    }

    public static void OpenJournal()
    {
        recipesTab.enabled = true;
        rumorsTab.enabled = true;
        logTab.enabled = true;
        monsterPediaTab.enabled = true;
        singleton.SwitchJournalTab((int)journalState);
        switch(journalState)
        {
            case JournalTabs.RECIPES:
                UIManagerScript.ChangeUIFocusAndAlignCursor(JournalScript.recipesTab);
                break;
            case JournalTabs.RUMORS:
                UIManagerScript.ChangeUIFocusAndAlignCursor(JournalScript.rumorsTab);
                break;
            case JournalTabs.COMBATLOG:
                UIManagerScript.ChangeUIFocusAndAlignCursor(JournalScript.logTab);
                break;
            case JournalTabs.MONSTERPEDIA:
                UIManagerScript.ChangeUIFocusAndAlignCursor(JournalScript.monsterPediaTab);
                break;

        }
        
    }

    public void SwitchJournalTab(int i)
    {
        if ((JournalTabs)i != journalState)
        {
            UIManagerScript.PlayCursorSound("Organize");
        }        
        switch(i)
        {
            case (int)JournalTabs.RUMORS:
                questStuff.gameObject.SetActive(true);
                for (int x = 0; x < recipeButtonList.Length; x++)
                {
                    recipeButtonList[x].enabled = false;
                }
                recipeStuff.SetActive(false);
                combatLogStuff.SetActive(false);
                monsterPediaParent.SetActive(false);
                journalState = JournalTabs.RUMORS;
                UpdateQuests();

                recipesTab.neighbors[(int)Directions.NORTH] = quest3;
                recipesTab.neighbors[(int)Directions.SOUTH] = quest1;

                rumorsTab.neighbors[(int)Directions.NORTH] = quest3;
                rumorsTab.neighbors[(int)Directions.SOUTH] = quest1;

                logTab.neighbors[(int)Directions.NORTH] = quest3;
                logTab.neighbors[(int)Directions.SOUTH] = quest1;

                monsterPediaTab.neighbors[(int)Directions.NORTH] = quest3;
                monsterPediaTab.neighbors[(int)Directions.SOUTH] = quest1;
                if (UIManagerScript.uiObjectFocus != rumorsTab)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(rumorsTab);
                }                
                break;
            case (int)JournalTabs.RECIPES:
                questStuff.gameObject.SetActive(false);
                for (int x = 0; x < recipeButtonList.Length; x++)
                {
                    recipeButtonList[x].enabled = true;
                }
                recipeStuff.SetActive(true);
                combatLogStuff.SetActive(false);
                monsterPediaParent.SetActive(false);
                journalState = JournalTabs.RECIPES;
                UpdateRecipes();

                recipesTab.neighbors[(int)Directions.NORTH] = recipeButtonList[15];
                recipesTab.neighbors[(int)Directions.SOUTH] = recipeButtonList[0];

                rumorsTab.neighbors[(int)Directions.NORTH] = recipeButtonList[15];
                rumorsTab.neighbors[(int)Directions.SOUTH] = recipeButtonList[0];

                logTab.neighbors[(int)Directions.NORTH] = recipeButtonList[15];
                logTab.neighbors[(int)Directions.SOUTH] = recipeButtonList[0];

                monsterPediaTab.neighbors[(int)Directions.NORTH] = recipeButtonList[15];
                monsterPediaTab.neighbors[(int)Directions.SOUTH] = recipeButtonList[0];
                if (UIManagerScript.uiObjectFocus != recipesTab)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(recipesTab);
                }                
                break;
            case (int)JournalTabs.COMBATLOG:
                questStuff.gameObject.SetActive(false);
                for (int x = 0; x < recipeButtonList.Length; x++)
                {
                    recipeButtonList[x].enabled = false;
                }
                recipeStuff.SetActive(false);
                monsterPediaParent.SetActive(false);
                journalState = JournalTabs.COMBATLOG;
                combatLogStuff.SetActive(true);
                UpdateCombatLog();
                if (UIManagerScript.uiObjectFocus != logTab)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(logTab);
                }                
                break;

            case (int)JournalTabs.MONSTERPEDIA:
                monsterPediaParent.gameObject.SetActive(false);
                for (int x = 0; x < monsterButtonList.Length; x++)
                {
                    monsterButtonList[x].enabled = true;
                }
                recipeStuff.SetActive(false);
                combatLogStuff.SetActive(false);
                questStuff.SetActive(false);
                monsterPediaParent.SetActive(true);
                journalState = JournalTabs.MONSTERPEDIA;
                UpdateMonsterPedia();

                logTab.neighbors[(int)Directions.NORTH] = monsterButtonList[monsterButtonList.Length-1];
                logTab.neighbors[(int)Directions.SOUTH] = monsterButtonList[0];

                recipesTab.neighbors[(int)Directions.NORTH] = monsterButtonList[monsterButtonList.Length - 1];
                recipesTab.neighbors[(int)Directions.SOUTH] = monsterButtonList[0];

                rumorsTab.neighbors[(int)Directions.NORTH] = monsterButtonList[monsterButtonList.Length - 1];
                rumorsTab.neighbors[(int)Directions.SOUTH] = monsterButtonList[0];

                monsterPediaTab.neighbors[(int)Directions.NORTH] = monsterButtonList[monsterButtonList.Length - 1];
                monsterPediaTab.neighbors[(int)Directions.SOUTH] = monsterButtonList[0];
                if (UIManagerScript.uiObjectFocus != monsterPediaTab)
                {
                    UIManagerScript.ChangeUIFocusAndAlignCursor(monsterPediaTab);
                }
                break;
        }
    }
    
    public void FocusCursor(int i)
    {
        switch(i)
        {
            case (int)JournalTabs.RUMORS:
                UIManagerScript.ChangeUIFocusAndAlignCursor(rumorsTab);
                break;
            case (int)JournalTabs.RECIPES:
                UIManagerScript.ChangeUIFocusAndAlignCursor(recipesTab);
                break;
            case (int)JournalTabs.COMBATLOG:
                UIManagerScript.ChangeUIFocusAndAlignCursor(logTab);
                break;
            case (int)JournalTabs.MONSTERPEDIA:
                UIManagerScript.ChangeUIFocusAndAlignCursor(monsterPediaTab);
                break;
        }    
    }

    public static void GetRecipeInfoAndTryCook(int index)
    {
        GetRecipeInfo(index);

        if (index >= MetaProgressScript.recipesKnown.Count)
        {
            return;
        }

        // Only allow this if player is near a campfire.
        CustomAlgorithms.GetTilesAroundPoint(GameMasterScript.heroPCActor.GetPos(), 1, MapMasterScript.activeMap);

        bool campfireNearby = false;
        for (int i = 0; i < CustomAlgorithms.numTilesInBuffer; i++)
        {
            foreach(Actor act in CustomAlgorithms.tileBuffer[i].GetAllActors())
            {
                if (act.GetActorType() != ActorTypes.NPC) continue;
                NPC n = act as NPC;
                if (n.cookingPossible)
                {
                    campfireNearby = true;
                    break;
                }
            }
            if (campfireNearby)
            {
                break;
            }
        }

        if (!campfireNearby) return;

        // There is a campfire nearby, but do we have the ingredients for this recipe?

        Recipe recipeToTry = CookingScript.FindRecipe(MetaProgressScript.recipesKnown[index]);

        //List<Item> allUsableIngredients = GameMasterScript.heroPCActor.myInventory.GetAllCookingIngredients();

        Item createdItem = CookingScript.MakeRecipeIfPossible(recipeToTry);
        if (createdItem == null)
        {
            UIManagerScript.DisplayPlayerError(GameMasterScript.heroPCActor);
        }
        else
        {
            GameMasterScript.heroPCActor.myInventory.AddItem(createdItem, true);
            StringManager.SetTag(0, createdItem.displayName);
            SendFoodSpriteToPlayerWhenCookedFromJournal(createdItem, recipeButtonList[index]);
            GameLogScript.LogWriteStringRef("cooked_food");
            //UIManagerScript.ForceCloseFullScreenUIWithNoFade();
            //GameMasterScript.SetAnimationPlaying(true);
            TDVisualEffects.PopupSprite(createdItem.spriteRef, GameMasterScript.heroPC.transform, true, createdItem.GetSpriteForUI());
            UIManagerScript.PlayCursorSound("CookingSuccess");

            //Update info that relies on quantity of cooked items and 
            //amount of ingredients left
            //UpdateRecipeButtonBasedOnIngredientAvailability(index);
            // Actually don't we need to do this for every recipe on the list?
            for (int i = 0; i < MetaProgressScript.recipesKnown.Count; i++)
            {
                UpdateRecipeButtonBasedOnIngredientAvailability(i);
            }
            GetRecipeInfo(index);
        }
    }

    static void SendFoodSpriteToPlayerWhenCookedFromJournal(Item cookedFude, UIManagerScript.UIObject buttonShadowObject )
    {
        Image[] buttonImages = buttonShadowObject.gameObj.GetComponentsInChildren<Image>();

        Image foodImage = buttonImages[1];
        GameObject go = Instantiate(foodImage.gameObject);

        //Debug.Log("How many images? " + buttonImages.Length);

        //size it up to match the button we clicked
        go.transform.SetParent(buttonShadowObject.gameObj.transform);
        RectTransform newImageRect = go.transform as RectTransform;
        newImageRect.anchoredPosition = foodImage.rectTransform.anchoredPosition;

        //once positioned, parent it instead to the CSC block
        go.transform.SetParent(UIManagerScript.singletonUIMS.GetCSBlockImageTransform());

        //add the sprite image here
        Image img = go.GetComponent<Image>();
        img.sprite = cookedFude.GetSpriteForUI();

        //everything in this UI scales
        img.transform.localScale = Vector3.one;

        //fly to the face in the corner of the screen
        LeanTween.moveX(img.rectTransform, 72f, 1.0f).setEaseInOutBack();
        LeanTween.moveY(img.rectTransform, -72f, 1.0f).setEaseInOutBack();
        LeanTween.scale(img.rectTransform, Vector3.zero, 1.0f).setEaseInBack();
        LeanTween.rotateAroundLocal(img.rectTransform, Vector3.forward, 1080.0f, 2.0f).setEaseOutQuart();

        //destroy it when done
        Destroy(go, 1.01f);

        //ok cool now flash the button
        buttonShadowObject.gameObj.transform.localScale = Vector3.one;

        Image borderImage = Instantiate(buttonShadowObject.gameObj, buttonShadowObject.gameObj.transform).GetComponent<Image>();

#if UNITY_EDITOR
        Debug.Log("Parent name: " + borderImage.transform.parent.name);
#endif

        borderImage.transform.localPosition = Vector3.zero;
        borderImage.color = Color.cyan;
        LeanTween.color(borderImage.rectTransform, Color.white, 0.2f);

        //try to make the clone unclickable. Good luck.
        Destroy(borderImage.GetComponent<EventTrigger>());
        borderImage.raycastTarget = false;
        borderImage.transform.GetComponentInChildren<TextMeshProUGUI>().raycastTarget = false;

        borderImage.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
        LeanTween.scale(borderImage.rectTransform, new Vector3(1f, 1f, 1f), 0.2f).setEaseInOutBounce();
        borderImage.rectTransform.SetParent(borderImage.transform.parent.parent);
        Destroy(borderImage.gameObject, 0.3f);

    }

    public static void GetRecipeInfo(int index)
    {
        Recipe rSelected = null;

        if (index >= MetaProgressScript.recipesKnown.Count)
        {
            recipeText.text = FontManager.GetLargeSizeTagForCurrentLanguage() + "???? " + StringManager.GetString("ui_cooking_recipe") + " </size>";
            return;
        }

        rSelected = CookingScript.FindRecipe(MetaProgressScript.recipesKnown[index]);

        string text = "";

        //Find how many the player has on them
        Item iRef = Item.GetItemTemplateFromRef(rSelected.itemCreated);
        int iQuantity = GameMasterScript.heroPCActor.myInventory.GetItemQuantity(rSelected.itemCreated);
        string strQuantity = iQuantity > 0 ? "\n<size=36>" + StringManager.GetString("ui_in_inventory") +": " + iQuantity + "</size>\n\n" : "\n\n";
        text = FontManager.GetLargeSizeTagForCurrentLanguage() + "<color=yellow>" + rSelected.displayName + "</color></size>" + strQuantity;

        text += iRef.description + "\n\n";
        if( CookingScript.CheckRecipe(rSelected.refName,GameMasterScript.heroPCActor.myInventory.GetAllCookingIngredients()) != null)
        {
            text += "<color=yellow>" + StringManager.GetString("misc_craft_requires").ToUpperInvariant() + ":</color> " + rSelected.ingredientsDescription + "\n\n";
        }
        else
        {
            text += "<color=red>" + StringManager.GetString("misc_craft_requires").ToUpperInvariant() + ": " + rSelected.ingredientsDescription + "</color>\n\n";
        }

        Consumable c = iRef as Consumable;
        if (c.isHealingFood)
        {            
            text += "<color=yellow>" + c.EstimateFoodHealing() + "</color>\n\n";
        }
        if (!String.IsNullOrEmpty(iRef.extraDescription))
        {
            text += UIManagerScript.cyanHexColor + iRef.extraDescription + "</color>\n\n";
        }
        if (!String.IsNullOrEmpty(c.effectDescription))
        {
            text += UIManagerScript.cyanHexColor + c.effectDescription + "</color>\n\n";
        }

        recipeText.text = text;
    }

    public static void UpdateMonsterPedia()
    {
        monsterPediaText.text = StringManager.GetString("monsterpedia_default_text");

        List<MonsterTemplateData> pediaToUse = BakedMonsterpedia.GetAllMonstersInPedia();

        int maxMonsters = pediaToUse.Count;

        int numButtons = monsterButtonList.Length;

        // If we CAN scroll up or down, show the appropriate clickable buttons / visual indicators.
        if (pediaUpButton != null)
        {
            if (monsterPediaOffset > 0)
            {
                pediaUpButton.color = Color.white;
            }
            else
            {
                pediaUpButton.color = UIManagerScript.transparentColor;
            }
        }
        if (pediaDownButton != null)
        {
            int offsetLimit = maxMonsters - numButtons;
            if (monsterPediaOffset >= offsetLimit)
            {
                pediaDownButton.color = UIManagerScript.transparentColor;
            }
            else
            {
                pediaDownButton.color = Color.white;
            }
        }

        for (int i = 0; i < numButtons; i++)
        {
            monsterButtonList[i].gameObj.GetComponent<RectTransform>().localScale = Vector3.one;

            int workIndex = i + monsterPediaOffset;

            if (workIndex < maxMonsters)
            {
                monsterButtonList[i].enabled = true;
                string mRef = pediaToUse[workIndex].refName;
                if (MetaProgressScript.monstersDefeated.ContainsKey(mRef))
                {
                    monsterButtonList[i].subObjectImage.color = Color.white;
                }
                else
                {
                    monsterButtonList[i].subObjectImage.color = UIManagerScript.transparentColor;
                }
            }
            else
            {
                monsterButtonList[i].enabled = false;
                monsterButtonList[i].subObjectImage.color = UIManagerScript.transparentColor;
            }

        }

        GameMasterScript.gmsSingleton.statsAndAchievements.SetMonstersKnown(MetaProgressScript.monstersDefeated.Keys.Count);

    }

    public static void UpdateQuests()
    {
        quest1.enabled = false;
        quest2.enabled = false;
        quest3.enabled = false;
        quest1.gameObj.SetActive(false);
        quest2.gameObj.SetActive(false);
        quest3.gameObj.SetActive(false);
        
        if (GameMasterScript.heroPCActor.myQuests.Count == 0)
        {
            //questText.text = "You have not heard any rumors lately. Try asking around in <color=yellow>Riverstone Camp</color>...";
            return;
        }
        else
        {
            //Completed or failed quests are not removed until the end of a game turn.
            //If the player drinks her flask, that quest might fail, but the turn won't end.
            //Using this code below we can make sure to never display completed quests here.
            var listQuests = GameMasterScript.heroPCActor.myQuests;
            listQuests.RemoveAll(q => q == null || q.complete);

            for (int i = 0; i < listQuests.Count; i++)
            {
                if (listQuests[i] == null)
                {
                    Debug.Log("Hero has null quest?");
                    continue;
                }

                switch(i)
                {
                    case 0:
                        quest1.gameObj.SetActive(true);
                        quest1Text.text = "<size=40>1.</size> " + listQuests[i].GetAllQuestTextExceptRewards(40);
                        quest1RewardText.text = listQuests[i].GetRewardText(40);
                        quest1.enabled = true;
                        break;
                    case 1:
                        quest2.gameObj.SetActive(true);
                        quest2Text.text = "<size=40>2.</size> " + listQuests[i].GetAllQuestTextExceptRewards(40);
                        quest2RewardText.text = listQuests[i].GetRewardText(40);
                        quest2.enabled = true;
                        break;
                    case 2:
                        quest3.gameObj.SetActive(true);
                        quest3Text.text = "<size=40>3.</size> " + listQuests[i].GetAllQuestTextExceptRewards(40);
                        quest3RewardText.text = listQuests[i].GetRewardText(40);
                        quest3.enabled = true;
                        break;
                    default:
                        Debug.Log("The Quest Log only has room for three quests.");
                        break;
                }
            }
            //questText.text = builder;
        }
    }

    public static void UpdateCombatLog()
    {
        combatLogText.text = "";
        StringBuilder sb = new StringBuilder();
        //We want the most recent events to be at the top of the display.
        foreach(string tex in GameLogScript.journalLogStringBuffer)
        {
            sb.Insert(0, tex + "\n");
        }
        combatLogText.text = sb.ToString();

        if (PlatformVariables.OPTIMIZED_GAME_LOG)
        {
            combatLogText.lineSpacing = GameLogScript.dictLogSpacingInfoByLanguageAndSize[StringManager.gameLanguage][false].iLineSpacing;
        }
    }

    public static void UpdateRecipes()
    {
        recipeText.text = "";

        for (int i = 0; i < recipeButtonList.Length; i++)
        {
            if (i >= MetaProgressScript.recipesKnown.Count)
            {
                recipeButtonList[i].subObjectImage.sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, "assorteditems_513");
                recipeButtonList[i].gameObj.GetComponentInChildren<TextMeshProUGUI>().text = "????";
            }
            else
            {
                UpdateRecipeButtonBasedOnIngredientAvailability(i);
            }
        }
    }

    static void UpdateRecipeButtonBasedOnIngredientAvailability( int iButtonIndex )
    {
        Recipe rSelected = CookingScript.FindRecipe(MetaProgressScript.recipesKnown[iButtonIndex]);
        Item iRef = Item.GetItemTemplateFromRef(rSelected.itemCreated);
        recipeButtonList[iButtonIndex].subObjectImage.sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, iRef.spriteRef);

        //Color the recipe red if it cannot be crafted for lack of ingredients
        var txtName = recipeButtonList[iButtonIndex].gameObj.GetComponentInChildren<TextMeshProUGUI>();
        txtName.text = rSelected.displayName;

        if (StringManager.gameLanguage == EGameLanguage.jp_japan || StringManager.gameLanguage == EGameLanguage.zh_cn)
        {
            FontManager.AdjustFontSize(txtName, 24f);
        }

        Dictionary<Item, int> itemsUsed = CookingScript.CheckRecipe(rSelected.refName, GameMasterScript.heroPCActor.myInventory.GetInventory());
        if (itemsUsed == null)
        {
            txtName.color = Color.red;
        }
        else
        {
            txtName.color = Color.white;
        }
    }

    public void TryQuitRumor(int index)
    {        
        if (index >= GameMasterScript.heroPCActor.myQuests.Count)
        {
            Debug.Log("Trying to quit unknown rumor.");
            return;
        }
        if (GameMasterScript.heroPCActor.myQuests[index] == null)
        {
            Debug.Log("Can't quit null quest.");
            return;
        }

        UIManagerScript.ForceCloseFullScreenUIWithNoFade();        
        Conversation abandon = GameMasterScript.FindConversation("quitrumor");
        QuestScript qsToQuit = GameMasterScript.heroPCActor.myQuests[index];
        StringManager.ClearTags();
        StringManager.SetTag(0, qsToQuit.displayName);
        GameMasterScript.gmsSingleton.SetTempGameData("abandonquestindex", index);
        UIManagerScript.StartConversation(abandon, DialogType.STANDARD, null);
    }

    public void HoverMonsterInfo(int index)
    {
        //Debug.Log("Index is " + index + " pedia length " + GameMasterScript.monstersInPedia.Count);

        UIManagerScript.ChangeUIFocusAndAlignCursor(monsterButtonList[index]);

        int workIndex = index + monsterPediaOffset;

        List<MonsterTemplateData> pediaToUse = BakedMonsterpedia.GetAllMonstersInPedia();

        if (workIndex >= pediaToUse.Count) return;

        MonsterTemplateData mtd = pediaToUse[workIndex];

        int numDefeated = MetaProgressScript.GetMonstersDefeated(mtd.refName);
            
        if (numDefeated == 0)
        {
            monsterPediaText.text = "?????";
            return;
        }

        string text = FontManager.GetLargeSizeTagForCurrentLanguage() + mtd.monsterName + "</size>\n\n";

        StringManager.SetTag(0, mtd.baseLevel.ToString());
        StringManager.SetTag(1, Monster.GetFamilyName(mtd.monFamily));

        text += StringManager.GetString("monsterpedia_stats1");

        StringManager.SetTag(0, numDefeated.ToString());
        text += StringManager.GetString("monsterpedia_stats2");

        if (mtd.isBoss || numDefeated >= 3)
        {
            int baseHealth = (int)mtd.hp;
            float[] statArray = new float[12];
            statArray[(int)StatTypes.STRENGTH] = mtd.strength;
            statArray[(int)StatTypes.SWIFTNESS] = mtd.swiftness;
            statArray[(int)StatTypes.SPIRIT] = mtd.spirit;
            statArray[(int)StatTypes.DISCIPLINE] = mtd.discipline;
            statArray[(int)StatTypes.GUILE] = mtd.guile;

            float highest = 0f;
            int iHighIndex = -1;
            for (int i = 0; i < statArray.Length; i++)
            {
                if (statArray[i] > highest)
                {
                    highest = statArray[i];
                    iHighIndex = i;
                }
            }
            StringManager.SetTag(0, baseHealth.ToString());
            StringManager.SetTag(1, StatBlock.statNames[iHighIndex]);
            StringManager.SetTag(2, ((int)statArray[iHighIndex]).ToString());
            StringManager.SetTag(3, mtd.aggroRange.ToString());

            text += StringManager.GetString("monsterpedia_stats3");
        }

        string wildChildText = "";

        if (mtd.isBoss || numDefeated >= 5)
        {
            Weapon w = GameMasterScript.masterItemList[mtd.weaponID] as Weapon;
            StringManager.SetTag(0, ((int)w.power * 10f).ToString());

            string abils = "";
            int x = 0;
            foreach (MonsterPowerData mpd in mtd.monsterPowers)
            {
                if (!string.IsNullOrEmpty(mpd.abilityRef.teachPlayerAbility))
                {
                    abils += "</color>" + UIManagerScript.greenHexColor + "*" + mpd.abilityRef.abilityName + "*</color>" + UIManagerScript.cyanHexColor;
                    StringManager.SetTag(4, GameMasterScript.masterAbilityList[mpd.abilityRef.teachPlayerAbility].abilityName);
                    wildChildText = StringManager.GetString("monsterpedia_playerlearn");
                }
                else
                {
                    abils += mpd.abilityRef.abilityName;
                }
                
                if (x < mtd.monsterPowers.Count - 1)
                {
                    abils += ", ";
                }
                x++;
            }
            StringManager.SetTag(1, abils);
            text += StringManager.GetString("monsterpedia_stats4");
            if (!string.IsNullOrEmpty(wildChildText))
            {
                text += wildChildText + "\n\n";
            }
        }

        if (mtd.isBoss || numDefeated >= 10)
        {
            string attributeText = "";
            int countAttr = 0;
            for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
            {
                if (mtd.monAttributes[i] > 0)
                {
                    if (!string.IsNullOrEmpty(Monster.GetAttributeName(i)))
                    {
                        countAttr++;
                    }
                }
            }

            int localCount = 0;
            for (int i = 0; i < (int)MonsterAttributes.COUNT; i++)
            {
                if (mtd.monAttributes[i] > 0)
                {
                    attributeText += "<color=yellow>" + Monster.GetAttributeName(i) + "</color> (" + mtd.monAttributes[i] + StringManager.GetLocalizedSymbol(AbbreviatedSymbols.PERCENT) + ")";
                    if (localCount < countAttr - 1)
                    {
                        attributeText += ", ";
                    }
                    localCount++;
                }
            }


            StringManager.SetTag(0, attributeText);
            text += StringManager.GetString("monsterpedia_stats5");
        }

        monsterPediaText.text = text;
    }

    public void TryScrollPediaDown(int dummy)
    {
        List<MonsterTemplateData> pediaToUse = BakedMonsterpedia.GetAllMonstersInPedia();

        // Say we have 85 monsters
        // Normally there are 70 buttons total
        // Our offset should not exceed (85-70+10) = 25
        int max = pediaToUse.Count - monsterButtonList.Length + NUM_MONSTERS_PER_ROW;
        if (monsterPediaOffset + NUM_MONSTERS_PER_ROW > max)
        {
            // Loop back to top button, but DON'T scroll list to top? See how it feels.
            monsterButtonList[monsterButtonList.Length - 1].MoveCursorToNeighbor((int)Directions.SOUTH);

            return;
        }

        UIManagerScript.PlayCursorSound("Move");

        monsterPediaOffset += NUM_MONSTERS_PER_ROW;
        singleton.UpdateMonsterPediaGraphics(firstTimeSetup: false);
        UpdateMonsterPedia();
    }

    // Scrolls UP from the top of the pedia, shifting offset by 10
    // If we are already at 0, move to neighbor instead.
    public void TryScrollPediaUp(int dummy)
    {
        if (monsterPediaOffset == 0)
        {
            monsterButtonList[0].MoveCursorToNeighbor((int)Directions.NORTH); // should move to Monsterpedia tab
            return;
        }

        UIManagerScript.PlayCursorSound("Move");
        monsterPediaOffset -= NUM_MONSTERS_PER_ROW;
        singleton.UpdateMonsterPediaGraphics(firstTimeSetup:false);
        UpdateMonsterPedia();
    }

    void UpdateMonsterPediaGraphics(bool firstTimeSetup)
    {
        int column = 0;
        int row = 0;

        List<MonsterTemplateData> pediaToUse = BakedMonsterpedia.GetAllMonstersInPedia();
        int monsterCount = pediaToUse.Count;

        for (int i = 0; i < monsterButtonList.Length; i++)
        {
            int workIndex = i + monsterPediaOffset;
            // Index is taken into account when we have an offset
            // We want to pull the correct monster from the master list of (potentially) 100+ monsters


            // Only need to construct the UIObject once
            // After that ,we're just changing the contents (sprite)
            // Other functions will reference the appropriate offset from the master monster list
            if (firstTimeSetup)
            {
                monsterButtonList[i] = new UIManagerScript.UIObject();
                monsterButtonList[i].gameObj = GameMasterScript.TDInstantiate("MonsterCompendiumButton");
                monsterButtonList[i].gameObj.GetComponent<RectTransform>().localScale = Vector3.one;
                monsterButtonList[i].subObjectImage = monsterButtonList[i].gameObj.GetComponentsInChildren<Image>()[1];
            }

            bool buttonIsVisible = false;

            if (workIndex < monsterCount)
            {
                GameObject go = GameMasterScript.TDInstantiate(pediaToUse[workIndex].prefab);
                Sprite spr = go.GetComponent<Animatable>().myAnimations[0].mySprites[0].mySprite;
                monsterButtonList[i].subObjectImage.sprite = spr;
                // Some sprites are so HUUUUGE that we can't 2x scale them without covering up other sprites!
                // So let's make sure we're only 2x scaling reasonably sized ones.
                float useWidth = spr.rect.width;
                float useHeight = spr.rect.height;
                if (useWidth < 150f) useWidth *= 2f;
                if (useHeight < 150f) useHeight *= 2f;
                monsterButtonList[i].subObjectImage.gameObject.GetComponentInChildren<RectTransform>().sizeDelta = new Vector2(useWidth, useHeight);
                // We can get rid of the prefab once we pull the necessary sprite from it
                GameMasterScript.ReturnToStack(go, go.name.Replace("(Clone)", String.Empty));
                buttonIsVisible = true;
                monsterButtonList[i].enabled = true;
                monsterButtonList[i].gameObj.GetComponent<Image>().color = compendiumButtonBGColor;
            }
            else
            {
                monsterButtonList[i].enabled = false; // Disable buttons where there is no corresponding monster in list
                monsterButtonList[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
            }

            monsterButtonList[i].gameObj.transform.SetParent(monsterPediaParent.transform);
            monsterButtonList[i].gameObj.GetComponent<Image>().sprite = null;
            monsterButtonList[i].onSelectValue = i;
            monsterButtonList[i].myOnSelectAction = HoverMonsterInfo;

            // Events only need to be set up once per button
            if (firstTimeSetup)
            {
                EventTrigger et = monsterButtonList[i].gameObj.GetComponent<EventTrigger>();
                et.triggers.Clear();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;

                int local_i = i;

                entry.callback.AddListener((eventData) => { HoverMonsterInfo(local_i); });
                et.triggers.Add(entry);

                float calcX = BASE_X_POS + (column * MONSTER_PIXEL_SPACE_X);
                float calcY = BASE_Y_POS - (row * MONSTER_PIXEL_SPACE_Y);

                monsterButtonList[i].gameObj.transform.localPosition = new Vector2(calcX, calcY);
            }        

            column++;

            if (column >= NUM_MONSTERS_PER_ROW)
            {
                column = 0;
                row++;
            }
        }
    }

    public void OnHoverRumorExitButton(int index)
    {
        UIManagerScript.ChangeUIFocusAndAlignCursor(questExitButtons[index]);
    }

    public static void TryWriteLogToDisk()
    {
        // Writing the log as a text file to disk really only makes sense on PC.
#if UNITY_SWITCH || UNITY_PS4 || UNITY_XBOXONE || UNITY_ANDROID
        return;
#endif
        string logPath = Application.persistentDataPath + "/combatlog" + GameStartData.saveGameSlot + ".txt";
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }
        StringBuilder sb = new StringBuilder();
        //We want the most recent events to be at the top of the display.
        foreach (string tex in GameLogScript.journalLogStringBuffer)
        {
            sb.Insert(0, tex + "\n");
        }
        string allText = sb.ToString();
        allText = CustomAlgorithms.StripColors(allText);
        File.WriteAllText(logPath, allText);
    }
}
