using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class ElectricityFunctions
{

	public static List<IElectricityIO> FindPossibleConnections(Vector2 searchVec, Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, ConnPoint ConnPoints)
	{
		List<IElectricityIO> possibleConns = new List<IElectricityIO>();
		List<IElectricityIO> connections = new List<IElectricityIO>();
		int progress = 0;
		searchVec.x -= 1;
		searchVec.y -= 1;
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				Vector3Int pos = new Vector3Int((int)searchVec.x + x,
					(int)searchVec.y + y, 0);
				var conns = matrix.GetElectricalConnections(pos);
				foreach (IElectricityIO io in conns)
				{
					possibleConns.Add(io);
					if (CanConnectTo.Contains
						(io.InData.Categorytype))
					{
						//Check if InputPosition and OutputPosition connect with this wire
						if (ConnectionMap.IsConnectedToTile(ConnPoints, (AdjDir)progress, io.GetConnPoints()))
						{
							connections.Add(io);
						}
					}
				}
				progress++;
			}
		}
		return (connections);
	}
	public static bool CalculateDirectionBool(IElectricityIO From, IElectricityIO To, bool Upstream)
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

	public static bool CalculateDirectionFromID(IElectricityIO On, int TheID)
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
		foreach (KeyValuePair<IElectricityIO, float> CurrentItem in On.Data.ResistanceComingFrom[On.Data.FirstPresent])
		{

			if (!(On.Data.ResistanceComingFrom[TheID].ContainsKey(CurrentItem.Key)))
			{
				isTrue = false;
			}
		}
		return (isTrue);
	}

	public static float WorkOutResistance(Dictionary<IElectricityIO, float> ResistanceSources)
	{ //Worked out per source
		float ResistanceXAll = 0;
		foreach (KeyValuePair<IElectricityIO, float> Source in ResistanceSources)
		{
			ResistanceXAll += 1 / Source.Value;
		}
		return (1 / ResistanceXAll);
	}

	public static float WorkOutCurrent(Dictionary<IElectricityIO, float> ReceivingCurrents)
	{ //Worked out per source
		float Current = 0;
		foreach (KeyValuePair<IElectricityIO, float> Source in ReceivingCurrents)
		{

			Current += Source.Value;
		}
		return (Current);
	}

	public static Electricity WorkOutActualNumbers(IElectricityIO ElectricItem)
	{  //Sometimes gives wrong readings at junctions, Needs to be looked into
		float Current = 0; //Calculates the actual voltage and current flowing through the Node
		float Voltage = 0;
		Dictionary<IElectricityIO, float> AnInterestingDictionary = new Dictionary<IElectricityIO, float>();
		foreach (KeyValuePair<int, float> CurrentIDItem in ElectricItem.Data.SourceVoltages) { //Voltages easy to work out just add up all the voltages from different sources
			Voltage += CurrentIDItem.Value;
		}
		foreach (KeyValuePair<int, Dictionary<IElectricityIO, float>> CurrentIDItem in ElectricItem.Data.CurrentComingFrom)
		{
			foreach (KeyValuePair<IElectricityIO, float> CurrentItem in CurrentIDItem.Value) //Tricky for current since it can flow one way or the other
			{ 
				if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
				{
					AnInterestingDictionary[CurrentItem.Key] += CurrentItem.Value;
				}
				else
				{
					AnInterestingDictionary[CurrentItem.Key] = CurrentItem.Value;
				}
			}
			if (ElectricItem.Data.CurrentGoingTo.ContainsKey(CurrentIDItem.Key))
			{
				foreach (KeyValuePair<IElectricityIO, float> CurrentItem in ElectricItem.Data.CurrentGoingTo[CurrentIDItem.Key])
				{
					if (AnInterestingDictionary.ContainsKey(CurrentItem.Key))
					{
						AnInterestingDictionary[CurrentItem.Key] += -CurrentItem.Value;
					}
					else
					{
						AnInterestingDictionary[CurrentItem.Key] = -CurrentItem.Value;
					}
				}
			}
		}
		foreach (KeyValuePair<IElectricityIO, float> CurrentItem in AnInterestingDictionary)
		{
			if (CurrentItem.Value > 0)
			{
				Current += CurrentItem.Value;
			}
		}
		//Logger.Log (Voltage.ToString () + " < yeah Those voltage " + Current.ToString() + " < yeah Those Current " + (Voltage/Current).ToString() + " < yeah Those Resistance" + ElectricItem.GameObject().name.ToString() + " < at", Category.Electrical);
		Electricity Cabledata = new Electricity();
		Cabledata.Current = Current;
		Cabledata.Voltage = Voltage;
		Cabledata.EstimatedResistant = Voltage / Current;
		return (Cabledata);
	}

	public static void CircuitSearchLoop(IElectricityIO Thiswire, IProvidePower ProvidingPower)
	{
		InputOutputFunctions.DirectionOutput(Thiswire.GameObject(), Thiswire);
		bool Break = true;
		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO>();
		while (Break)
		{
			IterateDirectionWorkOnNextList = new List<IElectricityIO>(ProvidingPower.DirectionWorkOnNextList);
			ProvidingPower.DirectionWorkOnNextList.Clear();
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
			{
				IterateDirectionWorkOnNextList[i].DirectionOutput(Thiswire.GameObject());
			}
			if (ProvidingPower.DirectionWorkOnNextList.Count <= 0)
			{
				IterateDirectionWorkOnNextList = new List<IElectricityIO>(ProvidingPower.DirectionWorkOnNextListWait);
				ProvidingPower.DirectionWorkOnNextListWait.Clear();
				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
				{
					IterateDirectionWorkOnNextList[i].DirectionOutput(Thiswire.GameObject());
				}
			}
			if (ProvidingPower.DirectionWorkOnNextList.Count <= 0 && ProvidingPower.DirectionWorkOnNextListWait.Count <= 0)
			{
				//Logger.Log ("stop!");
				Break = false;
			}
		}
	}

	//	public static void CircuitResistanceLoop(IElectricityIO Thiswire, IProvidePower ProvidingPower ){
	//		bool Break = true;
	//		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> ();
	//		while (Break) {
	//			IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.ResistanceWorkOnNextList);
	//			ProvidingPower.ResistanceWorkOnNextList.Clear();
	//			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
	//				IterateDirectionWorkOnNextList [i].ResistancyOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
	//			}
	//			if (ProvidingPower.ResistanceWorkOnNextList.Count <= 0) {
	//				IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.ResistanceWorkOnNextListWait);
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
