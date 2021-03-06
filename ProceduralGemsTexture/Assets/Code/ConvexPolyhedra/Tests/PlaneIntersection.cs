﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace ConvexPolyhedra.Tests
{
    [ExecuteInEditMode]
    class PlaneIntersection : MonoBehaviour
    {
        public Transform pa, pb;

        void Start()
        {
            //Ray2D a = new Ray2D(new Vector2(2, 0), new Vector2(-1, 1));
            //Ray2D b = new Ray2D(new Vector2(3, 4), new Vector2(0, -1));
            //Debug.Log(a.GetIntersection(b));
        }

        void OnDrawGizmos()
        {
            if(pa != null && pb != null)
            {
                Plane a = new Plane(pa.localRotation * Vector3.up, pa.localPosition);
                Plane b = new Plane(pb.localRotation * Vector3.up, pb.localPosition);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(-a.normal * a.distance, -a.normal * a.distance + a.normal * 5f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(-b.normal * b.distance, -b.normal * b.distance + b.normal * 5f);

                Ray? inters = a.GetIntersection(b);

                if (inters.HasValue)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(inters.Value.origin, inters.Value.origin + inters.Value.direction * 10f);
                    Gizmos.DrawLine(inters.Value.origin, inters.Value.origin - inters.Value.direction * 10f);
                }
            }
        }
    }
}
