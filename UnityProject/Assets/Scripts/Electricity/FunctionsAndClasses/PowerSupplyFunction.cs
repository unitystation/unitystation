using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowerSupplyFunction  { //Responsible for keeping the update and day to clean up off the supply in check
	public static void TurnOffSupply(ElectricalOIinheritance Supply)
	{
		Supply.Data.ChangeToOff = true;
		Supply.InData.ControllingDevice.isOnForInterface = false;
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}
	public static void TurnOnSupply(ElectricalOIinheritance Supply)
	{
		Supply.Data.ChangeToOff = false;
		Supply.InData.ControllingDevice.isOnForInterface = true;
		ElectricalSynchronisation.AddSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		ElectricalSynchronisation.NUStructureChangeReact.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUResistanceChange.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}

	public static void PowerUpdateStructureChangeReact(ElectricalOIinheritance Supply)
	{
		ElectricalSynchronisation.CircuitSearchLoop(Supply);
	}
	public static void PowerUpdateCurrentChange(ElectricalOIinheritance Supply)
	{
		Supply.FlushSupplyAndUp(Supply.GameObject());
		if (Supply.connectedDevices.Count > 0)
		{
			if (!Supply.Data.ChangeToOff)
			{
				if (Supply.Data.SupplyingCurrent != 0)
				{
					Supply.ElectricityOutput(Supply.Data.SupplyingCurrent, Supply.GameObject());
				}
				else if (Supply.Data.SupplyingVoltage != 0)
				{
					int SourceInstanceID = Supply.GameObject().GetInstanceID();
					Supply.ElectricityOutput((Supply.Data.SupplyingVoltage) / (Supply.Data.InternalResistance + ElectricityFunctions.WorkOutResistance(Supply.Data.ResistanceComingFrom[SourceInstanceID])), Supply.GameObject());
				}

			}
			else {
				foreach (ElectricalOIinheritance connectedDevice in Supply.connectedDevices) {
					ElectricalSynchronisation.NUCurrentChange.Add(connectedDevice.InData.ControllingDevice);
				}
			}
			ELCurrent.Currentloop(Supply.GameObject());
		}

		if (Supply.Data.ChangeToOff)
		{
			Supply.Data.ChangeToOff = false;
			Supply.InData.ControllingDevice.TurnOffCleanup();
			ElectricalSynchronisation.RemoveSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		}

	}

}
