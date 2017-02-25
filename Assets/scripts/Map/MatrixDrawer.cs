using Matrix;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SectionEditor {

    public class Section {
        public string name;
        public Func<MatrixNode, bool> eval;
        public Color color = Color.blue;

        public Section(string name, Func<MatrixNode, bool> eval, Color color) {
            this.name = name;
            this.eval = eval;
            this.color = color;
        }
    }

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
        public List<Section> sections = new List<Section>();

        public static void AddSection(string name, Func<MatrixNode, bool> eval, Color color) {
            color.a = 0.5f;
            Instance.sections.Add(new Section(name, eval, color));
        }
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
                        foreach(Section section in scr.sections) {
                            if(section.eval(node)) {
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
