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
	public static bool CalculateDirectionBool(ElectricalOIinheritance From, ElectricalOIinheritance To, bool Upstream)
	{
		bool isTrue = false;
		int UesID = From.Data.FirstPresent;
		if (Upstream)
		{
			if (From.Data.SupplyDependent[UesID].Upstream.Contains(To))
			{
				isTrue = true;
				return (isTrue);
			}
			else
			{
				return (isTrue);
			}
		}
		else
		{
			if (From.Data.SupplyDependent[UesID].Downstream.Contains(To))
			{
				isTrue = true;
				return (isTrue);
			}
			else
			{
				return (isTrue);
			}
		}
	}

	//public static bool CalculateDirectionFromID(ElectricalOIinheritance On, int TheID)
	//{
	//	bool isTrue = false;
	//	if (!(On.Data.ResistanceComingFrom.ContainsKey(TheID)))
	//	{
	//		return (true);
	//	}
	//	if (!(On.Data.ResistanceComingFrom.ContainsKey(On.Data.FirstPresent)))
	//	{
	//		return (true);
	//	}
	//	isTrue = true;
	//	foreach (KeyValuePair<ElectricalOIinheritance, float> CurrentItem in On.Data.ResistanceComingFrom[On.Data.FirstPresent])
	//	{

	//		if (!(On.Data.ResistanceComingFrom[TheID].ContainsKey(CurrentItem.Key)))
	//		{
	//			isTrue = false;
	//		}
	//	}
	//	return (isTrue);
	//}

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

	public static (float, float, float) WorkOutActualNumbers(ElectricalOIinheritance ElectricItem)
	{  //Sometimes gives wrong readings at junctions, Needs to be looked into
		float Current = 0; //Calculates the actual voltage and current flowing through the Node
		float Voltage = 0;
		foreach (var Supply in ElectricItem.Data.SupplyDependent) //Voltages easy to work out just add up all the voltages from different sources
		{
			Voltage += Supply.Value.SourceVoltages;
		}
		foreach (var Supply in ElectricItem.Data.SupplyDependent) //Voltages easy to work out just add up all the voltages from different sources
		{
			foreach (KeyValuePair<ElectricalOIinheritance, float> CurrentItem in Supply.Value.CurrentComingFrom) //Tricky for current since it can flow one way or the other
			{
				Current += CurrentItem.Value;
			}
		}
		//Logger.Log (Voltage.ToString () + " < yeah Those voltage " + Current.ToString() + " < yeah Those Current " + (Voltage/Current).ToString() + " < yeah Those Resistance" + ElectricItem.GameObject().name.ToString() + " < at", Category.Electrical);

		//Electricity Cabledata = new Electricity();
		//Cabledata.Current = Current;
		//Cabledata.Voltage = Voltage;
		//Cabledata.EstimatedResistant = Voltage / Current;
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

	//public static void CircuitSearchLoop(ElectricalOIinheritance Thiswire, ElectricalOIinheritance ProvidingPower)
	//{
	//	InputOutputFunctions.DirectionOutput(Thiswire.GameObject(), Thiswire);
	//	bool Break = true;
	//	List<ElectricalOIinheritance> IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance>();
	//	while (Break)
	//	{
	//		IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance>(ProvidingPower.DirectionWorkOnNextList);
	//		ProvidingPower.DirectionWorkOnNextList.Clear();
	//		for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
	//		{
	//			IterateDirectionWorkOnNextList[i].DirectionOutput(Thiswire.GameObject());
	//		}
	//		if (ProvidingPower.DirectionWorkOnNextList.Count <= 0)
	//		{
	//			IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance>(ProvidingPower.DirectionWorkOnNextListWait);
	//			ProvidingPower.DirectionWorkOnNextListWait.Clear();
	//			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
	//			{
	//				IterateDirectionWorkOnNextList[i].DirectionOutput(Thiswire.GameObject());
	//			}
	//		}
	//		if (ProvidingPower.DirectionWorkOnNextList.Count <= 0 && ProvidingPower.DirectionWorkOnNextListWait.Count <= 0)
	//		{
	//			//Logger.Log ("stop!");
	//			Break = false;
	//		}
	//	}
	//}

	//	public static void CircuitResistanceLoop(ElectricalOIinheritance Thiswire, ElectricalOIinheritance ProvidingPower ){
	//		bool Break = true;
	//		List<ElectricalOIinheritance> IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance> ();
	//		while (Break) {
	//			IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance> (ProvidingPower.ResistanceWorkOnNextList);
	//			ProvidingPower.ResistanceWorkOnNextList.Clear();
	//			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
	//				IterateDirectionWorkOnNextList [i].ResistancyOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
	//			}
	//			if (ProvidingPower.ResistanceWorkOnNextList.Count <= 0) {
	//				IterateDirectionWorkOnNextList = new List<ElectricalOIinheritance> (ProvidingPower.ResistanceWorkOnNextListWait);
	//				ProvidingPower.ResistanceWorkOnNextListWait.Clear();
	//				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
	//					IterateDirectionWorkOnNextList [i].ResistancyOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
	//				}
	//			}
	//			if (ProvidingPower.ResistanceWorkOnNextList.Count <= 0 && ProvidingPower.ResistanceWorkOnNextListWait.Count <= 0) {
	//				Break = false;
	//			}
	//		}
	//	}
}
