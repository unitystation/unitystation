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

        var start = Camera.current.ScreenToWorldPoint(Vector3.zero); // bottom left
        var end = Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth, Camera.current.pixelHeight));

        for(int y = Mathf.RoundToInt(start.y); y < Mathf.RoundToInt(end.y + 1); y++) {
            for(int x = Mathf.RoundToInt(start.x); x < Mathf.RoundToInt(end.x + 1); x++) {
                var node = Matrix.Matrix.At(x, y, false);
                if(node != null && !node.IsPassable())
                    Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
            }
        }
    }
}
