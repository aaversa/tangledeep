using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollbarResizerComponent : MonoBehaviour {

	// Use this for initialization
	void Start () {
        GetComponent<Scrollbar>().size = 0;
	}

}
