using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MatrixCheckView : BasicView
{
	private static List<Check<MetaTileMap>> checks = new List<Check<MetaTileMap>>();

	static MatrixCheckView()
	{
		checks.Add(new NonEmptyCheck());
		checks.Add(new PassableCheck());
		checks.Add(new AtmosPassableCheck());
		checks.Add(new SpaceCheck());
	}

	public override void DrawContent()
	{
		foreach (Check<MetaTileMap> check in checks)
		{
			check.Active = GUILayout.Toggle(check.Active, check.Label);
		}
	}

	[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
	private static void DrawGizmo(MetaTileMap source, GizmoType gizmoType)
	{
		GizmoUtils.DrawGizmos(source, checks);
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

	private class SpaceCheck : Check<MetaTileMap>
	{
		public override string Label { get; } = "Is Space";

		public override void DrawGizmo(MetaTileMap source, Vector3Int position)
		{
			if (source.IsSpaceAt(position))
			{
				GizmoUtils.DrawCube(position, Color.red);
			}
		}
	}
}