using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using RNG = UnityEngine.Random;

namespace ConvexPolyhedra
{
    public class Generator
    {
        struct NeighAndDist
        {
            public Vector3 Neigh;
            public float Dist;

            public NeighAndDist(Vector3 neigh, float dist)
            {
                this.Neigh = neigh;
                this.Dist = dist;
            }
        }

        public struct Edge : IEquatable<Edge>
        {
            public int A, B;

            public Edge(int a, int b)
            {
                this.A = a;
                this.B = b;
            }            

            public bool Equals(Edge other)
            {
                return (other.A == A && other.B == B) ||
                       (other.A == B && other.B == A);
            }

            public override int GetHashCode()
            {
                return (A << 16) + B;
            }

            public override bool Equals(object obj)
            {                
                return Equals((Edge)obj);
            }
        }

        void Start()
        {
            
        }

        public Vector3[] GeneratePointsOnSphere(int n)
        {
            Vector3[] points = new Vector3[n];
            for (int i = 0; i < n; i++)            
                points[i] = RNG.onUnitSphere;
            return points;            
        }

        float DistanceBetweenPointsOnSphere(Vector3 a, Vector3 b)
        {
            bool lessThan90 = Vector3.Dot(a, b) > 0;
            float asin = Mathf.Asin(Mathf.Clamp01(Vector3.Cross(a, b).magnitude));
            return lessThan90 ? asin : Mathf.PI - asin;
        }

        public static float InverseQuadraticRepel(float dist)
        {
            float stableDist = dist + 0.01f;
            return 1 / (stableDist * stableDist);
        }

        public static float InverseLinearRepel(float dist)
        {
            float stableDist = dist + 0.01f;
            return 1 / stableDist;
        }

        Quaternion RepelRotationForPointOnSphere(Vector3 a, Vector3 b, float angle)
        {
            Vector3 axis = Vector3.Cross(a, b);
            if (axis.sqrMagnitude < 1e-6)
                return Quaternion.identity;
            else
                return Quaternion.AngleAxis(-Mathf.Rad2Deg * angle, axis);            
        }

        public Vector3[] FindRelaxedConfigurationOfPointsOnSphere(Vector3[] start, int nNearestNeigh, Func<float, float> repelFunc, float stepAngle, float stepReduction, int nIters)
        {
            Vector3[] points = start;
            float currStep = stepAngle;

            for (int k = 0; k < nIters; k++)
            {
                points = RepelPointsOnSphere(points, nNearestNeigh, repelFunc, currStep);
                currStep *= stepReduction;
            }

            return points;
        }

        public Vector3[] RepelPointsOnSphere(Vector3[] points, int nNearestNeigh, Func<float, float> repelFunc, float stepAngle)
        {
            Vector3[] newPositions = new Vector3[points.Length]; 
            
            for(int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];
                NeighAndDist[] nearestNeighs = points.Where(x => x != p)
                                                        .Select(x => new NeighAndDist(x, DistanceBetweenPointsOnSphere(x, p)))
                                                        .OrderBy(x => x.Dist)
                                                        .Take(nNearestNeigh)
                                                        .ToArray();                

                Quaternion rotation = Quaternion.identity;
                foreach(NeighAndDist nd in nearestNeighs)
                {
                    float repelAngle = repelFunc(nd.Dist) * stepAngle;
                    Quaternion repelRot = RepelRotationForPointOnSphere(p, nd.Neigh, repelAngle);
                    rotation *= repelRot;
                }

                float finalAngle;
                Vector3 finalAxis;
                rotation.ToAngleAxis(out finalAngle, out finalAxis);
                rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * stepAngle * Mathf.Sign(finalAngle), finalAxis);

                newPositions[i] = (rotation * p).normalized;
            }                

            return newPositions;
        }   
        
        bool IsOuterEdge(Vector3 a, Vector3 b, Vector3[] allPoints)
        {
            //Project points into plane orthogonal to the edge
            //and check if all projected points fall into some half-circle
            Vector3 edge = (b - a).normalized;
            Vector3 planeXAxis = edge.GetSomeOrthogonal();
            Vector3 planeYAxis = Vector3.Cross(edge, planeXAxis);

            List<float> angles = new List<float>();
            for (int i = 0; i < allPoints.Length; i++)
            {
                Vector3 pp = allPoints[i] - a;
                float x = Vector3.Dot(pp, planeXAxis);
                float y = Vector3.Dot(pp, planeYAxis);
                if (Mathf.Abs(x) < 1e-5 && Mathf.Abs(y) < 1e-5)
                    continue;
                
                angles.Add(Mathf.Atan2(y, x));
            }

            //Find if atan2 [-180,180] angles fall into half-circle,
            //it's tricky but we need only 4 angles for this
            List<float> negativeAngles = angles.Where(x => x < 0).ToList();
            List<float> positiveAngles = angles.Where(x => x >= 0).ToList();
            bool isInHalfCircle;
            if (negativeAngles.Count == 0 || positiveAngles.Count == 0)
            {
                isInHalfCircle = true;
            }
            else
            {
                float negMin = negativeAngles.Min();
                float negMax = negativeAngles.Max();
                float posMin = positiveAngles.Min();
                float posMax = positiveAngles.Max();

                isInHalfCircle = negMin + Math.PI >= posMax ||
                                 posMin - Math.PI >= negMax;
            }

            return isInHalfCircle;
        }

        public List<Edge> GetConvexHullEdges(Vector3[] points)
        {
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < points.Length; i++)
                for (int j = i + 1; j < points.Length; j++)
                    if(IsOuterEdge(points[i], points[j], points))                
                        edges.Add(new Edge(i, j));

            return edges;               
        }

        public void GenerateConvexHullTriangles(Vector3[] points, List<Vector3> vertices, List<int> triangles)
        {
            List<Edge> edges = GetConvexHullEdges(points);
            List<HashSet<Vector3>> addedTriangles = new List<HashSet<Vector3>>();

            for(int k = 0; k < edges.Count; k++)
            {
                Edge edge = edges[k];
                for (int i = 0; i < points.Length; i++)
                {
                    if (i == edge.A || i == edge.B)
                        continue;                   

                    if (edges.Contains(new Edge(edge.A, i)) &&
                       edges.Contains(new Edge(edge.B, i)))
                    {
                        Vector3 v0 = points[i];
                        Vector3 v1 = points[edge.A];
                        Vector3 v2 = points[edge.B];                        

                        HashSet<Vector3> triSet = new HashSet<Vector3>();
                        triSet.Add(v0);
                        triSet.Add(v1);
                        triSet.Add(v2);

                        if (addedTriangles.Any(x => x.SetEquals(triSet)))
                            continue;

                        addedTriangles.Add(triSet);

                        //Correct orientation
                        if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), v0) < 0)
                        {
                            Vector3 tmp = v2;
                            v2 = v1;
                            v1 = tmp;
                        }

                        int vidx = vertices.Count;
                        vertices.Add(v0);
                        vertices.Add(v1);
                        vertices.Add(v2);
                        triangles.Add(vidx);
                        triangles.Add(vidx + 1);
                        triangles.Add(vidx + 2);
                    }                    
                }
            }            
        }        

        //Each point defines a plane, 
        //we get polyhedra enclosed by all such planes
        public List<List<Vector3>> GetPlaneCutPolygons(Vector3[] points)
        {
            List<List<Vector3>> polys = new List<List<Vector3>>();

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];                           

                Plane plane = new Plane(p.normalized, p);

                //Project everything on a point-based plane
                //and find 2d rays of intersection with other planes
                Vector3 planeBasisX = p.GetSomeOrthogonal().normalized;
                Vector3 planeBasisY = Vector3.Cross(p, p.GetSomeOrthogonal()).normalized;
                
                List<Ray2D> intersWithOther = new List<Ray2D>();
                for (int j = 0; j < points.Length; j++)
                {
                    if (i == j)
                        continue;

                    Plane other = new Plane(points[j].normalized, points[j]);
                    Ray? possibleInter = plane.GetIntersection(other);
                    if (possibleInter == null)
                        continue;
                    Ray inter = possibleInter.Value;

                    Ray2D interInPlane = new Ray2D(
                            Extensions.ProjectOnPlanarBasis(planeBasisX, planeBasisY,  inter.origin - p),
                            Extensions.ProjectOnPlanarBasis(planeBasisX, planeBasisY, inter.direction)
                        );

                    //Set winding dir
                    if (Extensions.PerpDot(interInPlane.direction, interInPlane.origin) > 0)
                        interInPlane.direction = -interInPlane.direction;

                    intersWithOther.Add(interInPlane);
                }              

                //Find nearest ray
                float smallestSqrDist = float.MaxValue;
                Vector2 nearestPoint = Vector2.zero;
                int nearestRayIndex = -1;

                for (int j = 0; j < intersWithOther.Count; j++)
                {
                    Ray2D r = intersWithOther[j];
                    Vector2 candidateNearestPoint = r.GetNearestPointOnRay(Vector2.zero);
                    if (candidateNearestPoint.sqrMagnitude < smallestSqrDist)
                    {
                        smallestSqrDist = candidateNearestPoint.sqrMagnitude;
                        nearestPoint = candidateNearestPoint;
                        nearestRayIndex = j;
                    }
                }               

                List<Vector2> polyVertices = new List<Vector2>();
              
                //Go clockwise searching nearest intersections between current ray and others
                int startRayIndex = nearestRayIndex;
                int rayIndex = startRayIndex;
                int prevRayIndex = rayIndex;
                Vector2 position = nearestPoint;
               
                do
                {
                    Ray2D ray = intersWithOther[rayIndex];
                    smallestSqrDist = float.MaxValue;
                    Vector2 nearestNextPosition = Vector2.zero;

                    for (int j = 0; j < intersWithOther.Count; j++)
                    {
                        Ray2D otherRay = intersWithOther[j];                       
                        if (j == rayIndex || j == prevRayIndex)
                            continue;
                        
                        Vector2 inters = ray.GetIntersection(otherRay);                        
                        if ((inters - position).sqrMagnitude < smallestSqrDist && Vector2.Dot(ray.direction, inters - position) > 0)
                        {
                            smallestSqrDist = (inters - position).sqrMagnitude;
                            nearestNextPosition = inters;
                            nearestRayIndex = j;
                        }
                    }

                    position = nearestNextPosition;
                    polyVertices.Add(position);
                    prevRayIndex = rayIndex;
                    rayIndex = nearestRayIndex;
                } while (rayIndex != startRayIndex);

                List<Vector3> polyVertices3D = new List<Vector3>();
                foreach (Vector2 v in polyVertices)
                    polyVertices3D.Add(p + v.x * planeBasisX + v.y * planeBasisY);

                polys.Add(polyVertices3D);
            }

            return polys;
        }

        public void GeneratePolyTriangles(Vector3[] points, List<Vector3> vertices, List<int> triangles)
        {
            List<List<Vector3>> polys = GetPlaneCutPolygons(points);

            foreach (List<Vector3> poly in polys)
            {
                int vidx = vertices.Count;
                vertices.AddRange(poly);

                for (int i = 1; i < poly.Count - 1; i++)
                {
                    triangles.Add(vidx);
                    triangles.Add(vidx + i);
                    triangles.Add(vidx + i + 1);
                }
            }
        }
    }
}
