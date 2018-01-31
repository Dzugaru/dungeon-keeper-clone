using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    Transform swivel, stick;

    float zoom = 1;
    float rotationAngle = 0;

    public float minZoom, maxZoom, zoomSpeed, rotationSpeed;

	void Start()
    {
        swivel = transform.Find("Swivel");
        stick = swivel.Find("Stick");        
    }
	
	void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
            AdjustZoom(zoomDelta);

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)        
            AdjustRotation(rotationDelta);        
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta * zoomSpeed);
        float distance = Mathf.Lerp(minZoom, maxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);        
    }

    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0)        
            rotationAngle += 360f;
        else if (rotationAngle >= 360f)        
            rotationAngle -= 360f;

        transform.localRotation = Quaternion.AngleAxis(rotationAngle, Vector3.up);
    }
}
