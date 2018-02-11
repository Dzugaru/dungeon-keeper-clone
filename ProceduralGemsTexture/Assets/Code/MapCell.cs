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

