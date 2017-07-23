using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Matrix;

public class MatrixCheck: EditorWindow {
    [MenuItem("Window/Matrix Check")]
    public static void ShowWindow() {
        GetWindow<MatrixCheck>("Matrix Check");
    }

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    public void OnDisable() {
        drawGizmos = false;
        TileTypeDrawer.DrawGizmos = false;
        TilePropertyDrawer.DrawGizmos = false;
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    private SceneView currentSceneView;

    private bool drawGizmos;
    private int checkTypeIndex;

    void OnGUI() {
        var tempDrawGizmos = GUILayout.Toggle(drawGizmos, "Draw Gizmos");

        if(tempDrawGizmos != drawGizmos) {
            drawGizmos = tempDrawGizmos;
            TileTypeDrawer.DrawGizmos = drawGizmos;
            TilePropertyDrawer.DrawGizmos = drawGizmos;
            currentSceneView.Repaint();
        }

        checkTypeIndex = GUILayout.Toolbar(checkTypeIndex, new string[] { "Tile Types", "Tile Property" });

        if(checkTypeIndex == 0) {
            foreach(var tiletype in TileType.List) {
                if(tiletype == TileType.None) continue;

                GUILayout.BeginHorizontal();

                OnGUISelection(TileTypeDrawer.BaseTileTypes, tiletype);
                OnGUISelection(TileTypeDrawer.ConditionalTileTypes, tiletype);

                GUILayout.Label(tiletype.Name);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("1st column: base, 2nd column: condition");
        } else if(checkTypeIndex == 1) {
            foreach(var property in TilePropertyDrawer.Properties) {
                GUILayout.BeginHorizontal();
                bool isSelected = property == TilePropertyDrawer.Selected;
                bool selected = EditorGUILayout.Toggle(isSelected, GUILayout.MaxWidth(15));

                if(selected && !isSelected) {
                    TilePropertyDrawer.Selected = property;
                    currentSceneView.Repaint();
                } else if(!selected && isSelected) {
                    TilePropertyDrawer.Selected = null;
                    currentSceneView.Repaint();
                }

                GUILayout.Label(property.Name);
                var negate = EditorGUILayout.Toggle(property.Negate, GUILayout.MaxWidth(15));
                if(negate != property.Negate) {
                    property.Negate = negate;
                    currentSceneView.Repaint();

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("1st column: select, 2nd column: negate");
        }
    }

    private void OnGUISelection(List<TileType> tileTypeList, TileType tileType) {

        var isSelected = tileTypeList.Contains(tileType);

        var selected = EditorGUILayout.Toggle(isSelected, GUILayout.MaxWidth(15));

        if(selected && !isSelected) {
            tileTypeList.Add(tileType);
            currentSceneView.Repaint();
        } else if(!selected && isSelected) {
            tileTypeList.Remove(tileType);
            currentSceneView.Repaint();
        }

    }

    void OnSceneGUI(SceneView sceneView) {
        currentSceneView = sceneView;
    }
}