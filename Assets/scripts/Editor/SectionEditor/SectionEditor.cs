using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SectionEditor {

    public class SectionEditor: EditorWindow {
        [MenuItem("Window/Section Editor")]
        public static void ShowWindow() {
            var window = GetWindow<SectionEditor>("Section Editor");
        }

        public void OnEnable() {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        public void OnDisable() {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private Section selectedSection = null;
        private string newSectionName = string.Empty;
        private Vector2 scrollPosition;

        private SceneView currentSceneView;


        void OnGUI() {
            var drawGizmos = GUILayout.Toggle(MatrixDrawer.Instance.drawGizmos, "Draw Gizmos");

            if(MatrixDrawer.Instance.drawGizmos != drawGizmos) {
                currentSceneView.Focus();
            }
            MatrixDrawer.Instance.drawGizmos = drawGizmos;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach(var section in SectionData.Sections) {
                EditorGUILayout.BeginHorizontal();
                var selected = EditorGUILayout.Toggle(selectedSection == section, GUILayout.MaxWidth(15));

                if(selected) {
                    if(selectedSection != section) {
                        selectedSection = section;
                        currentSceneView.Focus();
                    }
                } else if(selectedSection == section) {
                    selectedSection = null;
                    currentSceneView.Focus();
                }


                section.Name = GUILayout.TextField(section.Name, GUILayout.MaxWidth(150));
                section.color = EditorGUILayout.ColorField(section.color);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(23);

            newSectionName = GUILayout.TextField(newSectionName, GUILayout.MaxWidth(150));

            if(GUILayout.Button("Add Section") && !string.IsNullOrEmpty(newSectionName)) {

                var color = Random.ColorHSV(0, 1, 1, 1, 0.5f, 1, 0.5f, 0.5f);
                SectionData.AddSection(newSectionName, color);

                EditorUtility.SetDirty(SectionData.Instance);

                newSectionName = "";
            }
            EditorGUILayout.EndHorizontal();
        }


        void OnSceneGUI(SceneView sceneView) {
            currentSceneView = sceneView;


            var e = Event.current;

            if(e.isKey) {
                if(e.type == EventType.KeyDown) {
                    Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

                    int x = Mathf.RoundToInt(r.origin.x);
                    int y = Mathf.RoundToInt(r.origin.y);

                    switch(e.character) {
                        case 'a':
                            if(selectedSection != null) {
                                SetSectionAt(x, y, selectedSection);
                                e.Use();
                            }
                            break;
                        case 'd':
                            SetSectionAt(x, y, null);
                            e.Use();
                            break;
                    }
                }
            }
        }

        private void SetSectionAt(int x, int y, Section section) {
            Matrix.Matrix.At(x, y).Section = section;
            //EventSystem.Invoke(x, y);
            EditorUtility.SetDirty(Matrix.Matrix.Instance);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
