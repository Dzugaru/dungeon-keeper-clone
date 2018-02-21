using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
public class MapEditor : MonoBehaviour
{
    [NonSerialized]
    public Plane floor, ceiling;

    public float mapScale = 10f;
    public int newMapSize = 5;
    public Map map;

    void OnValidate()
    {
        floor = new Plane(Vector3.up, Vector3.zero);
        ceiling = new Plane(Vector3.up, new Vector3(0, mapScale, 0));

        //Debug.Log("updated");
    }
}

