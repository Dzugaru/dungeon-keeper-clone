using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackFacesDepthCamera : MonoBehaviour
{
    public Shader backfaceDepthShader;
    void OnEnable()
    {
        Camera camera = GetComponent<Camera>();
        camera.depthTextureMode = DepthTextureMode.Depth;
        camera.SetReplacementShader(backfaceDepthShader, "Gem");
    }    
}
