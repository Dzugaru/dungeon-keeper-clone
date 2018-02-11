//***************************************
//http://catlikecoding.com/unity/tutorials/noise-derivatives/
//***************************************

using UnityEngine;

public struct PerlinNoiseSample
{

    public float value;
    public Vector3 derivative;

    public static PerlinNoiseSample operator +(PerlinNoiseSample a, float b)
    {
        a.value += b;
        return a;
    }

    public static PerlinNoiseSample operator +(float a, PerlinNoiseSample b)
    {
        b.value += a;
        return b;
    }

    public static PerlinNoiseSample operator +(PerlinNoiseSample a, PerlinNoiseSample b)
    {
        a.value += b.value;
        a.derivative += b.derivative;
        return a;
    }

    public static PerlinNoiseSample operator -(PerlinNoiseSample a, float b)
    {
        a.value -= b;
        return a;
    }

    public static PerlinNoiseSample operator -(float a, PerlinNoiseSample b)
    {
        b.value = a - b.value;
        b.derivative = -b.derivative;
        return b;
    }

    public static PerlinNoiseSample operator -(PerlinNoiseSample a, PerlinNoiseSample b)
    {
        a.value -= b.value;
        a.derivative -= b.derivative;
        return a;
    }

    public static PerlinNoiseSample operator *(PerlinNoiseSample a, float b)
    {
        a.value *= b;
        a.derivative *= b;
        return a;
    }

    public static PerlinNoiseSample operator *(float a, PerlinNoiseSample b)
    {
        b.value *= a;
        b.derivative *= a;
        return b;
    }

    public static PerlinNoiseSample operator *(PerlinNoiseSample a, PerlinNoiseSample b)
    {
        a.derivative = a.derivative * b.value + b.derivative * a.value;
        a.value *= b.value;
        return a;
    }
}