using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This component wakes up when the player begins to target an ability or ranged attack,
/// and then goes to sleep when that is over. 
/// </summary>
public class PlayerInputTargetingManager : MonoBehaviour 
{
	/// <summary>
	/// Usually only need one, unless we're doing multi-target stuff
	/// </summary>
	private List<TargetingLineScript> allTargetingLines;

	/// <summary>
	/// Almost always the first one, but sometimes not.
	/// </summary>
	private TargetingLineScript activeTargetingLine;

	/// <summary>
	/// Keep track of the line we're using.
	/// </summary>
	private int idxTargetingLine;

	/// <summary>
	/// The most recent tile we looked at.
	/// </summary>
	private Vector2 lastCheckedTargetLocation;
	
	/// <summary>
	/// Keep track of the direction pressed when we begin targeting, this lets us keep our
	/// ranged attack focus sticky on the target during the initial button press.
	/// </summary>
	private static Directions dirPressedWhenTargetingBegan;

	private static bool shouldBeStickyAboutRangedTargeting;


	private static PlayerInputTargetingManager _instance;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        allTargetingLines = new List<TargetingLineScript>();

    }

	/// <summary>
	/// Activates the manager and creates a line starting at the player and pointing at a given location.
	/// </summary>
	/// <param name="destinationLocation"></param>
	public static void TurnOn(Vector2 destinationLocation)
	{
		_instance._TurnOn(destinationLocation);
	}

	void _TurnOn( Vector2 destinationLocation )
	{
		//create or grab our first line
		if (allTargetingLines.Count < 1)
		{
			allTargetingLines.Add(TargetingLineScript.CreateTargetingLine(GameMasterScript.heroPCActor.GetPos(),
				destinationLocation));
			activeTargetingLine = allTargetingLines[0];
		}
		else
		{
			activeTargetingLine = allTargetingLines[0];
			activeTargetingLine.UpdateStartAndEndPoints(GameMasterScript.heroPCActor.GetPos(),destinationLocation);
		}

		activeTargetingLine.enabled = true;
        activeTargetingLine.gameObject.SetActive(true);
		idxTargetingLine = 0;
		lastCheckedTargetLocation = new Vector2(-1,-1);

        enabled = true;

        //if (Debug.isDebugBuild) Debug.Log("Enabled targeting line");
	}


	/// <summary>
	/// Clears all lines and hides them.
	/// </summary>
	public static void TurnOff()
	{
        foreach (var tl in _instance.allTargetingLines)
		{
			tl.Hide();
		}

		_instance.idxTargetingLine = 0;
		_instance.enabled = false;
		shouldBeStickyAboutRangedTargeting = false;

        //if (Debug.isDebugBuild) Debug.Log("Disabled targeting line");
    }


	/// <summary>
	/// When we start targeting, keep track of whatever direction the stick is pressed in.
	/// If it is not neutral, then ignore that direction until player input no longer matches
	/// that direction.
	///
	/// Meaning, if you're holding left and press the ranged attack button, the targeting tile won't just fly off
	/// to the left. You'll need to press a different direction, or return to neutral then press left. If you aren't
	/// pressing ANY direction when you start targeting, this doesn't apply.
	/// </summary>
	/// <param name="d"></param>
	public static void SetStickyDirectionOnTargetingStart(Directions d)
	{
		if (d == Directions.NEUTRAL) return;

		shouldBeStickyAboutRangedTargeting = true;
		dirPressedWhenTargetingBegan = d;
	}

	/// <summary>
	/// If we just entered targeting mode while holding down a direction, we want to ignore that direction until we
	/// receive a different input.
	/// </summary>
	/// <param name="currentInput">The active direction held by the player</param>
	/// <returns>The passed in direction, unless we're ignoring it, in which case we get neutral.</returns>
	public static Directions ChangePlayerInputBasedOnStickyTargeting(Directions currentInput)
	{
		if (!shouldBeStickyAboutRangedTargeting) return currentInput;

		//ignore the sticky direction
		if (currentInput == dirPressedWhenTargetingBegan)
		{
			return Directions.NEUTRAL;
		}
		
		//we changed!
		dirPressedWhenTargetingBegan = Directions.NEUTRAL;
		shouldBeStickyAboutRangedTargeting = false;
		return currentInput;
	}
	
	
	void Update()
	{
		//while we're alive, we should make sure arrows don't draw.
		Switch_AnalogMovementArrowComponent.RequestDoNotDrawArrowsThisFrame();
		
		
	}


	/// <summary>
	/// Keep track of where the player is pointing and if the tile is valid or not. 
	/// </summary>
	/// <param name="location"></param>
	/// <param name="isGoodTile"></param>
	public static void UpdateCurrentTargetingInformation(Vector2 location, bool isGoodTile)
	{
		if (!_instance.enabled) return; 
		
		_instance._UpdateCurrentTargetingInformation(location, isGoodTile);
	}

	void _UpdateCurrentTargetingInformation(Vector2 location, bool isGoodTile)
	{
		//Don't re-update the same space over and over
		if( location == lastCheckedTargetLocation)
		{
			return;
		}

		lastCheckedTargetLocation = location;
		
		//move the line, change the color
		activeTargetingLine.UpdateEndPoint(location);
		activeTargetingLine.SetColor( isGoodTile ? 
			Color.cyan :
			new Color(0.75f, 0.75f, 0.75f, 0.75f));
            
		//if we were not targeting a valid tile, but now we are, maybe play a sound
		if (isGoodTile)
		{
			//if the player has asked us NOT to play a sound, then don't. This should happen on the very first
			//targeting of a given ranged attack press, so that constant fire doesn't make UI noise all the time.
			if (GameMasterScript.heroPCActor.ReadActorData("ranged_start_no_sound") > 0)
			{
				GameMasterScript.heroPCActor.SetActorData("ranged_start_no_sound", 0); 
			}
			//if the tile is not empty, play a sound
			else
			{
				var mtd = MapMasterScript.GetTile(location);
				if (!mtd.IsEmpty())
				{
					UIManagerScript.PlayCursorSound("UITick");
				}
			}			
		}
	}

	/// <summary>
	/// If the player is using a multi-target ability, do this when confirm is pressed. This version locks the
	/// current line's destination into whatever value you set here. You might need to do this if deep somewhere
	/// in some call targeting is turned on/off. 
	/// </summary>
	/// <param name="currentDestination"></param>
	/// <param name="nextOrigin"></param>
	/// <param name="nextDestination"></param>
	public static void LockCurrentArrowAndActivateNextOne( Vector2 currentDestination,
		Vector2 nextOrigin, Vector2 nextDestination)
	{
		_instance._LockCurrentArrowAndActivateNextOne(currentDestination, nextOrigin, nextDestination);
	}

	private void _LockCurrentArrowAndActivateNextOne(Vector2 currentDestination,
		Vector2 nextOrigin, Vector2 nextDestination)
	{
		//take current arrow and set it to have new values
		activeTargetingLine.UpdateEndPoint(currentDestination);
		
		_LockCurrentArrowAndActivateNextOne(nextOrigin, nextDestination);
	}
	
	/// <summary>
	/// If the player is using a multi-target ability, do this when confirm is pressed.
	/// </summary>
	/// <param name="nextOrigin"></param>
	/// <param name="nextDestination"></param>
	public static void LockCurrentArrowAndActivateNextOne(Vector2 nextOrigin, Vector2 nextDestination)
	{
		_instance._LockCurrentArrowAndActivateNextOne(nextOrigin, nextDestination);
	}

	private void _LockCurrentArrowAndActivateNextOne(Vector2 nextOrigin, Vector2 nextDestination)
	{
		idxTargetingLine++;
		
		//either create a new line
		if (idxTargetingLine >= allTargetingLines.Count())
		{
			var tl = TargetingLineScript.CreateTargetingLine(nextOrigin, nextDestination, 5);
			allTargetingLines.Add(tl);
			activeTargetingLine = tl;
		}
		//or update an existing one
		else
		{
			activeTargetingLine = allTargetingLines[idxTargetingLine];
			activeTargetingLine.enabled = true;
			activeTargetingLine.UpdateStartAndEndPoints(nextOrigin, nextDestination);
			activeTargetingLine.SetColor(Color.cyan);
		}
	}

}
