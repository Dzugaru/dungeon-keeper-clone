using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Serializable]
public struct MinMaxRangeFloat
{
    public float Min, Max;
    public MinMaxRangeFloat(float min, float max)
    {
        this.Min = min;
        this.Max = max;
    }
}

