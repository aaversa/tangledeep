using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public partial class EQItemButtonScript : MonoBehaviour {

    Button myButton;
    public int myID;
    private UIManagerScript uims;
    EventTrigger myTrigger;
    public bool invertDefaultFontColor; // If inverted, the text is black
    public bool dontSetColorOnStart;

    // Use this for initialization
    public virtual void Start () {
        myButton = GetComponent<UnityEngine.UI.Button>();
        //myButton.onClick.AddListener(delegate () { ButtonClicked(); });
        uims = GameObject.Find("UIManager").GetComponent<UIManagerScript>();
        myButton = GetComponent<UnityEngine.UI.Button>();
        if (myButton == null) {
        	//Debug.Log(gameObject.name + " has no button.");
        }
        else {
			//myButton.onClick.AddListener(delegate () { ButtonClicked(); });
        }
        myTrigger = GetComponent<EventTrigger>();
        if (myTrigger == null) {
        Debug.Log(gameObject.name + " has no event trigger.");
        	return;
        }
        UnityEvent myPointerEnter = new UnityEvent();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { ChangeUIFocus(); });
        myTrigger.triggers.Add(entry);
        TextMeshProUGUI[] tmPros = GetComponentsInChildren<TextMeshProUGUI>();

        if (dontSetColorOnStart)
        {
            return;
        }

        for (int i = 0; i < tmPros.Length; i++)
        {
            if (invertDefaultFontColor)
            {
                FontManager.LocalizeMe(tmPros[i], TDFonts.BLACK);
            }
            else
            {
                FontManager.LocalizeMe(tmPros[i], TDFonts.WHITE);
            }   
        }
    }


	public void ChangeUIFocus()
    {
        // Quantity dialog must prevent other stuff from firing.
        if (ShopUIScript.CheckShopInterfaceState() && GameMasterScript.gmsSingleton.ReadTempGameData("dropitem") >= 0)
        {
            return;
        }

        //If the tooltip hasn't locked the cursor down, OR we're both parented by the same object,
        if (UIManagerScript.AllowCursorToFocusOnThis(gameObject))
        {
            UIManagerScript.FocusCursorViaMouse(gameObject);
        }
    }

    public void ButtonClicked()
    {
        //UIManagerScript.SetEQItemListPosition(myID);
        //uims.TryUseEQItem(myID);
    }

    void Destroy()
    {
        myButton.onClick.RemoveAllListeners();
    }

}

