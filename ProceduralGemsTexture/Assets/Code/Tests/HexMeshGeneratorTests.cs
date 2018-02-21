using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMeshGeneratorTests : MonoBehaviour
{
    public Map map;
    HexMeshGenerator generator;

	void Awake ()
    {
        generator = new HexMeshGenerator(18, 1);        

        map = ScriptableObject.CreateInstance<Map>();
        map.New(10);
        map.GetCell(1, 0).state = MapCell.State.Full;
        map.GetCell(6, 2).state = MapCell.State.Full;
        map.GetCell(7, 2).state = MapCell.State.Excavated;
        map.GetCell(7, 1).state = MapCell.State.Full;
        map.GetCell(8, 1).state = MapCell.State.Full;
        map.GetCell(10, -2).state = MapCell.State.Full;

        RedrawMeshes();
    }	

    public void RedrawMeshes()
    {
        MeshData floor = new MeshData(), ceiling = new MeshData(), walls = new MeshData();

        generator.Generate(map, 0, 0, c => c.state == MapCell.State.Excavated ? floor : ceiling);
        generator.GenerateWalls(walls);

        Mesh floorMesh = new Mesh();
        floor.SetToMesh(floorMesh);
        floorMesh.RecalculateNormals(); //TODO: remove when use analytical

        Mesh ceilMesh = new Mesh();
        ceiling.SetToMesh(ceilMesh);
        ceilMesh.RecalculateNormals(); //TODO: remove when use analytical

        Mesh wallMesh = new Mesh();
        walls.SetToMesh(wallMesh);
        wallMesh.RecalculateNormals();

        transform.Find("Walls").GetComponent<MeshFilter>().sharedMesh = wallMesh;
        transform.Find("Floor").GetComponent<MeshFilter>().sharedMesh = floorMesh;
        transform.Find("Ceiling").GetComponent<MeshFilter>().sharedMesh = ceilMesh;
    }
}
