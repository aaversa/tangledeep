using UnityEngine;
using System.Collections;

[System.Serializable]
public class GridPrefabFollower : MonoBehaviour {

    public float xOffset;
    public float yOffset;
    public bool quantizer;

	// Use this for initialization
	void Start () {
        //mc = GameObject.Find("Main Camera");
	}
	
	// Update is called once per frame
	void Update () {
        gameObject.transform.rotation = Quaternion.identity;
        return;
        /*
        if (mc != null)
        {
            if (((quantizer) && (mc.transform.position.x % 0.5f <= 0.001f)) || (!quantizer))
            {                
                pos = mc.transform.position;
                pos.x += xOffset;
                pos.y += yOffset;
                pos.z = 0;
                transform.position = pos;
                gameObject.transform.rotation = Quaternion.identity;
            }
        }
        */
	}
}
