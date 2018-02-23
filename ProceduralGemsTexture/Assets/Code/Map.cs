using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class Map : ScriptableObject
{ 
    public int size, radius;    

    [SerializeField]
    MapCell[] cells;
    
    public MapCell externalCell;

    [SerializeField]
    HexXY center;

    public MapCell GetCell(int x, int y)
    {
        if (HexXY.Dist(new HexXY(x, y), center) > radius)
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
        this.radius = radius;
        this.size = 2 * radius + 1;
        this.center = new HexXY(radius, radius);

        externalCell = new MapCell();        
        externalCell.type = MapCell.CellType.Stone;

        //TODO: temporary, when drawing in editor
        externalCell.state = MapCell.State.Excavated; 

        cells = new MapCell[size * size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)              
                cells[i * size + j] = new MapCell();            
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

