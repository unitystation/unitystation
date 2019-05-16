using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TransformerModule : ElectricalModuleInheritance
{
	[Header("Transformer Settings")]
	public float TurnRatio; //the Turn ratio of the transformer so if it 2, 1v in 2v out 
	public bool InvertingTurnRatio;  //what it will be limited to
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
		if (InvertingTurnRatio) {
			TurnRatio = 1 / TurnRatio;
		}
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

		float Resistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.Data.SupplyDependent[InstanceID].ResistanceComingFrom);
		float Voltage = (Current * Resistance);		//float Voltage = ElectricityFunctions.WorkOutVoltage(ControllingNode.Node);

		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance" + ActualCurrent.ToString() + " < ActualCurrent" + Current.ToString() + " < Current");
		Tuple<float, float> Currentandoffcut = TransformerCalculations.TransformerCalculate(this, Voltage: Voltage, ResistanceModified: Resistance, ActualCurrent: ActualCurrent);
		if (Currentandoffcut.Item2 > 0)
		{
			ControllingNode.Node.Data.SupplyDependent[InstanceID].CurrentGoingTo[ControllingNode.Node.GameObject().GetComponent<ElectricalOIinheritance>()] = Currentandoffcut.Item2;
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
