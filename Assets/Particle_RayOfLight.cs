using System.Collections;
using System.Collections.Generic;
using Rewired.UI.ControlMapper;
using UnityEngine;

public class Particle_RayOfLight : MonoBehaviour
{

    public Vector2 fixedRotationRange;

    [Tooltip("Don't use negative numbers, if you want to rotate in both directions, check the bool below")]
    public Vector2 MinMaxRotationSpeed;
    public bool RandomRotationDirection;
    public bool StartWithRandomRotation;
    private float fRotationSpeed;
    public Vector2 MinMaxScale = Vector2.one;
    private float fScaleMultiplier;
    public float  BeamLengthInUnits;
    public Color   BeamColor;
    public SpriteRenderer BeamRenderer;

    private SpriteRenderer myParentRenderer;

    public bool rotateWithinFixedRange;

    [Tooltip("0 means live forever!")]
    public float MaxLifeTime;
    private float fLifetime;

    bool rotationDirection = true;

    void OnEnable()
    {
        //decide on a rotation speed;
        fRotationSpeed = Random.Range(MinMaxRotationSpeed.x, MinMaxRotationSpeed.y);

        //coin toss to go counter clockwise if we so desire
        if (RandomRotationDirection && Random.value < 0.5f)
        {
            fRotationSpeed *= -1.0f;
        }

        //decide on a scale adjustment
        fScaleMultiplier = Random.Range(MinMaxScale.x, MinMaxScale.y);

        //The beam should be offset vertically by half of its length, modified by scale
        Vector3 vBeamOffsetFromCenter = new Vector3(0,0,0);
        float fVerticalOffset = BeamLengthInUnits * 0.5f;
        fVerticalOffset *= fScaleMultiplier;
        vBeamOffsetFromCenter.y = fVerticalOffset;

        BeamRenderer.transform.localPosition = vBeamOffsetFromCenter;
        BeamRenderer.transform.localScale = new Vector3(1, fScaleMultiplier, 1);

        //Face some direction to start
        float fRotationDegrees = 0f;
        if (StartWithRandomRotation)
        {
            fRotationDegrees = Random.Range(0, 360f);
        }
        else if (rotateWithinFixedRange)
        {
            fRotationDegrees = fixedRotationRange.x;
        }

        //rotate ourselves, not the child, because the child is offset
        transform.rotation = Quaternion.Euler(0, 0, fRotationDegrees);

        //pretty colors
        BeamRenderer.color = BeamColor;

        //if we have a draw parent, stay at that parent's sort order
        if (transform.parent != null)
        {
            myParentRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }

    }

	// Update is called once per frame
	void Update ()
    {
        //Tick down to eternal oblivion if necessary
        if (MaxLifeTime > 0f)
        {
            fLifetime += Time.deltaTime;
            if (fLifetime >= MaxLifeTime)
            {
                Destroy(gameObject);
            }
        }

        //rotate as requested
        Vector3 vRotation = transform.rotation.eulerAngles;

        if (rotateWithinFixedRange)
        {
            if (rotationDirection)
            {
                vRotation.z += fRotationSpeed * Time.deltaTime;
                if (vRotation.z >= fixedRotationRange.y)
                {
                    vRotation.z = fixedRotationRange.y - 0.001f;
                    rotationDirection = false;
                }
            }
            else
            {
                vRotation.z -= fRotationSpeed * Time.deltaTime;
                if (vRotation.z <= fixedRotationRange.x)
                {
                    vRotation.z = fixedRotationRange.x + 0.001f;
                    rotationDirection = true;
                }
            }

        }
        else
        {
            vRotation.z += fRotationSpeed * Time.deltaTime;
        }
        
        transform.rotation = Quaternion.Euler(vRotation);        

        //stay at the correct z-draw in the world
        if (myParentRenderer != null)
        {
            BeamRenderer.sortingOrder = myParentRenderer.sortingOrder;
        }

    }
}
