using System.Collections.Generic;
using UnityEngine;

namespace Systems.Electricity.NodeModules
{
	[RequireComponent(typeof(ResistanceSourceModule))]
	public class BatterySupplyingModule : ModuleSupplyingDevice
	{
		[Header("Battery Settings")]
		public float MaximumCurrentSupport = 0; //The maximum number of amps can be pulled From the battery
		public float MinimumSupportVoltage = 0; //At which point the battery kicks in
		public float StandardSupplyingVoltage = 0;
		public float CapacityMax = 0;
		public float CurrentCapacity = 0;
		public float ExtraChargeCutOff = 0; //if  The voltages less than this it will decrease the charge steps until A it is not or B it reaches zero then stops charging
		public float IncreasedChargeVoltage = 0; // At what voltage the charge multiplier will increase
		public float StandardChargeNumber = 0; //Basically part of the multiplier of how much it should charge
		public float ChargeSteps = 0; //The steps it will go up by when adjusting the charge current
		public float MaxChargingMultiplier = 0;
		public float ChargingMultiplier = 0;
		public bool CanCharge = false;
		public bool Cansupport = false;
		public bool ToggleCanCharge = false;
		public bool ToggleCansupport = false;
		public bool SlowResponse = false; //If set to true then the battery won't respond instantly to loss of power waiting one tick to update

		public float PullLastDeductedTime = 0;
		public float ChargLastDeductedTime = 0;
		public float PullingWatts = 0;
		public float ChargingWatts = 0;
		public float CircuitResistance = 0;
		public float VoltageAtChargePort = 0;
		public float VoltageAtSupplyPort = 0;
		public bool isOnForInterface = false;

		public ResistanceSourceModule ResistanceSourceModule { get; private set; }
		public TransformerModule TTransformerModule { get; private set; }

		private float MonitoringResistance = 9999999999;
		private void Awake()
		{
			ResistanceSourceModule = GetComponent<ResistanceSourceModule>();
			TTransformerModule = GetComponent<TransformerModule>();
		}

		public override void BroadcastSetUpMessage(ElectricalNodeControl Node)
		{
			RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
			{
				ElectricalUpdateTypeCategory.PowerUpdateStructureChange,
				ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact,
				ElectricalUpdateTypeCategory.PowerUpdateCurrentChange,
				ElectricalUpdateTypeCategory.TurnOnSupply,
				ElectricalUpdateTypeCategory.TurnOffSupply,
				ElectricalUpdateTypeCategory.PowerNetworkUpdate,
				ElectricalUpdateTypeCategory.ModifyElectricityOutput,
				ElectricalUpdateTypeCategory.PotentialDestroyed, //Remember to keep the inherited updates
			};
			ModuleType = ElectricalModuleTypeCategory.BatterySupplyingDevice;
			ControllingNode = Node;
			Node.AddModule(this);
			if (StartOnStartUp)
			{
				TurnOnSupply();
			}
		}

		public override void TurnOnSupply()
		{
			isOnForInterface = true;
			PowerSupplyFunction.TurnOnSupply(this);
			PowerNetworkUpdate();
		}

		public override void TurnOffSupply()
		{
			isOnForInterface = false;
			PowerSupplyFunction.TurnOffSupply(this);
		}

		public override void PowerUpdateCurrentChange()
		{
			if (ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(ControllingNode.Node))
			{
				if (ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].ResistanceComingFrom.Count > 0)
				{
					if (!(SlowResponse && PullingWatts == 0))
					{
						ControllingNode.Node.InData.FlushSupplyAndUp(ControllingNode.Node); //Room for optimisation
						CircuitResistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].ResistanceComingFrom); // //!!
						VoltageAtChargePort = ElectricityFunctions.WorkOutVoltageFromConnector(ControllingNode.Node, ResistanceSourceModule.ReactionTo.ConnectingDevice);
						VoltageAtSupplyPort = ElectricityFunctions.WorkOutVoltageFromConnectors(ControllingNode.Node, ControllingNode.CanConnectTo);
						if (Cansupport) //Denotes capacity to Provide current
						{
							//NOTE This assumes that the voltage will be same on either side
							if (ToggleCansupport && IsAtVoltageThreshold()) // ToggleCansupport denotes Whether at the current time it is allowed to provide current
							{
								if (CurrentCapacity > 0)
								{
									var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
									current = needToPushVoltage / CircuitResistance;
									if (current > MaximumCurrentSupport)
									{
										current = MaximumCurrentSupport;
									}
									PullingWatts = current * StandardSupplyingVoltage; // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
								}
							}
							else if (PullingWatts > 0)
							{ //Cleaning up values if it can't supply
								PullingWatts = 0;
								current = 0;
								PullLastDeductedTime = 0;
							}
						}

						if (current != Previouscurrent)
						{
							if (current == 0)
							{
								ControllingNode.Node.InData.FlushSupplyAndUp(ControllingNode.Node);
							}
							ControllingNode.Node.InData.Data.SupplyingCurrent = current;
							Previouscurrent = current;
						}
					}
				}
				else {
					CircuitResistance = MonitoringResistance;
				}
			}
			PowerSupplyFunction.PowerUpdateCurrentChange(this);
		}
		public override void PowerNetworkUpdate()
		{
			VoltageAtChargePort = ElectricityFunctions.WorkOutVoltageFromConnector(ControllingNode.Node, ResistanceSourceModule.ReactionTo.ConnectingDevice);
			VoltageAtSupplyPort = ElectricityFunctions.WorkOutVoltageFromConnectors(ControllingNode.Node, ControllingNode.CanConnectTo);

			//Checks if the battery is actually on This is not needed in PowerUpdateCurrentChange Since having those updates Would mean it would be on
			if (isOnForInterface)
			{
				if (CanCharge)
				{
					if (ToggleCanCharge)
					{
						if (ResistanceSourceModule.Resistance != MonitoringResistance)
						{
							if (ChargLastDeductedTime == 0)
							{
								ChargLastDeductedTime = Time.time;
							}
							ChargingWatts = VoltageAtChargePort / ResistanceSourceModule.Resistance * VoltageAtChargePort;
							CurrentCapacity += ChargingWatts * (Time.time - ChargLastDeductedTime);
							ChargLastDeductedTime = Time.time;

							if (VoltageAtChargePort > IncreasedChargeVoltage && !(ChargingMultiplier >= MaxChargingMultiplier))
							{ //Increasing the current charge by
								ChargingMultiplier += ChargeSteps;
								ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber * ChargingMultiplier);
							}
							else if (VoltageAtChargePort < ExtraChargeCutOff)
							{
								if (!(0.1 >= ChargingMultiplier))
								{
									ChargingMultiplier -= ChargeSteps;
									ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber * ChargingMultiplier);
								}
								else
								{ //Turning off charge if it pulls too much
									ChargingWatts = 0;
									ChargingMultiplier = 0.1f;
									ResistanceSourceModule.Resistance = MonitoringResistance;
									ChargLastDeductedTime = 0;
								}
							}
							if (CurrentCapacity >= CapacityMax)
							{
								CurrentCapacity = CapacityMax;
								ChargingWatts = 0;
								ToggleCansupport = true;
								ChargingMultiplier = 0.1f;
								ResistanceSourceModule.Resistance = MonitoringResistance;
								ChargLastDeductedTime = 0;
							}
						}
						else if (VoltageAtChargePort > IncreasedChargeVoltage && !(CurrentCapacity >= CapacityMax))
						{
							if (ChargingMultiplier == 0)
							{
								ChargingMultiplier = ChargeSteps;
							}
							ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber * ChargingMultiplier);
							ChargLastDeductedTime = Time.time;
						}
					}
					else if (ResistanceSourceModule.Resistance != MonitoringResistance)
					{
						ChargingWatts = 0;
						ChargingMultiplier = 0.1f;
						ResistanceSourceModule.Resistance = MonitoringResistance;
						ChargLastDeductedTime = 0;
					}
				}
				if (Cansupport)
				{
					if (ToggleCansupport)
					{
						if (PullingWatts > 0)
						{
							if (PullLastDeductedTime == 0)
							{
								PullLastDeductedTime = Time.time;
							}
							CurrentCapacity -= PullingWatts * (Time.time - PullLastDeductedTime);
							PullLastDeductedTime = Time.time;
							if (CurrentCapacity < 0)
							{
								CurrentCapacity = 0;
								ToggleCansupport = false;
								PullingWatts = 0;
								current = 0;
								PullLastDeductedTime = 0;
							}
						}
						else if (VoltageAtSupplyPort < MinimumSupportVoltage && CurrentCapacity > 0)
						{
							var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
							current = needToPushVoltage / CircuitResistance;
							if (current > MaximumCurrentSupport)
							{
								current = MaximumCurrentSupport;
							}
							PullingWatts = current * StandardSupplyingVoltage;
						}
					}
					else if (PullingWatts > 0)
					{
						PullingWatts = 0;
						current = 0;
						PullLastDeductedTime = 0;
					}
				}
			}
			if (current != Previouscurrent | SupplyingVoltage != PreviousSupplyingVoltage | InternalResistance != PreviousInternalResistance)
			{
				ControllingNode.Node.InData.Data.SupplyingCurrent = current;
				Previouscurrent = current;

				ControllingNode.Node.InData.Data.SupplyingVoltage = SupplyingVoltage;
				PreviousSupplyingVoltage = SupplyingVoltage;

				ControllingNode.Node.InData.Data.InternalResistance = InternalResistance;
				PreviousInternalResistance = InternalResistance;

				ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(ControllingNode);
			}
		}

		public override VIRCurrent ModifyElectricityOutput(VIRCurrent current, ElectricalOIinheritance sourceInstance)
		{
			if (sourceInstance != ControllingNode.Node)
			{
				if (!ElectricalManager.Instance.electricalSync.NUCurrentChange.Contains(ControllingNode))
				{
					ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(ControllingNode);
				}
			}
			return current;
		}

		private bool IsAtVoltageThreshold()
		{
			if (TTransformerModule != null)
			{
				var highSide = false;
				var lowSide = false;
				foreach (var canConnectTo in ControllingNode.Node.InData.CanConnectTo)
				{
					if (TTransformerModule.HighsideConnections.Contains(canConnectTo))
					{
						highSide = true;
					}

					if (TTransformerModule.LowsideConnections.Contains(canConnectTo))
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
				if (highSide) //Outputs to highSide
				{
					return VoltageAtSupplyPort < MinimumSupportVoltage &&  VoltageAtChargePort*TTransformerModule.TurnRatio < MinimumSupportVoltage;
				}

				if (lowSide) //Outputs to lowSide
				{
					return VoltageAtSupplyPort < MinimumSupportVoltage &&  (VoltageAtChargePort*(1/TTransformerModule.TurnRatio))
						< MinimumSupportVoltage;
				}
				Logger.LogError("No side was found for Transformer battery combo falling back to default Calculation");
			}
			return VoltageAtSupplyPort < MinimumSupportVoltage &&  VoltageAtChargePort < MinimumSupportVoltage;
		}

	}
}
