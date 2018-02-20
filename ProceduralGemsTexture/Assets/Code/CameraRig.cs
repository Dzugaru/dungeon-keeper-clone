using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    Transform swivel, stick;

    float zoom = 0;
    float rotationAngle = 0;

    public float minZoom, maxZoom, zoomSpeed, rotationSpeed, scrollSpeed;

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

        float dx = Input.GetAxis("Horizontal");
        float dy = Input.GetAxis("Vertical");

        if(dx != 0 || dy != 0)        
            AdjustPosition(dx, dy);        
    }

    void AdjustPosition(float dx, float dy)
    {        
        Vector3 disp = transform.localRotation * new Vector3(dx, 0, dy) * scrollSpeed;
        transform.localPosition = transform.localPosition + disp;
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
