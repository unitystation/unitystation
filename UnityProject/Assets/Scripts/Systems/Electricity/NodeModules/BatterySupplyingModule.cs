using System.Collections.Generic;
using UnityEngine;

namespace Systems.Electricity.NodeModules
{
	[RequireComponent(typeof(ResistanceSourceModule))]
	public class BatterySupplyingModule : ModuleSupplyingDevice
	{
		[Header("Battery Settings")]
		public float MaximumCurrentSupport; //The maximum number of amps can be pulled From the battery
		public float MinimumSupportVoltage; //At which point the battery kicks in
		public float StandardSupplyingVoltage;
		public float CapacityMax;
		public float CurrentCapacity;
		public float ExtraChargeCutOff; //if  The voltages less than this it will decrease the charge steps until A it is not or B it reaches zero then stops charging
		public float IncreasedChargeVoltage; // At what voltage the charge multiplier will increase
		public float StandardChargeNumber; //Basically part of the multiplier of how much it should charge
		public int MaxChargingDivider;
		public int ChargingDivider;
		public bool CanCharge;
		public bool Cansupport;
		public bool ToggleCanCharge;
		public bool ToggleCansupport;
		public bool SlowResponse; //If set to true then the battery won't respond instantly to loss of power waiting one tick to update

		public float PullLastDeductedTime ;
		public float ChargLastDeductedTime;
		private bool chargeCapacityTime = true;
		public float PullingWatts;
		public float ChargingWatts;
		public float CircuitResistance;
		public float VoltageAtChargePort;
		public float VoltageAtSupplyPort;
		public bool isOnForInterface;

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
								PullLastDeductedTime = -1;
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
							ChargingWatts = VoltageAtChargePort / ResistanceSourceModule.Resistance * VoltageAtChargePort;
							if (chargeCapacityTime)
							{
								CurrentCapacity += ChargingWatts * (Time.time - ChargLastDeductedTime);
							}
							ChargLastDeductedTime = Time.time;

							if (VoltageAtChargePort > IncreasedChargeVoltage && ChargingDivider < MaxChargingDivider)
							{ //Increasing the current charge by
								ChargingDivider += 10;
								ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber / ChargingDivider);
							}
							else if (VoltageAtChargePort < ExtraChargeCutOff)
							{
								if (10 < ChargingDivider)
								{
									ChargingDivider -= 10;
									ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber / ChargingDivider);
								}
								else
								{ //Turning off charge if it pulls too much
									ChargingWatts = 0;
									ChargingDivider = 10;
									ResistanceSourceModule.Resistance = MonitoringResistance;
									chargeCapacityTime = false;
								}
							}
							if (CurrentCapacity >= CapacityMax)
							{
								CurrentCapacity = CapacityMax;
								ChargingWatts = 0;
								ToggleCansupport = true;
								ChargingDivider = 10;
								ResistanceSourceModule.Resistance = MonitoringResistance;
								chargeCapacityTime = false;
							}
						}
						else if (VoltageAtChargePort > IncreasedChargeVoltage && CurrentCapacity < CapacityMax)
						{
							if (ChargingDivider == 0)
							{
								ChargingDivider = 10;
							}
							ResistanceSourceModule.Resistance = 1000 / (StandardChargeNumber / ChargingDivider);
							chargeCapacityTime = true;
							ChargLastDeductedTime = Time.time;
						}
					}
					else if (ResistanceSourceModule.Resistance != MonitoringResistance)
					{
						ChargingWatts = 0;
						ChargingDivider = 10;
						ResistanceSourceModule.Resistance = MonitoringResistance;
						chargeCapacityTime = false;
					}
				}
				if (Cansupport)
				{
					if (ToggleCansupport)
					{
						if (PullingWatts > 0)
						{
							if (PullLastDeductedTime <= 0)
							{
								PullLastDeductedTime = Time.time;
							}
							CurrentCapacity -= PullingWatts * (Time.time - PullLastDeductedTime);
							PullLastDeductedTime = Time.time;
							if (CurrentCapacity <= 0)
							{
								CurrentCapacity = 0;
								ToggleCansupport = false;
								PullingWatts = 0;
								current = 0;
								PullLastDeductedTime = -1;
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
						PullLastDeductedTime = -1;
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
