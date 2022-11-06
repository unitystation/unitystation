using System.Collections.Generic;
using System.Linq;
using Shared.Editor;
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
		localChecks.Add(new RadiationLevel());
		localChecks.Add(new ElectricityVision());
		localChecks.Add(new AtmosIsOccupied());
		localChecks.Add(new HasSmoke());
	}

	public override void DrawContent()
	{
		foreach (var check in localChecks)
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

			if (Application.isPlaying == false)
			{
				if (source.ExistsAt(position) && source.IsSpaceAt(position))
				{
					GizmoUtils.DrawCube(position, Color.red);
				}
				return;
			}

			var INWorld = MatrixManager.LocalToWorld(position, source.Matrix.MatrixInfo);
			if (MatrixManager.AtPoint(INWorld.RoundToInt(), true).Matrix == source.Matrix)
			{
				if (source.ExistsAt(position) && source.IsSpaceAt(position))
				{
					GizmoUtils.DrawCube(position, Color.red);
				}
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

			if (Application.isPlaying == false)
			{
				MetaDataNode node = source.Get(position, false);

				if (node.Exists)
				{
					if (node.IsSpace || node.Neighbors.Any(n => n != null && n.IsSpace))
					{
						GizmoUtils.DrawCube(position, Color.red);
					}
				}
			}
			else
			{
				var INWorld = MatrixManager.LocalToWorld(position, source.Matrix.MatrixInfo);
				if (MatrixManager.AtPoint(INWorld.RoundToInt(), true).Matrix == source.Matrix)
				{
					MetaDataNode node = source.Get(position, false);
					if (node.Exists)
					{
						if (node.IsSpace || node.Neighbors.Any(n => n != null && n.IsSpace))
						{
							GizmoUtils.DrawCube(position, Color.red);
						}
					}
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
					Vector3 p2 = LocalToWorld(neighbor.ReactionManager, neighbor.LocalPosition);

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
			if (AtmosManager.Instance.simulation.IsInUpdateList(node))
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


		private class RadiationLevel : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Radiation level";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				GizmoUtils.DrawCube(position, Color.green, alpha:node.RadiationNode.RadiationLevel / 1000);
			}
		}

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				Vector3 p = LocalToWorld(source, position);
				GizmoUtils.DrawText($"{node.RadiationNode.RadiationLevel}", p, false);
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
				GizmoUtils.DrawText($"{node.GasMix.GasesArray.Count}", p, false);
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


	private class HasSmoke : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Smoke";

		public override void DrawLabel(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.SmokeNode.IsActive)
			{
				GizmoUtils.DrawCube(position, Color.gray);
			}
		}
	}

	private class AtmosIsOccupied : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Occupied Directions";

		public override void DrawLabel(MetaDataLayer source, Vector3Int positionInt)
		{
			MetaDataNode node = source.Get(positionInt, false);

			if (node.OccupiedType.HasFlag(NodeOccupiedType.None)) return;
			if (node.Exists == false) return;

			if (node.OccupiedType.HasFlag(NodeOccupiedType.Full))
			{
				GizmoUtils.DrawCube(positionInt, Color.yellow, size: 0.2f);
				return;
			}

			if (node.OccupiedType.HasFlag(NodeOccupiedType.Up))
			{
				GizmoUtils.DrawCube(new Vector3(positionInt.x, positionInt.y + 0.25f, positionInt.z), Color.red, size: 0.2f);
			}

			if (node.OccupiedType.HasFlag(NodeOccupiedType.Down))
			{
				GizmoUtils.DrawCube(new Vector3(positionInt.x, positionInt.y  - 0.25f, positionInt.z), Color.green, size: 0.2f);
			}

			if (node.OccupiedType.HasFlag(NodeOccupiedType.Left))
			{
				GizmoUtils.DrawCube(new Vector3(positionInt.x - 0.25f, positionInt.y, positionInt.z), Color.blue, size: 0.2f);
			}

			if (node.OccupiedType.HasFlag(NodeOccupiedType.Right))
			{
				GizmoUtils.DrawCube(new Vector3(positionInt.x + 0.25f, positionInt.y, positionInt.z), Color.magenta, size: 0.2f);
			}
		}
	}

	private class ElectricityVision : Check<MetaDataLayer>
	{
		public override string Label { get; } = "Electricity Vision";

		public override void DrawGizmo(MetaDataLayer source, Vector3Int position)
		{
			MetaDataNode node = source.Get(position, false);

			if (node.Exists)
			{
				if (node.ElectricalData.Count > 0)
				{
					var IntrinsicData = node.ElectricalData[0];
					switch (IntrinsicData.InData.Categorytype)
					{
						case PowerTypeCategory.StandardCable:
							GizmoUtils.DrawCube(position, Color.red);
							break;
						case PowerTypeCategory.LowVoltageCable:
							GizmoUtils.DrawCube(position, Color.blue);
							break;
						case PowerTypeCategory.HighVoltageCable:
							GizmoUtils.DrawCube(position, Color.yellow);
							break;
					}

				}
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
