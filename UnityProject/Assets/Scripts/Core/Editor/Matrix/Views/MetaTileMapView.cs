using System.Collections.Generic;
using TileManagement;
using UnityEditor;
using UnityEngine;

public class MetaTileMapView : BasicView
{
	private static List<Check<MetaTileMap>> localChecks = new List<Check<MetaTileMap>>();
	private static List<Check<MatrixManager>> globalChecks = new List<Check<MatrixManager>>();

	static MetaTileMapView()
	{
		localChecks.Add(new NonEmptyCheck());
		localChecks.Add(new ObjectLayerCheck());
		localChecks.Add(new PassableCheck());
		localChecks.Add(new AtmosPassableCheck());
		localChecks.Add(new ShowLocalPositionsCheck());

		globalChecks.Add(new SpaceCheck());
		globalChecks.Add(new ShowGlobalPositionsCheck());
		globalChecks.Add(new ShowPositionsCheck());
		globalChecks.Add(new AtPointCheck());
		globalChecks.Add(new StickyClientCheck());
	}

	public override void DrawContent()
	{
		for (int i = 0; i < localChecks.Count; i++)
		{
			Check<MetaTileMap> check = localChecks[i];
			check.Active = GUILayout.Toggle(check.Active, check.Label);
		}

		for (int i = 0; i < globalChecks.Count; i++)
		{
			Check<MatrixManager> check = globalChecks[i];
			check.Active = GUILayout.Toggle(check.Active, check.Label);
		}
	}

	[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
	private static void DrawGizmoLocal(MetaTileMap source, GizmoType gizmoType)
	{
		GizmoUtils.DrawGizmos(source, localChecks);
	}

	[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
	private static void DrawGizmoGlobal(MatrixManager source, GizmoType gizmoType)
	{
		GizmoUtils.DrawGizmos(source, globalChecks, false);
	}

	private class NonEmptyCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Not Empty";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (!source.IsEmptyAt(position, false))
			{
				GizmoUtils.DrawCube(position, Color.green);
			}
		}
	}
	private class ObjectLayerCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Object Layer Tiles";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (source.HasObject(position, CustomNetworkManager.Instance._isServer))
			{
				GizmoUtils.DrawCube(position, Color.magenta);
			}
		}
	}

	private class AtmosPassableCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Not AtmosPassable";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (!source.IsAtmosPassableAt(position, false))
			{
				GizmoUtils.DrawCube(position, Color.blue);
			}
		}
	}

	private class PassableCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Not Passable";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (source.IsPassableAtOneTileMap(position, position, false) == false)
			{
				GizmoUtils.DrawCube(position, Color.blue);
			}
		}
	}

	private class SpaceCheck : Check<MatrixManager>
	{
		public override string Label { get; } = "Space";

		public override void DrawGizmo(MatrixManager source, Vector3Int position)
		{
			if (MatrixManager.IsSpaceAt(position, false))
			{
				GizmoUtils.DrawCube(position, Color.red, false);
			}
		}
	}
	private class StickyClientCheck : Check<MatrixManager>
	{
		public override string Label { get; } = "Sticky";

		public override void DrawGizmo(MatrixManager source, Vector3Int position)
		{
			if (!MatrixManager.IsNonStickyAt(position, false))
			{
				GizmoUtils.DrawCube(position, Color.yellow, false);
			}
		}
	}

	private class ShowLocalPositionsCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Local Positions";

		public override void DrawLabel(MetaTileMap source, Vector3Int position)
		{
			if (!source.IsEmptyAt(position, false))
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{position.x}, {position.y}", p, false);
			}
		}
	}

	private class ShowGlobalPositionsCheck : Check<MatrixManager>
	{
		public override string Label { get; } = "Global Positions";

		public override void DrawLabel(MatrixManager source, Vector3Int position)
		{
			GizmoUtils.DrawText($"{position.x}, {position.y}", position, false);
		}
	}

	private class ShowPositionsCheck : Check<MatrixManager>
	{
		public override string Label { get; } = "Positions";

		public override void DrawLabel(MatrixManager source, Vector3Int position)
		{
			if (!MatrixManager.IsSpaceAt(position, false))
			{
				MatrixInfo matrix = MatrixManager.AtPoint(position, false);
				Vector3 localPosition = MatrixManager.WorldToLocal(position, matrix);

				GizmoUtils.DrawText($"{localPosition.x}, {localPosition.y}", position, false);
			}
			else
			{
				GizmoUtils.DrawText($"{position.x}, {position.y}", position, Color.gray, false);
			}
		}
	}

	private class AtPointCheck : Check<MatrixManager>
	{
		public override string Label { get; } = "Matrix ID At Point";

		public override void DrawLabel(MatrixManager source, Vector3Int position)
		{
			if (!MatrixManager.IsSpaceAt(position, false))
			{
				MatrixInfo matrix = MatrixManager.AtPoint(position, false);

				GizmoUtils.DrawText($"{matrix.Id}", position, false);
			}
		}
	}
}