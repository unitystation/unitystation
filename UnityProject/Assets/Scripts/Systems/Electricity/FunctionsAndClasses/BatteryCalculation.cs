using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class BatteryCalculation
{
	public static float MonitoringResistance = 9999999999;

	public static void TurnOffEverything(BatterySupplyingModule Battery)
	{
		Battery.ChargingWatts = 0;
		Battery.ChargingMultiplier = 0.1f;
		Battery.ResistanceSourceModule.Resistance = MonitoringResistance;
		Battery.ChargLastDeductedTime = 0;

		Battery.PullingWatts = 0;
		Battery.current = 0;
		Battery.PullLastDeductedTime = 0;
	}

	public static void PowerUpdateCurrentChange(BatterySupplyingModule Battery)
	{
		if (Battery.Cansupport) //Denotes capacity to Provide current
		{
			if (Battery.ToggleCansupport && Battery.VoltageAtSupplyPort < Battery.MinimumSupportVoltage) // Battery.ToggleCansupport denotes Whether at the current time it is allowed to provide current
			{
				if (Battery.CurrentCapacity > 0)
				{
					float NeedToPushVoltage = Battery.StandardSupplyingVoltage - Battery.VoltageAtSupplyPort;
					Battery.current = NeedToPushVoltage / Battery.CircuitResistance;
					if (Battery.current > Battery.MaximumCurrentSupport)
					{
						Battery.current = Battery.MaximumCurrentSupport;
					}
					Battery.PullingWatts = Battery.current * Battery.StandardSupplyingVoltage; // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
				}
			}
			else if (Battery.PullingWatts > 0)
			{ //Cleaning up values if it can't supply
				Battery.PullingWatts = 0;
				Battery.current = 0;
				Battery.PullLastDeductedTime = 0;
			}
		}
	}

	public static void PowerNetworkUpdate(BatterySupplyingModule Battery)
	{
		if (Battery.isOnForInterface)
		{ //Checks if the battery is actually on This is not needed in PowerUpdateCurrentChange Since having those updates Would mean it would be on
			if (Battery.CanCharge)
			{
				if (Battery.ToggleCanCharge)
				{
					if (!(Battery.ResistanceSourceModule.Resistance == MonitoringResistance))
					{
						if (Battery.ChargLastDeductedTime == 0)
						{
							Battery.ChargLastDeductedTime = Time.time;
						}
						//Logger.Log (Battery.VoltageAtChargePort.ToString () + " < ActualVoltage " + Battery.ResistanceSourceModule.Resistance.ToString () + " < Resistance ", Category.Electrical);
						Battery.ChargingWatts = (Battery.VoltageAtChargePort / Battery.ResistanceSourceModule.Resistance) * Battery.VoltageAtChargePort;
						Battery.CurrentCapacity = Battery.CurrentCapacity + (Battery.ChargingWatts * (Time.time - Battery.ChargLastDeductedTime));
						Battery.ChargLastDeductedTime = Time.time;

						if (Battery.VoltageAtChargePort > Battery.IncreasedChargeVoltage && !(Battery.ChargingMultiplier >= Battery.MaxChargingMultiplier))
						{ //Increasing the current charge by 
							Battery.ChargingMultiplier = Battery.ChargingMultiplier + Battery.ChargeSteps;
							Battery.ResistanceSourceModule.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier)));
						}
						else if (Battery.VoltageAtChargePort < Battery.ExtraChargeCutOff)
						{
							if (!(0.1 >= Battery.ChargingMultiplier))
							{
								Battery.ChargingMultiplier = Battery.ChargingMultiplier - Battery.ChargeSteps;
								Battery.ResistanceSourceModule.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier)));
							}
							else
							{ //Turning off charge if it pulls too much
								Battery.ChargingWatts = 0;
								Battery.ChargingMultiplier = 0.1f;
								Battery.ResistanceSourceModule.Resistance = MonitoringResistance;
								Battery.ChargLastDeductedTime = 0;
							}
						}

						if (Battery.CurrentCapacity >= Battery.CapacityMax)
						{
							Battery.CurrentCapacity = Battery.CapacityMax;
							Battery.ChargingWatts = 0;
							Battery.ToggleCansupport = true;
							Battery.ChargingMultiplier = 0.1f;
							Battery.ResistanceSourceModule.Resistance = MonitoringResistance;
							Battery.ChargLastDeductedTime = 0;
							//Logger.Log ("Turn off charging battery full", Category.Electrical);
						}
					}
					else if ((Battery.VoltageAtChargePort > Battery.IncreasedChargeVoltage) && (!(Battery.CurrentCapacity >= Battery.CapacityMax)))
					{
						if (Battery.ChargingMultiplier == 0)
						{
							Battery.ChargingMultiplier = Battery.ChargeSteps;
						}
						Battery.ResistanceSourceModule.Resistance = (1000 / ((Battery.StandardChargeNumber * Battery.ChargingMultiplier)));
						Battery.ChargLastDeductedTime = Time.time;
						//Logger.Log ("Charging turning back on from line voltage checks\n" + Battery.ResistanceSourceModule.Resistance, Category.Electrical);
					}
				}
				else if (!(Battery.ResistanceSourceModule.Resistance == MonitoringResistance))
				{
					Battery.ChargingWatts = 0;
					Battery.ChargingMultiplier = 0.1f;
					Battery.ResistanceSourceModule.Resistance = MonitoringResistance;
					Battery.ChargLastDeductedTime = 0;
					//Logger.Log (" Turning off Charging because support was terminated for charging", Category.Electrical);
				}
			}

			if (Battery.Cansupport)
			{
				if (Battery.ToggleCansupport)
				{
					if (Battery.PullingWatts > 0)
					{
						if (Battery.PullLastDeductedTime == 0)
						{
							Battery.PullLastDeductedTime = Time.time;
						}
						Battery.CurrentCapacity = Battery.CurrentCapacity - (Battery.PullingWatts * (Time.time - Battery.PullLastDeductedTime));
						Battery.PullLastDeductedTime = Time.time;
						if (Battery.CurrentCapacity < 0)
						{
							Battery.CurrentCapacity = 0;
							Battery.ToggleCansupport = false;
							Battery.PullingWatts = 0;
							Battery.current = 0;
							Battery.PullLastDeductedTime = 0;
							//Logger.Log ("Turning off supply from loss of capacity", Category.Electrical);
						}
					}
					else if (Battery.VoltageAtSupplyPort < Battery.MinimumSupportVoltage && Battery.CurrentCapacity > 0)
					{
						float NeedToPushVoltage = Battery.StandardSupplyingVoltage - Battery.VoltageAtSupplyPort;
						Battery.current = NeedToPushVoltage / Battery.CircuitResistance;
						if (Battery.current > Battery.MaximumCurrentSupport)
						{
							Battery.current = Battery.MaximumCurrentSupport;
						}
						Battery.PullingWatts = Battery.current * Battery.StandardSupplyingVoltage;
					}
				}
				else if (Battery.PullingWatts > 0)
				{
					Battery.PullingWatts = 0;
					Battery.current = 0;
					Battery.PullLastDeductedTime = 0;
				}
			}
		}
	}
}
//1 Nothing, 3 Start charging, 4 Stop charging
//  /\ idk what this is?