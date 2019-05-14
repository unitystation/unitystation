using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ElectricalDataCleanup { //To clean out data on cables and machines
	public static void CleanConnectedDevices(ElectricalOIinheritance Thiswire){
		//Logger.Log ("Cleaning it out");
		foreach (KeyValuePair<ElectricalOIinheritance,HashSet<PowerTypeCategory>> IsConnectedTo in Thiswire.Data.ResistanceToConnectedDevices) {
			IsConnectedTo.Key.connectedDevices.Remove (Thiswire);
		}
		Thiswire.Data.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(ElectricalOIinheritance Thiswire){
		//Logger.Log ("Cleaning it out");
		foreach (ElectricalOIinheritance IsConnectedTo in Thiswire.connectedDevices) {
			IsConnectedTo.Data.ResistanceToConnectedDevices.Remove (Thiswire);
		}
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies{
		public static void FlushConnectionAndUp (ElectricalOIinheritance Object){

			Object.Data.FirstPresent = 0;
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
				//bool log = false;
				//if (Object.InData.Categorytype == PowerTypeCategory.SolarPanelController) {
				//	log = true;
				//}

				foreach (KeyValuePair<int, ElectronicSupplyData> Supply in Object.Data.SupplyDependent)
				{
					foreach (ElectricalOIinheritance Device in Supply.Value.Downstream)
					{
						Device.FlushConnectionAndUp();
						//if (log)
						//{
						//	Logger.Log("Device1" + Device);
						//}
					}
					foreach (ElectricalOIinheritance Device in Supply.Value.Upstream)
					{
						Device.FlushConnectionAndUp();
						//if (log)
						//{
						//	Logger.Log("Device1" + Device);
						//}
					}
				}
				foreach (KeyValuePair<int, ElectronicSupplyData> Supply in Object.Data.SupplyDependent)
				{
					Supply.Value.CurrentComingFrom.Clear();
					Supply.Value.CurrentGoingTo.Clear();
					Supply.Value.ResistanceGoingTo.Clear();
					Supply.Value.ResistanceComingFrom.Clear();
					Supply.Value.Upstream.Clear();
					Supply.Value.Downstream.Clear();
					Supply.Value.SourceVoltages = 0;
				}
			}
		}

		public static void FlushResistanceAndUp (ElectricalOIinheritance Object,  GameObject SourceInstance = null  ){
			if (SourceInstance == null) {
				Logger.Log ("yo do not?");
				bool pass = false;
				foreach (var Supply in Object.Data.SupplyDependent) {
					if (Supply.Value.ResistanceComingFrom.Count > 0) {
						pass = true;
					}
				}
				if (pass) {
					foreach (var Supply in Object.Data.SupplyDependent)
					{
						Supply.Value.ResistanceComingFrom.Clear();
						Supply.Value.ResistanceGoingTo.Clear();
						Supply.Value.CurrentGoingTo.Clear();
						Supply.Value.CurrentComingFrom.Clear();
						Supply.Value.SourceVoltages = 0;
	
					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
					{
						JumpTo.FlushResistanceAndUp();
					}
					Object.Data.CurrentInWire = new float();
					Object.Data.ActualVoltage = new float();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();

				if (Object.Data.SupplyDependent[InstanceID].ResistanceComingFrom.Count > 0 || Object.Data.SupplyDependent[InstanceID].ResistanceGoingTo.Count > 0) {
					Object.Data.SupplyDependent[InstanceID].ResistanceComingFrom.Clear();
					Object.Data.SupplyDependent[InstanceID].ResistanceGoingTo.Clear();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp (SourceInstance);
					}
					Object.Data.SupplyDependent[InstanceID].CurrentGoingTo.Clear();
					Object.Data.SupplyDependent[InstanceID].CurrentComingFrom.Clear();
					Object.Data.SupplyDependent[InstanceID].SourceVoltages = 0;
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}
			}
		}

		public static void FlushSupplyAndUp (ElectricalOIinheritance Object,GameObject SourceInstance = null ){
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
						Supply.Value.SourceVoltages = 0;
					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
					{
						JumpTo.FlushSupplyAndUp();
					}
					Object.Data.CurrentInWire = new float();
					Object.Data.ActualVoltage = new float();
				}
			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				//Logger.Log(Object.GameObject().name); 
				if (Object.Data.SupplyDependent.ContainsKey(InstanceID))
				{
					if (Object.Data.SupplyDependent[InstanceID].CurrentComingFrom.Count > 0 || Object.Data.SupplyDependent[InstanceID].CurrentGoingTo.Count > 0)
					{
						Object.Data.SupplyDependent[InstanceID].CurrentGoingTo.Clear();
						Object.Data.SupplyDependent[InstanceID].CurrentComingFrom.Clear();
						foreach (ElectricalOIinheritance JumpTo in Object.Data.connections)
						{
							JumpTo.FlushSupplyAndUp(SourceInstance);
						}
					}
					Object.Data.SupplyDependent[InstanceID].SourceVoltages = 0;
				}

				ElectricityFunctions.WorkOutActualNumbers (Object);
			}
		}

		public static void RemoveSupply(ElectricalOIinheritance Object,GameObject SourceInstance = null ){		
			if (SourceInstance == null) {
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
					Object.Data.SupplyDependent.Clear();
					Object.Data.FirstPresent = new int ();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply ();
					}
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
					Object.Data.EstimatedResistance = new float ();
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
					Object.Data.ResistanceToConnectedDevices.Clear();
					Object.connectedDevices.Clear();
				}
			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.SupplyDependent[InstanceID].Downstream.Count > 0) {
					if (Object.Data.FirstPresent == InstanceID) {
						Object.Data.FirstPresent = new int ();
					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply (SourceInstance);
					}
					if (InstanceID == Object.GameObject ().GetInstanceID ()) {
						CleanConnectedDevicesFromPower (Object);
						Object.Data.ResistanceToConnectedDevices.Clear();
					}
					Object.Data.SupplyDependent.Remove(InstanceID);
					ElectricityFunctions.WorkOutActualNumbers(Object);
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
				}
			}
		}
	}
}
