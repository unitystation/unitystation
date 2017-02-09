using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TileTypeView : EditorWindow {

    [MenuItem("Window/Tile Type View")]
    public static void ShowWindow() {
        GetWindow<TileTypeView>("Tile Type View");
    }

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    public void OnDisable() {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView) {
        Gizmos.DrawCube(new Vector3(1778, 1136, 0), Vector3.one);
        HandleUtility.Repaint();
    }
}
