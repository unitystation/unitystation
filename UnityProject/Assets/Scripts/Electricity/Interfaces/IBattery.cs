using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBattery  { //Responsible for battery charging and Discharging
	bool isOnForInterface {get; set;}


	float MinimumSupportVoltage {get; set;} //At which point the battery kicks in
	float StandardSupplyingVoltage {get; set;}
	float PullingWatts {get; set;} 
	float CapacityMax {get; set;}
	float CurrentCapacity {get; set;}

	float PullLastDeductedTime {get; set;}
	float ChargLastDeductedTime {get; set;}
	float MaximumCurrentSupport {get; set;} //The maximum number of amps can be pulled 

	float StandardChargeNumber {get; set;} //set to 0 if Can never charge

	float ExtraChargeCutOff  {get; set;} //Is where the multiplier decreases until 0  If it is lower than this number
	float IncreasedChargeVoltage  {get; set;} // At what voltage the charge multiplier will increase

	float ChargeSteps  {get; set;}
	float MaxChargingMultiplier  {get; set;}
	float ChargingMultiplier  {get; set;}

	float ChargingWatts  {get; set;}
	float Resistance  {get; set;}
	float current {get; set;}

	bool CanCharge  {get; set;}
	bool Cansupport {get; set;}

	float ActualVoltage {get; set;} 

	bool ToggleCanCharge {get; set;}
	bool ToggleCansupport {get; set;}
	bool PassChangeToOff  {get; set;}
	float CircuitResistance {get; set;}
}
