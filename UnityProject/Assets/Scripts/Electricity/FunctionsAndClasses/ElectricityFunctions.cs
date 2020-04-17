using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class ElectricityFunctions
{
	public static HashSet<Vector2> MachineConnectorDirections = new HashSet<Vector2>()
	{
		Vector2.up,
		Vector2.down,
		Vector2.right,
		Vector2.left
	};


	public static void FindPossibleConnections(Matrix matrix,
		HashSet<PowerTypeCategory> CanConnectTo,
		ConnPoint ConnPoints,
		IntrinsicElectronicData OIinheritance,
		HashSet<IntrinsicElectronicData> InPutHashSet)
	{
		Vector2 searchVec = OIinheritance.GetLocation();
		//Location
		SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointA, OIinheritance,InPutHashSet);
		SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointB, OIinheritance,InPutHashSet);
	}

	public static HashSet<IntrinsicElectronicData> SwitchCaseConnections(Vector2 searchVec,
		Matrix matrix,
		HashSet<PowerTypeCategory> CanConnectTo,
		Connection ConnPoints,
		IntrinsicElectronicData OIinheritance,
		HashSet<IntrinsicElectronicData> connections)
	{
		var Direction = Connection.Overlap;
		var Position = new Vector3Int();
		var MachineConnect = false;
		switch (ConnPoints)
		{
			case Connection.North:
			{
				Direction = Connection.North;
				Position = new Vector3Int((int) searchVec.x + 0, (int) searchVec.y + 1, 0);
			}
				break;
			case Connection.NorthEast:
			{
				Direction = Connection.NorthEast;
				Position = new Vector3Int((int) searchVec.x + 1, (int) searchVec.y + 1, 0);
			}
				break;
			case Connection.East:
			{
				Direction = Connection.East;
				Position = new Vector3Int((int) searchVec.x + 1, (int) searchVec.y + 0, 0);
			}
				break;
			case Connection.SouthEast:
			{
				Direction = Connection.SouthEast;
				Position = new Vector3Int((int) searchVec.x + 1, (int) searchVec.y - 1, 0);
			}
				break;
			case Connection.South:
			{
				Direction = Connection.South;
				Position = new Vector3Int((int) searchVec.x + 0, (int) searchVec.y - 1, 0);
			}
				break;
			case Connection.SouthWest:
			{
				Direction = Connection.SouthWest;
				Position = new Vector3Int((int) searchVec.x + -1, (int) searchVec.y - 1, 0);
			}
				break;
			case Connection.West:
			{
				Direction = Connection.West;
				Position = new Vector3Int((int) searchVec.x + -1, (int) searchVec.y + 0, 0);
			}
				break;
			case Connection.NorthWest:
			{
				Direction = Connection.NorthWest;
				Position = new Vector3Int((int) searchVec.x + -1, (int) searchVec.y + 1, 0);
			}
				break;
			case Connection.Overlap:
			{
				Direction = Connection.Overlap;
				Position = new Vector3Int((int) searchVec.x + 0, (int) searchVec.y + 0, 0);
			}
				break;
			case Connection.MachineConnect:
			{
				Direction = Connection.MachineConnect;
				MachineConnect = true;
			}
				break;
		}

		var PositionE = new Vector3Int((int) searchVec.x, (int) searchVec.y, 0);
		var Econns = matrix.GetElectricalConnections(PositionE);
		foreach (var con in Econns)
			if (OIinheritance != con)
				if (OIinheritance.WireEndA == con.WireEndA && OIinheritance.WireEndB == con.WireEndB ||
				    OIinheritance.WireEndA == con.WireEndB && OIinheritance.WireEndB == con.WireEndA)
					Logger.LogErrorFormat("{0} < duplicate Please remove {1}",
						Category.Electrical,
						PositionE,
						OIinheritance);
		Econns.Clear();
		ElectricalPool.PooledFPCList.Add(Econns);
		if (!MachineConnect)
		{
			var conns = matrix.GetElectricalConnections(Position);
			foreach (var con in conns)
				if (CanConnectTo.Contains(con.Categorytype))
					if (ConnectionMap.IsConnectedToTile(Direction, con.GetConnPoints()))
					{
						connections.Add(con);
					}

			conns.Clear();
			ElectricalPool.PooledFPCList.Add(conns);
			if (connections.Count == 0)
			{
				Position = new Vector3Int((int) searchVec.x, (int) searchVec.y, 0);
				conns = matrix.GetElectricalConnections(Position);
				foreach (var con in conns)
					if (OIinheritance != con)
						if (CanConnectTo.Contains(con.Categorytype))
							if (ConnectionMap.IsConnectedToTileOverlap(Direction, con.GetConnPoints()))
							{
								connections.Add(con);
							}
				conns.Clear();
				ElectricalPool.PooledFPCList.Add(conns);
			}
		}
		else
		{
			foreach (var Direction_ in MachineConnectorDirections)
			{
				var pos = new Vector3Int((int) searchVec.x + (int) Direction_.x,
					(int) searchVec.y + (int) Direction_.y, 0);
				var conns = matrix.GetElectricalConnections(pos);
				foreach (var con in conns)
					if (OIinheritance != con)
						if (CanConnectTo.Contains(con.Categorytype))
							if (ConnectionMap.IsConnectedToTile(Direction, con.GetConnPoints()))
							{
								connections.Add(con);
							}
				conns.Clear();
				ElectricalPool.PooledFPCList.Add(conns);
			}
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

		if (Indata != null && ResistanceSources.ResistanceGoingTo.ContainsKey(Indata) &&
		    ResistanceSources.ResistanceComingFrom.ContainsKey(Indata))
		{
			HashSet<ResistanceWrap> ResistanceSour =
				new HashSet<ResistanceWrap>(ResistanceSources.ResistanceComingFrom[Indata].ResistanceSources);

			//ResistanceSour.UnionWith(ResistanceSources.ResistanceComingFrom[Indata].ResistanceSources);

			//HashSet<ResistanceWrap> ResistanceSour = new HashSet<ResistanceWrap>(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			//ResistanceSourtoreove.UnionWith(ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources);

			//ResistanceSour.ExceptWith(ResistanceSourtoreove);

			float Toadd = 0;
			float ToRemove = 0;

			foreach (var Resistance in ResistanceSour)
			{
				if (ResistanceSources.ResistanceGoingTo[Indata].ResistanceSources.Contains(Resistance))
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
			if (ResistanceSources.ResistanceComingFrom.ContainsKey(Indata))
			{
				Resistanc = ResistanceSources.ResistanceComingFrom[Indata].Resistance();
			}
			else if (ResistanceSources.ResistanceGoingTo.ContainsKey(Indata))
			{
				Resistanc = ResistanceSources.ResistanceGoingTo[Indata].Resistance();
			}
		}

		//Logger.Log((1 / ResistanceXAll)+ "< Return");
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
		if (ResistanceSources.ResistanceGoingTo.ContainsKey(Indata) &&
		    ResistanceSources.ResistanceComingFrom.ContainsKey(Indata))
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
			Resistanc = ResistanceSources.ResistanceGoingTo[Indata].Resistance();
			if (Resistanc > ResistanceSources.ResistanceComingFrom[Indata].Resistance())
			{
				Resistanc = Resistanc - ResistanceSources.ResistanceComingFrom[Indata].Resistance();
			}
			else
			{
				Resistanc = ResistanceSources.ResistanceComingFrom[Indata].Resistance() - Resistanc;
			}
		}
		else
		{
			if (ResistanceSources.ResistanceGoingTo.ContainsKey(Indata))
			{
				Resistanc = ResistanceSources.ResistanceGoingTo[Indata].Resistance();
			}
			else if (ResistanceSources.ResistanceComingFrom.ContainsKey(Indata))
			{
				Resistanc = ResistanceSources.ResistanceComingFrom[Indata].Resistance();
			}
			else
			{
				Resistanc = 0;
			}
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
		foreach (var Supply in ElectricItem.Data.SupplyDependent
		) //Voltages easy to work out just add up all the voltages from different sources
		{
			Voltage += Supply.Value.SourceVoltage;
		}

		foreach (var CurrentIDItem in ElectricItem.Data.SupplyDependent)
		{
			foreach (var CurrentItem in CurrentIDItem.Value.CurrentComingFrom)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += (float) CurrentItem.Value.Current();
				}
				else
				{
					AnInterestingDictionary[CurrentItem.Key] = (float) CurrentItem.Value.Current();
				}
			}

			foreach (var CurrentItem in CurrentIDItem.Value.CurrentGoingTo)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += (float) -CurrentItem.Value.Current();
				}
				else
				{
					AnInterestingDictionary[CurrentItem.Key] = (float) -CurrentItem.Value.Current();
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
				}
			}

			if (!pass)
			{
				foreach (var subcheck in Supply.Value.CurrentGoingTo)
				{
					if (subcheck.Key.Categorytype == SpecifiedDevice)
					{
						pass = true;
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
				}
			}

			if (!pass)
			{
				foreach (var subcheck in Supply.Value.CurrentGoingTo)
				{
					if (SpecifiedDevices.Contains(subcheck.Key.Categorytype))
					{
						pass = true;
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
		string Compound;
		if (WireEndA < WireEndB)
		{
			Compound = WireEndA + "_" + WireEndB;
		}
		else {
			Compound = WireEndB + "_" + WireEndA;
		}
		int spriteIndex = WireDirections.GetSpriteIndex(Compound);

		switch (powerTypeCategory)
		{
			case PowerTypeCategory.StandardCable:
				Tile = ElectricalManager.Instance.MediumVoltageCables.Tiles[spriteIndex];
				break;
			case PowerTypeCategory.LowVoltageCable:
				Tile = ElectricalManager.Instance.LowVoltageCables.Tiles[spriteIndex];
				break;
			case PowerTypeCategory.HighVoltageCable:
				Tile = ElectricalManager.Instance.HighVoltageCables.Tiles[spriteIndex];
				break;
		}

		return (Tile);
	}

}