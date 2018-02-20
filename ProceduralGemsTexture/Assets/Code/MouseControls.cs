using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MouseControls : MonoBehaviour
{
    static float mapScale = 10;

    Map map;

    Plane floor = new Plane(Vector3.up, Vector3.zero);
    Plane ceiling = new Plane(Vector3.up, new Vector3(0, mapScale, 0));

    MapCellAndCoords MapCellFromPoint(Vector3 p)
    {
        Vector2 pos2d = new Vector2(p.x, p.z) / mapScale;
        HexXY cellCoords = HexXY.FromPlaneCoordinates(pos2d);
        return new MapCellAndCoords(map.GetCell(cellCoords), cellCoords);
    }

    //Tries ceiling first then floor
    MapCellAndCoords? MapCellFromRay(Ray ray)
    {
        float dist;

        ceiling.Raycast(ray, out dist);
        MapCellAndCoords cell = MapCellFromPoint(ray.origin + dist * ray.direction);
        if (cell.Cell.state == MapCell.State.Low)
        {
            floor.Raycast(ray, out dist);
            cell = MapCellFromPoint(ray.origin + dist * ray.direction);

            if (cell.Cell.state == MapCell.State.Low)
                return cell;
        }
        else
            return cell;

        //Hit a wall
        return null;
    }

    void LeftClick(Ray ray)
    {
        MapCellAndCoords? cc = MapCellFromRay(ray);
        if(cc != null)
        {
            MapCell cell = map.GetCell(cc.Value.Coords);
            cell.state = cell.state == MapCell.State.High ? MapCell.State.Low : MapCell.State.High;
            GetComponent<HexMeshGeneratorTests>().RedrawMeshes();
        }
        //Debug.Log(cell);        
    }

    void Start()
    {
        map = GetComponent<HexMeshGeneratorTests>().map;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            LeftClick(Camera.main.ScreenPointToRay(Input.mousePosition));
        }
    }
}

