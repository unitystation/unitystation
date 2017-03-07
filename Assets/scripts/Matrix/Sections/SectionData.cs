using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[Serializable]
public class SectionData : ScriptableObject {
    private const string AssetPath = "Assets/Data/Sections.asset";
    private static SectionData sectionData;

    public static SectionData Instance {
        get {
            if(!sectionData) {
                LoadSections();
            }
            return sectionData;
        }
    }

    private static void LoadSections() {
		#if UNITY_EDITOR
        sectionData = AssetDatabase.LoadAssetAtPath<SectionData>(AssetPath);

        if(!sectionData) {
            sectionData = CreateInstance<SectionData>();
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(sectionData, AssetPath);
        }
		#endif
    }
    [SerializeField]
    private List<Section> sections;

    public void OnEnable() {
        if(sections == null) {
            sections = new List<Section>();
        }
    }

    public static List<Section> Sections {
        get { return Instance.sections; }
    }

    public static void AddSection(string name, Color color) {
        var section = CreateInstance<Section>();
        section.Init(name, color);
        Sections.Add(section);
		#if UNITY_EDITOR
        AssetDatabase.AddObjectToAsset(section, AssetPath);
		#endif
    }
}
