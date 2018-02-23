using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapEditor))]
public class MapEditorEditor : Editor
{
    MapEditor editor;


    //MapCellAndCoords MapCellFromPoint(Vector3 p)
    //{
    //    Vector2 pos2d = new Vector2(p.x, p.z) / mapScale;
    //    HexXY cellCoords = HexXY.FromPlaneCoordinates(pos2d);
    //    return new MapCellAndCoords(map.GetCell(cellCoords), cellCoords);
    //}

    ////Tries ceiling first then floor
    //MapCellAndCoords? MapCellFromRay(Ray ray)
    //{
    //    float dist;

    //    ceiling.Raycast(ray, out dist);
    //    MapCellAndCoords cell = MapCellFromPoint(ray.origin + dist * ray.direction);
    //    if (cell.Cell.state == MapCell.State.Low)
    //    {
    //        floor.Raycast(ray, out dist);
    //        cell = MapCellFromPoint(ray.origin + dist * ray.direction);

    //        if (cell.Cell.state == MapCell.State.Low)
    //            return cell;
    //    }
    //    else
    //        return cell;

    //    //Hit a wall
    //    return null;
    //}

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

        if (Camera.current == null)
            return;

        //This is needed to disable default object selection in scene
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.modifiers == EventModifiers.None)
        {
            Vector2 pos = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(pos);
            float dist;

            editor.floor.Raycast(ray, out dist);
            Vector3 p = ray.origin + dist * ray.direction;

            GUIUtility.hotControl = controlId;
            e.Use();

            Debug.Log(p);            
        }
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

