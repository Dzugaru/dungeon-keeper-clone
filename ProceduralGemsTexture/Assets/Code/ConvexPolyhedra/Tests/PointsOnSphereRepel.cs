using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace ConvexPolyhedra.Tests
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class PointsOnSphereRepel : MonoBehaviour
    {
        public int seed = 1;
        public int nPoints = 20;
        public int nIter = 1000;
        public float fps = 4;
        public float stepAngle = 0.01f;
        public int nearest = 3;
        public float stepReduction = 1f;

        public KeyCode testRepelKey = KeyCode.Z;
        public KeyCode testEdgesKey = KeyCode.X;
        public KeyCode testMesh = KeyCode.C;

        Generator gen = new Generator();
        Vector3[] points;
        List<Transform> markers = new List<Transform>();

        List<Generator.Edge> edges = new List<Generator.Edge>();

        List<List<Vector3>> polys;

        void Start()
        {
                
        }

        IEnumerator Repel()
        {
            UnityEngine.Random.InitState(seed);
            points = gen.GeneratePointsOnSphere(nPoints);

           
            foreach (Vector3 p in points)
            {
                Transform t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                t.GetComponent<MeshRenderer>().sharedMaterial.color = Color.green;
                t.parent = transform;
                t.localPosition = p;

                markers.Add(t);
                t.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }

            float currentStep = stepAngle;

            for (int k = 0; k < nIter; k++)
            {
                points = gen.RepelPointsOnSphere(points, nearest, Generator.InverseLinearRepel, currentStep);
                currentStep *= stepReduction;
                for (int i = 0; i < nPoints; i++)
                {
                    markers[i].localPosition = points[i];
                }

                yield return new WaitForSeconds(1f / fps);
            }
        }

        void GenerateEdges()
        {
            UnityEngine.Random.InitState(seed);
            points = gen.GeneratePointsOnSphere(nPoints);
            points = gen.FindRelaxedConfigurationOfPointsOnSphere(points, nearest, Generator.InverseLinearRepel, stepAngle, stepReduction, nIter);

            polys = gen.GetPlaneCutPolygons(points);

            //edges = gen.GetConvexHullEdges(points);

            foreach (Vector3 p in points)
            {
                Transform t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                t.GetComponent<MeshRenderer>().sharedMaterial.color = Color.green;
                t.parent = transform;
                t.localPosition = p;

                markers.Add(t);
                t.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }

        void GenerateMesh()
        {
            UnityEngine.Random.InitState(seed);
            points = gen.GeneratePointsOnSphere(nPoints);
            points = gen.FindRelaxedConfigurationOfPointsOnSphere(points, nearest, Generator.InverseLinearRepel, stepAngle, stepReduction, nIter);

            List<Vector3> vertices = new List<Vector3>();
            List<int> tris = new List<int>();
            //gen.GenerateConvexHullTriangles(points, vertices, tris);
            gen.GeneratePolyTriangles(points, vertices, tris);

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().sharedMesh = mesh;

            //AssetDatabase.CreateAsset(mesh, "Assets/GemMesh.asset");
            //AssetDatabase.SaveAssets();
        }

        void Update()
        {
            if (Input.GetKeyDown(testRepelKey))
                StartCoroutine(Repel());

            if (Input.GetKeyDown(testEdgesKey))
                GenerateEdges();

            if (Input.GetKeyDown(testMesh))
                GenerateMesh();

        }

        void OnDrawGizmos()
        {
            //if (edges.Count == 0)
            //{
            //    Gizmos.color = Color.yellow;
            //    Gizmos.DrawWireSphere(Vector3.zero, 10);
            //}

            if (polys != null)
            {
                Gizmos.color = Color.red;
                foreach(List<Vector3> poly in polys)
                {
                    for (int i = 0; i < poly.Count; i++)
                    {
                        Gizmos.DrawLine(poly[i] * 10f, poly[(i + 1) % poly.Count] * 10f);
                    }
                }
            }

            Gizmos.color = Color.red;
            foreach (Generator.Edge e in edges)
            {
                Gizmos.DrawLine(points[e.A] * 10f, points[e.B] * 10f);
            }
        }
    }
}


