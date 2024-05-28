using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    static void AssignAttributeAndElementStrings()
    {
        monsterAttributeNames = new string[(int)MonsterAttributes.COUNT];
        monsterAttributeNames[0] = "GREEDY";
        monsterAttributeNames[1] = "TIMID";
        monsterAttributeNames[2] = "BERSERKER";
        monsterAttributeNames[3] = "SNIPER";
        monsterAttributeNames[4] = "LOVESBATTLES";
        monsterAttributeNames[5] = "STALKER";
        monsterAttributeNames[6] = "GANGSUP";
        monsterAttributeNames[7] = "PREDATOR";
        monsterAttributeNames[8] = "HEALER";
        monsterAttributeNames[9] = "COMBINABLE";
        monsterAttributeNames[10] = "RONIN";

        elementNames = new string[(int)DamageTypes.COUNT];
        elementNames[(int)DamageTypes.POISON] = StringManager.GetString("misc_dmg_poison");
        elementNames[(int)DamageTypes.PHYSICAL] = StringManager.GetString("misc_dmg_physical");
        elementNames[(int)DamageTypes.LIGHTNING] = StringManager.GetString("misc_dmg_lightning");
        elementNames[(int)DamageTypes.FIRE] = StringManager.GetString("misc_dmg_fire");
        elementNames[(int)DamageTypes.WATER] = StringManager.GetString("misc_dmg_water");
        elementNames[(int)DamageTypes.SHADOW] = StringManager.GetString("misc_dmg_shadow");
    }

}
