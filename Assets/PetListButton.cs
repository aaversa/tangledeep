using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PetListButton : MonoBehaviour {

    public int indexOfButtonInList;
    public Image parentImage;
    public Image childImage;
    public TextMeshProUGUI childButtonText;

	// Use this for initialization
	void Start () {
		
	}
	
	public void PopulateButtonContents(Monster petMon)
    {        
        if (petMon == null)
        {
            Debug.Log("Pet button " + indexOfButtonInList + " cannot populate null monster obj.");
            return;
        }

        string monsterText = "<color=yellow>" + petMon.displayName + "</color>, " + StringManager.GetString("misc_xp_level") + " " + petMon.myStats.GetLevel() + " " + petMon.myTemplate.monsterName + "\n";
    }
}
