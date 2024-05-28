using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public enum MovementTypes { LERP, SMOOTH, SLERP, TOSS, COUNT }

[System.Serializable]
public class Movable : MonoBehaviour {

    Actor owner;

	public Vector3 position;
    private GameObject trackObject;

    public int sortOrderOffset;

    bool trackingObject;

    public float permanentYOffset;
    public float permanentXOffset;

    public float spriteHeight;
    public float spriteWidth;

    private bool moving;
    public bool fadingOut;
    public bool fadingIn;
    public bool debugRevealData;
    private bool arcMove;
    private bool arcMoveVertical;
    private bool arcMoveHorizontal;
    public bool startXFlippedAtRandom;

    //For toss motion, the Y draw value of the projectile will change 
    //based on a sine wave calculated by how complete the motion is.
    private float fTossHeight;

    //public bool collidable;
    public bool defaultCollidable;
    public bool remember;
    public int rememberTurns;
    public int turnsSinceLastSeen;
    public bool inSight;
    public Vector2 dirFacing;
    public bool destructible;

    private AudioStuff myAudioStuff;
    //private GameMasterScript gms;

    public List<AudioClip> footstepSounds;

    public lerpMoveData moveData;

    private Vector3 eulerAngles;
    private float startAlpha;
    private float finishAlpha;

    private bool jittering;
    private bool jitterLeft = true;
    Vector3 jitterPosition;
    float timeJitterStarted;
    const float jitterTime = 0.1f;

    bool shouldBeVisible = true;

    int visibilityFrames = 0;

    public int extraHeightTiles;
    public bool diagonalBlock; // Block vision NORTHEAST and NORTHWEST one tile?
    public bool diagonalLBlock;

    Vector3 centerPosition;

    Vector3 jabPosition;
    bool jabbing;
    bool jabOut;
    float jabTime;
    float timeJabStarted;
    //Vector2 jabStep;
    //Vector2 totalJab;
    //Vector2 maxJab;

    MapTileData south1;
    MapTileData south2;
    MapTileData southEast;
    MapTileData southSouthEast;
    MapTileData southWest;
    MapTileData southSouthWest;

    Queue<lerpMoveData> queuedMovements;

    public Dictionary<string, GameObject> particleSystems;
    bool particleSystemsSet;

    public Animatable fxAnimatableComponent; // used only for non-actor sprite effects, i.e. movables with no Owner
    public bool hasFXAnimatable;
    float cachedBaseOpacityValue; // some animations have a fixed opacity, we don't need to check for it constantly. cache it here

    //When flag is true, the SpriteRenderer on this object will be disabled
    //no matter what anyone else says. This class is the place to put this,
    //because nearly everything that draws in the game world has a moveable.
    private bool bAuthoritativeDoNotRender;

    public class lerpMoveData
    {
        public Vector3 startPosition;
        public Vector3 targetPosition;
        public Vector3 finalPosition;
        public float timeAtMoveStart;
        public float moveLength;
        public float rotation;
        public bool step;

        public string strAnimDuringMove;
        public Directions dirAnimFacingDuringMove;
        public float fAnimSpeedMultiplierDuringMove = 1.0f;
    }

    private Vector3 velocity = Vector3.zero;
    public SpriteRenderer sr;

    public bool srFound;

    private Color srColor;
    private float timeAtFadeStart;
    private float fFadeDuration;
    bool visible;
    private MovementTypes curMoveType;
    public bool spriteEffect;

    // Pooling
    List<OverlayData> intermittent;
    float calcTime;
    Vector3 v3;
    float timeSinceStarted;
    float percentComplete;
    float lerp;
    // End pooling

    public float rotateStatusOverlayCounter;
    public int statusOverlayIndex;

    private bool dieAfterFadeout = false;

    public Color forceColor;
    public bool usePublicForceColor;
    public bool terrainTile;
    public bool transparentStairs;
    public bool voidTile;
    public bool laserTile;
    const int VOID_UPDATE_PER_FRAMES = 2;
    int framesUntilUpdate = 0;

    HealthBarScript healthBar;
    bool hasHealthBar;
    bool healthBarFound;

    public bool overlappingTransparentObject;
    bool actorInitialized; // was initialization handled by a spawn actor? If not, follow Awake() initialization for sprite FX
    bool genericInitialized; // has our List<> been set up?

    public void SetColor(Color c)
    {
        if (usePublicForceColor) return;
        forceColor = c;
    }

    void OnEnable ()
    {
    	shouldBeVisible = true;
    }

    public void SetBVisible(bool value)
    {
        visible = value;
    }

    public void SetBShouldBeVisible(bool value)
    {
        shouldBeVisible = value;
    }

    public bool GetShouldBeVisible()
    {
        return shouldBeVisible;
    }

    IEnumerator Internal_WaitThenChangeSortingOrder(float time, int amount)
    {
        yield return new WaitForSeconds(time);
        if (sr != null)
        {
            sr.sortingOrder += amount;
        }
    }

    public void WaitThenChangeSortingOrder(float time, int amount)
    {
        StartCoroutine(Internal_WaitThenChangeSortingOrder(time, amount));
    }

    public void IncreaseSortingOrder()
    {
        if (sr != null)
        {
            sr.sortingOrder++;
        }
    }

    IEnumerator _WaitThenFlip(float time)
    {
        yield return new WaitForSeconds(time);
        sr.flipX = !sr.flipX;
    }

    public void WaitThenFlip(float time)
    {
        StartCoroutine(_WaitThenFlip(time));
    }

    public void AttachParticleSystem(string refName, GameObject go)
    {
        if (!particleSystemsSet)
        {
            return;
        }
        if (particleSystems.ContainsKey(refName)) return;
        particleSystems.Add(refName, go);
    }

    public void RemoveParticleSystem(string refName)
    {
        GameObject pSystem;        
        if (particleSystems.TryGetValue(refName, out pSystem))
        {            
            particleSystems.Remove(refName);
            if (pSystem.gameObject == null) return;
            GameMasterScript.ReturnToStack(pSystem, pSystem.gameObject.name.Replace("(Clone)", String.Empty));
        }
    }

    IEnumerator WaitThenUpdateObjectRotation(float time, Directions dir)
    {
        yield return new WaitForSeconds(time);
        UpdateObjectRotation(0f, dir);
    }

    public void UpdateObjectRotation(float updateTime, Directions dir)
    {
        if (gameObject == null) return;
        if (!gameObject.activeSelf) return;
        if (updateTime == 0f)
        {
            CustomAlgorithms.RotateGameObject(gameObject, dir);
        }
        else
        {
            StartCoroutine(WaitThenUpdateObjectRotation(updateTime, dir));
        }
    }


    public void Jitter(float amount)
    {
        if (moving) return;
        if ((jittering) || (jabbing))
        {
            return;
        }

        centerPosition = transform.position;
        jitterPosition.x = centerPosition.x - amount;
        jitterPosition.y = centerPosition.y;
        timeJitterStarted = Time.fixedTime;
        jittering = true;
        jitterLeft = true;
    }

    public void Jab(Directions dir) {
    	if ((jittering) || (jabbing)) return;
        if (moving) return;

        centerPosition = transform.position;
        jabPosition.x = centerPosition.x + (MapMasterScript.xDirections[(int)dir].x / 4f);
        jabPosition.y = centerPosition.y + (MapMasterScript.xDirections[(int)dir].y / 4f);

        jabbing = true;
        timeJabStarted = Time.fixedTime;
    	jabOut = true;    	
        jabTime = 0.09f;
    }

    // Use this for initialization
    void Start()
    {
        if (GetComponent<AudioStuff>() != null)
        {
            myAudioStuff = GetComponent<AudioStuff>();
        }
        if (!srFound)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                srColor = sr.color;
                srFound = true;
            }
        }
        if (!spriteEffect)
        {
            if (GetComponent<SpriteEffect>() != null)
            {
                spriteEffect = true;
            }
        }
        if (owner != null)
        {
            if (owner.GetActorType() == ActorTypes.MONSTER)
            {
                healthBar = GetComponentInChildren<HealthBarScript>();
                hasHealthBar = true;
                if (healthBar != null)
                {
                    healthBarFound = true;
                    healthBar.parentSR = owner.GetObject().GetComponent<SpriteRenderer>();
                }
            }
        }
    }

    // This is run when actors set their movable object.
    public void Initialize()
    {
        if (!srFound)
        {
            sr = GetComponent<SpriteRenderer>();
        }        
        if (sr != null)
        {
            srFound = true;
            srColor = sr.color;
        }
        ResetToDefaults(Vector2.zero);
        visible = true;
        shouldBeVisible = true;
        actorInitialized = true;        
    }
    void Awake()
    {
        // This should not be run on actors, since we might have already set Visible and ShouldBeVisible elsewhere
        // ResetToDefaults will reset all that at some undetermined point (due to Awake being asynchronous)
        // Initialize() is manually run on actors, which handles all the default setting stuff anyway.
        if (actorInitialized) return;
        particleSystems = new Dictionary<string, GameObject>();
        particleSystemsSet = true;
        ResetToDefaults(Vector2.zero);
    }

    public void ResetToDefaults(Vector2 pos)
    {       
        if (queuedMovements == null)
        {
            queuedMovements = new Queue<lerpMoveData>();
        }
        else
        {
            queuedMovements.Clear();
        }
        
        if (!usePublicForceColor)
        {
            forceColor = Color.white;
        }

        moving = false;
        fadingIn = false;
        fadingOut = false;
        dieAfterFadeout = false;
        visible = false;        
        shouldBeVisible = false;
        moveData = new lerpMoveData();
        trackObject = null;
        trackingObject = false;
        jabbing = false;
        eulerAngles = new Vector3(0, 0, 0);
        position = pos;
        genericInitialized = true;
    }

    float VectorDifference(Vector3 vec1, Vector3 vec2)
    {
        float xDiff = Mathf.Abs(vec1.x - vec2.x);
        float yDiff = Mathf.Abs(vec1.y - vec2.y);
        return (xDiff + yDiff);
    }

    void RoundV3ToNearestPixels()
    {
        
    }

    // Deprecated
    /* void UpdateHealthbar(float alpha)
    {
        if (!srFound) return;
        if (!sr.enabled) return;
        if (hasHealthBar) { 
            if (!healthBarFound)
            {
                healthBar = GetComponentInChildren<HealthBarScript>();
                if (healthBar != null)
                {
                    healthBar.SetAlpha(alpha);
                    healthBarFound = true;
                }
                else
                {
                    // Couldn't find healthbar for some reason...? Stop looking every frame.
                    hasHealthBar = false;
                }
            }            

            
        }
    } */

    // -1: unknown
    //  0: known, and we should not be quick-out
    //  1: known, and we are a terrainTile that isn't a LaserTile, so quick-out 
    private int quickyTerrainTileEarlyOutStatus = -1;
    void UpdateMovable()
    {
        if (quickyTerrainTileEarlyOutStatus < 0)
        {
            if (terrainTile && !laserTile)
                quickyTerrainTileEarlyOutStatus = 1;
            else
                quickyTerrainTileEarlyOutStatus = 0;
        }
        if (quickyTerrainTileEarlyOutStatus == 1)
        {
            return;
        }
        if (!genericInitialized) return;

        if (GameMasterScript.playerDied) return;

        if (!MapMasterScript.mapLoaded) return;

        if (!srFound) return;

        // Terrain tiles do not need this color check.
        if (!fadingIn && !fadingOut && (!terrainTile || voidTile))
        {
            forceColor.a = sr.color.a;
            sr.color = forceColor;
        }

        bool ownerIsNull = owner == null;
        bool ownerIsHero = false;
        if (!ownerIsNull && owner.GetActorType() == ActorTypes.HERO)
        {
            ownerIsHero = true;
        }
        // What was the purpose of this line?
        if (!terrainTile && ownerIsNull && !dieAfterFadeout && !spriteEffect) return;       

        if (!ownerIsNull && !owner.actorEnabled)
        {
            EnableRenderer(false);
            return;
        }

        visibilityFrames++;

        if (visibilityFrames > 5)
        {
            visibilityFrames = 0;
            if (!fadingIn && !fadingOut && !spriteEffect)
            {
                if (shouldBeVisible && visible)
                {
                    EnableRenderer(true);
                }
                else if (!shouldBeVisible && !visible)
                {                    
                    EnableRenderer(false);
                }
            }
        }

		/* If there's some issue where the hero isn't idling correctly, it might be because there was code 
		here that forced her to idle if some five conditions were met. But I removed them from the Switch build
		and everything seemed fine. */

        if (trackingObject)
        {
            if (trackObject != null && trackObject.activeSelf)
            {
                moveData.targetPosition = trackObject.transform.position;
                moveData.finalPosition = trackObject.transform.position;
            }
            else
            {
                trackingObject = false;
            }
        }
        if (!ownerIsNull)
        {
            if (ownerIsHero && !moving)
            {
                if (owner.myAnimatable != null && !owner.myAnimatable.hitOrAttackState)
                {
                    owner.myAnimatable.SetAnimDirectional("Idle", owner.lastMovedDirection, owner.lastCardinalDirection);
                }
            }
            if (owner.GetActorType() == ActorTypes.MONSTER && !moving)
            {
                transform.rotation = Quaternion.identity;
            }

            //get any intermittent overlays and do stuff with them
            intermittent = owner.overlays;

            /*
            if (owner.overlays != null && owner.overlays.Any(o => !o.alwaysDisplay)) 
            {
                intermittent = owner.overlays.Where(o => !o.alwaysDisplay).ToList();
            }
            */

            //if we have any overlays that are NOT set to alwaysDisplay, then...
            if (intermittent != null && intermittent.Count > 0)
            {
                //we want statusOverlayIndex to be the very last one in our list
                //that doesn't have o.alwaysDisplay on it.
                statusOverlayIndex = intermittent.Count - 1;
                while (statusOverlayIndex >= 0 &&
                       (intermittent[statusOverlayIndex] == null ||
                       intermittent[statusOverlayIndex].alwaysDisplay))
                {
                    //Starting at the ass end of the list,
                    //grab the first one we see that doesn't .alwaysDisplay
                    statusOverlayIndex--;
                }

                //if statusOverlayIndex < 0, we have nothing, good day sir.
                if (statusOverlayIndex >= 0 &&
                    intermittent[statusOverlayIndex].overlayGO.gameObject != null && 
                    intermittent[statusOverlayIndex].overlayGO.activeSelf)
                {
                    rotateStatusOverlayCounter += Time.deltaTime;

                    Animatable anim = intermittent[statusOverlayIndex].overlayGO.GetComponent<Animatable>();
                    if (anim != null)
                    {
                        calcTime = anim.calcGameWaitTime;

                        if (rotateStatusOverlayCounter >= calcTime)
                        {
                            owner.SetOverlaysCurVisibility(false);
                            rotateStatusOverlayCounter = 0.0f;
                            statusOverlayIndex++;

                            if (statusOverlayIndex >= intermittent.Count)
                            {
                                statusOverlayIndex = 0;
                            }

                            var go = intermittent[statusOverlayIndex].overlayGO;

                            if (go == null)
                            {
                                //if (Debug.isDebugBuild) Debug.Log("Cannot update status in " + statusOverlayIndex + " " + gameObject.name);
                                intermittent.RemoveAt(statusOverlayIndex);
                            }
                            else
                            {
                                var se = go.GetComponent<SpriteEffect>();
                                if (se != null)
                                {
                                    se.SetCurVisible(true);
                                }
                            }
                        }
                    }
                }

            }
        }
        if (jittering)
        {
            percentComplete = (Time.fixedTime - timeJitterStarted) / jitterTime;
            v3 = transform.position;
            if (jitterLeft)
            {
                v3 = Vector3.Lerp(centerPosition, jitterPosition, percentComplete);
                if (percentComplete >= 1f)
                {
                    jitterLeft = false;
                    timeJitterStarted = Time.fixedTime;
                }
            }
            else
            {
                v3 = Vector3.Lerp(jitterPosition, centerPosition, percentComplete);
                if (percentComplete >= 1f)
                {
                    jittering = false;
                }
            }
            if (v3 != Vector3.zero && !float.IsNaN(v3.x))
            {
                //v3.y += permanentYOffset; 
                SetTransformPosition(v3);
                }
            }

        if (jabbing)
        {
            percentComplete = (Time.fixedTime - timeJabStarted) / jabTime;
            v3 = transform.position;
            if (jabOut)
            {
                v3 = Vector3.Lerp(centerPosition, jabPosition, percentComplete);
                if (percentComplete >= 1f)
                {
                    jabOut = false;
                    timeJabStarted = Time.fixedTime;
                }
            }
            else
            {
                v3 = Vector3.Lerp(jabPosition, centerPosition, percentComplete);
                if (percentComplete >= 1.0f)
                {
                    jabbing = false;
                }
            }
            if (v3 != Vector3.zero)
            {
                //v3.y += permanentYOffset;
                SetTransformPosition(v3);
            }
        }
        if (moving)
        {
            if (debugRevealData)
            {
                Debug.Log("In movement: " + transform.position + " " + moveData.targetPosition + " " + curMoveType);
            }
            if (curMoveType == MovementTypes.SMOOTH)
            {
                SetTransformPosition(Vector3.SmoothDamp(transform.position, moveData.targetPosition, ref velocity, moveData.moveLength));
            }
            // Object has animated movement - if below is true, movement is finished
            if (LerpPosition())
            {
                if (arcMove)
                {
                    AnimateSetPosition(moveData.finalPosition, moveData.moveLength, false, moveData.rotation, 0.0f, curMoveType);
                }

                moving = false;
                trackingObject = false;
                if (ownerIsHero 
                    && GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.SHARA
                    && GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.MIRAISHARA)
                {
                    owner.myAnimatable.SetAnimDirectional("Idle",owner.lastMovedDirection,owner.lastCardinalDirection);
                }

                // NEW - Check for transparency code.
                overlappingTransparentObject = false;
                if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.floor == MapMasterScript.FINAL_BOSS_FLOOR2)
                {                    
                    overlappingTransparentObject = true;
                    if (!ownerIsNull && owner.GetActorType() == ActorTypes.MONSTER)
                    {
                        Monster tMon = owner as Monster;
                        if (tMon.isBoss && tMon.actorRefName == "mon_finalboss2")
                        {
                            overlappingTransparentObject = false;
                        }
                    }
                }
                Vector2 localPos = Vector2.zero;
                localPos.x = Mathf.Round(position.x);
                localPos.y = Mathf.Round(position.y);
                if (localPos.y >= 2f && !overlappingTransparentObject)
                {
                    south1 = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x, localPos.y - 1f));

                    if (south1 != null && south1.CheckHasExtraHeight(1))
                    {
                        overlappingTransparentObject = true;
                    }
                }
                if (localPos.x >= 2f && !overlappingTransparentObject)
                {
                    southWest = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x -1f, localPos.y - 1f));
                    if (southWest != null && southWest.CheckDiagonalBlock())
                    {
                        overlappingTransparentObject = true;
                    }
                }
                if (localPos.x < MapMasterScript.activeMap.columns-2 && !overlappingTransparentObject)
                {
                    southEast = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x + 1f, localPos.y - 1f));
                    if (southEast != null && southEast.CheckDiagonalBlock())
                    {
                        overlappingTransparentObject = true;
                    }
                }
                if (localPos.y >= 3f && !overlappingTransparentObject)
                {
                    south2 = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x, localPos.y - 2f));
                    if (south2 != null && south2.CheckHasExtraHeight(2))
                    {
                        overlappingTransparentObject = true;
                    }
                    if (localPos.x >= 2f)
                    {
                        southSouthWest = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x -1f, localPos.y - 2f));
                        if (southSouthWest != null && southSouthWest.CheckDiagonalLBlock())
                        {
                            overlappingTransparentObject = true;
                        }
                    }
                    if (localPos.x < MapMasterScript.activeMap.columns-2)
                    {
                        southSouthEast = MapMasterScript.activeMap.GetTile(new Vector2(localPos.x + 1f, localPos.y - 2f));
                        if (southSouthEast != null && southSouthEast.CheckDiagonalLBlock())
                        {
                            overlappingTransparentObject = true;
                        }
                    }
                }
                // Movement is completed.
                if (queuedMovements.Count > 0)
                {                    
                    lerpMoveData lmd = queuedMovements.Dequeue();
                    AnimateSetPosition(lmd.finalPosition, lmd.moveLength, lmd.step, lmd.rotation, 0f, MovementTypes.LERP);

                    if (!string.IsNullOrEmpty(lmd.strAnimDuringMove))
                    {
                        owner.myAnimatable.SetAnimDirectional(lmd.strAnimDuringMove,lmd.dirAnimFacingDuringMove, Directions.NEUTRAL);
                        owner.myAnimatable.speedMultiplier = lmd.fAnimSpeedMultiplierDuringMove;
                    }

                }


                // 1/24/18 Andrew you dumbass. WHY. WHY. WHY is the movable component calling this check.
                // Something that has an indeterminate movement length
                // that isn't supposed to be messing with game logic
                // Is calling something
                // That affects game logic of other actors
                // Just
                // why
                // you
                // moron
                // Signed
                // Andrew
                //If we are done moving, then check for stacked actors
                /* else if( owner != null )
                { 
                    // #todo - Make this not a null check but a bool flag? for cpu?
                    if (owner != null)
                    {
                        MapMasterScript.activeMap.CheckForStackedActors(owner.GetPos(), owner, true);
                    }
                    
                } */
            }
            if (curMoveType == MovementTypes.SMOOTH)
            {
                if (VectorDifference(transform.position, moveData.targetPosition) <= 0.01f) // Was 
                {
                    moving = false;
                    trackingObject = false;
                    velocity = Vector3.zero;
                    SetTransformPosition(moveData.targetPosition);
                    if (ownerIsHero)
                    {
                        owner.myAnimatable.SetAnimIfStopped("Idle");
                    }
                }
            }


        }
        if (fadingOut || fadingIn)
        {
            if (fadingOut)
            {
                if (shouldBeVisible && !dieAfterFadeout)
                {
                    EnableRenderer(true);
                    srColor.a = 1.0f;
                    fadingOut = false;
                    return;
                }
            }
            if (fadingIn)
            {
                // Experimental - prevent monsters from being in LOS when they're not supposed to be
                // Previously checked !visible also, but we're trying something new 5/2/17
                if (!shouldBeVisible && sr.enabled)
                {
                    EnableRenderer(false);
                    srColor.a = 0.0f;
                    fadingIn = false;
                    return;
                }
            }

            timeSinceStarted = Time.time - timeAtFadeStart;
            percentComplete = timeSinceStarted / fFadeDuration;
            lerp = Mathf.Lerp(startAlpha, finishAlpha, percentComplete);
            if (usePublicForceColor)
            {
                srColor = forceColor;
            }
            else
            {
                // Experimental 5/30/2018 to prevent sprites from flashing to black.
                // 6/8/2018 I realize that this will make semi-transparent animations flash to 1.0f alpha
                // Therefore we need to calculate the "full opacity" color if there is an animatable
                srColor = Color.white;
                srColor.a = GetMaxAlpha();
            }
            srColor.a = lerp;
            sr.color = srColor;
            if (percentComplete >= 1.0f)
            {
                // Verify final position.

                if (fadingOut)
                {
                    EnableRenderer(false);
                    visible = false;
                    if (dieAfterFadeout)
                    {
                        ReturnToStack();
                    }
                }
                else
                {
                    visible = true;
                }
                fadingIn = false;
                fadingOut = false;
            }
        }
        else if (!fadingIn && !fadingOut)
        {
            // This really shouldn't be needed. Before this checked visible and not, but we should go by "Should Be Visible"? Maybe?
            if (shouldBeVisible && !sr.enabled)
            {
                EnableRenderer(true);
            }
        }

    }

    void ReturnToStack()
    {
        quickyTerrainTileEarlyOutStatus = -1;
        // Sprite effect must use a slightly different method
        SpriteEffect se = GetComponent<SpriteEffect>();
        if (se != null)
        {
            GameMasterScript.ReturnToStack(gameObject, se.refName);
            return;
        }

        // Below assumes an actor is dying, not an effect.
        if (!GameMasterScript.TryReturnChildrenToStack(gameObject))
        {
            //Destroy(gameObject); 
            // No more destroying actors! Return them to stack!
            if (owner != null)
            {
                // It's possible that the listed prefab is not the same as the GameObject used...
                // For example, obj_dangersquare has a listed prefab of EnemyTargeting, but it *could* spawn PlayerTargeting
                // A monster in a Costume Party has a listed prefab and then a totally different prefab elsewhere
                // So, just use the gameobject name.
                GameMasterScript.ReturnActorObjectToStack(owner, gameObject, gameObject.name.Replace("(Clone)", String.Empty));
            }
            else
            {
                GameMasterScript.ReturnToStack(gameObject, gameObject.name.Replace("(Clone)", String.Empty));
            }
        }
        else
        {
            GameMasterScript.ReturnToStack(gameObject, gameObject.name.Replace("(Clone)",String.Empty));
        }
    }

    public void DieAfterSeconds(float fSeconds)
    {
        if (gameObject.activeSelf)
        {
            StartCoroutine(DieAfterSeconds_Internal(fSeconds));
        }        
    }

    IEnumerator DieAfterSeconds_Internal(float fSeconds)
    {
        yield return new WaitForSeconds(fSeconds);
        ReturnToStack();
    }

	public void CheckTransparencyBelow()
    {
        if (!MapMasterScript.mapLoaded) return;

        if (GameMasterScript.gameLoadSequenceCompleted && MapMasterScript.activeMap.floor == MapMasterScript.FINAL_BOSS_FLOOR2)
        {
            // In the final battle, due to Shara's size and everything's craziness, always show transparencies where possible
            overlappingTransparentObject = true;
            if (owner != null && owner.GetActorType() == ActorTypes.MONSTER)
            {
                Monster tMon = owner as Monster;
                if (tMon.isBoss && tMon.actorRefName == "mon_finalboss2")
                {
                    overlappingTransparentObject = false;
                }
            }            
            return;
        }

        if (position.y >= 2f)
        {
            south1 = MapMasterScript.activeMap.GetTile(new Vector2(position.x, position.y - 1f));
            if (south1.CheckHasExtraHeight(1))
            {
                overlappingTransparentObject = true;
                return;
            }
        }
        if (position.y >= 3f)
        {
            south2 = MapMasterScript.activeMap.GetTile(new Vector2(position.x, position.y - 2f));
            if (south2.CheckHasExtraHeight(2))
            {
                overlappingTransparentObject = true;
                return;
            }

            if (position.x >= 2f)
            {
                southSouthWest = MapMasterScript.activeMap.GetTile(new Vector2(position.x - 1f, position.y - 2f));
                if (southSouthWest.CheckDiagonalLBlock())
                {
                    overlappingTransparentObject = true;
                    return;
                }
            }
            if (position.x < MapMasterScript.activeMap.columns - 2)
            {
                southSouthEast = MapMasterScript.activeMap.GetTile(new Vector2(position.x + 1f, position.y - 2f));
                if (southSouthEast.CheckDiagonalLBlock())
                {
                    overlappingTransparentObject = true;
                    return;
                }
            }

        }
        if (position.x >= 2f)
        {
            southWest = MapMasterScript.activeMap.GetTile(new Vector2(position.x - 1f, position.y - 1f));
            if (southWest.CheckDiagonalBlock())
            {
                overlappingTransparentObject = true;
                return;
            }
        }
        if (position.x < MapMasterScript.activeMap.columns - 2)
        {
            southEast = MapMasterScript.activeMap.GetTile(new Vector2(position.x + 1f, position.y - 1f));
            if (southEast.CheckDiagonalBlock())
            {
                overlappingTransparentObject = true;
                return;
            }
        }

        overlappingTransparentObject = false;
    }

	void Update() {
	if (transparentStairs) 
	{
		return;
	}

        if (voidTile && framesUntilUpdate > 0)
        {
            framesUntilUpdate--;
            return;
            }
            else
            {
            framesUntilUpdate = VOID_UPDATE_PER_FRAMES;
            }            

        if (gameObject == null) { return; }

	    UpdateMovable(); 
    }

    void LateUpdate()
    {
        //Make sure we don't render if we don't want to,
        //No matter what anyone else says
        if (bAuthoritativeDoNotRender)
        {
            EnableRenderer(false);
        }
    }

    public void EnableRenderer(bool value)
    {
        if (srFound)
        {
            sr.enabled = value;
            /* if (!value)
            {
                Debug.Log(gameObject.name + " turned off. " + visibilityFrames + " " + visible + " " + shouldBeVisible);
            }  */
        }        
    }

    public void FadeIn(float fFadeTime = -1.0f)
    {
        if (!srFound) return;
        EnableRenderer(true);
        shouldBeVisible = true;
        if (fadingIn) return;
        if (sr.color.a == 1.0f) return;        
        if (sr.color.a == 1.0f || fadingIn)
        {
            EnableRenderer(true);
            return;
        }

        // Redundant since we already enabled the renderer above!
        /* if (srFound)
        {
            if (GetComponent<SpriteEffect>() != null)
            {
                if (GetComponent<SpriteEffect>().GetCurVisible())
                {
                    EnableRenderer(true);
                }
            }
            else
            {
                EnableRenderer(true);
            }            
        }    */     

        if (GameMasterScript.actualGameStarted)
        {
            fadingIn = true;
            fadingOut = false;
            startAlpha = sr.color.a;
            finishAlpha = 1.0f;
            timeAtFadeStart = Time.time;

            //Allow for a dynamic time setting, but if we don't set one use the
            //default global variable
            fFadeDuration = fFadeTime;
            if (fFadeDuration <= 0f)
            {
                fFadeDuration = GameMasterScript.gmsSingleton.visionFadeTime;
            }

        }

    }
    public void FadeOutThenDie()
    {
        Animatable anim = GetComponent<Animatable>();
        if (anim != null)
        {
            anim.StopAnimation();
        }
        ForceFadeOut();
        dieAfterFadeout = true;
        if( particleSystems != null )
        {
            foreach (GameObject go in particleSystems.Values)
            {
                GameMasterScript.ReturnToStack(go, go.name.Replace("(Clone)", String.Empty));
            }
        }

        var mn = owner as Monster;
        if (mn != null && mn.elemAuraObject != null)
        {
            GameMasterScript.ReturnToStack(mn.elemAuraObject.gameObject, "ElementalAura");
        }

        // DirectionalIndicators and ChargingSkillParticles are getting destroyed, oh no!
        foreach(Transform child in gameObject.transform)
        {
            string name = child.name.Replace("(Clone)","");
            if (name.Contains("DirectionalIndicator") || name.Contains("ChargingSkillParticles"))
            {
                // Hey, don't get rid of me!
                GameMasterScript.ReturnToStack(child.gameObject, name);
            }
        }
        
    }

    public void ForceFadeOut(float fFadeTime = -1.0f)
    {
        fadingOut = true;
        shouldBeVisible = false;
        if (srFound)
        {
            startAlpha = sr.color.a;
        }        
        finishAlpha = 0.0f;
        timeAtFadeStart = Time.time;

        //Allow for a dynamic time setting, but if we don't set one use the
        //default global variable
        fFadeDuration = fFadeTime;
        if (fFadeDuration <= 0f)
        {
            fFadeDuration = GameMasterScript.gmsSingleton.visionFadeTime;
        }
    }

    public void FadeOut( float fFadeTime = -1.0f)
    {
        if (fadingOut) return;
        if (!srFound) return;
        if (sr.color.a == 0f) return;
        if (!sr.enabled) return;
        if (shouldBeVisible)
        {
            // This is *great* code.
            FadeIn( fFadeTime );
            return;
        }

        if (sr.color.a == 0.0f || fadingOut)
        {
            return;
        }
        if (GameMasterScript.actualGameStarted)
        {
            fadingIn = false;
            fadingOut = true;
            startAlpha = sr.color.a;
            finishAlpha = 0.0f;
            timeAtFadeStart = Time.time;

            //Allow for a dynamic time setting, but if we don't set one use the
            //default global variable
            fFadeDuration = fFadeTime;
            if (fFadeDuration <= 0f)
            {
                fFadeDuration = GameMasterScript.gmsSingleton.visionFadeTime;
            }
        }
        else
        {
            if (srFound)
            {
                shouldBeVisible = false;
                EnableRenderer(false);
            }            
        }

    }

    public bool GetInSight()
    {
        return inSight;
    }

    public void SetInSightAndSnapEnable(bool value)
    {
        if (GameMasterScript.actualGameStarted && owner != null && !owner.actorEnabled) return;

        inSight = value;
        shouldBeVisible = value;
        visible = value; //Is this necessary?
        if (sr != null)
        {
            EnableRenderer(value);
            Color toUse = Color.white;
            toUse.a = GetMaxAlpha();
            sr.color = toUse;
        }
        if (owner != null)
        {
            owner.UpdateSpriteOrder();
        }        
        //Debug.Log("Turning " + gameObject.name + " " + value);
    }

    public void SetInSightAndForceFade(bool value)
    {
        if (GameMasterScript.actualGameStarted && owner != null && !owner.actorEnabled) return;

        SetInSightAndFade(value);
    }

    public void SetInSightAndFade(bool value)
    {
        if (GameMasterScript.actualGameStarted && owner != null && !owner.actorEnabled) return;

        inSight = value;
        shouldBeVisible = value;
        if (value)
        {
            EnableRenderer(true);
            FadeIn();
        }
        else
        {
            FadeOut();
        }
    }

    public void SetInSight(bool value)
    {
        inSight = value;
        shouldBeVisible = value;
    }

    public void SetTurnsSinceLastSeen(int amt)
    {
        turnsSinceLastSeen = amt;
    }

    public int GetTurnsSinceLastSeen()
    {
        return turnsSinceLastSeen;
    }

    public bool GetRemember()
    {
        if (rememberTurns <= 0)
        {
            return remember;
        }
        else
        {
            if (turnsSinceLastSeen >= rememberTurns)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public int GetRememberTurns()
    {
        return rememberTurns;
    }

    /* public bool IsCollidable()
    {
        return collidable;
    } */

    private bool LerpPosition()
    {
        // Returns true if finished lerping
        float timeSinceStarted = Time.time - moveData.timeAtMoveStart;
        if (moveData.moveLength == 0) {
        	moveData.moveLength = 0.1f;
        }
        float percentComplete = timeSinceStarted / moveData.moveLength;
        if (percentComplete >= 1.0f)
        {
            percentComplete = 1.0f;
        }
        if (curMoveType != MovementTypes.SMOOTH)
        {
			try
            {
                //generate a new position based on LERP, and modify it if necessary for TOSS, SLERP, or WHATEVER we end up doing :D
                Vector3 vLerpedPosition = Vector3.Lerp(moveData.startPosition, moveData.targetPosition, percentComplete);

                //if we are TOSSing the object, the Y value increases based on the toss height and how complete the movement is.
                if (curMoveType == MovementTypes.TOSS)
                {                    
                    //We want to send in a number between 0 and PI, because the results are 0 to 0, with 1 at the top
                    //Multiply that number by the toss height to determine how high up the object is.
                    float fYDelta = Mathf.Sin((float) Math.PI * percentComplete) * fTossHeight;
                    vLerpedPosition.y += fYDelta;
                }

                //SLERP motion goes here, if we ever mess with that again.
                //transform.position = Vector3.SlerpUnclamped(moveData.startPosition, moveData.targetPosition, percentComplete);

                //if our movement type is LERP, we already lerped the herp, so derp.
                SetTransformPosition(vLerpedPosition);

#if UNITY_EDITOR
                if (debugRevealData)
                {
                    Debug.Log(gameObject.name + " CURRENT position: " + transform.position + " Started at " + moveData.startPosition + " to target " + moveData.targetPosition + " pcomplete " + percentComplete);
                    //Debug.Log(timeSinceStarted + " " + moveData.timeAtMoveStart + " " + moveData.moveLength);
                }
#endif

            }
            catch (Exception e)
            {
                Debug.LogWarning("Very Bad Error! Movable:LerpPosition " + gameObject.name + " threw exception: " + e.ToString());
			}
            //Debug.Log("Lerp " + percentComplete + " " + moveData.startPosition + " " + moveData.targetPosition + " " + transform.position);
        }

        // Shouldn't x and y rotation always be 0...? 12/14/17
        eulerAngles.x = 0f; // transform.eulerAngles.x;
        eulerAngles.y = 0f; //  transform.eulerAngles.y;
        eulerAngles.z = transform.eulerAngles.z;
        if (moveData.rotation != 0)
        {
            eulerAngles.z = Mathf.Lerp(0.0f, moveData.rotation, percentComplete);
            transform.eulerAngles = eulerAngles;
        }                
        if (percentComplete >= 1.0f)
        {
            // verify final number is even

            if ((!CustomAlgorithms.CompareFloats(transform.position.x, moveData.targetPosition.x)) || (!CustomAlgorithms.CompareFloats(transform.position.y, moveData.targetPosition.y)))
            {
                SetTransformPosition(moveData.targetPosition);
                if (debugRevealData)
                {
                    Debug.Log("Now at " + transform.position + " to match " + moveData.targetPosition);
                }
            }
            return true;
        }
            return false;
       
    }

    void SetTransformPosition(Vector3 newTPos)
    {
        transform.position = newTPos;
        if (debugRevealData) Debug.Log(gameObject.name + " transform set to " + newTPos);
    }

	public Vector3 GetTruePosition () {
		return transform.position;
	}

    /* public Vector2 GetGridPosition()
    {
        return position;
    } */

    public void SyncGridPosition()
    {
        position = transform.position;
    }

    public bool IsMoving() {
		return moving;
	}
		
	public void SetPosition(Vector3 newPos) {

        bool enqueueMovement = true;

        if (enqueueMovement && moving)
        {
            lerpMoveData newStep = new lerpMoveData();
            newStep.targetPosition = newPos;
            newStep.finalPosition = newPos;
            newStep.moveLength = 0.01f;
            newStep.step = false;
            queuedMovements.Enqueue(newStep);
            //Debug.Log(gameObject.name + " " + owner.actorUniqueID + " enqueues new movement to " + newPos + " as it is already moving to " + moveData.finalPosition + " from " + owner.GetPos() + " prev: " + owner.previousPosition + " turn: " + GameMasterScript.turnNumber);
            return;
        }
        velocity = Vector3.zero;
        moving = false;
        jittering = false;
        jabbing = false;
        trackingObject = false;
        newPos.y += permanentYOffset;
        newPos.x += permanentXOffset;
        position = newPos;
        SetTransformPosition(position);
	}

    public void TrackObject(GameObject go)
    {
        if (go == null) return;
        trackObject = go;
        trackingObject = true;
    }

    public void AnimateSetPositionNoChange(Vector3 newPos, float animLength, bool step, float rotation, float arcMult, MovementTypes moveType)
    {
        if (MapMasterScript.activeMap.alwaysSpin)
        {
            if (owner != GameMasterScript.heroPCActor)
            {
                rotation = 360f;
            }            
        }

        newPos.y += permanentYOffset;

        if (moving)
        {
            // New code to address forcing a target to move DURING movement.
            moveData.targetPosition = newPos;
            moveData.finalPosition = newPos;
            moveData.timeAtMoveStart = Time.time;
            position = newPos;
            return;
        }

        curMoveType = moveType;
        if (moveType == MovementTypes.SMOOTH)
        {
            curMoveType = MovementTypes.LERP; // EXPERIMENTAL!
        }

        if (animLength == 0)
        {
            animLength = 0.1f;
        }
        moveData.moveLength = animLength;
        moveData.startPosition = position;
        moveData.targetPosition = newPos;
        moveData.timeAtMoveStart = Time.time;
        moveData.finalPosition = newPos;
        moveData.rotation = rotation;
        arcMove = false;

        float baseArc = 0.1f;
        baseArc *= arcMult;

        arcMult = 0.0f; // Comment this out for EXPERIMENTAL
        position = newPos;
        moving = true;
    }

    public void ClearMovementQueue()
    {
        if (queuedMovements != null)
        {
            queuedMovements.Clear();
        }

        moving = false;
    }

    /// <summary>
    /// Returns the movedata that will be used to power the movement, whether or not it is queued.
    /// </summary>
    /// <param name="newPos"></param>
    /// <param name="animLength"></param>
    /// <param name="step"></param>
    /// <param name="rotation"></param>
    /// <param name="arcMult"></param>
    /// <param name="moveType"></param>
    /// <returns>The lerpMoveData that has been assigned, or null if there is no motion</returns>
    public lerpMoveData AnimateSetPosition(Vector3 newPos, float animLength, bool step, float rotation, float arcMult, MovementTypes moveType)
    {        
        bool debugMovement = false;
        if (owner != null && owner.GetActorType() == ActorTypes.MONSTER)
        {
            if (owner.actorUniqueID == 3330) Debug.Log(gameObject.name + " at " + gameObject.transform.position + " move to " + newPos + " over " + animLength);
            // Stop jittering because this can cause movement / sprite desync?
            if (jittering)
            {
                SetTransformPosition(new Vector3(centerPosition.x, centerPosition.y, transform.position.z));
                jittering = false;
            }
        }
        if (MapMasterScript.activeMap.alwaysSpin)
        {
            rotation = 360f;
        }

        if (moving && (newPos == moveData.targetPosition || newPos == moveData.finalPosition))
        {
            //Debug.Log(newPos + " is equal to " + moveData.targetPosition + " " + moveData.finalPosition);
            return null;
        }

        newPos.y += permanentYOffset;

        if (moving)
        {
            // 11/28, experimental. Let's QUEUE movements rather than janking the sprite around. 
            // Sometimes a double movement call would cause bad-looking behavior.

            lerpMoveData newStep = new lerpMoveData();
            newStep.targetPosition = newPos;
            newStep.finalPosition = newPos;
            newStep.moveLength = animLength;
            newStep.step = step;
            queuedMovements.Enqueue(newStep);

            return newStep;
        }

        curMoveType = moveType;
        if (moveType == MovementTypes.SMOOTH)
        {
            curMoveType = MovementTypes.LERP; // EXPERIMENTAL!
        }
        
        if (animLength == 0)
        {
        	animLength = 0.1f;
        }
        if (owner == GameMasterScript.heroPCActor)
        {
            if (step)
            {
                owner.myAnimatable.SetAnimDirectional("Walk",owner.lastMovedDirection,owner.lastCardinalDirection);
            }
        }

        //Debug.Log(owner.actorRefName + " " + owner.actorUniqueID + " new movement to " + newPos + " from " + position + " " + GameMasterScript.turnNumber);

        moveData.moveLength = animLength;
        moveData.startPosition = position;
        moveData.targetPosition = newPos;
        moveData.timeAtMoveStart = Time.time;
        moveData.finalPosition = newPos;
        moveData.step = step;
        moveData.rotation = rotation;
        arcMove = false;

        if (debugRevealData)
        {
            Debug.Log("Normal move from " + position + " to " + newPos + " time: " + moveData.timeAtMoveStart + " over length " + animLength);
        }
        
        
        float baseArc = 0.1f;
        baseArc *= arcMult;

        arcMult = 0.0f; // Comment this out for EXPERIMENTAL

        //Debug.Log("Moving from " + position + " to " + newPos + " transform " + transform.position);

        if (arcMult > 0.0f)
        {
            arcMove = true;
            moveData.finalPosition = newPos;
            moveData.moveLength = animLength / 2f;
            // Calculate arc.
            arcMoveVertical = false;
            arcMoveHorizontal = false;
            if (newPos.x > position.x || newPos.x < position.x)
            {
                arcMoveHorizontal = true;
            }
            if (newPos.y > position.y || newPos.y < position.y)
            {
                arcMoveVertical = true;
            }
            if (arcMoveHorizontal && !arcMoveVertical)
            {
                float xDiff = (newPos.x - position.x) / 2f; // For example, 30 - 29 = 1. Or 30 - 31 = -1.
                moveData.targetPosition = new Vector3(position.x + xDiff, position.y + baseArc, position.z); // .35f is the bounce amount
            }
            if (!arcMoveHorizontal && !arcMoveVertical) // Fix this later
            {
                float yDiff = (newPos.y - position.y) / 2f; // For example, 30 - 29 = 1. Or 30 - 31 = -1.
                moveData.targetPosition = new Vector3(position.x, position.y + yDiff + baseArc, position.z); // .35f is the bounce amount
            }
            if (arcMoveHorizontal && arcMoveVertical)
            {
                // Diagonal
                float xDiff = (newPos.x - position.x) / 2f; // For example, 30 - 29 = 1. Or 30 - 31 = -1.
                float yDiff = (newPos.y - position.y) / 2f; // For example, 30 - 29 = 1. Or 30 - 31 = -1.
                moveData.targetPosition = new Vector3(position.x + xDiff, position.y + yDiff + baseArc, position.z); // .35f is the bounce amount
            }
        }

        if (owner != null)
        {
            owner.SetCurPos(newPos);
        }

        position = newPos;

        moving = true;

        // SFX present?
        if (myAudioStuff != null && footstepSounds.Count > 0 && step)
        {
            // Play footstep SFX
            myAudioStuff.PlayCue("Footstep");
        }

        return moveData;
    }

    public int QueuedMovementCount()
    {
        if (queuedMovements == null)
        {
            return 0;
        }

        return queuedMovements.Count;
    }


    public IEnumerator HideThenShowAfterSeconds(float fHideDuration, string strEffectOnShow = "", string strEffectOnHide = "")
    {
        //this will also hush our translucency layer
        EnableRenderer(false);
        bAuthoritativeDoNotRender = true;
        yield return new WaitForSeconds(fHideDuration);
        EnableRenderer(true);
        bAuthoritativeDoNotRender = false;

        if (!string.IsNullOrEmpty(strEffectOnShow))
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(transform.position, strEffectOnShow, null, true);
        }
    }

    //Please note that if you set this to true, it will override every other
    //request to render this actor until you turn it off. Don't forget!
    public void SetAuthoritativeDoNotRender(bool bValue)
    {
        bAuthoritativeDoNotRender = bValue;
        EnableRenderer(!bValue);
    }

    public void SetTossHeight(float f)
    {
        fTossHeight = f;
        curMoveType = MovementTypes.TOSS;
    }

    IEnumerator IWaitToFadeOut(float waitTime, float fadeTime)
    {
        yield return new WaitForSeconds(waitTime);
        ForceFadeOut(fadeTime);

    }

    public void WaitThenFadeOut(float waitTime, float fadeTime)
    {
        StartCoroutine(IWaitToFadeOut(waitTime, fadeTime));
    }

    public void SetOwner(Actor own)
    {
        owner = own;
    }

    public Actor GetOwner()
    {
        return owner;
    }

    // Maybe our baseline alpha is supposed to be enforced elsewhere, like a semi-transparent animation
    // This function will check for animatables in non-actors (sprite FX), OR actors with animatables
    public float GetMaxAlpha()
    {        
        if (cachedBaseOpacityValue > 0f)
        {
            return cachedBaseOpacityValue;
        }
        if (!hasFXAnimatable && owner == null)
        {
            cachedBaseOpacityValue = 1.0f;
        }
        else
        {
            // We're making the assumption that any given animation's startOpacity is the same throughout
            // This might not always be true but for all current animations in the game as of 6/8/18 it is true
            if (hasFXAnimatable)
            {
                cachedBaseOpacityValue = fxAnimatableComponent.animPlaying.startOpacity;
            }
            else
            {
                if (owner.myAnimatable != null && owner.myAnimatable.validAnimationSet)
                {
                    cachedBaseOpacityValue = owner.myAnimatable.animPlaying.startOpacity;
                }
                else
                {
                    // Some kind of static object with no animatable, like a tree perhaps. But actors SHOULD have animatables
                    // if they don't, then we messed up the prefabs.
                    cachedBaseOpacityValue = 1.0f;
                }
                
            }
        }

        return cachedBaseOpacityValue;
    }


}
