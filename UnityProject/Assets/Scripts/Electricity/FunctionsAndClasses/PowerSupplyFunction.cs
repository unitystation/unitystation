using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowerSupplyFunction  { //Responsible for keeping the update and day to clean up off the supply in check
	public static void TurnOffSupply(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.InData.Data.ChangeToOff = true;
		ElectricalManager.Instance.electricalSync.NUCurrentChange.Add (Supply.ControllingNode);
	}
	public static void TurnOnSupply(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.InData.Data.ChangeToOff = false;
		var sync = ElectricalManager.Instance.electricalSync;
		sync.AddSupply(Supply.ControllingNode, Supply.ControllingNode.ApplianceType);
		sync.NUStructureChangeReact.Add (Supply.ControllingNode);
		sync.NUResistanceChange.Add (Supply.ControllingNode);
		sync.NUCurrentChange.Add (Supply.ControllingNode);
	}

	public static void PowerUpdateStructureChangeReact(ModuleSupplyingDevice Supply)
	{
		ElectricalManager.Instance.electricalSync.CircuitSearchLoop(Supply.ControllingNode.Node);
	}

	public static void PowerUpdateCurrentChange(ModuleSupplyingDevice Supply)
	{
		var sync = ElectricalManager.Instance.electricalSync;
		if (Supply.ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(Supply.ControllingNode.Node)){
			//Logger.Log("PowerUpdateCurrentChange for Supply  > " + Supply.name);
			Supply.ControllingNode.Node.InData.FlushSupplyAndUp(Supply.ControllingNode.Node); //Needs change

			if (!Supply.ControllingNode.Node.InData.Data.ChangeToOff)
			{
				if (Supply.ControllingNode.Node.InData.Data.SupplyingCurrent != 0)
				{
					Supply.CurrentSource.current = Supply.ControllingNode.Node.InData.Data.SupplyingCurrent;
					var WrapCurrentSource = ElectricalPool.GetWrapCurrent();
					WrapCurrentSource.Current = Supply.CurrentSource;
					WrapCurrentSource.Strength = 1;

					var VIR = ElectricalPool.GetVIRCurrent();
					VIR.addCurrent(WrapCurrentSource);



					Supply.ControllingNode.Node.InData.ElectricityOutput(VIR,
																  Supply.ControllingNode.Node);
				}
				else if (Supply.ControllingNode.Node.InData.Data.SupplyingVoltage != 0)
				{
					float Current = (Supply.SupplyingVoltage) / (Supply.InternalResistance
					+ ElectricityFunctions.WorkOutResistance(Supply.ControllingNode.Node.InData.Data.SupplyDependent[Supply.ControllingNode.Node].ResistanceComingFrom));


					Supply.CurrentSource.current = Current;
					var WrapCurrentSource = ElectricalPool.GetWrapCurrent();
					//Logger.Log("Supply.CurrentSource.current" + Supply.CurrentSource.current);
					WrapCurrentSource.Current = Supply.CurrentSource;
					WrapCurrentSource.Strength = 1;
					//Logger.Log("2 > " + WrapCurrentSource.Current.current);
					var VIR = ElectricalPool.GetVIRCurrent();
					VIR.addCurrent(WrapCurrentSource);

					//Logger.Log("3 > " + VIR);
					Supply.ControllingNode.Node.InData.ElectricityOutput(VIR,
																  Supply.ControllingNode.Node
																  );
					//Logger.Log("END > " + VIR);
				}
			}
			else {
				foreach (var  connectedDevice in Supply.ControllingNode.Node.connectedDevices)
				{
					if (sync.ReactiveSuppliesSet.Contains(connectedDevice.Categorytype))
					{
						sync.NUCurrentChange.Add(connectedDevice.ControllingDevice);
					}
				}
			}
		}
		//ELCurrent.Currentloop(Supply.gameObject);

		if (Supply.ControllingNode.Node.InData.Data.ChangeToOff)
		{
			Supply.ControllingNode.Node.InData.RemoveSupply(Supply.ControllingNode.Node);
			Supply.ControllingNode.Node.InData.Data.ChangeToOff = false;
			Supply.ControllingNode.TurnOffCleanup();

			sync.RemoveSupply(Supply.ControllingNode.Node.InData.ControllingDevice, Supply.ControllingNode.Node.InData.Categorytype);
		}
	}
}
