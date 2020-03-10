using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ElectricalDataCleanup { //To clean out data on cables and machines
	public static void CleanConnectedDevices(ElectricalOIinheritance Thiswire){ 
		//Logger.Log ("CleanConnectedDevices" + Thiswire, Category.Electrical);
		foreach (var IsConnectedTo in Thiswire.Data.ResistanceToConnectedDevices) {
			IsConnectedTo.Key.connectedDevices.Remove (Thiswire);
		}
		Thiswire.Data.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(ElectricalOIinheritance Thiswire){
		//Logger.Log ("CleanConnectedDevicesFromPower" + Thiswire, Category.Electrical);
		foreach (ElectricalOIinheritance IsConnectedTo in Thiswire.connectedDevices) {
			IsConnectedTo.Data.ResistanceToConnectedDevices.Remove (Thiswire);
		}
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies{
		public static void FlushConnectionAndUp (ElectricalOIinheritance Object){

			Object.Data.CurrentInWire = new float();
			Object.Data.ActualVoltage = new float();
			Object.Data.ResistanceToConnectedDevices.Clear();
			Object.connectedDevices.Clear();
			if (Object.Data.connections.Count > 0) {
				List<ElectricalOIinheritance> Backupconnections = new List<ElectricalOIinheritance>(Object.Data.connections);
				Object.Data.connections.Clear();

				foreach (ElectricalOIinheritance JumpTo in Backupconnections) {
					JumpTo.FlushConnectionAndUp ();
	
				}
				foreach (KeyValuePair<ElectricalOIinheritance, ElectronicSupplyData> Supply in Object.Data.SupplyDependent)
				{
					foreach (ElectricalOIinheritance Device in Supply.Value.Downstream)
					{
						Device.FlushConnectionAndUp();
					}
					foreach (ElectricalOIinheritance Device in Supply.Value.Upstream)
					{
						Device.FlushConnectionAndUp();
					}
				}
				foreach (KeyValuePair<ElectricalOIinheritance, ElectronicSupplyData> Supply in Object.Data.SupplyDependent)
				{
					Supply.Value.CurrentComingFrom.Clear();
					Supply.Value.CurrentGoingTo.Clear();
					Supply.Value.ResistanceGoingTo.Clear();
					Supply.Value.ResistanceComingFrom.Clear();
					Supply.Value.Upstream.Clear();
					Supply.Value.Downstream.Clear();
					Supply.Value.SourceVoltages.Clear();
				}
			}
		}

		public static void FlushResistanceAndUp (ElectricalOIinheritance Object,  ElectricalOIinheritance SourceInstance = null  ){
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
						Supply.Value.ResistanceComingFrom.Clear();
						Supply.Value.ResistanceGoingTo.Clear();
						Supply.Value.CurrentGoingTo.Clear();
						Supply.Value.CurrentComingFrom.Clear();
						Supply.Value.SourceVoltages.Clear();

					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
					{
						JumpTo.FlushResistanceAndUp();
					}
					Object.Data.CurrentInWire = new float();
					Object.Data.ActualVoltage = new float();
				}

			} else {
				if (Object.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Count > 0 || Object.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.Count > 0) {
					Object.Data.SupplyDependent[SourceInstance].ResistanceComingFrom.Clear();
					Object.Data.SupplyDependent[SourceInstance].ResistanceGoingTo.Clear();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp (SourceInstance);
					}
					Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo.Clear();
					Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom.Clear();
					Object.Data.SupplyDependent[SourceInstance].SourceVoltages.Clear();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}
			}
		}

		public static void FlushSupplyAndUp (ElectricalOIinheritance Object,ElectricalOIinheritance SourceInstance = null ){
			if (SourceInstance == null) {
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
						Supply.Value.CurrentComingFrom.Clear();
						Supply.Value.CurrentGoingTo.Clear();
						Supply.Value.SourceVoltages.Clear();
					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
					{
						JumpTo.FlushSupplyAndUp();
					}
					Object.Data.CurrentInWire = new float();
					Object.Data.ActualVoltage = new float();
				}
			} else {
				if (Object.Data.SupplyDependent.ContainsKey(SourceInstance))
				{
					if (Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom.Count > 0 || Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo.Count > 0)
					{
						Object.Data.SupplyDependent[SourceInstance].CurrentGoingTo.Clear();
						Object.Data.SupplyDependent[SourceInstance].CurrentComingFrom.Clear();
						foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
						{
							JumpTo.FlushSupplyAndUp(SourceInstance);
						}
					}
					Object.Data.SupplyDependent[SourceInstance].SourceVoltages.Clear();
				}

				ElectricityFunctions.WorkOutActualNumbers (Object);
			}
		}

		public static void RemoveSupply(ElectricalOIinheritance Object,ElectricalDirectionStep Path ,ElectricalOIinheritance SourceInstance = null ){		
			if (SourceInstance == null) {
				//bool pass = false;
				//foreach (var Supply in Object.Data.SupplyDependent)
				//{
				//	if (Supply.Value.Downstream.Count > 0 || Supply.Value.Upstream.Count > 0)
				//	{
				//		pass = true;
				//	}
				//}
				//if (pass)
				//{
				//	Object.Data.SupplyDependent.Clear();
				//	foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
				//		JumpTo.RemoveSupply ();
				//	}
				//	Object.Data.CurrentInWire = new float ();
				//	Object.Data.ActualVoltage = new float ();
				//	Object.Data.EstimatedResistance = new float ();
				//	Object.Data.ResistanceToConnectedDevices.Clear();
				//	Object.connectedDevices.Clear();
				//}
			} else {
				if (Path.Downstream.Count > 0) {
					foreach (var JumpTo in Path.Downstream) {
						if (JumpTo.InData?.Present != null)
						{
							JumpTo.InData.Present.RemoveSupply(JumpTo, SourceInstance);
						}
					}
					if (SourceInstance == Object) {
						CleanConnectedDevicesFromPower (Object);
						Object.Data.ResistanceToConnectedDevices.Clear();
					}
					Object.Data.SupplyDependent.Remove(SourceInstance);
					ElectricityFunctions.WorkOutActualNumbers(Object);
				}
			}
		}
	}
}
