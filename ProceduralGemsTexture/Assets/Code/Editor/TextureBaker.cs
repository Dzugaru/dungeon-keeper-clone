using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using System.IO;

public class TextureBaker : EditorWindow
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
    Material planeMaterial, gemMaterial;

    [MenuItem("Window/Texture Baker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TextureBaker));
    }

    void Setup()
    {
        GameObject existingBaker = GameObject.Find("Baker");
        if(existingBaker != null)
            DestroyImmediate(existingBaker);

        Transform root = new GameObject("Baker").transform;
        Transform camTrans = new GameObject("Camera").transform;
        camTrans.parent = root;
        camTrans.localRotation = Quaternion.LookRotation(new Vector3(0, -1, 0));

        Camera camera = camTrans.gameObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.transform.localPosition = new Vector3(0.5f, 1, 0.5f);
        camera.orthographicSize = 0.5f;

        Transform basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
        basePlane.parent = root;
        basePlane.localPosition = new Vector3(0.5f, planeY, 0.5f);
        basePlane.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        basePlane.GetComponent<MeshRenderer>().sharedMaterial = planeMaterial;


        Transform gems = new GameObject("Gems").transform;
        gems.parent = root;
        
        string[] gemMeshesPaths = Directory.GetFiles(Application.dataPath + "/" + meshesFolder, "*.asset")
                                           .Select(x => Path.GetFileName(x))
                                           .ToArray();

        Mesh[] gemMeshes = gemMeshesPaths.Select(x => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/" + meshesFolder + "/" + x)).ToArray();

        List<Vector2> positions = Noise.PoissonDiskSample(new Vector2(0, 0), new Vector2(1, 1), diskR, maxNumGems);

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

        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGB24, false);

        camera.targetTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
        camera.Render();

        RenderTexture.active = camera.targetTexture;
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);

        AssetDatabase.CreateAsset(tex, "Assets/Baked.asset");
        AssetDatabase.SaveAssets();
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

        planeMaterial = (Material)EditorGUILayout.ObjectField("Plane material", planeMaterial, typeof(Material), false);
        gemMaterial = (Material)EditorGUILayout.ObjectField("Gem material", gemMaterial, typeof(Material), false);
        if(GUILayout.Button("Prepare"))
        {
            Setup();
        }
    }
}
