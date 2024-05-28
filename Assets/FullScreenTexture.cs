using UnityEngine;
using System.Collections;

public class FullScreenTexture : MonoBehaviour
{
    void Update()
    {
        float height = Camera.main.orthographicSize * 2f;
        float width = Camera.main.aspect * height;
        transform.localScale = new Vector3(width, height, 1f);
    }
}

