using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputOutputFunctions
{
	public static void ElectricityOutput(int tick, float Current, GameObject SourceInstance, IElectricityIO Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float SimplyTimesBy = 0;
		float SupplyingCurrent = 0;
		Dictionary<IElectricityIO, float> ThiswireResistance = new Dictionary<IElectricityIO, float>();
		if (Thiswire.Data.ResistanceComingFrom.ContainsKey(SourceInstanceID))
		{
			ThiswireResistance = Thiswire.Data.ResistanceComingFrom[SourceInstanceID];
		}
		else
		{
			Logger.LogError("now It doesn't" + SourceInstanceID.ToString() + " with this " + Thiswire.GameObject().name.ToString(), Category.Electrical);

		}
		float Voltage = Current * (ElectricityFunctions.WorkOutResistance(ThiswireResistance));
		foreach (KeyValuePair<IElectricityIO, float> JumpTo in Thiswire.Data.ResistanceComingFrom[SourceInstanceID])
		{
			if (Voltage > 0)
			{
				SupplyingCurrent = (Voltage / JumpTo.Value);
			}
			else
			{
				SupplyingCurrent = Current;
			}
			if (!(Thiswire.Data.CurrentGoingTo.ContainsKey(SourceInstanceID)))
			{
				Thiswire.Data.CurrentGoingTo[SourceInstanceID] = new Dictionary<IElectricityIO, float>();
			}
			Thiswire.Data.CurrentGoingTo[SourceInstanceID][JumpTo.Key] = SupplyingCurrent;
			JumpTo.Key.ElectricityInput(tick, SupplyingCurrent, SourceInstance, Thiswire);
		}
	}

	public static void ElectricityInput(int tick, float Current, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire)
	{
		//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (!(Thiswire.Data.SourceVoltages.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.SourceVoltages[SourceInstanceID] = new float();
		}
		if (!(Thiswire.Data.CurrentComingFrom.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.CurrentComingFrom[SourceInstanceID] = new Dictionary<IElectricityIO, float>();
		}
		Thiswire.Data.CurrentComingFrom[SourceInstanceID][ComingFrom] = Current;
		Thiswire.Data.SourceVoltages[SourceInstanceID] = Current * (ElectricityFunctions.WorkOutResistance(Thiswire.Data.ResistanceComingFrom[SourceInstanceID]));
		Thiswire.ElectricityOutput(tick, ElectricityFunctions.WorkOutCurrent(Thiswire.Data.CurrentComingFrom[SourceInstanceID]), SourceInstance);

	}

	public static void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float ResistanceSplit = 0;
		if (Resistance == 0) {
			ResistanceSplit = 0;
		} else {
			if (Thiswire.Data.Upstream[SourceInstanceID].Count > 1)
			{
				float CalculatedCurrent = 1000 / Resistance;
				float CurrentSplit = CalculatedCurrent / (Thiswire.Data.Upstream[SourceInstanceID].Count);
				ResistanceSplit = 1000 / CurrentSplit;
			}
			else
			{
				ResistanceSplit = Resistance;
			}
		}

		foreach (IElectricityIO JumpTo in Thiswire.Data.Upstream[SourceInstanceID])
		{
			if (ResistanceSplit == 0) {
				Thiswire.Data.ResistanceGoingTo[SourceInstanceID].Remove (JumpTo);
			} else {
				Thiswire.Data.ResistanceGoingTo[SourceInstanceID][JumpTo] = ResistanceSplit;
			}
			JumpTo.ResistanceInput(tick, ResistanceSplit, SourceInstance, Thiswire);
		}
	}

	public static void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire)
	{
		if (ComingFrom == null)
		{
			var IElec = SourceInstance.GetComponent<IElectricityIO>();
			if (Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(IElec))
			{
				if (Thiswire.Data.ResistanceToConnectedDevices [SourceInstance.GetComponent<IElectricityIO> ()].Count > 1) {
					Logger.Log ("oh no!, problem!!!!");
				}
				foreach (PowerTypeCategory ConnectionFrom in Thiswire.Data.ResistanceToConnectedDevices[SourceInstance.GetComponent<IElectricityIO>()])
				{

					Resistance = Thiswire.InData.ConnectionReaction[ConnectionFrom].ResistanceReactionA.Resistance.Ohms;
					//Logger.Log (Resistance.ToString () + " < to man Resistance |            " + ConnectionFrom.ToString() + " < to man ConnectionFrom |      " + Thiswire.GameObject().name + " < to man IS ");
					ComingFrom = ElectricalSynchronisation.DeadEnd;

				}
			}
		}
		if (ComingFrom != null) {

			int SourceInstanceID = SourceInstance.GetInstanceID();
			if (!(Thiswire.Data.ResistanceComingFrom.ContainsKey (SourceInstanceID))) {
				Thiswire.Data.ResistanceComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
			} 
				
			if (!(Thiswire.Data.ResistanceGoingTo.ContainsKey(SourceInstanceID)))
			{
				Thiswire.Data.ResistanceGoingTo[SourceInstanceID] = new Dictionary<IElectricityIO, float>();
			}
				
			if (Resistance == 0) {
				Thiswire.Data.ResistanceComingFrom [SourceInstanceID].Remove (ComingFrom);
			} else {
				Thiswire.Data.ResistanceComingFrom[SourceInstanceID][ComingFrom] = Resistance;
			}
				
			if (Thiswire.Data.connections.Count > 2)
			{
				KeyValuePair<IElectricityIO,IElectricityIO> edd = new KeyValuePair<IElectricityIO,IElectricityIO> (SourceInstance.GetComponent<IElectricityIO> (),Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextListWait.Add (edd);
				//Logger.Log("Bdded");
			}
			else
			{
				KeyValuePair<IElectricityIO,IElectricityIO> edd = new KeyValuePair<IElectricityIO,IElectricityIO> (SourceInstance.GetComponent<IElectricityIO> (),Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextList.Add (edd);
				//Logger.Log("added");
			}
		}
	}

	public static void DirectionOutput(int tick, GameObject SourceInstance, IElectricityIO Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (!(Thiswire.Data.Upstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Upstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (!(Thiswire.Data.Downstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Downstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (Thiswire.Data.connections.Count <= 0)
		{
			Thiswire.FindPossibleConnections();
		}
		for (int i = 0; i < Thiswire.Data.connections.Count; i++)
		{
			if (!(Thiswire.Data.Upstream[SourceInstanceID].Contains(Thiswire.Data.connections[i])) && (!(Thiswire == Thiswire.Data.connections[i])))
			{

				if (!(Thiswire.Data.Downstream.ContainsKey(SourceInstanceID)))
				{
					Thiswire.Data.Downstream[SourceInstanceID] = new HashSet<IElectricityIO>();
				}
				if (!(Thiswire.Data.Downstream[SourceInstanceID].Contains(Thiswire.Data.connections[i])))
				{
					Thiswire.Data.Downstream[SourceInstanceID].Add(Thiswire.Data.connections[i]);

					Thiswire.Data.connections[i].DirectionInput(tick, SourceInstance, Thiswire);
				}
			}
		}
	}

	public static void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (Thiswire.Data.FirstPresent == 0)
		{
			Thiswire.Data.FirstPresent = SourceInstanceID;
		}
		if (!(Thiswire.Data.Upstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Upstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (!(Thiswire.Data.Downstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Downstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (ComingFrom != null)
		{
			Thiswire.Data.Upstream[SourceInstanceID].Add(ComingFrom);
		}

		bool CanPass = true;
		if (Thiswire.InData.ConnectionReaction.ContainsKey(ComingFrom.InData.Categorytype))
		{
			if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReaction)
			{
				IProvidePower SourceInstancPowerSupply = SourceInstance.GetComponent<IProvidePower>();
				if (SourceInstancPowerSupply != null)
				{
					IElectricityIO IElectricityIOPowerSupply = SourceInstance.GetComponent<IElectricityIO>();
					if (!Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(IElectricityIOPowerSupply))
					{
						Thiswire.Data.ResistanceToConnectedDevices[IElectricityIOPowerSupply] = new HashSet<PowerTypeCategory>();
					}
					Thiswire.Data.ResistanceToConnectedDevices[IElectricityIOPowerSupply].Add(ComingFrom.InData.Categorytype);
					SourceInstancPowerSupply.connectedDevices.Add(Thiswire);
					ElectricalSynchronisation.InitialiseResistanceChange.Add (Thiswire.InData.ControllingUpdate);
				}

				if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReactionA.YouShallNotPass)
				{
					CanPass = false;
				}
			}
		}
		if (CanPass)
		{
			if (Thiswire.Data.connections.Count > 2)
			{
				SourceInstance.GetComponent<IProvidePower>().DirectionWorkOnNextListWait.Add(Thiswire);
			}
			else
			{
				SourceInstance.GetComponent<IProvidePower>().DirectionWorkOnNextList.Add(Thiswire);
			}
		}
	}
}