using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class DecorativeScreenObject
{
    /// <summary>
    /// Should this be updated or not?
    /// </summary>
    public bool isActive;

    /// <summary>
    /// Stored in 0-1 screen space, where 0,0 is bottom left.
    /// </summary>
    public Vector2 currentPosition;

    /// <summary>
    /// The direction and speed of motion per second, stored in 0-1 screen space.
    /// </summary>
    public Vector2 moveVelocity;

    /// <summary>
    /// The SpriteRenderer on the destructible we're moving around.
    /// </summary>
    public SpriteRenderer mySR;

    /// <summary>
    /// Hang on to this so we can replace the destructible when we are done.
    /// </summary>
    private string destructiblePrefabName;

    /// <summary>
    /// If you would like to do something with this object on Update, assign an action here.
    /// </summary>
    public Func<DecorativeScreenObject, Vector2> updateAndGetPosition;

    /// <summary>
    /// If you would like to do something with this object on LateUpdate, assign an action here.
    /// </summary>
    public Action<DecorativeScreenObject> onLateUpdate;

    /// <summary>
    /// If you would like to do something when this object leaves the screen after moving, assign an action here.
    /// </summary>
    public Action<DecorativeScreenObject> onMovementFinished;

    /// <summary>
    /// A random value to make sure every effect doesn't animate at the exact same time.
    /// </summary>
    public float randomTimeOffset;

    /// <summary>
    /// Set to true the first time this object can be seen, and then set to false shortly after it leaves the bounds
    /// of the screen. When that happens, the object is deactivated, and any onMovementFinished calls are made.
    /// </summary>
    public bool visibleOnScreen;

    /// <summary>
    /// Our enforced movement type, if any
    /// </summary>
    public DecorativeScreenEffectMoveType moveType;

    /// <summary>
    /// If true, our position is always consistent with regard to camera view. If false, we do whatever we want
    /// </summary>
    public bool lockToCameraView;

    /// <summary>
    /// If we are not camera locked, we need to know this when setting our position
    /// </summary>
    public Vector2 moveOffsetThisFrame;

    /// <summary>
    /// The Unity GO we're tied to.
    /// </summary>
    public GameObject gameObject
    {
        get { return mySR.gameObject; }
    }

    /// <summary>
    /// Cue to play when we are visible
    /// </summary>
    public string sfxCue;

    /// <summary>
    /// Once visible, chance to play our cue
    /// </summary>
    public float chanceToPlaySfx;

    /// <summary>
    /// Prepare this object for readiness
    /// </summary>
    public void InitializeAndActivate()
    {
        currentPosition = Vector2.zero;
        moveVelocity = Vector2.zero;
        if (mySR != null)
        {
            mySR.enabled = false;
        }
        mySR = null;
        updateAndGetPosition = null;
        onLateUpdate = null;
        isActive = true;
        randomTimeOffset = UnityEngine.Random.value * 6.28f;
        visibleOnScreen = false;       
    }

    /// <summary>
    /// Called once we're in sight.
    /// </summary>
    public void OnVisible(bool canBeSeen)
    {
        if (!visibleOnScreen && canBeSeen)
        {
            // First time coming into view.
            mySR.enabled = true;
        }
        visibleOnScreen = canBeSeen;
        if (visibleOnScreen && UnityEngine.Random.Range(0,1f) <= chanceToPlaySfx && !string.IsNullOrEmpty(sfxCue)
            && !UIManagerScript.AnyInteractableWindowOpen())
        {
            UIManagerScript.PlayCursorSound(sfxCue);
        }
    }

    /// <summary>
    /// Called when this object leaves the screen.
    /// </summary>
    public void Deactivate()
    {
        //this returns the DSO to the pool
        isActive = false;        

        if (onMovementFinished != null)
        {
            onMovementFinished(this);
        }

        //the destructible needs to go too
        if (mySR != null)
        {
            mySR.enabled = false;
            GameMasterScript.ReturnToStack(mySR.gameObject, destructiblePrefabName);
        }        
    }

    /// <summary>
    /// Ties us to an object for updating. The object needs a sprite renderer.
    /// </summary>
    /// <param name="stackName">The stack to return the GO to at the end.</param>
    /// <param name="newGO"></param>
    /// <param name="startPosition">Position in 0-1 screen space</param>
    /// <param name="velocity">Move direction and speed per second in 0-1 screen space</param>
    /// <param name="moveType">What type of motion would you like the object to have? If you select custom, you must
    /// also submit a function in the next parameter.</param>
    /// <param name="customUpdateFunction">A custom movement update function for this object. If you end up using
    /// this custom function in multiple places, consider making it part of the moveType list and adding it to the class.</param>
    public void AssignToGO(string stackName, GameObject newGO, Vector2 startPosition, Vector2 velocity,
        DecorativeScreenEffectMoveType moveType = DecorativeScreenEffectMoveType.direct_travel,
        Func<DecorativeScreenObject, Vector2> customUpdateFunction = null)
    {
        destructiblePrefabName = stackName;
        mySR = newGO.GetComponent<SpriteRenderer>();

        //Take note, this will place the object above the FOW. If you want it below, set it to Hero, 
        //and then adjust the sortingOrder to 9001 or something.
        mySR.sortingLayerName = "Foreground";

        currentPosition = startPosition;
        moveVelocity = velocity;
        switch (moveType)
        {
            case DecorativeScreenEffectMoveType.custom:
                if (customUpdateFunction == null)
                {
                    if (Debug.isDebugBuild) Debug.Log("Assigning object " + newGO.name + " to 'custom' movement requires a custom update function.");
                }

                updateAndGetPosition = customUpdateFunction;
                break;
            case DecorativeScreenEffectMoveType.direct_travel:
                updateAndGetPosition = Update_DirectMovement;
                break;
            case DecorativeScreenEffectMoveType.fall_like_leaf:
                updateAndGetPosition = Update_FallLikeALeaf;
                break;
            default:
                if (Debug.isDebugBuild) Debug.Log("Assigning object " + newGO.name + " to '" + moveType + "' movement, but there's no function to support that.");
                break;
        }

    }

    /// <summary>
    /// Called during lateUpdate. Use this if you want to fiddle with transparency or other visual settings, because
    /// the contained destructible will have already made whatever changes it wants to by then and should no longer
    /// get in your way.
    /// </summary>
    /// <param name="action"></param>
    public void SetOnLateUpdate(Action<DecorativeScreenObject> action)
    {
        onLateUpdate = action;
    }

    /// <summary>
    /// Called when the object has finished moving and left the screen. 
    /// </summary>
    /// <param name="action"></param>
    public void SetOnMovementFinished(Action<DecorativeScreenObject> action)
    {
        onMovementFinished = action;
    }

    /// <summary>
    /// Set the currentPosition based on pixels. 0,0 is the bottom left of the screen.
    /// </summary>
    /// <param name="xPx"></param>
    /// <param name="yPx"></param>
    public void SetPositionPX(int xPx, int yPx)
    {
        currentPosition.x = xPx / (float)Screen.width;
        currentPosition.y = yPx / (float)Screen.height;
    }

    /// <summary>
    /// Set the speed and direction based on pixels per second.
    /// </summary>
    /// <param name="xPx"></param>
    /// <param name="yPx"></param>
    public void SetMoveVelocityPX(int xPx, int yPx)
    {
        moveVelocity.x = xPx / (float)Screen.width;
        moveVelocity.y = yPx / (float)Screen.height;
    }

    /// <summary>
    /// Moves the object in a straight line. 
    /// </summary>
    /// <param name="dso"></param>
    /// <returns></returns>
    public static Vector2 Update_DirectMovement(DecorativeScreenObject dso)
    {        
        //update our core position
        Vector2 movement = dso.moveVelocity * Time.deltaTime;
        //Debug.Log("We are at " + dso.currentPosition.x + "," + dso.currentPosition.y + " and we would move " + movement.x +"," + movement.y + " but also add " + dso.moveOffsetThisFrame.x + "," + dso.moveOffsetThisFrame.y);
        dso.currentPosition += movement - dso.moveOffsetThisFrame;

        //yep
        return dso.currentPosition;
    }


    /// <summary>
    /// Moves the object's core position, but also adds adjustment values based on a gentle swaying motion.
    /// The returned value will be different from the object's core position.
    /// </summary>
    /// <param name="dso"></param>
    /// <returns></returns>
    public static Vector2 Update_FallLikeALeaf(DecorativeScreenObject dso)
    {
        //update our core position
        dso.currentPosition += (dso.moveVelocity * Time.deltaTime) + dso.moveOffsetThisFrame;

        //adjust for a smooth swaying motion
        float timer = (Time.realtimeSinceStartup + dso.randomTimeOffset) * 1.57f;
        float sinResult = Mathf.Sin(timer);
        sinResult *= sinResult *= sinResult;
        sinResult = Mathf.Abs(sinResult) * -1f;
        float verticalOffset = sinResult * 0.12f;

        float horizontalOffset = Mathf.Cos(timer) * 0.12f;

        return new Vector2(dso.currentPosition.x + horizontalOffset, dso.currentPosition.y + verticalOffset);
    }

    public DecorativeScreenObject(string cue = "", float sfxChance = 0, DecorativeScreenEffectMoveType forceMoveType = DecorativeScreenEffectMoveType.direct_travel, bool lockToCamera = true)
    {
        sfxCue = cue;
        chanceToPlaySfx = sfxChance;
        moveType = forceMoveType;
        lockToCameraView = lockToCamera;
    }

    /// <summary>
    /// Copies parameters from a given DSO template
    /// </summary>
    /// <param name="template"></param>
    public void UpdateInfoFromTemplate(DecorativeScreenObject template)
    {
        sfxCue = template.sfxCue;
        chanceToPlaySfx = template.chanceToPlaySfx;
        lockToCameraView = template.lockToCameraView;
    }
}