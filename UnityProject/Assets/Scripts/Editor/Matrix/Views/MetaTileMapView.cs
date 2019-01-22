using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MetaTileMapView : BasicView
{
	private static List<Check<MetaTileMap>> localChecks = new List<Check<MetaTileMap>>();
	private static List<Check<MatrixManager>> globalChecks = new List<Check<MatrixManager>>();

	static MetaTileMapView()
	{
		localChecks.Add(new NonEmptyCheck());
		localChecks.Add(new PassableCheck());
		localChecks.Add(new AtmosPassableCheck());
		localChecks.Add(new ShowLocalPositionsCheck());

		globalChecks.Add(new SpaceCheck());
		globalChecks.Add(new ShowGlobalPositionsCheck());
		globalChecks.Add(new ShowPositionsCheck());
	}

	public override void DrawContent()
	{
		foreach (Check<MetaTileMap> check in localChecks)
		{
			check.Active = GUILayout.Toggle(check.Active, check.Label);
		}

		foreach (Check<MatrixManager> check in globalChecks)
		{
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
			if (!source.IsEmptyAt(position))
			{
				GizmoUtils.DrawCube(position, Color.green);
			}
		}
	}

	private class AtmosPassableCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Not AtmosPassable";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (!source.IsAtmosPassableAt(position))
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
			if (!source.IsPassableAt(position))
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
			if (MatrixManager.IsSpaceAt(position))
			{
				GizmoUtils.DrawCube(position, Color.red, false);
			}
		}
	}

	private class ShowLocalPositionsCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Local Positions";

		public override void DrawLabel(MetaTileMap source, Vector3Int position)
		{
			if (!source.IsEmptyAt(position))
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
			if (!MatrixManager.IsSpaceAt(position))
			{
				MatrixInfo matrix = MatrixManager.AtPoint(position);
				Vector3 localPosition = MatrixManager.WorldToLocal(position, matrix);

				GizmoUtils.DrawText($"{localPosition.x}, {localPosition.y}", position, false);
			}
			else
			{
				GizmoUtils.DrawText($"{position.x}, {position.y}", position, Color.gray, false);
			}
		}
	}
}