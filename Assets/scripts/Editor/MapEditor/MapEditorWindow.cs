using UnityEngine;
using UnityEditor;
using Matrix;
using System.Linq;
using UI;
using System.Collections.Generic;

namespace MapEditor {

    public class MapEditorWindow: EditorWindow {
        private int categoryIndex;
        private string[] categoryNames;

        private OptionsView optionsView = new OptionsView();
        private CategoryView[] categories = { new StructuresView(), new ObjectsView(), new ItemsView() };

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
            categories = new CategoryView[] { new StructuresView(), new ObjectsView(), new ItemsView() };
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