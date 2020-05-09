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

		if (Thiswire.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
		{
			if (supplyDep.CurrentComingFrom.TryGetValue(ComingFrom, out VIRCurrent currentComFrom))
			{
				currentComFrom.addCurrent(Current);
			}
			else
			{
				supplyDep.CurrentComingFrom[ComingFrom] = Current;
			}

			if (!(supplyDep.ResistanceComingFrom.Count > 0))
			{
				var sync = ElectricalManager.Instance.electricalSync;
				sync.StructureChange = true;
				sync.NUStructureChangeReact.Add(Thiswire.ControllingDevice);
				sync.NUResistanceChange.Add(Thiswire.ControllingDevice);
				sync.NUCurrentChange.Add(Thiswire.ControllingDevice);
				Logger.LogErrorFormat("Resistance isn't initialised on", Category.Electrical);
				return;
			}

			supplyDep.SourceVoltage = (float)Current.Current() * (ElectricityFunctions.WorkOutResistance(supplyDep.ResistanceComingFrom));
		}

		//ELCurrent.CurrentWorkOnNextListADD(Thiswire);
		Thiswire.ElectricityOutput(Current, SourceInstance);
	}

	public static void ResistancyOutput(ResistanceWrap Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData Thiswire)
	{
		if (Thiswire.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
		{
			foreach (var JumpTo in supplyDep.Upstream)
			{
				if (!supplyDep.ResistanceGoingTo.TryGetValue(JumpTo, out VIRResistances resGoTo))
				{
					resGoTo = supplyDep.ResistanceGoingTo[JumpTo] = ElectricalPool.GetVIRResistances();
				}
				resGoTo.AddResistance(Resistance);
				JumpTo.ResistanceInput(Resistance, SourceInstance, Thiswire);
			}
		}
	}

	public static void ResistanceInput(ResistanceWrap Resistance,
									   ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData ComingFrom,
									   IntrinsicElectronicData Thiswire)
	{
		if (Thiswire.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
		{
			if (!supplyDep.ResistanceComingFrom.TryGetValue(ComingFrom, out VIRResistances resComeFrom))
			{
				resComeFrom = supplyDep.ResistanceComingFrom[ComingFrom] = ElectricalPool.GetVIRResistances();
			}
			resComeFrom.AddResistance(Resistance);
		}

		Thiswire.ResistancyOutput(Resistance, SourceInstance);
	}

	public static void DirectionOutput(ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData Thiswire,
									   CableLine RelatedLine = null)
	{
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections();
		}

		if (!(Thiswire.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData outputSupplyingUsingData)))
		{
			outputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstance] = ElectricalPool.GetElectronicSupplyData();
		}

		foreach (IntrinsicElectronicData Relatedindata in Thiswire.Data.connections)
		{
			if (!(outputSupplyingUsingData.Upstream.Contains(Relatedindata)) && (!(Thiswire == Relatedindata)))
			{
				bool pass = true;
				if (RelatedLine != null)
				{
					if (RelatedLine.Covering.Contains(Relatedindata))
					{
						pass = false;
					}
				}
				if (!(outputSupplyingUsingData.Downstream.Contains(Relatedindata)) && pass)
				{
					outputSupplyingUsingData.Downstream.Add(Relatedindata);
					Relatedindata.DirectionInput(SourceInstance, Thiswire);
				}
			}
		}
	}

	public static void DirectionInput(ElectricalOIinheritance SourceInstance,
									  IntrinsicElectronicData ComingFrom,
									  IntrinsicElectronicData Thiswire)
	{
		if (Thiswire.Data.connections.Count == 0)
		{
			Thiswire.FindPossibleConnections(); //plz don't remove it is necessary for preventing incomplete cleanups when there has been multiple
		}

		if (!Thiswire.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
		{
			supplyDep = Thiswire.Data.SupplyDependent[SourceInstance] = ElectricalPool.GetElectronicSupplyData();
		}

		if (ComingFrom != null)
		{
			supplyDep.Upstream.Add(ComingFrom);
		}

		if (Thiswire.ConnectionReaction.TryGetValue(ComingFrom.Categorytype, out PowerInputReactions reaction))
		{
			if (reaction.DirectionReaction || reaction.ResistanceReaction)
			{
				if (SourceInstance != null)
				{
					if (!Thiswire.Data.ResistanceToConnectedDevices.TryGetValue(SourceInstance, out var resToConDev))
					{
						resToConDev = Thiswire.Data.ResistanceToConnectedDevices[SourceInstance] = new Dictionary<Resistance, HashSet<IntrinsicElectronicData>>();
					}

					if (!resToConDev.TryGetValue(reaction.ResistanceReactionA.Resistance, out HashSet<IntrinsicElectronicData> resToConDevHash))
					{
						resToConDevHash = resToConDev[reaction.ResistanceReactionA.Resistance] = new HashSet<IntrinsicElectronicData>();
					}
					resToConDevHash.Add(ComingFrom);

					SourceInstance.connectedDevices.Add(Thiswire);
					ElectricalManager.Instance.electricalSync.InitialiseResistanceChange.Add(Thiswire.ControllingDevice);
				}
				if (reaction.DirectionReactionA.YouShallNotPass)
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