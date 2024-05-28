using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public partial class TitleScreenScript
{
    public Directions GetDirectionalInput(out bool dpadPressed)
    {
        ControllerType controlTypeUsed = ReInput.controllers.GetLastActiveControllerType();
        bool usingJoystick = controlTypeUsed == ControllerType.Joystick;

        // This should stay as COUNT until we confirm some kind of directional input
        Directions directionCapturedThisFrame = Directions.COUNT;

        // Check to see if there's dpad or stick movement. 
        float joystickDeadZone = PlayerOptions.buttonDeadZone / 100f;

        dpadPressed = player.GetButton("DPadPressed");

        //if the dpad is being pressed, we do not want a deadzone, because we want to 
        //act as if the dpad press is digital 0/1, instead of an analog axis like the stick gives us.
        if (dpadPressed || !usingJoystick)
        {
            //dpad ends the deadzone. Only verdant life blooms here now.
            joystickDeadZone = 0f;
        }

        if (player.GetButton("Move Up+Left"))
        {
            directionCapturedThisFrame = Directions.NORTHWEST;
        }
        if (player.GetButton("Move Down+Left"))
        {
            directionCapturedThisFrame = Directions.SOUTHWEST;
        }
        if (player.GetButton("Move Up+Right"))
        {
            directionCapturedThisFrame = Directions.NORTHEAST;
        }
        if (player.GetButton("Move Down+Right"))
        {
            directionCapturedThisFrame = Directions.SOUTHEAST;
        }
        if (player.GetButton("Move Left"))
        {
            directionCapturedThisFrame = Directions.WEST;
        }
        if (player.GetButton("Move Right"))
        {
            directionCapturedThisFrame = Directions.EAST;
        }
        if (player.GetButton("Move Up"))
        {
            directionCapturedThisFrame = Directions.NORTH;
        }
        if (player.GetButton("Move Down"))
        {
            directionCapturedThisFrame = Directions.SOUTH;
        }

        if (usingJoystick && directionCapturedThisFrame == Directions.COUNT)
        {
            Vector2 vMoveAxis = new Vector2(player.GetAxis("Move Horizontal"), player.GetAxis("Move Vertical"));

            // if deadzone, check for the four diagonal move buttons
            // otherwise return neutral
            if (vMoveAxis.magnitude < joystickDeadZone)
            {
                directionCapturedThisFrame = Directions.NEUTRAL;
            }

            if (directionCapturedThisFrame != Directions.NEUTRAL)
            {
                //this value is clamalamped at 180, so if we know we are pointing left, subtract this value from
                //360 to get a True Angle TM
                float fStickAngle = Vector2.Angle(vMoveAxis, Vector2.up);
                if (vMoveAxis.x < 0)
                {
                    fStickAngle = 360f - fStickAngle;
                }

                //break compass into eight parts, for the eight directions that will go into the Directions Gauntlet.
                float fConeDegrees = 360.0f / 8;

                //quantize our 0-360 value to 45 degree increments, and then 0-8 (360 == 8)
                int iRoundifiedValue = (int)Mathf.Round(fStickAngle / fConeDegrees) % 8;

                //if we're diagonal only and not in a menu, drop any not-diagonal values
                if (player.GetButton("Diagonal Move Only") &&
                    !UIManagerScript.AnyInteractableWindowOpen() &&
                    iRoundifiedValue % 2 == 0)
                {
                    directionCapturedThisFrame = Directions.NEUTRAL;
                }
                else
                {
                    directionCapturedThisFrame = Directions.NORTH + (iRoundifiedValue % 8);
                }
            }
        }

        if (directionCapturedThisFrame == Directions.COUNT) directionCapturedThisFrame = Directions.NEUTRAL;

        TDInputHandler.lastActiveControllerType = controlTypeUsed;

        return directionCapturedThisFrame;
    }
}