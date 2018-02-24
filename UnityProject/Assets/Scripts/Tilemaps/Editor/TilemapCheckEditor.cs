﻿using System.Collections.Generic;
using Tilemaps.Behaviours;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Tiles;
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


		private static readonly List<HashSet<Vector3Int>> rooms = new List<HashSet<Vector3Int>>();

		private static HashSet<Vector3Int> currentRoom;

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

		private void OnSceneGUI(SceneView sceneView)
		{
			currentSceneView = sceneView;
		}

		private void OnGUI()
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
			{
				currentSceneView.Repaint();
			}
		}

		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmo2(MetaDataLayer scr, GizmoType gizmoType)
		{
			if (!DrawGizmos)
			{
				return;
			}
			
			Vector3Int start = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(Vector3.one * -32) - scr.transform.position); // bottom left
			Vector3Int end =
				Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) -
				                      scr.transform.position);
			start.z = 0;
			end.z = 1;
			
			if (end.y - start.y > 100)
			{
				// avoid being zoomed out too much (creates too many objects)
				return;
			}
			
			Gizmos.matrix = scr.transform.localToWorldMatrix;
			
			Color blue = Color.blue;
			blue.a = 0.5f;

			Color red = Color.red;
			red.a = 0.5f;
			
			foreach (Vector3Int position in new BoundsInt(start, end - start).allPositionsWithin)
			{
				MetaDataNode node = scr.Get(position, false);
				if (node != null)
				{
					if(node.Room > 0)
					{
						Gizmos.color = blue;
					}

					if (node.Room < 0)
					{
						Gizmos.color = red;
					}
					
					Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
				}
				
			}

		}

		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmo(MetaTileMap scr, GizmoType gizmoType)
		{
			if (!DrawGizmos)
			{
				return;
			}

			Vector3Int start = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(Vector3.one * -32) - scr.transform.position); // bottom left
			Vector3Int end =
				Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) -
				                      scr.transform.position);
			start.z = 0;
			end.z = 1;


			if (end.y - start.y > 100)
			{
				// avoid being zoomed out too much (creates too many objects)
				return;
			}

			Gizmos.matrix = scr.transform.localToWorldMatrix;


			Color blue = Color.blue;
			blue.a = 0.5f;

			Color red = Color.red;
			red.a = 0.5f;

			Color green = Color.green;
			red.a = 0.5f;

			if (room)
			{
				DrawRoom(scr);
			}
			else
			{
				foreach (Vector3Int position in new BoundsInt(start, end - start).allPositionsWithin)
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

								int corner_count = 0;
								foreach (Vector3Int pos in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right, Vector3Int.up})
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

		private static void DrawRoom(MetaTileMap metaTileMap)
		{
			Vector3Int mousePos = Vector3Int.RoundToInt(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin);
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

						Queue<Vector3Int> posToCheck = new Queue<Vector3Int>();
						posToCheck.Enqueue(mousePos);

						while (posToCheck.Count > 0)
						{
							Vector3Int pos = posToCheck.Dequeue();
							currentRoom.Add(pos);

							foreach (Vector3Int dir in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right})
							{
								Vector3Int neighbor = pos + dir;

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
				Color color = Color.cyan;
				color.a = 0.5f;
				Gizmos.color = color;

				foreach (Vector3Int pos in currentRoom)
				{
					Gizmos.DrawCube(pos + new Vector3(0.5f, 0.5f, 0), Vector3.one);
				}
			}
		}
	}
}