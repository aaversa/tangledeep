using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Rewired;

public class CreationFeat
{
    public string featName;
    public string description;
    public string skillRef;
    public bool mustBeUnlocked;
    public string spriteRef;

    public bool randomJobModeDisallow;
    
    // 0 - disallowed in SharaMode
    // 1 - allowed in SharaMode
    // 2 - exclusive to SharaMode
    public int sharaModeStatus;

    public static CreationFeat FindFeatBySkillRef(string featRef)
    {
        foreach(CreationFeat cf in GameMasterScript.masterFeatList)
        {
            if (cf.skillRef == featRef)
            {
                return cf;
            }
        }
        return null;
    }
}

public enum ENameEntryScreenState
{
    deciding_on_name = 0,
    name_confirmed_and_ready_to_go,
    game_loading_stop_updating,
    max
}
public class CharCreation : MonoBehaviour {

    public const float JOBSELECT_POS_X_START = -660f;
    public const float JOBSELECT_XDISTANCE_DEFAULT = 250f;

    public static UIManagerScript.UIObject[] jobButtons;
    public static GameObject[] jobPrefabs;
    public static TextMeshProUGUI jobDescText;
    public static UIManagerScript.UIObject[] jobSkillIcons;
    public static GameObject ccSkillHover;
    public static TextMeshProUGUI ccSkillHoverText;
    public static UIManagerScript.UIObject createCharButton;
    public static bool jobSelected;
    public static int indexOfHoveringJob;
    public static int indexOfSelectedJob;
    //public static int enumOfSelectedJob;
    public static CharCreation singleton;
    public static bool creationActive;
    public static int totalCharacters;

    public static UIManagerScript.UIObject titleScreenConfirmButton;
    public static UIManagerScript.UIObject randomNameButton;

    [SerializeField]
    private CanvasGroup NameInputParentCanvasGroup;
    [SerializeField]
    public TMP_InputField NameInputTextBox;
    [SerializeField]
    public TMP_InputField worldSeedInput;
    [SerializeField]
    private TextMeshProUGUI worldSeedPlaceholder;

    [SerializeField]
    private List<Image> img_feats;
    [SerializeField]
    private List<TextMeshProUGUI> label_feats;
    [SerializeField]
    private TextMeshProUGUI label_difficulty;
    [SerializeField]
    private TextMeshProUGUI label_title;
    [SerializeField]
    private TextMeshProUGUI label_worldSeedTitle;

    [SerializeField]
    private List<TextMeshProUGUI> list_nameEntryCommands;

    [SerializeField]
    private TextMeshProUGUI label_begin_game;
    [SerializeField]
    private TextMeshProUGUI label_go_back;

    [SerializeField]
    private GameObject beginGameButton;
    [SerializeField]
    private GameObject goBackButton;

    [SerializeField]
    private TextMeshProUGUI label_job_name;

    [SerializeField]
    private Image           img_portrait;

    [SerializeField]
    private Animatable      anim_SelectedHeroineComponent;

    private int             iSelectedConfirmCharacterOption;

    [HideInInspector]
    static ENameEntryScreenState nameEntryScreenState = ENameEntryScreenState.max;
    public static ENameEntryScreenState NameEntryScreenState
    {
        get
        {
            return nameEntryScreenState;
        }
        set
        {            
            nameEntryScreenState = value;
            //if (Debug.isDebugBuild) Debug.Log("Setting name entry state to " + nameEntryScreenState);
        }
    }

    public static TMP_InputField nameInputTextBox;
    public static CanvasGroup nameInputParentCanvasGroup;
    public static List<string> randomNameList;

    static int[] jobEnumOrder;

    public const int NUM_JOBS = 14;

    public static TextMeshProUGUI jobAbilHeader;

    public static float timeAtLastJobChange;

    private bool bInitialized;

    void Awake()
    {
        nameInputTextBox = NameInputTextBox;
        nameInputParentCanvasGroup = NameInputParentCanvasGroup;

        if (myCG == null) myCG = GetComponent<CanvasGroup>();

        myCG.interactable = false;
        myCG.blocksRaycasts = false;

        if (nameInputTextBox == null) return;

        nameInputTextBox.onDeselect.AddListener(arg0 =>
        {
            //if (Debug.isDebugBuild) Debug.Log("Dialog box input text box has been deselected.");
            nameInputIsActive = false;
        });

    }


    void Start ()
    {
        singleton = this;

        titleScreenConfirmButton = new UIManagerScript.UIObject();
        titleScreenConfirmButton.gameObj = GameObject.Find("ConfirmName");
        titleScreenConfirmButton.button = new ButtonCombo();
        titleScreenConfirmButton.button.dbr = DialogButtonResponse.CONTINUE;
        titleScreenConfirmButton.mySubmitFunction = StartCharacterCreation4FromButton;
        if (titleScreenConfirmButton.gameObj != null)
        {
            TextMeshProUGUI titleConfirm = titleScreenConfirmButton.gameObj.GetComponentInChildren<TextMeshProUGUI>();
            if (titleConfirm != null)
            {
                if (StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    titleConfirm.text = StringManager.GetString("dialog_adjust_quantity_main_btn_0").ToUpperInvariant();
                }
                else
                {
                    titleConfirm.text = StringManager.GetString("dialog_adjust_quantity_main_btn_0");
                }
                
            }
        }
        
        //titleScreenConfirmButton.mySubmitFunction = UIManagerScript.singletonUIMS.StartCharacterCreation4FromButton;

        randomNameButton = new UIManagerScript.UIObject();
        randomNameButton.gameObj = GameObject.Find("RandomName");
        randomNameButton.mySubmitFunction = GenerateRandomName;
        if (randomNameButton.gameObj != null)
        {
            TextMeshProUGUI randomButton = randomNameButton.gameObj.GetComponentInChildren<TextMeshProUGUI>();
            if (randomButton != null)
            {
                if (StringManager.gameLanguage != EGameLanguage.de_germany)
                {
                    randomButton.text = StringManager.GetString("misc_shape_random").ToUpperInvariant();
                }
                else
                {
                    randomButton.text = StringManager.GetString("misc_shape_random");
                }
                
            }
        }        

        jobAbilHeader = GameObject.Find("JobAbilHeader").GetComponent<TextMeshProUGUI>();

        titleScreenConfirmButton.neighbors[(int)Directions.WEST] = randomNameButton;
        titleScreenConfirmButton.neighbors[(int)Directions.EAST] = randomNameButton;

        randomNameButton.neighbors[(int)Directions.WEST] = titleScreenConfirmButton;
        randomNameButton.neighbors[(int)Directions.EAST] = titleScreenConfirmButton;
    }

    bool localizedEverything = false;

    void SetFontsAndSizesForAllText()
    {        
        //localmalize all these
        if (localizedEverything)
        {
            return;
        }
        if (NameInputTextBox != null ) FontManager.LocalizeMe(NameInputTextBox.textComponent, TDFonts.WHITE);
        if( worldSeedInput != null ) FontManager.LocalizeMe(worldSeedInput.textComponent, TDFonts.WHITE);
        if (worldSeedPlaceholder != null)
        {
            FontManager.LocalizeMe(worldSeedPlaceholder, TDFonts.WHITE_NO_OUTLINE);
            worldSeedPlaceholder.text = ""; //StringManager.GetString("ui_text_placeholder_inputseed");
        }

        if (label_difficulty != null) FontManager.LocalizeMe(label_difficulty, TDFonts.WHITE);
        if (label_title != null) FontManager.LocalizeMe(label_title, TDFonts.WHITE);
        if (label_worldSeedTitle != null) FontManager.LocalizeMe(label_worldSeedTitle, TDFonts.WHITE);
        if (label_begin_game != null) FontManager.LocalizeMe(label_begin_game, TDFonts.BLACK);
        if (label_go_back != null) FontManager.LocalizeMe(label_go_back, TDFonts.BLACK);
        if (label_job_name != null) FontManager.LocalizeMe(label_job_name, TDFonts.WHITE);

        if( label_feats != null )
        {
            foreach (var tmp in label_feats)
            {
                FontManager.LocalizeMe(tmp, TDFonts.WHITE);
            }
        }

        if( list_nameEntryCommands != null )
        {
            foreach (var tmp in list_nameEntryCommands)
            {
                FontManager.LocalizeMe(tmp, TDFonts.WHITE);
            }
        }

        localizedEverything = true;
    }
    public void GenerateRandomName(int dummy)
    {
        GenerateRandomNameAndFillField();
    }

    public void SetWorldSeed()
    {
        if (string.IsNullOrEmpty(worldSeedInput.text))
        {
            return;
        }
        long holder;
        Int64.TryParse(worldSeedInput.text, out holder);
        if (holder >= Int32.MaxValue)
        {
            holder %= Int32.MaxValue;            
        }

        GameStartData.worldSeed = (int)holder;

        if (Debug.isDebugBuild) Debug.Log("Set world seed in char creation to " + GameStartData.worldSeed);
    }

	public void Initialize (bool firstCCInitialize = false)
    {
	    if (bInitialized && !firstCCInitialize) return;
	    bInitialized = true;

	    jobAbilHeader.text = StringManager.GetString("ui_misc_jobabilities");

        FontManager.LocalizeMe(jobAbilHeader, TDFonts.WHITE);

        jobButtons = new UIManagerScript.UIObject[NUM_JOBS];
        jobPrefabs = new GameObject[NUM_JOBS];


        jobEnumOrder = new int[(int)CharacterJobs.COUNT];

        jobEnumOrder[0] = (int)CharacterJobs.BRIGAND;
        jobEnumOrder[1] = (int)CharacterJobs.FLORAMANCER;
        jobEnumOrder[2] = (int)CharacterJobs.SWORDDANCER;
        jobEnumOrder[3] = (int)CharacterJobs.PALADIN;
        jobEnumOrder[4] = (int)CharacterJobs.BUDOKA;
        jobEnumOrder[5] = (int)CharacterJobs.HUNTER;        
        jobEnumOrder[6] = (int)CharacterJobs.SPELLSHAPER;
        jobEnumOrder[7] = (int)CharacterJobs.EDGETHANE;
        jobEnumOrder[8] = (int)CharacterJobs.SOULKEEPER;
        jobEnumOrder[9] = (int)CharacterJobs.GAMBLER;
        jobEnumOrder[10] = (int)CharacterJobs.HUSYN;
        jobEnumOrder[11] = (int)CharacterJobs.WILDCHILD;
        jobEnumOrder[12] = (int)CharacterJobs.DUALWIELDER;
        jobEnumOrder[13] = (int)CharacterJobs.MIRAISHARA;

        for (int i = 0; i < jobButtons.Length; i++)
        {
            string find = "Job" + (i + 1) + "Image";
            var goFind = GameObject.Find(find);

            jobButtons[i] = new UIManagerScript.UIObject();
            jobButtons[i].gameObj = goFind;

            find = "Job" + (i + 1) + "Sprite";
            goFind = GameObject.Find(find);

            jobButtons[i].subObjectImage = goFind.GetComponent<Image>();

            jobButtons[i].myOnSelectAction = HoverJobInfo;
            jobButtons[i].onSelectValue = i;

            jobButtons[i].mySubmitFunction = SelectJob;
            jobButtons[i].onSubmitValue = i;

            jobPrefabs[i] = GameObject.Find("Job" + (i + 1) + "GameObject");            
        }

        jobDescText = GameObject.Find("JobDescText").GetComponent<TextMeshProUGUI>();
        ccSkillHover = GameObject.Find("CCSkillHover");
        ccSkillHoverText = GameObject.Find("CCSkillHoverText").GetComponent<TextMeshProUGUI>();

        FontManager.LocalizeMe(jobDescText, TDFonts.WHITE);
        FontManager.LocalizeMe(ccSkillHoverText, TDFonts.WHITE);

        ccSkillHover.SetActive(false);

        jobSkillIcons = new UIManagerScript.UIObject[14];

        for (int i = 0; i < jobSkillIcons.Length; i++)
        {
            jobSkillIcons[i] = new UIManagerScript.UIObject();
            jobSkillIcons[i].gameObj = GameObject.Find("JobSkill" + (i + 1));
            jobSkillIcons[i].gameObj.SetActive(false);
            jobSkillIcons[i].onSelectValue = i;
            jobSkillIcons[i].myOnSelectAction = HoverSkillInfo;
        }

        createCharButton = new UIManagerScript.UIObject();
        createCharButton.gameObj = GameObject.Find("CreateCharacter");
        createCharButton.myOnSelectAction = ClearJobSkillPopup;

        TextMeshProUGUI createCharButtonText = createCharButton.gameObj.GetComponentInChildren<TextMeshProUGUI>();
        createCharButtonText.text = StringManager.GetString("ui_btn_selectjob");
        FontManager.LocalizeMe(createCharButtonText, TDFonts.BLACK);

        createCharButton.neighbors[(int)Directions.NORTH] = jobSkillIcons[7];
        createCharButton.neighbors[(int)Directions.SOUTH] = jobSkillIcons[0];

        jobButtons[0].neighbors[(int)Directions.NORTH] = jobButtons[6];
        jobButtons[1].neighbors[(int)Directions.NORTH] = jobButtons[7];
        jobButtons[2].neighbors[(int)Directions.NORTH] = jobButtons[8];
        jobButtons[3].neighbors[(int)Directions.NORTH] = jobButtons[9];
        jobButtons[4].neighbors[(int)Directions.NORTH] = jobButtons[10];
        jobButtons[5].neighbors[(int)Directions.NORTH] = jobButtons[11];

        jobButtons[0].neighbors[(int)Directions.SOUTH] = jobButtons[6];
        jobButtons[1].neighbors[(int)Directions.SOUTH] = jobButtons[7];
        jobButtons[2].neighbors[(int)Directions.SOUTH] = jobButtons[8];
        jobButtons[3].neighbors[(int)Directions.SOUTH] = jobButtons[9];
        jobButtons[4].neighbors[(int)Directions.SOUTH] = jobButtons[10];
        jobButtons[5].neighbors[(int)Directions.SOUTH] = jobButtons[11];


        jobButtons[6].neighbors[(int)Directions.NORTH] = jobButtons[0];
        jobButtons[7].neighbors[(int)Directions.NORTH] = jobButtons[1];
        jobButtons[8].neighbors[(int)Directions.NORTH] = jobButtons[2];
        jobButtons[9].neighbors[(int)Directions.NORTH] = jobButtons[3];
        jobButtons[10].neighbors[(int)Directions.NORTH] = jobButtons[4];
        jobButtons[11].neighbors[(int)Directions.NORTH] = jobButtons[5];


        jobButtons[6].neighbors[(int)Directions.SOUTH] = jobButtons[0];
        jobButtons[7].neighbors[(int)Directions.SOUTH] = jobButtons[1];
        jobButtons[8].neighbors[(int)Directions.SOUTH] = jobButtons[2];
        jobButtons[9].neighbors[(int)Directions.SOUTH] = jobButtons[3];
        jobButtons[10].neighbors[(int)Directions.SOUTH] = jobButtons[4];
        jobButtons[11].neighbors[(int)Directions.SOUTH] = jobButtons[5];

        jobButtons[0].neighbors[(int)Directions.EAST] = jobButtons[1];
        jobButtons[0].neighbors[(int)Directions.WEST] = jobButtons[12]; // Was 5, but now points to 12 (new job)

        jobButtons[1].neighbors[(int)Directions.EAST] = jobButtons[2];
        jobButtons[1].neighbors[(int)Directions.WEST] = jobButtons[0];

        jobButtons[2].neighbors[(int)Directions.EAST] = jobButtons[3];
        jobButtons[2].neighbors[(int)Directions.WEST] = jobButtons[1];

        jobButtons[3].neighbors[(int)Directions.EAST] = jobButtons[4];
        jobButtons[3].neighbors[(int)Directions.WEST] = jobButtons[2];

        jobButtons[4].neighbors[(int)Directions.EAST] = jobButtons[5];
        jobButtons[4].neighbors[(int)Directions.WEST] = jobButtons[3];

        jobButtons[5].neighbors[(int)Directions.EAST] = jobButtons[12]; // Was 0, but now points to 12 (new job)
        jobButtons[5].neighbors[(int)Directions.WEST] = jobButtons[4];

        // Stupid 13th job sits on the top row
        jobButtons[12].neighbors[(int)Directions.EAST] = jobButtons[0];
        jobButtons[12].neighbors[(int)Directions.WEST] = jobButtons[5];

        jobButtons[6].neighbors[(int)Directions.EAST] = jobButtons[7];
        jobButtons[6].neighbors[(int)Directions.WEST] = jobButtons[11];

        jobButtons[7].neighbors[(int)Directions.EAST] = jobButtons[8];
        jobButtons[7].neighbors[(int)Directions.WEST] = jobButtons[6];

        jobButtons[8].neighbors[(int)Directions.EAST] = jobButtons[9];
        jobButtons[8].neighbors[(int)Directions.WEST] = jobButtons[7];

        jobButtons[9].neighbors[(int)Directions.EAST] = jobButtons[10];
        jobButtons[9].neighbors[(int)Directions.WEST] = jobButtons[8];

        jobButtons[10].neighbors[(int)Directions.EAST] = jobButtons[11];
        jobButtons[10].neighbors[(int)Directions.WEST] = jobButtons[9];

        jobButtons[11].neighbors[(int)Directions.EAST] = jobButtons[6];
        jobButtons[11].neighbors[(int)Directions.WEST] = jobButtons[10];


        jobSkillIcons[0].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[1].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[2].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[3].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[4].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[5].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[6].neighbors[(int)Directions.NORTH] = createCharButton;
        jobSkillIcons[7].neighbors[(int)Directions.NORTH] = jobSkillIcons[0];
        jobSkillIcons[8].neighbors[(int)Directions.NORTH] = jobSkillIcons[1];
        jobSkillIcons[9].neighbors[(int)Directions.NORTH] = jobSkillIcons[2];
        jobSkillIcons[10].neighbors[(int)Directions.NORTH] = jobSkillIcons[3];
        jobSkillIcons[11].neighbors[(int)Directions.NORTH] = jobSkillIcons[4];
        jobSkillIcons[12].neighbors[(int)Directions.NORTH] = jobSkillIcons[5];
        jobSkillIcons[13].neighbors[(int)Directions.NORTH] = jobSkillIcons[6];

        jobSkillIcons[0].neighbors[(int)Directions.SOUTH] = jobSkillIcons[7];
        jobSkillIcons[1].neighbors[(int)Directions.SOUTH] = jobSkillIcons[8];
        jobSkillIcons[2].neighbors[(int)Directions.SOUTH] = jobSkillIcons[9];
        jobSkillIcons[3].neighbors[(int)Directions.SOUTH] = jobSkillIcons[10];
        jobSkillIcons[4].neighbors[(int)Directions.SOUTH] = jobSkillIcons[11];
        jobSkillIcons[5].neighbors[(int)Directions.SOUTH] = jobSkillIcons[12];
        jobSkillIcons[6].neighbors[(int)Directions.SOUTH] = jobSkillIcons[13];
        jobSkillIcons[7].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[8].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[9].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[10].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[11].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[12].neighbors[(int)Directions.SOUTH] = createCharButton;
        jobSkillIcons[13].neighbors[(int)Directions.SOUTH] = createCharButton;

        jobSkillIcons[0].neighbors[(int)Directions.EAST] = jobSkillIcons[1];
        jobSkillIcons[1].neighbors[(int)Directions.EAST] = jobSkillIcons[2];
        jobSkillIcons[2].neighbors[(int)Directions.EAST] = jobSkillIcons[3];
        jobSkillIcons[3].neighbors[(int)Directions.EAST] = jobSkillIcons[4];
        jobSkillIcons[4].neighbors[(int)Directions.EAST] = jobSkillIcons[5];
        jobSkillIcons[5].neighbors[(int)Directions.EAST] = jobSkillIcons[6];
        jobSkillIcons[6].neighbors[(int)Directions.EAST] = jobSkillIcons[0];
        jobSkillIcons[7].neighbors[(int)Directions.EAST] = jobSkillIcons[8];
        jobSkillIcons[8].neighbors[(int)Directions.EAST] = jobSkillIcons[9];
        jobSkillIcons[9].neighbors[(int)Directions.EAST] = jobSkillIcons[10];
        jobSkillIcons[10].neighbors[(int)Directions.EAST] = jobSkillIcons[11];
        jobSkillIcons[11].neighbors[(int)Directions.EAST] = jobSkillIcons[12];
        jobSkillIcons[12].neighbors[(int)Directions.EAST] = jobSkillIcons[13];
        jobSkillIcons[13].neighbors[(int)Directions.EAST] = jobSkillIcons[7];

        jobSkillIcons[0].neighbors[(int)Directions.WEST] = jobSkillIcons[6];
        jobSkillIcons[1].neighbors[(int)Directions.WEST] = jobSkillIcons[0];
        jobSkillIcons[2].neighbors[(int)Directions.WEST] = jobSkillIcons[1];
        jobSkillIcons[3].neighbors[(int)Directions.WEST] = jobSkillIcons[2];
        jobSkillIcons[4].neighbors[(int)Directions.WEST] = jobSkillIcons[3];
        jobSkillIcons[5].neighbors[(int)Directions.WEST] = jobSkillIcons[4];
        jobSkillIcons[6].neighbors[(int)Directions.WEST] = jobSkillIcons[5];
        jobSkillIcons[7].neighbors[(int)Directions.WEST] = jobSkillIcons[13];
        jobSkillIcons[8].neighbors[(int)Directions.WEST] = jobSkillIcons[7];
        jobSkillIcons[9].neighbors[(int)Directions.WEST] = jobSkillIcons[8];
        jobSkillIcons[10].neighbors[(int)Directions.WEST] = jobSkillIcons[9];
        jobSkillIcons[11].neighbors[(int)Directions.WEST] = jobSkillIcons[10];
        jobSkillIcons[12].neighbors[(int)Directions.WEST] = jobSkillIcons[11];
        jobSkillIcons[13].neighbors[(int)Directions.WEST] = jobSkillIcons[12];

        //createCharButton.mySubmitFunction = StartCharCreation4;
        createCharButton.mySubmitFunction = ConfirmJobSelection;

	    SetFontsAndSizesForAllText();

        if (myCG == null) myCG = gameObject.GetComponent<CanvasGroup>();
        myCG.interactable = false;
        myCG.blocksRaycasts = false;

        gameObject.SetActive(false);

        jobSelected = false;
        creationActive = false;

        OrganizeAvailableJobs();
    }

    void EnableMiraiShara()
    {
        float distanceOffset = -20f;

        Vector3 posForJob14 = jobButtons[11].gameObj.transform.localPosition;
        posForJob14.x += JOBSELECT_XDISTANCE_DEFAULT + distanceOffset;

        jobButtons[13].enabled = true;

        jobButtons[13].gameObj.transform.localPosition = posForJob14;

        jobButtons[11].neighbors[(int)Directions.EAST] = jobButtons[13];
        jobButtons[13].neighbors[(int)Directions.WEST] = jobButtons[11];

        jobButtons[6].neighbors[(int)Directions.WEST] = jobButtons[13];
        jobButtons[13].neighbors[(int)Directions.EAST] = jobButtons[6];

        jobButtons[13].neighbors[(int)Directions.NORTH] = jobButtons[12];
        jobButtons[12].neighbors[(int)Directions.SOUTH] = jobButtons[13];

        jobButtons[13].neighbors[(int)Directions.SOUTH] = jobButtons[12];
        jobButtons[12].neighbors[(int)Directions.NORTH] = jobButtons[13];
    }

    void DisableMiraiShara()
    {
        jobButtons[13].enabled = false;
        Vector3 currentPos = jobButtons[13].gameObj.transform.localPosition;
        currentPos.x += 1500f;
        jobButtons[13].gameObj.transform.localPosition = currentPos;

        // Reroute the left and rightmost buttons in top row to skip MiraiShara
        jobButtons[6].neighbors[(int)Directions.WEST] = jobButtons[11];
        jobButtons[11].neighbors[(int)Directions.EAST] = jobButtons[6];
    }

    /// <summary>
    /// Sets up job object positioning, disables jobs that are not available based on DLC ownership
    /// </summary>
    void OrganizeAvailableJobs()
    {
        if (!DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1))
        {
            if (Debug.isDebugBuild) Debug.Log("Disabling LoS content at char creation.");

            // Disable the Calligrapher if we don't have first DLC, put it offscreen.
            jobButtons[12].enabled = false;

            Vector3 currentPos = jobButtons[12].gameObj.transform.localPosition;
            currentPos.x += 1500f;
            jobButtons[12].gameObj.transform.localPosition = currentPos;

            // Reroute the left and rightmost buttons in top row to skip Calligrapher
            jobButtons[0].neighbors[(int)Directions.WEST] = jobButtons[5];
            jobButtons[5].neighbors[(int)Directions.EAST] = jobButtons[0];

            DisableMiraiShara();
        }
        else
        {
            jobButtons[12].enabled = true;
            float startOffset = -50f;
            float distanceOffset = -20f;
            for (int i = 0; i < 12; i++)
            {
                Vector3 currentPos = jobButtons[i].gameObj.transform.localPosition;
                currentPos.x += startOffset;

                int effectiveIndex = i;
                if (i >= 6)
                {
                    effectiveIndex -= 6; 
                    // we're in second row, so change the offset
                }

                currentPos.x += (distanceOffset * effectiveIndex);
                jobButtons[i].gameObj.transform.localPosition = currentPos;
            }
            Vector3 posForJob13 = jobButtons[5].gameObj.transform.localPosition;
            posForJob13.x += JOBSELECT_XDISTANCE_DEFAULT + distanceOffset;
            jobButtons[12].gameObj.transform.localPosition = posForJob13;

            if (SharaModeStuff.CheckForSorceressUnlock())
            {
                EnableMiraiShara();
            }
            else
            {
                DisableMiraiShara();
            }
        }
    }

    void ClearJobSkillPopup(int value)
    {
        HoverSkillInfo(-1);
    }

    public void ConfirmJobSelection(int dummy)
    {
        if (!jobSelected)
        {
            // Play an error sound here?
            return;
        }

        int enumOfSelectedJob = jobEnumOrder[indexOfSelectedJob];

        if (GameMasterScript.actualGameStarted)
        {
            
            if ((CharacterJobs)enumOfSelectedJob == GameMasterScript.heroPCActor.myJob.jobEnum)
            {
                UIManagerScript.PlayCursorSound("Error");
                return;
            }

            string jobName = CharacterJobData.GetJobDataByEnum(enumOfSelectedJob).jobName.ToUpperInvariant();
            GameMasterScript.heroPCActor.ChangeJobs(jobName, GameMasterScript.GetItemToUse());
            EndCharCreation();
            
            UIManagerScript.PlayCursorSound("Ultra Learn");
            UIManagerScript.HideDialogMenuCursor();
            timeAtLastJobChange = Time.time;
        }
        else
        {
            EndCharCreation();

            GameStartData.playerJob = CharacterJobData.GetJobDataByEnum(enumOfSelectedJob).jobName;
            GameStartData.jobAsEnum = (CharacterJobs)enumOfSelectedJob;

            // go from here to feat selection, no longer name selection
            StartCharacterCreation_FeatSelect();


        }


    }

    /// <summary>
    /// Checks to see if we have the DLC installed and if the campaign is unlocked
    /// </summary>
    /// <returns>True if we own the expansion and have unlocked the mode</returns>
    public static bool IsSharaCampaignAvailable()
    {
        return DLCManager.CheckDLCInstalled(EDLCPackages.EXPANSION1) 
            && SharedBank.sharedProgressFlags[(int)SharedSlotProgressFlags.SHARA_MODE];
    }


    
    public void SelectJob(int buttonIndex)
    {
        if (!creationActive) return;
        if (jobButtons == null) return;

        int enumOfSelectedJob = jobEnumOrder[buttonIndex];

        if (!SharedBank.CheckIfJobIsUnlocked((CharacterJobs)enumOfSelectedJob))
        {
            SetMysteryJob();
            return;
        }

        UIManagerScript.PlayCursorSound("AltSelect");

        foreach (UIManagerScript.UIObject obj in jobButtons)
        {
            if ((obj == null) || (obj.gameObj == null))
            {
                //Debug.Log(value + " object null in CC?");
                return;
            }
            obj.gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
        }

        createCharButton.gameObj.SetActive(true);

        SelectJobRefresh(buttonIndex);

        UIManagerScript.ChangeUIFocusAndAlignCursor(createCharButton);

        jobButtons[buttonIndex].gameObj.GetComponent<Image>().color = Color.white;

        jobSelected = true;
        indexOfSelectedJob = buttonIndex;

        indexOfHoveringJob = buttonIndex; // new 1/13/18

        //enumOfSelectedJob = jobEnumOrder[value];
    }

    public static void DeselectJob()
    {
        if (jobSelected)
        {
            UIManagerScript.PlayCursorSound("Cancel");
        }        

        singleton.ResetJobButtonStates();
        createCharButton.gameObj.SetActive(false);
        jobSelected = false;
        UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[0]);
    }

    public void HoverSkillInfo(int indexOfSkill)
    {
        if (!creationActive) return;
        if (GameMasterScript.masterJobList == null || GameMasterScript.masterAbilityList == null)
        {
            Debug.Log("Job or ability list null.");
            return;
        }
        if (indexOfSkill == -1)
        {
            ccSkillHover.SetActive(false);
            return;
        }

        int jobEnumToUse = jobEnumOrder[indexOfHoveringJob]; // was index of selected...

        CharacterJobData cjd = CharacterJobData.GetJobDataByEnum(jobEnumToUse);

        int count = 0;
        JobAbility abilToDisp = null;
        foreach(JobAbility ja in cjd.JobAbilities)
        {
            if (ja.innate) continue;
            if (indexOfSkill == count)
            {
                abilToDisp = ja;
                break;
            }
            count++;
        }

        if (abilToDisp == null) return;

        string knownString = "";
        if (GameMasterScript.actualGameStarted)
        {
            if (GameMasterScript.heroPCActor.myAbilities.HasAbilityRef(abilToDisp.ability.refName))
            {
                knownString = " (" + StringManager.GetString("ui_skill_learned") + ") ";
            }
        }

        ccSkillHover.SetActive(true);
        ccSkillHoverText.text = "<color=yellow><size=56>" + abilToDisp.ability.abilityName + knownString + "</size></color>\n\n" + abilToDisp.ability.GetAbilityInformation();
        ccSkillHover.transform.position = new Vector3(jobSkillIcons[0].gameObj.transform.position.x - 40f, jobSkillIcons[0].gameObj.transform.position.y + 40f, jobSkillIcons[0].gameObj.transform.position.z);


    }
    
    public void Update()
    {
        //Is the cursor behind a text box? We should hide it.
        if (ccSkillHover != null && ccSkillHover.activeInHierarchy)
        {
            RectTransform rt = ccSkillHover.transform as RectTransform;
            if (rt != null)
            {
                Vector3[] vCorners = new Vector3[4];
                rt.GetWorldCorners(vCorners);
                if (!UIManagerScript.ChangeCursorOpacityIfInBounds(vCorners, 0f))
                {
                    UIManagerScript.singletonUIMS.ChangeCursorOpacity(1.0f);
                }
            }
        }
        else
        {
            UIManagerScript.singletonUIMS.ChangeCursorOpacity(1.0f);
        }
    }

    // Accepts INDEX value, not actual enum value
    public void SelectJobRefresh(int buttonIndex)
    {
        ccSkillHover.SetActive(false);
        ResetJobButtonStates();
        jobPrefabs[buttonIndex].GetComponent<Animatable>().SetAnim("Walk");
        UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[buttonIndex]);

        //int jobEnumToUse = jobEnumOrder[value];
        int enumOfSelectedJob = jobEnumOrder[buttonIndex];

        CharacterJobData cjd = CharacterJobData.GetJobDataByEnum(enumOfSelectedJob);
        string buildText = "";

        buildText = cjd.GetFullJobReadout("");

        jobDescText.text = buildText;

        int count = 0;
        foreach (JobAbility ja in cjd.JobAbilities)
        {
            if (ja.innate) continue;
            jobSkillIcons[count].gameObj.SetActive(true);
            jobSkillIcons[count].gameObj.GetComponent<Image>().sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictUIGraphics, ja.ability.iconSprite);
            jobSkillIcons[count].enabled = true;
            count++;
        }
    }

    IEnumerator WaitAndTryHoverJobInfo(int value)
    {
        yield return new WaitForSeconds(0.1f);
        HoverJobInfo(value);
    }

    public void SetMysteryJob()
    {
        jobDescText.text = StringManager.GetString("ui_job_locked");
        jobDescText.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < jobSkillIcons.Length; i++)
        {
            jobSkillIcons[i].gameObj.SetActive(false);
            jobSkillIcons[i].enabled = false;
        }

    }
    
    public void HoverJobInfo(int buttonIndex)
    {
        if (!creationActive)
        {
            return;
        }
        ccSkillHover.SetActive(false);
        if (jobSelected) return;

        int enumOfJobSelected = jobEnumOrder[buttonIndex];
        indexOfHoveringJob = buttonIndex;

        if (GameMasterScript.masterJobList == null || GameMasterScript.masterAbilityList == null)
        {
            StartCoroutine(WaitAndTryHoverJobInfo(enumOfJobSelected));
            return;
        }

        //Debug.Log("Button index " + buttonIndex + " Which is job enum " + (CharacterJobs)buttonIndex + " OR " + (CharacterJobs)jobEnumOrder[buttonIndex] + " OR " + (CharacterJobs)indexOfHoveringJob);

        if (!SharedBank.CheckIfJobIsUnlocked((CharacterJobs)enumOfJobSelected)) // is this value or converted value?
        {
            SetMysteryJob();
            UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[indexOfHoveringJob]);
            return;
        }

        jobDescText.alignment = TextAlignmentOptions.TopLeft;

        ResetJobButtonStates();
        jobPrefabs[buttonIndex].GetComponent<Animatable>().SetAnim("Walk");
        
        UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[indexOfHoveringJob]);

        CharacterJobData cjd = CharacterJobData.GetJobDataByEnum(enumOfJobSelected);

        string buildText = "";
        string extraText = "";

        if (GameMasterScript.actualGameStarted)
        {
            int unspentJP = (int)(GameMasterScript.heroPCActor.jobJP[(int)enumOfJobSelected]);
            if (unspentJP > 0)
            {
                extraText = " (" + StringManager.GetString("ui_unspent_jp") + ": " + unspentJP + ")";
            }
        }

        buildText = cjd.GetFullJobReadout(extraText);

        jobDescText.text = buildText;

        int count = 0;
        foreach(JobAbility ja in cjd.GetBaseJobAbilities())
        {
            if (ja.innate) continue;
            jobSkillIcons[count].gameObj.SetActive(true);
            
            AbilityScript jobAbil = ja.ability;
            jobSkillIcons[count].gameObj.GetComponent<Image>().sprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictUIGraphics, jobAbil.iconSprite);
            jobSkillIcons[count].enabled = true;
            count++;
        }

    }

    void ResetJobButtonStates()
    {
        for (int i = 0; i < jobButtons.Length; i++)
        {
            jobButtons[i].gameObj.GetComponent<Image>().color = UIManagerScript.transparentColor;
            jobPrefabs[i].GetComponent<Animatable>().SetAnim("Default");
            //jobPrefabs[i].GetComponent<Image>().sprite = UIManagerScript.transparentSprite;
        }

        for (int i = 0; i < jobSkillIcons.Length; i++)
        {
            jobSkillIcons[i].gameObj.GetComponent<Image>().sprite = UIManagerScript.transparentSprite;
            jobSkillIcons[i].gameObj.SetActive(false);
            jobSkillIcons[i].enabled = false;
        }
    }

    public void EndCharCreation()
    {
        creationActive = false;
        gameObject.SetActive(false);
        if (GameMasterScript.gmsSingleton.titleScreenGMS)
        {            
            UIManagerScript.PlayCursorSound("Equip Item"); // Job selected 
        }        
    }

    public CanvasGroup myCG;

    public void BeginCharCreation_JobSelection()
    {
        gameObject.SetActive(true);
        
        createCharButton.gameObj.SetActive(false);

        if (GameMasterScript.gmsSingleton.titleScreenGMS)
        {
            UIManagerScript.CloseDialogBox();            
        }
        else
        {
            GuideMode.ToggleSnackBagUIPulse(false);
            GuideMode.ToggleFlaskUIPulse(false);
        }

        GameStartData.CurrentLoadState = LoadStates.NORMAL;

        if (myCG == null) myCG = gameObject.GetComponent<CanvasGroup>();

        myCG.alpha = 1.0f;
        myCG.interactable = true;
        myCG.blocksRaycasts = true;
        //ResetJobButtonStates();

        UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(gameObject.transform);
        UIManagerScript.ShowDialogMenuCursor();
        UIManagerScript.singletonUIMS.EnableCursor();

        UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[0]);

        UIManagerScript.ClearUIObjects();

        for (int i = 0; i < jobButtons.Length; i++)
        {
            UIManagerScript.AddUIObject(jobButtons[i]);
            jobButtons[i].enabled = true;
        }

        for (int i = 0; i< jobSkillIcons.Length; i++)
        {
            UIManagerScript.AddUIObject(jobSkillIcons[i]);
            jobSkillIcons[i].enabled = true;
        }

        for (int i = 0; i < jobPrefabs.Length; i++)
        {
            Image img = jobPrefabs[i].GetComponent<Image>();

            if (!SharedBank.CheckIfJobIsUnlocked((CharacterJobs)jobEnumOrder[i]))
            {
                img.color = new Color(0f, 0f, 0f, 1f);                
            }
            else
            {
                img.color = Color.white;
            }
        }

        UIManagerScript.AddUIObject(createCharButton);
        createCharButton.enabled = true;

        creationActive = true;

        DeselectJob();

        HoverJobInfo(0);

        UIManagerScript.ChangeUIFocusAndAlignCursor(jobButtons[0]);
    }

    public static void CancelPressed()
    {
        if (jobSelected)
        {
            DeselectJob();
            return;
        }
        else if (GameMasterScript.actualGameStarted)
        {
            singleton.EndCharCreation();
            UIManagerScript.HideDialogMenuCursor();
        }
    }

    public void StartCharacterCreation4FromButton(int dummy)
    {
        StartCharacterCreation_FeatSelect();
    }

    /// <summary>
    /// Picks feats for the player
    /// </summary>
    public void StartCharacterCreation_FeatSelect()
    {
        string nameValue = SharaModeStuff.IsSharaModeActive() ? StringManager.GetString("npc_npc_shara_preboss3_name") : nameInputTextBox.text;

        // We might need portraits by now?
        UIManagerScript.singletonUIMS.TryLoadingAllPortraits();

        if (nameValue.Length == 0)
        {
            nameValue = randomNameList[UnityEngine.Random.Range(0, randomNameList.Count)];
        }

        if (nameValue.Length > UIManagerScript.PLAYERNAME_MAX_CHARACTERS) // Max characters in name
        {
            UIManagerScript.PlayCursorSound("Error");
            return;
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            TitleScreenScript.ReturnToMenu();
            return;
        }

        GameStartData.playerName = nameValue;

        if (TDCalendarEvents.IsAprilFoolsDay()) // A1
        {
            if (nameValue.Contains("Male"))
            {
                GameStartData.playerName = nameValue.Replace("Male", "(Male)");
                GameStartData.miscGameStartTags.Add("malemode");
            }
        }


        UIManagerScript.PlayCursorSound("Equip Item");

        UIManagerScript.ignoreNextButtonConfirm = false;

        nameInputParentCanvasGroup.gameObject.SetActive(false);
        UIManagerScript.nameInputOpen = false;

        // Above stuff was in the job area before.

        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
        Conversation charCreation = new Conversation();

        UIManagerScript.requireDoubleConfirm = false;

        if (GameMasterScript.gmsSingleton.ReadTempGameData("first_reset_feats") != 1)
        {
            GameMasterScript.gmsSingleton.SetTempGameData("first_reset_feats", 1);
            GameStartData.ClearPlayerFeats();
        }


        TitleScreenScript.CreateStage = CreationStages.PERKSELECT;

        TextBranch ccPage3 = new TextBranch();
        ccPage3.text = StringManager.GetString("ui_select_feats");

        // This branch requires some special formatting by the dialog box, so it must be named
        ccPage3.branchRefName = "player_select_feats";

        bool randomJobMode = GameMasterScript.gmsSingleton.ReadTempGameData("cc_randomjob") == 1;

        foreach (CreationFeat cf in GameMasterScript.masterFeatList)
        {
            //disable some feats based on gameplay mode
            if (SharaModeStuff.IsSharaModeActive() && cf.sharaModeStatus == 0)
            {
                continue;
            }

            if (randomJobMode && cf.randomJobModeDisallow)
            {
                continue;
            }

            //Some feats are Shara only
                if (!SharaModeStuff.IsSharaModeActive() && cf.sharaModeStatus == 2)
            {
                continue;
            }


            ButtonCombo bc = new ButtonCombo();

            bc.threeColumnStyle = true;
            bc.spriteRef = cf.spriteRef;

            if (cf.mustBeUnlocked && !SharedBank.IsFeatUnlocked(cf.skillRef))
            {
                bc.buttonText = UIManagerScript.silverHexColor + StringManager.GetString("ui_feat_locked") + "</color>";
                bc.dbr = DialogButtonResponse.NOTHING;
                bc.headerText = "?????";
            }
            else
            {
                bc.buttonText = cf.description;
                bc.headerText = "<#fffb00>" + cf.featName + "</color>";
                bc.buttonText = cf.description;
                bc.dbr = DialogButtonResponse.TOGGLE;
                bc.actionRef = cf.skillRef;

                if (GameStartData.HasFeat(bc.actionRef))
                {
                    bc.toggled = true;
                    // In JP, adding the asterisk was overflowing some lines. Switched to green highlight instead.
                    bc.buttonText = UIManagerScript.greenHexColor + bc.buttonText + "</color>";
                }
            }

            ccPage3.responses.Add(bc);
        }

        ButtonCombo randomFeats = new ButtonCombo();
        randomFeats.buttonText = StringManager.GetString("ui_feats_random");
        randomFeats.dbr = DialogButtonResponse.TOGGLE;
        randomFeats.actionRef = "randomfeats";
        ccPage3.responses.Add(randomFeats);

        ButtonCombo optional = new ButtonCombo();
        optional.buttonText = "[" + StringManager.GetString("ui_misc_optional_gamemods") + "</color>]";
        optional.dbr = DialogButtonResponse.GAMEMODIFIERSELECT; // was NewGame?
        ccPage3.responses.Add(optional);      

        ButtonCombo finished = new ButtonCombo();
        finished.buttonText = UIManagerScript.greenHexColor + StringManager.GetString("ui_misc_finished") + "</color>";
        finished.dbr = DialogButtonResponse.NEWGAME; // was NewGame?

        ccPage3.responses.Add(finished);


        float fSize = (StringManager.gameLanguage != EGameLanguage.jp_japan && StringManager.gameLanguage != EGameLanguage.zh_cn) ? 1280f : 1400f;
        Vector2 size = new Vector2(fSize, 50f);

        UIManagerScript.ToggleDialogBox(DialogType.CCFEATSELECT, true, true, size, Vector2.zero);

        UIManagerScript.currentConversation = charCreation;
        UIManagerScript.SwitchConversationBranch(ccPage3);

        UIManagerScript.OverrideDialogWidth(fSize);

        UIManagerScript.UpdateDialogBox();
    }

    /// <summary>
    /// Begins the selection of difficulty mods, and is optional

    public void StartCharacterCreation_SelectGameMods()
    {
        //UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("OPSelect");
        
        UIManagerScript.ToggleDialogBox(DialogType.EXIT, false, false);
        Conversation charCreation = new Conversation();

        UIManagerScript.requireDoubleConfirm = false;

        //GameStartData.ResetGameModifiers();

        TitleScreenScript.CreateStage = CreationStages.GAME_MODS;

        TextBranch gameModSelect = new TextBranch();
        gameModSelect.text = StringManager.GetString("ui_select_game_modifiers") + "\n";

        bool sharaMode = SharaModeStuff.IsSharaModeActive();

        List<GameModifiers> customSortedList = new List<GameModifiers>();
        customSortedList.Add(GameModifiers.PLAYER_REGEN);
        customSortedList.Add(GameModifiers.PLAYER_RESOURCEREGEN);
        customSortedList.Add(GameModifiers.FAST_FULLNESS);
        if (!sharaMode)
        {
            customSortedList.Add(GameModifiers.PETS_DONTDIE);

            if (GameMasterScript.gmsSingleton.ReadTempGameData("cc_randomjob") != 1)
            {
                customSortedList.Add(GameModifiers.JOB_SPECIALIST);
                customSortedList.Add(GameModifiers.FREE_JOBCHANGE);
            }
        }

        customSortedList.Add(GameModifiers.GOLDFROGS_ANYWHERE);
        
        customSortedList.Add(GameModifiers.MULTI_PANDORA);
        customSortedList.Add(GameModifiers.NO_PANDORA);
        customSortedList.Add(GameModifiers.FRIENDLY_FIRE);
        customSortedList.Add(GameModifiers.MONSTER_REGEN);
        customSortedList.Add(GameModifiers.JP_HALF);
        customSortedList.Add(GameModifiers.FEWER_POWERUPS);
        customSortedList.Add(GameModifiers.MONSTERS_MIN_1POWER);
        customSortedList.Add(GameModifiers.CONSUMABLE_COOLDOWN);        
        customSortedList.Add(GameModifiers.NO_GOLD_DROPS);

        if (!sharaMode && GameMasterScript.gmsSingleton.ReadTempGameData("cc_randomjob") != 1)
        {
            customSortedList.Add(GameModifiers.NO_JOBCHANGE);
        }
        
        foreach (GameModifiers gm in customSortedList)
        {
            int i = (int)gm;
            ButtonCombo bc = new ButtonCombo();

            if (gm != GameModifiers.JOB_SPECIALIST)
            {
                bc.buttonText = StringManager.GetString("gamemods_" + ((GameModifiers)i).ToString().ToLowerInvariant());
            }
            else
            {
                if (!PlatformVariables.CAN_USE_ABILITIES_REGARDLESS_OF_HOTBAR)
                {
                    bc.buttonText = StringManager.GetString("gamemods_job_specialist_good");
                }
                else
                {
                    bc.buttonText = StringManager.GetString("gamemods_job_specialist_bad");
                }
            }

            
            bc.dbr = DialogButtonResponse.TOGGLE;
            bc.actionRef = ((GameModifiers)i).ToString();            
            gameModSelect.responses.Add(bc);

            if (GameStartData.gameModifiers[i])
            {
                bc.toggled = true;
                bc.buttonText = "*" + bc.buttonText;
            }
        }

        ButtonCombo finished = new ButtonCombo();
        finished.buttonText = StringManager.GetString("ui_misc_finished");
        
        finished.dbr = DialogButtonResponse.CREATIONSTEP2;

        gameModSelect.responses.Add(finished);

        Vector2 size = new Vector2(1200f, 50f);
        UIManagerScript.ToggleDialogBox(DialogType.CCGAMEMODSELECT, true, true, size, Vector2.zero);

        UIManagerScript.currentConversation = charCreation;
        UIManagerScript.SwitchConversationBranch(gameModSelect);
        UIManagerScript.UpdateDialogBox();
        UIManagerScript.myDialogBoxComponent.GetDialogText().fontSize = 36;
    }


    public static string GetRandomHeroineName()
    {
        return randomNameList[UnityEngine.Random.Range(0, randomNameList.Count)];
    }

    public void GenerateRandomNameAndFillField(bool bDoButtonPulse = true)
    {
        string randomName = GetRandomHeroineName();
        nameInputTextBox.text = randomName;

        if (!bDoButtonPulse) return;

        //pulse the random button
        RectTransform rt = list_nameEntryCommands[1].rectTransform;
        rt.localScale = new Vector3(1.15f, 1.15f, 1.15f);
        LeanTween.scale(rt, Vector3.one, 0.2f ).setEaseInOutBounce();
        list_nameEntryCommands[1].StartCoroutine(UIGenericItemTooltip.LerpTextColor(list_nameEntryCommands[1], Color.yellow, Color.white, 0.2f));
        // UIManagerScript.singletonUIMS.nameInputParentObject.GetComponent<TMP_InputField>().text = randomName;
        UIManagerScript.PlayCursorSound("AltSelect");
    }

    public void HoverOverRandomName()
    {
        if (nameInputTextBox.isFocused) return;
        UIManagerScript.ChangeUIFocusAndAlignCursor(randomNameButton);
    }

    public void HoverOverConfirm()
    {
        if (nameInputTextBox.isFocused) return;
        UIManagerScript.ChangeUIFocusAndAlignCursor(titleScreenConfirmButton);
    }


    // Not currently used, but may be useful for something later.
    public string ValidateNameCharacters(string inputName)
    {
        string returnName = inputName;
        TMP_FontAsset tmFont = FontManager.GetFontAsset(TDFonts.WHITE);
        for (int i = 0; i < inputName.Length; i++)
        {
            char charToCheck = inputName[i];
            if (!tmFont.HasCharacter(charToCheck))
            {
                returnName = returnName.Replace(charToCheck, '?');
            }
        }

        return returnName;
    }

    /// <summary>
    /// Sets flags and job type to the correct values.
    /// </summary>
    public static void SetGameStartDataForSharaMode()
    {
        GameStartData.gameInSharaMode = true;
        GameStartData.playerJob = "shara";
        GameStartData.jobAsEnum = CharacterJobs.SHARA;
        DLCManager.SetLastPlayedCampaign(StoryCampaigns.SHARA);
    }
    /// <summary>
    /// Fill in all the required boxes, labels, and images
    /// With the character information so far.
    /// </summary>
    public static void PrepareNameEntryPage()
    {
        singleton.PrepareNameEntryPage_Internal();
    }

    private void PrepareNameEntryPage_Internal()
    {
        SetFontsAndSizesForAllText();

        nameInputIsActive = false;

        UIManagerScript.singletonUIMS.TryLoadingAllPortraits();

        UIManagerScript.singletonUIMS.characterCreationBG.SetActive(true);

        //make sure we know where we are
        NameEntryScreenState = ENameEntryScreenState.deciding_on_name;

        EnableControllerLabelsIfControllerActive();

        FontManager.LocalizeMe(list_nameEntryCommands[0], TDFonts.WHITE);
        FontManager.LocalizeMe(list_nameEntryCommands[1], TDFonts.WHITE);
        FontManager.LocalizeMe(list_nameEntryCommands[2], TDFonts.WHITE);

        //translato our name change instructions
        list_nameEntryCommands[0].text = StringManager.GetString("label_nameentry_change", true, false);
        list_nameEntryCommands[1].text = StringManager.GetString("label_nameentry_random", true, false);
        list_nameEntryCommands[2].text = StringManager.GetString("label_nameentry_confirm", true, false);

        //show our feets

        for (int t = 0; t < img_feats.Count; t++)
        {
            //clean them out in case there is no feat here (monoperk disorder)
            img_feats[t].enabled = false;
            label_feats[t].text = "";

            //which two feats did the player select?
            if (GameStartData.playerFeats != null && GameStartData.playerFeats.Count > t)
            {
                //look at them all, and pick the very best one (that matches)
                foreach (CreationFeat cf in GameMasterScript.masterFeatList)
                {
                    if (cf.skillRef == GameStartData.playerFeats[t])
                    {
                        img_feats[t].enabled = true;
                        img_feats[t].sprite = UIManagerScript.LoadSpriteFromAtlas(UIManagerScript.allUIGraphics, cf.spriteRef);
                        label_feats[t].text = cf.featName;
                    }
                }
            }
        }

        //job name?
        if (!RandomJobMode.IsCurrentGameInRandomJobMode() && !RandomJobMode.preparingEntryForRandomJobMode)
        {
            label_job_name.text = CharacterJobData.GetJobDataByEnum((int)GameStartData.jobAsEnum).DisplayName;
        }
        else
        {
            label_job_name.text = StringManager.GetString("job_wanderer");
        }
        

        //game difficulty
        string strMode = "";
        switch (GameStartData.GetGameMode())
        {
            case GameModes.NORMAL:
                strMode = StringManager.GetLocalizedStringInCurrentLanguage("modename_heroic");
                break;
            case GameModes.ADVENTURE:
                strMode = StringManager.GetLocalizedStringInCurrentLanguage("modename_adventure");
                break;
            case GameModes.HARDCORE:
                strMode = StringManager.GetLocalizedStringInCurrentLanguage("modename_hardcore");
                break;
            case GameModes.COUNT:
                strMode = "no mode, whoops";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (RandomJobMode.IsCurrentGameInRandomJobMode() || RandomJobMode.preparingEntryForRandomJobMode) 
        {
            strMode = StringManager.GetString("randomjob_mode");
        }

        label_difficulty.text = strMode;

        //title
        label_title.text = StringManager.GetLocalizedStringInCurrentLanguage("label_nameentry_entername");

        FontManager.LocalizeMe(label_begin_game, TDFonts.BLACK);
        FontManager.LocalizeMe(label_go_back, TDFonts.BLACK);

        //hide the let's go / go back buttons for now
        label_begin_game.text = "";
        label_go_back.text = "";

        beginGameButton.SetActive(false);
        goBackButton.SetActive(false);

        EnableClickableButtonsForKeyboardAndMouse();

        //and the world seedery
        label_worldSeedTitle.text = "";
        worldSeedInput.gameObject.SetActive(false);

        CharacterJobData cjd = CharacterJobData.GetJobDataByEnum((int)GameStartData.jobAsEnum);

        if (cjd == null) Debug.Log("Job data for " + GameStartData.jobAsEnum + " not found?");

        Sprite[] possiblePortraits = UIManagerScript.GetPortraitForDialog(cjd.portraitSpriteRef);

        if (possiblePortraits == null) Debug.Log("Possible portraits null?");
        if (img_portrait == null) Debug.Log("Null portrait...?");

        img_portrait.sprite = possiblePortraits[0];

        //our running champion
        CharacterJobs jobEnum = GameStartData.jobAsEnum;            
            
        var prefab = LoadingWaiterManager.GetPrefabForJob(jobEnum);
        
        bool anyPrefab = prefab != null;

        if (Debug.isDebugBuild)
        {
            if (!anyPrefab)
            {
                Debug.Log("4 Null prefab.");
                prefab = LoadingWaiterManager.GetPrefabForJob(CharacterJobs.BRIGAND);
            }
        }

        if (anyPrefab)
        {
            // really?
            foreach(zirconAnim anim in prefab.GetComponent<Animatable>().myAnimations)
            {
                if (anim.animName.ToLowerInvariant() == "walk")
                {
                    foreach(zirconAnim.AnimationFrameData afd in anim.mySprites)
                    {
                        if (afd.mySprite == null) Debug.Log("Sprite is null for original animation.");
                        //else Debug.Log("Sprite is NOT null for original animation.");
                    }
                }
            }


            anim_SelectedHeroineComponent = GameMasterScript.CopyComponent(prefab.GetComponent<Animatable>(), anim_SelectedHeroineComponent.gameObject) as Animatable;

            //some magic to help the transplant along
            anim_SelectedHeroineComponent.IsUIElement = true;
            anim_SelectedHeroineComponent.opacityMod = 1;
            anim_SelectedHeroineComponent.SearchForSpriteRenderer(true);
            anim_SelectedHeroineComponent.SetAnim("Walk");

            Sprite firstFrame = anim_SelectedHeroineComponent.animPlaying.mySprites[0].mySprite;

            //ensure the size of the render image is correct
            RectTransform myRT = anim_SelectedHeroineComponent.transform as RectTransform;
            myRT.sizeDelta = new Vector2(firstFrame.rect.width * 4, firstFrame.rect.height * 4);

            //best
            Vector2 vBest = myRT.anchoredPosition;
            vBest.y = GameStartData.jobAsEnum == CharacterJobs.BUDOKA ? -70 : 0;
            myRT.anchoredPosition = vBest;
        }       
    }

    IEnumerator Coroutine_NameSelectHeroAnim()
    {
        while (true)
        {
            string strAddendum = "";
            float froll = UnityEngine.Random.value;
            float fWaitVal = 0f;
            bool bFlipALip = false;
            if (froll < 0.25f)
            {
                strAddendum = "Side";

            }
            else if( froll < 0.5f )
            {
                bFlipALip = true;
                strAddendum = "Side";
            }
            // else face down like normal, no flip.

            anim_SelectedHeroineComponent.StopAnimation();

            //pick the anim type
            if (UnityEngine.Random.value < 0.7f)
            {
                anim_SelectedHeroineComponent.SetAnim("Walk" + strAddendum);
                fWaitVal = 0.3f;
            }
            else
            {
                anim_SelectedHeroineComponent.SetAnim("Attack" + strAddendum);
                fWaitVal = 0.3f;
            }

            //it is not an attack animation.
            //it is not.
            anim_SelectedHeroineComponent.animPlaying.isAttackAnimation = false;
            anim_SelectedHeroineComponent.spriteTimer = 0f;
            anim_SelectedHeroineComponent.timeAtFrameStart = 0f;
            anim_SelectedHeroineComponent.animPlaying.ignoreScale = true;
            anim_SelectedHeroineComponent.animPlaying.startScale = 1.0f;
            anim_SelectedHeroineComponent.FlipSpriteXFromDirectionalAnim(bFlipALip);


            //ensure the size of the render image is correct
            Sprite firstFrame = anim_SelectedHeroineComponent.animPlaying.mySprites[0].mySprite;
            RectTransform myRT = anim_SelectedHeroineComponent.transform as RectTransform;
            myRT.sizeDelta = new Vector2(firstFrame.rect.width * 4, firstFrame.rect.height * 4);
            

            yield return new WaitForSeconds(fWaitVal);

        }
    }

    /// <summary>
    /// The player has entered and pressed confim in the name box.
    /// We have a name, this is all we need, present the final countdown.
    /// </summary>
    public void OnNameEntryBoxConfirm()
    {
        //Debug.Log("Now3: nameinput. " + nameInputTextBox.isFocused + " " + nameInputTextBox.isActiveAndEnabled + " " + nameInputTextBox.IsInteractable() + " " + (EventSystem.current.currentSelectedGameObject == nameInputTextBox));

        label_title.text = StringManager.GetLocalizedStringInCurrentLanguage("label_nameentry_R_U_READY");

        beginGameButton.SetActive(true);
        goBackButton.SetActive(true);

        //show these buttons, they will be our new faux dialog entries
        label_begin_game.text = StringManager.GetLocalizedStringInCurrentLanguage("label_nameentry_ready_confirm");
        label_go_back.text = StringManager.GetLocalizedStringInCurrentLanguage("label_nameentry_not_so_much");

        titleScreenConfirmButton.gameObj.SetActive(false);
        randomNameButton.gameObj.SetActive(false);

        //here's these
        label_worldSeedTitle.text = StringManager.GetLocalizedStringInCurrentLanguage("ui_text_worldseed");
        worldSeedInput.gameObject.SetActive(true);

        //hide the controller stuff
        foreach (var l in list_nameEntryCommands)
        {
            l.enabled = false;
        }            

        //Point at the Ready to Go! Text
        iSelectedConfirmCharacterOption = 0;

        //if the text box is empty, put a random name in
        if (string.IsNullOrEmpty(nameInputTextBox.text))
        {
            GenerateRandomNameAndFillField(false);
        }

        //But don't highlight the box any longer, looks weird.
        nameInputTextBox.OnDeselect(null);

        //Debug.Log("Now4: nameinput. " + nameInputTextBox.isFocused + " " + nameInputTextBox.isActiveAndEnabled + " " + nameInputTextBox.IsInteractable() + " " + (EventSystem.current.currentSelectedGameObject== nameInputTextBox));

        //change our state here
        NameEntryScreenState = ENameEntryScreenState.name_confirmed_and_ready_to_go;

        //sound!
        UIManagerScript.PlayCursorSound("OPSelect");

        //if (Debug.isDebugBuild) Debug.Log("OnNameEntryBoxConfirm. " + NameEntryScreenState + " and " + nameInputIsActive);
    }

    static bool nameInputIsActive = false;

    /// <summary>
    /// Checks for confirm, random, and change. And nothing else.
    /// </summary>
    public static void HandleInputNameEntry_DecidingOnName()
    {        
        var rwplayer = TitleScreenScript.titleScreenSingleton.player;

        // But this button is just the A button, which means we will go to 
        // the name change keyboard, and not accept the name as is. 
        if (rwplayer.GetButtonDown("Confirm"))
        {
            //if (Debug.isDebugBuild) Debug.Log("Confirmed with Confirm while handling name input entry while deciding on name. State? " + nameEntryScreenState + " Name input active? " + nameInputIsActive);

            // This should probably never happen now
            //if (PlatformVariables.GAMEPAD_ONLY && nameInputIsActive) // If we were already typing there, stop
            if (nameInputIsActive) // If we were already typing there, stop
            {
                singleton.OnNameEntryBoxConfirm();
                nameInputIsActive = false;
                //if (Debug.isDebugBuild) Debug.Log("Name input deactivated.");
                return;
            }

            nameInputIsActive = true;

            //highlight this box so the controller can interact with it.
            nameInputTextBox.OnSelect(null);

            //EventSystem.current.SetSelectedGameObject(nameInputTextBox.gameObject);
            return;
        }

        // Just shuffalo a new name in, see what happens.
        if (rwplayer.GetButtonDown("Jump to Hotbar"))
        {
            singleton.GenerateRandomNameAndFillField();
            return;
        }

        // Options (the + key) is also Start on this weird crazy
        // lovable switch. 
        if (rwplayer.GetButtonDown("Toggle Menu Select")) 
        {
            if (Debug.isDebugBuild) Debug.Log("Confirmed with Start while handling name input entry while deciding on name. State? " + nameEntryScreenState + " Name input active? " + nameInputIsActive);
            singleton.OnNameEntryBoxConfirm();
            return;
        }
    }

    public static void HandleInputNameEntry_ConfirmedAndReady(Directions dir)
    {
        singleton.HandleInputNameEntry_ConfirmedAndReady_Internal(dir);
    }

    void HandleInputNameEntry_ConfirmedAndReady_Internal(Directions dir)
    {
        var rwplayer = TitleScreenScript.titleScreenSingleton.player;

        // check for up, down
        if (dir != Directions.NEUTRAL)
        {
            int oldidx = iSelectedConfirmCharacterOption;
            switch (dir)
            {
                case Directions.NORTH:
                    iSelectedConfirmCharacterOption--;
                    break;
                case Directions.SOUTH:
                    iSelectedConfirmCharacterOption++;
                    break;
            }

            iSelectedConfirmCharacterOption = Mathf.Clamp(iSelectedConfirmCharacterOption, 0, 2);
            UIManagerScript.PlayCursorSound(iSelectedConfirmCharacterOption == oldidx ? "UITock" : "Move");
            
            worldSeedInput.DeactivateInputField();
            UIManagerScript.charCreationInputFieldActivated = false;
            nameInputTextBox.DeactivateInputField();
        }
        //if the player presses + right after pressing + to confirm the name, we should just go.
        else if (rwplayer.GetButtonDown("Toggle Menu Select") && iSelectedConfirmCharacterOption == 0)
        {
            //launch game!
            ConfirmedAndGameIsReadyToStart();
        }
        //otherwise, use confirm to execute one of the three options
        else if (rwplayer.GetButtonDown("Confirm"))
        {
            switch (iSelectedConfirmCharacterOption)
            {
                case 0:
                    //launch game!
                    ConfirmedAndGameIsReadyToStart();
                    break;
                case 1:
                    //start over completely
                    TitleScreenScript.ReturnToMenu();
                    break;
                case 2:
                    //focus on the seed selector
                    EventSystem.current.SetSelectedGameObject(worldSeedInput.gameObject);
                    worldSeedInput.ActivateInputField();
                    break;
            }
        }
    }

    public void ConfirmedAndGameIsReadyToStart()
    {
        if (!TitleScreenScript.bReadyForMainMenuDialog) return;
        
        if (TitleScreenScript.CreateStage != CreationStages.NAMEINPUT && TitleScreenScript.CreateStage != CreationStages.JOBSELECT && 
        TitleScreenScript.CreateStage != CreationStages.PERKSELECT && TitleScreenScript.CreateStage != CreationStages.GAME_MODS) return;

        NameEntryScreenState = ENameEntryScreenState.game_loading_stop_updating;

        //confirmalize a name
        string nameValue = nameInputTextBox.text;
        if (string.IsNullOrEmpty(nameValue))
        {
            nameValue = randomNameList[UnityEngine.Random.Range(0, randomNameList.Count)];
        }
        if (nameValue.Length > UIManagerScript.PLAYERNAME_MAX_CHARACTERS) // Max characters in name
        {
            nameValue = nameValue.Substring(0, UIManagerScript.PLAYERNAME_MAX_CHARACTERS - 1);
        }

        //DECIDED LIKE THIS
        GameStartData.playerName = nameValue;

        //Mannify our NPCs if it is april fools and "Male" is entered.
        if (TDCalendarEvents.IsAprilFoolsDay()) // A1
        {
            if (nameValue.Contains("Male"))
            {
                GameStartData.playerName = nameValue.Replace("Male", "(Male)");
                GameStartData.miscGameStartTags.Add("malemode");
            }
        }

        //this seems important
        SetWorldSeed();

        GameStartData.newGame = true;

        MetaProgressScript.loadedGameVersion = GameMasterScript.GAME_BUILD_VERSION;
        GameStartData.loadGameVer = GameMasterScript.GAME_BUILD_VERSION;

        Debug.Log("Set game version to " + GameStartData.loadGameVer);

        //here we go!
        UIManagerScript.PlayCursorSound("OPSelect");
        UIManagerScript.singletonUIMS.StartCoroutine(UIManagerScript.singletonUIMS.FadeOutThenLoadGame());

        //hide the cursor
        UIManagerScript.HideDialogMenuCursor();

        //stop the animation if we are running it
        //if (GameStartData.jobAsEnum != CharacterJobs.BUDOKA && GameStartData.jobAsEnum != CharacterJobs.SOULKEEPER)
        //{
        //    anim_SelectedHeroineComponent.StopCoroutine(Coroutine_NameSelectHeroAnim());
        //}

        //hooray!
        //anim_SelectedHeroineComponent.SetAnim("UseItem");
        //Sprite firstFrame = anim_SelectedHeroineComponent.animPlaying.mySprites[0].mySprite;

        //ensure the size of the render image is correct
        //RectTransform myRT = anim_SelectedHeroineComponent.transform as RectTransform;
        //myRT.sizeDelta = new Vector2(firstFrame.rect.width * 4, firstFrame.rect.height * 4);

    }

    public void UpdateCursorForNameSelect()
    {
        if (NameEntryScreenState != ENameEntryScreenState.name_confirmed_and_ready_to_go)
        {
            return;
        }

        TextMeshProUGUI tmp = null;
        switch (iSelectedConfirmCharacterOption)
        {
            case 0:
                tmp = label_begin_game;
                break;
            case 1:
                tmp = label_go_back;
                break;
            case 2:
                tmp = label_worldSeedTitle;
                break;
        }

        UIManagerScript.singletonUIMS.EnableCursor(true);
        GameObject pointAtMe = tmp.gameObject;
        var rt = tmp.transform as RectTransform;

        //this is a negative number because the text is centered, and so the first char X
        //is left of center.
        float fCharacterDelta = tmp.textInfo.characterInfo[0].bottomLeft.x;
        float fBoxWidth = ((RectTransform) tmp.transform).rect.width;
        //cursor starts at 0, on the left side
        //move it in by half the full size of the object
        //and then back to the left.

        float fXOffset = fBoxWidth / 2 + fCharacterDelta;

        //eyeball it
        fXOffset -= 8f;
        float fYOffset = -8f;

        //scale the value based on our resolution
        fXOffset *= rt.lossyScale.x;

        UIManagerScript.AlignCursorPos(pointAtMe, fXOffset, fYOffset, false);

    }	

    void EnableClickableButtonsForKeyboardAndMouse()
    {
        if (PlatformVariables.GAMEPAD_ONLY)
        {
            titleScreenConfirmButton.gameObj.SetActive(false);
            randomNameButton.gameObj.SetActive(false);
            UIManagerScript.HideDialogMenuCursor();
            return;
        }

        bool lastControllerJoystick = ReInput.controllers.GetLastActiveControllerType() == ControllerType.Joystick;

        // enable clickable / key navigable buttons IF we are not using controller.

        titleScreenConfirmButton.gameObj.SetActive(!lastControllerJoystick);
        randomNameButton.gameObj.SetActive(!lastControllerJoystick);

        if (lastControllerJoystick)
        {
            UIManagerScript.HideDialogMenuCursor();
        }
    }

    public void GoBackToMainMenu()
    {
        if (!TitleScreenScript.bReadyForMainMenuDialog) return;
        TitleScreenScript.ReturnToMenu();
    }

    void EnableControllerLabelsIfControllerActive()
    {
        //hide the controller stuff
        foreach (var l in list_nameEntryCommands)
        {
            if (PlatformVariables.GAMEPAD_ONLY)
            {
                l.enabled = true;
            }
            else
            {
                // Enable buttons like "(+) Confirm" only if we have a joystick attached.
                l.enabled = ReInput.controllers.GetLastActiveControllerType() == ControllerType.Joystick;
            }
        }
    }

    
}

