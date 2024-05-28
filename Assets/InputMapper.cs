using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum InputControls { CONFIRM, CANCEL, WAIT, FORCEDIAGONAL, NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST, INVENTORYSHEET, CHARACTERSHEET, EQUIPMENTSHEET, JOBSHEET, RUMORSHEET, HELP,
    OPTIONSMENU, SKILLSHEET, FIRERANGED, WEAPON1, WEAPON2, WEAPON3, WEAPON4, CYCLEWEAPONRIGHT, CYCLEWEAPONLEFT, HOTBAR1, HOTBAR2, HOTBAR3, HOTBAR4, HOTBAR5, HOTBAR6, HOTBAR7, HOTBAR8, MINIMAP,
    HEALINGFLASK, TOWNPORTAL, ROTATEABILITY, DROPITEM, BUILDPLANKS, COUNT }

public class TDControl
{
    public InputControls theControl;
    public KeyCode keyMap1;
    public KeyCode keyMap2;

    public TDControl(InputControls ic)
    {
        theControl = ic;
        keyMap1 = KeyCode.None;
        keyMap2 = KeyCode.None;
    }
}

public class InputMapper : MonoBehaviour {

    public static string[] controlNames;
    public static Dictionary<InputControls, TDControl> dictAllInputMaps;

	// Use this for initialization
	void Start () {        
        controlNames = new string[(int)InputControls.COUNT];
        controlNames[(int)InputControls.CONFIRM] = "Confirm";
        controlNames[(int)InputControls.CANCEL] = "Cancel";
        controlNames[(int)InputControls.WAIT] = "Wait Turn";
        controlNames[(int)InputControls.NORTH] = "Move Up";
        controlNames[(int)InputControls.NORTHEAST] = "Move Up+Right";
        controlNames[(int)InputControls.EAST] = "Move Right";
        controlNames[(int)InputControls.SOUTHEAST] = "Move Down+Right";
        controlNames[(int)InputControls.SOUTH] = "Move Down";
        controlNames[(int)InputControls.SOUTHWEST] = "Move Down+Left";
        controlNames[(int)InputControls.WEST] = "Move Left";
        controlNames[(int)InputControls.NORTHWEST] = "Move Up+Left";
        controlNames[(int)InputControls.FORCEDIAGONAL] = "Force Diagonal Move";
        controlNames[(int)InputControls.INVENTORYSHEET] = "Open Inventory";
        controlNames[(int)InputControls.CHARACTERSHEET] = "View Character";
        controlNames[(int)InputControls.EQUIPMENTSHEET] = "Open Equipment";
        controlNames[(int)InputControls.JOBSHEET] = "Learn New Skills";
        controlNames[(int)InputControls.SKILLSHEET] = "View Skills";
        controlNames[(int)InputControls.RUMORSHEET] = "View Rumors";
        controlNames[(int)InputControls.HELP] = "Help";
        controlNames[(int)InputControls.OPTIONSMENU] = "Options Menu";
        controlNames[(int)InputControls.FIRERANGED] = "Fire Ranged Weapon";
        controlNames[(int)InputControls.WEAPON1] = "Switch to Weapon 1";
        controlNames[(int)InputControls.WEAPON2] = "Switch to Weapon 2";
        controlNames[(int)InputControls.WEAPON3] = "Switch to Weapon 3";
        controlNames[(int)InputControls.WEAPON4] = "Switch to Weapon 4";
        controlNames[(int)InputControls.CYCLEWEAPONLEFT] = "Cycle Weapons Left";
        controlNames[(int)InputControls.CYCLEWEAPONRIGHT] = "Cycle Weapons Right";
        controlNames[(int)InputControls.HOTBAR1] = "Use Item/Skill in Hotbar 1";
        controlNames[(int)InputControls.HOTBAR2] = "Use Item/Skill in Hotbar 2";
        controlNames[(int)InputControls.HOTBAR3] = "Use Item/Skill in Hotbar 3";
        controlNames[(int)InputControls.HOTBAR4] = "Use Item/Skill in Hotbar 4";
        controlNames[(int)InputControls.HOTBAR5] = "Use Item/Skill in Hotbar 5";
        controlNames[(int)InputControls.HOTBAR6] = "Use Item/Skill in Hotbar 6";
        controlNames[(int)InputControls.HOTBAR7] = "Use Item/Skill in Hotbar 7";
        controlNames[(int)InputControls.HOTBAR8] = "Use Item/Skill in Hotbar 8";
        controlNames[(int)InputControls.MINIMAP] = "Toggle Minimap";
        controlNames[(int)InputControls.HEALINGFLASK] = "Use Healing Flask";
        controlNames[(int)InputControls.TOWNPORTAL] = "Use Town Portal";
        controlNames[(int)InputControls.ROTATEABILITY] = "Rotate Targeting";
        controlNames[(int)InputControls.DROPITEM] = "Drop Item from Inv/Equipment";
        controlNames[(int)InputControls.BUILDPLANKS] = "Build Planks (Item World)";

        dictAllInputMaps = new Dictionary<InputControls, TDControl>();

        for (int i = 0; i < (int)InputControls.COUNT; i++)
        {
            dictAllInputMaps.Add((InputControls)i, new TDControl((InputControls)i));
        }
    }

    public static bool GetControlDown(InputControls ic)
    {
        TDControl c = dictAllInputMaps[ic];
        if (Input.GetKeyDown(c.keyMap1))
        {
            return true;
        }
        if (Input.GetKeyDown(c.keyMap2))
        {
            return true;
        }
        return false;
    }
	
	
}
