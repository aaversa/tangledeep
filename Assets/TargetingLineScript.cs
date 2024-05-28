using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Texture2D = UnityEngine.Texture2D;

[System.Serializable]
public class TargetingLineScript : MonoBehaviour {

    LineRenderer myRenderer;

    public Vector3 startPoint;
    public Vector3 endPoint;

    public int numSamples;

    private Material myTargetingMaterial;

    public static Material targetingMaterial;
    bool initialized;

    Vector3 lastKnownParentPosition;
    Vector3 lastKnownTargetPosition;
    const int UPDATE_FRAME_DELAY = 5;
    int framesToUpdate = 5;
    bool preparingToRedrawLine;
    Transform targetTransform;

    bool parentTransformConnected;
    Transform parentTransform;

    int attemptsToConnectParentTransform;

    const int MAX_ATTEMPTS_TO_CONNECT_PARENT = 30;

    private float fXArrowOffset;

    //Used to let the arrow fade in and out if we need to based
    //on targeting
    private Color currentMaterialColor;

    /// <summary>
    /// Set explicitly if we want an arrow that does not need to have a parent anchor.
    /// </summary>
    private bool doesNotRequireParent;
    
	// Use this for initialization
	void Start () {
        if (initialized) return;
        Initialize();
	}
	
    void Initialize()
    {
        myRenderer = GetComponent<LineRenderer>();
        myRenderer.sortingOrder = 99;
        myRenderer.sortingLayerName = "Foreground";

        parentTransform = transform.parent;
        if (parentTransform != null)
        {
            lastKnownParentPosition = transform.parent.position;
            parentTransformConnected = true;
        }

        myTargetingMaterial = Instantiate(targetingMaterial);
        myRenderer.material = myTargetingMaterial;

        SetColor(Color.red);
        
        initialized = true;

    }

    public static void LoadAllMaterials()
    {
        targetingMaterial = Resources.Load<Material>("LineDrawMaterial");
        targetingMaterial.mainTexture = Resources.Load<Texture2D>("SpriteEffects/Spritesheets/Targeting Arrow 1");

        /* var assets = TDAssetBundleLoader.GetBundleIfExists("targetingline").LoadAllAssets();
        Texture2D loadTex = null;

        foreach (var thing in assets)
        {
            //We'll make copies of this material with every instance.
            var mat = thing as Material;
            if (mat != null)
            {
                targetingMaterial = mat;
                continue;
            }

            //And this is the texture we'd like to use.
            var tex = thing as Texture2D;
            if (tex != null)
            {
                loadTex = tex;
            }
        }

        targetingMaterial.mainTexture = loadTex; */
    }

    /// <summary>
    /// Point the arrow somewhere else manually. Won't work if the arrow is attached to a targetTransform
    /// </summary>
    /// <param name="end"></param>
    public void UpdateEndPoint(Vector2 end)
    {
        SetNewStartAndEndPoints(startPoint, end, targetTransform);
    }

    /// <summary>
    /// Change the start position of the arrow. Only works on arrows that aren't attached to a gameobject
    /// </summary>
    /// <param name="start"></param>
    public void UpdateStartPoint(Vector2 start)
    {
        if (!doesNotRequireParent)
        {
            return;
        }
        
        SetNewStartAndEndPoints(start, endPoint, targetTransform);
    }

    /// <summary>
    /// Change both positions of the arrow. Only works on arrows that aren't attached to a gameobject
    /// </summary>
    /// <param name="start"></param>
    public void UpdateStartAndEndPoints(Vector2 start, Vector2 end)
    {
        if (!doesNotRequireParent)
        {
#if UNITY_EDITOR
            Debug.LogError("Can't manually change the origin of a targeting line that is attached to an object.");
#endif
            return;
        }
        
        SetNewStartAndEndPoints(start, end, targetTransform);
    }
    
    private void SetNewStartAndEndPoints(Vector2 start, Vector2 end, Transform targ)
    {
        if (!initialized)
        {
            Initialize();
        }

        startPoint = start;
        endPoint = end;
        Vector3[] positions = new Vector3[numSamples];

        if (numSamples == 2)
        {
            positions[0] = start;
            positions[1] = end;
        }
        else 
        {
            for (int i = 0; i < numSamples; i++)
            {
                float interval = (float)i / (numSamples - 1);
                Vector3 point = Vector3.Slerp(startPoint, endPoint, interval) + (Vector3.up * Mathf.Sin(interval * Mathf.PI));
                positions[i] = point;
            }
        }

        myRenderer.positionCount = numSamples;
        myRenderer.SetPositions(positions);

        if (targ != null)
        {
            targetTransform = targ;
            lastKnownTargetPosition = targetTransform.position;
        }
    }

    /// <summary>
    /// Adjusts the material color. Note that the texture is Red by default, so that'll mess with this setting.
    /// </summary>
    /// <param name="newColor"></param>
    public void SetColor(Color newColor)
    {
        if (newColor != currentMaterialColor)
        {
            currentMaterialColor = newColor;
            myTargetingMaterial.SetColor("_Color", currentMaterialColor);
        }
    }

    /// <summary>
    /// Disable until called back into action later.
    /// </summary>
    public void Hide()
    {        
        SetColor(Color.clear);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sleep now <3 
    /// </summary>
    public void ReturnToStack()
    {
        enabled = false;
        transform.SetParent(null);
        parentTransform = null;
        GameMasterScript.ReturnToStack(gameObject, gameObject.name.Replace("(Clone)", String.Empty));
    }

	// Update is called once per frame
	void Update ()
    {
        if (!initialized) return;

        //Try to connect to a parent until we surrender. Either way, don't update this frame.
        if (!doesNotRequireParent && !parentTransformConnected)
        {
            parentTransform = transform.parent;
            if (parentTransform == null)
            {
                attemptsToConnectParentTransform++;
                if (attemptsToConnectParentTransform >= MAX_ATTEMPTS_TO_CONNECT_PARENT)
                {
                    Debug.Log(gameObject.name + " has no parent, destroying.");
                    ReturnToStack();
                }
                return;
            }

            parentTransformConnected = true;
        }

        //cycle the arrow position once a second
        fXArrowOffset += Time.deltaTime;
        fXArrowOffset %= 1.0f;

        //but quantize the offset at 12.5% increments to get that not-as-smooth-as-32bit feel 
        float fTest = fXArrowOffset * 8.0f;
        fTest = Mathf.Floor(fTest);
        fTest /= 8.0f;

        //change the offset
        myTargetingMaterial.SetTextureOffset("_MainTex", new Vector2(1.0f - fTest, 0) );
        
        // Check for reasons we might need to go die.
        if (!doesNotRequireParent && 
            (parentTransform == null || 
            targetTransform != null && !targetTransform.gameObject.activeSelf))
        {
            ReturnToStack();
            return;
        }
        
        //If we do not have a target transform, don't freak out, the arrow is being calculated somewhere else.
        //probably.
        if (targetTransform == null)
        {
            return;
        }

        // If parent or target object has moved, redraw / recalculate. But don't do it too often, since it's CPU intensive.
        if (!preparingToRedrawLine && (parentTransform.position != lastKnownParentPosition || targetTransform.position != lastKnownTargetPosition))
        {
            preparingToRedrawLine = true;
            framesToUpdate = UPDATE_FRAME_DELAY;
        }
        else if (preparingToRedrawLine && transform.parent != null )
        {
            framesToUpdate--;
            if (framesToUpdate <= 0)
            {
                preparingToRedrawLine = false;
                lastKnownParentPosition = transform.parent.position;
                lastKnownTargetPosition = targetTransform.position;
                SetNewStartAndEndPoints(lastKnownParentPosition, targetTransform.position, targetTransform);
            }
        }

    }

    public static TargetingLineScript CreateTargetingLine(GameObject start, GameObject finish, int numSamples = -1)
    {
        if (start == null || finish == null) return null;
        GameObject targetingLine = GameMasterScript.TDInstantiate("TargetingLine");
        targetingLine.transform.SetParent(start.transform);
        var tscript = targetingLine.GetComponent<TargetingLineScript>();
        tscript.enabled = true;
        tscript.doesNotRequireParent = false;

        if (numSamples != -1)
        {
            tscript.numSamples = numSamples;
        }
        tscript.SetNewStartAndEndPoints(start.transform.position, finish.transform.position, finish.transform);
        return tscript;
    }

    /// <summary>
    /// Create a targeting line that does not rely on a gameobject for a final destination.
    /// </summary>
    /// <param name="origin">The anchor for the opening of this object</param>
    /// <param name="destination">A location to point at</param>
    /// <param name="numSamples">Defaults to 2, which will make a straight line. Add more numbers to create
    /// a pleasant curve from start to end</param>
    /// <returns></returns>
    public static TargetingLineScript CreateTargetingLine(GameObject origin, Vector2 destination, int numSamples = 2)
    {
        if (origin == null) return null;
        GameObject targetingLine = GameMasterScript.TDInstantiate("TargetingLine");
        targetingLine.transform.SetParent(origin.transform);
        var tscript = targetingLine.GetComponent<TargetingLineScript>();
        tscript.enabled = true;
        tscript.doesNotRequireParent = false;
        tscript.numSamples = numSamples;
        
        //we don't need a transform here
        tscript.SetNewStartAndEndPoints(origin.transform.position, destination, null);
        Debug.Log("New targeting arrow made!");
        return tscript;
    }

    /// <summary>
    /// Create a targeting line that does not use any gameobject for origin or destination. The position must be
    /// updated manually. 
    /// </summary>
    /// <param name="origin">A location to point from</param>
    /// <param name="destination">A location to point at</param>
    /// <param name="numSamples">Defaults to 2, which will make a straight line. Add more numbers to create
    /// a pleasant curve from start to end</param>
    /// <returns></returns>
    public static TargetingLineScript CreateTargetingLine(Vector2 origin, Vector2 destination, int numSamples = 2)
    {
        GameObject targetingLine = GameMasterScript.TDInstantiate("TargetingLine");
        var tscript = targetingLine.GetComponent<TargetingLineScript>();
        tscript.enabled = true;
        tscript.doesNotRequireParent = true;
        tscript.numSamples = numSamples;
        
        tscript.SetNewStartAndEndPoints(origin, destination, null);
        return tscript;
    }

    public static IEnumerator DelayMoveAfterTargeting(float f)
    {
        yield return new WaitForSeconds(f);
    }

    
}