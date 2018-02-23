using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapEditor))]
public class MapEditorEditor : Editor
{
    Rect sceneGUIRect = new Rect(20, 20, 150, 60);

    MapEditor editor;
    MapCellAndCoords MapCellFromPoint(Vector3 p)
    {
        Vector2 pos2d = new Vector2(p.x, p.z) / editor.mapScale;
        HexXY cellCoords = HexXY.FromPlaneCoordinates(pos2d);
        return new MapCellAndCoords(editor.map.GetCell(cellCoords), cellCoords);
    }

    //Tries ceiling first then floor
    MapCellAndCoords? MapCellFromRay(Ray ray)
    {
        float dist;

        editor.ceiling.Raycast(ray, out dist);
        MapCellAndCoords cell = MapCellFromPoint(ray.origin + dist * ray.direction);
        if (cell.Cell.state == MapCell.State.Excavated)
        {
            editor.floor.Raycast(ray, out dist);
            cell = MapCellFromPoint(ray.origin + dist * ray.direction);

            if (cell.Cell.state == MapCell.State.Excavated)
                return cell;
        }
        else
            return cell;

        //Hit a wall
        return null;
    }

    //void LeftClick(Ray ray)
    //{
    //    MapCellAndCoords? cc = MapCellFromRay(ray);
    //    if (cc != null)
    //    {
    //        //MapCell cell = map.GetCell(cc.Value.Coords);
    //        //cell.state = cell.state == MapCell.State.High ? MapCell.State.Low : MapCell.State.High;
    //        //GetComponent<HexMeshGeneratorTests>().RedrawMeshes();
    //    }
    //    //Debug.Log(cell);        
    //}

    Tool lastTool = Tool.None;   

    private void OnEnable()
    {
        editor = (MapEditor)target;
        editor.map = AssetDatabase.LoadAssetAtPath<Map>("Assets/map.asset");

        //This disables default transform gizmo that gets in the way of editing map
        lastTool = Tools.current;
        Tools.current = Tool.None;
    }

    void OnDisable()
    {
        Tools.current = lastTool;
    }

    
    private void OnSceneGUI()
    {
        Tools.current = Tool.None;

        if (Camera.current == null || editor.map == null)
            return;

        //This is needed to disable default object selection in scene
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && e.modifiers == EventModifiers.None &&
            !sceneGUIRect.Contains(e.mousePosition))
        {
            editor.DrawWithBrush();
            GUIUtility.hotControl = controlId;
            e.Use();
        }

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            Vector2 pos = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(pos);
            bool changed = editor.SetCurrentMousePos(MapCellFromRay(ray));
            if (changed && e.type == EventType.MouseDrag && e.button == 0 && e.modifiers == EventModifiers.None)
                editor.DrawWithBrush();
        }

        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Period)
            {
                editor.brushSize = Math.Min(9, editor.brushSize + 1);
                AfterHotkey(e);
            }
            else if (e.keyCode == KeyCode.Comma)
            {
                editor.brushSize = Math.Max(1, editor.brushSize - 1);
                AfterHotkey(e);
            }  
            else if(e.keyCode == KeyCode.N)
            {
                editor.brushMode = MapEditor.BrushMode.Excavate;
                AfterHotkey(e);
            }
            else if(e.keyCode == KeyCode.M)
            {
                editor.brushMode = MapEditor.BrushMode.Fill;
                AfterHotkey(e);
            }
        }

        RenderSceneUI();
    }

    private void AfterHotkey(Event e)
    {
        EditorUtility.SetDirty(editor);
        editor.OnParameterChanged();
        e.Use();
    }

    void RenderSceneUI()
    {
        //Example UI
        Handles.BeginGUI();

        GUILayout.BeginArea(sceneGUIRect);

        //var rect = EditorGUILayout.BeginVertical();
        //GUI.color = Color.yellow;
        //GUI.Box(rect, GUIContent.none);

        //GUI.color = Color.white;

        //GUILayout.BeginHorizontal();
        //GUILayout.FlexibleSpace();
        //GUILayout.Label("Rotate");
        //GUILayout.FlexibleSpace();
        //GUILayout.EndHorizontal();

        //GUILayout.BeginHorizontal();
        //GUI.backgroundColor = Color.red;

        //if (GUILayout.Button("Left"))
        //{
        //    Debug.Log("left");
        //}

        //if (GUILayout.Button("Right"))
        //{
        //    Debug.Log("right");
        //}

        //GUILayout.EndHorizontal();

        //EditorGUILayout.EndVertical();


        GUILayout.EndArea();

        Handles.EndGUI();
    }

    public override void OnInspectorGUI()
    {  
        DrawDefaultInspector();
        
        if(GUILayout.Button("Create map"))
        {
            AssetDatabase.DeleteAsset("Assets/map.asset");

            editor.map = CreateInstance<Map>();
            editor.map.New(editor.newMapRadius);
            EditorUtility.SetDirty(editor.map);

            editor.CreateMapMeshes();

            AssetDatabase.CreateAsset(editor.map, "Assets/map.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();                       
        }
    }
}

