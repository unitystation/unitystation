using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;


public class SMES : InputTrigger, IElectricalNeedUpdate, IBattery
{
	public PowerSupply powerSupply;

	//Is the SMES turned on
	[SyncVar(hook="UpdateState")]
	public bool isOn = false;
	public bool isOnForInterface {get; set;}  = false;
	public bool ChangeToOff = false;
	public bool PassChangeToOff  {get; set;} = false;
	public int currentCharge; // 0 - 100
	public float current {get; set;} = 20;
	public float Previouscurrent = 20;
	public float PreviousResistance = 0;

	//Sprites:
	public Sprite offlineSprite;
	public Sprite onlineSprite;
	public Sprite[] chargeIndicatorSprites;
	public Sprite statusCriticalSprite;
	public Sprite statusSupplySprite;
	//

	public int DirectionStart = 0;
	public int DirectionEnd = 9;
	//


	public float MinimumSupportVoltage  {get; set;} = 2700;
	public float StandardSupplyingVoltage {get; set;} = 3000;
	public float PullingWatts {get; set;} = 0;
	public float CapacityMax {get; set;} = 1800000;
	public float CurrentCapacity {get; set;} = 1800000;
	public float PullLastDeductedTime {get; set;}= 0;
	public float ChargLastDeductedTime {get; set;} = 0;


	public float ExtraChargeCutOff {get; set;} = 3000;
	public float IncreasedChargeVoltage {get; set;} = 3010;
	public float StandardChargeNumber {get; set;} = 100;
	public float ChargeSteps {get; set;} = 0.1f;
	public float MaxChargingMultiplier {get; set;} = 1.5f;
	public float ChargingMultiplier {get; set;} = 0.1f;
	public float MaximumCurrentSupport {get; set;} = 3;

	public float ChargingWatts {get; set;} = 0;
	public float Resistance {get; set;} = 0;
	public float CircuitResistance {get; set;} = 0;

	public bool CanCharge {get; set;} = true;
	public bool Cansupport {get; set;} = true;
	public bool ToggleCanCharge {get; set;} = true;
	public bool ToggleCansupport {get; set;} = true;


	public float ActualVoltage {get; set;}  = 0;
	//Renderers:
	public SpriteRenderer statusIndicator;
	public SpriteRenderer OnOffIndicator;
	public SpriteRenderer chargeIndicator;


	public PowerTypeCategory ApplianceType = PowerTypeCategory.SMES;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.MediumMachineConnector
	};

	public override void OnStartClient(){
		base.OnStartClient();
		powerSupply.CanConnectTo = CanConnectTo;
		powerSupply.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;

		//UpdateState(isOn);

		//Test
		currentCharge = 100;
	}
	public void PowerUpdateStructureChange(){
		//if (current > 0) {
		powerSupply.PowerUpdateStructureChange ();
		//} 

	}
	public void PowerUpdateStructureChangeReact(){
		//if (current > 0) { Potential optimisation
		powerSupply.PowerUpdateStructureChangeReact ();
		//}
	}

	public void PowerUpdateResistanceChange(){
		//if (current > 0) {
		powerSupply.PowerUpdateResistanceChange ();
		//}
	}
	public void PowerUpdateCurrentChange (){
		powerSupply.FlushSupplyAndUp (powerSupply.gameObject); //Room for optimisation
		CircuitResistance = ElectricityFunctions.WorkOutResistance (powerSupply.ResistanceComingFrom [powerSupply.gameObject.GetInstanceID ()]);// //!!
		ActualVoltage = powerSupply.ActualVoltage;

		BatteryCalculation.PowerUpdateCurrentChange (this);


		if (current != Previouscurrent) {
			if (Previouscurrent == 0 && !(current <= 0)) {
				//

			} else if (current == 0 && !(Previouscurrent <= 0)) { 
				powerSupply.FlushSupplyAndUp (powerSupply.gameObject);
				//powerSupply.TurnOffSupply(); 
			}

			powerSupply.SupplyingCurrent = current;
			Previouscurrent = current;
		}
		//if (current > 0) {
		powerSupply.PowerUpdateCurrentChange ();
		//}
	}

	public void PowerNetworkUpdate (){
		powerSupply.PowerNetworkUpdate ();
		ActualVoltage = powerSupply.ActualVoltage;

		BatteryCalculation.PowerNetworkUpdate (this);



		if (ChangeToOff) {
			ChangeToOff = false;
			//PassChangeToOff = true;
			//ElectricalSynchronisation.ResistanceChange = true;
			//ElectricalSynchronisation.CurrentChange = true;
			//powerSupply.TurnOffSupply(); 
			powerSupply.TurnOffSupply();
			BatteryCalculation.TurnOffEverything (this);
			ElectricalSynchronisation.RemoveSupply (this, ApplianceType);
		}

		if (current != Previouscurrent) {
			if (Previouscurrent == 0 && !(current <= 0)) {
				//

			} else if (current == 0 && !(Previouscurrent <= 0)) { 
				Logger.Log ("FlushSupplyAndUp");
				powerSupply.FlushSupplyAndUp (powerSupply.gameObject);
				//powerSupply.TurnOffSupply(); 
			}
			powerSupply.SupplyingCurrent = current;
			Previouscurrent = current;
			ElectricalSynchronisation.CurrentChange = true;
		}

		if (Resistance != PreviousResistance) {
			if (PreviousResistance == 0 && !(Resistance == 0)) {
				powerSupply.CanProvideResistance = true;

			} else if (Resistance == 0 && !(PreviousResistance <= 0)) { 
				powerSupply.CanProvideResistance = false;
				ElectricityFunctions.CleanConnectedDevices (powerSupply);
			}

			powerSupply.PassedDownResistance = Resistance;
			PreviousResistance = Resistance;
			ElectricalSynchronisation.ResistanceChange = true;
			ElectricalSynchronisation.CurrentChange = true;
		}
		Logger.Log (CurrentCapacity.ToString() + " < CurrentCapacity", Category.Electrical);
	}
	//Update the current State of the SMES (sprites and statistics) 
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
			OnOffIndicator.sprite = onlineSprite;
			chargeIndicator.gameObject.SetActive (true);
			statusIndicator.gameObject.SetActive (true);

			int chargeIndex = (currentCharge / 100) * 4;
			chargeIndicator.sprite = chargeIndicatorSprites [chargeIndex];
			if (chargeIndex == 0) {
				statusIndicator.sprite = statusCriticalSprite;
			} else {
				statusIndicator.sprite = statusSupplySprite;
			}



		} else {
			Logger.Log ("off");
			ChangeToOff = true;
			OnOffIndicator.sprite = offlineSprite;
			chargeIndicator.gameObject.SetActive (false);
			statusIndicator.gameObject.SetActive (false);

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

