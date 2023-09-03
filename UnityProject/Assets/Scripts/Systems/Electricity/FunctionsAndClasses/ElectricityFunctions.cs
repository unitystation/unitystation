using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace Systems.Electricity
{
	public static class ElectricityFunctions
	{
		public static HashSet<Vector3Int> MachineConnectorDirections = new HashSet<Vector3Int>()
		{
			Vector3Int.up,
			Vector3Int.down,
			Vector3Int.right,
			Vector3Int.left
		};

		// dictionary of connections checked in "SurroundingTiles" connection
		public static readonly Dictionary<Connection, Vector3Int> NeighbourDirections =
			new Dictionary<Connection, Vector3Int>()
			{
				{Connection.North, new Vector3Int(0, 1, 0)}, // north
				{Connection.NorthEast, new Vector3Int(1, 1, 0)}, // north east
				{Connection.East, new Vector3Int(1, 0, 0)}, // east
				{Connection.SouthEast, new Vector3Int(1, -1, 0)}, // south east
				{Connection.South, new Vector3Int(0, -1, 0)}, // south
				{Connection.SouthWest, new Vector3Int(-1, -1, 0)}, // south west
				{Connection.West, new Vector3Int(-1, 0, 0)}, // west
				{Connection.NorthWest, new Vector3Int(-1, 1, 0)} // north west
			};

		public static void FindPossibleConnections(Matrix matrix,
			HashSet<PowerTypeCategory> CanConnectTo,
			ConnPoint ConnPoints,
			IntrinsicElectronicData OIinheritance,
			HashSet<IntrinsicElectronicData> InPutHashSet)
		{
			Vector2 searchVec = OIinheritance.GetLocation();
			SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointA, OIinheritance, InPutHashSet,
				ConnPoints.pointB);
			SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointB, OIinheritance, InPutHashSet,
				ConnPoints.pointA);
		}

		public static void SwitchCaseConnections(Vector2 searchVec,
			Matrix matrix,
			HashSet<PowerTypeCategory> CanConnectTo,
			Connection connectionPoint,
			IntrinsicElectronicData OIinheritance,
			HashSet<IntrinsicElectronicData> connections,
			// used in SurroundingTiles connection
			Connection otherConnectionPoint = Connection.NA)
		{
			var searchVecInt = new Vector3Int((int) searchVec.x, (int) searchVec.y, 0);

			{
				// LogError Duplicate wires
				if (matrix  == null ) return;
				var eConnsAtSearchVec = matrix.GetElectricalConnections(searchVecInt);
				foreach (var con in eConnsAtSearchVec.List)
				{
					if (OIinheritance != con)
					{
						if ((OIinheritance.WireEndA == con.WireEndA && OIinheritance.WireEndB == con.WireEndB) ||
						    (OIinheritance.WireEndA == con.WireEndB && OIinheritance.WireEndB == con.WireEndA))
						{
							Loggy.LogError($"{searchVecInt} < duplicate Please remove {OIinheritance.Categorytype}",
								Category.Electrical);
						}
					}
				}

				eConnsAtSearchVec.Pool();
			}

			if (connectionPoint == Connection.SurroundingTiles)
			{
				foreach (var dir in NeighbourDirections)
				{
					// check all nearby connections except connection that is specified in other wire end
					if (dir.Key == otherConnectionPoint) continue;

					var pos = searchVecInt + dir.Value;
					var conns = matrix.GetElectricalConnections(pos);

					// get connections pointing towards our tile [ex. if neighbour is at north, get all south connections(SW, S, SE)]
					HashSet<Connection> possibleConnections = ConnectionMap.GetConnectionsTargeting(dir.Key);
					if (possibleConnections != null)
					{
						foreach (var con in conns.List)
						{
							if (OIinheritance != con
							    && CanConnectTo.Contains(con.Categorytype)
							    // check if possibleConnections contains our WireEnd A or B
							    && (possibleConnections.Contains(con.WireEndA) ||
							        possibleConnections.Contains(con.WireEndB))
							    // check if contains to avoid errors
							    && !connections.Contains(con))
							{
								connections.Add(con);
							}
						}
					}

					conns.Pool();
				}

				return;
			}

			// Connect to machine connnectors
			if (connectionPoint == Connection.MachineConnect)
			{
				foreach (var dir in MachineConnectorDirections)
				{
					var pos = searchVecInt + dir;
					var conns = matrix.GetElectricalConnections(pos);
					foreach (var con in conns.List)
					{
						if (OIinheritance != con && CanConnectTo.Contains(con.Categorytype) &&
						    ConnectionMap.IsConnectedToTile(Connection.MachineConnect, con.GetConnPoints()) &&
						    !connections.Contains(con))
						{
							connections.Add(con);
						}
					}

					conns.Pool();
				}

				return;
			}

			// Make a vector representing the connection direction
			Vector3Int connVectorInt = ConnectionMap.GetDirectionFromConnection(connectionPoint);
			Vector3Int position = searchVecInt + connVectorInt;

			// Connect wires
			{
				var eConnsAtPosition = matrix.GetElectricalConnections(position);
				bool connectionsAdded = false;
				foreach (var con in eConnsAtPosition.List)
				{
					if (CanConnectTo.Contains(con.Categorytype) &&
					    ConnectionMap.IsConnectedToTile(connectionPoint, con.GetConnPoints()))
					{
						connections.Add(con);
						connectionsAdded = true;
					}
				}

				eConnsAtPosition.Pool();

				if (connectionsAdded)
				{
					return;
				}
			}

			// Connect to overlap
			{
				var eConnsAtSearchVec = matrix.GetElectricalConnections(searchVecInt);
				foreach (var con in eConnsAtSearchVec.List)
				{
					if (OIinheritance != con && CanConnectTo.Contains(con.Categorytype) &&
					    ConnectionMap.IsConnectedToTileOverlap(connectionPoint, con.GetConnPoints()))
					{
						connections.Add(con);
					}
				}

				eConnsAtSearchVec.Pool();
			}
		}

		public static float WorkOutResistance(Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceSources)
		{
			//Worked out per source
			float ResistanceXAll = 0;
			foreach (KeyValuePair<IntrinsicElectronicData, VIRResistances> Source in ResistanceSources)
			{
				ResistanceXAll += 1 / Source.Value.Resistance();
			}

			return 1 / ResistanceXAll;
		}
		private static readonly Dictionary<IntrinsicElectronicData, float> UpstreamAndDownstreamCurrentValues = new Dictionary<IntrinsicElectronicData, float>();

		public static void WorkOutActualNumbers(IntrinsicElectronicData electricItem)
		{
			//Sometimes gives wrong readings at junctions, Needs to be looked into
			//Calculates the actual voltage and current flowing through the Node
			float current = 0;
			float voltage = 0;

			foreach (var supply in electricItem.Data.SupplyDependent)
			{
				//Voltages easy to work out just add up all the voltages from different sources
				voltage += supply.Value.SourceVoltage;
			}

			lock (UpstreamAndDownstreamCurrentValues)
			{
				UpstreamAndDownstreamCurrentValues.Clear(); //Voltages easy to work out just add up all the voltages from different sources
				foreach (var CurrentIDItem in electricItem.Data.SupplyDependent)
				{
					foreach (var Upstream in CurrentIDItem.Value.CurrentComingFrom)
					{
						if (UpstreamAndDownstreamCurrentValues.ContainsKey(Upstream.Key))
						{
							UpstreamAndDownstreamCurrentValues[Upstream.Key] += (float) Upstream.Value.Current();
						}
						else
						{
							UpstreamAndDownstreamCurrentValues[Upstream.Key] = (float) Upstream.Value.Current();
						}
					}

					foreach (var Downstream in CurrentIDItem.Value.CurrentGoingTo)
					{
						if (UpstreamAndDownstreamCurrentValues.ContainsKey(Downstream.Key))
						{
							UpstreamAndDownstreamCurrentValues[Downstream.Key] += (float) -Downstream.Value.Current();
						}
						else
						{
							UpstreamAndDownstreamCurrentValues[Downstream.Key] = (float) -Downstream.Value.Current();
						}
					}
				}

				foreach (var CurrentItem in UpstreamAndDownstreamCurrentValues)
				{
					if (CurrentItem.Value > 0)
					{
						current += CurrentItem.Value;
					}
				}
			}

			electricItem.Data.CurrentInWire = current;
			electricItem.Data.ActualVoltage = voltage;
			electricItem.Data.EstimatedResistance = (voltage / current);
		}

		public static float WorkOutVoltage(ElectricalOIinheritance ElectricItem)
		{
			float voltage = 0;

			foreach (var supply in ElectricItem.InData.Data.SupplyDependent)
			{
				voltage += supply.Value.SourceVoltage;
			}

			return voltage;
		}

		public static float WorkOutVoltageFromConnector(ElectricalOIinheritance ElectricItem,
			PowerTypeCategory SpecifiedDevice)
		{
			float Voltage = 0;
			foreach (var Supply in ElectricItem.InData.Data.SupplyDependent)
			{
				bool pass = false;
				foreach (var subcheck in Supply.Value.CurrentComingFrom)
				{
					if (subcheck.Key.Categorytype == SpecifiedDevice)
					{
						pass = true;
						break;
					}
				}

				if (!pass)
				{
					foreach (var subcheck in Supply.Value.CurrentGoingTo)
					{
						if (subcheck.Key.Categorytype == SpecifiedDevice)
						{
							pass = true;
							break;
						}
					}
				}

				if (pass)
				{
					Voltage += Supply.Value.SourceVoltage;
				}
			}

			return Voltage;
		}

		public static float WorkOutVoltageFromConnectors(ElectricalOIinheritance ElectricItem,
			HashSet<PowerTypeCategory> SpecifiedDevices)
		{
			float Voltage = 0;
			foreach (var Supply in ElectricItem.InData.Data.SupplyDependent)
			{
				bool pass = false;
				foreach (var subcheck in Supply.Value.CurrentComingFrom)
				{
					if (SpecifiedDevices.Contains(subcheck.Key.Categorytype))
					{
						pass = true;
						break;
					}
				}

				if (!pass)
				{
					foreach (var subcheck in Supply.Value.CurrentGoingTo)
					{
						if (SpecifiedDevices.Contains(subcheck.Key.Categorytype))
						{
							pass = true;
							break;
						}
					}
				}

				if (pass)
				{
					Voltage += Supply.Value.SourceVoltage;
				}
			}

			return Voltage;
		}

		public static ElectricalCableTile RetrieveElectricalTile(Connection WireEndA, Connection WireEndB,
			PowerTypeCategory powerTypeCategory)
		{
			ElectricalCableTile Tile = null;
			int spriteIndex = WireDirections.GetSpriteIndex(WireEndA, WireEndB);

			switch (powerTypeCategory)
			{
				case PowerTypeCategory.StandardCable:
				{
					Tile = ElectricalManager.Instance.MediumVoltageCables.Tiles[spriteIndex];
					break;
				}
				case PowerTypeCategory.LowVoltageCable:
				{
					Tile = ElectricalManager.Instance.LowVoltageCables.Tiles[spriteIndex];
					break;
				}
				case PowerTypeCategory.HighVoltageCable:
				{
					Tile = ElectricalManager.Instance.HighVoltageCables.Tiles[spriteIndex];
					break;
				}
			}

			return Tile;
		}
	}
}
