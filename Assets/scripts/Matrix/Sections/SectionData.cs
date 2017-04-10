using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SectionData : ScriptableObject {
    private static string AssetPath = "Assets/Data/Sections.asset";
    private static string activeSceneName;

    private static SectionData sectionData;
    public static SectionData Instance {
        get {
            Scene scene = SceneManager.GetActiveScene();
            if(!sectionData || !activeSceneName.Equals(scene.name)) {
                LoadSections();
            }
            return sectionData;
        }
    }

    private static void LoadSections() {
        #if UNITY_EDITOR
        Scene scene = SceneManager.GetActiveScene();
        activeSceneName = scene.name;
        AssetPath = "Assets/Data/" + activeSceneName + "_Sections.asset";

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

    public static Section AddSection(string name, Color color) {
        var section = CreateInstance<Section>();
        section.Init(name, color);
        Sections.Add(section);
#if UNITY_EDITOR
        AssetDatabase.AddObjectToAsset(section, AssetPath);
#endif

        return section;
    }
}
