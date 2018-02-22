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
    public int newMapRadius = 1;
    public int meshPatchSize = 18;
    public int meshSubdivsShift = 1;
    public Map map;

    public Material floorMat, ceilingMat, wallsMat;

    HexMeshGenerator meshGenerator;
    Transform[] mapPatchObjs;
    int patchesW, patchesH, patchesX0, patchesY0;

    void OnValidate()
    {
        floor = new Plane(Vector3.up, Vector3.zero);
        ceiling = new Plane(Vector3.up, new Vector3(0, mapScale, 0));        
    }    

    Transform CreateMeshGameObject(Transform parent, string name, MeshData meshData, Material mat)
    {
        Transform t = new GameObject(name).transform;
        MeshFilter meshFilter = t.gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        meshData.SetToMesh(mesh);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals(); //TODO: make normals inside generator (analytical)      
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = mat;
        t.parent = parent;
        return t;
    }

    Transform CreatePatch(int i, int j)
    {
        //DEBUG
        //if (i != 0 || j != -1)
        //    return null;

        Transform patch = new GameObject(string.Format("Patch {0} {1}",i,j)).transform;
        MeshData floor = new MeshData(), ceiling = new MeshData(), walls = new MeshData(), mapEdges = new MeshData();
        
        Func<MapCell, MeshData> meshSelector = cell =>
        {
            if (cell == map.externalCell)
                return mapEdges;
            else
                return cell.state == MapCell.State.Excavated ? floor : ceiling;
        };

        meshGenerator.Generate(map, j, i, meshSelector);
        meshGenerator.GenerateWalls(walls);
        
        if (floor.triangles.Count > 0)        
            CreateMeshGameObject(patch, "Floor", floor, floorMat);
        if (ceiling.triangles.Count > 0)
            CreateMeshGameObject(patch, "Ceiling", ceiling, ceilingMat);
        if (floor.triangles.Count > 0)
            CreateMeshGameObject(patch, "Walls", walls, wallsMat);

        //NOTE: we don't draw mapEdges

        //TODO: if nothing was added - don't create a patch

        return patch;
    }

    public void CreateMapMeshes()
    {
        meshGenerator = new HexMeshGenerator(meshPatchSize, meshSubdivsShift);

        //DEBUG
        foreach (var cell in map.AllCells())
        {
            if (HexXY.Dist(new HexXY(map.size / 2, map.size / 2), cell.Coords) < map.size / 3)
                cell.Cell.state = MapCell.State.Excavated;
        }

        int s = (map.size - 1) / meshPatchSize + 1;
        patchesX0 = -2 * s;
        patchesY0 = -1;
        patchesW = 3 * s;
        patchesH = 2 * s + 1;

        mapPatchObjs = new Transform[patchesH * patchesW];

        Transform exPatches = transform.Find("Map patches");
        if(exPatches != null)        
            DestroyImmediate(exPatches.gameObject);
        

        Transform mapPatches = new GameObject("Map patches").transform;
        mapPatches.localScale = new Vector3(mapScale, mapScale, mapScale);
        mapPatches.parent = this.transform;

        for (int i = 0; i < patchesH; i++)
        {
            for (int j = 0; j < patchesW; j++)
            {
                Transform patch = CreatePatch(patchesY0 + i, patchesX0 + j);
                if (patch != null)
                {
                    patch.SetParent(mapPatches, false);
                    Vector2 pos = ((patchesX0 + j) * HexMeshGenerator.rhex + (patchesY0 + i) * HexMeshGenerator.rhey) * meshPatchSize;
                    patch.localPosition = new Vector3(pos.x, 0, pos.y);
                }
            }
        }

        //DEBUG        
        //List<Vector2Int> patchIdxs = new List<Vector2Int>();
        //int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        //foreach(var cell in map.AllCells())
        //{
        //    HexMeshGenerator.ListPatchIndicesForCell(meshPatchSize, cell.Coords, patchIdxs);

        //    foreach(Vector2Int idx in patchIdxs)
        //    {
        //        minX = Mathf.Min(idx.x, minX);
        //        maxX = Mathf.Max(idx.x, maxX);
        //        minY = Mathf.Min(idx.y, minY);
        //        maxY = Mathf.Max(idx.y, maxY);
        //    }
        //    //Debug.Log("Coords: " + cell.Coords.ToString());
        //    //Debug.Log(string.Join(" ", patchIdxs.Select(x => x.ToString()).ToArray()));
        //}

        //Debug.Log(map.size);

        //Debug.Log(string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY));

        //if (minX < patchesX0 || maxX >= patchesX0 + patchesW || minY < patchesY0 || maxY >= patchesY0 + patchesH)
        //    Debug.Log(string.Format("Wrong! {0} {1} {2} {3}", patchesX0, patchesY0, patchesX0 + patchesW - 1, patchesY0 + patchesH - 1));
    }
}

