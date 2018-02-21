using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
class GemsInStoneSettings : ScriptableObject
{
    public int maxNumGems = 10;
    public string meshesFolder = "Resources/GemMeshes";
    public float diskR = 0.05f;
    public float yShiftPercent = 0.2f;
    public float planeY = 0;
    public MinMaxRangeFloat size = new MinMaxRangeFloat(0.02f, 0.02f);
    public Material gemMaterial;
    public string combineMeshOutPath = "Resources/GemsInStoneHex.asset";

    //Wall specific
    public float wallHeight;
    public float wallTopOffset;
}