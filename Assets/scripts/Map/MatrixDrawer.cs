using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MatrixDrawer : MonoBehaviour {

    public bool allowGizmos;

    void Update() {

    }
}

public class MyScriptGizmoDrawer {

    [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
    static void DrawGizmo(MatrixDrawer scr, GizmoType gizmoType) {
        if(!scr.allowGizmos)
            return;

        var color = Color.blue;
        color.a = 0.5f;

        Gizmos.color = color;

        for(int y = 1000; y < 1300; y++) {
            for(int x = 1700; x < 2000; x++) {
                if(!Matrix.Matrix.At(x, y).IsPassable())
                    Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
            }
        }
    }
}
