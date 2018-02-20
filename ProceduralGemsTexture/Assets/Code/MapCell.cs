using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapCell : ScriptableObject
{
    public enum State
    {
        Low,
        High
    }

    public State state;   
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

