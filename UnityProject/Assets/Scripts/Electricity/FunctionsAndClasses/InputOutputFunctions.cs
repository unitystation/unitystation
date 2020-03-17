using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Telepathy;

public static class InputOutputFunctions //for all the date of formatting of   Output / Input
{
	public static System.Random RNG = new System.Random();

	public static void ElectricityOutput(WrapCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 ElectricalOIinheritance ComingFrom,
										 ElectricalOIinheritance Thiswire,
										 ElectricalDirectionStep Path)
	{
		Dictionary<ElectricalDirectionStep, float> ResistanceDirectionDictionary = new Dictionary<ElectricalDirectionStep, float>();
		Dictionary<ElectricalDirectionStep, WrapCurrent> DirectionDictionary = new Dictionary<ElectricalDirectionStep, WrapCurrent>();


		float TotalCurrentSplit = 0;


		foreach (var JumpTo in Path.Downstream)
		{
			float res = 0;
			var has = new Dictionary<Resistance, int>();
			foreach (var _Resistanc in Path.Downstream)
			{
				foreach (var AA in _Resistanc.resistance.ReturnBearResistances()) {
					if (has.ContainsKey(AA))
					{
						has[AA] = has[AA] + 1;
					}
					else {
						has[AA] = 1;
					}
				}

			}


			res = JumpTo.resistance.Resistance(has);
			ResistanceDirectionDictionary[JumpTo] = res;
			//Logger.Log(res + " this res");
			if (res != 0)
			{
				TotalCurrentSplit = TotalCurrentSplit + 1 / res;
			}
		}

		//Logger.Log(TotalCurrentSplit + "TotalrestSplit");
		if (TotalCurrentSplit != 0)
		{
			TotalCurrentSplit = ((float)Current.SendingCurrent * (1 / TotalCurrentSplit));
		}

		foreach (var JumpTo in Path.Downstream)
		{

			var newCurrent = new WrapCurrent();
			float Resistance = 0;

			Resistance = ResistanceDirectionDictionary[JumpTo];

			newCurrent.SetUp(Current);
			newCurrent.SendingCurrent = (float)TotalCurrentSplit / Resistance;
	
			//Logger.Log(Thiswire.Data.SupplyDependent[SourceInstanceID].ResistanceComingFrom[JumpTo.InData].ResistanceFromResistanceForces(JumpTo.Sources) + " Makes it so that it sending " + newCurrent.SendingCurrent + " Total TotalCurrentSplit " + TotalCurrentSplit);
			DirectionDictionary[JumpTo] = newCurrent;
		}



		foreach (var JumpTo in Path.Downstream)
		{
			var SupplyingCurrent = DirectionDictionary[JumpTo];
			if (!Thiswire.Data.SupplyDependent[SourceInstance].CurrentGoingTo.ContainsKey(JumpTo.InData))
			{
				Thiswire.Data.SupplyDependent[SourceInstance].CurrentGoingTo[JumpTo.InData] = new VIRCurrent();
			}
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentGoingTo[JumpTo.InData].addCurrent(SupplyingCurrent);

			if (JumpTo.InData.Present != null)
			{
				//Logger.Log(SupplyingCurrent + " < Current  " + JumpTo?.InData?.Present + " < Jumping to");
				var newCurrent = new WrapCurrent();
				newCurrent.SetUp(SupplyingCurrent);
				JumpTo.InData.Present.ElectricityInput(newCurrent, SourceInstance, Thiswire, JumpTo);
			}
		}
	}


	public static void ElectricityInput(WrapCurrent Current,
										ElectricalOIinheritance SourceInstance,
										ElectricalOIinheritance ComingFrom,
										ElectricalDirectionStep Path,
										ElectricalOIinheritance Thiswire)
	{
		//addd where it been
		//if (Current != 0)
		//{
			//Logger.Log(Current + " <Current "
			//			+ SourceInstance.ToString()
			//			+ " <SourceInstance " + ComingFrom.ToString()
			//			+ " <ComingFrom " + Thiswire.ToString()
			//		   + " <Thiswire ", Category.Electrical);
		//}

		//ElectricalSynchronisation.InputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];


		if (!Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom.ContainsKey(ComingFrom.InData))
		{
			Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom.InData] = new VIRCurrent();
		}
		WrapCurrent newCurrent = new WrapCurrent();
		newCurrent.SetUp(Current);
		Thiswire.Data.SupplyDependent[SourceInstance].CurrentComingFrom[ComingFrom.InData].addCurrent(newCurrent);


		if (!(Thiswire.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Count > 0))
		{
			ElectricalSynchronisation.StructureChange = true;
			ElectricalSynchronisation.NUStructureChangeReact.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUResistanceChange.Add(Thiswire.InData.ControllingDevice);
			ElectricalSynchronisation.NUCurrentChange.Add(Thiswire.InData.ControllingDevice);
			Logger.LogErrorFormat("Resistance isn't initialised on", Category.Electrical);
			return;
		}

		Thiswire.Data.SupplyDependent[SourceInstance].SourceVoltages.Add(Current.SendingCurrent *
		ComingFrom.Data.SupplyDependent[SourceInstance].ResistanceComingFrom[Thiswire.InData].Resistance());
		//Logger.Log(Thiswire.Data.CurrentStoreValue + " <Current ");
		Thiswire.ElectricityOutput(Current, SourceInstance, ComingFrom, Path);
		//Thiswire.ElectricityOutput()
	}









	public static void ResistancyOutput(ResistanceWrap Resistance,
										ResistanceWrap UnmodifiedResistance,
										ElectricalOIinheritance SourceInstance,
										List<ElectricalDirectionStep> Directions,
										ElectricalOIinheritance Thiswire

									   )
	{
		//ElectricalSynchronisation.OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];



		var ToNext = new Dictionary<ElectricalDirectionStep, List<ElectricalDirectionStep>>();
		foreach (var Direction in Directions)
		{
			if (Direction != null)
			{
				if (!ToNext.ContainsKey(Direction))
				{
					ToNext[Direction] = new List<ElectricalDirectionStep>();
				}
				//Logger.Log("Jump from" + Thiswire.name + " to " + Direction?.Node.name);
				ToNext[Direction].Add(Direction.Upstream);
				Direction.Sources.Add(Resistance.resistance);
	
				Direction.resistance.AddResistance(Resistance);
				///Direction.Downstream 
				if (Direction.Upstream != null)
				{
					//Logger.Log("seting " + Direction?.InData.Present?.name + "  as the Downstream of  " + Direction?.Upstream?.InData.Present?.name + "\n"
					//          + "with " + Direction.Upstream.DownstreamCount + " Already present and adding " + Direction.DownstreamCount);
					Direction.Upstream.Downstream.Add(Direction);
					//Direction.Upstream.DownstreamCount += 1;
				}
			}
		}

		foreach (var JumpTo in ToNext)
		{
			if (!Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.ContainsKey(JumpTo.Key.InData))
			{
				Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo.Key.InData] = new VIRResistances();
			}



			Thiswire.Data.SupplyDependent[SourceInstance].ResistanceGoingTo[JumpTo.Key.InData].AddResistance(UnmodifiedResistance);
	

			var NResistance = new ResistanceWrap();
			NResistance.SetUp(Resistance);
			//Modify with transformer stuff here
			JumpTo.Key.InData.Present.ResistanceInput(NResistance, SourceInstance, Thiswire.InData, new List<ElectricalDirectionStep>(JumpTo.Value));
		}
	}

	public static void ResistanceInput(ResistanceWrap Resistance,
									   ElectricalOIinheritance SourceInstance,
									   IntrinsicElectronicData ComingFrom,
									   List<ElectricalDirectionStep> Directions,
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

			var NResistance = new ResistanceWrap();
			NResistance.SetUp(Resistance);

			Thiswire.ResistancyOutput(NResistance, SourceInstance, Directions);
		}
	}

	public static void DirectionOutput(GameObject SourceInstance,
									   ElectricalOIinheritance Thiswire,
									   List<ElectricalDirectionStep> Directions,
									   CableLine RelatedLine = null)
	{
		//int SourceInstanceID = SourceInstance.GetInstanceID();
		//if (Thiswire.Data.connections.Count == 0)
		//{
		//	Thiswire.FindPossibleConnections();
		//}
		//if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstanceID)))
		//{
		//	Thiswire.Data.SupplyDependent[SourceInstanceID] = new ElectronicSupplyData();
		//}
		//ElectricalSynchronisation.OutputSupplyingUsingData = Thiswire.Data.SupplyDependent[SourceInstanceID];
		//foreach (ElectricalOIinheritance Related in Thiswire.Data.connections)
		//{
		//	if (!(ElectricalSynchronisation.OutputSupplyingUsingData.Upstream.Contains(Related)) && (!(Thiswire == Related)))
		//	{
		//		bool pass = true;
		//		if (RelatedLine != null)
		//		{
		//			if (RelatedLine.Covering.Contains(Related))
		//			{
		//				pass = false;
		//			}
		//		}
		//		if (!(ElectricalSynchronisation.OutputSupplyingUsingData.Downstream.Contains(Related)) && pass)
		//		{
		//			ElectricalSynchronisation.OutputSupplyingUsingData.Downstream.Add(Related);
		//			Related.DirectionInput(SourceInstance, Thiswire);
		//		}
		//	}
		//}
	}

	public static void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalOIinheritance Thiswire)
	{
		//if (Thiswire.Data.connections.Count == 0)
		//{
		//	Thiswire.FindPossibleConnections(); //plz don't remove it is necessary for preventing incomplete cleanups when there has been multiple
		//}
		//int SourceInstanceID = SourceInstance.GetInstanceID();
		//if (!(Thiswire.Data.SupplyDependent.ContainsKey(SourceInstanceID)))
		//{
		//	Thiswire.Data.SupplyDependent[SourceInstanceID] = new ElectronicSupplyData();
		//}
		//if (ComingFrom != null)
		//{
		//	Thiswire.Data.SupplyDependent[SourceInstanceID].Upstream.Add(ComingFrom);
		//}

		//bool CanPass = true;
		//if (Thiswire.InData.ConnectionReaction.ContainsKey(ComingFrom.InData.Categorytype))
		//{
		//	if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReaction || Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].ResistanceReaction)
		//	{
		//		ElectricalOIinheritance SourceInstancPowerSupply = SourceInstance.GetComponent<ElectricalOIinheritance>();
		//		if (SourceInstancPowerSupply != null)
		//		{
		//			if (!Thiswire.Data.ResistanceToConnectedDevices.ContainsKey(SourceInstancPowerSupply))
		//			{
		//				Thiswire.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply] = new HashSet<PowerTypeCategory>();
		//			}
		//			Thiswire.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply].Add(ComingFrom.InData.Categorytype);
		//			SourceInstancPowerSupply.connectedDevices.Add(Thiswire);
		//			ElectricalSynchronisation.InitialiseResistanceChange.Add(Thiswire.InData.ControllingDevice);
		//		}
		//		if (Thiswire.InData.ConnectionReaction[ComingFrom.InData.Categorytype].DirectionReactionA.YouShallNotPass)
		//		{
		//			CanPass = false;
		//		}
		//	}
		//}
		//if (CanPass)
		//{
		//	if (Thiswire.Data.connections.Count > 2)
		//	{
		//		ElectricalSynchronisation.DirectionWorkOnNextListWaitADD(Thiswire);
		//	}
		//	else
		//	{
		//		ElectricalSynchronisation.DirectionWorkOnNextListADD(Thiswire);
		//	}
		//}
	}
}