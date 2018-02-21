using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class Map : ScriptableObject
{
    int x0, y0, w;

    [SerializeField]
    int size;

    [SerializeField]
    MapCell[] cells;

    [SerializeField]
    MapCell externalCell;

    public MapCell GetCell(int x, int y)
    {
        if (y < y0 || y >= y0 + w || x < x0 || x >= x0 + w)
            return externalCell;
        else
            return cells[(y - y0) * w + (x - x0)];
    }    

    public MapCell GetCell(HexXY c)
    {
        return GetCell(c.x, c.y);
    }

    private void OnEnable()
    {
        InitSizes();
    }

    void InitSizes()
    {
        x0 = -size;
        y0 = -size;
        w = 2 * size + 1;
    }

    public void New(int size)
    {
        this.size = size;

        InitSizes();

        HexXY center = new HexXY(size, size);

        externalCell = new MapCell();
        externalCell.immutable = true;
        externalCell.type = MapCell.CellType.Stone;

        cells = new MapCell[w * w];
        for (int i = 0; i < w; i++)
            for (int j = 0; j < w; j++)
            {
                if (HexXY.Dist(new HexXY(j, i), center) > size) 
                    cells[i * w + j] = externalCell; //shape map like a big hexagon
                else
                    cells[i * w + j] = new MapCell();
            }
    }
}

