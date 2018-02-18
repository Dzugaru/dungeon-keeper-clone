using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class HexMeshGenerator
{
    static readonly float invSqrt3 = 1f / Mathf.Sqrt(3);

    //rhombus grid basis vectors
    static readonly Vector2 rhex = new Vector2(1f * invSqrt3, 0);
    static readonly Vector2 rhey = new Vector2(0.5f * invSqrt3, 0.5f);

    public static Vector2[] HexCellVertices = new Vector2[]
    {
        new Vector2(-0.5f * invSqrt3, 0.5f),
        new Vector2(0.5f * invSqrt3, 0.5f),
        new Vector2(1f * invSqrt3, 0),
        new Vector2(0.5f * invSqrt3, -0.5f),
        new Vector2(-0.5f * invSqrt3, -0.5f),
        new Vector2(-1f * invSqrt3, 0),
    };

    sealed class WallVertIndices
    {
        public List<int> High = new List<int>();
        public List<int> Low = new List<int>();
    }

    //Info on all vertices than are in a cell.
    //We use it in constructing triangles indices.
    sealed class CellVertInfo
    {
        public readonly HexXY Coords; //TODO: remove, not needed by anyone
        public int LastRowIdx = -1;
        public List<int> RowStarts = new List<int>();
        public List<int> Idxs = new List<int>();

        //Info about adjacent cells vertices to construct walls
        public int WallCount = 0;
        public WallVertIndices[] WallIndices;

        public CellVertInfo(HexXY coords)
        {
            this.Coords = coords;
            WallIndices = new WallVertIndices[6];
            for (int i = 0; i < 6; i++)            
                WallIndices[i] = new WallVertIndices();            
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
            for (int i = 0; i < 6; i++)
            {
                WallCount = 0;
                WallIndices[i].High.Clear();
                WallIndices[i].Low.Clear();               
            }            
        }
    }

    struct VertInStack
    {
        public MapCell cell;
        public HexXY coords;
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
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> uvs;

    //Needed for UVs
    List<Vector2Int> vertGridCoords;

    public List<Vector3> wallVertices;
    public List<int> wallTriangles;
    public List<Vector2> wallUVs;

    List<VertInStack> vertsInStack;

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

        this.vertices = new List<Vector3>();
        this.triangles = new List<int>();
        this.uvs = new List<Vector2>();
        this.vertGridCoords = new List<Vector2Int>();

        this.wallVertices = new List<Vector3>();
        this.wallTriangles = new List<int>();
        this.wallUVs = new List<Vector2>();

        this.vertsInStack = new List<VertInStack>(3);
    }

    //WARNING: this one is insane
    //---
    //We've got a rhombus grid overlaid on a hex grid
    //with basis vectors of 1/3 * (ex - ey) and 1/3 * (2*ex + ey) where
    //ex and ey are basis vectors of our hex grid.
    //Using this we can get hex cells coords from rhombus grid coords like this:
    //x = (row + 2*col) / 3; y = (col - row) / 3;
    //After that we can observe that rhombus grid points can fall into only 3 distinct parts of hex cells
    //determined by mods of our /3 divisions - mods can be 0,0;1,2;2;1, corresponding to "config" 0, 1 and 2
    //Carefully observing various cases of points in a subdivided rhombus grid we can get what hex cells include them    
    public NearCells GetNearCells(int x, int y)
    {
        int row = x >> subDivsShift;
        int col = y >> subDivsShift;
        int fx = x & subDivsMask;
        int fy = y & subDivsMask;

        int cx = (row + 2 * col) / 3;
        int cy;
        if (col - row >= 0)
            cy = (col - row) / 3;
        else
            cy = (col - row - 2) / 3;

        int config = (row + 2 * col) % 3;
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

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        wallVertices.Clear();
        wallTriangles.Clear();
        wallUVs.Clear();
    }

    private void AddNearCellVertices(ref int nextIdx, int row, int col, int k, HexXY cellCoords, MapCell cell)
    {
        int sharedIdx = -1;
        for (int l = 0; l < k; l++)
        {
            if (cell.state == vertsInStack[l].cell.state)
            {
                sharedIdx = vertsInStack[l].vidx;
                break;
            }
        }

        int idx;
        if (sharedIdx == -1)
        {
            Vector2 planeCoords = row * subdivRhey + col * subdivRhex;
            float y = cell.state == MapCell.State.High ? 1 : 0;
            Vector3 vertex = new Vector3(planeCoords.x, y, planeCoords.y);
            vertices.Add(vertex);
            uvs.Add(new Vector2(vertex.x, vertex.z));
            vertGridCoords.Add(new Vector2Int(col, row));
            idx = nextIdx;
            nextIdx++;
        }
        else
        {
            idx = sharedIdx;
        }

        vertsInStack.Add(new VertInStack() { cell = cell, vidx = idx, coords = cellCoords });
        CellVertInfo cvi = cellVertInfos[(cellCoords.y - cyMin) * cw + (cellCoords.x - cxMin)];
        cvi.AddIdx(row, idx);
    }

    private void AddWallInfo()
    {
        for (int k = 0; k < vertsInStack.Count; k++)
        {
            VertInStack vis = vertsInStack[k];
            CellVertInfo cvi = cellVertInfos[(vis.coords.y - cyMin) * cw + (vis.coords.x - cxMin)];

            if (vis.cell.state == MapCell.State.High)
            {
                for (int l = 0; l < vertsInStack.Count; l++)
                {
                    VertInStack vis2 = vertsInStack[l];
                    if (vis2.cell.state == MapCell.State.Low)
                    {
                        HexXY diff = vis2.coords - vis.coords;
                        int neighIdx = HexXY.DiffToNeighIndex(diff.x, diff.y);
                        cvi.WallIndices[neighIdx].High.Add(vis.vidx);
                        cvi.WallIndices[neighIdx].Low.Add(vis2.vidx);
                        cvi.WallCount++;
                    }
                }
            }
        }
    }

    void CreateVerticesAndAssignToCells(Map map, int mapOffsetX, int mapOffsetY, Func<MapCell, bool> drawCell)
    {
        int nextIdx = 0;
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
                    if (!drawCell(cell))
                        continue;

                    AddNearCellVertices(ref nextIdx, i, j, k, cellCoords, cell);
                }
                
                AddWallInfo();
            }
        }
    }

    public void Generate(Map map, int mapOffsetX, int mapOffsetY, Func<MapCell, bool> drawCell)
    {
        Reset();
        CreateVerticesAndAssignToCells(map, mapOffsetX, mapOffsetY, drawCell);      

        //Fill triangles indices separately for each hex cell
        //using vertIndicesByCell saved data
        for (int i = 0; i < ch; i++)        
            for (int j = 0; j < cw; j++)
                FillTriangleIndices(cellVertInfos[i * cw + j]);

        GenerateWalls();
    }

    private void FillTriangleIndices(CellVertInfo cvi)
    {
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
                    triangles.Add(cvi.Idxs[rowStart + k]);
                    triangles.Add(cvi.Idxs[nextRowStart + k]);
                    triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    triangles.Add(cvi.Idxs[rowStart + k + 1]);
                    triangles.Add(cvi.Idxs[nextRowStart + k]);
                    triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                }
            }
            else if (rowLen < nextRowLen)
            {
                triangles.Add(cvi.Idxs[rowStart]);
                triangles.Add(cvi.Idxs[nextRowStart]);
                triangles.Add(cvi.Idxs[nextRowStart + 1]);

                for (int k = 0; k < rowLen - 1; k++)
                {
                    triangles.Add(cvi.Idxs[rowStart + k]);
                    triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                    triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    triangles.Add(cvi.Idxs[rowStart + k + 1]);
                    triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
                    triangles.Add(cvi.Idxs[nextRowStart + k + 2]);
                }
            }
            else
            {
                for (int k = 0; k < rowLen - 1; k++)
                {
                    triangles.Add(cvi.Idxs[rowStart + k]);
                    triangles.Add(cvi.Idxs[nextRowStart + k]);
                    triangles.Add(cvi.Idxs[rowStart + k + 1]);

                    if (k < rowLen - 2)
                    {
                        triangles.Add(cvi.Idxs[rowStart + k + 1]);
                        triangles.Add(cvi.Idxs[nextRowStart + k]);
                        triangles.Add(cvi.Idxs[nextRowStart + k + 1]);
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

    private void GenerateWalls()
    {        
        int nextIdx = 0;

        int cw = patchSize + 1;
        int ch = 2 * patchSize / 3 + 1;

        for (int i = 0; i < ch; i++)
        {
            for (int j = 0; j < cw; j++)
            {
                CellVertInfo cvi = cellVertInfos[i * cw + j];
                if(cvi.WallCount > 1)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        WallVertIndices wis = cvi.WallIndices[k];

                        for (int l = 0; l < wis.High.Count; l++)
                        {
                            //Verts
                            int highIdx = wis.High[l];
                            int lowIdx = wis.Low[l];
                            Vector3 highVert = vertices[highIdx];
                            Vector3 lowVert = vertices[lowIdx];                           

                            wallVertices.Add(highVert);
                            wallVertices.Add(lowVert);
                         
                            //UVs
                            float u = WallUForGridCoords(vertGridCoords[highIdx]);
                            wallUVs.Add(new Vector2(u, 1));
                            wallUVs.Add(new Vector2(u, 0));
                        }

                        for (int l = 0; l < wis.High.Count - 1; l++)
                        {
                            //Different winding order due to wall vertices
                            //filling order in main mesh generation above
                            if (k >= 1 && k <= 3)
                            {
                                wallTriangles.Add(nextIdx);
                                wallTriangles.Add(nextIdx + 3);
                                wallTriangles.Add(nextIdx + 1);

                                wallTriangles.Add(nextIdx);
                                wallTriangles.Add(nextIdx + 2);
                                wallTriangles.Add(nextIdx + 3);
                            }
                            else
                            {
                                wallTriangles.Add(nextIdx);
                                wallTriangles.Add(nextIdx + 1);
                                wallTriangles.Add(nextIdx + 3);

                                wallTriangles.Add(nextIdx);
                                wallTriangles.Add(nextIdx + 3);
                                wallTriangles.Add(nextIdx + 2);
                            }

                            nextIdx += 2;
                        }

                        nextIdx = wallVertices.Count;
                    }
                }
            }
        }
    }
}

