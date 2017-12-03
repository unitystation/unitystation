using Matrix;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TileTypeDrawer: MonoBehaviour {
    public static bool DrawGizmos;

    private static TileTypeDrawer instance;
    public static TileTypeDrawer Instance {
        get {
            if(!instance) {
                GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                if(instanceTemp != null) {
                    instance = instanceTemp.GetComponentInChildren<TileTypeDrawer>(true);
                } else {
                    instance = null;
                }
            }

            return instance;
        }
    }
    private List<TileType> baseTileTypes = new List<TileType>();
    private List<TileType> conditionalTileTypes = new List<TileType>();

    public static List<TileType> BaseTileTypes { get { return Instance.baseTileTypes; } }

    public static List<TileType> ConditionalTileTypes { get { return Instance.conditionalTileTypes; } }
}

#if UNITY_EDITOR
public class TileTypeGizmosDrawer {
    private static Color colorTrue;
    private static Color colorFalse;

    static TileTypeGizmosDrawer() {
        colorTrue = Color.green;
        colorTrue.a = 0.5f;
        colorFalse = Color.red;
        colorFalse.a = 0.5f;
    }

    [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
    static void DrawGizmo(TileTypeDrawer scr, GizmoType gizmoType) {
        if(!TileTypeDrawer.DrawGizmos)
            return;

        var start = Camera.current.ScreenToWorldPoint(Vector3.zero); // bottom left
        var end = Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth, Camera.current.pixelHeight));

        for(int y = Mathf.RoundToInt(start.y); y < Mathf.RoundToInt(end.y + 1); y++) {
            for(int x = Mathf.RoundToInt(start.x); x < Mathf.RoundToInt(end.x + 1); x++) {
                var node = Matrix.Matrix.At(x, y, false);

                if(node != null) {
                    foreach(var tileType in TileTypeDrawer.BaseTileTypes) {
                        if(node.HasTileType(tileType)) {
                            Gizmos.color = colorTrue;

                            foreach(var condtileType in TileTypeDrawer.ConditionalTileTypes) {
                                if(!node.HasTileType(condtileType)) {
                                    Gizmos.color = colorFalse;
                                    break;
                                }
                            }

                            Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
                        }
                    }
                }
            }
        }
    }
}
#endif