using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ELCurrent
{
	public static HashSet<IElectricityIO> CurrentWorkOnNextList = new HashSet<IElectricityIO>();
	public static void Currentloop(GameObject SourceInstance)
	{
		//Logger.Log("yo");
		HashSet<IElectricityIO> WorkingList = new HashSet<IElectricityIO>();
		bool Break = true;
		while (Break) {
			//Logger.Log(CurrentWorkOnNextList.Count.ToString() + "yeah man");
			WorkingList = new HashSet<IElectricityIO>(CurrentWorkOnNextList);
			CurrentWorkOnNextList.Clear();
			foreach (IElectricityIO Node in WorkingList)
			{
				//Logger.Log("yow");
				if (!Node.InData.ElectricityOverride)
				{
					int SourceInstanceID = SourceInstance.GetInstanceID();
					//float SimplyTimesBy = 0;
					float SupplyingCurrent = 0;
					Dictionary<IElectricityIO, float> ThiswireResistance = new Dictionary<IElectricityIO, float>();
					if (Node.Data.ResistanceComingFrom.ContainsKey(SourceInstanceID))
					{
						ThiswireResistance = Node.Data.ResistanceComingFrom[SourceInstanceID];
					}
					else
					{
						Logger.LogError("now It doesn't" + SourceInstanceID.ToString() + " with this " + Node.GameObject().name.ToString(), Category.Electrical);

					}
					float Voltage = Node.Data.CurrentStoreValue * (ElectricityFunctions.WorkOutResistance(ThiswireResistance));
					foreach (KeyValuePair<IElectricityIO, float> JumpTo in Node.Data.ResistanceComingFrom[SourceInstanceID])
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
							Node.Data.CurrentGoingTo[SourceInstanceID] = new Dictionary<IElectricityIO, float>();
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
								JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID] = new Dictionary<IElectricityIO, float>();
							}
							JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID][Node] = SupplyingCurrent;
							JumpTo.Key.Data.SourceVoltages[SourceInstanceID] = SupplyingCurrent * (ElectricityFunctions.WorkOutResistance(JumpTo.Key.Data.ResistanceComingFrom[SourceInstanceID]));
							CurrentWorkOnNextList.Add(JumpTo.Key);
							JumpTo.Key.Data.CurrentStoreValue = ElectricityFunctions.WorkOutCurrent(JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID]);
							//JumpTo.Key.ElectricityOutput(ElectricityFunctions.WorkOutCurrent(JumpTo.Key.Data.CurrentComingFrom[SourceInstanceID]), SourceInstance);
						}
						else { 
							JumpTo.Key.ElectricityInput(SupplyingCurrent, SourceInstance, Node);
						}
					}
					Node.Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(Node);
					Node.Data.CurrentInWire = Node.Data.ActualCurrentChargeInWire.Current;
					Node.Data.ActualVoltage = Node.Data.ActualCurrentChargeInWire.Voltage;
					Node.Data.EstimatedResistance = Node.Data.ActualCurrentChargeInWire.EstimatedResistant;
					if (ElectricalSynchronisation.WireConnectRelated.Contains(Node.InData.Categorytype)) {
						Node.GameObject().GetComponent<WireConnect>().UpdateRelatedLine();
					}
				}
				else {
					Node.ElectricityOutput(Node.Data.CurrentStoreValue, SourceInstance);
				}
			}
			if (CurrentWorkOnNextList.Count <= 0)
			{
				Break = false;
			}
		}	
	}
}
