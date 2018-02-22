using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class HexMeshGenerator
{
    static readonly float invSqrt3 = 1f / Mathf.Sqrt(3);

    //rhombus grid basis vectors
    public static readonly Vector2 rhex = new Vector2(1f * invSqrt3, 0);
    public static readonly Vector2 rhey = new Vector2(0.5f * invSqrt3, 0.5f);

    public static Vector2[] HexCellVertices = new Vector2[]
    {
        new Vector2(-0.5f * invSqrt3, 0.5f),
        new Vector2(0.5f * invSqrt3, 0.5f),
        new Vector2(1f * invSqrt3, 0),
        new Vector2(0.5f * invSqrt3, -0.5f),
        new Vector2(-0.5f * invSqrt3, -0.5f),
        new Vector2(-1f * invSqrt3, 0),
    };

    sealed class WallVerticesInfo
    {
        public List<Vector3> High = new List<Vector3>();
        public List<Vector3> Low = new List<Vector3>();
        public List<Vector2Int> GridCoords = new List<Vector2Int>();
    }

    //Info on all vertices than are in a cell.
    //We use it in constructing triangles indices.
    sealed class CellVertInfo
    {
        public readonly HexXY Coords; //TODO: remove, not needed by anyone
        public MeshData Mesh;
        public int LastRowIdx = -1;
        public List<int> RowStarts = new List<int>();
        public List<int> Idxs = new List<int>();

        //Info about adjacent cells vertices to construct walls
        public int WallCount = 0;
        public WallVerticesInfo[] WallVertices;

        public CellVertInfo(HexXY coords)
        {
            this.Coords = coords;
            WallVertices = new WallVerticesInfo[6];
            for (int i = 0; i < 6; i++)            
                WallVertices[i] = new WallVerticesInfo();            
        }

        public void SetMesh(MeshData mesh)
        {
            //DEBUG---
            if (Mesh != null && mesh != this.Mesh)
                throw new InvalidProgramException();
            //--------
            this.Mesh = mesh;
        }

        public void AddIdx(int row, int idx)
        {
            if(row != LastRowIdx)
            {
                LastRowIdx = row;
                RowStarts.Add(Idxs.Count);
            }
            Idxs.Add(idx);
        }

        public void Clear()
        {
            LastRowIdx = -1;
            RowStarts.Clear();
            Idxs.Clear();
            Mesh = null;
            for (int i = 0; i < 6; i++)
            {
                WallCount = 0;
                WallVertices[i].High.Clear();
                WallVertices[i].Low.Clear();
                WallVertices[i].GridCoords.Clear();
            }            
        }
    }

    struct VertInStack
    {
        public MapCell cell;
        public HexXY coords;
        public MeshData mesh;
        public int vidx;
    }

    public struct NearCells
    {
        public int Count;
        public HexXY C1, C2, C3;

        public NearCells(int x1, int y1)
        {
            this.Count = 1;
            this.C1 = new HexXY(x1, y1);
            this.C2 = this.C3 = new HexXY();
        }

        public NearCells(int x1, int y1, int x2, int y2)
        {
            this.Count = 2;
            this.C1 = new HexXY(x1, y1);
            this.C2 = new HexXY(x2, y2);
            this.C3 = new HexXY();
        }

        public NearCells(int x1, int y1, int x2, int y2, int x3, int y3)
        {
            this.Count = 3;
            this.C1 = new HexXY(x1, y1);
            this.C2 = new HexXY(x2, y2);
            this.C3 = new HexXY(x3, y3);
        }

        public HexXY GetByIndex(int i)
        {
            return i == 0 ? C1 : (i == 1 ? C2 : C3);
        }

        public override string ToString()
        {
            switch(Count)
            {
                case 1:
                    return string.Format("{0}", C1);
                case 2:
                    return string.Format("{0};{1}", C1, C2);
                case 3:
                    return string.Format("{0};{1};{2}", C1, C2, C3);
            }

            throw new InvalidProgramException();
        }
    }

    int patchSize, subDivs, subDivsShift, subDivsMask; 
    int cxMin, cyMin, cw, ch;
    Vector2 subdivRhex, subdivRhey;

    CellVertInfo[] cellVertInfos;
    List<VertInStack> vertsInStack;

    Map map;
    int patchOffsetX, patchOffsetY, mapOffsetX, mapOffsetY;    
    Func<MapCell, MeshData> selectCellMesh;

    PerlinNoise noise;

    public HexMeshGenerator(int patchSize, int subDivsShift)
    {
        Debug.Assert((patchSize % 3) == 0);

        this.patchSize = patchSize;
        this.subDivsShift = subDivsShift;
        this.subDivs = 1 << subDivsShift;
        this.subDivsMask = subDivs - 1;

        this.cxMin = 0;
        this.cyMin = -patchSize / 3;
        this.cw =  patchSize + 1;        
        this.ch = 2 * patchSize / 3 + 1;

        this.subdivRhex = rhex / subDivs;
        this.subdivRhey = rhey / subDivs;

        this.cellVertInfos = new CellVertInfo[cw * ch];
        for (int i = 0; i < ch; i++)
        {
            for (int j = 0; j < cw; j++)
            {
                CellVertInfo cvi = new CellVertInfo(new HexXY(cxMin + j, cyMin + i));       
                this.cellVertInfos[i * cw + j] = cvi;
            }
        }
        this.vertsInStack = new List<VertInStack>(3);

        noise = new PerlinNoise();
    }

    public static void ListPatchIndicesForCell(int patchSize, HexXY c, List<Vector2Int> meshPartsIdxs)
    {
        meshPartsIdxs.Clear();

        int x = c.x - 2 * c.y;
        int y = c.x + c.y;

        int ix;
        if (x >= 0)
            ix = x / patchSize;
        else
            ix = (x - patchSize + 1) / patchSize;        
        int iy = y / patchSize;

        int fx = x - ix * patchSize;
        int fy = y - iy * patchSize;

        meshPartsIdxs.Add(new Vector2Int(ix, iy));
        if (fx == 0)
        {
            meshPartsIdxs.Add(new Vector2Int(ix - 1, iy));
            if (fy == 0)
            {
                meshPartsIdxs.Add(new Vector2Int(ix, iy - 1));
                meshPartsIdxs.Add(new Vector2Int(ix - 1, iy - 1));
            }
        }
        else if(fy == 0)
        {
            meshPartsIdxs.Add(new Vector2Int(ix, iy - 1));
        }
    }

    //WARNING: this one is insane
    //---
    //We've got a rhombus grid overlaid on a hex grid
    //with basis vectors of 1/3 * (ex - ey) and 1/3 * (2*ex + ey) where
    //ex and ey are basis vectors of our hex grid.
    //Using this we can get hex cells coords from rhombus grid coords like this:
    //x = (col + 2*row) / 3; y = (row - col) / 3;
    //After that we can observe that rhombus grid points can fall into only 3 distinct parts of hex cells
    //determined by mods of our /3 divisions - mods can be 0,0;1,2;2;1, corresponding to "config" 0, 1 and 2
    //Carefully observing various cases of points in a subdivided rhombus grid we can get what hex cells include them    
    public NearCells GetNearCells(int x, int y)
    {
        int col = x >> subDivsShift;
        int row = y >> subDivsShift;
        int fx = x & subDivsMask;
        int fy = y & subDivsMask;

        int cx = (col + 2 * row) / 3;
        int cy;
        if (row - col >= 0)
            cy = (row - col) / 3;
        else
            cy = (row - col - 2) / 3;

        int config = (col + 2 * row) % 3;
        switch (config)
        {
            case 0: //rhomb is split between (cx, cy) and (cx+1, cy) cells
                int fs = fx + fy;
                if (fs < subDivs)
                    return new NearCells(cx, cy);
                if (fs > subDivs)
                    return new NearCells(cx + 1, cy);                

                return new NearCells(cx, cy, cx + 1, cy);

            case 1: //rhomb lies inside right lower part of (cx+1, cy+1) cell
                if (fy == 0)
                {
                    if (fx == 0)
                        return new NearCells(cx, cy + 1, cx + 1, cy + 1, cx, cy);
                    else
                        return new NearCells(cx + 1, cy + 1, cx, cy);
                }
                else
                {
                    return new NearCells(cx + 1, cy + 1);
                }

            case 2: //rhomb lies inside left upper part of (cx + 1, cy) cell
                if (fx == 0)
                {
                    if (fy == 0)
                        return new NearCells(cx, cy, cx + 1, cy, cx + 1, cy + 1);
                    else
                        return new NearCells(cx + 1, cy, cx + 1, cy + 1);
                }
                else
                {
                    return new NearCells(cx + 1, cy);
                }
        }

        throw new InvalidProgramException();
    }    

    void Reset()
    {
        foreach (CellVertInfo cvi in cellVertInfos)
            cvi.Clear();
    }

    private void AddNearCellVertices(int row, int col, HexXY cellCoords, MapCell cell)
    {
        MeshData mesh = selectCellMesh(cell);

        int sharedIdx = -1;
        for (int l = 0; l < vertsInStack.Count; l++)
        {
            if (vertsInStack[l].mesh == mesh)
            {
                sharedIdx = vertsInStack[l].vidx;
                break;
            }
        }

        int idx;
        if (sharedIdx == -1)
        {
            Vector2 patchOffset = patchOffsetX * patchSize * rhex + patchOffsetY * patchSize * rhey;

            idx = mesh.vertices.Count;
            Vector2 planeCoords = row * subdivRhey + col * subdivRhex;
            float y = cell.state == MapCell.State.Full ? 1 : 0;
            Vector3 vertex = new Vector3(planeCoords.x, y, planeCoords.y);

            PerlinNoiseSample vNoise = noise.Perlin2D(patchOffset + planeCoords, 0.5f);
            vertex.y += vNoise.value * 0.3f;

            mesh.vertices.Add(vertex);
            Vector2 uvPos = patchOffset + new Vector2(planeCoords.x, planeCoords.y);
            mesh.uvs.Add(uvPos);            
        }
        else
        {
            idx = sharedIdx;
        }

        vertsInStack.Add(new VertInStack() { cell = cell, mesh = mesh, vidx = idx, coords = cellCoords });
        CellVertInfo cvi = cellVertInfos[(cellCoords.y - cyMin) * cw + (cellCoords.x - cxMin)];
        cvi.SetMesh(mesh);
        cvi.AddIdx(row, idx);
    }

    private void AddWallInfo(int i, int j)
    {
        for (int k = 0; k < vertsInStack.Count; k++)
        {
            VertInStack vis = vertsInStack[k];
            CellVertInfo cvi = cellVertInfos[(vis.coords.y - cyMin) * cw + (vis.coords.x - cxMin)];

            if (vis.cell.state == MapCell.State.Full)
            {
                for (int l = 0; l < vertsInStack.Count; l++)
                {
                    VertInStack vis2 = vertsInStack[l];
                    if (vis2.cell.state == MapCell.State.Excavated)
                    {
                        HexXY diff = vis2.coords - vis.coords;
                        int neighIdx = HexXY.DiffToNeighIndex(diff.x, diff.y);
                        cvi.WallVertices[neighIdx].High.Add(vis.mesh.vertices[vis.vidx]);
                        cvi.WallVertices[neighIdx].Low.Add(vis2.mesh.vertices[vis2.vidx]);
                        cvi.WallVertices[neighIdx].GridCoords.Add(new Vector2Int(i, j));
                        cvi.WallCount++;
                    }
                }
            }
        }
    }

    void CreateVerticesAndAssignToCells()
    {        
        for (int i = 0; i < patchSize * subDivs + 1; i++)
        {
            for (int j = 0; j < patchSize * subDivs + 1; j++)
            {                
                NearCells nc = GetNearCells(j, i);
                vertsInStack.Clear();
                for (int k = 0; k < nc.Count; k++)
                {
                    HexXY cellCoords = nc.GetByIndex(k);
                    MapCell cell = map.GetCell(mapOffsetX + cellCoords.x, mapOffsetY + cellCoords.y);
                    AddNearCellVertices(i, j, cellCoords, cell);
                }                
                
                AddWallInfo(i, j);  
            }
        }
    }

    public void Generate(Map map, int patchOffsetX, int patchOffsetY, Func<MapCell, MeshData> selectCellMesh)
    {
        Reset();       
        this.map = map;

        this.patchOffsetX = patchOffsetX;
        this.patchOffsetY = patchOffsetY;
        this.mapOffsetX = (2 * patchOffsetY + patchOffsetX) * patchSize / 3;
        this.mapOffsetY = (patchOffsetY - patchOffsetX) * patchSize / 3;
      
        this.selectCellMesh = selectCellMesh;

        CreateVerticesAndAssignToCells();      

        //Fill triangles indices separately for each hex cell
        //using vertIndicesByCell saved data
        for (int i = 0; i < ch; i++)        
            for (int j = 0; j < cw; j++)
                FillTriangleIndices(cellVertInfos[i * cw + j]);        
    }

    private void FillTriangleIndices(CellVertInfo cvi)
    {
        MeshData mesh = cvi.Mesh;
        for (int r = 0; r < cvi.RowStarts.Count - 1; r++)
        {
            int rowStart = cvi.RowStarts[r];
            int nextRowStart = cvi.RowStarts[r + 1];
            int afterNextRowStart = r == cvi.RowStarts.Count - 2 ? cvi.Idxs.Count : cvi.RowStarts[r + 2];

            int rowLen = nextRowStart - rowStart;
            int nextRowLen = afterNextRowStart - nextRowStart;

            //TODO: simplify cases?
            if (rowLen == nextRowLen) //this rhombus part always leans to the right
            {
                for (int k = 0; k < rowLen - 1; k++)
                {
                    mesh.triangles.Add(cvi.Idxs[rowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                }
            }
            else if (rowLen < nextRowLen)
            {
                mesh.triangles.Add(cvi.Idxs[rowStart]);
                mesh.triangles.Add(cvi.Idxs[nextRowStart]);
                mesh.triangles.Add(cvi.Idxs[nextRowStart + 1]);

                for (int k = 0; k < rowLen - 1; k++)
                {
                    mesh.triangles.Add(cvi.Idxs[rowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                    mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k + 2]);
                }
            }
            else
            {
                for (int k = 0; k < rowLen - 1; k++)
                {
                    mesh.triangles.Add(cvi.Idxs[rowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[nextRowStart + k]);
                    mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    if (k < rowLen - 2)
                    {
                        mesh.triangles.Add(cvi.Idxs[rowStart + k + 1]);
                        mesh.triangles.Add(cvi.Idxs[nextRowStart + k]);
                        mesh.triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                    }
                }
            }
        }
    }

    //WARNING: this one is eldritch too
    //---
    //We (almost) plane-project UVs on walls from 2y + x direction (perpendicular to direction of 2 and 5 neighs),
    //but we must take care of 0,1,3,4 neigh walls squeezing.
    //This can be done by carefully inspecting areas when 2y + x should be un-squeezed
    float WallUForGridCoords(Vector2Int gridCoords)
    {   
        int u = gridCoords.y * 2 + gridCoords.x;
        int iu = u >> subDivsShift;
        int fu = u & subDivsMask;

        if ((iu % 3) == 0)
            fu = fu + (subDivs >> 1);
        iu -= iu / 3;

        return iu + (float)fu / subDivs;  
    }

    public void GenerateWalls(MeshData wallMesh)
    {
        int nextIdx = 0;

        int cw = patchSize + 1;
        int ch = 2 * patchSize / 3 + 1;

        for (int i = 0; i < ch; i++)
        {
            for (int j = 0; j < cw; j++)
            {
                CellVertInfo cvi = cellVertInfos[i * cw + j];
                if (cvi.WallCount > 1)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        WallVerticesInfo wis = cvi.WallVertices[k];

                        for (int l = 0; l < wis.High.Count; l++)
                        {
                            //Verts
                            Vector3 highVert = wis.High[l];
                            Vector3 lowVert = wis.Low[l];

                            wallMesh.vertices.Add(highVert);
                            wallMesh.vertices.Add(lowVert);

                            //UVs
                            Vector2Int gridCoords = wis.GridCoords[l];

                            //TODO: this is possibly wrong (offsets)
                            gridCoords.x += patchOffsetX * patchSize * subDivs;
                            gridCoords.y += patchOffsetY * patchSize * subDivs;

                            float u = WallUForGridCoords(gridCoords);
                            wallMesh.uvs.Add(new Vector2(u, 1));
                            wallMesh.uvs.Add(new Vector2(u, 0));
                        }

                        for (int l = 0; l < wis.High.Count - 1; l++)
                        {
                            //Different winding order due to wall vertices
                            //filling order in main mesh generation above
                            if (k >= 1 && k <= 3)
                            {
                                wallMesh.triangles.Add(nextIdx);
                                wallMesh.triangles.Add(nextIdx + 3);
                                wallMesh.triangles.Add(nextIdx + 1);

                                wallMesh.triangles.Add(nextIdx);
                                wallMesh.triangles.Add(nextIdx + 2);
                                wallMesh.triangles.Add(nextIdx + 3);
                            }
                            else
                            {
                                wallMesh.triangles.Add(nextIdx);
                                wallMesh.triangles.Add(nextIdx + 1);
                                wallMesh.triangles.Add(nextIdx + 3);

                                wallMesh.triangles.Add(nextIdx);
                                wallMesh.triangles.Add(nextIdx + 3);
                                wallMesh.triangles.Add(nextIdx + 2);
                            }

                            nextIdx += 2;
                        }

                        nextIdx = wallMesh.vertices.Count;
                    }
                }
            }
        }
    }

    
}

