using System.Collections.Generic;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEditor;
using UnityEngine;

namespace Tilemaps.Editor
{
    public class TilemapCheckEditor : EditorWindow
    {
        private static bool DrawGizmos;

        private static bool passable;

        private static bool north;
        private static bool south;

        private static bool space;

        private SceneView currentSceneView;

        [MenuItem("Window/Tilemap Check")]
        public static void ShowWindow()
        {
            GetWindow<TilemapCheckEditor>("Tilemap Check");
        }

        public void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        public void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            currentSceneView = sceneView;
        }

        void OnGUI()
        {
            DrawGizmos = GUILayout.Toggle(DrawGizmos, "Draw Gizmos");
            passable = GUILayout.Toggle(passable, "Passable");
            passable = !GUILayout.Toggle(!passable, "Atmos Passable");
            north = GUILayout.Toggle(north, "From North");
            south = GUILayout.Toggle(south, "From South");
            space = GUILayout.Toggle(space, "Is Space");

            if (currentSceneView)
                currentSceneView.Repaint();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
        static void DrawGizmo(MetaTileMap scr, GizmoType gizmoType)
        {
            if (!DrawGizmos)
                return;

            var start = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(Vector3.one * -32) - scr.transform.position); // bottom left
            var end = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) - scr.transform.position);
            start.z = 0;
            end.z = 1;


            if (end.y - start.y > 100)
            {
                // avoid being zoomed out too much (creates too many objects)
                return;
            }

            Gizmos.matrix = scr.transform.localToWorldMatrix;


            var blue = Color.blue;
            blue.a = 0.5f;

            var red = Color.red;
            red.a = 0.5f;

            foreach (var position in new BoundsInt(start, end - start).allPositionsWithin)
            {
                if (!space)
                {
                    Gizmos.color = blue;
                    if (passable)
                    {
                        if (north)
                        {
                            if (!scr.IsPassableAt(position + Vector3Int.up, position))
                            {
                                Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                            }
                        }
                        else if (south)
                        {
                            if (!scr.IsPassableAt(position + Vector3Int.down, position))
                            {
                                Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                            }
                        }
                        else if (!scr.IsPassableAt(position))
                        {
                            Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                        }
                    }
                    else
                    {
                        if (!scr.IsAtmosPassableAt(position))
                        {
                            Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                        }
                    }
                }
                else
                {
                    if (scr.IsSpaceAt(position))
                    {
                        Gizmos.color = red;
                        Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                    }
                }
            }
        }
    }
}