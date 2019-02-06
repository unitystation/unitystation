using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowerSupplyFunction  { //Responsible for keeping the update and day to clean up off the supply in check
	public static void TurnOffSupply(IElectricityIO Supply)
	{
		Supply.RemoveSupply(Supply.GameObject());
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}
	public static void TurnOnSupply(IElectricityIO Supply)
	{
		ElectricalSynchronisation.AddSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		ElectricalSynchronisation.NUStructureChangeReact.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUResistanceChange.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}

	public static void PowerUpdateStructureChangeReact(IElectricityIO Supply)
	{
		ElectricityFunctions.CircuitSearchLoop(Supply, Supply.GameObject().GetComponent<IProvidePower>());
		if (Supply.Data.ChangeToOff)
		{
			Supply.Data.ChangeToOff = false;
			TurnOffSupply(Supply);
			Supply.InData.ControllingDevice.TurnOffCleanup ();
			ElectricalSynchronisation.RemoveSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		}
	}
	public static void PowerUpdateCurrentChange(IElectricityIO Supply)
	{
		Supply.FlushSupplyAndUp(Supply.GameObject());
		if (Supply.connectedDevices.Count > 0)
		{
			if (Supply.Data.SupplyingCurrent != 0)
			{
				Supply.ElectricityOutput(Supply.Data.SupplyingCurrent, Supply.GameObject());
			}
			else if (Supply.Data.SupplyingVoltage != 0) {
				int SourceInstanceID = Supply.GameObject().GetInstanceID();
				Supply.ElectricityOutput((Supply.Data.SupplyingVoltage)/(Supply.Data.InternalResistance + ElectricityFunctions.WorkOutResistance(Supply.Data.ResistanceComingFrom[SourceInstanceID])), Supply.GameObject());
			}
		}
	}
}
