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
        map.GetCell(1, 0).state = MapCell.State.High;
        map.GetCell(6, 2).state = MapCell.State.High;
        map.GetCell(7, 2).state = MapCell.State.High;
        map.GetCell(7, 1).state = MapCell.State.High;
        map.GetCell(8, 1).state = MapCell.State.High;
        map.GetCell(10, -2).state = MapCell.State.High;

        //HexMeshGenerator gen = new HexMeshGenerator(3, 0);

        //Map map = ScriptableObject.CreateInstance<Map>();
        //map.Init(0, -10, 20, 20);
        //map.GetCell(0, 0).state = MapCell.State.High;
        //map.GetCell(2, 0).state = MapCell.State.High;

        //gen.Generate(map, 0, 0, c => c.state == MapCell.State.High);
        gen.Generate(map, 0, 0, c => true);       

        Mesh mesh = new Mesh();
        mesh.SetVertices(gen.vertices);
        mesh.SetTriangles(gen.triangles, 0);
        mesh.SetUVs(0, gen.uvs);
        mesh.RecalculateNormals(); //TODO: remove when use analytical

        GetComponent<MeshFilter>().sharedMesh = mesh;

        GameObject walls = transform.Find("Walls").gameObject;
        Mesh wallsMesh = new Mesh();
        wallsMesh.SetVertices(gen.wallVertices);
        wallsMesh.SetTriangles(gen.wallTriangles, 0);
        wallsMesh.SetUVs(0, gen.wallUVs);
        wallsMesh.RecalculateNormals(); //TODO: remove when use analytical
        walls.GetComponent<MeshFilter>().sharedMesh = wallsMesh;

        //for (int y = 0; y < 5; y++)
        //{
        //    for (int x = 0; x < 5; x++)
        //    {
        //        Debug.Log(x.ToString() + "," + y + "> " + gen.GetNearCells(x, y));
        //    }
        //}
    }	
}
