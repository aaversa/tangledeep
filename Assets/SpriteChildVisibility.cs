using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteChildVisibility : MonoBehaviour {

    SpriteRenderer mySR;
    bool mySRFound;
    SpriteRenderer myParentSR;
    bool parentSRFound;

	// Update is called once per frame
	void Update ()
    {
        if (!mySRFound)
        {
            mySR = GetComponent<SpriteRenderer>();
            if (mySR != null)
            {
                mySRFound = true;
            }
        }
        if (!mySRFound) return;
        if (transform.parent == null) return;
        if (!parentSRFound)
        {
            myParentSR = transform.parent.GetComponent<SpriteRenderer>();
            if (myParentSR != null)
            {
                parentSRFound = true;
            }
        }
        if (!parentSRFound)
        {
            return;
        }

        // Just have our renderer track our parent's renderer. Simple!
        mySR.enabled = myParentSR.enabled;

	}
}
