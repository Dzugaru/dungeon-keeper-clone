using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static float PerpDot(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public static Vector2 OrthLeft(this Vector2 a)
    {
        return new Vector2(-a.y, a.x);
    }

    public static Vector2 ProjectOnBasis(Vector3 ex, Vector3 ey, Vector3 a)
    {
        return new Vector2(Vector3.Dot(a, ex), Vector3.Dot(a, ey));
    }

    public static Vector3 GetSomeOrthogonal(this Vector3 x)
    {
        if (Mathf.Abs(Vector3.Dot(Vector3.right, x)) < Mathf.Abs(Vector3.Dot(Vector3.up, x)))
            return Vector3.Cross(Vector3.right, x);
        else
            return Vector3.Cross(Vector3.up, x);
    }

    public static Ray? GetIntersection(this Plane a, Plane b)
    {
        Vector3 intersectDir = Vector3.Cross(a.normal, b.normal);
        if (intersectDir.sqrMagnitude < 1e-6)
            return null;

        Vector3 originA = -a.distance * a.normal;
        Vector3 originB = -b.distance * b.normal;

        //Project the problem on a plane perpendicular to both a and b, 
        //it gets much easier this way
        Vector3 planarBasisY = a.normal;
        Vector3 planarBasisX = Vector3.Cross(intersectDir, planarBasisY).normalized;

        Vector2 planarOriginB = ProjectOnBasis(planarBasisX, planarBasisY, originB - originA);
        Vector2 planarNormalB = ProjectOnBasis(planarBasisX, planarBasisY, b.normal);
        Vector2 planarB = planarNormalB.OrthLeft();
        float planarIntersectX = planarOriginB.x - planarOriginB.y * planarB.x / planarB.y;

        Vector3 intersectPos = originA + planarBasisX * planarIntersectX;

        return new Ray(intersectPos, intersectDir);
    }
}
