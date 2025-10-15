using System;
using System.Collections.Generic;
using Light2D;
using Logs;
using NaughtyAttributes;
using Objects.Machines;
using Systems.Construction.Parts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Electricity.NodeModules
{
	[RequireComponent(typeof(ResistanceSourceModule))]
	public class BatterySupplyingModule : ModuleSupplyingDevice, IRefreshParts
	{
		[Header("Battery Settings")]
		[FormerlySerializedAs("MaximumCurrentSupport")]
		public float InitialMaximumCurrentSupport; // The maximum number of amps can be pulled from the battery
		[NonSerialized] public float MaximumCurrentSupport; // The maximum number of amps that can be pulled from the battery

		[FormerlySerializedAs("MinimumSupportVoltage")]
		public float InitialMinimumSupportVoltage; // At which point the battery kicks in
		[NonSerialized] public float MinimumSupportVoltage; // At which point the battery kicks in

		[FormerlySerializedAs("StandardSupplyingVoltage")]
		public float InitialStandardSupplyingVoltage;
		[NonSerialized] public float StandardSupplyingVoltage;

		public float CapacityMax;


		[FormerlySerializedAs("CurrentCapacity")]
		public float InitialCurrentCapacity;
		public float GetSetCurrentCapacity
		{
			get
			{
				float Capacity = 0;
				foreach (var MachinePart in Machine.getObjectpartsInFrame)
				{
					if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
					{
						Capacity += MachinePart.itemObject.GetComponentCustom<Battery>().Watts;
					}
				}

				return Capacity;
			}
			set
			{
				if (value == 0)
				{
					foreach (var MachinePart in Machine.getObjectpartsInFrame)
					{
						if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
						{
							MachinePart.itemObject.GetComponentCustom<Battery>().Watts = 0;
						}
					}
				}
				else
				{
					int maxwatts = 0;
					int Number = 0;
					foreach (var MachinePart in Machine.getObjectpartsInFrame)
					{
						if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
						{
							maxwatts += MachinePart.itemObject.GetComponentCustom<Battery>().MaxWatts;
							Number++;
						}
					}

					var Percentage = value/  maxwatts ;

					if (Percentage > 1)
					{
						Percentage = 1;
					}

					foreach (var MachinePart in Machine.getObjectpartsInFrame)
					{
						if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
						{
							var bat = MachinePart.itemObject.GetComponentCustom<Battery>();
							bat.Watts =  Mathf.RoundToInt(bat.MaxWatts * Percentage);
						}
					}

				}
			}
		}

		[FormerlySerializedAs("ExtraChargeCutOff")]
		public float InitialExtraChargeCutOff; // If the voltage is less than this it will decrease the charge steps until either A) it is not or B) it reaches zero then stops charging
		[NonSerialized] public float ExtraChargeCutOff; // If the voltage is less than this it will decrease the charge steps until either A) it is not or B) it reaches zero then stops charging


		[FormerlySerializedAs("IncreasedChargeVoltage")]
		public float InitialIncreasedChargeVoltage; // At what voltage the charge multiplier will increase
		[NonSerialized] public float IncreasedChargeVoltage; // At what voltage the charge multiplier will increase



		[FormerlySerializedAs("StandardChargeNumber")]
		public float InitialStandardChargeNumber; // Basically part of the multiplier of how much it should charge
		[NonSerialized] public float StandardChargeNumber; // Basically part of the multiplier of how much it should charge



		[FormerlySerializedAs("MaxChargingDivider")]
		public int InitialMaxChargingDivider;
		[NonSerialized] public int MaxChargingDivider;


		[FormerlySerializedAs("ChargingDivider")]
		public int InitialChargingDivider;
		[NonSerialized] public int ChargingDivider;

		[FormerlySerializedAs("InputLevel")]
		public int InitialInputLevel = 100;
		[NonSerialized] public int InputLevel = 100;

		[FormerlySerializedAs("OutputLevel")]
		public int InitialOutputLevel = 100;
		[NonSerialized] public int OutputLevel = 100;


		[FormerlySerializedAs("CanCharge")]
		public bool InitialCanCharge;
		[NonSerialized] public bool CanCharge;

		[FormerlySerializedAs("Cansupport")]
		public bool InitialCanSupport;
		[NonSerialized] public bool Cansupport;


		[FormerlySerializedAs("ToggleCanCharge")]
		public bool InitialToggleCanCharge;
		[NonSerialized] public bool ToggleCanCharge;

		[FormerlySerializedAs("ToggleCansupport")]
		public bool InitialToggleCanSupport;
		[NonSerialized] public bool ToggleCanSupport;

		[FormerlySerializedAs("SlowResponse")]
		public bool InitialSlowResponse; // If set to true then the battery won't respond instantly to loss of power, waiting one tick to update
		[NonSerialized] public bool SlowResponse; // If set to true then the battery won't respond instantly to loss of power, waiting one tick to update

		[NonSerialized] public float PullLastDeductedTime;
		[NonSerialized] public float ChargLastDeductedTime;
		[NonSerialized] private bool chargeCapacityTime = true;
		[ReadOnly] public float PullingWatts;
		[ReadOnly] public float ChargingWatts;
		[NonSerialized] public float CircuitResistance;
		[NonSerialized] public float VoltageAtChargePort;
		[NonSerialized] public float VoltageAtSupplyPort;
		[NonSerialized] public bool isOnForInterface;

		public ResistanceSourceModule ResistanceSourceModule { get; private set; }
		public TransformerModule TTransformerModule { get; private set; }

		private float MonitoringResistance = 9999999999;

		private bool Init = false;

		private Machine Machine;



		public void RefreshParts(List<PartReference> partsInFrame, Machine Frame)
		{
			float Capacity = 0;
			foreach (var MachinePart in partsInFrame)
			{
				if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
				{
					Capacity += MachinePart.itemObject.GetComponentCustom<Battery>().MaxWatts;
				}
			}

			CapacityMax = Capacity;
		}

		public void ApplyInitialValues(bool Mapspawn)
		{
			if (Init) return;
			Init= true;

			MaximumCurrentSupport = InitialMaximumCurrentSupport;
			MinimumSupportVoltage = InitialMinimumSupportVoltage;
			StandardSupplyingVoltage = InitialStandardSupplyingVoltage;
			if (Mapspawn)
			{
				GetSetCurrentCapacity = InitialCurrentCapacity;
			}

			ExtraChargeCutOff = InitialExtraChargeCutOff;
			IncreasedChargeVoltage = InitialIncreasedChargeVoltage;
			StandardChargeNumber = InitialStandardChargeNumber;
			MaxChargingDivider = InitialMaxChargingDivider;
			ChargingDivider = InitialChargingDivider;
			InputLevel = InitialInputLevel;
			OutputLevel = InitialOutputLevel;
			CanCharge = InitialCanCharge;
			Cansupport = InitialCanSupport;
			ToggleCanCharge = InitialToggleCanCharge;
			ToggleCanSupport = InitialToggleCanSupport;
			SlowResponse = InitialSlowResponse;
		}

		public void CurrentCapacityDelta(int Delta)
		{

			if (Delta == 0) return;
			foreach (var MachinePart in Machine.getObjectpartsInFrame)
			{
				if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
				{
					var batty = MachinePart.itemObject.GetComponentCustom<Battery>();

					if (Delta > 0)
					{
						var SpareCapacity = batty.MaxWatts - batty.Watts;
 						if (Delta > SpareCapacity)
					    {
						    batty.Watts = batty.MaxWatts;
						    Delta -= SpareCapacity;
					    }
					    else
					    {
						    batty.Watts += Delta;
						    break;
					    }
					}
					else
					{
						var SpareCapacity = batty.Watts;
						if (Mathf.Abs(Delta) > SpareCapacity)
						{
							batty.Watts = 0;
							Delta += SpareCapacity;
						}
						else
						{
							batty.Watts += Delta;
							break;
						}
					}
				}
			}
		}

		private void Awake()
		{
			Machine = GetComponent<Machine>();
			ResistanceSourceModule = GetComponent<ResistanceSourceModule>();
			TTransformerModule = GetComponent<TransformerModule>();
		}

		private void Start()
		{
			ApplyInitialValues(Machine.MapSpawned);

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
				if (ControllingNode.Node.InData.Data.SupplyDependent[ControllingNode.Node].ResistanceComingFrom.Count >
				    0)
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
							if (ToggleCanSupport &&
							    IsAtVoltageThreshold()) // ToggleCansupport denotes Whether at the current time it is allowed to provide current
							{
								if (GetSetCurrentCapacity > 0)
								{
									var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
									current = needToPushVoltage / CircuitResistance;
									if (current > MaximumCurrentSupport)
									{
										current = MaximumCurrentSupport;
									}

									PullingWatts =
										((current * StandardSupplyingVoltage) *
										 (OutputLevel /
										  100)); // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
								}
							}
							else if (PullingWatts > 0)
							{
								//Cleaning up values if it can't supply
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
				else
				{
					CircuitResistance = MonitoringResistance;
				}
			}

			PowerSupplyFunction.PowerUpdateCurrentChange(this);
		}

		public override void PowerNetworkUpdate()
		{
			try
			{
				VoltageAtChargePort = ElectricityFunctions.WorkOutVoltageFromConnector(ControllingNode.Node,
					ResistanceSourceModule.ReactionTo.ConnectingDevice);
				VoltageAtSupplyPort =
					ElectricityFunctions.WorkOutVoltageFromConnectors(ControllingNode.Node, ControllingNode.CanConnectTo);

			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}

			//Checks if the battery is actually on This is not needed in PowerUpdateCurrentChange Since having those updates Would mean it would be on
			if (isOnForInterface)
			{
				if (CanCharge)
				{
					if (ToggleCanCharge)
					{
						if (ResistanceSourceModule.Resistance != MonitoringResistance)
						{
							ChargingWatts = VoltageAtChargePort / ResistanceSourceModule.Resistance *
							                VoltageAtChargePort;
							if (chargeCapacityTime)
							{
								CurrentCapacityDelta(Mathf.RoundToInt((ChargingWatts * (Time.time - ChargLastDeductedTime) * (InputLevel / 100f))));
							}

							ChargLastDeductedTime = Time.time;

							if (VoltageAtChargePort > IncreasedChargeVoltage && ChargingDivider < MaxChargingDivider)
							{
								//Increasing the current charge by
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
								{
									//Turning off charge if it pulls too much
									ChargingWatts = 0;
									ChargingDivider = 10;
									ResistanceSourceModule.Resistance = MonitoringResistance;
									chargeCapacityTime = false;
								}
							}

							if (GetSetCurrentCapacity >= CapacityMax)
							{
								GetSetCurrentCapacity = CapacityMax;
								ChargingWatts = 0;
								ToggleCanSupport = true;
								ChargingDivider = 10;
								ResistanceSourceModule.Resistance = MonitoringResistance;
								chargeCapacityTime = false;
							}
						}
						else if (VoltageAtChargePort > IncreasedChargeVoltage && GetSetCurrentCapacity < CapacityMax)
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
					if (ToggleCanSupport)
					{
						if (PullingWatts > 0)
						{
							if (PullLastDeductedTime <= 0)
							{
								PullLastDeductedTime = Time.time;
							}

							CurrentCapacityDelta(Mathf.RoundToInt(- (PullingWatts * (OutputLevel / 100f)) * (Time.time - PullLastDeductedTime)));

							PullLastDeductedTime = Time.time;
							if (GetSetCurrentCapacity <= 0)
							{
								GetSetCurrentCapacity = 0;
								ToggleCanSupport = false;
								PullingWatts = 0;
								current = 0;
								PullLastDeductedTime = -1;
							}
						}


						if (VoltageAtSupplyPort < MinimumSupportVoltage && GetSetCurrentCapacity > 0)
						{
							var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
							current = needToPushVoltage / CircuitResistance;
							if (current > MaximumCurrentSupport)
							{
								current = MaximumCurrentSupport;
							}

							PullingWatts = ((current * StandardSupplyingVoltage) * (OutputLevel / 100f));
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

			if (current != Previouscurrent
			    || SupplyingVoltage != PreviousSupplyingVoltage
			    || InternalResistance != PreviousInternalResistance)
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
					Loggy.Error("Transformer 'high side' connected to its 'low side', and will not work.",
						Category.Electrical);
				}

				if (highSide) //Outputs to highSide
				{
					return VoltageAtSupplyPort < MinimumSupportVoltage &&
					       VoltageAtChargePort * TTransformerModule.TurnRatio < MinimumSupportVoltage;
				}

				if (lowSide) //Outputs to lowSide
				{
					return VoltageAtSupplyPort < MinimumSupportVoltage &&
					       (VoltageAtChargePort * (1 / TTransformerModule.TurnRatio))
					       < MinimumSupportVoltage;
				}

				Loggy.Error("No side was found for Transformer battery combo, falling back to default",
					Category.Electrical);
			}

			return VoltageAtSupplyPort < MinimumSupportVoltage && VoltageAtChargePort < MinimumSupportVoltage;
		}
	}
}