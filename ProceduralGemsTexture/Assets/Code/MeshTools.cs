using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public static class MeshTools
{
    static readonly float invSqrt3 = 1f / Mathf.Sqrt(3);

    public static Vector2[] HexCellVertices = new Vector2[]
    {
        new Vector2(-0.5f * invSqrt3, 0.5f),
        new Vector2(0.5f * invSqrt3, 0.5f),
        new Vector2(1f * invSqrt3, 0),
        new Vector2(0.5f * invSqrt3, -0.5f),
        new Vector2(-0.5f * invSqrt3, -0.5f),
        new Vector2(-1f * invSqrt3, 0),
    };

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

