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
		localChecks.Add(new TemperatureCheck());
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

			int neighborCount = node.GetNeighbors().Length;

			if (neighborCount > 0)
			{
				GizmoUtils.DrawCube(position,  Color.blue, alpha:0.25f * neighborCount);
			}
		}

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			int neighborCount = node.GetNeighbors().Length;

			if (neighborCount > 0)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{neighborCount}", p, false);
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
				GizmoUtils.DrawCube(position, Color.blue, alpha:node.Atmos.Pressure / 200);
			}
		}

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.Pressure:0.###}", p, false);
			}
		}
	}

	private class TemperatureCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Temperature";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
//				GizmoUtils.DrawCube(position, Color.blue, alpha:node.Atmos.Temperature / 200);
			}
		}

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.Temperature:0.###}", p, false);
			}
		}
	}
}