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
		
		//Logger.Log("Currentloop", Category.Electrical);
		SourceInstanceID = SourceInstance.GetInstanceID();
		bool Break = true;
		while (Break) {
			//Logger.Log(CurrentWorkOnNextList.Count.ToString() + "how many processing in Currentloop", Category.Electrical);
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
						ElectricalSynchronisation.OutputSupplyingUsingData.CurrentComingFrom[Node] = SupplyingCurrent;
						ElectricalSynchronisation.OutputSupplyingUsingData.SourceVoltages = SupplyingCurrent * (ElectricityFunctions.WorkOutResistance(ElectricalSynchronisation.OutputSupplyingUsingData.ResistanceComingFrom));
						//Logger.Log(SupplyingCurrent.ToString() + " <Current " + SourceInstance.ToString() + " <SourceInstance " + Node.ToString() + " <ComingFrom " + JumpTo.ToString() + " <Thiswire ", Category.Electrical);
						CurrentWorkOnNextListADD(JumpTo.Key);
						JumpTo.Key.Data.CurrentStoreValue = ElectricityFunctions.WorkOutCurrent(ElectricalSynchronisation.OutputSupplyingUsingData.CurrentComingFrom);
						}
					else {
						JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Node);
					}
				}
			}
			else {
				Node.ElectricityOutput(Node.Data.CurrentStoreValue, SourceInstance);
			}
		}
	}
}
