using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TextHelper : MonoBehaviour {

    public bool isDirty;
    public TextMeshProUGUI myTMPro;

	// Update is called once per frame
	void Update () {
		if (isDirty)
        {
            transform.position = transform.position;
            myTMPro.SetText(myTMPro.text);
            isDirty = false;
        }
	}

    public void SetDirty()
    {
        isDirty = true;
    }
}
