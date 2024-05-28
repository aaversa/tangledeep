using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A place to keep all the new Examine Mode logic.
public partial class TDInputHandler
{
    private static float fExamineModeGlassStartSpeed = 1.0f;
    private static float fExamineModeGlassMaxSpeed = 12.0f;
    private static float fExamineModeTimeCursorMoving;
    private static float fExamineModeTimeToMaxSpeed = 1.0f;

    private static Vector2 vExamineModeLastQuantizedLocation;
    private static Actor examineModeLastExaminedActor;
    
    static bool HandleExamineModeInput_GamepadStyle()
    {
        if (!UIManagerScript.examineMode) return false;

        //Cancel closes the mode no matter what.
        if (player.GetButtonDown("Cancel"))
        {
            UIManagerScript.PlayCursorSound("Cancel");
            uims.CloseExamineMode();
            return true;
        }

        //Confirm checks to see what is in the tile and perhaps talk to it.
        if (player.GetButtonDown("Confirm"))
        {
            Vector2 clickedPosition = uims.GetVirtualCursorPosition();
            MapTileData mtd = MapMasterScript.GetTile(clickedPosition);
            
            return GameMasterScript.gmsSingleton.CheckTileForFriendlyConversation(mtd, rightClick: false);
        }

        //If we aren't moving the stick then don't do anything right now.
        Vector2 vStickNative = new Vector2(player.GetAxis("Move Horizontal"), player.GetAxis("Move Vertical"));
        if (vStickNative.sqrMagnitude <= 0.04f)
        {
            //if we are looking at something and not moving, we should be a little more transparent.
            if (examineModeLastExaminedActor != null && examineModeLastExaminedActor.actorUniqueID != GameMasterScript.heroPCActor.actorUniqueID)
            {
                GameMasterScript.heroPCActor.examineModeIconRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.7f);
            }

            //relax
            fExamineModeTimeCursorMoving = 0f;
            return false;
        }

        //we are moving
        GameMasterScript.heroPCActor.examineModeIconRenderer.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        //we are moving
        fExamineModeTimeCursorMoving += Time.deltaTime;

        //accelerate a bit
        float fSpeedRightMeow = Mathf.Lerp(fExamineModeGlassStartSpeed, fExamineModeGlassMaxSpeed, 
            fExamineModeTimeCursorMoving / fExamineModeTimeToMaxSpeed);

        //move the icon into position.
        GameMasterScript.heroPCActor.examineModeIconRenderer.transform.localPosition += (Vector3)vStickNative * Time.deltaTime * fSpeedRightMeow;

        //the tile is based on the transform of the icon
        Vector2 vQuantizedLocation = GameMasterScript.heroPCActor.examineModeIconRenderer.transform.position;

        //offset because of tile offsets vs unity grid
        vQuantizedLocation += new Vector2(0.5f,0.5f);

        vQuantizedLocation.x = Mathf.Floor(vQuantizedLocation.x);
        vQuantizedLocation.y = Mathf.Floor(vQuantizedLocation.y);

        UIManagerScript.singletonUIMS.SetVirtualCursorPosition(vQuantizedLocation);

        //a new tile perhaps?
        if (vQuantizedLocation != vExamineModeLastQuantizedLocation)
        {
            //This will update HoverInfoScript's currentHoveredActor;
            HoverInfoScript.GetHoverText(MapMasterScript.activeMap.GetTile(vQuantizedLocation));
            vExamineModeLastQuantizedLocation = vQuantizedLocation;

            //if we are looking at someone different from last time, do a cute thing
            /* if (HoverInfoScript.currentHoveredActor != null &&
                HoverInfoScript.currentHoveredActor != examineModeLastExaminedActor)
            {
                OnNewActorBeingExamined();
            } */
        }

        //face the glass
        GameMasterScript.heroPCActor.UpdateLastMovedDirection(MapMasterScript.GetDirectionFromAngle(CombatManagerScript.GetAngleBetweenPoints(Vector2.zero, GameMasterScript.heroPCActor.examineModeIconRenderer.transform.localPosition)));
        GameMasterScript.heroPCActor.myStats.UpdateStatusDirections();

        return false;
    }

    /// <summary>
    /// A new actor is being moused over, do something cute with it.
    /// </summary>
    static void OnNewActorBeingExamined()
    {
        //keep track of this.
        //examineModeLastExaminedActor = HoverInfoScript.currentHoveredActor;

        //if the "new actor" is null, there's nothing to do here.
        if (examineModeLastExaminedActor == null )
        {
            return;
        }

        bool bShouldPulseActor = false;

        //now, just what is this here duder?
        switch (examineModeLastExaminedActor.GetActorType())
        {
            //Do not pulse
            case ActorTypes.HERO:
            case ActorTypes.DOOR:
            case ActorTypes.THING:
            case ActorTypes.STAIRS:
            case ActorTypes.COUNT:
                //I know this is defined above, but having the list here feels clean.
                bShouldPulseActor = false; 
                break;

            //Do yes pulse
            case ActorTypes.ITEM:
            case ActorTypes.MONSTER:
            case ActorTypes.POWERUP:
            case ActorTypes.NPC:
                bShouldPulseActor = true;
                break;

            //Maybe pulse? Only if the player
            //can use or break it.
            case ActorTypes.DESTRUCTIBLE:
                var d = examineModeLastExaminedActor as Destructible;
                bShouldPulseActor = d != null && !d.isDestroyed && d.targetable;
                break;
        }

        //boop
        if (bShouldPulseActor)
        {
            var aTransform = examineModeLastExaminedActor.GetObject().transform;
            Vector3 vScale = aTransform.localScale;

            //don't boop if we're already boopin' or bigger
            if (vScale.x == 1.0f && vScale.y == 1.0f)
            {
                aTransform.localScale = vScale * 1.2f;
                LeanTween.scale(aTransform.gameObject, vScale, 0.35f).setEaseOutBack();
                UIManagerScript.PlayCursorSound("Move");
            }
        }

    }
}

public partial class UIManagerScript
{

    private void UpdateSwitchStyleExamineModeTiles()
    {
        if (cursorTargetingMesh == null) return;
        TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
        Vector2 baseTile = virtualCursorPosition;
        // Find the base tile.
        ctms.goodTiles.Clear();
        ctms.goodTiles.Add(baseTile);
        ctms.BuildMesh();
    }

    //Turn the glass on and off.
    public void ToggleSwitchStyleExamine()
    {
        //Turn off
        if (examineMode)
        {
            CloseExamineMode();
            examineMode = false;
        }
        else
        {
            // Enter examine mode
            examineMode = true;

            ExitTargeting();
            CloseHotbarNavigating();
            SetVirtualCursorPosition_Internal(GameMasterScript.heroPCActor.GetPos());
            cursorTargetingMesh = Instantiate(GameMasterScript.GetResourceByRef("CursorTargetingMesh"));
            TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
            Vector2 baseTile = virtualCursorPosition;
            // Find the base tile.
            ctms.goodTiles.Add(baseTile);
            ctms.BuildMesh();

            //return the glass to center and turn it on
            var glass = GameMasterScript.heroPCActor.examineModeIconRenderer;

            glass.transform.localPosition = new Vector3(0,0,0f);
            glass.enabled = true;

            //boop
            glass.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            LeanTween.scale(glass.gameObject, Vector3.one, 0.35f);

            //play a sound
            PlayCursorSound("Select");

        }
    }

}