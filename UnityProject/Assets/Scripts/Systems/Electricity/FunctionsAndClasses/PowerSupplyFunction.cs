using System;
using System.Collections;
using System.Collections.Generic;
using Logs;

namespace Systems.Electricity.NodeModules
{
	/// <summary>
	/// Responsible for keeping the update and day to clean up off the supply in check
	/// </summary>
	public static class PowerSupplyFunction
	{
		/// <summary>
		/// Called when a Supplying Device is turned off.
		/// </summary>
		/// <param name="supply">The supplying device that is turned off</param>
		public static void TurnOffSupply(ModuleSupplyingDevice supply)
		{
			if (supply.ControllingNode == null)
			{
				Loggy.LogError("Supply.ControllingNode == null", Category.Electrical);
				return;
			}

			supply.ControllingNode.Node.InData.Data.ChangeToOff = true;
			ElectricalManager.Instance.electricalSync.NUCurrentChange.Add(supply.ControllingNode);
		}

		public static void TurnOnSupply(ModuleSupplyingDevice supply)
		{
			supply.ControllingNode.Node.InData.Data.ChangeToOff = false;
			var sync = ElectricalManager.Instance.electricalSync;
			sync.AddSupply(supply.ControllingNode, supply.ControllingNode.ApplianceType);
			sync.NUStructureChangeReact.Add(supply.ControllingNode);
			sync.NUResistanceChange.Add(supply.ControllingNode);
			sync.NUCurrentChange.Add(supply.ControllingNode);
		}

		public static void PowerUpdateStructureChangeReact(ModuleSupplyingDevice supply)
		{
			ElectricalManager.Instance.electricalSync.CircuitSearchLoop(supply.ControllingNode.Node);
		}

		public static void PowerUpdateCurrentChange(ModuleSupplyingDevice supply)
		{
			var sync = ElectricalManager.Instance.electricalSync;
			if (supply.ControllingNode.Node.InData.Data.SupplyDependent.ContainsKey(supply.ControllingNode.Node))
			{
				supply.ControllingNode.Node.InData.FlushSupplyAndUp(supply.ControllingNode.Node); //Needs change

				//Reactive supplies are Triggered by the Electrical In-N-Out

				if (supply.current != 0 && supply.ControllingNode.Node.InData.Data.ChangeToOff == false)
				{
					PushCurrentDownline(supply, supply.current);
				}
				else if (supply.SupplyingVoltage != 0 && supply.ControllingNode.Node.InData.Data.ChangeToOff == false)
				{
					float Current = (supply.SupplyingVoltage) / (supply.InternalResistance
					                                             + ElectricityFunctions.WorkOutResistance(supply
						                                             .ControllingNode.Node.InData.Data
						                                             .SupplyDependent[supply.ControllingNode.Node]
						                                             .ResistanceComingFrom));
					PushCurrentDownline(supply, Current);
				}
				else if (supply.ProducingWatts != 0 && supply.ControllingNode.Node.InData.Data.ChangeToOff == false)
				{
					float Current = (float) (Math.Sqrt(supply.ProducingWatts *
					                                   ElectricityFunctions.WorkOutResistance(supply.ControllingNode
						                                   .Node.InData.Data
						                                   .SupplyDependent[supply.ControllingNode.Node]
						                                   .ResistanceComingFrom))
					                         / ElectricityFunctions.WorkOutResistance(supply.ControllingNode.Node.InData
						                         .Data.SupplyDependent[supply.ControllingNode.Node]
						                         .ResistanceComingFrom));
					PushCurrentDownline(supply, Current);
				}
				else
				{
					foreach (var connectedDevice in supply.ControllingNode.Node.connectedDevices) //Makes it so reactive supplies can react to no Current
					{
						if (sync.ReactiveSuppliesSet.Contains(connectedDevice.Categorytype))
						{
							sync.NUCurrentChange.Add(connectedDevice.ControllingDevice);
						}
					}
				}
			}

			if (supply.ControllingNode.Node.InData.Data.ChangeToOff)
			{
				supply.ControllingNode.Node.InData.RemoveSupply(supply.ControllingNode.Node);
				supply.ControllingNode.Node.InData.Data.ChangeToOff = false;
				supply.ControllingNode.TurnOffCleanup();

				sync.RemoveSupply(supply.ControllingNode, supply.ControllingNode.Node.InData.Categorytype);
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
}