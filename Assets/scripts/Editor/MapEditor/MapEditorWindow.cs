using UnityEngine;
using UnityEditor;
using Matrix;
using System.Linq;
using UI;

namespace MapEditor {

    public class MapEditorWindow: EditorWindow {
        private bool showOptions;
        private int xCount = 4;
        private int[] gridIndices;
        private Vector2[] scrollPositions;

        [MenuItem("Window/Map Editor")]
        public static void ShowWindow() {
            //Show existing window instance. If one doesn't exist, make one.
            GetWindow<MapEditorWindow>("Map Editor");
        }

        public void OnEnable() {
            SceneView.onSceneGUIDelegate += Main.OnSceneGUI;
            Init();
        }

        public void OnDisable() {
            SceneView.onSceneGUIDelegate -= Main.OnSceneGUI;
        }

        void Init() {
            MapEditorData.Clear();
            MapEditorData.LoadPrefabs();
            MapEditorControl.HashCode = GetHashCode();

            if(gridIndices == null) {
                gridIndices = new int[MapEditorMap.SubSectionNames.Length];
                for(int i = 0; i < gridIndices.Length; i++) {
                    gridIndices[i] = -1;
                }
                scrollPositions = new Vector2[gridIndices.Length];
            }
            MapEditorMap.MapObject = GameObject.FindGameObjectWithTag("Map");
        }

        void OnGUI() {
            Main.EnableEdit = EditorGUILayout.BeginToggleGroup("Map Editor Mode", Main.EnableEdit);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();


            if(!MapEditorMap.MapLoaded)
                MapEditorMap.MapObject = GameObject.FindGameObjectWithTag("Map");

            showOptions = EditorGUILayout.Foldout(showOptions, "Options");

            if(showOptions) {
                InputControl.AllowMouse = EditorGUILayout.Toggle("Create On Mouse Click", InputControl.AllowMouse);
                PreviewObject.ShowPreview = EditorGUILayout.Toggle("Enable Preview", PreviewObject.ShowPreview);
                MapEditorControl.CheckTileFit = EditorGUILayout.Toggle("Check Tile Fit", MapEditorControl.CheckTileFit);
                //to handle different keyboard types (i.e. German and US);
                InputControl.RotateOptA = EditorGUILayout.Toggle("Use Rotate Keys: z and x", InputControl.RotateOptA);
                InputControl.RotateOptB = EditorGUILayout.Toggle("Use Rotate Keys: < and >", InputControl.RotateOptB);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Map", GUILayout.Width(100));
                EditorGUILayout.LabelField(MapEditorMap.MapName);
                EditorGUILayout.EndHorizontal();

                if(GUILayout.Button("Refresh Data")) {
                    Init();
                }
            }

            EditorGUILayout.BeginHorizontal();
            string[] options = new string[MapEditorMap.Sections.Count];
            foreach(GameObject section in MapEditorMap.Sections) {
                options[MapEditorMap.Sections.IndexOf(section)] = section.name;
            }
            EditorGUILayout.LabelField("Section", GUILayout.Width(100));
            MapEditorMap.SectionIndex = EditorGUILayout.Popup(MapEditorMap.SectionIndex, options);
            EditorGUILayout.EndHorizontal();

            MapEditorMap.SubSectionIndex = GUILayout.Toolbar(MapEditorMap.SubSectionIndex, MapEditorMap.SubSectionNames);
            int tabIndex = MapEditorMap.SubSectionIndex;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 75, fixedWidth = 75 };

            if(Event.current.type == EventType.Repaint)
                xCount = (int) ((GUILayoutUtility.GetLastRect().width + 20) / 75);

            EditorGUILayout.BeginHorizontal();
            scrollPositions[tabIndex] = EditorGUILayout.BeginScrollView(scrollPositions[tabIndex]);

            gridIndices[tabIndex] = GUILayout.SelectionGrid(gridIndices[tabIndex], MapEditorData.CurrentTextures, xCount, buttonStyle);

            if(gridIndices[tabIndex] >= 0) {
                PreviewObject.Prefab = MapEditorData.CurrentPrefabs[gridIndices[tabIndex]];
            } else {
                PreviewObject.Prefab = null;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
        }
    }
}