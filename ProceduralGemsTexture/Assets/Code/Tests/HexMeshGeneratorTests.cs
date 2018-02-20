using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMeshGeneratorTests : MonoBehaviour
{
	void Start ()
    {
        HexMeshGenerator gen = new HexMeshGenerator(18, 1);
        //HexMeshGenerator gen = new HexMeshGenerator(3, 0);

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
        MeshData floor = new MeshData(), ceiling = new MeshData(), walls = new MeshData();

        gen.Generate(map, 0, 0, c => c.state == MapCell.State.Low ? floor : ceiling);        
        gen.GenerateWalls(walls);

        Mesh floorMesh = new Mesh();
        floor.SetToMesh(floorMesh);        
        floorMesh.RecalculateNormals(); //TODO: remove when use analytical

        Mesh ceilMesh = new Mesh();
        ceiling.SetToMesh(ceilMesh);
        ceilMesh.RecalculateNormals();

        Mesh wallMesh = new Mesh();
        walls.SetToMesh(wallMesh);
        wallMesh.RecalculateNormals();
        

        transform.Find("Walls").GetComponent<MeshFilter>().sharedMesh = wallMesh;
        transform.Find("Floor").GetComponent<MeshFilter>().sharedMesh = floorMesh;
        transform.Find("Ceiling").GetComponent<MeshFilter>().sharedMesh = ceilMesh;
       

        //for (int y = 0; y < 5; y++)
        //{
        //    for (int x = 0; x < 5; x++)
        //    {
        //        Debug.Log(x.ToString() + "," + y + "> " + gen.GetNearCells(x, y));
        //    }
        //}
    }	
}
