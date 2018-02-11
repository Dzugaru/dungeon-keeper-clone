using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMeshGeneratorTests : MonoBehaviour
{
	void Start ()
    {
        HexMeshGenerator gen = new HexMeshGenerator(18, 1);

        Map map = ScriptableObject.CreateInstance<Map>();
        map.Init(0, -10, 20, 20);
        map.GetCell(5, 1).state = MapCell.State.Low;
        //map.GetCell(2, 0).state = MapCell.State.Low;

        gen.Generate(map, 0, 0);

        Mesh mesh = new Mesh();
        mesh.SetVertices(gen.vertices);
        mesh.SetTriangles(gen.triangles, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        //for (int y = 0; y < 5; y++)
        //{
        //    for (int x = 0; x < 5; x++)
        //    {
        //        Debug.Log(x.ToString() + "," + y + "> " + gen.GetNearCells(x, y));
        //    }
        //}
    }	
}
