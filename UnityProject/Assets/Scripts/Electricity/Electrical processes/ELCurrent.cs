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
				float SupplyingCurrent = 0;
				float Voltage = Node.Data.CurrentStoreValue * (ElectricityFunctions.WorkOutResistance(Node.Data.ResistanceComingFrom[SourceInstanceID]));
				foreach (KeyValuePair<ElectricalOIinheritance, float> JumpTo in Node.Data.ResistanceComingFrom[SourceInstanceID])
				{
					if (Voltage > 0)
					{
						SupplyingCurrent = (Voltage / JumpTo.Value);
					}
					else
					{
						SupplyingCurrent = Node.Data.CurrentStoreValue;
					}
					if (!(Node.Data.CurrentGoingTo.ContainsKey(SourceInstanceID)))
					{
						Node.Data.CurrentGoingTo[SourceInstanceID] = new Dictionary<ElectricalOIinheritance, float>();
					}
					Node.Data.CurrentGoingTo[SourceInstanceID][JumpTo.Key] = SupplyingCurrent;
					if (!JumpTo.Key.InData.ElectricityOverride)
					{
						//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
						if (!(JumpTo.Key.Data.SourceVoltages.ContainsKey(SourceInstanceID)))
						{
							JumpTo.Key.Data.SourceVoltages[SourceInstanceID] = new float();
						}
						if (!(JumpTo.Key.Data.CurrentComingFrom.ContainsKey(SourceInstanceID)))
						{
							JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID] = new Dictionary<ElectricalOIinheritance, float>();
						}
						JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID][Node] = SupplyingCurrent;
						JumpTo.Key.Data.SourceVoltages[SourceInstanceID] = SupplyingCurrent * (ElectricityFunctions.WorkOutResistance(JumpTo.Key.Data.ResistanceComingFrom[SourceInstanceID]));
						CurrentWorkOnNextListADD(JumpTo.Key);
						JumpTo.Key.Data.CurrentStoreValue = ElectricityFunctions.WorkOutCurrent(JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID]);
						//JumpTo.Key.ElectricityOutput(ElectricityFunctions.WorkOutCurrent(JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID]), SourceInstance);
					}
					else {
						JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Node);
					}
					ElectricityFunctions.WorkOutActualNumbers(Node);
				}
			}
			else {
				Node.ElectricityOutput(Node.Data.CurrentStoreValue, SourceInstance);
			}
		}
	}
}
