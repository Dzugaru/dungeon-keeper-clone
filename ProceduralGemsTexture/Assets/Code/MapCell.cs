using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class MapCell
{
    public enum State
    {        
        Full,
        Excavated
    }

    public enum CellType
    {
        Earth,
        Stone       
    }

    public bool immutable;
    public State state;
    public CellType type;
}

public struct MapCellAndCoords
{
    public MapCell Cell;
    public HexXY Coords;

    public MapCellAndCoords(MapCell cell, HexXY coords)
    {
        this.Cell = cell;
        this.Coords = coords;
    }
}

