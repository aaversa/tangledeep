using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TDExpansion : MonoBehaviour {

    public static bool IsExpansionActive(int expNumber)
    {
        return false;
    }

    public static void VerifyExpansionStuffOnLoad()
    {
        if (!IsExpansionActive(0)) return;
        /* if (GameMasterScript.heroPCActor.ReadActorData("boss4fight_phase2") >= 2 || // Beat final boss on this character.
            GameMasterScript.gmsSingleton.statsAndAchievements.stat_boss4defeated >= 1 || // Have the achievement
            MetaProgressScript.ReadMetaProgress("boss4fight_phase2") >= 2)  
        {
            if (!GameMasterScript.heroPCActor.myInventory.HasItemByRef("item_sharaorb"))
            {
                Item orb = LootGeneratorScript.CreateItemFromTemplateRef("item_sharaorb", 1.0f, 0f, false);
                GameMasterScript.heroPCActor.myInventory.AddItemRemoveFromPrevCollection(orb, false);
                StringManager.SetTag(0, orb.displayName);
                StringManager.SetTag(1, orb.displayName);
                GameLogScript.LogWriteStringRef("log_corral_pickup");
            }
        } */
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
