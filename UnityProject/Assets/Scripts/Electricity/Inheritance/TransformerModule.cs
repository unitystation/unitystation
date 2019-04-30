using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TransformerModule : ElectricalModuleInheritance
{
	[Header("Transformer Settings")]
	public float TurnRatio; //the Turn ratio of the transformer so if it 2, 1v in 2v out 
	public float VoltageLimiting; //If it requires VoltageLimiting and  At what point the VoltageLimiting will kick in
	public float VoltageLimitedTo;  //what it will be limited to

	public void BroadcastSetUpMessage(ElectricalNodeControl Node)
	{
		RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
		{
			ElectricalUpdateTypeCategory.ModifyElectricityInput,
			ElectricalUpdateTypeCategory.ModifyResistancyOutput,
		};
		ModuleType = ElectricalModuleTypeCategory.Transformer;
		ControllingNode = Node;
		Node.AddModule(this);
	}
	public override void OnStartServer()
	{
	}
	//ModifyElectricityOutput!!!
	public override float ModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		int InstanceID = SourceInstance.GetInstanceID();

		float ActualCurrent = ControllingNode.Node.Data.CurrentInWire;

		float Resistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance" + ActualCurrent.ToString() + " < ActualCurrent" + Current.ToString() + " < Current");
		Tuple<float, float> Currentandoffcut = TransformerCalculations.TransformerCalculate(this, Voltage: Voltage, ResistanceModified: Resistance, ActualCurrent: ActualCurrent);
		if (Currentandoffcut.Item2 > 0)
		{
			if (!(ControllingNode.Node.Data.CurrentGoingTo.ContainsKey(InstanceID)))
			{
				ControllingNode.Node.Data.CurrentGoingTo[InstanceID] = new Dictionary<ElectricalOIinheritance, float>();
			}
			ControllingNode.Node.Data.CurrentGoingTo[InstanceID][ControllingNode.Node.GameObject().GetComponent<ElectricalOIinheritance>()] = Currentandoffcut.Item2;
		}
		//return (Current);
		return (Currentandoffcut.Item1);
	}
	public override float ModifyResistancyOutput(float Resistance, GameObject SourceInstance)
	{
		Tuple<float, float> ResistanceM = TransformerCalculations.TransformerCalculate(this, ResistanceToModify: Resistance);
		//return (Resistance);
		return (ResistanceM.Item1);
	}

}
