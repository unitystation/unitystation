using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
	public static readonly Dictionary<Connection, Vector3Int> NeighbourDirections = new Dictionary<Connection, Vector3Int>()
	{
		{ Connection.North,     new Vector3Int(0,1,0)   },		// north
		{ Connection.NorthEast, new Vector3Int(1,1,0)   },		// north east
		{ Connection.East,      new Vector3Int(1,0,0)   },		// east
		{ Connection.SouthEast, new Vector3Int(1,-1,0)  },		// south east
		{ Connection.South,     new Vector3Int(0,-1,0)  },		// south
		{ Connection.SouthWest, new Vector3Int(-1,-1,0) },		// south west
		{ Connection.West,      new Vector3Int(-1,0,0)  },		// west
		{ Connection.NorthWest, new Vector3Int(-1,1,0)  }       // north west
	};

	public static void FindPossibleConnections(Matrix matrix,
		HashSet<PowerTypeCategory> CanConnectTo,
		ConnPoint ConnPoints,
		IntrinsicElectronicData OIinheritance,
		HashSet<IntrinsicElectronicData> InPutHashSet)
	{
		Vector2 searchVec = OIinheritance.GetLocation();
		SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointA, OIinheritance, InPutHashSet, otherConnectionPoint: ConnPoints.pointB);
		SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointB, OIinheritance, InPutHashSet, otherConnectionPoint: ConnPoints.pointA);
	}

	public static HashSet<IntrinsicElectronicData> SwitchCaseConnections(Vector2 searchVec,
		Matrix matrix,
		HashSet<PowerTypeCategory> CanConnectTo,
		Connection connectionPoint,
		IntrinsicElectronicData OIinheritance,
		HashSet<IntrinsicElectronicData> connections,
		// used in SurroundingTiles connection
		Connection otherConnectionPoint = Connection.NA)
	{
		var searchVecInt = new Vector3Int((int)searchVec.x, (int)searchVec.y, 0);

		{   // LogError Duplicate wires
			var eConnsAtSearchVec = matrix.GetElectricalConnections(searchVecInt);
			foreach (var con in eConnsAtSearchVec)
			{
				if (OIinheritance != con)
				{
					if ((OIinheritance.WireEndA == con.WireEndA && OIinheritance.WireEndB == con.WireEndB) ||
						(OIinheritance.WireEndA == con.WireEndB && OIinheritance.WireEndB == con.WireEndA))
					{
						Logger.LogErrorFormat("{0} < duplicate Please remove {1}",
							Category.Electrical,
							searchVecInt,
							OIinheritance.Categorytype);
					}
				}
			}

			eConnsAtSearchVec.Clear();
			ElectricalPool.PooledFPCList.Add(eConnsAtSearchVec);
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
					foreach (var con in conns)
					{
						if (OIinheritance != con
							 && CanConnectTo.Contains(con.Categorytype)
							 // check if possibleConnections contains our WireEnd A or B
							 && (possibleConnections.Contains(con.WireEndA) || possibleConnections.Contains(con.WireEndB))
							 // check if contains to avoid errors
							 && !connections.Contains(con))
						{
							connections.Add(con);
						}
					}
				}

				conns.Clear();
				ElectricalPool.PooledFPCList.Add(conns);
			}

			return connections;
		}

		// Connect to machine connnectors
		if (connectionPoint == Connection.MachineConnect)
		{
			foreach (var dir in MachineConnectorDirections)
			{
				var pos = searchVecInt + dir;
				var conns = matrix.GetElectricalConnections(pos);
				foreach (var con in conns)
				{
					if (OIinheritance != con && CanConnectTo.Contains(con.Categorytype) &&
						ConnectionMap.IsConnectedToTile(Connection.MachineConnect, con.GetConnPoints()) && !connections.Contains(con))
					{
						connections.Add(con);
					}
				}

				conns.Clear();
				ElectricalPool.PooledFPCList.Add(conns);
			}
			return connections;
		}

		// Make a vector representing the connection direction
		Vector3Int connVectorInt = ConnectionMap.GetDirectionFromConnection(connectionPoint);
		Vector3Int position = searchVecInt + connVectorInt;

		// Connect wires
		{
			var eConnsAtPosition = matrix.GetElectricalConnections(position);
			bool connectionsAdded = false;
			foreach (var con in eConnsAtPosition)
			{
				if (CanConnectTo.Contains(con.Categorytype) &&
					ConnectionMap.IsConnectedToTile(connectionPoint, con.GetConnPoints()))
				{
					connections.Add(con);
					connectionsAdded = true;
				}
			}
			eConnsAtPosition.Clear();
			ElectricalPool.PooledFPCList.Add(eConnsAtPosition);

			if (connectionsAdded)
			{
				return connections;
			}
		}

		// Connect to overlap
		{
			var eConnsAtSearchVec = matrix.GetElectricalConnections(searchVecInt);
			foreach (var con in eConnsAtSearchVec)
			{
				if (OIinheritance != con && CanConnectTo.Contains(con.Categorytype) &&
					ConnectionMap.IsConnectedToTileOverlap(connectionPoint, con.GetConnPoints()))
				{
					connections.Add(con);
				}
			}
			eConnsAtSearchVec.Clear();
			ElectricalPool.PooledFPCList.Add(eConnsAtSearchVec);
		}
		return connections;
	}

	public static float WorkOutResistance(Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceSources)
	{
		//Worked out per source


		//var ToLog = "\n";
		//ToLog += "ResistanceGoingTo > ";
		//ToLog += string.Join(",", ResistanceSources.ResistanceGoingTo) + "\n";
		//ToLog += "ResistanceComingFrom > ";
		//ToLog += string.Join(",", ResistanceSources.ResistanceComingFrom) + "\n";
		//Logger.Log("WorkOutResistance!" + ToLog);
		float ResistanceXAll = 0;
		foreach (KeyValuePair<IntrinsicElectronicData, VIRResistances> Source in ResistanceSources)
		{
			ResistanceXAll += 1 / Source.Value.Resistance();
		}

		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return ((1 / ResistanceXAll));
	}

	public static VIRResistances WorkOutVIRResistance(
		Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceSources)
	{
		var ResistanceXAll = ElectricalPool.GetVIRResistances();
		//Worked out per source
		//var ToLog = "\n";
		//ToLog += "ResistanceGoingTo > ";
		//ToLog += string.Join(",", ResistanceSources.ResistanceGoingTo) + "\n";
		//ToLog += "ResistanceComingFrom > ";
		//ToLog += string.Join(",", ResistanceSources.ResistanceComingFrom) + "\n";
		//Logger.Log("WorkOutResistance!" + ToLog);
		foreach (KeyValuePair<IntrinsicElectronicData, VIRResistances> Source in ResistanceSources)
		{
			ResistanceXAll.AddResistance(Source.Value);
		}

		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return (ResistanceXAll);
	}

	// TODO Add documentation, clean up, remove commented out code, etc.
	public static float WorkOutAtResistance(ElectronicSupplyData ResistanceSources, IntrinsicElectronicData Indata)
	{
		float Resistanc = 0;

		if (Indata == null)
		{
			return Resistanc;
		}

		bool goingToExists = ResistanceSources.ResistanceGoingTo.TryGetValue(Indata, out VIRResistances goingTo);
		bool comingFromExists = ResistanceSources.ResistanceComingFrom.TryGetValue(Indata, out VIRResistances comingFrom);

		if (goingToExists && comingFromExists)
		{
			HashSet<ResistanceWrap> ResistanceSour = new HashSet<ResistanceWrap>(comingFrom.ResistanceSources);

			//ResistanceSour.UnionWith(ResistanceSources.ResistanceComingFrom[Indata].ResistanceSources);

			//HashSet<ResistanceWrap> ResistanceSour = new HashSet<ResistanceWrap>(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			//ResistanceSourtoreove.UnionWith(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			//ResistanceSour.ExceptWith(ResistanceSourtoreove);

			float Toadd = 0;
			float ToRemove = 0;

			foreach (var Resistance in ResistanceSour)
			{
				if (goingTo.ResistanceSources.Contains(Resistance))
				{
					Toadd = 1 / Resistance.Resistance();
				}

				//else {
				//	ToRemove = 1 / Resistance.Resistance();
				//}
			}

			if (Toadd != 0)
			{
				Toadd = 1 / Toadd;
			}

			Resistanc = Toadd - ToRemove;
		}
		else
		{
			if (comingFromExists)
			{
				Resistanc = comingFrom.Resistance();
			}
			else if (goingToExists)
			{
				Resistanc = goingTo.Resistance();
			}
		}

		return (Resistanc);
	}

	public static int SplitNumber(ElectronicSupplyData ResistanceSources, ResistanceWrap InitialWrap)
	{
		int Total = 0;
		foreach (var ResistanceSource in ResistanceSources.ResistanceGoingTo)
		{
			foreach (var Resistance in ResistanceSource.Value.ResistanceSources)
			{
				if (Resistance != InitialWrap && InitialWrap.resistance == Resistance.resistance)
				{
					Total = Total + 1;
				}
			}
		}

		return (Total);
	}

	// TODO Add documentation, clean up, remove commented out code, fix spelling, etc.
	public static float WorkOutResistance(ElectronicSupplyData ResistanceSources, IntrinsicElectronicData Indata)
	{
		//Worked out per source
		float Resistanc = 0;

		bool goingToExists = ResistanceSources.ResistanceGoingTo.TryGetValue(Indata, out VIRResistances goingTo);
		bool comingFromExists = ResistanceSources.ResistanceComingFrom.TryGetValue(Indata, out VIRResistances comingFrom);
		if (goingToExists && comingFromExists)
		{
			//HashSet<ResistanceWrap> ResistanceSour = new HashSet<ResistanceWrap> (ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			//ResistanceSour.UnionWith(ResistanceSources.ResistanceComingFrom[Indata].ResistanceSources);

			////HashSet<ResistanceWrap> ResistanceSour = new HashSet<ResistanceWrap>(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			////ResistanceSourtoreove.UnionWith(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			////ResistanceSour.ExceptWith(ResistanceSourtoreove);

			//float Toadd = 0;
			//float ToRemove = 0;

			//foreach (var Resistance in ResistanceSour) {
			//	if (ResistanceSources.ResistanceComingFrom[Indata].ResistanceSources.Contains(Resistance))
			//	{
			//		Toadd = 1 / Resistance.Resistance();
			//	}
			//	else {
			//		ToRemove = 1 / Resistance.Resistance();
			//	}
			//}
			//if (Toadd != 0) {
			//	Toadd = 1 / Toadd;
			//}


			//if (ToRemove != 0)
			//{
			//	ToRemove= 1 / ToRemove;
			//}


			//Resistanc = Toadd - ToRemove;

			//if (Resistancxall != 0)
			//{
			//	Resistanc = (1 / Resistancxall);
			//}
			//else {
			//	Resistanc = 0;
			//}
			//ResistanceSources
			Resistanc = goingTo.Resistance();

			Resistanc = Resistanc > comingFrom.Resistance() ?
				Resistanc - comingFrom.Resistance() : comingFrom.Resistance() - Resistanc;
		}
		else if (goingToExists)
		{
			Resistanc = goingTo.Resistance();
		}
		else if (comingFromExists)
		{
			Resistanc = comingFrom.Resistance();
		}
		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return (Resistanc);
	}

	public static float WorkOutCurrent(Dictionary<IntrinsicElectronicData, float> ReceivingCurrents)
	{
		//Worked out per source
		float Current = 0;
		foreach (KeyValuePair<IntrinsicElectronicData, float> Source in ReceivingCurrents)
		{
			Current += Source.Value;
		}

		return (Current);
	}

	public static Dictionary<IntrinsicElectronicData, float> AnInterestingDictionary =
		new Dictionary<IntrinsicElectronicData, float>();

	public static (float, float, float) WorkOutActualNumbers(IntrinsicElectronicData ElectricItem)
	{
		//Sometimes gives wrong readings at junctions, Needs to be looked into
		float Current = 0; //Calculates the actual voltage and current flowing through the Node
		float Voltage = 0;
		AnInterestingDictionary.Clear();

		//Voltages easy to work out just add up all the voltages from different sources
		foreach (var Supply in ElectricItem.Data.SupplyDependent)
		{
			Voltage += Supply.Value.SourceVoltage;
		}

		foreach (var CurrentIDItem in ElectricItem.Data.SupplyDependent)
		{
			foreach (var CurrentItem in CurrentIDItem.Value.CurrentComingFrom)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += (float)CurrentItem.Value.Current();
				}
				else
				{
					AnInterestingDictionary[CurrentItem.Key] = (float)CurrentItem.Value.Current();
				}
			}

			foreach (var CurrentItem in CurrentIDItem.Value.CurrentGoingTo)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += (float)-CurrentItem.Value.Current();
				}
				else
				{
					AnInterestingDictionary[CurrentItem.Key] = (float)-CurrentItem.Value.Current();
				}
			}
		}

		foreach (var CurrentItem in AnInterestingDictionary)
		{
			if (CurrentItem.Value > 0)
			{
				Current += CurrentItem.Value;
			}
		}
		//Logger.Log (Voltage.ToString () + " < yeah Those voltage " + Current.ToString() + " < yeah Those Current " + (Voltage/Current).ToString() + " < yeah Those Resistance" + ElectricItem.GameObject().name.ToString() + " < at", Category.Electrical);

		ElectricItem.Data.CurrentInWire = Current;
		ElectricItem.Data.ActualVoltage = Voltage;
		ElectricItem.Data.EstimatedResistance = (Voltage / Current);
		return (Current, Voltage, (Voltage / Current));
	}

	public static float WorkOutVoltage(ElectricalOIinheritance ElectricItem)
	{
		float Voltage = 0;
		foreach (var Supply in ElectricItem.InData.Data.SupplyDependent)
		{
			Voltage += Supply.Value.SourceVoltage;
		}

		return (Voltage);
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

		return (Voltage);
	}

	public static float WorkOutVoltageNOTFromConnector(ElectricalOIinheritance ElectricItem,
		PowerTypeCategory SpecifiedDevice)
	{
		float Voltage = 0;
		foreach (var Supply in ElectricItem.InData.Data.SupplyDependent)
		{
			bool pass = true;
			foreach (var subcheck in Supply.Value.Upstream)
			{
				if (subcheck.Categorytype == SpecifiedDevice)
				{
					pass = false;
					break;
				}
			}

			if (pass)
			{
				Voltage += Supply.Value.SourceVoltage;
			}
		}

		return (Voltage);
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

		return (Voltage);
	}

	public static ElectricalCableTile RetrieveElectricalTile(Connection WireEndA, Connection WireEndB, PowerTypeCategory powerTypeCategory)
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

		return (Tile);
	}
}