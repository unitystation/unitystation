using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class BatteryCalculation  {
	public static void TurnOffEverything(IBattery Battery){
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
		if (Battery.Cansupport) {
			if (Battery.ToggleCansupport) {
				if (Battery.ActualVoltage < Battery.MinimumSupportVoltage) {
					if (Battery.CurrentCapacity > 0) {
						float NeedToPushVoltage = Battery.StandardSupplyingVoltage - Battery.ActualVoltage;
						Battery.current = NeedToPushVoltage / Battery.CircuitResistance;
						if (Battery.current > Battery.MaximumCurrentSupport) {
							Battery.current = Battery.MaximumCurrentSupport;
						}
						Battery.PullingWatts = Battery.current * Battery.StandardSupplyingVoltage; // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
					}
				} else {
					if (Battery.PullingWatts > 0) {
						Battery.PullingWatts = 0;
						Battery.current = 0; 
						Battery.PullLastDeductedTime = 0;
						Logger.Log ("Turning off");
					}
				}
			} else {
				if (Battery.PullingWatts > 0) {
					Battery.PullingWatts = 0;
					Battery.current = 0;
					Battery.PullLastDeductedTime = 0;
					Logger.Log ("Turning off");
				}
			} 
		} 
	}
	public static void PowerNetworkUpdate(IBattery Battery){
		if (Battery.isOnForInterface) {
			if (Battery.CanCharge) {
				if (Battery.ToggleCanCharge) {
					if (Battery.Resistance > 0) {

						if (Battery.ChargLastDeductedTime == 0) {
							Battery.ChargLastDeductedTime = Time.time;
						}
						//Logger.Log (Battery.ActualVoltage.ToString () + " < ActualVoltage " + Battery.Resistance.ToString () + " < Resistance ", Category.Electrical);
						Battery.ChargingWatts = (Battery.ActualVoltage / Battery.Resistance) * Battery.ActualVoltage;
						Battery.CurrentCapacity = Battery.CurrentCapacity + (Battery.ChargingWatts * (Time.time - Battery.ChargLastDeductedTime));//Using wrong resistance
						Battery.ChargLastDeductedTime = Time.time;

						if (Battery.ActualVoltage > Battery.IncreasedChargeVoltage) {
							if (!(Battery.ChargingMultiplier >= Battery.MaxChargingMultiplier)) {

								Battery.ChargingMultiplier = Battery.ChargingMultiplier + Battery.ChargeSteps;
								Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) / 1000)); 
							}

						} else if (Battery.ActualVoltage < Battery.ExtraChargeCutOff) {
							if (!(0.1 >= Battery.ChargingMultiplier)) {  
								Battery.ChargingMultiplier = Battery.ChargingMultiplier - Battery.ChargeSteps;
								Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) / 1000));
							} else { 
								Battery.ChargingWatts = 0;
								Battery.ChargingMultiplier = 0.1f;
								Battery.Resistance = 0;
								Battery.ChargLastDeductedTime = 0;
							}
						}


						if (Battery.CurrentCapacity >= Battery.CapacityMax) {
							Battery.CurrentCapacity = Battery.CapacityMax;
							Battery.ChargingWatts = 0;
							Battery.ChargingMultiplier = 0.1f;
							Battery.Resistance = 0;
							Battery.ChargLastDeductedTime = 0;
							Logger.Log (" turn off!!");
						}
					} else if ((Battery.ActualVoltage > Battery.IncreasedChargeVoltage) && (!(Battery.CurrentCapacity >= Battery.CapacityMax))) {
						Battery.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier) / 1000));
						Battery.ChargLastDeductedTime = Time.time;
						Logger.Log ("Turn back on");
					}
	
				} else {
					if (Battery.Resistance > 0) {
						Battery.ChargingWatts = 0;
						Battery.ChargingMultiplier = 0.1f;
						Battery.Resistance = 0;
						Battery.ChargLastDeductedTime = 0;
						Logger.Log (" turn off!!");
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
							Logger.Log (" turn off!!");
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