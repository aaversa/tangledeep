using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManagerScript
{
    public bool GetExamineMode()
    {
        return examineMode;
    }

    public void CloseExamineMode()
    {
        if (examineMode == false) return;
        examineMode = false;
        if (cursorTargetingMesh != null)
        {
            Destroy(cursorTargetingMesh);
            cursorTargetingMesh = null;
        }

        //Hide the magnifying glass
        if (PlatformVariables.GAMEPAD_ONLY && GameMasterScript.heroPCActor.examineModeIconRenderer != null)
        {
            GameMasterScript.heroPCActor.examineModeIconRenderer.enabled = false;
        }

        //Debug.Log("Closing examine mode.");
        HideGenericInfoBar();
    }

    public void ToggleExamine()
    {
        if (PlatformVariables.GAMEPAD_ONLY)
        {
            ToggleSwitchStyleExamine();
            return;
        }

        if (examineMode)
        {
            
            UIManagerScript.singletonUIMS.uiDialogMenuCursor.GetComponent<AudioStuff>().PlayCue("Cancel");
            CloseExamineMode();
        }
        else
        {
            // Enter examine mode for non-gamepad-only style.
            ExitTargeting();
            CloseHotbarNavigating();
            SetVirtualCursorPosition_Internal(GameMasterScript.heroPCActor.GetPos());
            cursorTargetingMesh = Instantiate(GameMasterScript.GetResourceByRef("CursorTargetingMesh"));
            TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
            Vector2 baseTile = virtualCursorPosition;
            // Find the base tile.
            ctms.goodTiles.Add(baseTile);
            ctms.BuildMesh();
        }
        examineMode = !examineMode;
    }

    private void UpdateExamineModeTiles()
    {
        if (cursorTargetingMesh == null) return;
        TargetingMeshScript ctms = cursorTargetingMesh.GetComponent<TargetingMeshScript>();
        Vector2 baseTile = virtualCursorPosition;
        // Find the base tile.
        ctms.goodTiles.Clear();
        ctms.goodTiles.Add(baseTile);
        ctms.BuildMesh();
        //Debug.Log("Updated to be " + baseTile + " from virtual cursor pos, " + ctms.goodTiles.Count + " " + ctms.badTiles.Count);
    }
}