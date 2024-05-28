using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticleScript : MonoBehaviour {

    private ParticleSystem ps;
    private Renderer rend;
    private float timer;

    public float lifetime;
    
	// Use this for initialization
	void Start () {
        ps = GetComponent<ParticleSystem>();
        rend = ps.GetComponent<Renderer>();
        rend.sortingLayerName = "Foreground";
	}

    void Awake ()
    {
        timer = 0.0f;
    }
	
	// Update is called once per frame
	void Update () {
        if ((ps != null) && (lifetime > 0))
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

	}
}
