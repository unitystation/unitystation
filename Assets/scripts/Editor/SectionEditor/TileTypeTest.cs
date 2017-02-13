using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SectionEditor {

    public class TileTypeTest: EditorWindow {
        [MenuItem("Window/Tile Type Test")]
        public static void ShowWindow() {
            var window = GetWindow<TileTypeTest>("Tile Type Test");
        }

        public void OnEnable() {
            Initialize();
        }

        public void Initialize() {
            MatrixDrawer.AddSection("Kitchen", n => !n.IsPassable(), Color.blue);
            MatrixDrawer.AddSection("Bar", n => !n.IsSpace() && n.IsAtmosPassable(), Color.green);
            MatrixDrawer.AddSection("Space", n => n.IsSpace(), Color.red);
        }

        void OnGUI() {
            MatrixDrawer.Instance.drawGizmos = GUILayout.Toggle(MatrixDrawer.Instance.drawGizmos, "Draw Gizmos");

        }

        private bool IsDoor(Matrix.MatrixNode node) {
            return node.IsDoor();
        }

        private bool IsPassable(Matrix.MatrixNode node) {
            return node.IsPassable();
        }
    }
}
