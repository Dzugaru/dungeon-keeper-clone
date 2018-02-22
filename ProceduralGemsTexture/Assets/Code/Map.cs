using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class Map : ScriptableObject
{   
    public int size;    

    [SerializeField]
    MapCell[] cells;

    [SerializeField]
    public MapCell externalCell;

    public MapCell GetCell(int x, int y)
    {
        if (y < 0 || y >= size || x < 0 || x >= size)
            return externalCell;
        else
            return cells[y * size + x];
    }    

    public MapCell GetCell(HexXY c)
    {
        return GetCell(c.x, c.y);
    }

    private void OnEnable()
    {
        
    }   

    public void New(int radius)
    {
        this.size = 2 * radius + 1;        

        HexXY center = new HexXY(size, size);

        externalCell = new MapCell();        
        externalCell.type = MapCell.CellType.Stone;

        cells = new MapCell[size * size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                if (HexXY.Dist(new HexXY(j, i), center) > size) 
                    cells[i * size + j] = externalCell; //shape map like a big hexagon
                else
                    cells[i * size + j] = new MapCell();
            }
    }

    public IEnumerable<MapCellAndCoords> AllCells()
    {
        HexXY center = new HexXY(size, size);
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                if (HexXY.Dist(new HexXY(j, i), center) <= size)
                    yield return new MapCellAndCoords(cells[i * size + j], new HexXY(j, i));
    }
}

