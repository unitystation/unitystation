using System;
using System.Collections.Generic;
using System.Globalization;
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
		private static bool passable, atmosPassable;

		private static bool north;
		private static bool south;

		private static bool space;

		private static bool corners;
		private static bool rooms;
		private static bool atmos;
		private static bool positions;

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
			passable = GUILayout.Toggle(passable, "Passable");
			atmosPassable = GUILayout.Toggle(atmosPassable, "Atmos Passable");
			north = GUILayout.Toggle(north, "From North");
			south = GUILayout.Toggle(south, "From South");
			space = GUILayout.Toggle(space, "Is Space");
			corners = GUILayout.Toggle(corners, "Show Corners");
			rooms = GUILayout.Toggle(rooms, "Show Rooms");
			atmos = GUILayout.Toggle(atmos, "Show Atmos");
			positions = GUILayout.Toggle(positions, "Show Positions");

			if (currentSceneView)
			{
				currentSceneView.Repaint();
			}
		}

		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmo2(MetaDataLayer scr, GizmoType gizmoType)
		{
			Vector3Int start = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(Vector3.one * -32) - scr.transform.position); // bottom left
			Vector3Int end =
				Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) -
				                      scr.transform.position);
			start.z = 0;
			end.z = 1;

			float camDistance = end.y - start.y;

			if (camDistance > 100)
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
			green.a = 0.5f;

			foreach (Vector3Int position in new BoundsInt(start, end - start).allPositionsWithin)
			{
				MetaDataNode node = scr.Get(position, false);


				Vector3 centerPosition = position + new Vector3(0.5f, 0.5f, 0);

				if (rooms)
				{
					if (node.IsRoom)
					{
						Gizmos.color = blue;
						Gizmos.DrawCube(centerPosition, Vector3.one);
					}

					if (node.IsSpace)
					{
						Gizmos.color = red;
						Gizmos.DrawCube(centerPosition, Vector3.one);
					}
				}

				if (atmos)
				{
					if (node.Exists)
					{

						if (node.atmosEdge)
						{
							Gizmos.color = red;
							Gizmos.DrawCube(centerPosition, Vector3.one);
						}

						if (node.updating)
						{
							Gizmos.color = green;
							Gizmos.DrawCube(centerPosition, Vector3.one);
						}
						
						if (camDistance < 10f)
						{
							Handles.Label(position + Vector3.one, $"{node.Pressure:0.###}");
						}
					}
				}

				if (positions && camDistance < 10f)
				{
					Handles.Label(centerPosition + new Vector3(0, 1f, 0), position.x + "," + position.y);
				}
			}
		}

		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmo(MetaTileMap scr, GizmoType gizmoType)
		{
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
						else if (atmosPassable)
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
}