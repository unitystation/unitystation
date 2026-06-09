using System;
using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Items.Traits;
using US13.Systems.Construction;
using US13.Systems.Construction.Parts;
using US13.Systems.Electricity.Electrical_processes;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Systems.Electricity.Inheritance;
using Util;

namespace US13.Systems.Electricity.NodeModules
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

		[NonSerialized] public float CapacityMax;


		[FormerlySerializedAs("CurrentCapacity")]
		public float InitialCurrentCapacity;
		public float GetSetCurrentCapacity
		{
			get => Machine.CurrentBatteryCapacity;
			set => Machine.CurrentBatteryCapacity = value;
		}
		public Action<float, float> OnCapacityChangedEvent;

		[FormerlySerializedAs("ExtraChargeCutOff")]
		public float InitialExtraChargeCutOff; // If the voltage is less than this it will decrease the charge steps until either A) it is not or B) it reaches zero then stops charging
		[NonSerialized] public float ExtraChargeCutOff; // If the voltage is less than this it will decrease the charge steps until either A) it is not or B) it reaches zero then stops charging


		[FormerlySerializedAs("IncreasedChargeVoltage")]
		public float InitialIncreasedChargeVoltage; // At what voltage the charge multiplier will increase
		[NonSerialized] public float IncreasedChargeVoltage; // At what voltage the charge multiplier will increase




		public int InitialStandardChargeIncrementWatts; // Basically part of the multiplier of how much it should charge
		[NonSerialized] public int StandardChargeIncrementWatts; // Basically part of the multiplier of how much it should charge



		[FormerlySerializedAs("MaxTargetWattsCharge")]
		public int InitialMaxTargetWattsCharge;
		[NonSerialized] public int MaxTargetWattsCharge;

		[NonSerialized] public int TargetWattsCharge;

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


		public float InitialSlowResponseTime; //How long it will take the battery to kick in
		[NonSerialized] public float SlowResponseTime;


		public float InitialDropOffAtPercentage = 0.25f;
		[NonSerialized] public float DropOffAtPercentage;

		public float InitialMinimumDropOffCurrentPercentage = 0.10f;
		[NonSerialized] public float MinimumDropOffCurrentPercentage;

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

		[PlayModeOnly] public float TimeAtLowVoltage;

		private float LastCachedTime;


		[NaughtyAttributes.Button]
		public void Discharge()
		{
			GetSetCurrentCapacity = GetSetCurrentCapacity * 0.5f;
		}

		public void RefreshParts(List<PartReference> partsInFrame, Machine Frame)
		{
			float Capacity = 0;
			foreach (var MachinePart in partsInFrame)
			{
				if (MachinePart.itemTrait == CommonTraits.Instance.PowerCell)
				{
					Capacity += MachinePart.itemObject.GetCachedComponent<Battery>().MaxWatts;
				}
			}

			CapacityMax = Capacity;
		}

		public void ApplyInitialValues(bool Mapspawn)
		{
			if (Init) return;
			Init= true;

			DropOffAtPercentage = InitialDropOffAtPercentage;
			MinimumDropOffCurrentPercentage = InitialMinimumDropOffCurrentPercentage;
			MaximumCurrentSupport = InitialMaximumCurrentSupport;
			MinimumSupportVoltage = InitialMinimumSupportVoltage;
			StandardSupplyingVoltage = InitialStandardSupplyingVoltage;
			if (Mapspawn)
			{
				GetSetCurrentCapacity = InitialCurrentCapacity;
			}

			ExtraChargeCutOff = InitialExtraChargeCutOff;
			IncreasedChargeVoltage = InitialIncreasedChargeVoltage;
			StandardChargeIncrementWatts = InitialStandardChargeIncrementWatts;
			MaxTargetWattsCharge = InitialMaxTargetWattsCharge;
			InputLevel = InitialInputLevel;
			OutputLevel = InitialOutputLevel;
			CanCharge = InitialCanCharge;
			Cansupport = InitialCanSupport;
			ToggleCanCharge = InitialToggleCanCharge;
			ToggleCanSupport = InitialToggleCanSupport;
			SlowResponse = InitialSlowResponse;
			SlowResponseTime = InitialSlowResponseTime;
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
			ChargingWatts = 0;
			TargetWattsCharge = 10;
			ResistanceSourceModule.Resistance = MonitoringResistance;
			chargeCapacityTime = false;
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
							if (ToggleCanSupport && IsAtVoltageThreshold() && TimeAtLowVoltage > SlowResponseTime ) // ToggleCansupport denotes Whether at the current time it is allowed to provide current
							{

								var capp = GetSetCurrentCapacity;
								if (capp > 0)
								{
									var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
									current = needToPushVoltage / CircuitResistance;
									if (current > MaximumCurrentSupport)
									{
										current = MaximumCurrentSupport;
									}

									//current

									var capPercent = capp / CapacityMax;
									if (DropOffAtPercentage > capPercent)
									{
										// Normalised factor
										float t = (capPercent / DropOffAtPercentage);

										// Apply fraction of initial value
										current =  Mathf.Min( current * t,current );

										if (MinimumDropOffCurrentPercentage * MaximumCurrentSupport > current)
										{
											current = MinimumDropOffCurrentPercentage * MaximumCurrentSupport;
										}
									}


									PullingWatts =
										((current * StandardSupplyingVoltage) *
										 (OutputLevel /
										  100f)); // Should be the same as NeedToPushVoltage + powerSupply.ActualVoltage
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
			var Different = Time.time - LastCachedTime;
			LastCachedTime = Time.time;
			if (Different > 10)
			{
				Different = 1;
			}

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
								Machine.BatteryChangeChargedByDelta(Mathf.RoundToInt((ChargingWatts * (Time.time - ChargLastDeductedTime) * (InputLevel / 100f))));
							}

							ChargLastDeductedTime = Time.time;

							if (VoltageAtChargePort > IncreasedChargeVoltage && TargetWattsCharge < MaxTargetWattsCharge)
							{


								//Increasing the current charge by
								TargetWattsCharge += StandardChargeIncrementWatts;
								ResistanceSourceModule.Resistance = ExtraChargeCutOff / (TargetWattsCharge / ExtraChargeCutOff);


							}
							else if (VoltageAtChargePort < ExtraChargeCutOff)
							{
								if (StandardChargeIncrementWatts < TargetWattsCharge)
								{
									TargetWattsCharge -= StandardChargeIncrementWatts;
									ResistanceSourceModule.Resistance = ExtraChargeCutOff / (TargetWattsCharge / ExtraChargeCutOff);
								}
								else
								{
									//Turning off charge if it pulls too much
									ChargingWatts = 0;
									TargetWattsCharge = StandardChargeIncrementWatts;
									ResistanceSourceModule.Resistance = MonitoringResistance;
									chargeCapacityTime = false;
								}

							}

							if (GetSetCurrentCapacity >= CapacityMax)
							{
								GetSetCurrentCapacity = CapacityMax;
								ChargingWatts = 0;
								ToggleCanSupport = true;
								TargetWattsCharge = 10;
								ResistanceSourceModule.Resistance = MonitoringResistance;
								chargeCapacityTime = false;
							}
						}
						else if (VoltageAtChargePort > IncreasedChargeVoltage && GetSetCurrentCapacity < CapacityMax)
						{
							if (TargetWattsCharge == 0)
							{
								TargetWattsCharge = 10;
							}

							ResistanceSourceModule.Resistance =  ExtraChargeCutOff / (TargetWattsCharge / ExtraChargeCutOff);
							chargeCapacityTime = true;
							ChargLastDeductedTime = Time.time;
						}
					}
					else if (ResistanceSourceModule.Resistance != MonitoringResistance)
					{
						ChargingWatts = 0;
						TargetWattsCharge = 10;
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
							bool? notDepleted = null;
							notDepleted =  Machine.BatteryChangeChargedByDelta(Mathf.RoundToInt(- (PullingWatts * (OutputLevel / 100f)) * (Time.time - PullLastDeductedTime)));

							PullLastDeductedTime = Time.time;
							if (notDepleted == false)
							{
								GetSetCurrentCapacity = 0;
								ToggleCanSupport = false;
								PullingWatts = 0;
								current = 0;
								PullLastDeductedTime = -1;
							}
						}


						var capp = GetSetCurrentCapacity;
						if (VoltageAtSupplyPort < MinimumSupportVoltage && capp > 0 )
						{
							if ((TimeAtLowVoltage > SlowResponseTime) == false)
							{
								TimeAtLowVoltage += Different;
							}
							else
							{
								var needToPushVoltage = StandardSupplyingVoltage - VoltageAtSupplyPort;
								current = needToPushVoltage / CircuitResistance;
								if (current > MaximumCurrentSupport)
								{
									current = MaximumCurrentSupport;
								}

								var capPercent = capp / CapacityMax;
								if (DropOffAtPercentage > capPercent)
								{
									// Normalised factor
									float t = (capPercent / DropOffAtPercentage);

									// Apply fraction of initial value
									current =  Mathf.Min( current * t,current );

									if (MinimumDropOffCurrentPercentage * MaximumCurrentSupport > current)
									{
										current = MinimumDropOffCurrentPercentage * MaximumCurrentSupport;
									}
								}

								PullingWatts = ((current * StandardSupplyingVoltage) * (OutputLevel / 100f));
							}
						}
						else
						{
							TimeAtLowVoltage = 0;
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
			var percent = GetSetCurrentCapacity / CapacityMax;
			OnCapacityChangedEvent?.Invoke(percent, percent);
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