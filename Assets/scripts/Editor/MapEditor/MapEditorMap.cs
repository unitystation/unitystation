using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapEditorMap
{
    
    public static string mapName = "Map Not Loaded";
    public static GameObject mapObj;
    public static List<GameObject> mapSections = new List<GameObject>();

    public static void SetMap(GameObject map)
    {
        mapName = map.name;
        mapObj = map;
        foreach (Transform child in mapObj.transform)
        {
            mapSections.Add(child.gameObject);
        }
    }

}
