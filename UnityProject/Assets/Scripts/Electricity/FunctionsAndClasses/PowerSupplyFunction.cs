using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Responsible for keeping the update and day to clean up off the supply in check
/// </summary>
public static class PowerSupplyFunction  {
	/// <summary>
	/// Called when a Supplying Device is turned off.
	/// </summary>
	/// <param name="Supply">The supplying device that is turned off</param>
	public static void TurnOffSupply(ModuleSupplyingDevice Supply)
	{
		Supply.ControllingNode.Node.InData.Data.ChangeToOff = true;
		ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(Supply.ControllingNode);
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
				if (Supply.current != 0)
				{
					PushCurrentDownline(Supply, Supply.current);
				}
				else if (Supply.SupplyingVoltage != 0)
				{
					float Current = (Supply.SupplyingVoltage) / (Supply.InternalResistance
					+ ElectricityFunctions.WorkOutResistance(Supply.ControllingNode.Node.InData.Data.SupplyDependent[Supply.ControllingNode.Node].ResistanceComingFrom));
					PushCurrentDownline(Supply, Current);
				}
				else if (Supply.ProducingWatts != 0)
 				{
	                float Current =(float) (Math.Sqrt(Supply.ProducingWatts *
	                ElectricityFunctions.WorkOutResistance(Supply.ControllingNode.Node.InData.Data.SupplyDependent[Supply.ControllingNode.Node].ResistanceComingFrom))
	                /ElectricityFunctions.WorkOutResistance(Supply.ControllingNode.Node.InData.Data.SupplyDependent[Supply.ControllingNode.Node].ResistanceComingFrom));
	                PushCurrentDownline(Supply, Current);
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

			sync.RemoveSupply(Supply.ControllingNode, Supply.ControllingNode.Node.InData.Categorytype);
		}
	}


	public static void PushCurrentDownline(ModuleSupplyingDevice Supply, float FloatCurrent)
	{
		Supply.CurrentSource.current = FloatCurrent;
		var WrapCurrentSource = ElectricalPool.GetWrapCurrent();
		WrapCurrentSource.Current = Supply.CurrentSource;
		WrapCurrentSource.Strength = 1;
		var VIR = ElectricalPool.GetVIRCurrent();
		VIR.addCurrent(WrapCurrentSource);
		Supply.ControllingNode.Node.InData.ElectricityOutput(VIR,
			Supply.ControllingNode.Node
		);
	}

}
