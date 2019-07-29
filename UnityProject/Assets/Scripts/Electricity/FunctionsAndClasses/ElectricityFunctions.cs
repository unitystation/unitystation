using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class ElectricityFunctions
{

	public static HashSet<ElectricalOIinheritance> FindPossibleConnections(Vector2 searchVec, Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, ConnPoint ConnPoints, ElectricalOIinheritance OIinheritance )
	{
		
		HashSet<ElectricalOIinheritance> connections = new HashSet<ElectricalOIinheritance>();
		connections = SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointA, OIinheritance);
		connections.UnionWith(SwitchCaseConnections(searchVec, matrix, CanConnectTo, ConnPoints.pointB, OIinheritance));
		return (connections);
	}
	public static HashSet<ElectricalOIinheritance> SwitchCaseConnections(Vector2 searchVec, Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, Connection ConnPoints, ElectricalOIinheritance OIinheritance) {
		HashSet<ElectricalOIinheritance> connections = new HashSet<ElectricalOIinheritance>();
		Connection Direction = Connection.Overlap;
		Vector3Int Position = new Vector3Int();
		bool MachineConnect = false;
		switch (ConnPoints)
		{
			case Connection.North:
				{
					Direction = Connection.North;
					Position = new Vector3Int((int)searchVec.x + 0, (int)searchVec.y + 1, 0);
				}
				break;
			case Connection.NorthEast:
				{
					Direction = Connection.NorthEast;
					Position = new Vector3Int((int)searchVec.x + 1, (int)searchVec.y + 1, 0);
				}
				break;
			case Connection.East:
				{
					Direction = Connection.East;
					Position = new Vector3Int((int)searchVec.x + 1, (int)searchVec.y + 0, 0);
				}
				break;
			case Connection.SouthEast:
				{
					Direction = Connection.SouthEast;
					Position = new Vector3Int((int)searchVec.x + 1, (int)searchVec.y - 1, 0);
				}
				break;
			case Connection.South:
				{
					Direction = Connection.South;
					Position = new Vector3Int((int)searchVec.x + 0, (int)searchVec.y - 1, 0);
				}
				break;
			case Connection.SouthWest:
				{
					Direction = Connection.SouthWest;
					Position = new Vector3Int((int)searchVec.x + -1, (int)searchVec.y - 1, 0);
				}
				break;
			case Connection.West:
				{
					Direction = Connection.West;
					Position = new Vector3Int((int)searchVec.x + -1, (int)searchVec.y + 0, 0);
				}
				break;
			case Connection.NorthWest:
				{
					Direction = Connection.NorthWest;
					Position = new Vector3Int((int)searchVec.x + -1, (int)searchVec.y + 1, 0);
				}
				break;
			case Connection.Overlap:
				{
					Direction = Connection.Overlap;
					Position = new Vector3Int((int)searchVec.x + 0, (int)searchVec.y + 0, 0);
				}
				break;
			case Connection.MachineConnect:
				{
					Direction = Connection.MachineConnect;
					MachineConnect = true;
				}
				break;
		}
		Vector3Int PositionE = new Vector3Int((int)searchVec.x, (int)searchVec.y, 0);
		var Econns = matrix.GetElectricalConnections(PositionE);
		foreach (var con in Econns)
		{
			if (OIinheritance != con)
			{
				if ((OIinheritance.WireEndA == con.WireEndA && OIinheritance.WireEndB == con.WireEndB) ||
					(OIinheritance.WireEndA == con.WireEndB && OIinheritance.WireEndB == con.WireEndA)) { 
					Logger.LogErrorFormat("{0} < duplicate Please remove {1}", Category.Electrical, PositionE, OIinheritance.InData.Categorytype);
				}

			}
		}
		if (!MachineConnect)
		{
			var conns = matrix.GetElectricalConnections(Position);
			foreach (var con in conns)
			{
				if (CanConnectTo.Contains(con.InData.Categorytype))
				{
					if (ConnectionMap.IsConnectedToTile(Direction, con.GetConnPoints()))
					{

						connections.Add(con);
					}
				}
			}
			if (connections.Count == 0)
			{
				Position = new Vector3Int((int)searchVec.x, (int)searchVec.y, 0);
				conns = matrix.GetElectricalConnections(Position);
				foreach (var con in conns)
				{
					if (OIinheritance != con)
					{
						if (CanConnectTo.Contains(con.InData.Categorytype))
						{
							if (ConnectionMap.IsConnectedToTileOverlap(Direction, con.GetConnPoints()))
							{
								connections.Add(con);
							}
						}
					}
				}
			}
		}
		else {
			for (int x = 0; x < 3; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					Vector3Int pos = new Vector3Int((int)searchVec.x + (x - 1), (int)searchVec.y + (y - 1), 0);
					var conns = matrix.GetElectricalConnections(pos);
					foreach (var con in conns)
					{
						if (OIinheritance != con)
						{
							if (CanConnectTo.Contains(con.InData.Categorytype))
							{
								if (ConnectionMap.IsConnectedToTile(Direction, con.GetConnPoints()))
								{
									connections.Add(con);
								}
							}
						}
					}
				}
			}
		}
		return (connections);
	}

	public static float WorkOutResistance(Dictionary<ElectricalOIinheritance, float> ResistanceSources)
	{ //Worked out per source
		float ResistanceXAll = 0;
		foreach (KeyValuePair<ElectricalOIinheritance, float> Source in ResistanceSources)
		{
			ResistanceXAll += 1 / Source.Value;
		}
		return (1 / ResistanceXAll);
	}

	public static float WorkOutCurrent(Dictionary<ElectricalOIinheritance, float> ReceivingCurrents)
	{ //Worked out per source
		float Current = 0;
		foreach (KeyValuePair<ElectricalOIinheritance, float> Source in ReceivingCurrents)
		{

			Current += Source.Value;
		}
		return (Current);
	}
	public static Dictionary<ElectricalOIinheritance, float> AnInterestingDictionary = new Dictionary<ElectricalOIinheritance, float>();

	public static (float, float, float) WorkOutActualNumbers(ElectricalOIinheritance ElectricItem)
	{  //Sometimes gives wrong readings at junctions, Needs to be looked into
		float Current = 0; //Calculates the actual voltage and current flowing through the Node
		float Voltage = 0;
		AnInterestingDictionary.Clear();
		foreach (var Supply in ElectricItem.Data.SupplyDependent) //Voltages easy to work out just add up all the voltages from different sources
		{
			Voltage += Supply.Value.SourceVoltages;
		}

		foreach (var CurrentIDItem in ElectricItem.Data.SupplyDependent)
		{
			foreach (var CurrentItem in CurrentIDItem.Value.CurrentComingFrom)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += CurrentItem.Value;
				}
				else {
					AnInterestingDictionary[CurrentItem.Key] = CurrentItem.Value;
				}
			}
			foreach (var CurrentItem in CurrentIDItem.Value.CurrentGoingTo)
			{
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += -CurrentItem.Value;
				}
				else {
					AnInterestingDictionary[CurrentItem.Key] = -CurrentItem.Value;
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
		foreach (var Supply in ElectricItem.Data.SupplyDependent) 
		{
			Voltage += Supply.Value.SourceVoltages;
		}
		return (Voltage);
	}


	public static float WorkOutVoltageFromConnector(ElectricalOIinheritance ElectricItem, PowerTypeCategory SpecifiedDevice)
	{
		float Voltage = 0;
		foreach (var Supply in ElectricItem.Data.SupplyDependent)
		{
			bool pass = false;
			foreach (var subcheck in Supply.Value.Upstream)
			{
				if (subcheck.InData.Categorytype == SpecifiedDevice)
				{
					pass = true;

				}
			}
			if (!pass) { 
				foreach (var subcheck in Supply.Value.Downstream)
				{
					if (subcheck.InData.Categorytype == SpecifiedDevice)
					{
						pass = true;
					}
				}
			}
			if (pass)
			{
				Voltage += Supply.Value.SourceVoltages;
			}
		}
		return (Voltage);
	}

	public static float WorkOutVoltageNOTFromConnector(ElectricalOIinheritance ElectricItem, PowerTypeCategory SpecifiedDevice)
	{
		float Voltage = 0;
		foreach (var Supply in ElectricItem.Data.SupplyDependent)
		{
			bool pass = true;
			foreach (var subcheck in Supply.Value.Upstream)
			{

				if (subcheck.InData.Categorytype == SpecifiedDevice)
				{
					pass = false;

				}
			}
			if (pass)
			{
				Voltage += Supply.Value.SourceVoltages;
			}
		}
		return (Voltage);
	}

	public static float WorkOutVoltageFromConnectors(ElectricalOIinheritance ElectricItem, HashSet<PowerTypeCategory> SpecifiedDevices)
	{
		float Voltage = 0;
		foreach (var Supply in ElectricItem.Data.SupplyDependent)
		{
			bool pass = false;
			foreach (var subcheck in Supply.Value.Upstream)
			{
				if (SpecifiedDevices.Contains(subcheck.InData.Categorytype))
				{
					pass = true;

				}
			}
			if (!pass) { 
				foreach (var subcheck in Supply.Value.Downstream)
				{
					if (SpecifiedDevices.Contains(subcheck.InData.Categorytype))
					{
						pass = true;

					}
				}
			}
			if (pass)
			{
				Voltage += Supply.Value.SourceVoltages;
			}
		}
		return (Voltage);
	}
}
