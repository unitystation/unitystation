using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapEditor {

    public class MapEditorMap {
        public static int SectionIndex { get; set; }

        public static List<GameObject> Sections = new List<GameObject>();

        public static string MapName { get; private set; }
        public static bool MapLoaded { get; private set; }

        private static GameObject mapObject;

        public static GameObject MapObject {
            get {
                return mapObject;
            }

            set {
                if(value == null) {
                    MapName = "Map Not Loaded";
                    MapLoaded = false;
                    return;
                }
                MapLoaded = true;

                MapName = value.name;
                mapObject = value;
                Sections.Clear();
                foreach(Transform child in mapObject.transform) {
                    Sections.Add(child.gameObject);
                }
            }
        }

        public static GameObject CurrentSection {
            get {
                if(SectionIndex < Sections.Count) {
                    return Sections[SectionIndex];
                }
                return null;
            }
        }

        public static string CurrentSubSectionName {
            get; set;
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
}