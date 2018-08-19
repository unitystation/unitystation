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

		globalChecks.Add(new SpaceCheck());
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
		public override string Label { get; } = "Is Space";

		public override void DrawGizmo(MatrixManager source, Vector3Int position)
		{
			if (MatrixManager.IsSpaceAt(position))
			{
				GizmoUtils.DrawCube(position, Color.red, false);
			}
		}
	}
}