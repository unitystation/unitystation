using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowerSupplyFunction  { //Responsible for keeping the update and day to clean up off the supply in check
	public static void TurnOffSupply(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.Data.ChangeToOff = true;
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.ControllingNode);
	}
	public static void TurnOnSupply(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.Data.ChangeToOff = false;
		ElectricalSynchronisation.AddSupply(Supply.ControllingNode, Supply.ControllingNode.ApplianceType);
		ElectricalSynchronisation.NUStructureChangeReact.Add (Supply.ControllingNode);
		ElectricalSynchronisation.NUResistanceChange.Add (Supply.ControllingNode);
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.ControllingNode);
	}

	public static void PowerUpdateStructureChangeReact(ModuleSupplyingDevice Supply)
	{
		ElectricalSynchronisation.CircuitSearchLoop(Supply.ControllingNode.Node);
	}

	public static void PowerUpdateCurrentChange(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.FlushSupplyAndUp(Supply.gameObject);
		if (!Supply.ControllingNode.Node.Data.ChangeToOff)
		{
			if (Supply.ControllingNode.Node.Data.SupplyingCurrent != 0)
			{
				Supply.ControllingNode.Node.ElectricityOutput(Supply.ControllingNode.Node.Data.SupplyingCurrent, Supply.ControllingNode.Node.GameObject());
			}
			else if (Supply.ControllingNode.Node.Data.SupplyingVoltage != 0)
			{
				int SourceInstanceID = Supply.ControllingNode.Node.GameObject().GetInstanceID();
				float Current = (Supply.SupplyingVoltage) / (Supply.InternalResistance + ElectricityFunctions.WorkOutResistance(Supply.ControllingNode.Node.Data.SupplyDependent[SourceInstanceID].ResistanceComingFrom));
				Supply.ControllingNode.Node.ElectricityOutput(Current, Supply.gameObject);

			}
		}
		else {
			foreach (ElectricalOIinheritance connectedDevice in Supply.ControllingNode.Node.connectedDevices)
			{
				if (ElectricalSynchronisation.ReactiveSuppliesSet.Contains(connectedDevice.InData.Categorytype))
				{
					ElectricalSynchronisation.NUCurrentChange.Add(connectedDevice.InData.ControllingDevice);
				}
			}
		}
		ELCurrent.Currentloop(Supply.gameObject);

		if (Supply.ControllingNode.Node.Data.ChangeToOff)
		{
			Supply.ControllingNode.Node.Data.ChangeToOff = false;
			Supply.ControllingNode.TurnOffCleanup();
			ElectricalSynchronisation.RemoveSupply(Supply.ControllingNode.Node.InData.ControllingDevice, Supply.ControllingNode.Node.InData.Categorytype);
		}
	}
}
