using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

[System.Serializable]
public class SharaTKSprite
{
	public SpriteRenderer spriteRenderer;
	public float startingAlpha;
	
	[Tooltip("The alpha here gets overwritten very quickly.")]
	public Color blendColor;
}

/// <summary>
/// Audio / Visual effect for Shara's melee weapon attacks
/// </summary>
public class SharaWeaponTKEffect : MonoBehaviour
{
	[Tooltip("The Sprite Renderers that make up the weapon trail.")]
	[SerializeField]
	private List<SharaTKSprite> weaponSpriteCollection;

	[Tooltip("The amount of time in seconds for a full fade from 1.0 to 0.0 alpha")]
	[SerializeField]
	private float weaponFadeTime;
	
	[Header("The spinning weapon")]
	[SerializeField] 
	private SpriteRenderer weaponSpinning;
	[SerializeField] 
	private float spinDegreesPerSecond;
	[Tooltip("Start fading at this time...")]
	[SerializeField] 
	private float spinFadeAfterTime;
	[Tooltip("...and be done by this time.")]
	[SerializeField] 
	private float spinFullFadeTime;
	
	[Tooltip("Axes hit every enemy around us.")]
	[SerializeField]
	private float axeFullRotationDegreesPerSecond = 1440.0f;

	/// <summary>
	/// Keep track of how long we've been active.
	/// </summary>
	private float spinTimer;

	[Header("Start and end offsets for weapon spin.")]
	[SerializeField] 
	private Vector2 spinOffsetStart;
	[SerializeField] 
	private Vector2 spinOffsetEnd;
	

	/// <summary>
	/// A counter that turns us off when it reaches 0.
	/// </summary>
	private float remainingFadeTime;

	/// <summary>
	/// you know
	/// </summary>
	private bool isActive;

	/// <summary>
	/// Flip the draw direction of the attacks sometimes because fun.
	/// </summary>
	private bool isMirrored;

	/// <summary>
	/// Most recent weapon facing.
	/// </summary>
	private Directions facing;

	/// <summary>
	/// Make sure to update this with player's actual weapon
	/// </summary>
	private string currentActiveWeaponSpriteRef;

	/// <summary>
	/// Axes do full spins around the hero
	/// </summary>
	private bool weaponIsAxe;
	
	
	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (GameMasterScript.heroPCActor == null)
		{
			return;
		}

		//Make sure we're using the same sprite as the hero is.
		var hero = GameMasterScript.heroPCActor;
		var activeWeapon = hero.myEquipment.equipment[(int) EquipmentSlots.WEAPON] as Weapon;
		if (activeWeapon != null &&  
		    currentActiveWeaponSpriteRef != activeWeapon.spriteRef)
		{
			currentActiveWeaponSpriteRef = activeWeapon.spriteRef;
			weaponIsAxe = activeWeapon.weaponType == WeaponTypes.AXE;
			
			var newSprite = UIManagerScript.LoadSpriteFromDict(UIManagerScript.dictItemGraphics, currentActiveWeaponSpriteRef);
			foreach (var s in weaponSpriteCollection)
			{
				s.spriteRenderer.sprite = newSprite;
			}

			weaponSpinning.sprite = newSprite;
		}
		
		if (isActive)
		{
			//if we have no time left, go away.
			if (spinTimer >= spinFullFadeTime)
			{
				Deactivate();
				return;
			}
			
			//The longer we live the more we fade
			var amountToFade = 1.0f - remainingFadeTime / weaponFadeTime;
			
			//easings
			amountToFade *= amountToFade;
			
			foreach (var s in weaponSpriteCollection)
			{
				var fadeAlpha = s.startingAlpha - amountToFade;
				Color c = s.blendColor;
				c.a = fadeAlpha;
				s.spriteRenderer.color = c;
			}
			
			//spin the spinblade
			weaponSpinning.transform.localRotation = 
				Quaternion.Euler(0,0, spinDegreesPerSecond * (Time.realtimeSinceStartup % 600.0f) * -1.0f);
			
			
			//fade the blade even less than the trails
			var bladeAlpha = 1.0f;
			var fadeDelta = (spinTimer - spinFadeAfterTime) / (spinFullFadeTime - spinFadeAfterTime);
			if (spinTimer > spinFadeAfterTime)
			{
				bladeAlpha = 1.0f - fadeDelta;
			}
			weaponSpinning.color = new Color(1,1,1, bladeAlpha);

			//move the spinblade based on the full fade timing. (🎶spinning child on go-kart song plays during lerp🎶)
			var bladeOffsetXY = Vector2.Lerp(spinOffsetStart, spinOffsetEnd, Mathf.Pow(spinTimer / spinFullFadeTime, 0.33f));
			
			//if we are in the fade process, pull the blade back toward ourselves
			if (fadeDelta >= 0f)
			{
				bladeOffsetXY.y = Mathf.Lerp(bladeOffsetXY.y, 0.2f, fadeDelta);
			}
			
			//scale the weapon down a little after being pulled in
			var weaponScale = 1.2f;
			if (fadeDelta > 0f)
			{
				weaponScale = 1.2f - 0.4f * fadeDelta;
			}
			weaponSpinning.transform.localScale = new Vector3(weaponScale, weaponScale, weaponScale);
			
			weaponSpinning.transform.localPosition = 
				new Vector3( bladeOffsetXY.x, bladeOffsetXY.y, isMirrored ? 1.0f : -1.0f);

			//axes spin all around us.
			if (weaponIsAxe)
			{
				var rot = transform.rotation.eulerAngles;
				rot.z += axeFullRotationDegreesPerSecond * Time.deltaTime * (isMirrored ? 1.0f : -1.0f);
				transform.rotation = Quaternion.Euler(rot);
			}
			
			//keeping two timers is not the very best, but it's that or use even more
			//tortured math.
			remainingFadeTime -= Time.deltaTime;
			spinTimer += Time.deltaTime;

		}
	}

	/// <summary>
	/// Turn on the display, point it at a given direction, and begin fading
	/// </summary>
	/// <param name="dir"></param>
	public void Activate(Directions dir)
	{
		isActive = true;
		remainingFadeTime = weaponFadeTime;

		//set facing to match input.
		facing = dir;

		//Each tick away from North is 45 degrees
		float zRotation = 0f;
		float yRotation = 0f;

		isMirrored = !isMirrored;
		if (isMirrored)
		{
			yRotation = 180f;
			zRotation = 180f + (int) facing * 45.0f;
		}
		else
		{
			yRotation = 0f;
			zRotation = (int) facing * -45.0f;
		}
		
		//fiddle value, it might look better ticked back a touch
		zRotation += 22.5f;

		transform.localRotation = Quaternion.Euler(yRotation,0,zRotation);
		
		//prepare to spin to win
		spinTimer = 0f;
	}

	/// <summary>
	/// Hush, no more.
	/// </summary>
	public void Deactivate()
	{
		isActive = false;
		remainingFadeTime = 0f;
		foreach (var s in weaponSpriteCollection)
		{
			s.spriteRenderer.color = Color.clear;
		}

		weaponSpinning.color = Color.clear;
	}
}
