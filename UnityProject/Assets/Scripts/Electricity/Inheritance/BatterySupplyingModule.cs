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

	public float PullLastDeductedTime = 0;
	public float ChargLastDeductedTime = 0;
	public float PullingWatts = 0;
	public float ChargingWatts = 0;
	public float CircuitResistance = 0;
	public float ActualVoltage = 0;
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
		};
		ModuleType = ElectricalModuleTypeCategory.BatterySupplyingDevice;
		ControllingNode = Node;
		Node.AddModule(this);
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
		if (ControllingNode.Node.Data.ResistanceComingFrom.ContainsKey(ControllingNode.Node.gameObject.GetInstanceID()))
		{
			ControllingNode.Node.FlushSupplyAndUp(ControllingNode.Node.gameObject); //Room for optimisation
			CircuitResistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.Data.ResistanceComingFrom[ControllingNode.Node.gameObject.GetInstanceID()]); // //!!
			ActualVoltage = ControllingNode.Node.Data.ActualVoltage;

			BatteryCalculation.PowerUpdateCurrentChange(this);

			if (current != Previouscurrent)
			{
				if (Previouscurrent == 0 && !(current <= 0))
				{

				}
				else if (current == 0 && !(Previouscurrent <= 0))
				{
					ControllingNode.Node.FlushSupplyAndUp(ControllingNode.Node.gameObject);
				}
				ControllingNode.Node.Data.SupplyingCurrent = current;
				Previouscurrent = current;
			}
		}
		else {
			//not  Getting reset on  Cable cut
			CircuitResistance = 999999999999;
		}
		PowerSupplyFunction.PowerUpdateCurrentChange(this);
	}
	public override void PowerNetworkUpdate()
	{
		ActualVoltage = ControllingNode.Node.Data.ActualVoltage;
		BatteryCalculation.PowerNetworkUpdate(this);
		if (current != Previouscurrent | SupplyingVoltage != PreviousSupplyingVoltage | InternalResistance != PreviousInternalResistance)
		{
			ControllingNode.Node.Data.SupplyingCurrent = current;
			Previouscurrent = current;

			ControllingNode.Node.Data.SupplyingVoltage = SupplyingVoltage;
			PreviousSupplyingVoltage = SupplyingVoltage;

			ControllingNode.Node.Data.InternalResistance = InternalResistance;
			PreviousInternalResistance = InternalResistance;

			ElectricalSynchronisation.NUCurrentChange.Add(ControllingNode.Node.InData.ControllingDevice);
		}
		//Logger.Log(CurrentCapacity + " < CurrentCapacity", Category.Electrical);
	}
}
