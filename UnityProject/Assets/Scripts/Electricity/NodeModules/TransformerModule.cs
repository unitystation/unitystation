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

	public List<PowerTypeCategory> HighsideConnections = new List<PowerTypeCategory>();
	public List<PowerTypeCategory> LowsideConnections = new List<PowerTypeCategory>();
	public PowerTypeCategory TreatComingFromNullAs;
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
	public override float ModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		int InstanceID = SourceInstance.GetInstanceID();
		float Resistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.Data.SupplyDependent[InstanceID].ResistanceComingFrom);
		float Voltage = (Current * Resistance);

		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance"  + Current.ToString() + " < Current");
		Tuple<float, float> Currentandoffcut = TransformerCalculations.ElectricalStageTransformerCalculate(this, Voltage: Voltage, ResistanceModified: Resistance, FromHighSide : HighsideConnections.Contains(ComingFrom.InData.Categorytype)  );
		if (Currentandoffcut.Item2 > 0)
		{
			ControllingNode.Node.Data.SupplyDependent[InstanceID].CurrentGoingTo[ControllingNode.Node] = Currentandoffcut.Item2;
		}
		return (Currentandoffcut.Item1);
	}
	public override float ModifyResistancyOutput(float Resistance, GameObject SourceInstance)
	{
		//return (Resistance);		int InstanceID = SourceInstance.GetInstanceID();
		bool FromHighSide = false;
		foreach (var Upst in ControllingNode.Node.Data.SupplyDependent[InstanceID].Upstream) {
			if (LowsideConnections.Contains(Upst.InData.Categorytype)) {
				FromHighSide = true;
			}
		}
			
		Tuple<float, float> ResistanceM = TransformerCalculations.ResistanceStageTransformerCalculate(this, ResistanceToModify: Resistance, FromHighSide : FromHighSide);
		return (ResistanceM.Item1);
	}

}
