using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
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
		localChecks.Add(new SolidTemperatureCheck());
		localChecks.Add(new MolesCheck());
		localChecks.Add(new VolumeCheck());
		localChecks.Add(new ExistCheck());
		localChecks.Add(new OccupiedCheck());
		localChecks.Add(new SpaceCheck());
		localChecks.Add(new NeighborCheck());
		localChecks.Add(new SpaceConnectCheck());
		localChecks.Add(new HotspotCheck());
		localChecks.Add(new WindCheck());
		localChecks.Add(new NumberOfGasesCheck());
		localChecks.Add(new PlasmaCheck());
		localChecks.Add(new OxygenCheck());
		localChecks.Add(new NitrogenCheck());
		localChecks.Add(new CarbonDioxideCheck());
		localChecks.Add(new RoomNumberCheck());
		localChecks.Add(new AirlockCheck());
		localChecks.Add(new SlipperyCheck());
		localChecks.Add(new AtmosUpdateCheck());
		localChecks.Add(new ThermalConductivity());
		localChecks.Add(new HeatCapacity());
	}

	public override void DrawContent()
	{
		for (var i = 0; i < localChecks.Count; i++)
		{
			Check<MetaDataLayer> check = localChecks[i];
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
				if (node.IsSpace || node.Neighbors.Any(n => n != null && n.IsSpace))
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
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{neighborCount}", p, false);
			}
		}

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);
			foreach (MetaDataNode neighbor in node.Neighbors)
			{
				if (neighbor != null)
				{
					Vector3 p2 = LocalToWorld(neighbor.ReactionManager, neighbor.Position);

					p2 = WorldToLocal(source, p2);

					GizmoUtils.DrawRay(position, p2 - position);
				}
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
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.Pressure:0.###}", p, false);
			}
		}
	}

	private class TemperatureCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Gas Temperature";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{(node.GasMix.Temperature):0.##}", p, false);
			}
		}
	}

	private class SolidTemperatureCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Solid Temperature";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);

				p.y -= 0.2f;
				GizmoUtils.DrawText($"{(node.ConductivityTemperature):0.##}", p, false);
			}
		}
	}

	private class MolesCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Total Moles";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.Moles:0.###}", p, false);
			}
		}
	}

	private class VolumeCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Volume";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.Volume:0.###}", p, false);
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
	private class WindCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Space Wind";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);
			if (node.HasWind)
			{
				var alpha = Mathf.Clamp(node.WindForce / 20, 0.1f, 0.8f);
				GizmoUtils.DrawCube( position, Color.blue, true, alpha );

				Gizmos.color = Color.white;
				GizmoUtils.DrawText($"{node.WindForce:0.#}", LocalToWorld(source, position) + (Vector3)Vector2.down/4, false);

				GizmoUtils.DrawArrow( position, (Vector2)node.WindDirection/2 );
			}
		}
	}

	private class AtmosUpdateCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Atmos Update";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);
			if (AtmosThread.IsInUpdateList(node))
			{
				GizmoUtils.DrawCube( position, Color.blue, true );
			}
		}
	}

	private class ThermalConductivity : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Thermal Conductivity";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.ThermalConductivity:0.###}", p, false);
			}
		}
	}

	private class HeatCapacity : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Heat Capacity";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.HeatCapacity:0.###}", p, false);
			}
		}
	}

	private class AirlockCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Closed Airlock";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);
			if (node.IsIsolatedNode)
			{
				GizmoUtils.DrawCube( position, Color.blue, true );
			}
		}
	}
	private class SlipperyCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Slippery";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);
			if (node.IsSlippery)
			{
				GizmoUtils.DrawCube( position, Color.cyan, true );
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
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.GetMoles(Gas.Plasma):0.###}", p, false);
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
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.GetMoles(Gas.Oxygen):0.###}", p, false);
			}
		}
	}

	private class NitrogenCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Nitrogen";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.GetMoles(Gas.Nitrogen):0.###}", p, false);
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
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.GetMoles(Gas.CarbonDioxide):0.###}", p, false);
			}
		}
	}

	private class NumberOfGasesCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Number Of Gases On Tile";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.GasMix.GasesArray.Length}", p, false);
			}
		}
	}

	private class RoomNumberCheck : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Room Number";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.IsRoom)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.RoomNumber}", p, false);
			}
		}
	}

	private static Vector3 LocalToWorld(Component source, Vector3 position)
	{
		return MatrixManager.LocalToWorld(position, MatrixManager.Get(source.GetComponent<Matrix>()));
	}

	private static Vector3 WorldToLocal(Component source, Vector3 position)
	{
		return MatrixManager.WorldToLocal(position, MatrixManager.Get(source.GetComponent<Matrix>()));
	}
}
