using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Map : ScriptableObject
{
    [SerializeField]
    int x0, y0, w, h;

    [SerializeField]
    MapCell[] cells;

    public MapCell GetCell(int x, int y)
    {
        return cells[(y - y0) * w + (x - x0)];
    }    

    public void Init(int x0, int y0, int w, int h)
    {
        this.x0 = x0;
        this.y0 = y0;
        this.w = w;
        this.h = h;

        this.cells = new MapCell[w * h];

        for (int i = 0; i < h; i++)        
            for (int j = 0; j < w; j++)             
                this.cells[i * w + j] = CreateInstance<MapCell>();
    }
}

