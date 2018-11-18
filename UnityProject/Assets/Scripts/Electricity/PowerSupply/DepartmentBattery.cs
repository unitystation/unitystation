using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

using UnityEngine.Networking;


public class DepartmentBattery : InputTrigger , IElectricalNeedUpdate, IInLineDevices, Itransformer,  IBattery
{
	public InLineDevice RelatedDevice; 

	//Is the SMES turned on
	[SyncVar(hook="UpdateState")]
	public bool isOn = false;
	public bool isOnForInterface {get; set;}  = false;
	public bool ChangeToOff = false;
	public bool PassChangeToOff  {get; set;} = false;
	public int currentCharge; // 0 - 100
	public float current {get; set;} = 0;
	public float Previouscurrent = 0;
	public float PreviousResistance = 0;
	//Sprites:
	public int DirectionStart = 0;
	public int DirectionEnd = 9;


	public float TurnRatio  {get; set;} 
	public float VoltageLimiting {get; set;} 
	public float VoltageLimitedTo  {get; set;}


//	##        Dictionary['Maximum_Current_support'] = 8  

	public float MaximumCurrentSupport {get; set;} = 8;
	public float MinimumSupportVoltage  {get; set;} = 216;
	public float StandardSupplyingVoltage {get; set;} = 240;
	public float PullingWatts {get; set;} = 0;
	public float CapacityMax {get; set;} = 432000;
	public float CurrentCapacity {get; set;} = 432000;
	public float PullLastDeductedTime {get; set;}= 0;
	public float ChargLastDeductedTime {get; set;} = 0;


	public float ExtraChargeCutOff {get; set;} = 240;
	public float IncreasedChargeVoltage {get; set;} = 250;
	public float StandardChargeNumber {get; set;} = 10;
	public float ChargeSteps {get; set;} = 0.1f;
	public float MaxChargingMultiplier {get; set;} = 1.2f;
	public float ChargingMultiplier {get; set;} = 0.1f;

	public float ChargingWatts {get; set;} = 0;
	public float Resistance {get; set;} = 0;
	public float CircuitResistance {get; set;} = 0;

	public bool CanCharge {get; set;} = true;
	public bool Cansupport {get; set;} = true;
	public bool ToggleCanCharge {get; set;} = true;
	public bool ToggleCansupport {get; set;} = true;

	public float ActualVoltage {get; set;}  = 0;


	public PowerTypeCategory ApplianceType = PowerTypeCategory.DepartmentBattery;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.MediumMachineConnector
	};

	public override void OnStartClient(){
		base.OnStartClient();
		RelatedDevice.CanConnectTo = CanConnectTo;
		RelatedDevice.Categorytype = ApplianceType;
		RelatedDevice.DirectionStart = DirectionStart;
		RelatedDevice.DirectionEnd = DirectionEnd;

		TurnRatio = 12.5f;
		VoltageLimiting = 0;
		VoltageLimitedTo = 0;
		RelatedDevice.RelatedDevice = this;
		//UpdateState(isOn);

		//Test
		currentCharge = 100;
		//UpdateState(isOn);
	}
	public void PowerUpdateStructureChange(){
		RelatedDevice.PowerUpdateStructureChange ();
	}
	public void PowerUpdateStructureChangeReact(){
		RelatedDevice.PowerUpdateStructureChangeReact ();
	}
	public void PowerUpdateResistanceChange(){
		RelatedDevice.PowerUpdateResistanceChange ();
	}
	public void PowerUpdateCurrentChange (){
		RelatedDevice.FlushSupplyAndUp (RelatedDevice.gameObject); //Room for optimisation
		CircuitResistance = ElectricityFunctions.WorkOutResistance (RelatedDevice.ResistanceComingFrom [RelatedDevice.gameObject.GetInstanceID ()]);// //!!
		ActualVoltage = RelatedDevice.ActualVoltage;

		BatteryCalculation.PowerUpdateCurrentChange (this);


		if (current != Previouscurrent) {
			if (Previouscurrent == 0 && !(current <= 0)) {
				//

			} else if (current == 0 && !(Previouscurrent <= 0)) { 
				RelatedDevice.FlushSupplyAndUp (RelatedDevice.gameObject);
				//powerSupply.TurnOffSupply(); 
			}

			RelatedDevice.SupplyingCurrent = current;
			Previouscurrent = current;
		}
		//if (current > 0) {
		RelatedDevice.PowerUpdateCurrentChange ();
		//}
	}

	public void PowerNetworkUpdate (){
		RelatedDevice.PowerNetworkUpdate ();
		ActualVoltage = RelatedDevice.ActualVoltage;

		BatteryCalculation.PowerNetworkUpdate (this);



		if (ChangeToOff) {
			ChangeToOff = false;
			//PassChangeToOff = true;
			//ElectricalSynchronisation.ResistanceChange = true;
			//ElectricalSynchronisation.CurrentChange = true;
			//powerSupply.TurnOffSupply(); 
			RelatedDevice.TurnOffSupply();
			BatteryCalculation.TurnOffEverything (this);
			ElectricalSynchronisation.RemoveSupply (this, ApplianceType);
		}

		if (current != Previouscurrent) {
			if (Previouscurrent == 0 && !(current <= 0)) {
				//

			} else if (current == 0 && !(Previouscurrent <= 0)) { 
				Logger.Log ("FlushSupplyAndUp");
				RelatedDevice.FlushSupplyAndUp (RelatedDevice.gameObject);
				//powerSupply.TurnOffSupply(); 
			}
			RelatedDevice.SupplyingCurrent = current;
			Previouscurrent = current;
			ElectricalSynchronisation.CurrentChange = true;
		}

		if (Resistance != PreviousResistance) {
			if (PreviousResistance == 0 && !(Resistance == 0)) {
				RelatedDevice.CanProvideResistance = true;

			} else if (Resistance == 0 && !(PreviousResistance <= 0)) { 
				RelatedDevice.CanProvideResistance = false;
				ElectricityFunctions.CleanConnectedDevices (RelatedDevice);
			}

			RelatedDevice.PassedDownResistance = Resistance;
			PreviousResistance = Resistance;
			ElectricalSynchronisation.ResistanceChange = true;
			ElectricalSynchronisation.CurrentChange = true;
		}
		Logger.Log (CurrentCapacity.ToString() + " < CurrentCapacity", Category.Electrical);
	}

	void UpdateState(bool _isOn){
		isOn = _isOn;
		isOnForInterface = _isOn;
		if (isOn) {
			ElectricalSynchronisation.AddSupply (this, ApplianceType);


			ElectricalSynchronisation.StructureChangeReact = true;
			ElectricalSynchronisation.ResistanceChange = true; //Potential optimisation
			ElectricalSynchronisation.CurrentChange = true;

			//				Resistance = (1000/((StandardChargeNumber * ChargingMultiplier)/1000));
			//				powerSupply.PassedDownResistance = Resistance;
			//				powerSupply.CanProvideResistance = true;

			//powerSupply.TurnOnSupply (0); //Test supply of 3000volts and 20amps, lol yeah
			Logger.Log ("on");
//			OnOffIndicator.sprite = onlineSprite;
//			chargeIndicator.gameObject.SetActive (true);
//			statusIndicator.gameObject.SetActive (true);
//
//			int chargeIndex = (currentCharge / 100) * 4;
//			chargeIndicator.sprite = chargeIndicatorSprites [chargeIndex];
//			if (chargeIndex == 0) {
//				statusIndicator.sprite = statusCriticalSprite;
//			} else {
//				statusIndicator.sprite = statusSupplySprite;
//			}



		} else {
			Logger.Log ("off");
			ChangeToOff = true;
//			OnOffIndicator.sprite = offlineSprite;
//			chargeIndicator.gameObject.SetActive (false);
//			statusIndicator.gameObject.SetActive (false);

		}
	}
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		//Interact stuff with the SMES here
		if (!isServer) {
			InteractMessage.Send(gameObject, hand);
		} else {
			if (!ChangeToOff) {
				isOn = !isOn;
			}

		}
	}



//	[ContextMethod("Turn on/Turn off","Power_Button")]
//	public void ToggleSupply(){
//		UpdateState (isOn);
//
//	}

	public float ModifyElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){
		int InstanceID = SourceInstance.GetInstanceID ();

		float ActualCurrent = RelatedDevice.CurrentInWire;

		float Resistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance" + ActualCurrent.ToString() + " < ActualCurrent" + Current.ToString() + " < Current");
		Tuple<float,float> Currentandoffcut = ElectricityFunctions.TransformerCalculations (this,Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0) {
			if (!(RelatedDevice.CurrentGoingTo.ContainsKey (InstanceID))) {
				RelatedDevice.CurrentGoingTo [InstanceID] = new Dictionary<IElectricityIO, float> ();
			}
			RelatedDevice.CurrentGoingTo[InstanceID] [RelatedDevice.GameObject().GetComponent<IElectricityIO>()] = Currentandoffcut.Item2;
		}
		return(Currentandoffcut.Item1);

	}
	public float ModifyElectricityOutput(int tick, float Current, GameObject SourceInstance){
		return(Current);
	}
	public float ModifyResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		return(Resistance);
	}
	public float ModifyResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		Tuple<float,float> ResistanceM = ElectricityFunctions.TransformerCalculations (this, ResistanceToModify : Resistance);
		return(ResistanceM.Item1);
	}

	[ContextMethod("Toggle Charge","Power_Button")]
	public void ToggleCharge(){
		ToggleCanCharge = !ToggleCanCharge;
	}
	[ContextMethod("Toggle Support","Power_Button")]
	public void ToggleSupport(){
		ToggleCansupport = !ToggleCansupport; 
	}

	public void OnDestroy(){
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.RemoveSupply (this, ApplianceType);
		//Then you can destroy
	}
}

