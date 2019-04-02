using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowerSupplyFunction  { //Responsible for keeping the update and day to clean up off the supply in check
	public static void TurnOffSupply(ElectricalOIinheritance Supply)
	{
		Supply.RemoveSupply(Supply.GameObject());
		//Logger.Log("13");
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}
	public static void TurnOnSupply(ElectricalOIinheritance Supply)
	{
		//Logger.Log("yyyyyyyyyyyyyyyyyyyyy");
		ElectricalSynchronisation.AddSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		ElectricalSynchronisation.NUStructureChangeReact.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUResistanceChange.Add (Supply.InData.ControllingUpdate);
		ElectricalSynchronisation.NUCurrentChange.Add (Supply.InData.ControllingUpdate);
	}

	public static void PowerUpdateStructureChangeReact(ElectricalOIinheritance Supply)
	{
		//Logger.Log("ElectricalSynchronisation.CircuitSearchLoop(Supply);");
		ElectricalSynchronisation.CircuitSearchLoop(Supply);
		if (Supply.Data.ChangeToOff)
		{
			Supply.Data.ChangeToOff = false;
			TurnOffSupply(Supply);
			Supply.InData.ControllingDevice.TurnOffCleanup ();
			ElectricalSynchronisation.RemoveSupply(Supply.InData.ControllingUpdate, Supply.InData.Categorytype);
		}
	}
	public static void PowerUpdateCurrentChange(ElectricalOIinheritance Supply)
	{
		//Logger.Log("PowerUpdateCurrentChange(ElectricalOIinheritance Supply)");
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
			ELCurrent.Currentloop(Supply.GameObject());
		}
	}
}
