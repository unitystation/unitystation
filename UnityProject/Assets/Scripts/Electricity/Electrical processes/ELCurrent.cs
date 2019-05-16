using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ELCurrent
{
	public static HashSet<ElectricalOIinheritance> CurrentWorkOnNextList = new HashSet<ElectricalOIinheritance>();
	public static HashSet<ElectricalOIinheritance> WorkingList = new HashSet<ElectricalOIinheritance>();
	public static bool doWorkingList = false;
	public static int SourceInstanceID = 0;
	public static void CurrentWorkOnNextListADD(ElectricalOIinheritance toADD) {
		if (doWorkingList)
		{
			CurrentWorkOnNextList.Add(toADD);
		}
		else 
		{
			WorkingList.Add(toADD);
		}
	}

	public static void Currentloop(GameObject SourceInstance)
	{
		//Logger.Log("yo");
		SourceInstanceID = SourceInstance.GetInstanceID();
		bool Break = true;
		while (Break) {
			//Logger.Log(CurrentWorkOnNextList.Count.ToString() + "yeah man");
			doWorkingList = false;
			DoCurrentloop(CurrentWorkOnNextList, SourceInstance);
			CurrentWorkOnNextList.Clear();
			doWorkingList = true;
			DoCurrentloop(WorkingList, SourceInstance);
			WorkingList.Clear();

			if ((CurrentWorkOnNextList.Count <= 0) & (WorkingList.Count <= 0))
			{
				Break = false;
			}
		}	
	}
	public static void DoCurrentloop(HashSet<ElectricalOIinheritance> WorkingOn, GameObject SourceInstance) { 
		foreach (ElectricalOIinheritance Node in WorkingOn)
		{
			//Logger.Log("yow");
			if (!Node.InData.ElectricityOverride)
			{
				ElectricalSynchronisation.OutputSupplyingUsingData = Node.Data.SupplyDependent[SourceInstanceID];
				float SupplyingCurrent = 0;
				float Voltage = Node.Data.CurrentStoreValue * (ElectricityFunctions.WorkOutResistance(ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom));
				foreach (KeyValuePair<ElectricalOIinheritance, float> JumpTo in ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom)
				{
					if (Voltage > 0)
					{
						SupplyingCurrent = (Voltage / JumpTo.Value);
					}
					else
					{
						SupplyingCurrent = Node.Data.CurrentStoreValue;
					}
					ElectricalSynchronisation.OutputSupplyingUsingData.CurrentGoingTo[JumpTo.Key] = SupplyingCurrent;
					if (!JumpTo.Key.InData.ElectricityOverride)
					{
						ElectricalSynchronisation.OutputSupplyingUsingData = JumpTo.Key.Data.SupplyDependent[SourceInstanceID];
						//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
						ElectricalSynchronisation.OutputSupplyingUsingData.CurrentComingFrom[Node] = SupplyingCurrent;
						ElectricalSynchronisation.OutputSupplyingUsingData.SourceVoltages = SupplyingCurrent * (ElectricityFunctions.WorkOutResistance(ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom));
						CurrentWorkOnNextListADD(JumpTo.Key);
						JumpTo.Key.Data.CurrentStoreValue = ElectricityFunctions.WorkOutCurrent(ElectricalSynchronisation.OutputSupplyingUsingData.CurrentComingFrom);
						//JumpTo.Key.ElectricityOutput(ElectricityFunctions.WorkOutCurrent(JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID]), SourceInstance);
					}
					else {
						JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Node);
					}
					//ElectricityFunctions.WorkOutActualNumbers(Node);
				}
			}
			else {
				Node.ElectricityOutput(Node.Data.CurrentStoreValue, SourceInstance);
			}
		}
	}
}
