using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

using UnityEngine.Networking;


public class Transformer : NetworkBehaviour, IInLineDevices, Itransformer {
	public float TurnRatio  {get; set;} 
	public float VoltageLimiting {get; set;} 
	public float VoltageLimitedTo  {get; set;}
	public InLineDevice RelatedDevice; 
	public PowerTypeCategory ApplianceType = PowerTypeCategory.Transformer;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.HighVoltageCable,
	};
	public override void OnStartClient()
	{
		base.OnStartClient();
		TurnRatio = 250;
		VoltageLimiting = 3300;
		VoltageLimitedTo = 3300;
		RelatedDevice.RelatedDevice = this;
		RelatedDevice.CanConnectTo = CanConnectTo;
		RelatedDevice.Categorytype = ApplianceType;
	}

	public float ModifyElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){
		return(Current);
	}
	public float ModifyElectricityOutput(int tick, float Current, GameObject SourceInstance){
		int InstanceID = SourceInstance.GetInstanceID ();

		float ActualCurrent = RelatedDevice.CurrentInWire;

		float Resistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		Tuple<float,float> Currentandoffcut = ElectricityFunctions.TransformerCalculations (this,Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0) {
			if (!(RelatedDevice.CurrentGoingTo.ContainsKey (InstanceID))) {
				RelatedDevice.CurrentGoingTo [InstanceID] = new Dictionary<IElectricityIO, float> ();
			}
			RelatedDevice.CurrentGoingTo[InstanceID] [RelatedDevice.GameObject().GetComponent<IElectricityIO>()] = Currentandoffcut.Item2;
		}
		return(Currentandoffcut.Item1);
	}

	public float ModifyResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		Tuple<float,float> ResistanceM = ElectricityFunctions.TransformerCalculations (this, ResistanceToModify : Resistance);
		return(ResistanceM.Item1);
	}
	public float ModifyResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		return(Resistance);
	}

	public void OnDestroy(){
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		//Then you can destroy
	}
}
