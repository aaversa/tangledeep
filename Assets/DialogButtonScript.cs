using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using TMPro;

public enum DialogButtonResponse { EXIT, NOTIPS, RESTARTGAME, LOADGAME, BRIGAND, FLORAMANCER, HUSYN, SOULKEEPER, SWORDDANCER, SPELLSHAPER, PALADIN, BUDOKA, HUNTER, EDGETHANE, GAMBLER,
NEWGAME, CONTINUE, CREATIONSTEP2, TOGGLE, NOTHING, SHOPBUY, SHOPSELL, CASINOSLOTS, CASINOBLACKJACK, BACKTOTITLE, NEWQUEST, FLOOR1, PASSAGE1, PASSAGE2, FLOOR17,
    HEALME, CHANGEJOBS, BLESSATTACK, BLESSDEFENSE, BLESSXP, RELEASEMONSTER, BLESSJP, NO, YES, SETUP_BOTHHANDS, SETUP_WASD, WARPTOSTAIRS, MANAGEDATA, NEWGAMEPLUS, BACKTODUNGEON,
    BACKTOITEMDREAM, VIEWMONSTER,  REBUILDMAPS, WILDCHILD, CASINOCEELO, COMMUNITY, COMMUNITY_OPEN_DISCORD, COMMUNITY_REJECT, GAMEMODIFIERSELECT, DAILY_LEADERBOARD, WEEKLY_LEADERBOARD,
    DAILY_LEADERBOARD_FRIENDS, WEEKLY_LEADERBOARD_FRIENDS, COUNT }

public enum TDLayoutType { NORMAL, INVISIBLE, COUNT }

[System.Serializable]
public class DialogButtonScript : MonoBehaviour {

    Button myButton;
    EventTrigger myTrigger;
    public HorizontalLayoutGroup myLayoutGroup;
    public int myID;
    public DialogButtonResponse myResponse;
    public Image iconSprite;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI headerText;

    public TDLayoutType buttonLayoutType;

    public bool parented;

    public void UpdateLayoutElement(bool isSaveGameOrManageDataDialog)
    {
        /* Debug.Log(buttonLayoutType + " " + isSaveGameOrManageDataDialog);
        if (!isSaveGameOrManageDataDialog && buttonLayoutType != TDLayoutType.NORMAL)
        {
            buttonLayoutType = TDLayoutType.NORMAL;
            gameObject.GetComponent<LayoutElement>().minHeight = 40f;
        } 
        else if (isSaveGameOrManageDataDialog && buttonLayoutType == TDLayoutType.NORMAL)
        {
            buttonLayoutType = TDLayoutType.INVISIBLE;
            gameObject.GetComponent<LayoutElement>().minHeight = 0f;
        } */
        
    }

    void Awake ()
    {
        if (myLayoutGroup == null)
        {
            myLayoutGroup = GetComponent<HorizontalLayoutGroup>(); // needed for padding adjustment
        }        
    }

    public void InitializeButton()
    {
        if (myLayoutGroup == null)
        {
            myLayoutGroup = GetComponent<HorizontalLayoutGroup>(); // needed for padding adjustment
        }
    }

    // Use this for initialization
    void Start () {
        myButton = GetComponent<UnityEngine.UI.Button>();
        //myButton.onClick.AddListener(delegate () { ButtonClicked(); });
        myTrigger = GetComponent<EventTrigger>();
        if (myTrigger == null) {
        	return;
        }        

        if (bodyText != null)
        {
            FontManager.LocalizeMe(bodyText, TDFonts.WHITE);
        }
        if (headerText != null)
        {
            FontManager.LocalizeMe(headerText, TDFonts.WHITE);
        }

        UnityEvent myPointerEnter = new UnityEvent();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { ChangeUIFocus(); });
        myTrigger.triggers.Add(entry);
    }

	public void ChangeUIFocus()
    {
    	UIManagerScript.FocusCursorViaMouse(gameObject);
    }

    public void ButtonClicked()
    {
        if (!TitleScreenScript.bReadyForMainMenuDialog) return;

#if UNITY_EDITOR
        UIManagerScript.DialogCursorConfirm();
#else
        try { UIManagerScript.DialogCursorConfirm(); }
        catch(Exception e)
        {
            Debug.Log("Button error: " + e);
        }
#endif
    }

    void Destroy()
    {
        myButton.onClick.RemoveAllListeners();
    }
}
