using Systems.Electricity.NodeModules;
using UnityEngine;

namespace Systems.Electricity.FunctionsAndClasses
{
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

		public static bool IsAtVoltageThreshold(BatterySupplyingModule Battery)
		{
			if (Battery.TTransformerModule != null)
			{
				bool highSide = false;
				bool lowSide = false;
				foreach (var CanConnectTo in Battery.ControllingNode.Node.InData.CanConnectTo)
				{
					if (Battery.TTransformerModule.HighsideConnections.Contains(CanConnectTo))
					{
						highSide = true;
					}

					if (Battery.TTransformerModule.LowsideConnections.Contains(CanConnectTo))
					{
						lowSide = true;
					}
				}

				if (highSide && lowSide)
				{
					Logger.LogError("You have a connection from a battery Transformer combo that is the high side of the transformer and the low side of the transformer, " +
					                 "It's presumed that the charging port would be on the opposite side of the Transformer" +
					                "I could fix this but currently it's not used anywhere so just telling you it won't work properly");
				}

				if (highSide) //Outputs to highside
				{
					return Battery.VoltageAtSupplyPort < Battery.MinimumSupportVoltage &&  Battery.VoltageAtChargePort*Battery.TTransformerModule.TurnRatio < Battery.MinimumSupportVoltage;
				}

				if (lowSide) //Outputs to lowSide
				{
					return Battery.VoltageAtSupplyPort < Battery.MinimumSupportVoltage &&  (Battery.VoltageAtChargePort*(1/Battery.TTransformerModule.TurnRatio))
						< Battery.MinimumSupportVoltage;
				}

				Logger.LogError("No side was found for Transformer battery combo falling back to default Calculation");
			}

			return Battery.VoltageAtSupplyPort < Battery.MinimumSupportVoltage &&  Battery.VoltageAtChargePort < Battery.MinimumSupportVoltage;

		}

		public static void PowerUpdateCurrentChange(BatterySupplyingModule Battery)
		{
			if (Battery.Cansupport) //Denotes capacity to Provide current
			{
				//NOTE This assumes that the voltage will be same on either side
				if (Battery.ToggleCansupport && (IsAtVoltageThreshold(Battery))) // Battery.ToggleCansupport denotes Whether at the current time it is allowed to provide current
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
}
//1 Nothing, 3 Start charging, 4 Stop charging
//  /\ idk what this is?