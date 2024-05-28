using System.Collections;
using System.Collections.Generic;
using Rewired;
using Rewired.Platforms.Switch;
using UnityEngine;

public partial class GameMasterScript
{
#if UNITY_SWITCH

    private enum EMotionSpecificActions
    {
        change_hotbar_via_pitch = 0,
        max
    }

    private float[] fLastInputTimeForMotionCommand;

    private static readonly NpadId[] NpadIds =
    {
        NpadId.Handheld,
        NpadId.No1
    };

    private NpadStyle npadStyles = NpadStyle.Handheld | NpadStyle.FullKey | NpadStyle.JoyConDual;
    private NpadStyle preNpadStyle = NpadStyle.None;

    private nn.util.Float4 npadQuaternion = new nn.util.Float4();
    private Quaternion quaternion = new Quaternion();

    private static bool switchRewiredInitialized;

    private static ControllerMap goodProControllerMap;

    //It's possible rewired is also calling some of this? 
    void Input_InitializeSwitchControllers()
    {
        fLastInputTimeForMotionCommand = new float[ (int)EMotionSpecificActions.max ];
        
        ReInput.ControllerConnectedEvent += OnControllerConnected;
        ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
        ReInput.controllers.AddLastActiveControllerChangedDelegate(OnLastActiveControllerChanged);
        
    }

    /// <summary>
    /// The controller map on the pro-controller is getting stripped off when the switch is undocked. This is our
    /// unfortunate way around it.
    /// </summary>
    /// <param name="args"></param>
    void DoUnfortunateProControllerHack(Controller c)
    {
        if (!gameLoadSequenceCompleted) return;
        
        if (!c.name.Contains("Pro"))
        {
            return;
        }
        
        player = ReInput.players.GetPlayer(0);

        var maps = player.controllers.maps.GetMaps(c);
        if (maps == null ||
            maps.Count == 0 ||
            maps[0] == null)
        {
            //The pro controller got de-mapped, let us reattach.
            player.controllers.maps.AddMap(c, goodProControllerMap );
            player.controllers.maps.SetAllMapsEnabled(true, c);
        }
        else
        {
            //cache this map for later when it is stripped off.
            goodProControllerMap = maps[0];
        }
        
    }
    
    /// <summary>
    /// Check to see if we have a pro-controller. If there's a map, cache it. If there's no map, use the cached map.
    /// </summary>
    /// <param name="args"></param>
    void OnLastActiveControllerChanged(Controller c)
    {
        //DoUnfortunateProControllerHack(c);
    }

    /// <summary>
    /// Check to see if we have a pro-controller. If there's a map, cache it. If there's no map, use the cached map.
    /// </summary>
    /// <param name="args"></param>
    void OnControllerConnected(ControllerStatusChangedEventArgs args)
    {
        player = ReInput.players.GetPlayer(0);
        if (player == null) 
        {
            return;
        }
        
        foreach (var c in player.controllers.Controllers)
        {
            //DoUnfortunateProControllerHack(c);
        }
    }

    /// <summary>
    /// Check to see if we have a pro-controller. If there's a map, cache it. If there's no map, use the cached map.
    /// </summary>
    /// <param name="args"></param>
    void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
    {
        player = ReInput.players.GetPlayer(0);
        if (player == null) 
        {
            return;
        }
        
        foreach (var c in player.controllers.Controllers)
        {
            //DoUnfortunateProControllerHack(c);
        }
    }
    
    //Trying to use this as a place to lock down any inputs that only apply to 
    //joycons or switch touch. Might not be a good idea, will change if it ends up
    //being too many conditional shoehorns.
    bool HandleSwitchSpecificInput()
    {
        return false;
    }

    //updates the controller information and converts math into usable flags
    void Input_UpdateSwitchControllers()
    {
        //UIManagerScript.Debug_SetSwitchDebugText("Switch Controllers: \n");

        // Loop through all Joysticks in the Player
        for (int i = 0; i < player.controllers.joystickCount; i++)
        {
            Joystick joystick = player.controllers.Joysticks[i];

            // Get the Switch Gamepad Extension from the Joystick
            ISwitchIMUDevice ext = joystick.GetExtension<ISwitchIMUDevice>();
            if (ext != null)
            {
                Quaternion orientation;

                // Get the orientation of the controller from the first available sensor
                orientation = ext.GetOrientation();

                // Query each sensor individually
                int sensorCount = ext.imuCount;
                for (int j = 0; j < sensorCount; j++)
                {
                    // Get the orientation as a Unity-coordinate quaternion
                    orientation = ext.GetOrientation(j);

                    // Get the acceleration as a Unity-coordinate Vector3
                    Vector3 acceleration = ext.GetAcceleration(j);

                    // Get the angular velocity as a Unity-coordinate Vector3
                    Vector3 angularVelocity = ext.GetAngularVelocity(j);

                    // Get the rotation angle as a Unity-coordinate Vector3
                    Vector3 rotationAngle = ext.GetRotationAngle(j);

                    // Check if the sensor is at rest
                    bool isAtRest = ext.IsIMUAtRest(j);

                    //UIManagerScript.Debug_AddSwitchDebugText(" == Controller " + i + " Sensor " + j + "\nAcceleration " + acceleration +
                    //                                        "\nAngularVelocity " + angularVelocity + "\nAngle " + rotationAngle + "\nChillin? " + isAtRest);

                    // There are more methods and properties in the ISwitchIMUDevice interface.
                }
            }
        }
    }

    //If the Use Power button is held down (default [Y]) and the controller
    //is tossed upwards, flip the power bar.
    public bool Input_CheckMotionChangeHotbar()
    {
        //if we just did this, don't do it again
        if (Time.realtimeSinceStartup -
            fLastInputTimeForMotionCommand[(int) EMotionSpecificActions.change_hotbar_via_pitch] < 0.3f)
        {
            return false;
        }

        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.floor == MapMasterScript.SHARA_START_CAMPFIRE_FLOOR) return false;

        //get joycon info
        var ext = GetExtentionForController(0);

        //check the R joycon
        if (player.GetButton("Jump to Hotbar") )
        {
            //UIManagerScript.Debug_AddSwitchDebugText("Holding button down and velocity X is " +  ext.GetAngularVelocityRaw(1).x);
            if (ext.GetAngularVelocityRaw(1).x > 0.2f)
            {
                //mark that we just did this
                fLastInputTimeForMotionCommand[(int) EMotionSpecificActions.change_hotbar_via_pitch] =
                    Time.realtimeSinceStartup;
                Debug.Log("Motion get!!");
                return true;
            }
        }

        //not so much
        return false;
    }

    //joycon left is 0, joycon right is 1,
    //but procontroller is also 0
    ISwitchIMUDevice GetExtentionForController(int iControllerNumber)
    {
        Joystick joystick = player.controllers.Joysticks[iControllerNumber];

        // Get the Switch Gamepad Extension from the Joystick
        return joystick.GetExtension<ISwitchIMUDevice>();
    }
}

public partial class UIManagerScript
{
    private string strSwitchDebugText = "SWITCH DEBUG LOG READY";
    private string strCachedSwitchDebugText;

    public static void Debug_SetSwitchDebugText(string strLine)
    {
        //singletonUIMS.strSwitchDebugText = strLine;
    }

    public static void Debug_AddSwitchDebugText(string strLine)
    {
        //singletonUIMS.strSwitchDebugText += strLine + "\n";
    }

/*    
    public void OnGUI()
    {
        if (!string.IsNullOrEmpty(strSwitchDebugText))
        {
            strCachedSwitchDebugText = strSwitchDebugText;
        }

        GUI.Label( new Rect(32,32,1024,1024), strCachedSwitchDebugText );
    }
    */
#endif
}


