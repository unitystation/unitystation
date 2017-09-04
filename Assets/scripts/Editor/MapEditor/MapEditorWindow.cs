using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace MapEditor {

    public class MapEditorWindow: EditorWindow {
        private int categoryIndex;
        private string[] categoryNames;

        private OptionsView optionsView = new OptionsView();
        private CategoryView[] categories;

        private bool showOptions;

        [MenuItem("Window/Map Editor")]
        public static void ShowWindow() {
            GetWindow<MapEditorWindow>("Map Editor");
        }

        public void OnEnable() {
            SceneView.onSceneGUIDelegate += Main.OnSceneGUI;
            Initialize();
        }

        public void OnDisable() {
            SceneView.onSceneGUIDelegate -= Main.OnSceneGUI;
        }

        public void Initialize() {
            string path = Application.dataPath;
            IEnumerable<string> files = Directory.GetDirectories(path + "/Prefabs", "*.*", SearchOption.AllDirectories);
            categories = new CategoryView[] { new StructuresView(files,path), new ObjectsView(files,path), new ItemsView(files,path) };
            categoryNames = new string[categories.Length];
            for(int i = 0; i < categoryNames.Length; i++) {
                categoryNames[i] = categories[i].Name;
            }
        }

        void OnGUI() {

            Main.EnableEdit = EditorGUILayout.BeginToggleGroup("Map Editor Mode", Main.EnableEdit);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();

            DrawContent();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndToggleGroup();
        }

        private void DrawContent() {
            showOptions = EditorGUILayout.Foldout(showOptions, "Options");

            if(showOptions) {
                optionsView.OnGUI();
            }

            categoryIndex = GUILayout.Toolbar(categoryIndex, categoryNames);

            categories[categoryIndex].OnGUI();
        }
    }
}