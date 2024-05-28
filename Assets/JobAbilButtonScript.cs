using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JobAbilButtonScript : MonoBehaviour {

    Button myButton;
    public int myID;
    public bool knownSkillList;

    // Use this for initialization
    void Start()
    {
        myButton = GetComponent<UnityEngine.UI.Button>();
        //myButton.onClick.AddListener(delegate () { ButtonClicked(); });
    }

    public void ButtonClicked()
    {
    	if (!knownSkillList) {
			UIManagerScript.SetJobAbilityListPosition(myID);
       		//UIManagerScript.singletonUIMS.TryLearnJobAbility(myID); // Does this need to pass my ID?
    	}
    	else {
			UIManagerScript.SetJobAbilityListPosition(myID);
			UIManagerScript.DialogCursorConfirm();
    	}
    }

    void Destroy()
    {
        myButton.onClick.RemoveAllListeners();
    }


}
