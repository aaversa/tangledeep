using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a plane behind the dungeon scene, used in case we want to
/// have a scrolling background behind the dungeon. Useful for things like
/// bridges or caverns where we can see beneath us.
/// </summary>
public class DungeonParallaxManager : MonoBehaviour
{
	[Header("Background Motion")]
	[Tooltip("For every 1 unit in the hero's X position, the plane will offset by this much.")]
	public float xShiftPerTile;
	[Tooltip("For every 1 unit in the hero's Y position, the plane will offset by this much.")]
	public float yShiftPerTile;

	private static DungeonParallaxManager _instance;
	private SpriteRenderer sr;

	[Tooltip("Direction and speed in units per second that the texture should scroll at all times.")]
	public Vector2 textureScrollVector;

	private Vector2 currentTextureOffset;

	[Header("Debug")] 
	public bool runInTestScene;

	// Use this for initialization
	void Awake () 
	{
		if (_instance != null)
		{
			Destroy(gameObject);
			return;
		}

		_instance = this;
		sr = GetComponent<SpriteRenderer>();
		sr.material = Instantiate(sr.material);
	}
	
	// Update is called once per frame
	void Update () 
	{
		//our position is based on our shifting values and the cam loc
		if (!GameMasterScript.actualGameStarted && !runInTestScene )
		{
			return;
		}

		Vector3 camPos = Camera.main.transform.position;
		Vector3 localPos = new Vector3(
			camPos.x * xShiftPerTile,
			camPos.y * yShiftPerTile,
			1);

		transform.localPosition = localPos;

		//if you want to adjust textureScrollVector every tick, do so here.
		//
		//for instance, if you want the .y value to change according to a sin wave to have stuff bob up and down,
		//or if you want to accelerate the motion because the wind is picking up or the ship is moving faster.
		//
		//You can also set a co-routine somewhere to change the values over time as well.
		
		//There should be a parallax test .scene in the project you can load to mess with values in the editor,
		//or try some scripting here as well.


	}

	private void LateUpdate()
	{		
		if (!GameMasterScript.actualGameStarted && !runInTestScene )
		{
			return;
		}
		
		//Advance the offset by the vector we've defined
		currentTextureOffset += textureScrollVector * Time.deltaTime;
		
		//Wrap it around between 0~1
		if (currentTextureOffset.x < 0)
		{
			currentTextureOffset.x += 1;
		}
		else if (currentTextureOffset.x > 1)
		{
			currentTextureOffset.x -= 1;
		}
		
		if (currentTextureOffset.y < 0)
		{
			currentTextureOffset.y += 1;
		}
		else if (currentTextureOffset.y > 1)
		{
			currentTextureOffset.y -= 1;
		}

		//Apply it to the material
		sr.material.mainTextureOffset = currentTextureOffset;
		
		/*
		 *	Is it not working?
		 *
		 *  The sprite used for the background must be set to wrap mode Repeat and not Clamp in the texture settings.
		 *
		 * 	The sprite renderer on this object needs to use the Parallax scrolling material.
		 *
		 *  That material should have render queue set to Geometry, higher values might cause other things to not draw.
		 */
	}

	/// <summary>
	/// Removes the sprite so that nothing draws in the background for this next area.
	/// </summary>
	public static void ClearBackground()
	{
		_instance.sr.sprite = null;
		_instance.sr.enabled = false;
	}

	public static void SetBackground(string spriteRef, float xShiftPerTile, float yShiftPerTile, int numTilesToSet)
	{

		_instance.sr.sprite = Resources.Load<Sprite>("Art/Tilesets/" + spriteRef);
		_instance.sr.enabled = true;

		_instance.sr.tileMode = SpriteTileMode.Continuous;
		_instance.sr.size = new Vector2(numTilesToSet, numTilesToSet);

		_instance.xShiftPerTile = xShiftPerTile;
		_instance.yShiftPerTile = yShiftPerTile;
	}

	private void OnDestroy()
	{
		if (sr != null)
		{
			Destroy(sr.material);
			sr.material = null;
		}
	}
}
