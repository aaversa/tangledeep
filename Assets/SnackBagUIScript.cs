using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SnackBagUIScript : MonoBehaviour {

    public static SnackBagUIScript singleton;

    public void OpenSnackBagUIFromButton(int dummy)
    {
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;

        UIManagerScript.singletonUIMS.CloseHotbarNavigating();
        OpenSnackBagUI();
    }

    public void OpenSnackBagUI()
    {
        if (GameMasterScript.IsGameInCutsceneOrDialog()) return;

        //Debug.Log("Sort snack bag inventory.");
        GameMasterScript.heroPCActor.myInventory.SortMyInventory(InventorySortTypes.CONSUMABLETYPE, true, false);

        UIManagerScript.OpenSnackBagFullScreenUI();
    }

	// Use this for initialization
	void Start () {
        singleton = this;
	}
	
}
