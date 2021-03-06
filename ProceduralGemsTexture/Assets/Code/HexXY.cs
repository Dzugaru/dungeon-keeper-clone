﻿using System;
using UnityEngine;


[Serializable]
public struct HexXY : IEquatable<HexXY>
{
    //Basis on a plane
    public static readonly Vector2 ex = new Vector2(Mathf.Sqrt(3f) * 0.5f, 0.5f);
    public static readonly Vector2 ey = new Vector2(-Mathf.Sqrt(3f) * 0.5f, 0.5f);

    //Basis of plane in hex space
    public static Vector2 iex = new Vector2(1f / Mathf.Sqrt(3), -1f / Mathf.Sqrt(3));
    public static Vector2 iey = new Vector2(1, 1);

    public static readonly HexXY[] neighbours = new[] { new HexXY(1, 1), new HexXY(1, 0), new HexXY(0, -1), new HexXY(-1, -1), new HexXY(-1, 0), new HexXY(0, 1) };

    public int x, y;

    public HexXY(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static int DiffToNeighIndex(int dx, int dy)
    {
        if(dx == 0)
            return dy == 1 ? 5 : 2;
        if (dx == 1)
            return dy == 1 ? 0 : 1;
        return dy == -1 ? 3 : 4;        
    }

    public static uint Dist(HexXY a)
    {
        if ((a.x < 0) == (a.y < 0))
            return (uint)Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y));
        else
            return (uint)Mathf.Abs(a.x - a.y);
    }

    public static uint Dist(HexXY a, HexXY b)
    {
        return Dist(new HexXY(a.x - b.x, a.y - b.y));
    }

    public Vector2 ToPlaneCoordinates()
    {
        return x * ex + y * ey;
    }

    public static HexXY FromPlaneCoordinates(Vector2 coords)
    {
        //this just searches for the nearest center of 4 possible hex cells 
        //inside a basis (ex, ey) "square" (a rhombus) of [i,j,i+1,j+1]
        float x = coords.x * iex.x + coords.y * iey.x;
        float y = coords.x * iex.y + coords.y * iey.y;
        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        Vector2 de = new Vector2(x - ix, y - iy);
        Vector2 d = de.x * ex + de.y * ey;
        float d00 = d.sqrMagnitude;
        float d10 = (ex - d).sqrMagnitude;
        float d01 = (ey - d).sqrMagnitude;
        float d11 = (ex + ey - d).sqrMagnitude;
        float mind = d00; int mini = 0;
        if (d10 < mind) { mind = d10; mini = 1; }
        if (d01 < mind) { mind = d01; mini = 2; }
        if (d11 < mind) { mind = d11; mini = 3; }
        switch (mini)
        {
            case 0: return new HexXY(ix, iy);
            case 1: return new HexXY(ix + 1, iy);
            case 2: return new HexXY(ix, iy + 1);
            case 3: return new HexXY(ix + 1, iy + 1);
            default: throw new System.InvalidProgramException();
        }
    }

    public static HexXY operator +(HexXY lhs, HexXY rhs)
    {
        return new HexXY(lhs.x + rhs.x, lhs.y + rhs.y);
    }

    public static HexXY operator -(HexXY lhs, HexXY rhs)
    {
        return new HexXY(lhs.x - rhs.x, lhs.y - rhs.y);
    }

    public bool Equals(HexXY other)
    {
        return this == other;
    }

    public override bool Equals(object obj)
    {
        HexXY tobj = (HexXY)obj;
        return tobj == this;
    }

    public override int GetHashCode()
    {
        return (x << 16) + y;
    }

    public static bool operator ==(HexXY lhs, HexXY rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y;
    }

    public static bool operator !=(HexXY lhs, HexXY rhs)
    {
        return !(lhs == rhs);
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", x, y);
    }
}

