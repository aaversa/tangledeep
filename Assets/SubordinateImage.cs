using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Child fades will follow their parent, when instructed.
public class SubordinateImage : MonoBehaviour {

    public Image parentImage;    
    public Image myImage;


    bool enableState = false;

    // Update is called once per frame
    void Update ()
    {
        //if (!doUpdate) return;
        if (!GameMasterScript.gameLoadSequenceCompleted || UIManagerScript.prettyLoadingArtComponent.IsActive())
        {
            return;
        }        
        else
        {
            DisableImage();            
        }
        if (!parentImage.gameObject.activeSelf)
        {
            DisableImage();
        }
        else
        {
            if (parentImage.enabled && !enableState)
            {
                enableState = true;
                myImage.enabled = true;
            }
            else if (!parentImage.enabled && enableState)
            {
                DisableImage();
            }
            
            myImage.color = parentImage.color;
            
        }
    }

    void DisableImage()
    {
        if (enableState)
        {
            myImage.enabled = false;
            enableState = false;
        }
    }

    void Start ()
    {
        myImage.color = new Color(myImage.color.r, myImage.color.g, myImage.color.b, 0f);
    }

    public void UpdateFromParent()
    {
        if (!GameMasterScript.gameLoadSequenceCompleted || UIManagerScript.prettyLoadingArtComponent.IsActive())
        {
            return;
        }        
        else
        {
            if (enableState)
            {
                enableState = false;
                myImage.enabled = false;
            }
        }

        if (!parentImage.gameObject.activeSelf)
        {
            if (enableState)
            {
                enableState = false;
                myImage.enabled = false;
            }
        }
        else
        {
            myImage.color = parentImage.color;

            if (enableState != parentImage.enabled)
            {
                myImage.enabled = parentImage.enabled;
                enableState = myImage.enabled;
            }

        }
    }
    
}
