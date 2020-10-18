using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class ElectricalDataCleanup
{ //To clean out data on cables and machines
	public static void CleanConnectedDevices(ElectricalOIinheritance Thiswire)
	{
		//Logger.Log ("CleanConnectedDevices" + Thiswire, Category.Electrical);
		foreach (var IsConnectedTo in Thiswire.InData.Data.ResistanceToConnectedDevices)
		{
			IsConnectedTo.Key.connectedDevices.Remove(Thiswire.InData);
		}
		Thiswire.InData.Data.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(ElectricalOIinheritance Thiswire)
	{
		//Logger.Log ("CleanConnectedDevicesFromPower" + Thiswire, Category.Electrical);
		foreach (var IsConnectedTo in Thiswire.connectedDevices)
		{
			IsConnectedTo.Data.ResistanceToConnectedDevices.Remove(Thiswire);
		}
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies
	{
		public static void FlushConnectionAndUp(IntrinsicElectronicData Object)
		{
			Object.Data.CurrentInWire = 0;
			Object.Data.ActualVoltage = 0;
			Object.Data.ResistanceToConnectedDevices.Clear();
			//Object.connectedDevices.Clear();//#
			if (Object.Data.connections.Count > 0)
			{
				List<IntrinsicElectronicData> Backupconnections = new List<IntrinsicElectronicData>(Object.Data.connections); //GC
				Object.Data.connections.Clear();

				foreach (IntrinsicElectronicData JumpTo in Backupconnections)
				{
					JumpTo.FlushConnectionAndUp();

				}
			}
			foreach (KeyValuePair<ElectricalOIinheritance, ElectronicSupplyData> Supply in Object.Data.SupplyDependent)
			{
				Pool(Supply.Value.CurrentGoingTo);
				Pool(Supply.Value.CurrentComingFrom);
				Pool(Supply.Value.ResistanceGoingTo);
				Pool(Supply.Value.ResistanceComingFrom);
				Supply.Value.Upstream.Clear();
				Supply.Value.Downstream.Clear();
				Supply.Value.SourceVoltage = 0;
			}
		}

		public static void FlushResistanceAndUp(IntrinsicElectronicData Object, ElectricalOIinheritance SourceInstance = null)
		{
			if (SourceInstance == null)
			{
				bool pass = false;
				foreach (var Supply in Object.Data.SupplyDependent)
				{
					if (Supply.Value.ResistanceComingFrom.Count > 0)
					{
						pass = true;
						break;
					}
				}
				if (pass)
				{
					foreach (var Supply in Object.Data.SupplyDependent)
					{
						Pool(Supply.Value.ResistanceComingFrom);
						Pool(Supply.Value.ResistanceGoingTo);
						Pool(Supply.Value.CurrentGoingTo);
						Pool(Supply.Value.CurrentComingFrom);
						Supply.Value.SourceVoltage = 0;

					}
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.FlushResistanceAndUp();
					}
					Object.Data.CurrentInWire = 0;
					Object.Data.ActualVoltage = 0;
				}
			}
			else
			{
				ElectronicSupplyData supplyDep = Object.Data.SupplyDependent[SourceInstance];
				if (supplyDep.ResistanceComingFrom.Count > 0 || supplyDep.ResistanceGoingTo.Count > 0)
				{
					Pool(supplyDep.ResistanceComingFrom);
					Pool(supplyDep.ResistanceGoingTo);
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.FlushResistanceAndUp(SourceInstance);
					}
					Pool(supplyDep.CurrentGoingTo);
					Pool(supplyDep.CurrentComingFrom);
					supplyDep.SourceVoltage = 0;
					Object.Data.CurrentInWire = new float();
					Object.Data.ActualVoltage = new float();
				}
			}
		}

		public static void FlushSupplyAndUp(IntrinsicElectronicData Object, ElectricalOIinheritance SourceInstance = null)
		{
			if (SourceInstance == null)
			{
				bool pass = false;
				foreach (var Supply in Object.Data.SupplyDependent)
				{
					if (Supply.Value.CurrentComingFrom.Count > 0)
					{
						pass = true;
						break;
					}
				}
				if (pass)
				{
					foreach (var Supply in Object.Data.SupplyDependent)
					{

						Pool(Supply.Value.CurrentComingFrom);
						Pool(Supply.Value.CurrentGoingTo);
						Supply.Value.SourceVoltage = 0;
					}
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.FlushSupplyAndUp();
					}
					Object.Data.CurrentInWire = 0;
					Object.Data.ActualVoltage = 0;
				}
			}
			else if (Object.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
			{
				if (supplyDep.CurrentComingFrom.Count > 0 || supplyDep.CurrentGoingTo.Count > 0)
				{
					Pool(supplyDep.CurrentGoingTo);
					Pool(supplyDep.CurrentComingFrom);
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.FlushSupplyAndUp(SourceInstance);
					}
				}
				supplyDep.SourceVoltage = 0;
			}
		}

		public static void RemoveSupply(IntrinsicElectronicData Object, ElectricalOIinheritance SourceInstance = null)
		{
			if (SourceInstance == null)
			{
				bool pass = false;
				foreach (var Supply in Object.Data.SupplyDependent)
				{
					if (Supply.Value.Downstream.Count > 0 || Supply.Value.Upstream.Count > 0)
					{
						pass = true;
						break;
					}
				}
				if (pass)
				{
					Pool(Object.Data.SupplyDependent);
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.RemoveSupply();
					}
					Object.Data.CurrentInWire = 0;
					Object.Data.ActualVoltage = 0;
					Object.Data.EstimatedResistance = 0;
					Object.Data.ResistanceToConnectedDevices.Clear();
					//Object.connectedDevices.Clear();#
				}
			}
			else
			{
				bool pass = false;
				if (Object.Data.SupplyDependent.TryGetValue(SourceInstance, out ElectronicSupplyData supplyDep))
				{
					if (supplyDep.Downstream.Count > 0 || supplyDep.Upstream.Count > 0)
					{
						pass = true;
					}
					supplyDep.Pool();
					Object.Data.SupplyDependent.Remove(SourceInstance);
				}

				if (SourceInstance == Object.Present)
				{
					CleanConnectedDevicesFromPower(Object.Present);
					Object.Data.ResistanceToConnectedDevices.Clear();
				}

				if (pass)
				{
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.RemoveSupply(SourceInstance);
					}
				}
			}
		}
	}

	public static void Pool(Dictionary<IntrinsicElectronicData, VIRCurrent> ToPool)
	{
		foreach (var poolling in ToPool)
		{
			poolling.Value.Pool();
		}
		ToPool.Clear();
	}

	public static void Pool(Dictionary<IntrinsicElectronicData, VIRResistances> ToPool)
	{
		foreach (var poolling in ToPool)
		{
			poolling.Value.Pool();
		}
		ToPool.Clear();
	}

	public static void Pool(Dictionary<ElectricalOIinheritance, ElectronicSupplyData> ToPool)
	{
		foreach (var poolling in ToPool)
		{
			poolling.Value.Pool();
		}
		ToPool.Clear();
	}
}