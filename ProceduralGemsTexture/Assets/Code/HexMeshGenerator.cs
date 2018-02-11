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

    sealed class CellVertIndices
    { 
        public int LastRowIdx = -1;
        public List<int> RowStarts = new List<int>();
        public List<int> Idxs = new List<int>();

        public void AddIdx(int row, int idx)
        {
            if(row != LastRowIdx)
            {
                LastRowIdx = row;
                RowStarts.Add(Idxs.Count);
            }
            Idxs.Add(idx);
        }
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
    CellVertIndices[] vertIndicesByCell;
    public List<Vector3> vertices;
    public List<int> triangles;

    public HexMeshGenerator(int patchSize, int subDivsShift)
    {
        Debug.Assert((patchSize % 3) == 0);

        this.patchSize = patchSize;
        this.subDivsShift = subDivsShift;
        this.subDivs = 1 << subDivsShift;
        this.subDivsMask = subDivs - 1;        
        
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

    public void Generate(Map map, int cx0, int cy0)
    {
        int cxMin = cx0;
        int cxMax = cx0 + patchSize + 1;
        int cyMin = cy0 - patchSize / 3;
        int cyMax = cy0 + patchSize / 3 + 1;

        int cw = cxMax - cxMin;
        int ch = cyMax - cyMin;

        vertIndicesByCell = new CellVertIndices[cw * ch];
        for (int i = 0; i < cw * ch; i++)        
            vertIndicesByCell[i] = new CellVertIndices();
        
        vertices = new List<Vector3>();
        triangles = new List<int>();

        Vector2 subdivRhex = rhex / subDivs;
        Vector2 subdivRhey = rhey / subDivs;

        List<MapCell> nonSharedVertCells = new List<MapCell>();

        //Create vertices and assign to cells
        int vidx = 0;
        for (int i = 0; i < patchSize * subDivs + 1; i++)
        {
            for (int j = 0; j < patchSize * subDivs + 1; j++)
            {
                Vector2 planeCoords = i * subdivRhey + j * subdivRhex;
                NearCells nc = GetNearCells(j, i);

                //keep a list of cells containing current vertex 
                //for sharing optimization
                nonSharedVertCells.Clear(); 
                for (int k = 0; k < nc.Count; k++)
                {                    
                    HexXY cellCoords = nc.GetByIndex(k);
                    MapCell cell = map.GetCell(cellCoords.x, cellCoords.y);
                    
                    int sharedOffset = -1;
                    for (int l = 0; l < nonSharedVertCells.Count; l++)
                        if (MapCell.CanHaveSharedVertices(cell, nonSharedVertCells[l]))
                        {
                            sharedOffset = nonSharedVertCells.Count - 1 - l;
                            break;
                        }

                    int finalVIdx;
                    if(sharedOffset == -1)
                    {
                        float y = cell.state == MapCell.State.High ? 1 : 0;
                        Vector3 vertex = new Vector3(planeCoords.x, y, planeCoords.y);
                        vertices.Add(vertex);
                        finalVIdx = vidx;
                        vidx++;
                        nonSharedVertCells.Add(cell);
                    }
                    else
                    {
                        finalVIdx = vidx - sharedOffset - 1;
                    }                   
                    
                    CellVertIndices cvi = vertIndicesByCell[(cellCoords.y - cyMin) * cw + (cellCoords.x - cxMin)];
                    cvi.AddIdx(i, finalVIdx);                    
                }
            }
        }

        for (int i = 0; i < ch; i++)
        {
            for (int j = 0; j < cw; j++)
            {
                CellVertIndices cvi = vertIndicesByCell[i * cw + j];
                
                for (int r = 0; r < cvi.RowStarts.Count - 1; r++)
                {
                    int rowStart = cvi.RowStarts[r];
                    int nextRowStart = cvi.RowStarts[r + 1];
                    int afterNextRowStart = r == cvi.RowStarts.Count - 2 ? cvi.Idxs.Count : cvi.RowStarts[r + 2];

                    int rowLen = nextRowStart - rowStart;
                    int nextRowLen = afterNextRowStart - nextRowStart;

                    //TODO: simplify cases?
                    if(rowLen == nextRowLen) //this rhombus part always leans to the right
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
                    else if(rowLen < nextRowLen)
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
        }
    }    
}

