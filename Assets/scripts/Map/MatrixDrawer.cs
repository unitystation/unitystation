using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SectionEditor {

    public class MatrixDrawer: MonoBehaviour {
        private static MatrixDrawer instance;
        public static MatrixDrawer Instance {
            get {
                if(!instance) {
                    GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                    if(instanceTemp != null) {
                        instance = instanceTemp.GetComponentInChildren<MatrixDrawer>(true);
                    } else {
                        instance = null;
                    }
                }

                return instance;
            }
        }

        public bool drawGizmos;
    }

    public class MyScriptGizmoDrawer {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
        static void DrawGizmo(MatrixDrawer scr, GizmoType gizmoType) {
            if(!scr.drawGizmos)
                return;

            var start = Camera.current.ScreenToWorldPoint(Vector3.zero); // bottom left
            var end = Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth, Camera.current.pixelHeight));

            for(int y = Mathf.RoundToInt(start.y); y < Mathf.RoundToInt(end.y + 1); y++) {
                for(int x = Mathf.RoundToInt(start.x); x < Mathf.RoundToInt(end.x + 1); x++) {
                    var node = Matrix.Matrix.At(x, y, false);

                    if(node != null) {
                        foreach(Section section in SectionData.Sections) {
                            if(node.Section == section) {
                                Gizmos.color = section.color;
                                Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
                            }
                        }
                    }
                }
            }
        }
    }
}
