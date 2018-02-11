using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapCell : ScriptableObject
{
    public enum State
    {
        High,
        Low
    }

    public State state;

    public static bool CanHaveSharedVertices(MapCell a, MapCell b)
    {
        return a.state == b.state;
    }
}

