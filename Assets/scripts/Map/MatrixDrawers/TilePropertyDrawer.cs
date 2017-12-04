using MatrixOld;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Property {
    public string Name { get; private set; }
    public Func<MatrixNode, bool> Eval { get; private set; }
    public bool IgnoreNull { get; private set; }

    public bool Negate { get; set; }

    public Property(string name, Func<MatrixNode, bool> eval, bool ignoreNull = true) {
        Name = name;
        Eval = eval;
        Negate = false;
        IgnoreNull = ignoreNull;
    }
}

public class TilePropertyDrawer: MonoBehaviour {
    private static TilePropertyDrawer instance;
    public static TilePropertyDrawer Instance {
        get {
            if(!instance) {
                GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                if(instanceTemp != null) {
                    instance = instanceTemp.GetComponentInChildren<TilePropertyDrawer>(true);
                } else {
                    instance = null;
                }
            }

            return instance;
        }
    }

    static TilePropertyDrawer() {
        Properties.Clear();
        Properties.Add(new Property("Space", node => node == null || node.IsSpace(), false));
        Properties.Add(new Property("Passable", node => node.IsPassable()));
        Properties.Add(new Property("Atmos Passable", node => node.IsAtmosPassable()));
    }

    public static List<Property> Properties = new List<Property>();

    public static Property Selected = null;

    public static bool DrawGizmos;
}

#if UNITY_EDITOR
public class TilePropertyGizmoDrawer {
    private static Color color;

    static TilePropertyGizmoDrawer() {
        color = Color.blue;
        color.a = 0.5f;
    }

    [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
    static void DrawGizmo(TilePropertyDrawer scr, GizmoType gizmoType) {
        if(!TilePropertyDrawer.DrawGizmos || TilePropertyDrawer.Selected == null)
            return;

        var start = Camera.current.ScreenToWorldPoint(Vector3.zero); // bottom left
        var end = Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth, Camera.current.pixelHeight));

        for(int y = Mathf.RoundToInt(start.y); y < Mathf.RoundToInt(end.y + 1); y++) {
            for(int x = Mathf.RoundToInt(start.x); x < Mathf.RoundToInt(end.x + 1); x++) {
//                var node = MatrixOld.Matrix.At(x, y, false);
//
//                if(node != null || !TilePropertyDrawer.Selected.IgnoreNull) {
//                    var eval = TilePropertyDrawer.Selected.Eval(node);
//                    var negate = TilePropertyDrawer.Selected.Negate;
//                    if(!negate && eval || negate && !eval) {
//                        Gizmos.color = color;
//                        Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
//                    }
//                }
            }
        }
    }
}
#endif