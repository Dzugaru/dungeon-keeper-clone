using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static Vector3 GetSomeOrthogonal(this Vector3 x)
    {
        if (Mathf.Abs(Vector3.Dot(Vector3.right, x)) < Mathf.Abs(Vector3.Dot(Vector3.up, x)))
            return Vector3.Cross(Vector3.right, x);
        else
            return Vector3.Cross(Vector3.up, x);
    }
}
