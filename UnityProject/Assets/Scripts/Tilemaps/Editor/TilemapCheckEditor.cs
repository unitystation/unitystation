using System;
using System.Collections.Generic;
using System.Globalization;
using Tilemaps.Behaviours;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Behaviours.Meta.Data;
using Tilemaps.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tilemaps.Editor
{
	public class TilemapCheckEditor : EditorWindow
	{
		private static bool passable, atmosPassable;

		private static bool space;
		private static bool spaceMatrix;

		private static bool corners;
		private static bool rooms;
		private static bool atmos;
		private static bool pressure;
		private static bool positions;

		private static bool nodes;
		private static bool neighbors;

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
			space = GUILayout.Toggle(space, "Is Space");
			spaceMatrix = GUILayout.Toggle(spaceMatrix, "Is Space (Matrix)");
			corners = GUILayout.Toggle(corners, "Show Corners");
			rooms = GUILayout.Toggle(rooms, "Show Rooms");
			atmos = GUILayout.Toggle(atmos, "Show Atmos");
			pressure = GUILayout.Toggle(pressure, "Show Pressure");
			positions = GUILayout.Toggle(positions, "Show Positions");
			nodes = GUILayout.Toggle(nodes, "Show Nodes");
			neighbors = GUILayout.Toggle(neighbors, "Neighbors Node");

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
				Vector3Int.RoundToInt(
					Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) -
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

			foreach (Vector3Int position in new BoundsInt(start, end - start).allPositionsWithin)
			{
				MetaDataNode node = scr.Get(position, false);

				if (rooms)
				{
					if (node.IsRoom)
					{
						DrawCube(position, Color.blue);
					}

					if (node.IsSpace)
					{
						DrawCube(position, Color.red);
					}
				}

				if (atmos)
				{
					if (node.Exists)
					{
						if (node.Atmos.State != AtmosState.None)
						{
							DrawCube(position, node.Atmos.State == AtmosState.Updating ? Color.green : Color.red);
						}

						if (camDistance < 10f)
						{
							Handles.Label(position + Vector3.one, $"{node.Atmos.Pressure:0.###}");
						}
					}
				}

				if (pressure)
				{
					if (node.Exists)
					{
						DrawCube(position, Color.blue, node.Atmos.Pressure / 200);

						if (camDistance < 10f)
						{
							Handles.Label(position + Vector3.one, $"{node.Atmos.Pressure:0.###}");
						}
					}
				}

				if (positions && camDistance < 10f)
				{
					Handles.Label(position + new Vector3(0.5f, 1.5f, 0), position.x + "," + position.y);
				}

				if (nodes)
				{
					if (node.Exists)
					{
						DrawCube(position, Color.green);
					}
				}

				if (neighbors)
				{
					if (node.Exists)
					{
						DrawCube(position, Color.blue, node.Neighbors.Count / 6f);

						if (camDistance < 10f)
						{
							Handles.Label(position + Vector3.one, $"{node.Neighbors.Count}");
						}
					}
				}
			}
		}

		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmo(MetaTileMap scr, GizmoType gizmoType)
		{
			Vector3Int start = Vector3Int.RoundToInt(Camera.current.ScreenToWorldPoint(Vector3.one * -32) - scr.transform.position); // bottom left
			Vector3Int end =
				Vector3Int.RoundToInt(
					Camera.current.ScreenToWorldPoint(new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32)) -
					scr.transform.position);
			start.z = 0;
			end.z = 1;


			if (end.y - start.y > 100)
			{
				// avoid being zoomed out too much (creates too many objects)
				return;
			}

			Gizmos.matrix = scr.transform.localToWorldMatrix;

			foreach (Vector3Int position in new BoundsInt(start, end - start).allPositionsWithin)
			{
				if (!scr.GetBounds().Contains(position))
					continue;

				if (space)
				{
					if (scr.IsSpaceAt(position))
					{
						DrawCube(position, Color.red);
					}
				}
				else
				{
					if (passable)
					{
						if (!scr.IsPassableAt(position))
						{
							DrawCube(position, Color.blue);
						}
					}
					else if (atmosPassable)
					{
						if (!scr.IsAtmosPassableAt(position))
						{
							DrawCube(position, Color.blue);
						}
					}
					else if (spaceMatrix)
					{
						if (scr.IsSpaceAt(position))
						{
							DrawCube(position, Color.red);
						}
					}
				}
			}
		}

		private static void DrawCube(Vector3 position, Color color, float alpha = 0.5f)
		{
			color.a = alpha;
			Gizmos.color = color;
			Gizmos.DrawCube(position + new Vector3(0.5f, 0.5f, 0), Vector3.one);
		}
	}
}