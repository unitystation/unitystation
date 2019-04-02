using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 
using UnityEngine.Networking;


public class Transformer : PowerSupplyControlInheritance 
{
	public InLineDevice RelatedDevice; 
	public PowerTypeCategory ApplianceType = PowerTypeCategory.Transformer;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.HighVoltageCable,
	};
	public override void OnStartServerInitialise()
	{
		TurnRatio = 250;
		VoltageLimiting = 3300;
		VoltageLimitedTo = 3300;
		RelatedDevice.RelatedDevice = this;
		RelatedDevice.InData.CanConnectTo = CanConnectTo;
		RelatedDevice.InData.Categorytype = ApplianceType;
	}

	public override float ModifyElectricityOutput( float Current, GameObject SourceInstance){
		int InstanceID = SourceInstance.GetInstanceID ();
		float ActualCurrent = RelatedDevice.Data.CurrentInWire;
		float Resistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		Tuple<float,float> Currentandoffcut = TransformerCalculations.TransformerCalculate (this,Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0) {
			if (!(RelatedDevice.Data.CurrentGoingTo.ContainsKey (InstanceID))) {
				RelatedDevice.Data.CurrentGoingTo [InstanceID] = new Dictionary<ElectricalOIinheritance, float> ();
			}
			RelatedDevice.Data.CurrentGoingTo[InstanceID] [RelatedDevice.GameObject().GetComponent<ElectricalOIinheritance>()] = Currentandoffcut.Item2;
		}
		//return(Current);
		return(Currentandoffcut.Item1);
	}

	public override float ModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom  ){
		Tuple<float,float> ResistanceM = TransformerCalculations.TransformerCalculate (this, ResistanceToModify : Resistance);
		//return(Resistance);
		return(ResistanceM.Item1);
	}
}
