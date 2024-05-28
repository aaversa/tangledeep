using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUIScript : MonoBehaviour {

    static MiniMapScript miniMapOverlay;
    static RectTransform miniMapRT;
    public static bool shouldMiniMapBeOpen;
    static MinimapStates mmapState;
    public static MinimapStates MinimapState
    {
        get
        {
            return mmapState;
        }
        set
        {
            mmapState = value;
            //if (Debug.isDebugBuild) Debug.Log("Set minimap state to " + value + " for some reason.");
        }
    }
    public static MinimapStates prevMinimapState;
    public static MinimapUIScript singleton;

    static float prevSizeX;
    static float prevSizeY;

    //0.5f,0.5f == center of screen
    public static Vector2 vDesiredOffsetOnScreen;
    bool hasLocalScript;
    public const float MAP_MOVE_SPEED_MULTIPLIER = 5.6f; // was 2.8f, that was too slow.

    // Use this for initialization
    void Start () {
        singleton = this;
#if UNITY_SWITCH
        miniMapOverlay = GameObject.Find("MiniMapOverlay_PC").GetComponent<MiniMapScript>();
        miniMapOverlay.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 1f);
#else
        miniMapOverlay = GameObject.Find("MiniMapOverlay_PC").GetComponent<MiniMapScript>();
        miniMapOverlay.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, PlayerOptions.minimapOpacity);
#endif

        miniMapRT = miniMapOverlay.myRT;
        miniMapOverlay.ToggleActive(false);
        hasLocalScript = false;
    }

    public static bool GetOverlay()
    {
        if (miniMapOverlay == null) return false;
        if (!miniMapOverlay.IsMiniMapActive())
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static void StopOverlay()
    {
        //Debug.Log("Stopping overlay");
        if (miniMapOverlay == null) return;
		miniMapOverlay.OnMouseExit();
        prevMinimapState = MinimapState;
        MinimapState = MinimapStates.CLOSED;
        miniMapOverlay.ToggleActive(false);
    }

    public static void RefreshMiniMap()
    {
        //Debug.Log("Minimap has been refreshed.");
        miniMapOverlay.BuildNewTexture(MapMasterScript.activeMap.columns, MapMasterScript.activeMap.rows);
    }

    public static void UpdateMinimapColors()
    {
        if (!GetOverlay()) return;
        GenerateOverlay();
    }

    public static void GenerateOverlay()
    {
        if (PlayerOptions.draggedMinimap && MinimapState != MinimapStates.TRANSLUCENT)
        {
            if (PlayerOptions.miniMapPositionX > 0 && PlayerOptions.miniMapPositionX <= Screen.width - 100f 
			&& PlayerOptions.miniMapPositionY > 0 && PlayerOptions.miniMapPositionX <= Screen.height - 100f)
            {
                miniMapOverlay.gameObject.transform.position = new Vector3(PlayerOptions.miniMapPositionX, PlayerOptions.miniMapPositionY, miniMapOverlay.gameObject.transform.position.z);
            }
            else
            {
                PlayerOptions.draggedMinimap = false;
            }
        }

        miniMapOverlay.ToggleActive(true);

        //Debug.Log("Minimap overlay is now active");

        miniMapOverlay.BuildNewTexture(MapMasterScript.activeMap.columns, MapMasterScript.activeMap.rows);        



    }

    public static void MoveMinimap(float x, float y)
    {
        singleton.DragMiniMapInternal(false, (x*MAP_MOVE_SPEED_MULTIPLIER), (y* MAP_MOVE_SPEED_MULTIPLIER));
    }

    public void DragMiniMap()
    {
        DragMiniMapInternal(true, 0f, 0f);
    }

    public void DragMiniMapInternal(bool dragByMouse, float xOffset, float yOffset)
    {
        if (dragByMouse)
        {
            miniMapOverlay.SetTransformPosition(Input.mousePosition);
            PlayerOptions.SetCurrentMiniMapPosition(Input.mousePosition.x, Input.mousePosition.y);
        }
        else
        {
            if (Mathf.Abs(PlayerOptions.miniMapPositionX - miniMapOverlay.GetTransformPosition().x) >= 1f)
            {
                PlayerOptions.SetCurrentMiniMapPosition(miniMapOverlay.GetTransformPosition().x, miniMapOverlay.GetTransformPosition().y);
            }

            PlayerOptions.OffsetCurrentMiniMapPosition(xOffset, yOffset);

            PlayerOptions.SetCurrentMiniMapPosition(
                Mathf.Clamp(PlayerOptions.miniMapPositionX, 1f, Screen.width - 100f),
                Mathf.Clamp(PlayerOptions.miniMapPositionY, 1f, Screen.height - 100f)
                );

            miniMapOverlay.SetTransformPosition(new Vector2(PlayerOptions.miniMapPositionX, PlayerOptions.miniMapPositionY));
        }

        PlayerOptions.draggedMinimap = true;
        PlayerOptions.mapState = MinimapState;
    }

    /// <summary>
    /// Set a desired location for the minimap, and we will move towards it over time.
    /// </summary>
    /// <param name="v">An offset based on the size of the minimap. 0.5f 0.5f means "put minimap in center of screen"</param>
    public static void SetDesiredTransformPositionRelativeToScreen(Vector2 v)
    {
        vDesiredOffsetOnScreen = v;
    }
    public static void BaseMapChanged(int columns, int rows)
    {
        miniMapOverlay.BaseMapChanged(columns, rows);
    }

    public static void SetMinimapToStateBasedOnPlayerOptions()
    {
        if (Debug.isDebugBuild) Debug.Log("ORIGINAL input map style index is " + PlayerOptions.mapStyle);
        if (PlayerOptions.mapStyle < 0 || PlayerOptions.mapStyle >= (int)MiniMapStyles.COUNT)
        {
            PlayerOptions.mapStyle = 0;
        }

        int localValue = PlayerOptions.mapStyle;
        if (Debug.isDebugBuild) Debug.Log("PlayerOptions Input map style is " + (MiniMapStyles)localValue);

        if (PlatformVariables.GAMEPAD_STYLE_OPTIONS_MENU)
        {
            //localValue++;
            if (Debug.isDebugBuild) Debug.Log("Input map style is now " + (MiniMapStyles)localValue);
        }

        switch (localValue)
        {
            case (int)MiniMapStyles.CYCLE:
                // behave as normal
                MinimapState++;
                if (MinimapState == MinimapStates.MAX)
                {
                    MinimapState = MinimapStates.CLOSED;
                }
                break;
            case (int)MiniMapStyles.OVERLAY:
                if (MinimapState != MinimapStates.TRANSLUCENT)
                {
                    MinimapState = MinimapStates.TRANSLUCENT;
                }
                else
                {
                    MinimapState = MinimapStates.CLOSED;
                }
                break;
            case (int)MiniMapStyles.SMALL:
                if (MinimapState != MinimapStates.SMALL)
                {
                    MinimapState = MinimapStates.SMALL;
                }
                else
                {
                    MinimapState = MinimapStates.CLOSED;
                }
                break;
            case (int)MiniMapStyles.LARGE:
                if (MinimapState != MinimapStates.LARGE)
                {
                    MinimapState = MinimapStates.LARGE;
                }
                else
                {
                    MinimapState = MinimapStates.CLOSED;
                }
                break;
        }

        SetMinimapToSpecificState(MinimapState);
    }

    public static void SetMinimapToSpecificState(MinimapStates mms, bool alwaysSwitchOnOrOff = false)
    {
        //if (Debug.isDebugBuild) Debug.Log("Setting state to " + mms + ", state is " + miniMapOverlay.myRT.gameObject.activeSelf);

        if (!miniMapOverlay.IsMiniMapActive() && mms != MinimapStates.CLOSED)
        {            
            GenerateOverlay();
        }
        else 
        {
            if (alwaysSwitchOnOrOff)
            {
                StopOverlay();
                shouldMiniMapBeOpen = false;
                return;
            }
        }

        // Determine size.
        float sizeX = MapMasterScript.activeMap.columns * 8f;
        float sizeY = MapMasterScript.activeMap.rows * 8f;

        if (PlayerOptions.fixedMinimapSize > 0f && (mms == MinimapStates.LARGE || mms == MinimapStates.SMALL))
        {
            // force specific size.
            miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PlayerOptions.fixedMinimapSize);
            miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, PlayerOptions.fixedMinimapSize);
            MinimapState = mms;
            shouldMiniMapBeOpen = true;
            return;
        }

        //When we change sizes/styles, draw from scratch for that frame.
        miniMapOverlay.ResetMinimapDataInTMG();

        switch (mms)
        {
            case MinimapStates.CLOSED:
                StopOverlay();
                shouldMiniMapBeOpen = false;
                return;
            case MinimapStates.SMALL:
                miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeX*2);
                miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeY*2);
                break;
            case MinimapStates.LARGE:
            case MinimapStates.TRANSLUCENT:
                float sizeMult = 3f;
                // Don't let the minimap be so big that it exceeds screen bounds.
                while (sizeY * sizeMult >= 1040f)
                {
                    sizeMult -= 0.25f; // This will next happen on every time
                }

                miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeX * sizeMult);
                miniMapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeY * sizeMult);                
                break;
        }

        MinimapState = mms;
        shouldMiniMapBeOpen = true;
        RefreshMiniMap(); 
    }

    public void ToggleMiniMapFromButton()
    {
        MinimapState = MinimapStates.LARGE;
        if (miniMapOverlay.IsMiniMapActive())
        {
            StopOverlay();
            shouldMiniMapBeOpen = false;
        }
        else
        {
            SetMinimapToSpecificState(MinimapState);
        }
    }
    public static void DestroyTexturesAndCleanup()
    {
        miniMapOverlay.DestroyTexturesAndCleanup();
    }
}
