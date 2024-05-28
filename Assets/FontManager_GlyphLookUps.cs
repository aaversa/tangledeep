using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public partial class FontManager
{
    public static Dictionary<string, Dictionary<string, int>> dictAllSpriteIDsByControllerElements;

    // These are predefined GUIDs provided by Rewired. More can be added as needed by looking at Rewired documentation.
    public const string XBOX_GUID = "d74a350e-fe8b-4e9e-bbcd-efff16d34115";
    public const string PS4_GUID = "cd9718bf-a87a-44bc-8716-60a0def28a9f";
    public const string KEYBOARD_GUID = "ae4830f9-63db-4d4c-90b3-1beb46ecaf49";

    // Populate our master glyph dictionary. Depending on the controller type used, each physical control will have a different ID
    public static void CreateDictionaries()
    {
        if (dictAllSpriteIDsByControllerElements != null) return;

        Dictionary<string, int> XboxDictInts = new Dictionary<string, int>()
        {
            { "X", 6 },
            { "A", 4 },
            { "B", 5 },
            { "Y", 7 },
            { "Left Shoulder", 21 },
            { "Right Shoulder", 22 },
            { "Left Trigger", 19 },
            { "Right Trigger", 20 },
            { "D-Pad Up", 29 },
            { "D-Pad Right", 30 },
            { "D-Pad Down", 31 },
            { "D-Pad Left", 32 },
            { "Left Stick Up", 0 },
            { "Left Stick Right", 1 },
            { "Left Stick Down", 2 },
            { "Left Stick Left", 3 },

            //{"Start", 0 },
            //{"Guide", 0 }
        };

        Dictionary<string, int> PS4DictInts = new Dictionary<string, int>()
        {
            { "Square", 9 },
            { "Cross", 8 },
            { "Circle", 24 },
            { "Triangle", 10 },
            { "L1", 13 },
            { "R1", 14 },
            { "L2", 15 },
            { "R2", 16 },
            { "D-Pad Up", 33 },
            { "D-Pad Right", 34 },
            { "D-Pad Down", 35 },
            { "D-Pad Left", 36 },
            { "Left Stick Up", 0 },
            { "Left Stick Right", 1 },
            { "Left Stick Down", 2 },
            { "Left Stick Left", 3 },
            { "Options", 88 },
            { "Touchpad Button", 89 },

            //getting different elementIdentifierNames in editor(above) and on console(below)
            { "square button", 9 },
            { "cross button", 8 },
            { "circle button", 24 },
            { "triangle button", 10 },
            { "L1 button", 13 },
            { "R1 button", 14 },
            { "L2 button", 15 },
            { "R2 button", 16 },
            { "up button", 33 },
            { "right button", 34 },
            { "down button", 35 },
            { "left button", 36 },
            { "left stick up", 0 },
            { "left stick right", 1 },
            { "left stick down", 2 },
            { "left stick left", 3 },
            { "OPTIONS button", 88 },
            { "touch pad button", 89 }
        };

        Dictionary<string, int> KeyboardDictInts = new Dictionary<string, int>()
        {
            { "Up Arrow", 78 },
            { "Right Arrow", 79 },
            { "Down Arrow", 80 },
            { "Left Arrow", 81 },
            { "A", 37 },
            { "B", 38 },
            { "C", 39 },
            { "D", 40 },
            { "E", 41 },
            { "F", 42 },
            { "G", 43 },
            { "H", 44 },
            { "I", 45 },
            { "J", 46 },
            { "K", 47 },
            { "L", 48 },
            { "M", 49 },
            { "N", 50 },
            { "O", 51 },
            { "P", 52 },
            { "Q", 53 },
            { "R", 54 },
            { "S", 55 },
            { "T", 56 },
            { "U", 57 },
            { "V", 58 },
            { "W", 59 },
            { "X", 60 },
            { "Y", 61 },
            { "Z", 62 },
            { "Space", 63 },
            { "ESC", 66 },
            { "Return", 67 },
            { "Tab", 65 },
            { "LeftShift", 64 },
            { "Alpha1", 68 },
            { "Alpha2", 69 },
            { "Alpha3", 70 },
            { "Alpha4", 71 },
            { "Alpha5", 72 },
            { "Alpha6", 73 },
            { "Alpha7", 74 },
            { "Alpha8", 75 },
            { "Alpha9", 76 },
            { "Alpha0", 77 },
        };

        dictAllSpriteIDsByControllerElements = new Dictionary<string, Dictionary<string, int>>()
        {
            { XBOX_GUID,  XboxDictInts },
            { PS4_GUID, PS4DictInts },
            { KEYBOARD_GUID, KeyboardDictInts }
        };
    }

    public static string GetButtonAssignment(string buttonActionName, bool getAllAssignments = false)
    {
        IList<Rewired.ControllerMap> mapList;
        IList<Rewired.ControllerMap> controllerMapList;
        IList<Rewired.ControllerMap> keyboardMapList;

        if (dictAllSpriteIDsByControllerElements == null)
            CreateDictionaries();

        // Get the active player
        Rewired.Player rp;

        if (GameMasterScript.actualGameStarted)
        {
            rp = GameMasterScript.gmsSingleton.player;
        }
        else
        {
            rp = TitleScreenScript.titleScreenSingleton.player;
        }

        bool isControllerConnected = Rewired.ReInput.controllers.GetLastActiveControllerType() == Rewired.ControllerType.Joystick; //rp.controllers.GetLastActiveController().isConnected;

        int joyID = 0;

        if (isControllerConnected)
        {
            try { joyID = rp.controllers.GetLastActiveController().id; }
            catch (Exception e)
            {
                Debug.LogError("Failed to get controller joyID " + e);
                joyID = 0;
            }
        }

        // Get all active controller + keyboard maps
        controllerMapList = rp.controllers.maps.GetMaps(Rewired.ControllerType.Joystick, joyID);
        keyboardMapList = rp.controllers.maps.GetMaps(Rewired.ControllerType.Keyboard, 0);

        //bool isControllerConnected = ControllerConnection.IsControllerConnected();

        // This is a string like "D-Pad Left", "A", etc. It is NOT localized by default
        string identifierName = "";

        // For joysticks, this tells us the unique identifier that lets us know if its a DualShock 3, 360 controller etc.
        Guid controllerGUID;

        // For joysticks, this tells us what specific button it is, like L2 or R2 on a Dualshock
        int elementIdentifierID = 0;

        // STEP 1: Find the button mapping for buttonActionName
        // If we have a joystick connected and keyboard is not the last active controller, it will populate controllerGUID and identifierID

        if (isControllerConnected && !rp.controllers.GetLastActiveController().name.Contains("Keyboard"))
        {
            mapList = controllerMapList;
            // Attempt to find buttonActionName somewhere in our mapped controls
            foreach (Rewired.JoystickMap joyMap in mapList)
            {
                if (!joyMap.enabled) continue;

                Rewired.ActionElementMap cm = joyMap.GetFirstElementMapWithAction(buttonActionName);
                if (cm != null)
                {
                    // These values will be used to display the appropriate glyph from sprite font
                    controllerGUID = joyMap.hardwareGuid;
                    identifierName = cm.elementIdentifierName;
                    elementIdentifierID = cm.elementIdentifierId;
                    break;
                }
            }

        }
        else
        {
            isControllerConnected = false;
            mapList = keyboardMapList;
            foreach (Rewired.KeyboardMap keym in mapList)
            {
                // Ignore disconnected keyboards
                if (!keym.enabled) continue;

                Rewired.ActionElementMap cm = keym.GetFirstElementMapWithAction(buttonActionName);
                if (cm != null)
                {
                    if (getAllAssignments)
                    {
                        Rewired.ActionElementMap[] maps = keym.GetElementMapsWithAction(buttonActionName, true);
                        Debug.LogError("getAllAssignments is  not finished");
                        //numElementNames = 0;
                        //for (int i = 0; i < maps.Length; i++)
                        //{
                        //    elementNames[i] = maps[i].elementIdentifierName;
                        //    numElementNames++;
                        //}
                    }
                    // These values will be used to display the appropriate glyph from sprite font
                    controllerGUID = keym.hardwareGuid;
                    identifierName = cm.elementIdentifierName;
                    elementIdentifierID = cm.elementIdentifierId;
                    break;
                }
            }
        }

        // FAILURE: We didn't find anything with that mapping. It's unassigned. Whoops.
        if (string.IsNullOrEmpty(identifierName))
        {
            return StringManager.GetString("control_unassigned");
        }

        if (isControllerConnected)
        {
            //TDebug.Log("Glyph id is: " + elementIdentifierID + " and name is " + identifierName + " and guid is " + controllerGUID.ToString());
            // Using the GUID and element ID info, we can understand if the button pressed corresponds to
            // something like "Xbox 360 - X" "DualShock 3 - Circle"
            // We want to use that data to look up in our TMPro glyph spritesheet what sprite we should use
            // Rather than using GetString, we'd instead write "<sprite=n>" where n is the index in OUR sheet.
            // The mappings in OUR spritesheet are listed in FontManager toward the top of the file

            int glyphID = FontManager.GetSpriteIDFromControllerElementID(controllerGUID.ToString(), identifierName);
            //return "<sprite=" + glyphID + ">";

            //if we find glyphID in our dictionary, we then return "<sprite=x index=n>" where "x" is the name of the Sprite Asset and "n" is the index in our Sprite Asset
            //for TMPro to be able to find our Sprite Asset by name, it needs to be inside folder "Resources/Sprite Assets/"
            //if we can't find glyphID in our dictionary, instead of sprite we return text(Localized indentifier name)
            if (glyphID != -1)
                return "<sprite=\"FlatControlIconSpriteSheet\" index=" + glyphID + ">";
            else
                return StringManager.GetLocalizedStringOrFallbackToEnglish(identifierName);
        }
        else
        {
            // For keyboard and non-glyph-supported controllers, just return the localized version of "D-Pad Up" for example

            int glyphID = FontManager.GetSpriteIDFromControllerElementID(controllerGUID.ToString(), identifierName);
            //return "<sprite=" + glyphID + ">";

            //if we find glyphID in our dictionary, we then return "<sprite=x index=n>" where "x" is the name of the Sprite Asset and "n" is the index in our Sprite Asset
            //for TMPro to be able to find our Sprite Asset by name, it needs to be inside folder "Resources/Sprite Assets/"
            //if we can't find glyphID in our dictionary, instead of sprite we return text(Localized indentifier name)
            if (glyphID != -1)
                return "<sprite=\"FlatControlIconSpriteSheet\" index=" + glyphID + ">";
            else
                return StringManager.GetLocalizedStringOrFallbackToEnglish(identifierName);
        }
    }

    // Using guid and elementID, we can find the correct glyph in our dictionary.
    public static int GetSpriteIDFromControllerElementID(string guid, string elementID)
    {
        if (!dictAllSpriteIDsByControllerElements.ContainsKey(guid))
        {
            Debug.LogError("Dictionary does not contain  Guid : " + guid);
            return -1;
        }
        if (!dictAllSpriteIDsByControllerElements[guid].ContainsKey(elementID))
        {
            Debug.LogError("Dictionary does not contain element id : " + elementID + " for guid : " + guid);
            return -1;
        }

        return dictAllSpriteIDsByControllerElements[guid][elementID];
    }
}
