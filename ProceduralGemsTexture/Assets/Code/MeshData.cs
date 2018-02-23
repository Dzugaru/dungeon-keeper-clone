using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> uvs;
    public List<Vector3> normals;

    public MeshData()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
    }

    public void SetToMesh(Mesh mesh)
    {
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        if (normals.Count > 0)
            mesh.SetNormals(normals);
    }
}

