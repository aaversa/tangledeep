using UnityEngine;

[ExecuteInEditMode]
public class TVShader : MonoBehaviour
{
    public Material material;

    // Use this for initialization
    void Start()
    {
        //material = new Material(Shader.Find("Custom/CRTShader"));
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetTexture("_MainTex", source);
        Graphics.Blit(source, destination, material);
    }
}
