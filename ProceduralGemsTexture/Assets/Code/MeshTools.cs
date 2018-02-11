using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public static class MeshTools
{
   

    public static Mesh CombineGameObjectMeshes(IEnumerable<GameObject> gameObjects)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach(var obj in gameObjects)
        {
            Mesh objMesh = obj.GetComponent<MeshFilter>().sharedMesh;
            int baseVIdx = vertices.Count;

            foreach(Vector3 v in objMesh.vertices)            
                vertices.Add(obj.transform.TransformPoint(v));

            foreach (int idx in objMesh.triangles)
                triangles.Add(baseVIdx + idx);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }
}

