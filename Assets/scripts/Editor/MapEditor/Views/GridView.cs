using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Matrix;

namespace MapEditor {    

    public class GridView: AbstractView {

        private int gridIndex;
        private GridData gridData;
        private Vector2 scrollPosition;

        private string prefabPath;
        private string subsectionPath;

        private int elementsPerLine = 4;
        private int rightGridMargin = 3;

        public GridView(string prefabPath, string subsectionPath) {
            this.prefabPath = prefabPath;
            this.subsectionPath = subsectionPath;
        }

        public override void OnGUI() {
            gridData = new GridData(prefabPath);

            calculateMargin();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) {
                fixedHeight = 75, fixedWidth = 75, margin = new RectOffset(0, rightGridMargin, 3, 3)
            };


            EditorGUILayout.BeginHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            gridIndex = GUILayout.SelectionGrid(gridIndex, gridData.Textures, elementsPerLine, buttonStyle);

            PreviewObject.Prefab = gridData.Prefabs[gridIndex];
            BuildControl.CurrentSubSectionName = subsectionPath;

            EditorGUILayout.EndScrollView();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        private void calculateMargin() {
            if(Event.current.type == EventType.Repaint) {
                int fullWidth = (int) GUILayoutUtility.GetLastRect().width - 15;
                elementsPerLine = ((fullWidth) / 80);
                rightGridMargin = (fullWidth % 80) / elementsPerLine + 3;
            }
        }
    }
}