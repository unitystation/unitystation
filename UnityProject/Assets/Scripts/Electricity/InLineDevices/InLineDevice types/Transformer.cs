using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 
using UnityEngine.Networking;


public class Transformer : PowerSupplyControlInheritance 
{
	public override void OnStartServerInitialise()
	{
		TurnRatio = 250;
		VoltageLimiting = 3300;
		VoltageLimitedTo = 3300;
		powerSupply.RelatedDevice = this;
		CanConnectTo = new HashSet<PowerTypeCategory>{
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.HighVoltageCable,
		};
		ApplianceType = PowerTypeCategory.Transformer;
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
	}

	public override float ModifyElectricityOutput( float Current, GameObject SourceInstance){
		int InstanceID = SourceInstance.GetInstanceID ();
		float ActualCurrent = powerSupply.Data.CurrentInWire;
		float Resistance = ElectricityFunctions.WorkOutResistance(powerSupply.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		Tuple<float,float> Currentandoffcut = TransformerCalculations.TransformerCalculate (this,Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0) {
			if (!(powerSupply.Data.CurrentGoingTo.ContainsKey (InstanceID))) {
				powerSupply.Data.CurrentGoingTo [InstanceID] = new Dictionary<ElectricalOIinheritance, float> ();
			}
			powerSupply.Data.CurrentGoingTo[InstanceID] [powerSupply.GameObject().GetComponent<ElectricalOIinheritance>()] = Currentandoffcut.Item2;
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
