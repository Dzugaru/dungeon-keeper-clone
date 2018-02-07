using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using System.IO;

public class GemsInStone : EditorWindow
{
    struct Range
    {
        public int Min, Max;
        public Range(int min, int max)
        {
            this.Min = min;
            this.Max = max;
        }
    }

    int maxNumGems = 10;
    string meshesFolder = "Resources/GemMeshes";
    float diskR = 0.05f;
    float yShiftPercent = 0.2f;
    float planeY = 0;
    MinMaxRangeFloat size = new MinMaxRangeFloat(0.02f, 0.02f);
    Material gemMaterial;

    [MenuItem("Window/Gems in stone")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GemsInStone));
    }

    void Generate()
    {           
        Transform gems = new GameObject("Gems").transform;
        string[] gemMeshesPaths = Directory.GetFiles(Application.dataPath + "/" + meshesFolder, "*.asset")
                                           .Select(x => Path.GetFileName(x))
                                           .ToArray();

        Mesh[] gemMeshes = gemMeshesPaths.Select(x => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/" + meshesFolder + "/" + x)).ToArray();
        List<Vector2> positions = PoissonDiskInHexCellWithTilingSampler.Sample(diskR, maxNumGems);

        foreach(Vector2 pos in positions)
        {
            float exactSize = Random.Range(size.Min, size.Max);
            float exactYShift = Random.Range(-exactSize * yShiftPercent, exactSize * yShiftPercent);
            Mesh mesh = gemMeshes[Random.Range(0, gemMeshes.Length - 1)];

            GameObject gem = new GameObject("Gem");
            gem.transform.parent = gems;
            gem.transform.localRotation = Random.rotationUniform;
            gem.transform.localScale = new Vector3(exactSize, exactSize, exactSize);
            MeshFilter meshFilter = gem.AddComponent<MeshFilter>(); 
            MeshRenderer meshRenderer = gem.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            gem.transform.localPosition = new Vector3(pos.x, exactYShift, pos.y);

            meshRenderer.sharedMaterial = gemMaterial;
        }
    }

    void MakeFloatRangeField(string label, ref MinMaxRangeFloat range)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        range.Min = EditorGUILayout.FloatField(range.Min);
        range.Max = EditorGUILayout.FloatField(range.Max);
        GUILayout.EndHorizontal();
    }

    void OnGUI()
    {
        maxNumGems = EditorGUILayout.IntField("Num gems", maxNumGems);
        diskR = EditorGUILayout.FloatField("Gems min distance", diskR);
        planeY = EditorGUILayout.FloatField("Plane Y", planeY);
        yShiftPercent = EditorGUILayout.FloatField("Random Y shift", yShiftPercent);
        MakeFloatRangeField("Gems size", ref size);
        gemMaterial = (Material)EditorGUILayout.ObjectField("Gem material", gemMaterial, typeof(Material), false);

        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
    }

    //Samples a bunch of points no closer than 'diskR' to each other
    //inside a single hex cell, and takes care so that hex cell can be tiled
    static class PoissonDiskInHexCellWithTilingSampler
    {
        static readonly float limitX = 1f / Mathf.Sqrt(3);
        static readonly float limitY = 0.5f;

        static Vector2 SampleInsideHex()
        {            
            for(; ; )
            {
                Vector2 sample = new Vector2(Random.Range(-limitX, limitX), Random.Range(-limitY, limitY));
                if (HexXY.FromPlaneCoordinates(sample) == new HexXY(0, 0))
                    return sample;
            }            
        }

        static Vector2[] HexMirror(Vector2 v)
        {
            return new[]
            {
                v + HexXY.ex,
                v - HexXY.ex,
                v + HexXY.ey,
                v - HexXY.ey,
                v + HexXY.ex + HexXY.ey,
                v - HexXY.ex - HexXY.ey
            };           
        }

        public static List<Vector2> Sample(float diskR, int maxN)
        {
            const int maxNumTries = 100;
            float sqrDiskR = diskR * diskR;

            List<Vector2> mirroredSamples = new List<Vector2>();
            List<Vector2> samples = new List<Vector2>();
            for (int i = 0; i < maxN; i++)
            {
                Vector2 candidateSample = Vector2.zero;
                int numTries = 0;
                for (; numTries < maxNumTries; numTries++)
                {
                    candidateSample = SampleInsideHex();
                    if (samples.Concat(mirroredSamples).All(s => (s - candidateSample).sqrMagnitude >= sqrDiskR))
                        break;
                }

                if (numTries == maxNumTries)
                    break;
                else
                {
                    samples.Add(candidateSample);
                    mirroredSamples.AddRange(HexMirror(candidateSample));
                }
            }

            return samples;
        }
    }    
}
