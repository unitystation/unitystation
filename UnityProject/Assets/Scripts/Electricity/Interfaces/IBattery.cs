using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBattery  {
	bool isOnForInterface {get; set;}


	float MinimumSupportVoltage {get; set;}
	float StandardSupplyingVoltage {get; set;}
	float PullingWatts {get; set;}
	float CapacityMax {get; set;}
	float CurrentCapacity {get; set;}

	float PullLastDeductedTime {get; set;}
	float ChargLastDeductedTime {get; set;}
	float MaximumCurrentSupport {get; set;}

	float StandardChargeNumber {get; set;} //set to 0 if Can never charge

	float ExtraChargeCutOff  {get; set;}
	float IncreasedChargeVoltage  {get; set;}

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
