using System.Collections.Generic;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
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

        private static bool corners;
        private static bool room;

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
            corners = GUILayout.Toggle(corners, "Show Corners");
            room = GUILayout.Toggle(room, "Show Room");

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

            var green = Color.green;
            red.a = 0.5f;

            if (room)
            {
                DrawRoom(scr);
            }
            else
            {
                foreach (var position in new BoundsInt(start, end - start).allPositionsWithin)
                {
                    if (space)
                    {
                        if (scr.IsSpaceAt(position))
                        {
                            Gizmos.color = red;
                            Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                        }
                    }
                    else
                    {
                        if (corners)
                        {
                            if (scr.HasTile(position, LayerType.Walls))
                            {
                                Gizmos.color = green;

                                var corner_count = 0;
                                foreach (var pos in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right, Vector3Int.up})
                                {
                                    if (!scr.HasTile(position + pos, LayerType.Walls))
                                    {
                                        corner_count++;
                                    }
                                    else
                                    {
                                        corner_count = 0;
                                    }

                                    if (corner_count > 1)
                                    {
                                        Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                                        break;
                                    }
                                }
                            }
                        }
                        else
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
                    }
                }
            }
        }

        
        private static List<HashSet<Vector3Int>> rooms = new List<HashSet<Vector3Int>>();
        
        private static HashSet<Vector3Int> currentRoom;

        private static void DrawRoom(MetaTileMap metaTileMap)
        {
            var mousePos = Vector3Int.RoundToInt(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin);
            mousePos -= Vector3Int.one;
            mousePos.z = 0;

            if (currentRoom == null || !currentRoom.Contains(mousePos))
            {
                currentRoom = rooms.Find(x => x.Contains(mousePos));

                if (currentRoom == null)
                {
                    if (metaTileMap.IsAtmosPassableAt(mousePos) && !metaTileMap.IsSpaceAt(mousePos))
                    {
                        currentRoom = new HashSet<Vector3Int>();

                        var posToCheck = new Queue<Vector3Int>();
                        posToCheck.Enqueue(mousePos);
    
                        while (posToCheck.Count > 0)
                        {
                            var pos = posToCheck.Dequeue();
                            currentRoom.Add(pos);
    
                            foreach (var dir in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right})
                            {
                                var neighbor = pos + dir;
    
                                if (!posToCheck.Contains(neighbor) && !currentRoom.Contains(neighbor))
                                {
                                    if (metaTileMap.IsSpaceAt(neighbor))
                                    {
                                        currentRoom.Clear();
                                        posToCheck.Clear();
                                        break;
                                    }
                                    
                                    if (metaTileMap.IsAtmosPassableAt(neighbor))
                                    {
                                        posToCheck.Enqueue(neighbor);
                                    }
                                }
                            }
                        }
                    
                        rooms.Add(currentRoom);
                    }
                }
            }

            if (currentRoom != null)
            {
                var color = Color.cyan;
                color.a = 0.5f;
                Gizmos.color = color;

                foreach (var pos in currentRoom)
                {
                    Gizmos.DrawCube(pos + new Vector3(0.5f, 0.5f, 0), Vector3.one);
                }
            }
        }
    }
}
