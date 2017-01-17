using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapEditorMap {
    public static int SectionIndex { get; set; }
    public static int SubSectionIndex { get; set; }

    public static List<GameObject> Sections = new List<GameObject>();
    public static string[] SubSectionNames = new string[] { "Walls", "Floors", "Doors", "Tables" };

    public static string mapName = "Map Not Loaded";


    private static GameObject mapObject;

    public static GameObject MapObject {
        get {
            return mapObject;
        }

        set {
            if(value == null)
                return;

            mapName = value.name;
            mapObject = value;
            Sections.Clear();
            foreach(Transform child in mapObject.transform) {
                Sections.Add(child.gameObject);
            }
        }
    }

    public static GameObject CurrentSection {
        get {
            return Sections[SectionIndex];
        }
    }

    public static string CurrentSubSectionName {
        get {
            return SubSectionNames[SubSectionIndex];
        }
    }

    public static Transform CurrentSubSection {
        get {
            Transform subSection = CurrentSection.transform.FindChild(CurrentSubSectionName);
            if(subSection == null) {
                GameObject newSubSection = new GameObject(CurrentSubSectionName);
                newSubSection.transform.parent = CurrentSection.transform;
                newSubSection.transform.localPosition = Vector3.zero;
                subSection = newSubSection.transform;
            }
            return subSection;
        }
    }
}