using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SectionEditor
{

    public class SectionDrawer : MonoBehaviour
    {
        private static SectionDrawer instance;
        public static SectionDrawer Instance
        {
            get
            {
                if (!instance)
                {
                    GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                    if (instanceTemp != null)
                    {
                        instance = instanceTemp.GetComponentInChildren<SectionDrawer>(true);
                    }
                    else
                    {
                        instance = null;
                    }
                }

                return instance;
            }
        }

        public static bool DrawGizmos;
    }
#if UNITY_EDITOR
    public class GizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
        static void DrawGizmo(SectionDrawer scr, GizmoType gizmoType)
        {
            if (!SectionDrawer.DrawGizmos)
                return;

            var start = Camera.current.ScreenToWorldPoint(Vector3.zero); // bottom left
            var end = Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth, Camera.current.pixelHeight));

            for (int y = Mathf.RoundToInt(start.y); y < Mathf.RoundToInt(end.y + 1); y++)
            {
                //                for(int x = Mathf.RoundToInt(start.x); x < Mathf.RoundToInt(end.x + 1); x++) {
                //                    var node = MatrixOld.Matrix.At(x, y, false);
                //
                //                    if(node != null) {
                //                        foreach(Section section in SectionData.Sections) {
                //                            if(node.Section == section) {
                //                                Gizmos.color = section.color;
                //                                Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
                //                            }
                //                        }
                //                    }
                //                }
            }
        }

    }
#endif
}
