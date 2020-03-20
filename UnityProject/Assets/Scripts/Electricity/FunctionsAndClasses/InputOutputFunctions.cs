using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Telepathy;

public static class InputOutputFunctions //for all the date of formatting of   Output / Input
{

	public static void ElectricityOutput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 ElectricalOIinheritance Thiswire)
	{
		var OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstance];
		VIRCurrent SupplyingCurrent = null;
		float Divider = (ElectricityFunctions.WorkOutResistance(OutputSupplyingUsingData.ResistanceComingFrom));
		foreach (KeyValuePair<IntrinsicElectronicData, VIRResistances> JumpTo in OutputSupplyingUsingData.ResistanceComingFrom)
		{
			if (OutputSupplyingUsingData.ResistanceComingFrom.Count > 1)
			{
				SupplyingCurrent =  Current.SplitCurrent(Divider / JumpTo.Value.Resistance());
			}
			else
			{
				SupplyingCurrent = Current;
			}
			OutputSupplyingUsingData.CurrentGoingTo[JumpTo.Key] = SupplyingCurrent;
			if (JumpTo.Key?.Present != null)
			{
				JumpTo.Key.Present.ElectricityInput(SupplyingCurrent, SourceInstance, Thiswire);
			}
		}
	}


	public static void ElectricityInput(VIRCurrent Current,
										ElectricalOIinheritance SourceInstance,
										ElectricalOIinheritance ComingFrom,
										ElectricalOIinheritance Thiswire)
	{
		if (!Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom.ContainsKey(ComingFrom.InData))
		{
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom.InData] = Current;
		}
		else { 
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom.InData].addCurrent(Current);
		}



		if (!(Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Count > 0))
		{
			ElectricalSynchronisation.StructureChange = true;
			ElectricalSynchronisation.NUStructureChangeReact.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUResistanceChange.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUCurrentChange.Add(Thiswire.InData.ControllingDevice);
			Logger.LogErrorFormat("Resistance isn't initialised on", Category.Electrical);
			return;
		}

		Thiswire.Data.SupplyDependent[SourceInstance].SourceVoltages = (float) Current.Current() *
		ComingFrom.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[Thiswire.InData].Resistance();
		//Logger.Log(Thiswire.Data.CurrentStoreValue + " <Current ");
		Thiswire.ElectricityOutput(Current, SourceInstance);
		//Thiswire.ElectricityOutput()
	}









	public static void ResistancyOutput(VIRResistances Resistance,
										ElectricalOIinheritance SourceInstance,
										ElectricalOIinheritance Thiswire

									   )
	{


		foreach (var JumpTo in Thiswire.Data.SupplyDependent[SourceInstance].Upstream)
		{
			if (!Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.ContainsKey(JumpTo.InData))
			{
				Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo.InData] = new VIRResistances();
			}

			Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo.InData].AddResistance(Resistance);

			JumpTo.InData.Present.ResistanceInput(Resistance, SourceInstance, Thiswire.InData);
		}
	}

	public static void ResistanceInput(VIRResistances Resistance,
									   ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData ComingFrom,
									   ElectricalOIinheritance Thiswire)
	{



		if (ComingFrom != null)
		{
			if (!Thiswire.Data.SupplyDependent.ContainsKey(SourceInstance))
			{
				Thiswire.Data.SupplyDependent[SourceInstance] = new ElectronicSupplyData();
			}
			if (!Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.ContainsKey(ComingFrom))
			{
				Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[ComingFrom] = new VIRResistances();
			}

			Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[ComingFrom].AddResistance(Resistance);

			if (Thiswire.Data.connections.Count > 2)
			{
				KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> edd =
					new KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>(SourceInstance, Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextListWaitADD(edd);
			}
			else
			{
				KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> edd
				= new KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>(SourceInstance, Thiswire);
				ElectricalSynchronisation.ResistanceWorkOnNextListADD(edd);
			}
		}
	}

	public static void DirectionOutput(ElectricalOIinheritance SourceInstance,
									   ElectricalOIinheritance Thiswire,
									   CableLine RelatedLine = null)
	{
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections();
		}
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstance)))
		{
			Thiswire.Data.SupplyDependent[SourceInstance] = new ElectronicSupplyData();
		}
		var OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstance];
		foreach (ElectricalOIinheritance Related in Thiswire.Data.connections)
		{
			if (!(OutputSupplyingUsingData.Upstream.Contains(Related)) && (!(Thiswire == Related)))
			{
				bool pass = true;
				if (RelatedLine != null)
				{
					if (RelatedLine.Covering.Contains(Related.InData))
					{
						pass = false;
					}
				}
				if (!(OutputSupplyingUsingData.Downstream.Contains(Related)) && pass)
				{
					OutputSupplyingUsingData.Downstream.Add(Related);
					Related.DirectionInput(SourceInstance, Thiswire);
				}
			}
		}
	}

	public static void DirectionInput(ElectricalOIinheritance SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalOIinheritance Thiswire)
	{
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections(); //plz don't remove it is necessary for preventing incomplete cleanups when there has been multiple
		}
		if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstance)))
		{
			Thiswire.Data.SupplyDependent[SourceInstance] = new ElectronicSupplyData();
		}
		if (ComingFrom != null)
		{
			Thiswire.Data.SupplyDependent[SourceInstance].Upstream.Add(ComingFrom);
		}

		if (Thiswire.InData.ConnectionReaction.ContainsKey(ComingFrom.InData.Categorytype))
		{
			var Reaction = Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype];
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
							.Add(ComingFrom.InData);
					SourceInstance.connectedDevices.Add(Thiswire);
					ElectricalSynchronisation.InitialiseResistanceChange.Add(Thiswire.InData.ControllingDevice);
				}
				if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReactionA.YouShallNotPass)
				{
					return;
				}
			}
		}

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