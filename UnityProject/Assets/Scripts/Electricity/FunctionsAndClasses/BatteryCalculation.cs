using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class BatteryCalculation  {
	public static void TurnOffEverything(IBattery Battery){ //For turn off
		Battery.ChargingWatts = 0;
		Battery.ChargingMultiplier = 0.1f;
		Battery.Resistance = 0;
		Battery.ChargLastDeductedTime = 0;

		Battery.PullingWatts = 0;
		Battery.current  = 0;
		Battery.PullLastDeductedTime = 0;
		//Battery.PassChangeToOff = false;
	}

	public static void PowerUpdateCurrentChange (IBattery Battery){
		if (Battery.Cansupport) { //Denotes capacity to Provide current
			if (Battery.ToggleCansupport) { //Denotes Whether at the current time it is allowed to provide current
				if (Battery.ActualVoltage < Battery.MinimumSupportVoltage) {
					if (Battery.CurrentCapacity > 0) {
						float NeedToPushVoltage = Battery.StandardSupplyingVoltage - Battery.ActualVoltage;
						Battery.current = NeedToPushVoltage / Battery.CircuitResistance;
						if (Battery.current > Battery.MaximumCurrentSupport) { //Limits the maximum current
							Battery.current = Battery.MaximumCurrentSupport;
						}
						Battery.PullingWatts = Battery.current * Battery.StandardSupplyingVoltage; // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
					}
				} else {
					if (Battery.PullingWatts > 0) { //Cleaning up values if it can't supply
						Battery.PullingWatts = 0;
						Battery.current = 0; 
						Battery.PullLastDeductedTime = 0;
						Logger.Log ("Turning off support due to voltage levels being suitable", Category.Electrical);
					}
				}
			} else {
				if (Battery.PullingWatts > 0) { //Cleaning up values if it can't supply
					Battery.PullingWatts = 0;
					Battery.current = 0;
					Battery.PullLastDeductedTime = 0;
					Logger.Log ("Supply was turned off due to termination a support", Category.Electrical);
				}
			} 
		} 
	}
	public static void PowerNetworkUpdate(IBattery Battery){
		if (Battery.isOnForInterface) { //Checks if the battery is actually on This is not needed in PowerUpdateCurrentChange Since having those updates Would mean it would be on
			if (Battery.CanCharge) { //Ability to charge 
				if (Battery.ToggleCanCharge) { //Is available for charging
					if (Battery.Resistance > 0) {

						if (Battery.ChargLastDeductedTime == 0) { //so it's based on time
							Battery.ChargLastDeductedTime = Time.time;
						}
						//Logger.Log (Battery.ActualVoltage.ToString () + " < ActualVoltage " + Battery.Resistance.ToString () + " < Resistance ", Category.Electrical);
						Battery.ChargingWatts = (Battery.ActualVoltage / Battery.Resistance) * Battery.ActualVoltage;
						Battery.CurrentCapacity = Battery.CurrentCapacity + (Battery.ChargingWatts * (Time.time - Battery.ChargLastDeductedTime));
						Battery.ChargLastDeductedTime = Time.time;

						if (Battery.ActualVoltage > Battery.IncreasedChargeVoltage) { //Increasing the current charge by 
							if (!(Battery.ChargingMultiplier >= Battery.MaxChargingMultiplier)) {

								Battery.ChargingMultiplier = Battery.ChargingMultiplier + Battery.ChargeSteps;
								Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) )); 
							}

						} else if (Battery.ActualVoltage < Battery.ExtraChargeCutOff) {
							if (!(0.1 >= Battery.ChargingMultiplier)) {  
								Battery.ChargingMultiplier = Battery.ChargingMultiplier - Battery.ChargeSteps;
								Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) ));
							} else { //Turning off charge if it pulls too much
								Battery.ChargingWatts = 0;
								Battery.ChargingMultiplier = 0.1f;
								Battery.Resistance = 0;
								Battery.ChargLastDeductedTime = 0;
							}
						}


						if (Battery.CurrentCapacity >= Battery.CapacityMax) { //Making sure it doesn't go over Max capacity
							Battery.CurrentCapacity = Battery.CapacityMax;
							Battery.ChargingWatts = 0;
							Battery.ChargingMultiplier = 0.1f;
							Battery.Resistance = 0;
							Battery.ChargLastDeductedTime = 0;
							Logger.Log ("Turn off charging battery full", Category.Electrical);
						}
					} else if ((Battery.ActualVoltage > Battery.IncreasedChargeVoltage) && (!(Battery.CurrentCapacity >= Battery.CapacityMax))) {
						Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) ));
						Battery.ChargLastDeductedTime = Time.time;
						Logger.Log ("Charging turning back on from line voltage checks\n", Category.Electrical);
					}
	
				} else {
					if (Battery.Resistance > 0) {
						Battery.ChargingWatts = 0;
						Battery.ChargingMultiplier = 0.1f;
						Battery.Resistance = 0;
						Battery.ChargLastDeductedTime = 0;
						Logger.Log (" Turning off Charging because support was terminated for charging", Category.Electrical);
					}
				}
			}

			if (Battery.Cansupport) {
				if (Battery.ToggleCansupport) {
					if (Battery.PullingWatts > 0) {
						if (Battery.PullLastDeductedTime == 0) {
							Battery.PullLastDeductedTime = Time.time;
						}
						Battery.CurrentCapacity = Battery.CurrentCapacity - (Battery.PullingWatts * (Time.time - Battery.PullLastDeductedTime));
						Battery.PullLastDeductedTime = Time.time; 
						if (Battery.CurrentCapacity < 0) {  
							Battery.CurrentCapacity = 0;
							Battery.ToggleCansupport = false;
							Battery.PullingWatts = 0;
							Battery.current = 0;
							Battery.PullLastDeductedTime = 0;
							//Battery.PassChangeToOff = false;
							Logger.Log ("Turning off supply from loss of capacity", Category.Electrical);
						}
					} else {
						if (Battery.ActualVoltage < Battery.MinimumSupportVoltage) {
							if (Battery.CurrentCapacity > 0) {
								float NeedToPushVoltage = Battery.StandardSupplyingVoltage - Battery.ActualVoltage;
								Battery.current = NeedToPushVoltage / Battery.CircuitResistance;
								if (Battery.current > Battery.MaximumCurrentSupport) {
									Battery.current = Battery.MaximumCurrentSupport;
								}
								Battery.PullingWatts = Battery.current * Battery.StandardSupplyingVoltage; 
							}
						}
					}
				}  else {
					if (Battery.PullingWatts > 0) {
						//Battery.CurrentCapacity = 0;
						//Battery.ToggleCansupport = false;
						Battery.PullingWatts = 0;
						Battery.current = 0;
						Battery.PullLastDeductedTime = 0;
					}
				}
			}
		}
	}
}
//1 Nothing, 3 Start charging, 4 Stop charging