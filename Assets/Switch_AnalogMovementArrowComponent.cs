using System.Collections;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

public class Switch_AnalogMovementArrowComponent : MonoBehaviour
{
    [Tooltip("The last arrow is in captivity. The galaxy is at peace.")]
    [SerializeField]
    private SpriteRenderer arrow;

    [Tooltip("Another arrow may terrorize the galaxy. Pray for a true peace in space.")]
    [SerializeField]
    private SpriteRenderer otherA;
    
    [Tooltip("Mother Arrow was not stopped, and had 8 children.")]
    [SerializeField]
    private SpriteRenderer diagonalMovementArrows;
    

    private Rewired.Player rewiredPlayer;

    private bool bComponentsInitialized;

    private bool bArrowsHaveBeenHidden;

    private bool bDiagonalArrowActive;


    private static bool requestMadeToNotDrawArrowsThisFrame;
    
    /// <summary>
    /// Call this if you don't want to see the arrows this frame but also don't want to turn them off.
    /// </summary>
    public static void RequestDoNotDrawArrowsThisFrame()
    {
        requestMadeToNotDrawArrowsThisFrame = true;
    }

	// Use this for initialization
	void Start ()
	{
        TryInitializeComponents();
	}

    void TryInitializeComponents()
    {
        rewiredPlayer = ReInput.players.GetPlayer(0);

        bComponentsInitialized =
            arrow != null &&
            otherA != null &&
            rewiredPlayer != null;
    }

    /// <summary>
    /// Make sure the arrows are not visible in case the option is toggled off or other reasons.
    /// </summary>
    void EnsureArrowsAreHidden()
    {
        if (bArrowsHaveBeenHidden) return;

        bArrowsHaveBeenHidden = true;
        arrow.enabled = false;
        otherA.enabled = false;
    }

    // Update is called once per frame
    void Update ()
    {
      

        if (!bComponentsInitialized)
        {
            TryInitializeComponents();
            return;
        }

        if (!GameMasterScript.gameLoadSequenceCompleted)
        {
            return;
        }

        //Just keep them hidden if we're moving the cursor through the hotbar.
        if (UIManagerScript.singletonUIMS.uiHotbarNavigating)
        {
            RequestDoNotDrawArrowsThisFrame();
            return;
        }
        
        if (UIManagerScript.AnyInteractableWindowOpen())
        {
            return;
        }


        if (!PlatformVariables.GAMEPAD_ONLY && ReInput.controllers.GetLastActiveControllerType() != ControllerType.Joystick)
        {
            EnsureArrowsAreHidden();
            return;
        }


            //Look for other catches we might need to prevent us from crashalashin'
            if (!GameMasterScript.actualGameStarted || 
            Switch_RadialMenu.IsActive() ||
            UIManagerScript.singletonUIMS.GetExamineMode())
        {
            EnsureArrowsAreHidden();
            return;
        }
        
        //if holding diagonal-only move, draw that
        if (rewiredPlayer.GetButton("Diagonal Move Only"))
        {
            if (!bDiagonalArrowActive)
            {
                diagonalMovementArrows.transform.SetParent(GameMasterScript.gmsSingleton.GetHeroPC().transform);
                diagonalMovementArrows.transform.localPosition = Vector3.zero;
                diagonalMovementArrows.transform.localScale = new Vector3(0.8f,0.8f,0.8f);
                diagonalMovementArrows.transform.localRotation = Quaternion.identity;
                diagonalMovementArrows.enabled = true;
                bDiagonalArrowActive = true;

                //fancy pretty
                diagonalMovementArrows.color = Color.clear;
                LeanTween.color(diagonalMovementArrows.gameObject, Color.white, 0.2f);
                LeanTween.scale(diagonalMovementArrows.gameObject, Vector3.one, 0.5f).setEaseOutElastic();
            }
        }        
        else if( bDiagonalArrowActive )
        {
            LeanTween.cancel(diagonalMovementArrows.gameObject);
            diagonalMovementArrows.enabled = false;
            bDiagonalArrowActive = false;
        }
        
        //now leave if we aren't in step-move
        if (PlayerOptions.joystickControlStyle != JoystickControlStyles.STEP_MOVE)
        {
            EnsureArrowsAreHidden();
            return; 
        }
        
        //we know we're in step move, so draw the arrow
        DrawAnalogMovementArrow();

        //and hide the white square targeting dealie if the game is held up for whatever reason
        if (GameMasterScript.IsNextTurnPausedByAnimations() && GameMasterScript.heroPCActor.diagonalOverlay.activeSelf)
        {
            GameMasterScript.heroPCActor.diagonalOverlay.SetActive(false);
        }
	}

    /// <summary>
    /// Check to see if we need to hide the arrows for this frame.
    /// </summary>
    void LateUpdate()
    {
        if (requestMadeToNotDrawArrowsThisFrame && bComponentsInitialized)
        {
            otherA.color = Color.clear;
            arrow.color = Color.clear;
        }

        requestMadeToNotDrawArrowsThisFrame = false;
    }

    //draw a non-quantized targeting icon at the stick's location
    void DrawAnalogMovementArrow()
    {
        //If we are using the D-Pad to move, Move Horizontal and Move Vertical will still change. However,
        //on the Switch at least, we have Radial +/- commands that are bound to the dpad! So we can check these
        //and if they are active, the arrow must not be.
        if ( GameMasterScript.IsDPadPressed())
        {
            arrow.enabled = false;
            otherA.enabled = false;
            return;
        }

        //If we aren't moving the stick, just relax and hide the arrow.
        Vector2 vStickNative = new Vector2(rewiredPlayer.GetAxis("Move Horizontal"), rewiredPlayer.GetAxis("Move Vertical"));
        if (vStickNative.sqrMagnitude <= 0.04f)
        {
            arrow.enabled = false;
            otherA.enabled = false;
            return;
        }

        //at least one arrow is showing now
        bArrowsHaveBeenHidden = false;

        //use the amount of push to determine size and transparency
        float fRatio = vStickNative.magnitude / 1.4141f; // yeah sure

        //alpha based on push
        Color drawColor = Color.Lerp(Color.yellow, Color.white, Mathf.PingPong(Time.realtimeSinceStartup, 0.25f));
        drawColor.a = Mathf.Lerp(0.2f, 0.7f, fRatio);
        arrow.color = drawColor;

        //size based on push
        float fScaleValue = 0.5f + (0.3f * fRatio);
        arrow.transform.localScale = new Vector3(fScaleValue, fScaleValue, fScaleValue);

        //face the push direction
        Vector3 rotation = arrow.transform.rotation.eulerAngles;
        rotation.z = Vector2.Angle(Vector2.up, vStickNative);
        if (vStickNative.x > 0)
        {
            rotation.z *= -1.0f;
        }
        arrow.transform.rotation = Quaternion.Euler(rotation);

        //look how pretty.
        arrow.enabled = true;

        //Push the arrow out based on the stick input, but ensure it is always at least some degree out from center.
        //or else it looks like a pee pee. (ADS enabled)
        float fADSProtection = Mathf.Max(0.5f, Mathf.Min(0.8f,vStickNative.magnitude));

        //if we are currently running, make the arrow pulse a little.
        if (rewiredPlayer.GetButtonShortPress("Confirm"))
        {
            otherA.enabled = true;
            float fRunEffectDelta = Time.realtimeSinceStartup % 0.4f / 0.4f;
            fADSProtection *= Mathf.Lerp(0f, 0.35f, fRunEffectDelta );
            drawColor.a = Mathf.Lerp(0.7f, 0.0f, fRunEffectDelta * fRunEffectDelta * fRunEffectDelta);
            otherA.color = drawColor;
            otherA.transform.localPosition = new Vector3(0, fADSProtection, 0);

            //adjust the base arrow to move back a touch now -- halfway between out and in,
            //so that the glowy arrow doesn't run all over the grid
            fADSProtection = 0.8f;
        }
        else
        {
            otherA.enabled = false;
        }

        Vector2 vADSProtection = fADSProtection * vStickNative.normalized;
        arrow.transform.localPosition = vADSProtection;

    }


    private void OnDestroy()
    {
        bComponentsInitialized = false;
    }
}
