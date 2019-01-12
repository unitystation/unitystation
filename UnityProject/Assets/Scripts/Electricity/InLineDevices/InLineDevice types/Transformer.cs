using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

using UnityEngine.Networking;


public class Transformer : NetworkBehaviour, IInLineDevices, Itransformer, IDeviceControl {
	private bool SelfDestruct = false;
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
		RelatedDevice.InData.CanConnectTo = CanConnectTo;
		RelatedDevice.InData.Categorytype = ApplianceType;
		RelatedDevice.InData.ControllingDevice = this;
	}

	public void PotentialDestroyed(){
		if (SelfDestruct) {
			//Then you can destroy
		}
	}

	public float ModifyElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){
		return(Current);
	}
	public float ModifyElectricityOutput(int tick, float Current, GameObject SourceInstance){
		int InstanceID = SourceInstance.GetInstanceID ();
		float ActualCurrent = RelatedDevice.Data.CurrentInWire;
		float Resistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		Tuple<float,float> Currentandoffcut = TransformerCalculations.TransformerCalculate (this,Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0) {
			if (!(RelatedDevice.Data.CurrentGoingTo.ContainsKey (InstanceID))) {
				RelatedDevice.Data.CurrentGoingTo [InstanceID] = new Dictionary<IElectricityIO, float> ();
			}
			RelatedDevice.Data.CurrentGoingTo[InstanceID] [RelatedDevice.GameObject().GetComponent<IElectricityIO>()] = Currentandoffcut.Item2;
		}
		//return(Current);
		return(Currentandoffcut.Item1);
	}

	public float ModifyResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		Tuple<float,float> ResistanceM = TransformerCalculations.TransformerCalculate (this, ResistanceToModify : Resistance);
		//return(Resistance);
		return(ResistanceM.Item1);
	}
	public float ModifyResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		return(Resistance);
	}

	public void OnDestroy(){
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		SelfDestruct = true;
		//Make Invisible
	}
	public void TurnOffCleanup (){
	}
}
