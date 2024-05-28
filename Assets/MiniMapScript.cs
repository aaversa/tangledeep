using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//[RequireComponent(typeof(MeshFilter))]
//[RequireComponent(typeof(MeshRenderer))]
public class MiniMapScript : MonoBehaviour
{
    public Image myImage;
    public RawImage myRI;
    Texture2D myTexture;

    public RectTransform myRT;

    public RawImage switchRI;

    TileMeshGenerator tmg;

    /// <summary>
    /// Check the content we're hovering over every X frames.
    /// </summary>
    public int updateHoverFrames;

    Vector2 mapTextureSize;

    public static bool cursorIsHoveringOverMap;

    int framesUntilUpdate;
    // Use this for initialization

    string preMiniMapHoverText;
    string preMiniMapHoverTextBuffer;

    void Start()
    {
        if (myRI == null) myRI = GetComponent<RawImage>();
        if (myImage == null) myImage = GetComponent<Image>();
        if (tmg == null) tmg = GetComponent<TileMeshGenerator>();
        if (myRT == null) myRT = transform as RectTransform;

#if UNITY_SWITCH
        //myRI = switchRI;
        //myRT = switchRI.GetComponent<RectTransform>();
#endif

        
    }

    void UpdateTranslucentMode()
    {
        //In translucent mode, track the player as the center of the map.
        if (MinimapUIScript.MinimapState == MinimapStates.TRANSLUCENT)
        {
            var rt = transform as RectTransform;
            //find the distance from us to the goal, and cover some of it.
            Vector2 vCurrentPos = rt.localPosition;

            //the offset is based on screen space, so 0.5f 0.5f means center
            Vector2 vTranslatedOffset = new Vector2(0.5f, 0.5f) - MinimapUIScript.vDesiredOffsetOnScreen;
            float fMinimapTexWidth = rt.sizeDelta.x;
            float fMinimapTexHeight = rt.sizeDelta.y;

            //our position is based on our own width and height, it will be scaled to match the UI
            Vector2 vDesiredPos = new Vector2(fMinimapTexWidth * vTranslatedOffset.x, fMinimapTexHeight * vTranslatedOffset.y);

            //but wait! "center of the screen" really means "center of playable area" since there's a UI at the bottom
            //sooooo let's cheat.
            vDesiredPos.y += 128f;

            //ok <3
            vDesiredPos.x *= rt.lossyScale.x;
            vDesiredPos.y *= rt.lossyScale.y;

            //now move towards it.
            Vector2 vDelta = vDesiredPos - vCurrentPos;

            int iPxCloseEnough = 2;
            if (vDelta.sqrMagnitude < iPxCloseEnough * iPxCloseEnough)
            {
                rt.localPosition = vDesiredPos;
            }
            else
            {
                //close in by some % of the distance per second -- slowing as we approach
                //because it's some % every frame and the distance is shrinking
                vDelta *= 0.95f * Time.deltaTime;
                rt.localPosition = rt.localPosition + (Vector3)vDelta;
            }
        }
    }

    public void UpdateMiniMap(int rows, int columns)
    {

    }

    public void BaseMapChanged(int columns, int rows)
    {
        tmg.ForceRebuildTexture(columns, rows);
    }

    public void BuildNewTexture(int columns, int rows)
    {
        tmg.SetTileResolution(8);

        tmg.size_x = MapMasterScript.activeMap.columns;
        tmg.size_z = MapMasterScript.activeMap.rows;        
        tmg.BuildMesh();       

        //Grab the proper texture based on the state of the map
        myTexture = MinimapUIScript.MinimapState == MinimapStates.TRANSLUCENT ? tmg.GetTranslucentMinimapTexture() : tmg.GetTexture();

#if UNITY_SWITCH
        /* Sprite tempSprite = Sprite.Create(myTexture, new Rect(0.0f, 0.0f, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100.0f);
        myImage.sprite = tempSprite;
        myImage.color = new Color(1f, 1f, 1f, PlayerOptions.minimapOpacity); */

        myRI.texture = myTexture;
        myRI.gameObject.SetActive(true);
        myRI.enabled = true;
        //Debug.Log("Name of RI object is: " + myRI.gameObject.name + ", active? " + myRI.gameObject.activeSelf);
        myRI.color = new Color(1f, 1f, 1f,
            1f); // on Switch we must use 100% alpha for RawImage. For some reason.
#else
        myRI.texture = myTexture;
        myRI.color = new Color(1f, 1f, 1f,
            PlayerOptions.minimapOpacity);
#endif



        /* GameObject test = GameObject.Find("TestTestImage");
        test.GetComponent<RawImage>().texture = myTexture;
        test.GetComponent<RawImage>().color = Color.white;  */



    }

    public void UpdateColors(Color[] cArray)
    {
        if (!IsMiniMapActive())
        {
            return;
        }

        myTexture.SetPixels(cArray);
        ApplyTexture();
    }

    public void ApplyTexture()
    {
        myTexture.filterMode = FilterMode.Point;
        myTexture.wrapMode = TextureWrapMode.Clamp;
        myTexture.Apply();
        //mr.sharedMaterials[0].mainTexture = myTexture;
    }

    /// <summary>
    /// User clicks but does not *drag* the minimap.
    /// </summary>
    public void OnClick()
    {       
        //Debug.Log("Map size: " + myRectTransform.sizeDelta + " Local pos: " + transform.localPosition + " Global position: " + effectivePosition + " Clicked Position: " + Input.mousePosition + " mouse offset position: " + offsetMousePosition);
    }

    public void OnMouseEnter()
    {
        if (PlatformVariables.GAMEPAD_ONLY) return;
        if (Cursor.visible)
        {
            cursorIsHoveringOverMap = true;
            framesUntilUpdate = updateHoverFrames;
            preMiniMapHoverText = UIManagerScript.singletonUIMS.GetInfoText();
            preMiniMapHoverTextBuffer = UIManagerScript.bufferInfoBarText;
        }        
    }

    public void OnMouseExit()
    {
        if (PlatformVariables.GAMEPAD_ONLY) return;

        cursorIsHoveringOverMap = false;
        if (!string.IsNullOrEmpty(preMiniMapHoverTextBuffer)) UIManagerScript.bufferInfoBarText = preMiniMapHoverTextBuffer;
        if (!string.IsNullOrEmpty(preMiniMapHoverText)) UIManagerScript.singletonUIMS.SetInfoText(preMiniMapHoverText);
    }

    public MapTileData GetTileFromMousePosition()
    {
        // The center of the map is (0,0) due to how the anchors are set up, so we have to offset by half of its size.
        Vector3 effectivePosition = transform.position;

        float canvasScaleValue = Screen.width / 1920f; // to account for canvas scaler.

        // x and y will always be the same here.
        float actualMiniMapSize = myRT.sizeDelta.x * canvasScaleValue;

        effectivePosition.x -= actualMiniMapSize / 2f;
        effectivePosition.y -= actualMiniMapSize / 2f;

        // 0,0 is the lower left of the minimap
        Vector3 offsetMousePosition = Input.mousePosition - effectivePosition;

        int mapSize = MapMasterScript.activeMap.columns;
        float pixelsPerTile = actualMiniMapSize / mapSize;

        // Now we can find which tile was clicked!

        int tileX = (int)(offsetMousePosition.x / pixelsPerTile);
        int tileY = (int)(offsetMousePosition.y / pixelsPerTile);

        if (tileX < 0) tileX = 0;
        if (tileY < 0) tileY = 0;
        if (tileX >= MapMasterScript.activeMap.columns) tileX = MapMasterScript.activeMap.columns - 1;
        if (tileY >= MapMasterScript.activeMap.rows) tileX = MapMasterScript.activeMap.rows - 1;
        Vector2 tile = new Vector2(tileX, tileY);

        return MapMasterScript.GetTile(tile);        
    }

    public void Update()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;

        UpdateTranslucentMode();

        if (!cursorIsHoveringOverMap) return;

        framesUntilUpdate--;
        if (framesUntilUpdate != 0)
        {
            return;
        }
        framesUntilUpdate = updateHoverFrames;

        // Now let's try actually updating hover tile... but only if we have mouse.

        if (PlatformVariables.GAMEPAD_ONLY) return;

        MapTileData tile = GetTileFromMousePosition();

        if (tile == null) return;

        bool clearInfoBar = false;
        
        if (tile.iPos.x < 0 || tile.iPos.y < 0 || tile.iPos.x >= MapMasterScript.activeMap.exploredTiles.GetLength(0) 
            || tile.iPos.y >= MapMasterScript.activeMap.exploredTiles.GetLength(1))
        {
            return;
        }

        if (!MapMasterScript.activeMap.exploredTiles[tile.iPos.x, tile.iPos.y])
        {
            clearInfoBar = true;
        }
        else
        {
            Stairs st = tile.GetStairsInTile();

            if (st != null)
            {
                UIManagerScript.singletonUIMS.UpdateHoverBarTextWithInfoFromTile(tile, ignoreIfEmpty: true, overwriteBuffer: false);
            }
            else
            {
                clearInfoBar = true;
            }
        }
        

        if (clearInfoBar)
        {
            UIManagerScript.singletonUIMS.HideGenericInfoBar();
        }        
    }
	
    public void ResetMinimapDataInTMG()
    {
        tmg.ResetMinimapInfo();
    }

    
    private void OnDestroy()
    {
        DestroyTexturesAndCleanup();
    }
		
    public void DestroyTexturesAndCleanup()
    {
        if (myTexture != null)
        {
            Destroy(myTexture);
            myTexture = null;
        }
        tmg.DestroyTexturesAndCleanup();
    }	

    public Vector3 GetTransformPosition()
    {
        return myRT.transform.position;
    }

    public void SetTransformPosition(Vector3 v3)
    {
        myRT.transform.position = v3;
    }

    bool activeState = false;

    public bool IsMiniMapActive()
    {
        return activeState;

#if UNITY_SWITCH
        //if (Debug.isDebugBuild) Debug.Log("Minimap state is currently " + myRT.gameObject.activeSelf);
        return myRT.gameObject.activeSelf;
#else
        return gameObject.activeSelf;
#endif
    }

    public void ToggleActive(bool state)
    {
        //if (Debug.isDebugBuild) Debug.Log("Setting minimap active state to " + state);
        myRT.gameObject.SetActive(state);
        activeState = state;
    }
}
