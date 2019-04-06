using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class ElectricityFunctions
{

	public static List<ElectricalOIinheritance> FindPossibleConnections(Vector2 searchVec, Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, ConnPoint ConnPoints)
	{
		List<ElectricalOIinheritance> possibleConns = new List<ElectricalOIinheritance>();
		List<ElectricalOIinheritance> connections = new List<ElectricalOIinheritance>();
		int progress = 0;
		searchVec.x -= 1;
		searchVec.y -= 1;
		//Logger.Log("MMMMMMM");
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				Vector3Int pos = new Vector3Int((int)searchVec.x + x,
					(int)searchVec.y + y, 0);
				var conns = matrix.GetElectricalConnections(pos);
				//Logger.Log("AAAAAAAAA");
				foreach (ElectricalOIinheritance io in conns)
				{
					possibleConns.Add(io);
					//Logger.Log("BBBBBBBBBBBB");
					if (CanConnectTo.Contains
						(io.InData.Categorytype))
					{
						//Check if InputPosition and OutputPosition connect with this wire
						//Logger.Log("EEEEEEEEEEEE");
						if (ConnectionMap.IsConnectedToTile(ConnPoints, (AdjDir)progress, io.GetConnPoints()))
						{
							//Logger.Log("DDDDDDDDDDD");
							connections.Add(io);
						}
					}
				}
				progress++;
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
			if (From.Data.Upstream[UesID].Contains(To))
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
			if (From.Data.Downstream[UesID].Contains(To))
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

	public static bool CalculateDirectionFromID(ElectricalOIinheritance On, int TheID)
	{
		bool isTrue = false;
		if (!(On.Data.ResistanceComingFrom.ContainsKey(TheID)))
		{
			return (true);
		}
		if (!(On.Data.ResistanceComingFrom.ContainsKey(On.Data.FirstPresent)))
		{
			return (true);
		}
		isTrue = true;
		foreach (KeyValuePair<ElectricalOIinheritance, float> CurrentItem in On.Data.ResistanceComingFrom[On.Data.FirstPresent])
		{

			if (!(On.Data.ResistanceComingFrom[TheID].ContainsKey(CurrentItem.Key)))
			{
				isTrue = false;
			}
		}
		return (isTrue);
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

	public static (float, float, float) WorkOutActualNumbers(ElectricalOIinheritance ElectricItem)
	{  //Sometimes gives wrong readings at junctions, Needs to be looked into
		float Current = 0; //Calculates the actual voltage and current flowing through the Node
		float Voltage = 0;
		foreach (KeyValuePair<int, float> CurrentIDItem in ElectricItem.Data.SourceVoltages) { //Voltages easy to work out just add up all the voltages from different sources
			Voltage += CurrentIDItem.Value;
		}
		foreach (KeyValuePair<int, Dictionary<ElectricalOIinheritance, float>> CurrentIDItem in ElectricItem.Data.CurrentComingFrom)
		{
			foreach (KeyValuePair<ElectricalOIinheritance, float> CurrentItem in CurrentIDItem.Value) //Tricky for current since it can flow one way or the other
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
