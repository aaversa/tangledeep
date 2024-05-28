using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TDControlBindings { VIEW_SKILLS, VIEW_HELP, FIRE_RANGED_WEAPON, VIEW_CHAR_INFO, WAIT_TURN, TOGGLE_MENU_SELECT, JUMP_TO_HOTBAR, CONFIRM,
    TOGGLE_RING_MENU, VIEW_EQUIPMENT, VIEW_CONSUMABLES, USE_HEALING_FLASK, USE_TOWN_PORTAL, TOGGLE_MINIMAP, HIDE_UI, VIEW_RUMORS, EXAMINE_MODE,
    DIAGONAL_MOVE_ONLY, CYCLE_WEAPONS_LEFT, CYCLE_WEAPONS_RIGHT, EXAMINE, COUNT };

public class BakedInputBindingDisplay
{
    public static Dictionary<TDControlBindings, string> bindingToRewiredActionName = new Dictionary<TDControlBindings, string>()
    {
        { TDControlBindings.VIEW_SKILLS, "View Skills" },
        { TDControlBindings.VIEW_HELP, "View Help" },
        { TDControlBindings.FIRE_RANGED_WEAPON, "Fire Ranged Weapon" },
        { TDControlBindings.VIEW_CHAR_INFO, "View Character Info" },
        { TDControlBindings.WAIT_TURN, "Wait Turn" },
        { TDControlBindings.TOGGLE_MENU_SELECT, "Toggle Menu Select" },
        { TDControlBindings.JUMP_TO_HOTBAR, "Jump to Hotbar" },
        { TDControlBindings.CONFIRM, "Confirm" },
        { TDControlBindings.TOGGLE_RING_MENU, "Toggle Ring Menu" },
        { TDControlBindings.VIEW_EQUIPMENT, "View Equipment" },
        { TDControlBindings.VIEW_CONSUMABLES, "View Consumables" },
        { TDControlBindings.USE_HEALING_FLASK, "Use Healing Flask" },
        { TDControlBindings.USE_TOWN_PORTAL, "Use Town Portal" },
        { TDControlBindings.TOGGLE_MINIMAP, "Toggle Minimap" },
        { TDControlBindings.HIDE_UI, "Hide UI" },
        { TDControlBindings.VIEW_RUMORS, "View Rumors" },
        { TDControlBindings.EXAMINE_MODE, "Examine Mode" },
        { TDControlBindings.DIAGONAL_MOVE_ONLY, "Diagonal Move Only" },
        { TDControlBindings.CYCLE_WEAPONS_LEFT, "Cycle Weapons Left" },
        { TDControlBindings.CYCLE_WEAPONS_RIGHT, "Cycle Weapons Right" },
        { TDControlBindings.EXAMINE, "Confirm" }
    };

    public static string GetControlBinding(TDControlBindings binding)
    {
#if UNITY_SWITCH
        switch(binding)
        {
            case TDControlBindings.VIEW_SKILLS:
                return "+";
            case TDControlBindings.VIEW_CHAR_INFO:
                return "+";
            case TDControlBindings.TOGGLE_MENU_SELECT:
                return "+";
            case TDControlBindings.VIEW_EQUIPMENT:
                return "+";
            case TDControlBindings.VIEW_CONSUMABLES:
                return "+";
            case TDControlBindings.VIEW_RUMORS:
                return "+";
            case TDControlBindings.TOGGLE_MINIMAP:
                return "-";
            default:
                return CustomAlgorithms.GetButtonAssignment(bindingToRewiredActionName[binding]);
        }
#elif UNITY_PS4
        switch (binding)
        {
            case TDControlBindings.VIEW_SKILLS:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_CHAR_INFO:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.TOGGLE_MENU_SELECT:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_EQUIPMENT:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_CONSUMABLES:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_RUMORS:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.TOGGLE_MINIMAP:
                return FontManager.GetButtonAssignment("Cycle Minimap");
            default:
                return FontManager.GetButtonAssignment(bindingToRewiredActionName[binding]);
        }
#elif UNITY_XBOXONE
        switch (binding)
        {
            case TDControlBindings.VIEW_SKILLS:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_CHAR_INFO:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.TOGGLE_MENU_SELECT:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_EQUIPMENT:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_CONSUMABLES:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.VIEW_RUMORS:
                return FontManager.GetButtonAssignment("Toggle Menu Select");
            case TDControlBindings.TOGGLE_MINIMAP:
                return FontManager.GetButtonAssignment("Cycle Minimap");
            default:
                return FontManager.GetButtonAssignment(bindingToRewiredActionName[binding]);
        }
#elif UNITY_ANDROID
        switch (binding)
        {
            case TDControlBindings.VIEW_SKILLS:
                return "(Menu button)";
            case TDControlBindings.VIEW_CHAR_INFO:
                return "(Menu button)";
            case TDControlBindings.TOGGLE_MENU_SELECT:
                return "(Menu button)";
            case TDControlBindings.TOGGLE_RING_MENU:
                return "(Ring Menu button)";
            case TDControlBindings.VIEW_EQUIPMENT:
                return "(Menu button)";
            case TDControlBindings.VIEW_CONSUMABLES:
                return "(Menu button)";
            case TDControlBindings.VIEW_RUMORS:
                return "(Menu button)";
            case TDControlBindings.TOGGLE_MINIMAP:
                return "(View button)";
            default:
                return CustomAlgorithms.GetButtonAssignment(bindingToRewiredActionName[binding]);         
        }
#else
        return CustomAlgorithms.GetButtonAssignment(bindingToRewiredActionName[binding]);
#endif
    }
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
