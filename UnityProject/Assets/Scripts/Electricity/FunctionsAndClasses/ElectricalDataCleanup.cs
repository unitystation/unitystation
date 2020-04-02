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
			else {
				if (Object.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Count > 0 || Object.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.Count > 0)
				{
					Pool(Object.Data.SupplyDependent[SourceInstance].ResistanceComingFrom);
					Pool(Object.Data.SupplyDependent[SourceInstance].ResistanceGoingTo);
					foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
					{
						JumpTo.FlushResistanceAndUp(SourceInstance);
					}
					Pool(Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo);
					Pool(Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom);
					Object.Data.SupplyDependent[SourceInstance].SourceVoltage = 0;
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
			else {
				if (Object.Data.SupplyDependent.ContainsKey(SourceInstance))
				{
					if (Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom.Count > 0
					    || Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo.Count > 0)
					{
						Pool(Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo);
						Pool(Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom);
						foreach (IntrinsicElectronicData JumpTo in Object.Data.connections)
						{
							JumpTo.FlushSupplyAndUp(SourceInstance);
						}
					}
					Object.Data.SupplyDependent[SourceInstance].SourceVoltage = 0;
				}
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
			else {
				//SourceInstance
				bool pass = false;
				if (Object.Data.SupplyDependent.ContainsKey(SourceInstance))
				{

					if (Object.Data.SupplyDependent[SourceInstance].Downstream.Count > 0
					|| Object.Data.SupplyDependent[SourceInstance].Upstream.Count > 0)
					{
						pass = true;
					}
					Object.Data.SupplyDependent[SourceInstance].Pool();
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
	//SupplyDependent
	public static void Pool(Dictionary<ElectricalOIinheritance, ElectronicSupplyData> ToPool)
	{
		foreach (var poolling in ToPool)
		{
			poolling.Value.Pool();
		}
		ToPool.Clear();
	}
}
