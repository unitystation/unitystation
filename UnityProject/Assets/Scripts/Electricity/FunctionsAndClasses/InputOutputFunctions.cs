using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputOutputFunctions //for all the date of formatting of   Output / Input
{
	public static void ElectricityOutput(float Current, GameObject SourceInstance, ElectricalOIinheritance Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		ElectricalSynchronisation.OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
		float SupplyingCurrent = 0;
		float Voltage = Current * (ElectricityFunctions.WorkOutResistance(ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom));
		foreach (KeyValuePair<ElectricalOIinheritance, float> JumpTo in ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom)
		{
			if (Voltage > 0)
			{
				SupplyingCurrent = (Voltage / JumpTo.Value);
			}
			else
			{
				SupplyingCurrent = Current;
			}
			ElectricalSynchronisation.OutputSupplyingUsingData.CurrentGoingTo[JumpTo.Key] = SupplyingCurrent;
			JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Thiswire);
		}
	}

	public static void ElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalOIinheritance Thiswire)
	{
		//Logger.Log (" <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		ElectricalSynchronisation.InputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
		ElectricalSynchronisation.InputSupplyingUsingData.CurrentComingFrom[ComingFrom] = Current;
		if (!(ElectricalSynchronisation.InputSupplyingUsingData.ResistanceComingFrom.Count > 0))
		{
			ElectricalSynchronisation.StructureChange = true;
			ElectricalSynchronisation.NUStructureChangeReact.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUResistanceChange.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUCurrentChange.Add(Thiswire.InData.ControllingDevice);
			//Logger.LogErrorFormat("Resistance isn't initialised on {1}", Category.Electrical, SourceInstance);
			return;
		}
		ElectricalSynchronisation.InputSupplyingUsingData.SourceVoltages = Current * (ElectricityFunctions.WorkOutResistance(ElectricalSynchronisation.InputSupplyingUsingData.ResistanceComingFrom));
		ELCurrent.CurrentWorkOnNextListADD(Thiswire);
		Thiswire.Data.CurrentStoreValue = ElectricityFunctions.WorkOutCurrent(ElectricalSynchronisation.InputSupplyingUsingData.CurrentComingFrom);
	}

	public static void ResistancyOutput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance Thiswire)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		ElectricalSynchronisation.OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
		float ResistanceSplit = 0;
		if (Resistance == 0)
		{
			ResistanceSplit = 0;
		}
		else {
			if (ElectricalSynchronisation.OutputSupplyingUsingData.Upstream.Count > 1)
			{
				ResistanceSplit = Resistance * ElectricalSynchronisation.OutputSupplyingUsingData.Upstream.Count;
			}
			else
			{
				ResistanceSplit = Resistance;
			}
		}
		foreach (ElectricalOIinheritance JumpTo in ElectricalSynchronisation.OutputSupplyingUsingData.Upstream)
		{
			if (ResistanceSplit == 0)
			{
				ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceGoingTo.Remove(JumpTo);
			}
			else {
				ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceGoingTo[JumpTo] = ResistanceSplit;
			}
			JumpTo.ResistanceInput(ResistanceSplit, SourceInstance, Thiswire);
		}
	}

	public static void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalOIinheritance Thiswire, int ComingFromOverride = 0)
	{
		ElectricalOIinheritance IElec = SourceInstance.GetComponent<ElectricalOIinheritance>();
		if (ComingFrom == null)
		{
			if (IElec != Thiswire)
			{
				if (Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(IElec))
				{
					if (Thiswire.Data.ResistanceToConnectedDevices[IElec].Count > 1)
					{
						Logger.LogErrorFormat("{0} has too many resistance reactions specified.", Category.Electrical, Thiswire.ToString());
					}
					foreach (PowerTypeCategory ConnectionFrom in Thiswire.Data.ResistanceToConnectedDevices[IElec])
					{
						Resistance = Thiswire.InData.ConnectionReaction[ConnectionFrom].ResistanceReactionA.Resistance.Ohms;
						ComingFrom = ElectricalManager.Instance.defaultDeadEnd;
					}
				}
			}
			else {
				return;
			}
		}
		if (ComingFrom != null | ElectricalManager.Instance.defaultDeadEnd == ComingFrom)
		{
			int SourceInstanceID = SourceInstance.GetInstanceID();
			ElectricalSynchronisation.InputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
			if (Resistance == 0)
			{
				ElectricalSynchronisation.InputSupplyingUsingData.ResistanceComingFrom.Remove(ComingFrom);
			}
			else {
				ElectricalSynchronisation.InputSupplyingUsingData.ResistanceComingFrom[ComingFrom] = Resistance;
			}
			if (Thiswire.Data.connections.Count > 2)
			{
				KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> edd = new KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>(IElec, Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextListWaitADD(edd);
			}
			else
			{
				KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> edd = new KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>(IElec, Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextListADD(edd);
			}
		}
	}

	public static void DirectionOutput(GameObject SourceInstance, ElectricalOIinheritance Thiswire, CableLine RelatedLine = null)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections();
		}
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.SupplyDependent[SourceInstanceID] = new ElectronicSupplyData();
		}
		ElectricalSynchronisation.OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
		foreach (ElectricalOIinheritance Related in Thiswire.Data.connections)
		{
			if (!(ElectricalSynchronisation.OutputSupplyingUsingData.Upstream.Contains(Related)) && (!(Thiswire == Related)))
			{
				bool pass = true;
				if (RelatedLine != null)
				{
					if (RelatedLine.Covering.Contains(Related))
					{
						pass = false;
					}
				}
				if (!(ElectricalSynchronisation.OutputSupplyingUsingData.Downstream.Contains(Related)) && pass)
				{
					ElectricalSynchronisation.OutputSupplyingUsingData.Downstream.Add(Related);
					Related.DirectionInput(SourceInstance, Thiswire);
				}
			}
		}
	}

	public static void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalOIinheritance Thiswire)
	{
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections(); //plz don't remove it is necessary for preventing incomplete cleanups when there has been multiple
		}
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstanceID))) {
			Thiswire.Data.SupplyDependent[SourceInstanceID] = new ElectronicSupplyData();
		}
		if (ComingFrom != null)
		{
			Thiswire.Data.SupplyDependent[SourceInstanceID].Upstream.Add(ComingFrom);
		}

		bool CanPass = true;
		if (Thiswire.InData.ConnectionReaction.ContainsKey(ComingFrom.InData.Categorytype))
		{
			if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReaction || Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].ResistanceReaction)
			{
				ElectricalOIinheritance SourceInstancPowerSupply = SourceInstance.GetComponent<ElectricalOIinheritance>();
				if (SourceInstancPowerSupply != null)
				{
					if (!Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(SourceInstancPowerSupply))
					{
						Thiswire.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply] = new HashSet<PowerTypeCategory>();
					}
					Thiswire.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply].Add(ComingFrom.InData.Categorytype);
					SourceInstancPowerSupply.connectedDevices.Add(Thiswire);
					ElectricalSynchronisation.InitialiseResistanceChange.Add(Thiswire.InData.ControllingDevice);
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
				ElectricalSynchronisation.DirectionWorkOnNextListWaitADD(Thiswire);
			}
			else
			{
				ElectricalSynchronisation.DirectionWorkOnNextListADD(Thiswire);
			}
		}
	}
}