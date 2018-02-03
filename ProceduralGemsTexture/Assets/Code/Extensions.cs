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

    public static Vector2 ProjectOnPlanarBasis(Vector3 ex, Vector3 ey, Vector3 a)
    {
        return new Vector2(Vector3.Dot(a, ex), Vector3.Dot(a, ey));
    }

    public static Vector2 GetIntersection(this Ray2D a, Ray2D b)
    {        
        float t = PerpDot(b.origin - a.origin, b.direction) / PerpDot(a.direction, b.direction);        
        return a.origin + t * a.direction;
    }

    public static Vector2 GetNearestPointOnRay(this Ray2D a, Vector2 b)
    {
        float dist = Vector2.Dot(a.direction, b - a.origin);
        return a.origin + dist * a.direction;
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

        Vector2 planarOriginB = ProjectOnPlanarBasis(planarBasisX, planarBasisY, originB - originA);
        Vector2 planarNormalB = ProjectOnPlanarBasis(planarBasisX, planarBasisY, b.normal);
        Vector2 planarB = planarNormalB.OrthLeft();
        float planarIntersectX = planarOriginB.x - planarOriginB.y * planarB.x / planarB.y;

        Vector3 intersectPos = originA + planarBasisX * planarIntersectX;

        return new Ray(intersectPos, intersectDir);
    }
}
