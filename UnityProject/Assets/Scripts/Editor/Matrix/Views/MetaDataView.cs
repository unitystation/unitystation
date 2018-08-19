using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MetaDataView : BasicView
{
	private static List<Check<MetaDataLayer>> localChecks = new List<Check<MetaDataLayer>>();

	static MetaDataView()
	{
		localChecks.Add(new RoomCheck());
		localChecks.Add(new PressureCheck());
		localChecks.Add(new ExistCheck());
		localChecks.Add(new WallCheck());
		localChecks.Add(new NeighborCheck());
	}

	public override void DrawContent()
	{
		foreach (Check<MetaDataLayer> check in localChecks)
		{
			check.Active = GUILayout.Toggle(check.Active, check.Label);
		}
	}

	[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
	private static void DrawGizmoLocal(MetaDataLayer source, GizmoType gizmoType)
	{
		GizmoUtils.DrawGizmos(source, localChecks);
	}

	private class RoomCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Rooms";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			if (source.IsRoomAt(position))
			{
				GizmoUtils.DrawCube(position, Color.green);
			}
		}
	}

	private class ExistCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Exists";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			if (!source.IsEmptyAt(position))
			{
				GizmoUtils.DrawCube(position,  Color.green);
			}
		}
	}

	private class WallCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Wall";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			if (source.IsOccupiedAt(position))
			{
				GizmoUtils.DrawCube(position,  Color.blue);
			}
		}
	}

	private class NeighborCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Neighbors";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Neighbors.Count > 0)
			{
				GizmoUtils.DrawCube(position,  Color.blue, 0.25f * node.Neighbors.Count);
			}
		}
	}

	private class PressureCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Pressure";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				GizmoUtils.DrawCube(position, Color.blue, node.Atmos.Pressure / 200);
			}
		}

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				GizmoUtils.DrawText($"{node.Atmos.Pressure:0.###}", position);
			}
		}
	}
}