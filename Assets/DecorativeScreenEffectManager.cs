using System;
using System.Collections;
using System.Collections.Generic;
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    //using Boo.Lang.Environments;
#endif
using UnityEngine;
using Random = System.Random;

/*
 *	DecorativeScreenEffectManager lets you hand over game object prefabs and have them move around on screen
 *  in a method that doesn't go through the destructible/movable chain. 
 *
 *	Assign a start position, a velocity vector (direction and speed), and then select a movement type.
 *  Currently there's direct motion, which just moves the object along in a straight line,
 *  And fall like a leaf, which does neato swaying for the object on the way down.
 *
 *  both functions don't have gravity, you could assign something to move in any direction using fall like a leaf
 *  just to play with it.
 *
 *  You can write a custom function for updating as well, and pass that in, but if you find yourself using
 *  that custom function often, consider adding it to the class below and adding an enum for it as well.
 *
 *  There are hooks for update, lateupdate, and for when the object finishes moving off the screen.
 *
 *	= update hook is mandatory and controls the motion of the object.
 *
 *  = lateupdate is optional, and should be used if you want to do things like mess with opacity. Doing it in lateupdate
 *  means you won't have to worry about fighting against the code in destructible/zirconAnim
 * 
 *	= onMotionFinished is also optional. You can use this to spawn a new object once the object leaves the screen to
 *  create a looping snow/leaf fall, or play a sound, or do whatever.
 *
 *  All the DecorativeScreenObjects (DSO) here are pooled, and the string you pass in for a prefab is used via TDInstantiate to grab your
 *  gameObject from the stack. When the DSO leaves, it is returned to the pool and the gameObject you assigned it
 *  is returned to its stack as well.
 *
 *	
 *
 *
 * 
 */

public enum DecorativeScreenEffectMoveType
{
	custom,				//use a special function from a different class just for this object.	
	direct_travel,		//move in a straight line	
	fall_like_leaf,		//move down, 
	max,
	
}

public class DecorativeScreenEffectManager : MonoBehaviour
{
	private static DecorativeScreenEffectManager instance;

	private Resolution cachedScreenRes;

	private float screenUnitsWide;
	private float screenUnitsTall;

	private List<DecorativeScreenObject> listDSO;

    private static bool activelyGeneratingObjects;
    private static float timeAtLastObjectCreation;

    public Vector2 timeIntervalBetweenObjects; // Min, Max time in seconds.
    public float chanceOfMultiObjects; // If this happens, a bunch of objects are spawned one after the other
    public int minPossibleMultiObjects;
    public int maxPossibleMultiObjects;
    public Vector2 timeIntervalBetweenMultiObjects;

    [Header("Object and Movement Types")]
    public float chanceOfDirectMovement;

    [Header("Direct Movement Options")]
    public float chanceOfUpwardsDirectMovement;

    static Vector3 cameraPositionInUnityUnitsLastFrame;

    /// <summary>
    /// These templates store prefab + SFX data
    /// </summary>
    Dictionary<string, DecorativeScreenObject> dsoTemplates = new Dictionary<string, DecorativeScreenObject>()
    {
        {  "FlyingWhiteBird", new DecorativeScreenObject(cue:"BirdChirp", sfxChance:0.1f, forceMoveType: DecorativeScreenEffectMoveType.direct_travel, lockToCamera: false) }
    };

    void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;

		Initialize();
	}

	void Initialize()
	{
		CalculateScreenBounds();
		
		listDSO = new List<DecorativeScreenObject>();
		for (int t = 0; t < 10; t++)
		{
			listDSO.Add( new DecorativeScreenObject());
		}
	}
	
	void Start () 
	{
		
	}

    /// <summary>
    /// Waits for n seconds and then generates 1 or more objects. After these are set in motion, calls itself again.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator WaitThenGenerateFallingStuff(float time)
    {
        yield return new WaitForSeconds(time);
        if (!activelyGeneratingObjects)
        {
            yield break;
        }

        bool generateMultiObjects = UnityEngine.Random.Range(0, 1f) <= chanceOfMultiObjects;
        int numObjects = 1;
        if (generateMultiObjects)
        {
            numObjects = UnityEngine.Random.Range(minPossibleMultiObjects, maxPossibleMultiObjects + 1);
        }

        // #todo: Choose a prefab at random, which will inform future choices
        string prefabToUse = "FlyingWhiteBird";

        DecorativeScreenEffectMoveType moveType = dsoTemplates[prefabToUse].moveType;

        if (moveType != DecorativeScreenEffectMoveType.direct_travel)
        {
            // We don't want multiple leaf-type movements because it kinda looks dumb right now
            numObjects = 1;            
        }
        
        Vector2 velocity = GetVelocityForMovementType(moveType);
        Vector2 position = new Vector2(UnityEngine.Random.Range(0.1f,0.9f), UnityEngine.Random.value * 0.2f + 1.3f);

        bool goLeft = UnityEngine.Random.value < (position.x < 0.5f ? 0.4f : 0.6f);
        if (goLeft)
        {
            velocity.x *= -1f;
        }

        bool travelUpward = (moveType == DecorativeScreenEffectMoveType.direct_travel && UnityEngine.Random.Range(0, 1f) <= chanceOfUpwardsDirectMovement);
        if (travelUpward)
        {
            position.y *= -1f;
            velocity.y *= -1f;
        }

        DecorativeScreenObject dso = SpawnNewDSO(prefabToUse,
            position,
            velocity,
            moveType);

        dso.UpdateInfoFromTemplate(dsoTemplates[prefabToUse]);

        // If we're generating more than one object, we want to use the same movement type and just adjust the start position / velocity slightly.
        // These are generated in a tight cluster.
        float runningTime = 0f;
        for (int i = 1; i < numObjects; i++)
        {
            Vector2 newPos = new Vector2(UnityEngine.Random.Range(position.x * 0.8f, position.x * 1.2f), UnityEngine.Random.Range(position.y * 0.8f, position.y * 1.2f));
            Vector2 newVelocity = new Vector2(UnityEngine.Random.Range(velocity.x * 0.85f, velocity.x * 1.15f), UnityEngine.Random.Range(velocity.y * 0.9f, velocity.y * 1.1f));
            runningTime += UnityEngine.Random.Range(timeIntervalBetweenMultiObjects.x, timeIntervalBetweenMultiObjects.y);
            StartCoroutine(WaitThenGenerateSpecificThing(runningTime, prefabToUse, newPos, newVelocity, moveType));
        }

        // Now set up a coroutine to spawn the NEXT thing or set of things.
        StartCoroutine(WaitThenGenerateFallingStuff(UnityEngine.Random.Range(timeIntervalBetweenObjects.x, timeIntervalBetweenObjects.y)));
    }

    /// <summary>
    /// Waits for time, then generates a specific DSO with given parameters.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="prefab"></param>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    /// <param name="moveType"></param>
    /// <returns></returns>
    IEnumerator WaitThenGenerateSpecificThing(float time, string prefab, Vector2 pos, Vector2 vel, DecorativeScreenEffectMoveType moveType)
    {
        yield return new WaitForSeconds(time);

        if (!activelyGeneratingObjects)
        {
            yield break;
        }

        DecorativeScreenObject dso = SpawnNewDSO(prefab,
            pos,
            vel,
            moveType);

        dso.UpdateInfoFromTemplate(dsoTemplates[prefab]);
    }

    DecorativeScreenEffectMoveType GenerateRandomMoveType()
    {
        float roll = UnityEngine.Random.Range(0, 1f);
        if (roll <= chanceOfDirectMovement)
        {
            return DecorativeScreenEffectMoveType.direct_travel;
        }
        return DecorativeScreenEffectMoveType.fall_like_leaf;
    }

    Vector2 GetVelocityForMovementType(DecorativeScreenEffectMoveType moveType)
    {
        switch(moveType)
        {
            case DecorativeScreenEffectMoveType.direct_travel:
                Vector2 baseVelo = new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), (UnityEngine.Random.Range(-0.6f, -0.1f)));
                return baseVelo;
            case DecorativeScreenEffectMoveType.fall_like_leaf:
            default:
                return new Vector2(0, (UnityEngine.Random.value * -0.2f) - 0.05f);
        }
    }

    public static void EnableFallingStuffInOutsideArea()
    {
        activelyGeneratingObjects = true;
        timeAtLastObjectCreation = Time.realtimeSinceStartup;
        instance.StartCoroutine(instance.WaitThenGenerateFallingStuff(UnityEngine.Random.Range(instance.timeIntervalBetweenObjects.x, instance.timeIntervalBetweenObjects.y)));
    }

    public static void DisableFallingStuffInOutsideArea()
    {
        activelyGeneratingObjects = false;
        foreach(DecorativeScreenObject dso in instance.listDSO)
        {
            dso.Deactivate();
        }

        instance.listDSO.Clear();
    }

    void Update ()
	{
        if (!activelyGeneratingObjects)
        {
            return;
        }

		//This is only called again if the resolution changes mid-play.
		if (Screen.currentResolution.width != cachedScreenRes.width)
		{
			CalculateScreenBounds();
		}

        Vector2 cameraMoveDelta = gameObject.transform.position - cameraPositionInUnityUnitsLastFrame;
        // Convert this into % of screen for use by the DSOs
        cameraMoveDelta.x /= screenUnitsWide;
        cameraMoveDelta.y /= screenUnitsTall;

        foreach (DecorativeScreenObject dso in listDSO)
		{
			if (!dso.isActive) continue;
			Vector2 drawPosition = dso.currentPosition;

            dso.moveOffsetThisFrame = Vector2.zero;

            // If we are not locked to camera, then we want to cancel out the camera (our parent) motion
            if (!dso.lockToCameraView)
            {
                dso.moveOffsetThisFrame = cameraMoveDelta;
            }

            //if we have an update function, use it. If we don't, something is likely wrong.
            //null check is costly tho 
            //if (dso.updateAndGetPosition != null) 
			{
				drawPosition = dso.updateAndGetPosition(dso);
			}

            PositionObjectOnScreen(dso.gameObject, drawPosition);


            //check to see if we're visible. There's a 10% bonus bound as a guess, since these sprites will all
            //be different sizes. If that's not enough bounds, mess with it some here.
            var canBeSeen = drawPosition.x > -0.25 &&
			                drawPosition.x < 1.25 &&
			                drawPosition.y > -0.25 &&
			                drawPosition.y < 1.25;

            bool deactivated = false;

			//if we were not visible before maybe we are now.
			if (!dso.visibleOnScreen)
			{
                dso.OnVisible(canBeSeen);				
			}
			//but if we were visible, and now we're not, we are done.
			else if (canBeSeen == false)
			{
				dso.Deactivate();
                deactivated = true;
			}

            // if for some reason we are way, way off the screen, we're done
            if (!deactivated && (drawPosition.x > 5 || drawPosition.x < -5 || drawPosition.y > 5 || drawPosition.y < -5))
            {
                dso.Deactivate();
            }



        }

        cameraPositionInUnityUnitsLastFrame = gameObject.transform.position;

    }

	/// <summary>
	/// Make changes to the object after all the rest of the game has updated this tick. You shouldn't be adjusting
	/// position in here unless you've got a solid reason.
	/// </summary>
	private void LateUpdate()
	{
		foreach (DecorativeScreenObject dso in listDSO)
		{
			if (!dso.isActive ||
			    dso.onLateUpdate == null )
				continue;

			dso.onLateUpdate(dso);
		}
	}

	/// <summary>
	/// Store new values for the size of the screen
	/// </summary>
	void CalculateScreenBounds()
	{
		var orthoSize = Camera.main.orthographicSize;
		screenUnitsTall = orthoSize * 2;
		screenUnitsWide = (Screen.width / (float)Screen.height) * screenUnitsTall;
		cachedScreenRes = Screen.currentResolution;
	}

	void PositionObjectOnScreen(GameObject go, Vector2 screenPos)
	{
		//screenPos is based on a 0-1 rect for the whole screen.
		//values can be <0 or >1 to represent things off screen just a bit.
		
		//we want to translate those values into actual Unity positions.
		
		//This manager is at the camera, so any child of us with localPosition 0,0,0 is going to be dead center screen (0.5, 0.5)
		Vector3 unityPosFromScreen = new Vector3(
				(screenPos.x - 0.5f) * screenUnitsWide,
				(screenPos.y - 0.5f) * screenUnitsTall,
				3f
			);

		go.transform.localPosition = unityPosFromScreen;
		
	}
	
	/// <summary>
	/// Grab one from the pool, or make a new one if we have to.
	/// </summary>
	/// <returns></returns>
	DecorativeScreenObject GetDSOFromPool()
	{
		for (int t = 0; t < listDSO.Count; t++)
		{
			//if we have an inactive one, wake it up and send it out.
			var dso = listDSO[t];
			if (!dso.isActive)
			{
				dso.InitializeAndActivate();	
				return dso;
			}
		}
		
		//we need to make a new one, all our current ones are used up.
		var newDSO = new DecorativeScreenObject();
		newDSO.InitializeAndActivate();
		listDSO.Add(newDSO);
		return newDSO;
	}

	/// <summary>
	/// Create a DecorativeScreenObject based off a destructible, give it a position, velocity, and some optional behaviors.
	/// </summary>
	/// <param name="destructiblePrefab">The pretty object we want to move around.</param>
	/// <param name="startPosition">Position in 0-1 screen space</param>
	/// <param name="velocity">Move direction and speed per second in 0-1 screen space</param>
	/// <param name="moveType">What type of motion would you like the object to have? If you select custom, you must
	/// also submit a function in the next parameter.</param>
	/// <param name="customUpdateFunction">A custom movement update function for this object. If you end up using
	/// this custom function in multiple places, consider making it part of the moveType list and adding it to the class.</param>
	public static DecorativeScreenObject SpawnNewDSO(string destructiblePrefab, Vector2 startPosition, Vector2 velocity,
		DecorativeScreenEffectMoveType moveType = DecorativeScreenEffectMoveType.direct_travel,
		Func<DecorativeScreenObject, Vector2> customUpdateFunction = null)
	{
		//If this is null, this call will fail, so don't do that.
		var destructibleGO = GameMasterScript.TDInstantiate(destructiblePrefab);
        destructibleGO.transform.SetParent(instance.transform);
		
		var newDSO = instance.GetDSOFromPool();
		newDSO.AssignToGO(destructiblePrefab, destructibleGO, startPosition, velocity, moveType, customUpdateFunction);

#if UNITY_EDITOR
        //Debug.Log("Spawned DSO at " + startPosition + " with velocity " + velocity.x + "," + velocity.y + " move type " + moveType);
#endif

        string anim = "Idle";
        
        if (startPosition.y < 0f)
        {
            // started from the bottom, now we're going up, so flip us around
            anim = "IdleTop";
        }

        destructibleGO.GetComponent<Animatable>().SetAnim(anim);

        return newDSO;
	}
	
	/// <summary>
	/// Create a DecorativeScreenObject based off a destructible, give it a position, velocity, and some optional behaviors.
	/// </summary>
	/// <param name="destructiblePrefab">The pretty object we want to move around.</param>
	/// <param name="startPositionPX">Position in pixel space</param>
	/// <param name="velocityPX">Move direction and speed per second in pixel space</param>
	/// <param name="moveType">What type of motion would you like the object to have? If you select custom, you must
	/// also submit a function in the next parameter.</param>
	/// <param name="customUpdateFunction">A custom movement update function for this object. If you end up using
	/// this custom function in multiple places, consider making it part of the moveType list and adding it to the class.</param>
	public static DecorativeScreenObject SpawnNewDSO(string destructiblePrefab, Vector2Int startPositionPX, 
		Vector2Int velocityPX, DecorativeScreenEffectMoveType moveType = DecorativeScreenEffectMoveType.direct_travel,
		Func<DecorativeScreenObject, Vector2> customUpdateFunction = null)
	{
		//Call the assignment function above, then set the position and velocity via pixel values below.
		var newDSO = SpawnNewDSO(destructiblePrefab, Vector2.zero, Vector2.zero, moveType,
			customUpdateFunction);
		
		newDSO.SetPositionPX(startPositionPX.x, startPositionPX.y);
		newDSO.SetMoveVelocityPX(velocityPX.x, velocityPX.y);

		return newDSO;
	}

}


