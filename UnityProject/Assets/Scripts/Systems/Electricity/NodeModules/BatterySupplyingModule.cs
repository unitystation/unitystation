using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public ResistanceSourceModule ResistanceSourceModule;

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
					BatteryCalculation.PowerUpdateCurrentChange(this);

					if (current != Previouscurrent)
					{
						if (Previouscurrent == 0 && !(current == 0))
						{

						}
						else if (current == 0 && !(Previouscurrent == 0))
						{
							ControllingNode.Node.InData.FlushSupplyAndUp(ControllingNode.Node);
						}
						ControllingNode.Node.InData.Data.SupplyingCurrent = current;
						Previouscurrent = current;
					}
				}
			}
			else {
				CircuitResistance = 999999999999;
			}
		}
		PowerSupplyFunction.PowerUpdateCurrentChange(this);
	}
	public override void PowerNetworkUpdate()
	{
		VoltageAtChargePort = ElectricityFunctions.WorkOutVoltageFromConnector(ControllingNode.Node, ResistanceSourceModule.ReactionTo.ConnectingDevice);
		VoltageAtSupplyPort = ElectricityFunctions.WorkOutVoltageFromConnectors(ControllingNode.Node, ControllingNode.CanConnectTo);

		//Logger.Log(VoltageAtChargePort + " < VoltageAtChargePort on " + this);
		//Logger.Log(VoltageAtSupplyPort + " < VoltageAtSupplyPort on " + this);
		BatteryCalculation.PowerNetworkUpdate(this);
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
		//Logger.Log(CurrentCapacity + " < CurrentCapacity" + ControllingNode.Node.InData.Categorytype, Category.Electrical);
	}
	public override VIRCurrent ModifyElectricityOutput(VIRCurrent Current, ElectricalOIinheritance SourceInstance)
	{
		if (SourceInstance != ControllingNode.Node)
		{
			if (!ElectricalManager.Instance.electricalSync.NUCurrentChange.Contains(ControllingNode))
			{
				ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(ControllingNode);
			}
		}
		return Current;
	}

	[RightClickMethod]
	public void ToggleCharge()
	{
		ToggleCanCharge = !ToggleCanCharge;
	}

	[RightClickMethod]
	public void ToggleSupport()
	{
		ToggleCansupport = !ToggleCansupport;
	}
}
