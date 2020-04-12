using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Telepathy;

public static class InputOutputFunctions //for all the date of formatting of   Output / Input
{

	public static void ElectricityOutput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 IntrinsicElectronicData Thiswire)
	{
		//Logger.Log("4 > " + Current);
		//Logger.Log("poke > " + SourceInstance.InData.Data.SupplyDependent[SourceInstance].ToString());
		var OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstance];
		VIRCurrent SupplyingCurrent = null;
		float Divider = (ElectricityFunctions.WorkOutResistance(OutputSupplyingUsingData.ResistanceComingFrom));
		foreach (KeyValuePair<IntrinsicElectronicData, VIRResistances> JumpTo in OutputSupplyingUsingData.ResistanceComingFrom)
		{
			if (OutputSupplyingUsingData.ResistanceComingFrom.Count > 1)
			{
				SupplyingCurrent = Current.SplitCurrent(Divider / JumpTo.Value.Resistance());
			}
			else
			{
				SupplyingCurrent = Current;
			}
			OutputSupplyingUsingData.CurrentGoingTo[JumpTo.Key] = SupplyingCurrent;
			if (JumpTo.Key != null && JumpTo.Key.Categorytype != PowerTypeCategory.DeadEndConnection)
			{
				JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Thiswire);
			}
		}
	}


	public static void ElectricityInput(VIRCurrent Current,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData ComingFrom,
										IntrinsicElectronicData Thiswire)
	{
		//Logger.Log("ElectricityInput" + Thiswire + "  ComingFrom  > " + ComingFrom);
		//Logger.Log("5 > " + Current + "  Categorytype > " + Thiswire.Categorytype + "  ComingFrom  > " + ComingFrom.Categorytype);
		//Logger.Log("poke > " + SourceInstance.InData.Data.SupplyDependent[SourceInstance].ToString());
		if (!Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom.ContainsKey(ComingFrom))
		{
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom] = Current;
		}
		else {
			//Logger.Log("AADD");
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom].addCurrent(Current);
		}



		if (!(Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Count > 0))
		{
			var sync = ElectricalManager.Instance.electricalSync;
			sync.StructureChange = true;
			sync.NUStructureChangeReact.Add(Thiswire.ControllingDevice);
			sync.NUResistanceChange.Add(Thiswire.ControllingDevice);
			sync.NUCurrentChange.Add(Thiswire.ControllingDevice);
			Logger.LogErrorFormat("Resistance isn't initialised on", Category.Electrical);
			return;
		}

		Thiswire.Data.SupplyDependent[SourceInstance].SourceVoltage = (float)Current.Current() * (ElectricityFunctions.WorkOutResistance(Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom));
		//ELCurrent.CurrentWorkOnNextListADD(Thiswire);
		Thiswire.ElectricityOutput(Current, SourceInstance);
	}

	public static void ResistancyOutput(ResistanceWrap Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData Thiswire)
	{
		foreach (var JumpTo in Thiswire.Data.SupplyDependent[SourceInstance].Upstream)
		{
			if (!Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.ContainsKey(JumpTo))
			{
				Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo] = ElectricalPool.GetVIRResistances();
			}

			Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo].AddResistance(Resistance);

			JumpTo.ResistanceInput(Resistance, SourceInstance, Thiswire);
		}
	}

	public static void ResistanceInput(ResistanceWrap Resistance,
									   ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData ComingFrom,
									   IntrinsicElectronicData Thiswire)
	{
		if (!Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.ContainsKey(ComingFrom))
		{
			Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[ComingFrom] = ElectricalPool.GetVIRResistances();
		}

		Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[ComingFrom].AddResistance(Resistance);

		Thiswire.ResistancyOutput(Resistance,SourceInstance);

	}

	public static void DirectionOutput(ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData Thiswire,
									   CableLine RelatedLine = null)
	{
		//Logger.Log(Thiswire.Categorytype + "DirectionOutput");
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections();
		}
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstance)))
		{
			Thiswire.Data.SupplyDependent[SourceInstance] = ElectricalPool.GetElectronicSupplyData();
		}
		var OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstance];
		foreach (IntrinsicElectronicData Relatedindata in Thiswire.Data.connections)
		{
			if (!(OutputSupplyingUsingData.Upstream.Contains(Relatedindata)) && (!(Thiswire == Relatedindata)))
			{
				bool pass = true;
				if (RelatedLine != null)
				{
					if (RelatedLine.Covering.Contains(Relatedindata))
					{
						pass = false;
					}
				}
				if (!(OutputSupplyingUsingData.Downstream.Contains(Relatedindata)) && pass)
				{
					OutputSupplyingUsingData.Downstream.Add(Relatedindata);
					Relatedindata.DirectionInput(SourceInstance, Thiswire);
				}
			}
		}
	}

	public static void DirectionInput(ElectricalOIinheritance SourceInstance,
	                                  IntrinsicElectronicData ComingFrom,
	                                  IntrinsicElectronicData Thiswire)
	{
		//Logger.Log(Thiswire.Categorytype + "DirectionInput");
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections(); //plz don't remove it is necessary for preventing incomplete cleanups when there has been multiple
		}
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstance)))
		{
			Thiswire.Data.SupplyDependent[SourceInstance] = ElectricalPool.GetElectronicSupplyData();
		}
		if (ComingFrom != null)
		{
			Thiswire.Data.SupplyDependent[SourceInstance].Upstream.Add(ComingFrom);
		}

		if (Thiswire.ConnectionReaction.ContainsKey(ComingFrom.Categorytype))
		{
			var Reaction = Thiswire.ConnectionReaction[ComingFrom.Categorytype];
			if (Reaction.DirectionReaction || Reaction.ResistanceReaction)
			{
				if (SourceInstance != null)
				{
					if (!Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(SourceInstance))
					{
						Thiswire.Data.ResistanceToConnectedDevices[SourceInstance] = new Dictionary<Resistance, HashSet<IntrinsicElectronicData>>();
					}
					if (!Thiswire.Data.ResistanceToConnectedDevices[SourceInstance].ContainsKey(Reaction.ResistanceReactionA.Resistance))
					{
						Thiswire.Data.ResistanceToConnectedDevices[SourceInstance][Reaction.ResistanceReactionA.Resistance]
								= new HashSet<IntrinsicElectronicData>();
					}

					Thiswire.Data.ResistanceToConnectedDevices[SourceInstance][Reaction.ResistanceReactionA.Resistance]
							.Add(ComingFrom);
					SourceInstance.connectedDevices.Add(Thiswire);
					ElectricalManager.Instance.electricalSync.InitialiseResistanceChange.Add(Thiswire.ControllingDevice);
				}
				if (Thiswire.ConnectionReaction[ComingFrom.Categorytype].DirectionReactionA.YouShallNotPass)
				{
					return;
				}
			}
		}

		if (Thiswire.Data.connections.Count > 2)
		{
			ElectricalManager.Instance.electricalSync.DirectionWorkOnNextListWaitADD(Thiswire);
		}
		else
		{
			ElectricalManager.Instance.electricalSync.DirectionWorkOnNextListADD(Thiswire);
		}
	}
}