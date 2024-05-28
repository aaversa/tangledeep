using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class zirconAnim {

    public bool enableSpeedVariation;
    public int maxRandomFrame;
    public bool fixedAnimStartTimer;
    public bool varyAnimStartTimer;
    public string animName;
    public bool isAttackAnimation;
    public bool animationHandlesOrientation;
    public string completionLogic;
    public float startScale;
    public bool ignoreScale;
    public float startOpacity;
    public float startRotation;
    public float startOffsetX;
    public float startOffsetY;
    public float chanceToFlipX;
    public float xVariance;
    public float yVariance;
    public float chanceToFlipY;
    public bool smoothStepMovement;
    public List<AnimationFrameData> mySprites;
    public float gameWaitTime;

    public float calculatedLength;
    public float timePassedDuringAnim;

    private bool bSpritesLoaded;
    [System.Serializable]
	public class AnimationFrameData {
		public Sprite mySprite;
		public float spriteTime;
        public float scale = 1f;
        public bool ignoreScale;
        public float opacity = 1f;
        public float rotation;
        public int flipX;
        public int flipY;
        public float offsetX;
        public float offsetY;
        public bool offsetOnFramesOnly;
        public bool staticSprite;
        public float pivotOffsetX;
        public float pivotOffsetY;

        public AnimationFrameData()
        {
            // Do nothing.
        }
	}
    public void GetSpritesFromMemory()
    {
        if (mySprites == null || mySprites.Count == 0)
        {
            return;
        }

        //UnityEngine.Profiling.Profiler.BeginSample("Trying to load sprites from asset bundle in memory:");
        for (int t = 0; t < mySprites.Count; t++)
        {
            if (mySprites[t].mySprite != null)
            {
                string sprName = mySprites[t].mySprite.name;
                // The sprite might not be loaded yet. 
                var actualSpriteFromMemory = TDAssetBundleLoader.GetSpriteFromMemory(mySprites[t].mySprite.name);
                if (actualSpriteFromMemory != null)
                {
                    mySprites[t].mySprite = actualSpriteFromMemory;
                } 
                else
                {
                    //if (Debug.isDebugBuild) Debug.Log("Couldn't get sprite " + sprName);
                }
            }
        }
        //UnityEngine.Profiling.Profiler.EndSample();
    }

    public void SetSpriteOnly(int index, Sprite nSprite)
    {
        AnimationFrameData std = mySprites[index];
        std.mySprite = nSprite;
    }

	public void setSprite(List<AnimationFrameData> newSprites) {
		mySprites = newSprites;
	}

}

[System.Serializable]
public class Animatable : MonoBehaviour {

    // We are going to use these two variables to turn OFF updates for Actor objects that are not in the player's
    // current visibility range. Since some Animatables are attached to SpriteEffects, this will be set AT the time
    // of the Animatable component's attachment to the Actor object (in MapMaster's instantiation)
    bool isActor;
    Actor owner;

	public List<zirconAnim> myAnimations;

    public bool debugRevealData;

    /// <summary>
    /// 1.0f == normal speed
    /// </summary>
    public float speedMultiplier = 1.0f;

	public float startRotation;

	protected zirconAnim lastAnim;
	public float spriteTimer; // was private
	private SpriteRenderer mySpriteRenderer;
    public bool srFound;
    private Image myImage;
    public bool IsUIElement;
    public bool alphaSetExternally;
    //private SpriteScale scaler;
	public int spriteIndex;
	private bool indexUp;
	public bool animComplete;
    private bool sleep = true;
    private int adjustedSpriteStartIdx;

    public Directions defaultDirection;

    public float rotationAngleOffset;
    public Vector2 allFramePosOffset = new Vector2(0, 0);

    private Movable myMovable;

    public float timeAtFrameStart;
    private float curFrameOpacity;
    private float curFrameScale;
    private float curFrameRotation;
    private float curOffsetX;
    private float curOffsetY;
    private Vector2 originalPosition;
    private float nextFrameOpacity;
    private float nextFrameScale;
    private float nextFrameRotation;
    private float nextOffsetX;
    private float nextOffsetY;
    private Vector2 nextPosition;
    private bool lerpingOpacity;
    private bool lerpingScale;
    private bool lerpingRotation;
    private bool lerpingPosition;

    private Vector3 basePosition;

    private bool deferStart;
    private GameObject parentObject;

    public float calcGameWaitTime;

    public Directions spriteFacingDirection;

    public float opacityMod;

    public bool hitOrAttackState;
    public bool directionFlippedFromAnimation;

    bool forceLoopCurrentAnimation;

    // Pooling

    zirconAnim.AnimationFrameData curSpriteData;
    Sprite curSprite;
    public float curSpriteTime; // was private
    string completeLogic;
    bool finished;
    float timeSinceStarted;
    Color getColor;
    float curOpacity;
    float curPositionX;
    float curPositionY;
    float curScale;
    float curRotation;
    Vector3 myPos;
    int startIndex;
    float percentComplete;
    Vector3 qt;
    Vector3 mpos;

    public bool updatedAtLeastOnce;

    public bool validAnimationSet;

    bool paused;

    [Space(64)]
    [Header("I AM BULLSHIT DO NOT EDIT ME")]
    public zirconAnim animPlaying;

    public static string IDLE_NAME = "Idle";
    public static string IDLE2_NAME = "IdlePhysical";
    public static string IDLE3_NAME = "IdleEthereal";

    public string defaultIdleAnimationName = "Idle";
    public string defaultTakeDamageAnimationName = "TakeDamage";

    /// <summary>
    /// Cached strings to prevent strcats from the most common calls to SetAnimDirectional
    /// </summary>
    private static string sideIdle = "IdleSide";
    private static string topIdle = "IdleTop";

    SpriteEffect mySpriteEffectComponent;
    bool checkedForSpriteEffect = false;
    bool hasSpriteEffect = false;
    void Awake()
    {
    	ResetToDefaults();
        SearchForSpriteRenderer();

        myMovable = GetComponent<Movable>();
        CheckForSpriteEffectComponent(initialize: true);

        /* if (PlatformVariables.LOAD_EVERYTHING_FROM_ASSET_BUNDLES)
        {
            foreach (var anim in myAnimations)
            {
                anim.GetSpritesFromMemory();
            }
        } */

    }
    public bool SearchForSpriteRenderer( bool bForceLook = false)
    {
        //we may insist on a fresh look, especially if this animation
        // :notes: ~is on a new object. If not we will, be rekt. This is not circumspect~ :notes:
        // Mercs OST Arcade Mode Mission 3
        if (bForceLook)
        {
            srFound = false;
        }
        if (!IsUIElement)
        {
            if (!srFound)
            {
                mySpriteRenderer = GetComponent<SpriteRenderer>();
                if (mySpriteRenderer != null)
                {
                    srFound = true;
                }
            }            
        }
        else
        {
            myImage = GetComponent<Image>();
            srFound = myImage != null;
        }
        myMovable = GetComponent<Movable>();
        return srFound;
    }

    public void GetSpritesFromMemory()
    {
        foreach(zirconAnim anim in myAnimations)
        {
            anim.GetSpritesFromMemory();
        }
    }

    void ResetToDefaults()
    {
		spriteTimer = 0;
        spriteIndex = 0;
        lastAnim = null;
        indexUp = true;
        animComplete = false;
        spriteFacingDirection = defaultDirection;
        opacityMod = 1.0f;
        CheckForSpriteEffectComponent(initialize:true);
    }
    void CheckForSpriteEffectComponent(bool initialize)
    {
        if (checkedForSpriteEffect && !hasSpriteEffect)
        {
            return;
        }
        if (!checkedForSpriteEffect)
        {
            mySpriteEffectComponent = GetComponent<SpriteEffect>();
            if (mySpriteEffectComponent != null)
            {
                hasSpriteEffect = true;
            }
            checkedForSpriteEffect = true;
        }
        if (hasSpriteEffect && initialize)
        {
            mySpriteEffectComponent.animInitialized = true;
        }
    }

    public bool IsAnimAttackAnimation()
    {
        if (animPlaying == null) return false;
        return animPlaying.isAttackAnimation;
    }

    //This will grab the animation from another object, and copy the sprites it uses into our
    //version of this animation
    public Sprite CopySpritesFromOtherAnimation(Animatable otherAnimObject, string strAnimNameToCopy)
    {
        if (otherAnimObject == null)
        {
            return null;
        }

        zirconAnim myVersion = myAnimations.Find( a => String.Equals(a.animName, strAnimNameToCopy, StringComparison.InvariantCultureIgnoreCase) );
        if (myVersion == null)
        {
            return null;
        }

        zirconAnim otherVersion = otherAnimObject.myAnimations.Find(a => String.Equals(a.animName, strAnimNameToCopy, StringComparison.InvariantCultureIgnoreCase));
        if (otherVersion == null)
        {
            return null;
        }

        for (int t = 0; t < myVersion.mySprites.Count; t++)
        {
            zirconAnim.AnimationFrameData myAFD = myVersion.mySprites[t];
            zirconAnim.AnimationFrameData otherAFD = otherVersion.mySprites[t];

            myAFD.mySprite = otherAFD.mySprite;
            myAFD.scale = otherAFD.scale;
        }

        return myVersion.mySprites[0].mySprite;
    }

    void OnEnable ()
    {
        //Shep: startRotation is a float and thus is never null
		if (transform.eulerAngles.z != startRotation)
        { 
    		transform.eulerAngles = new Vector3(0f, 0f, startRotation);
    	}
    	else
        {
    		//Debug.Log(transform.eulerAngles + " is ok " + startRotation + " " + gameObject.name);
    	}
        CheckForSpriteEffectComponent(false);
		if (hasSpriteEffect)
        {
            if (mySpriteEffectComponent.spriteParent != null)
            {
                parentObject = mySpriteEffectComponent.spriteParent;
            }
        }
        ResetToDefaults();
    }

    void Start () {

        if (!IsUIElement)
        {
            if (!srFound)
            {
                mySpriteRenderer = GetComponent<SpriteRenderer>();
                if (mySpriteRenderer != null)
                {
                    srFound = true;
                }
            }
            
        }
        else
        {
            myImage = GetComponent<Image>();
        }
        myMovable = GetComponent<Movable>();

        CheckForSpriteEffectComponent(initialize: true);

        //scaler = GetComponent<SpriteScale>();
	}

    public void OrientSprite(Directions dir)
    {
        //if (animPlaying.animationHandlesOrientation) return;
        if (dir == MapMasterScript.oppositeDirections[(int)defaultDirection]) 
        {
            if (spriteFacingDirection == defaultDirection)
            {
                if (srFound)
                {
                    mySpriteRenderer.flipX = true;
                }
            }
            spriteFacingDirection = Directions.WEST;
        }
        else if (dir == defaultDirection)
        {
            if (spriteFacingDirection == MapMasterScript.oppositeDirections[(int)defaultDirection])
            {
                if (srFound)
                {
                    mySpriteRenderer.flipX = false;
                }
            }
            spriteFacingDirection = Directions.EAST;
        }
    }

    public void SetAnimDirectional(string animName, Directions dir, Directions previousDir, bool looping = false)
    {
        forceLoopCurrentAnimation = looping;
        hitOrAttackState = looping;

        string sideName = animName == "Idle" ? sideIdle : animName + "Side";
        string topName = animName == "Idle" ? topIdle : animName + "Top";
        switch(dir)
        {
            case Directions.WEST:
                if (CheckForAnim(sideName))
                {
                    SetAnim(sideName);
                    FlipSpriteXFromDirectionalAnim(true);
                }
                else
                {
                    SetAnim(animName);
                }
                break;
            case Directions.EAST:
                if (CheckForAnim(sideName))
                {
                    SetAnim(sideName);
                    FlipSpriteXFromDirectionalAnim(false);
                }
                else
                {
                    SetAnim(animName);
                }
                break;
            case Directions.NORTHEAST:
            case Directions.NORTHWEST:
            case Directions.NORTH:
                if (CheckForAnim(topName))
                {
                    SetAnim(topName);
                }
                else
                {
                    SetAnim(animName);
                }
                break;
            //default includes any South based direction
            default:
                SetAnim(animName);
                break;
        }
    }

    public bool CheckForAnim(string animName)
    {
        for (int i = 0; i < myAnimations.Count; i++)
        {
            if (myAnimations[i].animName == animName) return true;
        }
        return false;
    }

    public float GetCompletionPercentage()
    {
        float percent = animPlaying.timePassedDuringAnim / animPlaying.calculatedLength;
        return percent;
    }

    public Actor GetOwner()
    {
        return owner;
    }

    public void SetOwner(Actor _owner)
    {
        isActor = true;
        owner = _owner;
    }

    public void Pause()
    {
        paused = true;
    }
    public void Unpause()
    {
        paused = false;
    }

	// Update is called once per frame
	void Update () {
        if (sleep)
        {
            return;
        }
        if (!srFound && !IsUIElement)
        {
            return;
        }

        if (paused)
        {
            return;
        }

        // Make sure we run our update at least ONCE so that we have the correct sprite, color, etc.
        // After that, if we're not in a "RevealAll" area, and not friendly, and the player CAN'T see us...
        // Don't run the update function.
        if (GameMasterScript.gameLoadSequenceCompleted && isActor && updatedAtLeastOnce)            
        {
            Vector2 aPos = owner.GetPos();
            if (MapMasterScript.InBounds(aPos))
            {
                // Also don't do this on maps with special image overlays, because that probably means it's a plot area
                if (!MapMasterScript.activeMap.IsTownMap() && !GameMasterScript.heroPCActor.visibleTilesArray[(int)aPos.x, (int)aPos.y] &&
                    owner.actorfaction != Faction.PLAYER && !MapMasterScript.activeMap.dungeonLevelData.revealAll && string.IsNullOrEmpty(MapMasterScript.activeMap.dungeonLevelData.imageOverlay))
                {
                    return;
                }
            }

        }
        else if (!isActor)
        {
            // Stuff like DirectionalIndicator is firing anims even when the SR is disabled. If SR is off, don't animate.
            // But does this actually save CPU since we have to check .enabled
            if (srFound && !IsUIElement && !mySpriteRenderer.enabled)
            {
                return;
            }
        }

        updatedAtLeastOnce = true;

        if (myAnimations.Count == 0)
        {
            return;
        }

        if (deferStart)
        {
            if (!srFound && !IsUIElement)
            {
                return;
            }
            else
            {
                deferStart = false;
                SetAnim(animPlaying);
            }
        }

        if (animPlaying != lastAnim)
        {
			ResetAnim();
		}

        if (!validAnimationSet)
        {
            return;
        }
        if (animPlaying.mySprites.Count == 0)
        {
            return;
        }

		/* if (animPlaying == null)
        {
        	return; 
        }

		if (animPlaying.mySprites == null || animPlaying.mySprites.Count == 0) {
			return;
		} */

		lastAnim = animPlaying;

        startIndex = spriteIndex;

		// Increase the sprite timer based on actual time passing regardless of frame rate
	    spriteTimer += Time.deltaTime * speedMultiplier;

        if (animPlaying.enableSpeedVariation)
        {
            spriteTimer += UnityEngine.Random.Range(0f, 0.02f);
        }

        animPlaying.timePassedDuringAnim += Time.deltaTime;

		curSpriteData = animPlaying.mySprites[spriteIndex];
		curSprite = curSpriteData.mySprite;
        
		curSpriteTime = curSpriteData.spriteTime;
		completeLogic = animPlaying.completionLogic;

		finished = false;

	    timeSinceStarted = (Time.time - timeAtFrameStart) * speedMultiplier;

        getColor = !IsUIElement ? mySpriteRenderer.color : myImage.color;
        
        curOpacity = getColor.a;

        Vector3 curTransformPosition = transform.position;
        curPositionX = curTransformPosition.x;
        curPositionY = curTransformPosition.y;
        float curPositionZ = curTransformPosition.z;

        // float curScale = scaler.designSizeW;

        curScale = transform.localScale.x;

        if (lerpingRotation)
        {
            curRotation = transform.eulerAngles.z;
            if (parentObject != null)
            {
                curRotation = parentObject.transform.eulerAngles.z;
            }
        }        
        percentComplete = timeSinceStarted / curSpriteTime;
        if (lerpingOpacity && !alphaSetExternally)
        {
            curOpacity = Mathf.Lerp(curFrameOpacity, nextFrameOpacity, percentComplete);
            if (!IsUIElement)
            {
                mySpriteRenderer.color = new Color(mySpriteRenderer.color.r, mySpriteRenderer.color.g, mySpriteRenderer.color.b, curOpacity * opacityMod);
            }
            else
            {
                myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, curOpacity * opacityMod);
            }
            
        }
        if (lerpingScale && !animPlaying.ignoreScale)
        {
            curScale = Mathf.Lerp(curFrameScale, nextFrameScale, percentComplete);
            transform.localScale = new Vector3(curScale, curScale, curScale);
        }
        if (lerpingRotation)
        {
            curRotation = Mathf.Lerp(curFrameRotation, nextFrameRotation, percentComplete);
            // Shouldn't x/y always be 0 in 2d plane...? 12/14/2017
            qt.x = 0f; // transform.eulerAngles.x;
            qt.y = 0f; // transform.eulerAngles.y;
            qt.z = curRotation;
            //new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, curRotation);

            if (parentObject != null)
            {
                parentObject.transform.eulerAngles = qt;
            }
            else
            {
                transform.eulerAngles = qt;
            }            
        }  
        if (lerpingPosition && !animPlaying.mySprites[spriteIndex].offsetOnFramesOnly)
        {
            curPositionX = Mathf.Lerp(originalPosition.x, nextPosition.x, percentComplete);
            curPositionY = Mathf.Lerp(originalPosition.y, nextPosition.y, percentComplete);
            Vector3 nPos = new Vector3(curPositionX, curPositionY, curPositionZ);
            SetTransformPosition(nPos);
        }
       

        bool changedPosition = false;

        if (spriteTimer >= curSpriteTime)
        {
            // Switch frames

            if (animPlaying.mySprites[spriteIndex].offsetOnFramesOnly)
            {
                mpos = basePosition;
                if (myMovable != null)
                {
                    mpos = curTransformPosition;
                }
                mpos.x += animPlaying.mySprites[spriteIndex].offsetX;
                mpos.y += animPlaying.mySprites[spriteIndex].offsetY;
                SetTransformPosition(mpos);
                changedPosition = true;
            }

            if (indexUp) {
				spriteIndex++;
				if (spriteIndex >= animPlaying.mySprites.Count)
                {
					finished = true;                    
                    spriteIndex = animPlaying.mySprites.Count - 1;
				}
			}
			else {
					spriteIndex--;
					if (spriteIndex < 0)
                    {
						finished = true;
                        spriteIndex = 0;
					}
			}

            spriteTimer = 0;
			if (finished) {                
                if (string.Equals(completeLogic,"Loop"))
                {
					spriteIndex = 0;
					indexUp = true;
				}
				if (string.Equals(completeLogic,"Reverse"))
                {
					if (indexUp) {
						spriteIndex = animPlaying.mySprites.Count - 2;
						indexUp = false;
					}
					else {
						spriteIndex = 1;
						indexUp = true;
					}
				}
				if (string.Equals(completeLogic,"Stop")) {
					animComplete = true;					
					indexUp = true;
				}

                if (string.Equals(completeLogic, defaultIdleAnimationName))
                {
                    animComplete = true;
                    spriteIndex = 0;
                    SetAnim(defaultIdleAnimationName);
                }
			}

            // Execute rotation.
            // Switched frames.   
        }
		curSpriteData = animPlaying.mySprites[spriteIndex];
		curSprite = curSpriteData.mySprite;
        
        if (!animPlaying.mySprites[spriteIndex].staticSprite)
        {
            if (!IsUIElement)
            {
                mySpriteRenderer.sprite = curSprite;
            }
            else
            {
                myImage.sprite = curSprite;
            }
            
        }

        myPos = basePosition;

        if (myMovable != null)
        {
            myPos = curTransformPosition;           
        }



        if (changedPosition && parentObject == null)
        {
            SetTransformPosition(myPos);
        }
        

        // New frame?

        if (spriteIndex != startIndex && !animComplete)
        {
            // Anim changed 
            GetLerpData(curSpriteData,spriteIndex);
        }

        if (finished && (animPlaying.animName == defaultTakeDamageAnimationName || animPlaying.isAttackAnimation)) // 3rd conditional used to compare to "Attack"
        {
            if (animPlaying.isAttackAnimation)
            {
                SetAnimDirectional(defaultIdleAnimationName, myMovable.GetOwner().lastMovedDirection, myMovable.GetOwner().lastMovedDirection);
            }
            else
            {
                SetAnim("Default");
            }            
        }
	}

    public void ToggleIgnoreScale(bool bShouldIgnore = true)
    {
        animPlaying.ignoreScale = bShouldIgnore;
        for (int i = 0; i < myAnimations.Count; i++)
        {
            myAnimations[i].ignoreScale = bShouldIgnore;
        }
    }

    public void SetAllSpriteScale(float amount)
    {
        if (!validAnimationSet) return;
        if (animPlaying.ignoreScale) return;
        transform.localScale = new Vector3(amount, amount, 1f);
        for (int i = 0; i < animPlaying.mySprites.Count; i++)
        {
            animPlaying.mySprites[i].scale = amount;
            animPlaying.startScale = amount;
        }
        for (int i = 0; i < myAnimations.Count; i++)
        {
            myAnimations[i].startScale = amount;
            for (int x = 0; x < myAnimations[i].mySprites.Count; x++)
            {
                myAnimations[i].mySprites[x].scale = amount;                
            }
        }
    }

    public void OverrideFrameLength(float newTime)
    {
        animPlaying.mySprites[spriteIndex].spriteTime = newTime;
        for (int i = 0; i < animPlaying.mySprites.Count; i++)
        {
            animPlaying.mySprites[i].spriteTime = newTime;
        }
    }

    public void SetAnim(zirconAnim newAnim) {
        animPlaying = newAnim;
		lastAnim = newAnim;
		spriteTimer = 0;
		spriteIndex = 0;
		indexUp = true;
		animComplete = false;
        sleep = false;

        calcGameWaitTime = 0.0f;


        if (animPlaying == null)
        {
            validAnimationSet = false;
        	return;
        }

        validAnimationSet = true;

        for (int i = 0; i < animPlaying.mySprites.Count; i++)
        {
            calcGameWaitTime += animPlaying.mySprites[i].spriteTime;
        }

        if (animPlaying.varyAnimStartTimer)
        {
            spriteTimer += Random.Range(-0.15f, 0.15f); // Experimental, make animations look more staggered.
        }
        

        if ((Random.Range(0, 1f) < animPlaying.chanceToFlipX) && (!IsUIElement))
        {
            mySpriteRenderer.flipX = !(mySpriteRenderer.flipX);
        }

        if (animPlaying.xVariance != 0.0f)
        {
            float xRoll = UnityEngine.Random.Range(animPlaying.xVariance * -1f, animPlaying.xVariance);
            Vector3 nPos = gameObject.transform.position;
            nPos.x += xRoll;
            SetTransformPosition(nPos);
        }

        if ((Random.Range(0, 1f) < animPlaying.chanceToFlipY) && (!IsUIElement))
        {
            mySpriteRenderer.flipY = !(mySpriteRenderer.flipY);
        }

        zirconAnim.AnimationFrameData curSpriteData = animPlaying.mySprites[spriteIndex];

        float baseRotation = transform.eulerAngles.z + animPlaying.startRotation + rotationAngleOffset;
        
        if (parentObject == null)
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, baseRotation);
        }
        else
        {
            parentObject.transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, baseRotation);
        }

        if (parentObject == null)
        {
            basePosition = transform.position;
            Vector3 pos = basePosition;
            //pos.x += animPlaying.mySprites[0].offsetX;
            //if (!animPlaying.smoothStepMovement)
            {
                pos.x += animPlaying.startOffsetX;
                pos.y += animPlaying.startOffsetY;
                SetTransformPosition(pos);
            }
        }

        SetInitialOffsets();
        GetLerpData(curSpriteData, spriteIndex); // This was above parentObject part before.


        if (animPlaying.mySprites.Count > 1)
        {
            
            
        }

        /* Vector3 finalPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        finalPos.x += newAnim.startOffsetX;
        finalPos.y += newAnim.startOffsetY;
        transform.position = finalPos; */

        //zirconAnim.spriteTimeData curSpriteData = animPlaying.mySprites[spriteIndex];
        Sprite curSprite = curSpriteData.mySprite;

        if (!IsUIElement)
        {
            mySpriteRenderer.sprite = curSprite;
        }
        else
        {
            myImage.sprite = curSprite;
        }
        
    }

    void SetInitialOffsets()
    {
        Vector3 pos = transform.position;

        if (animPlaying == null) return;

        if (animPlaying.startOffsetX != 0f)
        {            
            pos.x += animPlaying.startOffsetX;
        }
        if (animPlaying.startOffsetY != 0f)
        {
            pos.y += animPlaying.startOffsetY;
        }
        SetTransformPosition(pos);
    }

    void SetTransformPosition(Vector3 newTPos)
    {
        transform.position = newTPos;
        if (debugRevealData)
        {
            Debug.Log(gameObject.name + " transform set to " + newTPos);
        }
    }

    // this kicks off the animation!
    private void GetLerpData(zirconAnim.AnimationFrameData curSpriteData, int index)
    {
        if ((!srFound) && (!IsUIElement))
        {
            return;
        }
        else if ((IsUIElement) && (myImage == null))
        {
            return;
        }


        int flipX = curSpriteData.flipX;
        int flipY = curSpriteData.flipY;
    
        if ((flipX == 1) && (!IsUIElement))
        {
            mySpriteRenderer.flipX = !(mySpriteRenderer.flipX);
        }

        if ((flipY == 1) && (!IsUIElement))
        {
            mySpriteRenderer.flipY = !mySpriteRenderer.flipY;
        }

        float scale;
        float opacity;
        float rotation;

        int prevIndex;

        if (indexUp)
        {
            prevIndex = index - 1;
        }
        else
        {
            prevIndex = index + 1;
        }

        if ((index == 0)) //|| (prevIndex == 0))
        {
            scale = animPlaying.startScale;
            opacity = animPlaying.startOpacity;
            rotation = transform.eulerAngles.z;
            curOffsetX = animPlaying.startOffsetX;
            curOffsetY = animPlaying.startOffsetY;          
        }
        else
        {
            scale = animPlaying.mySprites[prevIndex].scale;
            opacity = animPlaying.mySprites[prevIndex].opacity;
            rotation = animPlaying.mySprites[prevIndex].rotation;
            curOffsetX = animPlaying.mySprites[prevIndex].offsetX;
            curOffsetY = animPlaying.mySprites[prevIndex].offsetY;
        }


        float alphaValue = opacity * opacityMod;
        if (alphaSetExternally)
        {
            if (!IsUIElement)
            {
                alphaValue = mySpriteRenderer.color.a;
            }
            else
            {
                alphaValue = myImage.color.a;
            }
            
        }
        if (!IsUIElement)
        {
            mySpriteRenderer.color = new Color(mySpriteRenderer.color.r, mySpriteRenderer.color.g, mySpriteRenderer.color.b, alphaValue);
        }
        else
        {
            myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, alphaValue);
        }
        
        if (!animPlaying.ignoreScale)
        {
            transform.localScale = new Vector3(scale, scale, scale);
        }

        // scaler.designSizeW = scale;

        timeAtFrameStart = Time.time;
        curFrameOpacity = opacity;
        curFrameScale = scale;
        curFrameRotation = rotation;

        nextFrameOpacity = animPlaying.mySprites[index].opacity;
        nextFrameScale = animPlaying.mySprites[index].scale;
        nextFrameRotation = animPlaying.mySprites[index].rotation;

        // Lerp distance?
        nextOffsetX = animPlaying.mySprites[index].offsetX;
        nextOffsetY = animPlaying.mySprites[index].offsetY;

        lerpingOpacity = false;
        lerpingScale = false;
        lerpingRotation = false;
        lerpingPosition = false;
        if (curFrameOpacity != nextFrameOpacity)
        {
            lerpingOpacity = true;
        }
        if (curFrameScale != nextFrameScale)
        {
            lerpingScale = true;
        }
        if (nextFrameRotation != 0)
        {
            lerpingRotation = true;
        }

        if (!animPlaying.smoothStepMovement)
        {
            if (((curOffsetX != nextOffsetX) || (curOffsetY != nextOffsetY)) && (!animPlaying.mySprites[index].offsetOnFramesOnly))
            {
                lerpingPosition = true;
                originalPosition = transform.position;
                nextPosition = new Vector2(transform.position.x + nextOffsetX, transform.position.y + nextOffsetY);
            }
        }
        else
        {
            lerpingPosition = true;
            originalPosition = transform.position;
            nextPosition = new Vector2(transform.position.x + nextOffsetX, transform.position.y + nextOffsetY);
        }

        if ((nextOffsetY != 0) || (curOffsetY != 0))
        {
            //Debug.Log(gameObject.name + " " + transform.position + " " + animPlaying.startOffsetY + " " + curOffsetY + " " + nextOffsetY);
        }


        nextFrameRotation = nextFrameRotation + curFrameRotation;
    }

    public void StopAnimation()
    {
        animPlaying = null;
        validAnimationSet = false;
        sleep = true;
    }

    public void FlipSpriteY()
    {
        mySpriteRenderer.flipY = !mySpriteRenderer.flipY;
    }

    public void FlipSpriteXFromDirectionalAnim(bool value, bool bEvenIfIAmAUIElementBecauseSometimesIWantThatToo =false)
    {
        if (IsUIElement && !bEvenIfIAmAUIElementBecauseSometimesIWantThatToo)
        {
            myImage.rectTransform.localRotation = Quaternion.Euler(0, value ? 180 : 0, 0);
        }
        else
        {
            mySpriteRenderer.flipX = value;
        }

        
        directionFlippedFromAnimation = true;
    }

    public void ResetAnim() {
        sleep = false;
        spriteTimer = 0;
        spriteIndex = 0;
		indexUp = true;
		animComplete = false;

        // This call is pricey, can we please do better
        if (animPlaying != null && animPlaying.mySprites != null && animPlaying.mySprites.Count > 0)
        {
            animPlaying.timePassedDuringAnim = 0f;
            spriteIndex = adjustedSpriteStartIdx % animPlaying.mySprites.Count;
            adjustedSpriteStartIdx = 0;

            if (animPlaying.maxRandomFrame > 0)
            {
                spriteIndex = UnityEngine.Random.Range(0, animPlaying.maxRandomFrame);
            }
        }
    }

	public zirconAnim FindAnim(string name) {
		for (int i = 0; i < myAnimations.Count; i++) {
			if (string.Equals(myAnimations[i].animName, name)) {
				return myAnimations[i];
			}
		}
		return null;
	}

    public void SetAnimConditional(string name)
    {
        if (animPlaying != null && animPlaying.animName == name) return;

        SetAnim(name);
    }

    public void SetAnimWithDirectionalBackup(string name, string backup, Directions dir1, Directions dir2)
    {
        for (int i = 0; i < myAnimations.Count; i++)
        {
            if (myAnimations[i].animName == name)
            {
                SetAnim(name);
                return;
            }
        }
        SetAnimDirectional(backup, dir1, dir2);
    }

    public void SetAnimIfStopped(string name)
    {
        if (TDInputHandler.directionalInput)
        {
            return;
        }
        SetAnim(name);
    }

	public void SetAnim(string name)
    {
        bool animPlayingIsNull = animPlaying == null;

		if (!animPlayingIsNull && animPlaying.isAttackAnimation)
        {
			// Don't interrupt attack animation.
        	if (!animComplete && !name.Contains("Attack"))
            {
                if (GetCompletionPercentage() < 0.8f)
                {
                    return;
                }
            }
        	else if (!animComplete) 
        	{
				ResetAnim();
				return;
        	}
        }

        zirconAnim oldAnim = animPlaying;

        animPlaying = null;
        validAnimationSet = false;
        sleep = false;
	    speedMultiplier = 1.0f;

        if (!srFound)
        {
            mySpriteRenderer = GetComponent<SpriteRenderer>();
            if (mySpriteRenderer != null)
            {
                srFound = true;
            }
        }

        //Find the animation we asked for.
        for (int i = 0; i < myAnimations.Count; i++)
        {
			if (string.Equals(myAnimations[i].animName, name))
            {
				animPlaying = myAnimations[i];
                validAnimationSet = true;
                break;
			}
		}

        animPlayingIsNull = animPlaying == null;

        //If we asked for Idle, and if we didn't find it, look for Default.
        if (name == defaultIdleAnimationName && animPlayingIsNull)
        {
			for (int i = 0; i < myAnimations.Count; i++)
            {
				if (string.Equals(myAnimations[i].animName, "Default"))
                {
					animPlaying = myAnimations[i];
                    validAnimationSet = true;
				}
			}			
		}
        //Otherwise if we asked for Default, and didn't find it, look for Idle.
		else if (name == "Default" && animPlaying == null)
        {
			for (int i = 0; i < myAnimations.Count; i++) {
				if (string.Equals(myAnimations[i].animName, defaultIdleAnimationName)) {
					animPlaying = myAnimations[i];
                    validAnimationSet = true;
				}
			}			
		}
        //Lol just kidding about all of that if we don't have a sprite renderer and are a not a UIElement.
        if (!srFound && !IsUIElement)
        {
            deferStart = true;
            return;
        }

        //If we don't have an animation by this point, search frantically for the first anim called Idle or Default
        if (animPlaying == null)
        {
            for (int i = 0; i < myAnimations.Count; i++)
            {
                if (string.Equals(myAnimations[i].animName, defaultIdleAnimationName) || string.Equals(myAnimations[i].animName, "Default"))
                {
                    animPlaying = myAnimations[i];
                    validAnimationSet = true;
                }
            }
        }

        //give up
	    if (animPlaying == null)
	    {
	        return;
	    }


        //We're starting a new anim here
	    animComplete = false;
        calcGameWaitTime = 0.0f;
        for(int i = 0; i < animPlaying.mySprites.Count; i++)
        {
            calcGameWaitTime += animPlaying.mySprites[i].spriteTime;
        }

        if (Random.Range(0, 1f) < animPlaying.chanceToFlipX && !IsUIElement)
        {
            mySpriteRenderer.flipX = !(mySpriteRenderer.flipX);
        }

        if (Random.Range(0, 1f) < animPlaying.chanceToFlipY && !IsUIElement)
        {
            mySpriteRenderer.flipY = !(mySpriteRenderer.flipY);
        }

        animPlaying.calculatedLength = 0f;
        animPlaying.timePassedDuringAnim = 0f;
        for (int i = 0; i < animPlaying.mySprites.Count; i++)
        {
            animPlaying.calculatedLength += animPlaying.mySprites[i].spriteTime;
        }

        //If we changed animations, and have asked for an offset, set it up here.

        bool varyInitial = false;
        if (oldAnim != animPlaying)
        {
            if (!animPlaying.fixedAnimStartTimer)
            {
                spriteTimer += Random.Range(-0.15f, 0.15f); // Experimental, make animations look more staggered.
            }
            if (adjustedSpriteStartIdx != 0)
            {
                //adjust the sprite index if we asked to start on a non zero sprite
                spriteIndex = adjustedSpriteStartIdx % animPlaying.mySprites.Count;
                adjustedSpriteStartIdx = 0;
                if (animPlaying.maxRandomFrame > 0)
                {
                    spriteIndex = UnityEngine.Random.Range(0, animPlaying.maxRandomFrame);
                }

                SetInitialOffsets();
                GetLerpData(animPlaying.mySprites[spriteIndex], 0);
                varyInitial = true;
            }
        }
        if (!varyInitial)
	    {
	        SetInitialOffsets();
	        GetLerpData(animPlaying.mySprites[0], 0); 
        }

        //Debug.Log("Animation set! " + name + ", num sprites? " + animPlaying.mySprites.Count + " anim name? " + animPlaying.animName);
    }

	public zirconAnim GetAnim() {
		return animPlaying;
	}

	public virtual bool AnimComplete() {
		return animComplete;
	}

    public void AdjustAnimTiming(float fPercentComplete)
    {
        //if the sprite is playing, move to a different frame
        if (animPlaying != null)
        {
            spriteIndex = (int) Mathf.Lerp(0f, animPlaying.mySprites.Count, fPercentComplete);

            //Set this value here in case the animation is reset, we want to catch the 
            //next reset, and start on a different frame
            adjustedSpriteStartIdx = spriteIndex;
        }
        //otherwise, set a random number that will be used (modulo sprite count) when the next animation starts
        else
        {
            adjustedSpriteStartIdx = Random.Range(0, 100);
        }
    }

    public void OverrideCompletionBehavior(string loop)
    {
        //Debug.Log("Override loop to be " + loop + " on " + gameObject.name);
        animPlaying.completionLogic = loop;
        completeLogic = loop;
    }

    public void PlayDynamicAnimFromSprite(Sprite[] spriteList, float fFrameSpeedSeconds, bool bLoop)
    {
        var dynamicAnim = new zirconAnim();
        var animFrames = new List<zirconAnim.AnimationFrameData>();

        for (int t = 0; t < spriteList.Length; t++)
        {
            var fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spriteList[t];
            fram.spriteTime = fFrameSpeedSeconds;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        dynamicAnim.animName = "dynamic";
        dynamicAnim.setSprite(animFrames);
        dynamicAnim.completionLogic = bLoop ? "Loop" : "Stop";
        dynamicAnim.startOpacity = 1.0f;
        dynamicAnim.startScale = 1.0f; 

        StopAnimation();
        myAnimations.RemoveAll(a => a.animName == "dynamic");
        myAnimations.Add(dynamicAnim);
        SetAnim(dynamicAnim);
    }
    
    /// <summary>
    /// Allows for a dynamic anim to have multiple frame speeds
    /// </summary>
    /// <param name="spriteList"></param>
    /// <param name="frameSpeedSeconds"></param>
    /// <param name="bLoop"></param>
    public void PlayDynamicAnimFromSprite(Sprite[] spriteList, float[] frameSpeedSeconds, bool bLoop)
    {
        var dynamicAnim = new zirconAnim();
        var animFrames = new List<zirconAnim.AnimationFrameData>();

        for (int t = 0; t < spriteList.Length; t++)
        {
            var fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spriteList[t];
            fram.spriteTime = frameSpeedSeconds[t];
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        dynamicAnim.animName = "dynamic";
        dynamicAnim.setSprite(animFrames);
        dynamicAnim.completionLogic = bLoop ? "Loop" : "Stop";
        dynamicAnim.startOpacity = 1.0f;
        dynamicAnim.startScale = 1.0f; 

        StopAnimation();
        myAnimations.RemoveAll(a => a.animName == "dynamic");
        myAnimations.Add(dynamicAnim);
        SetAnim(dynamicAnim);
    }
}
