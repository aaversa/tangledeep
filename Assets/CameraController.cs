using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.ImageEffects;

public enum CameraScrollTypes { HERMITE, SINERP, COSERP, BERP, LERP, SLERP, SMOOTHDAMP, CLERP }
public enum CameraBGColors { STANDARD, SIDEAREAS, COUNT }

[System.Serializable]
public class CameraController : MonoBehaviour {

	//private Movable myHeroMovable;
	private GameObject gameMaster;
    private GameObject myLight;
    private PixelPerfectCamera pixelPerfectCamera;

    //When the screen is shaking, how many units from origin should it be allowed to move?
    //hint: keep this number small <3 
    public float MaxScreenshakeDeltaUnits;

    private float fShakeTime;
    //Camera shake shouldn't actually move the camera around the map.
    private Vector3 vPreshakePosition;

    public bool lockToPlayer;
    public bool cameraSmoothing;
    public static bool horizontalOnlyMovement = false;
    public CameraScrollTypes scrollType;

    public bool clampCamera = true;
    public float camUnlockMaxDistance;
    public float snapDivisor;

    public Grayscale[] grayScaleComponents;

    private static CameraController singletonCC;

	private Camera gameCamera;
    private Light lightSource;

    public float offsetX;
    public float offsetY;

    public float minXOffset;
    public float maxXOffset;
    public float minYOffset;
    public float maxYOffset;

    public int boardWidth = 0;
    public int boardHeight = 0;

    public float smoothTime;
    public const float BASE_SMOOTH_TIME = 0.27f; // 

    static float startCameraMoveTime;
    static Vector3 startPos;



    private static Vector2 velocity = Vector2.zero;
    private static Vector3 sNewPos;
    private static Vector3 prevPos = Vector3.zero;
    private static Vector3 targetPos;
    private static bool updateCameraPos;
    static Vector3 smoothingTracker;

    public float baseMaxTilesVertical;// = 38f; was for 1080 locked
    public float baseMaxTilesHorizontal;// = 62f;
    public float maxTilesVertical = 0;
    public float maxTilesHorizontal = 0;

    public Transform heroTransform;

    Vector3 lightPosition;

    // Used for pixel snapping
    static float percentComplete;
    static Vector3 rounded;
    static float cameraPPU;
    static float cameraUPP;
    static Vector2 offset;
    static float assetPPU;
    static float assetUPP;
    static float camPixelsPerAssetPixel;
    static Vector2 distance = Vector2.zero;
    static Vector2 roundedV2;

    public bool customAnimationPlaying;
    public bool haltCameraMovement;
    public bool revertGMSAnimationStateAtEnd;
    Vector2 customAnimStart;
    Vector2 customAnimFinish;
    float customAnimTime;
    float customAnimStartTime;

    float lightZDepth;

    float bufferSmoothTime;
    bool bufferingSmoothTime;

    bool cameraLockedExceptForCustomAnimation;

    //public static RenderTexture rt;

    public static void SetCameraMovementHaltState(bool state)
    {
        singletonCC.haltCameraMovement = state;
    }

    public static void WaitToResetSmoothTime(float value)
    {
        singletonCC.bufferingSmoothTime = true;
        singletonCC.bufferSmoothTime = value;
        //singletonCC.StartCoroutine(singletonCC.WaitThenResetSmoothTime(value));
    }

    void Awake ()
    {
        singletonCC = this;
        baseMaxTilesVertical = (Screen.height / 32) + 2;
        baseMaxTilesHorizontal = (Screen.width / 32)+2;
        gameMaster = GameObject.Find("GameMaster");
        gameCamera = gameObject.GetComponent<Camera>();
        myLight = GameObject.Find("BasicLight");
        lightSource = myLight.GetComponent<Light>();
        UpdateScanlinesFromOptionsValue();
        transform.localEulerAngles = Vector3.zero;
    }

    public void SetCustomCameraAnimation(Vector2 startPos, Vector2 finishPos, float animTime, bool endAnimationAtEnd = true)
    {
        customAnimationPlaying = true;
        revertGMSAnimationStateAtEnd = endAnimationAtEnd;
        customAnimStart = new Vector2(startPos.x + offsetX, startPos.y + offsetY);
        customAnimFinish = new Vector2(finishPos.x + offsetX, finishPos.y + offsetY);
        customAnimTime = animTime;
        customAnimStartTime = Time.time;
        lightZDepth = -6.8f;

        smoothingTracker = startPos; // New on launch day lol
    }

    public void StopAnimationFromPlaying()
    {
        customAnimationPlaying = false;
        if (revertGMSAnimationStateAtEnd)
        {
            GameMasterScript.SetAnimationPlaying(false);
        }
    }

    public void SetBGColor(CameraBGColors c)
    {
        switch(c)
        {
            case CameraBGColors.STANDARD:
                gameCamera.backgroundColor = Color.black;
                break;
            case CameraBGColors.SIDEAREAS:
                Color parsedC;
                ColorUtility.TryParseHtmlString("#13252EFF", out parsedC);
                gameCamera.backgroundColor = parsedC;
                break;
        }
        
    }

    public void WaitThenSetCustomCameraAnimation(Vector2 startPos, Vector2 finishPos, float animTime, float waitTime, bool endAnimationAtEnd = true, bool haltState = false)
    {
        StartCoroutine(WaitThenCCA(startPos, finishPos, animTime, waitTime, endAnimationAtEnd));
        haltCameraMovement = haltState;
    }

    public void WaitThenSetCustomCameraMovement(Vector2 finishPos, float animTime, float waitTime, bool endAnimationAfter = true)
    {
        startPos.x = transform.position.x - offsetX;
        startPos.y = transform.position.y - offsetY;

        StartCoroutine(WaitThenCCA(startPos, finishPos, animTime, waitTime, endAnimationAfter));
    }

    public void MoveCameraToPositionFromCurrentCameraPosition(Vector2 finishPos, float animTime, bool endAnimationAtEnd = true)
    {
        // The SetCCA function ADDS an offset, therefore we want to start by taking that offset into account
        startPos.x = transform.position.x - offsetX;
        startPos.y = transform.position.y - offsetY;
        //startPos = transform.position;
        SetCustomCameraAnimation(startPos, finishPos, animTime, endAnimationAtEnd);
    }

    IEnumerator WaitThenCCA(Vector2 startPos, Vector2 finishPos, float animTime, float waitTime, bool endAnimationAtEnd = true)
    {
        GameMasterScript.SetAnimationPlaying(true);
        yield return new WaitForSeconds(waitTime);
        SetCustomCameraAnimation(startPos, finishPos, animTime, endAnimationAtEnd);
    }

    public static void UpdateTileRanges()
    {
        singletonCC.maxTilesHorizontal = singletonCC.pixelPerfectCamera.nativeAssetResolution.x / 32f - 1f;
        singletonCC.maxTilesVertical = (singletonCC.pixelPerfectCamera.nativeAssetResolution.y / 32f) - 1f; // why is the -1 necessary???
    }

    public void SetToGrayscale(bool value)
    {
        for (int i = 0; i < grayScaleComponents.Length; i++)
        {
            grayScaleComponents[i].enabled = value;
        }
    }

    // Use this for initialization
    void Start () {
        pixelPerfectCamera = gameObject.GetComponent<PixelPerfectCamera>();
        /* rt = new RenderTexture(Screen.width, Screen.height, 16);
        rt.antiAliasing = 2;
        gameCamera.targetTexture = rt; */
    }

    public void CameraStart()
    {
        //myHero = GameObject.Find("HeroPC");
        //myHeroMovable = myHero.GetComponent<Movable>();

        //gameCamera.orthographicSize = baseOrthoSize/3f; // For 32 tiles per unit, this is 3x scale.
        UpdateLockToPlayerFromOptionsValue();
        UpdateCameraSmoothingFromOptionsValue();
        UpdateScanlinesFromOptionsValue();
        UpdateFOVFromOptionsValue();
    }

    // Update is called once per frame
    public void SnapPosition(Vector2 feedPos)
    {
        if (customAnimationPlaying) return;
        updateCameraPos = false;
        targetPos = feedPos;
        targetPos.z = -2f;
        velocity = Vector3.zero;
        feedPos.y += offsetY; 

        //Debug.Log("Snapping position to " + transform.position + " based on " + pos);
        feedPos = GetClampValue(feedPos); // New?
        //Debug.Log("Now pos is " + pos);

        smoothingTracker = new Vector3(feedPos.x, feedPos.y, -2f);
        roundedV2 = smoothingTracker;
        transform.position = PixelRound();

    }


    public void LeaveCameraInCurrentLocationUntilNextAnimation(bool state)
    {
        cameraLockedExceptForCustomAnimation = state;
    }

    static Vector3 PixelRound()
    {
		smoothingTracker.z = -2f;

        /* if ((Camera.current == null) || (singletonCC.cameraSmoothing))
        {
            roundedV2 = smoothingTracker;
            return smoothingTracker;
        } */

        rounded = smoothingTracker;
        cameraPPU = (float)Camera.main.pixelHeight / (2f * Camera.main.orthographicSize); // was current
        cameraUPP = 1.0f / cameraPPU;
        offset = new Vector2(0, 0);
        // offset for screen pixel edge if screen size is odd
        offset.x = (Camera.main.pixelWidth % 2 == 0) ? 0 : 0.5f; // was current
        offset.y = (Camera.main.pixelHeight % 2 == 0) ? 0 : 0.5f; // was current
        if (singletonCC.pixelPerfectCamera.retroSnap)
        {
            assetPPU = singletonCC.pixelPerfectCamera.assetsPixelsPerUnit;
            assetUPP = 1.0f / assetPPU;
            camPixelsPerAssetPixel = cameraPPU / singletonCC.snapDivisor;

            offset.x /= camPixelsPerAssetPixel; // zero or half a screen pixel in texture pixels
            offset.y /= camPixelsPerAssetPixel;
            rounded.x = (Mathf.Round(rounded.x / assetUPP - offset.x) + offset.x) * assetUPP;
            rounded.y = (Mathf.Round(rounded.y / assetUPP - offset.y) + offset.y) * assetUPP;
        }
        else
        {
            rounded.x = (Mathf.Round(smoothingTracker.x / cameraUPP - offset.x) + offset.x) * cameraUPP;
            rounded.y = (Mathf.Round(smoothingTracker.y / cameraUPP - offset.y) + offset.y) * cameraUPP;
        }

        rounded.z = -2f;
        roundedV2.x = rounded.x;
        roundedV2.y = rounded.y;
        //Debug.Log(Time.deltaTime + " " + smoothingTracker.x + "," + smoothingTracker.y + " " + rounded.x + "," + rounded.y);
        return rounded;
    }

    public static void UpdateCameraPosition(Vector2 pos, bool followingHero, float changeSmoothTime = 0f)
    {
        if (singletonCC.customAnimationPlaying) return;
        if (followingHero && !singletonCC.lockToPlayer) return;
        pos.y += singletonCC.offsetY;
        targetPos = pos;
        updateCameraPos = true;
        startCameraMoveTime = Time.fixedTime;
        startPos = singletonCC.gameObject.transform.position;
        startPos.y += singletonCC.offsetY; // new, is this needed?

        if (changeSmoothTime > 0f)
        {
            singletonCC.smoothTime = changeSmoothTime;            
        }
        else
        {
            singletonCC.smoothTime = BASE_SMOOTH_TIME;
        }

        //Debug.Log("Request update to " + pos + " from " + startPos + " " + singletonCC.gameObject.transform.position + " " + singletonCC.smoothTime);
    }

    void LateUpdate() {

        if (haltCameraMovement) return;
        if (customAnimationPlaying)
        {
            float customAnimPercentComplete = (Time.time - customAnimStartTime) / customAnimTime;
            if (customAnimPercentComplete > 1.0f)
            {
                customAnimPercentComplete = 1.0f;
            }
            Vector3 newPos = Vector2.Lerp(customAnimStart, customAnimFinish, customAnimPercentComplete);

            if (customAnimPercentComplete >= 1.0f)
            {
                StopAnimationFromPlaying();

                targetPos = transform.position;
                smoothingTracker = transform.position;
                //targetPos = newPos;
                //smoothingTracker = newPos;
            }

            newPos.z = -2f;

            smoothingTracker = newPos;

            newPos = PixelRound();

            transform.position = newPos;
            Vector3 lightPos = newPos;
            lightPos.z = lightZDepth;
            myLight.transform.position = lightPos;
            smoothingTracker = newPos;
            return;            
        }

        if (cameraLockedExceptForCustomAnimation)
        {
            return;
        }

        if (!GameMasterScript.actualGameStarted)
        {
            return;
        }

        if (heroTransform == null) return;

        lightPosition = myLight.transform.position;
        lightPosition.x = heroTransform.position.x;
        lightPosition.y = heroTransform.position.y;
        myLight.transform.position = lightPosition;

        //insert screenshake for great justice
        if (fShakeTime > 0f)
        {
            updateCameraPos = true;
        }

        // Follow hero movement

        if (updateCameraPos)
        {
            percentComplete = (Time.fixedTime - startCameraMoveTime) / smoothTime;

            switch (scrollType)
            {
                case CameraScrollTypes.SMOOTHDAMP:
                    if (smoothTime < 0.05f)
                    {
                        sNewPos = targetPos;
                    }
                    else
                    {
                        sNewPos = Vector2.SmoothDamp(smoothingTracker, targetPos, ref velocity, smoothTime, 999f, Time.deltaTime);
                    }                    
                    //sNewPos = Vector2.SmoothDamp(smoothingTracker, targetPos, ref velocity, smoothTime);
                    break;
                case CameraScrollTypes.SINERP:
                    sNewPos = Mathfx.Sinerp(smoothingTracker, targetPos, percentComplete);
                    break;
                case CameraScrollTypes.LERP:
                    sNewPos = Vector2.Lerp(smoothingTracker, targetPos, percentComplete);
                    break;
                case CameraScrollTypes.SLERP:
                    sNewPos = Vector3.Slerp(smoothingTracker, targetPos, percentComplete);
                    break;
                case CameraScrollTypes.BERP:
                    sNewPos = Mathfx.Berp(smoothingTracker, targetPos, percentComplete);
                    break;
                case CameraScrollTypes.COSERP:
                    sNewPos = Mathfx.Coserp(smoothingTracker, targetPos, percentComplete);
                    break;
                case CameraScrollTypes.HERMITE:
                    sNewPos = Mathfx.Hermite(smoothingTracker, targetPos, percentComplete);
                    break;
            }

            sNewPos.z = -2;
            SetCameraPosition(sNewPos);

            //Debug.Log(transform.position + " " + targetPos);

            if (percentComplete >= 1.0f || CustomAlgorithms.CompareFloats(transform.position.x, targetPos.x) && CustomAlgorithms.CompareFloats(transform.position.y, targetPos.y))
            {
                //updateCameraPos = false;
                if (bufferingSmoothTime)
                {
                    bufferingSmoothTime = false;
                    smoothTime = bufferSmoothTime;
                }
            }
        }
        /* newPos = Vector2.SmoothDamp(transform.position, heroPosition, ref velocity, smoothTime);            
        newPos.z = -2;
        transform.position = newPos; */

        /* if (!lockToPlayer)
        {
            checkLockFrames++;
            if (checkLockFrames == 1)
            {
                checkLockFrames = 0;
                float chk = Vector2.Distance(GameMasterScript.heroPCActor.GetPos(), roundedV2);
                if ((!updateCameraPos) && (chk >= camUnlockMaxDistance))
                {
                    float angle = CombatManagerScript.GetAngleBetweenPoints(GameMasterScript.heroPCActor.GetPos(), roundedV2);
                    Directions dir = MapMasterScript.oppositeDirections[(int)MapMasterScript.GetDirectionFromAngle(angle)];
                    Vector2 adjPos = roundedV2 + MapMasterScript.xDirections[(int)dir];
                    //Debug.Log("Drunk update to " + adjPos);
                    UpdateCameraPosition(adjPos, false);
                }
            }
            return;                    
        } */

    }

    public void AddScreenshake(float fTime)
    {
        LeaveCameraInCurrentLocationUntilNextAnimation(false); // Must be false if we are screenshaking, otherwise camera will not move.

        //If we are just starting this shake, make sure to save our core position.
        if (fShakeTime <= 0f)
        {
            vPreshakePosition = transform.position;
        }

        fShakeTime += fTime;
    }

    public void SetCameraPosition(Vector3 pos)
    {
        Vector3 vRoundedPosition;

        if (!clampCamera)
        {
            pos.y += offsetY; // Check this.
            smoothingTracker = pos;
            vRoundedPosition = PixelRound();         
        }
        else
        {
            Vector3 nPos = GetClampValue(pos);
            if (nPos == Vector3.zero) return;

            smoothingTracker = nPos;
            vRoundedPosition = PixelRound();
        }

        if (fShakeTime > 0f)
        {
            fShakeTime -= Time.deltaTime;

            //if we're ticking down shake, and Shake Is Over, go back to our set position
            if (fShakeTime <= 0f)
            {
                //do something here to reset position?
            }
            else
            {
                vRoundedPosition += (Vector3)Random.insideUnitCircle * MaxScreenshakeDeltaUnits;
            }
        }

        transform.position = vRoundedPosition;
        return;

    }

    public Vector3 GetClampValue(Vector3 pos)
    {
        pos.z = -2.0f;
        int extraValue = 0;
        int subtract = 0;

        if (MapMasterScript.activeMap.dungeonLevelData.GetMetaData("nocamerabounds") == 1)
        {
            return pos;
        }

        if (MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate != null && MapMasterScript.activeMap.dungeonLevelData.imageOverlay != null)
        {
            if (MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.visualRows != 0)
            {
                boardWidth = MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.visualColumns + 2;
                boardHeight = MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.visualRows + 2;
            }
            else
            {
                boardWidth = MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.numColumns + 2;
                boardHeight = MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.numRows + 2;
            }

            extraValue = 1;
            subtract = -2;
        }
        else
        {
            boardWidth = MapMasterScript.activeMap.columns;
            boardHeight = MapMasterScript.activeMap.rows;
        }

        float minXValue;
        float maxXValue;
        float minYValue;
        float maxYValue;

        minXValue = 0 + (maxTilesHorizontal / 2f) + minXOffset + extraValue;
        maxXValue = (boardWidth) - (maxTilesHorizontal / 2f) + maxXOffset + extraValue + subtract;
        minYValue = 0 + (maxTilesVertical / 2f) + minYOffset + extraValue;
        maxYValue = (boardHeight) - (maxTilesVertical / 2f) + maxYOffset + extraValue + subtract;

        bool noClamp = false;
        if (minXValue > maxXValue)
        {
            minXValue = maxXValue;
            noClamp = true;
        }
        if (minYValue > maxYValue)
        {
            minYValue = maxYValue;
            noClamp = true;
        }

        if ((noClamp) && (!MapMasterScript.activeMap.dungeonLevelData.HasSpecialClamp()))
        {
            minXValue = (boardWidth / 2f) + minXOffset + extraValue;
            maxXValue = (boardWidth / 2f) + maxXOffset + extraValue;
            minYValue = (boardHeight / 2f) + minYOffset + extraValue;
            maxYValue = (boardHeight / 2f) + maxYOffset + extraValue;

            // TODO: Find a better solution for clamping on small maps.

            
            smoothingTracker = pos;

            return pos;

            //transform.position = PixelRound();
            //return Vector3.zero;
        }
        else
        {
            // Regular clamp logic here.
            if (MapMasterScript.activeMap.dungeonLevelData.HasSpecialClamp())
            {
                minXValue += MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.clampMinX;
                maxXValue += MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.clampMaxX;
                minYValue += MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.clampMinY;
                maxYValue += MapMasterScript.activeMap.dungeonLevelData.specialRoomTemplate.clampMaxY;
            }
            else
            {
                minXValue = 0 + (maxTilesHorizontal / 2f) + minXOffset + extraValue;
                maxXValue = (boardWidth) - (maxTilesHorizontal / 2f) + maxXOffset + extraValue + subtract;
                minYValue = 0 + (maxTilesVertical / 2f) + minYOffset + extraValue;
                maxYValue = (boardHeight) - (maxTilesVertical / 2f) + maxYOffset + extraValue + subtract;
            }

            //if (!MapMasterScript.activeMap.dungeonLevelData.HasSpecialClamp())
            {
                if (minXValue > maxXValue)
                {
                    minXValue = maxXValue;
                }
                if (minYValue > maxYValue)
                {
                    minYValue = maxYValue;
                }
            }
        }

        //Debug.Log(minXValue + " " + maxXValue + " " + MapMasterScript.activeMap.dungeonLevelData.HasSpecialClamp() + " " + MapMasterScript.activeMap.floor + " " + pos.x);

        if (pos.x < minXValue)
        {
            pos.x = minXValue;
        }
        else if (pos.x > maxXValue)
        {
            pos.x = maxXValue;
        }
        if (pos.y < minYValue)
        {
            pos.y = minYValue;
        }
        else if (pos.y > maxYValue)
        {
            pos.y = maxYValue;
        }
        return pos;
    }

    public bool CheckScanlines()
    {
        return GetComponent<TVShader>().enabled;
    }

    public void UpdateScanlinesFromOptionsValue()
    {
        OLDTVTube otv = GetComponent<OLDTVTube>();
        otv.enabled = PlayerOptions.scanlines;
        if (otv.enabled)
        {
            int value = 0;
            if (pixelPerfectCamera == null) return;
            if (pixelPerfectCamera.nativeAssetResolution == Vector2.zero)
            {
                value = (int)(Screen.height / 2f);
            }
            else
            {
                value = (int)pixelPerfectCamera.nativeAssetResolution.y;
            }
            otv.scanlineCount = (int)pixelPerfectCamera.nativeAssetResolution.y;
        }
    }

    public void SetFOVInstant()
    {
        int sliderVal = PlayerOptions.zoomScale;

        if (pixelPerfectCamera == null)
        {
            pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        }

        SetFOV(sliderVal);

        SnapPosition(GameMasterScript.heroPCActor.GetPos());
    }

    void SetFOV(int sliderVal)
    {
        switch (sliderVal)
        {
            case 1:
                pixelPerfectCamera.targetCameraHalfHeight = 8f;
                break;
            case 2:
                pixelPerfectCamera.targetCameraHalfHeight = 7f;
                break;
            case 3:
                pixelPerfectCamera.targetCameraHalfHeight = 6f;
                break;
            case 4:
                pixelPerfectCamera.targetCameraHalfHeight = 5f;
                break;
            case 5:
                pixelPerfectCamera.targetCameraHalfHeight = 4f;
                break;
            case 6:
                pixelPerfectCamera.targetCameraHalfHeight = 2f;
                break;
        }

        pixelPerfectCamera.adjustCameraFOV();

        maxTilesHorizontal = pixelPerfectCamera.nativeAssetResolution.x / 32f - 1f;
        maxTilesVertical = (pixelPerfectCamera.nativeAssetResolution.y / 32f) - 1f; // why is the -1 necessary???
    }

    public void UpdateFOVFromOptionsValue()
    {
        int sliderVal = PlayerOptions.zoomScale;

        if (pixelPerfectCamera == null) {

        	pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        }

        SetFOV(sliderVal);

        if (GameMasterScript.actualGameStarted)
        {
            SetCameraPosition(GameMasterScript.heroPCActor.GetPos());
        }        
    }

    public static void UpdateLightPosition()
    {
        Vector3 pos = singletonCC.lightSource.transform.position;
        pos.x = singletonCC.transform.position.x;
        pos.y = singletonCC.transform.position.y;
        singletonCC.lightSource.transform.position = pos;
    }

    public bool CheckLockToPlayer()
    {
        return true;
        //return lockToPlayer;
    }

    public void ToggleLockToPlayer()
    {
        return;
        //PlayerOptions.lockCamera = !PlayerOptions.lockCamera;
        //UpdateLockToPlayerFromOptionsValue();
    }

    public void ToggleCameraSmoothing()
    {
        PlayerOptions.smoothCamera = !PlayerOptions.smoothCamera;
        UpdateCameraSmoothingFromOptionsValue();
    }

    public void UpdateLockToPlayerFromOptionsValue()
    {
        return;
        //lockToPlayer = PlayerOptions.lockCamera;
        //if (GameMasterScript.heroPCActor != null)
        //{
        //    UpdateCameraPosition(GameMasterScript.heroPCActor.GetPos(), true);
        //}        
    }

    public void UpdateCameraSmoothingFromOptionsValue()
    {
        cameraSmoothing = PlayerOptions.smoothCamera;

        scrollType = CameraScrollTypes.SMOOTHDAMP;
        smoothTime = 0.27f;
        return;
    }

}
