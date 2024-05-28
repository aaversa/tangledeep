using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeTowerController : MonoBehaviour
{
	/// <summary>
	/// The object in the world that is being assaulted
	/// </summary>
	private Destructible myTower;
	
	/// <summary>
	/// Who owns me?
	/// </summary>
	Map_SlimeDungeon.SlimeStatus mySlimeStatus;

	/// <summary>
	/// Am I being attacked? If so, by what type of slime? 
	/// </summary>
	private Map_SlimeDungeon.SlimeStatus slimeDamageStatus;

	/// <summary>
	/// Other slimes are trying to convert me to their flavor.
	/// </summary>
	private int damageTaken;

	private int damageMax = 20;

	public List<Sprite> damageSprites;
	public SpriteRenderer myDamageRenderer;

	/// <summary>
	/// Set this to true if you want to... convert on the next update! Great for avoiding the perils of calling stuff
	/// after Awake but before Start
	/// </summary>
	private bool bConvertNextUpdate;

    bool towerHasBeenSet = false;

	// Use this for initialization
	void Start ()
	{
		//create a unique copy of this material, since we'll be messing with it.
		myDamageRenderer.material = Material.Instantiate(myDamageRenderer.material);
		mySlimeStatus = Map_SlimeDungeon.SlimeStatus.Count;
	}

	private void OnEnable()
	{
        towerHasBeenSet = false;
		myTower = null;
		mySlimeStatus = Map_SlimeDungeon.SlimeStatus.Count;
	}

    private void OnDisable()
    {
        towerHasBeenSet = false;
        myTower = null;
    }

    // Update is called once per frame
    void Update () 
	{
		//As our destructible is spawned, it may not yet connect to the game object. When it does, we need to update
		//based on who we are aligned to.
		if (!towerHasBeenSet)
		{
			var checkTower = GetComponent<Animatable>().GetOwner() as Destructible;
			if (checkTower != null)
			{
				SetTower(checkTower);
			}
		}
		
		if (bConvertNextUpdate)
		{
			DoConversion(mySlimeStatus);
			bConvertNextUpdate = false;
		}
		
		if (damageTaken == 0)
		{
			myDamageRenderer.enabled = false;
			return;
		}
		
		//update the shader on our damage renderer.
		myDamageRenderer.material.SetFloat("_YOffset", (Time.realtimeSinceStartup * 0.5f) % 1.0f);

		switch (slimeDamageStatus)
		{
			case Map_SlimeDungeon.SlimeStatus.Unslimed:
				myDamageRenderer.material.SetColor("_SlimeColor", Color.clear);
				break;
			case Map_SlimeDungeon.SlimeStatus.Friendly:
                myDamageRenderer.material.SetColor("_SlimeColor", Color.yellow);                
                break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_1:
                myDamageRenderer.material.SetColor("_SlimeColor", Color.magenta);
                break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_2:
				break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_3:
				break;
			case Map_SlimeDungeon.SlimeStatus.Count:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		//which sprite do we use for damage?
		myDamageRenderer.enabled = true;
		float damageRatio = (float) damageTaken / damageMax;
		int idx = (int)(damageSprites.Count * damageRatio);
		idx = Mathf.Min(damageSprites.Count - 1, idx);
		myDamageRenderer.sprite = damageSprites[idx];
		
	}

	/// <summary>
	/// Some slime is trying to convert me, what do I do?
	/// </summary>
	/// <param name="damageType"></param>
	public void TakeDamage(Map_SlimeDungeon.SlimeStatus damageType)
	{
		//if this type matches my current type, reduce my damage if someone else is trying to convert me
		if (damageType == mySlimeStatus)
		{
			if (slimeDamageStatus != damageType)
			{
				damageTaken = Mathf.Max(0, damageTaken - 1);
			}
		}
		//this damage is different from me, so i'm being converted.
		else
		{
			//tick my damage up if i'm neutral, or already taking this type,
			if (slimeDamageStatus == damageType || 
			    slimeDamageStatus == Map_SlimeDungeon.SlimeStatus.Unslimed )
			{
				damageTaken = Mathf.Min(damageMax, damageTaken + 1);
				slimeDamageStatus = damageType;
			}
			else
			{
				//otherwise, tick the damage down
				damageTaken = Mathf.Max(0, damageTaken - 1);
			}
		}

		//clear the damage type from me if the count is 0
		if (damageTaken <= 0)
		{
			slimeDamageStatus = Map_SlimeDungeon.SlimeStatus.Unslimed;
		}

		//but if I've taken too much, I'm converted!
		if (damageTaken >= damageMax)
		{
			//i am converted!
		}
	}

	/// <summary>
	/// Should this tower change to a different owner
	/// </summary>
	/// <param name="convertToThis"></param>
	/// <returns></returns>
	public bool ShouldConvert(ref Map_SlimeDungeon.SlimeStatus convertToThis)
	{
		if (damageTaken >= damageMax)
		{
			convertToThis = slimeDamageStatus;
			return true;
		}

		convertToThis = Map_SlimeDungeon.SlimeStatus.Count;
		return false;
	}

	/// <summary>
	/// Change this tower to a new owner
	/// </summary>
	/// <param name="newSlimeStatus"></param>
	public void DoConversion(Map_SlimeDungeon.SlimeStatus newSlimeStatus)
	{
		damageTaken = 0;
		slimeDamageStatus = Map_SlimeDungeon.SlimeStatus.Unslimed;
		mySlimeStatus = newSlimeStatus;
		SetSpriteByStatus();
	}

	void SetSpriteByStatus()
	{
		var za = GetComponent<Animatable>();
		switch (mySlimeStatus)
		{
			case Map_SlimeDungeon.SlimeStatus.Unslimed:
				za.SetAnim("IdleNeutral");
				break;
			case Map_SlimeDungeon.SlimeStatus.Friendly:
				za.SetAnim("IdlePlayer");
				break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_1:
				za.SetAnim("IdleEnemy");
				break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_2:
				break;
			case Map_SlimeDungeon.SlimeStatus.Enemy_3:
				break;
			case Map_SlimeDungeon.SlimeStatus.Count:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	/// <summary>
	/// Set our slime state, and the next time we tick, do the conversion.
	/// </summary>
	/// <param name="towerSlimeStatus"></param>
	public void SetSlimeStatusForNextUpdate(Map_SlimeDungeon.SlimeStatus towerSlimeStatus)
	{
		mySlimeStatus = towerSlimeStatus;
		bConvertNextUpdate = true;
	}

	/// <summary>
	/// Update this unity object with the destructible, sometimes pooling can break this connection.
	/// </summary>
	/// <param name="tower"></param>
	public void SetTower(Destructible tower)
	{
        if (tower != null)
        {
            towerHasBeenSet = true;
        }
		myTower = tower;
		mySlimeStatus = Map_SlimeDungeon.GetSlimeStatusFromActorData(myTower);
		if (mySlimeStatus != Map_SlimeDungeon.SlimeStatus.Count)
		{
			bConvertNextUpdate = true;
		}
	}

	/// <summary>
	/// Maybe someone can help bring our damage down?
	/// </summary>
	/// <param name="canThisStatusHelp"></param>
	/// <returns></returns>
	public bool DoINeedRepairFrom(Map_SlimeDungeon.SlimeStatus canThisStatusHelp)
	{
		return canThisStatusHelp == mySlimeStatus && damageTaken > 0;
	}
}
