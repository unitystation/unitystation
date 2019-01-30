using System.Collections.Generic;
using System.Linq;
using Atmospherics;
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
		localChecks.Add(new MolesCheck());
		localChecks.Add(new ExistCheck());
		localChecks.Add(new OccupiedCheck());
		localChecks.Add(new SpaceCheck());
		localChecks.Add(new NeighborCheck());
		localChecks.Add(new SpaceConnectCheck());
		localChecks.Add(new HotspotCheck());
		localChecks.Add(new PlasmaCheck());
		localChecks.Add(new OxygenCheck());
		localChecks.Add(new CarbonDioxideCheck());
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

	private class SpaceCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Space";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			if (source.ExistsAt(position) && source.IsSpaceAt(position))
			{
				GizmoUtils.DrawCube(position, Color.red);
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

	private class OccupiedCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Occupied";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			if (source.IsOccupiedAt(position))
			{
				GizmoUtils.DrawCube(position,  Color.blue);
			}
		}
	}

	private class SpaceConnectCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Connected to Space";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				if (node.IsSpace || node.Neighbors.Any(n => n.IsSpace))
				{
					GizmoUtils.DrawCube(position,  Color.red);
				}
			}
		}
	}

	private class NeighborCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Neighbors";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			int neighborCount = node.NeighborCount;

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

	private class MolesCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Moles";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.Moles:0.###}", p, false);
			}
		}
	}

	private class HotspotCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Hotspots";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.HasHotspot)
			{
				GizmoUtils.DrawWireCube(position, Color.red, size:0.85f);
			}
		}
	}

	private class PlasmaCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Plasma";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.GetMoles(Gas.Plasma):0.###}", p, false);
			}
		}
	}

	private class OxygenCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Oxygen";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.GetMoles(Gas.Oxygen):0.###}", p, false);
			}
		}
	}

	private class CarbonDioxideCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "CarbonDioxide";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = source.transform.TransformPoint(position) + GizmoUtils.HalfOne;
				GizmoUtils.DrawText($"{node.Atmos.GetMoles(Gas.CarbonDioxide):0.###}", p, false);
			}
		}
	}
}
