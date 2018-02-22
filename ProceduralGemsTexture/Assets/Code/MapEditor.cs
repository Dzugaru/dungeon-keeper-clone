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
    public int meshPatchSize = 18;
    public Map map;

    Transform[] mapMeshObjs;



    void OnValidate()
    {
        floor = new Plane(Vector3.up, Vector3.zero);
        ceiling = new Plane(Vector3.up, new Vector3(0, mapScale, 0));       
    }

    void CreateMapMeshes()
    {
        int meshesW = 3 * map.size / meshPatchSize;
    }
}

