using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CursorBounce : MonoBehaviour {

    public bool blink;
    public bool bounce;
    public float blinkCycle;
    public float bounceCycle;

    Vector3 target = new Vector2(0, 0);
    Vector3 initPosition = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private float timeCounter;
    private bool imageOn = true;
    Vector3 smoothDamper = Vector3.zero;

    //When set to an object, we record this position
    //if that object moves, we move to the new position and store this again
    private Vector3 vWatchPosition;
    private GameObject objWatch;

    private Animatable myAnimatable;
    private bool bInShiftMode;

    private Directions dirCursorFacing;

    public void Start()
    {
        myAnimatable = gameObject.GetComponent<Animatable>();
        if (myAnimatable == null)
        {
            return;
        }
        myAnimatable.SetAnim("Default");
    }

    public void ResetBounce(Vector3 init, GameObject watchMe, bool faceLeft = false)
    {
        velocity = Vector3.zero;
        transform.position = init;
        initPosition = init;
        smoothDamper = init;

        if (watchMe != null)
        {
            objWatch = watchMe;
            vWatchPosition = objWatch.transform.position;
        }

        SetFacing(faceLeft ? Directions.WEST : Directions.EAST);
    }

    public void SetFacing(Directions dirNewFacing)
    {
        Animatable a = gameObject.GetComponent<Animatable>();
        dirCursorFacing = dirNewFacing;
        switch (dirCursorFacing)
        {
            case Directions.WEST: //old FaceLeft
                a.ToggleIgnoreScale(true);
                a.transform.localScale = new Vector3(-1, 1, 1);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case Directions.NORTH: //point up
                a.ToggleIgnoreScale(true);
                a.transform.localScale = new Vector3(1, 1, 1);
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
            default:    //normal facing
                a.ToggleIgnoreScale(false);
                a.transform.localScale = new Vector3(1, 1, 1);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
        }
    }

    void RoundPosition(Vector3 pos)
    {
        Vector3 newPos = Vector3.zero;
        newPos.y = smoothDamper.y;
        newPos.z = smoothDamper.z;
        newPos.x = Mathf.Floor(smoothDamper.x);
        //transform.position = newPos;
        Debug.Log(newPos);
    }

	void Update ()
    {
        Image imgCursor = gameObject.GetComponent<Image>();

        if (UIManagerScript.bForceHideCursor)
        {
            imgCursor.enabled = false;
        }

        if (imgCursor.enabled && blink)
        {
            timeCounter += Time.deltaTime;
            if (timeCounter > blinkCycle)
            {
                if (imageOn)
                {
                    imgCursor.color = Color.clear;
                }
                else
                {
                    imgCursor.color = Color.white;
                }
                imageOn = !imageOn;
                timeCounter = 0.0f;
            }
            
        }

        //stick with our object if it moves
        if (objWatch != null)
        {
            if (objWatch.transform.position != vWatchPosition)
            {
                //follow up
                Vector3 vDelta = objWatch.transform.position - vWatchPosition;
                ResetBounce(transform.position + vDelta, objWatch);
            }
        }

        if (GameMasterScript.actualGameStarted && gameObject == UIManagerScript.singletonUIMS.uiDialogMenuCursor)
        {
            // This isn't necessary except on skill sheet.
            if (UIManagerScript.GetUITabSelected() == UITabs.SKILLS)
            {
                bool bShiftaliftin = GameMasterScript.gmsSingleton.player.GetButton("Diagonal Move Only");
                if (bInShiftMode != bShiftaliftin)
                {
                    bInShiftMode = bShiftaliftin;
                    StartCoroutine(FlipOnShift(bInShiftMode ? "Shift" : "Default",
                        0.1f,
                        bInShiftMode ? "UITick" : "UITock"));
                }
            }
        }
    }

    IEnumerator FlipOnShift(string strAnimToPlay, float fFlipTime, string strCue)
    {
        UIManagerScript.PlayCursorSound(strCue);
        float fTime = 0f;
        while (fTime < fFlipTime)
        {
            Vector3 vFlipRot = new Vector3(Mathf.Lerp(0,360, fTime/fFlipTime),0,0);
            transform.localRotation = Quaternion.Euler(vFlipRot);
            fTime += Time.deltaTime;
            yield return null;
        }
        transform.localRotation = Quaternion.Euler(0,0,0);
        myAnimatable.SetAnim(strAnimToPlay);
    }
}
