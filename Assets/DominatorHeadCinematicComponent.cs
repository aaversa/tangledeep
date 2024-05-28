using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

/// <summary>
/// Simple object that makes this head bounce and spin into the air upon spawn
/// </summary>
public class DominatorHeadCinematicComponent : MonoBehaviour
{
	/// <summary>
	/// The bouncing spinning head
	/// </summary>
	[SerializeField]
	private SpriteRenderer childRenderer;

	/// <summary>
	/// Main object that is us.
	/// </summary>
	[SerializeField]
	private SpriteRenderer mainRenderer;

	/// <summary>
	/// Ground shadow
	/// </summary>
	[SerializeField]
	private SpriteRenderer shadowRenderer;

	
	/// <summary>
	/// Overall bounce timer
	/// </summary>
	private float currentAnimationTime;

	private float distanceFromGround;

	/// <summary>
	/// What sort of fancyness are we up to?
	///
	/// 0 == initial bounce
	/// 1 == after bounce, just chillin
	/// 2 == floating up to mid air
	/// 3 == bobbing in air.
	/// </summary>
	private int animationState;
	
	// Use this for initialization
	void Start () 
	{
		//hide our renderer and begin the bounce process for the child
		mainRenderer.enabled = false;

		childRenderer.enabled = true;
	}
	
	// Update is called once per frame
	void Update ()
	{
		switch (animationState)
		{
			case 0:
				//do a bounce
				UpdateBounceSimulation();
				return;
			case 1:
				//maybe spark every now and again?
				return;
			case 2:
				//float upwards and then bob in the air.
				UpdateFloatIntoPosition();
				return;
			case 3:
				//chill out
				UpdateFloatInPlace();
				return;
			default:
				//no more animating
				return;
		}
	}

	/// <summary>
	/// Makes the head bounce a few times up and down before settling.
	/// </summary>
	void UpdateBounceSimulation()
	{
		
		mainRenderer.enabled = false;
		
		distanceFromGround = 0f;
		float rotAngle = 0f;
		//first bounce
		if (currentAnimationTime < 2.0f)
		{
			distanceFromGround = GetBounceYPosition(0, 4.0f, currentAnimationTime, 2.0f);
			rotAngle = 3600 * (currentAnimationTime / 2.0f);
		}
		//second bounce
		else if (currentAnimationTime < 2.5f)
		{
			distanceFromGround = GetBounceYPosition(0, 3.0f, currentAnimationTime - 2.0f, 0.5f);
			rotAngle = 0f;
			rotAngle = 900 * ((currentAnimationTime - 2.0f)/ 0.5f);
		}
		else if (currentAnimationTime < 2.75f)
		{
			distanceFromGround = GetBounceYPosition(0, 1.0f, currentAnimationTime - 2.5f, 0.25f);
			rotAngle = 360f* ((currentAnimationTime - 2.5f)/ 0.25f);
		}
		//done
		else
		{
			childRenderer.enabled = false;
			mainRenderer.enabled = true;
			
			//move to chilling out formation
			animationState = 1;
			return;
		}

		childRenderer.transform.localRotation = Quaternion.Euler(0,0,rotAngle);

		currentAnimationTime += Time.deltaTime;
	}

	/// <summary>
	/// Called from a cutscene, Shara makes the object float with TK
	/// </summary>
	public void StartFloatingAnimation()
	{
		animationState = 2;
		currentAnimationTime = 0f;
	}

	/// <summary>
	/// Lift into the air, then start bobbing when at the goal.
	/// </summary>
	void UpdateFloatIntoPosition()
	{
		mainRenderer.enabled = false;
		childRenderer.enabled = true;
		
		currentAnimationTime += Time.deltaTime;
		var ratio = currentAnimationTime / 0.85f;

		//finish after X seconds and move to the bobbing state
		if (ratio >= 1.0f)
		{
			animationState = 3;
			distanceFromGround = 1.2f;
			currentAnimationTime = 0f;
			return;
		}
		
		//curve a little, using Very Good Math
		
		//Math.easeOutQuad = function (t, b, c, d) {
		//	t /= d;
		//	return -c * t*(t-2) + b;
		//};
		
		//float up to 1.5 units off the ground
		distanceFromGround = -1.2f * ratio * (ratio - 2);

	}

	/// <summary>
	/// We are floating 1.5ish units off the ground
	/// </summary>
	void UpdateFloatInPlace()
	{
		mainRenderer.enabled = false;
		childRenderer.enabled = true;

		currentAnimationTime += Time.deltaTime;
		if (currentAnimationTime > 3.0f)
		{
			currentAnimationTime -= 3.0f;
		}

		var sinRatio = currentAnimationTime / 3.0f;
		sinRatio *= 6.28f;
		
		//float at around 1.2 units off the ground, +/- lil bit
		distanceFromGround = 1.2f + Mathf.Sin( sinRatio ) * 0.12f;
	}
	
	/// <summary>
	/// Make sure our shadow is sized correctly regardless of bounce position.
	/// </summary>
	private void LateUpdate()
	{
		//Seal ourselves if we are done.
		if (animationState == 4)
		{
			mainRenderer.enabled = false;
			childRenderer.enabled = false;
			shadowRenderer.enabled = false;
			return;
		}
		
		childRenderer.transform.localPosition = new Vector3(0, distanceFromGround, 0);
		
		var shadowScale = 1.0f - (0.02f * distanceFromGround);
		shadowRenderer.transform.localScale = new Vector3(shadowScale, shadowScale, shadowScale);
	}

	/// <summary>
	/// Gets a value between min and max based on currentTime where 1/2 of fullBounceTime is max. There is an easing
	/// applied here, similar to a sin wave.
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="currentTime"></param>
	/// <param name="fullBounceTime"></param>
	/// <returns></returns>
	float GetBounceYPosition(float min, float max, float currentTime, float fullBounceTime)
	{
		float ratio = currentTime / fullBounceTime;
		ratio *= 3.14f;

		float range = max - min;
		return min + range * Mathf.Sin(ratio);
	}

	/// <summary>
	/// Just hold still.
	/// </summary>
	/// <returns></returns>
	public Vector3 StopFloatingAnimationAndReturnPosition()
	{
		return childRenderer.transform.position;
	}

	public void CleanUpAndRemove()
	{
		mainRenderer.enabled = false;
		childRenderer.enabled = false;
		shadowRenderer.enabled = false;
		
		Destroy(mainRenderer.gameObject);
	}
}
