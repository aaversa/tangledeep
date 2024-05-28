using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SpriteEffect : MonoBehaviour {

    public bool useForPoolingOnly;
	public string refName;
    public string onDeathCreateSpriteEffect;
	private Animatable myAnimatable;
    public GameObject spriteParent;
    public GameObject followObject;
    public Actor attachedActor;
    public bool onlyExtDestroy;
    public bool animInitialized = false;
    public Vector2 offset;
    private bool hadFollowObject = false;
    private bool baseVisible;
    private bool curVisible;
    private bool alwaysVisible;
    public AbilityScript parentAbility;
    public float baseRotation;

    public bool randomColor;

    // USed for lasers
    public bool stackToTargetLocation;

    bool hasSpriteRenderer;
    SpriteRenderer mySR;

    // Use this for initialization
    void Start ()
    {
		myAnimatable = gameObject.GetComponent<Animatable>();
        mySR = GetComponent<SpriteRenderer>();
		Initialize();        
	}


    

	void Initialize()
    {
		if (myAnimatable != null)
        {
            myAnimatable.SetAnim("Default");
        }
        if (mySR == null) mySR = GetComponent<SpriteRenderer>();
        hasSpriteRenderer = mySR != null;
        baseVisible = true;
        curVisible = true;
    }

	void OnEnable ()
    {
		Initialize();
	}

    public void SetAlwaysVisible(bool state)
    {
        alwaysVisible = state;
    }

    public void SetBaseVisible(bool state)
    {
        baseVisible = state;
        if (!state)
        {
            SetCurVisible(false);
        }
        else
        {
            SetCurVisible(true);
        }
    }    

    public bool GetCurVisible()
    {
        if (!baseVisible)
        {
            return false;
        }
        if (alwaysVisible)
        {
            return true;
        }
        return curVisible;
    }

    public void SetCurVisible(bool state)
    {
        // TODO: Make child object
        if (!baseVisible)
        {
            state = false;
        }
        curVisible = state;
        if (!GetCurVisible())
        {
            if (hasSpriteRenderer) GetComponent<SpriteRenderer>().enabled = false;
        }
        else
        {
            if (hasSpriteRenderer)GetComponent<SpriteRenderer>().enabled = true;
        }
        if (GetComponent<Animatable>() != null)
        {
            GetComponent<Animatable>().ResetAnim(); // Could this cause problems?
        }
    }

	public void SetFollowActor(Actor act) {
		attachedActor = act;
	}

    public void SetFollowObject(GameObject go, Directions newDir)
    {
        hadFollowObject = true;
        followObject = go;

        transform.position = go.transform.position;
        transform.SetParent(go.transform);
        transform.localPosition = Vector3.zero; // new to prevent stuff from spawning way off of follow actor/obj
        
        if (gameObject == null)
        {
            return;
        }
        GameMasterScript.AlignGameObjectToObject(gameObject, go, newDir, offset.x, offset.y);
    }

    void ReturnToStack()
    {
    	GameMasterScript.ReturnToStack(gameObject, refName);
    }

	// Update is called once per frame
	void Update () {

        if (useForPoolingOnly) return;

        if (!animInitialized)
        {
            return;
        }
        
        if (hadFollowObject && followObject == null) // Our parent died for some reason, so return to stack.
        {
			if (attachedActor != null)
            {
                if (attachedActor.overlays != null) // Why would overlays be null for an attached actor?
                {
                    attachedActor.RemoveOverlay(gameObject);
                }                	
            }
            ReturnToStack();          
            return;
        }
        if (followObject != null)
        {
            // TODO: Make child object
            // Update based on parent position.

            if (GetCurVisible() && followObject.GetComponent<SpriteRenderer>().enabled)
            {
                GetComponent<SpriteRenderer>().enabled = true;
            }
            else
            {
                GetComponent<SpriteRenderer>().enabled = false;
            }


        }

        if (myAnimatable != null && !onlyExtDestroy)
        {
			if (myAnimatable.AnimComplete())
            {
               CleanUpAndReturnToStack();
			}
		}
	}

    public void CleanUpAndReturnToStack()
    {
        bool returnedToStack = false;
        if (spriteParent != null) // We're a sub-effect of an effect system. Like one sword slash in a multi-slash FX
        {
            ReturnToStack(); // Return ourselves to stack
            gameObject.transform.SetParent(null);
            
            // But also return our parent to stack.
            GameMasterScript.ReturnToStack(spriteParent, spriteParent.name.Replace("(Clone)", String.Empty));
            returnedToStack = true;
        }
        if (attachedActor != null)
        {
            attachedActor.RemoveOverlay(gameObject);
        }
        if (!returnedToStack)
        {
            ReturnToStack();
        }
    }

	void OnDestroy () {
        //TriggerOnDeathEffect();
    }

    void OnDisable()
    {
        // #todo - Probably don't do this on scene change
        TriggerOnDeathEffect();
    }

    void TriggerOnDeathEffect()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;
        if (GameMasterScript.levelChangeInProgress) return;
        if (!string.IsNullOrEmpty(onDeathCreateSpriteEffect))
        {
            CombatManagerScript.GenerateSpecificEffectAnimation(transform.position, onDeathCreateSpriteEffect, null, true);
        }
    }
}

