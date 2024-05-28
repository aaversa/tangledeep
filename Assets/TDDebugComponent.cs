using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TDDebugComponent : MonoBehaviour {


    [Header("Hotbar")]
    public string[] weapons;
    public int activeSlot;

    [Header("GearEquipped")]
    public string currentWeapon;
    public string currentOffhand;

    bool exitImmediately;


    // Use this for initialization
    void Start () {
        weapons = new string[4];

#if !UNITY_EDITOR
        exitImmediately = true;
#endif
    }
	
	// Update is called once per frame
	void Update () {
        if (exitImmediately) return;

        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (!GameMasterScript.gmsSingleton.equipmentDebug) return;
        for (int i = 0; i < UIManagerScript.hotbarWeapons.Length; i++)
        {
            if (UIManagerScript.hotbarWeapons[i] == null || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(UIManagerScript.hotbarWeapons[i], onlyActualFists: true))
            {
                weapons[i] = "Fists";
            }
            else
            {
                string textToDisp = UIManagerScript.hotbarWeapons[i].actorRefName + " (" + UIManagerScript.hotbarWeapons[i].actorUniqueID + ")";
                weapons[i] = textToDisp;
            }
        }
        activeSlot = UIManagerScript.GetActiveWeaponSlot();

        if (GameMasterScript.heroPCActor.myEquipment.GetWeapon() == null 
            || GameMasterScript.heroPCActor.myEquipment.IsDefaultWeapon(GameMasterScript.heroPCActor.myEquipment.GetWeapon()))
        {
            currentWeapon = "Fists";
        }
        else
        {
            string textToDisp = GameMasterScript.heroPCActor.myEquipment.GetWeapon().actorRefName + " (" + GameMasterScript.heroPCActor.myEquipment.GetWeapon().actorUniqueID + ")";
            currentWeapon = textToDisp;
        }
        if (GameMasterScript.heroPCActor.myEquipment.GetOffhand() == null)
        {
            currentOffhand = "None";
        }
        else
        {
            string textToDisp = GameMasterScript.heroPCActor.myEquipment.GetOffhand().actorRefName + " (" + GameMasterScript.heroPCActor.myEquipment.GetOffhand().actorUniqueID + ")";
            currentOffhand = textToDisp;
        }

    }
}
