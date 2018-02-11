using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using System.IO;
using System;

public class GemsInStone : EditorWindow
{
    const string settingsAssetPath = "Assets/GemsInStoneSettings.asset";
    
    //NOTE: ScriptableObject classes serialized as asset work only if defined in their own files...
    GemsInStoneSettings settingsHex, settingsWall;
    GemsInStoneSettings settings { get { return mode == Mode.Hex ? settingsHex : settingsWall; } }

    List<GameObject> currentGemsObjs;

    public enum Mode
    {
        Hex,
        Wall
    }

    public Mode mode;

    [MenuItem("Window/Gems in stone")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GemsInStone));
    }

    void OnEnable()
    {
        UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath(settingsAssetPath);
        if (settings == null || settings.Length == 0)
        {
            settingsHex = CreateInstance<GemsInStoneSettings>();
            settingsWall = CreateInstance<GemsInStoneSettings>();
            AssetDatabase.CreateAsset(settingsHex, settingsAssetPath);
            AssetDatabase.AddObjectToAsset(settingsWall, settingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            settingsHex = (GemsInStoneSettings)settings[0];
            settingsWall = (GemsInStoneSettings)settings[1];
        }
    }

    void OnDestroy()
    {
        //NOTE: not sure if done right, but this works
        EditorUtility.SetDirty(settingsHex);
        EditorUtility.SetDirty(settingsWall);
    }

    List<Vector2> SampleHex()
    {
        return PoissonDiskWithTilingSampler.SampleHex(settings.diskR, settings.maxNumGems); 
    }

    List<Vector2> SampleWall()
    {
        float width = (HexMeshGenerator.HexCellVertices[1] - HexMeshGenerator.HexCellVertices[0]).magnitude;
        float height = settings.wallHeight - settings.wallTopOffset;
        return PoissonDiskWithTilingSampler.SampleRectMirrorX(settings.diskR, settings.maxNumGems, Vector2.zero, new Vector2(width, height));
    }

    Vector3 PositionHex(Vector2 sample, float yShift)
    {
        return new Vector3(sample.x, yShift, sample.y);
    }

    Vector3 PositionWall(Vector2 sample, float yShift)
    {
        Vector2 origin2d = HexMeshGenerator.HexCellVertices[0];
        Vector2 dir2d = HexMeshGenerator.HexCellVertices[1] - origin2d;
        Vector3 origin = new Vector3(origin2d.x, -settings.wallTopOffset, origin2d.y);
        Vector3 dirX = new Vector3(dir2d.x, 0, dir2d.y).normalized;
        Vector3 dirZ = new Vector3(0, -1, 0);
        Vector3 dirY = Vector3.Cross(dirX, dirZ);

        return origin + sample.x * dirX + sample.y * dirZ + yShift * dirY;
    }

    void DuplicateToCheckTilingHex(Transform gems)
    {
        foreach (HexXY neigh in HexXY.neighbours)
        {
            Transform dup = GameObject.Instantiate<GameObject>(gems.gameObject).transform;
            dup.parent = gems.parent;
            Vector2 offset = HexXY.ex * neigh.x + HexXY.ey * neigh.y;
            dup.transform.localPosition += new Vector3(offset.x, 0, offset.y);
        }
    }

    void DuplicateToCheckTilingWall(Transform gems)
    {
        for (int i = 0; i < 6; i++)
        {           
            Transform dup = GameObject.Instantiate<GameObject>(gems.gameObject).transform;
            dup.parent = gems.parent;
            dup.transform.localRotation = Quaternion.AngleAxis(60 * i, new Vector3(0, 1, 0));
        }      
    }

    void Generate()
    {
        string gemCellsObjectName = "GemCells" + mode.ToString();
        GameObject existingGemCells = GameObject.Find(gemCellsObjectName);
        if(existingGemCells != null)        
            DestroyImmediate(existingGemCells);        

        Transform gemCells = new GameObject(gemCellsObjectName).transform;
        Transform gems = new GameObject("Gems").transform;
        gems.parent = gemCells;

        string[] gemMeshesPaths = Directory.GetFiles(Application.dataPath + "/" + settings.meshesFolder, "*.asset")
                                           .Select(x => Path.GetFileName(x))
                                           .ToArray();

        Mesh[] gemMeshes = gemMeshesPaths.Select(x => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/" + settings.meshesFolder + "/" + x)).ToArray();

        List<Vector2> positions = mode == Mode.Hex ? SampleHex() : SampleWall();

        currentGemsObjs = new List<GameObject>(); //save for later combining into a single mesh
        foreach (Vector2 pos in positions)
        {
            float exactSize = UnityEngine.Random.Range(settings.size.Min, settings.size.Max);
            float exactYShift = UnityEngine.Random.Range(-exactSize * settings.yShiftPercent, exactSize * settings.yShiftPercent);
            Mesh mesh = gemMeshes[UnityEngine.Random.Range(0, gemMeshes.Length - 1)];

            GameObject gem = new GameObject("Gem");
            currentGemsObjs.Add(gem);
            gem.transform.parent = gems;
            gem.transform.localRotation = UnityEngine.Random.rotationUniform;
            gem.transform.localScale = new Vector3(exactSize, exactSize, exactSize);
            MeshFilter meshFilter = gem.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gem.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            gem.transform.localPosition = mode == Mode.Hex ? PositionHex(pos, exactYShift) : PositionWall(pos, exactYShift);

            meshRenderer.sharedMaterial = settings.gemMaterial;
        }

        if (mode == Mode.Hex)
            DuplicateToCheckTilingHex(gems);
        else
            DuplicateToCheckTilingWall(gems);       
    }  

    void CombineMesh()
    {
        GameObject existingGemCells = GameObject.Find("GemCells");
        if (existingGemCells != null)
        {
            Mesh combinedMesh = MeshTools.CombineGameObjectMeshes(currentGemsObjs);
            AssetDatabase.CreateAsset(combinedMesh, "Assets/" + settings.combineMeshOutPath);
            AssetDatabase.SaveAssets();
            //DestroyImmediate(existingGemCells);
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
        mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);

        settings.maxNumGems = EditorGUILayout.IntField("Num gems", settings.maxNumGems);
        settings.diskR = EditorGUILayout.FloatField("Gems min distance", settings.diskR);
        settings.planeY = EditorGUILayout.FloatField("Plane Y", settings.planeY);
        settings.yShiftPercent = EditorGUILayout.FloatField("Random Y shift", settings.yShiftPercent);
        MakeFloatRangeField("Gems size", ref settings.size);
        settings.gemMaterial = (Material)EditorGUILayout.ObjectField("Gem material", settings.gemMaterial, typeof(Material), false);

        if(mode == Mode.Wall)
        {
            settings.wallHeight = EditorGUILayout.FloatField("Wall height", settings.wallHeight);
            settings.wallTopOffset = EditorGUILayout.FloatField("Wall top offset", settings.wallTopOffset);
        }

        if (GUILayout.Button("Generate and show"))
        {            
            Generate();            
        }

        settings.combineMeshOutPath = EditorGUILayout.TextField("Output mesh path", settings.combineMeshOutPath);
        if (GUILayout.Button("Combine and save"))
        {
            CombineMesh();
        }
    }

   

    //Samples a bunch of points no closer than 'diskR' to each other
    //inside a hex or rectangle, and takes care so that configuration can be tiled
    static class PoissonDiskWithTilingSampler
    {
        static readonly float limitX = 1f / Mathf.Sqrt(3);
        static readonly float limitY = 0.5f;

        static Vector2 SampleInsideHex()
        {
            for (; ; )
            {
                Vector2 sample = new Vector2(UnityEngine.Random.Range(-limitX, limitX), UnityEngine.Random.Range(-limitY, limitY));
                if (HexXY.FromPlaneCoordinates(sample) == new HexXY(0, 0))
                    return sample;
            }
        }

        static Vector2 SampleInsideRect(Vector2 min, Vector2 max)
        {
            return new Vector2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
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

        static Vector2[] RectMirrorX(float width, Vector2 v)
        {
            return new[]
            {
                v + new Vector2(width, 0),
                v - new Vector2(width, 0)
            };
        }

        public static List<Vector2> SampleHex(float diskR, int maxN)
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

        public static List<Vector2> SampleRectMirrorX(float diskR, int maxN, Vector2 min, Vector2 max)
        {
            const int maxNumTries = 100;

            float width = max.x - min.x;
            float sqrDiskR = diskR * diskR;

            List<Vector2> mirroredSamples = new List<Vector2>();
            List<Vector2> samples = new List<Vector2>();
            for (int i = 0; i < maxN; i++)
            {
                Vector2 candidateSample = Vector2.zero;
                int numTries = 0;
                for (; numTries < maxNumTries; numTries++)
                {
                    candidateSample = SampleInsideRect(min, max);
                    if (samples.Concat(mirroredSamples).All(s => (s - candidateSample).sqrMagnitude >= sqrDiskR))
                        break;
                }

                if (numTries == maxNumTries)
                    break;
                else
                {
                    samples.Add(candidateSample);
                    mirroredSamples.AddRange(RectMirrorX(width, candidateSample));
                }
            }

            return samples;
        }
    }
}
