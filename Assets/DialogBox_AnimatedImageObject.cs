using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component designed to be on a unique GameObject that will display an image
/// on top of a dialog box, effectively being inside the dialog box.
/// </summary>
/// Notes: I feel a little guilty about this one, as it is implementing a second
/// form of animated image. That said, it needs to be lighter weight than Animatable,
/// which has a number of extra pieces bound to gameplay that I'd like to avoid.
public class DialogBox_AnimatedImageObject : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The single animation that this object will play. This animation will always loop.")]
    private List<DialogBox_AnimatedImageObject_Frame> listAnimationFrames;

    [SerializeField]
    [Tooltip("The UI image object that will display this animation.")]
    private Image myImage;

    [SerializeField]
    [Tooltip("The UI image object that will display a crossfade if we want that to happen.")]
    private Image myCrossfadeImage;

    /// <summary>
    /// True if we have no crossfade image and thus will never crossfade.
    /// </summary>
    private bool bNeverCrossfade;

    /// <summary>
    ///True if some error happened and we never want to update.
    /// </summary>
    private bool bDisabledForever;

    /// <summary>
    /// The frame in the list we are drawing right now.
    /// </summary>
    private int idxCurrentFrame;

    /// <summary>
    /// The amount of time left to draw the current frame.
    /// </summary>
    private float fTimeRemaining;

    /// <summary>
    /// If non-zero, we start a frame crossfade after this threshold.
    /// </summary>
    private float fCrossfadeAfter;

    /// <summary>
    /// Set to true when we begin crossfading in a frame, set to false when new frames start
    /// </summary>
    private bool bCrossfadingActive;

    void Start ()
	{
	    if (myImage == null)
	    {
            if (Debug.isDebugBuild) Debug.Log("No image attached to DialogBox_AnimatedImageObject '" + gameObject.name + "'");
	        bDisabledForever = true;
	        return;
	    }

        //not a failure condition
	    if (myCrossfadeImage == null)
	    {
	        bNeverCrossfade = true;
	    }

        //begin with the first frame active.
        SetFrame(idxCurrentFrame);

    }

    void Update ()
	{
	    if (bDisabledForever) return;

        //tick down the timer
	    fTimeRemaining -= Time.deltaTime;

        //if we are ready for the next frame, do so.
	    if (fTimeRemaining < 0f)
	    {
	        AdvanceOneFrame();
	    }

        //check out the crossfade if we need to.
	    if (!bNeverCrossfade && fTimeRemaining < fCrossfadeAfter)
	    {
	        UpdateCrossfadeImage();
	    }
	}

    /// <summary>
    /// Ensures the correct image is in the child image, and changes the alpha.
    /// </summary>
    private void UpdateCrossfadeImage()
    {
        //if we haven't started yet, do so now.
        if (!bCrossfadingActive)
        {
            var idxCrossFrame = idxCurrentFrame + 1;
            if (idxCrossFrame >= listAnimationFrames.Count)
            {
                idxCrossFrame = 0;
            }

            var crossFrame = listAnimationFrames[idxCrossFrame];
            myCrossfadeImage.sprite = crossFrame.sprite;

            //start faded to 0.
            myCrossfadeImage.color = new Color(1,1,1,0f);

            bCrossfadingActive = true;
        }

        //how far along are we? This value starts at 1.0f and goes down.
        float fRatio = fTimeRemaining / fCrossfadeAfter;

        //lerpaderp
        myImage.color  = Color.Lerp(Color.white, new Color(1,1,1,0), 1.0f - fRatio);
        myCrossfadeImage.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), fRatio);

    }

    /// <summary>
    /// Move to the next frame, clearing the crossfade if necessary.
    /// </summary>
    private void AdvanceOneFrame()
    {
        idxCurrentFrame++;
        if (idxCurrentFrame >= listAnimationFrames.Count)
        {
            idxCurrentFrame = 0;
        }

        //set up this new image.
        SetFrame(idxCurrentFrame);

        //crossfading is not active now, but will be when the set time is reached.
        bCrossfadingActive = false;
    }

    /// <summary>
    /// Places this frame in the active image and starts the timer.
    /// </summary>
    /// <param name="idx"></param>
    void SetFrame(int idx)
    {
        var frame = listAnimationFrames[idx];
        fTimeRemaining += frame.fLifeTime;
        fCrossfadeAfter = frame.fCrossfadeIntoNextFrameTime;
        myImage.sprite = frame.sprite;

        //clear the crossfade image if we have one.
        if (!bNeverCrossfade)
        {
            myCrossfadeImage.color = new Color(1, 1, 1, 0);
            myImage.color = new Color(1,1,1,1);
        }
    }
}

[System.Serializable]
public struct DialogBox_AnimatedImageObject_Frame
{
    [Tooltip("The sprite we would like to display.")]
    public Sprite sprite;

    [Tooltip("The amount of time this frame is visible.")]
    public float fLifeTime;

    [Tooltip("If non zero, this frame will fade into the next when there is this much time left on the frame timer.")]
    public float fCrossfadeIntoNextFrameTime;
}