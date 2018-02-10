using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Gems : EditorWindow
{
    enum MeshMode
    {
        ConvexHull,
        PlaneCut,
        Both
    }


    int numMeshesToGenerate = 10;
    MinMaxRangeInt numPoints = new MinMaxRangeInt(7, 15);
    MeshMode meshMode = MeshMode.PlaneCut;
    int nRelaxIter = 100;
    float stepAngle = 0.05f;
    float stepReduction = 0.95f;
    string folder = "Resources/GemMeshes";

    bool isGeneratingMeshes = false;
    int generatedMeshesCount = 0;


    [MenuItem("Window/Gems")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(Gems));
    }

    void MakeRangeField(string label, ref MinMaxRangeInt range)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        range.Min = EditorGUILayout.IntField(range.Min);
        range.Max = EditorGUILayout.IntField(range.Max);
        GUILayout.EndHorizontal();        
    }

    void GenerateMesh(string name)
    {
        ConvexPolyhedra.Generator gen = new ConvexPolyhedra.Generator();
        int exactNumPoints = Random.Range(numPoints.Min, numPoints.Max);
        Vector3[] points = gen.GeneratePointsOnSphere(exactNumPoints);
        int nearest = points.Length - 1;
        points = gen.FindRelaxedConfigurationOfPointsOnSphere(points, nearest, ConvexPolyhedra.Generator.InverseLinearRepel, stepAngle, stepReduction, nRelaxIter);

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();

        MeshMode currentMode = meshMode;
        if (currentMode == MeshMode.Both)
            currentMode = Random.value < 0.5 ? MeshMode.ConvexHull : MeshMode.PlaneCut;

        if(currentMode == MeshMode.ConvexHull)
            gen.GenerateConvexHullTriangles(points, vertices, tris);
        else
            gen.GeneratePolyTriangles(points, vertices, tris);

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        AssetDatabase.CreateAsset(mesh, "Assets/" + folder + "/" + name + ".asset");
        AssetDatabase.SaveAssets();        
    }

    void StartGeneratingMeshes()
    {
        isGeneratingMeshes = true;
        generatedMeshesCount = 0;

        FileUtil.DeleteFileOrDirectory("Assets/" + folder);
        AssetDatabase.Refresh();

        string path = "Assets";
        foreach (string subf in folder.Split('/'))
        {
            AssetDatabase.CreateFolder(path, subf);
            path += "/" + subf;
        }
    }

    void OnGUI()
    {
        GUILayout.BeginVertical(GUI.skin.box);      
        MakeRangeField("Num points", ref numPoints);
        meshMode = (MeshMode)EditorGUILayout.EnumPopup("Generation mode", meshMode);
        nRelaxIter = EditorGUILayout.IntField("Relax iters", nRelaxIter);
        stepAngle = EditorGUILayout.FloatField(new GUIContent("Step angle", "Starting maximum point angle change on single repel iteration"), stepAngle);
        stepReduction = EditorGUILayout.FloatField(new GUIContent("Step reduction", "Reduction factor of maximum angle change for each subsequent iteration"), stepReduction);
        folder = EditorGUILayout.TextField("Path to meshes", folder.Trim('/'));
        numMeshesToGenerate = EditorGUILayout.IntField("Num meshes", numMeshesToGenerate);

        bool generateButtonPressed = GUILayout.Button("Generate meshes");
        GUILayout.EndVertical();

        if (generateButtonPressed)        
            StartGeneratingMeshes();            
    }

    void Update()
    {
        if (isGeneratingMeshes)
        {
            EditorUtility.DisplayProgressBar("Generating...", "", (float)generatedMeshesCount / numMeshesToGenerate);
            GenerateMesh(generatedMeshesCount.ToString());
            generatedMeshesCount++;
            isGeneratingMeshes = generatedMeshesCount < numMeshesToGenerate;
        }
        else
        {
            EditorUtility.ClearProgressBar();           
        }
    }
}
