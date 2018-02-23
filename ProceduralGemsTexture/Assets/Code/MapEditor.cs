using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[ExecuteInEditMode]
public class MapEditor : MonoBehaviour
{
    const string mapPatchesName = "Map patches";

    public enum BrushMode
    {
        Excavate,
        Fill
    }

    [NonSerialized]
    public Plane floor, ceiling;

    public float mapScale = 10f;
    public int newMapRadius = 1;
    public int meshPatchSize = 18;
    public int meshSubdivsShift = 1;
    public int brushSize = 1;
    public BrushMode brushMode;
    public Color fillBrushColor, excavateBrushColor;
    public Map map;

    public Material floorMat, ceilingMat, wallsMat;

    HexMeshGenerator meshGenerator;
    Transform patchesRoot;
    Transform[] mapPatchObjs;
    int patchesW, patchesH, patchesX0, patchesY0;

    [NonSerialized]
    public MapCellAndCoords? currentMousePos;

    Mesh brushCenter, brushOutside;

    void OnValidate()
    {
        OnParameterChanged();
    }

    public void OnParameterChanged()
    {
        floor = new Plane(Vector3.up, Vector3.zero);
        ceiling = new Plane(Vector3.up, new Vector3(0, mapScale, 0));

        GenerateBrushMeshes(0.9f);
    }

    void OnEnable()
    {
        if(map != null && patchesW == 0)
        {
            //Fill mapPatchObjs back
            int s = (map.size - 1) / meshPatchSize + 1;
            patchesX0 = -2 * s;
            patchesY0 = -1;
            patchesW = 3 * s;
            patchesH = 2 * s + 1;

            patchesRoot = transform.Find(mapPatchesName);
            mapPatchObjs = new Transform[patchesH * patchesW];

            for (int i = 0; i < patchesH; i++)
            {
                for (int j = 0; j < patchesW; j++)
                {
                    Transform patch = patchesRoot.Find(GetPatchName(patchesY0 + i, patchesX0 + j));                    
                    mapPatchObjs[i * patchesW + j] = patch;                    
                }
            }

            Debug.Log("Filled map patches back");
        }
    }

    void GenerateBrushMeshes(float offset)
    {
        brushCenter = new Mesh();
        brushOutside = new Mesh();
        MeshData brushCenterData = new MeshData();
        MeshData brushOutsizeData = new MeshData();

        for (int i = -brushSize+1; i < brushSize; i++)
        {
            for (int j = -brushSize + 1; j < brushSize; j++)
            {
                if (HexXY.Dist(new HexXY(i, j), new HexXY(0, 0)) > brushSize - 1)
                    continue;

                Vector2 c = new HexXY(i, j).ToPlaneCoordinates();
                if(i == 0 && j == 0)
                {
                    int vidx = brushCenterData.vertices.Count;
                    brushCenterData.vertices.Add(new Vector3(c.x, 0, c.y));
                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 hc = c + HexMeshGenerator.HexCellVertices[k] * offset;
                        brushCenterData.vertices.Add(new Vector3(hc.x, 0, hc.y));
                        brushCenterData.triangles.Add(vidx);
                        brushCenterData.triangles.Add(vidx + k + 1);
                        brushCenterData.triangles.Add(vidx + (k + 1) % 6 + 1);
                    }                                        
                }
                else
                {
                    int vidx = brushOutsizeData.vertices.Count;
                    brushOutsizeData.vertices.Add(new Vector3(c.x, 0, c.y));
                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 hc = c + HexMeshGenerator.HexCellVertices[k] * offset;
                        brushOutsizeData.vertices.Add(new Vector3(hc.x, 0, hc.y));
                        brushOutsizeData.triangles.Add(vidx);
                        brushOutsizeData.triangles.Add(vidx + k + 1);
                        brushOutsizeData.triangles.Add(vidx + (k + 1) % 6 + 1);
                    }
                }
            }
        }

        brushCenterData.SetToMesh(brushCenter);
        brushOutsizeData.SetToMesh(brushOutside);
        brushCenter.RecalculateNormals();
        brushOutside.RecalculateNormals();
    }

    Transform CreateMeshGameObject(Transform parent, string name, MeshData meshData, Material mat)
    {
        Transform t = new GameObject(name).transform;
        MeshFilter meshFilter = t.gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        meshData.SetToMesh(mesh);        
        mesh.RecalculateBounds(); //TODO: do this when constructing too        
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = mat;
        t.parent = parent;
        return t;
    }

    string GetPatchName(int i, int j)
    {
        return string.Format("Patch {0} {1}", i, j);
    }

    Transform CreatePatch(int i, int j)
    {
        //DEBUG
        //if (i != 2 || j != -2)
        //    return null;

        Transform patch = new GameObject(GetPatchName(i,j)).transform;
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

        bool isEmpty = true;

        if (floor.triangles.Count > 0)
        {
            CreateMeshGameObject(patch, "Floor", floor, floorMat);
            isEmpty = false;
        }
        if (ceiling.triangles.Count > 0)
        {
            CreateMeshGameObject(patch, "Ceiling", ceiling, ceilingMat);
            isEmpty = false;
        }
        if (walls.triangles.Count > 0)
        {
            CreateMeshGameObject(patch, "Walls", walls, wallsMat);
            isEmpty = false;
        }

        //NOTE: we don't draw mapEdges

        if (isEmpty)
        {
            DestroyImmediate(patch.gameObject);
            return null;
        }
        else
            return patch;
    }

    public void CreateMapMeshes()
    {
        meshGenerator = new HexMeshGenerator(meshPatchSize, meshSubdivsShift);

        //DEBUG some walls
        foreach (var cell in map.AllCells())
        {
            if (HexXY.Dist(new HexXY(map.size / 2, map.size / 2), cell.Coords) < map.size / 3)
                cell.Cell.state = MapCell.State.Excavated;
        }
        //-------------

        int s = (map.size - 1) / meshPatchSize + 1;
        patchesX0 = -2 * s;
        patchesY0 = -1;
        patchesW = 3 * s;
        patchesH = 2 * s + 1;

        mapPatchObjs = new Transform[patchesH * patchesW];
                
        if(patchesRoot != null)        
            DestroyImmediate(patchesRoot.gameObject);
        

        patchesRoot = new GameObject(mapPatchesName).transform;
        patchesRoot.localScale = new Vector3(mapScale, mapScale, mapScale);
        patchesRoot.parent = this.transform;

        for (int i = 0; i < patchesH; i++)
        {
            for (int j = 0; j < patchesW; j++)
            {
                Transform patch = CreatePatch(patchesY0 + i, patchesX0 + j);
                if (patch != null)
                {
                    patch.SetParent(patchesRoot, false);
                    Vector2 pos = ((patchesX0 + j) * HexMeshGenerator.rhex + (patchesY0 + i) * HexMeshGenerator.rhey) * meshPatchSize;
                    patch.localPosition = new Vector3(pos.x, 0, pos.y);
                    mapPatchObjs[i * patchesW + j] = patch;
                }
            }
        }

        //DEBUG patches bounds checking...      
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

    void RecreateMapPatchMesh(Vector2Int c)
    {
        if(meshGenerator == null)
            meshGenerator = new HexMeshGenerator(meshPatchSize, meshSubdivsShift);

        int idx = (c.y - patchesY0) * patchesW + (c.x - patchesX0);
        DestroyImmediate(mapPatchObjs[idx].gameObject);

        Transform patch = CreatePatch(c.y, c.x);
        if (patch != null)
        {
            patch.SetParent(patchesRoot, false);
            Vector2 pos = (c.x * HexMeshGenerator.rhex + c.y * HexMeshGenerator.rhey) * meshPatchSize;
            patch.localPosition = new Vector3(pos.x, 0, pos.y);
            mapPatchObjs[idx] = patch;
        }
    }

    //Returns if position changed
    public bool SetCurrentMousePos(MapCellAndCoords? v)
    {
        if ((v != null) != (currentMousePos != null) || (v != null && currentMousePos != null && v.Value.Coords != currentMousePos.Value.Coords))
        {
            currentMousePos = v;
            SceneView.RepaintAll();
            return true;
        }
        return false;
    }

    public void DrawWithBrush()
    {
        if (currentMousePos == null) return;

        HexXY coords = currentMousePos.Value.Coords;
        HashSet<Vector2Int> patchesChanged = new HashSet<Vector2Int>();
        List<Vector2Int> patchIndicesForCell = new List<Vector2Int>();

        for (int i = -brushSize + 1; i < brushSize; i++)
        {
            for (int j = -brushSize + 1; j < brushSize; j++)
            {
                if (HexXY.Dist(new HexXY(i, j), new HexXY(0, 0)) > brushSize - 1)
                    continue;

                HexXY c = coords + new HexXY(i, j);
                MapCell cell = map.GetCell(c);
                if (cell == map.externalCell)
                    continue;

                bool changed = false;
                switch(brushMode)
                {
                    case BrushMode.Excavate:
                        if(cell.state != MapCell.State.Excavated)
                        {
                            changed = true;
                            cell.state = MapCell.State.Excavated;
                        }                        
                        break;
                    case BrushMode.Fill:
                        if (cell.state != MapCell.State.Full)
                        {
                            changed = true;
                            cell.state = MapCell.State.Full;
                        }
                        break;
                }

                if(changed)
                {
                    HexMeshGenerator.ListPatchIndicesForCell(meshPatchSize, c, patchIndicesForCell);
                    foreach(Vector2Int pi in patchIndicesForCell)
                        patchesChanged.Add(pi);
                }
            }
        }

        if (patchesChanged.Count > 0)
        {
            //Fix outermost wall issues (when using ListPatchIndicesForCell it may happen that we don't redraw outermost walls)
            //We do this by adding cells just outside the brush if anything is changed
            for (int i = -brushSize; i < brushSize + 1; i++)
            {
                for (int j = -brushSize; j < brushSize + 1; j++)
                {
                    if (HexXY.Dist(new HexXY(i, j), new HexXY(0, 0)) == brushSize)
                    {
                        HexXY c = coords + new HexXY(i, j);
                        HexMeshGenerator.ListPatchIndicesForCell(meshPatchSize, c, patchIndicesForCell);
                        foreach (Vector2Int pi in patchIndicesForCell)
                            patchesChanged.Add(pi);
                    }
                }
            }
        }

        foreach (Vector2Int c in patchesChanged)
            RecreateMapPatchMesh(c);

        if (patchesChanged.Count > 0)
            EditorUtility.SetDirty(map);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void OnDrawGizmosSelected()
    {        
        if (currentMousePos != null)
        {
            MapCell cell = currentMousePos.Value.Cell;
            Vector2 coords = currentMousePos.Value.Coords.ToPlaneCoordinates();

            float h = cell.state == MapCell.State.Full ? 1.1f : 0.1f;

            if (brushSize > 0)
            {
                Color color = brushMode == BrushMode.Fill ? fillBrushColor : excavateBrushColor;

                Gizmos.color = color;
                Gizmos.DrawMesh(brushCenter, new Vector3(coords.x, h, coords.y) * mapScale, Quaternion.identity, new Vector3(mapScale, mapScale, mapScale));

                if (brushSize > 1)
                {
                    Color outerColor = color;
                    outerColor.a -= 0.2f;
                    Gizmos.color = outerColor;
                    Gizmos.DrawMesh(brushOutside, new Vector3(coords.x, h, coords.y) * mapScale, Quaternion.identity, new Vector3(mapScale, mapScale, mapScale));
                }
            }           
        }
    }
}

